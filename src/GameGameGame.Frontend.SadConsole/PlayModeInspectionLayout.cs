namespace GameGameGame.Frontend.SadConsole;

internal sealed record PlayModeInspectionLayout(FrontendRect GridBounds, FrontendRect? InspectionBounds, FrontendRect? PlayerPanelBounds)
{
    public static PlayModeInspectionLayout Resolve(FrontendRect drawableBounds)
    {
        const int gap = 1;
        const int playerPanelMaximumHeight = 18;

        if (drawableBounds.Height < EntityInspectionPanelLayout.MinimumHeight || drawableBounds.Width < EntityInspectionPanelLayout.MinimumWidth)
        {
            return new PlayModeInspectionLayout(drawableBounds, null, null);
        }

        var inspection = EntityInspectionPanelLayout.ResolveResponsiveBounds(
            new FrontendRect(drawableBounds.X, drawableBounds.Y + gap, drawableBounds.Width, Math.Max(0, drawableBounds.Height - gap)),
            anchorRightPadding: 0);
        var playerPanelHeight = Math.Min(playerPanelMaximumHeight, drawableBounds.Height - 1);
        var playerPanel = new FrontendRect(
            drawableBounds.X,
            drawableBounds.Bottom - playerPanelHeight + 1,
            inspection.Width,
            playerPanelHeight);
        return new PlayModeInspectionLayout(drawableBounds, inspection, playerPanel);
    }
}
