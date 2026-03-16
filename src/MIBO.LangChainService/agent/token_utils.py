from __future__ import annotations

import json
from typing import Any

try:
    import tiktoken
except ImportError:  # pragma: no cover
    tiktoken = None


_ENCODING = None
if tiktoken is not None:
    try:
        _ENCODING = tiktoken.get_encoding("cl100k_base")
    except Exception:  # pragma: no cover
        _ENCODING = None


def serialize_for_llm(value: Any) -> str:
    if isinstance(value, str):
        return value

    try:
        return json.dumps(value, ensure_ascii=False, default=str)
    except TypeError:
        return str(value)


def count_tokens(text: str) -> int:
    if not text:
        return 0

    if _ENCODING is None:
        return max(1, len(text) // 4)

    return len(_ENCODING.encode(text))


def truncate_to_tokens(value: Any, max_tokens: int) -> str:
    text = serialize_for_llm(value)
    if count_tokens(text) <= max_tokens:
        return text

    if _ENCODING is None:
        return text[: max_tokens * 4].rstrip() + "..."

    encoded = _ENCODING.encode(text)
    truncated = _ENCODING.decode(encoded[:max_tokens]).rstrip()
    return f"{truncated}..."
