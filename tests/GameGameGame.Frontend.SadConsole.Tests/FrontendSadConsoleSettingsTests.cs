using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class FrontendSadConsoleSettingsTests
{
    [Fact]
    public void FrontendSadConsoleSettingsLoadsDefaultsWhenNoPersistenceExists()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "settings.json");

        var settings = FrontendSadConsoleSettingsStore.LoadOrDefault(missingPath);

        Assert.Equal(FrontendWindowMode.Fullscreen, settings.WindowMode);
        Assert.Equal(FrontendInputMode.Keyboard, settings.PreferredInputMode);
        Assert.True(settings.StartFullscreen);
        Assert.True(settings.WindowWidthPixels > 0);
        Assert.True(settings.WindowHeightPixels > 0);
    }
}
