---
id: plan.merged-topology-clean-implementation-sprint
title: Merged Topology Clean Implementation Sprint Plan
kind: plan
status: archived
truth_rank: 40
truth_domains: [planning-priority, implementation-navigation, test-trace]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - implementing the first clean merged inventory topology slice after the topology spike/refactor work
  - adding Core source-cell topology links or Content aligned joins
  - validating directional uniqueness for authored topology
related:
  - source.invariants
  - source.testing-charter
  - source.engine-editor-capabilities
  - source.vertical-slice-map
  - plan.topology-service
---

# Merged Topology Clean Implementation Sprint Plan

Status: Active clean behavior-adding sprint plan for the first merged inventory topology slice after the spike and behavior-preserving refactor sprint.

Reference implementation history:

- `docs/Archived/Merged-Topology-Refactor-Sprint-Plan.md` records the behavior-preserving refactor runway: topology fact vocabulary, directional uniqueness helper, coordinate vocabulary, Content mapper seam, and topology visibility projection seam.
- `docs/Archived/Topology-Service-Phase-1-Sprint-Plan.md` records the earlier topology-service foundation.

## Goal

Implement the smallest stable Core/Content slice that enables authored room-to-hall topology without importing the full spike prototype:

1. Core source-cell links over existing inventory planes.
2. Content semantic aligned joins resolving to source-cell links.
3. Directional uniqueness validation using `TopologyDirectionalUniqueness`.
4. One small room-to-hall scenario demonstrating a 3x3 room center doorway connected to a 5x1 hallway endpoint.

## Non-goals / deferred work

- Do not implement overlap-enabled layout mode in this sprint.
- Do not implement flagship folded-house content in this sprint.
- Do not implement generated placement, random generation, true Möbius transforms, sheet IDs, index reversal, facing transforms, one-way links, or general portal/entanglement systems.
- Do not change SadConsole rendering behavior.
- Do not implement richer topology-aware POV rendering beyond the existing projection seam.
- Do not promote the spike's whole YAML surface wholesale; add only the fields needed for aligned joins/source-cell links.

## TDD and behavior-change contract

This is planned semantic Core/Content work and must follow `docs/Source of Truth/testing-charter.md`.

Do not start production code changes until failing/revised tests exist for the current phase. Each phase should add the smallest testable behavior first, then implement the smallest coordinated Core/Content change needed to pass.

## Invariant/test trace

Affected invariants from `docs/Source of Truth/invariants.md`:

- `Entity locations are represented by occupancy of nodes in planes.`
  - Existing tests: `EntityLocationsAreRepresentedByNodeOccupancy`, `MovementCannotPlaceEntityOnOccupiedNode`.
- `At most one entity may occupy a node at a time.`
  - Existing tests: `MovementCannotPlaceEntityOnOccupiedNode`, `PrototypeRegistryValidationReportsOverlappingCarriedEntities`.
- `Plain adjacency means eight-way cardinal or intercardinal adjacency unless a contract explicitly says cardinal-only...`
  - Existing tests: `DefaultTopologyReturnsCardinalNeighborAndReportsOutOfBounds`, `DefaultTopologyReturnsUnblockedIntercardinalNeighbor`, `DefaultTopologyReportsTwoCornerIntercardinalBlock`, `DefaultTopologyEnumeratesEightDirectionsInStableOrder`, `AdjacencyAllowsUnblockedIntercardinalNeighbor`, `AdjacencyRejectsIntercardinalNeighborWhenBothCornersAreBlocked`.
- `Authored topology policy may add directed inventory-boundary adjacency...`
  - Existing tests: `EntityTopologyPolicyConnectsInventoryEdgeOutwardToExteriorAdjacency`, `EntityTopologyPolicyConnectsExteriorAdjacencyInwardToPreferredInventoryEdgeCell`, `EntityTopologyPolicyConnectsIntercardinalExteriorAdjacencyToInventoryCorners`, `ControlledActorAffordanceMovementReportsEntityTopologyOutwardDestination`, `ControlledActorAffordanceMovementReportsEntityTopologyInwardDestinationInsteadOfContainerBump`, `ActionChoiceRequestMoveOptionsExposeEntityTopologyDestinations`.
- Future/clean merged topology must preserve the spike's hard rule even before adding it as a stable invariant: every resolved `(source cell, direction)` must have zero or one destination.
  - Existing helper tests: `TopologyDirectionalUniquenessAcceptsUniqueAndDuplicateIdenticalEdges`, `TopologyDirectionalUniquenessRejectsConflictingDestinationsForSameCellAndDirection`.
- `YAML content loads from strings and files into registries that can be validated.`
  - Existing tests: `YamlContentLoaderCreatesRegistryFromDeclarativeContent`, `YamlContentLoaderCanLoadRegistryFromFile`, `PrototypeRegistryValidationPassesForBuiltInContent`.
- `Editable content documents round-trip through materialization and saved YAML.`
  - Existing tests: `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`, `MergedInventoryLayerDocumentMapperRoundTripsCurrentTopologyDtoShape`.
- `Frontend editor snapshots and service-backed template/action-plan mutations expose authored scenarios...`
  - Existing tests: `FrontendEditorServiceTests`, `FrontendEditorServiceAndAgentApiShareContentEditorSessionAsParallelSurfaces`, relevant `AgentContentEditorApiTests` and content tool dispatcher tests.

Existing tests to preserve/review before implementation:

- Core topology/movement: `TopologyServiceTests`, `CoreAdjacencyTests`, `ControlledActorAffordanceServiceTests`, `CoreActionChoiceTests`.
- Content/editor: `YamlContentLoaderTests`, `EditableContentDocumentTests`, `AgentContentEditorApiTests`, `FrontendEditorServiceTests`, `ScenarioToolingServiceTests`.

New tests planned:

1. Core source-cell link traversal test: a rock/actor in the east-middle cell of a 3x3 room moves East into the west endpoint of a 5x1 hallway, and can move West back.
2. Content YAML aligned join test: `joins: [{ from: { owner: roomA, edge: East }, to: { owner: hallAB, edge: West }, align: Center }]` resolves to the expected source-cell link.
3. Directional uniqueness validation test: two joins/links from the same source cell/direction to different destinations are invalid and report actionable diagnostics.
4. Scenario materialization/smoke test for a small room-to-hall authored scenario, if the scenario is added during this sprint.

## Proposed stable vocabulary for this slice

Use the refactor vocabulary where possible:

- **Source cell link**: runtime/Core topology edge between two concrete inventory source cells, each with an explicit outgoing direction.
- **Aligned join**: Content authoring sugar that references owner edges plus deterministic alignment and resolves to one or more source-cell links.
- **Topology layout coordinate**: not part of this sprint's required behavior; avoid adding layout/overlap semantics yet.

Candidate model names may change during implementation, but avoid spike-only names if they obscure the stable contract.

## Phase 1: Core source-cell links

### Intent

Add Core support for explicit links between source inventory cells, without adding Content authoring yet.

### Testable outcomes

- Core test builds a world with a 3x3 room inventory and 5x1 hallway inventory.
- A source-cell link connects room east-middle `(2,1)` East to hallway west endpoint `(0,0)` West.
- Movement follows the link in both directions.
- Existing default grid/entity-topology behavior remains unchanged.

### Verification

- Targeted Core link test.
- `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~CoreAdjacencyTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests"`
- `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"`

## Phase 2: Content aligned joins and directional uniqueness validation

### Intent

Add Content authoring for semantic aligned joins and resolve them to Core source-cell links during validation/materialization.

### Testable outcomes

- YAML `joins` with `align: Center` resolves 3x3 room East to 5x1 hallway West as a single source-cell link from room `(2,1)` East to hallway `(0,0)` West.
- Directional conflicts from authored joins/links are rejected using `TopologyDirectionalUniqueness`.
- Missing/non-contributing owners, non-cardinal endpoints, invalid offsets/lengths, and unusable inventories receive diagnostics where applicable.

### Verification

- Targeted YAML/validation tests.
- `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"`
- Relevant Core targeted tests if materialization touches runtime model.

## Phase 3: Editor/agent/frontend summary parity

### Intent

Expose the minimal aligned-join/source-cell-link authoring shape through shared Content/editor services and snapshots.

### Testable outcomes

- Editable document round-trip preserves joins/links.
- `ContentEditorService` and `AgentContentEditorApi` can author/list the shape using the centralized mapper seam.
- Frontend editor summaries expose the data without owning topology semantics.

### Verification

- Targeted editable/editor/agent/frontend service tests.
- `Suite=Content`.

## Phase 4: Small scenario/materialization slice

### Intent

Author or add an inline materialization fixture for one small room-to-hall scenario.

### Testable outcomes

- Scenario materializes source-cell links into `WorldState`.
- A manual-demoable room-to-hall content scenario exists if content permissions/workflow permit.
- Validation/materialization reports zero diagnostics for the scenario.

### Verification

- Targeted scenario materialization test.
- Content validation/manifest validation if authored Beta content is added.
- `Suite=Content` and `Suite=Core`.

## Follow-up after this sprint

If this clean slice is stable, subsequent plans can add, in order:

1. Overlap-enabled topology mode with same-contributor movement and explicit cross-contributor links.
2. Folded-house/flagship content.
3. Shared topology visibility projection improvements for local actor-relative topology views.
4. Richer frontend rendering only after shared projection facts stabilize.

## Phase log

Use this section to record completed turns, verification commands, friction, and any changes to the recommended follow-up order.

### Phase 1 - Core source-cell links

- Added failing Core traversal test `SourceCellLinksConnectAuthoredInventoryCellsBidirectionally` for a 3x3 room east-middle cell `(2,1)` linked East to a 5x1 hallway west endpoint `(0,0)`, then back West.
- Implemented `SourceCellLink`, `WorldState.SourceCellLinks`, clone/restore copying, `SourceCellLinkTopologyService`, and default `MovementService` topology composition so explicit source-cell links resolve before existing merged/entity/default topology behavior.
- Verification:
  - initial focused test failed for missing `WorldState.SourceCellLinks` / `SourceCellLink` as expected.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~SourceCellLinksConnectAuthoredInventoryCellsBidirectionally"` passed: 1 passed.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests|FullyQualifiedName~CoreAdjacencyTests|FullyQualifiedName~ControlledActorAffordanceServiceTests|FullyQualifiedName~CoreActionChoiceTests"` passed: 66 passed.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 356 passed.

### Phase 2 - Content aligned joins and directional uniqueness validation

- Added failing tests for YAML aligned joins, directional uniqueness validation, and scenario materialization to `WorldState.SourceCellLinks`.
- Implemented `MergedInventoryJoinAlignment`, `MergedInventoryJoinEndpoint`, `MergedInventoryAlignedJoin`, YAML DTO/mapper support, `MergedInventoryAlignedJoinResolver`, merged-layer validation for cardinal center joins, and directional conflict checks via `TopologyDirectionalUniqueness`.
- Scenario materialization now resolves valid aligned joins to Core `SourceCellLink` entries after carried entities and inventory planes are materialized.
- Updated capability/authoring/invariant docs to record first-slice YAML `joins` support and the resolved `(source cell, direction)` uniqueness invariant.
- Verification:
  - initial focused tests failed for missing `MergedInventoryLayerDefinition.Joins` / `MergedInventoryJoinAlignment` as expected.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~YamlContentLoaderLoadsAlignedMergedLayerJoin|FullyQualifiedName~ContentValidationRejectsMergedLayerJoinDirectionalConflict|FullyQualifiedName~ScenarioMaterializerResolvesAlignedMergedLayerJoinsToSourceCellLinks"` passed: 3 passed.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~YamlContentLoaderTests|FullyQualifiedName~EditableContentDocumentTests|FullyQualifiedName~ScenarioMaterializerResolvesAlignedMergedLayerJoinsToSourceCellLinks|FullyQualifiedName~SourceCellLinksConnectAuthoredInventoryCellsBidirectionally"` passed: 41 passed.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 268 passed.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 356 passed.

### Phase 3 - Editor/agent/frontend summary parity

- Added failing parity tests for editable mapper join round-trip, agent API merged-layer join authoring/listing, and frontend editor snapshot join summaries.
- Extended `AgentMergedInventoryLayerDefinition` with optional `AgentMergedInventoryAlignedJoin` data and mapped it through `ContentEditorService.UpsertMergedInventoryLayer` via the existing centralized mapper seam.
- Extended `FrontendEditorMergedInventoryLayerSummary` with join summaries so frontend/editor consumers can list `from`/`to` owners, edges, and alignment without owning topology semantics.
- Updated capability/authoring docs to record editor/agent/frontend preservation/listing parity for first-slice merged-layer joins.
- Verification:
  - initial focused tests failed for missing agent join DTOs and frontend summary joins as expected.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~MergedInventoryLayerDocumentMapperRoundTripsCurrentTopologyDtoShape|FullyQualifiedName~AgentContentEditorApiAuthorsMergedInventoryLayerPlacements|FullyQualifiedName~SnapshotIncludesMergedInventoryLayerJoinSummaries"` passed: 3 passed.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~EditableContentDocumentTests|FullyQualifiedName~AgentContentEditorApiTests|FullyQualifiedName~FrontendEditorServiceTests|FullyQualifiedName~YamlContentLoaderTests"` passed: 136 passed.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 269 passed.

### Phase 4 - Small scenario/materialization slice

- Authored `src/GameGameGame.Content/Beta/Topology/RoomHallAlignedJoinShowcase.yaml` with scenario `beta-room-hall-aligned-join`, carried inventory owners `roomHallRoomA` (3x3) and `roomHallHallAB` (5x1), and a semantic merged-layer `joins` entry from Room A East to Hall AB West with `align: Center`.
- Registered the showcase in the Beta manifest delta section for scenario browsing/packaging.
- Content-authoring friction: the documented YAML `joins` shape was sufficient for the small room-to-hall scenario. Minor validation friction: an explicitly empty passive behavior chain (`steps: []`) is rejected for new content, so passive room/hall owners were authored by omitting default action plans instead.
- Verification:
  - initial focused test failed because `RoomHallAlignedJoinShowcase.yaml` did not exist as expected.
  - content-editor verified `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~RoomHallAlignedJoinShowcaseLoadsValidatesAndMaterializesSourceCellLink"` passed.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~RoomHallAlignedJoinShowcaseLoadsValidatesAndMaterializesSourceCellLink|FullyQualifiedName~ScenarioCatalogValidationReportsCuratedManifestIssues|FullyQualifiedName~ScenarioCatalogLoadsCuratedManifestSectionsAndEntryMetadata|FullyQualifiedName~ScenarioCatalogRebasesRepositoryManifestPathsWhenLoadedFromPackagedContent"` passed: 4 passed.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed: 269 passed.
  - `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed: 356 passed.
