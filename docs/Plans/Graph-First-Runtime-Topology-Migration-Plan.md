---
id: plan.graph-first-runtime-topology-migration
title: Graph-First Runtime Topology Migration Plan
kind: plan
status: active
truth_rank: 40
truth_domains: [planning-priority, implementation-navigation, test-trace]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - migrating runtime topology from coordinate-primary to graph-primary behavior
  - changing Core movement adjacency pathing visibility or topology materialization
  - authoring overlap/folded topology support without Core coordinate special cases
related:
  - source.invariants
  - source.testing-charter
  - source.engine-editor-capabilities
  - source.vertical-slice-map
  - plan.merged-topology-clean-implementation-sprint
---

# Graph-First Runtime Topology Migration Plan

Status: Active architecture/refactor sprint plan. This plan commits runtime Core topology toward graph-first identity and treats coordinates as authoring/layout/display/debug projections rather than the primary simulation topology source.

## Decision

Runtime gameplay topology should be graph-first.

- Runtime locations should be identified by graph/topology nodes.
- Runtime movement, adjacency, pathing, topology visibility/reachability, Action Choice movement facts, and adjacency-based interactions should query graph edges.
- Coordinates should remain available as projections for content authoring, inventory-grid policies, reports, debugging, editor summaries, and frontend rendering, but should not be authoritative for runtime topology.
- Legacy coordinate-only gameplay behavior should be rewritten over graph traversal/metrics or retired.
- Overlap/folded topology should not require special runtime coordinate conflict handling. Distinct source cells may share layout/display coordinates because they remain distinct graph nodes.

## Goals

1. Introduce explicit runtime topology node identity and graph edge materialization.
2. Make existing topology services graph-backed while preserving compatibility APIs during migration.
3. Migrate movement/actions/affordances/pathing/visibility to graph-node-first semantics.
4. Remove coordinate-special-case runtime topology logic as each graph-backed replacement lands.
5. Enable overlap/folded topology as graph data plus projection metadata, not Core movement special cases.
6. Promote graph-native v2-style Core/Content/Editor APIs and phase out coordinate-facing compatibility adapters once callers move.

## Non-goals for the first migration sprint

- Do not do a big-bang removal of all `PlaneCoord` APIs.
- Do not require frontend polished folded-topology visualization before Core can represent graph topology.
- Do not implement full folded-house/Möbius/one-way/sheet/facing-transform semantics in the first graph materialization slice.
- Do not preserve legacy coordinate-only action behavior indefinitely if it conflicts with graph-first runtime semantics.

## TDD and behavior-change contract

This is planned semantic Core/Content/Editor work and must follow `docs/Source of Truth/testing-charter.md`.

For every phase below:

1. Confirm the phase has at least one testable outcome.
2. Use the phase-specific invariant/test trace before production edits.
3. Add or revise intentionally failing tests first.
4. Implement the smallest subtractive/coordinated change that makes those tests pass.
5. Run targeted tests plus the relevant broader suite.
6. Record verification and any deleted/reduced complexity in the phase log.

## Global invariant trace

Affected invariants from `docs/Source of Truth/invariants.md`:

- `Entity locations are represented by occupancy of nodes in planes.`
  - Existing tests: `EntityLocationsAreRepresentedByNodeOccupancy`, `MovementCannotPlaceEntityOnOccupiedNode`.
  - Migration interpretation: runtime occupancy should move toward topology graph nodes while retaining coordinate projections for compatibility/reporting until the invariant is updated.
- `At most one entity may occupy a node at a time.`
  - Existing tests: `MovementCannotPlaceEntityOnOccupiedNode`, `PrototypeRegistryValidationReportsOverlappingCarriedEntities`.
- `Plain adjacency means eight-way cardinal or intercardinal adjacency unless a contract explicitly says cardinal-only...`
  - Existing tests: `TopologyGraphReturnsCardinalNeighborAndReportsOutOfBounds`, `TopologyGraphReturnsUnblockedIntercardinalNeighbor`, `TopologyGraphReportsTwoCornerIntercardinalBlock`, `TopologyGraphEnumeratesEightDirectionsInStableOrder`, `AdjacencyAllowsUnblockedIntercardinalNeighbor`, `AdjacencyRejectsIntercardinalNeighborWhenBothCornersAreBlocked`.
- `Authored topology policy may add directed inventory-boundary adjacency... Merged inventory aligned joins may add explicit source-cell links between owner inventory cells; each resolved (source cell, direction) must have zero or one destination.`
  - Existing tests: `EntityTopologyPolicyConnectsInventoryEdgeOutwardToExteriorAdjacency`, `EntityTopologyPolicyConnectsExteriorAdjacencyInwardToPreferredInventoryEdgeCell`, `EntityTopologyPolicyConnectsIntercardinalExteriorAdjacencyToInventoryCorners`, `ControlledActorAffordanceMovementReportsEntityTopologyOutwardDestination`, `ControlledActorAffordanceMovementReportsEntityTopologyInwardDestinationInsteadOfContainerBump`, `ActionChoiceRequestMoveOptionsExposeEntityTopologyDestinations`, `SourceCellLinksConnectAuthoredInventoryCellsBidirectionally`, `YamlContentLoaderLoadsAlignedMergedLayerJoin`, `ContentValidationRejectsMergedLayerJoinDirectionalConflict`, `ScenarioMaterializerResolvesAlignedMergedLayerJoinsToSourceCellLinks`.
- `Target-path movement distance-band behavior must use legal movement topology...`
  - Existing tests: `TargetPathMoveFleeChoosesIncreasingPathDistanceFromAdjacency`, `TargetPathMoveMaintainDistanceSeeksWhenTooFar`, `TargetPathMoveMaintainDistanceFleesWhenTooClose`, `TargetPathMoveOrbitClockwiseFollowsDeterministicRing`, `TargetPathMoveOrbitAnticlockwiseFollowsDeterministicRing`, `TargetPathMoveOrbitFollowsOctagonalDistanceBandsAroundCorners`, `TargetPathMoveOrbitCorrectsToDesiredDistanceBeforeOrbiting`, `TargetPathMoveOrbitFallsThroughWhenNextRingStepIsBlocked`.
- `Controlled actor commands for direct player/frontend input resolve through a shared Core service...`
  - Existing tests: `ControlledActorCommandMoveReturnsStructuredSuccessAndAdvancesTurn`, `ControlledActorCommandFailedMoveRecordsFailureWithoutAdvancingTurn`, `ControlledActorCommandPickupReportsTargetAndDestinationAnchors`, `ControlledActorCommandPushMovesTargetAndAdvancesTurn`.
- `Controlled actor affordance queries... expose Core-derived move, pickup, drop, enter, exit, transfer, and push choices...`
  - Existing tests: `ControlledActorAffordanceQueryReportsValidAndBlockedMovementDirections`, `ControlledActorAffordanceQueryReportsPickupSourcesAndDestinations`, `ControlledActorAffordanceQueryReportsDropSourcesAndBlockedDropDestinations`, `ControlledActorAffordanceQueryReportsEnterTargetsAndExitDirections`, `ControlledActorAffordanceExitDirectionsUseTopologyNeighborDestinations`.
- Content pipeline/editor invariants for authored topology support.
  - Existing tests: `YamlContentLoaderLoadsMergedInventoryLayerPlacements`, `YamlContentLoaderLoadsAlignedMergedLayerJoin`, `ContentValidationRejectsMergedLayerJoinDirectionalConflict`, `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`, `MergedInventoryLayerDocumentMapperRoundTripsCurrentTopologyDtoShape`, `AgentContentEditorApiAuthorsMergedInventoryLayerPlacements`, `SnapshotIncludesMergedInventoryLayerJoinSummaries`.

## Phase 1: graph identity and materialization seam

### Intent

Introduce runtime topology graph vocabulary without changing existing gameplay behavior.

Candidate names may change during implementation:

- `TopologyNodeId`
- `TopologyNode`
- `TopologyGraph`
- `TopologyGraphEdge`
- `TopologyGraphMaterializer`

### Testable outcomes

- Existing plane/inventory cells materialize into distinct topology graph nodes with source-coordinate projections.
- Two source cells that may share a layout/display coordinate remain distinct topology nodes.
- Existing default grid neighbor facts can be represented as graph edges without changing runtime movement/adjacency behavior.
- Existing source-cell links materialize as graph edges.

### Invariant/test trace

- Affected invariants: entity locations as nodes, one occupant per node, plain adjacency, directed/source-cell topology uniqueness.
- Existing tests to preserve/review: `EntityLocationsAreRepresentedByNodeOccupancy`, `MovementCannotPlaceEntityOnOccupiedNode`, `DefaultTopologyReturnsCardinalNeighborAndReportsOutOfBounds`, `DefaultTopologyEnumeratesEightDirectionsInStableOrder`, `SourceCellLinksConnectAuthoredInventoryCellsBidirectionally`, `TopologyDirectionalUniquenessAcceptsUniqueAndDuplicateIdenticalEdges`, `TopologyDirectionalUniquenessRejectsConflictingDestinationsForSameCellAndDirection`.
- New failing tests first:
  1. graph materialization distinguishes source nodes even when layout/display projection collides;
  2. graph materialization emits default grid edges equivalent to current runtime neighbor facts;
  3. graph materialization emits source-cell-link edges equivalent to current source-cell links.

### Subtractive target

Centralize topology edge production so later phases can delete per-service coordinate-special-case logic.

### Verification

- Focused graph materialization tests.
- `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~CoreAdjacencyTests"`
- `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"`

## Phase 2: graph-backed topology service compatibility layer

### Intent

Make `ITopologyService` query graph edges internally while preserving current `PlaneCoord`-based method signatures for compatibility.

### Testable outcomes

- `TryGetNeighbor`, `GetNeighbors`, and `EvaluateAdjacency` return the same results as today for default grid, entity topology policy, merged-layer layout adjacency, and source-cell links.
- A graph node with a source-cell link resolves through graph data rather than through a separate source-cell-link topology wrapper.
- Directional uniqueness is enforced by graph edge materialization or validation before runtime graph use.

### Invariant/test trace

- Affected invariants: plain adjacency; authored topology policy/source-cell links; controlled actor movement/affordance/action-choice topology facts.
- Existing tests to preserve/review: `DefaultTopologyReturnsCardinalNeighborAndReportsOutOfBounds`, `DefaultTopologyReportsTwoCornerIntercardinalBlock`, `MergedInventoryLayerConnectsTwoPlacedInventorySpacesAsOneTopology`, `SourceCellLinksConnectAuthoredInventoryCellsBidirectionally`, `EntityTopologyPolicyConnectsInventoryEdgeOutwardToExteriorAdjacency`, `EntityTopologyPolicyConnectsExteriorAdjacencyInwardToPreferredInventoryEdgeCell`, `ControlledActorAffordanceMovementReportsEntityTopologyOutwardDestination`, `ActionChoiceRequestMoveOptionsExposeEntityTopologyDestinations`.
- New/revised failing tests first:
  1. graph-backed compatibility `TryGetNeighbor` resolves source-cell links with no source-link wrapper special case;
  2. graph-backed compatibility `EvaluateAdjacency` reports conflicts/blocking through graph edge facts;
  3. existing topology service composition can be reduced without behavior loss.

### Subtractive target

Delete or collapse at least one topology wrapper/special-case path after graph-backed parity is proven, preferably `SourceCellLinkTopologyService` as an independent runtime wrapper.

### Verification

- Targeted topology/affordance/action-choice tests.
- `Suite=Core`.

## Phase 3: graph-node-first movement service

### Intent

Move `MovementService` from coordinate-primary to graph-node-primary semantics, keeping coordinate wrappers temporarily.

### Testable outcomes

- Movement can be evaluated and executed by graph node identity.
- Occupancy can be checked by graph node identity while preserving coordinate-facing compatibility assertions.
- Existing coordinate movement APIs are thin wrappers over graph-node operations.
- Moving across aligned joins/source-cell links does not require merged-layer/source-cell special cases in `MovementService`.

### Invariant/test trace

- Affected invariants: entity locations/occupancy nodes, one occupant per node, movement controlled commands, topology adjacency.
- Existing tests to preserve/review: `EntityLocationsAreRepresentedByNodeOccupancy`, `MovementCannotPlaceEntityOnOccupiedNode`, `ControlledActorCommandMoveReturnsStructuredSuccessAndAdvancesTurn`, `ControlledActorCommandFailedMoveRecordsFailureWithoutAdvancingTurn`, `SourceCellLinksConnectAuthoredInventoryCellsBidirectionally`, `RoomHallAlignedJoinShowcaseLoadsValidatesAndMaterializesSourceCellLink`.
- New/revised failing tests first:
  1. graph-node move changes entity occupancy from one topology node to another;
  2. compatibility coordinate move delegates to graph-node move;
  3. overlapping layout projections do not collapse occupancy because graph nodes are distinct.

### Subtractive target

Reduce duplicate `PlaneCoord` relocation/destination logic in `MovementService`; coordinate APIs should become adapters.

### Verification

- `FullyQualifiedName~Movement|FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~ControlledActorCommandServiceTests` as applicable.
- `Suite=Core`.

## Phase 4: graph-native action and affordance transitions

### Intent

Migrate promoted gameplay actions and player-facing query services to graph-node transitions.

### Testable outcomes

- `Move`, `Pickup`, `Drop`, `Transfer`, `Enter`, `Exit`, and `Push` resolve adjacency/destinations through graph nodes.
- `ControlledActorAffordanceService`, `ActionChoiceService`, and `ControlledActorCommandService` consume graph-node facts and only project coordinates for DTO/debug compatibility.
- Bulk/Aperture and Enter/Exit policies remain authoritative but operate on graph-node destinations with coordinate projections only where policy semantics explicitly require inventory coordinates.

### Invariant/test trace

- Affected invariants: constrained inventory transitions, controlled commands, controlled affordances, Action Choice, directed topology/source links.
- Existing tests to preserve/review: `PickupFailsWhenTargetBulkExceedsAperture`, `DropFacingUsesApertureTransitionRules`, `EnterTargetFailsWhenActorBulkExceedsTargetAperture`, `ExitFacingFailsWhenActorBulkExceedsContainerAperture`, `CanonicalTransferActorToTargetUsesSelectedMovingEntityAndFacingCounterparty`, `ControlledActorAffordanceQueryReportsPickupSourcesAndDestinations`, `ControlledActorAffordanceExitDirectionsUseTopologyNeighborDestinations`, `ActionChoiceRequestExposesDropDestinationsFromTopologyNeighbors`, `ActionChoiceRequestExposesTransferCounterpartiesFromTopologyNeighbors`, `SubmitMoveChoiceSuccessAdvancesAndSetsFacing`, `SubmitTransferChoiceUsesTopologyDirectionForCounterparty`.
- New/revised failing tests first:
  1. pickup/enter/transfer adjacency uses graph edge adjacency when coordinate adjacency would disagree;
  2. Action Choice exposes graph-edge destinations with coordinate projections;
  3. Exit/drop destination resolution follows graph edges before coordinate offsets.

### Subtractive target

Delete duplicated coordinate adjacency checks in action/affordance services once graph-node checks own the behavior.

### Verification

- Targeted action/affordance/action-choice/command tests.
- `Suite=Core`.

## Phase 5: graph traversal, pathing, and visibility

### Intent

Make graph traversal the only promoted path/reachability traversal mechanism.

### Testable outcomes

- `TopologyTraversalService` traverses graph nodes directly.
- `TargetPathMove` pathfinding uses graph neighbors and graph distance/cost facts.
- `TopologyVisibilityProjectionService` consumes graph traversal and reports graph-node/source-coordinate projections without claiming line of sight.
- Coordinate/layout overlap does not affect graph reachability.

### Invariant/test trace

- Affected invariants: target-path movement topology, topology visibility projection seam, source-cell uniqueness.
- Existing tests to preserve/review: `TopologicalFloodFollowsDirectedOverlayWithoutUsingCoordinateDistance`, `TopologicalFloodDoesNotRevisitNodesThroughCycles`, `TargetPathMoveFleeChoosesIncreasingPathDistanceFromAdjacency`, `TargetPathMoveMaintainDistanceSeeksWhenTooFar`, `TargetPathMoveOrbitFollowsOctagonalDistanceBandsAroundCorners`, `TopologyVisibilityProjectionReportsDepthLimitedReachabilityWithoutClaimingLineOfSight`.
- New/revised failing tests first:
  1. graph traversal reaches nodes through graph edges that have no coordinate adjacency;
  2. target-path movement can path through source-cell graph links;
  3. topology visibility projection reports distinct graph nodes even when layout projections overlap.

### Subtractive target

Remove traversal/pathfinding code paths that walk raw layout coordinates when a graph traversal should be used.

### Verification

- Targeted traversal/pathing/visibility tests.
- `Suite=Core` and relevant `Suite=Content` if projections change.

## Phase 6: retire or rewrite legacy coordinate movement actions

### Intent

Remove runtime reliance on same-plane Manhattan/Chebyshev coordinate movement for legacy target movement behaviors.

### Candidate legacy behaviors

- `SeekTarget`
- `FleeTarget`
- `MaintainChebyshevDistanceTwo`
- `StrafeClockwise`
- `StrafeAnticlockwise`

### Testable outcomes

- Each behavior is either rewritten over graph traversal/graph metrics or removed from new authoring/runtime promotion.
- Any remaining coordinate-projection metric is explicit and documented as a projection metric, not default topology.
- Content validation rejects removed legacy coordinate-only behavior in new authored content or maps it to graph-native alternatives.

### Invariant/test trace

- Affected invariants: canonical Action Step state contracts, target-path movement distance-band behavior, content Action Plan shape/catalog contracts.
- Existing tests to preserve/review: legacy behavior tests named for `AcquireNearestTarget`, `SeekTarget`, `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, `StrafeAnticlockwise`; target-path movement tests; `PlanPrimitiveCatalogExposesAllCheckEffectAndValueKinds`; `ContentEditorListsCanonicalActionStepMetadata`; `PrototypeRegistryValidationReportsMixedActionPlanShapes`.
- New/revised failing tests first:
  1. removed coordinate-only actions are no longer exposed for new canonical authoring;
  2. graph-native replacement behavior uses graph distance/reachability when coordinates disagree;
  3. compatibility content either still loads with explicit legacy status or receives actionable validation diagnostics.

### Subtractive target

Delete coordinate-only movement handlers or reduce them to compatibility shims behind graph-native target-path behavior.

### Verification

- Targeted action-step catalog/content validation/action-plan interpreter tests.
- `Suite=Core` and `Suite=Content`.

## Phase 7: overlap/folded topology as ordinary graph data

### Intent

Enable overlapping/folded topology without Core movement special cases by materializing distinct graph nodes and explicit graph edges.

### Testable outcomes

- Content can author two or more cells with identical layout/display projections while graph node identity remains distinct.
- Movement/traversal/pathing follows graph edges and never collapses nodes by overlapping layout coordinate.
- Validation reports conflicts in authored graph edges, not overlap as a runtime movement ambiguity.
- A compact overlap-loop scenario can be authored as graph data.

### Invariant/test trace

- Affected invariants: source-cell directional uniqueness, occupancy node uniqueness, content YAML validation/materialization, scenario run/report behavior.
- Existing tests to preserve/review: `ContentValidationRejectsMergedLayerOverlapDisconnectedOrInvalidOwner`, `ContentValidationRejectsMergedLayerJoinDirectionalConflict`, `RoomHallAlignedJoinShowcaseLoadsValidatesAndMaterializesSourceCellLink`, scenario catalog/manifest validation tests.
- New/revised failing tests first:
  1. overlap-enabled content materializes distinct graph nodes with same layout projection;
  2. overlap-loop movement traverses explicit graph edges back to start;
  3. frontend/content projection exposes enough metadata for display without Core resolving by layout coordinate.

### Subtractive target

Remove merged-layer runtime layout-overlap special cases from Core. Overlap is projection metadata plus graph identity, not a topology resolver branch.

### Verification

- Targeted overlap-loop topology/content/scenario tests.
- `Suite=Core` and `Suite=Content`.

## Phase 8: graph-native v2 API consolidation

Status: Complete as of 2026-08-09. Remaining coordinate-returning Core APIs are compatibility/projection adapters, not Phase 8 blockers.

### Intent

Introduce canonical graph-native APIs at the Core/Content/Editor boundary and deliberately phase out coordinate-facing compatibility surfaces now that runtime topology services have been consolidated into `TopologyGraphMaterializer` plus graph traversal/query consumers.

This phase should not create broad duplicate `V2` copies of every service. It should add graph-native method/result types where the old coordinate-first names now hide graph semantics, migrate callers slice-by-slice, and then delete compatibility adapters when no call sites remain.

### Testable outcomes

- Movement exposes graph-edge/destination result APIs whose identity is `TopologyNodeId`; source/destination coordinates are projections.
- Core action choice, controlled command, controlled affordance, and target-path movement callers consume graph-native movement/adjacency facts before projecting old DTO coordinate fields.
- Content/editor/frontend-neutral projections that expose topology choices include node IDs as identity and coordinates/layout as source/display metadata.
- Existing coordinate-returning APIs (`TryGetMoveDestination`, coordinate `EvaluateAdjacency`, coordinate destination DTO fields) are either proven thin adapters over graph-native APIs or deleted once migrated.

### Invariant/test trace

- Affected invariants: entity location/occupancy node identity, one occupant per node, topology/source-cell directional uniqueness, controlled command/affordance/action-choice topology facts, target-path movement topology distance behavior, Content/editor authored topology projection contracts.
- Existing tests to preserve/review:
  - Core movement/graph: `MovementCanPlaceByTopologyNodeId`, `MovementMovesByTopologyNodeId`, `CoordinateMoveDelegatesThroughGraphNodeDestination`, `MovementAcrossOverlappingLayoutProjectionKeepsDistinctTopologyNodes`, `TopologyGraphTraversalTests`, `TopologyGraphMaterializerTests`.
  - Action/command/affordance: `ControlledActorCommandMoveReturnsStructuredSuccessAndAdvancesTurn`, `ControlledActorCommandMoveReportsSourceAndDestinationTopologyNodes`, `ControlledActorAffordanceQueryReportsValidAndBlockedMovementDirections`, `ControlledActorAffordanceExitDirectionsUseTopologyNeighborDestinations`, `ActionChoiceRequestMoveOptionsExposeEntityTopologyDestinations`, `ActionChoiceRequestExposesDropDestinationsFromTopologyNeighbors`, `ActionChoiceRequestExposesTransferCounterpartiesFromTopologyNeighbors`.
  - Target/path/content/editor: `TargetPathMovementActionStepTests`, `YamlContentLoaderLoadsAlignedMergedLayerJoin`, `ContentValidationRejectsMergedLayerJoinDirectionalConflict`, `SnapshotIncludesMergedInventoryLayerJoinSummaries`, topology visibility/POV projection tests.
- New/revised failing tests first:
  1. graph-native movement edge query returns source node, destination node, edge kind, blocked/failure facts, and source/layout/display projections without requiring coordinate identity;
  2. Action Choice/controlled affordance DTO construction uses graph-native destination/counterparty facts and only fills coordinate fields as projections;
  3. coordinate compatibility APIs return the same projected destination/adjacency facts as graph-native APIs and are marked as adapters in tests/docs;
  4. at least one old coordinate-primary caller is deleted or rewritten to graph-native calls in the same slice that introduces the v2 surface.

### Subtractive target

Delete or narrow coordinate-facing movement/adjacency compatibility APIs after their callers move. Avoid reintroducing general topology service injection or coordinate-primary topology wrappers. Prefer graph-native result names over blanket `V2` suffixes unless an external compatibility seam requires version naming.

### Verification

- Targeted movement/action-choice/command/affordance/target-path/topology projection tests for each migrated slice.
- `Suite=Core` for Core API migration slices.
- `Suite=Content` and documentation lint when Content/editor/frontend-neutral DTOs or capability docs change.

## Cross-phase cleanup rules

- Prefer replacing coordinate-special-case branches with graph materialization or graph queries.
- Keep compatibility wrappers only with explicit migration notes.
- Each phase log entry should say what complexity was removed or narrowed.
- Do not add new coordinate-primary gameplay behavior.
- If a behavior truly needs coordinates, name the projection being consumed: source coordinate, inventory coordinate, layout coordinate, or display coordinate.

## Open design questions

- Should graph node IDs be stable authored/source IDs, generated runtime IDs, or a composite source-cell identity?
- Should occupancy move directly to topology node IDs early, or should `NodeId` become/alias topology node identity first?
- How should graph distance metrics distinguish cardinal/intercardinal costs, authored edge costs, and projection-distance metrics?
- What is the minimum debug projection needed so graph-first traces remain readable?
- Which legacy coordinate actions should be removed versus rewritten?

## Phase log

Use this section to record completed turns, failing-test evidence, verification commands, friction, and subtractive cleanup completed in each phase.

### 2026-08-08 - Phase 1 TDD red step

- Added `TopologyGraphMaterializerTests` as the first executable expression of Phase 1 graph materialization behavior.
- Covered all Phase 1 new-test commitments: distinct topology nodes for colliding layout projections, default-grid edge materialization equivalent to `DefaultTopologyService`, and source-cell-link edge materialization.
- Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyGraphMaterializerTests|FullyQualifiedName~TopologyServiceTests"` fails because `TopologyGraphMaterializer` does not exist yet (`CS0103`), which is the expected red state before introducing the production graph materialization seam.
- Friction/blockers: none.

### 2026-08-08 - Phase 1 graph vocabulary/materializer green step

- Added the first runtime graph vocabulary in Core: `TopologyNodeId`, `TopologyNode`, `TopologyGraphEdge`, `TopologyGraph`, and `TopologyGraphMaterializer`.
- `TopologyGraphMaterializer.Materialize` now creates graph nodes for existing world nodes, preserves source coordinates, projects merged-inventory layout/display coordinates, emits default-grid graph edges for valid destination nodes, and emits bidirectional source-cell-link edges.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyGraphMaterializerTests|FullyQualifiedName~TopologyServiceTests"` passed: 31 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 359 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: none yet; this turn introduced the graph materialization seam needed before wrapper/special-case reduction in later Phase 1/Phase 2 work.
- Friction/blockers: none.

### 2026-08-08 - Phase 1 graph directed-edge fact projection

- Added a red test proving materialized graph edges can project to `TopologyDirectedEdgeFact` values for reuse by existing directional-uniqueness validation.
- Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyGraphMaterializerTests"` failed because `TopologyGraph.ToDirectedEdgeFacts` did not exist (`CS1061`).
- Implemented `TopologyGraph.ToDirectedEdgeFacts`, projecting unblocked graph edges back to source/destination `TopologyCellRef` facts.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyGraphMaterializerTests|FullyQualifiedName~TopologyServiceTests"` passed: 32 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 360 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: none yet; this keeps Phase 1 behavior-preserving while making existing uniqueness validation reusable over materialized graph output.
- Friction/blockers: none.

### 2026-08-08 - Phase 1 graph neighbor lookup seam

- Added a red test proving a materialized graph can reconstruct `TopologyNeighbor` facts for a source cell and direction, including default-grid success and out-of-bounds failure facts.
- Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyGraphMaterializerTests"` failed because `TopologyGraph.TryGetNeighbor` did not exist (`CS1061`).
- Implemented `TopologyGraph.TryGetNeighbor(TopologyCellRef, Direction, out TopologyNeighbor)` as a behavior-preserving lookup over materialized graph edges, with default-grid out-of-bounds fallback for directions that have no valid destination node.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyGraphMaterializerTests|FullyQualifiedName~TopologyServiceTests"` passed: 33 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 361 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Phase 1 status: complete enough to start Phase 2. The graph vocabulary/materialization seam now covers distinct source-node identity, layout/display projection, default-grid edges, source-cell-link edges, directed-edge fact projection, and neighbor-fact lookup without changing existing runtime service behavior.
- Subtractive cleanup: none in Phase 1 by design; wrapper/special-case reduction begins in Phase 2 after graph-backed compatibility is proven.
- Friction/blockers: none.

### 2026-08-08 - Phase 2 graph-backed topology compatibility first slice

- Added a red test proving a graph-backed `ITopologyService` can resolve source-cell links without composing the independent `SourceCellLinkTopologyService` wrapper.
- Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~GraphBackedTopologyResolvesSourceCellLinksWithoutSourceCellLinkWrapper|FullyQualifiedName~TopologyGraphMaterializerTests|FullyQualifiedName~TopologyServiceTests"` failed because `GraphBackedTopologyService` did not exist (`CS0246`).
- Implemented `GraphBackedTopologyService` as a behavior-preserving compatibility layer over `TopologyGraphMaterializer` plus an inner fallback topology service. It now uses materialized graph edges for source-cell links and default-grid facts when the fallback also resolves default-grid behavior, while preserving existing entity-topology-policy and merged-inventory-layer fallback semantics.
- Subtractive cleanup completed: `MovementService` default topology composition now uses `GraphBackedTopologyService(new MergedInventoryLayerTopologyService(new EntityTopologyService(new DefaultTopologyService())))`, removing `SourceCellLinkTopologyService` from the default runtime movement composition.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~GraphBackedTopologyResolvesSourceCellLinksWithoutSourceCellLinkWrapper|FullyQualifiedName~TopologyGraphMaterializerTests|FullyQualifiedName~TopologyServiceTests"` passed: 34 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~Movement|FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests"` passed: 115 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 362 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Phase 2 status: first compatibility/subtractive slice complete. Remaining Phase 2 follow-up before Phase 3 should decide whether entity-topology-policy and merged-inventory-layer edges move into graph materialization now or stay as fallback wrappers until the graph-node movement phase.
- Friction/blockers: none.

### 2026-08-08 - Phase 2 graph-backed entity topology policy slice

- Added a red test proving `GraphBackedTopologyService` can resolve entity-authored inventory-boundary topology policy without composing `EntityTopologyService` as a runtime wrapper.
- Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~GraphBackedTopologyResolvesEntityTopologyPolicyWithoutEntityTopologyWrapper|FullyQualifiedName~TopologyServiceTests"` failed because the graph did not yet materialize entity-topology-policy edges, so `TryGetNeighbor` returned false for the outward inventory edge.
- Implemented entity-topology-policy graph edge materialization by projecting `EntityTopologyService(DefaultTopologyService)` facts into `TopologyGraph` when the resolved edge kind is `EntityTopologyPolicy`.
- Updated `GraphBackedTopologyService` to resolve graph-backed `EntityTopologyPolicy` edges directly, while retaining fallback service behavior for merged-inventory-layer topology and other compatibility paths.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~GraphBackedTopologyResolvesEntityTopologyPolicyWithoutEntityTopologyWrapper|FullyQualifiedName~TopologyGraphMaterializerTests|FullyQualifiedName~TopologyServiceTests"` passed: 35 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~Movement|FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests"` passed: 116 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 363 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: graph-backed service can now replace `EntityTopologyService` for entity topology policy lookup; default `MovementService` still keeps the fallback wrapper in composition until a focused removal test proves no behavior loss across affordance/action-choice paths.
- Friction/blockers: none.

### 2026-08-08 - Phase 2 default movement composition removes entity-topology wrapper

- Removed `EntityTopologyService` from default `MovementService` composition. Default movement now composes `GraphBackedTopologyService(new MergedInventoryLayerTopologyService(new DefaultTopologyService()))`, so entity topology policy lookup is graph-backed in the normal runtime movement path.
- Verification initially exposed two graph-compatibility gaps after the wrapper removal:
  - Graph lookup selected default-grid edges before entity-topology-policy edges when both existed for the same `(source, direction)`.
  - Graph-backed adjacency did not preserve the previous entity-topology override behavior where coordinate-adjacent owner cells are not considered adjacent if the same direction resolves through topology into an inventory cell.
- Mitigation implemented:
  - `TopologyGraph.TryGetNeighbor` now prefers non-default materialized edges over default-grid edges for a source/direction.
  - `GraphBackedTopologyService.EvaluateAdjacency` now reports `TargetNotAdjacent` when a topology edge overrides the coordinate neighbor for the same coordinate direction.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~EntityTopologyPolicy|FullyQualifiedName~ControlledActorAffordanceMovementReportsEntityTopology|FullyQualifiedName~ActionChoiceRequestMoveOptionsExposeEntityTopologyDestinations|FullyQualifiedName~TopologyGraphMaterializerTests|FullyQualifiedName~GraphBackedTopology"` passed: 14 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~Movement|FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests"` passed: 116 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 363 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup completed: default movement no longer composes `EntityTopologyService`; source-cell links and entity topology policy are graph-backed for the default movement path.
- Friction/blockers: compatibility friction was resolved in this turn; no blocker remains.

### 2026-08-08 - Phase 2 graph-backed merged inventory layer and default wrapper removal

- Added a red test proving `GraphBackedTopologyService(new DefaultTopologyService())` can resolve a merged-inventory-layer seam without composing `MergedInventoryLayerTopologyService` as a runtime wrapper.
- Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~GraphBackedTopologyResolvesMergedInventoryLayerWithoutMergedInventoryWrapper|FullyQualifiedName~TopologyServiceTests"` failed because the graph did not yet materialize merged-inventory-layer edges, so the graph-backed service returned false for the cross-space seam.
- Implemented merged-inventory-layer graph edge materialization by projecting `MergedInventoryLayerTopologyService(DefaultTopologyService)` facts into `TopologyGraph` when the resolved edge kind is `MergedInventoryLayer`.
- Updated `GraphBackedTopologyService` to resolve graph-backed `MergedInventoryLayer` edges directly.
- Removed `MergedInventoryLayerTopologyService` from default `MovementService` composition. Default movement now composes only `GraphBackedTopologyService(new DefaultTopologyService())`.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~GraphBackedTopologyResolvesMergedInventoryLayerWithoutMergedInventoryWrapper|FullyQualifiedName~TopologyGraphMaterializerTests|FullyQualifiedName~TopologyServiceTests"` passed: 36 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~MergedInventoryLayer|FullyQualifiedName~SourceCellLinks|FullyQualifiedName~EntityTopologyPolicy|FullyQualifiedName~GraphBackedTopology|FullyQualifiedName~TopologyGraphMaterializerTests"` passed: 27 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~Movement|FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests|FullyQualifiedName~TargetPathMove|FullyQualifiedName~TopologyVisibility"` passed: 129 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 364 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Phase 2 status: complete enough to start Phase 3. Default runtime movement topology is now graph-backed for default-grid, source-cell-link, entity-topology-policy, and merged-inventory-layer neighbor facts, with coordinate-facing `ITopologyService` signatures preserved for compatibility.
- Subtractive cleanup completed: default movement no longer composes `SourceCellLinkTopologyService`, `EntityTopologyService`, or `MergedInventoryLayerTopologyService`; those wrappers remain available as compatibility/reference implementations while Phase 3 moves movement to graph-node-first semantics.
- Friction/blockers: none.

### 2026-08-08 - Phase 3 graph-node movement first slice

- Added Phase 3 red tests in `MovementServiceGraphNodeTests` for the planned graph-node-first movement seam:
  - graph-node move changes entity occupancy from one topology node to another;
  - coordinate-facing directional movement can expose/delegate to a graph-node destination;
  - overlapping layout projections do not collapse occupancy because movement targets distinct topology node IDs.
- Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~MovementServiceGraphNodeTests"` failed because `MovementService` did not yet expose graph-node movement APIs (`TryMove(... TopologyNodeId)`, `TryPlace(... TopologyNodeId)`, `TryGetMoveDestinationNode`).
- Implemented graph-node movement APIs in `MovementService`:
  - `CanPlace(WorldState, TopologyNodeId)`;
  - `TryPlace(WorldState, EntityId, TopologyNodeId)`;
  - `TryMove(WorldState, EntityId, TopologyNodeId)`;
  - `TryGetMoveDestinationNode(WorldState, EntityId, Direction, out TopologyNodeId)`.
- Updated coordinate-facing `TryPlace(WorldState, EntityId, PlaneCoord)` and directional `TryMove(WorldState, EntityId, Direction)` to delegate through graph-node IDs after resolving compatibility coordinates/neighbors.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~MovementServiceGraphNodeTests|FullyQualifiedName~TopologyGraphMaterializerTests|FullyQualifiedName~TopologyServiceTests"` passed: 39 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~Movement|FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests"` passed: 120 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 367 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Phase 3 status: complete enough to start Phase 4. Movement now has graph-node placement and movement seams, occupancy checks can target topology node IDs, and coordinate/direction APIs are compatibility adapters over graph-node destinations.
- Subtractive cleanup completed: directional `TryMove` no longer performs a second coordinate-neighbor lookup before placement; it resolves a graph-node destination and moves by topology node ID.
- Friction/blockers: none.

### 2026-08-08 - Phase 4 graph-node Move action/choice first slice

- Added Phase 4 red tests for promoted Move-facing paths exposing and consuming graph-node destinations:
  - `ActionChoiceRequestMoveOptionsExposeGraphNodeDestinationsWithCoordinateProjection` proves Move Action Choice direction options expose a graph `DestinationNodeId` while preserving the coordinate projection DTO.
  - `ControlledActorCommandMoveReportsGraphNodeDestinationWithCoordinateProjection` proves controlled Move command results report both coordinate destination and graph destination node.
- Red verification:
  - Action Choice test failed because `ActionChoiceDirectionOption.DestinationNodeId` did not exist.
  - Controlled command test failed because `ControlledActorCommandResult.DestinationNodeId` did not exist.
- Implemented graph-node destination propagation for the Move path:
  - `ControlledActorDirectionAffordance` now carries optional `DestinationNodeId` alongside coordinate destination.
  - `ActionChoiceDirectionOption` now carries optional `DestinationNodeId` and Move option projection populates it through `MovementService.TryGetMoveDestinationNode`.
  - `ControlledActorCommandResult` now carries optional `DestinationNodeId`; controlled Move resolves and reports the graph-node destination before executing through the existing authoritative action/turn path.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ControlledActorCommandMoveReportsGraphNodeDestinationWithCoordinateProjection|FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~CoreActionChoiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests"` passed: 46 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~Movement|FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests|FullyQualifiedName~ActionChoice"` passed: 122 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 369 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Phase 4 status: first promoted Move command/Action Choice graph-node transition slice is complete. Remaining Phase 4 work should repeat the same pattern for adjacency-based inventory/action paths such as Drop/Exit/Transfer before moving to Phase 5 traversal/pathing.
- Subtractive cleanup: none beyond routing Move DTO facts through graph-node movement seams; coordinate projections remain compatibility output.
- Friction/blockers: none.

### 2026-08-08 - Phase 4 graph-node adjacent action DTO expansion

- Extended the graph-node destination pattern from Move to adjacent inventory/action-choice paths.
- Added red assertions for Drop, Exit, Push, and Transfer choice/command DTOs:
  - Drop Action Choice destinations expose `DestinationNodeId`, and submitted Drop command results report the same graph destination node.
  - Exit Action Choice directions expose `DestinationNodeId`, and submitted Exit command results report coordinate destination plus graph destination node.
  - Push direction options expose `DestinationNodeId`, and submitted Push command results report coordinate destination plus graph destination node.
  - Transfer counterparty options expose `SourceNodeId` for the topology-resolved counterparty source cell.
- Red verification examples:
  - Drop failed because `ControlledActorDestinationAffordance.DestinationNodeId` did not exist.
  - Exit failed because exit direction options and command results did not resolve destination nodes.
  - Push failed because push direction options did not populate the already-available destination-node field.
  - Transfer failed because `ActionChoiceTransferCounterpartyOption.SourceNodeId` did not exist.
- Implemented:
  - `ControlledActorDestinationAffordance.DestinationNodeId` populated for generic destination evaluation and adjacent Drop destination projection.
  - Exit affordance destination-node projection and controlled Exit command destination/destination-node resolution.
  - Push direction destination-node projection.
  - `ActionChoiceTransferCounterpartyOption.SourceNodeId` populated from the topology-resolved counterparty coordinate.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~CoreActionChoiceTests|FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~MovementServiceGraphNodeTests"` passed: 49 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~Movement|FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests|FullyQualifiedName~ActionChoice|FullyQualifiedName~CorePushActionTests|FullyQualifiedName~InventoryTransferActionStepTests"` passed: 156 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 369 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Phase 4 status: complete enough to start Phase 5. Promoted Move, Drop, Exit, Push, and Transfer choice/command DTOs now carry graph-node identity where they expose topology-resolved source/destination facts, while coordinate projections remain compatibility DTO fields. Pickup and Enter target DTOs still expose entity/source coordinates rather than a selected movement destination; graph-node source identity can be added later if frontend/tooling needs it.
- Subtractive cleanup: coordinate-only DTO facts were not removed because they are compatibility/frontend projection fields; new graph-node facts are carried alongside them.
- Friction/blockers: none.

### 2026-08-08 - Phase 5 graph-native traversal first slice

- Added Phase 5 red tests in `TopologyGraphTraversalTests`:
  - graph flood reaches nodes through source-cell graph links that are not coordinate-adjacent;
  - graph flood keeps distinct topology nodes even when their layout projections overlap.
- Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyGraphTraversalTests"` failed because `TopologyGraphTraversalService` did not exist.
- Implemented graph-native traversal vocabulary and service:
  - `TopologyGraphFloodStep` carries `TopologyNodeId`, source-coordinate projection, layout-coordinate projection, distance, predecessor node, direction, and edge kind.
  - `TopologyGraphTraversalService.Flood(TopologyGraph, TopologyNodeId, int)` traverses unblocked graph edges by node identity and preserves distinct source nodes even when layout projections collide.
  - `TopologyGraph.TryGetNode(TopologyNodeId, out TopologyNode)` supports graph-node traversal lookup.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyGraphTraversalTests|FullyQualifiedName~TopologyGraphMaterializerTests|FullyQualifiedName~TopologyServiceTests"` passed: 38 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyGraphTraversalTests|FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~TargetPathMove|FullyQualifiedName~TopologyVisibility"` passed: 61 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 371 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Phase 5 status: first graph-native traversal seam is complete. Remaining Phase 5 work should migrate promoted pathing/visibility consumers, starting with `TargetPathMove`, to graph traversal/node-distance facts rather than coordinate-keyed BFS.
- Subtractive cleanup: none yet; coordinate-facing `TopologyTraversalService` and target-path code remain compatibility consumers until their own graph-native TDD slices land.
- Friction/blockers: none.

### 2026-08-08 - Phase 5 target-path adjacency graph-neighbor slice

- TDD trace:
  - Affected invariant: `Target-path movement distance-band behavior must use legal movement topology...`.
  - Existing tests preserved/reviewed: `TargetPathMovementActionStepTests`, especially `TargetPathMoveSeekFallsThroughWhenAlreadyAdjacent`, distance-band, flee, and orbit cases.
  - Added failing test: `TargetPathMoveSeekTreatsSourceCellLinkAsTargetAdjacency`.
- Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TargetPathMoveSeekTreatsSourceCellLinkAsTargetAdjacency"` failed for the expected reason: target-path adjacency was still coordinate-ring-first and reported `no reachable target-adjacent spaces` despite an authored source-cell graph link.
- Implemented the smallest target-path migration step: `GetLegalTargetAdjacency` now asks shared movement/topology neighbors for the target location and filters by legal path occupancy, instead of constructing `targetCoord.Offset(direction)` coordinates and evaluating those coordinate candidates only.
- Behavior impact: source-cell links and other graph-backed topology edges can define target-adjacent spaces for target-path `SeekAdjacency`; existing coordinate/default-grid target-path behavior remains preserved through graph-backed movement neighbors.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TargetPathMoveSeekTreatsSourceCellLinkAsTargetAdjacency|FullyQualifiedName~TargetPathMovementActionStepTests"` passed: 14 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyGraphTraversalTests|FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~TargetPathMove|FullyQualifiedName~TopologyVisibility"` passed: 62 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 372 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: removed the local coordinate-offset target-adjacency loop from target-path handling; target-adjacent candidate enumeration now reuses shared movement/topology neighbor facts.
- Friction/blockers: none.

### 2026-08-08 - Phase 5 graph shortest-path and target-path distance slice

- TDD trace:
  - Affected invariant: `Target-path movement distance-band behavior must use legal movement topology while treating diagonal movement as more expensive than cardinal movement...`.
  - Existing tests preserved/reviewed: `TargetPathMovementActionStepTests` and `TopologyGraphTraversalTests`.
  - Added failing tests first in `TopologyGraphTraversalTests`:
    - `GraphShortestPathToAnyUsesHalfStepDiagonalCosts` expected graph traversal to expose weighted half-step distances for target-path distance bands.
    - `GraphShortestPathToAnyCanFilterBlockedDestinationNodes` expected graph traversal to accept a legality predicate for occupancy/blocking-like filters.
- Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~GraphShortestPathToAny"` failed to compile because `TopologyGraphTraversalService.ShortestPathToAny` and `HalfStepDistanceToAny` did not exist.
- Implemented graph-native pathing vocabulary/service:
  - `TopologyGraphPathStep` carries node ID, source/layout projections, weighted half-step distance, predecessor node, direction, and edge kind.
  - `TopologyGraphTraversalService.ShortestPathToAny(...)` uses graph node IDs and Dijkstra-style weighted traversal over graph edges, with optional edge legality and direction-cost delegates.
  - `TopologyGraphTraversalService.HalfStepDistanceToAny(...)` returns weighted graph distance to any goal node.
- Migrated target-path distance/path helpers:
  - `HalfStepDistanceToAny` now materializes the topology graph and asks `TopologyGraphTraversalService.HalfStepDistanceToAny` with target-path occupancy legality.
  - `FindShortestPathToAny` now asks `TopologyGraphTraversalService.ShortestPathToAny` and projects graph path steps back to compatibility `PlaneCoord`/`Direction` facts for existing command execution and traces.
- Behavior impact: target-path seek, flee, maintain-distance, and orbit distance calculations now share graph-node traversal/path facts while preserving coordinate projections for movement placement and trace text.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~GraphShortestPathToAny"` passed: 2 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TargetPathMovementActionStepTests|FullyQualifiedName~TopologyGraphTraversalTests"` passed: 18 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyGraphTraversalTests|FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~TargetPathMove|FullyQualifiedName~TopologyVisibility"` passed: 64 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 374 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: removed coordinate-keyed BFS/Dijkstra implementation and path reconstruction from target-path handling; target-path now delegates graph traversal/pathing to the shared topology traversal service.
- Friction/blockers: none.

### 2026-08-08 - Phase 5 topology visibility graph projection slice

- TDD trace:
  - Affected invariants: topology visibility projection seam; source-cell uniqueness/graph identity for overlapping layout projections.
  - Existing tests preserved/reviewed: `TopologyVisibilityProjectionReportsDepthLimitedReachabilityWithoutClaimingLineOfSight`, `TopologyVisibilityProjectionReportsMissingObserverWithoutFrontendGuessing`, and `TopologyGraphTraversalTests`.
  - Added failing test first: `TopologyVisibilityProjectionReportsDistinctGraphNodesForOverlappingLayoutReachability` expected visibility projection to expose distinct graph node IDs and layout projections for two reachable source cells that share the same layout coordinate.
- Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyVisibilityProjectionReportsDistinctGraphNodesForOverlappingLayoutReachability"` failed to compile because `TopologyVisibleCellProjection` did not expose `LayoutCoord`/graph-node projection facts.
- Implemented graph-backed visibility projection:
  - `TopologyVisibleCellProjection` now carries optional `NodeId` and `LayoutCoord` in addition to existing source-cell/distance/predecessor/direction/edge-kind facts.
  - `TopologyVisibilityProjectionService.Project` now materializes the topology graph and uses `TopologyGraphTraversalService.Flood` rather than coordinate-facing `TopologyTraversalService.Flood`.
  - Visibility still reports depth-limited topology reachability and keeps the `LineOfSightNotImplemented` diagnostic; it does not claim LOS/audibility.
- Behavior impact: Content visibility projection can report graph-node identity and source/layout projections for folded/overlapping topology while preserving existing coordinate-facing DTO fields.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyVisibilityProjectionReportsDistinctGraphNodesForOverlappingLayoutReachability|FullyQualifiedName~TopologyVisibilityProjectionReportsDepthLimitedReachabilityWithoutClaimingLineOfSight|FullyQualifiedName~TopologyVisibilityProjectionReportsMissingObserverWithoutFrontendGuessing"` passed: 3 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyVisibilityProjection|FullyQualifiedName~TopologyGraphTraversalTests|FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~TargetPathMove"` passed: 65 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 374 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 270 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: visibility projection no longer depends on coordinate-facing `TopologyTraversalService.Flood`; it consumes shared graph traversal directly.
- Friction/blockers: none.

### 2026-08-08 - Phase 5 coordinate-facing traversal adapter completion

- TDD trace:
  - Affected invariants: source-cell topology uniqueness/reachability; topology visibility projection seam; target-path graph traversal dependency.
  - Existing tests preserved/reviewed: `TopologicalRayFollowsDirectedOverlayThenContinuesInSameDirection`, `TopologicalRayStopsBeforeBlockedOrOutOfBoundsNeighbor`, `TopologicalFloodIncludesOriginAndBoundedReachableNeighbors`, `TopologicalFloodDoesNotRevisitNodesThroughCycles`, `MergedInventoryLayerDistanceTreatsPlacedSpacesAsOneRigidLayer`.
  - Added failing tests first:
    - `TopologicalFloodUsesMaterializedGraphSourceCellLinksWithoutSourceLinkWrapper`.
    - `TopologicalRayUsesMaterializedGraphSourceCellLinksWithoutSourceLinkWrapper`.
- Red verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologicalFloodUsesMaterializedGraphSourceCellLinksWithoutSourceLinkWrapper"` failed because coordinate-facing flood still walked `ITopologyService.GetNeighbors` with `DefaultTopologyService` and did not see graph-materialized source-cell links.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologicalRayUsesMaterializedGraphSourceCellLinksWithoutSourceLinkWrapper"` failed because coordinate-facing ray still used `ITopologyService.TryGetNeighbor` with `DefaultTopologyService` and returned no source-link step.
- Implemented final Phase 5 traversal adapter step:
  - `TopologyTraversalService.Flood` now materializes a graph, runs `TopologyGraphTraversalService.Flood`, and projects graph traversal steps back to compatibility `TopologyFloodStep` source-coordinate facts.
  - `TopologyTraversalService.CastDirectionalRay` now follows materialized graph edges while preserving existing ray DTO shape.
  - `TopologyTraversalService` supplements the materialized graph with non-materialized compatibility topology edges from its injected `ITopologyService`, preserving existing directed-overlay tests while making graph-materialized default/entity/merged/source-link edges available even when a caller injects `DefaultTopologyService`.
- Phase 5 completion status: graph traversal/pathing/visibility migration goals are complete for promoted services. `TopologyGraphTraversalService` owns graph flood, weighted shortest path, and distance facts; target-path movement and topology visibility consume graph traversal; `TopologyTraversalService` is now a coordinate-facing adapter/projection over graph traversal.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologicalRayUsesMaterializedGraphSourceCellLinksWithoutSourceLinkWrapper|FullyQualifiedName~TopologicalRayFollowsDirectedOverlayThenContinuesInSameDirection|FullyQualifiedName~TopologicalRayStopsBeforeBlockedOrOutOfBoundsNeighbor|FullyQualifiedName~TopologicalFloodUsesMaterializedGraphSourceCellLinksWithoutSourceLinkWrapper|FullyQualifiedName~TopologicalFloodIncludesOriginAndBoundedReachableNeighbors|FullyQualifiedName~TopologicalFloodDoesNotRevisitNodesThroughCycles"` passed: 6 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~TopologyGraphTraversalTests|FullyQualifiedName~TopologyVisibilityProjection|FullyQualifiedName~TargetPathMove"` passed: 67 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 376 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 270 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: removed the remaining coordinate-keyed BFS/ray traversal implementation from `TopologyTraversalService`; coordinate traversal DTOs are now projections over graph traversal.
- Friction/blockers: preserving directed-overlay compatibility while moving traversal to graph required a small graph supplementation adapter because directed overlays are injected runtime topology services rather than world-materialized graph data. Mitigation: `TopologyTraversalService` materializes world graph edges first, then adds any non-duplicate edges exposed by the injected topology service before graph traversal.

### 2026-08-08 - Phase 6 legacy coordinate target movement authoring retirement start

- TDD trace:
  - Affected invariants: content Action Plan shape/catalog contracts; canonical Action Step state contracts; target-path movement is the promoted graph-native replacement for target-relative pathing.
  - Existing tests preserved/reviewed: `ContentEditorListsCanonicalActionStepMetadata`, `ContentEditorServiceListsCanonicalActionStepMetadata`, `ContentEditorServiceRejectsLegacyMetadataSettingActionStepsForCanonicalAuthoring`, `PrototypeRegistryValidationAcceptsDefaultableFacingForTurnActionSteps`, `PrototypeRegistryValidationAcceptsBehaviorChainPickupTargetAfterMoveFacingWritesTarget`, and legacy runtime reference tests under `PrototypeActionStepReferenceTests`/`TargetingActionStepTests`.
  - Added/revised failing tests first:
    - Extended `ContentEditorServiceRejectsLegacyMetadataSettingActionStepsForCanonicalAuthoring` to require editor authoring rejection for `SeekTarget`, `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, and `StrafeAnticlockwise`.
    - Added `PrototypeRegistryValidationRejectsLegacyCoordinateMovementActionSteps` to require persisted/new canonical content validation diagnostics for legacy coordinate target movement, with guidance to use `TargetPathMove`.
- Red verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentEditorServiceRejectsLegacyMetadataSettingActionStepsForCanonicalAuthoring"` failed because target movement compatibility was still allowed through `ActionStepCatalog.IsStableAuthoringStep`.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~PrototypeRegistryValidationRejectsLegacyCoordinateMovementActionSteps"` failed because registry validation still accepted `FleeTarget` in canonical behavior content.
- Implemented first Phase 6 retirement slice:
  - Removed the special `SeekTarget`/`FleeTarget`/`MaintainChebyshevDistanceTwo`/`StrafeClockwise`/`StrafeAnticlockwise` compatibility allowance from `ActionStepCatalog.IsStableAuthoringStep`.
  - Added `ActionStepCatalog.IsLegacyCoordinateTargetMovementStep` as an explicit retirement classifier.
  - Added `ContentDiagnosticCode.UnsupportedLegacyActionStep`.
  - `ActionPlanValidator` now rejects legacy coordinate target movement steps in authored canonical behavior and points authors toward `TargetPathMove`.
- Behavior impact: canonical editor/API authoring and persisted content validation no longer accept legacy coordinate target movement actions. Existing Core runtime handlers remain temporarily for direct compatibility/reference tests until later Phase 6 slices remove or rewrite them.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentEditorServiceRejectsLegacyMetadataSettingActionStepsForCanonicalAuthoring|FullyQualifiedName~PrototypeRegistryValidationRejectsLegacyCoordinateMovementActionSteps|FullyQualifiedName~PrototypeRegistryValidationAcceptsDefaultableFacingForTurnActionSteps"` passed: 3 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 271 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 376 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: removed the stable-authoring compatibility shim for legacy coordinate target movement.
- Friction/blockers:
  - Initial validation check was too broad and rejected legacy non-target turning steps (`TurnLeft`, `TurnRight`, `ReverseFacing`), breaking existing content validation tests. Mitigation: narrowed the validator to the explicit Phase 6 target movement retirement set via `IsLegacyCoordinateTargetMovementStep`.
  - One parallel test invocation hit a transient build output file lock (`CS2012`) while another test command was building. Mitigation: reran the targeted command sequentially; no code blocker.

### 2026-08-08 - Phase 6 legacy coordinate target movement runtime deletion

- Directional decision: deletion is preferred over preserving Alpha-era coordinate movement. Runtime support for retired coordinate target movement is no longer worth saving because graph-native `TargetPathMove` owns promoted target-relative movement and existing content/debug surfaces do not outrank graph-first correctness.
- TDD trace:
  - Affected invariants: canonical Action Step state contracts; target-path graph-native replacement behavior; content/action-plan shape contracts.
  - Existing tests revised/deleted: removed `TargetRelativeMovementActionStepTests`; removed the retired coordinate movement block from `PrototypeActionStepReferenceTests`; revised `TargetingActionStepTests` chained-target tests to use `TargetPathMove` instead of `SeekTarget`.
  - Added failing test first: `RuntimeRejectsRetiredLegacyCoordinateTargetMovementSteps` asserts `SeekTarget`, `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, and `StrafeAnticlockwise` throw an explicit retirement error that points to `TargetPathMove`.
- Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~RuntimeRejectsRetiredLegacyCoordinateTargetMovementSteps"` failed because all five retired steps still executed successfully/fallthrough instead of throwing.
- Implemented deletion slice:
  - Removed runtime dispatch to `ApplySeekTarget`, `ApplyFleeTarget`, `ApplyMaintainChebyshevDistanceTwo`, and `ApplyStrafeTarget`.
  - Deleted those coordinate-primary handler implementations from `ActionPlanInterpreter.TargetingHandlers.cs`.
  - Added one explicit runtime retirement guard in `BehaviorStepDispatcher` for the retired set, with `TargetPathMove` guidance.
  - Kept `AcquireNearestTarget` for now; it remains a separate legacy targeting acquisition behavior and is not part of this target-movement deletion set.
- Behavior impact: direct runtime execution of retired coordinate target movement now fails fast. Canonical graph-native movement behavior remains covered by `TargetPathMovementActionStepTests`, and chained acquisition now demonstrates continuation into `TargetPathMove`.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~RuntimeRejectsRetiredLegacyCoordinateTargetMovementSteps|FullyQualifiedName~PrototypeActionStepReferenceTests"` passed: 14 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 361 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 271 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: deleted one duplicate legacy target-relative movement test file and removed the retired coordinate movement handler code paths from runtime dispatch/implementation.
- Friction/blockers: deleting runtime support exposed two remaining Core tests that chained through `SeekTarget`; both were better expressed through graph-native `TargetPathMove`, so they were revised rather than preserving compatibility. No blocker.

### 2026-08-08 - Phase 6 AcquireNearestTarget retirement

- Directional decision: `AcquireNearestTarget` is also retired rather than rewritten. It was same-plane coordinate/range targeting, hidden from canonical authoring already, and graph-first targeting rules plus explicit target state are the replacement direction.
- TDD trace:
  - Affected invariants: canonical Action Step state contracts; content Action Plan shape/catalog contracts; graph-first target refresh/locality direction from the Action Plan Data invariant.
  - Existing tests revised/deleted: removed direct runtime `AcquireNearestTarget` behavior tests from `TargetingActionStepTests`; removed the old action-attempt special case test that treated successful `AcquireNearestTarget` as continued; existing editor tests already asserted it is absent from canonical authoring surfaces.
  - Added/revised failing tests first:
    - Extended `RuntimeRejectsRetiredLegacyTargetingAndCoordinateTargetMovementSteps` to include `AcquireNearestTarget`.
    - Revised `PrototypeRegistryValidationRejectsLegacyTargetingAndCoordinateMovementActionSteps` to use `AcquireNearestTarget` and expect graph-first targeting guidance.
- Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~RuntimeRejectsRetiredLegacyTargetingAndCoordinateTargetMovementSteps|FullyQualifiedName~PrototypeRegistryValidationRejectsLegacyTargetingAndCoordinateMovementActionSteps"` failed because `AcquireNearestTarget` still executed and validation still accepted it.
- Implemented deletion slice:
  - Added `AcquireNearestTarget` to `ActionStepCatalog.IsRetiredLegacyTargetingOrCoordinateMovementStep`.
  - `ActionPlanValidator` now rejects `AcquireNearestTarget` in canonical behavior content with graph-first targeting/`TargetPathMove` guidance.
  - Runtime dispatch now sends `AcquireNearestTarget` to the same explicit retirement guard as retired coordinate movement.
  - Deleted `ActionPlanInterpreter.TargetingHandlers.cs`, removing the last coordinate-primary targeting handler implementation.
  - Removed `ActionStepAttemptProjection` special casing/results projection for retired targeting/coordinate movement primitive traces.
- Behavior impact: `AcquireNearestTarget` remains in the enum/catalog only as a loadable retired value for diagnostics; it is not available for authoring and direct runtime execution fails fast.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~RuntimeRejectsRetiredLegacyTargetingAndCoordinateTargetMovementSteps|FullyQualifiedName~PrototypeRegistryValidationRejectsLegacyTargetingAndCoordinateMovementActionSteps"` passed: 7 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 359 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 271 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: deleted the last targeting handler file and removed direct runtime tests preserving `AcquireNearestTarget` semantics.
- Friction/blockers: another parallel Core/Content suite run hit a transient build artifact file lock. Mitigation: reran the suites sequentially; no code blocker.

### 2026-08-08 - Phase 6 retired descriptor catalog removal

- Directional decision: retired targeting/coordinate movement actions should not remain normal `ActionStepCatalog` descriptors. Enum values remain loadable only so old content can deserialize and receive diagnostics.
- TDD trace:
  - Affected invariants: content Action Plan shape/catalog contracts; graph-first targeting/target-path replacement direction.
  - Existing tests revised: `ActionStepCatalogExposesAllCanonicalActionStepKinds` now excludes retired targeting/coordinate movement values; retired descriptor compatibility assertions were removed from `ActionStepCatalogCompatibilityTests`.
  - Added failing test first: `RetiredLegacyTargetingAndCoordinateMovementStepsAreNotCatalogDescriptors` asserts `AcquireNearestTarget`, `SeekTarget`, `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, and `StrafeAnticlockwise` are classified as retired, absent from `ActionStepCatalog.Steps`, and rejected by `ActionStepCatalog.Get`.
- Red verification: targeted catalog tests failed because retired descriptors were still present in normal catalog metadata.
- Implemented deletion slice:
  - Removed retired targeting/coordinate movement descriptors from `ActionStepCatalog.Steps`.
  - Kept `ActionStepCatalog.IsRetiredLegacyTargetingOrCoordinateMovementStep` as the direct enum classifier for validation/runtime retirement guards.
  - Updated Content validation/state/snapshot/preview/defaulting paths to avoid requiring normal catalog metadata for retired steps while still surfacing diagnostics or simple invalid-content summaries.
  - Removed retired primitive trace projection special cases in the previous slice; no normal metadata path remains for these steps.
- Behavior impact: retired steps are no longer catalog/editor metadata. Old YAML can still deserialize and validation can identify the retired enum values, but normal graph-first catalog consumers no longer describe them as supported actions.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~RetiredLegacyTargetingAndCoordinateMovementStepsAreNotCatalogDescriptors|FullyQualifiedName~ActionStepCatalogExposesAllCanonicalActionStepKinds|FullyQualifiedName~PrototypeRegistryValidationRejectsLegacyTargetingAndCoordinateMovementActionSteps"` passed: 8 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 271 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 359 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: removed six retired descriptors from the normal Action Step catalog and deleted the tests that required retired descriptor compatibility.
- Friction/blockers: Content snapshot/preview code still touched catalog metadata for invalid old content. Mitigation: route retired enum values through simple invalid-content display/projection paths without restoring full catalog descriptors.

### 2026-08-08 - Phase 7 overlap as projection metadata first slice

- TDD trace:
  - Affected invariants: source-cell directional uniqueness/graph identity; occupancy node uniqueness; content YAML validation/materialization; scenario materialization graph projection.
  - Existing tests reviewed/preserved: `ContentValidationRejectsMergedLayerOverlapDisconnectedOrInvalidOwner` was split/revised; existing graph overlap tests in `MovementServiceGraphNodeTests`, `TopologyGraphTraversalTests`, and topology/source-link suites were preserved.
  - Added/revised failing tests first:
    - `ContentValidationAllowsMergedLayerOverlapAsProjectionMetadata` asserts overlapping merged-layer layout projections are valid authoring data.
    - `ContentValidationRejectsMergedLayerDisconnectedOrInvalidOwner` preserves disconnected/invalid-owner rejection without treating overlap itself as invalid.
- Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentValidationAllowsMergedLayerOverlapAsProjectionMetadata|FullyQualifiedName~ContentValidationRejectsMergedLayerDisconnectedOrInvalidOwner"` failed because content validation still rejected overlapping merged-layer cells as runtime ambiguity.
- Implemented first Phase 7 change:
  - `PrototypeContentRegistry.ValidateMergedInventoryLayers` no longer emits overlap errors for cells that share a merged-layer layout coordinate.
  - Connectivity validation continues to use the unique set of projected layout cells and still rejects disconnected layers without semantic joins.
  - Added `OverlappingMergedLayerContentMaterializesDistinctGraphNodesWithSharedLayoutProjection` to prove overlapped content can materialize into a scenario world where distinct source cells produce distinct topology graph nodes with the same layout projection.
- Behavior impact: overlap is now authoring/projection metadata, not a validation/runtime movement ambiguity. Graph node identity remains distinct; coordinates remain projections.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ContentValidationAllowsMergedLayerOverlapAsProjectionMetadata|FullyQualifiedName~OverlappingMergedLayerContentMaterializesDistinctGraphNodesWithSharedLayoutProjection|FullyQualifiedName~ContentValidationRejectsMergedLayerDisconnectedOrInvalidOwner"` passed: 3 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~YamlContentLoaderTests|FullyQualifiedName~TopologyGraphTraversalTests|FullyQualifiedName~MovementServiceGraphNodeTests|FullyQualifiedName~TopologyServiceTests"` passed: 67 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 359 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 273 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: removed the content validation branch that treated layout-coordinate overlap as an error.
- Friction/blockers: none.

### 2026-08-08 - Phase 7 authored overlap-loop characterization

- TDD/trace:
  - Affected invariants: source-cell directional uniqueness/graph identity; occupancy node uniqueness; scenario materialization graph data; movement follows graph edges rather than layout coordinate collapse.
  - Existing tests reviewed/preserved: merged-layer join resolution tests, source-cell-link movement tests, graph traversal overlap tests, and `MovementServiceGraphNodeTests`.
  - Added test: `OverlapLoopScenarioMovesThroughExplicitGraphEdgesWithoutCollapsingLayoutProjection`.
- Result: the new authored overlap-loop test passed without production changes, confirming the graph-first slices already support this Phase 7 behavior once overlap validation no longer rejects overlapped layout projections.
- Coverage added:
  - Authored YAML can define three one-cell merged-layer contributors sharing one layout projection.
  - Authored aligned joins materialize explicit source-cell graph edges forming a loop.
  - Distinct source cells materialize as three distinct topology node IDs with the same layout projection.
  - A runtime traveler moves through the loop by explicit graph directions and returns to the origin without layout-coordinate collapse.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~OverlapLoopScenarioMovesThroughExplicitGraphEdgesWithoutCollapsingLayoutProjection"` passed: 1 test.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~OverlapLoopScenarioMovesThroughExplicitGraphEdgesWithoutCollapsingLayoutProjection|FullyQualifiedName~ContentValidationAllowsMergedLayerOverlapAsProjectionMetadata|FullyQualifiedName~OverlappingMergedLayerContentMaterializesDistinctGraphNodesWithSharedLayoutProjection|FullyQualifiedName~TopologyGraphTraversalTests|FullyQualifiedName~MovementServiceGraphNodeTests"` passed: 10 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 359 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 274 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: none in this slice; no production code was needed because prior graph traversal/materialization work already owned the behavior.
- Friction/blockers: none.

### 2026-08-08 - Phase 7 merged-layer topology wrapper retirement

- TDD/trace:
  - Affected invariants: source-cell/graph identity, occupancy node uniqueness, merged-layer topology behavior, and graph-first traversal/movement.
  - Existing tests preserved/reviewed: `MergedInventoryLayerConnectsTwoPlacedInventorySpacesAsOneTopology`, `GraphBackedTopologyResolvesMergedInventoryLayerWithoutMergedInventoryWrapper`, `MergedInventoryLayerDistanceTreatsPlacedSpacesAsOneRigidLayer`, `TopologyGraphTraversalTests`, `MovementServiceGraphNodeTests`, and the authored overlap-loop tests.
  - This was a subtractive parity slice over existing tests rather than a new red behavior; the testable outcome was removing wrapper dependency while preserving graph-backed merged-layer behavior.
- Implemented:
  - `TopologyGraphMaterializer.MaterializeMergedInventoryLayerEdges` now builds merged-layer graph edges directly from merged-layer cells grouped by layout projection.
  - Overlapped layout coordinates can produce multiple destination graph edges while preserving distinct source node identities.
  - Intercardinal two-corner blocking now checks occupancy across all cells sharing each projected corner coordinate.
  - Deleted `MergedInventoryLayerTopologyService` runtime wrapper.
  - Updated remaining direct tests to use graph-backed topology instead of the deleted wrapper.
- Behavior impact: merged-layer runtime topology is now ordinary graph materialization data, not a coordinate-facing topology wrapper. `MergedInventoryLayerResolver` remains for named source/layout projection and owner-policy lookups, but traversal/movement merged-layer adjacency is graph materializer owned.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~TopologyGraphTraversalTests|FullyQualifiedName~MovementServiceGraphNodeTests|FullyQualifiedName~OverlapLoopScenarioMovesThroughExplicitGraphEdgesWithoutCollapsingLayoutProjection"` passed: 41 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 359 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 274 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: removed the merged-layer coordinate-facing topology wrapper class and removed graph materializer's dependency on it.
- Friction/blockers: none.

### 2026-08-08 - Phase 7 source-link and entity-policy wrapper retirement

- TDD/trace:
  - Affected invariants: graph/source-cell identity, topology adjacency semantics, inventory-boundary entry/exit policy, traversal projection, and movement/occupancy destination validation.
  - Existing tests preserved/reviewed: `SourceCellLinksConnectAuthoredInventoryCellsBidirectionally`, `GraphBackedTopologyResolvesSourceCellLinksWithoutSourceCellLinkWrapper`, `EntityTopologyPolicyConnectsInventoryEdgeOutwardToExteriorAdjacency`, `GraphBackedTopologyResolvesEntityTopologyPolicyWithoutEntityTopologyWrapper`, `EntityTopologyPolicyConnectsExteriorAdjacencyInwardToPreferredInventoryEdgeCell`, `EntityTopologyPolicyConnectsIntercardinalExteriorAdjacencyToInventoryCorners`, `EntityTopologyPolicyOutwardAdjacencySupportsPickupAcrossInventoryBoundary`, traversal graph tests, movement graph-node tests, and visibility projection tests.
  - This was a subtractive parity slice over existing graph-backed characterization; no new behavior was intended beyond deleting wrappers and preserving graph materialization outcomes.
- Implemented:
  - Deleted `SourceCellLinkTopologyService`; no production callers remained because source-cell links are materialized by `TopologyGraphMaterializer.MaterializeSourceCellLinkEdges`.
  - Deleted `EntityTopologyService`; `TopologyGraphMaterializer.MaterializeEntityTopologyPolicyEdges` now resolves inward/outward entity-policy adjacency directly.
  - Updated the last graph-backed source-link test to use `GraphBackedTopologyService(new DefaultTopologyService())` without wrapping entity topology.
  - Updated `TopologyVisibilityProjectionService` fallback construction away from the deleted entity topology wrapper; visibility remains graph-materializer driven.
- Behavior impact: default grid remains the coordinate fallback, while source-cell links, entity topology policies, and merged-layer seams are all graph materialization concerns.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~TopologyGraphTraversalTests|FullyQualifiedName~MovementServiceGraphNodeTests|FullyQualifiedName~TopologyVisibility"` passed: 43 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 359 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 274 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: removed the source-cell-link and entity-policy coordinate-facing topology wrapper classes and removed all references to them.
- Friction/blockers: none.

### 2026-08-08 - Phase 7 directed-overlay and traversal injection retirement

- TDD/trace:
  - Affected invariants: graph-first topology traversal, coordinate-facing traversal adapter behavior, movement destination validation, and visibility projection reachability.
  - Existing tests revised/reviewed: `TopologyServiceTests` traversal/ray/flood/source-link/merged-layer/entity-policy coverage, `ActorPovPlayProjectionTests`, `TopologyGraphTraversalTests`, and `MovementServiceGraphNodeTests`.
  - Directed-overlay tests were deliberately removed because the feature had no production authoring path or runtime caller and preserved an ad hoc coordinate-wrapper topology model that conflicts with graph-first topology as ordinary data.
- Implemented:
  - Deleted `DirectedTopologyEdge` and `DirectedOverlayTopologyService`.
  - Removed `TopologyEdgeKind.DirectedOverlay`.
  - Simplified `TopologyTraversalService` into a graph-materializer adapter with no injected `ITopologyService`; ray/flood traversal now reads only `TopologyGraphMaterializer.Materialize(world)`.
  - Removed obsolete `ITopologyService` injection from `TopologyVisibilityProjectionService` and updated tests to construct it without topology parameters.
- Behavior impact: directed overlay is no longer a runtime topology extension point. Runtime traversal uses materialized graph edges only: default grid, entity topology policy, merged inventory layer, and source-cell link.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~ActorPovPlayProjectionTests|FullyQualifiedName~TopologyGraphTraversalTests|FullyQualifiedName~MovementServiceGraphNodeTests"` passed: 41 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 353 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 274 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: removed directed-overlay topology runtime and obsolete traversal/visibility topology injection.
- Friction/blockers: none.

### 2026-08-08 - Phase 7 topology service bridge retirement

- TDD/trace:
  - Affected invariants: graph-first movement destination resolution, adjacency diagnostics, default-grid intercardinal corner blocking, action-choice topology destinations, controlled exit affordance destinations, source-cell-link traversal, entity-policy seams, and merged-layer seams.
  - Existing tests revised/reviewed: `TopologyServiceTests`, `TopologyGraphMaterializerTests`, `CoreActionChoiceTests`, `ControlledActorAffordanceServiceTests`, `MovementServiceGraphNodeTests`, and `TopologyGraphTraversalTests`.
  - This was a subtractive consolidation slice over existing graph-backed characterization. Fake `ITopologyService` override tests were rewritten to use real `WorldState.SourceCellLinks` so action-choice/affordance behavior is tested through authored graph data instead of injected topology wrappers.
- Implemented:
  - `MovementService` now queries `TopologyGraphMaterializer.Materialize(world)` directly for movement destinations, movement nodes, legal movement neighbors, adjacent relocation, and coordinate-facing adjacency compatibility.
  - Deleted `GraphBackedTopologyService`, `DefaultTopologyService`, and the `ITopologyService` abstraction.
  - Deleted the test-only `OverrideNeighborTopologyService`.
  - Inlined default-grid edge materialization into `TopologyGraphMaterializer` so default-grid cardinal/intercardinal edges are ordinary graph data.
  - Updated Content capability documentation to describe graph-materialized topology instead of service-wrapper topology.
- Behavior impact: runtime topology has no remaining coordinate-facing service-wrapper extension point. Default grid, source-cell links, entity topology policies, and merged inventory layers are all graph materializer outputs; `MovementService` remains the coordinate-facing compatibility API over that graph while action/editor call sites finish migrating to node IDs.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~TopologyGraphMaterializerTests|FullyQualifiedName~CoreActionChoiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~MovementServiceGraphNodeTests|FullyQualifiedName~TopologyGraphTraversalTests"` passed: 78 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 353 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 274 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: removed the topology service abstraction and remaining default/graph-backed service implementations.
- Friction/blockers: none.

### 2026-08-08 - Phase 8 planning/regroup after topology service consolidation

- Planning decision: after retiring runtime topology service wrappers and the `ITopologyService` bridge, the next sprint should not broaden into an unbounded Content/editor rewrite. It should introduce targeted graph-native API surfaces where old coordinate-first method names obscure graph semantics, then migrate callers and delete adapters slice-by-slice.
- Added `Phase 8: graph-native v2 API consolidation` with explicit TDD trace.
- TDD/trace planned for Phase 8:
  - Affected invariants: topology node identity, one occupant per node, source-cell directional uniqueness, controlled command/affordance/action-choice topology facts, target-path topology movement, and Content/editor topology projection contracts.
  - Existing tests to preserve/review are listed in the Phase 8 section before production work begins.
  - Required new/revised failing tests include graph-native movement edge query results, graph-native Action Choice/affordance DTO construction, coordinate adapter parity, and at least one deleted/replaced coordinate-primary caller per slice.
- Scope guard: prefer semantic graph-native names/result types over blanket `V2` suffixes unless an external API seam needs explicit versioning. Coordinate fields remain projections during migration, not topology identity.
- Verification for this planning-only turn: documentation lint should be run before completing the next implementation turn.
- Friction/blockers: none.

### 2026-08-08 - Phase 8 first graph-native movement edge API

- TDD/trace:
  - Affected invariants: entity location/occupancy node identity, one occupant per node, graph-first movement destination resolution, controlled command/affordance/action-choice topology facts, and source-cell/entity-policy topology edges.
  - Existing tests preserved/reviewed: `MovementServiceGraphNodeTests`, `ControlledActorCommandServiceTests`, `ControlledActorAffordanceServiceTests`, `CoreActionChoiceTests`, `TopologyServiceTests`, and `TargetPathMovementActionStepTests`.
  - Added failing tests first:
    - `GraphMovementEdgeReportsNodeIdentityAndProjectionFacts` required a graph-native movement edge result carrying source/destination node IDs, source coordinate projections, layout/display projection facts, direction, edge kind, and blocked/failure facts.
    - `CoordinateMoveDestinationAdaptersUseGraphMovementEdge` required existing coordinate/node destination APIs to match the new graph-native movement edge result.
  - Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~MovementServiceGraphNodeTests"` failed because `MovementService.TryGetMovementEdge` did not exist (`CS1061`).
- Implemented:
  - Added `MovementEdgeResult` as the first Phase 8 graph-native movement result type.
  - Added `MovementService.TryGetMovementEdge(WorldState, EntityId, Direction, out MovementEdgeResult)`.
  - Reworked `TryGetMoveDestination`, `TryGetMoveDestinationNode`, `CanMove`, and `GetBlockingEntity` to consume the graph-native edge result where possible, keeping coordinate-returning behavior as adapter/projection compatibility.
- Behavior impact: callers can now use node identity as the movement destination source of truth while legacy coordinate APIs remain available as thin adapters.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~MovementServiceGraphNodeTests"` passed: 5 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~MovementServiceGraphNodeTests|FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests|FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~TargetPathMovementActionStepTests"` passed: 92 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 355 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 274 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: no public compatibility APIs were deleted in this first Phase 8 slice; several internal movement operations now route through the graph-native edge result instead of independently resolving coordinate destinations.
- Friction/blockers: none.

### 2026-08-08 - Phase 8 affordance/action-choice edge-kind projection

- TDD/trace:
  - Affected invariants: controlled actor affordance topology facts, Action Choice topology facts, graph-first movement destination resolution, source-cell/entity-policy edge identity, and coordinate projection compatibility.
  - Existing tests revised/reviewed: `ControlledActorAffordanceServiceTests`, `CoreActionChoiceTests`, `MovementServiceGraphNodeTests`, `ControlledActorCommandServiceTests`, `TopologyServiceTests`, and `TargetPathMovementActionStepTests`.
  - Added/revised failing tests first requiring movement affordances, exit affordances, and move choices to expose graph edge kind alongside destination node ID and coordinate projection.
- Implemented optional `TopologyEdgeKind? EdgeKind` projection on `ControlledActorDirectionAffordance` and `ActionChoiceDirectionOption`; movement affordance, exit affordance, and move-choice construction now consume `MovementService.TryGetMovementEdge` for destination coordinate/node/edge-kind projection.
- Verification: targeted Phase 8 slice passed, `Suite=Core` passed: 355 tests, `Suite=Content` passed: 274 tests, and documentation lint passed.
- Subtractive cleanup: removed duplicate coordinate-destination plus node-destination lookups from movement affordance and move-choice construction; they now consume a single graph-native movement edge result.
- Friction/blockers: none.

### 2026-08-08 - Phase 8 drop/transfer/push edge-kind projection

- TDD/trace:
  - Affected invariants: Action Choice drop destination facts, transfer counterparty topology facts, push target-relative direction facts, graph-first movement destination resolution, source-cell-link identity, and coordinate projection compatibility.
  - Existing tests revised/reviewed: `CoreActionChoiceTests`, `ControlledActorAffordanceServiceTests`, `ControlledActorCommandServiceTests`, `MovementServiceGraphNodeTests`, `TopologyServiceTests`, and `TargetPathMovementActionStepTests`.
  - Added/revised failing tests first:
    - `ActionChoiceRequestExposesDropDestinationsFromTopologyNeighbors` now requires source-cell-link drop destinations to expose destination node ID and edge kind.
    - `ActionChoiceRequestExposesTransferCounterpartiesFromTopologyNeighbors` now requires source-cell-link transfer counterparties to expose source node ID and edge kind.
    - `ActionChoiceRequestExposesPushTargetsAndTargetRelativeDirections` now requires push directions to expose destination node ID and edge kind.
  - Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~CoreActionChoiceTests"` failed because `ControlledActorDestinationAffordance`, `ActionChoiceTransferCounterpartyOption`, and `ActionChoicePushDirectionOption` had no `EdgeKind` property (`CS1061`).
- Implemented:
  - Added optional `TopologyEdgeKind? EdgeKind` to `ControlledActorDestinationAffordance`, `ActionChoiceTransferCounterpartyOption`, and `ActionChoicePushDirectionOption`.
  - `ActionChoiceService.QueryPushDirections` now uses `MovementService.TryGetMovementEdge` for target-relative push destination coordinate/node/edge-kind projection.
  - `ActionChoiceService.QueryAdjacentDropDestinations` now uses `MovementService.TryGetMovementEdge` for adjacent drop destination coordinate/node/edge-kind projection, with coordinate adapter fallback for projected invalid/out-of-bounds destinations.
  - `ActionChoiceService.QueryTransferCounterparties` now uses `MovementService.TryGetMovementEdge` for counterparty source coordinate/node/edge-kind projection, with coordinate adapter fallback for non-edge directions.
- Behavior impact: Drop, Transfer, and Push choice DTOs can now identify which graph edge kind produced their directional topology facts while preserving existing coordinate projection fields.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~CoreActionChoiceTests"` passed: 31 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~CoreActionChoiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~MovementServiceGraphNodeTests|FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~TargetPathMovementActionStepTests"` passed: 92 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 355 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 274 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: removed duplicate coordinate-destination plus node-destination lookups from push/drop/transfer choice construction; each now consumes a single graph-native movement edge result where an edge exists.
- Friction/blockers: none.

### 2026-08-08 - Phase 8 command result edge-kind projection

- TDD/trace:
  - Affected invariants: controlled actor command topology facts, graph-first movement destination resolution, push target-relative topology facts, command result structured outcome anchors, and coordinate projection compatibility.
  - Existing tests revised/reviewed: `ControlledActorCommandServiceTests`, `CoreActionChoiceTests`, `ControlledActorAffordanceServiceTests`, and `MovementServiceGraphNodeTests`.
  - Added/revised failing tests first:
    - `ControlledActorCommandMoveReportsGraphNodeDestinationWithCoordinateProjection` now requires command results to expose the graph edge kind for a move command.
    - `ControlledActorCommandPushMovesTargetAndAdvancesTurn` now requires command results to expose push destination node ID and graph edge kind.
  - Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ControlledActorCommandServiceTests"` failed because `ControlledActorCommandResult` had no `EdgeKind` property (`CS1061`).
- Implemented:
  - Added optional `TopologyEdgeKind? EdgeKind` to `ControlledActorCommandResult`.
  - `ControlledActorCommandService.Execute` now resolves one graph-native `MovementEdgeResult` for move, exit, and push commands, then projects destination coordinate, destination node ID, and edge kind from that result before falling back to legacy command-specified coordinate destinations.
- Behavior impact: controlled command results can now identify which graph edge kind produced movement-like command destinations while preserving existing coordinate destination fields and destination node IDs.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ControlledActorCommandServiceTests"` passed: 7 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~CoreActionChoiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~MovementServiceGraphNodeTests"` passed: 51 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 355 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 274 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: command result destination/node/edge-kind projection for move/exit/push now comes from one graph-native movement edge result instead of separately resolving coordinate and node destinations.
- Friction/blockers: an over-broad exploratory targeted filter including `FullyQualifiedName~ActionLog` surfaced an unrelated historical projection assertion failure, while the relevant targeted slice and full `Suite=Core` passed. Mitigation: use precise targeted filters for the changed command/choice/affordance slice and rely on full suite verification for broader regressions.

### 2026-08-08 - Phase 8 relocation edge-fact projection

- TDD/trace:
  - Affected invariants: graph-first adjacent relocation, entity location/occupancy node identity, movement/action relocation behavior, and coordinate projection compatibility.
  - Existing tests revised/reviewed: `CoreRelocationTests`, `MovementServiceGraphNodeTests`, `ControlledActorCommandServiceTests`, `CoreActionChoiceTests`, `ControlledActorAffordanceServiceTests`, and canonical movement action tests.
  - Added failing test first: `RelocationEvaluationReportsGraphEdgeFactsForAdjacentDestination` requires adjacent relocation evaluation to expose destination node ID and edge kind for an entity-policy movement edge.
  - Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~CoreRelocationTests"` failed because `RelocationEvaluation` had no `DestinationNodeId` or `EdgeKind` properties (`CS1061`).
- Implemented:
  - Added optional `DestinationNodeId` and `EdgeKind` projections to `RelocationEvaluation`.
  - `MovementService.EvaluateRelocation` now resolves a graph-native movement edge for adjacent movement destinations and carries destination node ID and edge kind through success and failure evaluations.
  - `MovementService.TryResolveDestination` now uses `TryGetMovementEdge` for adjacent destinations before falling back to projected graph-neighbor failure facts.
- Behavior impact: lower-level relocation/action evaluation now carries graph edge facts for adjacent movement-like destinations, so higher-level Move/Push/Exit evaluations inherit the same graph fact source through relocation.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~CoreRelocationTests|FullyQualifiedName~MovementServiceGraphNodeTests|FullyQualifiedName~ControlledActorCommandServiceTests"` passed: 18 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~CoreRelocationTests|FullyQualifiedName~MovementServiceGraphNodeTests|FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~CoreActionChoiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CanonicalMovementActionStepTests"` passed: 64 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 356 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 274 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: adjacent relocation destination resolution now uses one `MovementEdgeResult` for destination coordinate, destination node ID, edge kind, and blocked/failure edge facts instead of separate graph-neighbor projection.
- Friction/blockers: none.

### 2026-08-08 - Phase 8 adjacency edge-fact projection

- TDD/trace:
  - Affected invariants: graph-first adjacency semantics, source-cell directional uniqueness, entity topology policy adjacency, pickup/enter/transfer adjacency consumers, controlled affordance/action-choice adjacency target filtering, and coordinate projection compatibility.
  - Existing tests revised/reviewed: `TopologyServiceTests`, `CoreRelocationTests`, `ControlledActorAffordanceServiceTests`, `CoreActionChoiceTests`, `ControlledActorCommandServiceTests`, and targeting/action-step tests.
  - Added/revised failing tests first:
    - `TopologyGraphResolvesEntityTopologyPolicyWithoutEntityTopologyWrapper` now requires `MovementService.EvaluateAdjacency` to expose source node ID, destination node ID, and edge kind for an entity-policy edge.
    - Source-cell-link topology adjacency coverage now also requires source node ID, destination node ID, and edge kind for a source-cell link.
  - Red verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyGraphResolvesEntityTopologyPolicyWithoutEntityTopologyWrapper"` failed because `AdjacencyEvaluation` had no `EdgeKind`, `SourceNodeId`, or `DestinationNodeId` properties (`CS1061`).
- Implemented:
  - Added optional `SourceNodeId`, `DestinationNodeId`, and `EdgeKind` projections to `AdjacencyEvaluation`.
  - `MovementService.EvaluateAdjacency` now queries the materialized graph edge directly for adjacency result node/edge projections, preserving existing coordinate-facing success/failure semantics.
  - Default coordinate adjacency fallback remains projection-only and does not invent graph IDs when no graph edge matched.
- Behavior impact: adjacency consumers can now use graph node/edge facts from the existing `EvaluateAdjacency` API while old boolean/direction/failure fields remain compatible.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyGraphResolvesEntityTopologyPolicyWithoutEntityTopologyWrapper|FullyQualifiedName~TopologyGraphResolvesSourceCellLinksWithoutSourceCellLinkWrapper"` passed: 2 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~CoreRelocationTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests|FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~TargetingActionStepTests"` passed: 84 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 356 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 274 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: no old adjacency API was deleted in this slice; the existing API now projects graph-native facts so callers can migrate without extra coordinate/node lookup calls.
- Friction/blockers: none.

### 2026-08-08 - Phase 8 adjacent entity consumers use adjacency graph facts

- TDD/trace:
  - Affected invariants: controlled actor affordance adjacent target facts, Action Choice adjacent push target facts, source-cell-link adjacency, graph-first adjacency semantics, and coordinate projection compatibility.
  - Existing tests revised/reviewed: `ControlledActorAffordanceServiceTests`, `CoreActionChoiceTests`, `TopologyServiceTests`, and targeting/action-step adjacency tests.
  - Added/revised failing tests first:
    - `ControlledActorAffordancePickupSourceReportsGraphAdjacencyFacts` requires pickup source affordances found through a source-cell-link adjacency to expose source node ID and edge kind.
    - `ActionChoiceRequestExposesPushTargetsAndTargetRelativeDirections` now requires push target entity options to expose adjacency source node ID and edge kind.
  - Red verification: focused targeted tests failed because `ControlledActorEntityAffordance` had no `SourceNodeId` or `EdgeKind` properties (`CS1061`).
- Implemented:
  - Added optional `SourceNodeId` and `EdgeKind` projections to `ControlledActorEntityAffordance`.
  - `ControlledActorAffordanceService.QueryPickupSources` now filters through `MovementService.EvaluateAdjacency` and carries adjacency destination node/edge facts onto entity affordances.
  - `ControlledActorAffordanceService.QueryEnterTargets` now filters through `MovementService.EvaluateAdjacency` and carries adjacency destination node/edge facts onto entity affordances.
  - `ActionChoiceService.QueryPushTargets` now filters through `MovementService.EvaluateAdjacency` and carries adjacency destination node/edge facts onto push target entity options.
  - Removed same-plane prefilters from these adjacent-target queries so graph adjacency, not plane-coordinate equality, controls candidate inclusion.
- Behavior impact: adjacent entity affordance/choice options now expose graph adjacency facts and can include topology-adjacent targets even when coordinate plane/projection checks would have rejected them.
- Verification:
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ControlledActorAffordancePickupSourceReportsGraphAdjacencyFacts|FullyQualifiedName~ActionChoiceRequestExposesPushTargetsAndTargetRelativeDirections|FullyQualifiedName~ActionChoiceRequestExposesEnterTargetsFromAuthoredEnterStep"` passed: 3 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests|FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~TargetingActionStepTests|FullyQualifiedName~ActionPlanInterpreter.InventoryHandlers"` passed: 72 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 357 tests.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 274 tests.
  - `dotnet run --project src/GameGameGame.Documentation/GameGameGame.Documentation.csproj -- lint` passed.
- Subtractive cleanup: removed plane-coordinate prefilters and duplicate boolean-only adjacency calls from pickup-source, enter-target, and push-target candidate construction; they now consume graph fact-bearing adjacency evaluations.
- Friction/blockers: none.

### 2026-08-09 - Phase 8 final action-internal graph fact propagation

- TDD/trace:
  - Affected invariants: directed/source-cell topology adjacency is authoritative for adjacency-based interactions such as pickup; actions produce structured traces for failed checks and resolutions; canonical Action Steps preserve documented state contracts for inventory transfer/enter/pickup behavior.
  - Existing tests reviewed/preserved: `PickupRejectsIntercardinalTargetWhenBothCornersAreBlocked`, `EntityTopologyPolicyOutwardAdjacencySupportsPickupAcrossInventoryBoundary`, `CanonicalTransferActorToTargetUsesSelectedMovingEntityAndFacingCounterparty`, `SubmitTransferChoiceUsesTopologyDirectionForCounterparty`, `PrimitivePickupTargetPicksUpPersistentActorTarget`, `PrimitivePickupTargetUsesFirstAvailableInventoryCoordinateRowMajor`, `EnterTargetReportsTargetInventoryMissingWithTargetCentricReason`.
  - Added failing tests first in `GraphFirstActionTraceTests`:
    - `PickupActionTraceReportsGraphAdjacencyFacts`.
    - `EnterActionTraceReportsGraphAdjacencyFacts`.
    - `TransferActionCounterpartyTraceReportsGraphMovementEdgeFacts`.
    - `PrimitivePickupTargetTraceReportsGraphAdjacencyFacts`.
  - Red verification: `dotnet test "tests/GameGameGame.Tests/GameGameGame.Tests.csproj" --filter "FullyQualifiedName~GraphFirstActionTraceTests"` failed 4/4 because action traces did not expose source node, destination node, or edge kind details for graph-resolved source-cell-link adjacency.
- Implemented:
  - Direct `PickupAction` and `EnterAction` adjacency success traces now include graph adjacency facts (`sourceNode`, `destinationNode`, `edge`, and direction).
  - `TransferAction.TryResolveCounterparty` now resolves the selected counterparty direction with `MovementService.TryGetMovementEdge`, preserves the graph edge destination projection for the legacy occupancy lookup, and traces the graph movement edge facts.
  - `ActionPlanInterpreter.InventoryHandlers` pickup-target primitive now records the same graph adjacency facts on its adjacency trace child; Enter/Transfer primitives inherit graph facts through direct action resolution traces.
  - Added `ActionTrace.FormatAdjacencyFacts` and `ActionTrace.FormatMovementEdgeFacts` helpers so action-internal traces use one graph-fact detail shape.
  - Marked coordinate `MovementService` movement-destination and adjacency APIs as compatibility adapters in code comments; no public API deletion was safe because remaining frontend-neutral/Core DTO construction still projects legacy coordinates.
  - Content/editor DTO audit: no additional Content-owned DTO changes were required in this final slice because the Phase 8 graph node/edge facts already live on Core frontend-neutral DTOs consumed by Content/frontends; docs were updated instead.
- Behavior impact: Phase 8 direct action internals/traces now make graph identity visible for pickup, enter, transfer-counterparty resolution, and primitive pickup, while coordinate fields remain projections/adapters.
- Verification:
  - `dotnet test "tests/GameGameGame.Tests/GameGameGame.Tests.csproj" --filter "FullyQualifiedName~GraphFirstActionTraceTests"` passed: 4 tests.
  - `dotnet test "tests/GameGameGame.Tests/GameGameGame.Tests.csproj" --filter "FullyQualifiedName~GraphFirstActionTraceTests|FullyQualifiedName~CoreInvariantTests|FullyQualifiedName~PrimitiveActionPlanInterpreterTests|FullyQualifiedName~CanonicalTransfer|FullyQualifiedName~CoreActionChoiceTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~ControlledActorCommandServiceTests|FullyQualifiedName~TopologyServiceTests"` passed: 117 tests.
  - `dotnet test "tests/GameGameGame.Tests/GameGameGame.Tests.csproj" --filter "Suite=Core"` passed: 361 tests.
  - `dotnet test "tests/GameGameGame.Tests/GameGameGame.Tests.csproj" --filter "Suite=Content"` passed: 274 tests.
  - `dotnet run --project "src/GameGameGame.Documentation/GameGameGame.Documentation.csproj" -- lint` passed.
- Subtractive cleanup: rewrote transfer counterparty resolution away from coordinate-first `TryGetMoveDestination` to graph-native `TryGetMovementEdge`; retained coordinate occupancy lookup only as the current projection/compatibility layer.
- Friction/blockers: none so far.
- Phase 8 completion decision: after this final action-internal propagation slice, graph-native movement/adjacency facts are exposed through movement APIs, direct action internals, action-choice/controlled-affordance/controlled-command DTOs, and relevant traces. Remaining coordinate-returning APIs are explicitly compatibility/projection adapters and are not blockers for closing Phase 8.
