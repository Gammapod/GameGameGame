---
id: plan.new-play-mode-mvp-sprint
title: New Play Mode MVP Sprint Plan
kind: plan
subkind: sprint-plan
status: completed
owners: [frontend-owner]
audience: [frontend-owner, core-owner]
lane: frontend-ux
truth_rank: 55
truth_domains: [planning-priority, frontend-presentation]
read_when:
  - implementing the new consumer-facing SadConsole Play mode skeleton
  - planning reusable inventory-space rendering components
  - separating player-facing Play from debug Simulation workflows
related:
  - source.frontend-ux-invariants
  - source.frontend-ux-standards
  - source.frontend-ux-decisions
  - plan.sadconsole-ui-specification
  - plan.sadconsole-frontend-roadmap
---

# New Play Mode MVP Sprint Plan

Status: Completed focused sprint plan for the first consumer-facing SadConsole Play mode skeleton.

## Sprint goal

Add a new consumer-facing Play mode route that launches from the scenario menu and renders the controlled actor's current inventory space using a reusable layered inventory-space component.

This sprint is intentionally a skeleton. It should create the durable route, component boundaries, and inventory drawing standard before adding broad gameplay UX. The first version only needs to show the player's current space.

## Non-goals

- Do not replace the existing editor/debug workflow in this sprint.
- Do not add new simulation, action legality, materialization, or content-authoring semantics in SadConsole.
- Do not implement a full action selector, settings screen, controller support, mouse interaction, lighting, raycasting, animation, or arbitrary inventory viewport scrolling yet.
- Do not build a separate SadConsole project yet; start as an isolated componentized mode inside `src/GameGameGame.SadConsole`.

## MVP outcome

At the end of the sprint:

1. Game start still opens the existing scenario menu.
2. Selecting a scenario shows `Play`, `Debug`, and `Edit` options.
3. `Play` launches the new consumer-facing Play mode skeleton.
4. `Debug` launches the existing debug Simulation/play path that used to be labeled `Play`.
5. `Edit` continues to launch the existing editor path.
6. The new Play mode launches the selected scenario through existing shared scenario/session services.
7. The new Play mode renders the controlled actor's current inventory space using a reusable inventory-space view model and renderer/component.
8. The new Play mode attempts fullscreen, calculates logical draw size from display pixels, reserves a one-tile glyph-181 border buffer, and confines gameplay UI to the inner drawable area.

Completion note:

- The route split is implemented: `Play` launches the new consumer Play mode, `Debug` launches the previous Simulation/play debug path, and `Edit` keeps the editor route.
- The new Play mode launches scenarios through `PlayableScenarioLauncher`, resolves the controlled actor's current containing space through shared projection services, and renders only the centered bare inventory grid in normal mode.
- The Play shell switches to fullscreen through SadConsole host APIs, resizes the SadConsole output surface with the host, calculates cell dimensions from active scaled tile size, and reserves the outer one-tile border buffer for presentation chrome only.
- `F12` toggles debug presentation: border color changes to red, row/column labels are drawn around the centered grid, and bottom diagnostics show controls, display metrics, scenario/status/current-space facts, and launch/runtime diagnostics.
- The inventory-space view model/component is accepted as stable for MVP reuse and demonstrated in the component gallery.

## Milestone 1: Route split — Play / Debug / Edit

Goal: introduce the new Play route without disrupting existing debug/editor flows.

Work:

- Rename the current scenario `Play` option to `Debug`.
- Add a new `Play` option before or above `Debug`.
- Route `Debug` to the existing Simulation/debug play path.
- Route `Play` to a new play-mode screen/shell.
- Preserve existing scenario menu behavior and context return behavior.

Acceptance criteria:

- Scenario menu still appears on startup.
- Selecting a scenario exposes `Play`, `Debug`, and `Edit`.
- `Debug` behaves like the old `Play` behavior.
- `Edit` behaves unchanged.
- `Play` enters a new placeholder play-mode screen.

## Milestone 2: New Play mode screen skeleton

Goal: create the isolated mode that can grow into the final player-facing frontend.

Work:

- Add a new componentized play-mode screen model.
- Launch the selected scenario using existing frontend-neutral launch/session services.
- Resolve the controlled/player entity through existing session data.
- Resolve the current containing inventory space / plane for the controlled entity.
- Add simple screen regions:
  - root play screen;
  - current-space component;
  - footer/context controls;
  - optional diagnostics/status text area.
- Add Cancel/Escape behavior to return to the previous menu/context.

Acceptance criteria:

- New Play mode can be entered from `Play`.
- It does not use the legacy debug play renderer as its primary rendering surface.
- It does not invent simulation/materialization semantics.
- It can identify and display the controlled actor's current space.
- Escape/Cancel exits predictably.

## Milestone 3: Inventory-space view model

Goal: define the durable frontend-owned data shape for drawing inventory spaces.

MVP view-model requirements:

1. **Cell metrics**
   - cell width;
   - cell height;
   - cell gap/spacing.
2. **Viewport**
   - origin cell;
   - visible width/height;
   - initially full-space viewport.
3. **Backdrop layer**
   - repeated default tile/style per cell.
4. **Entity primary visual layer**
   - identity glyph/tile;
   - foreground/background/style;
   - occupant position.
5. **Entity optional accent layer**
   - represented in the model;
   - allowed to be unused by the first renderer.
6. **Decorator/overlay layer model**
   - selected;
   - focused;
   - controlled;
   - warning/error;
   - future target/hover/facing roles.
7. **Optional border/frame**
   - visible/invisible;
   - title;
   - border style.
8. **Stable geometry**
   - mapping from inventory cell coordinates to rendered bounds.

Acceptance criteria:

- Inventory-space data is represented through a renderer-neutral view model.
- The model distinguishes backdrop, entity visuals, and decorators.
- The model supports cell gap and viewport even if initial values are simple.
- Accent layer and future visual modifiers can be represented without being fully rendered yet.
- The model remains presentation state and does not own action legality or simulation facts.

## Milestone 4: Inventory-space renderer/component

Goal: render the controlled actor's current space using the new standard component.

MVP rendering behavior:

- Render optional border/title.
- Render repeated backdrop cells.
- Render occupant primary glyphs/tiles.
- Render selected/focused/controlled markers.
- Render basic warning/error marker when present.
- Render footer/status text when needed.

Deferred rendering behavior:

- true two-layer sprites;
- dynamic lighting;
- raycast lines;
- mouse hover;
- animation;
- arbitrary backdrop patterns;
- scrolling/partial viewport interaction.

Acceptance criteria:

- The controlled actor's current space is visible.
- Entity identity glyphs are preserved.
- The controlled actor is visually distinguishable without replacing its glyph.
- Selection/focus/decorator rendering does not overwrite entity identity.
- Cell gap and cell size are applied by layout/geometry code.
- The component can be reused outside the play screen.

## Milestone 5: Component gallery example

Goal: make the inventory-space standard discoverable and reusable.

Work:

- Add a component-gallery example that does not require launching a scenario.
- Demonstrate:
  - small inventory space;
  - repeated backdrop;
  - several occupants;
  - selected cell;
  - controlled entity marker;
  - optional border/title;
  - one warning/decorator example if cheap.

Acceptance criteria:

- Future frontend work has a live accepted reference.
- The gallery example demonstrates the canonical layering order.
- The example is simple enough to copy from when building future space/inventory views.

## Milestone 6: Tests and documentation

Goal: pin MVP behavior and record deferred UX nice-to-haves.

Suggested tests:

- Scenario menu exposes `Play`, `Debug`, and `Edit`.
- `Debug` routes to the old play/debug path.
- `Play` routes to the new play-mode screen.
- New Play mode resolves the controlled actor's current space.
- Inventory layout computes stable bounds with cell size/gap.
- Viewport maps visible cells correctly.
- Entity glyph identity is preserved when decorators are present.
- Selected/focused/controlled decorators are represented separately from entity visuals.

Documentation updates:

- Record Play vs Debug distinction in the frontend UX decision log after implementation direction is accepted.
- Keep inventory-space drawing requirements and deferred nice-to-haves in `docs/Plans/SadConsole-UI-Specification.md` until a stable source-of-truth promotion is warranted.
- Add or update component-gallery notes when the reusable inventory-space component is accepted.

## Recommended implementation order

1. Record this sprint plan and update planning references.
2. Split scenario menu route labels and actions into `Play`, `Debug`, and `Edit`.
3. Add the new placeholder Play mode screen.
4. Add inventory-space view model and layout geometry.
5. Add inventory-space renderer/component.
6. Wire the renderer to the controlled actor's current space.
7. Add component-gallery example.
8. Add focused tests and polish the MVP route.

## Deferred nice-to-haves

These are intentionally deferred from the MVP but should remain visible in the UI specification/backlog:

- parent-property-driven backdrop tile selection;
- arbitrary per-cell backdrop patterns;
- true two-layer entity sprites;
- palette/accent rendering where each entity visual can draw primary and accent layers;
- per-cell and per-entity scaling, including smaller entities centered inside larger cells;
- partial viewport modes such as a focused entity plus its adjacent cells;
- dynamic dim/recolor/tint for lighting or visibility;
- raycast lines and vision-blocking overlays;
- mouse hover and cell/entity hit-testing;
- animation/blinking/pulsing decorators;
- richer topology, enter/exit policy, aperture, or warning visualization through space borders.
- pixel-perfect centering of the final SadConsole tile surface within leftover monitor pixels when the display resolution is not evenly divisible by the active scaled tile size;
- semi-transparent debug overlays; current debug text/glyphs are intentionally opaque and drawn topmost.
