---
id: source.frontend-play-visual-language
title: Frontend Play Visual Language
kind: source-of-truth
subkind: frontend-play-visual-language
status: active-stub
owners: [frontend-owner]
audience: [frontend-owner, content-editor, core-owner]
lane: frontend-ux
truth_rank: 35
truth_domains: [frontend-presentation, frontend-player-semantics]
read_when:
  - adding or changing Play-mode highlights, focus markers, overlays, action rows, prompts, or actor-state indicators
  - deciding how the player should interpret a visual treatment in `GameGameGame.Frontend.SadConsole`
  - promoting a Play-mode presentation pattern from implementation into a stable rule
related:
  - source.frontend-ux-decisions
  - source.frontend-ux-standards
  - source.frontend-game-text
---
# Frontend Play Visual Language

Status: Active stub. This document is the canonical home for player-facing Play-mode visual semantics in the new `GameGameGame.Frontend.SadConsole` frontend.

Purpose: Define what visible Play-mode treatments mean to the player, and what visual language the frontend uses to communicate those meanings. This is not an implementation changelog; reasons and code-level traces belong in `docs/Source of Truth/Frontend-UX-Decisions.md`.

Read when:

- adding or changing Play-mode highlights, focus markers, overlays, action rows, prompts, or actor-state indicators;
- deciding how the player should interpret a visible treatment;
- reviewing whether a new glyph, color, decorator, animation, or overlay communicates a concept that already has a rule here.

## Documentation boundary

- **This document:** player-facing concepts and visual semantics. Example: “a highlighted cell communicates what confirming/selecting that cell will do.”
- **Frontend UX Decision Log:** why a rule or implementation exists, alternatives rejected, affected files, and verification traces.
- **Frontend Game Text:** message IDs and wording conventions for text shown to players.

## Rule template

Use this template when promoting a visual treatment into a stable rule:

```md
### Concept name

**Player meaning:** What the player should infer.

**Visual language:** Glyph/color/decorator/overlay/focus treatment.

**Current implementation:** Code/data anchors such as `CellHighlightKind`, tileset roles, overlay classes, or tests.

**Rules:** Stable constraints to preserve.

**Open questions:** Deferred choices or planned variants.
```

## General rules

### Play selection modes form a stack

**Player meaning:** Only the top/current selection mode can change. Earlier selections remain contextual but cannot mutate while a deeper choice is active.

**Visual language:** The active selection mode owns input and highlight updates. Lower stack selections may remain highlighted to show context.

**Current implementation:** `PlaySelectionStack`, `PlaySelectionFrameKind`, and `PlayModeConsole` input routing.

**Rules:**

- **Adjacent selection:** base mode. The player chooses/highlights a space adjacent to the controlled actor. Movement aim belongs here.
- **Action selection:** the player chooses an action from an inspection/player panel. Adjacent selection is suspended and the adjacent target remains locked.
- **Cell selection:** the player chooses any cell in a relevant space. Adjacent and action selection are suspended; no other selection may change.
- Once a stack has all information required to submit an action and that action succeeds, clear back to adjacent selection for the next turn.

**Open questions:** Specific stacks for Drop, Push, Transfer, Enter, and Exit are deferred until those actions are implemented.

### Highlights communicate the action to be taken

**Player meaning:** A highlighted grid cell communicates what confirming/selecting that cell will do, not merely that the cell is interesting.

**Visual language:** Distinct highlight roles are used for distinct expected outcomes. Current examples use SadConsole `CellDecorator` overlays so entity glyph identity remains visible.

**Current implementation:** `PlayHighlightState`, `CellHighlightKind`, `CellHighlightPresentation`, `TilesetRoles.MoveHighlight`, and `TilesetRoles.EntityHighlight`.

**Rules:**

- Do not use one generic highlight for different player outcomes once those outcomes can be distinguished.
- Preserve entity glyph identity under highlights; highlights are presentation overlays, not entity glyph replacements.
- If a future cell has exactly one concrete action available, an action-specific highlight/icon may be introduced only after that action's UX semantics are designed.
- When inspection action focus is active, the current action row may refine the highlighted cell's visual role. Greyed-out/unavailable action rows use no-action language.

**Open questions:** Action-specific highlights for Pickup, Drop, Push, Transfer, Enter/Exit, and future actions are deferred until those workflows are designed one-by-one.

### Focus communicates the input owner

**Player meaning:** The focused UI region owns input; grid aim/movement should not change behind a focused panel or picker.

**Visual language:** Focus should be visible on the focused panel, row, picker, or overlay. Current inspection-action and player-panel focus use selected action row marker/color in their overlays.

**Current implementation:** `PlayFocusMode`, `PlayInspectionInputController`, `PlayInspectionController`, and inspection action row rendering.

**Rules:**

- Only the focused Play component responds to input.
- Grid movement input is locked while an inspection/player panel or future picker has focus.
- `Esc`/Left-style return paths should visibly and semantically return focus to the previous owner.

**Open questions:** Future picker focus language is deferred.

## Current visual treatments

### Actor point-of-view visibility

**Player meaning:** The normal Play grid shows only cells currently inside the controlled actor's topology point of view. Cells outside that point of view are unknown/irrelevant to immediate play and should not be shown as player-facing content.

**Visual language:** In normal mode, outside-POV context cells are not drawn. `F8` is a debug-only toggle that restores the previous dim outside-POV context presentation for inspecting topology projection behavior.

**Current implementation:** `TopologyVisibilityProjectionService`, `PlayGridViewModel.FromSession(..., showOutsidePointOfViewContext)`, and `PlayModeConsole` F8 handling.

**Rules:**

- Do not present outside-POV context cells in normal player-facing Play mode.
- The dim outside-POV treatment is debug presentation only and must not imply line-of-sight/audibility semantics beyond the shared topology projection facts.
- Keeping hidden context in layout bounds is allowed so toggling debug context does not change the relative position of visible POV cells.

**Open questions:** Final player-facing visual language for sensing, memory, discovered-but-not-currently-visible cells, and richer topology awareness is deferred.

### Movement destination highlight

**Player meaning:** Confirming will attempt to move the controlled actor toward the highlighted cell/direction.

**Visual language:** Cyan move-highlight decorator.

**Current implementation:** `CellHighlightKind.MovePreview`; `CellHighlightPresentation.MovePreview(...)`; tileset role `moveHighlight`.

**Rules:**

- Shown for the current movement aim/preview.
- Cleared when movement aim is released/cancelled.
- Does not submit movement until the player confirms.

**Open questions:** None for current preview semantics.

### Entity-target inspection highlight

**Player meaning:** Confirming/focusing this occupied adjacent cell will focus inspection/actions for the entity rather than moving into the cell.

**Visual language:** Distinct entity-target decorator, currently provisional purple.

**Current implementation:** `CellHighlightKind.EntityTarget`; `CellHighlightPresentation.EntityTarget(...)`; tileset role `entityHighlight`.

**Rules:**

- Use when the current aim targets an occupied cell that is not the controlled actor.
- The highlight means “selection will inspect/focus actions,” not “movement will occur.”
- The exact glyph/color may evolve, but it must remain visually distinct from movement preview.

**Open questions:** Whether entity-target highlights should specialize by the single available action is deferred.

### No-action highlight

**Player meaning:** The selected/focused action row cannot currently be executed for the highlighted target.

**Visual language:** Distinct no-action decorator from the tileset role `noActionHighlight`.

**Current implementation:** `CellHighlightKind.NoAction`, `CellHighlightPresentation.NoAction(...)`, and `PlayActionHighlightResolver`.

**Rules:**

- Use while inspection action focus is active and the selected action row is greyed out/unavailable.
- This communicates unavailable action state, not movement blockage in general.

**Open questions:** Final no-action color/glyph treatment may evolve with action-specific highlight polish.

### Pickup highlight

**Player meaning:** The focused inspection action is Pickup for the highlighted entity.

**Visual language:** Distinct pickup decorator from the tileset role `pickupHighlight`.

**Current implementation:** `CellHighlightKind.Pickup`, `CellHighlightPresentation.Pickup(...)`, and `PlayActionHighlightResolver` for selectable Pickup candidates.

**Rules:**

- Use only when inspection action focus is active and the selected row is a selectable Pickup candidate.
- Greyed-out Pickup rows use no-action language instead.
- During Pickup destination selection, empty valid player-inventory cells use pickup language; occupied or otherwise invalid cells use no-action language.
- Current stack: Adjacent selection over entity -> Inspection action selection over Pickup -> Player inventory cell selection over empty valid cell -> submit Pickup -> return to Adjacent selection.

**Open questions:** Pickup selection currently targets player inventory cells; other destination surfaces, alternate layouts, and final text/control polish remain open.

### Inspection action rows

**Player meaning:** The inspection panel lists known action candidates or unavailable action facts for the inspected entity.

**Visual language:** Selectable rows use action-row color/spacing; unavailable rows are dimmed/marked. Focused action rows show the panel-owned selected-row marker.

**Current implementation:** `EntityInspectionActionRow`, `PlayActionCandidate`, `EntityInspectionPanelRenderer.DrawActions(...)`.

**Rules:**

- Rows may carry structured action candidates, but selecting a row does not imply global prompt/auto-submit semantics yet.
- Unavailable rows should communicate that Core/Content exposed a candidate/fact that cannot currently execute, not that the UI invented a local rule.

**Open questions:** Prompt, picker, auto-submit, and action-specific confirmation semantics are deferred until after the player inventory/self-inspection overlay exists and then should be designed one action at a time.

### Player inventory/status panel

**Player meaning:** The bottom-left player panel is the controlled actor's own status/inventory/action surface. When focused, it owns input; when unfocused, it remains visible as ambient player state.

**Visual language:** Always-visible bottom-left translucent overlay using the same panel, portrait, action-row, and mixed-Candii inventory-cell language as entity inspection.

**Current implementation:** `PlayPlayerPanelController`, `PlayModeInspectionLayout.PlayerPanelBounds`, `EntityInspectionPanelModel`, `InspectionInventoryProjector`, and `EntityInspectionOverlayConsole`.

**Rules:**

- The player panel remains visible in Play mode for now.
- `I` toggles focus between the grid and the player panel.
- While the player panel is focused, grid movement/aim input is locked.
- Inventory cells are projected from the controlled actor's registered inventory plane when available.

**Open questions:** Player-panel action execution, prompt/follow-up semantics, and action-specific inventory selection language are deferred.
