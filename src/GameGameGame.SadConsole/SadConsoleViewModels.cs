using GameGameGame.Content;
using GameGameGame.Core;

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

internal readonly record struct SadConsoleRect(int Left, int Top, int Width, int Bottom)
{
    public int Height => Math.Max(0, Bottom - Top);
}

internal static class SadConsoleSessionLayout
{
    private const int PanelLeft = 1;
    private const int PanelTop = 6;
    private const int PanelBottom = 32;
    private const int PanelGap = 2;
    private const int CollapsedPanelWidth = 14;
    private const int PanelAreaWidth = SadConsoleShell.ScreenWidth - 2;

    public static SadConsoleRect GlobalLogRect => new(1, 33, SadConsoleShell.ScreenWidth - 2, SadConsoleShell.ScreenHeight);

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

internal static class LocalActivityViewBuilder
{
    public const string EmptyText = "No visible contents or controlled-command snippets.";

    public static IReadOnlyList<LocalActivityRow> Build(EntityPanelProjection panel, int maxRows)
    {
        if (maxRows <= 0)
        {
            return [];
        }

        var rows = new List<LocalActivityRow> { new("Local activity", IsHeader: true) };
        if (panel.Contents.Count == 0 && panel.LocalLog.Count == 0)
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

            if (!string.IsNullOrWhiteSpace(row.PreviousAction)
                && !AddIfRoom(rows, new LocalActivityRow($"└ {row.PreviousAction}", IsPositive: true), maxRows))
            {
                return rows;
            }
        }

        var contentEntityIds = panel.Contents.Select(row => row.EntityId).ToHashSet();
        var remainingRows = Math.Max(0, maxRows - rows.Count);
        foreach (var outcome in panel.LocalLog
                     .Where(outcome => !outcome.AnchorEntityIds.Any(contentEntityIds.Contains))
                     .TakeLast(remainingRows))
        {
            if (!AddIfRoom(rows, new LocalActivityRow($"└ {outcome.Sentence}", IsPositive: outcome.Succeeded, IsWarning: !outcome.Succeeded), maxRows))
            {
                return rows;
            }
        }

        return rows;
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
