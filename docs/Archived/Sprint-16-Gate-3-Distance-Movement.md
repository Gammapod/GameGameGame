# Sprint 16: Gate 3 Distance Movement Showcases

Status: Completed / archived during Sprint 16 wrap-up.

Related plans and source of truth:

- `docs/Source of Truth/Engine-Editor-Capabilities.md`
- `docs/Plans/High-Level-Roadmap.md`
- `docs/Plans/Beta-Content-Exploration-Plan.md`
- `docs/Source of Truth/Capability-Gap-Log.md`
- `docs/Archived/Sprint-14-Gate-2-Targeting-Showcases.md`
- `docs/Archived/Sprint-15-Debug-Scenario-Recorder.md`

## Sprint goal

Create and validate the Gate 3 target-distance / directional-choice beta showcase set: five small authored scenarios that demonstrate fleeing, hard-coded distance maintenance, and target-relative strafing while keeping authored action plans linear.

Sprint 16 should use the Sprint 15 recording workflow as part of the normal content validation loop. Each showcase should produce a debug GIF artifact after the scenario validates and runs satisfactorily.

## Design direction

Gate 3 should add the smallest useful primitive set for target-relative movement without introducing general branching or programmable distance checks in action plans.

Action plans should remain canonical ordered behavior chains. Any distance check, target-distance comparison, or directional-choice logic needed for this sprint should live inside an Action Step primitive rather than being authored as separate conditional branches.

More complex patterned pursuit, richer distance bands, and configurable parameters remain post-Sprint 16 work unless a selected showcase is unexpectedly blocked.

## Primitive set for Sprint 16

### Existing primitive: `SeekTarget`

Already supported from Gate 2.

Use as the baseline movement-toward-target primitive:

- read persistent actor `Target`;
- choose one deterministic cardinal step that reduces distance to the target;
- move if destination is open;
- consume the turn on successful movement;
- fail/fall through while preserving `Target` when missing, invalid, adjacent/contact-blocked, off-plane, out of bounds, or blocked.

### New primitive request: flee target

Working name: `FleeTarget`.

Final Action Step naming should be coordinated with the core owner before implementation, but the content intent is to use a concise target-relative verb rather than adding both `MoveAwayFromTarget` and `StepAwayFromTarget`.

Requested first-pass semantics:

- read persistent actor `Target`;
- require target to exist, not be self, and be on the actor's current plane;
- choose one deterministic cardinal move that increases distance from the target;
- move if destination is valid/open;
- consume the turn on successful movement;
- fail/fall through when no valid distance-increasing move exists;
- preserve persistent `Target` on success and failure;
- report selected escape direction, distance before/after, blocker/out-of-bounds failures, and invalid/missing target failures in compact traces and scenario reports.

### New primitive request: hard-coded Chebyshev distance two

Working shorthand: `C-DistanceTwo`. Final Action Step name: `MaintainChebyshevDistanceTwo`.

The final name uses catalog/YAML-friendly PascalCase and directly names the hard-coded Chebyshev distance-2 maintenance behavior.

Content intent:

- maintain Chebyshev distance exactly 2 from persistent `Target`;
- if closer than 2, move away/flee;
- if farther than 2, move toward/seek;
- if already exactly 2 away, fail/fall through without consuming the turn.

Requested first-pass semantics:

- read persistent actor `Target`;
- require target to exist, not be self, and be on the actor's current plane;
- compute Chebyshev distance between actor and target;
- when distance is less than 2, choose one deterministic valid/open cardinal move that increases Chebyshev distance toward 2;
- when distance is greater than 2, choose one deterministic valid/open cardinal move that decreases Chebyshev distance toward 2;
- when distance is exactly 2, fail/fall through without moving or consuming the turn;
- preserve persistent `Target`;
- report distance before/after, whether the primitive chose seek/flee/no-op-fallthrough mode, selected direction, and blockers/failures.

Design note:

- The exactly-distance-2 fallthrough is intentional. It allows linear chains such as `AcquireNearestTarget -> C-DistanceTwo -> StrafeClockwise`, where the actor closes/backs away until it reaches the desired distance, then orbits while already at range.
- Implemented first slice: `MaintainChebyshevDistanceTwo` uses Chebyshev distance, tries improving cardinal candidates in `North`, `South`, `West`, `East` order, reports `flee/back-away`, `seek/close`, or `ideal-distance fallthrough` mode, and preserves `Target` on success/failure.

### New primitive request: clockwise strafe

Working name: `StrafeClockwise`.

Implementation status: Core first slice implemented with final Action Step name `StrafeClockwise`.

Requested first-pass semantics:

- read persistent actor `Target`;
- require target to exist, not be self, and be on the actor's current plane;
- determine the deterministic primary seek direction toward the target using the same tie-break basis as `SeekTarget` where possible;
- choose the cardinal direction perpendicular clockwise to that seek direction;
- move if destination is valid/open;
- consume the turn on successful movement;
- fail/fall through when the strafe destination is invalid, blocked, or no primary seek direction can be selected;
- preserve persistent `Target`;
- report primary seek direction, selected strafe direction, and blocker/out-of-bounds/invalid-target failures.

### New primitive request: anticlockwise strafe

Working name: `StrafeAnticlockwise`.

Implementation status: Core first slice implemented with final Action Step name `StrafeAnticlockwise`.

Requested first-pass semantics:

- same as `StrafeClockwise`, except choose the opposite perpendicular direction from the primary seek direction;
- preserve persistent `Target`;
- consume the turn on successful movement;
- fail/fall through on invalid/blocked movement;
- report primary seek direction, selected strafe direction, and failures.

## Showcase package

Create Sprint 16 beta content under:

- `src/GameGameGame.Content/Beta/DistanceMovement/`

Prefer one YAML file per showcase unless repeated target/player/room definitions become noisy enough to justify grouping.

Each showcase should be:

1. authored as normal content using entity templates, presentations, scenario roots, player insertion, and canonical behavior chains;
2. validated through the editor/content validation path;
3. run headlessly until behavior traces and final state are satisfactory;
4. recorded through the Sprint 15 debug recording command/API;
5. handed off with the generated GIF path and a short interpretation note.

Expected recording command shape:

```text
dotnet run --project src/GameGameGame.Console -- record-scenario <content-file> <scenario-id> --turns <N> --output <directory>
```

Use an output directory outside source content or another agreed artifact location so generated PNG/GIF files do not get accidentally committed unless explicitly requested.

## Planned showcases

### 1. `beta-flee-target`

Status: Authored, headlessly validated, and recorded.

Content file:

- `src/GameGameGame.Content/Beta/DistanceMovement/FleeTargetShowcase.yaml`

Purpose: prove basic target-relative fleeing.

Suggested behavior chain:

- `AcquireNearestTarget -> FleeTarget`

Demonstrate:

- actor acquires the nearest same-plane target;
- actor chooses a deterministic move that increases distance from the target;
- target state is preserved across flee turns;
- blocked/invalid flee attempts are reported distinctly if included as a second actor/case.

Primitive dependency:

- `FleeTarget`

Implementation note:

- Final Action Step name is `FleeTarget`. The first slice uses Manhattan distance, matching `SeekTarget`, and evaluates distance-increasing cardinal moves with the same `North`, `South`, `West`, `East` tie-break, taking the first valid/open destination while preserving `Target`.

Testing notes:

- The showcase uses `AcquireNearestTarget -> FleeTarget` with one fleeing actor and one nearby target beacon. The persisted scenario player is placed farther away so first-pass unfiltered target acquisition selects the intended beacon during the proof turns.
- Headless validation confirms the actor acquires `fleeBeacon`, flees north on the first turn by the deterministic tie-break, continues increasing Manhattan distance over repeated turns, and preserves persistent `Target=fleeBeacon`.
- A debug recording was generated with 4 simulated turns, producing 5 frames and an animated GIF at `C:\Users\Scramble\AppData\Local\Temp\opencode\GameGameGame-Sprint16-beta-flee-target\beta-flee-target.gif`.

Recording expectation:

- Complete. The first recording shows the actor move away from the acquired target over several turns; a boundary/blocker failure case can be added later if repeated Gate 3 content needs explicit failure showcase coverage.

### 2. `beta-distance-two`

Status: Authored, headlessly validated, and recorded.

Content file:

- `src/GameGameGame.Content/Beta/DistanceMovement/DistanceTwoShowcase.yaml`

Purpose: prove hard-coded Chebyshev distance maintenance without action-plan branching.

Suggested behavior chain:

- `AcquireNearestTarget -> MaintainChebyshevDistanceTwo`

Demonstrate three cases where practical:

- too close: actor flees until moving toward Chebyshev distance 2;
- too far: actor seeks until moving toward Chebyshev distance 2;
- already distance 2: primitive fails/falls through without consuming the turn.

Primitive dependency:

- `MaintainChebyshevDistanceTwo`.

Testing notes:

- Final Action Step name is `MaintainChebyshevDistanceTwo`.
- The showcase uses three maintainer/beacon pairs in one room to prove each first-slice mode: too-close back-away, too-far close-in, and exact-distance fallthrough.
- Headless validation confirms the too-close maintainer acquires `tooCloseBeacon` and moves north to Chebyshev distance 2, the too-far maintainer acquires `tooFarBeacon` and moves south toward the distance band, and the ideal maintainer acquires `idealBeacon` then fails/falls through without consuming movement because it is already Chebyshev distance 2.
- A debug recording was generated with 3 simulated turns, producing 4 frames and an animated GIF at `C:\Users\Scramble\AppData\Local\Temp\opencode\GameGameGame-Sprint16-beta-distance-two\beta-distance-two.gif`.

Recording expectation:

- Complete. The recording shows too-close and too-far actors adjust toward the hard-coded Chebyshev distance band; the exact-distance case is intentionally static and should be interpreted through trace/report text.

### 3. `beta-strafe-clockwise`

Status: Authored, headlessly validated, and recorded.

Content file:

- `src/GameGameGame.Content/Beta/DistanceMovement/StrafeClockwiseShowcase.yaml`

Purpose: prove clockwise target-relative perpendicular movement.

Suggested behavior chain:

- `AcquireNearestTarget -> StrafeClockwise`

Demonstrate:

- actor identifies a primary seek direction toward target;
- actor moves perpendicular clockwise from that direction;
- repeated turns create readable orbit/circling pressure where room geometry permits;
- blocked strafe failure is reported if included.

Primitive dependency:

- `StrafeClockwise`

Implementation note:

- Final Action Step name is `StrafeClockwise`. It selects the same primary seek direction as `SeekTarget` using Manhattan reduction with `North`, `South`, `West`, `East` tie-break, then attempts only the clockwise perpendicular destination. It preserves `Target` on success/failure, consumes the turn only on successful movement, and reports primary/strafe direction plus invalid target or blocked/out-of-bounds failures.

Testing notes:

- The showcase uses `AcquireNearestTarget -> StrafeClockwise` with one strafer and one target beacon. The actor begins east of the target so the first primary seek direction is `West`, and clockwise strafing selects `North`.
- Headless validation confirms the actor acquires `clockwiseStrafeBeacon`, reports `primary=West`, moves north on the first strafe, and continues clockwise target-relative movement over repeated turns while preserving `Target=clockwiseStrafeBeacon`.
- A debug recording was generated with 4 simulated turns, producing 5 frames and an animated GIF at `C:\Users\Scramble\AppData\Local\Temp\opencode\GameGameGame-Sprint16-beta-strafe-clockwise\beta-strafe-clockwise.gif`.

Recording expectation:

- Complete. The recording uses the same layout shape as `beta-strafe-anticlockwise` so the two movement directions can be contrasted.

### 4. `beta-strafe-anticlockwise`

Status: Authored, headlessly validated, and recorded.

Content file:

- `src/GameGameGame.Content/Beta/DistanceMovement/StrafeAnticlockwiseShowcase.yaml`

Purpose: prove anticlockwise target-relative perpendicular movement and compare against clockwise strafe.

Suggested behavior chain:

- `AcquireNearestTarget -> StrafeAnticlockwise`

Demonstrate:

- actor identifies a primary seek direction toward target;
- actor moves perpendicular anticlockwise from that direction;
- movement is visibly mirrored or distinguishable from the clockwise showcase;
- blocked strafe failure is reported if included.

Primitive dependency:

- `StrafeAnticlockwise`

Implementation note:

- Final Action Step name is `StrafeAnticlockwise`. It uses the same primary seek direction selection as `StrafeClockwise`, then attempts only the anticlockwise perpendicular destination. It preserves `Target` on success/failure, consumes the turn only on successful movement, and reports primary/strafe direction plus invalid target or blocked/out-of-bounds failures.

Testing notes:

- The showcase mirrors the clockwise layout with `AcquireNearestTarget -> StrafeAnticlockwise`. The actor begins east of the target so the first primary seek direction is `West`, and anticlockwise strafing selects `South`.
- Headless validation confirms the actor acquires `anticlockwiseStrafeBeacon`, reports `primary=West`, moves south on the first strafe, and continues mirrored anticlockwise target-relative movement over repeated turns while preserving `Target=anticlockwiseStrafeBeacon`.
- A debug recording was generated with 4 simulated turns, producing 5 frames and an animated GIF at `C:\Users\Scramble\AppData\Local\Temp\opencode\GameGameGame-Sprint16-beta-strafe-anticlockwise\beta-strafe-anticlockwise.gif`.

Recording expectation:

- Complete. The recording uses a comparable layout to `beta-strafe-clockwise` and visually distinguishes the mirrored first move and subsequent path.

### 5. `beta-kiting-orbiter`

Status: Authored, headlessly validated, and recorded.

Content file:

- `src/GameGameGame.Content/Beta/DistanceMovement/KitingOrbiterShowcase.yaml`

Purpose: compose distance maintenance and strafing into the first Gate 3 behavior personality.

Suggested behavior chain:

- `AcquireNearestTarget -> MaintainChebyshevDistanceTwo -> StrafeClockwise -> StrafeAnticlockwise -> FleeTarget -> SeekTarget`

Intended behavior:

- if too close, the actor uses the distance-two primitive to back away;
- if too far, the actor uses the distance-two primitive to close in;
- if exactly Chebyshev distance 2, the distance-two primitive fails/falls through;
- the actor then attempts clockwise strafe;
- if clockwise strafe is blocked, it attempts anticlockwise strafe;
- if anticlockwise strafe is blocked, it attempts `FleeTarget`;
- if fleeing is blocked, it attempts `SeekTarget`.

Demonstrate:

- a linear action plan can express keep-distance/orbit behavior without separate branch/check steps;
- fallthrough at ideal distance is useful rather than an error;
- strafe/flee/seek fallback makes the actor more robust near obstacles and boundaries.

Primitive dependencies:

- `MaintainChebyshevDistanceTwo`;
- `StrafeClockwise`;
- `StrafeAnticlockwise`;
- `FleeTarget`;
- `SeekTarget`.

Testing notes:

- The main `beta-kiting-orbiter` scenario demonstrates the composed personality in a readable room with three actors: a too-close orbiter uses `MaintainChebyshevDistanceTwo` to back away toward range 2, an at-range orbiter falls through and strafes clockwise, and a top-edge at-range orbiter has clockwise strafe blocked by bounds then succeeds with anticlockwise strafe.
- A companion scenario in the same content file, `beta-kiting-orbiter-fallback-lane`, uses a one-row lane to demonstrate the deeper fallback path: one actor falls through distance maintenance and both strafe directions before `FleeTarget` succeeds; another falls through distance maintenance, both strafes, and blocked fleeing before `SeekTarget` succeeds.
- Fixture coverage validates both scenarios and confirms the intended chain order and final positions.
- Debug recordings were generated for both scenarios: `C:\Users\Scramble\AppData\Local\Temp\opencode\GameGameGame-Sprint16-beta-kiting-orbiter\beta-kiting-orbiter.gif` and `C:\Users\Scramble\AppData\Local\Temp\opencode\GameGameGame-Sprint16-beta-kiting-orbiter-fallback-lane\beta-kiting-orbiter-fallback-lane.gif`.

Recording expectation:

- Complete. The main recording shows distance maintenance plus clockwise/anticlockwise orbit behavior; the companion fallback-lane recording shows flee and seek fallback behavior when both strafing directions are blocked.

## Working cadence

For each showcase:

1. Confirm the needed primitive exists in `Engine-Editor-Capabilities.md`.
2. If missing, send the core owner a concise primitive request including content need, final naming candidates, state reads/writes, distance metric, direction tie-breaks, fallback behavior, turn consumption, trace/report expectations, validation/API requirements, and intended showcase.
3. After the primitive lands, author the content fixture using normal entity templates, presentations, scenario roots, and canonical behavior chains.
4. Validate the content document.
5. Run the scenario headlessly and inspect trace/final-state output.
6. Record the persisted scenario with `record-scenario` and save PNG/GIF artifacts to the agreed output location.
7. Hand off the GIF path and concise behavior notes.
8. Record unsupported behavior, naming friction, target-selection issues, report friction, or recorder limitations in `Beta-Capability-Gap-Log.md`.

## Definition of done

- Final Action Step names for `FleeTarget`, `MaintainChebyshevDistanceTwo`, `StrafeClockwise`, and `StrafeAnticlockwise` equivalents are coordinated with the core owner and documented.
- The selected Gate 3 primitives have Core runtime, descriptor/YAML, validation/default handling, editor service, agent/headless API, compact trace/report support, tests, and `Engine-Editor-Capabilities.md` updates.
- The five planned Distance Movement showcase scenarios exist under `src/GameGameGame.Content/Beta/DistanceMovement/` unless explicitly deferred with rationale.
- Each authored showcase validates as normal content.
- Each authored showcase has fixture coverage or equivalent headless validation.
- Each authored showcase has a generated debug GIF from the Sprint 15 recording workflow.
- The `beta-kiting-orbiter` showcase demonstrates useful linear composition of distance maintenance plus strafe fallback.
- Any unsupported behavior or reporting friction is recorded in `Beta-Capability-Gap-Log.md`.

## Non-goals

- Do not add generic action-plan branch/check programming for distance evaluation.
- Do not add both `SeekTarget` and `StepTowardTarget`; use existing `SeekTarget`.
- Do not add both `FleeTarget` and `StepAwayFromTarget`/`MoveAwayFromTarget`; choose one final away-from-target Action Step name.
- Do not implement configurable distance-band parameters unless the core owner identifies them as lower risk than the hard-coded Sprint 16 distance-two primitive.
- Do not implement full pathfinding around obstacles.
- Do not implement richer patterned pursuit such as rook/bishop/knight movement in Sprint 16.
- Do not implement `Give`/`Take`, template spawning, reactions, traps, combat systems, factions, relationships, scheduler/speed, or future frontend work in this sprint.
- Do not modify Core, Editor, or Console code from the content-owner role; coordinate primitive implementation and recording issues with the core owner.
