---
id: plan.sadconsole-inventory-space-zoom-sprint
title: SadConsole Inventory Space Zoom Sprint Plan
kind: sprint-plan
status: archived
truth_rank: 50
truth_domains: [planning-priority, frontend-presentation]
owners: [frontend-owner]
audience: [frontend-owner, core-owner]
read_when:
  - implementing mixed-size inventory-space rendering in SadConsole Play mode
  - changing inventory-space glyph scaling, child surfaces, facing decorators, or relationship-tier presentation
related:
  - source.frontend-ux-invariants
  - source.frontend-ux-standards
  - source.frontend-ux-decisions
  - plan.actor-pov-inventory-chain-play-layout
  - plan.sadconsole-frontend-roadmap
---

# SadConsole Inventory Space Zoom Sprint Plan

Status: Archived completed focused frontend sprint plan. The sprint proved and implemented mixed-scale inventory-space rendering, shared pixel presentation geometry, connector/tooltip/layer/performance mitigations, and SadConsole facing decorators for Consumer Play mode. Accepted reusable patterns were promoted to `docs/Source of Truth/Frontend-UX-Standards.md` and `docs/Source of Truth/Frontend-UX-Decisions.md`.

## Goal

Prove and, if viable, implement mixed-size inventory-space rendering for the consumer SadConsole Play mode so inventory spaces communicate their relationship to the current controlled actor through consistent visual scale.

This sprint establishes the first canonical vocabulary and implementation seam for drawing inventory spaces at different pixel sizes while keeping inventory, containment, action legality, facing facts, and simulation behavior owned by shared Core/Content services.

## Vocabulary for this sprint

- **Space Zoom**: the visual pixel size of one inventory coordinate/cell.
- **Content Zoom / Content Scale**: the visual size of an occupant inside a space cell. This is intentionally deferred except where existing one-glyph centered rendering naturally persists.
- **Relationship Tier**: the frontend presentation reason a space gets a particular Space Zoom.
- **Display Profile**: the frontend-owned bundle of Space Zoom, cell gap, render options, decorator density, and renderer choice.
- **Micro space**: a 4x4-pixel summary rendering that does not use Candii glyph identity and instead uses colored square/marker sprites.

Initial Space Zooms:

| Space Zoom | Pixel size | Renderer expectation |
|---|---:|---|
| `Huge32` | 32x32 | Candii 8x8 glyphs at 4x font size |
| `Large24` | 24x24 | Candii 8x8 glyphs at 3x font size |
| `Normal16` | 16x16 | Candii 8x8 glyphs at 2x font size/current baseline |
| `Small8` | 8x8 | Candii 8x8 glyphs at 1x font size |
| `Micro4` | 4x4 | non-Candii colored square/sprite renderer |

Initial Relationship Tier mapping:

| Relationship Tier | Space Zoom | Cell gap |
|---|---:|---:|
| Current location/current place | `Huge32` | 0px |
| Player/controlled actor inventory | `Large24` | 1px |
| Immediate parent inventory | `Normal16` | 0px |
| Grandparent inventory | `Small8` | 0px |
| Great-grandparent and beyond | `Micro4` | 0px |

## Non-goals

- Do not add Core simulation size semantics, entity physical-size rules, visibility rules, containment rules, action legality, or turn semantics.
- Do not make Content or Core depend on SadConsole pixel sizes.
- Do not replace entity identity glyphs with state glyphs. Facing must be a decorator/overlay treatment.
- Do not finalize art direction for facing sprites. Placeholder/canonical temporary glyphs are acceptable if they preserve identity.
- Do not solve arbitrary per-entity Content Zoom inside large cells beyond keeping the architecture open for it.
- Do not introduce a broad reusable layout DSL.
- Do not revive former Console frontend workflows.

## Current baseline

- SadConsole currently loads Candii as an 8x8 custom font and defaults to 2x UI scale, yielding 16x16 root cells.
- `InventorySpaceViewModel` already separates backdrop, entity visuals, decorators, frame, viewport, and cell metrics.
- `InventorySpaceComponent` and `SadConsoleComponentRenderer.DrawInventorySpaceComponent(...)` currently render inventory spaces directly into the root SadConsole console cell grid.
- Direct root-grid rendering can represent multi-root-cell areas, but cannot honestly represent 24x24 or 8x8 cells when the root console is 16x16.
- SadConsole documentation and existing component-gallery code support the likely viable path: child `Console`/`ScreenSurface` objects can use a different `FontSize` and `UsePixelPositioning = true`.
- The component gallery already contains a child-layer precedent in `ComponentGalleryConsole.RenderCandiiPreview()`.

## Viability checkpoint

Before broad Play-mode changes, implement or spike the smallest useful mixed-scaling proof in the component gallery or an equivalent isolated renderer seam.

Acceptance notes:

- Render the same tiny inventory-space sample at 32x32, 24x24, 16x16, 8x8, and 4x4.
- Use SadConsole child surfaces/consoles with different Candii `FontSize` values for 32/24/16/8.
- Use pixel positioning for child surfaces so they align inside root-console layout bounds.
- Render the 4x4 micro case through a non-Candii colored-square/sprite path or a deliberately isolated placeholder that proves the required sizing without pretending it is Candii glyph rendering.
- Prove 24x24 cells with a 1px gap between cells.
- Prove that component bounds and/or hit-test geometry can map from root-cell/pixel position back to inventory coordinates.
- If child surface scaling or 4x4 rendering proves too costly, stop and record friction before implementing broader Play-mode changes.

## Revised requirements after Play-mode spike

The first Play-mode application proved mixed scaling is visible in the real frontend, but it also exposed missing system-level requirements. Treat the first direct Play-mode child-surface application as a spike checkpoint, not an accepted final pattern.

Before mixed scaling is considered accepted in Play mode, the implementation must provide:

1. **One authoritative presentation geometry model.**
   - Every drawn inventory space must expose exact screen/pixel bounds for the whole space, each inventory cell, each entity visual, and decorator/anchor positions.
   - Rendering, connector endpoints, tooltip hit-testing, hover diagnostics, and future mouse selection must consume this same geometry rather than independently reconstructing coordinates.
2. **Connectors attach to visual anchors.**
   - A connector should connect a source space/cell/entity anchor to a target space/entity/region anchor through the geometry model.
   - Connector code should not assume one root SadConsole cell per inventory coordinate once a display profile exists.
3. **Tooltips and hover use the same hit regions as rendering.**
   - Mouse hit-testing must overlap the actually drawn entity/cell visual at 32/24/16/8/4 pixel scales.
   - Tooltip facts remain projected/frontend-neutral; hit-testing and delay state remain frontend presentation state.
4. **Layer policy is explicit and enforced.**
   - Gameplay inventory spaces are base gameplay layer.
   - Connectors are above gameplay spaces but below active UI overlays.
   - HUD/panels, action selectors, prompt overlays, tooltip overlays, and debug overlays must always be visible above gameplay inventory spaces.
   - Child surfaces or draw calls must not bypass this layer policy.
5. **Redraw performance is acceptable.**
   - Drawing a screen after the engine returns the next state should be near-instant from the player's perspective.
   - The renderer must avoid recreating large numbers of SadConsole child objects every input frame.
   - Preferred mitigations are reusable surfaces, one surface per inventory space when practical, batched draw calls, or cached per-component renderer state keyed by component id/profile.

## Must Have scope

### 1. Introduce frontend-owned zoom/profile models

Add narrow SadConsole frontend models for Space Zoom, Relationship Tier, and Display Profile.

Candidate names:

```csharp
internal enum InventorySpaceZoom { Micro4, Small8, Normal16, Large24, Huge32 }
internal enum InventorySpaceRelationshipTier { CurrentLocation, PlayerInventory, ImmediateParent, Grandparent, GreatGrandparentOrBeyond }
internal sealed record InventorySpaceDisplayProfile(...);
```

Acceptance notes:

- Keep these as presentation models in `src/GameGameGame.SadConsole`.
- Do not add gameplay meaning to zoom tiers.
- Provide tests for the initial relationship-tier mapping.
- Preserve or bridge existing `InventorySpaceCellMetrics` for current pure view-model geometry as needed.

### 2. Add a mixed-scale inventory-space renderer seam

Create a renderer path that can draw inventory-space components in true pixel-sized cells using SadConsole child surfaces for Candii zoom levels.

Acceptance notes:

- 32/24/16/8 spaces use the Candii font at 4x/3x/2x/1x respectively.
- Child surfaces should be created, reused, moved, hidden, and removed deterministically so stale surfaces do not remain after redraw.
- The renderer should remain driven by `InventorySpaceViewModel`; it must not query or mutate simulation state.
- The existing root-cell renderer may remain for ordinary UI/debug components if that reduces churn.
- If the chosen pattern becomes reusable, add/update a component gallery example.
- For Play mode, do not accept a renderer that recreates one child `Console` per visible cell on every redraw; that is acceptable only as an isolated gallery/probe technique.

### 2A. Add shared presentation geometry before final Play-mode application

Create a pure frontend-owned geometry model for inventory-space presentation.

Candidate shape:

```csharp
internal sealed record InventorySpacePresentationGeometry(
    string ComponentId,
    InventorySpaceDisplayProfile Profile,
    SadConsoleRect RootCellBounds,
    PixelRect SpacePixelBounds,
    ...);
```

Acceptance notes:

- The model should resolve from an `InventorySpaceComponent`, root cell pixel size, and optional content origin.
- It should expose `CellPixelBounds(GridCoord)`, entity visual bounds, and anchor points.
- It should expose hit-test data for cells/entities.
- It should be unit-tested without launching SadConsole.
- Connectors and tooltips should be migrated to this model before mixed-scale Play mode is considered accepted.

### 3. Implement relationship-tier scaling in Actor POV Play mode

Apply the profile mapping to the consumer Play mode actor-POV layout.

Acceptance notes:

- Current location/current place inventory space is drawn at 32x32 Space Zoom.
- Immediate parent inventory space is drawn at 16x16.
- Grandparent inventory space is drawn at 8x8.
- Great-grandparent and older ancestor summaries are drawn at 4x4.
- Layout may clip/omit spaces according to existing region constraints, but diagnostics/tests should make that behavior explicit.
- Connector endpoints and hit regions should derive from zoomed cell bounds rather than raw grid coordinates.

### 4. Scale player inventory to 24x24 with a 1px gap

Apply the player/controlled-actor inventory profile to the actor inventory surface and player-inventory prompt surfaces where they are part of the current Play-mode interaction loop.

Acceptance notes:

- Controlled actor inventory is rendered at 24x24 Space Zoom.
- There is a 1-pixel gap between player inventory cells.
- Pickup/drop/transfer prompt inventory panels that represent the controlled actor should use the same player-inventory profile unless the prompt is too constrained; if constrained, record the fallback and add a follow-up.

### 5. Show Facing information per entity in the current location

Add a current-location Facing decorator layer for entities that expose facing facts through existing projections/runtime facts.

Acceptance notes:

- Entity identity glyphs remain unchanged.
- Facing appears as a decorator/overlay/adjacent marker within the current-location 32x32 cells.
- Specific final facing sprites are pending; use a clearly temporary role-based placeholder mapping if necessary.
- The treatment should be implemented through the inventory-space decorator layer, not by special-casing player rendering.
- If projection data lacks enough frontend-neutral facing information for all entities, coordinate with `core-owner`/Content rather than inventing frontend simulation facts.

## Stretch goals

### S1. Content Zoom seed

If mixed Space Zoom is straightforward, add a small presentation-only seed for per-entity Content Zoom inside a larger cell, such as an 8x8 occupant centered inside a 32x32 current-location cell.

Acceptance notes:

- Keep this strictly presentation-only.
- Do not infer physical size from template ID, glyph, or entity name.
- Prefer explicit sample/gallery data over gameplay behavior unless shared facts already exist.

### S2. Mouse hit-test diagnostics for mixed-scale spaces

Extend existing hover diagnostics so mixed-scale inventory surfaces can report hovered space/cell/entity.

Acceptance notes:

- Diagnostics only unless a later feature explicitly routes clicks through shared action/editor services.
- Hit testing should account for child surface pixel positioning and 1px gaps.

## Suggested implementation order

1. Add focused tests for Space Zoom/profile mapping and required pixel-size math.
2. Build the gallery/probe mixed-scaling example for 32/24/16/8/4.
3. Decide go/no-go based on the viability checkpoint.
4. Add the presentation geometry model and tests for cell/entity bounds, anchors, and hit regions.
5. Migrate connector endpoint resolution and tooltip hit-testing to the geometry model.
6. Replace the Play-mode mixed-scale renderer with a layer/performance-safe path that does not recreate one child console per cell per input.
7. Route current-place rendering through `CurrentLocation -> Huge32`.
8. Route parent-chain rendering through immediate/grandparent/great-grandparent profiles.
9. Route controlled actor inventory and relevant prompt inventory panels through `PlayerInventory -> Large24` with 1px gap.
10. Add Facing decorators to current-place entity visuals.
11. Update component gallery and docs with accepted reusable patterns.

## Test plan

- Add or update `InventorySpaceViewModelTests` for profile/metrics geometry if model-level geometry changes.
- Add new focused tests for relationship-tier to zoom/profile mapping.
- Add or update `SadConsoleComponentGalleryTests` for the mixed-scale gallery/probe entry.
- Add or update `ActorPovPlayScreenModelTests` and/or `ConsumerPlayModeScreenTests` for:
  - current place profile/zoom is 32x32;
  - actor inventory profile/zoom is 24x24 with 1px gap;
  - parent chain profiles map immediate parent/grandparent/great-grandparent correctly;
  - facing decorators appear in the current-location view when facing facts exist.
- Add or update `LinkedInventorySpaceLayoutTests` if connector/hit-region geometry now consumes pixel-size profiles.
- Run `dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj` before declaring the sprint complete.
- Run broader tests only if shared Core/Content projection contracts are changed.

## Documentation updates when complete

- Update `docs/Source of Truth/Frontend-UX-Standards.md` with accepted Space Zoom, Relationship Tier, Display Profile, micro-space, and Facing decorator standards.
- Add a `Frontend-UX-Decisions.md` decision if the child-surface mixed-scaling pattern is accepted.
- Update the component gallery as the executable pattern reference.
- Update this plan's sprint findings log and mark it completed or record the stop condition if viability fails.

## Sprint findings log

- **2026-07-30 planning checkpoint:** Official SadConsole documentation says surfaces have independent `FontSize`, child screen objects render in a parent/child hierarchy, and `ScreenSurface.UsePixelPositioning` positions surfaces by pixel. Existing project code already uses a child `Console` with pixel positioning for the Candii preview layer. Initial direction is viable enough for a gallery/probe slice before broader Play-mode implementation.
- **2026-07-30 implementation checkpoint 1 friction:** SadConsole child surfaces can provide true 32/24/16/8 Candii cell sizes through `FontSize`, but ordinary SadConsole cell surfaces do not naturally express a 1-pixel gap between adjacent glyph cells; cells are contiguous at the selected font size.
- **Mitigation planned:** Keep `CellGapPixels` in the frontend display-profile model as a pixel-layout requirement, prove the 24x24+1px player-inventory case in the mixed-scaling probe, and use either child-surface composition with explicit pixel offsets or a small custom draw path for gap-aware inventory spaces rather than pretending the root-cell `InventorySpaceCellMetrics.Gap` represents one display pixel.
- **2026-07-30 implementation checkpoint 2 friction:** Play-mode component layout is still expressed in root SadConsole cells while mixed-scale inventory spaces are now expressed in pixels. The first Play-mode application needs a bridge from display-profile pixel requirements back into root-cell component bounds.
- **Mitigation used:** Add a narrow `RequiredRootCellSize(...)` bridge in the Actor POV component factory using the current default 16px root-cell assumption, then render profiled inventory components through the reusable mixed-scale renderer. This keeps the first Play-mode demo moving while leaving a follow-up to make root-cell pixel size an explicit layout input instead of a factory constant.
- **2026-07-30 implementation checkpoint 3 friction:** Real Play-mode testing found four coupled issues in the first direct child-surface application: inventory spaces rendered above active action selectors, connector lines missed resized entity/cell locations, tooltips no longer overlapped the drawn entity surfaces, and input/redraw lag became noticeable because many child consoles were recreated after each input.
- **Mitigation selected:** Classify the first Play-mode application as a spike artifact, keep the successful gallery/probe evidence, and pause final Play-mode scaling until a shared presentation geometry/layer/performance foundation is added. The next implementation step is a pure inventory-space presentation geometry model consumed by rendering, connectors, and hit-testing.
- **2026-07-30 implementation checkpoint 4:** Added the first pure presentation-geometry model and began migrating connector endpoints to consume it. Connector endpoints can now carry exact pixel anchors while preserving root-cell fallback fields for existing fallback rendering/tests.
- **Mitigation used:** Keep `ConnectorLineEndpoint` backward-compatible with root-cell endpoints, but allow geometry-derived pixel endpoints so smooth connector rendering can attach to the same pixel cell centers used by mixed-scale inventory rendering.
- **2026-07-30 implementation checkpoint 5:** Migrated hover/tooltip hit-testing for profiled inventory spaces to use `InventorySpacePresentationGeometry`. The hit tester converts root-cell mouse positions to pixel centers for profiled spaces, then converts hit pixel bounds back to root-cell tooltip placement bounds.
- **Mitigation used:** Preserve the existing root-cell hover path for unprofiled components while routing profiled inventory spaces through shared geometry. `ConsumerPlayModeScreen.BuildRenderFrame(...)` now accepts root cell pixel dimensions so the renderer/frontend host can provide the active display metrics.
- **2026-07-30 implementation checkpoint 6 friction:** Live testing showed profiled child surfaces still did not produce tooltips even after geometry-backed hit-testing. SadConsole mouse state can be relative to the screen object under the cursor, and the pixel-positioned child consoles may become the mouse target instead of the root Play console.
- **Mitigation used:** Mark mixed-scale child cell consoles as non-mouse/non-keyboard surfaces and read `MouseScreenObjectState.WorldCellPosition` in `ConsumerPlayModeConsole.ProcessMouse(...)` so hover state is based on root screen coordinates rather than child-surface-local coordinates.
- **2026-07-30 implementation checkpoint 7:** Began layer-policy mitigation for the temporary child-surface renderer by computing prompt/tooltip/debug occlusion rectangles before drawing mixed-scale gameplay spaces. Mixed-scale cells intersecting active overlay rectangles are skipped so active UI remains visible even while the final layer-safe renderer is pending.
- **Mitigation used:** Add `PixelRect.Intersects(...)` and pass overlay occlusion rectangles into `MixedScaleInventorySpaceRenderer`. This is a bridge, not the final layering model; the final renderer should make gameplay spaces a true base layer rather than relying on per-cell suppression.
- **2026-07-30 implementation checkpoint 8:** Reduced redraw/input lag in the temporary child-surface renderer by reusing existing per-cell child consoles across frames instead of removing and recreating all child surfaces after every input.
- **Mitigation used:** `MixedScaleInventorySpaceRenderer` now has `BeginFrame`/`EndFrame` lifecycle methods, tracks active cell-layer keys, updates existing child consoles in place, and removes only layers that are no longer active. This keeps the current child-surface approach usable while a future batched/base-layer renderer remains the cleaner final direction.
- **2026-07-30 implementation checkpoint 9 friction:** Live testing showed cached child consoles could keep displaying stale entity glyphs after state changes even though connector geometry and inspection panels updated correctly. Reusing child surfaces avoids lag, but SadConsole cached surfaces still need explicit dirty marking after their glyph/color content changes.
- **Mitigation used:** Mark each reused mixed-scale child cell surface dirty immediately after updating its glyph/foreground/background. Keep watching this area; if stale cells persist, the next mitigation should replace per-cell child consoles with a single explicitly redrawn per-space surface or batched draw-call renderer.
- **2026-07-31 implementation checkpoint 10:** Added the first facing-decorator path for mixed-scale inventory spaces. Official SadConsole docs confirm each `ColoredGlyphBase` cell has a `Mirror` property and each `CellDecorator` can carry a glyph, color, and mirror flag. The frontend now maps Candii glyph `252` to North/South, `253` to East/West, and `251` to diagonals with SadConsole `Mirror.Horizontal`/`Mirror.Vertical` combinations, then layers the yellow facing arrow as a `CellDecorator` over the entity glyph so entity identity is preserved.
- **Mitigation used:** Keep facing facts presentation-only in SadConsole by passing world action-facing facts into the Actor POV screen model as a read-only dictionary, then into `InventorySpaceViewModel.FromProjection(...)`; no Core/Content projection contract changes were made in this slice. Micro 4x4 spaces still cannot honestly show the Candii arrow decorator and should be treated as summary rendering until a micro-state marker policy is chosen.
- **2026-07-31 cleanup checkpoint 11:** Before promoting the pattern, removed the hidden 16px root-cell assumption from Actor POV component sizing/connector geometry. Root-cell pixel metrics are now explicit screen-model input (`InventorySpaceRootCellMetrics`) and flow from `ConsumerPlayModeScreen.BuildRenderFrame(...)` into component sizing and connector anchor resolution.
- **Mitigation used:** Keep `InventorySpaceRootCellMetrics.DefaultPlay` as the default test/component-model baseline, but allow the live renderer path to pass the active root-cell pixel dimensions. Also separated the profile preference `ShowFacingDecorators` from the renderer capability `CanRenderGlyphFacingDecorators`; `Micro4` may carry the facing-decorator preference while the current micro renderer honestly reports that it cannot render Candii glyph-facing decorators.

## Exit criteria

- Viability checkpoint is either passed with an accepted renderer pattern or the sprint stops with documented friction.
- If viable and implemented, the current location space renders at 32x32 Space Zoom.
- Immediate parent, grandparent, and great-grandparent-or-beyond spaces render at 16x16, 8x8, and 4x4 respectively where visible.
- Player/controlled actor inventory renders at 24x24 with a 1px gap between cells.
- Current location displays Facing information per entity as decorators without replacing entity identity glyphs.
- Tests cover the profile mapping and main Play-mode acceptance criteria without launching the real SadConsole window.
