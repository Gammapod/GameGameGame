using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class FrontendSadConsoleSettingsTests
{
    [Fact]
    public void FrontendSadConsoleSettingsLoadsDefaultsWhenNoPersistenceExists()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "settings.json");

        var settings = FrontendSadConsoleSettingsStore.LoadOrDefault(missingPath);

        Assert.Equal(FrontendWindowMode.OverlaySafeBorderlessWindowed, settings.WindowMode);
        Assert.Equal(FrontendInputMode.Keyboard, settings.PreferredInputMode);
        Assert.False(settings.StartFullscreen);
        Assert.True(settings.StartBorderless);
        Assert.True(settings.WindowWidthPixels > 0);
        Assert.True(settings.WindowHeightPixels > 0);
    }

    [Theory]
    [InlineData("Fullscreen")]
    [InlineData("BorderlessWindowed")]
    public void FrontendSadConsoleSettingsMigratesLegacyFullscreenLikeModesToOverlaySafeBorderless(string legacyMode)
    {
        var settingsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, $$"""
            {
              "WindowMode": "{{legacyMode}}",
              "WindowWidthPixels": 1280,
              "WindowHeightPixels": 720,
              "UiScale": 2,
              "PreferredInputMode": "Keyboard"
            }
            """);

        var settings = FrontendSadConsoleSettingsStore.LoadOrDefault(settingsPath);

        Assert.Equal(FrontendWindowMode.OverlaySafeBorderlessWindowed, settings.WindowMode);
    }

    [Fact]
    public void FrontendSadConsoleSettingsPreservesWindowedMode()
    {
        var settingsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, """
            {
              "WindowMode": "Windowed",
              "WindowWidthPixels": 1280,
              "WindowHeightPixels": 720,
              "UiScale": 2,
              "PreferredInputMode": "Keyboard"
            }
            """);

        var settings = FrontendSadConsoleSettingsStore.LoadOrDefault(settingsPath);

        Assert.Equal(FrontendWindowMode.Windowed, settings.WindowMode);
    }
}
