from __future__ import annotations

import json
from typing import Any, Dict, List, Optional

from langchain_core.messages import HumanMessage, SystemMessage
from langchain_groq import ChatGroq
from pydantic import ValidationError

from config import settings
from prompt_template import build_planner_prompt
from schemas import PlannerInputV1, ToolPlanV1

from .plan_normalizer import normalize_tool_plan, safe_json_loads
from .plugins import PlannerPluginRegistry


class IntentDetectionStage:
    def __init__(self, plugin_registry: PlannerPluginRegistry) -> None:
        self._plugins = plugin_registry

    def run(
        self,
        *,
        user_prompt: str,
        context: Dict[str, Any],
        shortlisted_tools: List[Dict[str, Any]],
        shortlisted_components: List[Dict[str, Any]],
    ) -> Dict[str, Any]:
        signals: List[Dict[str, Any]] = []
        for plugin in self._plugins.intent_detectors:
            try:
                signals.append(
                    plugin.detect(
                        user_prompt,
                        context=context,
                        shortlisted_tools=shortlisted_tools,
                        shortlisted_components=shortlisted_components,
                    )
                )
            except Exception:
                signals.append({"detector": getattr(plugin, "name", "unknown"), "error": "intent_plugin_failed"})
        return {"signals": signals}


class ReasoningStage:
    def __init__(self, llm: ChatGroq) -> None:
        self._llm = llm

    def run(
        self,
        *,
        input_data: PlannerInputV1,
        shortlisted_tools: List[Dict[str, Any]],
        shortlisted_components: List[Dict[str, Any]],
        intent: Dict[str, Any],
    ) -> Dict[str, Any]:
        if not settings.ENABLE_DEEP_REASONING:
            return {"mode": "disabled", "intent": intent}

        prompt = (
            "Return JSON with keys: intent, needs_tools, requested_visuals, entities, filters, confidence.\n"
            f"User prompt: {input_data.userPrompt}\n"
            f"Intent signals: {json.dumps(intent, ensure_ascii=False)}\n"
            f"Top tools: {json.dumps([t.get('name') for t in shortlisted_tools], ensure_ascii=False)}\n"
            f"Top UI: {json.dumps([c.get('name') for c in shortlisted_components], ensure_ascii=False)}"
        )

        try:
            resp = self._llm.invoke(
                [
                    SystemMessage(content="You are a strict planner reasoning assistant. Output JSON only."),
                    HumanMessage(content=prompt),
                ]
            )
            raw = json.loads(str(resp.content))
            if not isinstance(raw, dict):
                raw = {}
            raw.setdefault("intentSignals", intent)
            return raw
        except Exception:
            return {"mode": "fallback", "intentSignals": intent}


class PlanGenerationStage:
    def __init__(self, llm: ChatGroq) -> None:
        self._llm = llm

    def run(
        self,
        *,
        input_data: PlannerInputV1,
        compact_payload: Dict[str, Any],
    ) -> str:
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
        return str(resp.content)


class PlanValidationStage:
    def __init__(self, llm: ChatGroq, plugin_registry: PlannerPluginRegistry) -> None:
        self._llm = llm
        self._plugins = plugin_registry

    def run(
        self,
        *,
        raw_response: str,
        user_prompt: str,
        shortlisted_tools: List[Dict[str, Any]],
        shortlisted_components: List[Dict[str, Any]],
        allowed_tools: set[str],
    ) -> Dict[str, Any]:
        try:
            # With JSON mode active, json.loads should succeed directly.
            # Fall back to safe_json_loads (bracket extraction) if needed.
            try:
                raw = json.loads(raw_response)
            except json.JSONDecodeError:
                raw = safe_json_loads(raw_response)
        except Exception as ex:
            raw = self._repair_with_model(raw_text=raw_response, error=f"JSON parse failed: {ex}")

        normalized = normalize_tool_plan(raw)

        for plugin in self._plugins.post_processors:
            normalized = plugin.process(
                normalized,
                user_prompt=user_prompt,
                shortlisted_tools=shortlisted_tools,
                shortlisted_components=shortlisted_components,
                allowed_tools=allowed_tools,
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
                for plugin in self._plugins.post_processors:
                    normalized = plugin.process(
                        normalized,
                        user_prompt=user_prompt,
                        shortlisted_tools=shortlisted_tools,
                        shortlisted_components=shortlisted_components,
                        allowed_tools=allowed_tools,
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


class ResponseCompositionStage:
    @staticmethod
    def run(validated_plan: ToolPlanV1, *, intent: Dict[str, Any], reasoning: Dict[str, Any]) -> ToolPlanV1:
        safety = dict(validated_plan.safety or {})
        safety.setdefault("intent", intent)
        safety.setdefault("reasoning", reasoning)
        validated_plan.safety = safety
        return validated_plan
