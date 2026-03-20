from __future__ import annotations

from functools import lru_cache
from typing import Any

from agent.external_services.action_service import get_action_service_client
from agent.external_services.base import ActionServiceQueryDefinition, SupportsActionQueries

PRODUCT_CATALOG_QUERY = ActionServiceQueryDefinition(
    data_source_id="products.catalog",
    handler="products.catalog.query",
)
PRODUCT_DETAIL_QUERY = ActionServiceQueryDefinition(
    data_source_id="products.detail",
    handler="products.detail.get",
)
PRODUCT_CATEGORIES_QUERY = ActionServiceQueryDefinition(
    data_source_id="products.categories",
    handler="products.categories.list",
)


class ProductCatalogService:
    def __init__(self, action_queries: SupportsActionQueries) -> None:
        self._action_queries = action_queries

    async def search_products(self, query: str = "", limit: int = 10) -> dict[str, Any]:
        definition = PRODUCT_CATALOG_QUERY.with_default_args(limit=limit, skip=0)
        result = await self._action_queries.query(
            definition,
            args={"q": query, "query": query, "limit": limit, "skip": 0},
        )
        return result if isinstance(result, dict) else {}

    async def get_product(self, product_id: int) -> dict[str, Any]:
        result = await self._action_queries.query(
            PRODUCT_DETAIL_QUERY,
            args={"productId": product_id, "id": product_id},
        )
        return result if isinstance(result, dict) else {}

    async def list_products(
        self,
        *,
        category: str | None = None,
        limit: int = 10,
        skip: int = 0,
    ) -> dict[str, Any]:
        args: dict[str, Any] = {"limit": limit, "skip": skip}
        if category:
            args["category"] = category

        definition = PRODUCT_CATALOG_QUERY.with_default_args(limit=limit, skip=skip)
        result = await self._action_queries.query(definition, args=args)
        return result if isinstance(result, dict) else {}

    async def get_categories(self) -> dict[str, Any]:
        result = await self._action_queries.query(PRODUCT_CATEGORIES_QUERY)
        return result if isinstance(result, dict) else {}


@lru_cache(maxsize=1)
def get_product_catalog_service() -> ProductCatalogService:
    return ProductCatalogService(get_action_service_client())
