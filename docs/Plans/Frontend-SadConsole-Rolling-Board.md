---
id: plan.frontend-sadconsole-rolling-board
title: Frontend SadConsole Rolling Board
kind: rolling-board
status: active
owners: [frontend-owner]
audience: [frontend-owner, core-owner, content-editor]
lane: frontend-sadconsole
related:
  - source.frontend-ux-invariants
  - source.frontend-ux-standards
  - source.frontend-ux-decisions
  - source.frontend-game-text
  - plan.core-rolling-board
  - plan.content-rolling-board
---

# Frontend SadConsole Rolling Board

Status: Active rolling board for the new `GameGameGame.Frontend.SadConsole` workstream.

Purpose: Track small, continuously updated user stories without creating a dedicated sprint document for every frontend slice. Move items from **Next** or **Later** into **Now** as capacity opens. Update acceptance notes as items complete.

## Board policy

- **Now**: the current implementation focus. Keep this short enough that active work is obvious.
- **Next**: likely upcoming work with enough clarity to start soon.
- **Later**: known follow-ups, dependency-bound work, or larger decisions.
- Prefer user stories plus short implementation plans over task dumps.
- When an item completes, add a dated completion note and either remove it from the active board or move it to a completed/history section if the context remains useful.
- Preserve frontend boundaries: UI may present/focus/select; Core/Content remain the source for action legality, materialization, durable content semantics, and runtime facts.

## Now

No active frontend-owned implementation item is currently in progress. Pull from **Next** or **Later** when the next frontend slice starts.

## Next

No active frontend-owned item queued after the completed topology/POV foundation. Pull from **Later** or the Core/Content boards when dependencies are satisfied.

## Later

### Topology/POV presentation polish

**User story:** As a player, topology/POV spaces and seams are easier to understand visually after the functional shared topology foundation.

**Owners:** Frontend, consuming Core/Content projection seams.

**Plan:**

- Improve dimmed-context, seam, overlap, and diagnostics presentation without adding frontend-owned movement or visibility semantics.
- Keep line-of-sight/audibility claims out of presentation until Core/Content provide those facts.
- Coordinate with Content when a scenario experiment needs specific visualization affordances.

**Done when:**

- The presentation makes authored seams/debug topology easier to inspect while preserving Core/Content source identity.

### Introduce an action workflow descriptor seam

**User story:** As a frontend developer, I can change one action's player-facing workflow without adding another modal branch or switch case in every Play component.

**Owners:** Frontend + Core. Tracked primarily on `plan.core-rolling-board` until the Core descriptor seam exists.

**Priority dependency:** Old-frontend quarantine is complete; do when action UX churn becomes the selected bottleneck.

**Plan:**

- Consume Core-owned workflow/action-choice descriptors for target source, follow-up prompts, submit shape, and action-specific affordance facts.
- Keep focus, layout, animation, and component presentation frontend-owned.
- Migrate existing Move/Pickup/Drop/Enter/Exit/Transfer/Push workflows incrementally.

**Done when:**

- Existing action workflows still work through the new descriptor seam.
- Individual action workflow changes no longer require unrelated switch edits across Play components.

### Create a dedicated user-facing Log component

**User story:** As a player, I can review important action outcomes through a readable log component that complements animation rather than duplicating debug traces.

**Owners:** Frontend, with Core and Content collaboration required.

**Plan:**

- Decide which outcomes deserve user-facing log rows. Minimum rule: any action that resolves to an animation should also produce a log row.
- Consume structured Core/Content outcome/log projections rather than parsing trace text.
- Define component layout, clipping, focus, and relationship to inspection panels.
- Defer true perception/line-of-sight/audibility claims until Core/Content provide those facts.

**Done when:**

- A reusable Play log component exists.
- Animated action outcomes have corresponding user-facing log rows.
- Debug traces remain available separately from player-facing logs.

## Completed

### 2026-08-23: Make new frontend Play mode topology/POV based

**User story:** As a player, I see the spaces available from my controlled actor's point of view, including reachable spaces across layer/topology boundaries, rather than a single current room plane.

**Owners:** Frontend, consuming Core/Content projection seams.

**Plan:**

- Replace the single-plane `PlayGridViewModel` assumption with a topology/POV visible-cell model.
- Consume shared `PointOfViewService`, `ActorPovPlayProjectionService`, and/or `TopologyVisibilityProjectionService` facts rather than deriving ancestry or reachability in the frontend.
- Preserve source plane/node/layout identity so merged layers, inventory-boundary links, and later overlapping spaces can be presented without losing provenance.
- Keep line-of-sight/audibility claims out of presentation until Core/Content provide those facts.

**Done when:**

- The new frontend can render a controlled actor POV set instead of only one selected plane.
- Cells reached across topology seams retain enough identity for inspection, highlighting, and action selection.
- Existing one-room scenarios still render through the new model.

**Completion notes:** Functional topology/POV rendering is complete over shared Core/Content topology visibility projection facts. Display coordinates are normalized for rendering/animation while source/layout identity is preserved for inspection, movement highlights, and action selection. Overlapping visible cells prefer in-POV presentation over dimmed context, and movement/inspection previews resolve through Core movement edges rather than frontend display-coordinate guesses. Frontend-owner reviewed and approved the Core/frontend boundary split.

### 2026-08-16: Add player self-inspection/inventory panel

**User story:** As a player, I can focus my always-visible inventory/status panel with `I` without changing grid aim.

**Completion notes:** Added an always-visible bottom-left player panel overlay using the same child-console/mixed-Candii overlay family as entity inspection. `I` toggles player-panel focus/unfocus; `Esc`/Left also returns to grid. While focused, the player panel consumes input so movement/grid input does not fall through. The panel projects the controlled actor's portrait, status/action rows, and inventory cells from the actor's registered inventory plane.

**Deferred:** Player-panel action execution and prompt semantics remain deferred. Current player-panel action messages are inert until action-specific workflows are designed.

**Verification:** `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-player-panel3 -m:1 --filter "PlayInput|PlayInspectionState|EntityInspectionPanel|PlayMovement|PlayGrid|TilesetProfile|PlayActionCandidate|MovementPreview"` passed 53 tests.

### 2026-08-16: Add action candidate model for focused Play panels

**User story:** As a developer, I want inspection/player panel action rows to carry structured action candidates so future user-facing workflows can be implemented one at a time without re-reading Core affordances in each panel.

**Boundary decision:** Implemented as infrastructure only. Prompt-layer focus, picker overlays, auto-submit semantics, candidate ordering semantics, and action-confirmation UX are intentionally deferred until after the player inventory/self-inspection overlay exists and then one action at a time.

**Completion notes:** Added `PlayActionCandidate`, prompt/follow-up data models, and inert outcome classification. Inspection action rows carry candidates from the controller-owned request instead of being text-only rows.

**Verification:** Included in checkpoint `ae7e85f Add play action session groundwork` and reverified with the player-panel targeted test run above.

### 2026-08-16: Introduce new frontend Play action session controller

**User story:** As a frontend developer, I want one new-frontend controller to own Core action-choice submission, initiative stepping, history, and refreshed action facts so Play UI panels can execute actions without duplicating Core orchestration.

**Boundary decision:** Implemented as a session/execution controller only. It does not import old frontend prompt, menu, candidate ordering, shortcut, or panel-layout assumptions.

**Completion notes:** Added `PlayActionSessionController` to centralize Core `ActionChoiceService`, `SimulationHistorySession`, `InitiativePlayerChoiceStepper`, runtime action-plan synchronization, target refresh, current `ActionChoiceRequest`, action log, and movement submission. `PlayMovementController` now delegates to this controller. Inspection action rows now read the controller-owned current request instead of constructing their own independent request.

**Verification:** `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-session-controller4 -m:1 --filter "PlayMovement|PlayInspectionState|PlayInput|MovementPreview|EntityInspectionPanel|PlayGrid|LayeredPlaySurface|TilesetProfile"` passed 58 tests.

### 2026-08-15: Audit old frontend action-choice UX and quarantine candidates

**User story:** As a developer, I want to know which old frontend action/prompt patterns are still valuable before wiring new action execution.

**Port into new frontend planning:**

- `src/GameGameGame.SadConsole/Ui/Screens/GameplaySessionController.cs`: port the responsibility shape into a new frontend Play action session controller. Useful responsibilities are Core `ActionChoiceService` submission, `SimulationHistorySession`, `InitiativePlayerChoiceStepper`, runtime action-plan synchronization, target refresh, active controlled actor handoff, undo/history hooks, and action-log refresh.
- `src/GameGameGame.SadConsole/Ui/Screens/PlayModeIntentController.cs`: adapt the candidate/prompt-stack concept into new-frontend model types. Useful ideas are intent seeds, valid/complete candidates, auto-submit for a single complete candidate, prompt layers for incomplete choices, direction shortcuts, focus coordinates, cancel/back, and refine-on-select.
- `src/GameGameGame.SadConsole/Ui/Screens/ConsumerPlayModeScreen.cs` candidate builders: use as behavior references for Pickup, Drop, Enter, Exit, Transfer, and Push candidate construction from Core `ActionChoiceRequest` affordances.

**Keep as reference/later:**

- `ActionChoicePromptController`: comprehensive edge-case reference for pickup/drop/enter/exit/transfer mode transitions and cancel/back behavior, but too rigid/modal to port directly before the new candidate model exists.
- `GameplayMockScreen` selection-coordinate fields and prompt highlights: useful later for visual picker overlays and prompt highlight rules, but not part of the first action-execution slice.
- Old inventory/drop/transfer prompt panels: useful UX references for picker contents, but should be rebuilt with current overlay-console/panel patterns.

**Leave as legacy/avoid direct port:**

- Old `IUiComponent`, `SadConsoleRect`, component IDs, debug panel structure, and mock-screen layout/rendering stack.
- The mock/debug screen's diagnostic-heavy presentation as a gameplay UI model.
- Expanding direct compatibility movement as the gameplay path; the new frontend should submit through Core action-choice/session orchestration where available.

**Resulting board decisions:** Added **Introduce new frontend Play action session controller** and **Add action candidate/prompt model for focused Play panels** to **Next**. Existing Pickup/Drop items remain but now depend on the shared controller/candidate path instead of standalone wiring. The session controller is explicitly limited to Core orchestration/submission; action-selection UX remains a new-frontend design, with old prompt/candidate code used only as reference material.

### 2026-08-15: Harden Play focus/input routing

**User story:** As a player, when I focus an inspection/player panel, only that panel responds to input so movement aim and inspected-entity selection do not change behind it.

**Completion notes:** Added an inspection-focus input controller that maps only panel commands while consuming all other keys, including movement-key releases. `PlayModeConsole` now returns immediately when inspection actions have focus, preventing fallthrough to grid movement/cursor handling. `Esc`/Left returns focus to the grid.

**Verification:** `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-focus-lockout2 -m:1 --filter "PlayInput|MovementPreview|EntityInspectionPanel|TilesetProfile"` passed 38 tests.
