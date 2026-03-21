from __future__ import annotations

from typing import Any

from langchain_core.tools import tool

from agent.external_services.products import get_product_catalog_service

product_catalog_service = get_product_catalog_service()


@tool
async def search_products(query: str = "", limit: int = 10) -> dict[str, Any]:
    """Search products by free-text query, or list products when the query is empty."""
    return await product_catalog_service.search_products(query=query, limit=limit)


@tool
async def get_product(product_id: int) -> dict[str, Any]:
    """Fetch one product by its identifier."""
    return await product_catalog_service.get_product(product_id)


@tool
async def list_products(category: str | None = None, limit: int = 10, skip: int = 0) -> dict[str, Any]:
    """List products with optional category filtering and pagination."""
    return await product_catalog_service.list_products(category=category, limit=limit, skip=skip)


@tool
async def get_categories() -> dict[str, Any]:
    """Return the available product categories."""
    return await product_catalog_service.get_categories()
