namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed record GameplayMockLayoutFrame(
    int Width,
    int Height,
    IReadOnlyList<GameplayMockRegion> Regions)
{
    public GameplayMockRegion Root => Region("0");
    public GameplayMockRegion Hud => Region("0.1");
    public GameplayMockRegion CurrentPlace => Region("0.2");
    public GameplayMockRegion Inspection => Region("0.3");
    public GameplayMockRegion ActionSelector => Region("0.2.1");
    public GameplayMockRegion Diagnostics => Region("0.diagnostics");

    public SadConsoleRect RootBounds => Root.Bounds;
    public SadConsoleRect HudBounds => Hud.Bounds;
    public SadConsoleRect CurrentPlaceBounds => CurrentPlace.Bounds;
    public SadConsoleRect InspectionBounds => Inspection.Bounds;
    public SadConsoleRect ActionSelectorBounds => ActionSelector.Bounds;
    public SadConsoleRect DiagnosticsBounds => Diagnostics.Bounds;

    private GameplayMockRegion Region(string id) => Regions.Single(region => region.Id == id);
}

internal sealed record GameplayMockRegion(string Id, string Title, SadConsoleRect Bounds, int Layer);

internal sealed record GameplayMockHitTestResult(GameplayMockRegion Region, int LocalX, int LocalY)
{
    public string Format() => $"hit: {Region.Id} {Region.Title} local {LocalX},{LocalY} Z{Region.Layer}";
}

internal static class GameplayMockLayout
{
    private const int MinimumViewportWidth = 40;
    private const int MinimumViewportHeight = 18;
    private const int MainContentGap = 1;
    private const int RightEdgeGap = 1;
    private const int HudWidthMin = 20;
    private const int ContentWidthMin = 20;
    private const int InspectionHeightMin = 8;
    private const int CurrentPlaceHeightMin = 8;

    public static GameplayMockLayoutFrame Resolve(int width, int height)
    {
        var safeWidth = Math.Max(MinimumViewportWidth, width);
        var safeHeight = Math.Max(MinimumViewportHeight, height);
        var rootBounds = SadConsoleRect.FromSize(0, 0, safeWidth, safeHeight);

        var horizontal = SplitHorizontal(rootBounds, 1, 4, MainContentGap, RightEdgeGap, HudWidthMin, ContentWidthMin);
        var vertical = SplitVertical(horizontal.Second, 2, 1, InspectionHeightMin, CurrentPlaceHeightMin);
        var hudBounds = horizontal.First;
        var currentPlaceBounds = vertical.First;
        var inspectionBounds = SadConsoleRect.FromSize(
            horizontal.Second.Left,
            vertical.Second.Top,
            horizontal.Second.Width,
            vertical.Second.Height);
        var actionSelectorBounds = ResolveActionSelectorBounds(currentPlaceBounds, itemCount: 7);
        var diagnosticsBounds = SadConsoleRect.FromSize(
            horizontal.Second.Left,
            Math.Max(1, currentPlaceBounds.Bottom - 7),
            Math.Min(60, horizontal.Second.Width),
            Math.Min(6, currentPlaceBounds.Height - 2));

        return new GameplayMockLayoutFrame(
            safeWidth,
            safeHeight,
            [
                new GameplayMockRegion("0", "Play-mode screen", rootBounds, 0),
                new GameplayMockRegion("0.1", "HUD/status", hudBounds, 2),
                new GameplayMockRegion("0.2", "Current place", currentPlaceBounds, 1),
                new GameplayMockRegion("0.3", "Inspection", inspectionBounds, 1),
                new GameplayMockRegion("0.2.1", "Action selector", actionSelectorBounds, 10),
                new GameplayMockRegion("0.diagnostics", "Diagnostics", diagnosticsBounds, 20)
            ]);
    }

    public static SadConsoleRect ResolveActionSelectorBounds(GameplayMockLayoutFrame layout, int itemCount) =>
        ResolveActionSelectorBounds(layout.CurrentPlaceBounds, itemCount);

    public static GameplayMockHitTestResult? HitTest(GameplayMockLayoutFrame layout, int x, int y) =>
        layout.Regions
            .Where(region => Contains(region.Bounds, x, y))
            .OrderByDescending(region => region.Layer)
            .ThenByDescending(region => region.Id, StringComparer.Ordinal)
            .Select(region => new GameplayMockHitTestResult(region, x - region.Bounds.Left, y - region.Bounds.Top))
            .FirstOrDefault();

    private static SadConsoleRect ResolveActionSelectorBounds(SadConsoleRect currentPlaceBounds, int itemCount) =>
        SadConsoleRect.FromSize(
            currentPlaceBounds.Left + 2,
            currentPlaceBounds.Top + 2,
            Math.Min(38, currentPlaceBounds.Width - 4),
            Math.Min(10, Math.Max(6, itemCount + 3)));

    private static bool Contains(SadConsoleRect bounds, int x, int y) =>
        x >= bounds.Left && x < bounds.Left + bounds.Width && y >= bounds.Top && y < bounds.Bottom;

    private static GameplayMockSplit SplitHorizontal(
        SadConsoleRect bounds,
        int firstRatio,
        int secondRatio,
        int gap,
        int trailingGap,
        int firstMinWidth,
        int secondMinWidth)
    {
        // Integer division intentionally preserves the original floor rounding for the 20% HUD split.
        var ratioWidth = bounds.Width * firstRatio / (firstRatio + secondRatio);
        var firstWidth = Math.Clamp(ratioWidth, firstMinWidth, Math.Max(firstMinWidth, bounds.Width - gap - trailingGap - secondMinWidth));
        var secondLeft = bounds.Left + firstWidth + gap;
        var secondWidth = Math.Max(secondMinWidth, bounds.Width - firstWidth - gap - trailingGap);
        return new GameplayMockSplit(
            SadConsoleRect.FromSize(bounds.Left, bounds.Top, firstWidth, bounds.Height),
            SadConsoleRect.FromSize(secondLeft, bounds.Top, secondWidth, bounds.Height));
    }

    private static GameplayMockSplit SplitVertical(
        SadConsoleRect bounds,
        int firstRatio,
        int secondRatio,
        int secondMinHeight,
        int firstMinHeight)
    {
        // The lower inspection panel keeps at least one third of the viewport, matching the mock baseline.
        var secondHeight = Math.Max(secondMinHeight, bounds.Height * secondRatio / (firstRatio + secondRatio));
        var secondTop = Math.Max(firstMinHeight, bounds.Bottom - secondHeight);
        var firstHeight = Math.Max(0, secondTop - bounds.Top);
        return new GameplayMockSplit(
            SadConsoleRect.FromSize(bounds.Left, bounds.Top, bounds.Width, firstHeight),
            SadConsoleRect.FromSize(bounds.Left, secondTop, bounds.Width, Math.Max(0, bounds.Bottom - secondTop)));
    }
}

internal sealed record GameplayMockSplit(SadConsoleRect First, SadConsoleRect Second);
