---
id: archived.give-take-transfer-vertical-slice-sprint
title: Give/Take Transfer Vertical Slice Sprint Plan
kind: plan
status: archived
truth_rank: 45
truth_domains: [planning-priority, implementation-navigation]
owners: [core-owner]
audience: [core-owner, content-editor, frontend-owner]
read_when:
  - implementing canonical Give and Take vertical slices
  - designing containment transfer actions, Action Choice prompts, or transfer frontend workflows
  - authoring Give or Take outcome/player-interaction test rooms
related:
  - source.invariants
  - source.testing-charter
  - source.engine-editor-capabilities
  - source.content-authoring-manual
  - source.action-step-outcome-and-affordance-logic
  - source.frontend-game-text
  - plan.canonical-actions-vertical-slice
---
# Give/Take Transfer Vertical Slice Sprint Plan

Status: Archived completed sprint plan. The slice promoted canonical peer inventory **Transfer** as one controller-agnostic action with ActorToTarget and TargetToActor directions, while preserving legacy `GiveTarget`/`TakeTarget` compatibility. Give and Take are now player-facing/log directions of Transfer rather than separate promoted canonical actions.

## Completion summary

- Core added `TransferAction` and `TransferDirection` with atomic ActorToTarget/TargetToActor containment transfer semantics.
- ActorToTarget respects the adjacent destination holder's `EnterPolicy`/aperture/capacity while ignoring the actor's `ExitPolicy`; TargetToActor respects the adjacent source holder's `ExitPolicy`/aperture while ignoring the actor's `EnterPolicy`.
- Descriptor/YAML/editor/agent authoring supports `kind: Transfer` with `targetSlot`/`targetLabel`, `directionMode`, and `transferDirection` for autonomous authored behavior. Current target fields select a concrete moving entity from actor action target state; predicate matching such as "first potion in that inventory" remains deferred.
- Content added canonical Transfer outcome and player-interaction rooms in `src/GameGameGame.Content/Beta/CanonicalActions/CanonicalTransferShowcase.yaml` and manifest entries.
- Core Action Choice and SadConsole play mode added the first player workflow: choose Transfer, choose adjacent counterparty, then choose an item from either inventory. Core derives ActorToTarget vs TargetToActor from the selected item's current owner; the player does not choose Give or Take explicitly.
- Tests cover Core semantics, content authoring, room/report validation, Action Choice/history submission, and the first SadConsole prompt stack.

## Decisions and semantic scope

- Treat existing `GiveTarget` and `TakeTarget` behavior as legacy/prototype-compatible. Do not model the new canonical step on their old selection or transfer shortcuts except where compatibility must be preserved.
- Model canonical peer transfer as one atomic, controller-agnostic `Transfer` action with an authored transfer direction:
  - **ActorToTarget**: give-like transfer from actor containment to the adjacent counterparty containment.
  - **TargetToActor**: take-like transfer from adjacent counterparty containment to actor containment.
- The validation model is similar to a two-step movement, but execution must be atomic: there is no intermediate world state, no temporary dropped item, and no split event that other systems can observe between exit and enter.
- There is no gameplay distinction between "inventory" and "containment" for this slice. Any entity/container with usable containment can participate, including active creatures such as slimes and inert containers such as chests.
- Consent/permission is modeled by containment policy:
  - Take invokes the adjacent source holder's `ExitPolicy`.
  - Give invokes the adjacent destination holder's `EnterPolicy`.
- The actor's own policy does not constrain the actor's current action:
  - ActorToTarget must **not** invoke the actor/source `ExitPolicy`.
  - TargetToActor must **not** invoke the actor/destination `EnterPolicy`.
- Bulk/aperture and capacity/free-slot rules still apply to the relevant containment boundary:
  - Take is subject to the source holder's exit policy and source-side aperture/bulk rules, plus actor free containment capacity/slot availability.
  - Give is subject to destination free containment capacity/slot availability, destination `EnterPolicy`, and destination-side aperture/bulk rules.
- Failed actions must expose structured reasons specific enough for player-facing and debug surfaces to distinguish at least: not adjacent/no holder, selected slot empty or invalid, source exit policy denial, destination enter policy denial, source or destination bulk/aperture violation, destination unusable/full/no open slot, and successful transfer.

## Content-authoring and Action Choice input model

- The canonical action selects the moved item from a target/source slot rather than relying on implicit row-major item choice for the promoted player-facing workflow.
- Design correction from Phase 2 alignment: "target/source slot" means the existing action target slot/label (`targetSlot` / `targetLabel`), not an inventory grid coordinate. Authored Transfer reads the selected moving entity from actor action target state; richer target-rule/predicate matching remains deferred until explicitly designed.
- Transfer ActorToTarget input model:
  - selected target slot/label from the actor's action target state, resolved to one concrete moving entity that must already be in the actor's containment;
  - destination holder resolved from actor facing or an explicitly chosen relative/absolute adjacent direction for player choice;
  - destination cell must contain a valid adjacent entity/container with free containment space.
- Transfer TargetToActor input model:
  - adjacent source holder selected by direction/target;
  - selected target slot/label from the actor's action target state, resolved to one concrete moving entity that must already be in that holder's containment;
  - destination is the actor's containment.
- Compatibility aliases may continue to exist, but new content/editor/player workflows should expose the canonical `Transfer` shape with explicit transfer direction.

## Vertical-slice deliverables

1. **Core changes**
   - Add or refactor an atomic containment-transfer seam that lets action-specific validators compose source, destination, policy, aperture/bulk, and capacity checks without imposing fully symmetric transfer rules on every action.
   - Implement canonical Transfer execution over that seam.
   - Preserve legacy/prototype `GiveTarget`/`TakeTarget` compatibility unless this plan is revised with explicit migration/removal criteria.
   - Emit structured outcomes, failure reasons, anchors, and ratio/fact details needed by logs, Action Choice, scenario reports, and frontend panels.
2. **Take outcome test room**
   - Add a deterministic authored room/scenario that can emit every supported Take outcome: each failure category plus success.
   - The room should be runnable through shared scenario/player narrative log tooling and should not require bespoke manual setup to inspect the outcomes.
3. **Give outcome test room**
   - Add a deterministic authored room/scenario that can emit every supported Give outcome: each failure category plus success.
   - The room should explicitly cover facing/direction-based destination holder resolution.
4. **Combined player Give/Take room**
   - Add a player-controlled interaction room where the player can both Give and Take between adjacent containers/entities.
   - The room should exercise slot selection, direction/recipient choice, policy-denied interactions, capacity pressure, and successful round-trip transfer.
5. **Frontend workflow**
   - Design and implement the first player-facing workflow for slot-based transfer choices.
   - Open design options: reuse existing inspection/entity panels for selecting source/destination slots, or introduce a focused Transfer panel.
   - Regardless of UI shape, frontend must consume Core/Content Action Choice and outcome facts; it must not own legality, policy, aperture/bulk, or capacity semantics.

## TDD and invariant trace

Implementation must follow `docs/Source of Truth/testing-charter.md`: add or revise tests so the planned behavior fails for the expected reason before production code changes.

Affected invariants from `docs/Source of Truth/invariants.md`:

- `Plain adjacency means eight-way cardinal or intercardinal adjacency...` for adjacent Give recipients and Take sources.
- `Traversals through containment or inventory relationships must be cycle-safe.`
- `If a constrained inventory action moves entity A into or out of entity B's inventory plane, A's Bulk must be less than or equal to B's Aperture.`
- `Pickup, Drop, Give, Take, Enter, and Exit enforce constrained Bulk/Aperture inventory transitions...`
- `Constrained inventory-boundary transformations respect nullable entity inventory policies except that the current actor's own EnterPolicy and ExitPolicy never constrain that actor's current action...`
- `Actions must produce structured traces for failed checks and resolutions.`
- `Canonical Action Steps must preserve their documented state contracts for Facing, Target, movement, target selection, inventory transfer, fallthrough, and deterministic tie-breaks.`
- `Controlled actor commands...` and `Controlled actor affordance queries...` should be extended only if the chosen frontend/player path requires new direct-command compatibility; otherwise Action Choice is the promoted path.
- `Runtime control source... Action Choice requests expose...` should be extended for canonical Transfer choice requests and submissions.
- `Structured action outcome projections derive frontend log rows from structured command/action fields...`
- `Content editor operations...` and `The Action Step/primitive catalog describes every exposed primitive...` for canonical slot/direction fields and validation.

Existing tests to review, preserve, or revise as compatibility/baseline coverage:

- `GiveTargetTransfersFirstCarriedEntityToTargetInventoryRowMajor`
- `TakeTargetTransfersFirstTargetInventoryEntityToActorInventoryRowMajor`
- `GiveTargetCanTransferPlayerEntityWhenInventoryRulesAllowIt`
- `GiveTargetFailsWhenTransferBulkExceedsSourceAperture`
- `GiveTargetFailsWhenTransferBulkExceedsDestinationAperture`
- `TakeTargetFailsWhenTransferBulkExceedsSourceAperture`
- `TakeTargetFailsWhenTransferBulkExceedsDestinationAperture`
- `GiveTargetFailureFallsThroughWithoutConsumingStepTurn`
- `TakeTargetFailureFallsThroughWhenTargetInventoryIsEmpty`
- `DropFacingIgnoresActorExitPolicy`
- `PickupTargetIgnoresActorEnterPolicy`
- Enter/Exit policy tests around `FarthestFromOccupied`, `EdgeAlignedWithExitDirection`, structured policy failures, controlled affordance facts, and history submission.
- Action Choice tests for Pickup/Drop and Enter/Exit source/destination prompts and submissions.

New intentionally failing test targets before implementation:

- Core semantics:
  - `CanonicalTransferActorToTargetUsesSelectedMovingEntityAndFacingCounterparty`
  - `CanonicalTransferActorToTargetDoesNotInvokeActorExitPolicy`
  - `CanonicalTransferActorToTargetReportsDestinationApertureFailure`
  - `CanonicalTransferFailureKeepsSelectedItemInSourceSlotWithoutIntermediateWorldPlacement`
  - `CanonicalTransferTargetToActorUsesSelectedMovingEntityFromAdjacentHolder`
  - `CanonicalTransferTargetToActorDoesNotInvokeActorEnterPolicy`
  - `CanonicalTransferTargetToActorFailsWhenSourceExitPolicyRejectsItem`
  - `CanonicalTransferTargetToActorReportsActorInventoryFullSeparatelyFromExitPolicyFailure`
  - `CanonicalTransferBehaviorStepReadsTargetDirectionModeAndTransferDirection`
- Action Choice/history/outcome projection:
  - `ActionChoiceRequestExposesGiveSlotsAndAdjacentRecipientsFromAuthoredGiveStep`
  - `ActionChoiceRequestExposesTakeSourcesAndSlotsFromAuthoredTakeStep`
  - `SubmitGiveChoiceUsesSelectedSlotAndRecipientDirection`
  - `SubmitTakeChoiceUsesSelectedSourceDirectionAndSlot`
  - `ActionOutcomeProjectionExposesGiveAndTakeFailureReasonFacts`
- Content/editor/API:
  - `CanonicalTransferDescriptorRoundTripsDirectionModeAndTransferDirection`
  - `PrototypeRegistryValidationReportsMalformedCanonicalTransferFields`
  - `ContentEditorAuthorsCanonicalTransferBehaviorChain`
  - `AgentContentEditorApiAuthorsCanonicalTransferBehavior`
- Scenario rooms:
  - tests or reports proving the Take outcome room, Give outcome room, and combined player Give/Take room load, validate, materialize, and emit structured/player narrative outcomes through shared tooling.
- Frontend:
  - add lightweight frontend tests only after the workflow seam stabilizes; before that, record manual smoke checks for slot selection, recipient/source selection, failure display, success display, and history/rollback behavior.

## Phase 0: Core design and failing tests

Goal: pin the canonical semantics without relying on legacy Give/Take behavior.

Planned work:

1. Inspect current legacy `GiveTarget`/`TakeTarget`, Pickup/Drop, Enter/Exit policy, Action Choice, history, and outcome projection paths.
2. Choose the smallest shared atomic containment-transfer seam that supports action-specific policy participation.
3. Add/revise the Core semantic tests listed above so they fail for missing canonical slot/direction semantics, policy asymmetry, and atomicity.
4. Record any compatibility tests that must remain unchanged for legacy/prototype action names.

Exit criteria:

- Core failing tests exist for selected slot movement, facing/direction recipient resolution, policy asymmetry, structured failure reasons, and atomic transfer.

Phase 0 friction log:

- 2026-07-22: Existing code only exposes legacy/prototype `GiveTarget`/`TakeTarget` behavior over target entity plus implicit row-major item choice, while promoted semantics need explicit moved-item slots and direction/source selection. Mitigation: added the first failing Core tests against proposed low-level `GiveAction(GridCoord sourceSlot, Direction recipientDirection)` and `TakeAction(Direction sourceDirection, GridCoord sourceSlot)` intents rather than mutating legacy behavior first.
- 2026-07-22: Current `EnterPolicy` values select placement but do not include a denying policy such as "no entities can enter," so Phase 0 cannot yet express destination enter-policy denial without a policy-catalog design decision. Mitigation: deferred that specific failing test and covered available Phase 0 policy asymmetry with actor-policy bypass tests and source `ExitPolicy` denial for Take.

## Phase 1: Core canonical Give/Take implementation

Goal: make the Phase 0 Core tests pass with the smallest coordinated engine change.

Planned work:

1. Implement atomic action-specific transfer validation/execution for canonical Give and Take.
2. Emit structured traces/outcomes with separate source-exit, destination-enter, aperture/bulk, capacity, adjacency, invalid-slot, and success facts.
3. Preserve or explicitly compatibility-map existing legacy/prototype action behavior.
4. Update invariant traces after tests pass.

Exit criteria:

- Targeted Core tests pass for canonical semantics and legacy compatibility tests remain accounted for.

Phase 1 notes:

- 2026-07-22: Implemented the first canonical Core execution seam as low-level `GiveAction`/`TakeAction` intents while preserving legacy `GiveTarget`/`TakeTarget` behavior. This keeps Phase 1 independent from descriptor/YAML/editor shape decisions reserved for Phase 2.
- 2026-07-22: Added structured success details and additional failure coverage for destination aperture and full actor inventory. No new blocking design decision was needed; richer failure-reason enum names such as `DestinationFull` remain a possible Phase 2/4 outcome-projection polish if current `InvalidPlacement` detail is not specific enough for frontend wording.
- 2026-07-22 correction: The first low-level seam used explicit inventory grid coordinates, but the accepted authoring model uses `targetSlot`/`targetLabel` to select a matching contained entity. Phase 2 must revise the Core seam/tests before exposing the authoring model.
- 2026-07-22 correction: Separate canonical Give/Take actions are superseded by one canonical `Transfer` action with authored `TransferDirection` (`ActorToTarget` or `TargetToActor`). Phase 1 must replace the spike `GiveAction`/`TakeAction` seam with `TransferAction(transferDirection, movingEntityId, counterpartyDirection)` while preserving the validated policy asymmetry.

## Phase 2: Content, descriptor, validation, editor, and agent parity

Goal: make canonical Give/Take authorable without hand-editing unsupported YAML.

Planned work:

1. Add descriptor/catalog metadata for canonical slot/direction Give and slot/source Take fields.
2. Update YAML loading, materialization, validation diagnostics, editor service operations, and agent API facade support.
3. Add tests for roundtrip, malformed field diagnostics, editor service authoring, and agent API authoring.

Exit criteria:

- Content authors and agents can author canonical Give/Take steps through supported shared services and receive validation feedback for malformed data.
- Content-editor explicitly accepts the Phase 2 authoring slice before Phase 3 test-room authoring begins. The acceptance handoff should confirm the YAML shape, editor/API operations, validation diagnostics, and any documented limits around target matching.

Phase 2 notes:

- 2026-07-22: Implemented the accepted authoring shape as `kind: Transfer` plus `targetSlot`/`targetLabel`, `directionMode`, and `transferDirection`. `targetSlot`/`targetLabel` read a concrete moving entity from actor action target state; predicate-based selection such as "first Potion in the other inventory" is deferred pending a shared matcher design.
- 2026-07-22 content-editor acceptance checkpoint: ACCEPTED. Content-editor reviewed the Phase 2 authoring slice, requested only wording cleanup from "first matching" to concrete target-state entity selection, and confirmed Phase 3 room authoring may proceed with the documented deferred predicate-matching limit.

Content-editor accepted authoring caveats for Phase 3:

- `Transfer` is the canonical authored action; do not author new Give/Take rooms with legacy `GiveTarget` / `TakeTarget` behavior.
- `targetSlot` / `targetLabel` selects from actor action target state, not an inventory coordinate.
- Current Transfer authoring does not support predicate matching like "first Potion in the other inventory"; deterministic rooms should preselect the exact moving entity id in action target state.
- `targetSlot` is one-based; use `targetSlot: 1` explicitly unless a labeled target is populated.
- `targetLabel` and `targetSlot` remain mutually exclusive.
- `directionMode` resolves the adjacent counterparty direction; use `Forward` when actor facing matters or absolute modes for deterministic fixture choreography.

## Phase 3: Test rooms and scenario/player-log coverage

Goal: provide reusable content fixtures for all Give/Take outcomes and combined player interaction.

Planned work:

1. Author the deterministic Take outcome room.
2. Author the deterministic Give outcome room.
3. Author the combined player Give/Take room.
4. Run validation, scenario reports, and player narrative log projections for the rooms.

Exit criteria:

- The three rooms load, validate, materialize, and emit the intended structured/player-facing outcomes through shared tooling.

Phase 3 friction log:

- 2026-07-22: Current authored `EnterPolicy` values are placement policies (`FirstUnoccupiedRowMajor`, `FarthestFromOccupied`) and still do not include a true destination-enter denial policy. Mitigation: the Transfer outcome room covers the supported destination-side failure logs currently authorable through no inventory, full/no open placement, and aperture-blocked outcomes; a dedicated destination-enter-policy denial fixture should be added when a denying enter policy is designed.
- 2026-07-22: Phase 4 Transfer-specific Action Choice UI is not implemented yet, so the manual player room cannot expose rich slot/recipient/source prompts. Mitigation: authored a player-controlled room with a deterministic preselected `targetSlot: 1` item and adjacent `Forward` chest; repeated manual execution of the canonical behavior chain can give, then take the same item back without legacy `GiveTarget`/`TakeTarget`.

## Phase 4: Action Choice, history, logs, and frontend workflow

Goal: let a player use canonical Give/Take in componentized play mode through shared facts.

Planned work:

1. Extend Action Choice request/submission DTOs for Give slot + recipient direction and Take source direction + slot.
2. Integrate submissions with shared history/log projection and failure-without-advancement behavior.
3. Decide the frontend workflow: existing inspection/entity panels versus a new Transfer panel.
4. Implement the chosen workflow with frontend-owned presentation and Core-owned legality.
5. Add frontend tests only for stabilized UI seams; perform manual smoke checks for still-exploratory presentation.

Exit criteria:

- A player can Give and Take in the combined test room, understand success/failure causes, and use history/rollback without frontend-owned transfer semantics.

Phase 4 notes:

- 2026-07-22 frontend-owner coordination accepted the first workflow: selecting `Transfer` opens counterparty selection, then an item list containing contents from both actor and counterparty. The selected item's current owner determines ActorToTarget vs TargetToActor; the player does not choose Give or Take explicitly.
- 2026-07-22 implemented Core Action Choice facts for Transfer counterparties and transferable items, plus shared submission/history support that derives transfer direction from current ownership. SadConsole prompt support now reuses the action selector stack with TransferCounterparty and TransferItem modes; frontend presentation remains intentionally plain for later UX refinement.

## Phase 5: Documentation and wrap-up

Goal: promote the completed slice into the source-of-truth lanes.

Planned work:

1. Update `invariants.md` test traces.
2. Update `Engine-Editor-Capabilities.md` and `Content-Authoring-Manual.md` for actual support status.
3. Update `Action-Step-Outcome-And-Affordance-Logic.md` and `Frontend-Game-Text.md` for canonical Give/Take outcome/log slots.
4. Update the canonical action vertical slice plan and roadmap with completed evidence and deferred follow-ups.

Exit criteria:

- Give/Take meet the canonical action Definition of Done or any incomplete layer is explicitly documented as a deferred stopping point.
