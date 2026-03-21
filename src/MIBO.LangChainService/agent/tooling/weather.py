from __future__ import annotations

from typing import Any

from langchain_core.tools import tool

from agent.external_services.weather import get_weather_service

weather_service = get_weather_service()


@tool
async def get_current_weather(city: str) -> dict[str, Any]:
    """Get current weather for a city including temperature, humidity, wind, and conditions."""
    return await weather_service.get_current_weather(city=city)


@tool
async def get_weather_forecast(city: str) -> dict[str, Any]:
    """Get 5-day weather forecast for a city with 3-hour intervals."""
    return await weather_service.get_forecast(city=city)
