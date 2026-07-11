using GameGameGame.Content;

namespace GameGameGame.SadConsoleApp;

internal sealed record SadConsoleStartup(PlayableScenarioSession? DirectSession, ScenarioCatalogResult? Catalog, string? Error, string? DirectContentPath = null, string? DirectScenarioId = null, bool LaunchGallery = false, bool LaunchLegacyBetaEditor = false)
{
    public static SadConsoleStartup FromArgs(string[] args)
    {
        if (args.Contains("--gallery", StringComparer.OrdinalIgnoreCase))
        {
            return new SadConsoleStartup(null, null, null, LaunchGallery: true);
        }

        if (args.Contains("--new-scenario-selection", StringComparer.OrdinalIgnoreCase))
        {
            var filteredArgs = args.Where(arg => !string.Equals(arg, "--new-scenario-selection", StringComparison.OrdinalIgnoreCase)).ToArray();
            var scenarioCatalog = ResolveScenarioCatalog(filteredArgs);
            return new SadConsoleStartup(null, scenarioCatalog, null);
        }

        if (args.Contains("--beta-editor", StringComparer.OrdinalIgnoreCase))
        {
            var filteredArgs = args.Where(arg => !string.Equals(arg, "--beta-editor", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (filteredArgs.Length >= 2 && !filteredArgs[0].StartsWith("--", StringComparison.Ordinal))
            {
                return new SadConsoleStartup(null, null, null, filteredArgs[0], filteredArgs[1], LaunchLegacyBetaEditor: true);
            }

            var scenarioCatalog = ResolveScenarioCatalog(filteredArgs);
            return new SadConsoleStartup(null, scenarioCatalog, null, LaunchLegacyBetaEditor: true);
        }

        if (args.Length >= 2 && !args[0].StartsWith("--", StringComparison.Ordinal))
        {
            try
            {
                return new SadConsoleStartup(null, null, null, args[0], args[1]);
            }
            catch (Exception ex)
            {
                return new SadConsoleStartup(null, null, ex.Message);
            }
        }

        if (args.Length == 1 && !args[0].StartsWith("--", StringComparison.Ordinal))
        {
            return new SadConsoleStartup(null, null, Usage);
        }

        var catalog = ResolveScenarioCatalog(args);
        return new SadConsoleStartup(null, catalog, null);
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

    private const string Usage = "Usage: GameGameGame.SadConsole [--content <file> | --discover <folder> | --manifest <manifest>], --gallery, or --beta-editor [<content-file> <scenario-id>].";
}
