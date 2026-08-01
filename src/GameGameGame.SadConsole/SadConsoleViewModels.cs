using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;

namespace GameGameGame.SadConsoleApp;

internal sealed record SadConsoleSessionView(
    string Header,
    string Message,
    string AffordanceSummary,
    string SelectedSummary,
    string PromptHint,
    IReadOnlyList<SadConsolePanelView> Panels,
    SadConsoleLogView GlobalLog,
    ControlledActorAffordances Affordances);

internal sealed record SadConsolePanelView(
    string Title,
    EntityPanelProjection Projection,
    SadConsoleRect Bounds,
    GridCoord? Cursor,
    bool IsCollapsed = false);

internal sealed record SadConsoleLogView(
    string Title,
    SadConsoleRect Bounds,
    string EmptyText,
    IReadOnlyList<ActionOutcome> Rows);

internal sealed record SadConsoleSessionViewBuilderState(
    ShellMode Mode,
    string Message,
    EntityId? SelectedEntity,
    EntityId? InspectedEntity,
    GridCoord WorldCursor,
    GridCoord InventoryCursor,
    ActionLogProjection? ActionLog,
    bool CanUndo = false);

internal readonly record struct SadConsoleRect(int Left, int Top, int Width, int Bottom)
{
    public int Height => Math.Max(0, Bottom - Top);

    public bool Contains(int x, int y) =>
        x >= Left && x < Left + Width && y >= Top && y < Bottom;

    public static SadConsoleRect FromSize(int left, int top, int width, int height) =>
        new(left, top, width, top + height);
}

internal static class SadConsoleSessionLayout
{
    private const int PanelLeft = 1;
    private const int PanelTop = 6;
    private const int PanelBottom = 32;
    private const int PanelGap = 2;
    private const int CollapsedPanelWidth = 14;
    private const int PanelAreaWidth = SadConsoleScreenMetrics.ScreenWidth - 2;

    public static SadConsoleRect GlobalLogRect => new(1, 33, SadConsoleScreenMetrics.ScreenWidth - 2, SadConsoleScreenMetrics.ScreenHeight);

    public static IReadOnlyList<SadConsolePanelSlot> BuildPanelChainSlots(int panelCount)
    {
        if (panelCount <= 0)
        {
            return [];
        }

        if (panelCount == 1)
        {
            return [new SadConsolePanelSlot(new SadConsoleRect(PanelLeft, PanelTop, PanelAreaWidth, PanelBottom), false)];
        }

        if (panelCount <= 3)
        {
            var width = (PanelAreaWidth - (PanelGap * (panelCount - 1))) / panelCount;
            return Enumerable.Range(0, panelCount)
                .Select(index => new SadConsolePanelSlot(new SadConsoleRect(PanelLeft + (index * (width + PanelGap)), PanelTop, width, PanelBottom), false))
                .ToList();
        }

        var expandedCount = panelCount - 1;
        var expandedWidth = (PanelAreaWidth - CollapsedPanelWidth - (PanelGap * (panelCount - 1))) / expandedCount;
        var slots = new List<SadConsolePanelSlot>
        {
            new(new SadConsoleRect(PanelLeft, PanelTop, CollapsedPanelWidth, PanelBottom), true)
        };

        var left = PanelLeft + CollapsedPanelWidth + PanelGap;
        for (var index = 1; index < panelCount; index++)
        {
            slots.Add(new SadConsolePanelSlot(new SadConsoleRect(left, PanelTop, expandedWidth, PanelBottom), false));
            left += expandedWidth + PanelGap;
        }

        return slots;
    }
}

internal sealed record SadConsolePanelSlot(SadConsoleRect Bounds, bool IsCollapsed);

internal static class SadConsolePanelChainViewBuilder
{
    public static IReadOnlyList<EntityId> BuildPanelChain(EntityContainmentPath breadcrumb)
    {
        var chain = breadcrumb.Segments.Select(segment => segment.EntityId).ToList();
        if (chain.Count == 0)
        {
            chain.Add(breadcrumb.RequestedEntityId);
        }

        return chain;
    }

    public static IReadOnlyList<EntityId> BuildVisiblePanelChain(IReadOnlyList<EntityId> chain)
    {
        if (chain.Count <= 4)
        {
            return chain;
        }

        return chain.Take(1).Concat(chain.TakeLast(3)).ToList();
    }

    public static string PanelTitle(int index, EntityId entityId, EntityId currentContainerId, EntityId inspectedEntityId, int omittedCount)
    {
        var role = entityId == inspectedEntityId
            ? "Inspection"
            : entityId == currentContainerId
                ? "Current Container"
                : index == 0
                    ? "Root"
                    : "Ancestor";
        return omittedCount > 0 && index == 0 ? $"{role} (+{omittedCount})" : role;
    }
}

internal sealed class SadConsoleSessionViewBuilder(
    EntityPanelProjectionService panelProjection,
    ControlledActorAffordanceService affordances)
{
    public SadConsoleSessionView Build(PlayableScenarioSession session, SadConsoleSessionViewBuilderState state)
    {
        var world = session.World;
        var actorAffordances = affordances.Query(world, session.PlayerEntityId);
        var panels = BuildPanelChainViews(session, state);
        var selectedSummary = state.SelectedEntity is { } selected
            ? $"Selected: {world.FormatEntityAddress(selected)}"
            : "Selected: none";

        return new SadConsoleSessionView(
            $"GameGameGame SadConsole | {session.Name} | Turn {world.TurnNumber} | Mode {state.Mode}",
            state.Message,
            FormatAffordances(actorAffordances),
            selectedSummary,
            FormatPromptHint(state.Mode, state.SelectedEntity, actorAffordances, state.CanUndo),
            panels,
            new SadConsoleLogView(
                "Global action log",
                SadConsoleSessionLayout.GlobalLogRect,
                "No action outcomes recorded yet.",
                state.ActionLog?.Chronological ?? []),
            actorAffordances);
    }

    private IReadOnlyList<SadConsolePanelView> BuildPanelChainViews(PlayableScenarioSession session, SadConsoleSessionViewBuilderState state)
    {
        var inspectedEntityId = state.InspectedEntity ?? session.PlayerEntityId;
        var inspectedProjection = panelProjection.Project(session.World, inspectedEntityId, session.ActionPlans, session.PlayerEntityId, state.ActionLog);
        var fullChainEntityIds = SadConsolePanelChainViewBuilder.BuildPanelChain(inspectedProjection.Breadcrumb);
        var chainEntityIds = SadConsolePanelChainViewBuilder.BuildVisiblePanelChain(fullChainEntityIds);
        var slots = SadConsoleSessionLayout.BuildPanelChainSlots(chainEntityIds.Count);
        var currentContainerId = CurrentContainerEntityId(session);
        var omittedCount = Math.Max(0, fullChainEntityIds.Count - chainEntityIds.Count);
        var result = new List<SadConsolePanelView>();

        for (var index = 0; index < chainEntityIds.Count; index++)
        {
            var entityId = chainEntityIds[index];
            var projection = entityId == inspectedProjection.EntityId
                ? inspectedProjection
                : panelProjection.Project(session.World, entityId, session.ActionPlans, session.PlayerEntityId, state.ActionLog);
            var title = SadConsolePanelChainViewBuilder.PanelTitle(index, entityId, currentContainerId, inspectedEntityId, omittedCount);
            var cursor = CursorForPanel(state, entityId, currentContainerId, inspectedEntityId);
            result.Add(new SadConsolePanelView(title, projection, slots[index].Bounds, cursor, slots[index].IsCollapsed));
        }

        return result;
    }

    private static GridCoord? CursorForPanel(SadConsoleSessionViewBuilderState state, EntityId entityId, EntityId currentContainerId, EntityId inspectedEntityId)
    {
        if (UsesWorldCursor(state.Mode) && entityId == currentContainerId)
        {
            return state.WorldCursor;
        }

        if (UsesInventoryCursor(state.Mode) && entityId == inspectedEntityId)
        {
            return state.InventoryCursor;
        }

        return null;
    }

    private static EntityId CurrentContainerEntityId(PlayableScenarioSession session)
    {
        var playerPlaneId = session.World.GetEntityLocation(session.PlayerEntityId).PlaneId;
        return InventoryPlaneOwnership.TryFindOwner(session.World, playerPlaneId, out var containerId)
            ? containerId
            : session.PlayerEntityId;
    }

    private static bool UsesWorldCursor(ShellMode mode) => mode is ShellMode.PickupSource or ShellMode.DropDestination or ShellMode.InspectSource or ShellMode.EnterSource or ShellMode.ExitDirection;
    private static bool UsesInventoryCursor(ShellMode mode) => mode is ShellMode.PickupDestination or ShellMode.DropSource;

    private static string FormatAffordances(ControlledActorAffordances affordances)
    {
        var moves = string.Join(", ", affordances.MovementDirections.Where(a => a.CanExecute).Select(a => a.Direction));
        return $"Valid moves: {(string.IsNullOrWhiteSpace(moves) ? "none" : moves)} | pickups: {affordances.PickupSources.Count(a => a.CanExecute)} | drops: {affordances.DropSources.Count(a => a.CanExecute)} | enter: {affordances.EnterTargets.Count(a => a.CanExecute)} | exit: {affordances.ExitDirections.Count(a => a.CanExecute)}";
    }

    private static string FormatPromptHint(ShellMode mode, EntityId? selectedEntity, ControlledActorAffordances affordances, bool canUndo)
    {
        return mode switch
        {
            ShellMode.Play => $"Arrows move. I inspect. P pickup. D drop. E enter. X exit. U undo ({(canUndo ? "available" : "unavailable at frame 0")}). Highlights: green valid action target, red blocked move, blue controlled entity, purple current target, gold cursor. Facing/target appear in text.",
            ShellMode.PickupSource => FormatEntityAffordanceHint("Pickup source", affordances.PickupSources),
            ShellMode.PickupDestination when selectedEntity is { } target => FormatDestinationAffordanceHint("Pickup destination", affordances.PickupDestinations(target)),
            ShellMode.DropSource => FormatEntityAffordanceHint("Drop source", affordances.DropSources),
            ShellMode.DropDestination when selectedEntity is { } target => FormatDestinationAffordanceHint("Drop destination", affordances.DropDestinations(target)),
            ShellMode.EnterSource => FormatEntityAffordanceHint("Enter target", affordances.EnterTargets),
            ShellMode.ExitDirection => FormatDirectionAffordanceHint("Exit", affordances.ExitDirections),
            ShellMode.InspectSource => "Inspect: gold cursor selects visible entities in the current container panel.",
            _ => string.Empty
        };
    }

    private static string FormatEntityAffordanceHint(string label, IReadOnlyList<ControlledActorEntityAffordance> affordances)
    {
        var valid = affordances.Count(affordance => affordance.CanExecute);
        var blocked = affordances.FirstOrDefault(affordance => !affordance.CanExecute && !string.IsNullOrWhiteSpace(affordance.FailureDetail));
        return blocked is null
            ? $"{label}: {valid} valid highlighted target(s)."
            : $"{label}: {valid} valid target(s). Blocked: {blocked.FailureReason} {blocked.FailureDetail}";
    }

    private static string FormatDestinationAffordanceHint(string label, IReadOnlyList<ControlledActorDestinationAffordance> affordances)
    {
        var valid = affordances.Count(affordance => affordance.CanExecute);
        var blocked = affordances.FirstOrDefault(affordance => !affordance.CanExecute && !string.IsNullOrWhiteSpace(affordance.FailureDetail));
        return blocked is null
            ? $"{label}: {valid} valid highlighted cell(s)."
            : $"{label}: {valid} valid cell(s). Blocked: {blocked.FailureReason} {blocked.FailureDetail}";
    }

    private static string FormatDirectionAffordanceHint(string label, IReadOnlyList<ControlledActorDirectionAffordance> affordances)
    {
        var valid = affordances.Count(affordance => affordance.CanExecute);
        var blocked = affordances.FirstOrDefault(affordance => !affordance.CanExecute && !string.IsNullOrWhiteSpace(affordance.FailureDetail));
        return blocked is null
            ? $"{label}: {valid} valid highlighted direction(s)."
            : $"{label}: {valid} valid direction(s). Blocked: {blocked.FailureReason} {blocked.FailureDetail}";
    }
}

internal sealed record PromptChoiceCycleResult(GridCoord Cursor, string Message, bool HasChoice);

internal static class PromptChoiceCycler
{
    public static PromptChoiceCycleResult Cycle(
        IEnumerable<PlaneCoord?> candidates,
        PlaneId planeId,
        GridCoord currentCursor,
        string label)
    {
        var coords = OrderedDistinctCoords(candidates, planeId);
        if (coords.Count == 0)
        {
            return new PromptChoiceCycleResult(currentCursor, $"{label}: no valid choices.", false);
        }

        var index = coords.FindIndex(coord => coord == currentCursor);
        var cursor = coords[(index + 1 + coords.Count) % coords.Count];
        return new PromptChoiceCycleResult(cursor, $"{label}: selected {cursor}. Tab cycles, Enter confirms.", true);
    }

    public static GridCoord? FirstValidCoord(IEnumerable<PlaneCoord?> candidates, PlaneId planeId) =>
        OrderedDistinctCoords(candidates, planeId).Cast<GridCoord?>().FirstOrDefault();

    private static List<GridCoord> OrderedDistinctCoords(IEnumerable<PlaneCoord?> candidates, PlaneId planeId) => candidates
        .Where(candidate => candidate?.PlaneId == planeId)
        .Select(candidate => candidate!.Value.Coord)
        .Distinct()
        .OrderBy(coord => coord.Y)
        .ThenBy(coord => coord.X)
        .ToList();
}

internal sealed record LocalActivityRow(string Text, bool IsHeader = false, bool IsPositive = false, bool IsWarning = false, bool IsMuted = false);

internal enum PlayLogScope
{
    Global,
    CurrentLocation
}

internal enum LeftRegionMode
{
    ParentLocationChain,
    GlobalLog,
    CurrentLocationLog
}

internal sealed record PlayLogRow(string Text, bool Succeeded, EntityId? ActorId = null, bool IsMuted = false);

internal static class ActionOutcomeTextFormatter
{
    public static string FormatGlobal(ActionOutcome outcome)
    {
        var turn = outcome.TurnNumber is { } turnNumber ? $"T{turnNumber}: " : string.Empty;
        var status = outcome.Succeeded ? "OK" : "FAIL";
        return $"{turn}{status}: {FormatCore(outcome)}";
    }

    public static string FormatLocal(ActionOutcome outcome)
    {
        var status = outcome.Succeeded ? "OK" : "FAIL";
        return $"{status}: {FormatCore(outcome)}";
    }

    private static string FormatCore(ActionOutcome outcome)
    {
        var noTurn = outcome.ConsumedTurn ? string.Empty : " (no turn)";
        var attempt = outcome.Succeeded ? string.Empty : FormatFailedAttempt(outcome.ActionStepAttempts);
        return $"{outcome.Sentence}{noTurn}{attempt}";
    }

    private static string FormatFailedAttempt(IReadOnlyList<ActionStepAttempt> attempts)
    {
        var failed = attempts.FirstOrDefault(attempt => attempt.Status == TraceStatus.Failure);
        if (failed is null)
        {
            return string.Empty;
        }

        var reason = !string.IsNullOrWhiteSpace(failed.Detail)
            ? failed.Detail
            : failed.FailureReason?.ToString();
        return string.IsNullOrWhiteSpace(reason)
            ? $" [{failed.StepKind} failed]"
            : $" [{failed.StepKind} failed: {reason}]";
    }
}

internal static class LocalActivityViewBuilder
{
    public const string EmptyText = "No visible contents or local action snippets.";

    public static IReadOnlyList<LocalActivityRow> Build(EntityPanelProjection panel, int maxRows)
    {
        if (maxRows <= 0)
        {
            return [];
        }

        var rows = new List<LocalActivityRow> { new("Local activity", IsHeader: true) };
        if (panel.Contents.Count == 0)
        {
            AddIfRoom(rows, new LocalActivityRow(EmptyText, IsMuted: true), maxRows);
            return rows;
        }

        foreach (var row in panel.Contents)
        {
            if (!AddIfRoom(rows, new LocalActivityRow($"{row.Order}. {row.Glyph} {row.EntityName} [{row.Participation}]"), maxRows))
            {
                return rows;
            }

            var latestOutcome = panel.LocalLog.LastOrDefault(outcome => outcome.AnchorEntityIds.Contains(row.EntityId));
            if (latestOutcome is not null)
            {
                if (!AddOutcomeRows(rows, latestOutcome, maxRows))
                {
                    return rows;
                }
            }
            else if (!string.IsNullOrWhiteSpace(row.PreviousAction)
                     && !AddIfRoom(rows, new LocalActivityRow($"└ {row.PreviousAction}", IsPositive: true), maxRows))
            {
                return rows;
            }
        }

        return rows;
    }

    private static bool AddOutcomeRows(List<LocalActivityRow> rows, ActionOutcome outcome, int maxRows)
    {
        if (outcome.ActionStepAttempts.Count == 0)
        {
            return AddIfRoom(rows, new LocalActivityRow($"└ {ActionOutcomeTextFormatter.FormatLocal(outcome)}", IsPositive: outcome.Succeeded, IsWarning: !outcome.Succeeded), maxRows);
        }

        for (var index = 0; index < outcome.ActionStepAttempts.Count; index++)
        {
            var attempt = outcome.ActionStepAttempts[index];
            var connector = index == outcome.ActionStepAttempts.Count - 1 ? "└" : "├";
            if (!AddIfRoom(rows, new LocalActivityRow(
                    $"{connector} {FormatAttempt(attempt)}",
                    IsPositive: attempt.Status == TraceStatus.Success,
                    IsWarning: attempt.Status == TraceStatus.Failure),
                    maxRows))
            {
                return false;
            }
        }

        return true;
    }

    private static string FormatAttempt(ActionStepAttempt attempt)
    {
        var status = attempt.Status == TraceStatus.Success ? "OK" : attempt.Status == TraceStatus.Failure ? "FAIL" : attempt.Status.ToString().ToUpperInvariant();
        var reason = FormatAttemptReason(attempt);
        var fallback = attempt.Continued ? "continued" : "stopped";
        return $"{status}: {attempt.Order}. {attempt.StepKind}{reason} [{fallback}]";
    }

    private static string FormatAttemptReason(ActionStepAttempt attempt)
    {
        if (attempt.Status != TraceStatus.Failure)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(attempt.Detail))
        {
            return $" - {attempt.Detail}";
        }

        return attempt.FailureReason is { } reason ? $" - {reason}" : string.Empty;
    }

    private static bool AddIfRoom(List<LocalActivityRow> rows, LocalActivityRow row, int maxRows)
    {
        if (rows.Count >= maxRows)
        {
            return false;
        }

        rows.Add(row);
        return true;
    }
}

internal static class PlayLogViewBuilder
{
    public const string EmptyText = "No log entries.";

    public static IReadOnlyList<PlayLogRow> Build(
        ActionLogProjection? actionLog,
        PlayLogScope scope,
        PlaneId? currentLocationPlaneId,
        int maxRows)
    {
        if (maxRows <= 0)
        {
            return [];
        }

        if (scope == PlayLogScope.CurrentLocation && currentLocationPlaneId is null)
        {
            return [new PlayLogRow(EmptyText, Succeeded: true, IsMuted: true)];
        }

        var rows = ActionLogQueryService.Select(
            actionLog,
            new ActionLogQuery(
                PlaneAnchors: scope == PlayLogScope.CurrentLocation && currentLocationPlaneId is { } planeId
                    ? new HashSet<PlaneId> { planeId }
                    : null,
                Order: ActionLogOrder.NewestFirst,
                MaxRows: maxRows));

        if (rows.Count == 0)
        {
            return [new PlayLogRow(EmptyText, Succeeded: true, IsMuted: true)];
        }

        return rows
            .Select(outcome => new PlayLogRow(
                ActionOutcomeTextFormatter.FormatGlobal(outcome),
                outcome.Succeeded,
                outcome.ActorId))
            .ToList();
    }
}

internal static class CurrentRegionActivityViewBuilder
{
    public const string EmptyText = "Recent successes: none";

    public static IReadOnlyList<string> Build(EntityPanelProjection? currentPlace, int maxRows)
    {
        if (maxRows <= 0)
        {
            return [];
        }

        var rows = new List<string> { "Recent successes" };
        if (maxRows == 1)
        {
            return rows;
        }

        if (currentPlace?.InventoryGrid is not { } grid)
        {
            rows.Add(EmptyText);
            return rows.Take(maxRows).ToList();
        }

        var outcomes = ActionLogQueryService.Select(
            ActionLogProjection.FromOutcomes(currentPlace.LocalLog),
            new ActionLogQuery(
                PlaneAnchors: new HashSet<PlaneId> { grid.PlaneId },
                Succeeded: true,
                Order: ActionLogOrder.NewestFirst,
                MaxRows: maxRows - 1));

        if (outcomes.Count == 0)
        {
            rows.Add(EmptyText);
            return rows.Take(maxRows).ToList();
        }

        rows.AddRange(outcomes.Select(ActionOutcomeTextFormatter.FormatLocal));
        return rows.Take(maxRows).ToList();
    }
}

internal sealed record PlayEntityHoverInfo(
    string ComponentId,
    string ComponentTitle,
    EntityId EntityId,
    string EntityName,
    PlaneId PlaneId,
    GridCoord Coord,
    SadConsoleRect CellBounds,
    string? LastSuccessfulLog);

internal static class PlayEntityHoverHitTester
{
    public static PlayEntityHoverInfo? HitTest(
        int x,
        int y,
        IReadOnlyList<IUiComponent> components,
        ActionLogProjection? actionLog,
        int maxRecentSuccessRows = 3,
        int rootCellWidthPixels = 1,
        int rootCellHeightPixels = 1)
    {
        foreach (var component in components.OfType<InventorySpaceComponent>().Reverse())
        {
            if (component.DisplayProfile is not null)
            {
                var geometry = InventorySpacePresentationGeometry.FromComponent(component, rootCellWidthPixels, rootCellHeightPixels);
                var pixelX = (x * rootCellWidthPixels) + (rootCellWidthPixels / 2);
                var pixelY = (y * rootCellHeightPixels) + (rootCellHeightPixels / 2);
                if (geometry.HitTest(pixelX, pixelY) is not { EntityId: { } entityId } hit)
                {
                    continue;
                }

                var latestSuccess = LatestSuccess(actionLog, entityId);
                return new PlayEntityHoverInfo(
                    component.Id,
                    component.Title,
                    entityId,
                    hit.DisplayName ?? entityId.Value,
                    component.View.PlaneId,
                    hit.Coord,
                    PixelToRootCellRect(hit.Bounds, rootCellWidthPixels, rootCellHeightPixels),
                    latestSuccess?.Sentence);
            }

            if (!component.Bounds.Contains(x, y))
            {
                continue;
            }

            foreach (var entity in component.View.Entities.Where(entity => component.View.IsVisible(entity.Coord)).Reverse())
            {
                var cellBounds = component.CellBounds(entity.Coord);
                if (!cellBounds.Contains(x, y))
                {
                    continue;
                }

                var latestSuccess = LatestSuccess(actionLog, entity.EntityId);

                return new PlayEntityHoverInfo(
                    component.Id,
                    component.Title,
                    entity.EntityId,
                    entity.DisplayName ?? entity.EntityId.Value,
                    component.View.PlaneId,
                    entity.Coord,
                    cellBounds,
                    latestSuccess?.Sentence);
            }
        }

        return null;
    }

    private static ActionOutcome? LatestSuccess(ActionLogProjection? actionLog, EntityId entityId) =>
        ActionLogQueryService.Select(
                actionLog,
                new ActionLogQuery(
                    EntityAnchors: new HashSet<EntityId> { entityId },
                    Succeeded: true,
                    Order: ActionLogOrder.NewestFirst,
                    MaxRows: 1))
            .FirstOrDefault();

    private static SadConsoleRect PixelToRootCellRect(PixelRect rect, int rootCellWidthPixels, int rootCellHeightPixels)
    {
        var width = Math.Max(1, rootCellWidthPixels);
        var height = Math.Max(1, rootCellHeightPixels);
        var left = rect.Left / width;
        var top = rect.Top / height;
        var right = (int)Math.Ceiling(rect.Right / (double)width);
        var bottom = (int)Math.Ceiling(rect.Bottom / (double)height);
        return new SadConsoleRect(left, top, Math.Max(1, right - left), bottom);
    }
}

internal static class PlayEntityHoverTooltipBuilder
{
    public static IUiComponent? Build(
        PlayEntityHoverInfo? hover,
        SadConsoleRect drawableBounds,
        int mouseX,
        int mouseY)
    {
        if (hover is null || drawableBounds.Width <= 0 || drawableBounds.Height <= 0)
        {
            return null;
        }

        var text = string.IsNullOrWhiteSpace(hover.LastSuccessfulLog)
            ? hover.EntityName
            : $"{hover.EntityName} {TrimEntityPrefix(hover.LastSuccessfulLog, hover.EntityName)}";
        var rows = new List<string> { text };

        var width = Math.Min(
            Math.Max(24, rows.Concat([hover.EntityName]).Max(row => row.Length) + 2),
            Math.Max(1, drawableBounds.Width));
        var height = Math.Min(rows.Count + 2, Math.Max(1, drawableBounds.Height));
        var subjectCenterX = hover.CellBounds.Left + (hover.CellBounds.Width / 2);
        var preferredLeft = subjectCenterX - (width / 2);
        var preferredTop = hover.CellBounds.Bottom + 1;
        if (preferredTop + height > drawableBounds.Bottom)
        {
            preferredTop = hover.CellBounds.Top - height - 1;
        }

        var left = Math.Clamp(preferredLeft, drawableBounds.Left, Math.Max(drawableBounds.Left, drawableBounds.Left + drawableBounds.Width - width));
        var top = Math.Clamp(preferredTop, drawableBounds.Top, Math.Max(drawableBounds.Top, drawableBounds.Bottom - height));

        return new PlayEntityTooltipComponent(
            "actor-pov-hover-tooltip",
            "Hover",
            SadConsoleRect.FromSize(left, top, width, height),
            rows.Take(height).ToList());
    }

    private static string TrimEntityPrefix(string text, string entityName)
    {
        if (text.StartsWith(entityName, StringComparison.OrdinalIgnoreCase))
        {
            return text[entityName.Length..].TrimStart();
        }

        return text;
    }
}
