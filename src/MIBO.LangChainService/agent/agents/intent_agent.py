from __future__ import annotations

import os

from langchain_core.messages import HumanMessage, SystemMessage
from langchain_groq import ChatGroq

from agent.memory import compact_context, render_history_for_prompt
from agent.prompts import INTENT_SYSTEM_PROMPT
from agent.state import PipelineState
from agent.token_utils import count_tokens
from models.schemas import IntentResult, parse_json_payload

SOFT_TOKEN_LIMIT = 8500


def _build_model() -> ChatGroq:
    groq_api_key = os.getenv("GROQ_API_KEY", "")
    if not groq_api_key:
        raise RuntimeError("GROQ_API_KEY is not configured.")

    return ChatGroq(
        groq_api_key=groq_api_key,
        model="llama-3.3-70b-versatile",
        temperature=0,
        streaming=False,
    )


def _build_prompt_payload(history: list[dict], user_message: str) -> str:
    history_text = render_history_for_prompt(history)
    return f"Recent conversation history:\n{history_text}\n\nCurrent user message:\n{user_message}"


async def _ensure_context_budget(state: PipelineState) -> str:
    history = list(state.get("conversation_history", []))
    payload = _build_prompt_payload(history, state["user_message"])

    if count_tokens(f"{INTENT_SYSTEM_PROMPT}\n{payload}") <= SOFT_TOKEN_LIMIT:
        return payload

    history = await compact_context(state["session_id"])
    state["conversation_history"] = history
    payload = _build_prompt_payload(history, state["user_message"])

    while count_tokens(f"{INTENT_SYSTEM_PROMPT}\n{payload}") > SOFT_TOKEN_LIMIT and len(history) > 1:
        drop_index = 1 if history and history[0].get("role") == "system" else 0
        history.pop(drop_index)
        payload = _build_prompt_payload(history, state["user_message"])

    state["conversation_history"] = history
    return payload


async def intent_agent_node(state: PipelineState) -> PipelineState:
    payload = await _ensure_context_budget(state)
    response = await _build_model().ainvoke(
        [
            SystemMessage(content=INTENT_SYSTEM_PROMPT),
            HumanMessage(content=payload),
        ]
    )

    parsed = IntentResult.model_validate(parse_json_payload(str(response.content)))
    state["intent"] = parsed.model_dump()

    stream_callback = state.get("stream_callback")
    if stream_callback is not None:
        await stream_callback({"type": "status", "content": "Understanding your request..."})

    return state
