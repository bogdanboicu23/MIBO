from __future__ import annotations

from typing import Any

from langchain_core.tools import tool

from agent.external_services.news import get_news_service

news_service = get_news_service()


@tool
async def get_headlines(country: str = "us", category: str = "") -> dict[str, Any]:
    """Get top news headlines. Optionally filter by country code and category (business, entertainment, general, health, science, sports, technology)."""
    return await news_service.get_headlines(country=country, category=category)


@tool
async def search_news(query: str, sort_by: str = "publishedAt") -> dict[str, Any]:
    """Search for news articles by keyword. sort_by can be relevancy, popularity, or publishedAt."""
    return await news_service.search(query=query, sort_by=sort_by)