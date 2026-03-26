from __future__ import annotations

import json

from langchain_core.messages import HumanMessage, SystemMessage

from agent.groq_provider import invoke_with_rotation
from agent.prompts import PLANNER_SYSTEM_PROMPT
from agent.state import PipelineState
from agent.token_utils import count_tokens, truncate_to_tokens
from models.schemas import ExecutionPlan, parse_json_payload

SOFT_TOKEN_LIMIT = 8500


async def planner_agent_node(state: PipelineState) -> PipelineState:
    payload = json.dumps(
        {
            "original_user_message": state.get("user_message", ""),
            "intent": state.get("intent", {}),
        },
        ensure_ascii=False,
        indent=2,
    )
    if count_tokens(f"{PLANNER_SYSTEM_PROMPT}\n{payload}") > SOFT_TOKEN_LIMIT:
        payload = truncate_to_tokens(payload, 6000)

    response = await invoke_with_rotation(
        [
            SystemMessage(content=PLANNER_SYSTEM_PROMPT),
            HumanMessage(content=f"Planning input JSON:\n{payload}"),
        ]
    )

    parsed = ExecutionPlan.model_validate(parse_json_payload(str(response.content)))
    state["plan"] = parsed.model_dump()

    stream_callback = state.get("stream_callback")
    if stream_callback is not None:
        await stream_callback({"type": "status", "content": "Planning the response..."})

    return state
