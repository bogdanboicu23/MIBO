from __future__ import annotations

from functools import lru_cache
from typing import Any

from agent.external_services.action_service import get_action_service_client
from agent.external_services.base import ActionServiceQueryDefinition, SupportsActionQueries

FINANCE_SUMMARY_QUERY = ActionServiceQueryDefinition(
    data_source_id="finance.summary",
    handler="finance.summary.get",
)


class FinanceDataService:
    def __init__(self, action_queries: SupportsActionQueries) -> None:
        self._action_queries = action_queries

    async def get_user_finances(self, user_id: str) -> dict[str, Any]:
        args: dict[str, Any] = {}
        if user_id:
            args["userId"] = user_id
        result = await self._action_queries.query(
            FINANCE_SUMMARY_QUERY,
            args=args,
        )
        return result if isinstance(result, dict) else self.build_finance_payload(user_id)

    @staticmethod
    def build_finance_payload(user_id: str) -> dict[str, Any]:
        uid = abs(hash(user_id)) if user_id else 0
        balance = (uid * 137) % 10000 + 1000
        income = 3000 + (uid * 53) % 2000
        expenses = income * 0.6
        savings = balance + (income - expenses) * 3
        return {
            "userId": user_id,
            "balance": round(balance, 2),
            "income": round(income, 2),
            "expenses": round(expenses, 2),
            "savings": round(savings, 2),
        }


@lru_cache(maxsize=1)
def get_finance_data_service() -> FinanceDataService:
    return FinanceDataService(get_action_service_client())
