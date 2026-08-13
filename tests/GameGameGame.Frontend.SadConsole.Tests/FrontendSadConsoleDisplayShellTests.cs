using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class FrontendSadConsoleDisplayShellTests
{
    [Fact]
    public void FrontendSadConsoleDisplayShellResolvesDrawableBoundsInsideChrome()
    {
        var display = SadConsoleDisplaySettings.FromSettings(FrontendSadConsoleSettings.Default);

        var shell = FrontendDisplayShell.Resolve(1280, 720, display);

        Assert.Equal(80, shell.LogicalWidth);
        Assert.Equal(45, shell.LogicalHeight);
        Assert.Equal(1, shell.DrawableBounds.X);
        Assert.Equal(1, shell.DrawableBounds.Y);
        Assert.Equal(78, shell.DrawableBounds.Width);
        Assert.Equal(43, shell.DrawableBounds.Height);
        Assert.True(shell.DrawableBounds.Right < shell.LogicalWidth - 1);
        Assert.True(shell.DrawableBounds.Bottom < shell.LogicalHeight - 1);
    }
}
