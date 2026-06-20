# Sprint 13: Gate 1 Direction Showcases

Status: Planned / active sprint plan.

Related plans and source of truth:

- `docs/Source of Truth/Engine-Editor-Capabilities.md`
- `docs/Plans/High-Level-Roadmap.md`
- `docs/Plans/Beta-Content-Exploration-Plan.md`
- `docs/Plans/Beta-Capability-Gap-Log.md`
- `docs/Archived/Sprint-12-Beta-Primitive-Showcases.md`

## Sprint goal

Create and validate the Gate 1 direction-transform beta showcase set: five small authored scenarios that demonstrate facing-relative behavior made possible by `ReverseFacing`, `TurnLeft`, `TurnRight`, and `Backstep`.

Sprint 13 is intentionally both a content sprint and a cross-role coordination sprint. The content owner should request each missing Gate 1 primitive from the core owner as soon as the next selected showcase needs it, then author and validate the showcase against the newly available capability before moving on.

## Context from Sprint 12

Sprint 12 completed the first current-tool beta primitive showcase batch:

- `PushFacing`
- `DestroyTarget`
- `CreateFacing`
- `DropFacing`
- pickup/drop/weight behavior
- blocker/target fallback-chain composition

The curated actor zoo was deferred because current primitives alone did not make actor behavior varied enough. Direction transforms are the next planned beta gate and should unlock richer local movement patterns: turners, patrollers, bouncers, sentries, and backsteppers.

GAP-001 remains open for successful `CreateFacing` Console rendering/presentation binding. Do not promote GAP-001 during Sprint 13 unless a selected direction-transform showcase unexpectedly depends on creation/spawning.

## Gate 1 primitive requests

Request and coordinate with the core owner for these canonical Action Steps as needed:

1. `ReverseFacing`
   - Read persistent actor `Facing`.
   - Write persistent actor `Facing` to the opposite direction.
   - Move no entity.
   - Expected use: wall-bounce actor, reversing sentry.

2. `TurnLeft`
   - Read persistent actor `Facing`.
   - Write persistent actor `Facing` 90 degrees counter-clockwise.
   - Move no entity.
   - Expected use: patrol turns, rotating sentry, actor zoo.

3. `TurnRight`
   - Read persistent actor `Facing`.
   - Write persistent actor `Facing` 90 degrees clockwise.
   - Move no entity.
   - Expected use: patrol turns, rotating sentry, actor zoo.

4. `Backstep`
   - Read persistent actor `Facing`.
   - Move actor one cell opposite `Facing` without changing `Facing`.
   - On blocked movement, fail/fall through like `MoveFacing`.
   - Preferred first-pass blocker rule: write canonical `Target` when blocked by an entity; do not write a meaningful `Target` for out-of-bounds/non-entity blockage.
   - Expected use: retreat puzzle, backstepper actor, future facing-preserving tactical movement.

The core-owner implementation should include runtime execution, catalog metadata, descriptor/YAML support, validation/default handling, editor service support, agent/headless API support, compact trace wording, tests, and updates to `Engine-Editor-Capabilities.md`.

## Showcase package

Create Sprint 13 beta content under:

- `src/GameGameGame.Content/Beta/DirectionTransforms/`

Prefer one YAML file per showcase unless repeated template/action-plan definitions become noisy enough to justify grouping.

## Planned showcases

### 1. `beta-turn-showcase`

Status: Authored and headlessly validated.

Manual testing notes:

- `TurnLeft`, `TurnRight`, and `ReverseFacing` only change internal actor `Facing` state. Barring future features or invalid actor state, they effectively never fail.
- Because they effectively never fail, they act as behavior-chain terminal steps: any later fallback step in the same canonical chain is not expected to trigger after one of these steps succeeds.
- The result is not very interesting from the player's current perspective because the simulation has no visible facing indicator or player-facing action feedback. The state change is observable in headless traces/final-state summaries, but not strongly observable during Console play.
- Follow-up showcases should therefore use turn primitives as setup/enablers for visible movement patterns, such as wall bouncing, patrol turns, and actor-zoo behavior, rather than treating isolated in-place turning as a compelling player-facing vignette.

Purpose: prove in-place direction changes.

Demonstrate:

- `TurnLeft` updates `Facing` without moving;
- `TurnRight` updates `Facing` without moving;
- `ReverseFacing` updates `Facing` without moving;
- compact traces clearly report facing reads/writes.

Primitive dependency:

- `TurnLeft`
- `TurnRight`
- `ReverseFacing`

### 2. `beta-backstep-showcase`

Status: Authored and headlessly validated.

Testing notes:

- `Backstep` is more player-visible than isolated turns because it changes actor position while preserving `Facing`.
- Successful `Backstep` terminates the behavior chain by consuming the turn.
- Failed `Backstep` can participate in fallback chains like `MoveFacing`: entity-blocked failure writes canonical `Target`, while out-of-bounds failure does not write a meaningful `Target`.

Purpose: prove facing-preserving retreat and blocked retreat behavior.

Demonstrate:

- successful `Backstep` moves opposite `Facing`;
- actor preserves original `Facing` after moving;
- blocked `Backstep` fails and falls through;
- entity blocker writes `Target` if the selected first-pass rule is implemented.

Primitive dependency:

- `Backstep`

### 3. `beta-wall-bounce`

Status: Authored and headlessly validated.

Testing notes:

- `MoveFacing -> ReverseFacing` makes `ReverseFacing` useful as a visible fallback behavior: the actor moves, bumps into a blocker, reverses, then moves back on a later turn.
- The showcase confirms that turn primitives are more compelling when composed after a failing movement step than when demonstrated in isolation.
- The actor retains the `Target` written by the blocked `MoveFacing`; this is useful trace evidence but may be mildly noisy for pure movement-pattern actors.

Purpose: prove an actor can reverse when forward movement is blocked.

Suggested behavior chain:

- `MoveFacing -> ReverseFacing`

Demonstrate:

- actor advances while space is open;
- blocked forward movement falls through to `ReverseFacing`;
- later turns move the actor back in the reversed direction.

Primitive dependency:

- `ReverseFacing`

### 4. `beta-patrol-turn`

Status: Authored and headlessly validated.

Testing notes:

- `MoveFacing -> TurnRight` and `MoveFacing -> TurnLeft` produce clear obstacle-reactive patrol behavior: actors walk until blocked, turn, then continue on a later turn in the new direction.
- Like wall-bounce, this confirms that in-place turn primitives become player-visible when paired with movement failure.
- The retained `Target` from the blocked `MoveFacing` remains visible in final-state summaries even after the actor has moved away in the new direction; this may be useful for debugging but could become noisy in movement-only showcases.

Purpose: prove a simple obstacle-reactive patrol pattern.

Suggested behavior chains to compare:

- `MoveFacing -> TurnRight`
- optional second actor with `MoveFacing -> TurnLeft`

Demonstrate:

- actor walks while space is open;
- actor turns at an obstacle;
- actor continues on the following turn in the new facing direction.

Primitive dependency:

- `TurnRight`
- optionally `TurnLeft`

### 5. `beta-direction-actor-zoo`

Status: Deferred during Sprint 13 wrap-up.

Deferral notes:

- The focused Gate 1 showcases already proved the individual primitives and the most important movement compositions.
- A zoo would be useful as a lightweight gallery or copy/paste reference for future content authors, but it would mostly duplicate the authored evidence from `beta-turn-showcase`, `beta-backstep-showcase`, `beta-wall-bounce`, and `beta-patrol-turn`.
- Pure turner/reverser actors remain weak Console demos while facing has no visible rendering or player-facing action feedback.
- Revisit the zoo later if a curated gallery becomes useful for presentation, onboarding, or comparing a larger set of actor personalities.

Purpose: revive the deferred curated actor zoo with Gate 1 behavior now available.

Demonstrate one-room or side-by-side actors such as:

- left turner;
- right turner;
- reverser;
- backstepper;
- wall bouncer;
- simple patroller.

This showcase should be authored after the first four focused showcases, so it can reuse proven templates and action plans.

Primitive dependency:

- all Gate 1 primitives, unless intentionally scoped smaller.

## Working cadence

For each showcase:

1. Confirm the needed primitive exists in `Engine-Editor-Capabilities.md`.
2. If missing, send the core owner a concise primitive request including the showcase need, desired state reads/writes, fallback behavior, trace expectations, and validation/API requirements.
3. After the primitive lands, author the content fixture using normal entity templates, presentations, scenario roots, and canonical behavior chains.
4. Validate the content document.
5. Run the scenario headlessly and inspect trace/final-state output.
6. Hand off for Console testing when the headless report is satisfactory.
7. Record any unsupported behavior or reporting friction in `Beta-Capability-Gap-Log.md`.

## Definition of done

- The four focused Direction Transform showcase scenarios exist under `src/GameGameGame.Content/Beta/DirectionTransforms/`: `beta-turn-showcase`, `beta-backstep-showcase`, `beta-wall-bounce`, and `beta-patrol-turn`.
- The fifth planned showcase, `beta-direction-actor-zoo`, is explicitly deferred with rationale.
- Each authored showcase is valid content and has beta fixture test coverage.
- Each authored showcase has been run headlessly with readable traces and final-state observations.
- Console launch/manual testing is possible for the showcase set.
- `Engine-Editor-Capabilities.md` reflects the implemented Gate 1 capabilities.
- `Beta-Content-Exploration-Plan.md`, `High-Level-Roadmap.md`, and `Beta-Capability-Gap-Log.md` are updated with any Sprint 13 findings.
- Existing Sprint 12 beta showcases still validate and run.

## Non-goals

- Do not implement Gate 2 `AcquireNearestTarget` / `SeekTarget` in this sprint.
- Do not implement forced-movement brainstorm primitives such as shove, pull, or drag.
- Do not implement template spawning or fix GAP-001 unless a Sprint 13 showcase is blocked by it.
- Do not implement peer inventory transfer, reactions, traps, combat, or pathfinding.
- Do not modify Core, Editor, or Console code from the content-owner role; coordinate with the core owner for primitive implementation.

## Sprint 13 wrap-up notes

### Implementation and authoring lessons

- The in-place turn primitives were mechanically straightforward to author once implemented, but their design implications were more subtle than expected: because `TurnLeft`, `TurnRight`, and `ReverseFacing` mutate only internal `Facing` state and effectively never fail, they act as terminal behavior-chain steps. Any later fallback step is unreachable after they succeed.
- Isolated internal state changes are weak player-facing content. `beta-turn-showcase` was useful as a primitive proof and trace check, but not compelling as gameplay because current Console play does not visibly render facing or action intent.
- `Backstep` was more interesting than isolated turns because it changes position while preserving facing. Its success/failure behavior also made fallback semantics more meaningful: successful movement consumes the turn, blocked movement can fall through, and entity-blocked failure can write `Target` for later steps.
- The strongest movement showcases used turning as a fallback after failed movement. `MoveFacing -> ReverseFacing`, `MoveFacing -> TurnRight`, and `MoveFacing -> TurnLeft` turned internal facing updates into visible later movement.
- Retained `Target` from failed `MoveFacing` was useful for debugging and trace evidence, but it may become noisy for pure movement-pattern actors after they move away from the blocker. Future content/reporting work may need either clearer target-lifetime semantics or report wording that distinguishes stale blocker targets from active goals.

### Core-owner coordination lessons

- Coordination with core-owner was smooth. The content-owner could request primitives one showcase at a time with concrete scenario needs, desired state reads/writes, fallback behavior, trace expectations, and validation/API requirements.
- The incremental cadence worked well: implement only the primitives needed for the next showcase, then immediately prove them with authored content and fixture tests.
- No major cross-role friction appeared. The main coordination requirement was being explicit about semantics before implementation, especially for turn consumption, fallback behavior, and whether failed movement should write `Target`.
- The only operational friction was test-output/build locking, which was handled by running tests with isolated `OutDir` values. This did not affect content design.

### Most game-like showcases

Most game-like, in order:

1. `beta-patrol-turn`
   - Best demonstration of actor behavior that resembles a simple game enemy or environmental automaton: walk, react to obstacle, change route, continue.
2. `beta-wall-bounce`
   - Clear and readable movement personality; the actor visibly responds to a blocker and reverses direction.
3. `beta-backstep-showcase`
   - Useful and more visible than pure turning, but still more like a movement primitive/puzzle component than a complete behavior loop.
4. `beta-turn-showcase`
   - Valuable as a primitive proof, least game-like in Console because the meaningful state change is mostly hidden.

Overall finding: Gate 1 succeeded as a content foundation. The primitives are most valuable when they are composed with movement failure to create visible local behavior patterns, not when showcased as isolated state mutations.
