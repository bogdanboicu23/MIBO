from __future__ import annotations

import math
from typing import Any

from langchain_core.tools import tool

from agent.external_services.finance import get_finance_data_service
from agent.external_services.products import get_product_catalog_service

finance_data_service = get_finance_data_service()
product_catalog_service = get_product_catalog_service()


@tool
async def get_user_finances(user_id: str = "") -> dict[str, Any]:
    """Fetch finance data for the current user. user_id is injected automatically."""
    return await finance_data_service.get_user_finances(user_id)


@tool
async def calculate_affordability(budget: float, product_id: int) -> dict[str, Any]:
    """Calculate how many units of a product fit within a given budget."""
    product = await product_catalog_service.get_product(product_id)
    price = float(product.get("price", 0) or 0)
    quantity = math.floor(budget / price) if price > 0 else 0
    return {"quantity": quantity, "price": price, "budget": budget}
