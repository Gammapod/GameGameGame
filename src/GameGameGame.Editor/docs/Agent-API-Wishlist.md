# Agent API Wishlist

This document describes the API surface that would make GameGameGame content authoring practical and reliable for code agents. The goal is not to replace the human GUI, but to expose the same editor capabilities through a headless, scriptable, strongly validated interface.

## Primary goals

- Let agents create, edit, inspect, validate, and save content without GUI automation.
- Keep agent workflows aligned with the human editor so both use the same rules and validation.
- Make every operation deterministic, inspectable, and easy to recover from.
- Prefer structured inputs/outputs over free-form text.

## Required capabilities

### 1. Headless content session API

Agents need a programmatic equivalent of the editor session:

- Create new content document.
- Open existing YAML content file.
- Save and save-as.
- Reload from disk.
- Report dirty state.
- Return YAML preview.
- Return a real ordered diff against the saved baseline.
- Validate current in-memory document.

The current `ContentEditorSession` is close to this, but the agent-facing layer should expose stable, documented request/response contracts.

### 2. Machine-readable diagnostics

Validation should return structured diagnostics, not only strings.

Each diagnostic should include:

- severity
- code
- message
- YAML path, when known
- entity template id, when relevant
- action plan id/template id, when relevant
- step index, when relevant
- carried entity id, when relevant
- expected and actual value kinds, when relevant
- suggested fix, when available

Agents can act much more reliably when diagnostics identify exact content locations and expected corrections.

### 3. Strict schema validation

The API should support a strict validation mode that rejects:

- unknown YAML properties
- misspelled enum values
- missing required fields
- duplicate IDs
- malformed IDs
- invalid glyph values
- negative values where unsupported

The current loader ignores unmatched properties, which is convenient for compatibility but risky for agent-authored content.

### 4. Checked-in content schema

Please provide a schema file for the content format, ideally generated or kept in sync with the editor API.

Useful forms:

- JSON Schema for YAML editor tooling
- OpenAPI schema if the API is HTTP-based
- machine-readable enum/value catalog

This should include all valid values for:

- `PresentationColor`
- `PlanCheckKind`
- `PlanEffectKind`
- `PlanValueKind`
- `Direction`

### 5. CRUD operations for entity templates

Required entity operations:

- List entity templates.
- Get entity template by id.
- Create entity template from explicit fields.
- Duplicate entity template.
- Rename display name.
- Update inventory dimensions.
- Update weight/carrying capacity.
- Update presentation glyph/color.
- Delete entity template, with reference checks.
- List references to an entity template.

Important: creation should support both generated IDs and explicit requested IDs. If an explicit ID conflicts or is invalid, return a structured error.

### 6. Inventory layout operations

Required inventory operations:

- List carried entities for a template.
- Place carried entity in first open cell.
- Place carried entity at explicit coordinate.
- Move carried entity.
- Remove carried entity.
- Replace carried entity template reference.
- Find first open cell.
- Validate prospective placement without mutation.
- Return occupied/free cell grid.

Every mutating operation should return the changed object and any warnings/diagnostics.

### 7. CRUD operations for action plans

Required action plan operations:

- List action plans.
- Get action plan by id.
- Create action plan.
- Duplicate action plan.
- Delete action plan, with reference checks.
- List references to an action plan.
- Rename plan id/template id if supported.
- Validate action plan independently and in context of assigned entity templates.

### 8. Action plan step editing

Required step operations:

- List steps.
- Add step.
- Update step label.
- Move step.
- Remove step.
- Add/update/move/remove checks.
- Set/clear success effect.
- Set/clear failure effect.

The API should expose available check/effect primitives with their required fields and variable read/write behavior. Agents need to know which fields are required before constructing a step.

### 9. Default action plan variables

Required variable operations:

- List default variables for an entity template.
- Set default variable.
- Remove default variable.
- Suggest required variables for an assigned action plan.
- Identify missing variables.
- Identify type mismatches.
- Suggest safe default values by required kind.

### 10. Catalog/discovery endpoints

Agents need discoverable metadata instead of hardcoding assumptions.

Useful catalog data:

- valid enum values
- available plan check primitives
- available plan effect primitives
- required fields per primitive
- variable read/write fields per primitive
- content document version
- supported API version
- known content file paths
- default prototype content path

### 11. Transaction or dry-run support

Agents frequently need to test edits before committing them.

Useful modes:

- dry-run operation: return proposed YAML/diff/diagnostics without mutating session
- transaction begin/commit/rollback
- batch apply multiple operations atomically
- reject commit if validation fails above a configurable severity

### 12. Formatting and canonicalization

The API should provide a canonical formatter for content YAML.

Desired behavior:

- stable key ordering
- stable indentation
- predictable enum casing
- optional comments preserved if feasible
- no unrelated reformatting when possible

This reduces noisy diffs and makes agent edits easier to review.

### 13. Content-wide validation command

There should be one operation that validates every checked-in content file, not just the built-in prototype YAML.

The operation should report:

- files checked
- files skipped and why
- all diagnostics grouped by file
- overall pass/fail status

This is especially important for files like `Calibrations.yml` if they are intended to remain valid content.

### 14. Structured error model

All API failures should use a predictable error object, for example:

```json
{
  "code": "MissingActionPlanReference",
  "message": "Entity template slime references missing action plan wandering.",
  "path": "entityTemplates.slime.defaultActionPlanId",
  "recoverable": true,
  "suggestedActions": []
}
```

Avoid exceptions or free-form status strings as the primary control flow for agent-facing operations.

### 15. Stable IDs and reference management

Agents need clear ID rules:

- allowed characters
- casing convention
- maximum length, if any
- uniqueness scope
- generated ID algorithm
- rename behavior
- reference update behavior

If IDs can be renamed, the API should optionally update references automatically and return the full affected set.

## Nice-to-have capabilities

- Undo/redo history exposed to the API.
- Content search by id/name/glyph/action primitive/reference.
- Import/export selected templates or action plans.
- Compare two content documents semantically.
- Generate starter templates from presets, such as item, creature, container, terrain, AI actor.
- Explain an action plan in human-readable form.
- Simulate or trace an action plan against a small test world, once engine-facing support exists.

## Preferred interface shapes

Any of these would work well for agents:

- CLI tool emitting JSON.
- Local HTTP API with OpenAPI documentation.
- MCP server exposing content-editor tools.
- .NET library with stable public DTOs plus a thin CLI wrapper.

The most agent-friendly option would be a CLI or MCP layer over the same core service used by the GUI.

## Minimum viable agent API

If prioritizing a first pass, I would start with:

1. `validate <file> --json --strict`
2. `format <file>`
3. `list-entities <file> --json`
4. `get-entity <file> <id> --json`
5. `upsert-entity <file> <json>`
6. `list-action-plans <file> --json`
7. `get-action-plan <file> <id> --json`
8. `upsert-action-plan <file> <json>`
9. `diff <file> --json`
10. `apply-batch <file> <operations.json> --dry-run --json`

That would be enough for agents to author most current content safely.

## Summary

The existing content/editor services already provide a strong foundation. The main missing piece for agents is a formal, headless, structured API with strict validation, schemas, deterministic formatting, and machine-readable diagnostics. Once those are available, agents can author content confidently without relying on GUI automation or direct YAML guessing.
