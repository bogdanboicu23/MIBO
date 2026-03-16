from __future__ import annotations

from typing import Any, Awaitable, Callable, TypedDict


StreamCallback = Callable[[dict[str, Any]], Awaitable[None]]


class PipelineState(TypedDict):
    session_id: str
    user_message: str
    conversation_history: list[dict[str, Any]]
    intent: dict[str, Any]
    plan: dict[str, Any]
    tool_results: list[dict[str, Any]]
    final_response: dict[str, Any]
    stream_callback: StreamCallback | None
