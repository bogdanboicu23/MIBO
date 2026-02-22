from __future__ import annotations
from fastapi import FastAPI, HTTPException, APIRouter
from pydantic import ValidationError

from schemas import PlannerInputV1, ToolPlanV1
from planner import plan
from config import settings

app = FastAPI(
    title=settings.app_name,
    version="1.0",
    description="Generates tool plans (ToolPlanV1) from planner inputs.",
    contact={"name": "MIBO Team"},
    docs_url="/langchain/docs",
    redoc_url="/langchain/redoc",
    openapi_url="/langchain/openapi.json",
)

router = APIRouter(prefix="/langchain")

@router.get("/healthz")
def healthz():
    return {"status": "ok"}

@router.post("/v1/plan", response_model=ToolPlanV1)
async def v1_plan(inp: PlannerInputV1):
    try:
        return await plan(inp)
    except ValidationError as ve:
        raise HTTPException(status_code=400, detail=f"Planner failed: ValidationError: {ve}")
    except Exception as e:
        raise HTTPException(status_code=400, detail=f"Planner failed: {type(e).__name__}: {e}")

app.include_router(router)