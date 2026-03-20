from __future__ import annotations

from dataclasses import dataclass, field, replace
from typing import Any, Mapping, Protocol


@dataclass(frozen=True)
class ActionServiceQueryDefinition:
    data_source_id: str
    handler: str
    default_args: Mapping[str, Any] = field(default_factory=dict)

    def with_default_args(self, **default_args: Any) -> "ActionServiceQueryDefinition":
        if not default_args:
            return self

        return replace(self, default_args={**self.default_args, **default_args})


class SupportsActionQueries(Protocol):
    async def query(
        self,
        definition: ActionServiceQueryDefinition,
        *,
        args: Mapping[str, Any] | None = None,
    ) -> Any: ...
