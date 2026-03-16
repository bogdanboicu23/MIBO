from __future__ import annotations

import json
import re
from typing import Any, Literal

from pydantic import BaseModel, ConfigDict, Field


class ComponentSpec(BaseModel):
    model_config = ConfigDict(extra="ignore")

    type: str
    props: dict[str, Any] = Field(default_factory=dict)
    data_source: str | None = None
    data_source_params: dict[str, Any] = Field(default_factory=dict)


class FieldDescriptor(BaseModel):
    model_config = ConfigDict(extra="ignore")

    name: str
    kind: str


class FieldHintsSpec(BaseModel):
    model_config = ConfigDict(extra="ignore")

    entity_type: str | None = None
    collection_path: str | None = None
    label_field: str | None = None
    value_field: str | None = None
    title_field: str | None = None
    image_field: str | None = None
    category_field: str | None = None
    search_field: str | None = None
    default_table_fields: list[str] = Field(default_factory=list)
    text_fields: list[str] = Field(default_factory=list)
    numeric_fields: list[str] = Field(default_factory=list)
    fields: list[FieldDescriptor] = Field(default_factory=list)


class TransformFieldSpec(BaseModel):
    model_config = ConfigDict(extra="ignore")

    key: str
    label: str | None = None
    source_field: str | None = None
    value: Any = None
    expression: dict[str, Any] | None = None
    kind: str | None = None


class DataTransformSpec(BaseModel):
    model_config = ConfigDict(extra="ignore")

    id: str | None = None
    type: str
    input_path: str | None = None
    output_path: str | None = None
    fields: list[TransformFieldSpec] = Field(default_factory=list)
    field_hints: FieldHintsSpec | None = None


class DataSourceSpec(BaseModel):
    model_config = ConfigDict(extra="ignore")

    id: str
    handler: str
    default_args: dict[str, Any] = Field(default_factory=dict)
    refresh_on_load: bool = False
    refresh_on_conversation_open: bool = False
    stale_after_ms: int | None = None
    transforms: list[DataTransformSpec] = Field(default_factory=list)
    field_hints: FieldHintsSpec | None = None


class ActionSpec(BaseModel):
    model_config = ConfigDict(extra="ignore")

    id: str
    action_type: str = "ui.action.execute"
    handler: str | None = None
    data_source_id: str | None = None
    default_args: dict[str, Any] = Field(default_factory=dict)
    refresh_data_source_ids: list[str] = Field(default_factory=list)


class FinalResponse(BaseModel):
    model_config = ConfigDict(extra="ignore")

    text: str = ""
    components: list[ComponentSpec] = Field(default_factory=list)
    data_sources: list[DataSourceSpec] = Field(default_factory=list)
    actions: list[ActionSpec] = Field(default_factory=list)
    subscriptions: list[dict[str, Any]] = Field(default_factory=list)


class ToolCallSpec(BaseModel):
    model_config = ConfigDict(extra="ignore")

    tool: str
    args: dict[str, Any] = Field(default_factory=dict)
    result_key: str


class ComponentPlan(BaseModel):
    model_config = ConfigDict(extra="ignore")

    type: str
    data_origin: str
    data_source: str | None = None
    data_source_params: dict[str, Any] = Field(default_factory=dict)
    props_template: dict[str, Any] = Field(default_factory=dict)


class ExecutionPlan(BaseModel):
    model_config = ConfigDict(extra="ignore")

    tool_calls: list[ToolCallSpec] = Field(default_factory=list)
    data_sources: list[DataSourceSpec] = Field(default_factory=list)
    actions: list[ActionSpec] = Field(default_factory=list)
    components_plan: list[ComponentPlan] = Field(default_factory=list)
    subscriptions: list[dict[str, Any]] = Field(default_factory=list)
    response_text_instruction: str = ""


class IntentEntities(BaseModel):
    model_config = ConfigDict(extra="ignore")

    explicit_data: dict[str, Any] = Field(default_factory=dict)
    required_services: list[str] = Field(default_factory=list)
    suggested_components: list[str] = Field(default_factory=list)


class IntentResult(BaseModel):
    model_config = ConfigDict(extra="ignore")

    intent_type: Literal["data_query", "ui_request", "calculation", "mixed", "conversational"]
    needs_external_data: bool
    needs_ui: bool
    needs_calculation: bool
    entities: IntentEntities = Field(default_factory=IntentEntities)
    summary: str


class ChatRequest(BaseModel):
    session_id: str = ""
    message: str


class StoredMessage(BaseModel):
    role: Literal["system", "user", "assistant"]
    content: str
    timestamp: str
    tokens_approx: int


_JSON_BLOCK_PATTERN = re.compile(r"^\s*```(?:json)?\s*$", re.IGNORECASE)


def strip_outer_json_fence(raw_content: str) -> str:
    candidate = raw_content.strip()
    first_newline = candidate.find("\n")
    if first_newline == -1:
        return candidate

    opening_line = candidate[:first_newline].strip()
    if not _JSON_BLOCK_PATTERN.match(opening_line):
        return candidate

    body = candidate[first_newline + 1 :]
    trimmed_body = body.rstrip()
    if trimmed_body.endswith("```"):
        last_fence_index = trimmed_body.rfind("```")
        if last_fence_index != -1:
            body = trimmed_body[:last_fence_index]

    return body.strip()


def _extract_balanced_json_fragment(candidate: str) -> str | None:
    start = candidate.find("{")
    if start == -1:
        return None

    depth = 0
    in_string = False
    escaped = False

    for index in range(start, len(candidate)):
        char = candidate[index]

        if in_string:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
            continue

        if char == '"':
            in_string = True
            continue

        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return candidate[start : index + 1]

    return None


def parse_json_payload(raw_content: str) -> dict[str, Any]:
    candidate = strip_outer_json_fence(raw_content)

    if not candidate.startswith("{"):
        start = candidate.find("{")
        end = candidate.rfind("}")
        if start != -1 and end != -1 and end > start:
            candidate = candidate[start : end + 1]

    try:
        return json.loads(candidate)
    except json.JSONDecodeError:
        balanced = _extract_balanced_json_fragment(candidate)
        if balanced:
            return json.loads(balanced)
        raise
