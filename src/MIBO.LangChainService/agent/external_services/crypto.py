from __future__ import annotations

from functools import lru_cache
from typing import Any

from agent.external_services.action_service import get_action_service_client
from agent.external_services.base import ActionServiceQueryDefinition, SupportsActionQueries

CRYPTO_MARKETS_QUERY = ActionServiceQueryDefinition(
    data_source_id="crypto.markets",
    handler="crypto.markets",
)
CRYPTO_PRICE_QUERY = ActionServiceQueryDefinition(
    data_source_id="crypto.price",
    handler="crypto.price",
)
CRYPTO_TRENDING_QUERY = ActionServiceQueryDefinition(
    data_source_id="crypto.trending",
    handler="crypto.trending",
)
CRYPTO_SEARCH_QUERY = ActionServiceQueryDefinition(
    data_source_id="crypto.search",
    handler="crypto.search",
)
CRYPTO_COIN_DETAIL_QUERY = ActionServiceQueryDefinition(
    data_source_id="crypto.coin.detail",
    handler="crypto.coin.detail",
)


class CryptoService:
    def __init__(self, action_queries: SupportsActionQueries) -> None:
        self._action_queries = action_queries

    async def get_markets(self, vs_currency: str = "usd", limit: int = 20) -> dict[str, Any]:
        result = await self._action_queries.query(
            CRYPTO_MARKETS_QUERY,
            args={"vs_currency": vs_currency, "limit": limit},
        )
        return result if isinstance(result, dict) else {}

    async def get_price(self, ids: str, vs_currencies: str = "usd") -> dict[str, Any]:
        result = await self._action_queries.query(
            CRYPTO_PRICE_QUERY,
            args={"ids": ids, "vs_currencies": vs_currencies},
        )
        return result if isinstance(result, dict) else {}

    async def get_trending(self) -> dict[str, Any]:
        result = await self._action_queries.query(
            CRYPTO_TRENDING_QUERY,
            args={},
        )
        return result if isinstance(result, dict) else {}

    async def search(self, query: str) -> dict[str, Any]:
        result = await self._action_queries.query(
            CRYPTO_SEARCH_QUERY,
            args={"query": query},
        )
        return result if isinstance(result, dict) else {}

    async def get_coin_detail(self, coin_id: str) -> dict[str, Any]:
        result = await self._action_queries.query(
            CRYPTO_COIN_DETAIL_QUERY,
            args={"id": coin_id},
        )
        return result if isinstance(result, dict) else {}


@lru_cache(maxsize=1)
def get_crypto_service() -> CryptoService:
    return CryptoService(get_action_service_client())