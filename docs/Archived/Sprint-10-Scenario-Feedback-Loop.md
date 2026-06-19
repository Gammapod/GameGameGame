# Sprint 10: Scenario Feedback Loop

Status: Completed / archived during Sprint 10 wrap-up.

Read when:

- reviewing Sprint 10 scenario-feedback implementation scope and handoff notes;
- investigating why the first scenario-root runner/API was shaped around editor-authored inventory spaces;
- selecting follow-up scenario-feedback work for later sprints.

Related:

- `docs/Source of Truth/Engine-Editor-Capabilities.md`
- `docs/Source of Truth/planning-index.md`
- `docs/Plans/High-Level-Roadmap.md`

## Goal

Make generated scenario authoring and simulation feedback comfortable enough to evaluate behavior-system design choices before adding another large batch of movement/direction primitives.

## One-week sprint plan

Sprint priority: promote the test-local scenario report work into a small, reusable headless scenario feedback loop while keeping scope narrow enough to finish in one week. The sprint should improve feedback quality, not add new gameplay mechanics.

### Selected scope

1. **Promote a reusable scenario exercise helper**
   - Promote the useful shape from `tests/GameGameGame.Tests/ScenarioRunReportTests.cs` into editor/agent API surface, with tests driving the first slice.
   - Keep the helper frontend-agnostic and based on existing editor/content services plus Core simulation.
   - Avoid inventing a broad scenario content language; a scenario should be rooted in normal editor-authored entity templates and carried inventory layout.
   - First scenario model: the content-authoring agent selects a scenario-root entity template, the runner spawns that root entity, treats its inventory plane as the scenario simulation space, and runs all default-plan actors inside that inventory by deterministic initiative order.

2. **Improve compact state summaries**
   - Report high-signal setup and final state for watched entities: plane/inventory location, facing, target, and carried inventory contents where useful.
   - Prefer deterministic, readable text suitable for tests and future runlogs, but do not freeze a golden runlog format yet.

3. **Make capability gaps explicit and structured**
   - Preserve existing text report gap output.
   - Add enough structure that tests and future agent/API callers can distinguish unsupported design requests from validation failures and runtime execution failures.

4. **Exercise preview + simulation workflow**
   - Provide a narrow helper/API path that can include action-plan preview output and simulation trace/state output in one scenario report, or document why separate calls remain preferable.
   - Use the first utility Action Step batch (`DropFacing`, `PushFacing`, `DestroyTarget`, `CreateFacing`) plus existing `MoveFacing`/`PickupTarget` cases as coverage.

### Defer this sprint

- New Action Steps or gameplay mechanics, including `CreateFacing(templateId)`, `Give`, `Take`, `SeekTarget`, `TeleportTo`, scheduler/speed, reactions, or behavior templates.
- Checked-in content/prototype changes.
- Console application changes.
- Avalonia GUI changes unless a tiny maintenance fix is required to keep existing behavior from breaking.
- Saved runlogs/golden runlog tests until the report shape stabilizes.

### Testable outcomes

- A reusable helper can author temporary templates, carried inventory, default action-plan assignments, canonical behavior chains, and initial action state through editor-facing APIs before runtime spawn.
- The agent API can run a scenario-root entity template by treating the root's inventory plane as the starting simulation space and scheduling all contained entities with default action plans by deterministic initiative order.
- A reusable helper can run a small world for N turns and return compact behavior-chain trace lines plus setup/final state summaries.
- Scenario output classifies at least three result channels: content authoring/validation diagnostics, runtime execution failures, and unsupported capability gaps.
- At least one scenario report combines or clearly links action-plan preview details with simulation trace/state output for a canonical behavior chain.
- Existing per-Action-Step report coverage remains green for `MoveFacing`, `PickupTarget`, `DropFacing`, `PushFacing`, `DestroyTarget`, and `CreateFacing`.

### TDD readiness / invariant trace

- Affected invariants:
  - Actions must produce structured traces for failed checks and resolutions.
  - Action Plan resolution must distinguish failure that follows an explicit fallback from terminal resolution that ends the current root actor's turn.
  - Entity action state such as `Facing` and `Target` is typed and persists on the actor entity across plan executions.
  - Content editor operations preserve declared IDs, presentations, carried layouts, Action Plans/behavior assignments, legacy action plans, and validation results.
- Existing coverage to trace before implementation:
  - `ScenarioRunReportTests` for the current test-local report shape and result-channel examples.
  - `AgentContentEditorApiTests` and `ContentEditorServiceTests` for editor/API authoring operations.
  - `CoreActionPlanTests` for trace/fallback/terminal turn semantics.
  - Invariant coverage listed in `docs/Source of Truth/invariants.md` for structured traces, fallback/terminal behavior, typed entity action state, and content editor operations.
- First intentionally failing tests:
  - Add or revise scenario-helper tests so the desired reusable helper API and structured result channels fail before production/helper implementation.
  - Add a focused test for preview-plus-simulation report output before wiring that output into the helper.

### Suggested week split

- **Day 1:** Confirm helper home/API shape, write failing tests for reusable setup/run/report result channels.
- **Days 2-3:** Extract/promote the minimal helper and keep existing scenario cases passing.
- **Day 4:** Add compact watched-entity state/inventory summaries and structured capability-gap diagnostics.
- **Day 5:** Add preview-plus-simulation report path, run targeted and broader tests, update capability/planning docs and handoff notes.

## Current priority rationale

The sprint delivered canonical behavior-chain GUI clarity, safe legacy hiding, trace formatting, plan preview, and the first utility Action Step batch. The content-editor exercise showed that the next bottleneck is testing workflow: we can implement primitives, but we need faster feedback on whether authored scenarios behave as intended.

## Loose scope

- Build a headless scenario exercise workflow around existing editor/content services and Core simulation.
- Keep generated exercises out of checked-in content unless they become deliberate fixtures.
- Reuse `ContentEditorService`, `AgentContentEditorApi`, `PreviewActionPlan`, and `BehaviorChainTraceFormatter` rather than bypassing editor parity.
- Support multi-turn simulation reports with compact traces.
- Make unsupported capability gaps explicit in generated exercise output.
- Treat the current Avalonia GUI as out of scope unless explicitly selected; scenario feedback work should prioritize editor services, agent/headless APIs, tests, and future frontend readiness.

## Candidate testable outcomes

- A test/helper can create temporary entities, carried inventory, action plans, behavior chains, initial facing, and default plan assignments through editor-facing APIs.
- A test/helper can materialize a small world, run N turns/actions, and return compact trace summaries.
- A generated scenario can exercise `DropFacing`, `PushFacing`, `DestroyTarget`, and `CreateFacing` without manual YAML inspection.
- Scenario reports distinguish engine bugs from unsupported authoring/design requests.

## Design decisions to discuss before implementation

- What minimum scenario DSL/API shape is useful without becoming an editor-only content language?
- Should generated scenario helpers live in tests only, or become an editor service / agent API feature?
- What is the expected relationship between content templates and runtime entities in generated simulation exercises?
- How should failed/all-fallback-exhausted behavior-chain turn consumption be displayed in reports?

## Out of scope for Sprint 10 unless explicitly selected

- Resolving the full movement/direction philosophy.
- Implementing `CreateFacing(templateId)` or template spawning.
- Adding scheduler/speed/multiple-action systems.
- Adding behavior templates.
- Refactoring or extending the current Avalonia GUI.

## Promotion triggers for follow-up backlog items

- Promote compact world/state summaries if raw traces are not enough to understand scenario outcomes quickly.
- Promote plan preview + simulation in one API command if scenario exercises repeatedly require separate preview, simulation, trace, and state-diff calls to answer one authoring question.
- Promote capability-gap reporting if unsupported requests recur, especially missing Action Steps, direction overrides, template spawning, state slots, or inventory-transfer semantics.
- Promote saved runlogs or golden runlog tests only after the scenario report format is stable enough that expected-output fixtures will not churn constantly.
- Promote scenario documents only if temporary/generated setup becomes valuable enough to preserve as reusable checked-in fixtures or selectable scenarios.

## Handoff notes for future sessions

Update this section during or at the end of scenario-feedback work. Keep it concise and operational.

Record:

- implementation files or tests that became central;
- commands/tests that were useful for verification;
- scenario shapes that were exercised;
- capability gaps encountered;
- items intentionally deferred back to the roadmap;
- whether the active plan should be archived, extended, or replaced.

Current handoff state:

- Phase 1 has started in test infrastructure with `tests/GameGameGame.Tests/ScenarioRunReportTests.cs`.
- The first narrow demo scenario runs `DropFacing` for one turn and emits a completed text report with setup, compact behavior-chain trace lines, final state, diagnostics, and capability gaps.
- The runner now also has coverage for an editor/API-authored temporary setup: `AgentContentEditorApi` creates templates, carried inventory, initial facing, a canonical `DropFacing` behavior plan, and default plan assignment before the registry spawns runtime entities for the report.
- Successful isolated report coverage now exists for every current canonical Action Step: `MoveFacing`, `PickupTarget`, `DropFacing`, `PushFacing`, `DestroyTarget`, and `CreateFacing`. The `MoveFacing` report runs for two turns.
- Expected-failure report coverage now distinguishes content-authoring diagnostics, runtime execution failures, and unsupported capability gaps. Runtime failure diagnostics pull the specific failing trace detail when available.
- Sprint 10 first production/API slice added `AgentContentEditorApi.RunScenario(AgentScenarioRunRequest)`. The runner spawns an editor-authored scenario-root entity template, treats the root inventory plane as the scenario space, schedules all contained entities with default action plans in deterministic row-major initiative order, and returns structured setup, turn trace, final-state, validation, runtime-failure, and capability-gap data. Initial coverage: `AgentContentEditorApiRunsScenarioRootInventoryActorsInInitiativeOrder`.
- Content-editor scenario exercise: an in-memory 3x1 scenario-root room with two facing actors was possible through editor-facing APIs and `RunScenario`. A passive `MoveFacing` actor acted first and could not move against the blocker; a destructive actor then used canonical behavior to destroy the blocker. Follow-up implemented before wrap-up: `RunScenario` now surfaces rich canonical behavior-chain trace lines and records expected in-simulation inability to act as runtime observations rather than scenario-tainting runtime failures. Row-major initiative remains the current temporary deterministic ordering for scenario-root inventory actors; longer-term initiative ordering is deferred.
- Verification command: `dotnet test "tests\GameGameGame.Tests\GameGameGame.Tests.csproj"`.
- Next unblocked slice: add richer scenario-root report formatting/state summaries and decide whether preview-plus-simulation belongs in `RunScenario` or a separate helper command.
