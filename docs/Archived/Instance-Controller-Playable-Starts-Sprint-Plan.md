---
id: plan.instance-controller-playable-starts-sprint
title: Instance Controller Playable Starts Sprint Plan
kind: plan
status: archived
truth_rank: 55
truth_domains: [planning-priority, implementation-navigation]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - implementing content-authored initial control source
  - adding nested playable starts
  - changing persisted scenario player insertion or player-control compatibility behavior
related:
  - source.invariants
  - source.engine-editor-capabilities
  - source.content-authoring-manual
  - plan.canonical-actions-vertical-slice
---
# Instance Controller Playable Starts Sprint Plan

Status: Archived completed sprint plan. This plan records the selected minimum slice for content-authored initial control source / nested playable starts.

Implementation note: Initial implementation encountered two non-design friction points. First, the content tool host can lock default build outputs, so verification used `dotnet test --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\...`. Second, Core-owner edit permissions do not allow changing `src/GameGameGame.SadConsole`; the Content snapshot keeps the existing non-null `PlayerStart` compatibility property for SadConsole compilation and adds nullable `AuthoredPlayerStart` for new content/editor consumers.

Wrap-up note: the sprint completed instance-level controller authoring, nullable legacy player start handling, playerless scenario launch, and SadConsole inventory-grid controller toggling. It also exposed a larger pre-existing play-loop invariant violation, later completed by the archived initiative-aware PlayerChoice scheduler plan: play/headless simulation now advances initiative and pauses/reports at each `PlayerChoice` actor.

## Sprint target

Make initial playable control an instance-level authored fact on placed entities, not a consequence of scenario-root player insertion.

By the end of this sprint:

1. Every authored placed entity instance can declare a nullable `Controller` metadata property.
2. Supported values are `Player` and `Computer`; missing/null defaults to `Computer`.
3. Materialization initializes entities authored with `Controller: Player` to Core runtime `PlayerChoice` control source.
4. Authored `Controller: Player` instances may be nested in inventory layouts and multiple such instances may exist in one scenario.
5. If at least one authored `Controller: Player` instance exists, legacy player insertion from scenario `PlayerStart` is skipped.
6. If no authored `Controller: Player` exists, legacy player insertion remains as a backup only when legacy player template/entity/start fields are present.
7. If no authored `Controller: Player` exists and no legacy player insertion coordinates are present, the scenario is valid as a playerless scenario.

## Decisions and scope

- `Controller` is instance-level metadata on authored inventory placement / carried-entity instances, not an entity template/class/type property.
- Do not add template-level default controller support in this sprint.
- `Controller: Computer` is an explicit authoring spelling for the existing automatic/fallback control source. It does not introduce a new AI policy.
- Missing/null `Controller` is equivalent to `Computer`.
- `Controller: Player` maps to initial runtime `EntityControlSource.PlayerChoice` during materialization.
- `Controller: Computer` and missing/null map to initial runtime `EntityControlSource.Automatic`.
- Existing scenario `PlayerControls` remains a legacy compatibility input/output surface for now, but new preferred authoring is the placed-entity `Controller` property.
- Scenario `PlayerEntityId` remains observer/default focus/session metadata where applicable; it is not the source of control authority when authored controllers exist.
- Scenario `PlayerStart` / coordinates become truly nullable. Missing coordinates must not silently become `(0,0)`.
- Spacebar or equivalent play-mode turn progression should remain possible in playerless scenarios; SadConsole may own the specific keybinding/presentation, but shared services must not require a controlled entity just to advance automatic simulation.

## Non-goals

- Do not implement possession, runtime controller-changing actions/effects, or control transfer gameplay.
- Do not introduce template-level controller defaults.
- Do not redesign Action Choice menus beyond using existing `PlayerChoice` runtime control source.
- Do not remove legacy scenario player insertion or `PlayerControls` compatibility in this sprint.
- Do not require polished frontend UX for choosing among multiple controlled entities beyond preserving existing playable/session behavior.

## Phase 0: Baseline, compatibility map, and failing tests

Goal: make the current legacy player insertion behavior and new controller-authoring contract explicit before production changes.

Planned work:

1. Identify current DTO/domain/YAML paths for carried entity placement and scenario definitions.
2. Identify all places where missing `PlayerStart` is converted to a default coordinate.
3. Identify materialization paths that derive `PlayerControls` and set runtime `PlayerChoice`.
4. Decide the compatibility precedence in tests before implementation:
   - authored `Controller: Player` wins;
   - legacy insertion is skipped when any authored player controller exists;
   - otherwise legacy insertion is used only when the legacy player template/entity/start tuple is complete;
   - otherwise scenario materializes as playerless.

TDD / invariant trace:

- Affected invariants:
  - Runtime control source is mutable Core actor state and materialization initializes player-controlled entities to `PlayerChoice`.
  - Persisted scenario definitions materialize through the shared content materialization path and resolve player-control bindings before simulation.
  - Content editor operations preserve declared IDs, carried layouts, action-state defaults, validation results, and scenario definitions.
  - Scenario runs use shared Content/Core services and schedule contained actors deterministically for scenario-root inventory spaces.
- Existing tests to preserve/revise:
  - `ScenarioDefinitionsRoundTripAuthoredPlayerControlBindings`
  - `ScenarioMaterializerResolvesAuthoredPlayerControlBindings`
  - `ScenarioMaterializerDefaultsLegacyPlayerControlWhenNoBindingIsAuthored`
  - `ScenarioValidationReportsMissingControlledEntityReferences`
  - `ScenarioValidationReportsInvalidPlayerControlBindingShapes`
  - `ScenarioMaterializerInitializesPlayerControlledEntitiesForChoice`
  - `PlayableScenarioLauncherBuildsFrontendNeutralSessionFromPersistedScenario`
  - `PlayableScenarioLauncherBuildsFreshSessionFromCatalogEntry`
- New intentionally failing tests before implementation:
  - `CarriedEntityControllerRoundTripsThroughEditableDocument`
  - `ScenarioMaterializerInitializesPlacedPlayerControllerEntityForChoice`
  - `ScenarioMaterializerInitializesNestedPlayerControllerEntityForChoice`
  - `ScenarioMaterializerAllowsMultiplePlacedPlayerControllerEntities`
  - `ScenarioMaterializerSkipsLegacyPlayerInsertionWhenPlacedPlayerControllerExists`
  - `ScenarioMaterializerUsesLegacyPlayerInsertionWhenNoPlacedPlayerControllerExists`
  - `ScenarioMaterializerAllowsPlayerlessScenarioWhenNoControllerAndNoPlayerStart`
  - `ScenarioValidationDoesNotDefaultMissingPlayerStartToOrigin`

Exit criteria:

- Tests express the selected compatibility model and fail only because production support is missing.

## Phase 1: Content model, YAML round-trip, and validation

Goal: add the authoring surface without changing Core runtime semantics.

Planned work:

1. Add a nullable controller enum/string field to authored carried entity / inventory placement DTOs and content records.
2. Support YAML load/save round-trip for `controller: Player` and `controller: Computer` while omitting null/missing values where existing style prefers omission.
3. Validate controller values with stable diagnostics for malformed values.
4. Make scenario player coordinates truly nullable in domain/editor-facing models.
5. Update scenario validation so missing coordinates are allowed when no legacy player insertion is requested.
6. Preserve existing validation for occupied/out-of-bounds coordinates when legacy insertion is requested.

Testable outcomes:

- Existing content without `controller` still loads and validates.
- Placed entity controller metadata round-trips through editable documents.
- Missing player coordinates no longer validate as implicit `(0,0)`.

## Phase 2: Materialization precedence and runtime control-source initialization

Goal: make authored placed controllers drive initial runtime control source.

Planned work:

1. During template/entity spawn, propagate placed-instance controller metadata to materialized runtime entity IDs.
2. After scenario root materialization, scan materialized placed entities for `Controller: Player`.
3. Initialize those entities to `EntityControlSource.PlayerChoice`.
4. Leave all other entities `Automatic`.
5. If any placed player controller exists, skip legacy player insertion even if legacy player fields/coordinates are present.
6. If no placed player controller exists, preserve legacy insertion/default control when the complete legacy tuple is present.
7. If no placed player controller exists and the legacy tuple is incomplete/missing, return a valid playerless materialization result.
8. Continue producing compatibility `PlayerControls` output for session/agent consumers where useful, deriving it from authored controller instances or legacy fallback.

Testable outcomes:

- Nested placed player-controlled entities produce Action Choice requests through existing Core services.
- Multiple placed player-controlled entities initialize to `PlayerChoice`.
- Legacy scenarios keep their existing inserted-player behavior.
- Playerless scenarios materialize without setup diagnostics caused by missing player start.

## Phase 3: Editor/API/session parity

Goal: keep authoring and launch surfaces aligned with materialization behavior.

Planned work:

1. Expose placed-entity controller metadata in editor/frontend snapshots and agent inspection APIs.
2. Add service/API mutations for setting/clearing placed-entity controller where current carried-layout mutations live.
3. Update scenario materialization/run/preview reports to distinguish:
   - authored placed player controllers;
   - legacy inserted player fallback;
   - playerless scenario.
4. Ensure playable session launch tolerates no controlled entity / no inserted player location and still returns active scenario plane/session diagnostics.
5. Coordinate with frontend-owner if SadConsole needs a small compatibility change so a playerless scenario can advance automatic turns without a controlled actor.

Testable outcomes:

- Agent/editor APIs can author and inspect placed instance controllers.
- Playable launcher results are valid for authored-controller, legacy-fallback, and playerless scenarios.
- Headless scenario run can progress a playerless scenario with automatic actors.

## Phase 4: Documentation and content proof

Goal: document the preferred authoring model and prove it with small content fixtures.

Planned work:

1. Update `Content-Authoring-Manual.md` scenario authoring guidance to prefer placed-instance `Controller` over legacy player start insertion.
2. Update `Engine-Editor-Capabilities.md` and `invariants.md` after implementation to record the new supported behavior and tests.
3. Add or update focused content/test scenarios demonstrating:
   - nested playable start;
   - multiple playable starts;
   - playerless automatic scenario or explicit no-player scenario.
4. Update roadmap/backlog notes to mark content-authored initial control source / nested playable starts complete or partially complete.

Exit criteria:

- Documentation describes `Controller` as instance-level placement metadata.
- Legacy player insertion is documented as compatibility fallback only.
- Test traces name the coverage for nested playable starts, multiple controllers, legacy fallback, and playerless scenarios.

## Suggested verification

Run targeted tests first, then relevant broader suites:

```powershell
dotnet test tests/GameGameGame.Content.Tests/GameGameGame.Content.Tests.csproj
dotnet test tests/GameGameGame.Core.Tests/GameGameGame.Core.Tests.csproj
dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj
```

If build outputs are locked by a running frontend process, use temporary output/base-intermediate paths under `C:\Users\Scramble\AppData\Local\Temp\opencode`.
