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

### Harden Play focus/input routing

**User story:** As a player, when I focus an inspection/player panel, only that panel responds to input so movement aim and inspected-entity selection do not change behind it.

**Plan:**

- Introduce an explicit `PlayFocusTarget`/focus routing model for Play mode.
- Route `PlayModeConsole.ProcessKeyboard` by current focus target.
- Ensure grid input never runs while panel/picker focus is active.
- Preserve clear return paths such as `Esc`/Left returning focus to the grid.
- Add tests that panel focus blocks movement aim updates and inspected-entity changes.

**Done when:**

- Inspection action focus blocks movement aim/cursor updates.
- Returning to grid focus restores movement input.
- Tests prove no fallthrough input between focused components.

## Next

### Audit old frontend action-choice UX and quarantine candidates

**User story:** As a developer, I want to know which old frontend action/prompt patterns are still valuable before wiring new action execution.

**Plan:**

- Review old frontend action/input files, especially:
  - `src/GameGameGame.SadConsole/Ui/Screens/GameplaySessionController.cs`
  - `src/GameGameGame.SadConsole/Ui/Screens/PlayModeIntentController.cs`
  - `src/GameGameGame.SadConsole/Ui/Screens/GameplayMockScreen.cs`
  - `src/GameGameGame.SadConsole/Ui/Screens/ConsumerPlayModeScreen.cs`
- Identify patterns to port now, keep as later reference, or discard.
- Record a transfer/quarantine list in this board or a short linked note.

**Done when:**

- We have a documented list of useful old-frontend patterns and obsolete areas.
- Follow-up implementation items cite the patterns they intend to port or explicitly avoid.

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

## Later

### Wire Pickup action selection

**User story:** As a player, when inspecting an adjacent pickup target, I can choose Pickup and place it in my inventory without being asked for choices that are already deducible.

**Plan:**

- Select Pickup from the inspected-entity panel action list.
- Query/use Core `ActionChoiceService` facts for the controlled actor.
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
