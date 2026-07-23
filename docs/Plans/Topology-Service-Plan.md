---
id: plan.topology-service
title: Topology Service Plan
kind: plan
status: backlog-reference
truth_rank: 55
truth_domains: [planning-priority, runtime-behavior, implementation-navigation]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - planning graph topology movement or adjacency refactors
  - adding mechanics that connect spaces without coordinate adjacency
  - deciding whether vision raycasting flood fill or range queries should follow geometric or topological space
related:
  - source.invariants
  - source.testing-charter
  - source.engine-editor-capabilities
  - plan.high-level-roadmap
  - docs/Archived/Topology-Service-Phase-1-Sprint-Plan.md
---
# Topology Service Plan

Status: Backlog reference. Phase 1 behavior-preserving shared topology service work is complete; this plan now preserves the preferred later approach for directed topology overlays, topological rays, and future non-Euclidean mechanics such as entanglement, portals, explicit inventory-space links, and future vision/sound propagation. These mechanics should build on the same service rather than each action inventing spatial rules.

Read when:

- planning any change to movement, adjacency, range, ray, line-of-sight, flood-fill, or neighbor enumeration semantics;
- adding a mechanic where two spaces become adjacent even though their plane coordinates are not adjacent;
- reviewing whether an Action Step, affordance query, content validator, or frontend prompt is doing coordinate adjacency directly instead of using Core-owned topology facts.

Related source of truth:

- `docs/Source of Truth/invariants.md` records stable adjacency, movement, controlled-command, affordance, content, and scenario traces.
- `docs/Source of Truth/testing-charter.md` requires intentionally failing tests and invariant/test traces before semantic Core/Content/Editor changes.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` should be updated when topology support becomes an implemented engine/editor capability.

## Design intent

The current engine represents entity locations as node occupancy in planes, but many neighbor relationships are still derived directly from coordinate offsets. A topology service should make "what is adjacent from here in this direction" a shared Core question while preserving current grid behavior as the default topology.

Preferred first abstraction:

```csharp
public interface ITopologyService
{
    bool TryGetNeighbor(WorldState world, PlaneCoord origin, Direction direction, out TopologyNeighbor neighbor);
    IReadOnlyList<TopologyNeighbor> GetNeighbors(WorldState world, PlaneCoord origin);
    AdjacencyEvaluation EvaluateAdjacency(WorldState world, PlaneCoord first, PlaneCoord second);
}

public sealed record TopologyNeighbor(
    PlaneCoord Destination,
    Direction Direction,
    TopologyEdgeKind Kind,
    bool IsBlocked,
    FailureReason? FailureReason,
    string? FailureDetail);
```

Exact DTO names are not committed. The important constraints are:

- movement, adjacency-based interactions, and player affordance facts ask one Core-owned topology service for directional neighbors;
- default grid topology remains behavior-compatible with today's coordinate-offset rules;
- geometric utilities such as UI cursor movement may continue using coordinate offsets when they are explicitly not gameplay topology;
- future ray/vision APIs must state whether they are **geometric** or **topological**.

## Phase 1: Shared topology API, behavior-preserving

Status: Complete. See `docs/Archived/Topology-Service-Phase-1-Sprint-Plan.md` for the completed sprint plan and verification notes. Core now has `ITopologyService` / `DefaultTopologyService` support for default eight-way grid neighbors, neighbor enumeration, and adjacency evaluation. `MovementService`, controlled exit affordance projection, Action Choice drop destination enumeration, Action Choice transfer counterparty enumeration, and `TransferAction` counterparty lookup consume topology-backed movement facts. Remaining direct direction-offset calls in Core are either inside `DefaultTopologyService` or documented geometric/pathing candidate scoring in targeting handlers.

Goal: introduce a shared Core topology service over the existing implicit grid without changing gameplay behavior.

Scope:

1. Add a Core topology API that resolves ordinary eight-way grid neighbors and preserves the existing intercardinal two-corner blocking rule.
2. Migrate Core gameplay consumers that mean topological adjacency away from direct `Coord.Offset(direction)` calls. Expected candidates include `MovementService`, controlled actor affordance queries, Action Choice adjacent destination/counterparty enumeration, Transfer counterparty lookup, Enter/Exit adjacent placement, pickup adjacency, and movement/targeting steps whose legality is neighbor-based.
3. Leave geometric callers alone where appropriate, such as frontend/editor cursor movement or distance heuristics that intentionally measure coordinates.
4. Add code-level names or comments where a remaining coordinate offset is intentionally geometric rather than topological.

Non-goals:

- no entanglement, portals, non-grid edges, or content schema changes;
- no change to pathfinding, field of view, or tactical range semantics beyond routing neighbor lookup through a service;
- no frontend rendering changes.

TDD trace before implementation:

- Affected invariants:
  - `Entity locations are represented by occupancy of nodes in planes.`
  - `At most one entity may occupy a node at a time.`
  - `Plain adjacency means eight-way cardinal or intercardinal adjacency unless a contract explicitly says cardinal-only. Intercardinal adjacency is blocked when both orthogonal corner spaces between the spaces are occupied; shared Core adjacency evaluation owns this rule for adjacency-based mechanics.`
  - `Canonical Action Steps must preserve their documented state contracts for Facing, Target, movement, target selection, inventory transfer, fallthrough, and deterministic tie-breaks.`
  - `Controlled actor commands for direct player/frontend input resolve through a shared Core service...`
  - `Controlled actor affordance queries for direct player/frontend input expose Core-derived move, pickup, drop, enter, exit, and transfer choices...`
- Existing tests to preserve/revise as characterization tests:
  - `EntityLocationsAreRepresentedByNodeOccupancy`
  - `MovementCannotPlaceEntityOnOccupiedNode`
  - `AdjacencyAllowsUnblockedIntercardinalNeighbor`
  - `AdjacencyRejectsIntercardinalNeighborWhenBothCornersAreBlocked`
  - `PickupRejectsIntercardinalTargetWhenBothCornersAreBlocked`
  - `ControlledActorAffordanceQueryReportsPickupSourcesAndDestinations`
  - `ControlledActorAffordanceQueryReportsIntercardinalDropBlockedByTwoCorners`
  - `ControlledActorAffordanceQueryReportsValidAndBlockedMovementDirections`
  - `ControlledActorAffordanceQueryReportsDropSourcesAndBlockedDropDestinations`
  - `ControlledActorAffordanceQueryReportsEnterTargetsAndExitDirections`
  - `ActionChoiceRequestExposesPickupTargetsAndInventoryDestinationsFromAuthoredPickupStep`
  - `ActionChoiceRequestExposesDropSourcesAndAdjacentDestinationsFromAuthoredDropStep`
  - `ActionChoiceRequestExposesTransferCounterpartiesFromAuthoredTransferStep`
  - `ControlledActorCommandMoveReturnsStructuredSuccessAndAdvancesTurn`
  - `ControlledActorCommandFailedMoveRecordsFailureWithoutAdvancingTurn`
  - `CanonicalMoveDiagonalAllowsOneBlockedCorner`
  - `CanonicalMoveDiagonalRejectsTwoBlockedCorners`
  - `SubmitMoveChoiceSuccessAdvancesAndSetsFacing`
  - `SubmitMoveChoiceFailureLogsWithoutAdvancing`
- New tests needed:
  - a focused topology-service unit test for cardinal and intercardinal neighbor enumeration;
  - a focused topology-service unit test proving the two-corner intercardinal block is reported through topology diagnostics;
  - a regression test proving a chosen gameplay consumer, preferably Action Choice or controlled affordance movement, uses the topology service result rather than its own coordinate legality.

## Phase 2: Directed topology overlays and rays, still default-compatible

Goal: allow Core to represent non-default directional neighbor edges without changing default authored content behavior.

Scope:

1. Add an overlay source for directed topology edges. An overlay edge should make `origin + direction` resolve to an authored/runtime destination that need not be the coordinate offset destination.
2. Keep occupancy and placement authoritative at destination nodes: an edge can connect spaces, but movement still cannot place into an occupied or invalid destination.
3. Preserve diagonal corner-blocking for default grid intercardinal edges. Decide and test whether overlay edges carry their own blocking policy or bypass grid-corner checks by default.
4. Add optional topological traversal helpers that are useful beyond movement, such as directional ray stepping and bounded flood/range enumeration.
5. Keep geometric line/range helpers distinct. A caller must choose topological traversal when it wants warped/linked adjacency, and geometric traversal when it wants coordinate-space sight or UI geometry.

Non-goals:

- no content-authored entanglement yet;
- no final field-of-view system;
- no commitment that ordinary vision sees through future topology links. That is a mechanic-specific decision.

TDD trace before implementation:

- Affected invariants:
  - all Phase 1 invariants remain affected;
  - if overlays are cloned/restored with world history, also trace `Simulation history snapshots preserve restorable world state...`;
  - if overlays are persisted/materialized from content in this phase, also trace Content Pipeline invariants. Otherwise record Content Pipeline as `None` for this phase.
- Existing tests to preserve/revise:
  - all Phase 1 movement, adjacency, affordance, command, Action Choice, and diagonal-blocking tests;
  - `WorldStateClonePreservesMutableSimulationStateWithoutSharingCollections` if overlay state lives in `WorldState`;
  - `SimulationHistorySessionStartsWithFrameZeroSnapshot`, `RollbackRestoresFrameSnapshotAndVisibleTraceContext`, and `SubmitSuccessfulControlledCommandCreatesIntervalAndNextFrame` if topology overlays can change during simulated play before history snapshots.
- New tests needed:
  - overlay edge makes two non-coordinate-adjacent nodes adjacent in a chosen direction;
  - movement follows an overlay edge and still fails when the overlay destination is occupied;
  - adjacency evaluation reports the overlay direction consistently for both direct entity adjacency and plane-coordinate adjacency;
  - default grid movement and adjacency still pass existing tests without any overlay data;
  - directional topological ray follows overlay edges step by step;
  - geometric ray or coordinate utility test, if added, demonstrates it does not accidentally follow topology overlays.

## Phase 3: Entanglement relation and content authoring

Goal: implement the proposed entanglement mechanic as a normal topology overlay provider, with content/editor support sufficient for authored scenarios and agent workflows.

Mechanic statement:

- Two entities can be related by `Entangled`.
- When two entities are entangled, the spaces around them are considered directionally adjacent through topology edges.
- Movement and adjacency-based actions use those edges; the mechanic is not implemented as a teleport action.
- If either entangled entity moves, the derived perimeter edges move with the entities because the relation is between entities, not old coordinates.

Expected topology shape for an eight-neighbor perimeter relation:

- `A6 --North--> B2` and `B2 --South--> A6`
- `A4 --West--> B8` and `B8 --East--> A4`
- `B5 --NorthEast--> A1` and `A1 --SouthWest--> B5`

Exact edge mapping should be specified with a diagram and table before tests are written. The implementation should reject or ignore perimeter destinations that are outside a plane, lack a node, or are otherwise invalid according to the same placement/adjacency policies used by normal topology.

Core scope:

1. Add a runtime relation or topology provider for entangled entity pairs.
2. Generate dynamic directed topology edges from the current locations of both entities.
3. Make movement, adjacency evaluation, controlled affordances, Action Choice, pickup/enter/transfer/drop where adjacency-based, and topological rays use entanglement through the topology service.
4. Decide whether entanglement links are bidirectional only, whether multiple entanglements can stack, and how duplicate/conflicting outgoing edges in the same direction are diagnosed or ordered.

Content/editor scope:

1. Prefer scenario/materialization-level relation authoring first, for example a `relations` or `entanglements` section that references placed entity IDs.
2. Validate both endpoints exist, endpoints are distinct, pairs are not duplicated, and references resolve after scenario materialization.
3. Materialize authored relations into Core world topology state.
4. Expose typed editor service and agent API operations for authoring/removing/listing entanglements once the schema is stable.
5. Add scenario preview/run reporting that can surface relation diagnostics and exercise topology-linked movement.

Non-goals for first entanglement slice:

- general template-level property inheritance for entanglement;
- arbitrary portal authoring UI;
- polished frontend visualization of warped adjacency;
- final vision/line-of-sight behavior through entanglement, unless explicitly selected as part of a later topological vision slice.

TDD trace before implementation:

- Affected invariants:
  - `Every meaningful game object is an entity with a stable ID.`
  - `Entity locations are represented by occupancy of nodes in planes.`
  - `At most one entity may occupy a node at a time.`
  - `Plain adjacency means eight-way cardinal or intercardinal adjacency unless a contract explicitly says cardinal-only...` should be revised or clarified to distinguish default/plain coordinate-grid adjacency from topology-augmented adjacency.
  - `Canonical Action Steps must preserve their documented state contracts...` because `Move` and adjacency-based actions gain new legal destinations while preserving success/failure/facing/target contracts.
  - `Controlled actor commands...` and `Controlled actor affordance queries...` because player-facing move/action choices must expose entangled adjacency from Core facts.
  - Content Pipeline persisted scenario/materialization/editor invariants if authored entanglement relations are included in this slice.
  - Scenario Tooling invariants if scenario run/player-log reports are expected to exercise authored entanglement scenarios.
- Existing tests to revise as intentionally failing tests where behavior changes:
  - Add topology-augmented cases beside, not instead of, `AdjacencyAllowsUnblockedIntercardinalNeighbor` and `AdjacencyRejectsIntercardinalNeighborWhenBothCornersAreBlocked` so plain grid behavior remains covered.
  - Add topology-augmented movement cases beside `CanonicalMoveDiagonalAllowsOneBlockedCorner`, `CanonicalMoveDiagonalRejectsTwoBlockedCorners`, and controlled-command/Action Choice movement tests.
  - Extend `ControlledActorAffordanceQueryReportsValidAndBlockedMovementDirections` or add a sibling test for entangled move destinations.
  - Extend `ActionChoiceRequestExposesTransferCounterpartiesFromAuthoredTransferStep` or add a sibling test if Transfer through entangled adjacency is supported in the first slice.
  - Extend persisted scenario/materialization roundtrip tests only after the authoring shape is chosen; likely sibling tests to `ScenarioDefinitionsRoundTripAuthoredPlayerControlBindings`, `ScenarioMaterializerValidatesPersistedAlphaScenarioDefinitions`, `AgentContentEditorApiCreatesCombinedPersistedScenarioReport`, and `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`.
- New tests needed:
  - entangled pair creates the expected directed perimeter edges from both entities' current locations;
  - moving one entangled endpoint changes the derived edges without leaving stale adjacency behind;
  - `Move` through an entangled edge consumes the turn and sets `Facing` to the selected direction, not to a coordinate delta inferred from destination coordinates;
  - failed entangled-edge movement preserves position and `Facing` when the linked destination is occupied or invalid;
  - pickup/enter/transfer adjacency through an entangled edge succeeds or fails according to the same adjacency-based rules selected for the slice;
  - authored scenario relation YAML round-trips, validates dangling/duplicate/self-entanglement diagnostics, materializes, and can be run through shared scenario tooling.

## Vision, rays, and propagation notes

A topology service should make future raycasting and propagation work cleaner, but the plan must keep two modes explicit:

- **Geometric** vision/rays use coordinate-space lines, slopes, distance, and occlusion. They should not follow entanglement or other topology overlays unless a future mechanic explicitly says space-bending sight is intended.
- **Topological** rays/floods step through topology neighbors. These are appropriate for warped sight, lasers, sound, smell, auras, tactical reach, or other mechanics that should follow explicit graph links.

Future vision work should not silently switch ordinary player visibility to topology-augmented behavior merely because topology exists. The implementation plan for that slice must choose which queries are geometric, topological, or a composition of both.

## Open design questions

- Should default grid edges be materialized into world state, generated on demand, or cached by topology service?
- Should overlay edges be stored in `WorldState`, in a separate runtime topology component, or derived entirely from relations?
- How should conflicting outgoing edges with the same origin and direction be represented: disallowed, prioritized, multi-edge, or diagnostic failure?
- Do topology edges cross planes directly, and if so how should plane-local UI/highlighting present them?
- Should intercardinal two-corner blocking apply only to default grid edges or also to overlay edges with diagonal directions?
- Which action/pathing heuristics should remain coordinate-distance-based even after movement uses topology?
- When topological rays exist, which gameplay systems use them by default, and which remain geometric?

## Promotion trigger

Promote this backlog plan when one of the following becomes the selected roadmap need:

- a maintenance sprint chooses to centralize movement/adjacency before adding more Action Steps;
- a concrete scenario requires linked spaces, portals, entanglement, inventory-space exits, or other non-coordinate adjacency;
- vision, sound, aura, or tactical reach work needs graph traversal rather than one-off coordinate scans;
- frontend/editor refactor work needs more reliable Core-owned adjacency facts for highlighting and prompts.
