# SadConsole Tile Scaling Spike Findings

Status: Archived spike findings. Use as implementation guidance, not as a production plan.

Date: 2026-07-17

## Context

This spike explored moving the SadConsole editor/gallery away from IBM/text-terminal assumptions and toward a tile-native presentation model using the Candii `8x8` tileset. It also tested integer display scaling and early placement rules for scaled UI.

The spike was intentionally exploratory. Some code paths proved valuable as reusable patterns; others are scaffolding that should be reimplemented more deliberately when the tile/layout work is promoted on main.

## What worked

1. **Tileset profiles are the right abstraction.**
   - SadConsole glyph numbers must be treated as tilesheet indexes, not semantic CP437/Unicode values.
   - Candii role mappings for blank, panel borders, and color samples were successfully calibrated through inspected tilesheet contact sheets.
   - A profile-aware text/box helper is valuable because spaces, unsupported glyphs, and future tileset-specific mappings need explicit handling.

2. **Integer tile scaling is a good player-facing model.**
   - Native tile size multiplied by an integer scale produced crisp pixel-art presentation.
   - Candii `8x8` at `2x` (`16x16` physical pixels per UI tile) was comfortable for editor/gallery UI.
   - Candii at `3x` looked viable, but only with more deliberate component placement and layout rules.

3. **Scale should be role-aware, not permanently global.**
   - Future screens may want a dense play surface at `1x` while HUD, panels, and popups use `2x` or larger.
   - Useful roles identified by the spike: text/header/footer, panels/cards, overlays/popups, and play surface.

4. **Modal placement needs invariants independent of scale.**
   - Popup/modal overlays should generally be centered in the physical viewport regardless of UI scale.
   - Authored component size and placement policy should be separate concerns.

5. **The component gallery remains valuable as executable validation.**
   - The gallery made scale comfort and layout breakage immediately visible.
   - Future reusable SadConsole layout/rendering patterns should continue to be demonstrated there.

## What did not work as a durable pattern

1. **Fixed `SadConsoleRect` coordinates are not enough.**
   - Scaling fixed IBM-era component coordinates made the UI larger, but did not create a responsive layout system.
   - At larger scales, the gallery exposed overlap and awkward placement.
   - Production work should introduce layout regions, anchors, alignment, margins, and placement policies before broad migration.

2. **The renderer accumulated too many responsibilities.**
   - Spike code combined display settings, scale selection, child-layer creation, overlay placement, fallback rendering, and component-specific rendering in one renderer.
   - Production code should split display metrics, layout resolution, layer/surface creation, and component rendering responsibilities.

3. **Per-line child layers are useful for discovery but not an ideal final renderer.**
   - Rendering header/footer text as many short-lived child consoles made scale experiments easy.
   - A production implementation should prefer persistent region/layer surfaces when practical.

4. **`RenderRows()` remains migration debt.**
   - Converting legacy components through styled row strings was useful for rapid migration.
   - New tile-native components should expose structured view data and let renderers choose glyphs, roles, colors, clipping, and decorators.

5. **“All overlays are centered” is too broad as a final rule.**
   - Centering is right for modal dialogs.
   - Future overlay categories may include tooltips, anchored context menus, docked debugger panels, and command palettes with different placement rules.

## Decisions to preserve

- Use square/multiple-of-8 tilesets as the SadConsole graphics baseline.
- Use tileset profiles for semantic glyph roles.
- Use integer scaling by default for pixel-art clarity.
- Treat Candii `2x` as the current comfortable editor/gallery design target.
- Support role-aware scales so play surfaces, panels, text, and popups can differ.
- Center modal popups in the physical viewport regardless of UI scale.
- Keep layout in logical/component terms and transform to physical pixels at the rendering layer.

## Recommended production sequence

1. Reintroduce the safe tileset-profile and profile-aware rendering helpers on main.
2. Add display settings with role-aware integer scales as frontend-owned presentation state.
3. Add a layout model before broad scaled-UI migration:
   - screen regions;
   - anchors/alignment;
   - margins/safe area;
   - placement policy per component/overlay category;
   - scale role per region/component.
4. Demonstrate the layout model in the component gallery at `1x`, `2x`, and `3x`.
5. Migrate editor components from fixed coordinates and `RenderRows()` incrementally.
6. Keep play-mode conversion separate so the play surface can intentionally choose its own scale and layout density.

## Implementation guidance

Do not promote the spike implementation wholesale. Promote the findings and selected small helpers only after they fit a cleaner architecture.

Likely production responsibilities:

- `TilesetProfile`: tileset metadata and semantic glyph roles.
- `TilesetTextRenderer`: text-to-tile and role-to-tile rendering helper.
- `DisplaySettings`: selected tileset, role scales, window mode, viewport policy.
- `DisplayMetrics`: native tile size, scaled tile sizes, physical viewport, margins/offsets.
- `LayoutResolver`: regions, anchors, modal centering, docking, and responsive placement.
- `LayerFactory`: creates/reuses SadConsole child surfaces with the correct font, scale, and pixel positioning.
- `ComponentTileRenderer`: renders structured component view data to tile surfaces.
