# Beta Content Exploration Plan

Status: Paused / re-backlogged content-exploration plan. Beta mechanics/content expansion is not active sprint work while the SadConsole frontend roadmap is the unambiguous priority. Use this document as historical ordering and backlog context only; promote individual vignettes or gates through `docs/Plans/High-Level-Roadmap.md` when frontend feedback or a specific scenario need justifies them.

Read when:

- researching paused beta gameplay-vignette context;
- deciding whether frontend feedback should re-promote a beta content/mechanics gate;
- reviewing old content-gap rationale before updating `docs/Source of Truth/Capability-Gap-Log.md` or `docs/Plans/High-Level-Roadmap.md`.

Do not read when:

- selecting the next frontend sprint; use `docs/Plans/SadConsole-Frontend-Roadmap.md`;
- looking for the active strategic priority; use `docs/Plans/High-Level-Roadmap.md`.

Related source of truth:

- `docs/Source of Truth/Content-Authoring-Manual.md` is the source of truth for content-editor-facing authoring capabilities, workflows, limits, and gap logging.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` is the source of truth for maintainer-facing capability support tiers and layer coverage.
- `docs/Source of Truth/invariants.md` is the source of truth for stable behavior contracts and test traces when beta work changes existing behavior.
- `docs/Plans/High-Level-Roadmap.md` remains the strategic roadmap and backlog authority.
- `docs/Source of Truth/Capability-Gap-Log.md` records gaps discovered during scenario exploration.

## Beta exploration intent

Beta should produce several small authored gameplay vignettes, like pitch-deck slides, that are playable in Console, runnable headlessly, and useful for deciding which interactions are interesting enough to promote.

Sprint 17 completed the scenario/tooling decoupling needed before Gate 4 `Give`/`Take`: future beta content work should use UI-agnostic Content/Headless services rather than deepening dependencies on the legacy Avalonia editor. Headless/tooling services should remain compatible with a future integrated game/editor frontend.

This plan orders vignettes by what content tools can author now, then by explicit engine/action gates. Each phase should produce:

1. one or more authored scenarios;
2. a headless scenario report or run artifact;
3. capability-gap notes for unsupported desired behavior;
4. requests for new primitives, engine systems, content organization, or reporting only when scenario evidence justifies them.

## Content-file organization ownership

Author-facing workflow guidance belongs in `docs/Source of Truth/Content-Authoring-Manual.md`. This section only records beta-exploration file-organization planning.

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

Near-term scenario-selection ergonomics were completed in `docs/Archived/Sprint-21-Console-Scenario-Catalog.md`: existing loose YAML content documents can be discovered from a designated folder tree, a cached manifest/index can drive Console scenario selection, and content package files/import semantics remain deferred until content duplication or extensive reuse becomes a concrete bottleneck.

## Promotion and request policy

The reusable content-authoring gap workflow belongs in `docs/Source of Truth/Content-Authoring-Manual.md`. This section records the beta-exploration thresholds used by this plan.

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

### Lightweight editor-to-core-owner change-request workflow

Use this workflow for editor-discovered planned needs or recurring capability gaps that are not already selected sprint implementation work. The goal is to minimize development time, complexity, and token use while still letting the core-owner quickly assess whether a request is worth promoting.

1. **Editor identifies the need**
   - Notice a planned need, recurring authoring friction, or capability gap.
   - State the goal in content terms, not implementation terms.
   - Provide only the minimal context needed: scenario/vignette, desired behavior, current workaround/blocker, and why the request matters.

2. **Editor sends a viability request to core-owner**
   - Ask for technical viability first, not a TDD plan and not implementation details.
   - Split the request into:
     - **Necessary changes**: the smallest changes that would unblock the content goal;
     - **Nice-to-haves**: reusable, polished, or workflow-improving additions that would be helpful but are not required.

3. **Core-owner assesses viability**
   - For each Necessary and Nice-to-Have, classify technical viability and risk/complexity.
   - Do not start implementation planning until the viability decision is clear.

4. **Decision rule**
   - If any Necessary is complex or high-risk, deny the change request for now and report the blocker/risk.
   - If all Necessaries are simple, proceed to a TDD plan for the essentials.
   - If all Necessaries are simple and all Nice-to-Haves are also simple, proceed to a TDD plan for the full feature.
   - If Necessaries are simple but any Nice-to-Have is complex or high-risk, stop and explain the tradeoff to the user for a decision.

This workflow is intentionally lighter than normal selected sprint-work implementation. It should prevent agents from over-designing speculative features while still capturing enough context for useful core-owner assessment.

## Phase 0: current-tool vignettes

Status: First primitive-showcase batch completed in Sprint 12. Curated actor zoo deferred.

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
   - Deferred after Sprint 12; revisit after Direction Transform scenarios add richer actor behavior.

### Phase 0 reporting requests to watch for

- compact per-turn state diff;
- position/facing/target summary;
- inventory/containment summary;
- created/destroyed entity summary;
- plan preview plus simulation in one report;
- capability-gap section in scenario reports;
- actor-zoo/isolation report template.

## Gate 1: direction transform batch

Status: Completed in Sprint 13. See `docs/Archived/Sprint-13-Gate-1-Direction-Showcases.md` for the archived showcase plan and core-owner coordination cadence.

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

Status: Completed in Sprint 14. See `docs/Archived/Sprint-14-Gate-2-Targeting-Showcases.md` for the archived showcase plan and core-owner primitive request.

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

Status: Completed in Sprint 16 for the first distance-movement slice. See `docs/Archived/Sprint-16-Gate-3-Distance-Movement.md` for the archived showcase plan.

Implemented Sprint 16 capabilities:

- `FleeTarget`
- `MaintainChebyshevDistanceTwo`
- `StrafeClockwise`
- `StrafeAnticlockwise`

Deferred / future candidate capabilities:

- configurable `MaintainDistance` / richer distance bands;
- distance checks/evaluation;
- patterned movement variants such as cardinal-only, diagonal-only, rook-like, bishop-like, or knight-like movement.

### Gate 3 unlocked vignettes

1. **Fleeing**
   - Completed first slice in Sprint 16 with `FleeTarget`.

2. **Keep-away**
   - Completed hard-coded distance-two first slice in Sprint 16 with `MaintainChebyshevDistanceTwo`; configurable distance bands remain deferred.

3. **Kiting enemy**
   - Completed first composition in Sprint 16 through `MaintainChebyshevDistanceTwo -> StrafeClockwise -> StrafeAnticlockwise -> FleeTarget -> SeekTarget`.

4. **Pattern-constrained pursuit**
   - Chaser follows restricted movement patterns.

5. **Distance puzzle**
   - Player manipulates spacing to influence actor behavior.

## Gate 4: `Give` / `Take`

Status: First peer-transfer slice implemented as canonical Action Steps `GiveTarget` and `TakeTarget` and explored in Sprint 19 transfer showcases. Use the source-of-truth authoring/capability docs for current semantics.

Implemented first-pass support:

- `GiveTarget`;
- `TakeTarget`;
- deterministic first-carried/source selection;
- deterministic row-major destination placement;
- transfer diagnostics in behavior-chain traces.

Deferred follow-up work:

- carried-entity selection rules;
- source-inventory selection rules;
- transfer permissions/restrictions;
- barter/trade semantics;
- richer inventory report summaries.

### Gate 4 unlocked vignettes

1. **Passive chest**
   - Completed in Sprint 19 with `beta-passive-chest-transfer`.

2. **Trade vignette**
   - Entity exchanges inventory with player or another actor.
   - Deferred after Sprint 19; true barter/trade semantics remain future work, and a simple peer exchange can be authored later if it adds new evidence.

3. **Stealing actor**
   - Completed in Sprint 19 with `beta-stealing-actor`.

4. **Feeding / offering**
   - Completed in Sprint 19 with `beta-feeding-offering`.

5. **Collector-trader handoff**
   - Completed in Sprint 19 with `beta-collector-trader-handoff`: Collector picks up player, gives player to Trader, and Trader drops player.

6. **Restricted transfer gap demo**
   - Deferred; record any missing permission/denial model if desired transfer restrictions become scenario-blocking.

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

## Sprint 12 primitive brainstorm notes

Status: Possibilities and testing insights only. These are not planned features, promotion requests, or capability-gap entries unless later scenario evidence justifies them. Keep `Content-Authoring-Manual.md` as the authoring source of truth, `Engine-Editor-Capabilities.md` as the maintainer-facing support source of truth, and `High-Level-Roadmap.md` as the roadmap/backlog authority.

The first current-tool primitive showcases suggested several possible future Action Step families:

### Forced movement family

Related Sprint 12 showcase: `beta-push-showcase`.

- `ShoveFacing` / shove action: force an adjacent or blocking entity to move without the actor moving into the vacated cell.
- `PullFacing` / pull action: move the actor while dragging another entity into the actor's previous cell.
- `DragFacing` / drag action: move the actor and an adjacent entity sideways together.
- More general long-term possibility: a configurable forced-movement primitive that can move another entity according to a derived movement plan rather than only actor `Facing`.
- Example composition to test later: `MoveToward(destination)` tries a quick direct step, writes the blocking entity and intended direction on failure, `ForceMovePerpendicularToIntent(blocker)` attempts to clear the blocker sideways, `PathfindToward(destination)` falls back to routing around the obstruction, and `ChangeTarget` picks another goal if no viable path exists.
- Design insight: richer forced-movement chains may need distinct state slots for primary target/destination, blocking entity, and intended movement direction. Overloading canonical `Target` for all three would make these chains hard to express.

### Target alteration family

Related Sprint 12 showcase: `beta-destroy-showcase`.

- `DisableTarget` / kill-actor action: instead of destroying the target entity, remove or clear its action plan so it remains in the world as an inert entity.
- This could model killing, stunning, deactivating machines, or neutralizing hazards without committing to a health/damage model yet.
- Design question: decide whether this is a permanent runtime mutation, a status/state effect, or eventually a diegetic action-plan/entity interaction.

### Creation and duplication family

Related Sprint 12 showcase: `beta-create-showcase`.

- `CloneTargetFacing` / duplicate action: create a copy of the current `Target` in the actor's facing direction.
- Safer first interpretation may be template duplication, where the new entity is materialized from the target's content template rather than deep-copying all runtime state.
- Richer instance cloning would require identity, inventory, action-state, and nested-entity copy rules.
- This likely depends on the same template/presentation binding work as `CreateFacing(templateId)` / `SpawnTemplateFacing`.

### Target movement and fallback-routing family

Related Sprint 12 showcase: `beta-behavior-chain-composition`.

- `MoveTowardTarget` / `SeekTarget`: choose a neighboring step that lowers distance to the target. This matches the planned Gate 2 `SeekTarget` direction, but the brainstorm emphasizes a simple non-smart greedy movement version.
- `PathfindTowardTarget` / move around blockers: calculate a valid path to the target and move one cell along it. Prefer recalculating each turn for an initial version unless scenario scale proves cached paths are necessary.
- `ChangeTarget` / alternate-target selection: if no viable path exists to the current target or destination, find a different target. This overlaps with future target-acquisition work but frames retargeting as fallback from failed routing.
- Design insight: these steps would become most legible when failed movement reports preserve both the original goal and the blocker/intent that caused fallback.

## Negative capability-gap vignettes

It is acceptable to author or record intentionally blocked vignettes when they clarify future priorities.

Examples:

- chaser cannot chase because target acquisition and seeking are unavailable;
- chest cannot trade with barter semantics or transfer restrictions because `GiveTarget`/`TakeTarget` only provide first-pass free inventory transfer;
- trap cannot trigger because reaction slots are unavailable;
- builder cannot spawn authored templates because `CreateFacing` uses placeholder output;
- enter/exit cannot change active play space because containment transition semantics are unavailable.

Negative vignettes should not be treated as failed content. They are planning evidence.
