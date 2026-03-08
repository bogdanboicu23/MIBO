from __future__ import annotations

import json
from typing import Any, Dict, List, Optional


def safe_json_loads(text: str) -> Dict[str, Any]:
    content = (text or "").strip()
    try:
        parsed = json.loads(content)
        if isinstance(parsed, dict):
            return parsed
        return {}
    except json.JSONDecodeError:
        start = content.find("{")
        end = content.rfind("}")
        if start != -1 and end != -1 and end > start:
            candidate = content[start : end + 1]
            parsed = json.loads(candidate)
            if isinstance(parsed, dict):
                return parsed
        raise


def normalize_tool_plan(raw: Dict[str, Any]) -> Dict[str, Any]:
    if not isinstance(raw, dict):
        raw = {}

    raw.setdefault("schema", "tool_plan.v1")
    raw.setdefault("rationale", "Planned tool steps based on user request and available capabilities.")

    steps = raw.get("steps", [])
    if not isinstance(steps, list):
        steps = []

    normalized_steps: List[Dict[str, Any]] = []
    for i, step in enumerate(steps, start=1):
        if not isinstance(step, dict):
            continue
        tool_name = str(step.get("tool") or "").strip()
        if not tool_name:
            continue
        normalized_steps.append(
            {
                "id": str(step.get("id") or f"step_{i}"),
                "tool": tool_name,
                "args": step.get("args", {}) if isinstance(step.get("args", {}), dict) else {},
                "cache_ttl_seconds": step.get("cache_ttl_seconds", None),
            }
        )

    raw["steps"] = normalized_steps

    ui = raw.get("uiIntent")
    if ui is not None and isinstance(ui, dict):
        if "component_tree" not in ui and isinstance(ui.get("components"), list):
            ui["component_tree"] = {
                "type": "layout",
                "name": "column",
                "props": {"gap": 12},
                "children": [
                    {
                        "type": "component",
                        "name": str(c.get("name", "markdown")),
                        "props": c.get("props", {}) if isinstance(c, dict) else {},
                        "children": [],
                    }
                    for c in ui.get("components", [])
                    if isinstance(c, dict)
                ],
            }
            ui.pop("components", None)

        if "component_tree" not in ui:
            raw["uiIntent"] = None
        else:
            normalized_tree = _normalize_component_tree(ui.get("component_tree"))
            if normalized_tree is None:
                raw["uiIntent"] = None
                return raw

            ui["component_tree"] = normalized_tree
            ui["bindings"] = _normalize_bindings(ui.get("bindings"), normalized_tree)
            ui["subscriptions"] = _normalize_subscriptions(ui.get("subscriptions"))
            raw["uiIntent"] = ui
    else:
        raw["uiIntent"] = None

    safety = raw.get("safety")
    if not isinstance(safety, dict):
        safety = {}

    if not raw["steps"]:
        safety.setdefault("needAssistantAnswer", True)
    else:
        safety.setdefault("needAssistantAnswer", False)

    raw["safety"] = safety
    return raw


def filter_unknown_tools(plan: Dict[str, Any], allowed_tools: set[str]) -> Dict[str, Any]:
    if not isinstance(plan, dict):
        return plan
    if not allowed_tools:
        return plan

    steps = plan.get("steps", [])
    if not isinstance(steps, list):
        return plan

    filtered: List[Dict[str, Any]] = []
    removed: List[str] = []
    for step in steps:
        if not isinstance(step, dict):
            continue
        tool = str(step.get("tool", "")).strip()
        if tool in allowed_tools:
            filtered.append(step)
        elif tool:
            removed.append(tool)

    plan["steps"] = filtered
    if removed:
        safety = plan.get("safety")
        if not isinstance(safety, dict):
            safety = {}
        safety["needAssistantAnswer"] = True
        prev_reason = str(safety.get("reason", "")).strip()
        reason = f"Removed unknown tools: {', '.join(sorted(set(removed)))}."
        safety["reason"] = f"{prev_reason} {reason}".strip()
        plan["safety"] = safety

    return plan


def _normalize_component_tree(node: Any) -> Optional[Dict[str, Any]]:
    if isinstance(node, dict):
        if "root" in node and len(node) == 1:
            return _normalize_component_tree(node["root"])

        if node.get("type") in ("layout", "component"):
            ntype = node.get("type")
            name = str(node.get("name") or "").strip()
            if not name:
                return None

            props = node.get("props", {})
            if not isinstance(props, dict):
                props = {}

            children = node.get("children", [])
            if not isinstance(children, list):
                children = []

            normalized_children: List[Dict[str, Any]] = []
            for child in children:
                c = _normalize_component_tree(child)
                if c is not None:
                    normalized_children.append(c)

            return {
                "type": ntype,
                "name": name,
                "props": props,
                "children": normalized_children,
            }

        if "component" in node and isinstance(node["component"], str):
            props = {k: v for k, v in node.items() if k not in {"component", "children", "root"}}
            children = node.get("children", [])
            if not isinstance(children, list):
                children = []
            normalized_children = [c for c in (_normalize_component_tree(x) for x in children) if c is not None]
            return {
                "type": "component",
                "name": node["component"].strip(),
                "props": props,
                "children": normalized_children,
            }

        children: List[Dict[str, Any]] = []
        for k, v in node.items():
            if k in {"root", "bindings", "subscriptions"}:
                continue
            if isinstance(v, dict):
                props = v.get("props", v)
                if not isinstance(props, dict):
                    props = {}
            else:
                props = {}
            children.append(
                {
                    "type": "component",
                    "name": str(k).strip(),
                    "props": props,
                    "children": [],
                }
            )

        if children:
            return {
                "type": "layout",
                "name": "column",
                "props": {"gap": 12},
                "children": children,
            }
        return None

    if isinstance(node, list):
        children = [c for c in (_normalize_component_tree(x) for x in node) if c is not None]
        return {
            "type": "layout",
            "name": "column",
            "props": {"gap": 12},
            "children": children,
        }

    return None


def _normalize_bindings(bindings: Any, tree: Dict[str, Any]) -> List[Dict[str, Any]]:
    if not isinstance(bindings, list):
        return []

    normalized: List[Dict[str, Any]] = []
    for b in bindings:
        if not isinstance(b, dict):
            continue

        if all(k in b for k in ("componentPath", "prop", "from")):
            component_path = str(b["componentPath"]).strip()
            if not component_path.startswith("/"):
                component_path = "/root"
            prop = str(b["prop"]).strip()
            source = str(b["from"]).strip()
            if not prop or not source:
                continue
            normalized.append(
                {
                    "componentPath": component_path,
                    "prop": prop,
                    "from": source,
                }
            )
            continue

        component = str(b.get("component", "")).strip()
        prop = str(b.get("prop", "")).strip()
        tool = str(b.get("tool", "")).strip()
        arg = str(b.get("arg", "")).strip()
        if not prop or not tool:
            continue

        component_path = _find_component_path(tree, component) if component else None
        if not component_path:
            component_path = "/root"

        source = f"{tool}.{arg}" if arg else tool
        if component == "productDetail" and arg == "products":
            source = f"{tool}.products.0"
        normalized.append(
            {
                "componentPath": component_path,
                "prop": prop,
                "from": source,
            }
        )

    return normalized


def _normalize_subscriptions(subscriptions: Any) -> List[Dict[str, Any]]:
    if not isinstance(subscriptions, list):
        return []
    normalized: List[Dict[str, Any]] = []
    for s in subscriptions:
        if not isinstance(s, dict):
            continue
        event = str(s.get("event", "")).strip()
        refresh = s.get("refresh", [])
        if not event or not isinstance(refresh, list):
            continue
        norm_refresh = []
        for r in refresh:
            if not isinstance(r, dict):
                continue
            tool = str(r.get("tool", "")).strip()
            patch_path = str(r.get("patchPath", "")).strip()
            args = r.get("args", {})
            if not tool or not patch_path or not isinstance(args, dict):
                continue
            norm_refresh.append({"tool": tool, "args": args, "patchPath": patch_path})
        if norm_refresh:
            normalized.append({"event": event, "refresh": norm_refresh})
    return normalized


def _find_component_path(node: Dict[str, Any], name: str, path: str = "/root") -> Optional[str]:
    if node.get("type") == "component" and str(node.get("name", "")).strip() == name:
        return path
    children = node.get("children", [])
    if not isinstance(children, list):
        return None
    for i, child in enumerate(children):
        if not isinstance(child, dict):
            continue
        found = _find_component_path(child, name, f"{path}/children/{i}")
        if found:
            return found
    return None
