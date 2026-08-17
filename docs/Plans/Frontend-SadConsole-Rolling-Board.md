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

### Add action candidate model for focused Play panels

**User story:** As a developer, I want inspection/player panel action rows to carry structured action candidates so future user-facing workflows can be implemented one at a time without re-reading Core affordances in each panel.

**Boundary decision:** This Now item is infrastructure only. Do not implement prompt-layer focus, picker overlays, auto-submit semantics, candidate ordering semantics, or action-confirmation UX here. User-facing action semantics will be approached one-by-one after the player inventory/self-inspection overlay exists. Legacy `PlayModeIntentController` and `ConsumerPlayModeScreen` candidate builders remain references for Core affordance coverage and edge cases, not APIs or UX to import verbatim.

**Plan:**

- Design new-frontend model types for structured action candidates; use the legacy `PlayModeIntentController`/`PlayModeActionCandidate` idea only as a reference pattern. **Started:** `PlayActionCandidate`, `PlayActionPromptLayer`, `PlayActionPromptChoice`, and selection outcome models exist.
- Build candidates from the active controller's `ActionChoiceRequest` for inspection-entity rows first. **Started:** inspection-entity candidates are projected from the controller-owned request.
- Keep prompt-layer data inert until the player inventory/self-inspection overlay exists.
- Keep complete-candidate submit behavior inert until each action's user-facing flow is intentionally designed.
- Record unresolved UX decisions as deferred follow-ups rather than implementing broad prompt behavior now.

**Current implementation note:** Inspection rows now carry action candidates and no longer treat row selection as an unstructured “not wired yet” message. Prompt layers are modelled but intentionally not focused/rendered as interactive overlays. Defer prompt, picker, and auto-submit semantics until after the player inventory/self-inspection overlay is implemented.

**Done when:**

- Inspection action rows carry structured candidates instead of being text-only rows.
- Candidate resolver can classify no-selection, unavailable, ready-to-submit, and prompt-needed states without changing focus or submitting actions.
- Tests cover candidate projection and inert outcome classification.

## Next

### Add player self-inspection panel

**User story:** As a player, I can open my own inventory/status panel with `I` and focus its action list without changing grid aim.

**Plan:**

- Add a bottom-left player-panel overlay using the same child-console/transparent-overlay family as entity inspection.
- Reuse inspection panel rendering and Candii-size overlay patterns where practical.
- Add player-only projection for portrait/status/inventory/action rows.
- Filter player-panel action rows by first prompt source:
  - no-prompt or deducible actions;
  - player-inventory-first actions such as Drop or Give/Transfer ActorToTarget.
- Route `I` through Play focus/input handling.

**Done when:**

- `I` opens/focuses the player panel.
- The player panel displays the controlled actor's portrait/inventory/status/actions.
- Grid input is locked while the player panel has focus.

### Design first action prompt semantics after inventory overlay

**User story:** As a player, I can complete one specific non-move action through a deliberately designed focused flow rather than inheriting old frontend prompt assumptions.

**Plan:**

- Start only after the player inventory/self-inspection overlay exists.
- Pick one action path, likely Pickup or Drop.
- Decide its prompt/focus/submit behavior in isolation.
- Reuse inert `PlayActionCandidate`/`PlayActionPromptLayer` data where useful.
- Update focus routing and overlays only for that action's accepted flow.

**Done when:**

- One action's user-facing semantics are documented and implemented.
- The implementation does not imply global prompt behavior for unrelated actions.

## Later

### Wire Pickup action selection

**User story:** As a player, when inspecting an adjacent pickup target, I can choose Pickup and place it in my inventory without being asked for choices that are already deducible.

**Plan:**

- Select Pickup from the inspected-entity panel action list.
- Query/use the new frontend Play action session controller's current Core `ActionChoiceRequest`.
- If there is exactly one valid destination, submit directly.
- If multiple valid destinations exist, focus a player-inventory destination picker.
- Submit through the shared Core action-choice submission path.
- Redraw world and panels after result.

**Done when:**

- Pickup executes from the inspection panel.
- Destination selection only appears when needed.
- Failure/result/status text is shown through frontend text-message IDs.

### Wire Drop action selection

**User story:** As a player, I can choose Drop from the player panel, select an inventory item, and drop it into a valid adjacent destination.

**Plan:**

- Select Drop from the player panel action list.
- Focus a player-inventory source picker for occupied carried items.
- If exactly one valid adjacent/world destination is available, submit directly.
- Otherwise focus a destination picker.
- Submit through the shared Core action-choice submission path.

**Done when:**

- Drop executes from the player panel.
- Occupied inventory source selection works.
- Destination auto-submit/destination prompt behavior follows the “do not ask what can be deduced” rule.

### Decide old frontend quarantine/main-frontend policy

**User story:** As a developer, I want the new frontend to become the primary maintained frontend once it covers the old frontend's remaining useful interaction paths.

**Plan:**

- Revisit after the new frontend can execute at least one non-move canonical action path, preferably Pickup/Drop.
- Decide whether the old `src/GameGameGame.SadConsole` project becomes reference-only/quarantined.
- Identify old tests to migrate, delete, or keep only for shared-service protection.
- Update source-of-truth docs and normal workflow expectations.

**Done when:**

- Docs clearly state frontend ownership/status.
- Old frontend no longer drives new UX decisions except as archived/reference material.
- Normal build/test expectations are explicit.

## Completed

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
