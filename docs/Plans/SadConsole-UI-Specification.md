---
id: plan.sadconsole-ui-specification
title: SadConsole UI Specification
kind: plan
subkind: frontend-ui-specification
status: active
owners: [frontend-owner]
audience: [frontend-owner]
lane: frontend-ux
truth_rank: 60
truth_domains: [frontend-presentation, frontend-planning]
read_when:
  - planning SadConsole screen layout or play-mode UI changes
  - evaluating relative layout layered regions overlays resizing or user-customizable UI placement
related:
  - source.frontend-ux-invariants
  - source.frontend-ux-standards
  - source.frontend-ux-decisions
  - plan.sadconsole-frontend-roadmap
  - plan.sadconsole-playmode-layout-pattern-sprint
  - plan.new-play-mode-mvp-sprint
---

# SadConsole UI Specification

Status: Living planning matrix for SadConsole UI layout, rendering, and interaction capabilities.

This document records desired UI mechanics, current fit against project frontend architecture, and researched SadConsole support/risks. It is intentionally a planning artifact, not a source of Core gameplay semantics. Frontend-owned layout state may control placement, layering, focus, scrolling, clipping, hit-testing, and user presentation preferences; it must not own simulation/action/content-authoring semantics.

## Research sources checked

- SadConsole home page: feature overview lists any number of consoles, multiple fonts, GUI controls/windows, mouse/keyboard, animated consoles, and instruction engine: <https://sadconsole.com/>
- ScreenObject basics: position, absolute position, parent/child hierarchy, `SortOrder`, visibility/enabled flags, update/render propagation, input/focus, and `IgnoreParentPosition`: <https://sadconsole.com/guides/screenobject-basics/>
- GameHost screen overview: `GameHost.Screen` as the root object; all other objects are children; objects can have unlimited children at any depth: <https://sadconsole.com/guides/host-screen/>
- Surfaces and consoles overview: `ScreenObject`, `ScreenSurface`, `Console`, `ControlsConsole`, `Window`, `LayeredScreenSurface`, view/buffer scrolling, pixel positioning, modal/draggable windows, and layered surfaces: <https://sadconsole.com/guides/surfaces-overview/>
- Startup configuration overview: `SetScreenSize`, `SetStartingScreen`, startup delegates, and font configuration: <https://sadconsole.com/guides/config-overview/>
- `Settings.WindowResizeOptions`: final render-pass resize modes `Stretch`, `Center`, `Scale`, `Fit`, and `None`: <https://sadconsole.com/reference/sadconsole.settings.windowresizeoptions/>
- MonoGame host `Global`: render output, graphics-device resize/reset hooks, and render scaling reset hooks: <https://sadconsole.com/reference/sadconsole.host.global/>
- `LayeredScreenSurface` / `LayeredSurface`: layer collections, full-surface layers, transparent cells, and resize propagation: <https://sadconsole.com/reference/sadconsole.layeredscreensurface/> and <https://sadconsole.com/reference/sadconsole.components.layeredsurface/>

## Current project baseline

- Play mode currently uses `GameplayMockScreen` plus `GameplayMockConsole`.
- Mock play-mode layout bounds resolve through `GameplayMockLayout.Resolve(width, height)`, which emits named layered regions for `0`, `0.1`, `0.2`, `0.3`, `0.2.1`, and `0.diagnostics` while preserving compatibility bounds on `GameplayMockFrame`.
- The first accepted layout resolver is intentionally narrow: it models the current HUD/content and current-place/inspection splits with small ratio/gap/min-size helpers rather than a broad reusable layout DSL.
- Mock play mode has hidden `F12` layout debug rows for region IDs, bounds, layers, and mouse-hover hit-test diagnostics. `F11` manually re-resolves the current logical console layout and records the latest result in the debug panel.
- Root SadConsole screens are still sized through fixed `SadConsoleScreenMetrics.ScreenWidth` / `ScreenHeight` and startup pixel size is derived from `SadConsoleDisplaySettings`; OS window resizing currently changes pixel presentation, not logical cell dimensions.
- Layering exists in project code as ad hoc child consoles and component overlays: HUD layer, component-renderer centered overlay, layout debug overlay, and component-gallery pixel-positioned preview layer.
- FED-016 already accepts child `Console` overlays for modal/popup layers as a reusable SadConsole pattern; FED-021 records the accepted mock play-mode layout resolver/debug pattern.

## Must-have planning matrix

| ID | Spec | Project architectural fit | SadConsole support / best-practice notes | Warnings / limitations / open questions | Planning status |
|---|---|---|---|---|---|
| UI-M01 | Define layered screen regions. Example: component 1 takes the full screen on bottom/layer 0; component 2 draws on layer 2 on the left 30%; component 3 draws on layer 2 in 10% of remaining area. | Strong fit. Layering, clipping, bounds, focus, and hit-testing are frontend-owned presentation state. | SadConsole's `ScreenObject` hierarchy is designed for nested screen objects. Child objects render through the root screen automatically. Children render in collection order by default; `SortOrder` can be used with `Children.Sort()` for depth control. `LayeredScreenSurface` supports stacked full-surface cell layers when independent glyph layers on one surface are needed. | `LayeredScreenSurface` layers are full-surface layers, not a complete relative-region layout system. Relative region definitions and z-layer policy should be project-owned screen-model logic, resolved into SadConsole child object positions/sizes. Need define stable project rules for layer ordering, clipping, overlap, and input priority. | Initial mock play-mode implementation complete through `GameplayMockLayout` named/layered regions; broader first-class layout engine remains future work. |
| UI-M02 | Define regions by relative screen placement, not pixels or tiles. | Strong fit if relative definitions resolve into concrete cell rectangles at render time. The durable spec should remain relative; only the renderer receives cell bounds. | SadConsole positions `ScreenObject`s relative to parents and exposes absolute positions after hierarchy resolution. Nested `ScreenObject` containers are suitable for recursive layout. | SadConsole does not appear to provide a general CSS/Flex/Grid-style relative layout manager. We should build a pure tested project resolver for percentages, ratios, anchors, gaps, min/max cells, and rounding. | Initial mock play-mode ratio split complete for HUD/content and current-place/inspection; generalized recursive resolver remains future work. |
| UI-M03 | Draw the game screen based on user screen size by measuring window pixels, dividing into 8x8 chunks, and leaving a border/margin for pixels that do not fit another chunk. | Good fit with existing square-tile baseline and current `SadConsoleDisplaySettings`; the new consumer Play shell now validates fullscreen host resizing and cell-count derivation from display pixels. | SadConsole font configuration supports custom fonts and selected font sizes. `UsePixelPositioning` exists for pixel-positioned surfaces. `Settings.WindowResizeOptions` includes `Center`, `Scale`, `Fit`, and `None`, which are relevant to final output scaling/letterboxing. MonoGame host exposes render-output and graphics-device resize/reset hooks. | Need distinguish native tile size from scaled font size: current Candii native tiles are 8x8 but default UI scale is 2x, so displayed cells are currently derived from the active scaled font size. Deferred requested semantic: when the monitor resolution is not evenly divisible by tile size, the final SadConsole tile surface should be centered within the remaining monitor pixels rather than biased to an edge. This is pixel/render-rect centering, distinct from centering gameplay content inside the drawable tile area. | Fullscreen Play shell implemented for MVP; pixel-perfect leftover-margin centering remains deferred display-metrics/render-rect work. |

## Nice-to-have planning matrix

| ID | Spec | Project architectural fit | SadConsole support / best-practice notes | Warnings / limitations / open questions | Planning status |
|---|---|---|---|---|---|
| UI-N01 | Resize dynamically based on windowed screen size, either responsive or via explicit “recalculate graphics” trigger. | Good fit as frontend-owned presentation behavior. Prefer pure layout resolver first so responsive and manual recalculation share the same path. | `Settings.WindowResizeOptions` controls final render-pass behavior. MonoGame host exposes graphics-device resize and render-output reset hooks. `LayeredScreenSurface` and `LayeredSurface` expose resize APIs that propagate to layers. | Need confirm the project's SadConsole/MonoGame version's practical window-resize event path. Automatic responsive resizing may require reconstructing root surfaces/child consoles or resizing them carefully. Manual recalculation is lower risk for first slice. | Manual `F11` logical-console re-resolve seam exists; true window-pixel-to-cell recalculation needs a display-metrics/window-resize spike. |
| UI-N02 | Let the user customize/reposition regions during gameplay. | Fit as frontend preference/presentation state. Should not be game content or simulation state unless later explicitly defined as a frontend profile format. | SadConsole supports mouse input, focus events, child object positioning, modal windows, and draggable `Window`s. These can support a layout-edit mode or draggable handles. | Need design persistence location and reset/default behavior. Need input priority and hit-testing rules for overlapping regions. User customization should layer as overrides on top of default layout definitions, not mutate canonical screen components. | Future feature after default layout and hit-testing are stable. |
| UI-N03 | Draw components on top of region definitions and reposition them during gameplay; e.g. floating popup modals. | Strong fit. Existing project renderer already has a centered overlay child console pattern; FED-016 accepts child `Console` overlays. | SadConsole `Window` provides bordered/titled draggable panels, modal display via `Show(true)`, close-on-Escape, and built-in controls. `IgnoreParentPosition` supports screen-fixed overlays. Child objects plus `SortOrder` support non-window custom overlays. | For game-styled overlays, project components may still be preferable to SadConsole `Window` controls to preserve existing theme/glyph vocabulary. Debug overlays should draw over all normal Play content when enabled. Semi-transparent debug overlays are desirable but deferred; opaque topmost debug glyphs/text are acceptable for the current Play-mode MVP. Need decide when to use SadConsole `Window` versus project `IUiComponent` rendered on child consoles. | Promote generalized overlay/floating/debug-region support into layout engine and gallery. |
| UI-N04 | Define components themselves into internal regions; e.g. component 6.5 divides horizontally into 30:70 regardless of current component dimensions. | Strong fit and should be designed into the same resolver recursively rather than reinvented per component. | SadConsole `ScreenObject` containers can nest at unlimited depth, with positions relative to parent. Parent/child layout naturally maps to component-internal regions. | Same limitation as UI-M02: SadConsole provides composition primitives, not a full relative layout language. Need project-owned recursive layout definitions and resolved region tree. | Include in resolver design from the start, even if only used after top-level migration. |
| UI-N05 | Support mouse hit-testing and click interaction over resolved screen regions and cells, including click-to-focus, click-to-inspect, and click-to-select prompt targets where equivalent keyboard actions exist. | Fit as frontend-owned input, focus, hover, hit-testing, and selection behavior. It must submit through shared runtime/editor services and must not invent legality. | SadConsole supports mouse input on screen objects, exclusive mouse routing, focus events, relative/absolute positioning, and child-object hierarchy. A resolved region tree gives the project a natural hit-test index. | Overlapping layers require deterministic topmost-hit rules. Cell hit-testing must account for render scaling, pixel margins, and any `UsePixelPositioning` surfaces. Mouse behavior should remain a convenience path over the directional/Select/Cancel model. | Pure topmost-region hit-test and live hover diagnostics complete for mock play mode; click-to-act/focus remains future work. |
| UI-N06 | Support collapsible, expandable, pinned, and focusable multi-panel layouts as presentation state over the same region system. | Fit as frontend-owned component state and layout policy. Useful for entity panel chains, editor panels, dense local activity, and inspection surfaces. | SadConsole child visibility, enabled state, and component focus hooks support hiding/collapsing and focus treatment. Existing project component focus/border patterns and panel-chain tests provide a foundation. | Collapse/pin state should not change underlying runtime/editor facts. Need min-size, overflow, and selection rules so collapsed panels remain discoverable and keyboard-operable. | Related but distinct from UI-M01/UI-M02; model as region/component state atop the resolver. |
| UI-N07 | Support alternate render styles and configurable presentation themes/layout profiles for play/debug readability, including active-actor/focus cues, facing/target decorators, larger glyph tiles, 2x2 color-block experiments, and theme variants. | Fit as frontend-owned presentation styling. Must preserve entity glyph identity and consume shared state facts. | SadConsole supports custom fonts, multiple fonts, tile glyphs, cell foreground/background, effects, animated consoles, and surface tinting. Current project has theme-owned border/color/glyph styling and Candii glyph calibration. | Avoid replacing entity identity glyphs with state markers. Visual treatments promoted beyond experiments should be recorded in Frontend UX standards/decisions and demonstrated in the component gallery. Larger/mixed tile treatments must be reconciled with the square-cell baseline and relative layout resolver. | Keep as UI-spec note for roadmap items about active-actor/facing/target visualization and configurable themes/layouts. |

## Inventory-space drawing standard candidate

The new consumer-facing Play mode MVP should introduce a reusable inventory-space view model and renderer/component. This section records the planning standard until implementation evidence is stable enough to promote details into `Frontend-UX-Standards.md`.

### MVP inventory-space model requirements

Every drawn inventory space should be representable with:

1. **Cell metrics**: cell width, cell height, and optional gap/spacing between cells. The first renderer may use a fixed profile, but geometry should not assume gapless cells forever.
2. **Viewport**: an origin and visible width/height. The MVP can render the full space, but the model should not require full-space rendering.
3. **Backdrop layer**: a repeated default tile/style per cell. Later polish may derive backdrop treatment from parent entity properties or authored/presentational patterns. Use “backdrop” for the inventory-space base layer so “background” can refer specifically to SadConsole's cell secondary color.
4. **Entity visual layers**: a required primary identity layer and an optional accent layer. The MVP may render only the primary layer, but the model should not bake in one-glyph/one-color as the only future representation.
5. **Decorator/overlay layers**: selected, focused, controlled, warning/error, and future target/hover/facing/next-action roles should be separate from identity visuals and must not replace entity glyph identity.
6. **Optional frame/border**: a space-level border/title/status treatment. Future border styles may visualize parent-space facts such as topology, aperture, enter/exit policy, danger, or diagnostics.
7. **Stable geometry**: a deterministic mapping from inventory coordinates to rendered cell bounds for future hit-testing, focus routing, and viewport work.

### MVP rendering expectations

The first accepted inventory-space renderer should draw:

- optional border/title;
- repeated backdrop cells;
- occupant primary glyphs/tiles;
- selected/focused/controlled markers;
- basic warning/error markers if available;
- footer/status text when useful.

It should remain frontend-owned presentation over shared runtime/content facts. It must not own action legality, visibility semantics, containment rules, or simulation state.

### Deferred inventory-space nice-to-haves

The following should be designed-for where cheap but deferred from the MVP implementation:

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

## Research summary: SadConsole best-fit mechanics

1. **Use `ScreenObject`/children as the primary composition model.** SadConsole is explicitly built around a root `GameHost.Screen` with unlimited nested children. This matches our desired region tree and keeps layout composition outside simulation logic.
2. **Use child ordering/`SortOrder` for coarse z-depth.** This fits layered regions and floating overlays. The project should still define a deterministic layer-to-child-order adapter.
3. **Use `ScreenObject` containers for layout groups.** `ScreenObject` renders nothing and is recommended as a grouping/positioning object, which maps well to non-rendering layout regions.
4. **Use `LayeredScreenSurface` selectively.** It is useful for multiple independent cell layers on one surface, such as background/decorator/glyph layers within a map panel. It is not a substitute for screen-region layout.
5. **Use `Window` where built-in modal/drag/control behavior is desired.** SadConsole windows support title bars, borders, dragging, modal display, Escape close, and controls. For project-styled gameplay overlays, custom child consoles/components may still be better.
6. **Use `ScreenSurface` rather than `Console` for display-only panels where possible.** SadConsole docs recommend moving to `Console` only when cursor/text input is needed. Current project often uses `Console`; a later renderer cleanup can choose lighter surfaces for passive panels.
7. **Use view/buffer support for scrolling surfaces.** `ScreenSurface` can have a larger buffer than visible view, which is relevant for scrollable logs or maps.

## Consolidated backlog/roadmap mapping

The following frontend/UI backlog items from `High-Level-Roadmap.md`, `SadConsole-Frontend-Roadmap.md`, and `Gamma-Editor-MVP-Plan.md` are now covered here so they do not need to be repeated as standalone UI backlog bullets:

- **Reusable/centralized layout geometry** maps directly to UI-M01 and UI-M02.
- **Configurable layouts** maps to UI-N02 when it means user or profile-level layout overrides.
- **Floating overlays/modals** maps to UI-N03.
- **Nested component regions and 30:70 panel splits** map to UI-N04.
- **Mouse hit-testing over panel/cell geometry** maps to UI-N05.
- **Collapsible/expandable/pinned multi-panel layouts** map to UI-N06.
- **Active-actor/focus/facing/target visualization and alternate render styles/themes** map to UI-N07.

Related frontend-owned items that remain in broader roadmaps because they are workflows or shared-service consumers rather than layout specifications:

- scenario selection/loading, scenario curation UI, and content organization;
- Editor -> Preview -> Simulation handoff/return and source jumps;
- saved runlog/playback UX and SadConsole-rendered export;
- action prompt flows, no-valid-target suppression, and target/source-first or bump-to-interact interaction models;
- player/screen messages, structured failure feedback, local/global log wording, and game-text IDs;
- editor authoring surfaces backed by shared editor services;
- packaging/distribution and final frontend-engine decision;
- frontend test harnesses, fixture builders, boundary checks, and debug-only UI state panels.

## Research summary: warnings and limitations

1. **Relative layout is not provided as a turnkey SadConsole feature.** SadConsole provides positions, parents, children, surfaces, layers, and windows. Percentage/ratio layout, min/max sizing, rounding, and nested splits should be project-owned and tested independently.
2. **Final render scaling is distinct from logical layout.** `Settings.WindowResizeOptions` can stretch/center/scale/fit output, but that does not by itself change component layout. If we want more cells when the user has a larger window, we need to recompute logical viewport dimensions and resize/rebuild surfaces. If the final tile surface does not divide evenly into monitor pixels, leftover-pixel centering should be solved at the render-rect/display-metrics layer rather than by moving gameplay components.
3. **Layered surfaces are full-surface layers.** They are useful inside a region but not a direct implementation of arbitrary overlapping regional components.
4. **Dynamic resizing may require careful host integration.** The MonoGame host exposes resize/reset hooks, but project code currently fixes root console dimensions at startup. We need a spike before promising fully responsive window resizing.
5. **Mouse-driven customization has input/focus implications.** SadConsole supports mouse routing and exclusive mouse behavior, but overlapping regions require project-defined hit-test order and focus ownership.

## Proposed implementation slices

Completed low-risk pattern sprint: `docs/Plans/SadConsole-Playmode-Layout-Pattern-Sprint.md`.

Longer implementation sequence:

1. Add a pure, test-backed relative layout resolver that accepts viewport cells and emits a resolved region tree.
2. Recreate the current play-mode HUD/current-place/inspection/action-selector layout through that resolver without changing gameplay behavior.
3. Add deterministic z-layer ordering and overlay/floating-region support; promote an example into the component gallery.
4. Add display-metrics spike for pixel-window-to-cell-viewport calculation using active tile/font size and explicit edge margin behavior.
5. Add manual “recalculate graphics/layout” trigger.
6. Reassess automatic responsive resize after manual recalculation is stable.
7. Add user layout override/profile support after default layout, overlay hit-testing, and focus routing are stable.
