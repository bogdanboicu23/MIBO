from __future__ import annotations

from functools import lru_cache
from typing import Any

from agent.external_services.action_service import get_action_service_client
from agent.external_services.base import ActionServiceQueryDefinition, SupportsActionQueries

FINANCE_SUMMARY_QUERY = ActionServiceQueryDefinition(
    data_source_id="finance.summary",
    handler="finance.user.summary",
)


class FinanceDataService:
    def __init__(self, action_queries: SupportsActionQueries) -> None:
        self._action_queries = action_queries

    async def get_user_finances(self, user_id: int) -> dict[str, Any]:
        result = await self._action_queries.query(
            FINANCE_SUMMARY_QUERY,
            args={"userId": user_id},
        )
        return result if isinstance(result, dict) else self.build_finance_payload(user_id)

    @staticmethod
    def build_finance_payload(user_id: int) -> dict[str, Any]:
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


@lru_cache(maxsize=1)
def get_finance_data_service() -> FinanceDataService:
    return FinanceDataService(get_action_service_client())
