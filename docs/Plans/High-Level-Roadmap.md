# High-Level Roadmap

This roadmap tracks upcoming engine/editor parity work at a planning level. The agent editor API is the next planned item. Later items are intentionally listed as conceptualized, not yet planned until the agent API has been used to generate and validate test content.

## Planned

### Agent editor API layer and behavior-primitive remodeling

Status: Current priority / in-process work. The first in-process facade is implemented; the next priority is remodeling canonical action-plan authoring around behavior primitives before stabilizing or externalizing the API.

Supporting documents:

- [Agent API Wishlist](Agent-API-Wishlist.md)
- [Agent Editor API Plan](Agent-Editor-API-Plan.md)

Goal: create a stable, constrained agent-facing API over the editor/content services so agents can author content through the same capability model as the editor GUI. Canonical authoring should expose action plans as behavior primitives with typed configuration, required state, and followup ports, rather than exposing arbitrary low-level checks/effects/actions as the primary authoring model.

Completed first-slice scope:

- Add an in-process API facade over existing editor/content services before choosing an external protocol. The first facade is implemented as `AgentContentEditorApi` in the Editor project.
- Support document/session operations, validation, YAML preview/diff, entity template authoring, actor initial facing, action plans/steps, supported checks, and supported effects.
- Keep authoring canonical: do not expose legacy arbitrary variables, legacy variable fields, or `SetVariable` as new authoring commands.
- Return structured results and actionable diagnostics from mutating operations.
- Add tests that use the API to generate movement-capable test content, validate it, and inspect the resulting YAML.

Current phased work:

#### Phase 1: Define the behavior primitive model

Status: Satisfied by `Behavior-Primitive-Action-Plans.md`; keep open only for small clarifications discovered during implementation.

Purpose: establish the canonical model before expanding the API surface.

Scope:

- Define what a behavior primitive/action-plan primitive is.
- Define required state, initializable state, implicit state writes, and followup ports.
- Define `Wandering`, `SeekTarget`, `PickupTarget`, and `BumpTarget` as the first-pass primitive catalog.
- Define how low-level checks/effects remain available as legacy/advanced/internal machinery.

Testable outcomes:

- A checked-in design/update document describes behavior primitives, state requirements, followup ports, and the initial primitive catalog.
- The capability manual distinguishes canonical behavior-primitive authoring from advanced/legacy low-level step/check/effect authoring.
- The documented `Wandering` behavior explains that it requires `Facing`, attempts one move, sets `Target` to the blocker for followup, reverses `Facing` for the next turn when blocked, and resolves to one action.
- The documented `SeekTarget` behavior explains that it requires `Target`, moves toward target using counter-clockwise tie-breaking, and uses the generic followup when movement fails for any reason.

#### Phase 2: Add Core/content descriptor support

Purpose: represent behavior primitives in content without requiring authors to assemble implementation steps.

Scope:

- Add a descriptor shape for primitive-backed action plans or behavior plans.
- Preserve loading/runtime compatibility for existing step/check/effect descriptors.
- Materialize or interpret the first primitive subset through engine-owned behavior semantics.

Testable outcomes:

- Unit tests can materialize primitive descriptors into executable runtime behavior.
- A `Wandering` entity moves in its current `Facing` direction when unblocked.
- When blocked, `Wandering` updates canonical `Target` to the blocker and reverses canonical `Facing` for the next turn.
- A blocked `Wandering` plan can call its configured followup and still resolves to exactly one consumed action.
- A `SeekTarget` entity moves toward `Target` using counter-clockwise tie-breaking and uses followup or `Wait` when movement fails.
- `PickupTarget` reproduces the pickup portion of current `handleBlocker` behavior.
- `BumpTarget` reproduces the current `handleBlocker` fallback behavior.
- Existing step/check/effect YAML still loads and existing compatibility tests continue to pass.

#### Phase 3: Add validation and editor/content parity

Purpose: make behavior primitives safe and inspectable through content validation and editor services.

Scope:

- Validate required state for primitive-backed plans.
- Validate followup references and followup port configuration.
- Surface diagnostics for missing `Facing`, missing followup plans, unsupported primitive kinds, or malformed primitive configuration.
- Keep arbitrary variable mutation and `SetVariable` out of canonical authoring.

Testable outcomes:

- Validation reports an actionable diagnostic when an entity assigned `Wandering` lacks initial `Facing` or another valid source of `Facing`.
- Validation reports an actionable diagnostic when `Wandering.onBlocked` references a missing plan.
- Canonical validation passes for a valid entity using `Wandering` with initial `Facing` and valid followup.
- Canonical validation continues to flag arbitrary variable fields and `SetVariable` as non-canonical authoring.

#### Phase 4: Adjust the agent/editor API around behavior primitives

Purpose: expose the new canonical authoring model before external transport is added.

Scope:

- Add API commands for creating/selecting primitive-backed action plans.
- Add API commands for configuring primitive followup ports.
- Keep entity assignment and initial state authoring simple.
- Demote low-level step/check/effect construction to advanced/internal API surface or keep it out of normal agent workflows.

Testable outcomes:

- Tests can create a `Wandering` primitive plan through the agent API without manually adding `CanMove`, `Move`, `BlockingEntity`, `ReverseDirection`, or `CallPlan` steps.
- Tests can assign `Wandering` to an entity and set initial `Facing` through the agent API.
- Tests can configure `Wandering.onBlocked` to call a chosen followup plan through the agent API.
- The agent API rejects or clearly marks unsupported attempts to author arbitrary internal state mutation.
- Generated YAML/content validates with zero canonical diagnostics for the supported primitive path.

#### Phase 5: Re-run generated-content exercises and revise roadmap

Purpose: confirm the new model improves authoring and identify remaining true engine gaps.

Scope:

- Re-run the barrel/trap/rat exercise using temporary/generated content.
- Record which requests are supported by primitive behavior authoring and which need new engine capabilities.
- Decide whether additional primitive behaviors should be planned.

Testable outcomes:

- Barrel can still be authored as a passive container.
- Rat can be authored as a `Wandering` entity with initial `Facing` and configured followup without low-level step construction.
- Rat taking two actions per turn remains classified as scheduler/speed engine work unless a speed capability has been added.
- Trap bumping all four directions remains classified as a new behavior/action primitive or scheduler capability unless such a primitive has been added.
- The roadmap is updated based on the exercise results.

Additional API follow-up after behavior remodeling:

- Exercise the facade against temporary/generated content instead of checked-in prototype content.
- Add dry-run or save-preview support so agents can inspect canonicalization before writing existing files.
- Add convenience authoring helpers for common supported patterns discovered during exercises.
- Keep newly discovered gameplay semantics, such as multiple actions per turn or all-direction trap behavior, out of the API until they are planned as engine capabilities.

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

### Reaction action-plan slots and bump-triggered interactions

Status: Conceptualized, not yet planned.

Concept: entities may eventually expose action-plan slots beyond their normal turn behavior. The current/default turn plan should remain compatible with a future `onTurn` slot, while reaction slots may be invoked during another entity's root turn. The important future capability is that a successful interaction can trigger the target entity to run a reaction action plan; the exact trigger, including whether it is bump-specific, remains intentionally unsettled.

Planning notes:

- This depends on the behavior-primitive/fallback-chain remodeling but is not required for the first canonical chain slice.
- Persistent entity action state, especially `Facing` and `Target`, should be considered separately from per-invocation action-plan context before reaction slots are implemented.
- Cross-entity reaction chains will need explicit root actor/current actor/instigator semantics, trace causality, and temporal recursion guards.
- This may overlap with future scheduler/speed work, but it should not be used as a shortcut for multiple scheduled actions per turn.
- This may overlap with future action primitives and runtime states, especially any eventual reaction-trigger or interaction-target primitive.

### Behavior model consolidation

Status: Planned next architectural direction.

Concept: add a new canonical behavior system beside the existing low-level action-plan compatibility model. The editor-facing model should present an entity's Action Plan as an ordered fallback chain of engine-defined Action Steps. Existing low-level `steps/checks/effects` remain legacy/advanced/internal compatibility rather than the canonical GUI model.

Supporting document:

- [Behavior Model Consolidation Plan](Behavior-Model-Consolidation-Plan.md)

Archived foundation work:

- [Behavior Primitive/Fallback Foundation Archive](../Archived/Behavior-Primitive-Fallback-Foundation.md)

### Behavior chain trace formatter

Status: Conceptualized, not yet planned.

Concept: add a compact trace/log formatter for behavior fallback chains so tests, debugging, and future UI can inspect chain resolution without reading the full raw trace tree. Example output might summarize each attempted Action Step, whether it succeeded or failed, why fallback continued, and what entity/state was affected.

Planning deferred until the canonical behavior chain runtime exists and its trace shape has stabilized.
