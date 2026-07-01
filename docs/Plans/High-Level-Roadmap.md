# High-Level Roadmap

Status: Active roadmap.

Read when:

- selecting or refining sprint work;
- sorting conceptual ideas into priority buckets;
- deciding whether a design gap should become planned work or remain deferred.

Related source of truth:

- `docs/Source of Truth/invariants.md` records stable behavior contracts and test traces for TDD planning.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` describes maintainer-facing implemented support tiers and layer coverage.
- `docs/Source of Truth/Content-Authoring-Manual.md` describes what content authors and content-editing agents can safely author today.
- `docs/Source of Truth/planning-index.md` describes planning-document navigation and reading order.

## Current strategic direction

Alpha MVP is complete: the game can launch and be played in an authored scenario, and a player entity can be inserted into scenarios through persisted scenario definitions and reusable materialization. Beta produced several authored gameplay vignettes, scenario reports/recordings, transfer showcases, and a Console scenario catalog. Gamma now shifts from adding mechanics to preparing the existing scenarios for real tester feedback. Direction, inventory, spawning, scheduler, reaction, and frontend decisions should continue to be informed by scenario and tester evidence rather than broad speculative mechanics.

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

### Gamma release target: tester-shareable frontend demo

Status: Selected after Sprint 21 Console scenario catalog work. Beta mechanics expansion is paused while current authored scenarios are prepared for external feedback.

Recently completed supporting documents:

- [Sprint 10: Scenario Feedback Loop](../Archived/Sprint-10-Scenario-Feedback-Loop.md)
- [Sprint 11: Alpha Scenario Materialization](../Archived/Sprint-11-Alpha-Scenario-Materialization.md)
- [Sprint 12: Beta Primitive Showcases](../Archived/Sprint-12-Beta-Primitive-Showcases.md)
- [Sprint 13: Gate 1 Direction Showcases](../Archived/Sprint-13-Gate-1-Direction-Showcases.md)
- [Sprint 14: Gate 2 Targeting Showcases](../Archived/Sprint-14-Gate-2-Targeting-Showcases.md)
- [Sprint 15: Debug Scenario Recorder](../Archived/Sprint-15-Debug-Scenario-Recorder.md)
- [Sprint 16: Gate 3 Distance Movement Showcases](../Archived/Sprint-16-Gate-3-Distance-Movement.md)
- [Sprint 17: Scenario Tooling Decoupling](../Archived/Sprint-17-Scenario-Tooling-Decoupling.md)
- [Sprint 18: Tech Debt Cleanup](../Archived/Sprint-18-Tech-Debt-Cleanup.md)
- [Sprint 19: Gate 4 Peer Transfer Showcases](../Archived/Sprint-19-Gate-4-Peer-Transfer.md)
- [Sprint 20: Scenario Run and Report Polish](../Archived/Sprint-20-Scenario-Run-Report-Polish.md)
- [Sprint 21: Console Scenario Catalog](../Archived/Sprint-21-Console-Scenario-Catalog.md)
- [Sprint 22: Gamma Containment Path Service](../Archived/Sprint-22-Gamma-Containment-Path-Service.md)
- [Beta Content Exploration Plan](../Archived/Beta-Content-Exploration-Plan.md)

Active frontend planning documents:

- [Gamma Frontend Demo Plan](Gamma-Frontend-Demo-Plan.md)
- [SadConsole Frontend Roadmap](SadConsole-Frontend-Roadmap.md)

Immediate frontend priority:

- Carry the completed SadConsole spike findings into a frontend architecture plan before more frontend feature work.
- Use the SadConsole frontend roadmap to pave shared session/action/target/log/entity-panel contracts before handing rich UI work to frontend-owner.
- Prioritize shared controlled-action, valid-target, scenario/session-launch, and turn-log projection services so future Console, SadConsole, Godot, or editor-facing surfaces consume the same capabilities.
- Keep Console as the supported minimal demo surface until a replacement frontend is selected.

Planned next sprint:

- Begin `docs/Plans/SadConsole-Frontend-Roadmap.md` Stage 0, then Stage 1: frontend UX source-of-truth cleanup, debt inventory, and shared playable-session launch extraction. Console breadcrumb display is deferred/subsumed by the entity-panel projection and SadConsole debug-browser path unless explicitly re-selected as fallback polish.

Gamma/frontend target statement:

- The project can be shared with test players through a scenario-list-driven frontend/debug browser path.
- Testers can choose curated scenarios, understand what they are looking at, and give useful feedback without reading development docs.
- SadConsole is now the preferred canonical debug/editor browser direction; Console remains fallback/minimal tooling.
- Short-term work should preserve shared UI-agnostic query/catalog/session/action/log contracts that Console, SadConsole, or a later frontend engine can consume.

Gamma/frontend promoted stages:

1. Completed in Sprint 22: read-only Core inspection path service that produces cycle-safe structural containment paths for entities, including upward, root-relative, max-depth, and shared-root queries.
2. SadConsole roadmap Stage 0/1 contract paving: frontend UX source-of-truth cleanup, debt inventory, and shared playable-session launch extraction.
3. Shared controlled action / Action Choice-compatible command result, action target/affordance queries, structured outcome/log projection, and entity panel projection.
4. SadConsole debug/browser shell and information-density pass once shared contracts are ready for frontend-owner consumption.
5. Scenario/tester curation using the scenario manifest and descriptions only if tester confusion, deprecated/crashy entries, or naming issues make it necessary.

Deferred to the consolidated frontend bucket:

- Interactive breadcrumb navigation.
- Collapsible/expandable multi-entity inspection-chain panels.

Paused Beta target statement:

- The project can present several small authored gameplay vignettes, like pitch-deck slides, that demonstrate what the engine naturally makes possible.
- Each vignette should be playable in Console, runnable headlessly for validation, and useful for deciding which interactions are interesting or engaging.
- Beta should build enough primitives, fixtures, and scenario reports to inform the eventual unified frontend/player-interaction model.

Long-term frontend direction:

- SadConsole is the preferred canonical debug/editor browser direction while final frontend-engine choice remains deferred.
- That frontend should support title/menu flow, content loading, play, and eventually content editing.
- Major frontend feature work should follow the shared-contract paving sequence in `docs/Plans/SadConsole-Frontend-Roadmap.md` so frontend-owner work does not require Core knowledge or duplicate simulation semantics.
- The current Avalonia editor is legacy-priority and should not remain a dependency of Console, scenario materialization, scenario running, scenario recording, or future headless tooling. Scenario/tooling services should be UI-agnostic so a future commercial-engine frontend can consume the same Core/Content capabilities without inheriting Avalonia assumptions.

Current decision point: Sprint 21 completed Console scenario catalog/listing. Gate 5 template spawning and Gate 6 reactions are no longer the default next work; they remain backlog items until tester feedback or a specific scenario need re-promotes mechanics expansion.

Paused beta content-exploration order:

1. Explore and test scenarios that are authorable with current tools. Completed for the first primitive-showcase batch in Sprint 12; actor zoo deferred.
2. Gate 1: direction transform batch (`ReverseFacing`, `TurnLeft`, `TurnRight`, `Backstep`), then explore the scenarios unlocked by relative facing changes. Completed in Sprint 13.
3. Gate 2: `AcquireNearestTarget` + `SeekTarget`, then explore direct chase, collector, follower, and targeted interaction scenarios. Completed in Sprint 14 for acquire, direct chase, targeted destroyer, and collector; follower remains deferred until it has a differentiated use case.
4. Gate 3: target-distance / directional choice primitives, then explore fleeing, keep-away, kiting, and patterned pursuit. Completed in Sprint 16 for `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, `StrafeAnticlockwise`, and kiting/orbiter composition; richer configurable distance bands and patterned rook/bishop/knight-like pursuit remain deferred.
5. Gate 4: `Give` / `Take`, then explore passive containers, trade, stealing, feeding/offering, and transfer restrictions. First peer-transfer slice completed in Sprint 19 for passive chest, stealing, feeding/offering, and collector-trader handoff; true trade/barter semantics and transfer restrictions remain deferred.
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

Paused beta candidate focuses:

- Current-tool beta vignettes: Sprint 12 completed primitive showcase scenarios for `PushFacing`, `DestroyTarget`, `DropFacing`, and `CreateFacing`, plus pickup/drop/weight and blocker/target fallback-chain exercises. Sprint 13 completed focused direction-transform showcases for turning, backstep, wall-bounce, and patrol-turn behavior. Sprint 14 completed targeting showcases for acquisition, direct chase, targeted destruction, and autonomous collection. Sprint 16 completed distance-movement showcases for fleeing, hard-coded Chebyshev distance-two maintenance, clockwise/anticlockwise strafing, and kiting/orbiter fallback composition. The targeting/facing cleanup moved target acquisition and post-move facing updates out of normal action-plan scripting; the first curated actor zoo and follower scenario remain deferred until a broader gallery or differentiated use case becomes useful.
- Beta vignette design: define several small demo scenarios that probe different kinds of gameplay, such as movement puzzles, blocker/target interaction, pickup/drop containment, autonomous actors, creation/destruction, and peer transfer once supported.
- Scenario report and run workflow polish: text report/template for agents, richer inventory/containment state summaries, local turn-order/previous-action tables, compact per-turn state diffs, created/destroyed entity summaries, capability-gap sections, preview-plus-simulation in one command, actor-zoo/isolation report templates, and cleanup/replacement of the older test-local runner. Deferred tactical telegraphing should project each actor's next resolved behavior/fallback on a safe simulation snapshot; pull it forward when complex gameplay scenarios require tactical information beyond previous actions.
- Foundational movement/peer-interaction primitives: movement-relative primitives, `SeekTarget`, entity-authored targeting rules/slots, and `Give`/`Take` when vignettes demonstrate need. Legacy turn-in-place and acquisition steps remain compatibility behavior rather than preferred new authoring.
- Scenario/content package ergonomics: multiple fixture scenarios, scenario listing/selection, authoring helpers, and stronger validation/reporting around packages. Sprint 21 completed the near-term Console scenario catalog: scan a designated scenario/content folder for existing YAML scenario definitions, persist a generated manifest/cache with optional menu descriptions, and let Console list/select scenarios without introducing scenario-definition metadata fields or content package files.
- Capability-gap logging: record intentionally blocked or negative vignettes and promote feature requests when repeated scenario pressure or one high-value flagship scenario justifies it.
- Frontend/editor loop follow-up: keep future unified frontend requirements visible, but defer implementation until beta vignette playtests clarify interaction and authoring needs.

Paused beta selection guidance (historical):

- Prefer designing the first beta vignette set before adding broad primitives, so mechanics are pulled by demo needs.
- Prefer scenario/report polish first if content-authoring agents still need manual test harnesses or cannot quickly interpret vignette behavior.
- Prefer movement/peer-interaction primitives if beta candidate vignettes need behavior not representable with persistent `Facing`, current `Target`, and existing canonical Action Steps.
- Prefer scenario/content package ergonomics if manually launching, selecting, or comparing vignettes becomes the immediate beta bottleneck.
- Prefer inventory/containment work only after scenario exercises expose concrete transfer/containment needs.

## Prioritized backlog buckets

### Bucket 1: Scenario/testing/tooling feedback loop

Status: Highest-priority backlog bucket.

Priority order:

1. Completed Gamma inspection-path/breadcrumb query: cycle-safe read-only containment path service for the current player and inspected entity.
2. SadConsole/frontend contract paving from `docs/Plans/SadConsole-Frontend-Roadmap.md`, especially shared session launch, controlled action/Action Choice-compatible results, target affordances, structured logs, and entity panel projections.
3. SadConsole entity-panel breadcrumb display and improved inspection panel summaries.
4. Headless debug scenario recorder: `RecordScenario`-style sibling workflow, dotnet-accessible command, PNG frames, GIF output, and visual state debugging for scenario turns.
5. Compact world/state summary formatter for entity positions, facing, target, inventories/containment, created/destroyed entities, and changed state per turn.
6. Lightweight scenario report template once first runner output reveals the useful fields.
7. Capability-gap log/report section for unsupported authoring/simulation requests and intentionally blocked negative vignettes.
8. Plan preview + simulation in one API command.
9. Primitive showcase report support for demonstrating one Action Step's setup, success, failure/fallback, state reads/writes, and trace output.
10. Curated actor-zoo report template for one-room behavior demonstrations.
11. Automated actor isolation preview: generate a small room around an arbitrary entity template, run a fixed number of turns, and report behavior.
12. Cleanup/replacement path for the older test-local `MinimalScenarioRunner` now that `AgentContentEditorApi.RunScenario` exists.
13. Headless run command / scriptable entry point for running scenarios without writing tests or embedding C#.
14. Generalized scenario runner upgrade sprint.
15. Per-initiative debug recording frames for dense simulations.
16. Saved scenario runlogs.
17. Golden runlog tests.

Completed baseline:

- Sprint 10 added `AgentContentEditorApi.RunScenario`, scenario-root entity templates, inventory-plane scenario spaces, deterministic row-major contained-actor initiative, rich canonical behavior-chain traces, and observational runtime outcome reporting.
- Sprint 11 completed the alpha MVP scenario path: persisted `scenarios`, reusable scenario materialization, player insertion diagnostics, agent/editor scenario authoring/materialization, Console scenario launch by content path and scenario ID, and embedded alpha smoke coverage.
- Sprint 12 added beta current-tool content fixtures for push, destroy, create, drop, pickup/weight, and behavior-chain composition; consolidated beta fixture validation; and recorded GAP-001 for `CreateFacing` placeholder presentation/template binding.
- Sprint 15 added the persisted-scenario debug recorder with PNG/GIF artifacts and visual facing/target markers for reviewing authored scenario simulations.
- Sprint 16 added Gate 3 distance-movement primitives and beta fixtures: `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, `StrafeAnticlockwise`, and kiting/orbiter fallback composition, each validated headlessly and recorded as GIF artifacts.
- Sprint 17 moved scenario materialization/run/record workflows out of the legacy Editor dependency path: `GameGameGame.Content` owns canonical scenario materialization, `GameGameGame.Headless` owns scenario run/record services and debug rendering, Console no longer references Editor, and normal non-Editor tests validate scenario tooling without building Avalonia.
- Sprint 21 added a shared scenario catalog and Console scenario menu: single-file listing, folder discovery, automatic `Manifest.yaml` refresh, optional manifest-only descriptions, manifest loading, default Beta folder/manifest behavior, catalog-entry launch, and return-to-list flow while preserving direct content-file/scenario-ID launch.

Future generalized scenario runner wishlist:

- Replace or retire the older test-local `MinimalScenarioRunner` after the shared headless runner/report format is stable enough for the remaining report-text tests.
- Add an agent/headless API command that runs a persisted scenario by scenario ID, including player insertion/materialization, instead of only root-template simulation.
- Make root-only versus persisted-scenario simulation terminology explicit in commands/reports/docs.
- Add a small typed scenario setup model for planes, runtime entity placements, watched entities, turn count, expected diagnostics, and capability-gap notes without becoming a separate content language.
- Support editor/API-authored temporary content end-to-end: templates, carried inventory, initial action state, default plan assignment, behavior-chain authoring, validation, runtime spawn, simulation, and report generation.
- Provide richer compact state summaries for positions, facing, target, inventories/containment including carried inventory coordinates, created/destroyed entities, and changed state per turn.
- Add stable report sections suitable for saved runlogs and eventual golden comparisons once the format stops changing.
- Allow scenario reports to include plan preview, validation diagnostics, simulation trace, state diff, inventory/containment summary, and capability gaps in one result.
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

Direction decision: keep `Facing` as the first primitive orientation model for local movement. Prefer movement primitives that report their actual movement direction and let shared post-action state update persistent `Facing`, rather than scripting turn-only mutations into normal plans. Add goal-directed target movement through entity-level targeting rules and generic numeric target slots. Prioritize peer inventory transfer soon after movement basics so foundational content can experiment with entity-to-entity interactions.

Priority order:

1. Completed baseline: movement-relative primitives, `SeekTarget`, template-scoped entity `targetingRules`, numeric target slots, and post-move `Facing` updates.
2. `Give`: move a carried entity into the inventory space of the persistent target entity.
3. `Take`: move an entity from the inventory space of an adjacent/target entity into the actor inventory.
4. Explicit failure/turn-consumption policy for exhausted behavior chains, if scenario reports reveal author confusion beyond observational reporting.
5. Consistent blocker/target-slot writing rules for failed directional steps, including whether `DropFacing` and `Backstep` should write blockers.
6. Relationship/category targeting or target priority policies if template-targeting rules become too limited.
7. Patterned target movement such as rook/bishop/knight-like pursuit after basic `SeekTarget` is proven.
8. Wall-following helpers or sensory/conditional primitives after relative direction transforms and scenario reports show the useful abstraction level.
9. `TeleportTo`, likely requiring a new `TargetLocation`/destination state slot rather than overloading entity target slots.
10. `BumpTarget` or generic interaction fallback steps.
11. Player/screen messages if scenarios need action feedback beyond traces.

Dependencies:

- Scenario exercises should provide concrete movement and failure examples for each primitive batch.
- Relative movement/facing semantics and `SeekTarget` should remain stable before patterned rook/bishop/knight-like movement is promoted.
- `Give`/`Take` depend on the inventory/containment model enough to require explicit scenario coverage, but they are now considered foundational peer-interaction primitives rather than distant conceptual work.
- `TeleportTo` likely depends on a new location/destination state model.

Promotion trigger:

- Promote `Give`/`Take` when generated scenarios need peer inventory transfer between adjacent or targeted entities.
- Promote richer targeting policy only when template-scoped targeting rules and numeric target slots are too limited for authored scenarios.

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

1. Gamma tester scenario curation: decide whether `src/GameGameGame.Content/Beta/Manifest.yaml` is a checked-in curated index or local generated cache, curate descriptions/order/visibility, and keep deprecated/crashy scenarios from confusing testers.
2. Beta/Gamma content file organization: introduce folders or multiple content documents when fixture count makes single-file navigation, scenario selection, or validation noisy.
3. Scenario families and grouping once individual alpha scenario documents work.
4. Richer scenario metadata beyond alpha launch needs.
5. Scenario-level initial action-state overrides for specific materialized entities, especially initial `Target`, once vignettes need locked-on or non-nearest target setup.
6. Richer world/setup data beyond scenario-root inventory spaces.
7. Content package files and import/merge semantics, including possible separation of scenario definitions, reusable entity templates, presentations, and action plans, only after content duplication or extensive reuse becomes a concrete bottleneck.
8. Loading one scenario inside another for setpieces or nested levels.
9. Randomly generated levels.

Dependencies:

- Alpha scenario documents, player start metadata, and Console scenario loading are now tracked in the alpha roadmap.
- The alpha scenario materialization path should clarify the boundary between reusable content definitions, scenario setup, runtime world state, and player start metadata before richer packaging is promoted.
- The completed Sprint 21 scenario catalog/menu stays a discovery/cache layer over existing loose content documents; future work should not turn it into a package/import system by accident.
- Content package files are unnecessary until scenarios and templates are reused enough that duplication, cross-file references, or save-location ownership become the primary authoring pain.
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

1. Shared typed action-plan shape classifier for canonical behavior chains, transitional primitive plans, legacy low-level steps, empty/passive plans, and invalid mixed shapes.
2. Behavior/action-plan templates.
3. Apply-template workflow.
4. Save-as-template workflow.
5. Template editing.
6. Template usage display.

Dependencies:

- Current behavior-chain descriptors are sufficient for engine/editor parity, and templates are not currently required as a foundation for other capabilities.
- The typed shape classifier is a cleanup follow-up from Sprint 17; promote it when duplicated shape detection in Content, Editor adapters, or reports causes confusion or blocks further tooling polish.

Promotion trigger:

- Promote when repeated authored scenarios duplicate the same behavior chains often enough that manual chain authoring becomes a clear bottleneck.

### Bucket 8: Unified frontend, inspection UX, and integrated editor

Status: Active strategic bucket with the SadConsole prototype spike concluded as research findings. Current Avalonia GUI remains legacy-priority / maintenance-mode; Console remains the supported minimal frontend until a replacement is selected.

Consolidated scope:

- Tester-facing play frontend: scenario selection/loading, play controls, inspection, action prompts, feedback, and rendering.
- Debug/inspection frontend: breadcrumb/entity-chain navigation, entity panels, runlog stepping, local logs, visual focus/active-actor cues, and debug-render styles.
- Future integrated editor frontend: player-friendly authoring/editing surfaces backed by existing editor service/API concepts, not duplicated YAML/content logic.
- Frontend technology decision: compare SadConsole, Godot, Unity, or other candidates after prototype evidence clarifies play, inspection, mouse, layout, and editor needs.

Priority order:

1. Preserve frontend-agnostic Core/Content/Headless/Editor service contracts so Console, SadConsole, and any later game/editor frontend consume the same capabilities.
2. Convert the completed SadConsole spike findings into a frontend architecture plan, especially shared controlled-action, valid-target, scenario/session-launch, and turn-log projection services.
3. Turn-log UX for a future build: start from the universal turn trace, then distribute useful local per-panel logs beneath each entity by initiative/turn order without losing access to the full trace.
4. Scenario selection for a future build: add a manifest/scenario menu using the existing scenario catalog/manifest concepts so players can choose curated scenarios without command-line arguments.
5. Distribution for a future build: investigate and implement an itch.io-friendly browser/HTML5 path if the selected frontend stack can support it; otherwise record the blocker and provide the best shareable fallback while reassessing frontend technology risk.
6. Resume SadConsole frontend work only after the shared-service plan is clear and the initial contracts are ready for frontend-owner consumption; keep final engine comparison deferred until evidence from the canonical debug/browser path requires it.
7. Entity panel chain UX: display the inspected entity's containment/breadcrumb path as left-to-right panels, auto-focus newly inspected panels, and keep entity panels as the primary UX handle.
8. Collapsible/expandable multi-entity inspection-chain panels: expanded/collapsed state, focused-panel navigation, scrolling, and clear debug/detail layout.
9. Keyboard-first player-centric mode model: default Play mode for movement/actions, Inspect/action-prompt modes only when selecting entities or destinations, and coherent focus restoration after actions.
10. Action-prompt targeting polish: show valid targets/destinations, skip invalid cells where practical, and explain blocked pickup/drop/enter/exit choices without inventing frontend-only simulation rules.
11. Mouse convenience layer after keyboard UX is coherent: hit-test panels/cells, click to inspect/select prompt targets, and keep mouse behavior equivalent to keyboard-driven actions.
12. Facing/target/active-actor visualization for play and dense debug simulations, including alternate render styles such as 2x2 color blocks, larger bordered glyph tiles, configurable themes/layouts, and active-actor/focus display.
13. Extract reusable frontend layout/view-model geometry from prototype code so panel bounds, cell hit-testing, focus, and prompt rendering are testable and portable across frontend stacks.
14. Scenario/runlog inspection tools: test inspector or runlog stepper with forward/back controls, plus richer visual state debugging that remains backed by Headless run/record outputs.
15. Future integrated editor affordances: `Run in Console` or equivalent scenario-launch buttons, live preview of an entity performing its action plan, and eventually in-game editor functions using shared editor/API services.
16. Frontend technology decision checkpoint: assess SadConsole against Godot, Unity, or another option once the prototype covers keyboard play, mouse hit-testing, entity panels, logs, editor affordance needs, packaging, and tester feedback.
17. Retire or replace the current Avalonia GUI only when the future frontend/editor surface is viable.

SadConsole prototype coverage snapshot:

| Roadmap need | Current coverage |
| --- | --- |
| Direct scenario launch and materialization reuse | Partially covered by prototype command-line launch; production Console catalog/menu remains separate. |
| Manifest/scenario selection menu | Not covered in SadConsole; production Console has shared scenario catalog/menu concepts to reuse. |
| Entity panel chain from containment path | Partially covered; panels render from inspection path and auto-focus newly inspected panels. |
| Expand/collapse panels and keyboard focus | Partially covered; prototype supports collapse/expand and Tab focus, but layout/focus rules are not production-ready. |
| Keyboard-first play/inspect/action modes | Partially covered; Play, Inspect, pickup/drop/enter/exit prompt modes exist and need polish. |
| Action valid-target highlighting/skipping | Not covered. |
| Mouse hit-testing/click inspection | Not covered. |
| Facing/target/active-actor visualization | Partially covered elsewhere by headless debug rendering; not yet a strong SadConsole UX. |
| Local per-panel logs from universal turn trace | Not covered; prototype currently seeds a universal log view from last turn/local order reports. |
| Itch.io browser/HTML5 distribution | Not covered; current prototype targets `net10.0` with `MonoGame.Framework.DesktopGL`, so browser export needs investigation before assuming feasibility. |
| Reusable panel layout geometry/view models | Partially covered; view models exist, but panel geometry and hit-testing are not centralized. |
| Runlog stepper / debug playback frontend | Not covered. |
| Integrated editor affordances | Not covered; must reuse existing editor/API concepts when promoted. |
| Final frontend engine choice | Not covered; SadConsole is now the preferred canonical debug/browser direction, with final engine comparison deferred until packaging, editor-widget, mouse, or layout evidence requires it. |

Dependencies:

- Depends on shared Core/Content/Headless/Editor service/API contracts staying frontend-agnostic.
- Frontend behavior must not contradict engine/editor capability contracts or add frontend-only simulation semantics.
- Final frontend-engine choice should wait until play controls, inspection-chain interaction, mouse convenience, local logs, layout complexity, packaging, and in-game editing needs are clearer; short-term work should still pave SadConsole as the canonical debug/browser direction.
- Gamma Console breadcrumb work may continue as the supported minimal demo path, but interactive breadcrumbs, collapsible entity panels, and richer visual inspection belong in this consolidated frontend bucket.

Promotion trigger:

- Promote when SadConsole/frontend contract paving needs shared Core/Content/Headless work, when Gamma tester feedback shows current fallback Console cannot adequately support play/inspection/debug workflows, or when interactive breadcrumb/multi-panel UI becomes central enough to expand the SadConsole debug/browser surface.

Decision checkpoint after the timebox:

- **Replace Console as debug/prototype frontend** only if SadConsole proves it can cover debug play, scenario selection, logs, packaging, and developer ergonomics without losing important Console workflows.
- **Stay alongside Console as the main shareable frontend** if SadConsole is clearly better for testers but Console remains valuable as the minimal CLI/debug fallback.
- **Postmortem/R&D only** if the log/menu/package explorations reveal enough friction, especially around browser delivery, to justify starting a different shareable frontend path.

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
