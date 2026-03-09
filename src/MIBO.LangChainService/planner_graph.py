from __future__ import annotations

from typing import Any, Dict

from langchain_groq import ChatGroq
from langgraph.graph import END, StateGraph

from agent_context import ConfigContextLoader, ConversationMemoryStore
from config import settings
from planner_core import (
    CapabilityRegistry,
    IntentDetectionStage,
    PlanGenerationStage,
    PlanValidationStage,
    PlannerPluginRegistry,
    PlannerState,
    ReasoningStage,
    ResponseCompositionStage,
    fetch_external_context,
)
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
        # JSON-mode LLM: guarantees valid JSON output, eliminating manual
        # extraction and repair cycles in planning/reasoning stages.
        self._llm_json = self._llm.bind(
            response_format={"type": "json_object"}
        )

        self._plugin_registry = PlannerPluginRegistry()
        self._plugin_registry.register_default_plugins()
        if not settings.ENABLE_PLUGIN_INTENT:
            self._plugin_registry.intent_detectors = []
        if not settings.ENABLE_PLUGIN_POST_PROCESSING:
            self._plugin_registry.post_processors = []
        self._capability_registry = CapabilityRegistry()

        self._intent_stage = IntentDetectionStage(self._plugin_registry)
        self._reasoning_stage = ReasoningStage(self._llm_json)
        self._planning_stage = PlanGenerationStage(self._llm_json)
        self._validation_stage = PlanValidationStage(self._llm_json, self._plugin_registry)
        self._composition_stage = ResponseCompositionStage()
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
        graph.add_node("intent_detection", self._node_intent_detection)
        graph.add_node("reasoning", self._node_reasoning)
        graph.add_node("planning", self._node_planning)
        graph.add_node("validate", self._node_validate)
        graph.add_node("compose_response", self._node_compose_response)

        graph.add_edge("bootstrap_context", "deep_search")
        graph.add_edge("deep_search", "intent_detection")
        graph.add_edge("intent_detection", "reasoning")
        graph.add_edge("reasoning", "planning")
        graph.add_edge("planning", "validate")
        graph.add_edge("validate", "compose_response")
        graph.add_edge("compose_response", END)

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

        input_tools = inp.toolCatalog.get("tools", [])
        input_components = inp.uiComponentCatalog.get("components", [])
        capabilities = self._capability_registry.load(
            input_tool_catalog=input_tools if isinstance(input_tools, list) else [],
            input_ui_catalog=input_components if isinstance(input_components, list) else [],
            fallback_tools=base["configPack"].get("tools", []),
            fallback_components=base["configPack"].get("components", []),
            action_routes=base["configPack"].get("actionRoutes", []),
        )

        query = self._capability_registry.build_query(
            user_prompt=inp.userPrompt,
            memory_summary=str(state.get("memory", {}).get("summary", "")),
            conversation_context=inp.conversationContext,
        )

        shortlisted = self._capability_registry.shortlist(
            query=query,
            max_tools=settings.DEEP_SEARCH_MAX_TOOLS,
            max_components=settings.DEEP_SEARCH_MAX_COMPONENTS,
            max_action_routes=settings.DEEP_SEARCH_MAX_ACTION_ROUTES,
        )

        return {
            "capability_set": {
                "tools": capabilities.tools,
                "components": capabilities.components,
                "actionRoutes": capabilities.action_routes,
            },
            "shortlisted_tools": shortlisted["tools"],
            "shortlisted_components": shortlisted["components"],
            "shortlisted_actions": shortlisted["actions"],
        }

    def _node_intent_detection(self, state: PlannerState) -> PlannerState:
        intent = self._intent_stage.run(
            user_prompt=state["input_data"].userPrompt,
            context=state["base_context"],
            shortlisted_tools=state.get("shortlisted_tools", []),
            shortlisted_components=state.get("shortlisted_components", []),
        )
        return {"intent": intent}

    def _node_reasoning(self, state: PlannerState) -> PlannerState:
        reasoning = self._reasoning_stage.run(
            input_data=state["input_data"],
            shortlisted_tools=state.get("shortlisted_tools", []),
            shortlisted_components=state.get("shortlisted_components", []),
            intent=state.get("intent", {}),
        )
        return {"reasoning": reasoning}

    def _node_planning(self, state: PlannerState) -> PlannerState:
        inp = state["input_data"]
        max_steps = min(inp.constraints.maxSteps, settings.PLANNER_MAX_STEPS)

        compact_payload: Dict[str, Any] = {
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
                "intent": state.get("intent", {}),
                "reasoning": state.get("reasoning", {}),
            },
        }

        raw_response = self._planning_stage.run(
            input_data=inp,
            compact_payload=compact_payload,
        )

        return {"raw_response": raw_response}

    def _node_validate(self, state: PlannerState) -> PlannerState:
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

        result = self._validation_stage.run(
            raw_response=state.get("raw_response", ""),
            user_prompt=state["input_data"].userPrompt,
            shortlisted_tools=state.get("shortlisted_tools", []),
            shortlisted_components=state.get("shortlisted_components", []),
            allowed_tools=allowed_tools,
        )

        return {
            "normalized": result["normalized"],
            "validated": result["validated"],
        }

    def _node_compose_response(self, state: PlannerState) -> PlannerState:
        validated = state.get("validated")
        if not isinstance(validated, ToolPlanV1):
            fallback = ToolPlanV1(
                schema="tool_plan.v1",
                rationale="Fallback plan: missing validated plan.",
                steps=[],
                uiIntent=None,
                safety={"needAssistantAnswer": True, "reason": "validated_plan_missing"},
            )
            return {"validated": fallback}

        composed = self._composition_stage.run(
            validated,
            intent=state.get("intent", {}),
            reasoning=state.get("reasoning", {}),
        )
        return {"validated": composed}

    @staticmethod
    def _plan_summary(plan: ToolPlanV1) -> str:
        tools = [s.tool for s in plan.steps]
        if not tools:
            return "assistant_answer"
        return ", ".join(tools)
