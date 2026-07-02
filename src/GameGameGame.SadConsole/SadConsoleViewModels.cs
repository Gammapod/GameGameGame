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
