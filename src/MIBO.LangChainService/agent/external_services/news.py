from __future__ import annotations

from functools import lru_cache
from typing import Any

from agent.external_services.action_service import get_action_service_client
from agent.external_services.base import ActionServiceQueryDefinition, SupportsActionQueries

NEWS_HEADLINES_QUERY = ActionServiceQueryDefinition(
    data_source_id="news.headlines",
    handler="news.headlines",
)
NEWS_SEARCH_QUERY = ActionServiceQueryDefinition(
    data_source_id="news.search",
    handler="news.search",
)


class NewsService:
    def __init__(self, action_queries: SupportsActionQueries) -> None:
        self._action_queries = action_queries

    async def get_headlines(self, country: str = "us", category: str = "") -> dict[str, Any]:
        args: dict[str, Any] = {"country": country}
        if category:
            args["category"] = category
        result = await self._action_queries.query(
            NEWS_HEADLINES_QUERY,
            args=args,
        )
        return result if isinstance(result, dict) else {}

    async def search(self, query: str, sort_by: str = "publishedAt") -> dict[str, Any]:
        result = await self._action_queries.query(
            NEWS_SEARCH_QUERY,
            args={"query": query, "sortBy": sort_by},
        )
        return result if isinstance(result, dict) else {}


@lru_cache(maxsize=1)
def get_news_service() -> NewsService:
    return NewsService(get_action_service_client())