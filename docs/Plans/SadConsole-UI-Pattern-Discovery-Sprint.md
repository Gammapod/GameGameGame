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

The current `src/GameGameGame.SadConsole` project should remain the active SadConsole frontend. Do **not** start a separate replacement project unless the discovery sprint finds a hard framework or architecture blocker. The working catalog launch, editor-service integration, preview/materialization, Simulation launch/return, controlled-command Simulation mode, and tests are valuable foundations.

What should become legacy/replaced by the end of this sprint is the current Editor-mode presentation architecture, not the whole project.

### Should be deprecated as prototype/legacy

These pieces may remain temporarily as fallback/reference paths, but should not be extended as the durable editor UI pattern:

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

### Phase 1: Custom-drawn layout prototype

Goal: prove the mockup can be reproduced with direct SadConsole surface drawing.

Tasks:

1. Add a `TemplateEditorLayout` pure layout model with region rectangles and field rectangles.
2. Add tests for region and field geometry.
3. Add a dedicated template-editor drawing path using `CellSurfaceEditor` drawing primitives.
4. Draw static panel boxes, field boxes, glyph card, targeting area, inventory placeholder, and footer.
5. Keep current mutation/navigation state behind it where possible.

Expected tests:

- `TemplateEditorLayoutTests`
- `TemplateEditorFieldBoxViewTests`
- `TemplateEditorFooterViewTests`

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

## Initial next action

Begin with Phase 1: implement a pure `TemplateEditorLayout` model plus a static custom-drawn template editor mockup path. This offers the fastest way to break out of row-list rendering while establishing testable geometry for later SadConsole control comparisons.
