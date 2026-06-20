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

Alpha MVP is complete: the game can launch and be played in an authored scenario, and a player entity can be inserted into scenarios through persisted scenario definitions and reusable materialization. Post-alpha planning should use this scenario/Console feedback loop to choose beta work based on playable evidence rather than broad speculative mechanics. Direction, inventory, spawning, scheduler, and reaction decisions should continue to be informed by scenario evidence.

The current Avalonia GUI is legacy-priority / maintenance-mode. New authoring and scenario-feedback work should prioritize editor services, agent/headless APIs, tests, and future frontend readiness rather than maintaining broad Avalonia GUI parity. Long-term human-facing editor work is expected to move toward an integrated game/editor frontend.

## Alpha MVP: playable arbitrary scenarios

Status: Complete as of Sprint 11. The alpha path is represented end-to-end in tests: persisted scenario definition -> validation/materialization -> player insertion -> Console-launchable session -> player action.

Target statement:

- The game can launch and be played in an arbitrary authored scenario.
- A player entity is insertable into scenarios.

This target promoted the following items out of backlog buckets and into the completed alpha roadmap. New mechanics such as richer movement, `Give`/`Take`, template spawning, reactions, scheduler/speed, and future frontend replacement remain valuable for content richness, but were not upstream requirements for the first alpha launch/play loop.

### Alpha roadmap completion

1. **Scenario document / scenario package model** - Complete
   - Define a minimal explicit scenario definition that references normal content templates rather than creating a separate gameplay language or relying on magic template/entity names.
   - Required fields should include scenario identity/metadata, scenario-root entity/template or scenario space reference, player template/entity IDs, and player start placement.
   - Keep scenario setup compatible with the Sprint 10 scenario-root inventory model where possible.
   - A content package may eventually contain multiple scenarios; the alpha model should not require exactly one hardcoded `Game` and exactly one hardcoded `Player` entity, even if built-in prototype content keeps those names.

2. **Player insertion contract** - Complete
   - Define how a player entity template and runtime player entity ID are selected or overridden for a scenario.
   - Define how the player is inserted into the scenario-root inventory/play space: location, inventory plane behavior, initial action state, and conflict diagnostics when the start cell is occupied or invalid.
   - Preserve the existing direct player-input model initially; do not require AI/default-plan behavior or a player-controlled Action Step for alpha.
   - Treat `PlayerInputStep` / action-choice discovery as a future player-control model, not an alpha prerequisite.

3. **Scenario materialization service** - Complete
   - Promote reusable scenario materialization out of `AgentContentEditorApi.RunScenario` into a service usable by tests, editor/agent APIs, and Console without duplicating spawn/setup logic.
   - Materialize a scenario into `WorldState`, action-plan map, registry/presentation lookup, player entity ID, active play plane/container, and validation diagnostics.
   - Keep generated/headless scenario reports as a validation surface for the same materialization path.
   - Console should consume materialization results rather than hardcoded prototype IDs such as `PrototypeContent.PlayerId` or `PrototypeContent.GameInventoryPlaneId`.

4. **Editor/agent authoring and validation support** - Complete
   - Provide editor/agent operations to create, inspect, validate, and run/preview alpha scenario definitions.
   - Validate missing scenario roots, invalid player starts, missing player template/presentation, duplicate/occupied starts, and unsupported scenario requests with actionable diagnostics.
   - Keep scenario diagnostics categorized enough for agents to distinguish authoring/validation issues, unsupported capability gaps, expected runtime observations, and runtime engine errors.
   - Continue avoiding checked-in prototype-content edits unless an explicit alpha fixture is selected.

5. **Agent-friendly scenario report surface** - Complete for alpha MVP
   - Provide a concise text report surface for scenario runs, alongside structured data, so content-authoring agents can review setup, turns, observations, diagnostics, and final state without writing custom formatting code.
   - Include high-signal turn-by-turn state changes where practical, such as movement, facing/target changes, created/destroyed entities, and inventory/containment changes.
   - Keep the report shape lightweight until alpha scenarios reveal which sections are stable enough for future runlogs or golden comparisons.

6. **Console arbitrary scenario launch** - Complete
   - Let Console launch a selected scenario by path or simple scenario list instead of always using `PrototypeContent.CreateFirstSlice`.
   - Replace hard-coded prototype entity IDs in play/render/inspect flows with scenario materialization outputs, especially player entity ID and active play plane/container.
   - Keep controls minimal: movement, pickup/drop/inspect, turn advancement, and quit are enough for alpha.

7. **Alpha scenario fixtures and smoke validation** - Complete
   - Add one or more small authored alpha scenarios only after the scenario package model is stable enough to avoid churn.
   - Add tests that load/materialize an alpha scenario, insert the player, run at least one player action/turn, and verify built-in content/scenario validation.
   - Console smoke coverage is desirable but should remain focused on launch/play integration rather than re-testing Core movement.

### Not required for first alpha, unless selected scenario content demands it

- `ReverseFacing`, `TurnLeft`, `TurnRight`, `Backstep`, `SeekTarget`, `AcquireNearestTarget`, `Give`, and `Take`.
- `CreateFacing(templateId)` or other template-spawning mechanics.
- Scheduler/speed/multiple actions per turn.
- Saved runlogs/golden runlog tests.
- Future integrated frontend replacement.
- Avalonia GUI parity for scenario authoring.

## Active / likely next sprint

### Beta release target: gameplay demo vignettes

Status: Selected direction after Sprint 11 alpha MVP completion; sprint-sized implementation slices still pending selection.

Recently completed supporting documents:

- [Sprint 10: Scenario Feedback Loop](../Archived/Sprint-10-Scenario-Feedback-Loop.md)
- [Sprint 11: Alpha Scenario Materialization](../Archived/Sprint-11-Alpha-Scenario-Materialization.md)

Active beta planning document:

- [Beta Content Exploration Plan](Beta-Content-Exploration-Plan.md)

Active sprint plan:

- [Sprint 12: Beta Primitive Showcases](Sprint-12-Beta-Primitive-Showcases.md)

Beta target statement:

- The project can present several small authored gameplay vignettes, like pitch-deck slides, that demonstrate what the engine naturally makes possible.
- Each vignette should be playable in Console, runnable headlessly for validation, and useful for deciding which interactions are interesting or engaging.
- Beta should build enough primitives, fixtures, and scenario reports to inform the eventual unified frontend/player-interaction model.

Long-term frontend direction:

- A unified frontend remains desired eventually, potentially through SadConsole, Godot, Unity, Pico-8, or another frontend stack.
- That frontend should support title/menu flow, content loading, play, and eventually content editing.
- Do not start major frontend replacement work until beta vignettes reveal which player interactions, scenario transitions, and content-authoring workflows are worth optimizing.

Current decision point: use the completed alpha MVP loop to choose the first beta vignette batch and the minimum primitives/tooling required to make those vignettes readable, playable, and useful for playtest feedback.

Current beta content-exploration order:

1. Explore and test scenarios that are authorable with current tools.
2. Gate 1: direction transform batch (`ReverseFacing`, `TurnLeft`, `TurnRight`, `Backstep`), then explore the scenarios unlocked by relative facing changes.
3. Gate 2: `AcquireNearestTarget` + `SeekTarget`, then explore direct chase, collector, follower, and targeted interaction scenarios.
4. Gate 3: target-distance / directional choice primitives, then explore fleeing, keep-away, kiting, and patterned pursuit.
5. Gate 4: `Give` / `Take`, then explore passive containers, trade, stealing, feeding/offering, and transfer restrictions.
6. Gate 5: template spawning, then explore authored spawners, projectiles, traps/bombs, builders, and clone/summon prototypes.
7. Gate 6: reaction system, then explore traps, doors/buttons/pressure plates, chain reactions, contact combat, and environmental puzzle systems.

Beta content exploration goals:

1. **Movement-pattern vignettes**
   - Direct chase: an entity pursues the player by moving toward them.
   - Keep-away: an entity tries to remain at or near a chosen distance from the player.
   - Fleeing: an entity runs away from the player.
   - Pattern-constrained pursuit: an entity chases the player but can only move diagonally or by another constrained movement pattern.
   - Expected capability pressure: likely needs `AcquireNearestTarget`, `SeekTarget`, target-distance evaluation, and eventually patterned movement variants. Start with the smallest deterministic target-selection/movement semantics that can prove the vignette.

2. **In/out, containment, and transfer vignettes**
   - Enter/exit: an entity can be entered by moving into it, and exited by moving off the edge of its inventory/play space.
   - Trade/passive container: an entity can be freely traded with, like a passive chest.
   - Restricted transfer: another entity prevents give/take interactions or otherwise blocks transfer.
   - Expected capability pressure: likely needs clearer containment/plane-transition semantics, `Give`/`Take`, source/target carried-entity selection rules, and author-facing diagnostics for denied transfer. Treat enter/exit as potentially deeper than ordinary pickup/drop because it changes the active play space.

3. **Gameplay scenario suites**
   - Combat vignettes: adjacent attacks, ranged attacks, throwing inventory, and alternative health/damage models.
   - Puzzle vignettes: block pushing, trap avoidance, interactive puzzle elements, and chain reactions.
   - Expected capability pressure: combat should explicitly compare possible health models before implementation, such as HP as an entity property, HP as an inventory item/entity, or HP/status bestowed by an action plan/state model. Puzzle vignettes should distinguish content-only compositions of existing primitives from new engine capabilities such as reactions, trigger volumes, or chain-reaction scheduling.

For each candidate vignette, record before implementation whether the intended experiment is:

- **content-only** with existing capabilities;
- **new Action Step / primitive** using existing engine state models;
- **new engine capability** requiring new state, containment, reaction, scheduler, or frontend/player-interaction semantics.

Likely candidate focuses:

- Current-tool beta vignettes: primitive showcase scenarios for `PushFacing`, `DestroyTarget`, `DropFacing`, and `CreateFacing`; pickup/drop/weight puzzles; blocker/target fallback-chain exercises; and a first curated actor zoo.
- Beta vignette design: define several small demo scenarios that probe different kinds of gameplay, such as movement puzzles, blocker/target interaction, pickup/drop containment, autonomous actors, creation/destruction, and peer transfer once supported.
- Scenario report and run workflow polish: text report/template for agents, richer inventory/containment state summaries, local turn-order/previous-action tables, compact per-turn state diffs, created/destroyed entity summaries, capability-gap sections, preview-plus-simulation in one command, actor-zoo/isolation report templates, and cleanup/replacement of the older test-local runner. Deferred tactical telegraphing should project each actor's next resolved behavior/fallback on a safe simulation snapshot; pull it forward when complex gameplay scenarios require tactical information beyond previous actions.
- Foundational movement/peer-interaction primitives: `ReverseFacing`, `TurnLeft`, `TurnRight`, `Backstep`, then `SeekTarget`/`AcquireNearestTarget` and `Give`/`Take` when vignettes demonstrate need.
- Scenario/content package ergonomics: multiple fixture scenarios, scenario listing/selection, authoring helpers, and stronger validation/reporting around packages.
- Capability-gap logging: record intentionally blocked or negative vignettes and promote feature requests when repeated scenario pressure or one high-value flagship scenario justifies it.
- Frontend/editor loop follow-up: keep future unified frontend requirements visible, but defer implementation until beta vignette playtests clarify interaction and authoring needs.

Selection guidance:

- Prefer designing the first beta vignette set before adding broad primitives, so mechanics are pulled by demo needs.
- Prefer scenario/report polish first if content-authoring agents still need manual test harnesses or cannot quickly interpret vignette behavior.
- Prefer movement/peer-interaction primitives if beta candidate vignettes need behavior not representable with persistent `Facing`, current `Target`, and existing canonical Action Steps.
- Prefer scenario/content package ergonomics if manually launching, selecting, or comparing vignettes becomes the immediate beta bottleneck.
- Prefer inventory/containment work only after scenario exercises expose concrete transfer/containment needs.

## Prioritized backlog buckets

### Bucket 1: Scenario/testing/tooling feedback loop

Status: Highest-priority backlog bucket.

Priority order:

1. Compact world/state summary formatter for entity positions, facing, target, inventories/containment, created/destroyed entities, and changed state per turn.
2. Lightweight scenario report template once first runner output reveals the useful fields.
3. Capability-gap log/report section for unsupported authoring/simulation requests and intentionally blocked negative vignettes.
4. Plan preview + simulation in one API command.
5. Primitive showcase report support for demonstrating one Action Step's setup, success, failure/fallback, state reads/writes, and trace output.
6. Curated actor-zoo report template for one-room behavior demonstrations.
7. Automated actor isolation preview: generate a small room around an arbitrary entity template, run a fixed number of turns, and report behavior.
8. Cleanup/replacement path for the older test-local `MinimalScenarioRunner` now that `AgentContentEditorApi.RunScenario` exists.
9. Headless run command / scriptable entry point for running scenarios without writing tests or embedding C#.
10. Generalized scenario runner upgrade sprint.
11. Saved scenario runlogs.
12. Golden runlog tests.
13. Test inspector / runlog stepper with forward/back controls.
14. Editor `Run in Console` button after Console scenario launch exists.
15. Live in-editor preview window showing an entity performing its action plan.

Completed baseline:

- Sprint 10 added `AgentContentEditorApi.RunScenario`, scenario-root entity templates, inventory-plane scenario spaces, deterministic row-major contained-actor initiative, rich canonical behavior-chain traces, and observational runtime outcome reporting.
- Sprint 11 completed the alpha MVP scenario path: persisted `scenarios`, reusable scenario materialization, player insertion diagnostics, agent/editor scenario authoring/materialization, Console scenario launch by content path and scenario ID, and embedded alpha smoke coverage.

Future generalized scenario runner wishlist:

- Promote the current test-local runner/report shape into reusable test/editor-agent helpers rather than duplicating C# setup across scenario tests.
- Add a small typed scenario setup model for planes, runtime entity placements, watched entities, turn count, expected diagnostics, and capability-gap notes without becoming a separate content language.
- Support editor/API-authored temporary content end-to-end: templates, carried inventory, initial action state, default plan assignment, behavior-chain authoring, validation, runtime spawn, simulation, and report generation.
- Provide richer compact state summaries for positions, facing, target, inventories/containment, created/destroyed entities, and changed state per turn.
- Add stable report sections suitable for saved runlogs and eventual golden comparisons once the format stops changing.
- Allow scenario reports to include plan preview, validation diagnostics, simulation trace, state diff, and capability gaps in one result.
- Support primitive showcase and actor-zoo workflows once explicit authored scenarios reveal which setup variants are broadly useful.
- Keep Console/frontend playability as a later promotion step, after headless reports prove which scenario fields are useful.

Dependencies:

- World/state summary formatting should come before golden runlog tests, because stable summaries are likely part of the runlog format.
- The headless scenario runner should establish the first scenario execution/reporting shape before richer Console or future frontend/editor workflows are planned.
- The scenario report template should stay lightweight and follow evidence from early runner output rather than being designed exhaustively up front.

Promotion trigger:

- Promote follow-up items when the next scenario runner exposes repeated manual-inspection pain, unstable result comparison, or frequent unsupported authoring requests.

### Bucket 2: Foundational movement and peer interaction primitives

Status: High-priority content-foundation bucket after initial scenario feedback.

Direction decision: keep `Facing` as the first primitive orientation model for local movement. Prefer small canonical Action Steps that transform or move relative to `Facing` before adding broad absolute-direction movement. Add goal-directed target movement after relative direction transforms are in place. Prioritize peer inventory transfer soon after movement basics so foundational content can experiment with entity-to-entity interactions.

Priority order:

1. `ReverseFacing`: reverse persistent actor `Facing` without moving.
2. `TurnLeft`: rotate persistent actor `Facing` 90 degrees counter-clockwise without moving.
3. `TurnRight`: rotate persistent actor `Facing` 90 degrees clockwise without moving.
4. `Backstep`: move one cell opposite persistent actor `Facing` without changing `Facing`.
5. `SeekTarget` / move toward target: choose a deterministic step toward persistent `Target` after target movement semantics are concrete.
6. `AcquireNearestTarget`: select a nearby valid entity and write persistent `Target`, initially with simple deterministic same-plane rules.
7. `Give`: move a carried entity into the inventory space of the persistent `Target` entity.
8. `Take`: move an entity from the inventory space of an adjacent/target entity into the actor inventory.
9. Explicit failure/turn-consumption policy for exhausted behavior chains, if scenario reports reveal author confusion beyond observational reporting.
10. Consistent blocker/`Target` writing rules for failed directional steps, including whether `DropFacing` and `Backstep` should write blockers.
11. Patterned target movement such as rook/bishop/knight-like pursuit after basic `SeekTarget` is proven.
12. Wall-following helpers or sensory/conditional primitives after relative direction transforms and scenario reports show the useful abstraction level.
13. `TeleportTo`, likely requiring a new `TargetLocation`/destination state slot rather than overloading entity `Target`.
14. `BumpTarget` or generic interaction fallback steps.
15. Player/screen messages if scenarios need action feedback beyond traces.

Dependencies:

- Scenario exercises should provide concrete movement and failure examples for each primitive batch.
- `ReverseFacing`, `TurnLeft`, `TurnRight`, and `Backstep` should come before `SeekTarget` so relative movement/facing semantics are established.
- `SeekTarget` should come before patterned rook/bishop/knight-like movement so target pursuit tie-breaking and blocker behavior are proven first.
- `Give`/`Take` depend on the inventory/containment model enough to require explicit scenario coverage, but they are now considered foundational peer-interaction primitives rather than distant conceptual work.
- `TeleportTo` likely depends on a new location/destination state model.

Promotion trigger:

- Promote the first direction-transform batch (`ReverseFacing`, `TurnLeft`, `TurnRight`, `Backstep`) when Sprint 11 or later selects movement primitive expansion over scenario report polish.
- Promote `SeekTarget` / `AcquireNearestTarget` when generated scenarios need autonomous aggressive/chasing behavior.
- Promote `Give`/`Take` when generated scenarios need peer inventory transfer between adjacent or targeted entities.

Likely decision artifact:

- A short design note before implementing each primitive batch, covering state reads/writes, turn consumption, blocker/`Target` behavior, editor/API support, and scenario coverage.

### Bucket 3: Inventory, containment, and transfer mechanics

Status: High-value conceptual bucket; defer until scenario evidence clarifies expectations.

Priority order:

1. Weight mechanics simplification: replace carrying capacity as a primary mechanic with a simpler containment rule where an entity may exist inside another entity when contained weight is less than or equal to container weight.
2. Clarify containment/inventory rules through generated scenarios.
3. Carried entity selection rules for `Give`.
4. Source inventory selection rules for `Take`.
5. Richer containment/inventory report summaries for generated scenarios.

Dependencies:

- `Give`/`Take` are promoted into Bucket 2 as foundational peer-interaction primitives; this bucket retains the deeper inventory/containment model and selection-rule follow-up work.
- Give/take selection rules depend on the inventory/containment model.
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

### Bucket 5: Scenario/content packaging beyond alpha

Status: Alpha-critical subset promoted into the alpha release roadmap. This bucket tracks follow-up packaging capabilities beyond the first playable arbitrary-scenario release.

Priority order:

1. Beta content file organization: introduce folders or multiple content documents when fixture count makes single-file navigation, scenario selection, or validation noisy.
2. Scenario families and grouping once individual alpha scenario documents work.
3. Richer scenario metadata beyond alpha launch needs.
4. Richer world/setup data beyond scenario-root inventory spaces.
5. Loading one scenario inside another for setpieces or nested levels.
6. Randomly generated levels.

Dependencies:

- Alpha scenario documents, player start metadata, and Console scenario loading are now tracked in the alpha roadmap.
- The alpha scenario materialization path should clarify the boundary between reusable content definitions, scenario setup, runtime world state, and player start metadata before richer packaging is promoted.
- Content-file organization is owned by the content editor/content-authoring role and should be shaped by actual beta fixture growth rather than preemptive structure.

Promotion trigger:

- Promote beyond-alpha packaging when alpha scenario fixtures become repetitive enough that scenario families, nested scenarios, or richer metadata would reduce content-authoring friction.
- Promote content-folder/file reorganization when beta vignettes become hard to browse, validate, select, or compare in their current layout.

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

- Promote when alpha scenario materialization, headless scenario workflows, and agent APIs are stable enough that a frontend can consume them without duplicating content/editor logic.

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

### Bucket 10: Future player control and action choice model

Status: Deferred until alpha scenario launch/play works with direct Console control.

Concept:

- A future `PlayerInputStep` could be assignable to an entity Action Plan.
- When simulation reaches `PlayerInputStep`, the engine/frontend would pause for player input rather than automatically resolving the chain.
- Subsequent Action Steps in the plan could describe available player choices, such as move, pickup, drop, or interact, instead of behaving as ordinary fallback attempts.
- This would allow any entity to become player-controlled through authored behavior rather than through a special hardcoded player entity.

Dependencies:

- Requires alpha scenario materialization/player insertion to exist first so direct-control play has a stable baseline.
- Requires action-choice discovery, frontend/Console input integration, and likely revised action-plan resolution semantics.
- Should be designed with future integrated frontend needs in mind, not just the current Console.

Promotion trigger:

- Promote after alpha launch/play works and scenarios need authored player capability sets or controllable non-player entities.

### Bucket 11: Long-horizon diegetic/meta systems

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

### Sprint 10: Scenario feedback loop

Status: Completed / archived.

Archived supporting document:

- [Sprint 10: Scenario Feedback Loop](../Archived/Sprint-10-Scenario-Feedback-Loop.md)

Completed scope summary:

- Added `AgentContentEditorApi.RunScenario` as the first production/editor-agent scenario runner surface.
- Made editor-authored scenario-root entity templates usable as scenario spaces through their inventory planes.
- Scheduled all contained default-plan actors using deterministic row-major initiative for scenario runs.
- Returned structured setup, actor order, rich behavior-chain turn traces, final state, validation diagnostics, runtime observations, runtime failures, and capability gaps.
- Established that expected in-simulation inability to act is an observation, not a failed scenario result.
- Ran a content-editor scenario exercise and captured follow-up friction around report polish, initiative documentation, and future action semantics.

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
