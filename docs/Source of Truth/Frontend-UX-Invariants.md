# Frontend UX Invariants

Status: Source of truth for frontend-facing UX constraints and layer boundaries.

Read when:

- planning Console, SadConsole, or future frontend work;
- deciding whether behavior belongs in Core/Content/Headless/Editor services or in a frontend;
- shaping shared session, action, target, log, or panel contracts for frontend consumption.

Related documents:

- `docs/Source of Truth/Entity-Panel-UX-Spec.md` records the canonical entity-panel, breadcrumb, and log UX model.
- `docs/Source of Truth/Frontend-UX-Standards.md` records frontend UI-bible presentation standards that guide implementation but are not Core behavior invariants.
- `docs/Plans/SadConsole-Frontend-Roadmap.md` records the staged implementation roadmap.
- `docs/Plans/SadConsole-Spike-Findings.md` records prototype evidence behind these constraints.
- `docs/Source of Truth/invariants.md` records stable Core behavior contracts and test traces.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records maintainer-facing capability support.

## Stable frontend constraints

1. **Frontends do not invent simulation semantics.** Gameplay legality, action resolution, turn advancement, traces, target selection rules, containment, inventory, and materialization semantics belong in Core, Content, Headless, or Editor services.
2. **Frontend state is presentation state.** Frontends may own layout, focus, cursor, prompt mode, hover state, collapse/expand state, scrolling, selected visible panel, styling, animation, and key/mouse/controller bindings. They must not own independent simulation state or duplicate action legality rules.
3. **Player action should converge on shared action contracts.** The current direct-player-command path is a compatibility bridge. Future work should shape it toward Action Choice-style request/submission/resolution concepts rather than preserving a permanently special player-command layer.
4. **Action evaluation remains authoritative.** Target/affordance hints may help prompts and highlighting, but final action resolution must still be performed by shared Core-aware services.
5. **Logs derive from structured outcomes, not parsed display strings.** Frontends may choose wording and emphasis, but chronological logs, local logs, and inspectable log entries should be projected from structured action/turn outcomes with entity/location anchors.
6. **Scenario launch is frontend-neutral.** Scenario discovery, materialization, diagnostics, player insertion, registry/presentation lookup, and capability-gap classification should be reusable by Console, SadConsole, future shareable frontends, tests, and editor previews.
7. **Editor-like workflows consume editor/content APIs.** A future integrated game/editor frontend may present editing through rich panels, but it must not bypass canonical editor services, mutate YAML ad hoc, or introduce editor-only concepts that Core/Content cannot consume.
8. **Console is fallback/minimal tooling.** Console remains useful for CLI/debug workflows, smoke paths, scenario scanning, direct launch, recording, and simple developer interaction. Rich UI investment should prefer SadConsole or frontend-neutral services unless Console fallback polish is explicitly selected.
9. **SadConsole is the preferred debug/editor browser direction, not a final engine lock.** It is the current canonical direction for debug/browser UX work, while final engine selection remains informed by mouse interaction, browser/download packaging, animation, editor UX, and maintenance cost.

## Shared contract boundary names

The frontend roadmap uses these first shared contract boundaries:

| Boundary | Owner | Purpose |
|---|---|---|
| Playable scenario session launch | Content/Headless-aware services | Launch scenarios through a reusable result containing scenario identity, world, registry/presentation lookup, action plans, player entity, active plane/container, diagnostics, runtime failures, and capability gaps. |
| Controlled actor command / Action Choice compatibility result | Core-aware services | Execute current direct-control actions through one authoritative path and return success/failure, turn-consumption, trace, target/source/destination anchors, and resulting turn report. |
| Action target / affordance query | Core-aware services | Enumerate valid and invalid movement, pickup, drop, enter, exit, and related choices for prompts/highlighting without replacing authoritative resolution. |
| Structured action outcome / log projection | Core/Headless-aware services | Project traces and turn reports into inspectable chronological and local log rows with actor, target, context, result, and failure reason anchors. |
| Entity panel projection | Content/Headless-aware services | Provide frontend-neutral data for visible entity panels while keeping layout, scrolling, focus, and rendering frontend-owned. |

## Frontend-owned responsibilities

Frontends own:

- rendering panels, cells, breadcrumbs, logs, borders, colors, animation, and visual themes;
- panel layout, collapse/expand presentation, scrolling, clipping, screen-space geometry, and hit-testing;
- keyboard, mouse, and controller bindings;
- focus, cursor, selection, hover, and prompt state;
- menu presentation, distribution/packaging experiments, and player-facing wording derived from shared data.

## Not frontend-owned

Frontends must not become the durable owner of:

- action legality, failure policy, or failed-action turn-consumption rules;
- movement, inventory, aperture, containment, target-selection, or turn-advancement semantics;
- scenario materialization or player insertion rules;
- diagnostics classification for scenario launch;
- action/turn event facts that are needed by more than one frontend;
- editor/content mutation semantics.

## Current UX risks and decision pressure

These are known risks to evaluate before treating any frontend engine or interaction model as final:

- panel-chain readability with many panels, large grids, dense spaces, long names, or deep containment;
- keyboard focus/cursor complexity, especially when player movement and inspection focus diverge;
- mouse hit-testing, hover, and click affordance comfort in SadConsole;
- compact local logs that do not hide necessary context;
- clear facing/target indicators without grid clutter;
- browser/HTML5 or itch.io-friendly packaging versus downloadable desktop delivery;
- future editor UX comfort compared with a richer UI/game framework.

## Stage 0 debt inventory

The Stage 0 code/documentation audit found these current responsibility concentrations:

- `PlayableScenarioLauncher` in Content now owns frontend-neutral playable session launch over scenario materialization and catalog entries. `ConsoleScenarioLauncher` is a compatibility shim for fallback Console code.
- `ControlledActorCommandService` in Core now owns direct-control command execution policy for move, pickup, drop, enter, and exit. Failed direct commands record traces without advancing the turn; successful commands consume and advance the turn.
- `ControlledActorAffordanceService` in Core now exposes direct-control prompt/query hints for movement, pickup, drop, enter, and exit. Frontends may use these hints for highlighting/skipping/explanations, but `ControlledActorCommandService` remains authoritative for execution.
- `ActionOutcomeProjection` and `ActionLogProjection` in Core now expose structured log rows and local entity/plane filtering for controlled-command results. Broader autonomous turn-report outcome projection remains a follow-up.
- `EntityPanelProjectionService` in Content now exposes a first-pass frontend-neutral entity panel model combining inspection facts, breadcrumbs, action state, inventory grid, local contents, and structured local logs. This contract is intentionally expected to evolve after SadConsole consumption.
- `src/GameGameGame.Console/Program.cs` still owns menu presentation, direct/catalog launch selection, input modes, action prompting, failed-action display formatting, inspection-panel composition, breadcrumb display, local turn-order formatting, and rendering. Presentation/input can remain Console-owned.

Known first-pass panel projection limitations:

- action-plan summary is presence/type-level only, not a rich behavior-chain preview;
- structured previous-action snippets are best for controlled-command outcomes; autonomous turn-report projection is still a follow-up;
- local log inclusion currently uses entity and inventory-plane anchors and may need richer containment/affected-entity semantics;
- layout, scrolling, collapse, focus, filtering, and visual prioritization are intentionally not represented.
- `src/GameGameGame.Console/ConsolePlayerControls.cs` is a very small key-to-command/action helper. It is acceptable as fallback input mapping, but command creation/resolution should not become Console-specific.
- `src/GameGameGame.Console/ConsoleInspectionDisplayFormatter.cs` formats breadcrumbs and panel properties from shared inspection/path/turn-order data. This is useful reference behavior, but stable panel projection should move to frontend-neutral DTOs before SadConsole depends on equivalent composition.
- Only the SadConsole spike findings were carried forward to main. The old `src/GameGameGame.SadConsolePrototype` prototype source is intentionally not a current codebase artifact. Future SadConsole implementation should start fresh in `src/GameGameGame.SadConsole` after the shared contracts it needs are selected.

## Contradictions or clarification needed

No semantic contradiction was found in the core UX direction: the documents consistently say to stop adding SadConsole-only features until shared contracts are paved, keep Console fallback/minimal, and keep simulation semantics out of frontends.

Repository-state clarification:

- Several historical docs describe the completed prototype and old run commands. Those commands are archival only. Main should carry forward the findings, not the prototype project. Create a fresh `GameGameGame.SadConsole` project for future frontend work when the roadmap promotes it.
