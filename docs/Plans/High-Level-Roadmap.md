# High-Level Roadmap

This roadmap tracks upcoming engine/editor parity work at a planning level. The first canonical behavior-chain slice is complete. The active direction is now to finish GUI clarity for that slice, then clean up legacy behavior authoring before adding many new primitives.

## Planned

### Finish behavior-chain GUI clarity

Status: Current finishing slice before closing the sprint.

Supporting documents:

- [Behavior System Next Steps](Behavior-System-Next-Steps.md)
- [Behavior Model Consolidation First Slice](../Archived/Behavior-Model-Consolidation-First-Slice.md)

Goal: make the existing minimal GUI behavior editor easier to understand before calling the sprint finished.

Scope:

- Make the canonical behavior-chain section visually primary.
- Clearly label low-level `steps/checks/effects` as legacy/advanced compatibility.
- Show selected action-plan shape: canonical behavior, transitional primitive, legacy low-level, or empty/passive.
- Show concise default-state hints for current Action Steps, especially `Facing = West` and `Target = Self`.

### Legacy behavior cleanup and behavior trace formatter

Status: Upcoming planned work after the GUI finishing slice.

Supporting documents:

- [Behavior System Next Steps](Behavior-System-Next-Steps.md)
- [Agent API Wishlist](Agent-API-Wishlist.md)
- [Agent Editor API Plan](Agent-Editor-API-Plan.md)

Goal: reduce legacy behavior authoring surface area, then add a compact behavior-chain trace formatter before expanding the Action Step catalog.

Planned order:

1. Legacy behavior cleanup plan and implementation where safe.
2. Behavior trace formatter.
3. New canonical Action Step planning.

Legacy cleanup should not delete runtime/content compatibility until canonical replacements exist for important legacy patterns.

### Completed: behavior model consolidation first slice

Status: Completed / archived.

Supporting document:

- [Behavior Model Consolidation First Slice](../Archived/Behavior-Model-Consolidation-First-Slice.md)

Goal completed: make the editor-facing behavior model capable of representing an Action Plan as an ordered fallback chain of engine-defined Action Steps. The first canonical chain supports `MoveFacing -> PickupTarget` without requiring authors or agents to assemble low-level checks/effects or link separate primitive-backed fallback plan descriptors.

Completed foundation scope:

- Add an in-process API facade over existing editor/content services before choosing an external protocol. The first facade is implemented as `AgentContentEditorApi` in the Editor project.
- Support document/session operations, validation, YAML preview/diff, entity template authoring, actor initial facing, action plans/steps, supported checks, and supported effects.
- Keep authoring canonical: do not expose legacy arbitrary variables, legacy variable fields, or `SetVariable` as new authoring commands.
- Return structured results and actionable diagnostics from mutating operations.
- Add tests that use the API to generate movement-capable test content, validate it, and inspect the resulting YAML.
- Add persistent entity action state for `Facing` and `Target`.
- Add transitional primitive-backed descriptor/runtime support for `MoveFacing` and `PickupTarget`, including linked fallback references.
- Add validation, editor service, and agent API support for primitive-backed plans and a `MoveFacing -> PickupTarget` helper.

Completed sprint slice:

- Add a canonical Action Plan / Fallback Chain descriptor that is an ordered list of engine-defined Action Steps.
- Interpret that ordered list with fallback-by-order semantics.
- Keep legacy low-level descriptors and transitional primitive-backed descriptors loadable and executable.
- Add an Action Step catalog/metadata source for `MoveFacing` and `PickupTarget`.
- Add validation and editor/agent API operations for authoring the first canonical chain without low-level check/effect construction.
- Add minimal GUI support for viewing and editing catalog-backed behavior chains.
- Run a content-editor-style generated-content exercise and record outcomes.

#### Archived/superseded primitive remodeling notes

Status: completed as transitional foundation, then archived/superseded by behavior model consolidation.

The primitive-backed linked-plan foundation remains compatibility/prototype work and should keep loading/executing while the new canonical behavior chain is added. Details are preserved in:

- [Behavior Primitive Action Plans](Behavior-Primitive-Action-Plans.md)
- [Behavior Primitive/Fallback Foundation Archive](../Archived/Behavior-Primitive-Fallback-Foundation.md)

Do not continue the older plan by adding more linked top-level primitive plans as the editor-facing model. New canonical work should follow `Behavior-System-Next-Steps.md`.

#### Re-run generated-content exercises and revise roadmap

Status: Completed for the first behavior-chain exercise pass.

Purpose: confirm the new model improves authoring and identify remaining true engine gaps.

Scope:

- Re-run the barrel/trap/rat exercise using temporary/generated content.
- Record which requests are supported by ordered behavior-chain authoring and which need new engine capabilities.
- Decide whether additional canonical Action Steps or behavior templates should be planned.

Testable outcomes:

- Barrel can still be authored as a passive container.
- Rat can be authored with an ordered `MoveFacing -> PickupTarget` behavior chain and initial/defaulted `Facing` without low-level step construction or linked fallback plan descriptors.
- Rat taking two actions per turn remains classified as scheduler/speed engine work unless a speed capability has been added.
- Trap bumping all four directions remains classified as a new behavior/action primitive or scheduler capability unless such a primitive has been added.
- The roadmap is updated based on the exercise results.

Exercise results:

- A content-editor-style exercise found the docs and API surface sufficient to identify the preferred canonical behavior route.
- Passive barrel/container content is supported with entity template inventory/capacity fields and no default action plan.
- Rat behavior is supported through canonical `MoveFacing -> PickupTarget` behavior chains; a convenience helper now exists to avoid confusion with the older primitive-backed linked-chain helper.
- Trap all-direction behavior remains unsupported and should be planned as a future engine/editor capability only after semantics are defined.
- Rat two-actions-per-turn remains unsupported and should stay classified as future scheduler/speed work.

Additional API follow-up after behavior remodeling:

- Exercise the facade against temporary/generated content instead of checked-in prototype content.
- Add dry-run or save-preview support so agents can inspect canonicalization before writing existing files.
- Add convenience authoring helpers for common supported patterns discovered during exercises.
- Keep newly discovered gameplay semantics, such as multiple actions per turn or all-direction trap behavior, out of the API until they are planned as engine capabilities.

### New canonical Action Steps

Status: Upcoming after legacy cleanup and behavior trace formatting.

Candidate ideas include `Wait`, `ReverseFacing`, `BumpTarget`, and `SeekTarget`, but each should be planned with concrete semantics before implementation.

## Conceptualized, not yet planned

### Weight mechanics simplification

Status: Conceptualized, not yet planned.

Concept: remove carrying capacity as a primary mechanic and replace it with a simpler containment rule where an entity may exist inside another entity when the contained entity's weight is less than or equal to the container entity's weight. This likely treats weight more like bulk or volume than physical mass.

Planning deferred until the agent API can author and validate inventory/weight test content.

### Runtime entity indexing and simulation efficiency

Status: Conceptualized, not yet planned.

Concept: add runtime indexes so entity interactions can resolve more quickly while many entities are simulated. Likely indexes include entity ID, plane/world location, container ownership, and eventually relationship or template/tag lookups.

Planning deferred until current authored scenarios and generated test content provide clearer performance targets.

### Diegetic action-plan entities

Status: Conceptualized, not yet planned.

Concept: represent action plans diegetically as entities during gameplay. Each entity would have an action-plan stack that can be inspected as its own inventory-like space, and rearranging plans would change runtime behavior.

Planning deferred because this depends on stable action-plan authoring, inventory/containment semantics, and likely runtime indexing.

### New action primitives and runtime states

Status: Conceptualized, not yet planned.

Concept: add new primitives and state needed for basic gameplay scenarios, potentially including entity creation, entity destruction, player/screen messages, per-action-plan cooldowns, moving toward arbitrary targets, and friendly/hostile entity lists.

Generated content exercises have also surfaced possible future needs for multiple actions per turn and canonical multi-direction effects. These remain conceptualized, not yet planned.

Specific near-term canonical Action Steps such as `Wait`, `ReverseFacing`, `BumpTarget`, and `SeekTarget` are listed as upcoming only after legacy cleanup and behavior trace formatting. Broader gameplay primitives remain conceptual until selected and planned.

### Scheduler/speed and multiple actions per turn

Status: Conceptualized, not yet planned.

Concept: support entities taking more than one action per turn, variable speed, initiative, or action budgets. Rat two-actions-per-turn remains classified here after the generated-content exercise.

### Behavior/action-plan templates

Status: Conceptualized, not yet planned.

Concept: add quality-of-life workflows for reusable behavior templates, including apply-template, save-as-template, template editing, and template usage display.

Planning deferred because current behavior-chain descriptors are sufficient for engine/editor parity, and templates are not currently required as a foundation for other capabilities.

### Reaction action-plan slots and bump-triggered interactions

Status: Conceptualized, not yet planned.

Concept: entities may eventually expose action-plan slots beyond their normal turn behavior. The current/default turn plan should remain compatible with a future `onTurn` slot, while reaction slots may be invoked during another entity's root turn. The important future capability is that a successful interaction can trigger the target entity to run a reaction action plan; the exact trigger, including whether it is bump-specific, remains intentionally unsettled.

Planning notes:

- This depends on behavior-chain consolidation but is not required for the first canonical chain slice.
- Persistent entity action state, especially `Facing` and `Target`, should be considered separately from per-invocation action-plan context before reaction slots are implemented.
- Cross-entity reaction chains will need explicit root actor/current actor/instigator semantics, trace causality, and temporal recursion guards.
- This may overlap with future scheduler/speed work, but it should not be used as a shortcut for multiple scheduled actions per turn.
- This may overlap with future action primitives and runtime states, especially any eventual reaction-trigger or interaction-target primitive.
