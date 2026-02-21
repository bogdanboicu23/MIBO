# ai/main.py
from __future__ import annotations
from fastapi import FastAPI, HTTPException, Body

from pydantic import ValidationError
from schemas import PlannerInputV1, ToolPlanV1
from planner import plan

from config import settings
app = FastAPI(
    title=settings.app_name,
    version="1.0",
    description="Generates tool plans (ToolPlanV1) from planner inputs. Swagger UI is available at /docs.",
    contact={"name": "MIBO Team"},
    docs_url="/docs",
    redoc_url="/redoc",
)

# Example request body to show in Swagger UI
DEFAULT_EXAMPLE = {
    "type": "planner_input.v1",
    "conversation_id": "conv_123",
    "user_id": "user_1",
    "message": "Find budget-friendly laptops under 1000 EUR and show price comparisons.",
    "history": [],
    "tool_catalog": [
        {"name": "finance.getBudgetSnapshot", "description": "Returns user budget info"},
        {"name": "shop.searchProducts", "description": "Search products by query and filters"}
    ],
    "ui_component_catalog": [],
    "constraints": {"max_tool_steps": 5}
}


@app.post("/v1/plan", response_model=ToolPlanV1)
async def v1_plan(inp: PlannerInputV1):
    try:
        return await plan(inp)
    except ValidationError as ve:
        # return readable validation message for .NET logs
        raise HTTPException(status_code=400, detail=f"Planner failed: ValidationError: {ve}")
    except Exception as e:
        raise HTTPException(status_code=400, detail=f"Planner failed: {type(e).__name__}: {e}")