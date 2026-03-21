from __future__ import annotations

from agent.state import PipelineState
from agent.tool_registry import TOOL_REGISTRY
from agent.token_utils import truncate_to_tokens


async def executor_node(state: PipelineState) -> PipelineState:
    results: list[dict] = []
    tool_calls = state.get("plan", {}).get("tool_calls", [])
    user_id = state.get("user_id", "")

    for call in tool_calls:
        tool_name = call.get("tool", "")
        tool_args = call.get("args", {})
        result_key = call.get("result_key", tool_name or "result")
        tool_fn = TOOL_REGISTRY.get(tool_name)

        if tool_name.startswith("spotify_") and user_id:
            tool_args["user_id"] = user_id

        if tool_fn is None:
            results.append({"key": result_key, "data": truncate_to_tokens({"error": f"Unknown tool '{tool_name}'"}, 300)})
            continue

        try:
            raw_result = await tool_fn.ainvoke(tool_args)
            truncated = truncate_to_tokens(raw_result, max_tokens=1500)
            results.append({"key": result_key, "data": truncated})
        except Exception as exc:  # pragma: no cover
            results.append({"key": result_key, "data": truncate_to_tokens({"error": str(exc)}, 500)})

    state["tool_results"] = results

    stream_callback = state.get("stream_callback")
    if stream_callback is not None and tool_calls:
        await stream_callback({"type": "status", "content": "Fetching data..."})

    return state
