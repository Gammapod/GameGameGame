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

## Friction log

- 2026-08-12: The new project's namespace `GameGameGame.Frontend.SadConsole` collides syntactically with the external `SadConsole` namespace in a few type references, and SadConsole surface-clearing APIs differ from memory. Mitigation: use `global::SadConsole...` where needed and keep rendering code thin/simple until reusable renderer patterns are deliberately ported from the old project or promoted through the new gallery.
- 2026-08-12: Attempting to replace the root `ScenarioBrowserConsole` immediately after `Game.Create(...)` but before `Game.Instance.Run()` caused SadConsole `ScreenSurface` construction to throw a `NullReferenceException`; constructing the same console inside `SetStartingScreen(...)` is safe. Mitigation: removed pre-run root replacement. Startup fullscreen resizing remains a follow-up for a SadConsole lifecycle-safe hook; F11 during the running app remains the current resize experiment.
- 2026-08-12: Frontend test verification for the input-mode pass was blocked because a manually running `GameGameGame.Frontend.SadConsole` process held the frontend output DLL/EXE. A temporary-output test build compiled, but VSTest did not complete cleanly from the alternate output path. Mitigation: close the running frontend before normal test verification; keep future test runs on the normal output path when possible.
