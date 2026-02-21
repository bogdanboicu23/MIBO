from __future__ import annotations

import json
from typing import Any, Dict, Optional

from pydantic import ValidationError
from langchain_groq import ChatGroq
from langchain_core.messages import SystemMessage, HumanMessage

from config import settings
from schemas import PlannerInputV1, ToolPlanV1
from prompt_template import build_planner_prompt


def _safe_json_loads(text: str) -> Dict[str, Any]:
    """
    Attempts to parse JSON. If model wraps JSON in extra text, try to extract the first {...} block.
    """
    text = text.strip()
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        # Try extract the first balanced JSON object
        start = text.find("{")
        end = text.rfind("}")
        if start != -1 and end != -1 and end > start:
            candidate = text[start : end + 1]
            return json.loads(candidate)
        raise


def normalize_tool_plan(raw: Dict[str, Any]) -> Dict[str, Any]:
    """
    Deterministic normalization to satisfy ToolPlanV1 schema.
    This is NOT business coupling; it is contract enforcement.
    """
    if not isinstance(raw, dict):
        raw = {}

    raw.setdefault("schema", "tool_plan.v1")
    raw.setdefault(
        "rationale",
        "Planned tool steps based on the user request and available tools/components.",
    )

    # steps
    steps = raw.get("steps", [])
    if not isinstance(steps, list):
        steps = []
    for i, s in enumerate(steps):
        if isinstance(s, dict):
            s.setdefault("id", f"step_{i+1}")
            s.setdefault("args", {})
            # allow cache_ttl_seconds optional
            if "cache_ttl_seconds" not in s:
                s["cache_ttl_seconds"] = None
        else:
            steps[i] = {"id": f"step_{i+1}", "tool": "", "args": {}, "cache_ttl_seconds": None}
    raw["steps"] = steps

    # uiIntent normalization
    ui = raw.get("uiIntent", None)
    if ui is None:
        raw["uiIntent"] = None
        return raw

    if isinstance(ui, dict):
        # frequent model mistake: it outputs "components" instead of "component_tree"
        if "component_tree" not in ui and "components" in ui and isinstance(ui["components"], list):
            # convert list-of-components into a simple column layout
            ui["component_tree"] = {
                "type": "layout",
                "name": "column",
                "props": {"gap": 12},
                "children": [
                    {
                        "type": "component",
                        "name": c.get("name", "unknown"),
                        "props": c.get("props", {}),
                        "children": [],
                    }
                    for c in ui["components"]
                    if isinstance(c, dict)
                ],
            }
            ui.pop("components", None)

        # if still missing component_tree, set uiIntent to null
        if "component_tree" not in ui:
            raw["uiIntent"] = None
        else:
            ui.setdefault("bindings", None)
            ui.setdefault("subscriptions", None)
            raw["uiIntent"] = ui
    else:
        raw["uiIntent"] = None

    # safety optional
    if "safety" not in raw:
        raw["safety"] = {}

    return raw


def _repair_with_model(model: ChatGroq, raw_text: str, error: str) -> Dict[str, Any]:
    """
    Ask the model to output corrected JSON only.
    """
    sys = SystemMessage(
        content=(
            "You are a JSON repair assistant. Output ONLY valid JSON matching the required schema. "
            "No markdown, no extra text."
        )
    )
    user = HumanMessage(
        content=(
            "The previous JSON is invalid.\n\n"
            f"Validation error:\n{error}\n\n"
            "Here is the invalid JSON (or near-JSON):\n"
            f"{raw_text}\n\n"
            "Return ONLY corrected JSON with required fields: schema, rationale, steps[].id, steps[].tool, steps[].args, uiIntent.component_tree or uiIntent=null."
        )
    )
    resp = model.invoke([sys, user])
    return _safe_json_loads(resp.content)


async def plan(input_data: PlannerInputV1) -> ToolPlanV1:
    # constraints: respect request, but cap with env max to avoid abuse
    max_steps = min(input_data.constraints.maxSteps, settings.PLANNER_MAX_STEPS)

    payload = input_data.model_dump()
    payload["constraints"]["maxSteps"] = max_steps

    prompt = build_planner_prompt(payload)

    llm = ChatGroq(
        api_key=settings.GROQ_API_KEY,
        model=settings.GROQ_MODEL,
        temperature=settings.GROQ_TEMPERATURE,
        timeout=settings.GROQ_TIMEOUT_SECONDS,
        max_tokens=settings.GROQ_MAX_TOKENS,
    )

    # 1) First attempt
    resp = llm.invoke(
        [
            SystemMessage(content="You are a strict JSON planner."),
            HumanMessage(content=prompt),
        ]
    )

    raw_text = resp.content
    try:
        raw = _safe_json_loads(raw_text)
    except Exception as e:
        # If it fails parsing at all, try repair
        raw = _repair_with_model(llm, raw_text, f"JSON parse failed: {e}")

    # 2) Normalize
    raw = normalize_tool_plan(raw)

    # 3) Validate. If fails, repair N times, normalize each time, then validate.
    last_err: Optional[str] = None
    for _ in range(settings.PLANNER_REPAIR_ATTEMPTS + 1):
        try:
            return ToolPlanV1.model_validate(raw)
        except ValidationError as ve:
            last_err = str(ve)
            repaired = _repair_with_model(llm, json.dumps(raw), last_err)
            raw = normalize_tool_plan(repaired)

    raise ValidationError(f"Planner output invalid after repairs: {last_err}", ToolPlanV1)