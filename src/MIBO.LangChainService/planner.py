from __future__ import annotations

from schemas import PlannerInputV1, ToolPlanV1
from planner_graph import LangGraphPlanner


_planner = LangGraphPlanner()


async def plan(input_data: PlannerInputV1) -> ToolPlanV1:
    return await _planner.plan(input_data)
