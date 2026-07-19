---
id: source.vertical-slice-map
title: Vertical Slice Map
kind: source-of-truth
subkind: implementation-navigation
status: active
owners: [core-owner]
audience: [core-owner, frontend-owner, content-editor]
lane: vertical-slice
truth_rank: 35
truth_domains: [implementation-navigation]
read_when:
  - implementing a feature that touches more than one engine/editor layer
  - adding or changing an Action Step primitive descriptor validation rule editor/API operation or frontend/editor UI workflow
  - planning tests/docs for a vertical slice
do_not_read_when:
  - making a narrow one-layer fix whose owning file and tests are already known
related:
  - source.engine-editor-capabilities
  - source.content-authoring-manual
  - source.testing-charter
---
# Vertical Slice Map

Status: Source of truth for cross-layer implementation navigation.

Read when:

- implementing a feature that touches more than one engine/editor layer;
- adding or changing an Action Step, primitive, descriptor, validation rule, editor/API operation, or frontend/editor UI workflow;
- planning tests/docs for a vertical slice.

Do not read when:

- making a narrow one-layer fix whose owning file and tests are already known.

## Layer order for engine/editor parity work

Use this as a navigation map, not a mandatory scope checklist. A slice may intentionally stop before later layers, but the stopping point should be explicit in the active plan or handoff notes. The former Avalonia GUI has been removed, so new work normally stops at editor service, agent/headless API, SadConsole/future frontend UI when selected, tests, and docs.

1. **Core runtime behavior**
   - Engine semantics, turn/action execution, runtime state changes, trace behavior.
2. **Content descriptors / YAML model**
   - Serializable descriptors, DTO shape, canonical YAML fields, legacy compatibility fields.
3. **Validation and default policy**
   - Diagnostics, canonical/legacy checks, malformed content handling, defaultable state behavior.
4. **Catalog metadata / discovery**
   - Machine-readable Action Step, check, effect, state, or primitive metadata used by tools.
5. **Editor service authoring**
   - Typed operations over content documents/sessions; no direct YAML guessing when a service operation exists.
6. **Agent API facade**
   - Stable headless operations and structured results over editor/content services.
7. **Frontend/editor UI workflow, when selected**
   - Human-facing SadConsole or future frontend support only when the active plan explicitly selects UI work.
   - For canonical action slices, identify the player-facing facts exposed by the action, decide which facts require graphical presentation, reuse accepted component-gallery visual treatments where possible, and prototype new graphical treatments in the gallery before changing the play surface.
8. **Tests**
   - Layer-appropriate tests for Core, Content, Editor service/API, integration/scenario behavior, and frontend UI only when UI work is in scope.
9. **Capability and authoring documentation**
   - Update `Engine-Editor-Capabilities.md` when actual support status or layer coverage changes.
   - Update `Content-Authoring-Manual.md` when content-editor-facing authoring guidance or limits change.
10. **Planning/backlog documentation**
   - Update active plan or `High-Level-Roadmap.md` when scope changes, gaps are deferred, or follow-up items are promoted.

## Common vertical slices

### New canonical Action Step / primitive slice

Typical touched layers:

1. Core runtime behavior.
2. Descriptor/YAML model.
3. Validation/default policy.
4. Action Step catalog metadata.
5. Editor service authoring.
6. Agent API facade.
7. Frontend/editor UI support only if explicitly selected; otherwise document the intentional stopping point.
8. Tests and capability documentation.

Notes:

- Prefer canonical ordered behavior-chain Action Steps for new normal authoring.
- Do not reintroduce arbitrary variable-name authoring for canonical workflows.
- If frontend/editor UI support is intentionally deferred, document the support tier and stopping point.
- When frontend play-mode support is selected, do not default central player-facing facts to terminal-style text dumps. Preserve textual explanation/inspection, but decide the square-tile visual treatment or explicitly document why text-only presentation remains acceptable for that slice.

### Scenario feedback slice

Typical touched layers:

1. Editor/content service setup for temporary/generated content.
2. Core simulation execution.
3. Trace and state summary formatting.
4. Capability-gap classification.
5. Tests or generated temporary files, preferably not checked-in prototype content unless selected deliberately.
6. Roadmap updates for repeated gaps.

Notes:

- Use existing editor/content services and supported authoring operations where possible.
- Distinguish engine/editor bugs from unsupported design requests.

### Frontend/editor UI usability slice

Status: Selected only when active frontend/editor work calls for it.

Typical touched layers:

1. Existing frontend/component state and selection/refresh behavior.
2. UI layout/window changes.
3. Editor service calls backing the workflow.
4. Frontend/component-focused tests.
5. Capability documentation if frontend UI support status changes.

Notes:

- Preserve canonical behavior-chain authoring as visually primary.
- Keep legacy low-level action-plan authoring hidden unless editing an existing legacy low-level plan.
