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

### Highlights communicate the action to be taken

**Player meaning:** A highlighted grid cell communicates what confirming/selecting that cell will do, not merely that the cell is interesting.

**Visual language:** Distinct highlight roles are used for distinct expected outcomes. Current examples use SadConsole `CellDecorator` overlays so entity glyph identity remains visible.

**Current implementation:** `PlayHighlightState`, `CellHighlightKind`, `CellHighlightPresentation`, `TilesetRoles.MoveHighlight`, and `TilesetRoles.EntityHighlight`.

**Rules:**

- Do not use one generic highlight for different player outcomes once those outcomes can be distinguished.
- Preserve entity glyph identity under highlights; highlights are presentation overlays, not entity glyph replacements.
- If a future cell has exactly one concrete action available, an action-specific highlight/icon may be introduced only after that action's UX semantics are designed.

**Open questions:** Action-specific highlights for Pickup, Drop, Push, Transfer, Enter/Exit, and future actions are deferred until those workflows are designed one-by-one.

### Focus communicates the input owner

**Player meaning:** The focused UI region owns input; grid aim/movement should not change behind a focused panel or picker.

**Visual language:** Focus should be visible on the focused panel, row, picker, or overlay. Current inspection-action focus uses a selected action row marker/color in the inspection overlay.

**Current implementation:** `PlayFocusMode`, `PlayInspectionInputController`, `PlayInspectionController`, and inspection action row rendering.

**Rules:**

- Only the focused Play component responds to input.
- Grid movement input is locked while an inspection/player panel or future picker has focus.
- `Esc`/Left-style return paths should visibly and semantically return focus to the previous owner.

**Open questions:** Player inventory overlay focus language and future picker focus language are deferred.

## Current visual treatments

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

### Inspection action rows

**Player meaning:** The inspection panel lists known action candidates or unavailable action facts for the inspected entity.

**Visual language:** Selectable rows use action-row color/spacing; unavailable rows are dimmed/marked. Focused action rows show the panel-owned selected-row marker.

**Current implementation:** `EntityInspectionActionRow`, `PlayActionCandidate`, `EntityInspectionPanelRenderer.DrawActions(...)`.

**Rules:**

- Rows may carry structured action candidates, but selecting a row does not imply global prompt/auto-submit semantics yet.
- Unavailable rows should communicate that Core/Content exposed a candidate/fact that cannot currently execute, not that the UI invented a local rule.

**Open questions:** Prompt, picker, auto-submit, and action-specific confirmation semantics are deferred until after the player inventory/self-inspection overlay exists and then should be designed one action at a time.
