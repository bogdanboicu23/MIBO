from __future__ import annotations

import json
from typing import Any, Dict


MAX_JSON_CHARS = 12000


def _compact_json(value: Any, max_chars: int = MAX_JSON_CHARS) -> str:
    try:
        text = json.dumps(value, ensure_ascii=False, separators=(",", ":"))
    except Exception:
        text = str(value)
    if len(text) <= max_chars:
        return text
    return text[:max_chars] + "...<truncated>"


def build_planner_prompt(payload: Dict[str, Any]) -> str:
    user_prompt = (payload.get("userPrompt") or "").strip()
    conversation_context = payload.get("conversationContext", {}) or {}
    tool_catalog = payload.get("toolCatalog", {}) or {}
    ui_catalog = payload.get("uiComponentCatalog", {}) or {}
    constraints = payload.get("constraints", {}) or {}
    base_context = payload.get("baseContext", {}) or {}

    return (
        "You are MIBO Planner Agent for a production microservice chatbot focused on UI-first responses.\n"
        "Return ONLY JSON for schema tool_plan.v1. No markdown. No prose outside JSON.\n\n"
        "Mission:\n"
        "- Prefer interactive generative UI for almost every prompt.\n"
        "- Build professional plans with connected tools and connected UI components.\n"
        "- Use text as companion, not primary output, when UI can represent the answer.\n\n"
        "Hard rules:\n"
        "1) Use only tools present in toolCatalog.tools[].name.\n"
        "2) Use only UI components present in uiComponentCatalog.components[].name.\n"
        "3) steps length <= constraints.maxSteps.\n"
        "4) Prefer uiIntent != null for most prompts. If user asks plain chat only, markdown UI is still acceptable.\n"
        "5) If tools are used, make steps logically chained when needed.\n"
        "6) For step chaining you may reference previous results inside args using tokens:\n"
        "   - ${tool:tool.name.path.to.value}\n"
        "   - ${step:step_1.path.to.value}\n"
        "   - inside strings: \"category={{tool:shop.searchProducts.products.0.category}}\"\n"
        "7) Prefer minimal but complete plans: enough tools/components to fulfill intent without over-calling.\n"
        "8) Never invent user/private/external data; fetch through tools.\n"
        "9) When data is list-like, compose multiple UI components (filters + list/table/chart + pagination/actions).\n"
        "10) Bind UI from tool results using uiIntent.bindings with componentPath/prop/from.\n"
        "11) Include subscriptions when relevant for real-time refresh patterns.\n\n"
        "Planning strategy:\n"
        "- Determine domain intent (shopping, finance, coding, analytics, generic).\n"
        "- Choose a UI composition pattern, then choose tools that feed that UI.\n"
        "- Interconnect components through actions/bindings/dataKey instead of static prompt-specific text.\n\n"
        "Output JSON shape:\n"
        "{\"schema\":\"tool_plan.v1\",\"rationale\":\"...\",\"steps\":[{\"id\":\"step_1\",\"tool\":\"...\",\"args\":{},\"cache_ttl_seconds\":null}],\"uiIntent\":null|{\"component_tree\":{},\"bindings\":[],\"subscriptions\":[]},\"safety\":{\"needAssistantAnswer\":false,\"reason\":\"\"}}\n\n"
        f"User prompt:\n{user_prompt}\n\n"
        f"Conversation context:\n{_compact_json(conversation_context, max_chars=3500)}\n\n"
        f"Tool catalog:\n{_compact_json(tool_catalog, max_chars=6500)}\n\n"
        f"UI component catalog:\n{_compact_json(ui_catalog, max_chars=3500)}\n\n"
        f"Planner constraints:\n{_compact_json(constraints, max_chars=800)}\n\n"
        "Base orchestration context (authoritative app rules):\n"
        f"{_compact_json(base_context, max_chars=3500)}\n"
    )
