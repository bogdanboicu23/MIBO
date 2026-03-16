from __future__ import annotations

import json
import os
import re
from typing import Any

from langchain_core.messages import HumanMessage, SystemMessage
from langchain_groq import ChatGroq

from agent.prompts import COMPOSER_SYSTEM_PROMPT
from agent.state import PipelineState
from agent.token_utils import count_tokens, truncate_to_tokens
from models.schemas import FinalResponse, parse_json_payload, strip_outer_json_fence

SOFT_TOKEN_LIMIT = 8500


def _build_model() -> ChatGroq:
    groq_api_key = os.getenv("GROQ_API_KEY", "")
    if not groq_api_key:
        raise RuntimeError("GROQ_API_KEY is not configured.")

    return ChatGroq(
        groq_api_key=groq_api_key,
        model="llama-3.3-70b-versatile",
        temperature=0,
        streaming=True,
    )


def _chunk_to_text(chunk_content: Any) -> str:
    if isinstance(chunk_content, str):
        return chunk_content

    if isinstance(chunk_content, list):
        parts: list[str] = []
        for item in chunk_content:
            if isinstance(item, str):
                parts.append(item)
            elif isinstance(item, dict):
                text = item.get("text")
                if isinstance(text, str):
                    parts.append(text)
            elif hasattr(item, "text") and isinstance(item.text, str):
                parts.append(item.text)
        return "".join(parts)

    return ""


def _build_payload(intent: dict[str, Any], plan: dict[str, Any], tool_results: list[dict[str, Any]]) -> str:
    return "\n\n".join(
        [
            f"Intent:\n{json.dumps(intent, ensure_ascii=False, indent=2)}",
            f"Execution plan:\n{json.dumps(plan, ensure_ascii=False, indent=2)}",
            f"Tool results:\n{json.dumps(tool_results, ensure_ascii=False, indent=2)}",
        ]
    )


def _fit_payload(state: PipelineState) -> str:
    tool_results = list(state.get("tool_results", []))
    user_message = state.get("user_message", "")
    payload = _build_payload(state.get("intent", {}), state.get("plan", {}), tool_results)
    payload_with_user_message = "\n\n".join(
        [
            f"Original user message:\n{user_message}",
            payload,
        ]
    )

    if count_tokens(f"{COMPOSER_SYSTEM_PROMPT}\n{payload_with_user_message}") <= SOFT_TOKEN_LIMIT:
        return payload_with_user_message

    per_result_budget = 1000
    while per_result_budget >= 250:
        reduced_results = [
            {
                **result,
                "data": truncate_to_tokens(result.get("data", ""), per_result_budget),
            }
            for result in tool_results
        ]
        payload = _build_payload(state.get("intent", {}), state.get("plan", {}), reduced_results)
        payload_with_user_message = "\n\n".join(
            [
                f"Original user message:\n{user_message}",
                payload,
            ]
        )
        if count_tokens(f"{COMPOSER_SYSTEM_PROMPT}\n{payload_with_user_message}") <= SOFT_TOKEN_LIMIT:
            state["tool_results"] = reduced_results
            return payload_with_user_message
        per_result_budget -= 250

    state["tool_results"] = [
        {**result, "data": truncate_to_tokens(result.get("data", ""), 250)}
        for result in tool_results
    ]
    return "\n\n".join(
        [
            f"Original user message:\n{user_message}",
            _build_payload(state.get("intent", {}), state.get("plan", {}), state["tool_results"]),
        ]
    )


def _looks_like_markdown_or_code(text: str) -> bool:
    candidate = text.strip()
    if not candidate:
        return False

    return (
        "```" in candidate
        or "\n" in candidate
        or candidate.startswith("#")
        or candidate.startswith("* ")
        or candidate.startswith("- ")
        or "using " in candidate
        or "class " in candidate
        or "function " in candidate
        or "const " in candidate
        or "var " in candidate
    )


def _promote_markdown_response(response: FinalResponse) -> FinalResponse:
    if response.components:
        return response

    if not _looks_like_markdown_or_code(response.text):
        return response

    return FinalResponse.model_validate(
        {
            "text": "",
            "components": [
                {
                    "type": "markdown",
                    "props": {
                        "content": response.text,
                    },
                }
            ],
        }
    )


def _normalize_model_output(raw_content: str) -> str:
    return strip_outer_json_fence(raw_content)


def _decode_json_string_fragment(fragment: str) -> str:
    cleaned = fragment.strip().rstrip(",")
    attempts = [
        cleaned,
        cleaned.rstrip("\\"),
        cleaned.replace("\r", "\\r").replace("\n", "\\n"),
        cleaned.rstrip("\\").replace("\r", "\\r").replace("\n", "\\n"),
    ]

    for attempt in attempts:
        if not attempt:
            continue
        try:
            return json.loads(f'"{attempt}"')
        except json.JSONDecodeError:
            continue

    return (
        cleaned
        .replace('\\"', '"')
        .replace("\\n", "\n")
        .replace("\\r", "\r")
        .replace("\\t", "\t")
        .replace("\\\\", "\\")
    )


def _extract_text_field(candidate: str) -> str:
    match = re.search(r'"text"\s*:\s*"', candidate, re.DOTALL)
    if not match:
        return ""

    start = match.end()
    escaped = False
    for index in range(start, len(candidate)):
        char = candidate[index]
        if escaped:
            escaped = False
            continue
        if char == "\\":
            escaped = True
            continue
        if char == '"':
            return _decode_json_string_fragment(candidate[start:index])

    end_markers = [
        candidate.find(',"components"', start),
        candidate.find('"components"', start),
        candidate.find("```", start),
    ]
    valid_markers = [marker for marker in end_markers if marker != -1]
    end = min(valid_markers) if valid_markers else len(candidate)
    return _decode_json_string_fragment(candidate[start:end])


def _extract_components_field(candidate: str) -> list[dict[str, Any]]:
    return _extract_named_array_field(candidate, "components")


def _extract_named_array_field(candidate: str, field_name: str) -> list[dict[str, Any]]:
    match = re.search(rf'"{re.escape(field_name)}"\s*:\s*\[', candidate, re.DOTALL)
    if not match:
        return []

    start = candidate.find("[", match.start())
    depth = 0
    in_string = False
    escaped = False

    for index in range(start, len(candidate)):
        char = candidate[index]

        if in_string:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
            continue

        if char == '"':
            in_string = True
            continue

        if char == "[":
            depth += 1
        elif char == "]":
            depth -= 1
            if depth == 0:
                try:
                    parsed = json.loads(candidate[start : index + 1])
                    return parsed if isinstance(parsed, list) else []
                except json.JSONDecodeError:
                    return []

    return []


def _recover_final_response(raw_content: str) -> FinalResponse:
    candidate = _normalize_model_output(raw_content)
    text = _extract_text_field(candidate)
    components = _extract_named_array_field(candidate, "components")
    data_sources = _extract_named_array_field(candidate, "data_sources")
    actions = _extract_named_array_field(candidate, "actions")
    subscriptions = _extract_named_array_field(candidate, "subscriptions")

    if not text and not components:
        fallback_text = candidate.strip()
        if fallback_text.startswith("{") and fallback_text.endswith("}"):
            fallback_text = ""
        text = fallback_text

    return FinalResponse.model_validate(
        {
            "text": text,
            "components": components,
            "data_sources": data_sources,
            "actions": actions,
            "subscriptions": subscriptions,
        }
    )


async def composer_agent_node(state: PipelineState) -> PipelineState:
    payload = _fit_payload(state)
    raw_chunks: list[str] = []
    stream_callback = state.get("stream_callback")

    async for chunk in _build_model().astream(
        [
            SystemMessage(content=COMPOSER_SYSTEM_PROMPT),
            HumanMessage(content=payload),
        ]
    ):
        text = _chunk_to_text(getattr(chunk, "content", ""))
        if not text:
            continue

        raw_chunks.append(text)
        if stream_callback is not None:
            await stream_callback({"type": "chunk", "content": text})

    raw_output = "".join(raw_chunks)
    try:
        parsed = FinalResponse.model_validate(parse_json_payload(raw_output))
    except (json.JSONDecodeError, ValueError):
        parsed = _recover_final_response(raw_output)
    parsed = _promote_markdown_response(parsed)
    state["final_response"] = parsed.model_dump()
    return state
