# Canonical Actions Vertical Slice Plan

Status: Active release plan. This supersedes Delta point-of-view as the selected next implementation direction while preserving Delta POV as completed/available foundation work and follow-up context.

Read when:

- selecting canonical action promotion work;
- deciding whether an Action Step is legacy/prototype or promoted canonical behavior;
- planning player-facing logs, POV adjectives, success criteria, or scenario rooms for an action;
- designing the runtime control-source / Action Choice model and replacement componentized play-mode surface.

Related source of truth:

- `docs/Source of Truth/invariants.md` records stable Core behavior contracts and test traces.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records implemented Core/Editor/frontend-facing support tiers.
- `docs/Source of Truth/Content-Authoring-Manual.md` records what authors can safely create today.
- `docs/Source of Truth/Action-Step-Outcome-And-Affordance-Logic.md` records canonical Action Step outcome and affordance rules.
- `docs/Source of Truth/Frontend-Game-Text.md` records player-facing log message ID slots and argument expectations.
- `docs/Plans/Delta-Point-of-View-Release-Plan.md` records the POV foundation this plan consumes.
- `docs/Plans/Gamma-Editor-MVP-Plan.md` and `docs/Plans/SadConsole-Frontend-Roadmap.md` preserve the broader componentized editor/play-mode backlog.

## Release target

Promote current prototyped/MVP Action Steps into a smaller set of **canonical actions** through complete vertical slices. The existing Action Step catalog remains loadable/executable as legacy/prototype compatibility, but new canonical status is earned one action at a time by proving engine rules, structured outcomes, POV/affordance data, player-facing log IDs, content rooms, editor authorability, and frontend consumption together.

Target statement:

- A canonical action has authoritative engine semantics, structured success/failure outcomes, frontend-owned log IDs, content-authored test rooms, and editor/API support.
- Target-facing canonical actions expose success criteria, POV-related adjectives, and threshold-vs-actual success ratio facts where meaningful.
- Player-facing logs exist for every canonical action before the action is treated as release-ready.
- Content maintains two test rooms per canonical action: one deterministic log-outcome room and one player-interaction room.
- A Core-owned runtime control-source / Action Choice model lets any actor currently controlled by the player choose from that actor's normal authored action steps instead of being resolved by fallback policy.
- The componentized Gamma play-mode surface replaces the remaining legacy play-mode stopgap by consuming canonical Action Choice, POV, entity-panel, history, and log services.

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

Content-editor maintains two test rooms for every canonical action:

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

## Multi-phase implementation plan

### Phase 0: Planning, classification, and test traces

Goal: make the freeze/promotion model explicit before changing runtime behavior.

Scope:

1. Update source-of-truth docs to classify the existing catalog as legacy/prototype-compatible and define the promoted canonical tier.
2. Add a canonical-action checklist to the capability matrix and authoring manual.
3. Trace existing tests for candidate first actions and record missing coverage.
4. Pick the first promoted action.

Recommended first action:

- `MoveFacing`, because it proves the end-to-end canonical slice with simpler spatial semantics before target/adjective/ratio complexity.

Recommended first target-facing follow-up:

- `PickupTarget` or `EnterTarget`, because `portable`/`enterable` adjectives and aperture/bulk success criteria naturally exercise POV adjectives and ratios.

Exit criteria:

- The first action has a TDD plan with existing invariant traces and intentionally failing tests for missing vertical-slice coverage.

### Phase 1: First canonical action slice

Goal: promote one simple action end-to-end.

Scope:

1. Finalize engine outcome shape for the selected action.
2. Add or update player-facing log IDs.
3. Add the two content rooms.
4. Exercise the rooms through scenario/player-log tooling.
5. Update editor/content docs to mark the action as canonical.

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

### Phase 4: Componentized Gamma play-mode replacement

Goal: replace the remaining legacy/internal play-mode stopgap with the componentized Gamma surface based on the existing mock.

Scope:

1. Implement componentized play mode over shared scenario launch, history/session, entity-panel, POV, choice, target, and log projection services.
2. Consume canonical Action Choice requests instead of hardcoded direct player commands where possible.
3. Preserve debug/inspection affordances without duplicating engine legality.
4. Keep return-to-editor/preview concerns aligned with the on-hold Gamma plan, but do not expand editor scope unless selected.

Exit criteria:

- The componentized play-mode surface is the supported path for playing canonical-action/Action Choice scenarios, and the legacy play-mode stopgap can be removed or clearly demoted.

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

1. `MoveFacing` for the simplest end-to-end slice.
2. `PickupTarget` or `EnterTarget` for target adjective and aperture/bulk ratio proof.
3. `DropFacing`, `PushFacing`, or `SeekTarget` after the first target-facing slice clarifies log/ratio patterns.
4. Transfer actions such as `GiveTarget`/`TakeTarget` after inventory selection/log wording expectations are clearer.
5. Prototype utility/world-mutation actions such as `CreateFacing` only after template-backed spawning direction is revisited.

## Explicit non-goals for the first release slice

- Removing legacy/prototype Action Step runtime compatibility.
- Promoting the entire current Action Step catalog at once.
- Finalizing all Action Choice retry/failure policy beyond the provisional first-slice behavior.
- Adding broad new mechanics solely to exercise logs.
- Letting frontend code define action legality, target eligibility, or success ratios.
- Completing the full Gamma Editor MVP; this plan only promotes the componentized play-mode replacement needed for canonical action/Action Choice scenarios.
