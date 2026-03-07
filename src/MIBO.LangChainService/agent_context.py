from __future__ import annotations

import json
import threading
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, List, Optional

from config import settings


@dataclass
class ContextPack:
    tools: List[Dict[str, Any]]
    components: List[Dict[str, Any]]
    allowed_tool_refs: List[str]
    action_routes: List[Dict[str, Any]]
    compose_rules: Dict[str, Any]
    text_spec: Dict[str, Any]


class ConfigContextLoader:
    def __init__(self) -> None:
        self._lock = threading.Lock()
        self._cache: Optional[ContextPack] = None
        self._cache_mtime: float = 0.0

    def load(self) -> ContextPack:
        config_dir = self._resolve_config_dir()
        mtime = self._folder_mtime(config_dir)

        with self._lock:
            if self._cache is not None and mtime <= self._cache_mtime:
                return self._cache

            tools = self._read_json(config_dir / "tools.json").get("tools", [])
            components = self._read_json(config_dir / "ui-components.json").get("components", [])
            ui_compose = self._read_json(config_dir / "ui-compose-spec.json")
            action_routes = self._read_json(config_dir / "action-routing.json").get("routes", [])
            text_spec = self._read_json(config_dir / "text-spec.json")

            pack = ContextPack(
                tools=self._compact_tools(tools),
                components=self._compact_components(components),
                allowed_tool_refs=ui_compose.get("allowedToolRefs", []) or [],
                action_routes=self._compact_action_routes(action_routes),
                compose_rules={
                    "schema": ui_compose.get("schema", "ui.v1"),
                    "includeToolDataSnapshot": bool(ui_compose.get("includeToolDataSnapshot", True)),
                    "includeBindings": bool(ui_compose.get("includeBindings", True)),
                    "includeSubscriptions": bool(ui_compose.get("includeSubscriptions", True)),
                },
                text_spec={
                    "missingValue": text_spec.get("missingValue", "N/A"),
                    "bindings": text_spec.get("bindings", []) or [],
                },
            )

            self._cache = pack
            self._cache_mtime = mtime
            return pack

    @staticmethod
    def _resolve_config_dir() -> Path:
        configured = Path(settings.CONTEXT_CONFIG_DIR)
        if configured.is_absolute():
            return configured

        base_dir = Path(__file__).resolve().parent
        candidate = (base_dir / configured).resolve()
        if candidate.exists():
            return candidate

        fallback = (base_dir / ".." / "MIBO.ConversationService" / "config").resolve()
        return fallback

    @staticmethod
    def _folder_mtime(folder: Path) -> float:
        if not folder.exists():
            return 0.0
        max_mtime = folder.stat().st_mtime
        for p in folder.glob("*.json"):
            max_mtime = max(max_mtime, p.stat().st_mtime)
        return max_mtime

    @staticmethod
    def _read_json(path: Path) -> Dict[str, Any]:
        if not path.exists():
            return {}
        with path.open("r", encoding="utf-8") as f:
            data = json.load(f)
        return data if isinstance(data, dict) else {}

    @staticmethod
    def _compact_tools(tools: Any) -> List[Dict[str, Any]]:
        if not isinstance(tools, list):
            return []

        compact: List[Dict[str, Any]] = []
        for t in tools:
            if not isinstance(t, dict):
                continue
            compact.append(
                {
                    "name": t.get("name", ""),
                    "description": t.get("description", ""),
                    "method": t.get("method", "GET"),
                    "requiredArgs": t.get("requiredArgs", []) or [],
                    "defaultArgs": t.get("defaultArgs", {}) or {},
                    "cacheTtlSeconds": t.get("cacheTtlSeconds", None),
                    "returns": t.get("returns", {}) or {},
                }
            )
        return compact

    @staticmethod
    def _compact_components(components: Any) -> List[Dict[str, Any]]:
        if not isinstance(components, list):
            return []
        compact: List[Dict[str, Any]] = []
        for c in components:
            if not isinstance(c, dict):
                continue
            compact.append(
                {
                    "name": c.get("name", ""),
                    "propsSchema": c.get("propsSchema", {}) or {},
                }
            )
        return compact

    @staticmethod
    def _compact_action_routes(routes: Any) -> List[Dict[str, Any]]:
        if not isinstance(routes, list):
            return []
        compact: List[Dict[str, Any]] = []
        for route in routes:
            if not isinstance(route, dict):
                continue
            compact.append(
                {
                    "actionType": route.get("actionType", ""),
                    "mode": route.get("mode", "prompt"),
                    "tool": route.get("tool"),
                    "argMap": route.get("argMap", {}) or {},
                    "promptTemplate": route.get("promptTemplate"),
                }
            )
        return compact


class ConversationMemoryStore:
    def __init__(self) -> None:
        self._lock = threading.Lock()
        self._state: Dict[str, Dict[str, Any]] = {}

    def get(self, conversation_id: str) -> Dict[str, Any]:
        with self._lock:
            item = self._state.get(conversation_id)
            if item is None:
                return {
                    "summary": "",
                    "recent_prompts": [],
                    "recent_plans": [],
                    "updated_at": self._now_iso(),
                }
            return {
                "summary": item.get("summary", ""),
                "recent_prompts": list(item.get("recent_prompts", [])),
                "recent_plans": list(item.get("recent_plans", [])),
                "updated_at": item.get("updated_at", self._now_iso()),
            }

    def update(self, conversation_id: str, user_prompt: str, plan_summary: str) -> None:
        with self._lock:
            state = self._state.setdefault(
                conversation_id,
                {
                    "summary": "",
                    "recent_prompts": [],
                    "recent_plans": [],
                    "updated_at": self._now_iso(),
                },
            )

            prompts = state["recent_prompts"]
            prompts.append(user_prompt.strip())
            state["recent_prompts"] = prompts[-settings.MEMORY_PROMPT_WINDOW :]

            plans = state["recent_plans"]
            plans.append(plan_summary.strip())
            state["recent_plans"] = plans[-settings.MEMORY_PLAN_WINDOW :]

            summary_parts = []
            if state["recent_prompts"]:
                summary_parts.append(
                    "Recent user intents: " + " | ".join(state["recent_prompts"])[: settings.MEMORY_SUMMARY_CHAR_LIMIT]
                )
            if state["recent_plans"]:
                summary_parts.append(
                    "Recent planner actions: " + " | ".join(state["recent_plans"])[: settings.MEMORY_SUMMARY_CHAR_LIMIT]
                )

            state["summary"] = "\n".join(summary_parts)[: settings.MEMORY_SUMMARY_CHAR_LIMIT]
            state["updated_at"] = self._now_iso()

    @staticmethod
    def _now_iso() -> str:
        return datetime.now(timezone.utc).isoformat()
