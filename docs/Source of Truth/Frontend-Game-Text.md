---
id: source.frontend-game-text
title: Frontend Game Text
kind: source-of-truth
subkind: frontend-game-text
status: active
owners: [frontend-owner]
audience: [frontend-owner, core-owner, content-editor]
lane: frontend-game-text
truth_rank: 30
truth_domains: [frontend-presentation]
read_when:
  - translating structured action outcomes point-of-view facts target labels or affordance adjectives into user-facing sentences
  - adding or reviewing SadConsole/global/local log wording
  - deciding whether a wording decision is frontend presentation or a missing shared semantic fact
related:
  - source.frontend-ux-invariants
  - source.frontend-ux-standards
  - source.action-step-outcome-and-affordance-logic
  - plan.canonical-actions-vertical-slice
---
# Frontend Game Text

Status: Draft source of truth for frontend-owned player-facing game-text message IDs; final wording is intentionally deferred.

Read when:

- translating structured action outcomes, point-of-view facts, target labels, or affordance adjectives into user-facing sentences;
- adding or reviewing SadConsole/global/local log wording;
- deciding whether a wording decision is frontend presentation or a missing shared semantic fact.

Related documents:

- `docs/Source of Truth/Frontend-UX-Invariants.md` records the boundary rule that logs derive from structured outcomes, not parsed display strings.
- `docs/Source of Truth/Frontend-UX-Standards.md` records log presentation standards for global and local activity surfaces.
- `docs/Source of Truth/Action-Step-Outcome-And-Affordance-Logic.md` records canonical Action Step outcome and affordance semantics.
- `docs/Plans/Canonical-Actions-Vertical-Slice-Plan.md` records the active requirement that every promoted canonical action has player-facing success/failure log IDs and any needed ratio/reason variants.
- `docs/Archived/Delta-Point-of-View-Release-Plan.md` records the point-of-view, target adjective, and reciprocal adjective foundation.

## Purpose

This document catalogs stable player-facing message ID slots for current Action Step logs. Final sentence wording is intentionally deferred until later frontend development. It is intentionally a text/presentation document: it does not define action legality, success criteria, threshold values, failure policy, turn consumption, or materialization semantics.

The current entries are allowed to be placeholders. The important first step is that every promoted canonical action has explicit success/failure message ID slots, and actions with success-ratio data have explicit ratio/reason variant slots where those variants are supported. Tests should prefer message IDs, variants, args, and structured anchors over exact prose.

## Ownership and composition rules

1. **Shared services provide facts.** The log renderer should consume structured outcome fields such as actor, target, source, destination, direction, payload/actee, result, failure reason, success-ratio kind, ratio bucket, point-of-view observer, target labels, and adjectives.
2. **Frontend text chooses wording.** SadConsole or a future frontend may choose exact sentence wording, punctuation, emphasis, and line wrapping from those facts.
3. **Do not encode thresholds here.** Ratio bucket thresholds such as “barely” versus “large/easy” belong in shared semantic projection or configuration, not in this text catalog.
4. **Do not parse debug trace strings.** If a sentence needs a fact that is only available in a trace string, record that as a missing structured-log fact before relying on the trace.
5. **Keep point of view explicit.** Future renderers may vary actor/target naming by observer knowledge, relationship, visibility, target labels, and adjectives. Example: `{actor} sees something it {targetLabel} ({target})` or `{actor} sees {targetAdjective} {target}`.

## Placeholder conventions

- `{actor}`: acting entity as named from the observer's point of view.
- `{target}`: persistent target or action target.
- `{actee}`: moved/transferred/carried entity when distinct from target.
- `{source}` / `{destination}`: source or destination entity/place/inventory.
- `{direction}`: direction read from action state.
- `{targetLabel}`: authored target label such as `loves`, `food`, `home`, or `danger`.
- `{adjective}`: projected affordance/adjective such as `portable`, `enterable`, or `hostile`.
- Message IDs use lowercase dotted `action.<action_step>.<result>[.<variant>]` form, such as `action.move_facing.success` or `action.pickup_target.failure.large`. These IDs are the current contract; final user-facing language is not.

## Player motivations and UX feedback concerns

Player-facing log text should first answer why the player is reading the log, then choose sentence detail accordingly. Current player narrative projection output is useful for smoke-testing tone, but Alpha Slime showcase review exposed several UX concerns that should guide future text and structured-fact work.

### Player motivations / reasoning to support

1. **Understand what just changed.** A player checks the log to reconstruct the latest turn without replaying the whole scene visually.
2. **Know whether the change matters to them.** The log should make nearby, visible, audible, player-targeting, inventory-changing, or path-blocking events feel more important than distant background churn.
3. **Identify the participants.** When multiple entities share a display name, the player needs enough disambiguation to tell whether this is the same slime as before, a different slime, an item, or the player character.
4. **Understand cause and obstacle.** Failures should communicate actionable world reasons when known: wall, edge, blocker, too bulky, not adjacent, no room, or unknown/hidden cause.
5. **Track possession and containment changes.** Pickup, drop, transfer/give/take, enter, and exit events change where entities are. The player needs to know who gained/lost what, especially when an actor picks up another actor.
6. **Understand agency and turn structure.** The log should make clear whether the player acted, waited, or is only observing autonomous actors, and should help the player recognize repeated initiative/order patterns without needing debug indices.
7. **Distinguish intended behavior from scenario failure.** Validation/runtime status is useful to authors and testers because it tells them whether odd behavior is expected simulation output or a broken run.
8. **Preserve point-of-view trust.** A player-facing log must not imply omniscient perception. Until line-of-sight/audibility filtering exists, label output as a narrative projection and avoid wording that claims exact player knowledge.
9. **Separate narrative from inspection/debug detail.** The main line should stay readable; exact entity IDs, coordinates, action-step names, ratios, and failure enum values belong in debug views or expandable details.

### Feedback and UX concerns from Alpha Slime player-log review

- **Duplicate names need disambiguation.** Repeated `Small Slime` lines are hard to follow when multiple small slimes act in the same turn. Prefer structured naming support such as visible descriptors, stable local nicknames, relative positions, or inspectable identity anchors.
- **Pronouns and generic nouns hide important state.** Lines such as `{actor} picks something up.` or `{actor} cannot pick it up.` should use `{target}` / `{actee}` when visible or known; fall back to `something` only when point-of-view knowledge is genuinely incomplete.
- **Generic fallback verbs are weak player feedback.** `{actor} acts.` is too opaque for a player log. If the structured action is `ReverseFacing`, text should say `{actor} turns around.` or another diegetic verb.
- **Behavior loops are inferable but not confirmed.** A reader can guess that actors try to move, then try pickup on blockage, then run a fallback action, but the log does not state whether that is a plan, a rule, or incidental repeated output. Author/tester views should expose compact plan/action summaries separately from player prose.
- **Player passivity should be explicit when relevant.** The Alpha Slime output reads like three autonomous actors acting while the player only observes. If no player action is taken in a run, the report should make that clear in setup or turn framing so readers do not wonder whether player turns are missing.
- **Repeated turn order can look like duplicated events.** When actor names collide, a stable turn-order presentation such as `Small Slime A`, `Big Slime`, `Small Slime B` or local descriptors would help readers see that the same actors are acting in sequence rather than one entity acting twice.
- **Movement needs direction or relational context.** `{actor} moves.` confirms activity but not player-meaningful change. Prefer `{actor} moves {direction}.`, `{actor} moves nearby.`, `{actor} moves toward {target}.`, or `{actor} bumps into {target}` when facts are available.
- **Failure text should be actionable without leaking internals.** Replace internal reasons such as `InvalidPlacement`, `MoveOutOfBounds`, or raw aperture ratios with player wording like `the way is blocked`, `there is no room`, `the edge stops it`, or `{actee} is too bulky`.
- **Containment events should be explicit.** If a big slime picks up a small slime, the player line should distinguish that from picking up a pebble: `{actor} scoops up {actee}.` or `{actee} is now inside {actor}` when visible/known.
- **Structured anchors are needed for explanation.** Without target, blocker, actee, action-plan, or fallback-action anchors, neither player prose nor author review can explain what was picked up, what blocked movement, or what `{actor} acts.` actually did.
- **Run-health metadata helps interpretation.** The absence of validation/runtime errors helped an outside reader understand that the confusing log was still an intended simulation, not a crash. Keep this metadata in reports, but outside the main in-world player log.
- **Projection limits should remain visible to authors/testers.** The current player-log tool correctly labels output as `player narrative projection`; retain that caveat in reports until shared visibility/audibility filtering is implemented.

## Ratio-bearing Action Steps

Current ratio-bearing Action Steps are constrained inventory/containment transitions whose outcome may depend on actee bulk versus destination aperture. These entries have normal success/failure slots plus ratio-bucket variants.

| Action Step | Success ID | Failure ID | Success-Large ID | Success-Barely ID | Fail-Barely ID | Fail-Large ID |
|---|---|---|---|---|---|---|
| `PickupTarget` | `action.pickup_target.success` | `action.pickup_target.failure` | `action.pickup_target.success.large` | `action.pickup_target.success.barely` | `action.pickup_target.failure.barely` | `action.pickup_target.failure.large` |
| `DropFacing` | `action.drop_facing.success` | `action.drop_facing.failure` | `action.drop_facing.success.large` | `action.drop_facing.success.barely` | `action.drop_facing.failure.barely` | `action.drop_facing.failure.large` |
| `Transfer` | `action.transfer.success` | `action.transfer.failure` | `action.transfer.success.large` | `action.transfer.success.barely` | `action.transfer.failure.barely` | `action.transfer.failure.large` |
| `GiveTarget` | `action.give_target.success` | `action.give_target.failure` | `action.give_target.success.large` | `action.give_target.success.barely` | `action.give_target.failure.barely` | `action.give_target.failure.large` |
| `TakeTarget` | `action.take_target.success` | `action.take_target.failure` | `action.take_target.success.large` | `action.take_target.success.barely` | `action.take_target.failure.barely` | `action.take_target.failure.large` |
| `EnterTarget` | `action.enter_target.success` | `action.enter_target.failure` | `action.enter_target.success.large` | `action.enter_target.success.barely` | `action.enter_target.failure.barely` | `action.enter_target.failure.large` |
| `ExitFacing` | `action.exit_facing.success` | `action.exit_facing.failure` | `action.exit_facing.success.large` | `action.exit_facing.success.barely` | `action.exit_facing.failure.barely` | `action.exit_facing.failure.large` |

## Other current Action Steps

These Action Steps currently have ordinary success/failure text slots. Some may later gain richer variants if shared outcome projection exposes additional semantic buckets.

| Action Step | Success ID | Failure ID | Notes / future composition hooks |
|---|---|---|---|
| `Move` | `action.move.success` | `action.move.failure` | Promoted canonical movement. Current direct-command bridge supplies `direction`, `reason`, and `consumedTurn`; future Action Choice projection should add `directionMode`, previous/new facing, source/destination, attempted destination, and resolved direction. |
| `MoveFacing` | `action.move_facing.success` | `action.move_facing.failure` | Future variants may use direction/blocker facts. |
| `Backstep` | `action.backstep.success` | `action.backstep.failure` | Future args may include preserved facing/direction. |
| `PushFacing` | `action.push_facing.success` | `action.push_facing.failure` | Future variants may use pushability adjectives or blocker facts. |
| `DestroyTarget` | `action.destroy_target.success` | `action.destroy_target.failure` | Future wording may specialize verb by target/content. |
| `CreateFacing` | `action.create_facing.success` | `action.create_facing.failure` | Requires structured created-entity/presentation fact for final wording. |
| `SeekTarget` | `action.seek_target.success` | `action.seek_target.failure` | Future args may include target label/adjective. |
| `FleeTarget` | `action.flee_target.success` | `action.flee_target.failure` | Future args may include reciprocal adjective. |
| `MaintainChebyshevDistanceTwo` | `action.maintain_chebyshev_distance_two.success` | `action.maintain_chebyshev_distance_two.failure` | Later variants can distinguish closing, backing away, or already-at-distance outcomes. |
| `StrafeClockwise` | `action.strafe_clockwise.success` | `action.strafe_clockwise.failure` | Needs observer-relative wording if clockwise is not player-obvious. |
| `StrafeAnticlockwise` | `action.strafe_anticlockwise.success` | `action.strafe_anticlockwise.failure` | Same observer-relative caveat as clockwise strafing. |
| `ApplyPrePlan` | `action.apply_pre_plan.success` | `action.apply_pre_plan.failure` | Likely debug-facing unless plan changes become diegetic. |
| `ApplyMainPlan` | `action.apply_main_plan.success` | `action.apply_main_plan.failure` | Likely debug-facing unless plan changes become diegetic. |
| `ApplyPostPlan` | `action.apply_post_plan.success` | `action.apply_post_plan.failure` | Likely debug-facing unless plan changes become diegetic. |

## Future text dimensions

Track these as composition dimensions rather than multiplying this table immediately:

- **Observer person:** first person (`You squeeze into Tunnel`), second person, third person, or debug neutral.
- **Observer knowledge:** known identity, unknown entity, category, visible adjective, hidden target, or remembered target.
- **Relationship/target label:** `loves`, `hates`, `fears`, `seeks`, `guards`, or content-authored labels.
- **Affordance adjective:** `portable`, `enterable`, `pushable`, `breakable`, `receivable`, `takeable`, or reciprocal labels such as `hostile`.
- **Actor/actee grammatical roles:** some actions move the actor, some move a target, and some move a carried payload; sentence composition should use the structured role rather than assuming `{target}` is always the moved object.
- **Debug versus player-facing mode:** debug logs may include plan IDs, action step names, failure enum values, ratios, and entity IDs; player logs should prefer narrative text with inspectable details available separately.

## Open questions

1. Should ratio bucket names be `Barely` / `Large`, `Tight` / `Easy`, or another shared vocabulary?
2. Should failure variants use symmetric bucket names (`Fail-Large`) or player-facing intent names (`Fail-Impossible`, `Fail-MuchTooLarge`)?
3. Which layer should choose first-person versus third-person wording when the observer is also the controlled actor?
4. How should unknown or partially known targets be named when target labels/adjectives are visible but identity is not?
5. Do plan-override actions need player-facing diegetic text, or should they remain debug/editor-only log entries for now?
