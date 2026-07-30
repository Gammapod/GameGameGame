---
id: plan.sadconsole-play-mode-logging-sprint
title: SadConsole Play-Mode Logging Sprint Plan
kind: sprint-plan
status: active
truth_rank: 50
truth_domains: [frontend-presentation, frontend-boundary, planning-priority]
owners: [frontend-owner]
audience: [frontend-owner, core-owner]
lane: frontend-ux
read_when:
  - implementing Play-mode log displays in SadConsole
  - changing frontend log filtering local activity or hover tooltip behavior
  - deciding whether a logging need requires Core/shared-service coordination
related:
  - source.frontend-ux-invariants
  - source.frontend-ux-standards
  - source.frontend-ux-decisions
  - source.entity-panel-ux-spec
  - plan.actor-pov-inventory-chain-play-layout
---

# SadConsole Play-Mode Logging Sprint Plan

Status: Active focused frontend sprint plan.

## Goal

Add Play-mode logging surfaces that reuse structured engine log projections, use a Core-owned factual query seam for common log selection, and keep SadConsole responsible for presentation scope choices, wording, layout, and input.

Primary outcomes:

1. Core exposes a small canonical `ActionLogProjection` query seam for frontend/shared-service log filtering, ordering, success filtering, and clipping.
2. Content extracts the reusable persisted-scenario/player-narrative row projection behind the existing agent/headless report so future frontends can consume message IDs/args without using tool-specific DTO assembly.
3. `L` cycles the left region between:
   - parent/location chain;
   - full log history, newest first;
   - current-location log history, newest first.
4. Persistent current-region success log/history appears in the main/current-place region.
5. Stretch: mouse hover tooltips show entity name plus recent successful activity.

## Scope decisions

### Toggleable left-region log

The toggleable left-region log shows both successes and failures for its selected filter.

Initial modes:

```csharp
internal enum LeftRegionMode
{
    ParentLocationChain,
    GlobalLog,
    CurrentLocationLog
}
```

Behavior:

- `L` cycles `ParentLocationChain -> GlobalLog -> CurrentLocationLog -> ParentLocationChain`.
- Global log uses the full structured action log.
- Current-location log filters by structured current-place location/plane anchors, not by parsing strings or by looking up an actor's current location after the fact.
- Rows are newest first.
- Rows are clipped to the left-region body height.
- Scrolling is deferred, but the view-model shape should make later scroll offset support straightforward.

Suggested titles:

```text
Parent/location chain
Log: All
Log: Current location
```

### Persistent current-region activity

The persistent main/current-place activity shows only successes.

Behavior:

- Source rows from structured local/current-place log facts.
- Use the shared Core log query helper for `Succeeded == true`, newest-first ordering, de-duplication, and clipping.
- Display independently of the left-region mode.

This surface should be quieter and more player-facing than the toggleable left-region log. Failures remain visible through the toggleable global/current-location log and future debug/detail surfaces.

### Tooltip stretch goal

Mouse hover tooltips show only successful recent activity.

Tooltip content:

- hovered entity name;
- recent successful rows for that entity, if any.

Tooltip constraints:

- no click-to-act;
- no click-to-inspect;
- no mutation of gameplay, inspection, or action state;
- no string parsing to infer log semantics.

Because tooltip placement touches SadConsole mouse/hit-testing/anchored overlay behavior, it is a stretch goal. If no accepted project pattern fits, consult SadConsole documentation/reference material before promoting a durable tooltip pattern, then add a component-gallery example if accepted.

## Existing resources to mine

### Shared engine/content patterns

Useful existing facts/services:

- `SimulationHistorySession` records controlled and autonomous intervals.
- `GameplaySessionController.ActionLog` is built from `ActionLogProjection.FromHistory(...)` and refreshed after submissions/undo.
- `ActionLogProjection.Chronological` provides structured chronological rows.
- `ActionLogProjection.ForEntity(...)` and `ActionLogProjection.ForPlane(...)` provide legacy/simple anchor-based filtering.
- `ActionLogQueryService.Select(...)` is the selected Core-owned query seam for this sprint: use it for entity/plane anchor filtering, optional success/failure filtering, chronological/newest-first ordering, max-row clipping, and de-duplicated combined anchor queries.
- `ActionOutcome` exposes structured fields such as actor, target, success/failure, action kind, sentence, turn number, entity anchors, plane anchors, and action-step attempts.
- `EntityPanelProjection.LocalLog` already carries local panel log rows derived from structured action logs.
- `PlayerNarrativeLogProjection` is the selected Content helper for reusable player-narrative message ID/argument rows behind persisted-scenario agent/headless reports; keep SadConsole/global log displays over `ActionOutcome` unless a later narrative-wording slice explicitly selects message IDs/args for Play mode.

Relevant existing tests include:

- `ActionLogProjectionFiltersOutcomesByEntityAndPlaneAnchors`;
- `ActionLogQuerySelectReturnsEmptyForMissingLog`;
- `ActionLogQuerySelectOrdersAndClipsAfterFiltering`;
- `ActionLogQuerySelectFiltersByFailureEntityAndPlaneAnchors`;
- `ActionLogQuerySelectDeduplicatesRowsMatchingEntityAndPlaneAnchors`;
- `ActionLogProjectionFromHistoryIncludesSuccessfulIntervalsAndCurrentFrameFailuresInOrder`;
- `ActionLogProjectionFromHistoryFiltersProjectedRowsByEntityAndPlane`;
- `ActionLogProjectionFromHistoryIncludesAutonomousActorOutcomes`;
- `PlayerNarrativeLogProjectionProjectsStructuredRowsFromHistory`;
- `LocalActivityViewBuilderTests`.

### SadConsole/frontend patterns

Useful existing frontend seams:

- `ConsumerPlayModeScreen.BuildRenderFrame(...)` composes main components, debug components, diagnostics chrome, and prompt overlays.
- `ActorPovPlayComponentFactory.ParentChainComponents(...)` owns the current left parent/location chain component set.
- `ActorPovPlayLayout.ParentChain` provides the left-region bounds.
- `PanelComponent` is enough for first-pass clipped display-only log rows.
- `SelectableListComponent` has selection/scroll-offset behavior that can inform future scrollable logs, but the initial log can remain display-only.
- `ActionOutcomeTextFormatter.FormatGlobal(...)` and `FormatLocal(...)` can seed initial row wording.
- `InventorySpaceComponent.CellBounds(...)` and linked-space hit regions provide geometry needed for future hover hit testing.
- `GameplayMockConsole.ProcessMouse(...)` is the existing minimal SadConsole mouse plumbing reference.

### Design/documentation guidance

Relevant source-of-truth guidance:

- Frontends do not invent simulation semantics; logs derive from structured outcomes, not parsed display strings.
- Frontend state may own selected filter mode, open/closed state, scroll offsets, hover state, and wording. Factual row selection should use shared structured query/projection helpers rather than frontend-local semantic filtering.
- Global logs should be concise and may include all simulated entities from structured history projection.
- Local activity should converge contents and local log context, prioritizing recent local entities.
- Mouse should be a convenience layer over coherent keyboard behavior.
- Hit testing should reuse centralized layout/cell geometry rather than duplicating render math.

## Phase 0: Core/Content log seam consolidation

Before making broad frontend changes, add the small shared seams that make structured log consumption consistent across SadConsole and future frontends. This is intentionally a narrow consolidation, not a broad history or outcome rewrite.

### Phase 0A: Core-owned action-log query helper

Implementation status: Selected/initial helper added as `ActionLogQueryService.Select(...)` in Core, with tests for missing logs, success/failure filtering, entity/plane anchor filtering, newest-first ordering, clipping, and combined-anchor de-duplication. Treat this as the canonical row-selection seam for the required SadConsole logging surfaces unless implementation discovers a missing query capability.

The Core helper centralizes common frontend log selection concerns:

- filter by one or more entity anchors;
- filter by one or more plane anchors;
- optional success/failure filter;
- chronological or newest-first ordering;
- optional max-row clipping;
- de-duplicate rows when a query combines entity and plane anchors.

Suggested shape:

```csharp
public enum ActionLogOrder
{
    Chronological,
    NewestFirst
}

public sealed record ActionLogQuery(
    IReadOnlySet<EntityId>? EntityAnchors = null,
    IReadOnlySet<PlaneId>? PlaneAnchors = null,
    bool? Succeeded = null,
    ActionLogOrder Order = ActionLogOrder.Chronological,
    int? MaxRows = null);

public static class ActionLogQueryService
{
    public static IReadOnlyList<ActionOutcome> Select(
        ActionLogProjection? actionLog,
        ActionLogQuery query);
}
```

An equivalent `ActionLogProjection.Query(...)` method is acceptable if it keeps the same ownership boundary: Core owns factual selection over structured outcomes, while frontend presentation owns wording, layout, scrolling, hover state, and color.

Tests:

- null/missing action log returns empty rows;
- newest-first ordering reverses chronological rows without mutating the source projection;
- success-only and failure-only filters work;
- entity and plane anchor filters work independently;
- combined entity/plane filters de-duplicate rows that match both;
- max-row clipping applies after ordering/filtering.

Trace affected stable invariants before implementation:

- `Structured action outcome/log projection` / `History-backed log projection derives chronological structured outcomes...` in `docs/Source of Truth/invariants.md`.
- Existing tests: `ActionLogProjectionFiltersOutcomesByEntityAndPlaneAnchors`, `ActionLogProjectionFromHistoryIncludesSuccessfulIntervalsAndCurrentFrameFailuresInOrder`, `ActionLogProjectionFromHistoryFiltersProjectedRowsByEntityAndPlane`, `ActionLogProjectionFromHistoryIncludesAutonomousActorOutcomes`.

### Phase 0B: Content player-narrative row projection extraction

Implementation status: Selected/initial helper added as `PlayerNarrativeLogProjection` in Content and `ScenarioPlayerLogService` delegates to it for agent/headless report assembly. This helper is adjacent support for shared narrative row/message-ID projection; the required SadConsole log panels should still consume `ActionLogQueryService`/`ActionOutcome` rows unless a later wording slice chooses narrative IDs for Play mode.

Extracted reusable non-agent-specific parts of `ScenarioPlayerLogService.ProjectRows(...)` into a Content-level projection helper shaped like:

```csharp
public sealed record PlayerNarrativeLogProjectionRequest(
    SimulationHistorySession History,
    EntityId ObserverEntityId);

public static class PlayerNarrativeLogProjection
{
    public static IReadOnlyList<PlayerNarrativeLogRow> Project(PlayerNarrativeLogProjectionRequest request);
}
```

The exact names may differ, but the helper should:

- consume shared `SimulationHistorySession` / `ActionOutcomeProjection` facts;
- return stable message IDs, result, actor identity, step kind/index, args, turn/order fields, and known structured anchors;
- keep `ScenarioPlayerLogService` as the agent/headless report assembler rather than the only place where message IDs/args are projected;
- preserve the current caveat that this is a `player narrative projection`, not true line-of-sight/audibility.

Tests:

- extracted projection preserves the existing scenario player narrative rows/message IDs;
- `ScenarioPlayerLogService` delegates to the extracted projection without changing the existing agent report contract;
- rows are still derived from structured history/action-step projection rather than formatted trace-line parsing.

Trace affected stable invariants before implementation:

- `Persisted scenario player narrative log reports expose a compact player narrative projection...` in `docs/Source of Truth/invariants.md`.
- Existing tests: `AgentContentEditorApiRunsPersistedScenarioPlayerNarrativeLogById`, `ContentToolDispatcherRunsScenarioPlayerNarrativeLogById`.

### Audit result and known limits

After Phase 0A/0B, the required sprint scope should use shared helpers for:

- global log;
- current-location log by plane anchor;
- success/failure filtering;
- entity tooltip rows by entity anchor.

Known limit: autonomous outcomes currently have less-rich source/destination/target anchors than controlled-command outcomes. Current-location filtering should use available structured plane anchors and must not claim exact player perception, audibility, or full source/destination semantics until Core history/outcome projection grows those facts.

Coordinate with `core-owner` if any of these are missing or awkward:

1. Exact "since controlled actor's previous turn" interval support if Last Turn filtering is promoted into this sprint.
2. Richer actor/source/destination/location anchors for autonomous outcomes.
3. Structured actor/target/actee naming facts beyond current display names and entity IDs.

Sprint decision: do the narrow Core query helper and Content narrative-row extraction first; avoid any broader engine/history refactor unless this sprint discovers a real shared-service gap. Exact Last Turn semantics are not required for this logging sprint unless selected during implementation.

## Phase 1: Frontend log view models

Add SadConsole-owned presentation builders near the existing frontend view-model helpers.

Suggested types:

```csharp
internal enum PlayLogScope
{
    Global,
    CurrentLocation
}

internal sealed record PlayLogRow(
    string Text,
    bool Succeeded,
    EntityId? ActorId = null);

internal static class PlayLogViewBuilder
{
    public static IReadOnlyList<PlayLogRow> Build(
        ActionLogProjection? actionLog,
        PlayLogScope scope,
        PlaneId? currentLocationPlaneId,
        int maxRows);
}
```

Behavior:

- Global mode uses the Phase 0A Core log query helper with no anchors.
- Current-location mode uses the Phase 0A Core log query helper with the current-place plane anchor.
- Request `ActionLogOrder.NewestFirst` from the shared helper rather than manually reversing rows.
- Include successes and failures.
- Format with existing structured-outcome formatters or a narrow new presentation formatter.
- Request clipping through the shared helper or apply only final viewport clipping if the builder needs more rows for later scrolling; avoid duplicating filtering/order semantics.
- Provide an honest empty state.
- Do not parse formatted strings to infer success, failure, anchors, or action semantics.

Tests:

- newest first;
- global includes successes and failures;
- current-location filters by plane anchors;
- clips to max rows;
- empty/missing action log is honest.

## Phase 2: Wire `L` left-region cycle

Add left-region presentation state to `ConsumerPlayModeScreen`.

Suggested API:

```csharp
public LeftRegionMode LeftRegionMode { get; }
public string CycleLeftRegionMode();
```

Wire `L` in `ConsumerPlayModeConsole.ProcessKeyboard`.

Rendering direction:

- `ParentLocationChain`: keep existing `ActorPovPlayComponentFactory.ParentChainComponents(...)`.
- `GlobalLog`: replace parent-chain components with a log `PanelComponent` in the same region bounds.
- `CurrentLocationLog`: replace parent-chain components with a current-place filtered log `PanelComponent` in the same region bounds.

This may be done by adding an overload/options object to `ActorPovPlayComponentFactory.MainComponents(...)` or by composing the left-region replacement in `ConsumerPlayModeScreen` before returning the component list. Preserve current non-left regions and prompt/debug overlay behavior.

Footer/status should mention `L: left region` once the key is live.

Tests:

- default mode is parent/location chain;
- first `L` shows global log panel;
- second `L` shows current-location log panel;
- third `L` returns to parent/location chain;
- log panel rows are newest first and clipped;
- toggleable logs include successes and failures.

## Phase 3: Persistent current-region success activity

Add a success-only local/current-place activity builder.

Suggested type:

```csharp
internal static class CurrentRegionActivityViewBuilder
{
    public static IReadOnlyList<string> Build(
        EntityPanelProjection? currentPlace,
        int maxRows);
}
```

Behavior:

- Source from `currentPlace.LocalLog` or the underlying `ActionLogProjection`, but use the Phase 0A Core log query helper for success-only filtering, newest-first ordering, de-duplication, and clipping semantics.
- If using `currentPlace.LocalLog`, wrap it with `ActionLogProjection.FromOutcomes(...)` before querying rather than hand-rolling equivalent LINQ.
- Keep empty state quiet and player-facing.

Implementation note: ensure consumer Play projections pass `_sessionController.ActionLog` into `EntityPanelProjectionService.Project(...)` where needed. Some current `RefreshProjections()` calls project without an action log, which can leave `LocalLog` empty.

Tests:

- current-region activity includes successful local outcomes;
- excludes failures;
- newest first;
- remains independent of left-region mode;
- rows remain clipped to available space.

## Phase 4: Manual smoke and UX adjustment

Manual checks:

1. Launch a scenario with enough activity to produce multiple log rows.
2. Move/wait enough to generate controlled and autonomous outcomes.
3. Press `L` repeatedly and confirm the left region cycles correctly.
4. Confirm full/current-location log modes include failures and successes.
5. Confirm persistent current-region activity shows successes only.
6. Confirm newest entries appear at the top.
7. Confirm prompt overlays and debug overlays still draw above normal content.
8. Confirm no gameplay behavior changes.

## Stretch Phase 5: Mouse hover tooltip model

First implement a pure hover model before rendering tooltips.

Suggested type:

```csharp
internal sealed record PlayEntityHoverInfo(
    EntityId EntityId,
    string Name,
    SadConsoleRect AnchorBounds,
    IReadOnlyList<string> SuccessRows);
```

Data sources:

- visible `InventorySpaceComponent`s;
- `InventorySpaceComponent.CellBounds(...)`;
- entity visuals in `InventorySpaceViewModel`;
- the Phase 0A Core log query helper with the hovered entity anchor, `Succeeded == true`, `ActionLogOrder.NewestFirst`, and a tooltip-sized max-row limit.

Behavior:

- Hover over occupied cell returns entity name and successful recent rows.
- Hover over empty cell returns no tooltip.
- Failures are excluded.
- Prompt overlay may suppress tooltip display.
- Hover state remains transient presentation state.
- Tooltip rows must not parse rendered strings for success/failure or entity identity.

If rendering is attempted in this sprint:

- use a small anchored panel;
- clamp inside drawable bounds;
- draw topmost above main components/connectors;
- avoid covering prompt overlays or suppress while prompts are active;
- add/update a component-gallery example if the pattern is accepted.

Tests:

- hover over occupied visible cell returns the expected entity;
- hover over empty cell returns null;
- success rows exclude failures;
- hit testing uses component/cell geometry rather than duplicated simulation rules.

## Test plan

Run focused Core and Content tests for Phase 0 before frontend work:

```powershell
dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj --filter "FullyQualifiedName~ActionOutcomeProjectionTests|FullyQualifiedName~SimulationHistorySessionTests|FullyQualifiedName~ScenarioToolingServiceTests|FullyQualifiedName~AgentContentEditorApiTests|FullyQualifiedName~ContentToolDispatcherTests"
```

Run focused SadConsole tests after each frontend phase:

```powershell
dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj
```

If Core/log projection changes are made:

```powershell
dotnet test tests/GameGameGame.Tests/GameGameGame.Tests.csproj
dotnet test tests/GameGameGame.SadConsole.Tests/GameGameGame.SadConsole.Tests.csproj
```

## Definition of done

Required:

- Core exposes a tested canonical action-log query helper for anchor filtering, success/failure filtering, ordering, clipping, and de-duplication.
- Content exposes a reusable player-narrative row projection helper behind the existing persisted-scenario player-log service/tool contract.
- `L` cycles the left region through parent chain, global log, and current-location log.
- Toggleable logs show successes and failures.
- Toggleable logs are newest first and clipped.
- Current-location toggle filters via structured location/plane anchors.
- Persistent current-region activity exists and shows successes only.
- Frontend does not parse display strings for semantics.
- Tests cover log builders and mode cycling.
- Existing agent/headless player narrative log reports keep their current contract while delegating row projection to the shared Content helper.

Stretch:

- Mouse hover can identify visible entities.
- Tooltip shows entity name plus successful recent activity.
- Tooltip behavior is presentation-only and does not mutate inspection/action state.

## Follow-ups intentionally deferred

- Scrollable global/current-location logs.
- Exact Last Turn filtering since the controlled actor's previous completed turn.
- Richer grouped local activity rows by initiative/actor, beyond the success-only current-region display.
- Click-to-inspect or click-to-act mouse behavior.
- Durable anchored tooltip/gallery pattern if not completed during the stretch slice.

## Implementation friction log

- 2026-07-30 Phase 0A TDD test run was blocked before compilation by locked `GameGameGame.Core.dll` / `GameGameGame.Content.dll` copies under `src/GameGameGame.Content.Tools/bin/Debug/net10.0`; the lock holder was `GameGameGame.Content.Tools` process `28952`. Mitigation: record the lock here, stop the stale local tool-host process, and rerun the targeted test command so the intentionally failing compile/test result can be observed before production implementation.
