from __future__ import annotations

import json
from typing import Any, Dict, Optional

from langchain_core.messages import HumanMessage, SystemMessage
from langchain_groq import ChatGroq
from langgraph.graph import END, StateGraph
from pydantic import ValidationError

from agent_context import ConfigContextLoader, ConversationMemoryStore
from config import settings
from planner_core import (
    PlannerState,
    ensure_ui_when_requested,
    fetch_external_context,
    filter_unknown_tools,
    normalize_tool_plan,
    rank_candidates,
    safe_json_loads,
)
from prompt_template import build_planner_prompt
from schemas import PlannerInputV1, ToolPlanV1


class LangGraphPlanner:
    def __init__(self) -> None:
        self._context_loader = ConfigContextLoader()
        self._memory = ConversationMemoryStore()
        self._llm = ChatGroq(
            api_key=settings.GROQ_API_KEY,
            model=settings.GROQ_MODEL,
            temperature=settings.GROQ_TEMPERATURE,
            timeout=settings.GROQ_TIMEOUT_SECONDS,
            max_tokens=settings.GROQ_MAX_TOKENS,
        )
        self._graph = self._build_graph()

    async def plan(self, input_data: PlannerInputV1) -> ToolPlanV1:
        initial: PlannerState = {"input_data": input_data}
        result = self._graph.invoke(initial)
        validated = result.get("validated")
        if not isinstance(validated, ToolPlanV1):
            raise RuntimeError("Planner graph ended without validated ToolPlanV1")

        conversation_id = (input_data.meta.conversationId or "").strip() or "global"
        plan_summary = self._plan_summary(validated)
        self._memory.update(conversation_id, input_data.userPrompt, plan_summary)
        return validated

    def _build_graph(self):
        graph = StateGraph(PlannerState)
        graph.add_node("bootstrap_context", self._node_bootstrap_context)
        graph.add_node("deep_search", self._node_deep_search)
        graph.add_node("reasoning", self._node_reasoning)
        graph.add_node("generate", self._node_generate)
        graph.add_node("validate", self._node_validate)

        graph.add_edge("bootstrap_context", "deep_search")
        graph.add_edge("deep_search", "reasoning")
        graph.add_edge("reasoning", "generate")
        graph.add_edge("generate", "validate")
        graph.add_edge("validate", END)

        graph.set_entry_point("bootstrap_context")
        return graph.compile()

    def _node_bootstrap_context(self, state: PlannerState) -> PlannerState:
        inp = state["input_data"]
        cfg = self._context_loader.load()

        conversation_id = (inp.meta.conversationId or "").strip() or "global"
        memory = self._memory.get(conversation_id)

        base_context = {
            "configPack": {
                "tools": cfg.tools,
                "components": cfg.components,
                "allowedToolRefs": cfg.allowed_tool_refs,
                "composeRules": cfg.compose_rules,
                "actionRoutes": cfg.action_routes,
                "textSpec": cfg.text_spec,
            },
            "incomingConversationContext": inp.conversationContext,
            "memory": memory,
            "externalContext": fetch_external_context(inp.conversationContext),
        }

        return {"base_context": base_context, "memory": memory}

    def _node_deep_search(self, state: PlannerState) -> PlannerState:
        inp = state["input_data"]
        base = state["base_context"]

        tool_catalog = inp.toolCatalog.get("tools", [])
        if not isinstance(tool_catalog, list) or not tool_catalog:
            tool_catalog = base["configPack"].get("tools", [])

        ui_catalog = inp.uiComponentCatalog.get("components", [])
        if not isinstance(ui_catalog, list) or not ui_catalog:
            ui_catalog = base["configPack"].get("components", [])

        query = " ".join(
            [
                inp.userPrompt or "",
                str(state.get("memory", {}).get("summary", "")),
                json.dumps(inp.conversationContext, ensure_ascii=False),
            ]
        )

        shortlisted_tools = rank_candidates(
            items=tool_catalog,
            query=query,
            name_key="name",
            desc_key="description",
            limit=settings.DEEP_SEARCH_MAX_TOOLS,
        )
        shortlisted_components = rank_candidates(
            items=ui_catalog,
            query=query,
            name_key="name",
            desc_key="propsSchema",
            limit=settings.DEEP_SEARCH_MAX_COMPONENTS,
        )
        shortlisted_actions = rank_candidates(
            items=base["configPack"].get("actionRoutes", []),
            query=query,
            name_key="actionType",
            desc_key="tool",
            limit=settings.DEEP_SEARCH_MAX_ACTION_ROUTES,
        )

        return {
            "shortlisted_tools": shortlisted_tools,
            "shortlisted_components": shortlisted_components,
            "shortlisted_actions": shortlisted_actions,
        }

    def _node_reasoning(self, state: PlannerState) -> PlannerState:
        if not settings.ENABLE_DEEP_REASONING:
            return {"reasoning": {"mode": "disabled"}}

        inp = state["input_data"]
        prompt = (
            "Return JSON only with keys: intent, needs_tools, requested_visuals, entities, filters. "
            "No markdown.\n"
            f"User prompt: {inp.userPrompt}\n"
            f"Top tools: {json.dumps([t.get('name') for t in state.get('shortlisted_tools', [])], ensure_ascii=False)}\n"
            f"Top UI: {json.dumps([c.get('name') for c in state.get('shortlisted_components', [])], ensure_ascii=False)}"
        )

        try:
            resp = self._llm.invoke(
                [
                    SystemMessage(content="You are a strict planner reasoning assistant. Output JSON only."),
                    HumanMessage(content=prompt),
                ]
            )
            raw = safe_json_loads(resp.content)
            if not isinstance(raw, dict):
                raw = {}
            return {"reasoning": raw}
        except Exception:
            return {"reasoning": {"mode": "fallback"}}

    def _node_generate(self, state: PlannerState) -> PlannerState:
        inp = state["input_data"]
        max_steps = min(inp.constraints.maxSteps, settings.PLANNER_MAX_STEPS)

        compact_payload = {
            "schema": inp.schema,
            "userPrompt": inp.userPrompt,
            "conversationContext": {
                "memorySummary": state.get("memory", {}).get("summary", ""),
                "latest": inp.conversationContext,
            },
            "toolCatalog": {"tools": state.get("shortlisted_tools", [])},
            "uiComponentCatalog": {"components": state.get("shortlisted_components", [])},
            "constraints": {"maxSteps": max_steps},
            "meta": inp.meta.model_dump(),
            "baseContext": {
                "allowedToolRefs": state["base_context"]["configPack"].get("allowedToolRefs", []),
                "composeRules": state["base_context"]["configPack"].get("composeRules", {}),
                "actionRoutes": state.get("shortlisted_actions", []),
                "textSpec": state["base_context"]["configPack"].get("textSpec", {}),
                "externalContext": state["base_context"].get("externalContext", []),
                "reasoning": state.get("reasoning", {}),
            },
        }

        prompt = build_planner_prompt(compact_payload)
        resp = self._llm.invoke(
            [
                SystemMessage(
                    content=(
                        "You are a strict JSON planner for a production microservice system. "
                        "Output only valid JSON for tool_plan.v1."
                    )
                ),
                HumanMessage(content=prompt),
            ]
        )

        return {"prompt": prompt, "raw_response": str(resp.content)}

    def _node_validate(self, state: PlannerState) -> PlannerState:
        raw_text = state.get("raw_response", "")
        user_prompt = state["input_data"].userPrompt
        input_tool_catalog = state["input_data"].toolCatalog.get("tools", [])
        allowed_tools = {
            str(t.get("name", "")).strip()
            for t in state.get("shortlisted_tools", [])
            if isinstance(t, dict)
        }
        allowed_tools.update(
            {
                str(t.get("name", "")).strip()
                for t in (input_tool_catalog if isinstance(input_tool_catalog, list) else [])
                if isinstance(t, dict)
            }
        )

        try:
            raw = safe_json_loads(raw_text)
        except Exception as ex:
            raw = self._repair_with_model(raw_text=raw_text, error=f"JSON parse failed: {ex}")

        normalized = normalize_tool_plan(raw)
        normalized = filter_unknown_tools(normalized, allowed_tools)
        normalized = ensure_ui_when_requested(
            normalized,
            user_prompt,
            state.get("shortlisted_components", []),
            state.get("shortlisted_tools", []),
        )

        last_error: Optional[str] = None
        for _ in range(settings.PLANNER_REPAIR_ATTEMPTS + 1):
            try:
                validated = ToolPlanV1.model_validate(normalized)
                return {"normalized": normalized, "validated": validated}
            except ValidationError as ve:
                last_error = str(ve)
                repaired = self._repair_with_model(raw_text=json.dumps(normalized), error=last_error)
                normalized = normalize_tool_plan(repaired)
                normalized = filter_unknown_tools(normalized, allowed_tools)
                normalized = ensure_ui_when_requested(
                    normalized,
                    user_prompt,
                    state.get("shortlisted_components", []),
                    state.get("shortlisted_tools", []),
                )

        fallback = ToolPlanV1(
            schema="tool_plan.v1",
            rationale="Fallback plan: model output invalid, switching to assistant answer mode.",
            steps=[],
            uiIntent=None,
            safety={"needAssistantAnswer": True, "reason": last_error or "Planner validation failed."},
        )
        return {"normalized": normalized, "validated": fallback}

    def _repair_with_model(self, raw_text: str, error: str) -> Dict[str, Any]:
        sys = SystemMessage(
            content=(
                "You repair invalid planner JSON. Output ONLY valid JSON. "
                "No markdown, no extra text."
            )
        )
        user = HumanMessage(
            content=(
                "Validation/parsing failed for planner JSON.\n"
                f"Error: {error}\n\n"
                "Return corrected JSON for schema tool_plan.v1 with keys: "
                "schema, rationale, steps, uiIntent, safety.\n"
                f"Invalid content:\n{raw_text}"
            )
        )
        resp = self._llm.invoke([sys, user])
        return safe_json_loads(str(resp.content))

    @staticmethod
    def _plan_summary(plan: ToolPlanV1) -> str:
        tools = [s.tool for s in plan.steps]
        if not tools:
            return "assistant_answer"
        return ", ".join(tools)
