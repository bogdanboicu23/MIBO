# MIBO Refactor: Conversation + LangChain + Client (UI/Tool Pipeline)

## 1) New architecture (modules and responsibilities)

### MIBO.ConversationService

- `Services/Chat/Pipeline/*`
  - Pipeline orchestration (ordered steps):
    1. persist user message
    2. load compact conversation context
    3. build typed `planner_input.v1`
    4. resolve/validate plan (with fallback)
    5. execute tools
    6. compose + validate `ui.v1`
    7. compose text
    8. persist assistant message and UI instance
- `Services/UI/Validation/*`
  - Strict validator for `ui.v1` schema/root/bindings/subscriptions/template tokens.
  - Used before UI is persisted/returned.
- `Services/Actions/*`
  - Config-driven action routing (exact/wildcard/default route).
  - Generic argument binding (payload/ui/context templates).
  - Tool selection via `toolCandidates`.
  - In-place patch composition (`ui.patch.v1`) with optional aliases.
- `DTOs/PlannerContracts/PlannerInputV1.cs`
  - Typed contract between ConversationService and LangChain planner.
  - Includes constraints/meta/correlation id.

### MIBO.LangChainService

- `planner_core/plugins.py`
  - Plugin registry for intent detectors and plan post-processors.
- `planner_core/stages.py`
  - Explicit stages:
    - intent detection
    - reasoning
    - planning
    - validation/repair
    - response composition
- `planner_core/capabilities.py`
  - Capability registry for tool/UI/action catalogs and shortlist generation.
- `planner_graph.py`
  - LangGraph now wires modular stages instead of embedding logic in one class.

### MIBO.Client

- Extended contract handling in `useChat.ts`
  - Supports mixed action responses (`text + uiPatch + toolStates + warnings`).
  - Applies in-place patches first, keeps backward-compatible message append when patch is absent.
  - Can bootstrap UI from patch seed when full `uiV1` is not present yet.
- `types/ui.ts`, `types/chat.ts`, `types/miboActionApi.ts`
  - Updated for new optional fields and `replace` patch op.

---

## 2) End-to-end standard flow

1. `POST /v1/chat`
2. Conversation pipeline first resolves conversation-memory intents (ex: "întrebarea anterioară"), then builds typed `planner_input.v1` with compact context when planning is needed.
3. LangChain returns `tool_plan.v1`.
4. Plan validated (tool existence + UI contract structure when strict mode is on).
5. Tool plan executes with arg binding/chaining and defaults.
6. UI composed as `ui.v1` and validated.
7. Text composed from tool results or safe fallback.
8. Response persisted and returned as `chat.response.v2` (backward compatible fields retained).

For UI actions:

1. `POST /v1/action`
2. Config route resolved (`action-routing.json`).
3. Args bound (payload + templates + defaults).
4. Tool executed.
5. Response mapped to `ui.patch.v1` and sent in-place.
6. Optional event publication and tool state metadata returned.

---

## 3) How to add a new tool

1. Add tool definition in:
   - `src/MIBO.ConversationService/config/tools.json`
2. Include:
   - `name`, `method`, `urlTemplate`
   - `requiredArgs`
   - optional `defaultArgs`, timeout/retry/cache fields
3. No code change needed for execution path:
   - Tool registry refresh picks it up.
   - Planner receives it via `planner_input.v1`.
   - Action routes can bind it directly.

---

## 4) How to add a new UI component

1. Register component contract in:
   - `src/MIBO.ConversationService/config/ui-components.json`
2. Add frontend renderer plugin/registry entry in:
   - `src/MIBO.Client/client/src/components/sandbox/uiRuntime/registry.tsx`
3. If component needs prop normalization, extend adapter logic in:
   - `UiRenderer.tsx` adapters section.
4. No ConversationService orchestration code changes are required.

---

## 5) Action -> Tool -> UI Patch

1. Define route in:
   - `src/MIBO.ConversationService/config/action-routing.json`
2. Use:
   - `mode: "tool"`
   - `tool` or `toolCandidates`
   - `argMap` / `argTemplate`
   - `patchPath` (+ optional `patchAliases`)
3. Action runtime:
   - `TemplateActionArgumentBinder` resolves args.
   - `ActionPatchComposer` generates `ui.patch.v1`.
4. Client applies patch to current UI instance without adding a new assistant message.

---

## 6) Migration and backward compatibility

- Chat/action payloads keep `action.v1`, `action.result.v1` and existing `text/uiV1/correlationId`.
- New fields are additive (`warnings`, `toolStates`, `schema`, patch metadata).
- Frontend still supports fallback behavior when backend sends full `uiV1` instead of patch.

---

## 7) Risks, trade-offs, and next steps

### Risks

- ConversationService was not built locally in this refactor cycle (explicit project constraint), so compile/runtime integration must be validated in running environment.
- Action declarative rules depend on config quality; invalid templates can lead to missing args.

### Trade-offs

- Strict UI validation can drop invalid UI and prefer safe text fallback (higher safety, lower permissiveness).
- Config-driven routing increases flexibility but requires stronger config governance/versioning.

### Next steps

1. Add automated tests:
   - action route resolution + arg binding + patch composition
   - UI validator (schema/binding/subscription edge cases)
   - planner stage/post-processor unit coverage
2. Add contract version tests between ConversationService and LangChain (`planner_input.v1`, `tool_plan.v1`).
3. Add observability dashboards for:
   - fallback rates
   - route misses
   - UI validation failures
   - tool latency/error distribution
