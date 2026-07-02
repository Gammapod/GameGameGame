# Frontend Testing Strategy Proposal

Status: Archived after initial SadConsole frontend test suite and source-of-truth testing guidance were established.

Read when:

- deciding how to test SadConsole, Console fallback, or future frontend applications;
- planning a frontend testability/refactor sprint;
- deciding whether a frontend behavior should be covered by engine tests, shared-service tests, pure frontend tests, or manual UI testing.

Related source of truth:

- `docs/Source of Truth/Frontend-UX-Invariants.md` defines frontend/shared-service boundaries.
- `docs/Source of Truth/Frontend-UX-Standards.md` defines presentation standards that tests may protect where practical.
- `docs/Plans/SadConsole-Frontend-Roadmap.md` defines current frontend implementation stages.
- `docs/Archived/Frontend-Sprint-2-SadConsole-Balanced.md` records the first substantial SadConsole Simulation-mode UX sprint that exposed these testing priorities.

## Proposal summary

Frontend tests should initially target **pure frontend-owned view, layout, prompt, and presentation-model logic**, not SadConsole's interactive window or cell-perfect rendering.

The engine already has extensive tests for simulation semantics. Frontend tests should protect the layer that consumes shared facts and turns them into SadConsole/Console presentation and input state while preserving the invariant that frontends do not own simulation legality, materialization, or durable content semantics.

## Initial implementation status

The first approved slice is implemented through `tests/GameGameGame.SadConsole.Tests` and the lightweight frontend standards in `docs/Source of Truth/testing-charter.md`.

Covered by the initial suite:

- `SadConsoleSessionLayoutTests` covers panel slot geometry, non-overlap, bounds, and collapsed root slots;
- `SadConsolePanelChainViewTests` covers visible chain selection and role/title policy;
- `PromptChoiceCyclerTests` covers plane filtering, row-major ordering, wrap-around, unknown-current selection, and empty-choice messages;
- `LocalActivityViewBuilderTests` covers empty local activity text, content rows, previous-action snippets, and remaining local log snippets.

Still intentionally deferred:

- `SadConsoleSessionViewBuilder` extraction and tests for header/message/prompt summaries, selected summary, global log title/empty text, and full panel view list construction;
- exit-direction-specific prompt cycling and preview destination tests, because the current initial helper covers coordinate candidate cycling but not direction-affordance preview movement;
- scenario menu summaries, global log text summaries, and broader lightweight text snapshots until view-builder output stabilizes;
- SadConsole window automation, MonoGame initialization, cell-perfect rendering, and mouse hit-testing automation.

## Testing priorities discovered from frontend sprints

### Priority 1: Pure view/layout model tests

Goal: protect the frontend-owned model that sits between shared projections and SadConsole drawing.

Candidate coverage:

- `SadConsoleSessionView` construction from a launched scenario/session;
- header/message/prompt summaries;
- global log title and empty text, especially honest `controlled-command log` labeling;
- panel view list construction;
- selected-entity summaries;
- separation between shared projection data and frontend layout state.

Required/refactor direction:

- extract session view construction from `SadConsoleShell` into a testable builder, such as `SadConsoleSessionViewBuilder`.

### Priority 2: Panel-chain and layout tests

Goal: make breadcrumb-as-panel-chain behavior safe to iterate.

Candidate coverage:

- one-entity containment path produces one panel;
- shallow paths render all panels;
- long paths render root collapsed plus recent ancestor/inspection panels;
- inspected entity is last;
- current-container and inspected roles produce predictable panel titles;
- panel rectangles do not overlap and remain within the screen area;
- collapsed card flags are set as intended.

Required/refactor direction:

- keep `SadConsoleSessionLayout`, visible-chain selection, and panel-title assignment pure/static or in a small testable builder.

### Priority 3: Prompt candidate and cycling tests

Goal: protect keyboard-first valid-choice UX without creating frontend action legality.

Candidate coverage:

- candidate filtering by plane;
- stable row-major presentation ordering for candidates already supplied by shared/frontend-neutral facts;
- cycling from no/unknown current selection chooses the first valid candidate;
- cycling from the last candidate wraps to the first;
- empty candidates produce an explicit no-choice result/message;
- inspect target cycling remains navigation, not Core action legality;
- exit-direction cycling selects valid directions and can update preview cursor destination.

Required/refactor direction:

- extract prompt candidate/cursor cycling from `SadConsoleShell` into a small pure helper, such as `PromptChoiceCycler` or `PromptSelectionModel`.

Boundary note:

- These tests should assert how SadConsole presents, filters by visible/current plane, and cycles through already-computed candidates. They should not assert whether pickup/drop/enter/exit/move choices are legal; legality and failed-action behavior remain Core-owned through shared affordance and command services.
- If candidate ordering becomes semantically meaningful beyond presentation, such as a shared default/recommended Action Choice or deterministic submission order, promote that ordering into a Core/shared contract before relying on SadConsole-local tests.

### Priority 4: Local activity presentation tests

Goal: protect the presentation rules for local panel activity while broader autonomous log projection remains a shared-service follow-up.

Candidate coverage:

- visible contents rows are preserved in projected order;
- previous-action snippets are shown under the corresponding content row;
- remaining local controlled-command snippets appear after content rows when space allows;
- empty activity has honest empty text;
- labels do not imply complete autonomous simulation history.

Required/refactor direction:

- extract local activity row composition from drawing into a small presentation builder, such as `LocalActivityViewBuilder`.

### Priority 5: Lightweight text snapshot tests

Goal: catch large unintended presentation regressions after view builders stabilize.

Candidate coverage:

- scenario menu summaries;
- panel-chain summaries;
- local activity text summaries;
- global log text summaries.

Recommendation:

- defer until the view-builder layer is stable enough that snapshots will not churn after every UX iteration.

## Tests to defer

### SadConsole window/integration automation

Do not prioritize launching the real SadConsole window in automated tests yet. It is likely brittle, slow, and environment-sensitive.

### Cell-perfect rendering tests

Do not prioritize cell-perfect assertions while layout and presentation vocabulary are still changing. Revisit after panel chains, prompt UX, and local activity presentation settle.

### Mouse hit-testing tests

Defer until mouse interaction is implemented. However, keep layout geometry pure so hit-testing can be tested without a SadConsole window later.

### Engine semantic tests in frontend projects

Avoid duplicating Core simulation legality tests in frontend test projects. If a behavior depends on action legality, target choice semantics, turn consumption, materialization, provenance, or autonomous log projection, test or extend the shared/Core/Content service instead.

## Proposed test project shape

Create a dedicated test project:

```text
tests/GameGameGame.SadConsole.Tests
```

Initial dependencies:

- `src/GameGameGame.SadConsole`
- the repository's existing xUnit test framework/package conventions (`net10.0`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, and `coverlet.collector`);
- `src/GameGameGame.Content` and `src/GameGameGame.Core` only as needed for shared DTOs and lightweight fixtures, preferably through the SadConsole project reference rather than deep frontend-side scenario construction.

Implementation details:

- Keep the first tests as pure .NET unit tests. Do not launch the real SadConsole window, initialize MonoGame, or assert cell-perfect renderer output.
- Extract small frontend-owned helpers from `SadConsoleShell` opportunistically before testing them, such as `SadConsoleSessionViewBuilder`, `PromptChoiceCycler` / `PromptSelectionModel`, `LocalActivityViewBuilder`, and panel-chain/layout helpers.
- Prefer testing helper inputs/outputs over constructing full runtime scenarios. Use existing test helpers such as `TestWorld.cs` only when a view builder genuinely needs Core/Content DTOs.
- Use `InternalsVisibleTo("GameGameGame.SadConsole.Tests")` for frontend internals if needed, or make extracted helpers public only when they are stable app-facing abstractions rather than test seams.
- Keep SadConsole tests independent of engine semantic coverage already owned by Core/Content tests.

Initial test groups:

```text
SadConsoleSessionLayoutTests
SadConsolePanelChainViewTests
PromptChoiceCyclerTests
LocalActivityViewBuilderTests
```

## Ownership split

### Frontend-owner test ownership

Frontend-owner should own automated tests for SadConsole/Console presentation and interaction behavior that consumes existing shared contracts without redefining simulation semantics:

- layout geometry, panel rectangles, collapse/expand presentation, and future hit-test geometry;
- panel-chain visibility, collapsed-card policy, panel title roles, selected/inspected summaries, and session view text labels;
- prompt mode, cursor state, candidate presentation filtering, candidate cycling, wrapping, empty-choice messages, and preview cursor movement over shared affordance/projection data;
- menu summaries, controlled-command log labels, local/global log presentation labels, and local activity row composition;
- future lightweight text snapshots once view-builder output stabilizes.

### Core-owner test ownership

Core-owner should retain automated tests for behavior and contracts that define engine/shared semantics:

- action legality, failed-action policy, turn consumption, turn advancement, traces, movement, inventory, containment, aperture, target selection, and autonomous behavior;
- `ControlledActorCommandService` execution semantics;
- `ControlledActorAffordanceService` legality/hint facts;
- structured action outcome/log projection facts when they are shared across frontends;
- any future Action Choice request/submission/resolution semantics, including default/recommended choice ordering if that ordering becomes authoritative rather than presentational.

### Content/editor-aware test ownership

Content/editor-aware tests should cover scenario/content/editor integration consumed by frontend/editor surfaces:

- scenario discovery, materialization, player insertion, and playable session launch;
- editor/content APIs for future Editor mode mutation workflows;
- source-navigation, preview, validation, and provenance contracts when promoted.

## Proposed implementation sequence

### Slice A: Establish test project and pure layout tests

1. Add `tests/GameGameGame.SadConsole.Tests`.
2. Add tests for `SadConsoleSessionLayout.BuildPanelChainSlots`.
3. Add tests for visible chain selection and collapsed-card policy after extracting it from `SadConsoleShell`.

Exit criteria:

- test project builds and runs with existing solution/test conventions;
- panel-chain layout behavior is covered without launching SadConsole.

### Slice A.5: Clarify frontend testing charter

After the first SadConsole test project exists and the initial pure frontend tests establish the intended style, update `docs/Source of Truth/testing-charter.md` or create a linked frontend-specific charter to clarify the workflow split:

- the current strict red/green TDD workflow is intended for engine/shared semantic work, especially Core/Content/Editor behavior with stable invariants;
- frontend work should use a testability-first workflow rather than requiring strict TDD for every exploratory UI change;
- stable pure frontend logic such as layout slots, panel-chain selection, prompt cycling, local activity composition, and future hit-test geometry should receive automated tests;
- exploratory UX work such as visual density, wording, colors, animation, and interaction feel may be implemented and manually evaluated before being pinned by tests;
- frontend tests must still respect the architectural split and must not duplicate Core legality/materialization/turn semantics.

Exit criteria:

- repository testing guidance no longer implies that the Core-owner TDD workflow automatically applies unchanged to SadConsole/Console/future frontend UX work;
- the frontend testing charter points back to this strategy or its accepted successor.

### Slice B: Extract and test prompt cycling

1. Extract cursor/candidate cycling into a pure helper.
2. Add tests for stable row-major presentation ordering, plane filtering, wrap-around, and empty candidates.
3. Keep existing SadConsole behavior unchanged.

Exit criteria:

- valid-choice cycling regressions are covered by tests;
- `SadConsoleShell` delegates selection mechanics to testable frontend logic.

### Slice C: Extract and test session/panel view building

1. Move `BuildSessionView` and panel-chain construction into a builder that accepts current session/presentation state.
2. Add tests for panel title roles, inspected panel placement, log labels, and selected summary.

Exit criteria:

- SadConsole view composition has tests independent of drawing.

### Slice D: Extract and test local activity view composition

1. Move `DrawLocalActivity` composition decisions into a view builder.
2. Add tests for content/action snippet grouping and honest empty state.

Exit criteria:

- local activity presentation rules are covered without asserting SadConsole cells.

## Manual testing remains required

Until a richer frontend automation strategy exists, each frontend sprint should still include a manual smoke pass:

1. Launch SadConsole default catalog/manifest.
2. Select representative scenarios.
3. Verify panel chains, collapsed cards, inspect cycling, action cycling, and local activity readability.
4. Confirm glyph identity is preserved.
5. Confirm limited logs are honestly labeled.

## Open review questions

These questions are resolved for the initial suite:

- SadConsole tests live in the dedicated `tests/GameGameGame.SadConsole.Tests` project.
- Console fallback behavior receives only targeted smoke coverage unless Console fallback polish is explicitly selected.
- `SadConsoleShell` should be refactored opportunistically into small testable helpers; do not refactor the whole shell before tests.
- Text snapshots are deferred until view-builder output is stable enough that snapshots catch real regressions rather than expected UX iteration.
