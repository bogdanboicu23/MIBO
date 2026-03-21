from __future__ import annotations

from functools import lru_cache
from typing import Any

from agent.external_services.action_service import get_action_service_client
from agent.external_services.base import ActionServiceQueryDefinition, SupportsActionQueries

WEATHER_CURRENT_QUERY = ActionServiceQueryDefinition(
    data_source_id="weather.current",
    handler="weather.current.get",
)
WEATHER_FORECAST_QUERY = ActionServiceQueryDefinition(
    data_source_id="weather.forecast",
    handler="weather.forecast.get",
)


class WeatherService:
    def __init__(self, action_queries: SupportsActionQueries) -> None:
        self._action_queries = action_queries

    async def get_current_weather(self, city: str) -> dict[str, Any]:
        result = await self._action_queries.query(
            WEATHER_CURRENT_QUERY,
            args={"city": city},
        )
        return result if isinstance(result, dict) else {}

    async def get_forecast(self, city: str) -> dict[str, Any]:
        result = await self._action_queries.query(
            WEATHER_FORECAST_QUERY,
            args={"city": city},
        )
        return result if isinstance(result, dict) else {}


@lru_cache(maxsize=1)
def get_weather_service() -> WeatherService:
    return WeatherService(get_action_service_client())
