# SadConsole UI Pattern Discovery Sprint

Status: Active planning draft for the next frontend sprint.

Read when:

- beginning SadConsole editor recreation work after the current prototype checkpoint;
- choosing SadConsole features for reusable frontend component patterns;
- comparing custom-drawn versus SadConsole UI-control implementations;
- deciding how visual editor components should map to frontend UX invariants and standards.

Related source of truth:

- `docs/Source of Truth/Frontend-UX-Invariants.md` records frontend layer boundaries, directional navigation, contextual controls, text-entry submodes, and semantic editor focus targets.
- `docs/Source of Truth/Frontend-UX-Standards.md` records the Editor/Simulation mode model, semantic focus layout direction, entity-neutral presentation, and glyph standards.
- `docs/Source of Truth/Frontend-UX-Decisions.md` records chronological frontend UX decisions, including semantic editor focus.
- `docs/Plans/Gamma-Editor-MVP-Plan.md` records the broader Gamma Editor MVP release target.
- `docs/Plans/SadConsole-Frontend-Roadmap.md` records the broader SadConsole/debug-browser roadmap.

## Sprint goal

Each visual component of the entity-template editor mockup has associated SadConsole features that could potentially represent it. The sprint should discover, prototype, compare, and decide reusable SadConsole UI patterns before continuing the editor rebuild.

The intended output is not only a prettier entity-template editor. The intended output is a reusable frontend component vocabulary for recurring patterns such as screen panels, semantic fields, scroll lists, grids, contextual footers, popups, and focus/selection treatments.

## Background / checkpoint

The current SadConsole Editor mode is useful but is now considered a prototype surface that has served its purpose. It successfully proved service-backed entity-template mutations and semantic focus navigation, but it is still visually constrained by list/detail row rendering and monolithic shell drawing.

Next work should recreate the editor while deliberately experimenting with SadConsole features instead of extending the existing row-list menu pattern by default.

### Completed pre-sprint checkpoint

The frontend session that created this sprint plan completed the following prototype/checkpoint work:

- documented semantic editor focus as a frontend UX invariant, standard, and decision;
- exposed service-backed entity-template edits through SadConsole semantic focus fields, including name, glyph, color, metadata, initial facing, default action plan, targeting rules, template create/duplicate/delete, and save/dirty/preview-stale behavior;
- validated the prototype with focused SadConsole tests;
- agreed that the current editor menu/list-detail surface is a useful prototype and should not be the final visual pattern.

This checkpoint is complete, but it was not represented by a standalone active sprint plan before this document. No planning document is being archived for that checkpoint; this plan records the transition from completed prototype checkpoint to the next active sprint.

## Prototype/legacy replacement target

The current `src/GameGameGame.SadConsole` implementation is now explicitly **legacy/deprecated reference code**. It remains in-tree because the working catalog launch, editor-service integration, preview/materialization, Simulation launch/return, controlled-command Simulation mode, and tests are valuable reference material, but new frontend work should not extend the monolithic shell/list-detail architecture by default.

The replacement target is a clean componentized architecture inside the SadConsole frontend lane: reusable screen models, selectable/focusable components, explicit border/focus states, service-backed data projection, and eventually SadConsole rendering/input adapters over those testable models. Starting a separate replacement project is no longer the default requirement; the important boundary is legacy/reference versus new componentized code.

### Should be deprecated as prototype/legacy

These pieces may remain temporarily as fallback/reference paths, but should not be extended as the durable editor or simulation UI pattern:

1. **Row-list Editor rendering as the primary screen model.**
   - Current examples: `SadConsoleEditorViewBuilder` rows, `DetailRows`, `ScenarioRows`, `DiagnosticRows`, and generic row printing in `SadConsoleShell.DrawEditor()`.
   - Replacement target: dedicated editor screens/components with explicit regions, field rectangles, panels, and footer geometry.
2. **Inline semantic focus markers inside text rows.**
   - Current example: `>[field]<` embedded in detail strings.
   - Replacement target: visible field boxes, focus outlines/backgrounds, component state, or SadConsole control focus styling.
3. **Hotkey-first editor affordances.**
   - Current examples: `N/G/C/A/Y/B/S/R/P/J/M` as primary discoverability.
   - Replacement target: directional navigation plus Select/Cancel and visible contextual controls, with hotkeys retained only as optional accelerators while useful.
4. **Command menus rendered as detail rows.**
   - Current example: command menu entries injected into `DetailRows`.
   - Replacement target: a reusable popup/window/list/dialog pattern selected during this sprint.
5. **Textual targeting/inventory submode rendering.**
   - Current examples: targeting-rule rows and inventory brush rows rendered as text summaries.
   - Replacement target: reusable row/field/grid components, potentially using `Table`, `SurfaceViewer`, custom drawing, or selected SadConsole controls.
6. **Monolithic shell ownership of Editor drawing/input.**
   - Current example: `SadConsoleShell` directly handles most Editor rendering and input submodes.
   - Replacement target: shell as mode/router, with componentized Editor screens such as `SadConsoleEditorRootScreen`, `TemplateEditorScreen`, future `ActionPlanEditorScreen`, and future `ScenarioEditorScreen`.

### Should not be deprecated

These foundations should be preserved and reused unless a concrete blocker appears:

- Simulation mode and shared runtime service consumption;
- scenario catalog/content-file startup;
- Editor -> Preview -> Simulation -> Return context flow;
- `FrontendEditorService` / editor-service-backed mutation behavior;
- preview materialization and stale-preview policy;
- existing frontend tests, though row-string assertions should migrate toward layout/component assertions as replacement components land.

### End-of-sprint replacement checkpoint

The sprint should aim to leave the codebase in a state where:

- the old row-list Editor mode is explicitly fallback/reference, not the primary direction;
- at least the entity-template editor has a componentized visual layout matching the mockup direction;
- accepted component patterns have focused test suites;
- decisions for screen panels, semantic fields, footer controls, popups/dialogs, scroll lists/tables, and inventory/grid surfaces are either documented as accepted patterns or explicitly marked unresolved with findings.

## Sprint principles

1. **Prototype before committing to a component pattern.** When multiple SadConsole features look promising, implement small comparison prototypes and evaluate them against UX invariants.
2. **Prefer reusable component patterns.** A solution for a template panel, field box, scroll list, or footer should be reusable for action-plan and scenario editor screens where sensible.
3. **Keep authored mutations service-backed.** UI components may own presentation/focus/input state only. Mutations must continue routing through shared editor/content services.
4. **Document decisions as they become concrete.** Findings can live in this sprint plan while exploratory. Accepted durable patterns should be promoted into `Frontend-UX-Standards.md` and chronological decisions in `Frontend-UX-Decisions.md`.
5. **Test reusable component behavior.** New component patterns should receive focused frontend test suites. Tests should cover layout/focus/view-model behavior, not duplicate editor-service semantics.

## Initial SadConsole feature-to-mockup trace

This table is the starting trace. It should be updated as prototypes confirm, reject, or refine candidate features.

| SadConsole feature / API area | Likely utility | Mockup component(s) to try it on | Prototype question | Current expectation |
|---|---|---|---|---|
| `CellSurfaceEditor` drawing primitives: `DrawBox`, `DrawLine`, `Print`, `Fill`, `SetGlyph`, foreground/background setters | Highest utility for custom visual vocabulary | Orange non-diegetic region boxes; pink editable field rectangles; glyph preview card; footer box; inventory grid; targeting field boxes | Can custom-drawn regions/fields reproduce the mockup cleanly with predictable layout and testable geometry? | Strong candidate for base visual layer and first prototype. |
| `ScreenSurface` | Independent rendered surfaces with view dimensions, dirty state, view position, tint, font/font size | Entity info panel; targeting panel; inventory panel; footer/control bar; scrollable content region | Are separate surfaces easier to compose, clip, and eventually scroll than one monolithic console? | Strong candidate for screen regions/components, especially panels and large/scrolling areas. |
| `ScreenObject` child hierarchy, focus, position, keyboard/mouse processing | Component composition and input routing | Dedicated `TemplateEditorScreen`; child panel components; footer component; popup/menu screens | Does splitting the editor into child screen objects reduce shell complexity and make focus/input ownership clearer? | Strong candidate for replacing monolithic `DrawEditor`. |
| `SadConsole.UI.Controls.Panel` | Built-in container/control grouping | Orange region containers; editor panels; nested property groups | Does the built-in panel give enough visual/focus control, or is custom drawing better? | Prototype alongside custom-drawn panels. |
| `SadConsole.UI.Controls.SelectionButton` | Directional focus between selectable controls | Pink editable fields such as Name, Color, Action Plan, targeting label/target/range | Can SelectionButton naturally model controller-friendly field-to-field navigation? | Promising for semantic fields; compare with custom focus graph. |
| `SadConsole.UI.Controls.TextBox` | Text entry field | Template name; glyph if constrained; targeting-rule label; create/duplicate name prompts | Does TextBox give better text-entry UX than current manual typed submode, while preserving confirm/cancel expectations? | Prototype for text fields and create/duplicate dialogs. |
| `SadConsole.UI.Controls.NumberBox` | Numeric entry field | Inventory width/height; bulk; aperture; targeting range | Does NumberBox handle numeric editing and validation better than manual integer typing? | Strong candidate for numeric fields. |
| `SadConsole.UI.Controls.ComboBox` | Pick-one selection | Color; initial facing; default action plan; target template picker | Does ComboBox fit controller/directional navigation and compact field boxes? | Candidate for picker fields; compare with custom picker/popup. |
| `SadConsole.UI.Controls.CharacterPicker` | Glyph selection | Template glyph field; future glyph palettes | Is a glyph picker more usable than single-character typing/cycling? | Candidate for glyph edit popup. |
| `SadConsole.UI.Controls.ColorPicker` / `ColorBar` | Color selection | Template color field; future palette editing | Is a full color picker overkill versus palette cycling? | Prototype only if palette cycling feels insufficient. |
| `SadConsole.UI.Controls.ListBox` | Scrollable list | Template browser list; action-plan list; scenario list; command lists | Does ListBox provide enough control over selected-row styling and directional navigation? | Strong candidate for browser/transitional list screens. |
| `SadConsole.UI.Controls.Table` | Structured rows/columns | Targeting rules; action-plan steps; diagnostics; references | Can Table represent row/column editor surfaces better than custom rows? | Strong candidate for targeting/action-plan/diagnostic grids. |
| `SadConsole.UI.Controls.SurfaceViewer` | Drawing a larger surface with optional scroll bars | Large inventory layout; long panels; YAML/diff; action-plan step canvas | Does SurfaceViewer solve clipping/scrolling for expanding regions? | Strong candidate for inventory and long content surfaces. |
| `SadConsole.UI.Window` and popup/window controls | Modal or transient UI | Create/duplicate/delete confirmation; pickers; command menus | Are windows/popup controls a better pattern for transitional menus than inline detail rows? | Strong candidate for command/menu/dialog pattern. |
| `CellDecorator`, `Effects`, `Blink`, `Recolor`, `Fade`, `Instruction` APIs | Focus and state emphasis | Current selection marker; focused field highlight; dirty/stale warning; validation warning; selection cursor | Can effects make focus/selection more readable without violating glyph identity standards? | Useful after static layout is working. |
| `LayeredScreenSurface` / layered surface components | Overlays without mutating base region drawing | Focus outlines; hover overlays; modal dimming; validation warning overlays | Are layers cleaner than redrawing all base cells for focus/hover? | Explore after base custom-drawn prototype. |
| Mouse input support on `ScreenObject`/`ScreenSurface` and controls | Future mouse affordances | Click focused field; click inventory cell; hover diagnostics; click popup controls | Can the same component geometry support mouse later without redesign? | Defer implementation, but record hit-test needs during component design. |

## Mockup component inventory

The sprint should trace and eventually decide patterns for each of these components:

| Mockup component | Description | Candidate SadConsole features | Decision status |
|---|---|---|---|
| Whole template editor screen | Full authored entity-template edit surface replacing the prototype list/detail view | `ScreenObject`, `ScreenSurface`, child surfaces, custom `DrawBox` regions | Undecided. |
| Entity info panel | Left panel with glyph preview, name/color fields, action plan, read-only step summary | Custom drawn `CellSurfaceEditor`; `Panel`; `SelectionButton`; `TextBox`; `ComboBox`; `CharacterPicker`; `ColorPicker` | Undecided. |
| Glyph preview card | Large visual identity card using entity glyph/color | Custom drawing; `SetGlyph`; `Fill`; decorators/effects for focus | Undecided. |
| Editable field boxes | Pink field rectangles such as Name, Color, Action Plan, targeting fields | Custom field-box drawing; `SelectionButton`; `TextBox`; `NumberBox`; `ComboBox` | Undecided. |
| Read-only step summary | Action plan steps shown under action-plan field | Custom panel/list; `ListBox`; `Table`; `SurfaceViewer` if long | Undecided. |
| Targeting information panel | Top/right region with rule label/target/range fields, expandable as properties grow | Custom drawn panel; `Table`; `SelectionButton`; `TextBox`; `NumberBox`; `ComboBox` | Undecided. |
| Inventory editor region | Large spatial authored inventory layout area with brush selector | Custom grid drawing; `SurfaceViewer`; `ScreenSurface`; `DrawingArea`; `SelectionButton` for brush | Undecided. |
| Contextual footer/control bar | Bottom instructions showing current focus and Select/Cancel behavior | Custom drawn `ScreenSurface`; `Panel`; `Label`; effects for warning/status | Undecided. |
| Transitional command/menu dialogs | Create/duplicate/delete, pickers, confirmations | `Window`; `ListBox`; `TextBox`; `ButtonBox`; custom popup surface | Undecided. |
| Scroll/expansion behavior | Regions expand right/down or scroll when too large | `SurfaceViewer`; `ScreenSurface` view position; `ScrollBar`; `ListBox`; `Table` | Undecided. |
| Focus/selection treatment | Visual indication of current semantic focus | Background colors; `CellDecorator`; `Effects`; `SelectionButton` state; layered overlays | Undecided. |

## Proposed implementation sequence

### Phase 0: Baseline capture and acceptance criteria

1. Keep the current editor prototype available as a behavior/reference path.
2. Capture the mockup component list and initial trace in this plan.
3. Define acceptance criteria for a visual prototype:
   - editor regions are visually separated;
   - semantic fields are visible as field boxes;
   - directional focus is visible and matches frontend invariant 13;
   - footer describes current focus controls;
   - no editor mutation bypasses shared services.

### Phase 1: Component-library foundation

Goal: create reusable, themeable, testable frontend components before rebuilding any full screen.

Tasks:

1. Add a separate theme/token model for panel borders, list rows, fields, and footers.
2. Add primitive component models for bordered panels, selectable lists, editable fields, and field groups.
3. Add a shared focus router that owns the screen-level selected-versus-focused component policy.
4. Keep these primitives pure/testable and independent from direct SadConsole rendering so the user can review component behavior before we wire visual screens.
5. Treat legacy shell/list-detail behavior as reference only while new screens compose these primitives.

Expected tests:

- `SadConsoleUiComponentLibraryTests`
- `SadConsoleComponentGalleryTests`
- Existing `SadConsoleExplorationComponentsTests` remain the first screen-flow trace over these ideas.

Review artifact:

- `ComponentGalleryScreen` composes Phase 1 primitives into a non-game gallery that shows panel border states, selectable-list behavior, editable-field states, and footer control wording. This should be the first thing reviewed/adjusted before Scenario Selection is rebuilt.
- Run the visual gallery with `GameGameGame.SadConsole --gallery` (or equivalent `dotnet run --project src/GameGameGame.SadConsole -- --gallery`) to inspect the current SadConsole-rendered version. The gallery exits on Cancel/Esc when no component is focused.
- Built-in themes now include `Default` and `Blueprint`. Themes carry both color tokens and border glyph sets, so review can evaluate presentation differences beyond color before editor-parity work resumes.
- Reusable field editor overlays now include `TextEntryOverlayComponent`, `IntSetterOverlayComponent`, and `ChoicePickerOverlayComponent`. These are intended to be the common mutation panels for text fields, controller-friendly integer fields, and pick-one fields before bespoke editors are introduced.
- `ConfirmOverlayComponent` is also part of the accepted gallery/prototype set for risky operations such as delete or exit-with-unsaved-changes. `CommandPaletteOverlayComponent` remains in code as a deferred action-editing idea, but it is intentionally not shown in the gallery until action-plan editing has a concrete command/menu requirement.

Phase gate before Phase 2:

- Review border/focus states and theme token names.
- Review selectable-list behavior: empty state, selected row, focused row, scrolling.
- Review editable-field behavior: read-only, editable, editing, dirty, invalid.
- Review focus-router behavior: when no component is focused, controls select components; when focused, controls route to that component; Cancel releases focus or leaves the screen.
- Do not rebuild Scenario Selection until the component vocabulary is accepted or adjusted.

### Phase 1B: Custom-drawn layout prototype

Goal: prove accepted component models can be rendered with direct SadConsole surface drawing.

Tasks:

1. Add component renderers over the accepted component models.
2. Add tests for renderer-independent geometry where practical.
3. Draw static panel boxes, field boxes, list boxes, inventory placeholder, and footer using the shared theme.
4. Prefer a component gallery/mock screen before wiring a real scenario flow.

### Phase 2: Built-in UI control comparison

Goal: identify where SadConsole controls beat custom drawing.

Prototype candidates:

1. `SelectionButton` field navigation prototype for semantic fields.
2. `TextBox` for name/label/create/duplicate text entry.
3. `NumberBox` for numeric metadata/range entry.
4. `ComboBox` for color/facing/action-plan/target-template pickers.
5. `Panel` for region containment.

Compare against custom drawing on:

- controller/directional usability;
- visual fit to mockup;
- ease of theming;
- testability;
- mouse readiness;
- compatibility with current editor-service mutation flows.

### Phase 3: Scroll/grid/list pattern comparison

Goal: choose reusable patterns for recurring large/dynamic content.

Prototype candidates:

1. `ListBox` for template/action-plan/scenario lists.
2. `Table` for targeting rules/action-plan steps/diagnostics.
3. `SurfaceViewer` for inventory/YAML/diff/large visual canvases.
4. Custom grid surface for authored inventory editing.

Expected tests:

- `ScrollableListViewTests`
- `EditorTableViewTests`
- `InventoryGridEditorViewTests`

### Phase 4: Pattern decisions and documentation

For each accepted component pattern:

1. Record the decision in `Frontend-UX-Decisions.md`.
2. Add durable guidance to `Frontend-UX-Standards.md` if it affects future frontend work.
3. Update this plan's trace table from “Undecided” to the chosen pattern.
4. Ensure tests cover the reusable behavior.

### Phase 5: Apply to editor rebuild

Goal: recreate the entity-template editor with chosen component patterns.

Scope:

1. Replace the current Templates list/detail editor as the primary template-editing surface.
2. Preserve service-backed mutations already implemented in the prototype.
3. Add authored inventory layout editing using chosen grid/surface/list patterns.
4. Keep old prototype code only as temporary fallback until the new screen reaches parity.

## Stretch goals

### Stretch goal 1: Finish editor with chosen patterns

Use accepted patterns to implement or recreate:

- full entity-template editor, including inventory layout editing;
- action-plan editor screen;
- scenario editor screen;
- transitional menu/dialog system.

### Stretch goal 2: Improve Simulation/gameplay UI with learned patterns

Apply useful component patterns to Simulation mode:

- entity panels and panel chains;
- local activity lists;
- global logs;
- prompt/footer controls;
- focus/selection/highlight effects;
- mouse hit-testing over component geometry.

## Open questions

1. Should the first visual prototype use one root `Console` with custom drawing, or multiple child `ScreenSurface` objects?
2. Should we adopt SadConsole UI controls directly, or treat them as reference implementations while keeping a custom controller-first focus graph?
3. How much should popups/windows be used versus inline footer/menu panels?
4. What is the minimum viable visual parity with the mockup before continuing inventory editing?
5. Should component geometry become a shared frontend-owned layout model usable for future mouse hit-testing?

## SadConsole feature research checkpoint: size, placement, layering, movement, drag

Status: Research checkpoint recorded after the component gallery and first editor-flow skeletons proved child-console overlays in the new Scenario Selection and Entity Template targeting-detail screens.

Research sources:

- SadConsole surfaces/consoles overview: `https://sadconsole.com/guides/surfaces-overview/`
- SadConsole `IScreenObject`/`ScreenObject` overview: `https://sadconsole.com/guides/screenobject-basics/`
- `Settings.WindowResizeOptions`: `https://sadconsole.com/reference/sadconsole.settings.windowresizeoptions/`
- `Overlay`: `https://sadconsole.com/reference/sadconsole.components.overlay/`
- `LayeredScreenSurface`: `https://sadconsole.com/reference/sadconsole.layeredscreensurface/`
- `Window`: `https://sadconsole.com/reference/sadconsole.ui.window/`
- `MouseDragViewPort`: `https://sadconsole.com/reference/sadconsole.components.mousedragviewport/`
- `ObjectComponentMove`: `https://sadconsole.com/reference/sadconsole.components.objectcomponentmove/`
- `SmoothMove`: `https://sadconsole.com/reference/sadconsole.components.smoothmove/`

Findings:

1. **Screen size and resizing.** SadConsole supports screen/surface dimensions in cells, surfaces with a visible view over a larger backing buffer, and final render resize policies through `Settings.WindowResizeOptions` (`Stretch`, `Center`, `Scale`, `Fit`, `None`). This confirms that frontend layout should keep an explicit cell-geometry model rather than hard-code only the initial 120x40-style demo assumptions. Near-term work can still use fixed cell bounds, but the component screen models should expose panel rectangles so a future layout pass can recompute them when the host size changes.
2. **Dynamic placement.** `ScreenObject.Position` is relative to the parent; `AbsolutePosition` is resolved from the parent chain; `IgnoreParentPosition = true` supports fixed overlays. Child objects render/update through the parent tree. This fits the current child-console overlay experiment and suggests a frontend-owned layout model can place panels as either children of a screen container or fixed overlays without changing the pure component models.
3. **Layering and transparency.** SadConsole supports multiple layering approaches: child `ScreenObject` render order and `SortOrder`, `Overlay` components that draw a matching surface on top of a host surface, `LayeredScreenSurface` with transparent cells over lower layers, and `Window` for titled/bordered modal dialogs. Our current child `Console` overlays are valid for popup/menu exploration; for focus outlines, dimming, validation marks, and hover highlights, `LayeredScreenSurface` or an `Overlay` component may be cleaner than mutating the base panel drawing.
4. **Movement and translation.** Moving a parent `ScreenObject` moves its children as a group. SadConsole also includes `ObjectComponentMove` for keyboard-driven movement and `SmoothMove` for animated position transitions. This is useful for future debug/gallery experiments and possible animated panel transitions, but durable Editor mode should not let generic movement components consume arrow keys that belong to semantic focus navigation.
5. **Dragging and mouse readiness.** `Window` has built-in dragging (`CanDrag`) and modal/close-on-Esc behavior. `MouseDragViewPort` enables dragging a scrollable surface viewport. General mouse input is available through `UseMouse`, `ProcessMouse`, exclusive mouse routing, and `MouseScreenObjectState`. This supports future mouse hit-testing over the same component rectangles, but immediate editor work should remain controller/keyboard-first.

Current architectural implication:

- Keep the pure frontend component models and focus router as the source of truth for keyboard/controller behavior.
- Add/keep explicit geometry on rendered components and overlays so placement can become responsive later.
- Continue using child `Console` overlays for simple popups in the near term because they already work in Scenario Selection and targeting detail.
- Prototype `Window` only when we need built-in modal/control behavior or mouse-drag affordances; do not switch all popups to `Window` preemptively.
- Prototype `LayeredScreenSurface`/`Overlay` for visual effects such as modal dimming, focus outlines, validation badges, or hover states after the static editor skeleton stabilizes.

## SadConsole feature research checkpoint: fonts, glyphs, and tilesets

Status: Research checkpoint recorded after the color-picker preview proved that Unicode-looking text markers are not always reliable as rendered SadConsole glyphs.

Research sources:

- SadConsole font overview: `https://sadconsole.com/guides/fonts-overview/`
- SadConsole startup config font options: `https://sadconsole.com/guides/config-overview/`
- `SadFont`: `https://sadconsole.com/reference/sadconsole.sadfont/`
- `GlyphDefinition`: `https://sadconsole.com/reference/sadconsole.glyphdefinition/`
- `FontExtensions`: `https://sadconsole.com/reference/sadconsole.fontextensions/`

Findings:

1. **SadConsole fonts are tilesets, not system fonts.** A SadConsole font is a PNG texture plus `.font` metadata. Surfaces draw cells by glyph index into that texture. A .NET `char` or Unicode symbol in a string is only visible if the active font/tileset maps that code point or glyph index to a useful tile.
2. **Default font model is Code Page 437-oriented.** SadConsole's documented default is IBM 8x16 Code Page 437. The project currently calls `UseBuiltinFontExtended()` in `src/GameGameGame.SadConsole/Program.cs`, so we are using SadConsole's built-in extended font, but it is still safer to treat CP437/basic ASCII and explicit glyph indices as the portable UI baseline.
3. **Important reserved/known glyphs.** Glyph index `0` should be transparent/dead and may be skipped for optimization. `SolidGlyphIndex` is required; for Code Page 437 fonts the solid fill glyph is index `219`. This is the right glyph to use for color swatches/fill samples rather than relying on Unicode `■` rendering.
4. **Unsupported glyph handling exists.** `SadFont` exposes `UnsupportedGlyphIndex`/`UnsupportedGlyphRectangle`, and missing glyphs may render as that unsupported glyph instead of the intended symbol. This means renderer output should prefer known glyph indices for UI primitives, borders, cursors, and samples.
5. **Named glyph definitions/decorators are possible.** Fonts can define named `GlyphDefinition`s and create decorators/cells from them. This may be useful later for custom icons, but current reusable components should avoid depending on custom font definitions unless the frontend also owns and loads the required font asset.
6. **Font sizing is separate from component cell geometry.** Surfaces have a font and `FontSize`; `FontExtensions` can convert cell positions to pixel rectangles. Our current component layout should remain cell-based, but future mouse/hit-test and custom drawing work should remember font size controls the pixel footprint.

Current architectural implication:

- Treat component model strings as semantic/debug rows, not a guarantee of final glyph rendering.
- Renderer-owned glyph constants should be used for UI primitives that must be visually reliable. Current accepted example: color swatches use glyph index `219`.
- Keep theme border glyphs constrained to ASCII/CP437-safe characters unless we explicitly verify the active built-in extended font renders them correctly.
- For authored entity glyph editing, keep accepting a single character for now because content already models glyphs as `char`, but add future validation/preview guidance before expanding to glyph palettes or non-ASCII art.
- If we later want richer icons, create a small frontend glyph contract: named semantic icon -> glyph index/font requirement -> fallback glyph.

## Initial next action

Continue Phase 1 by reviewing the component-library primitives and theme/focus behavior with the user before rebuilding Scenario Selection. After acceptance, add a small component gallery/mock screen and direct SadConsole renderers, then proceed to Phase 2 Scenario Selection using only accepted components.

## Phase 2 Scenario Selection rebuild checkpoint

Status: Started.

Current demo command:

```text
dotnet run --project src/GameGameGame.SadConsole -- --new-scenario-selection
```

Optional catalog arguments may be combined with the flag, for example:

```text
dotnet run --project src/GameGameGame.SadConsole -- --new-scenario-selection --manifest <manifest>
dotnet run --project src/GameGameGame.SadConsole -- --new-scenario-selection --discover <folder>
dotnet run --project src/GameGameGame.SadConsole -- --new-scenario-selection --content <file>
```

Demoable behavior:

- new component-based Scenario Selection screen renders the scenario list through `SelectableListComponent`;
- Up/Down changes selected scenario;
- Enter opens a component-based Play/Edit sub-panel;
- The Play/Edit sub-panel is now rendered as a SadConsole child `Console` overlay rather than shrinking the scenario list. This is an exploration of SadConsole's screen-object layering model and should be reassessed after the next screen exposes more popup/menu needs.
- Up/Down changes the command while the command panel is open;
- Enter returns Play/Edit/Exit routing results for the selected scenario;
- Esc is Cancel/Back: it closes the command panel first, then exits when back on the scenario list;
- catalog diagnostics render as an error panel.

Known intentional placeholders:

- Play and Edit currently report routing results in the message line rather than handing off to Simulation or Scenario Edit. Those handoffs are the next screens/phases.
- Renderer code is still simple and should be consolidated with gallery rendering once the second real screen exposes enough shared renderer needs.
- Overlay rendering currently uses a child `Console` owned by the Scenario Selection renderer. If child-surface layering adds friction, the model can fall back to drawing overlay components last on one surface without changing the screen model.

## Phase 3 Scenario Edit rebuild checkpoint

Status: Started.

Demo path:

```text
dotnet run --project src/GameGameGame.SadConsole -- --new-scenario-selection
```

Then select a scenario, press Enter, choose Edit, and press Enter.

Demoable behavior:

- Scenario Edit opens from the new Scenario Selection Edit route;
- screen uses component API panels for 2.1 Scenario preview, 2.2 Player starting position, 2.3 Defined entities, and 2.4 Defined action plans;
- screen includes a prominent save-status panel: Brown/dirty when unsaved changes are pending and Green/saved when the editor snapshot is unmodified;
- S is a persistent save hotkey throughout the service-backed Scenario Edit flow, except while text entry is active; saving is also the preview-refresh boundary, so dirty and preview-stale are treated as one user-facing state for now;
- arrows choose a component while no component is focused;
- Enter focuses the selected component;
- Esc releases focus; when no component is focused, it returns to Scenario Selection if saved, or opens 2.5 Unsaved changes if dirty;
- 2.5 Unsaved changes warns about pending changes and offers Back to Editing, Save & Exit, and Exit without Saving; Esc from 2.5 returns to editing;
- focused entity/action-plan lists support Up/Down selection;
- 2.3 Defined entities has a pinned Create New Template row; selecting it opens a name-entry box, creates an initialized template through `FrontendEditorService.CreateEntityTemplate`, and proceeds to screen 3;
- selecting an existing entity template through 2.1 or 2.3 opens 2.3.1 with Edit Template, Duplicate Template, and Delete Template;
- 2.3.1 Edit Template proceeds to screen 3, Duplicate Template opens a name-entry box then proceeds to screen 3 for the duplicate, and Delete Template uses a confirmation modal before calling `FrontendEditorService.DeleteEntityTemplate`;
- 2.4 Defined action plans has a pinned Create New Action Plan row; selecting it opens a name-entry box, creates through `FrontendEditorService.CreateActionPlan`, and proceeds to screen 4;
- selecting an existing action plan opens 2.4.1 with Edit Action Plan, Duplicate Action Plan, and Delete Action Plan;
- 2.4.1 Edit Action Plan proceeds to screen 4, Duplicate Action Plan opens a name-entry box then proceeds to screen 4 for the duplicate, and Delete Action Plan uses a confirmation modal before calling `FrontendEditorService.DeleteActionPlan`.

Known intentional placeholders:

- Scenario preview is a first derived/authored summary and entity row list, not yet the full materialized containment tree view.
- Scenario root and player position fields remain read-only; editable scenario root/player start likely need additional shared editor API design.

## Phase 4 Entity Template Edit rebuild checkpoint

Status: Started.

Demo path:

```text
dotnet run --project src/GameGameGame.SadConsole -- --new-scenario-selection
```

Then select a scenario, choose Edit, focus the Defined entities list, select an entity, and press Enter.

Demoable behavior:

- Entity Template Edit opens from the new Scenario Edit entity route;
- screen uses component API panels for 3.1 Presentation information, 3.2 Targeting information, and 3.3 Inventory information;
- arrows choose a component while no component is focused;
- Enter focuses the selected component;
- Esc releases focus, then returns to Scenario Edit when no component is focused;
- focused Presentation information supports Up/Down field selection; Enter opens reusable field-editor overlays for name, glyph, and color, then confirms through `FrontendEditorService.UpdateTemplatePresentation` when opened from a service-backed Scenario Edit screen;
- focused Inventory information supports Up/Down metadata-field selection; Enter opens reusable integer overlays for inventory width, inventory height, aperture, and bulk, then confirms through `FrontendEditorService.UpdateTemplateMetadata` when opened from a service-backed Scenario Edit screen;
- focused Inventory information also exposes `3.3.2 inventory grid editor`; Enter opens a dedicated cursor-grid editing mode rather than forcing grid authoring through the normal field/Submit-only model;
- the 3.3 panel keeps only the five functional selections on the left and renders a clipped read-only inventory-grid preview on the right; brush selection, carried-entry detail rows, and explanatory placeholder copy were removed from the parent panel;
- Targeting information now derives read-only target labels from the template's selected/default Action Plan via shared `FrontendEditorService` targeting requirements; if no Action Plan/requirements exist, it asks the author to choose an Action Plan;
- targeting supports focused Up/Down target-label selection; Enter opens a layered 3.2.1 target-label detail sub-panel over the template screen;
- 3.2.1 keeps the label read-only and supports target-template choice plus target-range integer editing, confirming through `FrontendEditorService.SetTemplateTargetingRule` when opened from a service-backed Scenario Edit screen;
- authored targeting rules whose labels are not referenced by the selected/default Action Plan render as unused/orphaned rows instead of being deleted;
- Enter on the Presentation action-plan row opens an Action Plan picker: `(none)` clears the default Action Plan, defined plans assign the default through `FrontendEditorService`, and `Edit current action plan` is the separate affordance that jumps to the Action Plan editor when a default plan exists.

Inventory grid editor behavior:

- arrow keys move the grid cursor;
- grid cells render at fixed positions; moving the cursor changes only the cursor highlight, not cell spacing or glyph positions;
- Enter places the current brush at the cursor through `FrontendEditorService.OverwriteTemplateInInventory`, silently replacing the current occupant;
- Delete removes the carried entity at the cursor; Backspace is accepted as an alias but help text says Delete;
- Space starts move mode from an occupied cursor cell, and Space or Enter places the moving carried entity at the new cursor location;
- Esc cancels a pending move, otherwise returns to Entity Template Edit;
- C copies the carried entity template at the cursor into the current brush;
- Tab opens the brush picker; previous/next brush hotkeys are intentionally deferred because adjacent template order is unlikely to be useful in large template sets;
- the bottom-right Current cell panel always inspects the highlighted cursor cell; the `?`/`/` inspection hotkey was removed;
- undo, clear-brush hotkeys, large-grid navigation acceleration, viewport/pan controls, coordinate toggles, and mouse controls are deferred.

Known intentional placeholders:

- preview-stale/rematerialize controls beyond save/dirty status are not surfaced in the componentized shell yet.

## Phase 5 Action Plan Edit rebuild checkpoint

Status: Started.

Demo path:

```text
dotnet run --project src/GameGameGame.SadConsole -- --new-scenario-selection
```

Then either:

- select a scenario, choose Edit, focus the Defined action plans list, select a plan, and press Enter; or
- open Entity Template Edit, focus Presentation, and press Enter when the template has a default action plan.

Demoable behavior:

- Action Plan Edit opens from Scenario Edit action-plan selection;
- Action Plan Edit also opens from Entity Template Presentation default-plan jump;
- screen uses component API panels for 4.1 Action steps and 4.2 Highlighted step details;
- Enter focuses the action-step list;
- focused Up/Down changes the highlighted existing step;
- Enter on a highlighted existing step opens 4.1.1, a primitive picker over stable engine-defined authoring steps; choosing a primitive replaces that step through `FrontendEditorService.ReplaceActionPlanStep`;
- Delete/Backspace removes the highlighted existing step through `FrontendEditorService.RemoveActionPlanStep`, collapsing later steps downward;
- I opens 4.1.2, an insert-position picker for `insert above` / `insert below`; choosing a position opens 4.1.1, and choosing a primitive inserts through `FrontendEditorService.InsertActionPlanStep`;
- Space enters move mode; while moving, Up/Down swaps the selected step through `FrontendEditorService.MoveActionPlanStep`, and Enter or Space places/confirms the moved step;
- 4.2 always describes the currently highlighted item, whether the highlight is in 4.1 or inside the 4.1.1 primitive picker;
- Tab, R, `<`, and `>` intentionally do nothing in the Action Plan editor;
- the detail panel summarizes target-label requirements projected from the current action plan so authors can relate action-plan edits to Entity Template 3.2 targeting requirements;
- Esc releases focus first, then returns to Scenario Edit or Entity Template Edit depending on entry path.

Known intentional placeholders:

- action-step parameter editing, check/effect editing, and label/slot field editing are not implemented in the rebuilt UI yet;
- action-step mutation is limited to canonical behavior-chain steps exposed by shared editor services;
- step parameter editing is important but is not yet designed for the screen 4 workflow because the current frontend projection exposes step kind/display information, not a typed editable parameter surface.

## Rebuilt vs legacy/prototype checkpoint assessment

Status: Checkpoint captured after the componentized Scenario Selection -> Scenario Edit -> Entity Template Edit -> Action Plan Edit loop became demoable, including service-backed entity-template presentation edits, default Action Plan selection, action-plan-derived targeting requirements, targeting template/range edits, and inventory metadata edits.

Comparison targets:

- **Legacy/deprecated shell:** `SadConsoleShell` plus `SadConsoleEditorContext`. This path contains more historical editing semantics, but concentrates menu, editor, simulation, renderer, focus, mutation, preview invalidation, and modal state in large classes.
- **Intermediate prototype:** `SadConsoleExplorationComponents`. This proved the panel/component visual direction but was mostly model/snapshot projection rather than durable reusable UI infrastructure.
- **Rebuilt componentized path:** `Ui/Screens`, `Ui/Components`, `Ui/Navigation`, `Ui/Rendering`, and `Ui/Styling`.

### Architectural strengths vs code smells

Rebuilt strengths:

- Screen state, reusable components, styling, focus routing, and SadConsole rendering are now separated enough to test screen behavior without the SadConsole host.
- Shared content/editor services remain the mutation boundary; the frontend does not reimplement content semantics for presentation, metadata, default Action Plans, or targeting rules.
- The new component model gives recurring affordances names and tests: panels, field groups, selectable lists, text/int/choice overlays, confirmation overlays, themes, and focus routing.
- Targeting now follows shared action-plan-derived projections instead of letting the frontend author arbitrary labels that may not correspond to the selected/default Action Plan.
- The new path has clearer mode/screen transitions and explicit return destinations, especially for Entity Template -> Action Plan -> Entity Template flows.

Rebuilt code smells / risks:

- `EntityTemplateEditScreen` is accumulating several field-specific concerns in one class. It is much cleaner than the legacy context, but could become the next monolith if inventory and action-plan editing are added directly into it.
- Overlay dispatch is still string/id driven (`action-plan`, `target-template`, etc.). This is acceptable for the prototype checkpoint but should evolve toward typed field/action descriptors if the number of editable fields grows.
- Snapshot refresh and selected-index preservation are implemented per screen. This is simple today, but repeated mutation/refresh patterns may deserve a shared helper once Action Plan and inventory editing are added.
- The renderer still has special cases for some component visuals, such as color samples. This is tolerable while SadConsole glyph behavior is being learned, but component-owned render hints may be preferable later.

Legacy strengths:

- The legacy editor context already had several useful mutation semantics: default Action Plan picker, action-step insert/replace, targeting-rule mutation, inventory brush placement, template create/duplicate/delete, and preview-stale marking.
- Many edge cases around mutation result handling and stale preview state were already explored there.

Legacy code smells:

- Large classes combine input dispatch, modal state, editor service calls, selection restoration, preview invalidation, and rendering-facing state.
- The command/menu model is harder to discover visually than the new panel/field model.
- Some legacy semantics were frontend-shaped rather than UX-shaped, such as editable targeting labels before the action-plan-derived targeting requirement model existed.
- Reusing legacy behavior in future frontend modes would require extracting logic from monolithic state machines rather than composing smaller UI components.

Assessment: the rebuilt path is architecturally stronger and should remain canonical. The legacy path is best treated as a semantic mine for missing affordances, not a structure to preserve.

### Ease of use

Rebuilt strengths:

- The flow is more discoverable: Scenario Selection offers Play/Edit, Scenario Edit exposes preview/player/entities/action plans, Entity Template Edit exposes presentation/targeting/inventory, and Action Plan Edit has its own screen.
- Keyboard/controller behavior is consistent: no focus means arrows choose components, Enter focuses, focused components consume arrows/Enter, and Esc releases focus or returns.
- Field editing uses focused overlays with concise prompts; risky/branching interactions use popup overlays instead of hidden key chords.
- The new default Action Plan picker matches author expectations better: selecting a plan and editing a plan are separate actions.
- Targeting UX is now more honest: labels are read-only requirements from the selected/default Action Plan, and unconfigured requirements tell the user what still needs assignment.

Rebuilt ease-of-use gaps:

- Inventory contents/layout editing now has a first cursor-grid implementation; large-grid navigation polish, mouse controls, undo, and advanced replacement rules remain deferred.
- Action Plan editing now supports insert/replace/delete/reorder for canonical behavior-chain steps; parameter/check/effect editing remains deferred.
- Save/dirty affordances are surfaced through the Scenario Edit status panel, S hotkey, and 2.5 unsaved-exit modal; saving is currently treated as the preview-refresh/rematerialize boundary.
- Scenario/player-start editing remains mostly review-only in the rebuilt Scenario Edit screen.
- Per-instance initial facing is intentionally deferred as a capability gap: the desired model is inventory-owned per carried instance state, while templates keep hidden/default state not exposed as a primary entity-template editor field.

Legacy/prototype ease-of-use strengths:

- The legacy context exposed more keyboard shortcuts for power-user mutation flows.
- Legacy inventory brush and action-step editing proved useful service-backed mutation flows that the rebuilt screens have now largely recaptured with clearer structure.

Legacy/prototype ease-of-use weaknesses:

- Hidden command/menu states and key chords were harder to discover and harder to explain in footer text.
- Multiple editor modes/modals in the same shell made it easy for the user to be unsure whether arrows were moving a screen selection, a field, a brush, an action-step row, or a picker.

Assessment: the rebuilt path is easier to learn and evaluate, and now covers the core inventory/action-plan authoring loops; advanced action-step parameter/check/effect editing remains out of scope for this checkpoint.

### Modularity/extensibility

Rebuilt strengths:

- Components and overlays are reusable across screens and gallery-testable.
- `FocusRouter` gives a small shared model for the cross-screen focus contract.
- `SadConsoleTheme` centralizes color/glyph/border choices and keeps visual language consistent.
- Screen models are unit-testable without launching SadConsole, which makes iterative UX changes much safer.
- The service-backed mutation boundary should allow future frontends to share semantics while presenting different UX shells.

Rebuilt extensibility risks:

- Future advanced inventory/action-plan features should continue as dedicated subcomponents/subscreens rather than accumulating as branches inside existing screen classes.
- Typed command/action descriptors may become necessary before adding many more editable fields, especially action-step parameter editors.
- The frontend still needs a reusable pattern for mutation result handling: refresh snapshot, preserve selection, mark preview stale, and report status.

Legacy/prototype modularity strengths:

- The legacy code proves that the editor services have enough capability for several missing rebuilt semantics.
- The intermediate exploration model proved reusable visual panel concepts before the current component library existed.

Legacy/prototype modularity weaknesses:

- Behavior is not naturally reusable because it is bound to a specific shell/context state machine.
- Adding a new screen or editing mode tends to increase branching in central classes instead of composing smaller pieces.

Assessment: the rebuilt model is substantially more extensible. The next implementation work should preserve that advantage by extracting inventory/action-plan editing as composable modules rather than cloning legacy state-machine branches.

### Parity conclusion

The rebuilt UI is at or above the legacy/prototype direction for visual grammar, navigation consistency, service-backed entity-template presentation/default-plan/targeting edits, and testability.

Remaining major semantic gaps before claiming full MVP editor parity:

1. Scenario/player-start editing should be reviewed against the intended MVP definition and likely needs shared editor API support.
2. Per-instance facing/state in inventory should be designed as a shared content/editor capability rather than as template-level UI.
3. Advanced Action Plan parameter/check/effect editing needs a designed screen 4 UX and typed frontend projection/mutation contract.

Recommended next checkpoint: review scenario/player-start editing needs, per-instance inventory state, and action-step parameter projection/mutation design.
