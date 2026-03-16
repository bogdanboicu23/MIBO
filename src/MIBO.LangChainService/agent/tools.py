from __future__ import annotations

import math
import os
from typing import Any

import httpx
from langchain_core.tools import tool


def _resolve_action_service_url() -> str:
    candidates = (
        os.getenv("ACTION_SERVICE_URL"),
        os.getenv("services__action-service__http__0"),
        os.getenv("services__action-service__default__0"),
        os.getenv("ACTION_SERVICE"),
        "http://action-service:8080",
    )

    for candidate in candidates:
        if candidate and candidate.strip():
            return candidate.strip().rstrip("/")

    return "http://action-service:8080"


ACTION_SERVICE_URL = _resolve_action_service_url()


async def _request_json(base_url: str, path: str, params: dict[str, Any] | None = None) -> Any:
    async with httpx.AsyncClient(timeout=30.0) as client:
        response = await client.get(f"{base_url}{path}", params=params)
        response.raise_for_status()
        return response.json()


def _to_float(value: Any) -> float:
    try:
        return float(value)
    except (TypeError, ValueError):
        return 0.0


def _to_int(value: Any) -> int:
    try:
        return int(value)
    except (TypeError, ValueError):
        return 0


def _to_str(value: Any) -> str:
    return value if isinstance(value, str) else ""


def _map_product(raw_product: dict[str, Any]) -> dict[str, Any]:
    raw_images = raw_product.get("images", [])
    images = [image for image in raw_images if isinstance(image, str)] if isinstance(raw_images, list) else []

    return {
        "id": _to_int(raw_product.get("id")),
        "title": _to_str(raw_product.get("title")),
        "description": _to_str(raw_product.get("description")),
        "price": _to_float(raw_product.get("price")),
        "discountPercentage": _to_float(raw_product.get("discountPercentage")),
        "rating": _to_float(raw_product.get("rating")),
        "stock": _to_int(raw_product.get("stock")),
        "brand": _to_str(raw_product.get("brand")),
        "category": _to_str(raw_product.get("category")),
        "thumbnail": _to_str(raw_product.get("thumbnail")),
        "images": images,
    }


def _map_product_list(payload: dict[str, Any], category: str | None = None) -> dict[str, Any]:
    raw_products = payload.get("products", [])
    products = [_map_product(item) for item in raw_products if isinstance(item, dict)]

    return {
        "items": products,
        "total": _to_int(payload.get("total")),
        "limit": _to_int(payload.get("limit")),
        "skip": _to_int(payload.get("skip")),
        "category": category,
    }


def _humanize_slug(value: str) -> str:
    return " ".join(part.capitalize() for part in value.split("-") if part)


def _build_finance_payload(user_id: int) -> dict[str, Any]:
    balance = (user_id * 137) % 10000 + 1000
    income = 3000 + (user_id * 53) % 2000
    expenses = income * 0.6
    savings = balance + (income - expenses) * 3
    return {
        "userId": user_id,
        "balance": round(balance, 2),
        "income": round(income, 2),
        "expenses": round(expenses, 2),
        "savings": round(savings, 2),
    }


def _map_categories(payload: Any) -> dict[str, Any]:
    if not isinstance(payload, list):
        return {"items": []}

    categories: list[dict[str, str]] = []
    for item in payload:
        if isinstance(item, str):
            categories.append({"name": _humanize_slug(item), "slug": item})
            continue

        if isinstance(item, dict):
            slug = _to_str(item.get("slug")) or _to_str(item.get("name")).strip().lower().replace(" ", "-")
            name = _to_str(item.get("name")) or _humanize_slug(slug)
            categories.append({"name": name, "slug": slug})

    return {"items": categories}


async def _query_action_service(
    *,
    data_source_id: str,
    handler: str,
    default_args: dict[str, Any] | None = None,
    args: dict[str, Any] | None = None,
) -> Any:
    payload = {
        "dataSource": {
            "id": data_source_id,
            "handler": handler,
            "defaultArgs": default_args or {},
        },
        "args": args or {},
    }

    async with httpx.AsyncClient(timeout=30.0) as client:
        response = await client.post(f"{ACTION_SERVICE_URL}/api/actions/query", json=payload)
        response.raise_for_status()
        body = response.json()

    if isinstance(body, dict) and isinstance(body.get("data"), dict):
        return body["data"]

    return body


@tool
async def search_products(query: str = "", limit: int = 10) -> dict[str, Any]:
    """Search products by free-text query, or list products when the query is empty."""
    result = await _query_action_service(
        data_source_id="products.catalog",
        handler="products.catalog.query",
        default_args={"limit": limit, "skip": 0},
        args={"q": query, "query": query, "limit": limit, "skip": 0},
    )
    return result if isinstance(result, dict) else {}


@tool
async def get_product(product_id: int) -> dict[str, Any]:
    """Fetch one product by its identifier."""
    result = await _query_action_service(
        data_source_id="products.detail",
        handler="products.detail.get",
        args={"productId": product_id, "id": product_id},
    )
    return result if isinstance(result, dict) else {}


@tool
async def list_products(category: str | None = None, limit: int = 10, skip: int = 0) -> dict[str, Any]:
    """List products with optional category filtering and pagination."""
    args: dict[str, Any] = {"limit": limit, "skip": skip}
    if category:
        args["category"] = category

    result = await _query_action_service(
        data_source_id="products.catalog",
        handler="products.catalog.query",
        default_args={"limit": limit, "skip": skip},
        args=args,
    )
    return result if isinstance(result, dict) else {}


@tool
async def get_categories() -> dict[str, Any]:
    """Return the available product categories."""
    result = await _query_action_service(
        data_source_id="products.categories",
        handler="products.categories.list",
    )
    return result if isinstance(result, dict) else {}


@tool
async def get_user_finances(user_id: int) -> dict[str, Any]:
    """Fetch deterministic finance data for a user."""
    result = await _query_action_service(
        data_source_id="finance.summary",
        handler="finance.user.summary",
        args={"userId": user_id},
    )
    return result if isinstance(result, dict) else _build_finance_payload(user_id)


@tool
async def calculate_affordability(budget: float, product_id: int) -> dict[str, Any]:
    """Calculate how many units of a product fit within a given budget."""
    product = await get_product.ainvoke({"product_id": product_id})
    price = float(product.get("price", 0) or 0)
    quantity = math.floor(budget / price) if price > 0 else 0
    return {"quantity": quantity, "price": price, "budget": budget}
