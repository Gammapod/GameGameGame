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
