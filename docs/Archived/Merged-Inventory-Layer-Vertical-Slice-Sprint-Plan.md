---
id: plan.merged-inventory-layer-vertical-slice-sprint
title: Merged Inventory Layer Vertical Slice Sprint Plan
kind: plan
status: archived
truth_rank: 40
truth_domains: [planning-priority, implementation-navigation, test-trace]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - implementing merged inventory topology layers
  - changing topology, movement, containment-owner derivation, Enter/Exit, or POV current-place behavior
  - authoring or validating content where multiple entity inventories form one interior space
related:
  - source.invariants
  - source.testing-charter
  - source.engine-editor-capabilities
  - source.vertical-slice-map
  - plan.topology-service
---

# Merged Inventory Layer Vertical Slice Sprint Plan

Status: Archived sprint/spike implementation log for the first Core/Content/Editor merged inventory topology slice. The spike proved explicit merged inventory layers, seams, overlap-enabled topology, aligned joins, and the flagship folded-house scenario. Current findings and wrap-up recommendations live in `docs/Plans/Merged-Inventory-Topology-Spike-Findings.md`; production code changes must still follow the TDD workflow in `docs/Source of Truth/testing-charter.md`.

## Goal

Allow authored entity-owned inventory spaces to behave like one durable navigable interior layer. The MVP acceptance scenario is:

1. A player starts outside two entities, A and B.
2. A and B each own an inventory space.
3. Content authors fuse those inventories into one merged layer by explicitly placing both inventory rectangles into shared layer coordinates.
4. The player enters A.
5. The player moves across the internal seam from A-owned cells into B-owned cells.
6. The player exits and appears outside adjacent to B, because the player's current cell belongs to B's inventory contribution.

This proves the "inventory spaces as rooms / entities as gates" model without implementing generated corridors, neutral cells, or complex runtime layer mutation.

## Design framing

Merged inventory layers are not authored as teleport actions or one-off portals. They are durable topology plans composed from existing entity inventory cells:

- a **merged layer** is a single runtime topology/distance space;
- a **space contribution** is one entity's existing inventory rectangle placed into that layer;
- each traversable layer cell maps back to exactly one source inventory coordinate and therefore one local owner entity;
- crossing from one contribution to another is normal movement, not Enter/Exit, Pickup/Drop, Give/Take, or Transfer;
- Enter/Exit policies remain properties of the entity that owns a space and apply only when crossing from exterior space into that owner's contribution or from that contribution back outside.

This feature supersedes the current experimental `EntityTopologyPolicy` direction for final authored merged-interior topology, but the sprint does not require removing `EntityTopologyPolicy` compatibility.

## Scope

### In scope

1. Core model for a merged layer composed from two or more entity-owned inventory spaces; the initial acceptance scenario still uses two gates for clarity.
2. Explicit coordinate placement of each contribution into one shared layer coordinate plan.
3. No overlaps: every layer cell resolves to exactly one source inventory coordinate.
4. Cross-contribution movement through shared layer adjacency for cardinal and intercardinal neighbors, using the same two-corner intercardinal blocking semantics where applicable.
5. Local owner derivation from the actor's current resolved source inventory coordinate.
6. Enter through one owner, move across the seam, and exit through the other owner.
7. Durable layer behavior when contributing owners move, are picked up, enter another entity, or separate in exterior space.
8. Conservative destruction rule: destroying an entity that contributes to an active merged layer is blocked for the MVP.
9. Content YAML load/round-trip and validation support for the MVP authoring shape.
10. Editor/shared service and agent API exposure sufficient to inspect and author the MVP shape.
11. A focused content scenario/fixture that demonstrates the acceptance flow without pinning transient balance or presentation choices.
12. Source-of-truth updates after implementation to record stable behavior and test coverage.

### Out of scope / follow-up

- Generated corridors or any generated/new walkable cells.
- Layer-owned neutral cells.
- Overlapping contributions.
- Edge-join shorthand such as `A.East -> B.West alignment: Top`.
- Automatic runtime rearrangement/recombination if a contribution is removed.
- Orphaned cells after owner destruction.
- Rich SadConsole visualization of merged layers beyond consuming shared projection facts.
- General portal/entanglement systems unrelated to merged inventory layers.

## Proposed authoring model

The first slice should use explicit placement only. Example canonical YAML shape:

```yaml
mergedLayers:
  sharedInterior:
    spaces:
    - owner: entityA
      origin:
        x: 0
        y: 0
    - owner: entityB
      origin:
        x: 3
        y: 0
```

If A's inventory is `3x3` and B's inventory is `2x2`, this resolves to:

```text
ooooo
ooooo
ooo
```

Moving B's origin to `{ x: 3, y: 1 }` resolves to:

```text
ooo
ooooo
ooooo
```

The resolved layer is durable and should be previewable by editor/agent tooling. Later sprints may add deterministic edge-join authoring sugar, but the canonical runtime/content form remains explicit placements in a shared coordinate plan.

## Core-owned TDD trace before implementation

Implementation must not start until intentionally failing tests are added or revised for the step being implemented. The trace below satisfies the pre-implementation trace required by `docs/Source of Truth/testing-charter.md`.

### Affected invariants

Existing invariants affected by the MVP:

- `Entity locations are represented by occupancy of nodes in planes.`
- `At most one entity may occupy a node at a time.`
- `Plain adjacency means eight-way cardinal or intercardinal adjacency unless a contract explicitly says cardinal-only...`
- `Authored topology policy may add directed inventory-boundary adjacency...` Existing topology-policy behavior must remain compatible while merged layers are introduced as a separate/final topology direction.
- `Traversals through containment or inventory relationships must be cycle-safe.`
- `Point-of-view queries for an arbitrary observer entity must reuse cycle-safe containment breadcrumbs...`
- `Nested Enter/Exit transitions intentionally cross inventory owner apertures on both sides of the move...`
- `Constrained inventory-boundary transformations respect nullable entity inventory policies...`
- `Canonical Action Steps must preserve their documented state contracts for Facing, Target, movement, target selection, inventory transfer, fallthrough, and deterministic tie-breaks.`
- `Controlled actor commands for direct player/frontend input resolve through a shared Core service for move, wait, pickup, drop, enter, exit, transfer, and push...`
- `Controlled actor affordance queries for direct player/frontend input expose Core-derived move, pickup, drop, enter, exit, transfer, and push choices...`
- `Entity panel projections combine inspection, containment path, point-of-view current-place facts...`
- `YAML content loads from strings and files into registries that can be validated.`
- `Editable content documents round-trip through materialization and saved YAML.`
- `Frontend editor snapshots and service-backed template/action-plan mutations expose authored scenarios, entity templates... and save results through shared content/editor services...`
- `Scenario runs use shared Content/Core services and schedule contained actors deterministically for scenario-root inventory spaces.`

New invariant to add after implementation when behavior is stable:

- Authored merged inventory layers compose existing entity inventory spaces into a durable unified topology. Each layer cell maps to exactly one source inventory coordinate/local owner; internal seam traversal is ordinary movement and does not consult Enter/Exit policies; Enter/Exit uses the local owner of the actor's current contribution cell; active contributing owners cannot be destroyed in the MVP.

### Existing tests to review/preserve

Core topology and movement:

- `DefaultTopologyReturnsCardinalNeighborAndReportsOutOfBounds`
- `DefaultTopologyReturnsUnblockedIntercardinalNeighbor`
- `DefaultTopologyReportsTwoCornerIntercardinalBlock`
- `DefaultTopologyEnumeratesEightDirectionsInStableOrder`
- `AdjacencyAllowsUnblockedIntercardinalNeighbor`
- `AdjacencyRejectsIntercardinalNeighborWhenBothCornersAreBlocked`
- `MovementCannotPlaceEntityOnOccupiedNode`
- `EntityTopologyPolicyConnectsInventoryEdgeOutwardToExteriorAdjacency`
- `EntityTopologyPolicyConnectsExteriorAdjacencyInwardToPreferredInventoryEdgeCell`
- `EntityTopologyPolicyConnectsIntercardinalExteriorAdjacencyToInventoryCorners`
- `ControlledActorAffordanceMovementReportsEntityTopologyOutwardDestination`
- `ControlledActorAffordanceMovementReportsEntityTopologyInwardDestinationInsteadOfContainerBump`
- `ActionChoiceRequestMoveOptionsExposeEntityTopologyDestinations`

Containment, Enter/Exit, and constrained movement:

- `EnterTargetFailsWhenActorBulkExceedsTargetAperture`
- `ExitFacingFailsWhenActorBulkExceedsContainerAperture`
- `DropFacingUsesApertureTransitionRules`
- `ControlledActorAffordanceQueryReportsEnterTargetsAndExitDirections`
- `ControlledActorAffordanceExitDirectionsUseTopologyNeighborDestinations`

POV/current-place and containment traversal:

- `PointOfViewUsesContainmentBreadcrumbsAndSelectsNearestContainerAsCurrentPlace`
- `PointOfViewReportsMissingObserverDiagnostic`
- `PointOfViewReportsNoCurrentPlaceWhenObserverHasNoContainingInventoryOwner`
- `PointOfViewPreservesBreadcrumbTruncationFromQueryOptions`
- `EntityContainmentPathServiceDetectsContainmentCycle`
- `EntityContainmentPathServiceSharedRootPathIsCycleSafe`

Content/editor parity:

- `YamlContentLoaderLoadsEntityEnterAndExitPolicies`
- `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`
- `ContentEditorServicePlacesAndMovesCarriedEntityInInventoryLayout`
- `PrototypeRegistryValidationReportsOverlappingCarriedEntities`
- `AgentContentEditorApiAuthorsInventoryBoundaryPolicies`
- `SetAndClearTemplateInventoryBoundaryPoliciesMutatesTemplatePolicies`

Scenario tooling:

- representative persisted scenario materialization and scenario run report tests that cover player-controlled placed entities and inventory summaries.

### Planned intentionally failing tests

Add or revise tests before production changes for these outcomes:

1. `MergedInventoryLayerConnectsTwoPlacedInventorySpacesAsOneTopology`
   - Builds a world with A `3x3` and B `2x2` placed into one layer at explicit origins.
   - Asserts adjacent cells across the seam are neighbors with stable directions.
   - Asserts non-touching cells are not adjacent.

2. `MergedInventoryLayerMovementCrossesSeamAndUpdatesLocalOwner`
   - Places an actor in an A-owned contribution cell.
   - Moves across the seam into B's contribution.
   - Asserts the actor's location resolves to B's inventory coordinate/local owner without executing Enter/Exit or Transfer semantics.

3. `MergedInventoryLayerAllowsEnterThroughOneOwnerAndExitThroughOtherOwner`
   - Full MVP acceptance test.
   - Actor enters A from exterior, moves across the seam, exits while standing in B-owned contribution cells, and appears adjacent to B.
   - Asserts A's `ExitPolicy` does not govern the final exit from B-owned cells and seam traversal does not consult either owner's Enter/Exit policy.

4. `MergedInventoryLayerDistanceTreatsPlacedSpacesAsOneRigidLayer`
   - Verifies topology traversal/flood or path-distance across the seam uses the resolved layer shape rather than containment hops or exterior owner positions.
   - Moving A or B externally does not change the measured interior distance.

5. `MergedInventoryLayerBlocksDestroyOfActiveContributingOwner`
   - Attempts to destroy A or B while its inventory contributes to an active layer.
   - Asserts the destroy action fails with a structured reason and leaves the layer and occupants intact.

6. `YamlContentLoaderLoadsMergedInventoryLayerPlacements`
   - Loads inline YAML with the MVP `mergedLayers` shape.
   - Asserts contribution owners and origins materialize into the registry/world model.

7. `ContentValidationRejectsMergedLayerOverlapDisconnectedOrInvalidOwner`
   - Rejects overlapping contribution cells, unknown owners, owners without usable inventory, and disconnected MVP placements.
   - Accepts two or more spaces while preserving overlap, disconnected, invalid-owner, and unusable-inventory diagnostics.

8. `EditableContentDocumentRoundTripsMergedInventoryLayers`
   - Ensures explicit placement authoring survives load/materialize/save/reload without losing origins or owners.

9. `AgentContentEditorApiAuthorsMergedInventoryLayerPlacements`
   - Adds or updates the shared editor/agent API for creating/inspecting the MVP layer and verifies YAML preview/diff reflects the layer.

10. `PersistedScenarioRunReportsMergedLayerTraversalAcceptance`
    - Uses a focused scenario fixture to exercise enter-A, move-across, exit-B through shared Content/Core services.
    - Report expectations should assert semantic final location/observations, not transient prototype balance or presentation details.

### Tests that may need revision instead of only adding new tests

- POV/current-place tests may need to derive current containing/local owner from resolved location rather than assuming a single direct inventory owner from the plane alone.
- Enter/Exit affordance tests may need to route exit candidates through the actor's resolved local owner when the actor is in a merged layer.
- Scenario inventory summaries may need to report local contribution ownership without recursively walking into stale parent assumptions.

Any revised test must be made intentionally failing for the new planned behavior before production code changes.

## Implementation phases

### Phase 1: Core resolved layer model and topology adjacency

- Add the smallest Core representation that can resolve two inventory contributions into a layer coordinate map.
- Integrate with topology neighbor/evaluate-adjacency queries while preserving default grid and `EntityTopologyPolicy` behavior.
- Add traversal/distance coverage across the seam.

### Phase 2: Location/local-owner derivation and movement

- Ensure movement across the seam relocates the actor to the destination source inventory coordinate.
- Add a service/API for resolving local owner/current contribution from runtime location.
- Keep entity location as the source of truth; POV consumes derived facts rather than owning them.

### Phase 3: Enter/Exit through local owner and destroy guard

- Route Exit from merged-layer cells through the resolved local owner.
- Confirm Enter places into the selected owner's contribution according to existing EnterPolicy semantics.
- Block destruction of active contributing owners with a structured failure.

### Phase 4: Content schema, validation, editor, and agent parity

- Load and validate MVP `mergedLayers` YAML.
- Preserve editable-document round-trip behavior.
- Expose shared editor service / agent API operations or snapshots sufficient for content authors to create and inspect the layer.

### Phase 5: Focused scenario and docs

- Add a small scenario fixture demonstrating enter A, cross seam, exit B.
- Update `docs/Source of Truth/invariants.md`, `docs/Source of Truth/Engine-Editor-Capabilities.md`, content authoring docs, and this plan with completed test coverage.

## Deferred design questions

- Should destroyed contributing owners eventually leave orphaned cells, remove their contribution, or have authored destruction behavior?
- What additional editor previews/visualization are needed for large three-or-more-parent complex spaces?
- What deterministic edge-join shorthand should the editor expose first: Start/Center/End alignment, explicit offset, or both?
- Should generated corridors be represented by synthetic owner entities, layer-owned neutral cells, or required author-supplied corridor owners?
- How should SadConsole visualize merged layers and ownership boundaries without duplicating Core topology semantics?
- For future non-Euclidean seam/link authoring and generated connections, every resolved `(cell, direction)` must still have zero or one neighbor before Core consumes the layer. Prototype Euclidean placement enforces this conservatively by rejecting duplicate or multi-layer source-space ownership; later graph/seam slices should reject directional conflicts or require explicit deterministic conflict resolution.

## Implementation notes and friction log

### Phase 1 turn 1: Core resolved layer topology adjacency

- Added intentionally failing Core tests first for explicit two-space layer seam adjacency and rigid flood distance across the seam.
- Mitigation/implementation strategy: keep Phase 1 isolated to an opt-in `MergedInventoryLayerTopologyService` and `WorldState.MergedInventoryLayers` model so existing `MovementService` defaults and experimental `EntityTopologyPolicy` behavior remain untouched until Phase 2 decides default composition.
- Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests"` passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed.
- Friction: the full `GameGameGame.Tests` suite currently fails at `ConsoleScenarioLaunchTests.FeedbackScenarioManifestEntriesLaunchAsPlayableSessions` for `beta-pocket-bazaar should be playable`. This is outside the Phase 1 Core topology path and also fails when run as the only selected test. Mitigation: record the broader-suite failure here, keep Phase 1 verification scoped to the targeted topology tests and Core suite, and revisit the console/catalog content failure separately before using full-suite status as a merge gate for this slice.

### Phase 2 turn 1: Movement seam traversal and local-owner resolution

- Added an intentionally failing Core test for moving an entity from an A-owned contribution cell across the seam into a B-owned contribution cell, then resolving the actor's local owner from its new source inventory coordinate.
- Mitigation/implementation strategy: factored merged-layer source-cell lookup into `MergedInventoryLayerResolver` so movement, future Enter/Exit, POV, and validation can consume the same source-of-truth derivation instead of duplicating parent/owner guesses. Updated the default `MovementService` topology stack to include `MergedInventoryLayerTopologyService` ahead of existing entity-topology/default-grid behavior; empty `WorldState.MergedInventoryLayers` preserves current behavior.
- Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests"` passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed.
- Friction: default topology composition now has a precedence decision: merged-layer cells are treated as part of the authored layer first, before experimental `EntityTopologyPolicy` outward/inward edges. Mitigation: keep this as the MVP rule because merged layers are intended to be the final authored interior topology; if content needs mixed behavior later, add explicit validation/docs rather than allowing ambiguous simultaneous policies silently.

### Phase 3 turn 1: Exit local-owner routing and destroy guard

- Added intentionally failing Core tests for exiting from a B-owned contribution after seam traversal and for blocking `DestroyTarget` against an active merged-layer contributing owner.
- Mitigation/implementation strategy: route `ExitAction` through `MergedInventoryLayerResolver.TryFindLocalOwner`, which falls back to ordinary inventory-plane ownership for non-merged spaces. Added `WorldState.TryFindMergedInventoryLayerContribution` and guarded `DestroyTarget` before recursive destruction so the action reports `InventoryPolicyBlocked` and leaves the layer contributor and inventory plane intact.
- Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~InventoryTransferActionStepTests|FullyQualifiedName~PrototypeActionStepReferenceTests|FullyQualifiedName~TopologyServiceTests"` passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed.
- Friction: root behavior-chain resolution still reports terminal failed steps according to existing plan-resolution semantics, so the destroy-guard test asserts semantic failure, preserved entities/planes, and structured `InventoryPolicyBlocked` detail rather than introducing a broader consuming-turn contract change in this topology sprint. If no-turn failed lifecycle actions are desired later, handle that under the canonical behavior-chain state-contract plan.

### Phase 4 turn 1: Content schema, validation, materialization, and agent/editor exposure

- Added intentionally failing Content/editor tests for YAML loading, validation of overlap/disconnection/unknown owners, editable-document round trip, scenario materialization into `WorldState.MergedInventoryLayers`, and agent API authoring/snapshot exposure.
- Mitigation/implementation strategy: keep the first authoring shape as top-level `mergedLayers` with explicit `spaces[].owner` entity IDs and `spaces[].origin` layer coordinates. Content validation resolves owner entity IDs from authored carried-entity layouts and validates the MVP contract: usable inventory owners, no overlapping layer cells, and one connected eight-way layer. Scenario materialization copies validated definitions into the world after spawning the scenario root and its carried entities.
- Added shared editor/API exposure through `ContentEditorService.UpsertMergedInventoryLayer`, `ListMergedInventoryLayers`, `AgentContentEditorApi.UpsertMergedInventoryLayer`, `AgentDocumentSnapshot.MergedInventoryLayers`, and `FrontendEditorSnapshot.MergedInventoryLayers`. The frontend snapshot is data-only; no SadConsole rendering behavior is added in this phase.
- Verification: targeted Phase 4 tests passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Editor"` passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed.
- Friction: running targeted tests and the Content suite in parallel caused a transient `CS2012` file-lock error on `GameGameGame.Content.dll`, likely from concurrent builds/Defender. Mitigation: reran the Content suite sequentially; it passed. Avoid parallel `dotnet test` invocations against the same project when verifying this slice on Windows.

### Edge-probing turn: current support boundaries

- Added Core edge tests showing the runtime topology currently supports different contribution dimensions/offsets, owners in different exterior rooms, and durable layer behavior when both owners move externally. These passed through the default `MovementService` stack and `ExitAction` local-owner routing.
- Added a Core-only test showing `MergedInventoryLayerTopologyService` can traverse a layer with three contributing spaces when the world is manually configured. At this point, Content/editor validation still rejected more than two spaces, identifying a deliberate authoring boundary rather than a Core topology limitation.
- Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests"` passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed.
- Friction/boundary: the first content fixture remains a two-gate manual demo for clarity even though Core can handle more contributors. Mitigation: add a small follow-up to relax Content validation and cover 3+ contributors with explicit Content tests before authoring larger examples.

### Edge expansion turn: authoring support for 3+ contributors

- Added an intentionally failing Content test for a valid three-contributor merged layer, then relaxed `PrototypeContentRegistry` validation from exactly two spaces to at least two spaces.
- Validation still checks the important authoring invariants for every contributor count: each owner entity ID must resolve from authored carried layouts, owners must have usable inventory spaces, layer cells must not overlap, and the final layer must be connected.
- Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~YamlContentLoaderTests.ContentValidationAllowsMergedLayerWithThreeContributors|FullyQualifiedName~YamlContentLoaderTests.ContentValidationRejectsMergedLayerOverlapDisconnectedOrInvalidOwner"` passed.

### Topology invariant hardening turn: directional uniqueness

- Promoted directional uniqueness to a hard topology invariant: every resolved `(cell, direction)` may have zero or one neighbor. Future seam/generator authoring must reject or explicitly resolve conflicts before Core movement, distance, POV, targeting, or interaction semantics consume the topology.
- For the current prototype, ambiguous source-space participation is the practical conflict class: if one owner inventory appears more than once in a layer or in more than one layer, the same source cell can map to multiple layer cells and therefore multiple possible directional neighborhoods. Validation now rejects those cases instead of relying on resolver enumeration order.

### Non-Euclidean seam experiment turn: Core seam links

- Added a Core-only seam experiment on `MergedInventoryLayer`: `MergedInventoryLayerSeam` connects two owner edges and creates symmetric edge-to-edge traversal. This is not yet YAML/editor authored.
- Verified prototype support for pacman-style self wrapping, rotational self mapping such as East edge to North edge, and multiple different edges connecting the same two spaces while preserving one neighbor per `(cell, direction)` in the tested cases.
- Current seam mapping is deterministic same-index edge mapping and supports cardinal edges only. Different edge lengths currently produce a blocked unresolved seam neighbor rather than generated interpolation or stretching.
- Verification: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~TopologyServiceTests"` passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed.
- Follow-up before content authoring: add seam validation for directional conflicts, edge-length mismatch diagnostics, cardinal-edge-only diagnostics, and YAML/editor authoring shape. The hard invariant remains that conflicts must be rejected or explicitly resolved before Core consumers see the layer.

### Non-Euclidean seam authoring turn: YAML/editor parity

- Added intentionally failing Content tests for YAML seam load/materialization and seam validation diagnostics, including seam-vs-seam conflicts, seam-vs-Euclidean placement conflicts, and edge-length mismatches.
- Implemented `mergedLayers.*.seams` across YAML DTOs, editable documents, editor service save/list operations, agent snapshots, frontend editor summaries, and scenario materialization into `WorldState.MergedInventoryLayers`.
- Validation now allows a one-space layer only when it has self-connected seams, rejects non-cardinal seam endpoints, missing/non-contributing seam owners, edge-length mismatches, duplicate/multi-layer source ownership, and directional conflicts before Core consumes authored topology. Connectivity considers both ordinary placement adjacency and seam links.
- Added compact Beta showcase scenarios for pacman wrap, rotational self mapping, and multi-edge three-room seams in `MergedInventoryLayerSeamShowcase.yaml` and the Beta manifest.
- Verification: targeted seam YAML tests passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed; `dotnet build src/GameGameGame.SadConsole/GameGameGame.SadConsole.csproj` passed.

### Planned overlap/Möbius loop experiment

Testable outcomes before implementation:

1. Core can traverse an explicitly linked overlapping-layout loop: A east to B, B south to C, C west to D, D north to E, E east to F, F south to G, G west to H, H north to A, returning to A after eight moves.
2. Content YAML can author that shape with overlapping layout origins and validate/materialize it into `WorldState.MergedInventoryLayers`.
3. Validation still rejects directional conflicts, including explicit links that would conflict with another link or with ordinary placement adjacency when ordinary placement adjacency is enabled.

Invariant/test trace:

- Affected invariant: `Resolved topology adjacency must be directionally unique...` Existing tests: `ContentValidationRejectsMergedLayerDuplicateOrMultiLayerOwner`, `ContentValidationRejectsMergedLayerSeamDirectionalConflictsAndLengthMismatch`, `ContentValidationRejectsMergedLayerSeamConflictsWithEuclideanPlacementAdjacency`, and Core seam traversal tests in `TopologyServiceTests`.
- Affected invariant: `YAML content loads from strings and files into registries that can be validated.` Existing tests: `YamlContentLoaderLoadsMergedLayerSeams`, `PrototypeRegistryValidationPassesForBuiltInContent`.
- Affected invariant: `Editable content documents round-trip through materialization and saved YAML.` Existing tests: merged-layer editable document roundtrip tests should keep preserving newly added fields.
- New tests needed: a Core overlapping-loop traversal test, a YAML load/materialization/validation test for overlap-enabled linked loops, and a validation test proving overlapping placement remains rejected unless the author explicitly opts into overlap-layout topology.

Initial experiment constraints:

- Treat overlap as layout/presentation overlap, not merged cell identity. Runtime cell identity remains the contributing owner inventory cell.
- Use current cardinal edge-to-edge same-index seam semantics for the first loop; true Möbius index reversal/facing transforms remain follow-up unless the 1x1 loop proves insufficient.
- To avoid ambiguous coordinate-derived neighbors, overlap-enabled layers disable ordinary Euclidean placement adjacency and rely on explicit seam/link topology for internal movement/connectivity.
- Frontend topology-aware rendering is out of this Core/Content slice; frontend can later hide cells outside the actor's current range/POV using shared projection facts.

### Overlap/Möbius loop experiment turn: explicit seam-only overlap topology

- Added an intentionally failing Core test for the eight-room loop `A east -> B south -> C west -> D north -> E east -> F south -> G west -> H north -> A`, with all 1x1 room contributions sharing the same layout origin.
- Added intentionally failing Content tests for `allowLayoutOverlap: true` YAML authoring and for preserving the default overlap rejection when that explicit opt-in is absent.
- Implemented `MergedInventoryLayer.AllowLayoutOverlap` across Core model, Content definition/YAML, editable DTOs, editor-service upsert/list, agent definitions, frontend editor summaries, and scenario materialization.
- Runtime rule: when `AllowLayoutOverlap` is true, normal Euclidean placement adjacency inside the layer is disabled; movement and adjacency use explicit seams only. This keeps overlapped layout coordinates presentation-only and avoids coordinate-derived ambiguity.
- Validation rule: overlap remains rejected by default. When enabled, connectivity is seam-only; seam validation still enforces cardinal endpoints, edge-length matches, owner membership, duplicate/multi-layer owner rejection, and one seam neighbor per `(source cell, direction)`.
- Added Beta scenario `delta-merged-layer-overlap-loop` to `MergedInventoryLayerSeamShowcase.yaml` and the Beta manifest for manual review.
- Verification: targeted overlap tests passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed; `dotnet build src/GameGameGame.SadConsole/GameGameGame.SadConsole.csproj` passed after stopping an existing locked SadConsole process.

### Planned aligned-join / partial-doorway experiment

Testable outcomes before implementation:

1. Core can traverse an explicit source-cell link from the center east doorway of a 3x3 room into the west end of a 5x1 hallway, then back again.
2. Content YAML can author a higher-level `joins` entry such as `roomA.East -> hallAB.West align: Center` even though the full edge lengths differ, and resolve it into deterministic source-cell links.
3. Validation rejects ambiguous aligned joins that would produce more than one neighbor for one `(source cell, direction)` and still rejects malformed endpoints/missing owners.

Invariant/test trace:

- Affected invariant: `Resolved topology adjacency must be directionally unique...` Existing tests: `ContentValidationRejectsMergedLayerDuplicateOrMultiLayerOwner`, `ContentValidationRejectsMergedLayerSeamDirectionalConflictsAndLengthMismatch`, `ContentValidationRejectsMergedLayerSeamConflictsWithEuclideanPlacementAdjacency`, overlap loop tests, and Core seam traversal tests in `TopologyServiceTests`.
- Affected invariant: `YAML content loads from strings and files into registries that can be validated.` Existing tests: `YamlContentLoaderLoadsMergedLayerSeams`, `YamlContentLoaderAllowsOverlapLayoutLoopWhenExplicitlyEnabled`, `PrototypeRegistryValidationPassesForBuiltInContent`.
- Affected invariant: `Editable content documents round-trip through materialization and saved YAML.` Existing merged-layer editable document roundtrip tests should continue preserving newly added fields.
- New tests needed: a Core cell-link traversal test for 3x3 room to 5x1 hallway, a YAML aligned-join load/validation test, and a validation conflict test for aligned joins.

Initial experiment constraints:

- Add lower-level explicit source-cell links as the runtime representation. This keeps Core independent from authoring sugar and gives validation one shared directional-conflict surface for seams and joins.
- Add Content `joins` as author-facing sugar that resolves edge endpoints plus `align: Start|Center|End` or optional `offset` into one or more source-cell links. For the first slice, unequal edge lengths resolve to the shorter edge length centered/start/end-aligned on the longer edge; exact span semantics beyond that remain spike-local.
- Keep random placement/generation out of this slice.

### Aligned-join / partial-doorway experiment turn

- Added an intentionally failing Core test for a `MergedInventoryLayerCellLink` connecting the center east cell of a 3x3 room to the west end of a 5x1 hallway, proving the required room/hall doorway traversal without whole-edge length matching.
- Added intentionally failing Content tests for YAML `joins` resolving `roomA.East -> hallAB.West align: Center` into a deterministic cell link, and for rejecting directional conflicts when two joins target the same `(source cell, direction)`.
- Implemented lower-level Core `MergedInventoryLayerCellEndpoint` and `MergedInventoryLayerCellLink` support. Cell links are bidirectional records with an explicit direction from each endpoint, and Core topology checks them before seam or Euclidean placement adjacency.
- Implemented Content `MergedInventoryLayerJoin` authoring with `align: Start|Center|End`, optional `offset`, and optional `length`. Joins resolve to source-cell links during registry construction. Unequal edge joins use the shorter edge span and deterministic alignment; for 3x3 room edge to 1-wide hallway end, `Center` selects the room edge middle cell.
- Tightened overlap-mode runtime/validation semantics for larger contributors: overlap-enabled layers still disable coordinate-derived cross-contributor Euclidean adjacency, but preserve ordinary internal movement within each contributor's own inventory cells. This lets 3x3 rooms and 1x5/5x1 halls remain walkable inside an overlap-enabled non-Euclidean layer.
- Extended YAML DTOs, editable document DTOs, editor-service upsert/list, agent definitions, frontend editor summaries, scenario materialization, and validation to carry cell links and joins.
- Verification: targeted aligned-join tests passed; editor/API/frontend snapshot test group passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed; `dotnet build src/GameGameGame.SadConsole/GameGameGame.SadConsole.csproj` passed.

### Flagship folded-house content turn

- Added Beta scenario `delta-merged-layer-flagship-folded-house` using the aligned-join model: six 3x3 rooms, 1x1 short halls, 5x1 east-west long hall, 1x5 north-south long halls, and `allowLayoutOverlap: true` with explicit `joins` for all room/hall doorways.
- Manual flow: enter Room A, travel east through a long hall to B, south through a short hall to C, west through a short hall to D, north through a long hall to E, west through a short hall to F, then south through a long hall back to A.
- Each room contains a distinct corner object to make the folded loop reviewable as place content rather than only topology plumbing.
- Content-editor validation reported content valid, manifest valid, materialization playable with zero diagnostics/failures/gaps, 12 authored joins resolving to 12 cell links, and the expected Room A east-middle to Hall AB west link.
- Verification after content authoring: `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Content"` passed; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "Suite=Core"` passed; `dotnet build src/GameGameGame.SadConsole/GameGameGame.SadConsole.csproj` passed.
