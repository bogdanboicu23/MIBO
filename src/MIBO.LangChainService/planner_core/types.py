from __future__ import annotations

from typing import Any, Dict, List, TypedDict

from schemas import PlannerInputV1, ToolPlanV1


class PlannerState(TypedDict, total=False):
    input_data: PlannerInputV1
    base_context: Dict[str, Any]
    capability_set: Dict[str, Any]
    memory: Dict[str, Any]
    shortlisted_tools: List[Dict[str, Any]]
    shortlisted_components: List[Dict[str, Any]]
    shortlisted_actions: List[Dict[str, Any]]
    intent: Dict[str, Any]
    reasoning: Dict[str, Any]
    prompt: str
    raw_response: str
    normalized: Dict[str, Any]
    validated: ToolPlanV1
