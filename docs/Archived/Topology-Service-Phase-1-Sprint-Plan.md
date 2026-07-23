---
id: plan.topology-service-phase-1-sprint
title: Topology Service Phase 1 Sprint Plan
kind: plan
status: completed
truth_rank: 45
truth_domains: [planning-priority, runtime-behavior, implementation-navigation]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - implementing the behavior-preserving Core topology service refactor
  - auditing gameplay coordinate adjacency calls
  - preparing future directed topology overlays, entanglement, portals, or topological rays
related:
  - plan.topology-service
  - source.invariants
  - source.testing-charter
  - source.engine-editor-capabilities
  - source.vertical-slice-map
---
# Topology Service Phase 1 Sprint Plan

Status: Completed isolated refactor sprint. This plan scoped Phase 1 of `docs/Plans/Topology-Service-Plan.md` into behavior-preserving sub-phases. It did **not** implement entanglement, portals, authored topology overlays, content schema changes, frontend rendering changes, pathfinding changes, field-of-view changes, or tactical range semantics.

## Sprint target

Introduce a shared Core topology service for default eight-way grid adjacency so gameplay callers that mean "neighbor" or "adjacent" can use one Core-owned answer instead of direct coordinate offsets. Current gameplay behavior must remain compatible with existing coordinate-grid behavior, including the intercardinal two-corner blocking rule.

The sprint is valuable as an isolated refactor even before non-Euclidean mechanics exist: it centralizes movement/adjacency policy, creates a test seam for future overlays, and reduces the chance that future Action Steps or affordance queries invent their own spatial rules.

## Definition of done

1. Core exposes a default topology service that can answer directional neighbor lookup, neighbor enumeration, and adjacency evaluation over the existing grid.
2. Default topology preserves current cardinal and intercardinal behavior, including blocked intercardinal diagnostics when both orthogonal corner spaces are occupied.
3. `MovementService` routes ordinary adjacent movement and adjacency evaluation through the topology service while preserving existing public behavior.
4. At least one player-facing gameplay consumer outside `MovementService` proves via test that it consumes topology facts rather than performing its own coordinate-offset legality check. Prefer controlled movement affordances or Action Choice movement.
5. Remaining direct `Coord.Offset(direction)` gameplay call sites are audited and classified as migrated, intentionally geometric, or pending follow-up migration.
6. `docs/Source of Truth/Engine-Editor-Capabilities.md` is updated after implementation to record shared Core topology support and any explicit editor/content non-impact.
7. `docs/Source of Truth/invariants.md` is updated only if test names or trace wording change; behavior-preserving work should usually preserve existing invariant text.

## Sub-phase 0: Readiness, trace, and audit

Goal: make the refactor TDD-ready before production code changes.

Tasks:

1. Confirm the sprint remains Phase 1 only: default grid topology, no overlays, no content schema, no frontend rendering.
2. Re-read `docs/Source of Truth/testing-charter.md` and `docs/Source of Truth/invariants.md` before writing code.
3. Audit direct direction-offset call sites in `src/GameGameGame.Core` and classify each as one of:
   - **topological gameplay**: should use the topology service in this sprint or a named follow-up;
   - **geometric/pathing heuristic**: intentionally coordinate-based and should receive a comment/name if ambiguity remains;
   - **internal default topology implementation**: allowed to use coordinate offsets because it is the grid topology provider.
4. Choose the first non-`MovementService` consumer regression target. Preferred order:
   1. controlled movement affordance query;
   2. Action Choice canonical Move request;
   3. Transfer counterparty enumeration.

Deliverable: a short implementation note or plan update listing the chosen regression target and the offset audit categories.

Sprint-start audit note:

- Chosen first non-`MovementService` consumer regression target: controlled movement affordance query, because it is directly player-facing and already reports direction, destination, blocking entity, failure reason, and failure detail.
- Initial `Coord.Offset(direction)` classification in `src/GameGameGame.Core`:
  - **internal default topology implementation / migration target**: current `MovementService` adjacency corner checks, move destination lookup, and adjacent movement destination resolution should move behind the default topology implementation or delegate to it.
  - **topological gameplay, pending this sprint or follow-up**: `ActionChoiceService` adjacent drop destination enumeration and transfer counterparty enumeration; `Actions.TransferAction` counterparty lookup; `ControlledActorAffordanceService` exit destination projection.
  - **geometric/pathing heuristic until a later topology-aware pathing/range decision**: `ActionPlanInterpreter.TargetingHandlers` seek/flee/maintain/strafe candidate scoring and coordinate-distance tie-breaks. These should not silently become topology traversal in Phase 1.

Implementation progress note:

- Migrated in this sprint: `MovementService` adjacency evaluation, directional move destination lookup, adjacent relocation resolution, `ControlledActorAffordanceService` exit destination projection, `ActionChoiceService` adjacent drop destination enumeration, `ActionChoiceService` transfer counterparty enumeration, and `Actions.TransferAction` counterparty lookup now consume topology facts.
- Remaining topological gameplay follow-up candidates after the first slice: none identified in the initial Core `Coord.Offset(direction)` audit. Future adjacency/range work should re-audit new call sites before adding overlays.
- Remaining intentional geometric/pathing candidates: `ActionPlanInterpreter.TargetingHandlers` coordinate-distance candidate scoring for seek/flee/maintain/strafe behavior.
- Wrap-up verification note: targeted topology/movement/controlled-affordance/Action Choice/Transfer tests passed with 89 tests. The broader `GameGameGame.Tests` suite excluding legacy `ScenarioRecordingTests` passed with 585 tests when run against an isolated temporary build output. Full-suite execution using that temporary output is still blocked by existing scenario-recording tests that locate Beta content relative to the test output directory; normal output execution may also be blocked while `GameGameGame.Content.Tools` is running and locking copied assemblies.

## Sub-phase 1: Test seam and default topology contract

Goal: create intentionally failing tests for the service contract before introducing production behavior.

New tests needed:

1. Default topology returns cardinal neighbors for valid in-plane adjacent nodes and reports out-of-bounds or missing-node failures for invalid destinations.
2. Default topology returns intercardinal neighbors when no two-corner block exists.
3. Default topology reports `FailureReason.MoveBlocked` and a useful diagnostic when both orthogonal corner spaces block an intercardinal neighbor.
4. Default topology neighbor enumeration returns stable eight-direction results compatible with `DirectionMath.AllDirections` ordering.

Implementation notes:

- Exact DTO names from the backlog plan are not committed. Keep the API small and use existing domain types where practical.
- The default implementation may use coordinate offsets internally; gameplay consumers should not duplicate that logic.
- The service should be easy to inject in tests so a consumer can be proven to consume topology facts. If normal constructor injection is too disruptive, use a minimal internal seam that does not leak overlay semantics into content/editor layers.

## Sub-phase 2: MovementService migration

Goal: route movement and adjacency policy through topology while preserving current behavior.

Scope:

1. Migrate `MovementService.EvaluateAdjacency` to delegate topology-grid adjacency policy to the topology service.
2. Migrate directional move destination resolution in `MovementService` to ask topology for the directional neighbor.
3. Preserve `CanPlace`, occupancy, node validity, relocation trace behavior, and public method signatures unless a small constructor overload or default service parameter is needed for injection.
4. Preserve existing two-corner diagonal movement behavior and failure reason/detail expectations.

Existing tests to preserve as characterization tests:

- `EntityLocationsAreRepresentedByNodeOccupancy`
- `MovementCannotPlaceEntityOnOccupiedNode`
- `AdjacencyAllowsUnblockedIntercardinalNeighbor`
- `AdjacencyRejectsIntercardinalNeighborWhenBothCornersAreBlocked`
- `CanonicalMoveDiagonalAllowsOneBlockedCorner`
- `CanonicalMoveDiagonalRejectsTwoBlockedCorners`
- `ControlledActorCommandMoveReturnsStructuredSuccessAndAdvancesTurn`
- `ControlledActorCommandFailedMoveRecordsFailureWithoutAdvancingTurn`
- `SubmitMoveChoiceSuccessAdvancesAndSetsFacing`
- `SubmitMoveChoiceFailureLogsWithoutAdvancing`

Expected result: targeted movement and adjacency tests pass without gameplay behavior changes.

## Sub-phase 3: First player-facing consumer migration

Goal: prove at least one gameplay consumer outside `MovementService` no longer computes its own movement/adjacency legality from `Coord.Offset(direction)`.

Preferred slice: controlled movement affordance query.

Candidate behavior to prove:

- When a test topology service supplies a valid directional neighbor that is not the ordinary coordinate offset, the chosen consumer reports or uses that topology neighbor.
- When the test topology service reports a blocked directional neighbor, the chosen consumer reports the topology failure reason/detail rather than independently accepting the coordinate destination.

Existing tests to preserve/revise depending on chosen consumer:

- `ControlledActorAffordanceQueryReportsValidAndBlockedMovementDirections`
- `ActionChoiceRequestCoalescesCanonicalMoveStepsIntoOneEightDirectionChoice`
- `ActionChoiceRequestExposesDropSourcesAndAdjacentDestinationsFromAuthoredDropStep`
- `ActionChoiceRequestExposesTransferCounterpartiesFromAuthoredTransferStep`

Keep this sub-phase narrow. Do not migrate every Action Choice and inventory interaction caller unless the first migration exposes a very small shared helper that safely covers them.

## Sub-phase 4: Coordinate-offset audit cleanup and follow-up list

Goal: avoid leaving ambiguous spatial semantics behind.

Tasks:

1. Review remaining `Coord.Offset(direction)` calls in `src/GameGameGame.Core`.
2. Add comments or local naming only where needed to mark intentionally geometric/pathing uses. Avoid comment noise for obvious default topology internals.
3. Record follow-up migrations for adjacency-based callers not migrated in this sprint. Likely candidates include:
   - Action Choice adjacent drop destination enumeration;
   - Transfer counterparty lookup/enumeration;
   - Enter/Exit adjacent placement where applicable;
   - pickup/drop adjacency affordance paths;
   - targeting/pathing behaviors that may intentionally remain coordinate-distance-based until overlays exist.

Expected result: future overlay work has a clear list of remaining gameplay consumers and intentional geometric exceptions.

## Sub-phase 5: Documentation and verification

Goal: close the refactor sprint with source-of-truth and test evidence.

Verification commands should include targeted Core tests around movement, adjacency, controlled affordances, and Action Choice. Run broader Core tests if the migration touches shared constructors or service wiring across action execution.

Documentation updates after tests pass:

1. Update `docs/Source of Truth/Engine-Editor-Capabilities.md` to state that shared Core topology service support exists for default grid adjacency, and that content/editor authoring is unchanged in Phase 1.
2. Update `docs/Source of Truth/invariants.md` only if test names or trace wording change. The core invariant that shared Core adjacency evaluation owns the two-corner intercardinal rule should remain true.
3. Update `docs/Plans/Topology-Service-Plan.md` or this sprint plan with completed sub-phases and deferred follow-ups.

## TDD trace before implementation

Affected invariants:

- `Entity locations are represented by occupancy of nodes in planes.`
- `At most one entity may occupy a node at a time.`
- `Plain adjacency means eight-way cardinal or intercardinal adjacency unless a contract explicitly says cardinal-only. Intercardinal adjacency is blocked when both orthogonal corner spaces between the spaces are occupied; shared Core adjacency evaluation owns this rule for adjacency-based mechanics.`
- `Canonical Action Steps must preserve their documented state contracts for Facing, Target, movement, target selection, inventory transfer, fallthrough, and deterministic tie-breaks.`
- `Controlled actor commands for direct player/frontend input resolve through a shared Core service...`
- `Controlled actor affordance queries for direct player/frontend input expose Core-derived move, pickup, drop, enter, exit, and transfer choices...`

Existing tests to preserve/revise as characterization tests:

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

New tests required before production changes:

- focused topology-service unit test for cardinal neighbor lookup and neighbor enumeration;
- focused topology-service unit test for unblocked intercardinal neighbor lookup;
- focused topology-service unit test proving two-corner intercardinal blocking is reported through topology diagnostics;
- one consumer regression proving the chosen gameplay consumer uses topology service results rather than direct coordinate legality.

## Non-goals and guardrails

- Do not add entanglement, portals, directed overlay edges, cross-plane topology, topological rays, authored relations, or content YAML schema.
- Do not change ordinary player visibility, line-of-sight, raycasting, flood-fill, or range behavior.
- Do not migrate frontend/editor cursor movement or UI geometry to topology.
- Do not widen canonical Action Step semantics beyond routing existing adjacency questions through shared Core topology.
- Do not update content authoring manuals unless implementation unexpectedly changes author-facing behavior; Phase 1 should not.

## Follow-up after this sprint

The likely next topology work after this sprint is not Phase 3 entanglement. It is a Phase 1B/Phase 2 readiness pass that migrates the remaining adjacency-based consumers, then introduces directed overlay edges under tests. Entanglement should wait until default topology and overlay conflict/ordering policy are stable.
