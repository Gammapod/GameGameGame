using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GameGameGame.Content;

public sealed record ScenarioCatalogEntry(string ContentPath, string ScenarioId, string Name, string? Description = null);

public sealed record ScenarioCatalogResult(
    IReadOnlyList<ScenarioCatalogEntry> Entries,
    IReadOnlyList<string> Diagnostics);

public static class ScenarioCatalog
{
    public const string ManifestFileName = "Manifest.yaml";
    public const string DefaultDiscoveryFolder = "src\\GameGameGame.Content\\Beta";
    public const string DefaultManifestPath = "src\\GameGameGame.Content\\Beta\\Manifest.yaml";

    public static ScenarioCatalogResult BuildFromDocument(string contentPath, EditableContentDocument document)
    {
        var entries = document.Scenarios
            .Select(entry => new ScenarioCatalogEntry(
                contentPath,
                entry.Key,
                string.IsNullOrWhiteSpace(entry.Value.Name) ? entry.Key : entry.Value.Name!))
            .OrderBy(entry => entry.ScenarioId, StringComparer.Ordinal)
            .ToList();

        return new ScenarioCatalogResult(entries, []);
    }

    public static ScenarioCatalogResult DiscoverFolder(string folderPath)
    {
        var entries = new List<ScenarioCatalogEntry>();
        var diagnostics = new List<string>();

        if (!Directory.Exists(folderPath))
        {
            return new ScenarioCatalogResult([], [$"Scenario discovery folder {folderPath} does not exist."]);
        }

        var manifestPath = Path.Combine(folderPath, ManifestFileName);
        var existingDescriptions = File.Exists(manifestPath)
            ? LoadManifest(manifestPath).Entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Description))
                .ToDictionary(entry => (Path.GetFullPath(entry.ContentPath), entry.ScenarioId), entry => entry.Description, new ScenarioCatalogEntryKeyComparer())
            : [];

        foreach (var path in Directory.EnumerateFiles(folderPath, "*.yaml", SearchOption.AllDirectories)
                     .Concat(Directory.EnumerateFiles(folderPath, "*.yml", SearchOption.AllDirectories))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Where(path => !string.Equals(Path.GetFileName(path), ManifestFileName, StringComparison.OrdinalIgnoreCase))
                     .Order(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var document = EditableContentDocument.LoadYaml(File.ReadAllText(path));
                entries.AddRange(BuildFromDocument(path, document).Entries.Select(entry =>
                    existingDescriptions.TryGetValue((Path.GetFullPath(entry.ContentPath), entry.ScenarioId), out var description)
                        ? entry with { Description = description }
                        : entry));
            }
            catch (Exception ex)
            {
                diagnostics.Add($"{path}: {ex.Message}");
            }
        }

        var result = new ScenarioCatalogResult(
            entries
                .OrderBy(entry => entry.ContentPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.ScenarioId, StringComparer.Ordinal)
                .ToList(),
            diagnostics);

        try
        {
            SaveManifest(result, manifestPath);
        }
        catch (Exception ex)
        {
            return result with { Diagnostics = result.Diagnostics.Concat([$"Manifest could not be saved to {manifestPath}: {ex.Message}"]).ToList() };
        }

        return result;
    }

    public static void SaveManifest(ScenarioCatalogResult catalog, string manifestPath)
    {
        var dto = ScenarioCatalogManifestDto.From(catalog);
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        var directory = Path.GetDirectoryName(manifestPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(manifestPath, serializer.Serialize(dto));
    }

    public static ScenarioCatalogResult LoadManifest(string manifestPath)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            var dto = deserializer.Deserialize<ScenarioCatalogManifestDto>(File.ReadAllText(manifestPath)) ?? new ScenarioCatalogManifestDto();

            return dto.ToCatalogResult();
        }
        catch (Exception ex)
        {
            return new ScenarioCatalogResult([], [$"{manifestPath}: {ex.Message}"]);
        }
    }

    private sealed class ScenarioCatalogManifestDto
    {
        public List<ScenarioCatalogEntryDto>? Scenarios { get; set; }

        public List<string>? Diagnostics { get; set; }

        public static ScenarioCatalogManifestDto From(ScenarioCatalogResult catalog) => new()
        {
            Scenarios = catalog.Entries.Select(ScenarioCatalogEntryDto.From).ToList(),
            Diagnostics = catalog.Diagnostics.ToList()
        };

        public ScenarioCatalogResult ToCatalogResult() => new(
            (Scenarios ?? []).Select(entry => entry.ToEntry()).ToList(),
            Diagnostics ?? []);
    }

    private sealed class ScenarioCatalogEntryDto
    {
        public string? ContentPath { get; set; }

        public string? ScenarioId { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public static ScenarioCatalogEntryDto From(ScenarioCatalogEntry entry) => new()
        {
            ContentPath = entry.ContentPath,
            ScenarioId = entry.ScenarioId,
            Name = entry.Name,
            Description = entry.Description
        };

        public ScenarioCatalogEntry ToEntry() => new(ContentPath ?? string.Empty, ScenarioId ?? string.Empty, Name ?? ScenarioId ?? string.Empty, Description);
    }

    private sealed class ScenarioCatalogEntryKeyComparer : IEqualityComparer<(string ContentPath, string ScenarioId)>
    {
        public bool Equals((string ContentPath, string ScenarioId) x, (string ContentPath, string ScenarioId) y) =>
            string.Equals(x.ContentPath, y.ContentPath, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.ScenarioId, y.ScenarioId, StringComparison.Ordinal);

        public int GetHashCode((string ContentPath, string ScenarioId) obj) =>
            HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ContentPath), StringComparer.Ordinal.GetHashCode(obj.ScenarioId));
    }
}
