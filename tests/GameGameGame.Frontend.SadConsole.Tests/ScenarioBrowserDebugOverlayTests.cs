using GameGameGame.Content;
using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class ScenarioBrowserDebugOverlayTests
{
    [Fact]
    public void ScenarioBrowserDebugOverlayReportsCurrentScreenLayoutFacts()
    {
        var catalog = new WorkspaceScenarioCatalogResult(
            new ContentWorkspace([]),
            [new WorkspaceScenarioCatalogEntry("workspace:debug-room", "debug-room", "Debug Room", null, null, [], "DebugRoom.yaml", "debug.debug-room", WorkspaceScenarioLaunchKind.Workspace)],
            []);
        var model = new ScenarioBrowserScreenModel(catalog);
        var shell = FrontendDisplayShell.Resolve(1280, 720, SadConsoleDisplaySettings.FromSettings(FrontendSadConsoleSettings.Default));
        var layout = ScenarioBrowserLayout.Resolve(shell.DrawableBounds);

        var overlay = ScenarioBrowserDebugOverlay.Build(model, shell, layout, FrontendWindowMode.Fullscreen);

        Assert.True(overlay.IsVisible);
        Assert.Contains(overlay.Rows, row => row.Contains("screen cells", StringComparison.Ordinal));
        Assert.Contains(overlay.Rows, row => row.Contains("drawable", StringComparison.Ordinal));
        Assert.Contains(overlay.Rows, row => row.Contains("selected: debug-room", StringComparison.Ordinal));
        Assert.Contains(overlay.Rows, row => row.Contains("F11", StringComparison.Ordinal));
    }

    [Fact]
    public void ScenarioBrowserChromeStateTogglesLayoutDebugAndWindowMode()
    {
        var state = new ScenarioBrowserChromeState(FrontendWindowMode.Windowed);

        Assert.True(state.ToggleLayoutDebug());
        Assert.False(state.ToggleLayoutDebug());
        Assert.Equal(FrontendWindowMode.Fullscreen, state.ToggleWindowMode());
        Assert.Equal(FrontendWindowMode.Windowed, state.ToggleWindowMode());
    }
}
