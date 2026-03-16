from __future__ import annotations

from agent.state import PipelineState
from agent.token_utils import truncate_to_tokens
from agent.tools import (
    calculate_affordability,
    get_categories,
    get_product,
    get_user_finances,
    list_products,
    search_products,
)


async def executor_node(state: PipelineState) -> PipelineState:
    tool_registry = {
        "search_products": search_products,
        "get_product": get_product,
        "list_products": list_products,
        "get_categories": get_categories,
        "get_user_finances": get_user_finances,
        "calculate_affordability": calculate_affordability,
    }

    results: list[dict] = []
    tool_calls = state.get("plan", {}).get("tool_calls", [])

    for call in tool_calls:
        tool_name = call.get("tool", "")
        tool_args = call.get("args", {})
        result_key = call.get("result_key", tool_name or "result")
        tool_fn = tool_registry.get(tool_name)

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
