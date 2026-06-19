# Vertical Slice Map

Status: Source of truth for cross-layer implementation navigation.

Read when:

- implementing a feature that touches more than one engine/editor layer;
- adding or changing an Action Step, primitive, descriptor, validation rule, editor/API operation, or GUI workflow;
- planning tests/docs for a vertical slice.

Do not read when:

- making a narrow one-layer fix whose owning file and tests are already known.

## Layer order for engine/editor parity work

Use this as a navigation map, not a mandatory scope checklist. A slice may intentionally stop before later layers, but the stopping point should be explicit in the active plan or handoff notes. The current Avalonia GUI is legacy-priority / maintenance-mode, so new work normally stops at editor service, agent/headless API, tests, and docs unless GUI work is explicitly selected.

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
7. **Current Avalonia GUI workflow, optional/legacy-priority**
   - Human-facing view-model/window support only when the active plan explicitly selects current GUI work or when maintaining an already-supported GUI workflow.
8. **Tests**
   - Layer-appropriate tests for Core, Content, Editor service/API, integration/scenario behavior, and GUI only when GUI work is in scope.
9. **Capability documentation**
   - Update `Engine-Editor-Capabilities.md` when actual support status or authoring guidance changes.
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
7. Current Avalonia GUI support only if explicitly selected; otherwise document the intentional stopping point.
8. Tests and capability documentation.

Notes:

- Prefer canonical ordered behavior-chain Action Steps for new normal authoring.
- Do not reintroduce arbitrary variable-name authoring for canonical workflows.
- If current Avalonia GUI support is intentionally deferred, document the support tier and stopping point.

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

### GUI usability slice

Status: Legacy-priority / maintenance-mode unless explicitly selected.

Typical touched layers:

1. Existing view-model state and selection/refresh behavior.
2. GUI layout/window changes.
3. Editor service calls backing the workflow.
4. View-model and GUI-focused tests.
5. Capability documentation if GUI support status changes.

Notes:

- Preserve canonical behavior-chain authoring as visually primary.
- Keep legacy low-level action-plan authoring hidden unless editing an existing legacy low-level plan.
