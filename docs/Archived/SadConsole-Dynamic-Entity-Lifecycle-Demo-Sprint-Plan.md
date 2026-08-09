---
id: archived.sadconsole-dynamic-entity-lifecycle-demo-sprint
title: SadConsole Dynamic Entity Lifecycle Demo Sprint Plan
kind: archived
status: archived
truth_rank: 45
truth_domains: [frontend-boundary, test-trace]
owners: [frontend-owner]
audience: [frontend-owner, core-owner, content-editor]
read_when:
  - making the Create/Destroy/Polymorph flagship room demoable in SadConsole
  - changing SadConsole dynamic entity presentation lookup or initiative projection
  - planning frontend support for runtime-created or polymorphed entities
related:
  - source.frontend-ux-invariants
  - source.frontend-ux-standards
  - source.frontend-ux-decisions
  - source.testing-charter
  - archived.create-destroy-polymorph-vertical-slice-sprint
  - source.sadconsole-ui-specification
---

# SadConsole Dynamic Entity Lifecycle Demo Sprint Plan

Status: Archived completed focused frontend sprint plan. This plan made `delta-create-destroy-polymorph-flagship-room` honestly demoable in SadConsole after the Core/Content `CreateEntity`, `DestroyTarget`, and `PolymorphTarget` sprint.

## Goal

SadConsole should display the entity-lifecycle flagship scenario accurately enough for a reviewer to launch it from the manifest and observe:

- the rat king creating rats;
- created rats rendering as rats and joining automatic turns;
- rats walking and backstepping when blocked;
- the snake destroying adjacent rats;
- the lifecycle entity changing visible identity through Egg -> Caterpillar -> Cocoon -> Butterfly -> Egg.

## Boundary principles

- SadConsole must not own Create/Destroy/Polymorph legality, materialization, target selection, action-plan switching, initiative semantics, or trace facts.
- Frontend changes should consume world/runtime facts and shared Content/Core services.
- Frontend tests should focus on presentation/projection seams: lookup freshness, model refresh, fallback diagnostics, and non-crashing rendering behavior.
- Player-triggered Create/Destroy/Polymorph prompts are out of scope; this sprint covers autonomous demo correctness.

## Scope

### In scope

1. World-aware runtime entity presentation lookup for created and polymorphed entities.
2. SadConsole model refresh paths that do not rely on stale launch-time template assignments when runtime `Entity.TemplateId` changes.
3. Dynamic initiative/action-plan projection consumption where SadConsole displays turn order, actor facts, or action descriptors.
4. Safe fallback diagnostics for missing runtime template/presentation/action-plan facts.
5. Manual smoke validation of the flagship scenario in SadConsole.
6. Lightweight SadConsole tests over pure helpers/screen-model seams.

### Out of scope

- Core/Content gameplay semantics, except to record blockers and coordinate with core-owner if a shared service gap appears.
- Player Action Choice support for CreateEntity, DestroyTarget, or PolymorphTarget.
- New animation/effects, polished log wording, or new visual grammar beyond correct glyph/name/color refresh.
- Broad SadConsole layout redesign, connector work, or new mouse interaction.

## Stage 1: Dynamic presentation resolver

### Intent

Ensure every SadConsole path that renders an entity identity can resolve current runtime identity from `WorldState`, so created rats and polymorphed lifecycle entities use the right glyph/color/name instead of stale launch-time registry assignments or `?` fallbacks.

### Implementation plan

1. Inventory current SadConsole presentation lookup call sites.
2. Introduce or reuse one frontend-local adapter around shared Content registry facts, e.g. `RuntimeEntityPresentationResolver`.
3. Prefer `PrototypeContentRegistry.TryGetTemplateIdForEntity(world, entityId, out templateId)` or equivalent shared resolver over launch-time-only `TryGetTemplateIdForEntity(entityId, out ...)`.
4. Apply the adapter to inventory-space components, actor-POV screen models, inspection panels, initiative/local activity rows, and debug simulation paths that render entity glyphs/colors/names.
5. When lookup fails, render a visible diagnostic fallback without throwing and without inventing template semantics.

### TDD trace

Progress:

- Added failing SadConsole regression `ConsumerPlayModeCurrentSpaceUsesRuntimeTemplatePresentationWhenEntityPolymorphs`; it first rendered the stale launch-time egg glyph (`e`) for an entity whose runtime template had changed.
- Applied world-aware template lookup through SadConsole play/model/debug paths by preferring `PrototypeContentRegistry.TryGetTemplateIdForEntity(world, entityId, out ...)` and preserving `?` fallbacks when shared facts are absent.
- Updated the gameplay mock regression for removed registry assignments: when `Entity.TemplateId` is present, the frontend now uses the world/runtime template instead of falling back to `?`.

Affected frontend invariants:

- Frontends do not invent simulation semantics.
- Scenario launch is frontend-neutral.
- Entity glyphs must identify the entity consistently.
- Entity panels remain entity panels.

Existing tests to inspect/extend:

- `InventorySpaceViewModelTests`
- `ConsumerPlayModeScreenTests`
- `ActorPovPlayScreenModelBuilder` / actor-POV play projection tests if a SadConsole suite exists for the builder
- `LocalActivityViewBuilderTests`
- any SadConsole session/view-builder tests that assert glyph/color rows

New or revised tests before production changes:

- `RuntimeEntityPresentationResolverUsesCurrentWorldTemplateIdBeforeLaunchAssignment`
- `RuntimeEntityPresentationResolverFallsBackWithDiagnosticWhenTemplateMissing`
- `InventorySpaceViewModelUsesRuntimeTemplatePresentationForCreatedEntity`
- `InventorySpaceViewModelUsesRuntimeTemplatePresentationAfterPolymorph`

Manual check:

- Launch the flagship scenario and confirm created rats render as rat glyph/color and the lifecycle entity changes glyph/color as it polymorphs.

## Stage 2: Dynamic actor/initiative display refresh

### Intent

Ensure SadConsole display models refresh dynamic actor facts after creation/destruction/polymorph instead of freezing launch-time `Session.ActionPlans` and actor order.

### Implementation plan

1. Inventory SadConsole call sites that cache actor order, automatic action plans, or action-plan descriptors at scenario launch.
2. Prefer shared Content/Core dynamic scenario/session facts where available.
3. If shared session DTOs do not yet expose dynamic actor order/action-plan facts, coordinate with core-owner/content-owner rather than adding frontend-only scheduler semantics.
4. Update SadConsole screen/controller refresh after each automatic turn interval to consume current world/action-plan facts.
5. Remove or demote stale launch-only assumptions from frontend display code while preserving direct movement/Action Choice paths for controlled actors.

### TDD trace

Progress:

- Coordinated with core-owner to promote ScenarioRunService's private dynamic action-plan synchronization into shared Content service `DynamicScenarioActionPlanSynchronizer`.
- Added SadConsole regressions:
  - `GameplaySessionControllerSynchronizesCreatedActorActionPlanAfterAutomaticCreate`
  - `GameplaySessionControllerUsesPolymorphedActionPlanOnNextAutomaticCycle`
- Updated `GameplaySessionController` to maintain a runtime action-plan map, refresh dynamic actor order/action plans before and after automatic advancement, remove destroyed actors, append newly created actors, and preserve explicit session actor-order overrides used by existing tests.
- Updated `ConsumerPlayModeScreen` projections to consume controller `ProjectionActionPlans` instead of stale launch-time `Session.ActionPlans`.

Affected frontend invariants:

- Frontend state is presentation state.
- Player action should converge on shared action contracts.
- Scenario launch is frontend-neutral.
- Frontends do not invent simulation semantics.

Existing tests to inspect/extend:

- `ConsumerPlayModeScreenTests`
- `SadConsoleSessionViewBuilderTests`
- `SadConsolePanelChainViewTests`
- any gameplay/session controller tests under `tests/GameGameGame.SadConsole.Tests`

New or revised tests before production changes:

- `GameplaySessionControllerRefreshesActorOrderAfterCreateEntity`
- `GameplaySessionControllerRefreshesActionPlanAfterPolymorph`
- `PlayScreenModelShowsCreatedActorInInitiativeOrLocalActivityWhenSharedFactsContainIt`
- `PlayScreenModelRemovesDestroyedActorFromDisplayedInitiative`

Manual check:
- In the flagship scenario, verify created rats can appear as local actors after spawn, destroyed rats disappear, and the lifecycle actor's displayed name/action identity follows the current runtime type.

## Stage 3: Lifecycle scenario smoke path and diagnostics

### Intent

Make the manifest scenario launch path robust and reviewer-friendly without adding new gameplay UX.

### Implementation plan

1. Confirm `delta-create-destroy-polymorph-flagship-room` appears in the SadConsole scenario browser under the Delta section.
2. Add debug/status text only where needed to honestly explain missing dynamic facts.
3. Ensure Play and Debug/Simulation routes do not crash when dynamic entities lack optional presentation/provenance facts.
4. Add a short manual smoke checklist to this plan or the final handoff note.

### TDD trace

Progress:

- Added `ScenarioSelectionListsCreateDestroyPolymorphFlagshipRoomFromManifest` to cover manifest/Delta-section discoverability.
- Added `PlayModeDynamicLifecycleScenarioBuildsScreenModelWithoutMissingPresentationCrash` to cover launch-through-manifest and model construction without presentation-related diagnostics.
- Manual smoke result from user review: player-controlled caterpillar transformation is visible, rats are created and destroyed, and the scenario is now honestly demoable in SadConsole.

Affected frontend invariants:

- Contextual controls are visible for the current focus.
- Logs derive from structured outcomes, not parsed display strings.
- Scenario launch is frontend-neutral.

Existing tests to inspect/extend:

- `SadConsoleScenarioSelectionScreenTests`
- `ConsumerPlayModeScreenTests`
- `LocalActivityViewBuilderTests`

New or revised tests before production changes:

- `ScenarioSelectionListsCreateDestroyPolymorphFlagshipRoomFromManifest`
- `PlayModeDynamicLifecycleScenarioBuildsScreenModelWithoutMissingPresentationCrash`
- `LocalActivityRowsRemainHonestWhenLifecycleActionAnchorsAreSparse`

Manual smoke checklist:

1. Start SadConsole against the default Beta manifest.
2. Select `Create Destroy Polymorph Flagship Room` from Delta scenarios.
3. Launch Play and/or Debug Simulation route selected by implementation scope.
4. Let several automatic turns advance.
5. Confirm visible rat creation, rat movement/backstep, snake destruction, and lifecycle glyph/name cycling.
6. Toggle `F12` debug overlay and confirm any diagnostics are honest rather than crashes or silent stale data.

## Stage 4: Documentation and follow-up handoff

### Intent

Record what became demoable, what remains deferred, and which reusable frontend pattern was accepted.

### Implementation plan

1. Update this plan with completion notes and friction.
2. If a reusable resolver/helper becomes accepted, update `Frontend-UX-Decisions.md` or component gallery references only if the pattern is durable beyond this scenario.
3. Update `Frontend-UX-Invariants.md` test trace if a new stable frontend helper suite is added.
4. Record remaining Plan B items, especially player-triggered lifecycle actions and richer logs/animations.

### TDD trace

Affected docs/test traces:

- `Frontend-UX-Invariants.md` row for scenario launch / presentation state, if a new helper suite is added.
- `Frontend-UX-Decisions.md`, only if the world-aware resolver becomes a reusable accepted frontend pattern.

Verification:

- Targeted SadConsole tests for changed seams.
- Full `tests/GameGameGame.SadConsole.Tests`.
- Relevant shared tests only if shared Content/Core DTOs are touched.

## Friction log

- Existing test `FrameDoesNotThrowWhenCurrentPlaceLacksRegistryTemplateAssignment` encoded the old launch-assignment-only fallback. Mitigation: renamed it to `FrameUsesWorldTemplateWhenCurrentPlaceLacksRegistryTemplateAssignment` and changed assertions to the accepted world-aware behavior.
- Stage 2 initially exposed that SadConsole would need to duplicate ScenarioRunService's private dynamic action-plan synchronization. Mitigation: core-owner extracted shared Content-owned `DynamicScenarioActionPlanSynchronizer`, then SadConsole consumed that service.
- Refreshing actor order from shared scenario ordering broke tests that intentionally override `PlayableScenarioSession.ActorOrder` to exercise prompt sequencing. Mitigation: preserve explicit session actor-order entries that still exist, then append dynamic refreshed actors not already listed.

## Verification log

- `dotnet test "tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj" --filter "ConsumerPlayModeCurrentSpaceUsesRuntimeTemplatePresentationWhenEntityPolymorphs"` -> passed.
- `dotnet test "tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj"` -> 332 passed.
- `dotnet test "tests/GameGameGame.Tests/GameGameGame.Tests.csproj"; if ($?) { dotnet test "tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj" }` -> 627 shared tests passed, 332 SadConsole tests passed.
- `dotnet test "tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj" --filter "GameplaySessionControllerSynchronizesCreatedActorActionPlanAfterAutomaticCreate|GameplaySessionControllerUsesPolymorphedActionPlanOnNextAutomaticCycle"` -> passed.
- `dotnet test "tests/GameGameGame.Tests/GameGameGame.Tests.csproj"; if ($?) { dotnet test "tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj" }` -> 628 shared tests passed, 334 SadConsole tests passed.
- `dotnet test "tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj" --filter "ScenarioSelectionListsCreateDestroyPolymorphFlagshipRoomFromManifest|PlayModeDynamicLifecycleScenarioBuildsScreenModelWithoutMissingPresentationCrash"` -> passed.
- `dotnet test "tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj"` -> 336 passed.

## Exit criteria

- The flagship scenario launches from the SadConsole manifest path selected for this sprint.
- Created rats render with rat identity and do not crash presentation lookup.
- Polymorphed lifecycle entity renders current identity after each morph.
- Created/destroyed/polymorphed actor displays refresh from shared facts rather than stale launch-time assumptions.
- Tests cover the stable pure frontend seams added or changed.
- Any remaining SadConsole limitations are documented as follow-up, not hidden.
