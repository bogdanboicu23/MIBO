from __future__ import annotations

import os
from functools import lru_cache
from typing import Any, Mapping

import httpx

from agent.external_services.base import ActionServiceQueryDefinition, SupportsActionQueries

DEFAULT_ACTION_SERVICE_URL = "http://action-service:8080"
ACTION_SERVICE_URL_CANDIDATES = (
    "ACTION_SERVICE_URL",
    "services__action-service__http__0",
    "services__action-service__default__0",
    "ACTION_SERVICE",
)


def resolve_action_service_url() -> str:
    for env_name in ACTION_SERVICE_URL_CANDIDATES:
        candidate = os.getenv(env_name)
        if candidate and candidate.strip():
            return candidate.strip().rstrip("/")

    return DEFAULT_ACTION_SERVICE_URL


class ActionServiceClient(SupportsActionQueries):
    def __init__(self, base_url: str | None = None, timeout_seconds: float = 30.0) -> None:
        self._base_url = (base_url or resolve_action_service_url()).rstrip("/")
        self._timeout_seconds = timeout_seconds

    async def query(
        self,
        definition: ActionServiceQueryDefinition,
        *,
        args: Mapping[str, Any] | None = None,
    ) -> Any:
        payload = {
            "dataSource": {
                "id": definition.data_source_id,
                "handler": definition.handler,
                "defaultArgs": dict(definition.default_args),
            },
            "args": dict(args or {}),
        }

        async with httpx.AsyncClient(timeout=self._timeout_seconds) as client:
            response = await client.post(f"{self._base_url}/api/actions/query", json=payload)
            response.raise_for_status()
            body = response.json()

        if isinstance(body, dict) and isinstance(body.get("data"), dict):
            return body["data"]

        return body


@lru_cache(maxsize=1)
def get_action_service_client() -> ActionServiceClient:
    return ActionServiceClient()
