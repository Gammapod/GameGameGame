# High-Level Roadmap

Status: Active roadmap.

Read when:

- selecting or refining sprint work;
- sorting conceptual ideas into priority buckets;
- deciding whether a design gap should become planned work or remain deferred.

Related source of truth:

- `docs/Source of Truth/Engine-Editor-Capabilities.md` describes what is currently implemented and authorable.
- `docs/Source of Truth/planning-index.md` describes planning-document navigation and reading order.

## Current strategic direction

Prioritize the scenario feedback loop before adding broad new mechanics. The first utility Action Step batch is implemented, but generated exercises showed that authoring temporary scenarios, simulating turns, and reviewing compact behavior/state feedback is still too ad hoc. Direction, inventory, spawning, scheduler, and reaction decisions should be informed by scenario evidence instead of implemented speculatively.

The current Avalonia GUI is legacy-priority / maintenance-mode. New authoring and scenario-feedback work should prioritize editor services, agent/headless APIs, tests, and future frontend readiness rather than maintaining broad Avalonia GUI parity. Long-term human-facing editor work is expected to move toward an integrated game/editor frontend.

## Active / likely next sprint

### Generated scenario authoring and simulation feedback

Status: Likely next sprint focus.

Supporting document:

- [Next Sprint Scenario Testing Plan](Next-Sprint-Scenario-Testing-Plan.md)

Goal: improve the ability to create temporary/generated content, simulate turns, inspect behavior traces and state summaries, and identify unsupported capability gaps before committing to new mechanics.

Loose high-level scope:

- Add a headless scenario exercise helper around editor/content services and Core simulation.
- Support creating temporary content, assigning canonical behavior chains, spawning a small world, running turns, and collecting compact trace summaries.
- Prefer tests or generated temporary files over checked-in prototype content changes.
- Preserve Core/Editor parity: scenario helpers should use supported editor service / agent API operations where possible.

Potential testable outcomes:

- A generated scenario can author entities, inventories, action plans, and initial action state without editing checked-in content.
- A generated scenario can simulate multiple turns and return formatted behavior-chain traces and high-signal state summaries.
- The first utility Action Step batch can be exercised in generated scenarios without manual YAML inspection.
- Unsupported requests produce explicit capability-gap notes rather than silent YAML or simulation guesses.

## Prioritized backlog buckets

### Bucket 1: Scenario/testing/tooling feedback loop

Status: Highest-priority backlog bucket.

Priority order:

1. Headless generated scenario runner / exercise helper.
2. Compact world/state summary formatter for entity positions, facing, target, inventories, and other high-signal runtime state.
3. Plan preview + simulation in one API command.
4. Capability-gap reporter for unsupported authoring/simulation requests.
5. Lightweight scenario report template once first runner output reveals the useful fields.
6. Saved scenario runlogs.
7. Golden runlog tests.
8. Test inspector / runlog stepper with forward/back controls.
9. Ability to run arbitrary scenarios/content from Console.
10. Editor `Run in Console` button.
11. Live in-editor preview window showing an entity performing its action plan.

Dependencies:

- World/state summary formatting should come before golden runlog tests, because stable summaries are likely part of the runlog format.
- The headless scenario runner should establish the first scenario execution/reporting shape before richer Console or future frontend/editor workflows are planned.
- The scenario report template should stay lightweight and follow evidence from early runner output rather than being designed exhaustively up front.

Promotion trigger:

- Promote follow-up items when the next scenario runner exposes repeated manual-inspection pain, unstable result comparison, or frequent unsupported authoring requests.

### Bucket 2: Direction, movement, and canonical action semantics

Status: High-priority design bucket after initial scenario feedback.

Priority order:

1. Direction/movement philosophy decision.
2. Explicit failure/turn-consumption policy for exhausted behavior chains.
3. Consistent blocker/`Target` writing rules for failed directional steps, including whether `DropFacing` should write blockers.
4. Direction transform concepts: `ReverseFacing`, `SetFacing`, `MoveOppositeFacing`, and perpendicular-direction helpers.
5. `SeekTarget` / move toward target.
6. `TeleportTo`, likely requiring a new `TargetLocation`/destination state slot rather than overloading entity `Target`.
7. Canonical multi-direction effects.
8. `BumpTarget` or generic interaction fallback steps.
9. Player/screen messages if scenarios need action feedback beyond traces.

Dependencies:

- Scenario exercises should provide concrete movement and failure examples before implementation of another movement-heavy primitive batch.
- `TeleportTo` likely depends on a new location/destination state model.

Promotion trigger:

- Promote when generated scenarios repeatedly need behavior that cannot be represented with persistent `Facing`, current `Target`, and existing canonical Action Steps.

Likely decision artifact:

- A short design note before implementing more movement/direction primitives.

### Bucket 3: Inventory, containment, and transfer mechanics

Status: High-value conceptual bucket; defer until scenario evidence clarifies expectations.

Priority order:

1. Weight mechanics simplification: replace carrying capacity as a primary mechanic with a simpler containment rule where an entity may exist inside another entity when contained weight is less than or equal to container weight.
2. Clarify containment/inventory rules through generated scenarios.
3. `Give`: move a carried entity into the inventory space of the `Target` entity.
4. `Take`: move an entity from the inventory space of an adjacent/target entity into the actor inventory.
5. Carried entity selection rules.
6. Source inventory selection rules.

Dependencies:

- Give/take and selection rules depend on the inventory/containment model.
- Weight simplification should wait for clearer inventory/weight expectations from generated scenario tests.

Promotion trigger:

- Promote when scenario exercises need reliable inventory transfer, containment comparisons, or author-facing rules simpler than current carrying capacity semantics.

### Bucket 4: Spawning, creation, and template materialization

Status: Deferred design bucket.

Priority order:

1. Decide template-spawning model.
2. `CreateFacing(templateId)` / `SpawnTemplateFacing`.
3. More specific spawn/projectile/clone steps.
4. Relationship between content templates and runtime entities in generated scenarios.

Dependencies:

- Current placeholder-rock `CreateFacing` is sufficient as a prototype until scenario testing and direction philosophy clarify the next spawning API.
- Template spawning depends on clear content-template/runtime-entity binding semantics.

Promotion trigger:

- Promote when scenario exercises need repeatable spawning of authored templates rather than the current placeholder entity.

### Bucket 5: Scenario/content packaging

Status: Deferred until the scenario runner clarifies boundaries.

Priority order:

1. Scenario documents and scenario families.
2. Scenario metadata.
3. World/setup data.
4. Player/user entity start point.
5. Console scenario loading by path or simple list.
6. Loading one scenario inside another for setpieces or nested levels.
7. Randomly generated levels.

Dependencies:

- The headless generated scenario runner should clarify the boundary between reusable content definitions, scenario setup, runtime world state, and player start metadata.

Promotion trigger:

- Promote when generated temporary scenarios become useful enough that checked-in scenario fixtures or user-selectable scenario documents would reduce repeated setup work.

### Bucket 6: Runtime architecture and simulation scale

Status: Conceptual; avoid premature implementation.

Priority order:

1. Runtime entity indexing for simulation efficiency.
2. Entity ID, plane/world location, and container ownership indexes.
3. Relationship, template, or tag lookups.
4. Scheduler/speed/action budgets.
5. Multiple actions per turn.
6. Per-action-plan cooldowns or other runtime states.
7. Friendly/hostile entity lists and relationship queries if promoted as runtime infrastructure rather than primitive authoring.

Dependencies:

- Runtime indexing should wait for authored scenarios and generated test content to provide clearer performance targets.
- Scheduler/speed must not be used as a shortcut for reaction chains or one-off scenario behavior.

Promotion trigger:

- Promote indexing when scenario simulations reveal measurable lookup/performance bottlenecks.
- Promote scheduler/speed when at least two scenario exercises need variable action budgets or multiple actions per turn as a core mechanic.

### Bucket 7: Behavior authoring reuse and organization

Status: Lower-priority editor/content quality-of-life bucket.

Priority order:

1. Behavior/action-plan templates.
2. Apply-template workflow.
3. Save-as-template workflow.
4. Template editing.
5. Template usage display.

Dependencies:

- Current behavior-chain descriptors are sufficient for engine/editor parity, and templates are not currently required as a foundation for other capabilities.

Promotion trigger:

- Promote when repeated authored scenarios duplicate the same behavior chains often enough that manual chain authoring becomes a clear bottleneck.

### Bucket 8: Future integrated game/editor frontend

Status: Long-horizon strategic direction; current Avalonia GUI remains legacy-priority.

Priority order:

1. Preserve frontend-agnostic editor service and agent/headless API contracts.
2. Identify future frontend/editor requirements from scenario runner and agent workflows.
3. Choose a frontend technology when game rendering/play needs are clearer, such as Godot, Unity, SadConsole, or another suitable option.
4. Expose in-game editor functions through the same underlying editor service/API concepts instead of duplicating YAML/content logic in the frontend.
5. Retire or replace the current Avalonia GUI when the future frontend/editor surface is viable.

Dependencies:

- Depends on stronger headless editor/service APIs and scenario feedback.
- Frontend choice should wait until game rendering/play and in-game editing needs are clearer.

Promotion trigger:

- Promote when headless scenario workflows and agent APIs are stable enough that a frontend can consume them without duplicating content/editor logic.

### Bucket 9: Reactions and cross-entity behavior

Status: Significant future system; keep deferred until simpler action semantics stabilize.

Priority order:

1. Define action-plan slots beyond default/on-turn behavior.
2. Reaction slot model.
3. Root actor/current actor/instigator semantics.
4. Trace causality for reactions.
5. Temporal recursion guards.
6. Bump-triggered interactions.
7. Relationship with scheduler/speed.

Dependencies:

- Persistent entity action state, especially `Facing` and `Target`, should be considered separately from per-invocation action-plan context before reaction slots are implemented.
- Cross-entity reaction chains need explicit actor/instigator semantics, trace causality, and recursion guards.
- This overlaps with future scheduler/speed work, but should not be used as a shortcut for multiple scheduled actions per turn.

Promotion trigger:

- Promote when generated scenarios need target-driven reactions that cannot be modeled as the acting entity's normal behavior chain or simple interaction fallback.

### Bucket 10: Long-horizon diegetic/meta systems

Status: Long-horizon conceptual bucket.

Priority order:

1. Diegetic action-plan entities.
2. Runtime action-plan stacks as inventory-like spaces.
3. Rearranging plans to change runtime behavior.

Dependencies:

- Depends on stable action-plan authoring, inventory/containment semantics, and likely runtime indexing.

Promotion trigger:

- Promote only after core behavior authoring and inventory/containment systems are stable enough that action plans can be treated as gameplay objects without destabilizing foundational semantics.

## Recently completed / archived context

### Canonical behavior-chain usability and first utility batch

Status: Completed / archived.

Archived supporting documents:

- [Behavior System Next Steps](../Archived/Behavior-System-Next-Steps.md)
- [Behavior Model Consolidation First Slice](../Archived/Behavior-Model-Consolidation-First-Slice.md)
- [Behavior Primitive Action Plans](../Archived/Behavior-Primitive-Action-Plans.md)
- [Behavior Primitive/Fallback Foundation Archive](../Archived/Behavior-Primitive-Fallback-Foundation.md)

Completed scope summary:

- Made the canonical behavior-chain GUI visually primary.
- Hid legacy low-level behavior authoring except when editing existing legacy low-level plans.
- Created new GUI action plans as empty/passive instead of seeding legacy wait steps.
- Added compact Core behavior-chain trace formatting.
- Added canonical plan preview through editor service and agent API.
- Added canonical utility Action Steps: `DropFacing`, `PushFacing`, `DestroyTarget`, and `CreateFacing`.
- Ran a generated-content exercise with the content-editor agent and captured design gaps.
