# SadConsole Entity Panel Prototype

Status: Completed / archived experimental spike plan. Only the findings were carried forward to main; the old prototype project is not current source. See `docs/Plans/SadConsole-Spike-Findings.md` for the consolidated end-of-spike architecture findings.

Read when:

- reviewing the completed SadConsole frontend prototype findings;
- deciding whether near-term frontend experiments should stay in Console or move into SadConsole;
- recording UX findings that should inform future frontend engine choice.

Related source of truth:

- `docs/Source of Truth/Frontend-UX-Invariants.md` records broad frontend UX invariants.
- `docs/Source of Truth/Entity-Panel-UX-Spec.md` records the entity-panel UX model.
- `docs/Plans/Gamma-Frontend-Demo-Plan.md` records the current tester/demo frontend direction.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records implemented cross-layer capability support.

## Goal

Created a small SadConsole prototype that renders existing simulation state through entity panels, breadcrumbs, and action/log seed data, without committing SadConsole as the final frontend engine.

The spike is now stopped. The prototype findings remain useful reference material, but the prototype source should not be treated as current main-branch code. Future SadConsole implementation should start fresh as `src/GameGameGame.SadConsole` after shared frontend contracts are selected.

The proposed follow-up explorations were panel-specific log filters, a manifest/scenario selection menu, and itch.io HTML5/browser feasibility. The timebox is now closed. `docs/Plans/SadConsole-Spike-Findings.md` carries forward the post-spike decision inputs and architectural splits.

## Non-goals

- Do not choose the final frontend engine in this sprint.
- Do not add new gameplay mechanics.
- Do not modify checked-in game content.
- Do not modify Core, Content, Headless, or Editor semantics unless a missing capability is explicitly promoted and delegated.
- Do not implement editor tools yet.
- Do not polish final visual style, animation, or player-facing filtering.

## Ownership and boundaries

The old prototype code is not carried forward as current source. Future frontend-owned SadConsole code should live under a fresh `src/GameGameGame.SadConsole` project when implementation resumes.

The prototype should consume existing shared contracts, especially scenario materialization, entity inspection panels, containment paths, local turn-order reports, and turn traces. Missing contracts should be recorded as findings rather than patched around with frontend-only simulation behavior.

## Expected 4-5 turn shape

1. **Turn 1: setup and smoke**
   - Create this plan.
   - Add a prototype project.
   - Add SadConsole dependency and project references.
   - Verify a minimal SadConsole app can build, or record setup blockers.
2. **Turn 2: first entity panel render**
   - Load/materialize one existing scenario.
   - Render the player/current playspace as an entity panel with an inventory grid.
3. **Turn 3: inspection interaction**
   - Add selected/inspected entity panel.
   - Navigate/select contained entities and inspect them.
4. **Turn 4: breadcrumbs and turn log seed**
   - Render containment breadcrumbs.
   - Add a primitive universal turn-log area from existing reports/traces.
5. **Turn 5: consolidation and findings**
   - Make the prototype runnable enough for follow-up.
   - Record UX findings, missing data contracts, and SadConsole friction.

## Initial prototype requirements

Minimum useful prototype:

- starts as a standalone experimental frontend project;
- loads or materializes a known scenario using existing Content/Core services;
- renders at least one entity panel from `EntityInspectionPanel` data;
- renders a grid with glyph/color cells;
- shows basic entity status such as name, ID, address/location, bulk/aperture, inventory dimensions, facing, and target where available;
- keeps all simulation behavior in shared services.

Stretch goals:

- second inspected entity panel;
- keyboard cursor/select inspection;
- breadcrumb display from `EntityContainmentPathService`;
- local turn-order rows;
- basic turn-log rows from existing turn reports;
- simple facing/target markers.

## Questions to answer

- How difficult is SadConsole setup in this repository?
- Does SadConsole fit the entity-panel/grid/log mental model?
- What minimum collapsed entity-panel summary feels useful?
- How much panel state management should be nested expansion versus separate panes?
- Which current Console/headless-recorder display ideas transfer cleanly?
- What data needed by the entity-panel spec is missing or inconvenient to consume?
- When should a future prototype move to Godot or another richer 2D frontend?

## Historical run status

The completed spike had a runnable prototype with direct scenario launch on the spike branch. Those run commands are intentionally not carried forward here because the prototype project source is not current main-branch code. Future runnable SadConsole work should be created as a fresh `src/GameGameGame.SadConsole` project after shared frontend contracts are selected.

## First-pass outcome

The first prototype pass validated SadConsole as a useful near-term spike surface for the entity-panel UX model. The prototype can:

- load the default alpha scenario or a directly specified content file/scenario ID;
- materialize scenarios through existing Content/Core services;
- render a current playspace entity panel and an inspected entity panel;
- render entity status fields, inventory/play-space grids, glyphs, colors, facing, target, and containment paths;
- move a keyboard cursor through the current playspace grid;
- inspect an occupied cell and update the inspected entity panel;
- show a first universal-turn-log area seeded by local turn order when no turn report exists.

This is enough to continue exploring entity panels, breadcrumbs, selection, and debug layout without choosing a final frontend engine.

## UX findings

- Entity panels translate well from the existing Console inspection contract to SadConsole.
- Treating the current playspace as an entity panel feels viable: the current container can be a panel source for map-like navigation instead of requiring a separate map concept immediately.
- Keyboard selection in a space-owning panel is enough to test the inspect-arbitrary-entity loop.
- Breadcrumbs belong near entity identity, but full breadcrumb interaction remains future work.
- The universal turn log needs two modes: chronological action history after turns have occurred, and local/contextual order summaries before or beside turn history.
- Direct scenario launch is more useful for this spike than building a scenario menu immediately.
- Large or dense spaces will require clipping, scrolling, scaling, minimaps, or alternate collapsed summaries.
- The two-panel layout is useful for early testing but will not answer multi-panel collapse/pin/chain questions by itself.

## Technical findings

- SadConsole and `SadConsole.Host.MonoGame` work in a `net10.0` prototype once `MonoGame.Framework.DesktopGL` is referenced explicitly.
- The prototype should remain isolated from `GameGameGame.Console`; it is a separate frontend experiment, not a replacement yet.
- UI drawing code is already doing too much direct service querying. A future iteration should introduce a frontend-owned view-model adapter that precomputes entity panel, breadcrumb, grid, cursor, and log rows.
- Build validation is straightforward, but run validation is interactive/windowed and should not be treated as a normal non-interactive test command.
- `EntityInspectionPanel`, `EntityContainmentPathService`, and `LocalTurnOrderReport` are immediately useful as frontend-facing contracts.

## Missing or deferred capabilities

- No player movement or turn advancement in SadConsole yet.
- No chronological turn log because the prototype does not execute turns.
- No expandable/collapsible panel tree, pinned panels, multi-panel chains, or breadcrumb selection.
- No mouse interaction.
- No in-grid facing/target markers yet, even though the headless recorder has visual conventions for them.
- No scenario catalog/manifest menu; only direct launch and default fallback.
- No panel scrolling or viewport handling for large spaces.
- No frontend view-model adapter or tests around panel/log projection.
- No player-facing filtering rules; everything remains debug-first.
- No editor UX prototype.

## Recommended next slice

Superseded recommendation: this section recorded the pre-close recommendation for a **SadConsole shareable feedback build**. The current recommendation is to plan shared frontend-facing service contracts before more SadConsole feature work.

Small scope:

1. Split the current universal log into panel-specific filtered logs while retaining access to the full chronological trace.
2. Add a SadConsole manifest/scenario menu using the existing scenario catalog/manifest concepts so testers do not need command-line arguments.
3. Investigate an itch.io browser/HTML5 distribution path for the current SadConsole/MonoGame stack and implement it if feasible.
4. If browser export is blocked by the current stack, document the blocker, provide the best shareable fallback build, and treat browser support as a frontend technology decision risk.
5. Keep player controls and entity-panel inspection stable enough for feedback; defer nonessential polish until the shareable path exists.

Exit criteria:

- The three explorations above are implemented or explicitly documented as blocked.
- Findings are recorded with enough detail to support a decision about SadConsole's future role.
- No additional SadConsole feature work is started before that decision, except small fixes required to complete the three explorations.

Decision options after the timebox:

- **Replace Console as debug/prototype frontend**: choose this only if SadConsole covers the needed debug workflows and shareable packaging without creating unacceptable maintenance friction.
- **Stay alongside Console as main shareable frontend**: choose this if SadConsole is better for feedback/testers but Console remains valuable as a minimal fallback and regression/debug tool.
- **Postmortem/R&D findings only**: choose this if SadConsole validates UX ideas but fails key delivery constraints, especially HTML5/browser distribution.

Keep deferred:

- editor tools;
- final engine choice;
- polished visual style;
- complex panel docking/dragging/collapse semantics.

## Shareable feedback build requirements

Minimum useful external-feedback build:

- starts without command-line arguments and presents a scenario selection menu;
- reads curated scenarios through the shared scenario catalog/manifest flow instead of duplicating content-discovery rules;
- preserves direct scenario launch for developer testing;
- supports keyboard-first play and inspection through the existing panel chain;
- shows per-panel filtered logs under or inside entity panels, grouped from shared turn-report/local-order data rather than frontend-only simulation events;
- offers a visible way to inspect the full/global chronological log when panel filters hide context;
- has a documented packaging command for the shareable build;
- has either an itch.io browser/HTML5 build pipeline or a written feasibility note explaining why the current SadConsole/MonoGame stack cannot produce one yet.

### HTML5/browser packaging risk

The current prototype targets `net10.0` and explicitly references `MonoGame.Framework.DesktopGL`. That is a desktop OpenGL host path, not an established browser export path. MonoGame's public supported platform list covers Windows/macOS/Linux DesktopGL, WindowsDX, Android, iOS/iPadOS, and closed console platforms; it does not list an official browser/HTML5 target. Before promising itch.io browser play, verify whether SadConsole's MonoGame host can run on a supported WebAssembly/HTML5 backend in this project. If it cannot, do not hack around engine/runtime limits in the frontend; record the blocker and compare alternatives such as a downloadable itch.io build, a separate web-capable frontend shell, KNI/FNA-style ports, Godot, or another stack.

## Continued experiment findings

- View-model adapter slice: Added a small frontend-owned view-model layer (`PrototypeViewModels.cs`) with `PrototypeView`, `EntityPanelView`, `EntityGridView`, and `TurnLogView`. Rendering now consumes these views instead of directly querying containment paths, action state, target names, and local turn order in the SadConsole drawing methods. Behavior is intended to remain unchanged: cursor movement, direct scenario launch, inspected-panel updates, breadcrumbs, and turn-log seed still work. This makes the next player-action/turn-log slice safer because simulation state can change, then the view model can be rebuilt for display.
- Interactive turn/debug slice: Added WASD player movement through existing `TurnService.TakeActorTurnThenAdvance` with a direct `MoveAction`, while leaving arrow keys for inspection-cursor movement. After player movement, the prototype refreshes current-container and inspected panels, recenters the cursor on the player when the player remains in the current panel, and the universal turn-log area switches from local-order-only seed data to chronological `LastTurnReport` action rows. This validates the planned debug loop: player action -> shared simulation turn -> rebuilt frontend view model -> entity panels and universal log update from shared reports.
- Panel-chain slice A: Replaced the fixed two-panel render with a `PanelChainView` built from the inspected entity's containment path. The prototype now renders the breadcrumb trail as panels from left to right, currently capped to the first four visible panels while retaining the full chain in the view model. The current playspace/root panel still receives the inspection cursor when it is part of the inspected path, and Enter inspection changes the panel chain source. This confirms the core breadcrumb-as-panel-chain concept, but it also exposes the next layout problem: as more panels are visible, each panel needs collapsed/expanded states and likely horizontal scrolling or focus to remain readable.
- Panel-chain slice A.5: Added focused-panel navigation with `Tab` and per-panel cursors. Arrow keys now move the cursor inside the focused rendered panel instead of only the player's current plane, and Enter inspects an occupied cell in that focused panel. This allows inspecting entities rendered in nested inventory panels without requiring mouse support yet. The focused panel uses a brighter border. This is a useful bridge toward mouse selection and toward action controls that need different source/target panels, but it also shows that focus, selection, and action-target state should become explicit view-model/session concepts before pickup/drop/enter/exit grow complex.
- Expand/collapse slice B: Added per-panel collapsed state toggled with `Space`. Collapsed panels render as compact breadcrumb cards while expanded panels keep the existing status/grid details. The panel-chain renderer now allocates narrow widths to collapsed panels and divides remaining width among expanded panels, which makes longer breadcrumb trails more readable without introducing scrolling yet. Focus remains visible through a bright border. Validation used a temp `--artifacts-path` build because an interactive prototype window can lock the normal apphost output while running.
- Action-control slice C: Added first-pass player controls for pickup, drop, enter, and exit using existing Core `PickupAction`, `DropAction`, `EnterAction`, and `ExitAction`. `P` picks up the focused occupied cell into the first empty player inventory slot; `O` drops the focused carried entity to the current playspace cursor coordinate; `E` enters the focused occupied entity; `X` enters an exit-direction mode where WASD chooses the exit direction. The prototype evaluates actions before taking a turn so failed pickup/drop/enter/exit attempts report a message without advancing the simulation. This is intentionally rough but confirms that panel focus/cursors can drive action selection across different visible panels.

## Findings log

Record findings here during the spike.

- Turn 1: SadConsole setup was mostly straightforward. A new `net10.0` prototype project builds with `SadConsole` and `SadConsole.Host.MonoGame` version `10.10.1`, references Core/Content/Headless, and runs a minimal window after adding the explicit `MonoGame.Framework.DesktopGL` `3.8.4.1` package. The host package alone did not copy/load `MonoGame.Framework`, causing `FileNotFoundException: MonoGame.Framework, Version=3.8.4.1` until the DesktopGL package was added. Minor API friction: `SadConsole.Console` conflicts by name with `System.Console`, so prototype code should qualify or alias it. Running from the shell remains an interactive/windowed app and will appear to hang in non-interactive automation unless the window is closed or the command is timeout-limited.
- Turn 2: The prototype now materializes `alpha-smoke` from `src/GameGameGame.Content/AlphaScenarioContent.yaml` and renders two static entity panels from existing `EntityInspectionPanel` data: the current playspace/container and the player. The render uses existing inspection grids, glyph/color presentation, high-signal entity properties, `Facing`, and `Target`. This confirmed that the current Console inspection contract transfers cleanly into SadConsole for static panel rendering. A first diagnostic UX issue appeared when using older `Test_Content.yaml`: legacy content validation diagnostics blocked panel rendering because materialization was not considered playable. The prototype now uses the cleaner alpha scenario content for the happy-path panel spike; future debug UI should still distinguish blocking setup failures from non-blocking diagnostics.
- Turn 3: Added a keyboard-driven cursor in the current playspace panel and a separate inspected entity panel. Arrow keys move the highlighted cell, Enter inspects an occupied cell by rebuilding the inspected panel from `EntityInspectionService`, and Esc closes the prototype. This starts to validate the entity-panel navigation loop: a playable/space-owning entity panel can act as the selection source for opening another arbitrary entity panel. Implementation friction: moving from top-level statements to a real `Program` class made the SadConsole screen subclass and renderer easier to share, and the mutable inspected-panel state should stay prototype-only until a cleaner frontend view-model shape is designed.
- Turn 4: Added breadcrumb text to each entity panel using `EntityContainmentPathService.GetUpwardPath`, plus a primitive universal-turn-log area. Because the prototype does not advance simulation turns yet, the log area currently shows that there is no `LastTurnReport` and falls back to local turn order for the current playspace via `LocalTurnOrderReport`. This clarified a useful UX distinction: breadcrumbs belong close to entity identity, while the universal log needs both chronological action entries and local context when no turns have been taken. It also reinforced that a future frontend view model should precompute compact path/log strings instead of letting UI drawing code directly query multiple services.
- Turn 4 follow-up: Added direct launch support for `<content-file> <scenario-id>` so richer scenarios can be tested without implementing a scenario menu. No arguments still fall back to `AlphaScenarioContent.yaml` / `alpha-smoke`; malformed prototype arguments add a diagnostic and fall back to the default. This is enough for spike exploration and keeps full manifest/menu support deferred.
- Turn 5: Consolidated first-pass findings. The spike met its core goal: SadConsole can host a frontend-owned experimental surface over existing scenario materialization, entity inspection, breadcrumbs, and local turn-order contracts. The most important follow-up is not more layout polish; it is adding a small frontend view-model adapter and enough player-action/turn execution to populate the universal turn log with real chronological entries.
