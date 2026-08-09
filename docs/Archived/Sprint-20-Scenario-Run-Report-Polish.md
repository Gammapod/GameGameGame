# Sprint 20: Scenario Run and Report Polish

Status: Completed / archived during Sprint 20 wrap-up.

Related source of truth and active plans:

- `docs/Source of Truth/Content-Authoring-Manual.md`
- `docs/Source of Truth/Engine-Editor-Capabilities.md`
- `docs/Source of Truth/invariants.md`
- `docs/Source of Truth/testing-charter.md`
- `docs/Plans/High-Level-Roadmap.md`
- `docs/Plans/Beta-Content-Exploration-Plan.md`
- `docs/Source of Truth/Capability-Gap-Log.md`

## Sprint goal

Improve the scenario tooling/reporting feedback loop before adding more gameplay mechanics. Recent transfer and containment showcases proved that the existing engine/content model is rich enough for interesting beta vignettes, but the agent/headless report surface still makes authors do too much manual inspection to distinguish root-only simulations, persisted scenario runs with player insertion, and carried/contained final state.

This sprint intentionally prioritizes tooling/reporting over new mechanics.

## Selected scope

### Must-have

1. **Run persisted scenarios by scenario ID**
   - Resolves or substantially addresses GAP-002.
   - Add an explicit headless/editor-agent API path such as `RunScenarioById("beta-collector-trader-handoff", turns)` or equivalent request shape.
   - The run must use persisted scenario materialization, including scenario root, player template, player entity ID, player start, active scenario plane, and materialized action plans.
   - Preserve root-only scenario-root simulation as compatibility behavior, but make it clearly distinguishable from persisted scenario/player materialization.

2. **Clear root-only versus persisted-scenario terminology**
   - Resolves or substantially addresses GAP-007 alongside the scenario-ID run path.
   - Reports and API/request names should make it clear whether a run is root-only compatibility simulation or a persisted scenario run with scenario setup/player insertion.

3. **Rich inventory / containment summaries in scenario reports**
   - Resolves or substantially addresses GAP-005.
   - Scenario reports should summarize carried contents in final state, at least one level deep and preferably recursively with cycle guards.
   - Summaries should include contained entity ID/name and inventory coordinates when available.
   - Recursive containment must remain engine-legal; reporting must be cycle-safe rather than restrictive.

### Should-have, only after must-haves land cleanly

4. **Combined preview + persisted scenario simulation report**
   - Addresses GAP-006 if must-have work provides a clean persisted-run and inventory-summary foundation.
   - Compose existing document validation, canonical authoring validation, action-plan preview, scenario materialization, persisted scenario run, turn traces, final state, inventory summary, and capability-gap sections.
   - Do not add new engine mechanics or scenario-only behavior.

5. **Console layout robustness / clipping**
   - Fix the observed Console crash/UX issue when panel content exceeds terminal height.
   - Scope is limited to clipping, skipping lower sections, or showing a compact message instead of throwing from fixed-position rendering.

### Stretch only

6. **Scenario-level initial action-state overrides**
   - GAP-004 remains stretch and should only be promoted if must-have and should-have work lands cleanly and a concrete locked-on/follower scenario needs it.
   - If selected, it requires a separate TDD trace for scenario schema/materialization and validation behavior.

### Explicitly out of scope for this sprint

- Transfer-specific restrictions or guardrails beyond any incidental report clarity. Recursive containment remains legal.
- Template-backed creation/spawn prep or Gate 5 spawning work.
- New gameplay primitives or target-filter semantics.
- Existing checked-in content YAML changes under `src/GameGameGame.Content/**.yaml` unless a later explicit implementation plan selects content fixture updates; the default sprint scope is tooling/reporting and tests.

## First slice: persisted scenario run by scenario ID

### Testable outcomes

- A headless/editor-agent persisted scenario run request by scenario ID uses `ScenarioMaterializer.Materialize(document, scenarioId)` or equivalent shared materialization rather than `MaterializeRootOnly`.
- The report identifies itself as a persisted scenario run and includes scenario ID/name, scenario root, scenario plane, player entity ID/template/start when present, actor order, turn traces, final state, diagnostics, observations, failures, and capability gaps.
- Existing root-only runs remain supported but are labeled as root-only compatibility runs in setup/report text or typed report data.
- A player-involved persisted scenario can be run without test-local manual materialization and custom turn advancement.

### Invariant / test trace

Affected invariants from `docs/Source of Truth/invariants.md`:

- `Persisted scenario definitions materialize through the shared content materialization path, reference normal content templates, insert the selected player at the requested start, and report authoring diagnostics before simulation.`
- `Root-only scenario materialization remains compatibility behavior while supported and must be distinguishable from persisted scenario/player materialization.`
- `Headless scenario runs use shared Content/Core services and schedule contained actors deterministically for scenario-root inventory spaces.`
- `Scenario run reports expose setup, actor order, turn traces, final state, validation diagnostics, runtime observations, runtime failures, and capability gaps.`

Existing tests associated with these invariants:

- `ScenarioMaterializerMaterializesAlphaScenarioWithPlayerInsertion`
- `ScenarioMaterializerReportsAuthoringDiagnostics`
- `ScenarioMaterializerPersistsAndMaterializesAlphaScenarioDefinitionById`
- `ScenarioMaterializerValidatesPersistedAlphaScenarioDefinitions`
- `ConsoleScenarioLauncherBuildsPlayableSessionFromPersistedScenario`
- `ScenarioMaterializerSupportsRootOnlyScenarioCompatibility`
- `ScenarioRunServiceRunsRootInventoryActorsInInitiativeOrder`
- `ScenarioRunServiceShowsBehaviorStepsAndTreatsNoActionAsObservation`
- `ScenarioRunServiceReportsMultiTurnMoveFacingScenario`
- `ScenarioRunnerReportsUnsupportedCapabilityGap`

Planned first failing tests before production code changes:

- Add a scenario-run service test proving a persisted scenario ID run inserts the player and schedules/materializes from persisted scenario output.
- Add an editor/agent API test proving the API exposes a persisted scenario-ID run path and returns the persisted scenario report.
- Add or revise a root-only compatibility report test proving root-only runs are explicitly labeled as root-only compatibility simulations.

## Second slice: inventory / containment summaries

### Testable outcomes

- Final scenario reports include a containment summary section or lines for carried inventory contents.
- The summary includes entity names/IDs and inventory coordinates.
- The summary is cycle-safe.
- Transfer-heavy beta scenarios no longer require direct world-state inspection just to prove where carried entities ended up.

### Invariant / test trace

Affected invariants:

- `Traversals through containment or inventory relationships must be cycle-safe.`
- `Recursive carried weight includes all entities inside the entity's inventory space, plus anything those entities recursively carry.` as a conceptual adjacency only; report traversal should respect the same cycle-safety expectation without changing weight behavior.
- `Scenario run reports expose setup, actor order, turn traces, final state, validation diagnostics, runtime observations, runtime failures, and capability gaps.`

Existing tests associated with these invariants:

- `TraversingRecursiveInventoryWeightIsCycleSafe`
- `RecursiveCarriedWeightIncludesNestedInventoryContents`
- `ScenarioRunServiceReportsMultiTurnMoveFacingScenario`
- `ScenarioRunServiceShowsBehaviorStepsAndTreatsNoActionAsObservation`
- transfer beta fixture tests that currently assert disappearance from the scenario plane or rely on traces/direct state.

Planned failing tests before production code changes:

- Add a report test for one-level contained inventory summary.
- Add a report test for nested or recursive containment summary with cycle guard.
- Update at least one transfer/collector fixture assertion to rely on report-visible containment summary rather than only absence from the scenario plane.

## Should-have readiness gates

Do not start the combined preview+simulation report until:

- persisted scenario-ID run is implemented and tested;
- inventory/containment summaries are implemented and tested;
- report naming distinguishes root-only and persisted scenarios clearly enough that composition will not hide the distinction.

Do not start Console clipping unless it remains clearly isolated from the report/API work or must-have work is already complete.

## Validation plan

Targeted tests during implementation:

```text
dotnet test tests\GameGameGame.Tests\GameGameGame.Tests.csproj --filter "FullyQualifiedName~ScenarioRunReportTests|FullyQualifiedName~ScenarioToolingServiceTests|FullyQualifiedName~BetaContentFixtureTests" -m:1 --no-restore
dotnet test tests\GameGameGame.Editor.Tests\GameGameGame.Editor.Tests.csproj --filter "FullyQualifiedName~AgentContentEditorApiTests" -m:1 --no-restore
```

Relevant broader tests before handoff:

```text
dotnet test tests\GameGameGame.Tests\GameGameGame.Tests.csproj -m:1 --no-restore
dotnet test tests\GameGameGame.Editor.Tests\GameGameGame.Editor.Tests.csproj -m:1 --no-restore
```

Docs to update after implementation changes:

- `docs/Source of Truth/invariants.md` test coverage map if new stable behavior/test names are added.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` for new headless/editor-agent run/report support.
- `docs/Source of Truth/Content-Authoring-Manual.md` for content-editor-facing scenario run/report workflow changes.
- `docs/Source of Truth/Capability-Gap-Log.md` to mark GAP-002/GAP-005/GAP-007 resolved or partially resolved as appropriate.
