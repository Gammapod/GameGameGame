namespace GameGameGame.Frontend.SadConsole;

internal sealed record PlayModeInspectionLayout(FrontendRect GridBounds, FrontendRect? InspectionBounds, FrontendRect? PlayerPanelBounds)
{
    public static PlayModeInspectionLayout Resolve(FrontendRect drawableBounds)
    {
        const int preferredInspectionWidth = 42;
        const int minimumInspectionWidth = 28;
        const int gap = 1;
        const int minimumInspectionHeight = 16;

        if (drawableBounds.Height < minimumInspectionHeight || drawableBounds.Width < minimumInspectionWidth)
        {
            return new PlayModeInspectionLayout(drawableBounds, null, null);
        }

        var inspectionWidth = Math.Min(preferredInspectionWidth, Math.Max(minimumInspectionWidth, drawableBounds.Width - gap));
        var inspectionHeight = Math.Min(24, drawableBounds.Height - 1);
        var inspection = new FrontendRect(
            drawableBounds.Right - inspectionWidth,
            drawableBounds.Y + 1,
            inspectionWidth,
            inspectionHeight);
        var playerPanel = new FrontendRect(
            drawableBounds.X,
            drawableBounds.Bottom - Math.Min(18, drawableBounds.Height - 1) + 1,
            inspectionWidth,
            Math.Min(18, drawableBounds.Height - 1));
        return new PlayModeInspectionLayout(drawableBounds, inspection, playerPanel);
    }
}
