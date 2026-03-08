from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Dict, List, Protocol

from .plan_normalizer import filter_unknown_tools
from .ui_policy import ensure_ui_when_requested


class IntentDetectorPlugin(Protocol):
    name: str

    def detect(
        self,
        user_prompt: str,
        *,
        context: Dict[str, Any],
        shortlisted_tools: List[Dict[str, Any]],
        shortlisted_components: List[Dict[str, Any]],
    ) -> Dict[str, Any]:
        ...


class PlanPostProcessorPlugin(Protocol):
    name: str

    def process(
        self,
        plan: Dict[str, Any],
        *,
        user_prompt: str,
        shortlisted_tools: List[Dict[str, Any]],
        shortlisted_components: List[Dict[str, Any]],
        allowed_tools: set[str],
    ) -> Dict[str, Any]:
        ...


@dataclass
class PlannerPluginRegistry:
    intent_detectors: List[IntentDetectorPlugin] = field(default_factory=list)
    post_processors: List[PlanPostProcessorPlugin] = field(default_factory=list)

    def register_default_plugins(self) -> None:
        self.intent_detectors.append(KeywordIntentDetector())
        self.post_processors.append(AllowedToolsPostProcessor())
        self.post_processors.append(UiEnforcementPostProcessor())
        self.post_processors.append(SafetyReasonPostProcessor())


class KeywordIntentDetector:
    name = "keyword_intent_detector"

    def detect(
        self,
        user_prompt: str,
        *,
        context: Dict[str, Any],
        shortlisted_tools: List[Dict[str, Any]],
        shortlisted_components: List[Dict[str, Any]],
    ) -> Dict[str, Any]:
        prompt = (user_prompt or "").lower()
        tokens = prompt.split()
        domain = "generic"
        if any(k in prompt for k in ["product", "shop", "cart", "buy", "cump"]):
            domain = "shop"
        elif any(k in prompt for k in ["finance", "budget", "expense", "venit", "chelt"]):
            domain = "finance"
        elif any(k in prompt for k in ["code", "typescript", "python", "javascript"]):
            domain = "code"

        wants_ui = any(
            k in prompt
            for k in ["ui", "dashboard", "chart", "table", "card", "component", "grafic", "tabel"]
        )
        needs_tools = any(
            k in prompt
            for k in ["show", "search", "list", "find", "analize", "fetch", "lookup", "detalii", "detaliaza"]
        )

        return {
            "detector": self.name,
            "domain": domain,
            "wants_ui": wants_ui,
            "needs_tools": needs_tools,
            "prompt_tokens": tokens[:40],
            "candidate_tools": [t.get("name") for t in shortlisted_tools[:6]],
            "candidate_components": [c.get("name") for c in shortlisted_components[:6]],
        }


class AllowedToolsPostProcessor:
    name = "allowed_tools_filter"

    def process(
        self,
        plan: Dict[str, Any],
        *,
        user_prompt: str,
        shortlisted_tools: List[Dict[str, Any]],
        shortlisted_components: List[Dict[str, Any]],
        allowed_tools: set[str],
    ) -> Dict[str, Any]:
        return filter_unknown_tools(plan, allowed_tools)


class UiEnforcementPostProcessor:
    name = "ui_enforcement"

    def process(
        self,
        plan: Dict[str, Any],
        *,
        user_prompt: str,
        shortlisted_tools: List[Dict[str, Any]],
        shortlisted_components: List[Dict[str, Any]],
        allowed_tools: set[str],
    ) -> Dict[str, Any]:
        return ensure_ui_when_requested(
            plan,
            user_prompt,
            shortlisted_components,
            shortlisted_tools,
        )


class SafetyReasonPostProcessor:
    name = "safety_reason"

    def process(
        self,
        plan: Dict[str, Any],
        *,
        user_prompt: str,
        shortlisted_tools: List[Dict[str, Any]],
        shortlisted_components: List[Dict[str, Any]],
        allowed_tools: set[str],
    ) -> Dict[str, Any]:
        safety = plan.get("safety")
        if not isinstance(safety, dict):
            safety = {}

        if not plan.get("steps"):
            safety.setdefault("needAssistantAnswer", True)
            safety.setdefault("reason", "No executable steps after validation.")
        else:
            safety.setdefault("needAssistantAnswer", False)

        plan["safety"] = safety
        return plan
