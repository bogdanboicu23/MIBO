from __future__ import annotations
from fastapi import FastAPI, HTTPException
from pydantic import ValidationError

from schemas import PlannerInputV1, ToolPlanV1
from planner import plan
from config import settings

app = FastAPI(
    title=settings.app_name,
    version="1.0",
    description="Generates tool plans (ToolPlanV1) from planner inputs. Swagger UI is available at /docs.",
    contact={"name": "MIBO Team"},
    root_path="/langchain",  # 👈 IMPORTANT
    docs_url="/docs",
    redoc_url="/redoc",
)

@app.get("/healthz")
def healthz():
    return {"status": "ok"}

@app.post("/v1/plan", response_model=ToolPlanV1)
async def v1_plan(inp: PlannerInputV1):
    try:
        return await plan(inp)
    except ValidationError as ve:
        raise HTTPException(status_code=400, detail=f"Planner failed: ValidationError: {ve}")
    except Exception as e:
        raise HTTPException(status_code=400, detail=f"Planner failed: {type(e).__name__}: {e}")