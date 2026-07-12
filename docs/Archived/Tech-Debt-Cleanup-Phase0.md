# Tech Debt Cleanup Phase 0

Status: Completed and archived.

Read when:

- reviewing why the Console frontend was deleted;
- reviewing the completed move of `AgentContentEditorApi` out of the retired Avalonia editor project;
- reviewing the destination-first refactor sprint that followed the frontend milestone.

## Phase 0 goals

1. Cost the migration away from `src/GameGameGame.Console` before deciding whether to delete it immediately or keep it temporarily as backup tooling. Result: deleted.
2. Move the non-Avalonia `AgentContentEditorApi` surface out of the former Avalonia editor project so Avalonia can be retired independently. Result: moved to Content, then Avalonia deleted.
3. Preserve intentionally retained legacy systems for now: GIF scenario recording, primitive-backed linked plans, and legacy Action Steps.

## Console migration inventory

| Console concern | Former owner | Replacement / destination | Cost | Result |
|---|---|---|---:|---|
| Direct scenario launch by file and scenario ID | `ConsoleScenarioLauncher`, `Program.cs` | Already covered by `PlayableScenarioLauncher` in Content and SadConsole launch flows | Low | Deleted. |
| Scenario catalog menu | `Program.cs` | SadConsole scenario selection and `ScenarioCatalog` in Content | Low/medium | Deleted; catalog policy/tests remain in Content. |
| `scan-scenarios` CLI | `Program.cs` shell over Content `ScenarioCatalogScanService` | Content-defined scan service owns scan/save policy | Low | Console host deleted; future CLI can wrap `ScenarioCatalogScanService` if needed. |
| `record-scenario` CLI | Removed from Console | Future SadConsole recording/export | Done | Removed now; Headless legacy recorder can remain as an implementation/reference service only while tests or adapters still need it. |
| Manual play loop | `Program.cs` input modes and rendering | SadConsole Simulation mode and Core controlled-command services | Medium | Deleted. |
| Inspect/debug panel formatting | `ConsoleInspectionDisplayFormatter` | Mostly superseded by Content `EntityPanelProjection` and SadConsole panels | Low | Deleted. |
| Key mapping for enter/exit | `ConsolePlayerControls` | SadConsole input layer over Core controlled commands | Low | Deleted. |

### Console decision

Console deletion was not structurally risky because durable launch, catalog, controlled-command, scenario materialization, scenario run, and recording services already live outside Console. The main deletion cost was loss of a simple terminal backup and CLI host.

Completed outcome:

1. `AgentContentEditorApi` moved to Content.
2. `scan-scenarios` policy moved to Content `ScenarioCatalogScanService`.
3. `record-scenario` command was dropped from Console.
4. Console project/source files were deleted.
5. Invariant traces were retargeted away from `ConsoleScenarioLauncher*` and `AlphaScenarioFixtureCanLaunchInConsoleAndAcceptPlayerMove` toward `PlayableScenarioLauncher*` and Content catalog tests.

## AgentContentEditorApi migration plan

Completed destination: Content-owned authoring/API surface rather than Avalonia editor project.

Important dependency finding: `AgentContentEditorApi` depended on Headless scenario run/record services. `GameGameGame.Headless` already referenced `GameGameGame.Content`, so moving the file directly into Content would have created a project cycle if it continued to call Headless directly.

Destination-first split:

| Concern in API file | Destination |
|---|---|
| Document/session authoring operations | `GameGameGame.Content` |
| Agent result/error DTOs | `GameGameGame.Content` |
| Alpha scenario materialization DTO adapter over `ScenarioMaterializer` | `GameGameGame.Content` |
| Scenario run report API | `ScenarioRunService` moved from Headless to Content because it has no image dependency and is a content/scenario feedback service. |
| Scenario recording API | Implementation remains in Headless because it owns ImageSharp GIF/PNG rendering; Content exposes an optional recorder adapter seam. |

TDD / trace for the migration:

- Affected invariant: Content Pipeline invariant for editor operations and agent API authoring through shared content/editor services.
- Existing tests: `AgentContentEditorApiAuthorsCanonicalEnterExitBehavior`, `AgentContentEditorApiRunsPersistedScenarioById`, `AgentContentEditorApiCreatesCombinedPersistedScenarioReport`, `FrontendEditorServiceAndAgentApiShareContentEditorSessionAsParallelSurfaces`.
- First failing-test move: update Agent API tests/project references to consume `GameGameGame.Content.AgentContentEditorApi` without referencing the former Avalonia editor project.
- Implementation: move/split the API and required run-service support so tests pass without building Avalonia.

Completed implementation batch:

1. Moved `ScenarioRunService` and scenario summary/report support into Content, leaving `ScenarioRecordingService` in Headless.
2. Moved `AgentContentEditorApi` and Agent DTOs into Content and removed its direct Headless dependency for runs.
3. Added a Content-owned recording adapter seam rather than pulling ImageSharp into Content.
4. Removed former Avalonia editor references from Agent API tests and deleted Avalonia `EditorViewModelTests` with the Avalonia deletion batch.
