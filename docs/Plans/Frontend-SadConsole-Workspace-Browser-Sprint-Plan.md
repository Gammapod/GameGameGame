---
id: plan.frontend-sadconsole-workspace-browser-sprint
title: Frontend SadConsole Workspace Browser Sprint Plan
kind: plan
status: active
truth_rank: 45
truth_domains: [planning-priority, frontend-presentation, implementation-navigation, test-trace]
owners: [frontend-owner, core-owner]
audience: [frontend-owner, core-owner, content-editor]
read_when:
  - creating or implementing the new GameGameGame.Frontend.SadConsole project
  - adding workspace-aware scenario catalog or launch APIs for frontend consumption
  - establishing tests in tests/GameGameGame.Frontend.SadConsole.Tests
related:
  - source.frontend-ux-invariants
  - source.frontend-ux-standards
  - source.frontend-ux-decisions
  - source.testing-charter
  - source.invariants
  - source.engine-editor-capabilities
  - source.vertical-slice-map
  - plan.multi-document-content-workspace-compiler-sprint
  - plan.sadconsole-frontend-roadmap
---

# Frontend SadConsole Workspace Browser Sprint Plan

Status: Active spike-branch sprint plan for starting the new SadConsole-based frontend project and proving workspace-backed scenario browsing before rebuilding Play mode.

## Goal

Create a new frontend application:

```text
src/GameGameGame.Frontend.SadConsole
tests/GameGameGame.Frontend.SadConsole.Tests
```

The first checkpoint is a bootable fullscreen/drawable-bounds shell that shows available workspace scenarios, especially the multi-file `debug-room` scenario defined by `src/GameGameGame.Content/Debug/DebugRoom.yaml` and its canonical dependencies.

After that checkpoint, begin a clean new Play-mode surface over workspace-backed playable sessions. Copy intent and proven reusable components from the existing SadConsole frontend, but do not copy the legacy Debug/Edit shell flow.

## Directional decisions

1. `GameGameGame.Frontend.SadConsole` is the new active frontend experiment and intended successor surface.
2. `GameGameGame.SadConsole` remains buildable/reference-only until useful components, controllers, tests, glyph decisions, and display lessons have been mined.
3. The new project must not reference the old `GameGameGame.SadConsole` project. Copied code must be deliberate, renamed, simplified, and test-backed where it becomes stable.
4. SadConsole remains the frontend technology for this sprint. Do not pivot to Godot or revive Console-specific workflows.
5. Do not create an API v2. Add/evolve a Content-owned, frontend-neutral workspace scenario catalog/launch facade for multi-file content.
6. Debug mode is not a first-class route in the new frontend. Developer diagnostics may exist as explicit overlays/toggles, but should not define the player-facing UX.
7. Editor mode is deferred until the new scenario browser and Play surface exist. Future Editor work should be reinvented around workspace/editor services, not copied from the old layout.

## Scope

### Checkpoint 1: workspace scenario browser

Required outcome:

```text
dotnet run --project src/GameGameGame.Frontend.SadConsole/GameGameGame.Frontend.SadConsole.csproj
```

boots the new frontend, establishes display settings and drawable bounds, loads/list scenarios through a workspace-aware shared Content path, and shows `debug-room` in a navigable scenario list. Existing single-file scenarios should remain visible where feasible through compatibility entries.

### Checkpoint 2: first Play surface

Selecting a scenario, especially `debug-room`, launches through a workspace-aware Content/Core path and renders the initial clean Play surface. The first surface may be minimal, but it must be player-facing and must not copy the old Debug/Edit shell.

## Non-goals

- Do not delete or archive `src/GameGameGame.SadConsole` during this sprint.
- Do not make the new frontend depend on the old SadConsole project.
- Do not port current Debug mode.
- Do not port current Editor mode.
- Do not implement live content editing or runtime debug mutation.
- Do not make the frontend own scenario discovery semantics, workspace composition, materialization, player insertion, action legality, turn advancement, diagnostics classification, or YAML mutation.
- Do not overdesign settings menus or audio before the frontend can browse and launch scenarios.

## Core/Content API work

Owner: core-owner, with frontend-owner as consumer/reviewer.

### Desired service shape

Add or evolve a Content-owned, frontend-neutral service/facade that can:

1. build/load a `ContentWorkspace` from project content inputs suitable for frontend browsing;
2. compile/validate the workspace and preserve diagnostics as structured data;
3. list playable scenario entries/sections with stable IDs, display metadata, source/provenance, and compatibility classification where available;
4. launch a selected scenario through the workspace-aware path, probably wrapping `PlayableScenarioLauncher.CreateFromWorkspace(...)`;
5. preserve existing single-file scenario compatibility where feasible.

Suggested names, not mandated: `WorkspaceScenarioCatalogService`, `PlayableScenarioCatalogService`, `WorkspacePlayableScenarioService`, or `ContentScenarioLaunchService`.

### API guardrails

- The service must not be SadConsole-shaped.
- The `DebugRoom.yaml` path must not be launched through file-local `CreateFromFile(DebugRoom.yaml, "debug-room")`, because the debug room depends on canonical multi-file content.
- Existing `CreateFromFile(...)`, `CreateFromDocument(...)`, and `CreateFromCatalogEntry(...)` paths may remain compatibility adapters.
- Frontend consumers may display Content DTOs and diagnostics, but must not reclassify or reinterpret diagnostics as semantic policy.

### Content/Core TDD trace

Affected source-of-truth invariants: `docs/Source of Truth/invariants.md`:

- Content Pipeline: persisted scenario definitions materialize through the shared content materialization path and playable frontend sessions launch through a shared Content-level session launcher.
- Scenario catalog/selection contract: scenario catalogs list/discover/load curated entries and launch through shared session services.

Existing traces to preserve/run include:

- `ScenarioCatalogListsScenariosFromDocument`
- `ScenarioCatalogDiscoversFolderAndRoundTripsManifest`
- `ScenarioCatalogLoadsCuratedManifestSectionsAndEntryMetadata`
- `ScenarioCatalogValidationReportsCuratedManifestIssuesAndUnclassifiedCandidates`
- `ScenarioCatalogScanServiceDiscoversFolderAndWritesManifest`
- `PlayableScenarioLauncherBuildsFreshSessionFromCatalogEntry`
- `PlayableScenarioLauncherBuildsFrontendNeutralSessionFromPersistedScenario`
- `PlayableScenarioLauncherBuildsSessionFromWorkspaceScenario`
- `DebugRoomWorkspaceScenarioMaterializesAndRuns`

New or updated tests to consider before production changes:

- `WorkspaceScenarioCatalogDiscoversDebugRoomScenario`
- `WorkspaceScenarioCatalogListsSingleFileScenarioCompatibilityEntries`
- `WorkspaceScenarioLaunchCreatesPlayableSessionFromDebugRoom`
- `WorkspaceScenarioLaunchReturnsDiagnosticsWithoutFrontendSemantics`
- `WorkspaceScenarioLaunchUsesPlayableScenarioLauncherCreateFromWorkspace`

## New frontend project work

Owner: frontend-owner.

### Project skeleton

Create:

```text
src/GameGameGame.Frontend.SadConsole
tests/GameGameGame.Frontend.SadConsole.Tests
```

Initial responsibilities:

- SadConsole/MonoGame host bootstrap;
- frontend-owned settings model and persistence defaults;
- fullscreen/window/display startup shell;
- tile/display profile defaults;
- drawable bounds calculation;
- root screen/screen stack seam;
- scenario browser screen model and renderer;
- thin shared-service adapter for workspace scenario list/launch requests.

### Settings MVP

Frontend-owned app settings may include fullscreen/window mode, resolution/display mode, tile scale/profile, input mode, last selected catalog/scenario, and future audio settings.

Settings are app/presentation state only. Core and Content must not own user display/input/audio preferences. Persistence path and defaults are owned by the new frontend app, with a likely durable destination under per-user app data such as `GameGameGame/Frontend.SadConsole/settings.json`. A spike-local fallback is acceptable if documented as temporary.

### Scenario browser UX

Temporary root screen:

```text
Scenario List
  - debug-room
  - existing scenarios, when available

Footer/context controls:
  Up/Down: Move
  Enter/Select: Select
  Esc/Cancel: Back/Exit
```

Keyboard can be implemented first, but every stable player-facing UX decision should preserve a path for keyboard, mouse, and gamepad modes. The browser should use directional navigation plus Select/Cancel as the primary control grammar.

### Frontend component/testing trace

New test home:

```text
tests/GameGameGame.Frontend.SadConsole.Tests
```

The new project starts with TDD-friendly frontend seams: pure settings/default resolution, pure display/drawable-bounds layout, pure scenario browser view models and selection state, pure component/gallery screen models, pure Play-surface layout over shared `PlayableScenarioSession`/projection DTOs, and thin renderer/input adapters kept manual-smoke-tested.

Frontend tests assert that the frontend maps user selection and input to shared Content/Core requests and renders returned DTO/session data. They must not assert duplicate materialization rules, player insertion rules, action legality, turn advancement, YAML mutation semantics, command outcomes, or diagnostics classification. Those belong in Core/Content tests.

Suggested first frontend tests:

- `FrontendSadConsoleSettingsLoadsDefaultsWhenNoPersistenceExists`
- `FrontendSadConsoleDisplayShellResolvesDrawableBoundsInsideChrome`
- `ScenarioBrowserShowsWorkspaceDebugRoomCatalogEntry`
- `ScenarioBrowserSelectionCreatesLaunchRequestWithoutLaunchingSemantics`
- `ScenarioBrowserPreservesDiagnosticsAsDisplayData`
- `PlaySurfaceBuildsFromPlayableScenarioSession`
- `PlaySurfacePlacesComponentsInsideDrawableBounds`
- `PlaySurfaceInputModeRoutesNavigationWithoutOwningActionLegality`

## Component/gallery mining

Mine deliberately from `src/GameGameGame.SadConsole` and `tests/GameGameGame.SadConsole.Tests`.

Good candidates include display settings/layout models, fullscreen and drawable-bounds lessons, themes/colors/glyph-role mappings, componentized screen-model patterns, inventory-space view models/renderers, gallery examples for proven components, and input abstraction ideas that are presentation-only.

Do not copy monolithic shell flow, Debug mode as a route, old Editor layout, hardcoded prototype player/plane IDs, direct gameplay execution policy, or ad-hoc YAML mutation/discovery semantics.

The new gallery should be rewritten as a human-readable pattern browser rather than a surface that draws examples over one another:

```text
left: component/example list
right: selected example preview
bottom: contextual controls
```

Reusable SadConsole patterns accepted into the new project should receive a gallery example and focused tests once stable enough to pin.

## First Play-mode surface

Start only after Checkpoint 1 is complete.

Initial goal:

```text
Scenario Browser
  -> select debug-room
  -> workspace-backed playable session
  -> clean Play surface
```

The first Play surface should preserve current accepted standards: fullscreen starts from app boot; drawable content stays inside resolved drawable bounds; square tile rendering remains the baseline; controlled actor/current place/current space are rendered from shared session/projection data; action input routes through shared Action Choice/session services; optional debug overlays are explicit and presentation-only.

## Acceptance checkpoints

### Checkpoint 1 acceptance

- New project and test project build.
- New project has no reference to old `GameGameGame.SadConsole`.
- App settings defaults load.
- Display shell resolves drawable bounds.
- Scenario browser consumes workspace-aware Content service/facade.
- `debug-room` is visible.
- Existing single-file scenarios are visible where feasible or clearly reported as compatibility/follow-up.
- Frontend tests cover settings, drawable bounds, and browser selection without asserting Content/Core semantics.

### Checkpoint 2 acceptance

- Selecting `debug-room` launches a workspace-backed `PlayableScenarioSession` or equivalent shared session result.
- Initial Play surface renders inside drawable bounds.
- No old Debug/Edit shell is copied.
- Frontend tests cover Play-surface layout/presentation only.
- Core/Content tests cover launch/materialization/action semantics.

## Manual smoke checks

Until richer UI automation exists, manually check boot/fullscreen or fallback, scenario list readability, `debug-room` visibility, keyboard directional navigation and Select/Cancel, diagnostic visibility on catalog/workspace failure, no old-gallery overlap after gallery porting, and Play launch/render once Checkpoint 2 begins.

## Key files and docs

Shared docs: `Frontend-UX-Invariants.md`, `Frontend-UX-Standards.md`, `Frontend-UX-Decisions.md`, `testing-charter.md`, `invariants.md`, `Engine-Editor-Capabilities.md`, `vertical-slice-map.md`, `Multi-Document-Content-Workspace-Compiler-Sprint-Plan.md`, and `SadConsole-Frontend-Roadmap.md`.

Content/API files: `src/GameGameGame.Content/ScenarioCatalog.cs`, `ScenarioCatalogScanService.cs`, `PlayableScenarioLauncher.cs`, `ContentCompilationTypes.cs`, and `ScenarioMaterializer.cs`.

Debug room content: `src/GameGameGame.Content/Debug/DebugRoom.yaml`, `src/GameGameGame.Content/Canonical/Creatures/DebugPlayer.yaml`, and `src/GameGameGame.Content/Canonical/Spaces/DebugRoomRoot.yaml`.

Reference only: `src/GameGameGame.SadConsole` and `tests/GameGameGame.SadConsole.Tests`.

## Risks and mitigations

1. Workspace catalog/launch API is not ready. Mitigation: core-owner owns a narrow Content facade before frontend overfits to file-local APIs.
2. Single-file and multi-file scenarios diverge. Mitigation: shared service normalizes entries and marks compatibility/follow-up status rather than making the frontend guess.
3. Old code drags old architecture into the new project. Mitigation: no project reference, copy only deliberate components, and keep stable seams test-backed.
4. Frontend tests encode engine semantics. Mitigation: new test trace explicitly limits tests to presentation, selection, layout, settings, and shared request mapping.
5. Display/settings work expands too far. Mitigation: defaults and drawable bounds are enough for Checkpoint 1; settings menu/audio are deferred.

## Implementation log

- 2026-08-12 Checkpoint 1 start: Added Content-owned `WorkspaceScenarioCatalogService` plus tests for discovering and launching `debug-room` through workspace-backed `PlayableScenarioLauncher.CreateFromWorkspace(...)`. Added new `src/GameGameGame.Frontend.SadConsole` and `tests/GameGameGame.Frontend.SadConsole.Tests` projects with no dependency on the old SadConsole project. Added frontend-owned settings defaults, display/drawable-bounds resolver, scenario browser screen model, and first SadConsole scenario browser console. The browser can list workspace/file scenarios and selecting a scenario currently verifies that a playable session can load; the actual Play surface remains next work.
- Verification: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj` passed 5 tests; `dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "WorkspaceScenarioCatalog"` passed 3 tests; `dotnet build GameGameGame.sln --no-restore` succeeded.
- 2026-08-12 layout/debug pass: Added scenario-browser layout resolution over drawable bounds, clipped list/status/footer rendering, Candii tileset blank-glyph loading, F12 layout debug state with red outer border and semi-transparent current-screen overlay text, and F11 best-effort fullscreen/windowed toggle. Added focused tests for tileset space mapping, browser layout bounds, debug overlay rows, and chrome state toggles.
- Verification: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj` passed 9 tests.
- 2026-08-12 display-shell hardening pass: Extracted `SadConsoleDisplayHost` for fullscreen/windowed mode application and changed F11 to replace the scenario browser root console with a newly resolved shell/layout after the display mode changes. F12 handling now accepts key pressed/released activation for layout debug. This is intended to make fullscreen change the logical cell surface and drawable bounds instead of only changing the OS window state.
- Verification: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj` passed 9 tests.
- 2026-08-12 scenario-list viewport pass: Added a pure `ScenarioBrowserViewport` model that keeps the selected scenario visible within the list area, reports above/below scroll state, and exposes a selected/total position summary. The scenario browser now renders only the visible viewport slice and includes a heading summary like `Available scenarios (13/64 | showing 4-45)`. F12 layout debug includes viewport range.
- Verification: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj` passed 11 tests.
- 2026-08-12 mouse scenario-browser pass: Enabled mouse input on the scenario browser. Mouse wheel moves the current scenario selection through the same presentation-only selection state as keyboard navigation, and left-clicking a visible row selects/requests launch for that row. Added screen-model tests for mouse scroll and click request mapping without adding launch/materialization semantics to frontend tests.
- Verification: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj` passed 13 tests.
- 2026-08-12 mouse polish pass: Reversed mouse-wheel direction to match expected list feel: scroll up moves selection down, scroll down moves selection up. Added hover-only scenario row state; hovering highlights a visible row and updates status text without changing selection or scrolling the viewport, while clicking remains the Select/launch-request action. Keyboard movement clears hover state.
- Verification: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj` passed 15 tests.
- 2026-08-12 input-mode pass: Added scenario-browser active input-mode presentation state. Keyboard commands switch the footer/debug status to Keyboard; mouse wheel, hover, and click switch it to Mouse. This is an intentionally lightweight first pass and not the final cross-device input-mode design.
- 2026-08-12 scenario action-selector pass: Added a gamepad input-mode placeholder command seam and changed scenario selection to open a Play/Edit selector instead of launching immediately. The selector shows read-only scenario metadata, a read-only turn-0 preview placeholder, Play as the default option, and Edit as a nonfunctional placeholder. Keyboard/gamepad Select on Play requests launch; Edit reports placeholder status. Mouse click on a visible row opens the same selector. F12 debug now reports selector open/closed state and selected option.
- Verification: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj` passed 18 tests.
- 2026-08-12 modal-border/graphics-foundation pass: Recorded Candii size-parity tileset-family decisions in frontend UX docs. Extended the new frontend tileset profile to load manifest-backed panel-border roles and added a reusable `PanelRenderer.DrawPanel(...)`. The Play/Edit selector now draws a Candii-role-backed border around its modal, with tests for border role loading and selector panel bounds.
- Verification: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj` passed 20 tests.
- 2026-08-12 overlay-surface pass: Added `OverlayPanelGeometry` with half-tile pixel offset and an `OverlayPanelConsole` child-surface renderer. The Play/Edit selector now renders as a separate child overlay surface offset half a tile right/down instead of redrawing modal tiles directly into the root scenario browser surface. Added tests for half-tile overlay pixel geometry.
- Verification: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj` passed 21 tests.
- 2026-08-12 first Play-surface pass: Added manifest-backed `gridDotted`/default-backdrop role loading, a pure `PlayGridViewModel` over workspace-backed `PlayableScenarioSession`, and a minimal `PlayModeConsole`. Selecting Play for `debug-room` now replaces the browser with a clean Play surface that renders the active room grid inside drawable bounds, draws the fallback backdrop glyph under every cell, draws the player glyph from Candii presentation mappings, and returns to the browser with Esc.
- Verification: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj` passed 23 tests.
- 2026-08-12 launch-failure diagnostics pass: Added `ScenarioLaunchFailurePresenter` and browser-side launch diagnostic display. If a selected scenario launches to `CanPlay=false`, the browser now keeps the user in the scenario browser and shows the first validation/runtime/capability diagnostic in the status line plus up to three launch diagnostics in the diagnostics area. If launch throws, the browser shows the exception as a launch failure instead of silently looking stuck. This is intended to make unsupported/deprecated Beta scenarios visibly fail while preserving Content/Core ownership of validation semantics.
- Verification: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj` passed 25 tests.
- 2026-08-12 toast notification pass: Replaced inline launch-failure diagnostics with a reusable toast-style notification state/presenter. Scenario launch failures now show an offset child overlay panel, using the same half-tile overlay pattern as the Play/Edit selector, and auto-dismiss after four seconds via the SadConsole render delta. The scenario browser keeps catalog diagnostics in the inline diagnostics area while launch warnings are transient overlay notifications.
- Verification: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj` passed 28 tests.
- 2026-08-12 modal focus hardening pass: Established that scenario-browser modal focus applies to all input methods. While the Play/Edit selector is open, mouse hover, scroll, and row click no longer change the underlying selected scenario or hover state; users must close/act on the modal before interacting with the scenario list again. Added screen-model tests for mouse focus isolation.
- Verification: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj` passed 30 tests, with the running frontend process still locking apphost/exe copy but not blocking the test run.
- 2026-08-12 interactive gallery start: Added the first new-frontend interactive component gallery route, reachable from the scenario browser with F2 and returning with Esc. The gallery currently includes executable examples for the offset selector popup and the four-second toast popup, with pure screen-model tests pinning the selector/modal focus behavior and toast example creation. Extended toast overlay presentation with `ToOverlayAt(...)` so gallery examples can reuse the toast pattern outside the scenario-browser layout.
- Verification: targeted new gallery/toast tests passed with temporary artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-gallery-new-tests --filter "ComponentGallery|ToastNotification" -m:1` passed 7 tests. Full normal-path frontend test verification was blocked by a running `GameGameGame.Frontend.SadConsole` process locking frontend output DLLs; an alternate-artifacts full run compiled but repo-path-dependent catalog tests did not find workspace content from the temp artifacts context.
- 2026-08-12 static layered Play renderer checkpoint: Added pure layered Play presentation types (`PlayCamera`, world/screen coordinates, explicit render layers, backdrop visuals, entity visual bundles, overlays, and render commands), a projector that composes layer-ordered visible commands through the camera, and a thin SadConsole renderer. Added a gallery example for a static layered room with backdrop cells, an actor sprite, entity-owned facing/status glyphs, an item sprite, and separate UX highlights. This establishes the rule that cells own backdrops while entities own sprite/accent/status layers, and that all rendering goes through a camera transform even for small rooms.
- Verification: targeted gallery/toast/layered-renderer tests passed with temporary artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-layered-renderer-tests --filter "ComponentGallery|ToastNotification|LayeredPlaySurface" -m:1` passed 12 tests. Normal-path full verification remains blocked until the running frontend process is closed; alternate-artifact full verification is not suitable for repo-path-dependent workspace catalog tests without adjusting test path resolution.
- 2026-08-12 move-animation preview checkpoint: Added a pure `PlayMoveAnimation` model and animated command projection that interpolates entity visual bundles from one world cell to an adjacent world cell while keeping entity-owned sprite/accent/status layers at the same fractional position. Added a `Move animation` gallery example. The visible SadConsole preview uses a pixel-positioned one-cell child console for the moving sprite over a static backdrop preview; this proves simple slide motion is possible without moving the cell/backdrop layer, while richer attached accent/status visuals will need a multi-layer sprite child surface in a later pass.
- Verification: targeted gallery/toast/layered-renderer/move-animation tests passed with temporary artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-move-animation-tests --filter "ComponentGallery|ToastNotification|LayeredPlaySurface" -m:1` passed 15 tests.
- 2026-08-12 animation-queue gallery checkpoint: Added `PlayAnimationStep` and `PlayAnimationQueuePlayback` as a pure frontend playback model over committed animation facts. The queue filters out steps that do not intersect the current camera, plays visible steps sequentially in initiative-like order, supports a speed scalar, exposes the active animated commands, and records that a final committed-state redraw is required after draining. Added a `Move animation queue` gallery example with two sequential slide moves.
- Verification: targeted gallery/toast/layered-renderer/animation-queue tests passed with temporary artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-animation-queue-tests --filter "ComponentGallery|ToastNotification|LayeredPlaySurface" -m:1` passed 20 tests.
- 2026-08-12 playable-movement checkpoint: Wired the new Play surface to shared Core `ControlledActorCommandService` for arrow/WASD movement. Successful one-cell moves keep the pre-move grid as the static animation base, hide the controlled actor from that base, slide a pixel-positioned sprite child surface from the old cell to the new cell, then redraw from the committed session world after animation drains. Failed moves stay in Play mode and report the shared failure detail. The movement adapter filters the controlled player's authored default action plan out of the automatic-plan dictionary so a direct player command does not also execute the player's compatibility plan in the same turn; future real turn playback should replace this with a shared history/animation-fact contract.
- Verification: targeted play-movement/layered-renderer/component-gallery/toast tests passed with temporary artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-play-move-broad --filter "PlayMovement|LayeredPlaySurface|ComponentGallery|ToastNotification" -m:1` passed 21 tests.
- 2026-08-12 movement-feel polish checkpoint: Split real Play movement timing from the slower gallery demonstration timing with `PlayAnimationSettings.Default` at 120ms, added pixel-art position snapping to sprite-pixel increments for pixel-positioned movement, and added a one-slot queued movement buffer. While a movement animation is active, pressing another movement key replaces the queued direction; when the animation drains and the final committed state is redrawn, the queued direction executes next.
- Verification: targeted play-animation-settings/play-movement/layered-renderer tests passed with temporary artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-move-polish-tests2 --filter "PlayAnimationSettings|PlayMovement|LayeredPlaySurface" -m:1` passed 14 tests.
- 2026-08-12 facing-indicator start: Loaded Candii facing role glyphs (`facingDiag` 251, `facingNS` 252, `facingWE` 253) into the new frontend tileset profile and added direction-to-mirror mapping for all eight directions. `PlayGridViewModel` now projects action-facing state into entity-owned facing decorator data, and `PlayGridRenderer` draws a SadConsole `CellDecorator` on occupied cells without replacing the entity glyph. This gives the Play surface the first movement-related entity state visualization.
- Verification: targeted tileset facing tests passed with temporary artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-facing-tileset-tests --filter "TilesetProfile" -m:1` passed 4 tests. Broader temp-artifact PlayGrid tests still hit the known repo-path-dependent `debug-room` catalog issue outside normal output.
- 2026-08-12 eight-direction movement preview checkpoint: Changed Play movement input to a unified preview-confirm model. Held numpad keys aim all eight directions using standard roguelike mapping (`7` NW, `8` N, `9` NE, etc.); held arrow/WASD cardinal combinations resolve to the same eight-way preview (`Up+Right`/`W+D` => NE). Space/Enter confirms the current movement preview, and the destination cell is drawn as a cyan UX highlight separate from entity identity/facing. Confirming during an active movement animation queues the latest confirmed preview direction for the next move.
- Verification: targeted movement-preview/play-movement/play-grid tests passed with temporary artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-8way-preview-tests --filter "MovementPreview|PlayMovement|PlayAnimationSettings|PlayGrid" -m:1` passed 16 tests.
- 2026-08-12 reusable cell-highlight start: Promoted move preview from replacing a cell glyph to a UI-layer `CellDecorator` highlight. Loaded Candii `moveHighlight` glyph 218 and added `CellHighlightPresentation.MovePreview(...)` with a semi-transparent cyan foreground and no background mutation. `PlayGridRenderer` now draws the move highlight over backdrop/entity/facing layers as a transparent overlay, establishing the same pattern future inspected/targeted/threatened highlights can reuse with different glyphs/colors.
- Verification: targeted tileset/movement-preview/play-grid tests passed with temporary artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-highlight-tests --filter "TilesetProfile|MovementPreview|PlayGrid" -m:1` passed 17 tests.
- 2026-08-12 movement-preview polish checkpoint: Movement aim now clears when movement keys are released/no longer held, removing the destination highlight. Confirming movement with Space/Enter and no active preview now falls back to the controlled actor's current facing direction, so a player can tap Move to continue forward. Added pure confirmation/key-recognition tests around the preview/facing fallback behavior.
- Verification: targeted movement-preview/play-movement/play-grid/tileset tests passed with temporary artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-move-preview-polish-tests --filter "MovementPreview|PlayMovement|PlayGrid|TilesetProfile" -m:1` passed 22 tests.
- 2026-08-12 facing-animation polish checkpoint: Animated player movement now attaches the facing indicator as an entity-owned accent/decorator on the pixel-positioned movement sprite, so the facing indicator travels with the player during the slide instead of remaining on the source cell until final redraw. Hidden entities in the static animation base also suppress their facing decorators, preventing stale source-cell indicators.
- Verification: targeted layered-play/movement/play-grid tests passed with temporary artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-facing-animation-tests3 --filter "LayeredPlaySurface|PlayMovement|PlayGrid|MovementPreview" -m:1` passed 28 tests.
- 2026-08-12 Play controller extraction checkpoint: Extracted `PlayInputController`/`PlayControlIntent` to convert SadConsole keyboard state into a standard Play control language (`AimMove`, `ConfirmMove`, `ClearMoveAim`, `Cancel`) and extracted `PlayMovementAnimationPresenter` to own the pixel-positioned movement sprite, animation playback, hidden-base-grid state, and attached facing decorator. `PlayModeConsole` is now mostly orchestration between input intents, shared movement commands, animation presenter, grid rendering, and status text.
- Verification: full frontend SadConsole tests passed with isolated artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-play-controller-full -m:1` passed 71 tests.
- 2026-08-12 nested-room Play start fix: Updated `PlayGridViewModel` to render the current controlled actor's containing plane first, rather than always rendering the scenario root/active container plane. This supports the revised `debug-room` shape where `debugStartRoom` is inside the scenario root and the player starts inside `debugStartRoom`. The grid model now records rendered plane/container identity, and movement animation startup no longer crashes if the requested source cell/entity is not present in the rendered grid; it clears/skips the transient animation and lets the committed-state redraw recover.
- Verification: full frontend SadConsole tests passed with isolated artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-nested-room-full -m:1` passed 71 tests.
- 2026-08-12 entity-inspection gallery start: Added a static `Entity inspection panel` gallery example and layout model. The panel uses a titled border, top portrait/status split, junction glyph roles from `panelBorder`, action-list rows including disabled/failure text, and an optional inventory preview area. The portrait reserves a 6x6 Candii cell region for a future 3x3 Candii16 playspace, and the inventory preview is bounded to 10x6 Candii cells to represent 5x3 Candii16 cells.
- Verification: targeted entity-inspection/component-gallery/tileset tests passed with isolated artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-inspection-panel-tests2 --filter "EntityInspectionPanel|ComponentGallery|TilesetProfile" -m:1` passed 16 tests.
- 2026-08-14 Candii16 inspection overlay checkpoint: Registered/copied `Candii16` assets in the new frontend, keeping `Candii` as the default font and loading `Candii16` through `FontConfig.AddExtraFonts(...)`, then replaced the entity-inspection panel's temporary quadrupled 8x8 portrait/inventory mock with pixel-positioned child `Console` overlays using the `Candii16` font. The parent 8x8 panel still owns chrome/text/layout and reserves 6x6/10x6 Candii-cell regions; child consoles draw the 3x3 portrait and 5x3 inventory playspaces over those reserved regions, using the active UI scale's SadConsole font-size preset. The overlay now requires a loaded 16x16 `Candii16` font rather than silently falling back to the default 8x8 font.
- Verification: targeted entity-inspection/component-gallery/tileset tests passed with isolated artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-candii16-overlay-tests3 -m:1 --filter "EntityInspectionPanel|ComponentGallery|TilesetProfile"` passed 17 tests. A full isolated frontend test run compiled but currently fails `PlayMovementControllerMovesDebugRoomPlayerThroughSharedCommandService`; that failure is outside the Candii16 overlay slice and needs separate movement/content fixture follow-up.
- 2026-08-14 Play inspection panel display-only start: Added a top-right Play inspection overlay using the same child-console, half-tile-offset, translucent-background pattern as selector/toast overlays, so the main Play grid continues to use the full drawable play area instead of being split into regions. The panel shows a sticky adjacent inspected entity: aiming at an occupied adjacent cell updates the panel, aiming at an empty cell or clearing aim keeps the last adjacent inspected entity, and the panel blanks when no entity is adjacent to the controlled actor. This first in-game integration is display-only; action focus/selection remains follow-up. The same entity-inspection renderer and Candii16 child overlay presenter are reused from the gallery.
- Verification: targeted Play inspection/entity-inspection/gallery/grid tests passed with isolated artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-play-inspection-overlay2 -m:1 --filter "EntityInspectionPanel|ComponentGallery|TilesetProfile|PlayInspection|PlayGrid|MovementPreview"` passed 37 tests.
- 2026-08-14 inspection portrait state checkpoint: Changed the inspection portrait from a centered inspected-entity icon to a real 3x3 gameplay snapshot around the inspected entity. The Candii16 portrait overlay now renders actual backdrop/entity cells from `PlayGridViewModel` and preserves facing decorators, so inspecting the push block shows nearby scrap and the player in their true relative positions.
- Verification: targeted Play inspection/entity-inspection/gallery/grid tests passed with isolated artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-play-inspection-portrait-targeted -m:1 --filter "EntityInspectionPanel|ComponentGallery|TilesetProfile|PlayInspection|PlayGrid|MovementPreview"` passed 39 tests.
- 2026-08-14 inspection action-choice rows: Replaced the display-only placeholder action row with rows projected from Core `ActionChoiceService` for the controlled actor's authored/default action plan, filtered to choices whose target/source/counterparty is the inspected entity. Enabled rows are selectable-styled and disabled authored affordances retain failure text. Core-owner confirmed this actor-centric `ActionChoiceService.CreateRequest(...)` seam is the correct contract; the current `debug-room` player authoring exposes Move/PickupTarget but not canonical Push, so the push block correctly shows disabled Pickup rather than Push until content authoring changes.
- Verification: targeted Play inspection/entity-inspection/gallery/grid tests passed with isolated artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-inspection-actions-tests2 -m:1 --filter "PlayInspection|EntityInspectionPanel|ComponentGallery|TilesetProfile|PlayGrid|MovementPreview"` passed 40 tests.
- 2026-08-14 inspection overlay performance pass: Avoided recomputing the inspection model/action-choice rows and redrawing the inspection child overlay on every animation frame when the inspected entity and overlay bounds have not changed. Movement preview updates are now change-aware, so holding a direction key no longer triggers repeated full redraws for the same aim direction.
- Verification: targeted movement/input/entity-inspection tests passed with isolated artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-inspection-animation-cache2 -m:1 --filter "MovementPreview|PlayInput|EntityInspectionPanel"` passed 24 tests.
- 2026-08-14 inspection text-message start: Added a small frontend text-message/resolver pattern and converted inspection stat/action rows from raw strings to `FrontendTextMessage` IDs plus args. The current resolver intentionally preserves prototype wording like `Aperture.text.id: {value}`, but action/stat text can now be changed in one catalog instead of by rewriting inspection model projection. `Frontend-Game-Text.md` records the inspection text slot convention.
- 2026-08-14 inspection portrait highlight pass: Inspection portrait cells now carry highlight state for the corresponding represented play-grid coordinate. The Candii16 portrait overlay renders the same move-preview highlight decorator used by the main playspace, so holding a direction highlights the matching cell in both the main grid and the inspection portrait when the portrait includes that coordinate.
- Verification: targeted inspection/highlight/input tests passed with isolated artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-inspection-portrait-highlight2 -m:1 --filter "FullyQualifiedName~InspectionPanelModelMarksHighlightedRepresentedCellInPortrait|EntityInspectionPanel|MovementPreview|PlayInput"` passed 26 tests.
- 2026-08-14 inspected-entity target focus start: Added a distinct entity-target highlight treatment for occupied movement-preview cells, using the Candii `entityHighlight` glyph with a provisional purple foreground to signal that Select will focus inspection actions instead of moving. Confirming while a movement preview points at an occupied non-player cell now switches Play input focus to the inspection action rows; Up/Down changes the selected action row, Esc/Left returns to grid focus, and confirming an action currently reports that execution is not wired yet.
- Verification: targeted tileset/inspection/input tests passed with isolated artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-entity-target-highlight2 -m:1 --filter "TilesetProfile|EntityInspectionPanel|MovementPreview|PlayInput|FullyQualifiedName~InspectionPanelModelMarksHighlightedRepresentedCellInPortrait"` passed 32 tests.
- 2026-08-14 inspection refactor checkpoint: Split inspection projection into `InspectionPortraitProjector` and `InspectionActionChoiceProjector`, introduced explicit `PlayHighlightState`, and extracted `PlayInspectionController` to own Play inspection focus state, selected action row, model/overlay caching, and overlay redraw decisions. `PlayModeConsole` now delegates inspection state/projection/overlay responsibilities while keeping movement and animation orchestration.
- Verification: targeted frontend tests passed with isolated artifacts: `dotnet test tests/GameGameGame.Frontend.SadConsole.Tests/GameGameGame.Frontend.SadConsole.Tests.csproj --artifacts-path C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-inspection-refactor3 -m:1 --filter "TilesetProfile|EntityInspectionPanel|MovementPreview|PlayInput|PlayGrid|LayeredPlaySurface|ComponentGallery"` passed 49 tests.

## Friction log

- 2026-08-12: The new project's namespace `GameGameGame.Frontend.SadConsole` collides syntactically with the external `SadConsole` namespace in a few type references, and SadConsole surface-clearing APIs differ from memory. Mitigation: use `global::SadConsole...` where needed and keep rendering code thin/simple until reusable renderer patterns are deliberately ported from the old project or promoted through the new gallery.
- 2026-08-12: Attempting to replace the root `ScenarioBrowserConsole` immediately after `Game.Create(...)` but before `Game.Instance.Run()` caused SadConsole `ScreenSurface` construction to throw a `NullReferenceException`; constructing the same console inside `SetStartingScreen(...)` is safe. Mitigation: removed pre-run root replacement. Startup fullscreen resizing remains a follow-up for a SadConsole lifecycle-safe hook; F11 during the running app remains the current resize experiment.
- 2026-08-12: Frontend test verification for the input-mode pass was blocked because a manually running `GameGameGame.Frontend.SadConsole` process held the frontend output DLL/EXE. A temporary-output test build compiled, but VSTest did not complete cleanly from the alternate output path. Mitigation: close the running frontend before normal test verification; keep future test runs on the normal output path when possible.
