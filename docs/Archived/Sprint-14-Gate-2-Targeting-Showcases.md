# Sprint 14: Gate 2 Targeting Showcases

Status: Completed / archived during Sprint 14 wrap-up.

Related plans and source of truth:

- `docs/Source of Truth/Engine-Editor-Capabilities.md`
- `docs/Plans/High-Level-Roadmap.md`
- `docs/Plans/Beta-Content-Exploration-Plan.md`
- `docs/Source of Truth/Capability-Gap-Log.md`
- `docs/Archived/Sprint-13-Gate-1-Direction-Showcases.md`

## Sprint goal

Create and validate the Gate 2 targeting beta showcase set: four small authored scenarios that demonstrate same-plane target acquisition and greedy movement toward a persistent target.

Sprint 14 should follow the successful Sprint 13 cadence: request only the primitives needed for the next showcase, wait for Core/editor/API support to land, then immediately prove the capability with authored content, validation, and headless scenario runs.

## Context from Sprint 13

Sprint 13 completed the focused Gate 1 direction-transform showcase set:

- `beta-turn-showcase`
- `beta-backstep-showcase`
- `beta-wall-bounce`
- `beta-patrol-turn`

The planned `beta-direction-actor-zoo` was deferred because it would mostly duplicate the focused showcase evidence.

Gate 1's main content lesson was that internal facing changes are most valuable when they produce visible later movement. Gate 2 should now move from local movement personality to goal-directed actor behavior: actors selecting targets, chasing them, collecting them, or interacting with them after contact.

## Showcase package

Create Sprint 14 beta content under:

- `src/GameGameGame.Content/Beta/Targeting/`

Prefer one YAML file per showcase unless repeated targetable/player/template definitions become noisy enough to justify grouping.

## Gate 2 primitive request for core owner

The first showcase required the following canonical Action Steps before content could be authored and validated. These primitives have now landed for first-pass Sprint 14 content.

### `AcquireNearestTarget`

Content need:

- A selector/setup Action Step that chooses a target for later target-oriented steps.
- The first proof scenario is `beta-acquire-target-showcase`.

Requested first-pass semantics:

- Read the actor's current same-plane position.
- Search same-plane entities only.
- Exclude self.
- Use a deliberately small target-filter policy. Implemented first-pass behavior has no authorable filter fields; the filter is same plane, entity exists, and not self.
- Select the nearest valid target by Manhattan distance.
- Use deterministic tie-breaks for equal distances. Row-major world/plane coordinate order is acceptable if no better existing engine convention applies.
- Write persistent actor `Target` to the selected entity.
- If no target is found, fail/fall through without writing a new meaningful `Target`.
- This should be a setup/selector step that can continue to the next canonical behavior-chain step on success rather than consuming the root turn. Otherwise `AcquireNearestTarget -> SeekTarget` would never move in the same turn.

Requested report/trace wording:

- Report selected target entity ID/template/name where available.
- Report distance.
- Report tie-break behavior when useful.
- Report no-target failure distinctly from validation/runtime errors.

Requested support layers:

- Core runtime execution.
- Action Step catalog metadata.
- Descriptor/YAML support.
- Validation/default handling.
- Editor service support.
- Agent/headless API support.
- Compact behavior-chain trace formatting.
- Scenario-report visibility.
- Tests.
- `Engine-Editor-Capabilities.md` update.

### `SeekTarget`

Content need:

- A movement Action Step that expresses the content intent "move one step toward the persistent target" without making authors manually turn toward the target and then call `MoveFacing`.
- The flagship proof scenario is expected to be `beta-direct-chase`.

Requested first-pass semantics:

- Read persistent actor `Target`.
- Validate that the target exists and is on the actor's current plane.
- Choose one adjacent cardinal step that reduces Manhattan distance to the target.
- Use deterministic tie-breaks when multiple reducing steps are possible.
- Move the actor if the chosen destination is open.
- Consume the turn on successful movement.
- If the actor is already adjacent and the reducing step would enter the target's occupied cell, fail/fall through while preserving `Target`, so later steps such as `DestroyTarget` or `PickupTarget` can act.
- If movement is blocked by another entity, terrain, or bounds, fail/fall through and report the blocker/reason. Prefer preserving the goal `Target` rather than overwriting it with incidental blockers; Sprint 13 found stale blocker targets can be noisy for movement-pattern actors.
- If `Target` is missing, destroyed, self, or off-plane, fail/fall through with clear trace/report wording.

Requested report/trace wording:

- Report target read.
- Report selected movement step and tie-break basis.
- Report successful movement.
- Report blocked movement, stale/missing/off-plane target, and adjacent/contact fallthrough distinctly.

Requested support layers:

- Core runtime execution.
- Action Step catalog metadata.
- Descriptor/YAML support.
- Validation/default handling.
- Editor service support.
- Agent/headless API support.
- Compact behavior-chain trace formatting.
- Scenario-report visibility.
- Tests.
- `Engine-Editor-Capabilities.md` update.

## Planned showcases

### 1. `beta-acquire-target-showcase`

Status: Authored and headlessly validated.

Content file:

- `src/GameGameGame.Content/Beta/Targeting/AcquireTargetShowcase.yaml`

Testing notes:

- First-pass `AcquireNearestTarget` has no template/category filter, so all same-plane non-self entities are valid targets, including the inserted player. The showcase places the player farther away than the candidate target beacons so player insertion does not interfere with the intended acquisition evidence.
- Because `AcquireNearestTarget` is a setup/selector step that does not consume a turn, a single-step acquisition-only plan proves target writing through traces/final state rather than visible movement.
- The showcase uses two equally near target beacons to prove row-major tie-break behavior: `westTieTarget` at `(3,1)` is selected over `eastTieTarget` at `(5,1)`.

Purpose: prove target acquisition independently before combining it with pursuit movement.

Planned content shape:

- small room;
- one acquiring actor;
- at least two candidate target entities at different distances;
- optionally a same-distance candidate pair if tie-break behavior needs direct content evidence;
- player inserted away from the candidates so player insertion does not interfere with target-selection evidence.

Suggested behavior chain:

- `AcquireNearestTarget`

Demonstrate:

- actor selects nearest valid same-plane target;
- selected target is written to persistent `Target`;
- deterministic tie-breaks are legible if exercised;
- no-target behavior is reported clearly if included as a second actor/case.

Primitive dependency:

- `AcquireNearestTarget`

Core-owner coordination note:

- Complete for the first showcase. `AcquireNearestTarget` is listed as supported in `Engine-Editor-Capabilities.md`.

### 2. `beta-direct-chase`

Status: Authored and headlessly validated.

Content file:

- `src/GameGameGame.Content/Beta/Targeting/DirectChaseShowcase.yaml`

Testing notes:

- Headless `RunScenario` currently runs from the scenario-root template and does not insert the persisted scenario player. To keep the headless report meaningful, the showcase includes an inert `directChaseTarget` entity in the room and places the persisted player start farther away for Console/manual play.
- The first-pass no-filter `AcquireNearestTarget` semantics mean the direct chase room should stay simple: additional same-plane props near the chaser would become valid acquisition targets.
- `AcquireNearestTarget -> SeekTarget` successfully acquires and moves in the same turn, then repeats over multiple turns for visible pursuit.

Purpose: prove the first game-like Gate 2 pursuit behavior.

Suggested behavior chain:

- `AcquireNearestTarget -> SeekTarget`

Demonstrate:

- enemy acquires the nearest same-plane non-self target;
- enemy moves one step toward that target over repeated turns;
- deterministic step choice is visible in the report;
- stale/missing/off-plane or blocked-target cases are reported clearly if encountered.

Primitive dependency:

- `AcquireNearestTarget`
- `SeekTarget`

### 3. `beta-targeted-destroyer`

Status: Authored and headlessly validated.

Content file:

- `src/GameGameGame.Content/Beta/Targeting/TargetedDestroyerShowcase.yaml`

Testing notes:

- `AcquireNearestTarget -> SeekTarget -> DestroyTarget` works as the first full target-acquire/pursue/interact chain.
- The actor approaches for two turns, then `SeekTarget` fails/falls through at contact because the reducing step would enter the occupied target cell; `DestroyTarget` then succeeds using the preserved `Target`.
- This proves that preserving goal `Target` through `SeekTarget` contact failure is useful for targeted interaction chains.

Purpose: combine Gate 2 targeting with existing `DestroyTarget` to make a dynamic targeted interaction.

Suggested behavior chain:

- `AcquireNearestTarget -> SeekTarget -> DestroyTarget`

Demonstrate:

- actor acquires a destructible target;
- actor approaches it;
- when movement cannot enter the occupied target cell, fallthrough preserves `Target`;
- `DestroyTarget` removes the target.

Primitive dependency:

- `AcquireNearestTarget`
- `SeekTarget`
- existing `DestroyTarget`

### 4. `beta-collector`

Status: Authored and headlessly validated.

Content file:

- `src/GameGameGame.Content/Beta/Targeting/CollectorShowcase.yaml`

Testing notes:

- `AcquireNearestTarget -> SeekTarget -> PickupTarget` works as the first autonomous collector chain.
- The actor approaches for two turns, then `SeekTarget` fails/falls through at contact while preserving `Target`; `PickupTarget` then succeeds.
- The showcase was extended after initial testing so the materialized scenario player is also a valid later target. The collector picks up the gem first, then continues toward the inserted player and picks up the player as well.
- This required a core-owner follow-up to make canonical `PickupTarget` choose the first valid available actor inventory coordinate in deterministic row-major order rather than always using `(0,0)`. The resulting behavior allows the gem and player to occupy separate slots in the collector inventory.
- The root-only headless report still proves gem pickup in three turns; the beta fixture test additionally materializes the persisted scenario with player insertion and advances six actor turns to verify both `collectibleGem` and `betaPlayer` end up in the collector inventory.

Purpose: combine Gate 2 targeting with existing `PickupTarget` to make a simple autonomous collector.

Suggested behavior chain:

- `AcquireNearestTarget -> SeekTarget -> PickupTarget`

Demonstrate:

- actor acquires nearest item;
- actor approaches it;
- when movement cannot enter the occupied item cell, fallthrough preserves `Target`;
- `PickupTarget` moves the item into actor inventory;
- inventory/containment reporting is readable enough to verify the result.

Primitive dependency:

- `AcquireNearestTarget`
- `SeekTarget`
- existing `PickupTarget`

### Tentative/deferred: `beta-follower`

Status: Tentatively planned only if a differentiated use case appears.

Purpose: non-hostile following behavior.

Deferral rationale:

- Mechanically, this is likely the same as `beta-direct-chase`: `AcquireNearestTarget -> SeekTarget`.
- Author only if Sprint 14 reveals a clear presentation, player-experience, target-filter, or report-reading distinction that makes follower behavior meaningfully different from direct chase.

## Definition of done

- `AcquireNearestTarget` and `SeekTarget` are listed as supported in `Engine-Editor-Capabilities.md` with their state reads/writes, fallback behavior, and support layers.
- The first four targeting showcases exist under `src/GameGameGame.Content/Beta/Targeting/` unless a showcase is explicitly deferred with rationale.
- Each authored showcase validates as normal content.
- Each authored showcase has beta fixture test coverage or equivalent headless validation.
- Each authored showcase has been run headlessly with readable traces and final-state observations.
- Console launch/manual testing is possible for at least `beta-direct-chase`.
- Any unsupported behavior or reporting friction is recorded in `Beta-Capability-Gap-Log.md`.
- `beta-follower` is either authored with a documented differentiating use case or remains explicitly deferred.

## Non-goals

- Do not implement Gate 3 fleeing, keep-away, kiting, distance bands, or patterned pursuit.
- Do not implement `TurnTowardTarget` / `FaceTarget` unless the core owner chooses to include it as a low-cost extra; Sprint 14 content should not depend on it.
- Do not implement pathfinding around obstacles; first-pass `SeekTarget` should be greedy and deterministic.
- Do not implement `Give`/`Take`, template spawning, reactions, traps, combat systems, factions, relationships, or scheduler/speed changes.
- Do not modify Core, Editor, or Console code from the content-owner role; coordinate primitive implementation with the core owner.
