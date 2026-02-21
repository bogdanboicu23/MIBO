from __future__ import annotations
from typing import Any, Dict, List, Optional
from pydantic import BaseModel, Field


class ToolStep(BaseModel):
    id: str = Field(..., description="Unique id for step within plan (e.g., step_1).")
    tool: str = Field(..., description="Tool name from toolCatalog.tools[].name")
    args: Dict[str, Any] = Field(default_factory=dict)
    cache_ttl_seconds: Optional[int] = Field(
        default=None, description="Optional override for caching behavior"
    )


class UiIntent(BaseModel):
    """
    We keep this generic and schema-driven:
    - component_tree is an abstract tree the .NET UiComposer can translate into ui.v1
    """
    component_tree: Dict[str, Any] = Field(
        ...,
        description="Abstract component tree using names from uiComponentCatalog.components[].name",
    )
    bindings: Optional[List[Dict[str, Any]]] = None
    subscriptions: Optional[List[Dict[str, Any]]] = None


class ToolPlanV1(BaseModel):
    schema: str = Field("tool_plan.v1", pattern="^tool_plan\\.v1$")
    rationale: str = Field(..., description="Short explanation why these tools/components were selected.")
    steps: List[ToolStep] = Field(default_factory=list)
    uiIntent: Optional[UiIntent] = None
    safety: Optional[Dict[str, Any]] = None


class PlannerConstraints(BaseModel):
    maxSteps: int = Field(8, ge=1, le=32)


class PlannerMeta(BaseModel):
    conversationId: Optional[str] = None
    userId: Optional[str] = None


class PlannerInputV1(BaseModel):
    schema: str = Field("planner_input.v1", pattern="^planner_input\\.v1$")
    userPrompt: str

    # Generic objects for decoupling:
    conversationContext: Dict[str, Any] = Field(default_factory=dict)
    toolCatalog: Dict[str, Any] = Field(default_factory=dict)
    uiComponentCatalog: Dict[str, Any] = Field(default_factory=dict)
    constraints: PlannerConstraints = Field(default_factory=PlannerConstraints)
    meta: PlannerMeta = Field(default_factory=PlannerMeta)