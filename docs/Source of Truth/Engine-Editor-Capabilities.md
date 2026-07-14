# Engine-Editor Capability Matrix

Status: Source of truth for maintainer-facing engine/editor capability support, parity tiers, and layer coverage.

Read when:

- adding, removing, renaming, or re-tiering an engine/editor capability;
- deciding which layers must participate in a vertical slice;
- checking whether a capability is stable, advanced, legacy, planned, or intentionally unsupported in a layer.

Do not read when:

- looking for TDD invariant/test traces; use `docs/Source of Truth/invariants.md`;
- looking for content-authoring instructions without implementation detail; use `docs/Source of Truth/Content-Authoring-Manual.md`.

This document is intended for:

- engine/editor maintainers deciding how new Core features should be authored, validated, and exercised through editor tooling;
- agent API work, which should use the same canonical authoring model as the editor service and future frontend/editor surfaces.

Update this document whenever an engine capability is added, removed, renamed, promoted to editor support, intentionally kept engine-only, or moved into legacy compatibility.

Stable behavior contracts and test traces belong in `docs/Source of Truth/invariants.md`. Content-editor-facing usage guidance belongs in `docs/Source of Truth/Content-Authoring-Manual.md`. Canonical Action Step outcome summaries and actor/actee/spatial verb-affordance decision tables belong in `docs/Source of Truth/Action-Step-Outcome-And-Affordance-Logic.md`.

## Evolution policy

The engine, editor services, agent API, and supported frontend/editor surfaces should evolve together without reviving retired UI-specific workflows. The former Avalonia GUI has been removed; current human-facing editor investment targets the integrated SadConsole game/editor frontend over shared services.

Use staged support:

1. **Engine/runtime support**: Core can execute or represent the capability.
2. **Descriptor/YAML support**: content can serialize the capability without hand-written runtime code.
3. **Validation support**: malformed content receives actionable diagnostics where possible.
4. **Editor service support**: tooling and future agent APIs can author the capability through typed operations.
5. **Agent/headless API support**: agents, tests, scripts, and future frontends can author or inspect the capability through structured operations.
6. **Frontend/editor UI support**: a human-facing surface can create/edit the capability through shared services without owning engine/content semantics.

New capabilities may pass through these stages over time. Prefer typed descriptors and canonical engine concepts over ad-hoc editor-only fields. Do not add editor-only concepts that Core cannot consume.

## Authoring support tiers

### Stable authoring support

Stable support is appropriate for editor-service workflows, agent/headless API commands, tests, and current/future frontend/editor surfaces. These capabilities have canonical descriptors, validation, and editor-service support, and are intended for normal content authoring.

Current stable authoring areas:

- entity templates and presentations;
- inventory dimensions, bulk, aperture, and carried entity layout;
- legacy low-level action plans and action-plan steps remain loadable and editable as compatibility when an existing legacy plan is selected, but are hidden from current canonical authoring paths where canonical ordered behavior-chain authoring is available;
- actor initial `Facing` through `actionStateDefaults.facing`, with runtime post-action facing updates from successful directional movement;
- template `targetingRules` for pre-plan target-slot selection by optional target template, target-capability adjectives, and range;
- checks: `CanMove`, `BlockingEntity`, `CanPickup`;
- effects: `Wait`, `Move`, `Pickup`, `ReverseDirection`, `CallPlan`;
- movement effects: `Teleport`, `Drop` are functional and supported, but their human-facing UI remains advanced/generic rather than polished/specialized.
- transitional primitive-backed `MoveFacing` and `PickupTarget` action-plan descriptors with explicit fallback references are supported through Core/content validation, editor services, and the agent API; these linked descriptors are not the long-term canonical editor-facing model.
- canonical ordered behavior-chain descriptors with `MoveFacing`, `Backstep`, `PickupTarget`, `DropFacing`, `PushFacing`, `DestroyTarget`, `CreateFacing`, `SeekTarget`, `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, `StrafeAnticlockwise`, `GiveTarget`, `TakeTarget`, `EnterTarget`, and `ExitFacing` Action Steps have Core runtime, Action Step catalog metadata, descriptor/YAML, hardened validation/default handling, editor service, agent API, and SadConsole/editor UI support that makes canonical chains visually primary over legacy low-level authoring. Runtime compatibility remains for legacy metadata-setting steps `TurnLeft`, `TurnRight`, `ReverseFacing`, and `AcquireNearestTarget`, but new canonical authoring hides/rejects them.
- compact canonical behavior-chain trace formatting is available in Core for tests, debugging, and future editor/agent diagnostics.
- shared actor plan resolution is available in Core through `ActorTurnResolver`, centralizing per-actor planned-action trace/stop/fallthrough behavior for `TurnService`, headless scenario runs, and scenario recording while leaving broader full-turn loop/history migration as follow-up work.
- structured action-step attempt extraction is available in Core through `ActionStepAttemptProjection`, projecting canonical behavior-chain trace children into ordered attempts with step kind, status, failure reason/detail, continued/stopped classification, state reads/writes, selected result details, and preserved step traces for debug expansion.
- local turn-order reporting is available in Core for factual debugger/scenario output, including per-plane occupant ordering, actor/player/inert classification, and previous-action summaries from the latest simulated turn; SadConsole and shared panel projections are the active frontend consumers.
- structural entity containment path queries are available in Core for debugger/frontend foundations, including upward ancestry paths, root-relative paths, shared-root two-branch paths, max-depth truncation, not-under-root/no-shared-root statuses, and directional cycle diagnostics; presentation/content enrichment remains follow-up work.
- first-slice arbitrary-entity point-of-view queries are available in Core as read-only projection facts over containment breadcrumbs: given an observer entity, `PointOfViewService` returns the cycle-safe breadcrumb, the current-place candidate selected by the nearest containing inventory owner rule, the explicit selection basis, observer bulk, current-place aperture, `BulkToApertureRatio`, max-depth breadcrumb truncation status, and structured diagnostics for missing observers, incomplete breadcrumbs, missing current place, or unavailable ratio facts. When supplied action-plan descriptors, POV also derives one-way target adjectives from target-capability Action Steps in the observer's plan and reciprocal target adjectives from other current-place entities' plans, applying each adjective only when the matching non-mutating affordance query succeeds. Aperture-backed target-capability results now carry structured success-criterion ratios such as `SuccessRatio = available aperture / required bulk` for frontend-owned interpretation of degree of success. Content exposes these facts through `EntityPanelProjectionService` with presentation enrichment for the selected current place so frontends can consume the projection without owning POV semantics. Polished size language, place qualities, richer reciprocal language, and final graphical representation remain Delta follow-up work.
- playable frontend session launch is available in Content as a frontend-neutral wrapper over scenario materialization and catalog entries, returning scenario identity, world, registry/presentation lookup, action plans, player entity, active plane/container, diagnostics, runtime failures, and capability gaps; SadConsole consumes this shared launcher as the official debug/browser frontend.
- controlled actor command execution is available in Core for frontend/direct-player compatibility across move, pickup, drop, enter, and exit, returning structured command kind, actor, target/destination anchors, success/failure, failure reason/detail, turn consumption/advancement, trace, and turn report; SadConsole consumes this service instead of locally owning action execution policy.
- controlled actor affordance queries are available in Core for frontend/direct-player prompts across movement, pickup sources/destinations, drop sources/destinations, enter targets, and exit directions, including useful success/failure hints and blocking anchors while keeping final command execution authoritative.
- structured action outcome and log projection is available in Core for controlled-command results, including compact sentence rendering, actor/target/source/destination/direction/failure fields, entity and plane anchors, structured success/failure criterion ratios for aperture-constrained inventory transitions, and preserved traces for debug expansion; projection from broader autonomous turn reports remains a follow-up as outcome anchors mature.
- initial simulation history foundations are available in Core: `WorldState` can be cloned/restored with entities, planes, nodes, occupancy, inventory planes, action states, coordinate lookup integrity, and visible trace/turn-report context; `SimulationHistorySession` can create a frame-0 snapshot, submit controlled commands through the existing authoritative command service, record successful submissions as frame intervals with next snapshots and basic actor logs projected from the resulting turn report, record failed submissions as durable current-frame log entries without advancing frame or world turn, record non-controlled actor intervals for headless simulated turns, roll back to recorded frames while discarding later frames/intervals/log entries, expose previous-frame rollback availability, and project history intervals/frame-failure entries into chronological `ActionLogProjection` rows. History projection includes controlled outcomes and non-controlled interval actor outcomes with consumed-turn facts, conservative actor/plane anchors, preserved traces, and extracted action-step attempts when available. SadConsole consumes this shared history for its controlled-command log and presents `U undo` over shared rollback in Play mode. Richer target/source/destination anchors for autonomous outcomes remain follow-up work.
- scenario run reports now live in Content and use shared history intervals as their simulated-turn source for actor turn rows/traces while preserving the existing content-editor-facing report shape; the current headless scenario recorder remains legacy PNG/GIF fallback tooling, with a future history playback / SadConsole-rendered export preferred over deeper recorder migration.
- compact persisted-scenario player narrative log projection is available through Content and the agent/headless tool `ggg_content_run_scenario_player_log_by_id`; it runs shared persisted-scenario materialization/history simulation, defaults the observer to the scenario player entity, returns grouped compact message IDs plus structured rows/args while final wording remains deferred, and is explicitly labeled as a `player narrative projection` because true line-of-sight/audibility filtering is not implemented. Autonomous action-plan and target anchors remain follow-up where structured history projection does not yet expose them.
- entity panel projection is available in Content as a first-pass frontend-neutral DTO over `EntityInspectionPanel`, containment breadcrumbs, point-of-view current-place/ratio/one-way and reciprocal target-adjective/diagnostic facts, action state, inventory grids, local turn-order content rows, optional action-plan presence, and structured local log snippets; rich layout/focus/rendering and final wording remain frontend-owned and the DTO may evolve with SadConsole feedback.
- canonical action-plan preview is available through editor service and agent API commands, summarizing plan shape, ordered Action Steps, state hints/defaults, validation diagnostics, guidance, and YAML preview text.
- frontend editor snapshots and first template/action-plan mutations are available in Content through `FrontendEditorService`, a SadConsole-consumable service over `ContentEditorSession`/`ContentEditorService` that opens content files and exposes scenario, entity-template/carried-layout, action-state default, targeting-rule, default action-plan target-label requirement/orphaned-rule projection, action-plan target-label requirements, action-step target label/slot references, target-consuming metadata derived from Action Step catalog required-state contracts, stable engine-defined action-step catalog, validation diagnostic, YAML preview, diff, and turn-0 scenario preview DTOs without depending on Avalonia view models. Entity-template snapshot rows include carried-template names/presentation and template/carried diagnostics for authored entity-panel style displays. Current service-backed mutation slices support entity template create/duplicate/delete lifecycle operations; entity template name/glyph/color updates; inventory width/height, bulk, and aperture updates; initial facing set/clear; default action-plan assignment/clearing; targeting rule set/clear for slots 1-4 with lowercase alphanumeric labels and range 0-10; carried-template brush placement plus carried remove/move/template-replace/coordinate-overwrite operations for authored inventory layouts; action-plan create/passive-create/duplicate/delete lifecycle operations; canonical action-plan step kind replace/insert/remove/move and target-label set/clear; and save-when-file-backed results. `AgentContentEditorApi` remains a parallel editing surface over the same session/service layer. `GameGameGame.Content.Tools` exposes a curated session-aware local tool host over `AgentContentEditorApi` for content-editing agents, including document/session open/create/snapshot/validate/save, list/get inspection, semantic entity/action-plan behavior-chain mutations, action-step catalog/previews, persisted scenario inspection/materialization, and scenario run/review reports.

### Advanced but supported

Advanced support is usable and validated but may evolve as the engine grows. Content creators and agents may use these capabilities, but should expect workflow polish and command shapes to improve.

Current advanced support:

- low-level action-plan step/check/effect authoring only as compatibility for selected existing legacy plans while canonical ordered behavior-chain action plans are being implemented;
- primitive-backed linked action plans while canonical ordered behavior chains are being implemented;
- `Teleport`, the general relocation/ur-primitive;
- `Drop`, constrained relocation from actor-carried inventory to peer/world destination;
- typed movement target/destination descriptors;
- descriptor/runtime turn flags retained below current authoring surfaces for future advanced authoring.

Guidance:

- Prefer constrained primitives (`Move`, `Pickup`, `Drop`) for ordinary content.
- Use `Teleport` for advanced relocation cases.
- Do not model common simple behavior as arbitrary teleport if a constrained primitive expresses it more clearly.

### Legacy compatibility support

Legacy support is retained for old content/runtime compatibility. It should load, display, and execute where applicable, but should not be used by new canonical editor or agent API authoring.

Current legacy support:

- string-keyed `ActionPlanContext` variables;
- legacy descriptor fields: `directionVariable`, `targetVariable`, `variableName`;
- `defaultPlanVariables`;
- `SetVariable`;
- configurable turn flags on legacy `SetVariable` and descriptor-level `ReverseDirection`.

Guidance:

- Do not expose arbitrary variable-name authoring in new frontend/editor UI or agent API workflows.
- Do not rush to remove Core/runtime compatibility until there is a migration plan and confidence that content no longer depends on it.
- Canonical authoring should use engine-defined slots and typed descriptors instead.

## Capability support statuses

| Status | Meaning |
|---|---|
| Yes | The layer supports the capability directly. |
| Partial | The layer supports part of the capability, or supports it through a transitional/advanced path. |
| Legacy | Supported for old content/runtime compatibility, not intended for new canonical authoring. |
| Planned | The capability is intentionally planned but not currently exposed in that layer. |
| Intentional non-parity | The capability exists somewhere but is intentionally not exposed in another layer. |
| No | The layer does not currently support the capability. |

## Former Avalonia GUI status

The Avalonia desktop editor has been removed. New engine/editor capabilities should prioritize Core runtime, descriptor/YAML support, validation, editor service operations, agent/headless API support, SadConsole or future shared frontend/editor surfaces, tests, and documentation.

## Current editor capability summary

The editor can currently:

- create/open/save/reload content documents;
- edit entity templates, presentations, inventory dimensions, bulk, aperture, and carried entities;
- assign/clear default action plans;
- create new action plans as empty/passive plans so authors can add canonical behavior-chain Action Steps without first creating legacy low-level steps;
- edit actor initial `Facing`;
- create/edit/delete/reorder action plans and steps;
- author `CanMove`, `BlockingEntity`, and `CanPickup` checks;
- author `Wait`, `Move`, `Pickup`, `ReverseDirection`, `CallPlan`, `Teleport`, and `Drop` effects;
- edit pickup inventory coordinates and call-plan references;
- edit movement target/destination fields for `Teleport` and `Drop`;
- validate content and surface diagnostics for missing references, missing canonical slots, malformed movement descriptors, inventory layout issues, and legacy/arbitrary variable fields;
- load and validate canonical ordered behavior-chain descriptors for stable Action Steps using Action Step catalog metadata, with legacy metadata-setting steps loadable for compatibility but hidden/rejected for new authoring;
- author content through the first in-process `AgentContentEditorApi` facade over editor/content services;
- create transitional primitive-backed `MoveFacing` action-plan descriptors with optional fallback references through editor services and the agent API;
- create a transitional `MoveFacing -> PickupTarget` linked fallback chain through editor services and the agent API without low-level check/effect authoring;
- author canonical ordered behavior chains through editor services and the agent API without low-level check/effect authoring or linked fallback plan descriptors, including a convenience helper for the common `MoveFacing -> PickupTarget` chain;
- preview action plans through editor service and agent API commands before save/manual YAML inspection, including canonical plan shape, Action Step metadata, state hints such as `Facing=West` and `Target=Self`, validation diagnostics, and YAML preview text;
- run first-slice root-only compatibility headless scenarios through the agent API by selecting an editor-authored scenario-root entity template, spawning its inventory space as the scenario plane, scheduling all contained default-plan actors in deterministic row-major initiative order, and returning structured setup, rich behavior-chain turn trace, final-state, cycle-safe inventory/containment summary, validation, runtime-observation, runtime-failure, and capability-gap report data; scenario reports are observational and should not treat expected in-simulation inability to act as a failed scenario;
- run persisted scenarios by scenario ID through headless/editor-agent APIs using shared scenario materialization, including scenario root, player template, deterministic player entity ID, player start insertion, active scenario plane, materialized action plans, setup diagnostics, turn traces, final state, and cycle-safe inventory/containment summaries; setup report text labels persisted scenario simulation separately from root-only compatibility simulation;
- request a combined persisted scenario review report through the editor-agent API that composes document validation, canonical authoring validation, action-plan previews, scenario materialization, persisted scenario run, turn traces, final state, inventory summaries, runtime observations/failures, and capability gaps without adding scenario-only engine behavior;
- request a compact persisted scenario player narrative log through the editor-agent API/tooling, with scenario identity, optional source path, observer entity, validation/materialization diagnostics, grouped turn text, and structured rows derived from shared history/action-step projection rather than formatted trace-line parsing;
- persist and materialize first-slice alpha scenario definitions through content documents, editor services, and the agent API by naming a scenario, selecting normal content templates for the scenario root and player, choosing a deterministic runtime player entity ID, and inserting the player at a requested coordinate in the scenario-root inventory/play plane; scenario validation/materialization reports structured authoring diagnostics for missing root/player templates, unusable roots, invalid starts, occupied starts, and player ID conflicts before simulation;
- launch SadConsole as the official debug/browser frontend against a persisted scenario selected by content file path plus scenario ID, by one-file scenario list, by folder discovery, or by a generated scenario manifest/cache; folder discovery refreshes a `Manifest.yaml` cache in the scanned folder while preserving optional manifest-only `description` annotations for unchanged entries; default SadConsole startup uses `src\GameGameGame.Content\Beta\Manifest.yaml` when present and otherwise discovers `src\GameGameGame.Content\Beta`; scenario launch consumes materialization outputs for world state, action plans, player entity ID, and active scenario plane instead of hardcoded prototype player/plane IDs in play, inspect, and render flows; manifest scan/save policy is available through Content `ScenarioCatalogScanService`;
- record persisted scenarios through a legacy sibling headless `RecordScenario` service/API adapter that materializes a scenario by ID, captures frame 0 plus one frame after each full simulated turn, renders debug PNG frames with two-pane metadata/inventory/initiative layout, glyph/color cells, facing and target markers, visible-target arrows, and writes an animated GIF artifact; the previous Console `record-scenario` command has been removed, and future visual recording should prefer SadConsole-owned recording/export over extending this legacy renderer;
- load an embedded alpha smoke scenario fixture that validates, materializes an inserted player, launches through the shared frontend-neutral scenario launcher, and accepts at least one player movement action for smoke coverage;
- view and edit canonical ordered behavior chains through shared editor services and supported frontend/editor surfaces, including add/remove/reorder for catalog-backed Action Steps, plan-shape guidance, canonical-chain summaries, and default-state hints;
- load/display legacy variable-based content and legacy `SetVariable` effects without exposing them for new canonical authoring;
- hide legacy low-level steps/checks/effects in current canonical authoring surfaces unless the selected plan is already a legacy low-level plan.

The editor intentionally does not currently:

- author arbitrary action-plan variables through current frontend/editor UI;
- author `SetVariable` through current frontend/editor UI;
- author `directionVariable`, `targetVariable`, or `variableName` fields through current frontend/editor UI;
- provide polished/specialized behavior-template workflows beyond current action-plan controls;
- author initial `Target` actor state through current frontend/editor UI;
- expose `CanDrop`, which is deferred until a concrete branching use case appears;
- provide an external agent transport/protocol layer yet.

## Canonical behavior-chain action plans

Canonical action-plan authoring is being remodeled around ordered behavior chains. The completed first slice is archived at `docs/Archived/Behavior-Model-Consolidation-First-Slice.md`; the completed follow-up behavior-system sprint is archived at `docs/Archived/Behavior-System-Next-Steps.md`. The earlier behavior-primitive linked-plan foundation is archived/superseded by this direction; it remains supported as transitional compatibility/prototype work.

Current vocabulary/model assumptions:

- **Action Plan**: the behavior definition assigned to an entity as its default behavior or invoked by another supported mechanism.
- **Canonical behavior chain**: the preferred new authoring shape for normal behavior. It is an ordered list of engine-defined Action Steps on one Action Plan.
- **Action Step**: one engine-defined behavior attempt inside a canonical behavior chain, such as `MoveFacing`, `Backstep`, `PickupTarget`, `DropFacing`, `PushFacing`, `DestroyTarget`, `CreateFacing`, `TurnLeft`, `TurnRight`, `ReverseFacing`, `AcquireNearestTarget`, `SeekTarget`, `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, `StrafeAnticlockwise`, `GiveTarget`, `TakeTarget`, `EnterTarget`, or `ExitFacing`.
- **Fallback / fallthrough**: in canonical behavior chains, fallback means continuing to the next ordered Action Step in the same Action Plan when the current step fails or cannot act. It does not mean creating linked fallback plans for new normal authoring.
- **Primitive-backed linked plans**: transitional compatibility/prototype descriptors. They may remain loadable/supported where documented, but they are not the desired new authoring model.
- **Legacy low-level steps/checks/effects**: compatibility authoring for existing plans. New normal workflows should prefer canonical behavior chains and engine-defined slots instead of arbitrary variable names.
- **Canonical state slots**: engine-defined persistent actor state such as `Facing` and `Target`. Prefer these over string-keyed action-plan variables for new authoring.

Target model:

- an entity Action Plan is an ordered list of engine-defined Action Steps;
- each Action Step is attempted in order until one succeeds or the chain terminates;
- one root action-plan resolution should produce exactly one observable action;
- internal state changes are engine-defined consequences, not arbitrary author-authored variable mutation;
- the final failed/impossible step terminates the root turn without requiring an explicit linked followup plan;
- runtime Action Plan overrides have a first supported spine: each entity may hold one-turn `Pre`, `Main`, and `Post` override slots in action state; effective resolution is `Pre -> Main -> Post`, `Main` replaces the default plan when present, `Pre`/`Post` wrap it as fallback plans, setting an occupied slot replaces it, and slots present at turn start clear after that entity's turn resolution; current content/editor authoring exposes `ApplyPrePlan`, `ApplyMainPlan`, and `ApplyPostPlan` producers for the three slots;
- canonical behavior chains should coexist with current low-level step/check/effect plans and transitional primitive-backed linked plans during implementation, with low-level authoring becoming advanced/legacy over time.

Current and planned Action Step / primitive support:

| Primitive | Status | Required state | Default state | Followup behavior |
|---|---|---|---|---|
| `MoveFacing` | Supported as transitional primitive-backed linked plan; supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Facing` | `West` | Reads persistent actor `Facing`, moves one step, writes `Target` to blocker on blocked movement, and falls through to the next ordered Action Step in the canonical chain. Transitional descriptors follow explicit fallback or terminate the root turn. |
| `Backstep` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Facing` | `West` | Reads persistent actor `Facing`, moves one cell opposite `Facing`, preserves the original `Facing`, writes `Target` only when blocked by an entity, and falls through to the next ordered Action Step on blocked movement. |
| `Wandering` | Superseded as named first-pass primitive | `Facing` | `West` | The first canonical authored behavior should be represented directly as an ordered chain such as `MoveFacing -> PickupTarget`, not as a separate `Wandering` primitive descriptor. Reverse-facing and future `onBump` behavior are not included yet. |
| `PickupTarget` | Supported as transitional primitive-backed linked plan; supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Target` | `Self` | Reads persistent actor `Target`, attempts pickup into the first available valid actor inventory coordinate using deterministic row-major order (`Y`, then `X`) across the actor's inventory plane, and falls through to the next ordered Action Step when pickup fails. Transitional descriptors follow explicit fallback or terminate the root turn when pickup fails. |
| `DropFacing` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Facing` | `West` | Drops the first carried entity from actor inventory onto the floor in the actor's `Facing` direction; succeeds and consumes the turn when placement succeeds, otherwise falls through. |
| `PushFacing` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Facing` | `West` | Pushes the blocking entity one cell in `Facing`, then moves the actor into the blocker original cell; a successful push consumes the turn. Fails/falls through if there is no blocker or the pushed entity is blocked/out of bounds. |
| `DestroyTarget` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Target` | `Self` | Recursively destroys persistent actor `Target`, including its inventory space and contained entities. The current first pass rejects self-destruction. |
| `CreateFacing` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Facing` | `West` | Creates a placeholder rock-like entity in the actor's `Facing` direction when the destination is valid/open. This is a prototype for future spawning/projectile/clone steps and is expected to evolve. |
| `TurnLeft` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Facing` | `West` | Reads persistent actor `Facing`, writes persistent actor `Facing` 90 degrees counter-clockwise, moves no entity, writes no `Target`, and consumes the turn on success. |
| `TurnRight` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Facing` | `West` | Reads persistent actor `Facing`, writes persistent actor `Facing` 90 degrees clockwise, moves no entity, writes no `Target`, and consumes the turn on success. |
| `ReverseFacing` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Facing` | `West` | Reads persistent actor `Facing`, writes persistent actor `Facing` to the opposite direction, moves no entity, writes no `Target`, and consumes the turn on success. |
| `AcquireNearestTarget` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | none | none | Reads the actor's same-plane position, selects the nearest same-plane entity other than self by Manhattan distance, breaks equal-distance ties by row-major coordinate order (`Y`, then `X`, then entity ID for exact coordinate stability), writes persistent actor `Target`, succeeds without consuming the turn, and continues to the next ordered Action Step when present. If no candidate exists, fails/falls through without writing `Target`. First-pass filter semantics are deliberately small: same plane, entity exists, not self; template-ID allowlisting is not supported because runtime entities do not currently carry template IDs and behavior steps do not currently have parameters. |
| `SeekTarget` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Target` | `Self` | Reads persistent actor `Target`, verifies the target exists, is not self, and is on the actor's current plane, chooses one cardinal step that reduces Manhattan distance, and moves if the destination is open. Direction tie-break is `North`, then `South`, then `West`, then `East`. Successful movement consumes the turn. If the reducing step would enter the adjacent target's occupied cell, or if movement is blocked/out of bounds/off-plane/missing/self, the step fails/falls through while preserving `Target`. |
| `FleeTarget` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Target` | `Self` | Reads persistent actor `Target`, verifies the target exists, is not self, and is on the actor's current plane, evaluates cardinal steps that increase Manhattan distance, and moves to the first valid/open destination using the same direction tie-break as `SeekTarget`: `North`, then `South`, then `West`, then `East`. Successful movement consumes the turn. If no distance-increasing valid/open move exists, or if the target is missing/self/off-plane, the step fails/falls through while preserving `Target`. |
| `MaintainChebyshevDistanceTwo` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Target` | `Self` | Reads persistent actor `Target`, verifies the target exists, is not self, and is on the actor's current plane, computes Chebyshev distance to the target, and moves one valid/open cardinal step toward distance 2. When too close it uses `flee/back-away` mode; when too far it uses `seek/close` mode. Direction tie-break is `North`, then `South`, then `West`, then `East`. Successful movement consumes the turn. At exactly Chebyshev distance 2 it fails/falls through without moving so later linear Action Steps can act at ideal range. If no improving valid/open move exists, or if the target is missing/self/off-plane, the step fails/falls through while preserving `Target`. |
| `StrafeClockwise` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Target` | `Self` | Reads persistent actor `Target`, verifies the target exists, is not self, and is on the actor's current plane, selects the primary seek direction that `SeekTarget` would choose using Manhattan reduction and `North`, `South`, `West`, `East` tie-break, then attempts the clockwise perpendicular cardinal move. Successful movement consumes the turn and preserves `Target`. If no primary seek direction exists, or if the selected strafe destination is blocked/out of bounds/invalid, or if the target is missing/self/off-plane, the step fails/falls through while preserving `Target`. Adjacent targets still produce a primary direction; unlike `SeekTarget`, contact does not by itself fail strafing. |
| `StrafeAnticlockwise` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Target` | `Self` | Same as `StrafeClockwise`, except it attempts the anticlockwise perpendicular cardinal move from the selected primary seek direction. Successful movement consumes the turn; invalid target, missing primary direction, blocked, invalid, or out-of-bounds selected destinations fail/fall through while preserving `Target`. |
| `GiveTarget` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Target` | `Self` | Reads persistent actor `Target`, verifies the target exists and is not self, selects the first entity carried by the actor in row-major source order, and transfers it into the first valid target inventory coordinate in row-major destination order. Successful transfer consumes the turn. Missing/invalid/self target, nothing carried, target without usable inventory, no space, or aperture failure fails/falls through. Runtime transfer diagnostics include entity ID/name and source/destination coordinates; runtime entities do not currently carry template IDs. |
| `TakeTarget` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers | `Target` | `Self` | Reads persistent actor `Target`, verifies the target exists and is not self, selects the first entity carried by the target in row-major source order, and transfers it into the first valid actor inventory coordinate in row-major destination order. Successful transfer consumes the turn. Missing/invalid/self target, target without usable inventory or contents, actor without usable inventory, no space, or aperture failure fails/falls through. Runtime transfer diagnostics include entity ID/name and source/destination coordinates; runtime entities do not currently carry template IDs. |
| `EnterTarget` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers and shared controlled-command frontend service | `Target` | `Self` | Reads persistent actor `Target`, verifies the target exists, is not self, is adjacent, and has usable inventory, then moves the actor into the first valid target inventory coordinate in row-major order. Successful enter consumes the turn. Missing/invalid/self/non-adjacent target, target without usable inventory, no space, or aperture failure fails/falls through. The constrained move checks the destination inventory owner's aperture; target-inventory failures use target-centric trace reasons/details. |
| `ExitFacing` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API/GUI layers and shared controlled-command frontend service | `Facing` | `West` | Reads persistent actor `Facing`, finds the entity whose inventory plane currently contains the actor, and moves the actor to the cell adjacent to that container in `Facing`. Successful exit consumes the turn. Not being in an entity inventory, blocked/out-of-bounds destination, missing container, or aperture failure fails/falls through. The constrained move checks the source/container inventory owner's aperture before placing outside it. |
| `ApplyPrePlan` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API layers; GUI displays authored metadata | target label preferred or target slot default `1`; `planId` field | `Target=Self` | Reads the configured target label or compatibility numeric slot, validates the target exists, and installs the referenced `planId` as the target entity's one-turn `Pre` Action Plan override, replacing any existing pre override. Successful application consumes the actor's turn. Missing target, missing target entity, missing `planId`, or unknown plan fails/falls through. This is intentionally a small plan-reference field, not arbitrary script input. |
| `ApplyMainPlan` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API layers; GUI displays authored metadata | target label preferred or target slot default `1`; `planId` field | `Target=Self` | Same contract as `ApplyPrePlan`, but installs the referenced plan as the target entity's one-turn `Main` override, replacing the target's default main plan for its next turn. |
| `ApplyPostPlan` | Supported as canonical Action Step at Core/descriptor/YAML/validation/editor service/agent API layers; GUI displays authored metadata | target label preferred or target slot default `1`; `planId` field | `Target=Self` | Same contract as `ApplyPrePlan`, but installs the referenced plan as the target entity's one-turn `Post` override, tried after the target's main plan falls through. |
| `BumpTarget` | Conceptualized | `Target` | `Self` | Deferred; may overlap with reaction slots, push, destroy, or future interaction steps. |
| `TeleportTo` | Conceptualized | target location TBD | TBD | Deferred because it likely requires a new location/destination state slot rather than overloading entity `Target`. |

Canonical Action Step metadata is exposed by Core as the machine-readable source for editor/API discovery and validation. Initial metadata includes Action Step kind, display name, description/hint text, required state, defaultable state, state writes, and authoring tier. For a compact outcome matrix and actor/actee/spatial verb decision table, see `docs/Source of Truth/Action-Step-Outcome-And-Affordance-Logic.md`.

Canonical behavior-chain traces can be summarized through `BehaviorChainTraceFormatter`. The compact formatter reports the root plan outcome, each attempted Action Step, success/failure reason, whether fallback continued or stopped, canonical state reads/writes such as `Facing` and `Target`, Backstep preserve-facing results, AcquireNearestTarget selected target/distance/tie-break details, SeekTarget movement/contact/blocking details, FleeTarget escape direction/distance/blocking details, MaintainChebyshevDistanceTwo mode/distance/blocking details, StrafeClockwise/StrafeAnticlockwise primary/strafe direction and blocking details, GiveTarget/TakeTarget transfer details, EnterTarget/ExitFacing containment movement details, and the terminal consumed-turn outcome.

Canonical YAML authoring for the Gate 2 targeting Action Steps uses the same behavior-chain step shape as other canonical steps and has no parameters in the first pass:

```yaml
actionPlans:
  chaseNearest:
    id: chaseNearest
    behavior:
      steps:
        - kind: AcquireNearestTarget
        - kind: SeekTarget
```

`FleeTarget` uses the same parameterless behavior-chain step shape:

```yaml
actionPlans:
  fleeNearest:
    id: fleeNearest
    behavior:
      steps:
        - kind: AcquireNearestTarget
        - kind: FleeTarget
```

`StrafeClockwise` and `StrafeAnticlockwise` use the same parameterless behavior-chain step shape:

```yaml
actionPlans:
  orbitNearest:
    id: orbitNearest
    behavior:
      steps:
        - kind: AcquireNearestTarget
        - kind: StrafeClockwise
        - kind: StrafeAnticlockwise
```

`GiveTarget` and `TakeTarget` use the same parameterless behavior-chain step shape:

```yaml
actionPlans:
  transferWithTarget:
    id: transferWithTarget
    behavior:
      steps:
        - kind: GiveTarget
        - kind: TakeTarget
```

`EnterTarget` and `ExitFacing` use the same parameterless behavior-chain step shape; a common enter chain uses `MoveFacing` to write the blocking entity to `Target`, then `EnterTarget` to enter it. `ExitFacing` uses `Facing` from actor state/defaults and is normally authored on a separate contained-actor behavior because successful enter consumes the current turn:

```yaml
actionPlans:
  enterTarget:
    id: enterTarget
    behavior:
      steps:
        - kind: MoveFacing
        - kind: EnterTarget
  exitFacing:
    id: exitFacing
    behavior:
      steps:
        - kind: ExitFacing
```

`AcquireNearestTarget` currently has no `templateId`, `templateIds`, or filter field. It targets any same-plane non-self entity. Add more selective target filters only when Core runtime/entity metadata and behavior-step parameter descriptors can support them canonically across YAML, validation, editor service, and agent API.

Canonical action-plan previews are exposed through `ContentEditorService.PreviewActionPlan` and `AgentContentEditorApi.PreviewActionPlan`. The preview is non-mutating and reports the selected plan shape, guidance, ordered canonical Action Steps and metadata when present, state hints/defaults, validation diagnostics relevant to the plan/entity, and YAML preview text. Preview commands should remain descriptive and must not introduce new engine semantics.

Behavior-chain validation/default policy:

- editor/API authoring materializes `Facing = West` for assigned entities when adding or assigning `MoveFacing` behavior where possible;
- defaultable `Target = Self` makes `PickupTarget` valid as a first Action Step, even if it is often a no-op/failure until some future or prior interaction sets a more useful target;
- mixed action-plan shapes are invalid for authored content: use only one of canonical `behavior`, transitional `primitive`, or legacy low-level `steps`;
- editing tools should not save empty behavior chains; removing the last behavior Action Step clears the behavior shape;
- if Core encounters an empty behavior chain anyway, it resolves as no turn rather than as a wait or failure action.

## Action-plan checks

| Check | Tier | Engine | Descriptor | YAML | Validation | Editor service | GUI | Notes |
|---|---|---:|---:|---:|---:|---:|---:|---|
| `CanMove` | Stable | Yes | Yes, canonical | Yes | Yes, reads `Facing` | Yes | Yes | Uses canonical `Facing`; no editor-authored variable name required. |
| `BlockingEntity` | Stable | Yes | Yes, canonical | Yes | Yes, reads `Facing`, writes `Target` | Yes | Yes | Produces canonical `Target` for later target-based primitives. |
| `CanPickup` | Stable | Yes | Yes, canonical target + literal coord | Yes | Yes, reads `Target` | Yes | Yes | Uses canonical `Target`; editor authors inventory coordinate only. |
| `CanDrop` | Planned | No | No | No | No | No | No | Deferred. Add only if action plans need explicit branching before `Drop`. |

## Action-plan effects

| Effect | Tier | Engine | Descriptor | YAML | Validation | Editor service | GUI | Notes |
|---|---|---:|---:|---:|---:|---:|---:|---|
| `Wait` | Stable | Yes | Yes | Yes | Yes | Yes | Yes | Consumes a turn through `WaitAction`. |
| `Move` | Stable | Yes | Yes, canonical | Yes | Yes, reads `Facing` | Yes | Yes | Constrained movement primitive: self to adjacent peer/world location derived from canonical `Facing`. Uses relocation internally. |
| `Pickup` | Stable | Yes | Yes, canonical target + literal coord | Yes | Yes, reads `Target` | Yes | Yes | Constrained movement primitive: canonical `Target` to authored carried inventory coordinate. Keeps pickup-specific validation and Bulk/Aperture transition rules. |
| `ReverseDirection` | Stable | Yes | Yes, canonical | Yes | Yes, reads/writes `Facing` | Yes | Yes | GUI authors fixed default turn behavior. Descriptor/runtime retain advanced turn flags. |
| `CallPlan` | Stable | Yes | Yes | Yes | Yes, includes called-plan slot requirements | Yes | Yes | GUI can select called plan. |
| `Teleport` | Advanced | Yes | Yes | Yes | Yes | Yes | Yes | General relocation primitive for arbitrary entity/destination movement. GUI exposes generic movement fields. |
| `Drop` | Advanced | Yes | Yes | Yes | Yes | Yes | Yes | Constrained movement primitive: carried entity to peer/world destination. `CanDrop` intentionally deferred. |
| `SetVariable` | Legacy | Legacy | Legacy | Legacy load | Canonical authoring flags arbitrary variable fields | Legacy/display-only | Display-only | Runtime/deserialization compatibility remains, but canonical workflows should not create it. |

## Actor action-state defaults

Canonical actor action state is persistent entity runtime state. `Facing` and `Target` are stored on the actor's entity action state during execution, while legacy named action-plan variables remain compatibility machinery for older low-level plans.

| State | Tier | Engine context | Content model | YAML | Validation | Editor service | GUI | Notes |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Initial `Facing` | Stable | Yes | Yes | Yes | Yes | Yes | Yes | Canonical YAML is `actionStateDefaults.facing`; spawned actors initialize persistent entity action state. |
| Targeting rules | Stable | Yes | Yes | Yes | Yes | Yes | Display | Template `targetingRules` select nearest same-plane candidates before plan evaluation using a sentence-like shape: label/slot as verb, optional target template as noun, and `targetCapabilities` as adjectives. Supported target capabilities are `PickupTarget`, `EnterTarget`, `GiveTarget`, `TakeTarget`, `DestroyTarget`, and `PushFacing`; they use non-mutating Core affordance checks and do not execute action plans during target refresh. Behavior steps should prefer authored `targetLabel`, with numeric `targetSlot` retained for compatibility/advanced use. Frontend editor snapshots project default action-plan label requirements and orphaned/unused template rules so UIs can treat labels as action-plan-owned and rule target/range values as template-owned. |
| Initial `Target` | Legacy/Advanced | Yes | Yes | Yes | Yes | Partial | Planned | Direct initial target remains model/runtime compatibility. Prefer targeting rules for normal content. |

## Movement primitive model

Movement-like gameplay operations are treated as one movement primitive family.

Accepted direction:

- `Teleport` is the general engine relocation primitive: move any target entity to any valid destination.
- `Move`, `Pickup`, and `Drop` are constrained movement primitives built on shared relocation semantics.
- The editor exposes both friendly constrained primitives and advanced generic `Teleport`.
- The editor should not collapse everything into arbitrary teleport; constrained primitives remain easier for content creators and agents.

Current movement descriptor concepts:

- movement targets: `Self`, `CanonicalTarget`, explicit `Entity`, `CarriedInventoryCoord`;
- movement destinations: explicit `PlaneCoord`, `InventorySlot`, `AdjacentToSelf`, `AdjacentToEntity`, `AdjacentToCanonicalTarget`.

Policy decisions:

- `Teleport` is advanced and arbitrary; it does not enforce constrained Bulk/Aperture inventory transition rules.
- `Pickup` remains the constrained aperture-aware way to move peer/world entities into actor inventory.
- `Drop` validates that the target is carried by the actor and that the destination is on the actor plane.
- `CanDrop` is intentionally deferred until a concrete action-plan branching use case appears.
- Successful directional actor movement reports movement direction; turn execution updates persistent `Facing` after action resolution.

## Turn behavior policy

GUI authoring uses fixed default turn behavior per primitive. Descriptor/runtime support for configurable turn flags remains where it already exists so advanced authoring can be reintroduced deliberately later.

Current policy:

- `ReverseDirection` GUI authoring uses fixed defaults.
- Legacy `SetVariable` may still carry turn flags in old content.
- Do not expose turn flags in the first agent/editor API unless a concrete advanced use case requires them.

## Agent API readiness

The movement primitive parity baseline was sufficient for the first in-process agent API facade. The current API/editor service parity baseline supports canonical ordered behavior-chain authoring for the first Action Steps and the first utility Action Step batch.

Agent API currently has an in-process Content-owned `AgentContentEditorApi` facade over `ContentEditorSession` / `ContentEditorService`. It wraps document/session snapshots, validation, entity template updates, actor initial facing, canonical behavior-chain Action Step metadata and authoring, legacy low-level action plans/steps, transitional primitive-backed linked plans, canonical checks, canonical/advanced supported effects, scenario-root inventory simulation reports, and alpha scenario definition persistence/materialization/player insertion reports without depending on Avalonia view models. Scenario run reports are Content-owned; GIF/PNG scenario recording remains Headless-owned and is available to the Agent API through an optional `IAgentScenarioRecorder` adapter rather than a Content-to-Headless project dependency. The former Console frontend has been removed. The agent API rejects legacy `SetVariable` effect authoring.

Agent API should continue to:

- wrap `ContentEditorService`, not edit YAML/DTOs directly;
- expose stable and advanced supported capabilities through typed commands;
- prefer typed commands for canonical ordered behavior-chain Action Steps for normal movement, pickup, drop, push, destroy, prototype create, target acquisition, and target seeking authoring;
- avoid all legacy variable authoring;
- return structured results and validation diagnostics;
- materialize alpha scenario definitions through the shared materialization service rather than duplicating scenario-root/player setup logic;
- reuse movement target/destination descriptors for `Teleport` and `Drop`;
- keep initial `Target`, `CanDrop`, and advanced turn-flag authoring deferred until concrete use cases appear.

See `docs/Archived/Agent-Editor-API-Plan.md` for the historical implementation plan and next transport/protocol considerations.

## Upcoming behavior-system priorities

Near-term work follows the canonical actions vertical-slice release plan:

1. preserve the current broad Action Step catalog as loadable/runnable/editor-compatible legacy/prototype support while promoted canonical-action status is earned one action at a time;
2. define and track a promoted canonical-action tier whose Definition of Done includes engine semantics, structured outcomes, POV/affordance facts where applicable, frontend log IDs, two content test rooms, editor/API support, and componentized play-mode consumption;
3. promote the first simple action slice, recommended `MoveFacing`, then the first target-facing adjective/ratio slice, recommended `PickupTarget` or `EnterTarget`;
4. add the canonical runtime control-source / Action Choice foundation so any player-controlled actor can choose among its normal authored action steps while fallback-controlled actors keep normal fallback resolution;
5. extend Delta point-of-view facts only where canonical actions, player control, or componentized play-mode consumption need additional stable data.

Behavior templates, scheduler/speed work, reaction slots, diegetic action-plan entities, and broad new gameplay primitives remain conceptualized until selected for a concrete content/design need.
