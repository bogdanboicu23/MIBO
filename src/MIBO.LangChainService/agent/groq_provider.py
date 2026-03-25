"""Centralised Groq API-key pool with round-robin rotation and automatic
retry on rate-limit / quota errors.

Environment variables
---------------------
GROQ_API_KEYS  – comma-separated list of keys (preferred)
GROQ_API_KEY   – single key fallback for backward compatibility
"""

from __future__ import annotations

import logging
import os
import threading
from typing import Any, AsyncIterator, Sequence

from langchain_core.messages import BaseMessage
from langchain_groq import ChatGroq

logger = logging.getLogger(__name__)

_DEFAULT_MODEL = "llama-3.3-70b-versatile"


class GroqKeyPool:
    """Thread-safe singleton that hands out API keys in round-robin order."""

    _instance: GroqKeyPool | None = None
    _lock = threading.Lock()

    def __new__(cls) -> GroqKeyPool:
        with cls._lock:
            if cls._instance is None:
                cls._instance = super().__new__(cls)
                cls._instance._init_keys()
            return cls._instance

    def _init_keys(self) -> None:
        raw = os.getenv("GROQ_API_KEYS", "")
        keys = [k.strip() for k in raw.split(",") if k.strip()]
        if not keys:
            single = os.getenv("GROQ_API_KEY", "").strip()
            if single:
                keys = [single]
        self._keys: list[str] = keys
        self._index = 0
        self._key_lock = threading.Lock()
        if keys:
            logger.info("GroqKeyPool initialised with %d key(s).", len(keys))
        else:
            logger.warning("GroqKeyPool: no API keys configured.")

    @property
    def size(self) -> int:
        return len(self._keys)

    def next_key(self) -> str:
        if not self._keys:
            raise RuntimeError(
                "No Groq API keys configured. Set GROQ_API_KEYS or GROQ_API_KEY."
            )
        with self._key_lock:
            key = self._keys[self._index % len(self._keys)]
            self._index += 1
            return key


_pool = GroqKeyPool()


def build_groq_model(
    *,
    model: str = _DEFAULT_MODEL,
    temperature: float = 0,
    streaming: bool = False,
) -> ChatGroq:
    """Create a ``ChatGroq`` instance using the next key from the pool."""
    return ChatGroq(
        groq_api_key=_pool.next_key(),
        model=model,
        temperature=temperature,
        streaming=streaming,
    )


def _is_rate_limit_error(exc: BaseException) -> bool:
    """Return *True* for Groq rate-limit / quota errors regardless of how
    they surface (raw ``groq`` SDK exception **or** wrapped by LangChain)."""
    name = type(exc).__name__
    if name in ("RateLimitError", "AuthenticationError"):
        return True
    # LangChain may wrap the original error.
    cause = getattr(exc, "__cause__", None) or getattr(exc, "__context__", None)
    if cause is not None and type(cause).__name__ in ("RateLimitError", "AuthenticationError"):
        return True
    # HTTP-status based heuristic (works for httpx / requests wrappers).
    status = getattr(exc, "status_code", None) or getattr(exc, "code", None)
    if status in (429, 401):
        return True
    return False


async def invoke_with_rotation(
    messages: Sequence[BaseMessage],
    *,
    model: str = _DEFAULT_MODEL,
    temperature: float = 0,
) -> Any:
    """Call ``ainvoke`` with automatic key rotation on rate-limit errors."""
    max_attempts = min(3, _pool.size) if _pool.size > 1 else 1
    last_exc: BaseException | None = None

    for attempt in range(max_attempts):
        llm = build_groq_model(model=model, temperature=temperature, streaming=False)
        try:
            return await llm.ainvoke(list(messages))
        except Exception as exc:
            if _is_rate_limit_error(exc) and attempt < max_attempts - 1:
                logger.warning(
                    "Groq rate-limit hit (attempt %d/%d) — rotating key.",
                    attempt + 1,
                    max_attempts,
                )
                last_exc = exc
                continue
            raise

    raise last_exc  # type: ignore[misc]


async def astream_with_rotation(
    messages: Sequence[BaseMessage],
    *,
    model: str = _DEFAULT_MODEL,
    temperature: float = 0,
) -> AsyncIterator[Any]:
    """Yield chunks from ``astream`` with automatic key rotation on
    rate-limit errors raised *before* the first chunk is emitted."""
    max_attempts = min(3, _pool.size) if _pool.size > 1 else 1
    last_exc: BaseException | None = None

    for attempt in range(max_attempts):
        llm = build_groq_model(model=model, temperature=temperature, streaming=True)
        try:
            async for chunk in llm.astream(list(messages)):
                yield chunk
            return  # stream completed successfully
        except Exception as exc:
            if _is_rate_limit_error(exc) and attempt < max_attempts - 1:
                logger.warning(
                    "Groq rate-limit hit during stream (attempt %d/%d) — rotating key.",
                    attempt + 1,
                    max_attempts,
                )
                last_exc = exc
                continue
            raise

    raise last_exc  # type: ignore[misc]
