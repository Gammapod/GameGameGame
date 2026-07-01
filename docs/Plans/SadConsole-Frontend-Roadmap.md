# SadConsole Frontend Roadmap

Status: Active roadmap for paving the way toward SadConsole as the canonical debug/editor browser frontend.

Read when:

- selecting short-term frontend, UI/UX, debug-browser, or frontend-contract work;
- deciding what should be owned by Core/Content/Headless versus a frontend application;
- handing SadConsole/Console work to the frontend-owner agent;
- deciding whether Console work is fallback tooling or canonical frontend work.

Related source of truth:

- `docs/Plans/SadConsole-Spike-Findings.md` records the completed prototype findings that motivated this roadmap.
- `docs/Plans/SadConsole-Prototype-Assessment.md` records UX and technical-debt findings from the spike.
- `docs/Plans/SadConsole-Entity-Panel-Prototype.md` records the prototype's implementation sequence and findings log.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records implemented engine/editor/frontend-facing capability support.
- `docs/Source of Truth/invariants.md` records stable Core behavior contracts and test traces.

## Direction

SadConsole is now the preferred canonical debug/editor browser direction, with final frontend-engine selection deferred. The near-term goal is a functional, information-dense frontend over shared engine/content/headless contracts, not a polished final game UI.

Console remains valuable, but its long-term role should shift toward fallback CLI tooling, smoke/debug paths, scenario scanning, scenario recording, and simple developer commands. New rich UI investment should prefer SadConsole or shared frontend-neutral services unless a task explicitly targets Console fallback behavior.

## Design principles

- Frontends must not invent simulation semantics or frontend-only gameplay rules.
- Player actions should converge toward normal action-plan/action-step semantics. The long-term model is an input-requiring Action Step that pauses simulation and resumes through a selected Action Choice, not a permanently special player command path.
- Logs should be generated from structured action outcomes, not parsed display strings. Target shape: successful entries such as `{entity} {verb}ed {target/recipient}` and failed entries such as `{entity} tried to {verb} {target}, but {failure reason}`.
- The canonical debug UI should be composed of entity panels. Each panel should eventually show identity/metadata, action plans/slots, inventory/grid, and a contents list in initiative order with previous-turn trace/log context.
- SadConsole-specific state may include layout, focus, cursor, prompt mode, collapse state, hover state, and styling. It must not include independent simulation state or duplicate action legality rules.
- Spike-branch frontend UX docs may be used as research input, but only documents merged or recreated on main are canonical.

## Ownership split

### Core-owner / Core-aware work

Use Core-aware ownership for work that changes or defines simulation semantics, action resolution, turn advancement, structured outcomes, target legality, or invariants.

Examples:

- controlled actor command / future Action Choice contracts;
- action target and affordance queries;
- structured action/turn outcome models;
- eliminating duplicated turn/action resolution paths;
- `PlayerInputStep` semantics when promoted;
- tests that assert turn consumption, trace, target, movement, inventory, and containment behavior.

### Frontend-owner work

Use frontend-owner ownership for SadConsole/Console presentation and interaction work that consumes existing contracts without changing Core semantics.

Examples:

- SadConsole scenario menu, panels, layout, collapse/focus/cursor state;
- input modes and prompt UI over existing action/choice/target contracts;
- rendering logs, breadcrumbs, metadata, grids, and contents lists;
- mouse hit-testing and visual affordances;
- packaging/distribution experiments;
- Console fallback cleanup that does not alter engine behavior.

### Content/editor-aware work

Use content/editor-aware ownership for future debug-browser/editor features that inspect or mutate authored content through existing editor/content service APIs.

Examples:

- action-plan preview panels;
- read-only template/action-plan browsers;
- editor-backed mutation workflows once the browser is useful;
- validation and diagnostic presentation for authored content.

## Roadmap order

### Stage 0: Documentation and debt inventory

Owner: Core-aware planner, with frontend-owner review once available.

Goal: make the contracts and handoff boundaries explicit before adding more SadConsole features.

Scope:

1. Recreate canonical frontend UX docs on main, using spike-branch docs only as reference:
   - `docs/Source of Truth/Frontend-UX-Invariants.md`;
   - `docs/Source of Truth/Entity-Panel-UX-Spec.md`.
2. Record that Console is becoming fallback CLI/debug tooling while SadConsole is the canonical debug/editor browser direction.
3. Audit existing Console/SadConsole prototype code for duplicated app/session/action/rendering responsibilities.
4. Define the first shared contract names and boundaries before implementation:
   - playable scenario session launch;
   - controlled actor command / Action Choice compatibility result;
   - action target/affordance query;
   - structured action outcome/log projection;
   - entity panel projection.

Exit criteria:

- Broken references to frontend UX source-of-truth docs are resolved.
- The frontend-owner handoff boundary is documented.
- No new SadConsole feature work is started until Stage 1 contracts are selected.

### Stage 1: Shared session launch extraction

Owner: Core/content/headless-aware implementation.

Goal: remove Console ownership of playable-session launch so Console and SadConsole can launch scenarios through the same frontend-neutral contract.

Scope:

1. Extract `ConsoleScenarioLauncher` concepts into a shared Content/Headless-facing service or application-contract type.
2. Return a frontend-neutral playable session result with:
   - scenario ID/name;
   - world;
   - registry/presentation lookup;
   - action plans;
   - player entity ID;
   - active plane/container;
   - validation diagnostics;
   - runtime failures;
   - capability gaps.
3. Migrate Console to use the shared launcher.
4. Keep catalog/manifest discovery shared and reusable by SadConsole.

Exit criteria:

- Console launches scenarios through the shared service.
- SadConsole can depend on Content/Headless/Core contracts without referencing Console.

### Stage 2: Controlled action and future Action Choice contract

Owner: Core-aware implementation.

Goal: centralize current direct player command execution while shaping the API toward the long-term non-special player model.

Scope:

1. Add a controlled actor command service for current direct-control compatibility:
   - move;
   - pickup;
   - drop;
   - enter;
   - exit.
2. Return a structured command/choice resolution result:
   - actor;
   - command/action kind;
   - target/recipient/source/destination anchors;
   - success/failure;
   - failure reason/detail;
   - consumed turn;
   - advanced turn;
   - trace;
   - resulting turn report.
3. Make failed-action turn advancement behavior explicit and tested.
4. Name and shape the result so it can evolve into future `ActionChoiceRequest`, `ActionChoiceSubmission`, and `ActionChoiceResolution` concepts.
5. Migrate Console player controls to use this service instead of locally evaluating/executing actions.

Exit criteria:

- Console no longer owns action legality, failed-action execution policy, or direct turn-submission semantics.
- SadConsole can submit player actions through one shared API.
- The contract does not permanently encode player action as special; it is a compatibility bridge toward Action Choices.

### Stage 3: Action target / affordance queries

Owner: Core-aware implementation; frontend-owner can consume after completion.

Goal: let frontends highlight and constrain valid action choices without duplicating Core rules.

Scope:

1. Add query support for current direct-control actions:
   - valid movement directions;
   - pickup source candidates;
   - pickup inventory destinations;
   - carried drop sources;
   - drop destinations;
   - enter targets;
   - exit directions.
2. Include useful invalid/blocking reason data where practical.
3. Keep final action resolution authoritative even when target hints exist.
4. Add tests covering representative movement, inventory, aperture, occupancy, enter, and exit cases.

Exit criteria:

- SadConsole action prompts can use shared affordance data for highlighting/skipping/explanations.
- Console can optionally use the same queries for better prompts, but rich UI remains SadConsole-owned.

### Stage 4: Structured action outcome and log projection

Owner: Core/headless-aware implementation, with frontend-owner feedback on projection shape.

Goal: make global and local logs inspectable without parsing trace labels.

Scope:

1. Introduce structured action outcome records over existing traces/turn reports.
2. Support compact sentence rendering:
   - success: `{entity} {verb}ed {target/recipient}`;
   - failure: `{entity} tried to {verb} {target}, but {failure reason}`.
3. Preserve the full trace for debug expansion.
4. Provide anchors to actor, target/recipient/source/destination, containing panel/space, and affected entities where known.
5. Start local log projection for entity panels using outcome anchors and containment/space context.
6. Reduce or replace brittle trace-label parsing in `TurnActionSummaryFormatter` where practical.

Exit criteria:

- SadConsole can render a full chronological log and local panel-relevant logs from shared data.
- Frontend display strings are derived from structured outcome fields, not from ad-hoc trace parsing.

### Stage 5: Entity panel projection contract

Owner: Content/headless-aware implementation with frontend-owner review.

Goal: give SadConsole a stable frontend-neutral model for information-dense entity panels.

Scope:

1. Consolidate existing panel inputs:
   - `EntityInspectionPanel`;
   - `EntityContainmentPathService`;
   - `LocalTurnOrderReport`;
   - action state and target slots;
   - default action-plan/preview summaries where available;
   - structured previous-action outcomes.
2. Define an `EntityPanelProjection` or equivalent frontend-neutral DTO containing:
   - identity, glyph/color, ID, name;
   - location and breadcrumb path;
   - metadata such as bulk, aperture, inventory dimensions;
   - action state such as facing/target slots;
   - optional action-plan/default-plan summary;
   - inventory grid;
   - contents list in initiative order;
   - previous action/log snippet for each content entity.
3. Keep layout, scrolling, collapse, focus, and rendering frontend-owned.

Exit criteria:

- SadConsole entity panels can be rebuilt from one projection call per visible entity/panel.
- Console may keep simple formatting, but SadConsole is no longer forced to manually compose many services for each panel.

### Stage 6: SadConsole canonical debug browser shell

Owner: frontend-owner.

Goal: reintroduce SadConsole as the active canonical debug/browser frontend over the shared contracts.

Scope:

1. Create or refresh the SadConsole project on main as frontend-owned code.
2. Add scenario menu using shared catalog/session launch.
3. Preserve direct launch for developer testing.
4. Render entity panel chains from entity panel projections and containment paths.
5. Implement keyboard-first player-centric Play mode.
6. Implement Inspect mode and action prompt modes using shared affordance queries.
7. Submit actions only through the shared controlled command / choice-compatible service.

Exit criteria:

- SadConsole can launch, play, inspect, pickup/drop, enter/exit, and show panel chains without owning Core simulation semantics.
- Console remains available as fallback, but SadConsole is the preferred interactive debug browser.

### Stage 7: SadConsole information-density pass

Owner: frontend-owner, with Core-aware support only if missing projection data is discovered.

Goal: make SadConsole clearly more useful than Console for scenario understanding.

Scope:

1. Global chronological log panel.
2. Local per-panel logs under or inside entity panels.
3. Contents list in initiative order with previous action/trace summary.
4. Valid-target highlighting and blocked-action explanations.
5. Facing, target, active-actor, selected-entity, and focus visualization.
6. Collapsible/expandable panel cards.
7. Centralized panel layout geometry for later mouse hit-testing.

Exit criteria:

- SadConsole is the default recommended frontend for debugging authored scenarios.
- Frontend-owner can adjust layout/focus/log presentation without changing Core.

### Stage 8: Debug/editor browser foundations

Owner: frontend-owner for UI; content/editor-aware owner for editor-service integration.

Goal: start evolving SadConsole from play/debug frontend into read-only debug/editor browser.

Scope:

1. Browse scenario/catalog metadata.
2. Browse loaded entity templates, runtime entities, action plans, and references.
3. Show action-plan/default-plan summaries in entity panels.
4. Add action-plan preview panels through existing editor/content/API services.
5. Add validation diagnostics panels.
6. Keep mutation workflows read-only/deferred until browsing proves useful.

Exit criteria:

- SadConsole can answer common “what is this entity/template/plan doing?” questions without opening YAML or Avalonia.

### Stage 9: Long-term Action Choice / PlayerInputStep promotion

Owner: Core-aware implementation first, frontend-owner consumption second.

Goal: replace special direct player controls with authored, non-special player input semantics.

Scope:

1. Design `PlayerInputStep` or equivalent input-requiring Action Step.
2. Define simulation pause/resume semantics.
3. Define `ActionChoiceRequest`, available choices, target requirements, and choice submission.
4. Resolve submitted choices through normal action-step semantics.
5. Migrate SadConsole controls from direct-control compatibility commands to Action Choice requests where possible.
6. Preserve trace, outcome, log, and turn-consumption invariants.

Exit criteria:

- A player-controlled entity is designated by authored behavior, not by a permanent frontend special case.
- Frontends present available choices from the engine/authored plan instead of hardcoding the player command set.

### Stage 10: Mouse, packaging, and frontend-engine decision

Owner: frontend-owner.

Goal: improve usability and reassess whether SadConsole remains the right canonical frontend engine.

Scope:

1. Mouse hit-testing over centralized panel/cell geometry.
2. Click to inspect, select action targets, expand/collapse panels, and focus panels.
3. Packaging/distribution pass for downloadable builds.
4. Investigate browser/HTML5 feasibility as a technology-risk item, not a blocker for debug-browser usefulness.
5. Compare SadConsole against Godot/other options only if packaging, editor-widget, mouse, or layout constraints become significant.

Exit criteria:

- A deliberate frontend-engine checkpoint can be made with evidence from a functional debug/browser frontend.

## UI/UX backlog items consolidated from current buckets

Promote or consider while planning SadConsole work:

- Breadcrumb display and containment paths.
- Improved inspection summaries.
- Debug scenario recorder and visual state debugging.
- Compact world/state summaries.
- Scenario report templates and saved runlogs.
- Plan preview plus simulation/report workflows.
- Primitive showcase and actor-zoo reports.
- Actor isolation previews.
- Per-initiative frames and active-actor display.
- Player/screen messages and structured failure feedback.
- Containment/inventory summaries and selection rules.
- Runtime-created entity presentation/template binding for renderability.
- Scenario curation, grouping, metadata, and content organization.
- Entity/location/container indexes if frontend browsing or large scenarios create performance pressure.
- Behavior/action-plan templates, usage display, and future editor/browser workflows.
- Entity panel chain UX, collapsible panels, keyboard mode model, target highlighting, mouse layer, facing/target visualization, reusable layout geometry, runlog stepper, and integrated editor affordances.
- Reaction trace causality when reactions are promoted.
- Future Action Choice / `PlayerInputStep` model.
- Long-horizon diegetic action-plan UI if action plans become gameplay objects.

## Near-term selection recommendation

The next implementation sequence should be:

1. Stage 0 documentation/debt inventory.
2. Stage 1 shared session launch extraction.
3. Stage 2 controlled action / Action Choice-compatible command result.
4. Stage 3 action target / affordance queries.
5. Stage 4 structured outcome/log projection.
6. Stage 5 entity panel projection.
7. Stage 6 SadConsole canonical debug browser shell, handed to frontend-owner.

This order prioritizes debt cleanup and Core-only/shared-contract work before handing UI implementation to frontend-owner.
