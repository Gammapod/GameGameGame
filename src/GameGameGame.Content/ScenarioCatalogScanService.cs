namespace GameGameGame.Content;

public sealed record ScenarioCatalogScanResult(
    string FolderPath,
    string OutputPath,
    int EntryCount,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> Diagnostics)
{
    public bool Succeeded => EntryCount > 0;
}

public static class ScenarioCatalogScanService
{
    public static ScenarioCatalogScanResult Scan(string? folderPath = null, string? outputPath = null)
    {
        var folder = string.IsNullOrWhiteSpace(folderPath)
            ? ScenarioCatalog.DefaultDiscoveryFolder
            : folderPath;
        var output = string.IsNullOrWhiteSpace(outputPath)
            ? ScenarioCatalog.DefaultManifestPath
            : outputPath;

        var catalog = ScenarioCatalog.DiscoverFolder(folder);
        ScenarioCatalog.SaveManifest(catalog, output);

        return new ScenarioCatalogScanResult(
            folder,
            output,
            catalog.Entries.Count,
            [$"Wrote {catalog.Entries.Count} scenario entries to {output}."],
            catalog.Diagnostics);
    }
}
