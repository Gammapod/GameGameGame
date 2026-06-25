# Sprint 17: Scenario Tooling Decoupling

Status: Implementation complete; keep as wrap-up reference until archived.

Read when:

- reviewing the scenario/tooling decoupling completed before Gate 4 `Give`/`Take` work;
- changing scenario materialization, scenario running, scenario recording, Console launch, or agent/headless APIs;
- removing dependencies on `GameGameGame.Editor` or Avalonia from non-UI code.

Related source of truth:

- `docs/Source of Truth/Engine-Editor-Capabilities.md` remains the authority for implemented capabilities and authoring tiers.
- `docs/Plans/High-Level-Roadmap.md` remains the strategic roadmap and backlog authority.
- `docs/Plans/Beta-Content-Exploration-Plan.md` remains the beta vignette ordering authority after this tooling cleanup.

## Intent

Before implementing Gate 4 `Give`/`Take`, clean up the scenario/tooling architecture so beta content work can continue without deepening the current dependency on the legacy Avalonia editor.

The immediate goal is not to remove Avalonia in this sprint. The goal is to make that removal possible later by ensuring scenario materialization, scenario running, scenario recording, Console launch, tests, and future frontend/tooling workflows do not depend on `GameGameGame.Editor`.

Long-term frontend direction remains a future integrated game/editor frontend, likely through a commercial game engine or other dedicated frontend stack. The headless/tooling layer should support that direction by exposing UI-agnostic services, not by becoming another frontend.

## Completion summary

Sprint 17 completed the intended dependency cleanup without removing the legacy Avalonia editor:

- added `GameGameGame.Headless` for UI-agnostic scenario run, report, recording, PNG frame, and GIF workflows;
- kept canonical scenario materialization in `GameGameGame.Content.ScenarioMaterializer`, including root-only compatibility where still needed;
- made `AgentContentEditorApi` delegate to Content/Headless services instead of owning scenario materialization, run, or recording implementations;
- removed the `GameGameGame.Console -> GameGameGame.Editor` reference and made `record-scenario` call headless services directly;
- kept ImageSharp/debug-rendering dependencies out of `GameGameGame.Content` and moved ImageSharp Drawing out of `GameGameGame.Editor`;
- split normal non-Editor tests from legacy Editor tests so routine Core/Content/Headless validation no longer builds Avalonia;
- moved most scenario/materialization/recording/content-authoring coverage into non-Editor service tests while preserving thin legacy Editor API adapter coverage.

Remaining polish is intentionally backlog, not a blocker for Gate 4:

- physically moving linked legacy Editor test files into `tests/GameGameGame.Editor.Tests` if the file layout becomes confusing;
- reviewing `EditorViewModelTests` for any additional non-UI behavior that should move to `ContentEditorService` tests;
- replacing or retiring the older test-local `MinimalScenarioRunner` after the shared headless report shape is stable;
- continuing to clarify root-only versus persisted-scenario terminology in commands, reports, and docs;
- adding the shared typed action-plan shape classifier noted below.

## Target layer boundaries

- `GameGameGame.Core`: runtime model, action execution, movement, turn advancement, tracing, and gameplay rules.
- `GameGameGame.Content`: YAML-backed content model, registry creation, validation, scenario definitions, and canonical scenario materialization.
- Headless/tooling services: scenario run, scenario recording, report generation, and automation-friendly workflows over Core and Content.
- `GameGameGame.Console`: temporary/manual frontend over Core, Content, and headless/tooling services.
- `GameGameGame.Editor`: legacy Avalonia UI shell over Content and headless/tooling services while it still exists.
- Future integrated frontend: another frontend over the same Core, Content, and headless/tooling services.

Headless/tooling services must stay UI-agnostic. They should not own Console key controls, Avalonia view models, commercial-engine types, menu flow, widget state, or canonical renderer assumptions.

## Sprint sequence

### 1. Choose non-UI service location

Decision needed at sprint start: create a new project such as `src/GameGameGame.Headless` / `src/GameGameGame.Tooling`, or place the first extracted services in `GameGameGame.Content`.

Preferred direction:

- keep pure materialization in `GameGameGame.Content`;
- put scenario running, report generation, recording, PNG/GIF debug rendering, and ImageSharp dependencies in a non-UI headless/tooling project;
- keep `GameGameGame.Console` free of `GameGameGame.Editor` and Avalonia dependencies;
- keep `GameGameGame.Content` free of debug-rendering/image dependencies.

### 2. Consolidate canonical scenario materialization

Current issue: `src/GameGameGame.Content/ScenarioMaterializer.cs` and `src/GameGameGame.Editor/AgentContentEditorApi.cs` both contain alpha scenario materialization logic, including default IDs, host plane creation, scenario-root spawn, player insertion validation, setup lines, and helper methods.

Planned work:

- make `GameGameGame.Content.ScenarioMaterializer` the canonical scenario materializer;
- preserve persisted scenario materialization behavior and diagnostics;
- preserve root-only scenario simulation support only as a compatibility wrapper if still needed;
- centralize default IDs such as `scenarioRoot` and `scenarioHost`;
- make editor/API-facing materialization delegate to the canonical service;
- add or preserve tests proving Console and API-facing paths use equivalent materialization behavior.

Acceptance criteria:

- no duplicated root/player materialization implementation remains in `GameGameGame.Editor`;
- existing alpha scenario materialization and Console launch behavior remains intact;
- scenario diagnostics remain stable enough for current tests and content workflows;
- exact behavior around player insertion, action-plan assignment, default action state, and scenario plane IDs is preserved unless deliberately changed.

### 3. Extract scenario run services from Editor

Current issue: `AgentScenarioRunner` lives in `AgentContentEditorApi.cs` and uses a root-only `legacy-run` path. Tests also contain a separate `MinimalScenarioRunner` that overlaps with production runner/reporting needs.

Planned work:

- move scenario run logic out of `GameGameGame.Editor` into the selected non-UI service location;
- expose UI/API-neutral request and report records;
- keep `AgentContentEditorApi.RunScenario` only as a thin adapter while the Avalonia editor still exists;
- make root-only versus persisted-scenario simulation terminology explicit;
- defer full removal of `MinimalScenarioRunner` until the shared runner/report format is stable, then migrate tests incrementally.

Acceptance criteria:

- scenario run logic no longer lives in `GameGameGame.Editor`;
- agent/editor API code delegates to the extracted service rather than owning the workflow;
- current report semantics and trace formatting are preserved as much as practical;
- expected in-simulation inability to act remains an observation, not a scenario-tainting engine failure.

### 4. Extract scenario recording and debug rendering from Editor

Current issue: `AgentScenarioRecorder`, `DebugScenarioFrameRenderer`, PNG/GIF writing, and ImageSharp usage currently live in `AgentContentEditorApi.cs`. Console reaches this through `AgentContentEditorApi`, creating a `Console -> Editor -> Avalonia` dependency path.

Planned work:

- move scenario recording and debug rendering to the non-UI headless/tooling project;
- keep ImageSharp and frame/GIF dependencies outside `GameGameGame.Content`;
- make `Console record-scenario` call the extracted service directly;
- keep `AgentContentEditorApi.RecordScenario` only as a thin adapter while the legacy editor remains;
- preserve output directory validation, frame naming, GIF generation, facing/target markers, and report fields initially.

Acceptance criteria:

- `GameGameGame.Console` no longer references `GameGameGame.Editor`;
- `Program.cs` no longer imports `GameGameGame.Editor`;
- `record-scenario` still works through non-UI services;
- recording tests pass without relying on Avalonia/editor project references except for explicitly legacy editor tests.

### 5. Decouple normal tests from Avalonia where practical

Current issue: broad tests reference Console and Editor, so normal test runs build Avalonia even when testing Core, Content, or headless workflows.

Planned work:

- move agent/headless API coverage toward non-UI service tests where practical;
- keep `EditorViewModelTests` and other Avalonia-specific tests isolated as legacy editor UI tests;
- consider test categories or separate test projects later if the split becomes valuable;
- reduce unnecessary transitive Avalonia exposure during routine Core/Content/headless validation.

Acceptance criteria:

- most scenario/content/headless tests do not require the Avalonia editor project;
- editor-specific tests are clearly scoped;
- normal development workflows can validate scenario tooling without building UI code where possible.

## High-risk areas

Use extra caution around:

- exact scenario diagnostics and setup lines;
- `scenarioRoot`, `scenarioHost`, and scenario plane IDs;
- root-only versus persisted-scenario terminology;
- player insertion behavior and occupied/out-of-bounds diagnostics;
- default `Facing` / `Target` materialization;
- action-plan assignment for carried entities;
- deterministic row-major initiative ordering;
- turn advancement and expected failed-action observations;
- report text that tests or content-authoring workflows rely on;
- debug artifact paths and avoiding repository-local generated output unless intentional.

Preferred implementation style:

- add or preserve parity tests first;
- move code mechanically before refactoring behavior;
- keep old API methods as adapters until replacement services are proven;
- separate file moves from semantic changes where practical;
- defer broad renames until the dependency graph is healthy.

## Out of scope

- Implementing `Give` / `Take`.
- Removing legacy action-plan runtime compatibility.
- Removing the Avalonia project entirely.
- Rewriting the Console frontend.
- Designing the future commercial-engine frontend.
- Making PNG/GIF debug rendering the canonical renderer for future frontends.
- Introducing a new scenario scripting language.

## Follow-up note: canonical action-plan shape

After the scenario/tooling dependency cleanup, add a small cleanup item for a shared typed action-plan shape classifier.

The classifier should distinguish canonical behavior chains, transitional primitive plans, legacy low-level steps, empty/passive plans, and invalid mixed shapes. This will remove duplicated string-based shape logic from Content and Editor and help future content/reporting workflows identify legacy or transitional plan usage. Do not plan this in detail until the higher-priority Editor/Avalonia decoupling work is complete.
