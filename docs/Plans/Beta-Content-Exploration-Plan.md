# Beta Content Exploration Plan

Status: Active content-exploration plan.

Read when:

- designing or ordering beta gameplay vignettes;
- deciding whether a content gap should request a new Action Step, engine capability, or reporting feature;
- adding scenario fixtures or reorganizing content files for beta exploration.

Related source of truth:

- `docs/Source of Truth/Engine-Editor-Capabilities.md` is the single source of truth for implemented and authorable engine/editor capabilities.
- `docs/Plans/High-Level-Roadmap.md` remains the strategic roadmap and backlog authority.

## Beta exploration intent

Beta should produce several small authored gameplay vignettes, like pitch-deck slides, that are playable in Console, runnable headlessly, and useful for deciding which interactions are interesting enough to promote.

This plan orders vignettes by what content tools can author now, then by explicit engine/action gates. Each phase should produce:

1. one or more authored scenarios;
2. a headless scenario report or run artifact;
3. capability-gap notes for unsupported desired behavior;
4. requests for new primitives, engine systems, content organization, or reporting only when scenario evidence justifies them.

## Content-file organization ownership

The content editor/content-authoring role owns the organization of beta content files. As content grows, prefer introducing clear content package structure over letting all fixtures accumulate in a single YAML file.

Initial guidance:

- keep existing alpha/prototype content stable unless a selected fixture explicitly updates it;
- create beta exploration content as separate content documents when practical;
- group related vignettes together by exploration phase or scenario family;
- preserve normal content definitions: scenarios should reference entity templates, presentations, and action plans rather than inventing a separate scenario scripting language;
- add structure when repeated fixture work makes file navigation, scenario selection, or validation noisy.

Potential future structure, to be introduced only when useful:

- `src/GameGameGame.Content/Beta/CurrentTools/*.yaml`
- `src/GameGameGame.Content/Beta/DirectionTransforms/*.yaml`
- `src/GameGameGame.Content/Beta/Targeting/*.yaml`
- `src/GameGameGame.Content/Beta/Transfer/*.yaml`
- `src/GameGameGame.Content/Beta/Spawning/*.yaml`
- `src/GameGameGame.Content/Beta/Reactions/*.yaml`

## Promotion and request policy

Use a capability-gap log during every phase. Promote a feature request when any of these are true:

- three or more planned vignettes need the same missing capability;
- one flagship vignette is blocked by the missing capability;
- current reports make behavior too hard to interpret reliably;
- repeated content-authoring friction shows that organization, validation, or API/reporting support is the bottleneck rather than engine behavior.

Classify each gap as one of:

- content-only authoring friction;
- reporting/tooling request;
- new Action Step or primitive using existing engine state;
- new engine capability or system;
- content/package organization issue.

Suggested gap-log fields:

- scenario/vignette;
- desired behavior;
- current workaround, if any;
- missing capability;
- scenario count or scenario value unlocked;
- requested priority;
- classification.

## Phase 0: current-tool vignettes

No new engine work should be required for this phase. Use currently supported entity templates, presentations, scenario roots, player insertion, Console launch, headless scenario runs, inventory dimensions/weight/capacity, initial `Facing`, and canonical behavior chains.

Current canonical Action Steps available for phase 0:

- `MoveFacing`
- `PickupTarget`
- `DropFacing`
- `PushFacing`
- `DestroyTarget`
- `CreateFacing`

### Phase 0 vignette priority

1. **Primitive showcase: `PushFacing`**
   - Demonstrate successful push, blocked push, and push into obstruction/out-of-bounds.
   - Use this to validate blocker movement reporting.

2. **Primitive showcase: `DestroyTarget`**
   - Demonstrate clearing/destruction on contact or after target acquisition through local blocker behavior.
   - Treat as a combat/digging placeholder, not as final combat design.

3. **Primitive showcase: `DropFacing`**
   - Demonstrate dropping carried entities into adjacent world/floor cells.
   - Capture any friction around carried-entity selection and report readability.

4. **Primitive showcase: `CreateFacing`**
   - Demonstrate placeholder creation in front of an actor.
   - Record the expected gap for template-specific creation if scenario design immediately needs authored spawned entities.

5. **Pickup/drop/weight puzzle**
   - Exercise current weight and carrying-capacity rules.
   - Compare player, container, heavy-object, light-object, and nested/capacity edge cases.
   - Use results to inform whether the inventory system should keep current semantics or move toward simpler containment rules.

6. **Simple blocker/target interaction vignette**
   - Compose existing steps into short fallback chains, such as `MoveFacing -> PickupTarget`, `MoveFacing -> PushFacing`, or `MoveFacing -> DestroyTarget`.
   - Confirm whether canonical fallthrough behavior is legible in reports.

7. **Current actor zoo**
   - Curated one-room demonstrations for single-purpose actors: walker, pusher, destroyer, creator, dropper/carrier, and collector/pickup actor.
   - Prefer explicit scenarios at first, then automate isolation previews once repeated setup patterns are clear.

### Phase 0 reporting requests to watch for

- compact per-turn state diff;
- position/facing/target summary;
- inventory/containment summary;
- created/destroyed entity summary;
- plan preview plus simulation in one report;
- capability-gap section in scenario reports;
- actor-zoo/isolation report template.

## Gate 1: direction transform batch

Request and implement before the next vignette phase:

- `ReverseFacing`
- `TurnLeft`
- `TurnRight`
- `Backstep`

Design questions for the gate:

- state reads/writes for `Facing`;
- turn-consumption defaults;
- fallback behavior when movement fails;
- blocker/`Target` writing rules for `Backstep`;
- editor service and agent/headless API support;
- compact trace/report wording.

### Gate 1 unlocked vignettes

1. **Patrol vignette**
   - Actor walks, turns at obstacles, and continues.

2. **Rotating guard / sentry**
   - Actor changes facing in place.
   - Useful groundwork for future directional sensing or interaction.

3. **Wall-bounce actor**
   - Actor moves until blocked, reverses, and moves back.

4. **Backstep puzzle**
   - Actor or player retreats while preserving facing.

5. **Expanded actor zoo**
   - Add turner, patroller, bouncer, and backstepper demonstrations.

## Gate 2: `AcquireNearestTarget` + `SeekTarget`

Request and implement after direction transforms have been explored:

- `AcquireNearestTarget`
- `SeekTarget`

Keep the first semantics small and deterministic:

- same-plane target acquisition only;
- deterministic nearest-target tie-breaks;
- clear target filter policy, initially as simple as the selected vignettes allow;
- write persistent `Target`;
- move one step toward persistent `Target`;
- report target acquisition, target loss, movement choice, and blockers.

### Gate 2 unlocked vignettes

1. **Direct chase**
   - Enemy acquires and approaches the player.

2. **Targeted destroyer**
   - Actor acquires nearest target, seeks it, and destroys on contact.

3. **Collector**
   - Actor acquires nearest item, seeks it, and picks it up.

4. **Follower**
   - Non-hostile actor follows player or another chosen target.

5. **Predator/prey prototype**
   - Establish pursuit before adding flee/keep-away behavior.

## Gate 3: target-distance / directional choice primitives

Request only after target acquisition and seeking expose concrete needs. Candidate capabilities:

- `MoveAwayFromTarget`
- `StepTowardTarget`
- `StepAwayFromTarget`
- `MaintainDistance`
- distance checks/evaluation;
- patterned movement variants such as cardinal-only, diagonal-only, rook-like, bishop-like, or knight-like movement.

### Gate 3 unlocked vignettes

1. **Fleeing**
   - Actor acquires player and moves away.

2. **Keep-away**
   - Actor attempts to remain near a chosen distance band.

3. **Kiting enemy**
   - Actor approaches when far and retreats when close.

4. **Pattern-constrained pursuit**
   - Chaser follows restricted movement patterns.

5. **Distance puzzle**
   - Player manipulates spacing to influence actor behavior.

## Gate 4: `Give` / `Take`

Request and implement when vignettes need peer inventory transfer:

- `Give`
- `Take`
- carried-entity selection rules;
- source-inventory selection rules;
- transfer diagnostics and report summaries.

### Gate 4 unlocked vignettes

1. **Passive chest**
   - Player gives to and takes from a container-like entity.

2. **Trade vignette**
   - Entity exchanges inventory with player or another actor.

3. **Stealing actor**
   - Actor takes from player/container.

4. **Feeding / offering**
   - Player gives item to entity as a precursor to future reactions/state changes.

5. **Restricted transfer gap demo**
   - Record any missing permission/denial model if desired transfer restrictions are not yet supported.

## Gate 5: template spawning

Request and implement after placeholder `CreateFacing` is insufficient:

- `CreateFacing(templateId)` or `SpawnTemplateFacing`.

### Gate 5 unlocked vignettes

1. **Spawner**
   - Actor creates authored entities instead of placeholder rocks.

2. **Projectile-like object**
   - Actor spawns an authored object in its facing direction.

3. **Trap/bomb placement**
   - Actor places an authored hazardous object for later reaction/combat experiments.

4. **Builder puzzle**
   - Actor creates authored blockers, bridges, keys, or puzzle pieces.

5. **Clone/summon prototype**
   - Actor spawns another actor template.

## Gate 6: reaction system

Defer until simpler action semantics and scenario-report needs stabilize. Candidate system pieces:

- action-plan slots beyond default/on-turn behavior;
- reaction slots;
- bump/on-enter/on-destroy/on-created semantics;
- root actor/current actor/instigator model;
- trace causality;
- recursion guards;
- relationship with scheduler/speed.

### Gate 6 unlocked vignettes

1. **Traps**
   - Entity reacts when stepped on, bumped, created, or destroyed.

2. **Doors/buttons/pressure plates**
   - Cross-entity environmental interactions.

3. **Chain reactions**
   - One entity action triggers another action.

4. **Contact combat**
   - Passive entities react to being bumped or attacked.

5. **Environmental puzzle systems**
   - Multiple reactive entities coordinate through explicit reaction semantics.

## Actor zoo approach

Use two complementary forms.

### Curated actor zoo

Curated actor-zoo scenarios are authored beta demos. Each scenario should show one actor behavior clearly in a small room. These are preferred for pitch-deck-like vignettes and manual Console play.

### Automated isolation preview

An automated actor preview may later generate a small room around an arbitrary entity template, run it for a fixed number of turns, and report behavior. This should be considered reporting/tooling support, not a replacement for curated vignettes.

Candidate preview setups:

- empty room;
- blocker ahead;
- item ahead;
- player/target nearby;
- enclosed room;
- carried item preloaded.

## Primitive showcase approach

Each Action Step should eventually have a small demonstration scenario or generated preview that answers:

- what state it reads;
- what state it writes;
- what successful behavior looks like;
- what failure/fallback behavior looks like;
- what content setup is required;
- what report output should make obvious.

Start with explicit authored showcase scenarios. Add generated previews only after the repeated setup shape is clear.

## Negative capability-gap vignettes

It is acceptable to author or record intentionally blocked vignettes when they clarify future priorities.

Examples:

- chaser cannot chase because target acquisition and seeking are unavailable;
- chest cannot trade because `Give`/`Take` are unavailable;
- trap cannot trigger because reaction slots are unavailable;
- builder cannot spawn authored templates because `CreateFacing` uses placeholder output;
- enter/exit cannot change active play space because containment transition semantics are unavailable.

Negative vignettes should not be treated as failed content. They are planning evidence.
