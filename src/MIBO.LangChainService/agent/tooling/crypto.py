from __future__ import annotations

from typing import Any

from langchain_core.tools import tool

from agent.external_services.crypto import get_crypto_service

crypto_service = get_crypto_service()


@tool
async def get_trending_crypto() -> dict[str, Any]:
    """Get currently trending cryptocurrencies on CoinGecko."""
    return await crypto_service.get_trending()


@tool
async def search_crypto(query: str) -> dict[str, Any]:
    """Search for a cryptocurrency by name or symbol on CoinGecko."""
    return await crypto_service.search(query=query)


@tool
async def get_crypto_prices(ids: str, vs_currencies: str = "usd") -> dict[str, Any]:
    """Get current prices for one or more cryptocurrencies. ids is a comma-separated list of coin ids (e.g. 'bitcoin,ethereum')."""
    return await crypto_service.get_price(ids=ids, vs_currencies=vs_currencies)


@tool
async def get_crypto_markets(vs_currency: str = "usd", limit: int = 20) -> dict[str, Any]:
    """Get top cryptocurrencies by market cap with price, volume, and 24h change."""
    return await crypto_service.get_markets(vs_currency=vs_currency, limit=limit)


@tool
async def get_crypto_detail(coin_id: str) -> dict[str, Any]:
    """Get detailed information about a specific cryptocurrency including description, market data, and links."""
    return await crypto_service.get_coin_detail(coin_id=coin_id)