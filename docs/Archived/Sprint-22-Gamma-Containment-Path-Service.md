# Sprint 22: Gamma Containment Path Service

Status: Archived completed plan. Core structural containment path foundation implemented; Console breadcrumb rendering remains the next Gamma follow-up.

Related source of truth:

- `docs/Source of Truth/invariants.md` records stable behavior contracts and test traces.
- `docs/Source of Truth/testing-charter.md` records TDD expectations.
- `docs/Source of Truth/Engine-Editor-Capabilities.md` records implemented support status after this plan.
- `docs/Plans/Gamma-Frontend-Demo-Plan.md` records the active Gamma frontend/demo direction and follow-up stages.

## Goal

Add a Core-only, structural containment path service that future Console breadcrumb display, inspection panels, editor/debug tooling, and frontend experiments can consume without embedding UI formatting or presentation data in Core.

## Scope completed

- Added `EntityContainmentPathService` in Core.
- Added structural upward ancestry paths with entity-only segments containing:
  - entity ID;
  - containing plane ID when the segment is inside an entity inventory plane;
  - coordinate in containing plane;
  - container entity ID.
- Added max-depth truncation for “show N breadcrumbs upward” use cases.
- Added missing/unlocated/non-throwing path statuses.
- Added cycle detection with directional cycle edges from container entity to contained entity via the inventory plane.
- Added root-relative paths for UI cases like dungeon-root-to-player or scenario-root-to-inspected-entity.
- Added shared-root two-branch paths for future “show path to home vs current inspected entity” style navigation.
- Kept the service Core-only and structural: no Console rendering, display strings, glyph/color enrichment, content curation, or gameplay semantics were added.

## TDD and test trace

Affected invariant:

- `Traversals through containment or inventory relationships must be cycle-safe.`

Existing related tests before this sprint:

- `TraversingRecursiveInventoryWeightIsCycleSafe`
- `ScenarioInventorySummaryFormatterIsCycleSafe`

New tests:

- `EntityContainmentPathServiceBuildsUpwardPathForNestedEntity`
- `EntityContainmentPathServiceLimitsUpwardPathByMaxDepth`
- `EntityContainmentPathServiceReportsMissingEntity`
- `EntityContainmentPathServiceDetectsContainmentCycle`
- `EntityContainmentPathServiceReportsCycleEdgesWithDirection`
- `EntityContainmentPathServiceBuildsPathFromKnownRoot`
- `EntityContainmentPathServiceReportsEntityNotUnderRoot`
- `EntityContainmentPathServiceCanLimitRootRelativePathByMaxDepth`
- `EntityContainmentPathServiceFindsSharedRootForTwoEntities`
- `EntityContainmentPathServiceReturnsTwoBranchesFromSharedRoot`
- `EntityContainmentPathServiceReportsNoSharedRoot`
- `EntityContainmentPathServiceSharedRootPathIsCycleSafe`

## Validation

Targeted containment path suite:

```powershell
dotnet test "tests\GameGameGame.Tests\GameGameGame.Tests.csproj" --filter EntityContainmentPath --artifacts-path "C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-artifacts-turn5-targeted"
```

Result: Passed 12, failed 0.

Editor suite:

```powershell
dotnet test "tests\GameGameGame.Editor.Tests\GameGameGame.Editor.Tests.csproj" --artifacts-path "C:\Users\Scramble\AppData\Local\Temp\opencode\ggg-artifacts-turn5-editor"
```

Result: Passed 69, failed 0.

Main suite after build artifact lock workaround:

```powershell
dotnet test "tests\GameGameGame.Tests\GameGameGame.Tests.csproj" --no-build
```

Result: Passed 304, failed 0.

Note: a normal rebuild hit a locked `GameGameGame.Console.dll` artifact after the intentionally failing pre-cycle-detection test hung. The code/test validation above passed using temp artifacts and `--no-build` for the full main suite.

## Deferred follow-up

- Console breadcrumb display for current player and inspected entity using the shared Core path service.
- Optional Content/presentation enrichment layer for names/glyphs/colors if a frontend needs it.
- Inspection panel polish using path, inventory/current-space summaries, local turn order, and previous-action information.
- Interactive breadcrumb navigation and collapsible inspection-chain panels remain deferred to a real frontend engine unless Gamma feedback re-promotes them.
