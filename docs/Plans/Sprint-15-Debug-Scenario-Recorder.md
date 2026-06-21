# Sprint 15: Debug Scenario Recorder

Status: Planned / active sprint plan.

Related plans and source of truth:

- `docs/Source of Truth/Engine-Editor-Capabilities.md`
- `docs/Source of Truth/testing-charter.md`
- `docs/Source of Truth/invariants.md`
- `docs/Plans/High-Level-Roadmap.md`
- `docs/Plans/Beta-Content-Exploration-Plan.md`
- `docs/Plans/Beta-Capability-Gap-Log.md`
- `docs/Archived/Sprint-14-Gate-2-Targeting-Showcases.md`

## Sprint goal

Add a headless debug recording workflow for authored scenarios: run a scenario for `N` full simulated turns, capture frame 0 as the initial materialized state, capture one frame after each simulated turn, and write visual debug artifacts as PNG frames plus an animated GIF.

This sprint is a scenario/tooling feedback sprint, not a gameplay-mechanics sprint. The recorder should make Sprint 14-style targeting and containment behavior easier to inspect visually without replacing the future commercial/frontend direction.

## Selected scope

### 1. Sibling `RecordScenario` workflow

- Add a recording command/API sibling to the existing scenario runner rather than changing the `RunScenario` contract.
- Reuse existing scenario materialization and deterministic full-turn simulation behavior where practical.
- Prefer a persisted scenario-by-ID path for the user-facing dotnet command; support root-template recording only if it is a low-cost compatibility bridge.
- Return/write structured recording results: scenario identity, output paths, frame count, diagnostics, runtime observations/failures, and capability gaps.
- Keep current `RunScenario` reports stable unless a tiny shared-helper refactor changes purely internal implementation.

### 2. Purpose-built debug image renderer

Build a renderer separate from `System.Console`. The renderer should imitate the useful information density of Console while being deterministic and suitable for headless artifacts.

First debug layout:

- header above panes with scenario ID/name, frame number, turn number, and diagnostics/messages when present;
- left pane, approximately two-thirds width: the entity currently carrying/containing the player's active plane, falling back to the scenario root/current container when appropriate;
- right pane, approximately one-third width: the player entity;
- each pane displays entity metadata at the top, the entity's inventory plane centered, and initiative/order plus carried-entity info at the bottom.

Initial visual conventions:

- fixed character-cell style, approximately 12x18 pixels;
- sans-serif font with clear character differentiation;
- black background;
- `.` for empty cells;
- existing content glyphs and presentation colors for occupied cells;
- thin bright-yellow 1px edge line on the side indicated by an entity's persistent `Facing`, when present;
- white circle around the top-left corner of an entity cell when the entity has a non-self persistent `Target`;
- if both source and target entities are visible in any rendered pane, draw a white arrow/line overlay connecting their cell centers, even across rendered planes;
- overlapping arrows are acceptable for the first slice.

### 3. Frame cadence and artifacts

- Frame 0 is the initial materialized scenario state before simulated actor turns.
- Frame `k` is the state after full simulated turn `k` completes.
- The first slice does not need per-initiative/active-actor frames.
- Output both PNG frames and a GIF by default when the image dependency supports it.
- If GIF generation fails or is deferred by library friction, PNG frames should remain the core artifact and the report should explain the GIF limitation.

### 4. Dotnet-accessible command

- Provide a dotnet command path for manual use, likely through the existing Console project or a small sibling tool if that proves cleaner.
- Expected shape, subject to implementation details:

```text
dotnet run --project src/GameGameGame.Console -- record-scenario <content-file> <scenario-id> --turns <N> --output <directory>
```

- The command should be scriptable and should not require opening the Avalonia GUI.

## Defer this sprint

- Per-initiative frame capture, active actor focus frames, and frame cadence options.
- Alternate render styles such as 2x2 color cells, large bordered 32x32 tiles, sprites, or theme selection.
- Pixel-perfect Console parity or actual terminal screenshot capture.
- Full visual regression/golden image testing unless a tiny deterministic smoke assertion is enough.
- Replacing Console with Unity/Godot/SadConsole or another commercial/game frontend.
- Scenario runlogs, runlog stepper UI, playback controls, timeline editing, or video formats beyond GIF.
- New gameplay primitives, scheduler/speed changes, targeting filters, `Give`/`Take`, spawning/template materialization, reactions, traps, combat, or containment semantics.
- Broad Avalonia GUI integration; a GUI button can be considered later after the headless command is useful.

## Testable outcomes

- A recording request can materialize a valid persisted scenario and produce exactly `turnCount + 1` frame records: initial state plus one frame per full simulated turn.
- The existing scenario runner/API remains callable with its current report semantics after any shared simulation-helper extraction.
- The debug renderer can render a small scenario frame containing two panes, metadata, inventory grids, entity glyph/color cells, empty-cell dots, carried-entity/initiative info, facing markers, target markers, and same-visible-set target arrows.
- The recording workflow writes PNG frame artifacts to the requested output directory and reports their paths.
- The recording workflow writes a GIF artifact, or reports a clear capability gap/runtime limitation if the chosen library makes GIF unsupported in the first implementation slice.
- Invalid scenario/content/output requests return categorized diagnostics and do not produce misleading success reports.
- A dotnet command can record a checked-in beta scenario for a small turn count without requiring tests or GUI interaction.

## TDD readiness / invariant trace

Affected invariants:

- Entity locations are represented by occupancy of nodes in planes.
- At most one entity may occupy a node at a time.
- Entity action state such as `Facing` and `Target` is typed and persists on the actor entity across plan executions.
- Content editor operations preserve declared IDs, presentations, carried layouts, Action Plans/behavior assignments, legacy action plans, and validation results.

Existing coverage to trace before implementation:

- Occupancy/location: `EntityLocationsAreRepresentedByNodeOccupancy`, `MovementCannotPlaceEntityOnOccupiedNode`, `PrototypeRegistryValidationReportsOverlappingCarriedEntities`.
- Typed action state/defaults: `CanonicalFacingPersistsOnActorActionStateAcrossPlanExecutions`, `CanonicalTargetPersistsOnActorActionStateWhenBlockingEntityIsFound`, `SpawnedActionPlanUsesCanonicalInitialFacingDefault`.
- Scenario materialization/run behavior: existing `AgentContentEditorApi` scenario tests, `ConsoleScenarioLaunchTests`, `AlphaScenarioFixtureTests`, `BetaContentFixtureTests`, and `ScenarioRunReportTests` where still relevant.
- Content/editor preservation: `ContentEditorServiceValidatesCurrentDocumentAfterEdits`, `ContentEditorServiceValidationReportsCurrentDocumentErrors`, and editor/API scenario authoring tests.

First intentionally failing tests:

- Add recording API tests that expect frame count `turnCount + 1`, stable scenario identity/output metadata, and categorized diagnostics for invalid scenarios or output paths.
- Add renderer tests using a small in-memory/materialized world that assert frame model or artifact metadata includes pane layout, glyph/color occupancy, empty cells, facing markers, target markers, and visible-target arrows without requiring brittle pixel-perfect assertions.
- Add a command-level smoke test only if the existing test harness can run it cheaply; otherwise verify the command manually and keep lower-level API/rendering tests as automated coverage.

## Suggested week split

- **Day 1:** Confirm output library/dependency, command/API shape, and non-pixel-perfect test strategy; write failing tests for recording request/report and frame count.
- **Day 2:** Extract/reuse the smallest simulation loop helper needed for recording without changing `RunScenario` behavior.
- **Days 3-4:** Implement the debug renderer, PNG output, facing/target markers, and visible-target arrows; add artifact/report tests.
- **Day 5:** Add GIF output and dotnet command, run targeted and broader validation, update `Engine-Editor-Capabilities.md`, roadmap/capability-gap docs, and handoff notes.

## Definition of done

- `RecordScenario` or equivalent sibling workflow exists and is documented as a supported headless/debug capability.
- `RunScenario` remains stable for existing tests and content-authoring workflows.
- A small checked-in alpha or beta scenario can be recorded from a dotnet command into PNG frames and a GIF.
- Visual debug frames show the requested two-pane layout, metadata, inventory grids, glyph/color cells, empty dots, facing markers, target markers, and visible-target arrows.
- Tests cover the recording/report contract and renderer state mapping without overfitting to exact pixels.
- Any unsupported or deferred visual/debug behavior is recorded in the roadmap or capability-gap log.

## Day 1 implementation notes

- Preferred image dependency: SixLabors ImageSharp, because it supports cross-platform headless bitmap drawing/encoding without depending on terminal capture, Avalonia GUI lifetime, or Windows-only drawing APIs.
- First API shape under test: `AgentContentEditorApi.RecordScenario(AgentScenarioRecordingRequest)` with scenario ID, turn count, and output directory; report includes scenario ID, frame records, PNG paths, GIF path, and categorized diagnostics.
- First test strategy: assert recording contract, frame count, artifact existence, and renderer/state metadata where possible; avoid brittle pixel-perfect comparisons.

## Day 5 handoff notes

- Implemented `RecordScenario` on `AgentContentEditorApi` with persisted scenario ID, turn count, output directory, PNG frame paths, GIF path, and categorized diagnostics.
- Implemented a first debug renderer using ImageSharp/ImageSharp.Drawing in the Editor project. It renders two panes, metadata, inventory grids, glyph/color cells, empty dots, carried-entity state/initiative info, yellow facing edge markers, white non-self target markers, and visible-target arrows.
- Added dotnet command access through the prototype Console project: `dotnet run --project src/GameGameGame.Console -- record-scenario <content-file> <scenario-id> --turns <N> --output <directory>`.
- The Console command references the Editor project for the agent/headless API rather than duplicating recorder/rendering logic. This is acceptable for the prototype Console but can be moved to a dedicated tool later if Console dependencies become noisy.
- Verification included targeted recording tests, related API/Console tests, build with `/m:1`, and a manual dotnet smoke command against `beta-direct-chase`.
