# SadConsole Frontend Roadmap

Status: Active roadmap for paving the way toward SadConsole as the canonical debug/editor browser frontend.

Read when:

- selecting short-term frontend, UI/UX, debug-browser, or frontend-contract work;
- deciding what should be owned by Core/Content/Headless versus a frontend application;
- handing SadConsole work to the frontend-owner agent;
- deciding whether a requested workflow belongs in SadConsole or in shared frontend-neutral tooling.

Related source of truth:

- `docs/Archived/SadConsole-Spike-Findings.md` records the completed prototype findings that motivated this roadmap.
- `docs/Archived/Unified-Simulation-History-Log-Rollback.md` records the completed shared history/log/rollback sprint.
- `docs/Archived/SadConsole-Prototype-Assessment.md` records UX and technical-debt findings from the spike.
- `docs/Archived/SadConsole-Entity-Panel-Prototype.md` records the prototype's implementation sequence and findings log.
- `docs/Archived/SadConsole-UI-Pattern-Discovery-Sprint.md` records the completed componentized editor rebuild/parity sprint.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records implemented engine/editor/frontend-facing capability support.
- `docs/Source of Truth/invariants.md` records stable Core behavior contracts and test traces.

## Direction

SadConsole remains the preferred canonical debug/editor browser direction, with final frontend-engine selection deferred. The componentized SadConsole editor is now the default launch path. The former user-launchable `--beta-editor` legacy shell has been removed. A small internal legacy Simulation Play stopgap still uses the old shell while the componentized Simulation screen is rebuilt; it should not receive new editor features.

The former Console frontend has been removed. Simple developer commands, scenario scanning, reports, and recording workflows should live in shared Content/Headless tooling or future explicit CLI tools rather than reviving Console-specific UI workflows. New rich UI investment should prefer SadConsole or shared frontend-neutral services.

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

Use frontend-owner ownership for SadConsole and future frontend presentation/interaction work that consumes existing contracts without changing Core semantics.

Examples:

- SadConsole scenario menu, panels, layout, collapse/focus/cursor state;
- input modes and prompt UI over existing action/choice/target contracts;
- rendering logs, breadcrumbs, metadata, grids, and contents lists;
- mouse hit-testing and visual affordances;
- packaging/distribution experiments;
- future frontend or explicit CLI-tool cleanup that does not alter engine behavior.

### Content/editor-aware work

Use content/editor-aware ownership for future debug-browser/editor features that inspect or mutate authored content through existing editor/content service APIs.

Examples:

- action-plan preview panels;
- read-only template/action-plan browsers;
- editor-backed mutation workflows once the browser is useful;
- validation and diagnostic presentation for authored content.

## Completed foundation summary

The completed shared-service foundation and unified simulation history/log/rollback sprint are archived in:

- `docs/Archived/SadConsole-Spike-Findings.md`;
- `docs/Archived/Unified-Simulation-History-Log-Rollback.md`.

Implemented support status now lives in `docs/Source of Truth/Engine-Editor-Capabilities.md`; stable behavior/test traces live in `docs/Source of Truth/invariants.md`.

Remaining active work should be grouped around the Gamma Editor MVP plan: SadConsole should prove an Editor -> Preview -> Simulation -> Return loop over shared content/editor services and shared runtime/session/action/log/panel contracts. The editor parity slice is complete enough for service-backed template/action-plan/inventory authoring; next work should focus on preview quality, Simulation handoff/return, provenance/source jumps, and capability gaps discovered by the editor sprint. Debug-browser UX, richer autonomous anchors, saved runlogs/playback, and future Action Choice semantics remain important roadmap items when they support or follow that editor loop.

Completed componentized editor parity slice:

- Default SadConsole launch opens the componentized editor UI. The former `--beta-editor` legacy editor path has been removed; legacy shell usage remains only as an internal Simulation Play stopgap until replaced by a componentized Simulation screen.
- Scenario Edit supports save/dirty/unsaved-exit, template/action-plan create/duplicate/delete, and service-backed navigation to dedicated editors.
- Entity Template Edit supports presentation, default action plan, targeting requirements/rules, inventory metadata, and inventory-grid contents/layout editing.
- Action Plan Edit supports canonical behavior-step insert/replace/delete/move through shared editor services.

Known capability gaps promoted from the parity sprint:

- Scenario root/player-start editing likely needs shared editor API support before becoming editable fields.
- Per-carried-instance initial facing/state should be owned by inventory/placement authoring, not exposed as template-level UI; this needs shared content/editor/materialization support.
- Action-step parameter/check/effect editing needs a typed frontend projection and mutation contract before Screen 4 can expose it cleanly.

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
8. Valid inspection target highlighting/cycling for Inspect mode, using visible/projection data without inventing simulation legality.

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

- SadConsole can answer common “what is this entity/template/plan doing?” questions without opening YAML.

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
4. Package SadConsole as the default feedback build; investigate browser/HTML5 feasibility only if later distribution needs re-promote it as a technology-risk item.
5. Compare SadConsole against Godot/other options only if packaging, editor-widget, mouse, or layout constraints become significant.

Exit criteria:

- A deliberate frontend-engine checkpoint can be made with evidence from a functional debug/browser frontend.

## UI/UX backlog items consolidated from current buckets

Promote or consider while planning SadConsole work:

- Breadcrumb display and containment paths.
- Improved inspection summaries.
- History playback / SadConsole-rendered visual export for shareable debug artifacts.
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
- Scenario root/player-start editing, per-instance carried state/facing, and typed action-step parameter editing as shared editor capability gaps before richer authoring UI is added.
- No-valid-target prompt suppression for current direct-control prompt modes, while preserving shared command execution as authoritative.
- SadConsole temporary-output build script or command for verifying the frontend while an interactive app window may be locking normal build outputs.
- Saved runlogs / runlog stepper backed by shared history.
- Reaction trace causality when reactions are promoted.
- Future Action Choice / `PlayerInputStep` model.
- Long-horizon diegetic action-plan UI if action plans become gameplay objects.

## Near-term selection recommendation

The next implementation sequence should be selected from `docs/Plans/Gamma-Editor-MVP-Plan.md`. Stage 6+ work above remains the broader SadConsole roadmap, but Gamma release selection should prioritize the Editor -> Preview -> Simulation -> Return loop and classify remaining items as Must Have or Could Have for Editor MVP.
