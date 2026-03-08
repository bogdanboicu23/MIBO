from __future__ import annotations

import json
from dataclasses import dataclass
from typing import Any, Dict, List

from .ranking import rank_candidates


@dataclass
class CapabilitySet:
    tools: List[Dict[str, Any]]
    components: List[Dict[str, Any]]
    action_routes: List[Dict[str, Any]]


class CapabilityRegistry:
    def __init__(self) -> None:
        self._capabilities = CapabilitySet([], [], [])

    def load(
        self,
        *,
        input_tool_catalog: List[Dict[str, Any]],
        input_ui_catalog: List[Dict[str, Any]],
        fallback_tools: List[Dict[str, Any]],
        fallback_components: List[Dict[str, Any]],
        action_routes: List[Dict[str, Any]],
    ) -> CapabilitySet:
        tools = input_tool_catalog if input_tool_catalog else fallback_tools
        components = input_ui_catalog if input_ui_catalog else fallback_components
        self._capabilities = CapabilitySet(
            tools=tools if isinstance(tools, list) else [],
            components=components if isinstance(components, list) else [],
            action_routes=action_routes if isinstance(action_routes, list) else [],
        )
        return self._capabilities

    def shortlist(
        self,
        *,
        query: str,
        max_tools: int,
        max_components: int,
        max_action_routes: int,
    ) -> Dict[str, List[Dict[str, Any]]]:
        return {
            "tools": rank_candidates(
                items=self._capabilities.tools,
                query=query,
                name_key="name",
                desc_key="description",
                limit=max_tools,
            ),
            "components": rank_candidates(
                items=self._capabilities.components,
                query=query,
                name_key="name",
                desc_key="propsSchema",
                limit=max_components,
            ),
            "actions": rank_candidates(
                items=self._capabilities.action_routes,
                query=query,
                name_key="actionType",
                desc_key="tool",
                limit=max_action_routes,
            ),
        }

    @staticmethod
    def build_query(
        user_prompt: str,
        memory_summary: str,
        conversation_context: Dict[str, Any],
    ) -> str:
        return " ".join(
            [
                user_prompt or "",
                memory_summary or "",
                json.dumps(conversation_context, ensure_ascii=False),
            ]
        )
