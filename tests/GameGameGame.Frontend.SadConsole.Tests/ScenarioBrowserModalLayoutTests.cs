using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class ScenarioBrowserModalLayoutTests
{
    [Fact]
    public void ScenarioBrowserActionSelectorPanelFitsInsideDrawableBounds()
    {
        var shell = FrontendDisplayShell.Resolve(1280, 720, SadConsoleDisplaySettings.FromSettings(FrontendSadConsoleSettings.Default));
        var layout = ScenarioBrowserLayout.Resolve(shell.DrawableBounds);

        var width = Math.Min(Math.Max(0, layout.TextWidth - 2), 74);
        var panelBounds = new FrontendRect(layout.TextX + 1, layout.ListY, width + 2, Math.Min(10, layout.MessageY - layout.ListY));

        Assert.True(panelBounds.X > shell.DrawableBounds.X);
        Assert.True(panelBounds.Y >= shell.DrawableBounds.Y);
        Assert.True(panelBounds.Right <= shell.DrawableBounds.Right);
        Assert.True(panelBounds.Bottom < layout.MessageY);
    }
}
