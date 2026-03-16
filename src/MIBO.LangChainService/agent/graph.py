from __future__ import annotations

from langgraph.graph import StateGraph

from agent.agents.composer_agent import composer_agent_node
from agent.agents.executor_agent import executor_node
from agent.agents.intent_agent import intent_agent_node
from agent.agents.planner_agent import planner_agent_node
from agent.memory import memory_loader_node, memory_saver_node
from agent.state import PipelineState


def build_graph():
    graph = StateGraph(PipelineState)

    graph.add_node("memory_loader", memory_loader_node)
    graph.add_node("intent_agent", intent_agent_node)
    graph.add_node("planner_agent", planner_agent_node)
    graph.add_node("executor_agent", executor_node)
    graph.add_node("composer_agent", composer_agent_node)
    graph.add_node("memory_saver", memory_saver_node)

    graph.set_entry_point("memory_loader")
    graph.add_edge("memory_loader", "intent_agent")
    graph.add_edge("intent_agent", "planner_agent")
    graph.add_edge("planner_agent", "executor_agent")
    graph.add_edge("executor_agent", "composer_agent")
    graph.add_edge("composer_agent", "memory_saver")
    graph.set_finish_point("memory_saver")

    return graph.compile()
