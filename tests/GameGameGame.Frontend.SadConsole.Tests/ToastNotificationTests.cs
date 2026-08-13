using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class ToastNotificationTests
{
    [Fact]
    public void ToastNotificationExpiresAfterConfiguredDuration()
    {
        var toast = new ToastNotificationState(["Warning", "Cannot play scenario."], TimeSpan.FromSeconds(4));

        Assert.False(toast.Advance(TimeSpan.FromSeconds(3.9)));
        Assert.False(toast.IsExpired);
        Assert.True(toast.Advance(TimeSpan.FromSeconds(0.1)));
        Assert.True(toast.IsExpired);
    }

    [Fact]
    public void LaunchWarningToastIncludesSummaryAndFirstDiagnostics()
    {
        var failure = new ScenarioLaunchFailurePresentation(
            "Cannot play Direct Chase: Validation: deprecated field.",
            ["Validation: deprecated field.", "Runtime: missing actor.", "Capability gap: target acquisition.", "extra"]);

        var toast = ToastNotificationPresenter.LaunchWarning(failure);

        Assert.Equal(TimeSpan.FromSeconds(4), toast.Duration);
        Assert.Equal([
            "Warning",
            "Cannot play Direct Chase: Validation: deprecated field.",
            "Validation: deprecated field.",
            "Runtime: missing actor.",
            "Capability gap: target acquisition."
        ], toast.Rows);
    }

    [Fact]
    public void ToastOverlayUsesHalfTileOffsetPanelGeometry()
    {
        var displaySettings = SadConsoleDisplaySettings.FromSettings(FrontendSadConsoleSettings.Default);
        var shell = FrontendDisplayShell.Resolve(1280, 720, displaySettings);
        var layout = ScenarioBrowserLayout.Resolve(shell.DrawableBounds);
        var toast = new ToastNotificationState(["Warning", "Cannot play scenario."], TimeSpan.FromSeconds(4));

        var overlay = ToastNotificationPresenter.ToOverlay(toast, layout, displaySettings);

        Assert.Equal(displaySettings.ScaledTileWidth / 2, overlay.Geometry.PixelOffsetX);
        Assert.Equal(displaySettings.ScaledTileHeight / 2, overlay.Geometry.PixelOffsetY);
        Assert.Equal(layout.TextX + 1, overlay.Geometry.CellBounds.X);
        Assert.Equal(layout.ListY, overlay.Geometry.CellBounds.Y);
    }
}
