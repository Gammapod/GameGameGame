---
id: plan.merged-topology-refactor-sprint
title: Merged Topology Refactor Sprint Plan
kind: plan
status: archived
truth_rank: 40
truth_domains: [planning-priority, implementation-navigation, test-trace]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - preparing clean merged inventory topology implementation after the topology spike
  - refactoring topology identity, edge facts, validation helpers, content/editor plumbing, or POV projection seams
related:
  - source.invariants
  - source.testing-charter
  - source.engine-editor-capabilities
  - source.vertical-slice-map
  - plan.topology-service
---

# Merged Topology Refactor Sprint Plan

Status: Archived behavior-preserving refactor sprint plan. This sprint prepared the codebase for a clean merged inventory topology implementation, based on spike findings, without adding merged-layer behavior yet.

The spike findings live in `docs/Archived/Merged-Inventory-Topology-Spike-Findings.md`. The implementation log for the earlier topology service refactor is `docs/Archived/Topology-Service-Phase-1-Sprint-Plan.md`.

## Goal

Create a safer architecture runway for future merged inventory topology by refactoring existing topology, validation, Content/editor plumbing, and visibility projection seams while preserving current runtime/content/frontend behavior.

This sprint intentionally does **not** implement merged inventory layers, semantic joins, overlap-enabled layers, flagship folded-house content, or topology-aware rendering. Those features should be implemented in later clean vertical slices after these seams are in place.

## Non-goals

- Do not add new player-visible topology behavior.
- Do not add new YAML authoring fields for merged topology.
- Do not add `joins`, `cellLinks`, `allowLayoutOverlap`, or flagship folded-house content in this sprint.
- Do not change movement, adjacency, pickup/drop/transfer, Enter/Exit, Action Choice, POV, or SadConsole rendering results.
- Do not implement generated placement, random topology generation, true Möbius transforms, split-cell rendering, or polished topology-aware visualization.

## TDD and behavior-preservation contract

This sprint is planned code work and must follow `docs/Source of Truth/testing-charter.md`.

Because the sprint is behavior-preserving, the required testable outcome for each phase is:

> Existing behavior remains unchanged while the code gains a clearer seam or helper that future merged-topology work can reuse.

Before production edits in each phase, review the traced existing tests below and add focused characterization tests only where current behavior is not already protected. If a refactor accidentally requires changing behavior, stop and revise this plan with an explicit semantic-change trace before continuing.

## Invariant/test trace

Affected invariants from `docs/Source of Truth/invariants.md`:

- `Plain adjacency means eight-way cardinal or intercardinal adjacency unless a contract explicitly says cardinal-only...`
  - Existing tests: `DefaultTopologyReturnsCardinalNeighborAndReportsOutOfBounds`, `DefaultTopologyReturnsUnblockedIntercardinalNeighbor`, `DefaultTopologyReportsTwoCornerIntercardinalBlock`, `DefaultTopologyEnumeratesEightDirectionsInStableOrder`, `AdjacencyAllowsUnblockedIntercardinalNeighbor`, `AdjacencyRejectsIntercardinalNeighborWhenBothCornersAreBlocked`, `PickupRejectsIntercardinalTargetWhenBothCornersAreBlocked`, `ControlledActorAffordanceQueryReportsPickupSourcesAndDestinations`, `ControlledActorAffordanceQueryReportsIntercardinalDropBlockedByTwoCorners`, `ActionChoiceRequestExposesPickupTargetsAndInventoryDestinationsFromAuthoredPickupStep`.
- `Authored topology policy may add directed inventory-boundary adjacency...`
  - Existing tests: `EntityTopologyPolicyConnectsInventoryEdgeOutwardToExteriorAdjacency`, `EntityTopologyPolicyConnectsExteriorAdjacencyInwardToPreferredInventoryEdgeCell`, `EntityTopologyPolicyConnectsIntercardinalExteriorAdjacencyToInventoryCorners`, `EntityTopologyPolicyOutwardAdjacencySupportsPickupAcrossInventoryBoundary`, `ControlledActorAffordanceMovementReportsEntityTopologyOutwardDestination`, `ControlledActorAffordanceMovementReportsEntityTopologyInwardDestinationInsteadOfContainerBump`, `ActionChoiceRequestMoveOptionsExposeEntityTopologyDestinations`, `YamlContentLoaderLoadsEntityEnterAndExitPolicies`, `AgentContentEditorApiAuthorsInventoryBoundaryPolicies`, `SetAndClearTemplateInventoryBoundaryPoliciesMutatesTemplatePolicies`.
- `Traversals through containment or inventory relationships must be cycle-safe.`
  - Existing tests: `ScenarioInventorySummaryFormatterIsCycleSafe`, `EntityContainmentPathServiceDetectsContainmentCycle`, `EntityContainmentPathServiceReportsCycleEdgesWithDirection`, `EntityContainmentPathServiceSharedRootPathIsCycleSafe`.
- `Point-of-view queries for an arbitrary observer entity must reuse cycle-safe containment breadcrumbs...`
  - Existing tests: `PointOfViewUsesContainmentBreadcrumbsAndSelectsNearestContainerAsCurrentPlace`, `PointOfViewReportsMissingObserverDiagnostic`, `PointOfViewReportsNoCurrentPlaceWhenObserverHasNoContainingInventoryOwner`, `PointOfViewPreservesBreadcrumbTruncationFromQueryOptions`.
- `YAML content loads from strings and files into registries that can be validated.`
  - Existing tests: `YamlContentLoaderCreatesRegistryFromDeclarativeContent`, `YamlContentLoaderCanLoadRegistryFromFile`, `PrototypeRegistryValidationPassesForBuiltInContent`.
- `Editable content documents round-trip through materialization and saved YAML.`
  - Existing tests: `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`.
- `Frontend editor snapshots and service-backed template/action-plan mutations expose authored scenarios...`
  - Existing tests: `FrontendEditorServiceTests`, `FrontendEditorServiceAndAgentApiShareContentEditorSessionAsParallelSurfaces`, `AgentContentEditorApiAuthorsCanonicalEnterExitBehavior`, and relevant content tool dispatcher tests.
- `Entity panel projections combine inspection, containment path, point-of-view current-place facts...`
  - Existing tests: `EntityPanelProjectionCombinesIdentityPathStateGridAndContents`, `EntityPanelProjectionIncludesPointOfViewFactsForProjectedEntity`, `ActorPovPlayProjectionComposesCurrentPlaceAndParentChainFromPointOfViewBreadcrumb`, `ActorPovPlayProjectionCarriesPointOfViewDiagnosticsWithoutFrontendGuessing`.

New tests expected during this refactor:

- Prefer characterization tests around extracted seams over new semantic tests.
- Add tests for any new helper that cannot be fully covered by existing behavior tests, especially directional uniqueness helper behavior and DTO conversion round trips.
- If a phase only moves existing code behind an internal type and all traced tests already cover it, record that no new test was needed in the phase log.

## Phase 1: Topology identity and fact vocabulary

### Intent

Introduce stable internal vocabulary for topology facts without changing the existing topology-service outputs.

Candidate concepts:

- `TopologyCellRef` or `TopologyNodeRef`: a frontend-neutral/Core-neutral reference to a traversable cell or node.
- `TopologyEdgeFact`: source, direction, destination, kind, blocked status, failure facts.
- `TopologyNeighborFacts`: canonical wrapper around current `TopologyNeighbor` data if a new type is helpful.

### Constraints

- Existing `ITopologyService.TryGetNeighbor`, `GetNeighbors`, and `EvaluateAdjacency` behavior must remain unchanged.
- Avoid naming these types as merged-inventory-specific.
- Do not expose a stable public API prematurely if `internal` is sufficient.

### Testable outcomes

- Existing Core topology/movement/adjacency tests pass unchanged.
- If new mapping helpers are added, focused tests verify that they preserve existing `TopologyNeighbor` facts exactly.

### Verification

- Targeted: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~CoreAdjacencyTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests"`
- Broader: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"`

## Phase 2: Directional uniqueness helper

### Intent

Extract a reusable helper that can validate the hard future invariant:

> for each resolved `(source cell, direction)`, there may be zero or one destination.

This helper should be independent of merged-layer authoring and suitable for future use by Content validation of semantic joins, source-cell links, seams, and generated topology.

### Constraints

- Do not introduce a new invariant entry yet unless the helper is used by stable behavior.
- Do not change existing topology-policy semantics.
- Prefer a small, deterministic API that reports actionable conflicts.

### Testable outcomes

- Helper accepts duplicate identical edges when that is explicitly allowed by its contract, or rejects all duplicates if that is the selected contract; record the choice in the phase log.
- Helper rejects conflicting destinations for the same `(source, direction)`.
- Existing Content/Core validation remains unchanged.

### Verification

- Targeted helper tests.
- Existing `Suite=Core` and relevant Content validation tests.

## Phase 3: Separate topology identity, authored layout, and rendered/display coordinates

### Intent

Clarify types and naming so future work does not confuse:

- runtime/source cell identity;
- authored layout coordinate;
- rendered/display coordinate;
- physical plane coordinate.

This may be documentation-only plus narrow type aliases/records, or a small refactor in existing projection code if a safe seam exists.

### Constraints

- No behavior changes.
- Do not force all existing `GridCoord` usage through new wrappers at once.
- Add names at boundaries where ambiguity matters most: topology facts, Content authoring DTO conversion, and future projection seams.

### Testable outcomes

- Existing Core/Content/SadConsole tests that consume coordinates pass unchanged.
- Any new coordinate wrapper conversion helper has focused tests.

### Verification

- `Suite=Core`
- `Suite=Content`
- Targeted SadConsole projection tests if frontend projection files are touched.

## Phase 4: Content/editor topology plumbing seam

### Intent

Reduce the “touch ten files for each topology field” friction discovered by the spike before adding merged topology authoring back.

Possible refactors:

- centralize DTO conversion for topology-like content objects;
- isolate editor-service upsert/list snapshot assembly for topology-shaped data;
- define an internal content topology authoring module/mapper even if it only wraps existing topology policy fields initially;
- create a future-ready test fixture pattern for YAML → registry → editable document → editor snapshot → materialization flow.

### Constraints

- Do not add merged topology fields yet.
- Existing YAML, editable document, editor service, agent API, frontend editor snapshot, and materialization behavior must remain unchanged.
- Keep former Avalonia workflows out of scope.

### Testable outcomes

- Existing Content/editor/agent tests pass unchanged.
- New mapper tests, if added, prove round-trip equivalence to current output.

### Verification

- `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"`
- Targeted editor/agent/frontend-service tests touched by the refactor.

## Phase 5: Shared topology visibility/projection seam stub

### Intent

Reserve the correct ownership boundary for future topology-aware rendering without changing SadConsole rendering yet.

Candidate shape:

- `TopologyVisibilityProjectionService` or similarly named frontend-neutral service.
- Inputs: world, observer entity, topology service, optional range, presentation lookup if owned by Content.
- Outputs: visible topology nodes/cells, distance, source `PlaneCoord`, optional occupant/presentation facts, route/path metadata, overlap/conflict diagnostics.

For this refactor sprint, the service may be a minimal stub or adapter over existing POV/current-place facts. The goal is to define the seam and tests, not to implement topology unfolding.

### Constraints

- Do not change SadConsole rendering behavior.
- Do not move frontend layout decisions into Core/Content.
- Do not claim line-of-sight/audibility if not implemented.

### Testable outcomes

- A minimal projection returns honest facts for current existing topology or reports that richer topology visibility is unavailable.
- Existing POV/entity-panel/actor-POV projection tests pass unchanged.

### Verification

- Targeted POV/entity-panel/actor-POV projection tests.
- `Suite=Core` and `Suite=Content` if Core/Content projection code is touched.
- SadConsole tests only if a frontend seam is touched; otherwise SadConsole build is sufficient.

## Expected deliverables

- Internal topology fact/identity vocabulary ready for future source-cell link implementation.
- Reusable directional uniqueness helper with tests.
- Clear naming/types/docs separating topology identity from layout/render coordinates.
- Lower-friction Content/editor plumbing seam for future topology authoring fields.
- A frontend-neutral topology visibility/projection seam stub or documented placeholder.
- Phase logs in this document recording what was refactored, what tests preserved behavior, and any discovered blockers.

## Follow-up after this refactor sprint

Once this sprint is complete, the next clean implementation plan should start with a small behavior-adding vertical slice:

1. Core source-cell links over existing inventory planes.
2. Content semantic aligned joins resolving to source-cell links.
3. Directional uniqueness validation using the extracted helper.
4. One small room-to-hall scenario.

Only after that should overlap-enabled topology, folded-house flagship content, and topology-aware POV rendering return to the active backlog.

## Phase log

Use this section to record completed turns, verification commands, and friction discovered during implementation.

### Phase 1 turn 1: Topology fact vocabulary

- Added behavior-preserving Core topology vocabulary:
  - `TopologyCellRef`, a small wrapper around `PlaneCoord` for topology-node references;
  - `TopologyEdgeFact`, a source-aware edge/neighbor fact with blocked/failure fields and round-trip conversion to/from existing `TopologyNeighbor`.
- Added characterization tests proving `TopologyEdgeFact.FromNeighbor(...).ToNeighbor()` preserves existing default-grid blocked-neighbor facts and directed-overlay unblocked-neighbor facts exactly.
- No production topology services were rewired yet; existing `ITopologyService`, `TopologyNeighbor`, `GetNeighbors`, and `EvaluateAdjacency` behavior remains unchanged. This keeps Phase 1 a vocabulary seam only.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~CoreAdjacencyTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests"` passed: 62 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 352 tests.
- Friction: running the targeted and Core suites in parallel caused a transient `CS2012`/file-lock error on `GameGameGame.Headless.dll`. Sequential rerun passed. Continue avoiding parallel `dotnet test` invocations against the same project graph on Windows.
- Recommended next step: proceed to Phase 2 by extracting a directional uniqueness helper over `TopologyCellRef`/`TopologyEdgeFact`-style source/direction/destination facts, with focused helper tests before any Content validation integration.

### Phase 2 turn 1: Directional uniqueness helper

- Added `TopologyDirectedEdgeFact` as the minimal source/direction/destination input shape for uniqueness validation.
- Added `TopologyDirectionalUniqueness.Validate(...)`, returning `TopologyDirectionalUniquenessResult` and `TopologyDirectionalUniquenessConflict` records.
- Helper contract: duplicate identical edges are idempotent and accepted; conflicting destinations for the same `(source cell, direction)` are rejected and reported. This matches the intended future validation need where multiple authoring paths may resolve to the same edge without being ambiguous, but divergent destinations are always ambiguous.
- Added focused tests proving:
  - unique edges and duplicate identical edges are accepted;
  - conflicting destinations for the same source/direction are rejected with source, direction, first destination, and conflicting destination facts preserved.
- The helper is not integrated into production validation yet, so current runtime/content behavior remains unchanged.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests.TopologyDirectionalUniqueness"` passed: 2 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~CoreAdjacencyTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests"` passed: 64 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 354 tests.
- Friction: none beyond the Phase 1 Windows parallel-test file-lock lesson; tests were run sequentially.
- Recommended next step: proceed to Phase 3 by introducing naming/types/docs that distinguish runtime topology identity from authored layout and rendered/display coordinates, starting at topology fact boundaries rather than broad `GridCoord` replacement.

### Phase 3 turn 1: Coordinate vocabulary boundary

- Renamed `TopologyCellRef`'s positional property from generic `Coord` to `SourceCoord` to make clear that topology identity currently references a concrete runtime/source `PlaneCoord`.
- Added narrow coordinate wrappers:
  - `TopologyLayoutCoord` for authored/resolved topology-layout positions;
  - `TopologyDisplayCoord` for frontend/display-slot positions.
- Added characterization coverage proving the wrappers preserve their underlying coordinates and produce distinct diagnostic strings (`world(1,2)`, `layout(1,2)`, `display(1,2)`).
- Kept the refactor intentionally small: no broad `GridCoord` replacement, no Content DTO changes, and no frontend rendering changes. Existing topology behavior remains unchanged.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests.TopologyCoordinateVocabularyDistinguishesSourceLayoutAndDisplayCoordinates|FullyQualifiedName~TopologyServiceTests.TopologyEdgeFactRoundTrips|FullyQualifiedName~TopologyServiceTests.TopologyDirectionalUniqueness"` passed: 5 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~CoreAdjacencyTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests"` passed: 65 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 355 tests.
- Friction: none. The most important constraint was avoiding an over-broad coordinate-type migration in this phase.
- Recommended next step: proceed to Phase 4 by reducing Content/editor field-sprawl around topology-shaped data through mapper/seam extraction, while still avoiding new merged-topology YAML fields.

### Phase 4 turn 1: Merged-layer document mapper seam

- Extracted `MergedInventoryLayerDocumentMapper` in Content as the shared mapper for the existing merged-layer DTO shape.
- Centralized conversion between:
  - `EditableContentDocument.MergedInventoryLayerDto` / `MergedInventorySpaceContributionDto`;
  - `MergedInventoryLayerDefinition` / `MergedInventorySpaceContribution`.
- Updated `YamlContentLoader` to deserialize `mergedLayers` using the editable-document DTO shape and materialize definitions through the mapper, removing the duplicated private merged-layer DTO classes from the YAML loader.
- Updated `ContentEditorService.UpsertMergedInventoryLayer` to save through the mapper instead of assembling the DTO inline.
- Added characterization coverage for mapper round-trip of the current topology DTO shape, and reran existing YAML/editable/agent merged-layer tests to confirm behavior remained unchanged.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~EditableContentDocumentTests.MergedInventoryLayerDocumentMapperRoundTripsCurrentTopologyDtoShape|FullyQualifiedName~EditableContentDocumentTests.EditableContentDocumentRoundTripsMergedInventoryLayers|FullyQualifiedName~YamlContentLoaderTests.YamlContentLoaderLoadsMergedInventoryLayerPlacements|FullyQualifiedName~AgentContentEditorApiTests.AgentContentEditorApiAuthorsMergedInventoryLayerPlacements"` passed: 4 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 264 tests.
- Friction: direct record equality on `MergedInventoryLayerDefinition` compared the concrete collection instances for `Spaces`, so the mapper test now asserts the ID and structural spaces separately. This is a useful reminder that future DTO/definition mappers should avoid relying on record equality when collection properties are typed as interfaces.
- Recommended next step: proceed to Phase 5 by adding a minimal frontend-neutral topology visibility projection seam/stub that honestly reports current available topology facts without changing SadConsole rendering.

### Phase 5 turn 1: Topology visibility projection seam stub

- Added `TopologyVisibilityProjectionService` in Content as a frontend-neutral projection seam over existing Core topology traversal.
- Added projection DTOs:
  - `TopologyVisibilityProjection`;
  - `TopologyVisibleCellProjection`;
  - `TopologyVisibilityDiagnostic` and `TopologyVisibilityDiagnosticCode`.
- Current behavior is intentionally minimal and honest: it reports depth-limited topology reachability from the observer's current `PlaneCoord`, not line-of-sight or audibility. Every successful projection includes a `LineOfSightNotImplemented` diagnostic so consumers cannot mistake it for final visibility semantics.
- Missing observers and negative depths are reported as diagnostics without frontend guessing.
- No SadConsole rendering behavior was changed; this is only a shared projection boundary for future topology-aware rendering work.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyVisibilityProjection|FullyQualifiedName~ActorPovPlayProjectionTests"` passed: 6 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 355 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 266 tests.
  - `dotnet build src/GameGameGame.SadConsole/GameGameGame.SadConsole.csproj` passed.
- Friction: none. The main design constraint was keeping the projection honest and non-visual: topology reachability only, no claimed sight/audibility and no frontend layout policy.
- Recommended next step: close this refactor sprint and begin a clean behavior-adding merged-topology implementation plan. Start with Core source-cell links and Content aligned joins using the new topology fact vocabulary and directional uniqueness helper; defer overlap mode, flagship content, and richer POV rendering until that smaller slice is stable.
