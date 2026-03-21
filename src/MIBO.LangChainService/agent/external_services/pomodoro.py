from __future__ import annotations

from functools import lru_cache
from typing import Any

from agent.external_services.action_service import get_action_service_client
from agent.external_services.base import ActionServiceQueryDefinition, SupportsActionQueries

POMODORO_CONFIG_QUERY = ActionServiceQueryDefinition(
    data_source_id="pomodoro.config",
    handler="pomodoro.config.get",
)


class PomodoroService:
    def __init__(self, action_queries: SupportsActionQueries) -> None:
        self._action_queries = action_queries

    async def get_config(
        self,
        work_minutes: int = 25,
        short_break_minutes: int = 5,
        long_break_minutes: int = 15,
        sessions_before_long_break: int = 4,
    ) -> dict[str, Any]:
        result = await self._action_queries.query(
            POMODORO_CONFIG_QUERY,
            args={
                "workMinutes": work_minutes,
                "shortBreakMinutes": short_break_minutes,
                "longBreakMinutes": long_break_minutes,
                "sessionsBeforeLongBreak": sessions_before_long_break,
            },
        )
        return result if isinstance(result, dict) else {}


@lru_cache(maxsize=1)
def get_pomodoro_service() -> PomodoroService:
    return PomodoroService(get_action_service_client())
