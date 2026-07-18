# Canonical Actions Vertical Slice Plan

Status: Active release plan. The canonical `Move` vertical slice, first Pickup/Drop Action Choice interaction seam, and componentized play-mode refactor are complete enough to use as reference workflow evidence for promoting follow-up actions. This supersedes Delta point-of-view as the selected implementation direction while preserving Delta POV as completed/available foundation work and follow-up context.

Read when:

- selecting canonical action promotion work;
- deciding whether an Action Step is legacy/prototype or promoted canonical behavior;
- planning player-facing logs, POV adjectives, success criteria, or scenario rooms for an action;
- designing remaining runtime control-source / Action Choice follow-up work and componentized play-mode consumption.

Related source of truth:

- `docs/Source of Truth/invariants.md` records stable Core behavior contracts and test traces.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records implemented Core/Editor/frontend-facing support tiers.
- `docs/Source of Truth/Content-Authoring-Manual.md` records what authors can safely create today.
- `docs/Source of Truth/Action-Step-Outcome-And-Affordance-Logic.md` records canonical Action Step outcome and affordance rules.
- `docs/Source of Truth/Frontend-Game-Text.md` records player-facing log message ID slots and argument expectations.
- `docs/Archived/Delta-Point-of-View-Release-Plan.md` records the POV foundation this plan consumes.
- `docs/Plans/Gamma-Editor-MVP-Plan.md` and `docs/Plans/SadConsole-Frontend-Roadmap.md` preserve the broader componentized editor/play-mode backlog.

## Release target

Promote current prototyped/MVP Action Steps into a smaller set of **canonical actions** through complete vertical slices. The existing Action Step catalog remains loadable/executable as legacy/prototype compatibility, but new canonical status is earned one action at a time by proving engine rules, structured outcomes, POV/affordance data, player-facing log IDs, content rooms, editor authorability, and frontend consumption together.

Target statement:

- A canonical action has authoritative engine semantics, structured success/failure outcomes, frontend-owned log IDs, content-authored test rooms, and editor/API support.
- Target-facing canonical actions expose success criteria, POV-related adjectives, and threshold-vs-actual success ratio facts where meaningful.
- Player-facing logs exist for every canonical action before the action is treated as release-ready.
- Content maintains two test rooms per canonical action: one deterministic log-outcome room and one player-interaction room.
- A Core-owned runtime control-source / Action Choice model lets any actor currently controlled by the player choose from that actor's normal authored action steps instead of being resolved by fallback policy.
- The componentized Gamma play-mode surface consumes canonical Action Choice, POV, entity-panel, history, and log services; remaining work should be framed as polish, broader action coverage, or cleanup of any internal legacy stopgap paths rather than initial replacement.

## Current Action Step freeze policy

The current Action Step catalog is now treated as **legacy/prototype-compatible** unless and until a step is explicitly promoted by this plan.

Freeze does not mean removal:

- existing authored content should continue to load, validate, and run;
- existing editor/API operations should continue to support compatibility workflows;
- new features should not silently expand every legacy/prototype step as if it were canonical;
- source-of-truth docs should distinguish implemented legacy/prototype support from promoted canonical-action support.

Promotion should be conservative. Prefer a small canonical vocabulary with complete vertical slices over a broad catalog with partial player-facing or content coverage.

## Definition of Done for one canonical action

An Action Step is canonical only when all of the following are complete.

### Core / engine semantics

1. Authoritative execution behavior is documented and tested.
2. Structured success/failure outcomes are emitted without parsing display strings.
3. Success criteria are represented for target-facing or constrained actions when meaningful.
4. Ratio facts are represented where an action compares a threshold/requirement with an actual/available value. Ratios should expose the raw facts and a normalized interpretation such as `SuccessRatio`; final wording remains frontend-owned.
5. Stable invariants and test traces are added or updated in `invariants.md`.

### POV / affordance facts

1. Target-facing actions declare a POV-related adjective when the adjective is semantically meaningful.
2. Adjectives are derived from Core/shared affordance facts, not frontend guesses.
3. Reciprocal adjectives are explicit and tested if included; otherwise they remain deferred.

### Frontend game text

1. `Frontend-Game-Text.md` has success and failure log ID slots for the action.
2. Ratio-bucket or reason-specific variants are listed when they are part of the supported projection.
3. Log args are stable enough for frontend wording experiments and scenario/player-log review.

### Content rooms

Content-editor maintains two test rooms for every canonical action. Core-owner convenience note from the Core refactor/consolidation sprint: this fixture pattern is wanted not only for release readiness, but also for maintenance/debugging convenience, because it keeps success paths, common failure paths, editor authoring, player/action-choice execution, and trace/log projection evidence easy to find for each promoted action:

1. **Log-outcome room**: a deterministic scenario/room that can emit every supported log outcome for that action. `SuccessCriteriaLogShowcase` is the prototype example.
2. **Player-interaction room**: a scenario/room with a player-controlled entity that has the action and can interact with different entities/targets to produce every supported log outcome for that action.

These rooms are validation fixtures, not just demos. They should be runnable through shared scenario/player-log tooling and useful for author/tester review.

### Editor / content support

1. The action is authorable through canonical editor/API workflows.
2. Validation diagnostics cover malformed or unsupported authored action data.
3. Scenario materialization/run reports and player narrative projections can exercise the action.

### Frontend / play-mode consumption

1. Componentized play mode can present the action through shared choice/target/log/POV services.
2. The frontend does not own action legality, success/failure policy, or ratio semantics.
3. Any frontend tests assert UI consumption of shared facts, not duplicated engine rules.
4. For every newly promoted action, frontend planning explicitly decides which player-facing facts the action exposes, which of those facts need graphical presentation, and whether an existing canonical visual treatment can be reused or a new one must be prototyped in the SadConsole component gallery before changing the play surface.
5. New player-facing graphical treatments should fit the SadConsole square-tile rendering baseline: text, entity glyphs, decorators, panels, menus, and future sprites are tile-rendered UI elements, while gameplay state should not default to terminal-style text dumps when a tile-based visual treatment is needed for player understanding.

## Runtime control source and Action Choice

Preferred model:

- Player control is not an authored Action Step and not a frontend-only special case.
- Each actor can have mutable runtime control state, likely stored with or adjacent to `EntityActionState`, that identifies the current decision source such as fallback/automatic control or player choice.
- Scenario `PlayerEntityId` remains an initial/session binding and observer/default focus, not the permanent authority for who is controllable.
- Turn resolution selects a Core-owned decision policy for the actor's effective plan: fallback-controlled actors use the current ordered fallback resolver, while player-controlled actors produce an `ActionChoiceRequest` over that same actor's normal authored action steps.
- Selected choices execute through canonical Action Step semantics, traces, turn-consumption rules, target-capability rules, and structured outcome/log projection. Frontends present and submit choices; they do not own legality or fallback policy.
- Control source can change during gameplay by mutating runtime action state through future explicit Core semantics, for example a control-source effect/action, a scenario/session rule, or another canonical system. The control-source mutation is separate from the act of prompting for a choice.

First-slice semantics:

1. Add a Core-owned `ActionChoiceRequest` / submission / resolution contract for actors whose current control source is player choice.
2. The choice set is derived from the actor's effective Action Plan after one-turn pre/main/post overrides are composed, not from a hardcoded player command catalog.
3. The first slice should prefer offering the plan's authored behavior-chain Action Steps as choices unless a specific Core rule marks a step forced, hidden, unavailable, or non-choiceable.
4. Target or destination selection is modeled as a nested Core request after action selection when the chosen action requires additional input.
5. Missing frontend/session support for a required choice produces structured diagnostics rather than silently auto-resolving as fallback.
6. Failed chosen-action policy is provisional. The first slice may retry, stop without advancing, or log failure and ask again, but that policy must be Core-owned and tested before frontend prompt tests depend on it.
7. Multiple player-controlled actors are allowed; each can produce a choice request when its scheduled turn reaches player-choice resolution.

Open design details to resolve before implementation:

- exact runtime representation for control source and how it is cloned/restored through `WorldState` and history;
- exact DTO shape for action-choice and nested target-choice requests;
- which authored steps are visible, forced, hidden, unavailable, or grouped in choice menus;
- how to represent unavailable choices, if at all;
- retry/failure/logging policy for failed submitted choices;
- how pending choice requests and submissions integrate with `SimulationHistorySession` frame/interval recording;
- how direct controlled-command compatibility is bridged or retired during migration.

## Repeatable canonical vertical-slice workflow

Use the completed `Move` slice as the baseline workflow before promoting another action:

1. **Classify and scope**: decide whether the action remains legacy/prototype-compatible, becomes promoted canonical, or needs a new canonical action instead of reusing an old step name.
2. **Trace invariants first**: identify affected contracts in `docs/Source of Truth/invariants.md`, preserve existing compatibility tests, and add intentionally failing tests for the new canonical behavior before production changes.
3. **Implement Core semantics narrowly**: add the smallest engine/runtime behavior, structured outcome facts, and state-contract changes needed for the promoted action.
4. **Keep authoring parity**: update descriptor/YAML loading, validation, editor services, agent/headless tools, previews, and schemas/catalogs so authors do not need to hand-edit unsupported fields.
5. **Add fixture rooms**: maintain one deterministic log/outcome scenario and one player-interaction scenario for every promoted action.
6. **Connect play surfaces through shared services**: frontends consume Core/Content choice, target, history, POV, and log facts; they do not own legality or action results.
7. **Decide the frontend visual grammar**: list the player-facing facts exposed by the action, choose the canonical graphical treatment for each promoted visual fact, reuse existing gallery patterns when possible, and prototype any new treatment in the SadConsole component gallery before applying it to the play surface. Keep textual explanation/inspection available, but do not let new action UX regress to text-only terminal presentation when the fact is central to player decision-making.
8. **Update docs in the source-of-truth lanes**: invariants for behavior/test traces, capability matrix for layer coverage/tier, authoring manual for content-facing usage, action-logic/game-text docs for outcome/log expectations, frontend UX standards/decisions for accepted visual treatments, and this plan/roadmap for next-slice decisions.
9. **Validate broadly enough**: run targeted tests for Core, Content/editor/tooling, and frontend consumers, then the relevant broader suites before declaring the slice complete.

Agent/headless tooling rule: `ggg_*` tools should be structured, agent-accessible versions of established user-facing editor/play-mode surfaces. Prioritize tools that edit content and tools that run/inspect simulations, and keep them in parity with shared editor services and supported play-mode workflows rather than adding agent-only semantics.

## Multi-phase implementation plan

## Selected first slice: canonical `Move`

The first promoted canonical action is a new parameterized `Move` Action Step, not a direct promotion of legacy/prototype `MoveFacing` or `Backstep`.

Phase-0 decisions:

1. `Direction`/`Facing` expands to eight directions: `North`, `NorthEast`, `East`, `SouthEast`, `South`, `SouthWest`, `West`, `NorthWest`.
2. Canonical `Move` has a required closed-enum `directionMode` for autonomous/fallback resolution. Relative modes are eighth-turn offsets from the actor's previous `Facing`: `Forward`, `ForwardRight`, `Right`, `BackRight`, `Back`, `BackLeft`, `Left`, `ForwardLeft`. Absolute modes may be supported for deterministic authored fixtures.
3. Player-controlled/action-choice presentation does not require an authored `ChooseAbsolute` mode. If a player-controlled actor has one or more canonical `Move` steps in its effective plan, Core should expose one player-facing eight-direction absolute movement choice for the first slice, coalescing multiple authored `Move` steps into one player-facing Move option.
4. Successful canonical movement relocates the actor one adjacent cell, consumes the turn, and sets persistent `Facing` to the actual absolute direction moved. Failed canonical movement does not relocate, does not change `Facing`, and does not opportunistically change targets.
5. Canonical Action Steps do not select or overwrite targets as incidental side effects. Targets are refreshed before action-plan resolution from template `targetingRules`; target-consuming steps read those target facts. Legacy/prototype target-writing behavior remains compatibility behavior until migrated or retired.
6. Diagonal movement may cut one blocked orthogonal corner but may not pass when both orthogonal corner cells forming the diagonal passage are blocked.

### Phase 1 TDD trace

Affected invariants from `docs/Source of Truth/invariants.md`:

- `Canonical Action Steps must preserve their documented state contracts for Facing, Target, movement, target selection, inventory transfer, fallthrough, and deterministic tie-breaks.`
- `Controlled actor commands...` and `Controlled actor affordance queries...` remain relevant compatibility surfaces because direct-player movement already exposes direction choices; the canonical Move choice model will eventually supersede/bridge this path.
- `Entity action state such as Facing, numeric Target slots, and labeled targets is typed and persists...`
- `The Action Step/primitive catalog describes every exposed primitive, value kind, implied state contract, and field contract.`
- `Content editor operations...` must grow first-class editing/validation support for the required `directionMode` field.

Existing traced tests to revise or preserve as compatibility:

- Preserve direct movement post-action facing tests: `TurnServiceUpdatesFacingAfterSuccessfulDirectionalMovement`, `TurnServiceDoesNotUpdateFacingAfterFailedDirectionalMovement`.
- Preserve legacy/prototype compatibility tests for existing steps unless explicitly reclassified in the implementation: `BehaviorChainRunsMoveFacingThenPickupTargetWithoutLinkedFallbackPlan`, `BackstepMovesOppositeFacingConsumesTurnAndPreservesFacing`, `BackstepBlockedByEntityWritesTargetAndFallsThrough`.
- Add new canonical Move tests instead of rewriting those legacy tests in the first slice.

New intentionally failing tests for Phase 1:

- `CanonicalMoveRelativeBackSetsFacingToActualMovedDirection`.
- `CanonicalMoveBlockedByEntityDoesNotWriteTarget`.
- `CanonicalMoveDiagonalAllowsOneBlockedCorner`.
- `CanonicalMoveDiagonalRejectsTwoBlockedCorners`.
- `CanonicalMoveBehaviorStepRequiresDirectionModeForAuthoring`.
- `CanonicalMoveDescriptorRoundTripsDirectionMode`.
- `ControlledActorAffordanceQueryReportsEightMovementDirections`.

Implementation should not begin until these tests or equivalent targeted tests are present and failing for the expected reason.

### Phase 0: Planning, classification, and test traces

Goal: make the freeze/promotion model explicit before changing runtime behavior.

Scope:

1. Update source-of-truth docs to classify the existing catalog as legacy/prototype-compatible and define the promoted canonical tier.
2. Add a canonical-action checklist to the capability matrix and authoring manual.
3. Trace existing tests for candidate first actions and record missing coverage.
4. Pick the first promoted action.

Phase outcome:

- The first promoted action became canonical `Move`, not `MoveFacing`, so the release-facing movement contract could use explicit 8-way absolute/relative `directionMode` while preserving `MoveFacing` compatibility.
- The first target/source interaction seam was implemented for `TransformAdjacentToInventory`/`PickupTarget` and `TransformInventoryToAdjacent`/`DropFacing`, proving action-step-first menus, target/source lists, destination lists, and shared history submission.
- The next likely target-facing follow-up is `EnterTarget`/`ExitFacing`, because `enterable` adjectives, containment transitions, and aperture/bulk success criteria naturally exercise the same vertical-slice pattern without immediately adding ranged transform variants.

Exit criteria:

- The first action has a TDD plan with existing invariant traces and intentionally failing tests for missing vertical-slice coverage.

### Phase 1: First canonical action slice

Goal: promote one simple action end-to-end.

Status: Complete for the first promoted canonical action, `Move`.

Scope:

1. Finalize engine outcome shape for the selected action. Complete for `Move`: 8-way absolute/relative `directionMode`, success sets `Facing` to actual movement direction, failure preserves position/`Facing` and does not write `Target`, and diagonal movement may cut one blocked orthogonal corner but not two.
2. Add or update player-facing log IDs. Complete for `Move` with `action.move.success` and `action.move.failure` slots.
3. Add the two content rooms. Complete for `Move` in `src/GameGameGame.Content/Beta/CanonicalActions/CanonicalMoveShowcase.yaml`.
4. Exercise the rooms through scenario/player-log tooling. Complete through content validation/scenario run tests and manual 8-way play verification.
5. Update editor/content docs to mark the action as canonical. Complete for Core/content/editor/API/tooling docs.

Exit criteria:

- The action meets the Definition of Done above.

### Phase 2: First target/adjective/ratio action slice

Goal: prove the full target-facing canonical action model.

Scope:

1. Promote the selected target-facing action.
2. Add success criteria and ratio facts where meaningful.
3. Add POV adjective support and tests.
4. Add frontend log IDs including ratio/reason variants if needed.
5. Add both content rooms and run them through player-log tooling.

Exit criteria:

- At least one target-facing action meets the Definition of Done, including adjective and ratio support.

### Phase 3: Runtime control source and Action Choice

Goal: replace special direct player control with Core-owned runtime control source and authored action choice.

Status: First slice complete for canonical `Move`; first Pickup/Drop target/source/destination choices and submissions are implemented. Remaining Action Choice follow-up includes full pre/main/post descriptor composition, target-first menus, richer choice DTO fields, and additional action families such as Enter/Exit.

Core/shared backlog captured from frontend planning:

1. Add Core-owned `ActionChoiceRequest`, submission, and result contracts.
2. Add mutable runtime control-source state, cloned/restored through `WorldState` and history.
3. Derive choices from effective authored plans after `Pre`/`Main`/`Post` overrides are composed.
4. Add nested target/destination request DTOs for choices that require additional input.
5. Define failed submitted-choice policy in Core.
6. Integrate Action Choice with `SimulationHistorySession` and `ActionLogProjection`.
7. Expose a reusable actor-turn stepper/scheduler for play-mode active-actor highlighting and animation.
8. Promote materialized scenario `PlayerControls` from binding data into initial runtime control-source setup once the Core model exists.

Scope:

1. Add mutable runtime control-source state for actors, with clone/rollback/history behavior.
2. Add Core resolver policy that returns structured action-choice requests for player-controlled actors and preserves fallback resolution for fallback-controlled actors.
3. Derive choices from the actor's effective authored Action Plan instead of a hardcoded player command set.
4. Add nested target-choice request support for selected actions that need a target/destination.
5. Implement provisional Core-owned failed-choice policy.
6. Integrate with shared history/log projection without frontend-owned simulation policy.
7. Add content rooms showing one controlled entity, control-source changes, and multi-entity/team control.

Exit criteria:

- Any actor whose runtime control source is player choice can request player choice through shared services, and selected canonical actions execute authoritatively through Core/shared services.

### Selected first Action Choice slice

Scope selected after the canonical `Move` play-surface bridge:

1. Add mutable Core runtime control-source state on `EntityActionState`, starting with `Automatic` and `PlayerChoice`, and preserve it through `WorldState.Clone`/history rollback. Complete.
2. Materialized scenario `PlayerControls` initialize controlled runtime entities to `PlayerChoice` while non-controlled actors remain `Automatic`. Complete.
3. Add a Core `ActionChoiceService` that returns an action-choice request only for `PlayerChoice` actors. The first request shape supports the first visible canonical `Move` Action Step in the actor's effective/default behavior descriptor and coalesces multiple authored `Move` steps into one player-facing `Move` choice. Complete.
4. The first `Move` choice exposes eight absolute direction options with destination, can-execute, failure reason/detail, and blocking entity facts from Core movement evaluation. Complete.
5. Submitting a selected direction executes through Core/shared turn/history semantics, consumes/advances on success, logs failure without advancement on failed movement, and preserves the canonical movement contract that success faces the moved direction while failure preserves facing. Complete.

Implemented first-slice evidence and deferrals:

- Core Action Choice has a first Pickup/Drop source/destination seam: `TransformAdjacentToInventory`/`PickupTarget` choices expose adjacent pickup targets and inventory slots, `TransformInventoryToAdjacent`/`DropFacing` choices expose carried sources and adjacent map destinations, and submitted choices execute through controlled-command/history semantics.
- History submission helpers for Pickup/Drop Action Choices are available, and the SadConsole componentized play path has the first action-step-first menu model: direct movement controls may remain available, `Enter` opens authored action steps, then target/source and destination lists are selected from Core Action Choice facts.
- Full pre/main/post override descriptor composition in choice request projection remains follow-up.
- Target-first action selection remains a future pathway that should reuse the same Core facts.
- Rich choice DTO fields for previous/new facing and source/destination beyond existing command outcome facts remain follow-up.

Action menu first is now the implemented baseline for non-movement Action Choice prompts. Target/source-first and bump-to-interact menus remain future player-control models, not new engine-only semantics. Any selected path must still execute through normal authored Action Steps, Core target/source requests, shared history/log projection, and editor/tooling parity.

TDD trace for this slice:

- Affected invariants: runtime action-plan/control-source state cloning/rollback, controlled actor command/affordance semantics as compatibility bridge, canonical Action Step movement contracts, scenario materialization player-control bindings.
- Existing tests to preserve and extend: `WorldStateClonePreservesMutableSimulationStateWithoutSharingCollections`, `RollbackRestoresFrameSnapshotAndVisibleTraceContext`, `SubmitSuccessfulControlledCommandCreatesIntervalAndNextFrame`, `SubmitFailedControlledCommandAddsCurrentFrameLogWithoutAdvancingFrameOrTurn`, `ScenarioMaterializerResolvesAuthoredPlayerControlBindings`, `ScenarioMaterializerDefaultsLegacyPlayerControlWhenNoBindingIsAuthored`, canonical Move tests added in Phase 1.
- New intentionally failing tests: `WorldStateClonePreservesActionControlSource`, `ScenarioMaterializerInitializesPlayerControlledEntitiesForChoice`, `ActionChoiceRequestRequiresPlayerChoiceControlSource`, `ActionChoiceRequestCoalescesCanonicalMoveStepsIntoOneEightDirectionChoice`, `SubmitMoveChoiceSuccessAdvancesAndSetsFacing`, and `SubmitMoveChoiceFailureLogsWithoutAdvancing`.

### Phase 4: Componentized Gamma play-mode follow-through

Goal: continue the componentized Gamma play surface over shared services and remove/demote any remaining internal legacy stopgap paths when they no longer provide unique coverage.

Shared log/POV backlog captured from frontend play-mode planning:

- Add a shared POV-local player log projection, without full perception: given `WorldState`, `ActionLogProjection`, and an observer/current-place POV, return player-facing log rows with message IDs/categories and conservative inclusion reasons. Include outcomes when actor, target/actee, source, destination, or enter/exit crossing anchors intersect the observer's current place. Preserve traces only for debug expansion, not normal play output. Document that visibility/audibility is not implemented and the result is a narrative/local projection.

Scope:

1. Preserve and harden componentized play mode over shared scenario launch, history/session, entity-panel, POV, choice, target, and log projection services.
2. Continue moving player action prompts toward canonical Action Choice requests while keeping direct movement controls only as a compatibility/convenience bridge.
3. Preserve debug/inspection affordances without duplicating engine legality.
4. Keep return-to-editor/preview concerns aligned with the on-hold Gamma plan, but do not expand editor scope unless selected.

Exit criteria:

- The componentized play-mode surface remains the supported path for playing canonical-action/Action Choice scenarios, and any legacy play-mode stopgap is removed or clearly demoted once it has no unique coverage.

### Phase 5: Release decision and next canonical actions

Goal: decide whether the vertical-slice pipeline is stable enough to promote more actions in batches.

Scope:

1. Review the first simple action, first target-facing action, runtime control-source / Action Choice slice, and componentized play-mode replacement.
2. Update source-of-truth docs and gap logs.
3. Choose the next action promotion order.
4. Decide whether retry-on-failure remains correct for submitted Action Choices.

Exit criteria:

- The team can promote additional canonical actions with a repeatable checklist and fixture pattern.

## Initial backlog of candidate promotions

Suggested order, subject to scenario pressure:

1. `Move` - complete as the first promoted canonical movement/action-choice slice.
2. `TransformAdjacentToInventory`/`PickupTarget` and `TransformInventoryToAdjacent`/`DropFacing` - first non-movement player-interaction seam implemented; promote/harden only where the Definition of Done still lacks canonical rooms, wording, or documentation.
3. `EnterTarget`/`ExitFacing` - likely next immediate promotion pair because containment transitions already have Core runtime, controlled affordances, POV adjectives, aperture ratios, log ID slots, and authoring support, but still need Action Choice/player-interaction and canonical-room hardening.
4. `PushFacing`, `SeekTarget`, or transfer actions such as `GiveTarget`/`TakeTarget` after Enter/Exit clarifies additional target/menu/log patterns.
5. `TransformInventoryToRanged`/Throw and `TransformAdjacentToRanged`/Shove remain backlog variations until the broader action set is proven or a concrete scenario requires ranged transform semantics.
6. `Teleport` may be considered as an advanced/stretch relocation slice, but it should not be treated like constrained inventory transforms because it intentionally bypasses Bulk/Aperture transition rules.
7. Prototype utility/world-mutation actions such as `CreateFacing` only after template-backed spawning direction is revisited.

## Transform action family naming direction

For maintainability, non-actor movement actions should converge on an internal/canonical `Transform<Source>To<Destination>` naming family. `Transform` means the actor moves or relocates an entity other than itself through constrained gameplay rules. Preferred future names:

- `TransformAdjacentToInventory`: current Pickup semantics, adjacent map entity to actor inventory.
- `TransformInventoryToAdjacent`: current adjacent Drop semantics, actor inventory entity to adjacent map space.
- `TransformInventoryToRanged`: planned Throw semantics, actor inventory entity to ranged destination.
- `TransformAdjacentToRanged`: planned Shove semantics, adjacent map entity to ranged destination.

`TransformAdjacentToInventory` and `TransformInventoryToAdjacent` are now available as preferred behavior-chain Action Step aliases over the existing semantics. Existing `PickupTarget` and `DropFacing` names remain compatibility names while current content and older tests continue to use them.

## Explicit non-goals for the first release slice

- Removing legacy/prototype Action Step runtime compatibility.
- Promoting the entire current Action Step catalog at once.
- Finalizing all Action Choice retry/failure policy beyond the provisional first-slice behavior.
- Adding broad new mechanics solely to exercise logs.
- Letting frontend code define action legality, target eligibility, or success ratios.
- Completing the full Gamma Editor MVP; this plan only promotes the componentized play-mode replacement needed for canonical action/Action Choice scenarios.
