---
id: plan.high-level-roadmap
title: High-Level Roadmap
kind: roadmap
status: active
truth_rank: 50
truth_domains: [planning-priority]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - selecting or refining sprint work
  - sorting conceptual ideas into priority buckets
  - deciding whether a design gap should become planned work or remain deferred
related:
  - source.invariants
  - source.engine-editor-capabilities
  - source.content-authoring-manual
  - source.planning-index
---
# High-Level Roadmap

Status: Active roadmap. Canonical action vertical slices are the selected release direction.

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

Alpha MVP is complete: the game can launch and be played in an authored scenario, and a player entity can be inserted into scenarios through persisted scenario definitions and reusable materialization. Beta produced several authored gameplay vignettes, scenario reports/recordings, transfer showcases, and scenario catalog services. Delta established the arbitrary-entity point-of-view foundation: shared services can describe an observer's current place, breadcrumbs, relative bulk/aperture context, and first affordance/adjective facts so frontend/content presentation and future player-control semantics do not depend on a special player entity. The selected direction now promotes actions vertically: freeze the current broad Action Step catalog as legacy/prototype-compatible, then promote canonical actions one at a time with engine semantics, structured outcomes, POV/affordance facts, player-facing log IDs, content test rooms, editor support, and componentized play-mode consumption. Direction, inventory, spawning, scheduler, reaction, and frontend decisions should continue to be informed by scenario and tester evidence rather than broad speculative mechanics.

The former Avalonia GUI has been removed. New authoring and scenario-feedback work should prioritize editor services, agent/headless APIs, tests, and SadConsole/future frontend readiness.

## Alpha MVP: playable arbitrary scenarios

Status: Complete as of Sprint 11. The alpha path is represented end-to-end in tests: persisted scenario definition -> validation/materialization -> player insertion -> frontend-neutral playable session -> player action.

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
   - Treat runtime control-source / Action Choice discovery as a future player-control model, not an alpha prerequisite.

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
- Retired Avalonia GUI parity for scenario authoring.

## Active / likely next sprint

### Canonical action release target: vertical slices and player control

Status: Selected after the Delta point-of-view foundation and player-facing log groundwork.

Active canonical action planning document:

- [Canonical Actions Vertical Slice Plan](Canonical-Actions-Vertical-Slice-Plan.md)

Immediate canonical action priority:

- Preserve the completed freeze/promotion model: the broad Action Step catalog remains legacy/prototype-compatible unless promoted through a vertical slice.
- Treat the completed canonical `Move` slice as the reference promotion workflow: engine rules, structured outcomes, player-facing log IDs, two content rooms, editor/headless tool support, and Action Choice/play-mode consumption.
- Treat the first Pickup/Drop interaction seam as implemented evidence: Core Action Choice exposes target/source/destination facts for `TransformAdjacentToInventory`/`PickupTarget` and `TransformInventoryToAdjacent`/`DropFacing`, submissions execute through shared history/command services, and componentized play mode has an action-step-first menu path.
- Treat the completed Enter/Exit policy slice as implemented evidence: Core/content/editor/frontend layers support nullable `EnterPolicy`/`ExitPolicy`, typed Enter/Exit Action Choice prompts, frontend policy editing, and canonical Enter/Exit rooms.
- Treat the completed canonical peer inventory Transfer sprint as implemented evidence: Transfer is one controller-agnostic atomic containment action with ActorToTarget and TargetToActor directions, not a direct promotion of legacy `GiveTarget`/`TakeTarget` shortcuts. It now has Core semantics, descriptor/YAML/editor/API parity, content outcome/player rooms, Action Choice/history submission, and first SadConsole workflow support.
- Keep `TransformInventoryToRanged`/Throw and `TransformAdjacentToRanged`/Shove in backlog until the broader canonical action vocabulary is proven, unless a concrete scenario requires ranged transform semantics sooner.
- Consider `Teleport` only as an advanced/stretch relocation slice: it is already supported as a generic effect, but canonical player-facing semantics, safety/authoring limits, and log/POV expectations differ from constrained inventory verbs.

Canonical action target statement:

- Existing Action Steps remain compatible but are no longer all implicitly release-canonical.
- A canonical action is release-ready only when its engine semantics, success/failure outcome projection, POV/affordance facts where applicable, frontend log IDs, and content test rooms are complete. Wanted by core-owner after the Core refactor/consolidation sprint: keep the two-room fixture pattern explicit for every promoted action so each action proves success, common failure, editor authoring, player/action-choice, and trace/log projection paths without bespoke setup.
- Player control becomes runtime decision-source state over normal authored action steps rather than a permanently special player entity command path or a meta-control Action Step.
- Componentized play mode consumes canonical action/Action Choice/POV/log contracts without inventing frontend-only simulation semantics.

Recently completed focused sprint:

- [Give/Take Transfer Vertical Slice Sprint Plan](../Archived/Give-Take-Transfer-Vertical-Slice-Sprint-Plan.md): Core atomic Transfer semantics, policy-asymmetric ActorToTarget/TargetToActor directions, combined outcome/player rooms, Action Choice/history support, and a first frontend transfer workflow.

New backlog items from Enter/Exit wrap-up:

- **Content-authored initial control source / nested playable starts**: implemented through instance-level placed-entity `controller` metadata. Authored `controller: Player` instances initialize to `PlayerChoice`, missing/`Computer` defaults to automatic control, nested/multiple/playerless starts are supported at materialization, and legacy scenario-root `playerStart` insertion remains fallback only when no placed player controller exists. SadConsole and headless runs now use initiative-aware `PlayerChoice` scheduling: automatic actors advance in deterministic order, prompts occur when controlled actors are reached, and headless reports pending prompts as runtime observations.
- **Transfer policy interpretation**: implemented by the completed Transfer sprint. ActorToTarget invokes the adjacent destination holder's `EnterPolicy` but not the actor's `ExitPolicy`; TargetToActor invokes the adjacent source holder's `ExitPolicy` but not the actor's `EnterPolicy`. Both directions are atomic containment transfers with structured failure reasons.

Open player-control models after the first action-step-first path:

1. keep action-menu-first as the current implemented baseline: player opens authored action steps, then chooses target/source and destination from Core facts;
2. target/source-first remains a future pathway that should reuse the same Core Action Choice facts;
3. bump-to-interact can be evaluated later for occupied/interactive entities without making movement itself own interaction legality.

### Delta release target: arbitrary-entity point of view

Status: Foundation implemented; retained as reference/follow-up context for canonical action POV, adjective, ratio, and presentation needs.

Delta planning document:

- [Delta Point-of-View Release Plan](../Archived/Delta-Point-of-View-Release-Plan.md)

Delta foundation summary:

- Core/shared point-of-view queries can return observer breadcrumbs, selected current place, observer bulk, place aperture, `BulkToApertureRatio`, diagnostics, and max-depth breadcrumb truncation status.
- Content/entity-panel projections expose POV current-place, ratio, adjective, reciprocal adjective, and diagnostic facts for frontend consumption.
- Aperture-backed success criteria can expose structured ratios, and player narrative log tooling provides stable message IDs/args for wording experiments.
- Follow-up size language, place qualities, richer reciprocal language, and graphical representation should be promoted only when canonical action or frontend needs require them.

## Previous Gamma/frontend direction now on hold

The previous Gamma/SadConsole editor direction is preserved below as backlog context, but it is no longer the selected next-sprint commitment.

### Gamma release target: tester-shareable frontend demo

Status: On hold / returned to backlog after the refactor/code-cleanup reset. Previously selected after Sprint 21 scenario catalog work and later Console deletion; beta mechanics expansion remains paused unless re-promoted.

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
- [Sprint 21: Console Scenario Catalog](../Archived/Sprint-21-Console-Scenario-Catalog.md) (historical; catalog policy now lives in Content and Console has been removed)
- [Sprint 22: Gamma Containment Path Service](../Archived/Sprint-22-Gamma-Containment-Path-Service.md)
- [Gamma Frontend Demo Plan](../Archived/Gamma-Frontend-Demo-Plan.md)
- [SadConsole UI Pattern Discovery Sprint](../Archived/SadConsole-UI-Pattern-Discovery-Sprint.md)
- [Beta Content Exploration Plan](../Archived/Beta-Content-Exploration-Plan.md)

On-hold/backlog Gamma/frontend planning documents:

- [Gamma Editor MVP Plan](Gamma-Editor-MVP-Plan.md)
- [SadConsole Frontend Roadmap](SadConsole-Frontend-Roadmap.md)

Previous immediate Gamma priority, now on hold:

- Use the Gamma Editor MVP plan as the release checkpoint: SadConsole should prove the Editor -> Preview -> Simulation -> Return loop over shared content/editor and runtime services.
- Carry the completed SadConsole spike findings into the current SadConsole editor/debug-browser path.
- Use the SadConsole frontend roadmap to keep shared session/action/target/log/entity-panel contracts aligned while frontend-owner polishes the canonical debug/editor browser shell.
- Prioritize shared controlled-action, valid-target, scenario/session-launch, and turn-log projection services so SadConsole, Godot, or future editor-facing surfaces consume the same capabilities.
- Make SadConsole the supported shareable editor/debug frontend surface; the former Console fallback has been removed.

Previously planned next sprint, now on hold:

- Stages 0-5 of `docs/Plans/SadConsole-Frontend-Roadmap.md` paved the initial frontend contracts, and the archived SadConsole UI pattern discovery sprint rebuilt the editor as the default componentized SadConsole UI with core service-backed authoring parity. The selected Gamma work now continues from `docs/Plans/Gamma-Editor-MVP-Plan.md`: improve turn-0 preview quality, complete Simulation handoff/return from the componentized editor, add a first provenance-backed source jump, and then harden for release. Console breadcrumb display is deferred/subsumed by the entity-panel projection and SadConsole editor/debug-browser path unless explicitly re-selected as fallback polish.

Gamma/editor target statement:

- The project can be shared with test players and content/debug users through a SadConsole editor/debug browser path.
- Users can open content, browse authored scenarios/templates/action plans, inspect validation diagnostics, preview a scenario at turn 0, launch Simulation, and return to the same editor context.
- Testers can choose curated scenarios, understand what they are looking at, and give useful feedback without reading development docs.
- SadConsole is now the preferred canonical debug/editor browser direction; Console has been removed.
- Short-term work should preserve shared UI-agnostic query/catalog/session/action/log contracts that SadConsole or a later frontend engine can consume.

Gamma/editor promoted stages:

1. Completed in Sprint 22: read-only Core inspection path service that produces cycle-safe structural containment paths for entities, including upward, root-relative, max-depth, and shared-root queries.
2. SadConsole roadmap Stage 0/1 contract paving: frontend UX source-of-truth cleanup, debt inventory, and shared playable-session launch extraction.
3. Shared controlled action / Action Choice-compatible command result, action target/affordance queries, structured outcome/log projection, and entity panel projection.
4. Completed componentized SadConsole Editor shell: default SadConsole launch now opens the clean editor UI. The former `--beta-editor` legacy editor path has been removed; a temporary internal legacy Simulation Play stopgap remains until the componentized Simulation screen replaces it.
5. Completed service-backed Editor authoring parity slice: entity template presentation/default plan/targeting/inventory editing, action-plan step insert/replace/delete/move, template/action-plan create/duplicate/delete, save/dirty/unsaved-exit behavior, and Save-as-preview-refresh boundary.
6. Next Gamma work: turn-0 scenario preview quality, Simulation handoff/return over the new componentized editor, history/log continuity, and first provenance-backed source jump.
7. Scenario/tester curation using the scenario manifest and descriptions only if tester confusion, deprecated/crashy entries, or naming issues make it necessary.

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
- The retired Avalonia editor should not return as a dependency of scenario materialization, scenario running, scenario recording, or future headless tooling. Scenario/tooling services should be UI-agnostic so a future commercial-engine frontend can consume the same Core/Content capabilities without inheriting retired UI assumptions.

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

1. History playback / SadConsole-rendered recording export: take any valid turn history and render frames, GIF, or another visual artifact from the canonical frontend model. The older headless PNG/GIF scenario recorder remains legacy fallback tooling rather than the preferred investment path.
2. Saved scenario runlogs and runlog stepper/playback artifacts backed by shared history.
3. Golden runlog tests once saved runlog format stabilizes.
4. Compact world/state summary formatter for entity positions, facing, target, inventories/containment, created/destroyed entities, and changed state per turn.
5. Capability-gap log/report section for unsupported authoring/simulation requests and intentionally blocked negative vignettes.
6. Plan preview + simulation in one API command.
7. Primitive showcase report support for demonstrating one Action Step's setup, success, failure/fallback, state reads/writes, and trace output. Wanted by core-owner after the Core refactor/consolidation sprint as the lightweight "why did this actor do that?" debug view: attempted Action Steps, state reads/writes, target/facing state, fallback continued/stopped, and final turn result in one inspectable report.
8. Curated actor-zoo report template for one-room behavior demonstrations.
9. Automated actor isolation preview: generate a small room around an arbitrary entity template, run a fixed number of turns, and report behavior.
10. Cleanup/replacement path for the older test-local `MinimalScenarioRunner` now that `AgentContentEditorApi.RunScenario` exists.
11. Headless run command / scriptable entry point for running scenarios without writing tests or embedding C#.
12. Generalized scenario runner upgrade sprint.
13. Per-initiative debug recording frames for dense simulations, if saved runlogs/playback do not cover the need.

Completed baseline:

- Sprint 10 added `AgentContentEditorApi.RunScenario`, scenario-root entity templates, inventory-plane scenario spaces, deterministic row-major contained-actor initiative, rich canonical behavior-chain traces, and observational runtime outcome reporting.
- Sprint 11 completed the alpha MVP scenario path: persisted `scenarios`, reusable scenario materialization, player insertion diagnostics, agent/editor scenario authoring/materialization, Console scenario launch by content path and scenario ID, and embedded alpha smoke coverage.
- Sprint 12 added beta current-tool content fixtures for push, destroy, create, drop, pickup/weight, and behavior-chain composition; consolidated beta fixture validation; and recorded GAP-001 for `CreateFacing` placeholder presentation/template binding.
- Sprint 15 added the persisted-scenario debug recorder with PNG/GIF artifacts and visual facing/target markers for reviewing authored scenario simulations.
- Sprint 16 added Gate 3 distance-movement primitives and beta fixtures: `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, `StrafeAnticlockwise`, and kiting/orbiter fallback composition, each validated headlessly and recorded as GIF artifacts.
- Sprint 17 moved scenario materialization/run/record workflows out of the legacy Editor dependency path. Later cleanup removed Console and the Avalonia editor; `GameGameGame.Content` now owns canonical scenario materialization and run reports, and `GameGameGame.Headless` owns legacy debug recording/rendering.
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
- Core-owner convenience note from the Core refactor/consolidation sprint: prioritize trace/debug report shapes that make Action Step execution explainable without custom test assertions, especially attempted steps, reads/writes, target/facing state, fallback outcome, and final turn result.
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
3. Predicate/item selection rules for authored `Transfer` beyond the current concrete `targetSlot`/`targetLabel` moving-entity reference.
4. Richer peer-transfer restrictions/permissions, trade/barter rules, and author-facing diagnostics.
5. Richer containment/inventory report summaries for generated scenarios.

Dependencies:

- Canonical `Transfer` is promoted as a foundational peer-interaction primitive; this bucket retains deeper inventory/containment model, permission, and selection-rule follow-up work.
- Richer Transfer selection rules depend on the inventory/containment model.
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

1. Gamma tester scenario curation and manifest tooling: decide whether `src/GameGameGame.Content/Beta/Manifest.yaml` is a checked-in curated index or local generated cache, curate descriptions/order/visibility, keep deprecated/crashy scenarios from confusing testers, and make scan/refresh/editor workflows explicit enough that validation or play-testing does not accidentally rewrite checked-in manifests during unrelated content work.
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

Status: Strategic backlog bucket. SadConsole was the canonical debug/browser frontend project before roadmap reset; Console and the former Avalonia editor have been removed.

Consolidated scope:

- Tester-facing play frontend: scenario selection/loading, play controls, inspection, action prompts, feedback, and rendering.
- Debug/inspection frontend: breadcrumb/entity-chain navigation, entity panels, runlog stepping, local logs, visual focus/active-actor cues, and debug-render styles.
- Future integrated editor frontend: player-friendly authoring/editing surfaces backed by existing editor service/API concepts, not duplicated YAML/content logic.
- Frontend technology decision: compare SadConsole, Godot, Unity, or other candidates after prototype evidence clarifies play, inspection, mouse, layout, and editor needs.

Priority order:

1. Preserve frontend-agnostic Core/Content/Headless/Editor service contracts so SadConsole and any later game/editor frontend consume the same capabilities.
2. Continue SadConsole debug-browser UX polish over the completed shared history/session/action/target/log/panel contracts.
3. Entity panel chain UX: improve inspected containment/breadcrumb panel behavior, auto-focus, collapse/expand, and dense local activity readability.
4. Action-prompt targeting polish: show valid targets/destinations, skip invalid cells where practical, and explain blocked pickup/drop/enter/exit choices without inventing frontend-only simulation rules.
5. Mouse convenience layer after keyboard UX is coherent: hit-test panels/cells, click to inspect/select prompt targets, and keep mouse behavior equivalent to keyboard-driven actions.
6. Facing/target/active-actor visualization for play and dense debug simulations, including alternate render styles such as 2x2 color blocks, larger bordered glyph tiles, configurable themes/layouts, and active-actor/focus display.
7. Saved runlog/playback UX: test inspector or runlog stepper with forward/back controls, plus richer visual state debugging backed by shared history.
8. History playback / SadConsole-rendered visual export for shareable debug artifacts.
9. Distribution for SadConsole feedback builds; browser/HTML5 hosting is deferred unless a later frontend-technology checkpoint re-promotes it.
10. Componentized Editor -> Preview -> Simulation handoff: launch/play from the new editor surface, return to the same editor context, and add provenance-backed source jumps.
11. Future integrated editor affordances: `Run in SadConsole` or equivalent scenario-launch buttons, live preview of an entity performing its action plan, and eventually in-game editor functions using shared editor/API services.
12. Editor capability gaps discovered by the parity sprint: scenario root/player-start editing; per-carried-instance initial facing/state in inventory; typed action-step parameter/check/effect projection and mutation for Screen 4.
13. Frontend technology decision checkpoint: assess SadConsole against Godot, Unity, or another option once the prototype covers keyboard play, mouse hit-testing, entity panels, logs, editor affordance needs, packaging, and tester feedback.
12. Former Avalonia GUI retired; continue replacing its useful authoring affordances through shared services and SadConsole/future editor surfaces.

SadConsole prototype findings coverage snapshot:

This table summarizes findings from the completed spike; it does not imply that the old prototype project exists on main.

| Roadmap need | Current coverage |
| --- | --- |
| Direct scenario launch and materialization reuse | Covered by production SadConsole command-line launch through the shared playable session launcher. |
| Manifest/scenario selection menu | Covered in production SadConsole through the shared scenario catalog/menu path; scan/save policy lives in Content `ScenarioCatalogScanService`. |
| Entity panel chain from containment path | Partially covered; panels render from inspection path and auto-focus newly inspected panels. |
| Expand/collapse panels and keyboard focus | Partially covered; prototype supports collapse/expand and Tab focus, but layout/focus rules are not production-ready. |
| Keyboard-first play/inspect/action modes | Partially covered; Play, Inspect, pickup/drop/enter/exit prompt modes exist and need polish. |
| Action valid-target highlighting/skipping | Not covered. |
| Mouse hit-testing/click inspection | Not covered. |
| Facing/target/active-actor visualization | Partially covered elsewhere by headless debug rendering; not yet a strong SadConsole UX. |
| Local per-panel logs from universal turn trace | Partially covered in production SadConsole through history-backed global/local action logs with conservative autonomous anchors. |
| Itch.io browser/HTML5 distribution | Deferred; current SadConsole feedback builds target desktop `net10.0` with `MonoGame.Framework.DesktopGL`, so browser export remains a later technology-risk investigation rather than a current requirement. |
| Reusable panel layout geometry/view models | Partially covered; view models exist, but panel geometry and hit-testing are not centralized. |
| Runlog stepper / debug playback frontend | Not covered; backlog now prefers saved runlogs/history playback over extending the legacy recorder. |
| Integrated editor affordances | Not covered; must reuse existing editor/API concepts when promoted. |
| Final frontend engine choice | Not covered; SadConsole is now the preferred canonical debug/browser direction, with final engine comparison deferred until packaging, editor-widget, mouse, or layout evidence requires it. |

Dependencies:

- Depends on shared Core/Content/Headless/Editor service/API contracts staying frontend-agnostic.
- Frontend behavior must not contradict engine/editor capability contracts or add frontend-only simulation semantics.
- Final frontend-engine choice should wait until play controls, inspection-chain interaction, mouse convenience, local logs, layout complexity, packaging, and in-game editing needs are clearer; short-term work should still pave SadConsole as the canonical debug/browser direction.
- Interactive breadcrumbs, collapsible entity panels, and richer visual inspection belong in this consolidated SadConsole/frontend bucket.

Promotion trigger:

- Promote when SadConsole/frontend contract paving needs shared Core/Content/Headless work, when tester feedback shows current SadConsole feedback builds need stronger play/inspection/debug workflows, or when interactive breadcrumb/multi-panel UI becomes central enough to expand the SadConsole debug/browser surface.

Decision checkpoint after the timebox:

- **Continue with SadConsole as the main shareable frontend** if feedback builds cover debug play, scenario selection, logs, packaging, and developer ergonomics.
- **Reassess frontend technology** only if packaging, editor-widget, mouse, layout, or browser-delivery constraints become significant enough to justify starting a different shareable frontend path.

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

Status: Promoted into the active canonical-actions direction as runtime control-source / Action Choice work.

Concept:

- Runtime control source should be mutable per actor, likely with or adjacent to persistent entity action state.
- Fallback-controlled actors resolve their effective Action Plans through normal ordered fallback policy.
- Player-controlled actors produce Core-owned `ActionChoiceRequest` / target-choice / submission results over the actor's normal authored action steps rather than a hardcoded player command set.
- This allows any entity to become player-controlled through runtime state changes rather than through a special hardcoded player entity or a required input-sentinel Action Step in every controllable plan.
- Deferred design item: a future `Use` action may let an actor use a carried entity as an action source, for example using a carried goblin's special action against an adjacent target. This is intentionally not part of the runtime behavior-override spike because it needs explicit actor/source attribution, target legality, selected Action Step semantics, trace/log shape, and turn-consumption decisions.

Dependencies:

- Requires action-choice discovery, frontend input integration, and revised action-plan resolution semantics.
- Requires `WorldState` clone/rollback/history handling for control-source state and pending/submitted choices.
- Should be designed with future integrated frontend needs in mind, not just the current SadConsole debug/browser surface.

Promotion trigger:

- Continue promoting through `docs/Plans/Canonical-Actions-Vertical-Slice-Plan.md` when canonical action rooms need arbitrary controlled entities, control-source changes, or multi-entity/team control.

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

### Bucket 12: Core/editor developer ergonomics and refactor support

Status: Low-priority internal quality-of-life bucket; promote when maintenance friction blocks active roadmap work.

Priority order:

1. Core system ownership/refactor maps for large split systems, starting with `ActionPlanInterpreter`, `EditableContentDocument`, and major scenario/play-mode services. Each map should identify the facade file, executor files, dispatchers, handler clusters, helpers, and representative test suites.
2. Duplicate xUnit test-name detector or review script for future test-fixture migration/refactor sprints, so coverage moves can distinguish preserved tests from accidental duplicate fixtures.
3. Further internal handler-category organization for canonical action verbs, so movement, targeting, containment/inventory, and plan-override behavior can be navigated as a durable catalog without creating editor-only concepts or changing public engine APIs.

Dependencies:

- Ownership maps should follow the current code shape instead of prescribing architecture ahead of need.
- Duplicate-test detection should be lightweight enough to run during sprint wrap-up and should not become a required build step until it proves useful.
- Handler-category cleanup must preserve existing action semantics, trace shape, turn consumption, and editor/content parity.

Promotion trigger:

- Promote when another large Core/Content refactor begins, when duplicate test fixtures are found again during cleanup, or when canonical-action promotion repeatedly requires navigating the same private handler clusters.

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
