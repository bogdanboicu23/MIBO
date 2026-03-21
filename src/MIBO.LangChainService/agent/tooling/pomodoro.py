from __future__ import annotations

from typing import Any

from langchain_core.tools import tool

from agent.external_services.pomodoro import get_pomodoro_service

pomodoro_service = get_pomodoro_service()


@tool
async def start_pomodoro(
    work_minutes: int = 25,
    short_break_minutes: int = 5,
    long_break_minutes: int = 15,
    sessions_before_long_break: int = 4,
) -> dict[str, Any]:
    """Start a Pomodoro timer with configurable durations. Returns timer configuration for the frontend to render an interactive countdown."""
    return await pomodoro_service.get_config(
        work_minutes=work_minutes,
        short_break_minutes=short_break_minutes,
        long_break_minutes=long_break_minutes,
        sessions_before_long_break=sessions_before_long_break,
    )
