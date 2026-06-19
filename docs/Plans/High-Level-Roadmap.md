# High-Level Roadmap

This roadmap tracks upcoming engine/editor parity work at a planning level. Completed sprint planning documents are archived under `docs/Archived/`; the current capabilities source of truth remains `docs/Source of Truth/Engine-Editor-Capabilities.md`.

## Planned

### Next sprint: generated scenario authoring and simulation feedback

Status: Likely next sprint focus.

Supporting document:

- [Next Sprint Scenario Testing Plan](Next-Sprint-Scenario-Testing-Plan.md)

Goal: improve our ability to create temporary/generated content, simulate turns, inspect behavior traces, and evaluate whether newly added Action Steps support desired user experiences.

Why this is the current priority:

- The first utility Action Step batch is implemented, but generated exercises showed that we cannot test authored gameplay scenarios as comfortably or repeatedly as needed.
- Direction/movement philosophy decisions should be informed by more scenario evidence instead of adding many directional variants immediately.
- Existing preview and trace formatting are useful foundations, but they are not yet a full scenario exercise workflow.

Loose high-level scope:

- Add a headless scenario exercise helper around editor/content services and Core simulation.
- Support creating temporary content, assigning canonical behavior chains, spawning a small world, running turns, and collecting compact trace summaries.
- Prefer tests or generated temporary files over checked-in prototype content changes.
- Preserve Core/Editor parity: any scenario authoring helper should use supported editor service / agent API operations where possible.

Potential testable outcomes:

- A generated scenario can author entities, inventories, action plans, and initial action state without editing checked-in content.
- A generated scenario can simulate multiple turns and return formatted behavior-chain traces.
- The first utility Action Step batch can be exercised in generated scenarios without manual YAML inspection.
- Unsupported requests produce explicit capability-gap notes rather than silent YAML or simulation guesses.

### Direction and movement philosophy decision

Status: Design decision required before the next movement-heavy primitive batch.

Open design questions surfaced by the sprint exercise:

- Should canonical Action Steps always read persistent `Facing`, or should some support explicit direction parameters?
- Should direction transforms be separate Action Steps, e.g. `ReverseFacing`, `SetFacing`, `MoveOppositeFacing`, or a more general direction-expression system?
- Should failed directional steps write the blocking entity to `Target` consistently, including `DropFacing`?
- Should all-failed behavior chains always consume the turn, or should no-fallback failure policies be authorable/visible?
- How should template spawning fit into canonical steps: `CreateFacing(templateId)`, named spawn descriptors, clone descriptors, or separate specialized steps?

Likely decision artifact:

- a short design note before implementing more movement/direction primitives.

Planning note: this is intentionally upstream of creating new movement-heavy Action Steps in earnest.

### Completed this sprint: canonical behavior-chain usability and first utility batch

Status: Completed / archived.

Archived supporting documents:

- [Behavior System Next Steps](../Archived/Behavior-System-Next-Steps.md)
- [Behavior Model Consolidation First Slice](../Archived/Behavior-Model-Consolidation-First-Slice.md)
- [Behavior Primitive Action Plans](../Archived/Behavior-Primitive-Action-Plans.md)
- [Behavior Primitive/Fallback Foundation Archive](../Archived/Behavior-Primitive-Fallback-Foundation.md)

Completed scope:

- Made the canonical behavior-chain GUI visually primary.
- Hid legacy low-level behavior authoring except when editing existing legacy low-level plans.
- Created new GUI action plans as empty/passive instead of seeding legacy wait steps.
- Added compact Core behavior-chain trace formatting.
- Added canonical plan preview through editor service and agent API.
- Added canonical utility Action Steps: `DropFacing`, `PushFacing`, `DestroyTarget`, and `CreateFacing`.
- Ran a generated-content exercise with the content-editor agent and captured design gaps.

## Conceptualized, not yet planned

### Weight mechanics simplification

Status: Conceptualized, not yet planned.

Concept: remove carrying capacity as a primary mechanic and replace it with a simpler containment rule where an entity may exist inside another entity when the contained entity's weight is less than or equal to the container entity's weight. This likely treats weight more like bulk or volume than physical mass.

Planning deferred until generated scenario tests provide clearer inventory/weight expectations.

### Runtime entity indexing and simulation efficiency

Status: Conceptualized, not yet planned.

Concept: add runtime indexes so entity interactions can resolve more quickly while many entities are simulated. Likely indexes include entity ID, plane/world location, container ownership, and eventually relationship or template/tag lookups.

Planning deferred until authored scenarios and generated test content provide clearer performance targets.

### Diegetic action-plan entities

Status: Conceptualized, not yet planned.

Concept: represent action plans diegetically as entities during gameplay. Each entity would have an action-plan stack that can be inspected as its own inventory-like space, and rearranging plans would change runtime behavior.

Planning deferred because this depends on stable action-plan authoring, inventory/containment semantics, and likely runtime indexing.

### Deferred action primitives and runtime states

Status: Conceptualized, not yet planned.

Concept: add additional primitives and state needed for basic gameplay scenarios.

Deferred brainstorm Action Steps from the current utility review:

- `TeleportTo`: move to an arbitrary destination, likely requiring a new `TargetLocation`/destination state slot rather than overloading entity `Target`.
- `Give`: move a carried entity into the inventory space of the `Target` entity.
- `Take`: move an entity from the inventory space of an adjacent/target entity into the actor's inventory.
- `Wait`: explicit consumed no-op turn as a canonical Action Step.
- `ReverseFacing`, `SetFacing`, `MoveOppositeFacing`, and perpendicular-direction helpers.
- `BumpTarget` or other interaction fallback steps.
- `SeekTarget` / move toward target.
- `CreateFacing(templateId)` / `SpawnTemplateFacing` and more specific spawn/projectile/clone steps.
- Player/screen messages.
- Per-action-plan cooldowns or other runtime states.
- Friendly/hostile entity lists and relationship queries.

Generated content exercises have also surfaced possible future needs for multiple actions per turn, canonical multi-direction effects, and explicit behavior-chain failure turn-consumption policies. These remain conceptualized, not yet planned.

Template/entity spawning is explicitly deferred. The current placeholder-rock `CreateFacing` prototype is sufficient as a foundation until scenario testing and direction philosophy clarify the next spawning API.

### Preview and simulation inspection workflows

Status: Conceptualized, not yet planned beyond the next-sprint generated scenario testing focus.

Concept: improve the ability for editors, players, and agents to preview authored behavior before committing to content or starting a full play session.

Brainstormed workflows:

- live in-editor preview window showing an entity performing its action plan;
- ability to run arbitrary scenarios/content from Console;
- `Run in Console` button from the editor that launches a game window with loaded content;
- saved test scenario runlogs that capture simulation state each turn;
- test inspector tool for stepping through simulation/runlogs with forward/back controls.

Planning deferred until the generated scenario helper clarifies what simulation state, trace summaries, and content/runtime bindings are most useful.

### Scenario documents and scenario families

Status: Conceptualized, not yet planned.

Concept: treat each YAML file, or a family of YAML files, as an independently loadable game/scenario. Each scenario should eventually be able to define metadata, content definitions, world/setup data, and a user/player entity start point. Console could initially load a scenario by path or from a simple list, with possible later diegetic scenario selection.

Future extensions may include loading one scenario inside another for setpieces or nested levels, and eventually randomly generated levels.

Planning deferred until the generated scenario runner clarifies the boundary between reusable content definitions, scenario setup, runtime world state, and player start metadata.

### DevOps/tooling backlog

Status: Conceptualized, not yet planned.

Concept: improve developer and agent feedback loops for authored scenarios and behavior-system changes.

Deferred tooling ideas:

- Compact world/state summary formatter: produce concise summaries of entity positions, facing, target, inventories, and other high-signal runtime state after each simulated turn or action.
- Golden runlog tests: store expected scenario runlogs as fixtures and compare them in tests after the runlog format stabilizes.

Dependency note: the world/state summary formatter should come before golden runlog tests, because stable summaries are likely part of the runlog format.

Planning deferred until the next-sprint headless scenario runner establishes the first scenario execution/reporting shape.

### Editor usability backlog

Status: Conceptualized, not yet planned.

Concept: make editor/API feedback clearer for authors and content-editing agents as behavior authoring grows.

Deferred usability ideas:

- Plan preview + simulation in one API command: combine canonical plan preview, one or more simulated turns, trace summaries, and state diffs into one inspectable result.
- Capability-gap reporter: when requested behavior cannot be authored with current engine/editor capabilities, return structured gaps such as missing Action Step, missing direction override, missing template spawn, or missing state slot.

Planning deferred until generated scenario exercises reveal the most common authoring failures and preview/simulation report needs.

### Conceptual backlog sorting and prioritization

Status: Needed soon, not yet planned as an implementation slice.

Concept: review the growing conceptualized backlog, group related ideas, identify dependencies, and choose which items should become planned work versus remain deferred.

### Scheduler/speed and multiple actions per turn

Status: Conceptualized, not yet planned.

Concept: support entities taking more than one action per turn, variable speed, initiative, or action budgets. Rat two-actions-per-turn remains classified here after generated-content exercises.

### Behavior/action-plan templates

Status: Conceptualized, not yet planned.

Concept: add quality-of-life workflows for reusable behavior templates, including apply-template, save-as-template, template editing, and template usage display.

Planning deferred because current behavior-chain descriptors are sufficient for engine/editor parity, and templates are not currently required as a foundation for other capabilities.

### Reaction action-plan slots and bump-triggered interactions

Status: Conceptualized, not yet planned.

Concept: entities may eventually expose action-plan slots beyond their normal turn behavior. The current/default turn plan should remain compatible with a future `onTurn` slot, while reaction slots may be invoked during another entity's root turn. The important future capability is that a successful interaction can trigger the target entity to run a reaction action plan; the exact trigger, including whether it is bump-specific, remains intentionally unsettled.

Planning notes:

- This depends on behavior-chain consolidation but is not required for the current canonical chain work.
- Persistent entity action state, especially `Facing` and `Target`, should be considered separately from per-invocation action-plan context before reaction slots are implemented.
- Cross-entity reaction chains will need explicit root actor/current actor/instigator semantics, trace causality, and temporal recursion guards.
- This may overlap with future scheduler/speed work, but it should not be used as a shortcut for multiple scheduled actions per turn.
- This may overlap with future action primitives and runtime states, especially any eventual reaction-trigger or interaction-target primitive.
