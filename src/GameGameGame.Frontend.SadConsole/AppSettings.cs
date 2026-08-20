using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameGameGame.Frontend.SadConsole;

internal enum FrontendInputMode
{
    Keyboard,
    Mouse,
    Gamepad
}

internal enum FrontendWindowMode
{
    Fullscreen,
    Windowed,
    BorderlessWindowed
}

internal sealed record FrontendSadConsoleSettings(
    FrontendWindowMode WindowMode,
    int WindowWidthPixels,
    int WindowHeightPixels,
    int UiScale,
    FrontendInputMode PreferredInputMode,
    string? LastSelectedScenarioEntryId = null)
{
    public static FrontendSadConsoleSettings Default { get; } = new(
        FrontendWindowMode.BorderlessWindowed,
        WindowWidthPixels: 1280,
        WindowHeightPixels: 720,
        UiScale: 2,
        FrontendInputMode.Keyboard);

    public bool StartFullscreen => WindowMode == FrontendWindowMode.Fullscreen;
}

internal static class FrontendSadConsoleSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string DefaultSettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GameGameGame",
        "Frontend.SadConsole",
        "settings.json");

    public static FrontendSadConsoleSettings LoadOrDefault(string? path = null)
    {
        var settingsPath = path ?? DefaultSettingsPath;
        if (!File.Exists(settingsPath))
        {
            return FrontendSadConsoleSettings.Default;
        }

        try
        {
            return JsonSerializer.Deserialize<FrontendSadConsoleSettings>(File.ReadAllText(settingsPath), JsonOptions)
                ?? FrontendSadConsoleSettings.Default;
        }
        catch
        {
            return FrontendSadConsoleSettings.Default;
        }
    }
}
