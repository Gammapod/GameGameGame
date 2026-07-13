# Agent Content API Wishlist

Status: Wishlist / backlog reference, not an active implementation plan. Promote items only through the roadmap reset when frontend/editor-browser work needs them.

This document describes the remaining API surface that would make GameGameGame content authoring practical and reliable for code agents. It has been updated to reflect the current editor/content services.

The editor now has a strong headless foundation in `GameGameGame.Content`, especially `ContentEditorSession`, `ContentEditorService`, `EditableContentDocument`, `ContentValidationResult`, and `ContentDiagnostic`. The goal of the agent API is therefore not to recreate editor logic, but to expose the existing content-authoring capabilities through stable, documented, structured contracts.

## Primary goals

- Let agents create, edit, inspect, validate, format, diff, and save content without GUI automation.
- Keep agent workflows aligned with the human editor by reusing the same content services and validation rules.
- Make every operation deterministic, inspectable, and easy to recover from.
- Prefer structured inputs/outputs over free-form text.
- Avoid direct YAML guessing when an editor operation exists.

## Current foundation

The existing services already provide these core authoring capabilities and should be wrapped rather than reimplemented:

- Content sessions: create new, open YAML, save, save-as, reload, dirty state, YAML preview, validation.
- Entity presets: list, get, create with generated ID, duplicate, update, delete with reference checks.
- Entity fields: display name, inventory dimensions, weight, carrying capacity, glyph, color, default action plan.
- Inventory layout: list carried entities, place in first open cell, place at coordinate, move, remove, replace template reference, find first open cell, validate prospective placement.
- Action plans: list, create with generated ID, duplicate, delete with reference checks.
- Action plan steps: add, update, move, remove.
- Action plan checks/effects: add/update/move/remove checks, set success effect, set/clear failure effect.
- Actor state and targeting: set/clear initial facing using canonical `actionStateDefaults`; list/set/remove entity `targetingRules`; set behavior-step `targetSlot` where target-consuming behavior should use a non-default slot.
- Validation diagnostics: structured severity/code/message plus entity, plan, step, variable, slot, expected/actual kind, carried entity, related entity, and coordinate fields.

## Required remaining capabilities

### 1. Stable agent-facing interface

Provide a documented, headless API over the current content services.

Preferred shapes:

- CLI tool emitting JSON.
- MCP server exposing content-editor tools.
- Local HTTP API with OpenAPI documentation.
- .NET library with stable public DTOs plus a thin CLI or MCP wrapper.

The most useful first version would be a CLI or MCP layer over `ContentEditorSession` and `ContentEditorService`.

Every operation should have stable request/response DTOs. Agent-facing contracts should not expose implementation-specific exceptions or GUI view-model state as primary control flow.

### 2. Structured error model for all operations

Validation diagnostics are now structured, but mutation/file/session failures are still often string-only results or thrown exceptions. Agent-facing failures should use a predictable object, for example:

```json
{
  "code": "InventoryCellOccupied",
  "message": "Cannot place carried entity at 0,0; cell is already occupied by carriedRock.",
  "path": "entityTemplates.bag.carriedEntities[1].coord",
  "recoverable": true,
  "suggestedActions": []
}
```

Useful fields:

- `code`
- `message`
- `severity`
- `path`, when known
- `recoverable`
- `suggestedActions`
- relevant IDs/indexes/coordinates

Avoid exceptions or free-form status strings as the primary agent-facing response.

### 3. Strict and canonical validation modes

The current YAML loaders still ignore unmatched properties. Agent-authored content needs an opt-in strict mode that rejects:

- unknown YAML properties
- misspelled enum values
- missing required fields
- duplicate IDs
- malformed IDs
- invalid glyph values
- negative values where unsupported
- non-canonical legacy authoring forms

Canonical validation should also expose `EditableContentDocument.ValidateCanonicalAuthoring()` behavior, especially around legacy arbitrary plan variables and legacy variable fields.

### 4. Checked-in content schema and catalogs

Provide checked-in machine-readable schema/catalog files, generated or kept in sync with the content API.

Useful outputs:

- JSON Schema for YAML editor tooling.
- OpenAPI schema if HTTP-based.
- MCP/CLI metadata schema if tool-based.
- Machine-readable enum and primitive catalogs.

Catalogs should include valid values for:

- `PresentationColor`
- `PlanCheckKind`
- `PlanEffectKind`
- `PlanValueKind`
- `Direction`

They should also expose action primitive metadata already present in the engine/content model:

- available check primitives
- available effect primitives
- required/optional fields per primitive
- variable read/write fields per primitive, if still applicable
- slot read/write behavior
- default descriptor produced for each primitive
- legacy-only primitives or fields, such as `SetVariable`, when applicable
- stable versus legacy action-step authoring tiers, including compatibility-only target acquisition or turn-only facing steps

### 5. Explicit IDs and reference management

Current creation APIs generate IDs from names. Agents also need explicit-ID creation and upsert paths.

The API should define and enforce ID rules:

- allowed characters
- casing convention
- maximum length, if any
- uniqueness scope
- generated ID algorithm
- explicit-ID conflict behavior
- rename behavior
- reference update behavior

Required additions:

- Create entity template with requested ID.
- Create action plan with requested template ID/runtime ID.
- Rename entity template ID, optionally updating references.
- Rename action plan template/runtime ID, optionally updating references.
- Return affected references for any rename/delete operation.

### 6. Real ordered diff and semantic diff

`ContentEditorSession.GetYamlDiff()` currently provides a simple line-membership diff. Agents need a deterministic, ordered diff against the saved baseline.

Desired outputs:

- ordered unified diff text
- structured JSON diff hunks
- semantic diff option for entity/action-plan changes
- indication of whether formatting-only changes occurred

### 7. Dry-run, batch, and transaction support

Agents frequently need to test edits before committing them.

Useful modes:

- dry-run operation: return proposed YAML, ordered diff, and diagnostics without mutating the session
- batch apply multiple operations atomically
- reject commit/save if validation fails above a configurable severity
- transaction begin/commit/rollback, if stateful sessions are used

### 8. Formatting and canonicalization command

`EditableContentDocument.SaveYaml()` already canonicalizes some legacy content. Expose this as an explicit agent operation.

Desired behavior:

- stable key ordering where practical
- stable indentation
- predictable enum casing
- canonical actor state defaults instead of legacy `defaultPlanVariables.facing`
- canonical primitive fields, avoiding legacy arbitrary variable fields where possible
- no unrelated reformatting when possible

### 9. Content-wide validation command

Provide one operation that validates every checked-in content file intended to remain valid.

The operation should report:

- files checked
- files skipped and why
- all diagnostics grouped by file
- strict/canonical validation status per file
- overall pass/fail status

This is especially important for files such as `PrototypeContent.yaml` and `Calibrations.yml` if they are both intended to remain valid content inputs.

### 10. YAML path support in diagnostics

Current diagnostics contain useful semantic fields, but not YAML paths. Add path information where feasible.

Examples:

- `entityTemplates.slime.defaultActionPlanId`
- `entityTemplates.bag.carriedEntities[0].coord`
- `actionPlans.wandering.steps[1].checks[0]`
- `actionPlans.wandering.steps[1].onSuccess.planId`

When exact paths cannot be known, return the closest stable semantic location.

### 11. Canonical actor state, targeting, and plan slot authoring

The old wishlist focused on default action plan variables. Current content now has canonical actor state defaults and slot validation.

Agent APIs should prefer canonical operations:

- list actor state defaults for an entity template
- set/clear initial facing
- list/set/remove entity targeting rules by slot, target template, range, and optional hint
- set/clear behavior-step target slots for target-consuming behavior
- set/clear initial target only for compatibility or deliberately pre-seeded runtime state
- identify missing required slots for assigned action plans
- suggest safe actor state defaults by required slot kind

Arbitrary `defaultPlanVariables`, legacy variable fields, legacy target-acquisition steps, and turn-only facing mutation should be treated as legacy/advanced authoring unless deliberately reintroduced as supported content.

### 12. Content search and reference discovery

Add structured search/discovery helpers useful for agents:

- search by entity/action plan ID
- search by display name
- search by glyph/color
- search by primitive kind
- list references to an entity template
- list references to an action plan
- list carried entities referencing a template
- list templates assigned to an action plan

Some reference helpers already exist internally; the agent API should expose them consistently.

## Nice-to-have capabilities

- Undo/redo history exposed to the API.
- Import/export selected templates or action plans.
- Compare two content documents semantically.
- Generate starter templates from presets such as item, creature, container, terrain, AI actor.
- Explain an action plan in human-readable form.
- Simulate or trace an action plan against a small test world, once engine-facing support exists.

## Updated minimum viable agent API

If prioritizing a first pass, build a thin JSON-emitting CLI or MCP server around the existing content services.

Suggested MVP operations:

1. `validate <file> --json --strict --canonical`
2. `format <file> --check|--write --json`
3. `diff <file> --json --ordered`
4. `list-entities <file> --json`
5. `get-entity <file> <entityTemplateId> --json`
6. `create-entity <file> --json <entity.json> [--id <id>] [--dry-run]`
7. `update-entity <file> <entityTemplateId> --json <patch.json> [--dry-run]`
8. `delete-entity <file> <entityTemplateId> [--dry-run] --json`
9. `list-carried <file> <parentEntityTemplateId> --json`
10. `place-carried <file> <parentEntityTemplateId> <templateId> [--entity-id <id>] [--x <x> --y <y>] [--dry-run] --json`
11. `move-carried <file> <parentEntityTemplateId> <entityId> --x <x> --y <y> [--dry-run] --json`
12. `remove-carried <file> <parentEntityTemplateId> <entityId> [--dry-run] --json`
13. `list-action-plans <file> --json`
14. `get-action-plan <file> <actionPlanTemplateId> --json`
15. `create-action-plan <file> --json <plan.json> [--id <id>] [--dry-run]`
16. `update-action-plan <file> <actionPlanTemplateId> --json <patch.json> [--dry-run]`
17. `delete-action-plan <file> <actionPlanTemplateId> [--dry-run] --json`
18. `assign-default-action-plan <file> <entityTemplateId> <actionPlanTemplateId> [--dry-run] --json`
19. `set-actor-state <file> <entityTemplateId> --facing <direction>|--clear-facing [--dry-run] --json`
20. `list-targeting-rules <file> <entityTemplateId> --json`
21. `set-targeting-rule <file> <entityTemplateId> --slot <n> --target-template <id> --range <n> [--hint <text>] [--dry-run] --json`
22. `remove-targeting-rule <file> <entityTemplateId> --slot <n> [--dry-run] --json`
23. `set-behavior-target-slot <file> <actionPlanTemplateId> <stepIndex> --slot <n>|--clear [--dry-run] --json`
24. `catalog --json`
25. `validate-all --json --strict --canonical`
26. `apply-batch <file> <operations.json> --dry-run --json`

Each mutating command should return:

- success/failure
- changed object or affected IDs
- structured errors/diagnostics
- validation result after the proposed change
- ordered diff
- YAML preview when requested

## Summary

The original wishlist asked for many editor capabilities that now exist in `GameGameGame.Content`. The remaining need is a stable, structured, headless agent API over those capabilities, plus strict validation, schemas/catalogs, explicit ID/reference management, ordered diffs, dry-run/batch operations, and canonical actor-state/targeting-oriented authoring.
