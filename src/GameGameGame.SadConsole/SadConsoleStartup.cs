using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Tiles;

namespace GameGameGame.SadConsoleApp;

internal sealed record SadConsoleStartup(PlayableScenarioSession? DirectSession, ScenarioCatalogResult? Catalog, string? Error, string? DirectContentPath = null, string? DirectScenarioId = null, bool LaunchGallery = false, bool LaunchDirectSimulation = false, bool LaunchPlayMock = false, SadConsoleDisplaySettings? DisplaySettings = null)
{
    public static SadConsoleStartup FromArgs(string[] args)
    {
        var displaySettings = SadConsoleDisplaySettings.Default.WithUiScale(ReadUiScale(args));

        if (args.Contains("--gallery", StringComparer.OrdinalIgnoreCase))
        {
            return new SadConsoleStartup(null, null, null, LaunchGallery: true, DisplaySettings: displaySettings);
        }

        if (args.Length >= 3 && args[0].Equals("--play-mock", StringComparison.OrdinalIgnoreCase))
        {
            return new SadConsoleStartup(null, null, null, args[1], args[2], LaunchPlayMock: true, DisplaySettings: displaySettings);
        }

        if (args.Length >= 2 && !args[0].StartsWith("--", StringComparison.Ordinal))
        {
            try
            {
                return new SadConsoleStartup(null, null, null, args[0], args[1], DisplaySettings: displaySettings);
            }
            catch (Exception ex)
            {
                return new SadConsoleStartup(null, null, ex.Message, DisplaySettings: displaySettings);
            }
        }

        if (args.Length == 1 && !args[0].StartsWith("--", StringComparison.Ordinal))
        {
            return new SadConsoleStartup(null, null, Usage, DisplaySettings: displaySettings);
        }

        var catalog = ResolveScenarioCatalog(args);
        return new SadConsoleStartup(null, catalog, null, DisplaySettings: displaySettings);
    }

    public SadConsoleDisplaySettings ActiveDisplaySettings => DisplaySettings ?? SadConsoleDisplaySettings.Default;

    private static int ReadUiScale(string[] args)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (args[index].Equals("--ui-scale", StringComparison.OrdinalIgnoreCase) && int.TryParse(args[index + 1], out var scale))
            {
                return Math.Clamp(scale, 1, 4);
            }
        }

        return SadConsoleDisplaySettings.Default.UiScale;
    }

    private static ScenarioCatalogResult ResolveScenarioCatalog(string[] args)
    {
        if (args.Length == 0)
        {
            return File.Exists(ScenarioCatalog.DefaultManifestPath)
                ? ScenarioCatalog.LoadManifest(ScenarioCatalog.DefaultManifestPath)
                : ScenarioCatalog.DiscoverFolder(ScenarioCatalog.DefaultDiscoveryFolder);
        }

        if (args.Length >= 2 && args[0] == "--content")
        {
            try
            {
                return ScenarioCatalog.BuildFromDocument(args[1], EditableContentDocument.LoadYaml(File.ReadAllText(args[1])));
            }
            catch (Exception ex)
            {
                return new ScenarioCatalogResult([], [$"{args[1]}: {ex.Message}"]);
            }
        }

        if (args.Length >= 2 && args[0] == "--discover")
        {
            return ScenarioCatalog.DiscoverFolder(args[1]);
        }

        if (args.Length >= 2 && args[0] == "--manifest")
        {
            return ScenarioCatalog.LoadManifest(args[1]);
        }

        return new ScenarioCatalogResult([], [Usage]);
    }

    private const string Usage = "Usage: GameGameGame.SadConsole [--content <file> | --discover <folder> | --manifest <manifest>], --gallery, or --play-mock <file> <scenario-id>. Optional: --ui-scale <1-4>.";
}
