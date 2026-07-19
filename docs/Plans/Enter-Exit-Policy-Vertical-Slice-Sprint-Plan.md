---
id: plan.enter-exit-policy-vertical-slice-sprint
title: Enter/Exit Policy Vertical Slice Sprint Plan
kind: plan
status: active
truth_rank: 45
truth_domains: [planning-priority, implementation-navigation]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - implementing canonical Enter and Exit vertical slices
  - adding EnterPolicy or ExitPolicy support to engine content editor or frontend layers
  - updating constrained inventory-boundary transformation behavior
related:
  - source.invariants
  - source.engine-editor-capabilities
  - source.content-authoring-manual
  - source.action-step-outcome-and-affordance-logic
  - plan.canonical-actions-vertical-slice
---
# Enter/Exit Policy Vertical Slice Sprint Plan

Status: Active sprint plan for the next pair of canonical action vertical slices. This plan records the phased approach for promoting Enter and Exit while adding shared inventory-boundary placement/egress policies. It should be executed using the TDD workflow in `docs/Source of Truth/testing-charter.md`: add or revise intentionally failing tests before production code changes in each phase.

## Decisions and scope

- Promote the legacy/prototype `EnterTarget` and `ExitFacing` behavior into canonical player-facing **Enter** and **Exit** slices while preserving compatibility names where needed in engine/catalog/YAML layers.
- Add nullable entity/template policies:
  - `EnterPolicy`: controls where an entity is placed when entering an entity inventory.
  - `ExitPolicy`: controls whether an entity may leave an entity inventory through a selected direction/destination.
- Policies apply to every constrained inventory-boundary transformation, not only the Enter/Exit actions:
  - `EnterPolicy` applies to Enter, Pickup, Give, Take, and future constrained transforms that place an entity into an entity inventory.
  - `ExitPolicy` applies to Drop, Exit, and future constrained transforms that move an entity out of an entity inventory.
  - `Teleport` remains exempt as the unconstrained relocation primitive.
- If policies are absent/null, runtime defaults preserve current behavior:
  - `EnterPolicy = FirstUnoccupiedRowMajor`, scanning left-to-right, top-to-bottom, `(0,0)` first.
  - `ExitPolicy = AnyCell`, allowing exit from any source inventory coordinate when destination/bulk/aperture rules pass.
- First non-default policies:
  - `FarthestFromOccupied`: choose the empty destination cell farthest from occupied cells; ties resolve to the first row-major cell.
  - `EdgeAlignedWithExitDirection`: source inventory coordinate must be on the edge/corner matching the selected exit direction.
- `RandomEmpty` is deferred until deterministic seeded/replayable behavior is designed.
- Enter rejects self-entry and containment-cycle creation.
- Valid Enter targets are adjacent entities using shared eight-way adjacency and intercardinal two-corner blocking.
- Valid Exit destinations are empty cells in the eight adjacent directions around the entity being exited.
- If the actor and destination inventory owner are both player-controlled, Enter should allow the player to choose the destination cell; otherwise the destination owner's `EnterPolicy` selects the destination.

## Phase 0: Baseline tests, compatibility map, and shared policy design

Goal: make existing behavior and compatibility risks explicit before production changes.

Planned work:

1. Identify current Core services responsible for constrained inventory movement, aperture checks, occupancy checks, controlled commands, affordance queries, action choices, structured outcomes, and history submission.
2. Identify content/editor/YAML descriptors for entity templates and materialized entity state where policies should live.
3. Inspect Pickup and Drop tests and classify which tests should remain default-policy compatibility coverage versus which tests should be revised to assert shared policy behavior.
4. Define the shared policy resolver seam before implementation:
   - destination placement resolver for `EnterPolicy`;
   - source egress validator for `ExitPolicy`;
   - structured failure reasons/details for policy rejection.

TDD / invariant trace:

- Affected invariants:
  - `Entity locations are represented by occupancy of nodes in planes.`
  - `At most one entity may occupy a node at a time.`
  - `Traversals through containment or inventory relationships must be cycle-safe.`
  - `If a constrained inventory action moves entity A into or out of entity B's inventory plane, A's Bulk must be less than or equal to B's Aperture.`
  - `Nested Enter/Exit transitions intentionally cross inventory owner apertures on both sides of the move...`
  - `Pickup, Drop, Give, Take, Enter, and Exit enforce constrained Bulk/Aperture inventory transitions; Teleport is an unconstrained relocation primitive...`
  - `Peer inventory transfer must use deterministic row-major source and destination selection...`
  - `Actions must produce structured traces for failed checks and resolutions.`
  - `Controlled actor commands... move, wait, pickup, drop, enter, and exit...`
  - `Controlled actor affordance queries... move, pickup, drop, enter, and exit choices...`
- Existing tests to review/preserve:
  - `PickupFailsWhenTargetBulkExceedsAperture`
  - `PickupFailsWhenTargetBulkExceedsCarrierAperture`
  - `DropFailsWhenTargetBulkExceedsSourceCarrierAperture`
  - `DropFacingUsesApertureTransitionRules`
  - `GiveTargetTransfersFirstCarriedEntityToTargetInventoryRowMajor`
  - `TakeTargetTransfersFirstTargetInventoryEntityToActorInventoryRowMajor`
  - `EnterTargetMovesActorIntoAdjacentTargetInventoryRowMajor`
  - `ExitFacingMovesActorOutOfContainingInventoryToAdjacentContainerCell`
  - `ControlledActorAffordanceQueryReportsPickupSourcesAndDestinations`
  - `ControlledActorAffordanceQueryReportsDropSourcesAndBlockedDropDestinations`
  - `ControlledActorAffordanceQueryReportsEnterTargetsAndExitDirections`
- New intentionally failing tests before Phase 1 implementation:
  - `DefaultEnterPolicyPreservesRowMajorInventoryPlacementAcrossConstrainedTransforms`
  - `DefaultExitPolicyPreservesExistingDropAndExitEgressBehavior`
  - `InventoryBoundaryTransformRejectsContainmentCycle`

Exit criteria:

- The default-policy compatibility tests fail for the expected missing policy seam or pass as preserved baselines with explicit notes.
- Pickup/Drop friction is classified before policy behavior is generalized.

## Phase 1: Core policy model and default compatibility

Goal: add nullable policy state and shared Core resolution while preserving existing behavior for content with no policies.

Planned work:

1. Add Core policy types and effective-default resolution:
   - `EnterPolicy` nullable storage, effective default `FirstUnoccupiedRowMajor`.
   - `ExitPolicy` nullable storage, effective default `AnyCell`.
2. Add shared constrained inventory-boundary helpers:
   - destination placement selection uses effective `EnterPolicy`;
   - source egress validation uses effective `ExitPolicy`;
   - Teleport bypasses both policies.
3. Wire default policy behavior through Pickup, Drop, Give, Take, Enter, and Exit without changing absent-policy behavior.
4. Add cycle/self-entry rejection for Enter-like containment moves.
5. Preserve structured traces and criteria facts for aperture failures while adding policy-specific facts for policy failures.

TDD / invariant trace:

- Affected invariants:
  - Entity occupancy and one-entity-per-node invariants.
  - Cycle-safe containment traversal invariant.
  - Bulk/Aperture constrained inventory transitions invariant.
  - Nested Enter/Exit aperture invariant.
  - Peer inventory transfer deterministic selection invariant.
  - Structured trace invariant.
  - Canonical Action Step state-contract invariant for inventory transfer.
- Existing tests to preserve/revise:
  - Preserve row-major transfer expectations unless an explicit non-default policy is authored: `GiveTargetTransfersFirstCarriedEntityToTargetInventoryRowMajor`, `TakeTargetTransfersFirstTargetInventoryEntityToActorInventoryRowMajor`, `EnterTargetMovesActorIntoAdjacentTargetInventoryRowMajor`.
  - Preserve Drop/Exit default egress behavior: `DropFacingUsesApertureTransitionRules`, `ExitFacingMovesActorOutOfContainingInventoryToAdjacentContainerCell`.
  - Preserve Teleport aperture exemption and add policy exemption coverage near `TeleportBypassesApertureTransitionRules`.
- New intentionally failing tests before implementation:
  - `NullEnterPolicyUsesFirstUnoccupiedRowMajorForPickupEnterGiveAndTake`
  - `NullExitPolicyAllowsDropAndExitFromAnyInventoryCell`
  - `EnterRejectsSelfTarget`
  - `EnterRejectsTargetContainedWithinActor`
  - `TeleportBypassesEnterAndExitPolicyRules`
  - `PolicyFailuresEmitStructuredTraceFacts`

Exit criteria:

- Existing content with no policy fields behaves the same.
- All constrained inventory-boundary transforms route through shared policy-aware helpers.
- Enter self/cycle rejection is tested and traced.

## Phase 2: Non-default Core policies and affordance facts

Goal: implement the first non-default policies and expose authoritative affordance/choice facts.

Planned work:

1. Implement `FarthestFromOccupied` destination selection:
   - compute distance from candidate empty cells to occupied cells;
   - choose the farthest candidate;
   - tie-break row-major, left-to-right/top-to-bottom, `(0,0)` first.
2. Implement `EdgeAlignedWithExitDirection`:
   - cardinal exits require the matching edge;
   - intercardinal exits require the matching corner;
   - rejection produces structured policy-failure facts.
3. Update controlled-command execution for Enter/Exit so final execution remains authoritative after affordance selection.
4. Update controlled affordance queries:
   - Enter targets reflect adjacency, inventory usability, aperture, space, cycle checks, and policy viability.
   - Exit directions reflect eight adjacent empty destinations plus exit-policy legality/failure hints.
5. Update target-capability adjective behavior so `Enterable` is backed by the policy-aware non-mutating Enter affordance.

TDD / invariant trace:

- Affected invariants:
  - Plain adjacency/eight-way/two-corner intercardinal blocking invariant.
  - POV target adjectives invariant.
  - Controlled actor commands and affordance queries invariants.
  - Structured outcome projections expose criterion ratios/facts invariant.
  - Canonical Action Step state contracts and deterministic tie-breaks invariant.
- Existing tests to preserve/revise:
  - `ControlledActorAffordanceQueryReportsEnterTargetsAndExitDirections`
  - `PointOfViewAdjectivesComeFromObserverActionStepSuccessCapabilitiesInCurrentPlace`
  - `EntityPanelProjectionIncludesPointOfViewAdjectivesFromProjectedEntityActionPlan`
  - `ActionOutcomeProjectionExposesFailedEnterApertureDegree`
- New intentionally failing tests before implementation:
  - `FarthestFromOccupiedEnterPolicyChoosesFarthestEmptyCellWithRowMajorTieBreak`
  - `EdgeAlignedExitPolicyAllowsMatchingCardinalEdgeExit`
  - `EdgeAlignedExitPolicyAllowsMatchingIntercardinalCornerExit`
  - `EdgeAlignedExitPolicyRejectsNonMatchingInventoryCoordinate`
  - `ControlledAffordanceQueryReportsPolicyBlockedEnterTarget`
  - `ControlledAffordanceQueryReportsPolicyBlockedExitDirection`
  - `EnterableAdjectiveRequiresPolicyAwareEnterAffordanceSuccess`

Exit criteria:

- Non-default policies are deterministic and represented in traces/affordances.
- `Enterable` uses the same policy-aware Core affordance that execution uses.

## Phase 3: YAML, validation, content/editor service, and agent API parity

Goal: make policies authorable and inspectable through supported content/editor workflows.

Planned work:

1. Add YAML/descriptor support for nullable `enterPolicy` and `exitPolicy` on entity templates.
2. Materialize policies onto runtime entities and preserve them through clone/restore/history snapshots.
3. Add validation diagnostics for unknown or malformed policy values.
4. Add editor-service operations to set/clear policies.
5. Add snapshot projections showing authored and effective policies.
6. Add AgentContentEditorApi/tooling operations that use the same shared editor service operations.
7. Update action-plan previews/catalog guidance where Enter/Exit policy may affect outcomes.

TDD / invariant trace:

- Affected invariants:
  - YAML content loads from strings and files into registries that can be validated.
  - Editable content documents round-trip through materialization and saved YAML.
  - Content editor operations preserve declared IDs, presentations, carried layouts, Action Plans/behavior assignments, legacy action plans, and validation results.
  - Frontend editor snapshots and service-backed template mutations expose supported template inventory/action state/validation data through shared services.
  - Simulation history snapshots preserve restorable world state.
- Existing tests to preserve/revise:
  - `YamlContentLoaderCreatesRegistryFromDeclarativeContent`
  - `EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml`
  - `ContentEditorServiceValidatesCurrentDocumentAfterEdits`
  - `ContentEditorServiceUpdatesEntityPresetAndPresentation`
  - `FrontendEditorServiceTests`
  - `AgentContentEditorApiAuthorsCanonicalEnterExitBehavior`
  - `WorldStateClonePreservesMutableSimulationStateWithoutSharingCollections`
- New intentionally failing tests before implementation:
  - `YamlContentLoaderLoadsEntityEnterAndExitPolicies`
  - `EditableContentDocumentRoundTripsEnterAndExitPolicies`
  - `PrototypeRegistryValidationReportsUnknownEnterPolicy`
  - `PrototypeRegistryValidationReportsUnknownExitPolicy`
  - `ContentEditorServiceSetsAndClearsEntityEnterPolicy`
  - `ContentEditorServiceSetsAndClearsEntityExitPolicy`
  - `FrontendEditorSnapshotProjectsAuthoredAndEffectiveInventoryPolicies`
  - `AgentContentEditorApiAuthorsInventoryBoundaryPolicies`
  - `WorldStateClonePreservesInventoryBoundaryPolicies`

Exit criteria:

- Authors can define, inspect, validate, set, and clear both policies without hand-editing unsupported YAML.
- Runtime clones/history preserve policy state.

## Phase 4: Canonical Enter vertical slice

Goal: complete the Enter promotion using the new shared policy model.

Planned work:

1. Promote Enter as the canonical player-facing verb while keeping `EnterTarget` compatibility where required.
2. Add typed Action Choice support for Enter target selection and destination-cell selection when both actor and destination owner are player-controlled.
3. Add structured Enter success/failure outcomes and frontend game-text message ID slots.
4. Add/update POV adjective support for `Enterable`.
5. Coordinate with content-editor to add the two required Enter rooms:
   - deterministic log/outcome room;
   - player-interaction room.
6. Exercise the rooms through scenario/player-log tooling and, where appropriate, SadConsole manual play.

TDD / invariant trace:

- Affected invariants:
  - Controlled actor command execution invariant.
  - Runtime control-source / Action Choice invariant.
  - Structured action outcome/log projection invariant.
  - POV target adjective invariant.
  - Content pipeline and scenario tooling invariants for authored rooms/player narrative logs.
- Existing tests to preserve/revise:
  - `ActionChoiceRequestExposesNonParameterizedAuthoredStepsForCoreSubmission` should be preserved only as fallback compatibility; add typed Enter choice tests instead.
  - `SubmitAuthoredStepChoiceExecutesThroughCoreServiceAndAdvancesWhenConsuming` remains compatibility coverage.
  - `ActionOutcomeProjectionExposesFailedEnterApertureDegree` should be revised/extended for policy failure facts.
  - `AgentContentEditorApiAuthorsCanonicalEnterExitBehavior` remains authoring compatibility coverage.
- New intentionally failing tests before implementation:
  - `ActionChoiceRequestExposesEnterTargetsFromAuthoredEnterStep`
  - `ActionChoiceRequestExposesEnterDestinationCellsWhenBothEntitiesPlayerControlled`
  - `SubmitEnterChoiceUsesSelectedTargetAndDestinationCell`
  - `SubmitEnterChoiceUsesEnterPolicyWhenDestinationChoiceIsNotAvailable`
  - `ActionOutcomeProjectionRendersSuccessfulEnterFromStructuredCommandResult`
  - `ActionOutcomeProjectionExposesFailedEnterPolicyReason`
  - `CanonicalEnterLogOutcomeRoomProducesExpectedMessageIds`
  - `CanonicalEnterPlayerInteractionRoomMaterializesAndOffersEnterChoice`

Exit criteria:

- Enter satisfies the canonical action Definition of Done: Core semantics, structured outcomes, adjective support, game-text IDs, editor/content support, two rooms, and player-facing choice support.

## Phase 5: Canonical Exit vertical slice

Goal: complete the Exit promotion using the new shared policy model.

Planned work:

1. Promote Exit as the canonical player-facing verb while keeping `ExitFacing` compatibility where required.
2. Add typed Action Choice support for selecting among valid eight-direction exit destinations.
3. Ensure autonomous `ExitFacing` still reads `Facing` and applies `ExitPolicy`.
4. Add structured Exit success/failure outcomes and frontend game-text message ID slots.
5. Coordinate with content-editor to add the two required Exit rooms:
   - deterministic log/outcome room;
   - player-interaction room.
6. Exercise the rooms through scenario/player-log tooling and, where appropriate, SadConsole manual play.

TDD / invariant trace:

- Affected invariants:
  - Controlled actor command execution invariant.
  - Controlled actor affordance queries invariant.
  - Runtime control-source / Action Choice invariant.
  - Structured action outcome/log projection invariant.
  - Simulation history snapshot/rollback invariant for submitted choices.
  - Content pipeline and scenario tooling invariants for authored rooms/player narrative logs.
- Existing tests to preserve/revise:
  - `ControlledActorAffordanceQueryReportsEnterTargetsAndExitDirections`
  - `SubmitAuthoredStepChoiceThroughHistoryRecordsActorInterval`
  - `SubmitDropChoiceThroughHistoryFailureLogsWithoutAdvancing`
  - history/log projection tests for controlled submissions and failed commands.
- New intentionally failing tests before implementation:
  - `ActionChoiceRequestExposesExitDirectionsFromAuthoredExitStep`
  - `SubmitExitChoiceUsesSelectedDirectionAndDestination`
  - `ExitFacingUsesFacingAndAppliesExitPolicyForAutonomousActors`
  - `ActionOutcomeProjectionRendersSuccessfulExitFromStructuredCommandResult`
  - `ActionOutcomeProjectionExposesFailedExitPolicyReason`
  - `SubmitExitChoiceThroughHistoryAdvancesAndLogsStructuredOutcome`
  - `CanonicalExitLogOutcomeRoomProducesExpectedMessageIds`
  - `CanonicalExitPlayerInteractionRoomMaterializesAndOffersExitChoice`

Exit criteria:

- Exit satisfies the canonical action Definition of Done: Core semantics, structured outcomes, game-text IDs, editor/content support, two rooms, and player-facing choice support.

## Phase 6: Frontend editor and play-mode polish

Goal: ensure frontend/editor surfaces consume shared policy facts without owning engine semantics.

Planned work:

1. Add frontend editor UI flows backed by shared services for:
   - adding/setting `EnterPolicy`;
   - clearing `EnterPolicy` to null/default;
   - adding/setting `ExitPolicy`;
   - clearing `ExitPolicy` to null/default.
2. Display authored and effective policy values in entity/template editing views.
3. Present Enter/Exit choices in play mode using Core Action Choice facts.
4. Decide whether policy-blocked choices are hidden, disabled with hints, or shown only in inspection/debug panels; keep legality in Core.
5. Run manual SadConsole checks against the Enter and Exit player-interaction rooms.

TDD / invariant trace:

- Affected invariants:
  - Content editor operations invariant for frontend editor snapshots/service-backed mutations.
  - Controlled actor affordance and Action Choice invariants are consumed but not reimplemented by frontend tests.
  - Frontend tests, if added, should trace frontend UX invariants rather than Core legality.
- Existing tests to preserve/revise:
  - `FrontendEditorServiceTests`
  - SadConsole prompt/panel tests only if stable presentation seams change.
- New intentionally failing tests before implementation:
  - `FrontendEditorServiceSetsAndClearsTemplateEnterPolicy`
  - `FrontendEditorServiceSetsAndClearsTemplateExitPolicy`
  - `FrontendEditorSnapshotDisplaysEffectiveInventoryBoundaryPolicies`
  - Optional frontend-owned prompt/panel tests once the UI seam is stable, focused on consuming shared choice facts rather than recomputing legality.

Exit criteria:

- Human-facing editor workflows can add/edit/remove both policies through shared services.
- Play mode presents Enter/Exit choices from Core facts.
- Manual SadConsole review has covered the canonical Enter and Exit player-interaction rooms.

## Documentation updates required during implementation

- Update `docs/Source of Truth/invariants.md` when the policy behavior becomes stable and tests are in place.
- Update `docs/Source of Truth/Engine-Editor-Capabilities.md` when each layer's support status changes.
- Update `docs/Source of Truth/Content-Authoring-Manual.md` when policies become authorable and when Enter/Exit are promoted.
- Update `docs/Source of Truth/Action-Step-Outcome-And-Affordance-Logic.md` for policy-aware Enter/Exit/Pickup/Drop/Give/Take outcome rules.
- Update `docs/Source of Truth/Frontend-Game-Text.md` with Enter/Exit success/failure message ID slots before promotion is declared complete.
- Update the canonical action vertical-slice plan when Enter and Exit meet the Definition of Done.
