namespace GameGameGame.Frontend.SadConsole;

internal sealed record PlayModeInspectionLayout(FrontendRect GridBounds, FrontendRect? InspectionBounds, FrontendRect? PlayerPanelBounds)
{
    public static PlayModeInspectionLayout Resolve(FrontendRect drawableBounds)
    {
        const int gap = 1;
        const int gameplayPanelMaximumHeight = 32;

        if (drawableBounds.Height < EntityInspectionPanelLayout.MinimumHeight || drawableBounds.Width < EntityInspectionPanelLayout.MinimumWidth)
        {
            return new PlayModeInspectionLayout(drawableBounds, null, null);
        }

        var inspectionBase = EntityInspectionPanelLayout.ResolveResponsiveBounds(
            new FrontendRect(drawableBounds.X, drawableBounds.Y + gap, drawableBounds.Width, Math.Max(0, drawableBounds.Height - gap)),
            anchorRightPadding: 0);
        var inspectionHeight = Math.Min(gameplayPanelMaximumHeight, drawableBounds.Height - 1);
        var inspection = inspectionBase with { Height = inspectionHeight };
        var playerPanelHeight = Math.Min(gameplayPanelMaximumHeight, drawableBounds.Height - 1);
        var playerPanel = new FrontendRect(
            drawableBounds.X,
            drawableBounds.Bottom - playerPanelHeight + 1,
            inspection.Width,
            playerPanelHeight);
        return new PlayModeInspectionLayout(drawableBounds, inspection, playerPanel);
    }
}
