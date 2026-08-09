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
  - Existing tests: `DefaultTopologyReturnsCardinalNeighborAndReportsOutOfBounds`, `DefaultTopologyReturnsUnblockedIntercardinalNeighbor`, `DefaultTopologyReportsTwoCornerIntercardinalBlock`, `DefaultTopologyEnumeratesEightDirectionsInStableOrder`, `AdjacencyAllowsUnblockedIntercardinalNeighbor`, `AdjacencyRejectsIntercardinalNeighborWhenBothCornersAreBlocked`.
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
- Existing default grid neighbor facts can be represented as graph edges without changing `DefaultTopologyService` behavior.
- Existing source-cell links materialize as graph edges.

### Invariant/test trace

- Affected invariants: entity locations as nodes, one occupant per node, plain adjacency, directed/source-cell topology uniqueness.
- Existing tests to preserve/review: `EntityLocationsAreRepresentedByNodeOccupancy`, `MovementCannotPlaceEntityOnOccupiedNode`, `DefaultTopologyReturnsCardinalNeighborAndReportsOutOfBounds`, `DefaultTopologyEnumeratesEightDirectionsInStableOrder`, `SourceCellLinksConnectAuthoredInventoryCellsBidirectionally`, `TopologyDirectionalUniquenessAcceptsUniqueAndDuplicateIdenticalEdges`, `TopologyDirectionalUniquenessRejectsConflictingDestinationsForSameCellAndDirection`.
- New failing tests first:
  1. graph materialization distinguishes source nodes even when layout/display projection collides;
  2. graph materialization emits default grid edges equivalent to current default neighbor facts;
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
