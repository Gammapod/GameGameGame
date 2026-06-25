# Sprint 18: Tech Debt Cleanup

Status: Archived; implementation complete.

Read when:

- implementing Sprint 17 polish follow-ups before returning to Gate 4 `Give`/`Take` work;
- changing action-plan shape classification, validation, editor plan-shape display, or scenario-report tests;
- organizing Editor-specific tests or contributor README workflows.

Related source of truth:

- `docs/Source of Truth/Engine-Editor-Capabilities.md` remains the authority for implemented capabilities and authoring tiers.
- `docs/Plans/High-Level-Roadmap.md` remains the strategic roadmap and backlog authority.
- `docs/Archived/Sprint-17-Scenario-Tooling-Decoupling.md` is the source context for the cleanup items promoted into this sprint.

## Completion Summary

Sprint 18 completed the intended cleanup scope:

- added a shared typed action-plan shape classifier in Core;
- moved Editor-specific tests physically into `tests/GameGameGame.Editor.Tests`;
- reduced `MinimalScenarioRunner` usage where existing Headless services were a direct fit;
- updated README contributor workflows for running, recording, editing, building, and testing.

Remaining `MinimalScenarioRunner` usage is intentionally retained for specialized watched-entity/destroyed/inventory report assertions that production Headless reports do not yet cover.

## Intent

Clean up small but compounding tooling and organization debt before the next mechanics gate. This sprint should improve confidence in beta content exploration without introducing new gameplay capabilities or broad architecture rewrites.

The sprint emphasizes minimal, contained changes:

- consolidate duplicated action-plan shape logic behind one typed classifier;
- make Editor-specific tests physically live in the Editor test project;
- reduce older test-local scenario runner usage where existing Headless services already fit;
- make README workflows accurate for running, recording, and editing scenarios.

## Sprint Sequence

### 1. Add Shared Action-Plan Shape Classifier

Current issue: action-plan shape detection is duplicated and stringly typed across Content validation, Content editor models, Editor view models, and registry validation.

Planned work:

- add a typed `ActionPlanShape` classifier near `src/GameGameGame.Core/ActionPlanDescriptors.cs`;
- classify at least canonical behavior chains, transitional primitive plans, legacy low-level steps, empty/passive plans, and invalid mixed-shape plans;
- replace duplicated active-shape counting and string comparisons where practical;
- keep display strings and authoring guidance in Content/Editor-facing layers rather than in the Core classifier.

Acceptance criteria:

- Content validation and registry validation use the shared classifier for invalid mixed-shape detection;
- Content editor snapshots and Editor plan-shape UI use the shared classifier rather than recomputing shape with strings;
- existing validation behavior remains equivalent except where invalid mixed-shape labeling is deliberately clarified;
- tests cover classifier behavior directly or through validation/service behavior.

Future note:

- Do not enrich scenario reports during this sprint. The classifier may later support report summaries such as fully canonical scenarios, transitional-plan usage, or remaining legacy low-level plans.

### 2. Physically Relocate Editor Tests

Current issue: Editor-specific test files live under `tests/GameGameGame.Tests` and are excluded/linked into `tests/GameGameGame.Editor.Tests`, which makes ownership unclear.

Planned work:

- move `AgentContentEditorApiTests.cs` and `EditorViewModelTests.cs` into `tests/GameGameGame.Editor.Tests`;
- remove the corresponding `Compile Remove` entries from `tests/GameGameGame.Tests/GameGameGame.Tests.csproj`;
- remove linked includes for files that now physically live in the Editor test project;
- keep genuinely shared helpers linked only if moving them would create unnecessary churn.

Acceptance criteria:

- Editor-specific tests are physically located in `tests/GameGameGame.Editor.Tests`;
- normal Core/Content/Headless tests still do not build Avalonia through Editor-specific test files;
- both test projects still build and run.

### 3. Reduce Test-Local `MinimalScenarioRunner` Usage

Current issue: `tests/GameGameGame.Tests/ScenarioRunReportTests.cs` contains an older `MinimalScenarioRunner` that overlaps conceptually with Headless scenario run/report services.

Planned work:

- replace `MinimalScenarioRunner` usages with existing Headless services where the fit is direct;
- avoid inventing a new runner solely to remove the old one;
- leave any remaining local helper clearly scoped if the tests still validate a report shape not yet covered by Headless services.

Acceptance criteria:

- tests use production Headless services where doing so is natural and low-risk;
- any remaining local scenario helper has a clear reason to exist;
- no behavior change is introduced solely for test cleanup.

### 4. Update README Contributor Workflows

Current issue: README references a missing `.slnx` build/test workflow and does not foreground the most important newcomer tasks: running scenarios, recording scenarios, and opening editing tools.

Planned work:

- remove or de-emphasize the missing `.slnx` workflow;
- document running the prototype console app;
- document running a specific scenario from a content YAML file;
- document recording a scenario with `record-scenario`;
- document launching the current Avalonia editor;
- document normal non-Editor tests and Editor-specific tests separately.

Acceptance criteria:

- README commands correspond to current tracked files and actual Console behavior;
- a new contributor can find how to run or record a given scenario;
- a new contributor can find how to access current editing tools for scenario/content authoring.

## Out Of Scope

- Implementing `Give` / `Take`.
- Removing legacy action-plan runtime compatibility.
- Removing the Avalonia editor.
- Broadly refactoring `MainEditorViewModel`, `MainWindow`, or `ActionPlanInterpreter`.
- Splitting Headless rendering/artifact generation into another project.
- Adding new scenario-report enrichment based on action-plan shape.
- Committing to a `.slnx` or Visual Studio solution-file policy unless separately decided.

## Verification

Run at minimum:

```bash
dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj
dotnet test tests/GameGameGame.Editor.Tests/GameGameGame.Editor.Tests.csproj
```

If README command examples are changed, manually verify the command syntax against `src/GameGameGame.Console/Program.cs` and `src/GameGameGame.Console/ConsoleScenarioLauncher.cs`.
