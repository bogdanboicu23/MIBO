from __future__ import annotations

from langchain_core.tools import BaseTool

from agent.tooling import ALL_TOOLS


def build_tool_registry() -> dict[str, BaseTool]:
    return {tool.name: tool for tool in ALL_TOOLS}


TOOL_REGISTRY = build_tool_registry()
