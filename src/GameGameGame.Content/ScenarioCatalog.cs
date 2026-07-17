using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GameGameGame.Content;

public sealed record ScenarioCatalogEntry(
    string ContentPath,
    string ScenarioId,
    string Name,
    string? Description = null,
    string? Status = null,
    IReadOnlyList<string>? Tags = null,
    string? Source = null);

public sealed record ScenarioCatalogSection(
    string Id,
    string Name,
    string? Description,
    IReadOnlyList<ScenarioCatalogEntry> Entries,
    string? Status = null,
    IReadOnlyList<string>? Tags = null);

public sealed record ScenarioCatalogResult(
    IReadOnlyList<ScenarioCatalogEntry> Entries,
    IReadOnlyList<string> Diagnostics,
    IReadOnlyList<ScenarioCatalogSection>? Sections = null);

public sealed record ScenarioCatalogValidationResult(IReadOnlyList<string> Diagnostics)
{
    public bool IsValid => Diagnostics.Count == 0;
}

public static class ScenarioCatalog
{
    public const string ManifestFileName = "Manifest.yaml";
    private static readonly string RepositoryDefaultDiscoveryFolder = Path.Combine("src", "GameGameGame.Content", "Beta");

    public static string DefaultDiscoveryFolder => ResolveDefaultDiscoveryFolder();

    public static string DefaultManifestPath => Path.Combine(DefaultDiscoveryFolder, ManifestFileName);

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

    public static ScenarioCatalogValidationResult ValidateManifest(string manifestPath, string? scanFolderPath = null)
    {
        var catalog = LoadManifest(manifestPath);
        var diagnostics = catalog.Diagnostics.ToList();
        var sections = catalog.Sections ?? [];
        var sectionByEntry = sections
            .SelectMany(section => section.Entries.Select(entry => (section.Id, Entry: entry)))
            .GroupBy(pair => (Path.GetFullPath(pair.Entry.ContentPath), pair.Entry.ScenarioId), new ScenarioCatalogEntryKeyComparer())
            .ToDictionary(group => group.Key, group => group.First().Id, new ScenarioCatalogEntryKeyComparer());

        if (sections.Count == 0)
        {
            diagnostics.Add("Scenario manifest has no curated sections; add legacy, delta, user, or canonical sections before frontend browsing uses it as authoritative.");
        }

        foreach (var section in sections)
        {
            if (!AllowedSectionIds.Contains(section.Id))
            {
                diagnostics.Add($"Scenario manifest section {section.Id} is not a valid section. Expected one of: {string.Join(", ", AllowedSectionIds)}.");
            }
        }

        foreach (var group in catalog.Entries.GroupBy(entry => entry.ScenarioId, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            diagnostics.Add($"Scenario ID {group.Key} appears more than once in the manifest.");
        }

        foreach (var entry in catalog.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Description))
            {
                diagnostics.Add($"Scenario manifest entry {entry.ScenarioId} requires a description.");
            }

            if (!string.IsNullOrWhiteSpace(entry.Status) && !AllowedStatuses.Contains(entry.Status))
            {
                diagnostics.Add($"Scenario manifest entry {entry.ScenarioId} has invalid status {entry.Status}. Expected one of: {string.Join(", ", AllowedStatuses)}.");
            }

            if (sectionByEntry.TryGetValue((Path.GetFullPath(entry.ContentPath), entry.ScenarioId), out var sectionId)
                && !StatusBelongsInSection(entry.Status, sectionId))
            {
                diagnostics.Add($"Scenario manifest entry {entry.ScenarioId} status {entry.Status} does not belong in section {sectionId}.");
            }

            if (!File.Exists(entry.ContentPath))
            {
                diagnostics.Add($"Scenario manifest entry {entry.ScenarioId} content path {entry.ContentPath} does not exist.");
                continue;
            }

            try
            {
                var document = EditableContentDocument.LoadYaml(File.ReadAllText(entry.ContentPath));
                if (!document.Scenarios.ContainsKey(entry.ScenarioId))
                {
                    diagnostics.Add($"Scenario manifest entry {entry.ScenarioId} was not found in {entry.ContentPath}.");
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add($"Scenario manifest entry {entry.ScenarioId} could not load {entry.ContentPath}: {ex.Message}");
            }
        }

        if (!string.IsNullOrWhiteSpace(scanFolderPath))
        {
            foreach (var candidate in ScanCandidates(scanFolderPath).Entries)
            {
                var key = (Path.GetFullPath(candidate.ContentPath), candidate.ScenarioId);
                if (!sectionByEntry.ContainsKey(key))
                {
                    diagnostics.Add($"Scan discovered unclassified scenario {candidate.ScenarioId} at {candidate.ContentPath}.");
                }
            }
        }

        return new ScenarioCatalogValidationResult(diagnostics);
    }

    public static ScenarioCatalogResult ScanCandidates(string folderPath)
    {
        var entries = new List<ScenarioCatalogEntry>();
        var diagnostics = new List<string>();

        if (!Directory.Exists(folderPath))
        {
            return new ScenarioCatalogResult([], [$"Scenario discovery folder {folderPath} does not exist."]);
        }

        foreach (var path in EnumerateScenarioFiles(folderPath))
        {
            try
            {
                var document = EditableContentDocument.LoadYaml(File.ReadAllText(path));
                entries.AddRange(BuildFromDocument(path, document).Entries);
            }
            catch (Exception ex)
            {
                diagnostics.Add($"{path}: {ex.Message}");
            }
        }

        return new ScenarioCatalogResult(
            entries
                .OrderBy(entry => entry.ContentPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.ScenarioId, StringComparer.Ordinal)
                .ToList(),
            diagnostics);
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
                .GroupBy(entry => (Path.GetFullPath(entry.ContentPath), entry.ScenarioId), new ScenarioCatalogEntryKeyComparer())
                .ToDictionary(group => group.Key, group => group.First().Description, new ScenarioCatalogEntryKeyComparer())
            : [];

        foreach (var path in EnumerateScenarioFiles(folderPath))
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

            return RebaseManifestEntries(dto.ToCatalogResult(), manifestPath);
        }
        catch (Exception ex)
        {
            return new ScenarioCatalogResult([], [$"{manifestPath}: {ex.Message}"]);
        }
    }

    private sealed class ScenarioCatalogManifestDto
    {
        public List<ScenarioCatalogSectionDto>? Sections { get; set; }

        public List<ScenarioCatalogEntryDto>? Scenarios { get; set; }

        public List<string>? Diagnostics { get; set; }

        public static ScenarioCatalogManifestDto From(ScenarioCatalogResult catalog) => new()
        {
            Sections = (catalog.Sections?.Count ?? 0) > 0 ? catalog.Sections!.Select(ScenarioCatalogSectionDto.From).ToList() : null,
            Scenarios = (catalog.Sections?.Count ?? 0) > 0 ? null : catalog.Entries.Select(ScenarioCatalogEntryDto.From).ToList(),
            Diagnostics = catalog.Diagnostics.ToList()
        };

        public ScenarioCatalogResult ToCatalogResult()
        {
            var sections = (Sections ?? []).Select(section => section.ToSection()).ToList();
            var entries = sections.Count > 0
                ? sections.SelectMany(section => section.Entries).ToList()
                : (Scenarios ?? []).Select(entry => entry.ToEntry()).ToList();

            return new ScenarioCatalogResult(entries, Diagnostics ?? [], sections);
        }
    }

    private sealed class ScenarioCatalogSectionDto
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }

        public List<string>? Tags { get; set; }

        public List<ScenarioCatalogEntryDto>? Entries { get; set; }

        public static ScenarioCatalogSectionDto From(ScenarioCatalogSection section) => new()
        {
            Id = section.Id,
            Name = section.Name,
            Description = section.Description,
            Status = section.Status,
            Tags = section.Tags?.ToList(),
            Entries = section.Entries.Select(ScenarioCatalogEntryDto.From).ToList()
        };

        public ScenarioCatalogSection ToSection() => new(
            Id ?? string.Empty,
            Name ?? Id ?? string.Empty,
            Description,
            (Entries ?? []).Select(entry => entry.ToEntry()).ToList(),
            Status,
            Tags);
    }

    private sealed class ScenarioCatalogEntryDto
    {
        public string? ContentPath { get; set; }

        public string? ScenarioId { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }

        public List<string>? Tags { get; set; }

        public string? Source { get; set; }

        public static ScenarioCatalogEntryDto From(ScenarioCatalogEntry entry) => new()
        {
            ContentPath = entry.ContentPath,
            ScenarioId = entry.ScenarioId,
            Name = entry.Name,
            Description = entry.Description,
            Status = entry.Status,
            Tags = entry.Tags?.ToList(),
            Source = entry.Source
        };

        public ScenarioCatalogEntry ToEntry() => new(ContentPath ?? string.Empty, ScenarioId ?? string.Empty, Name ?? ScenarioId ?? string.Empty, Description, Status, Tags, Source);
    }

    private sealed class ScenarioCatalogEntryKeyComparer : IEqualityComparer<(string ContentPath, string ScenarioId)>
    {
        public bool Equals((string ContentPath, string ScenarioId) x, (string ContentPath, string ScenarioId) y) =>
            string.Equals(x.ContentPath, y.ContentPath, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.ScenarioId, y.ScenarioId, StringComparison.Ordinal);

        public int GetHashCode((string ContentPath, string ScenarioId) obj) =>
            HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ContentPath), StringComparer.Ordinal.GetHashCode(obj.ScenarioId));
    }

    private static string ResolveDefaultDiscoveryFolder()
    {
        if (Directory.Exists(RepositoryDefaultDiscoveryFolder))
        {
            return RepositoryDefaultDiscoveryFolder;
        }

        var packagedFolder = Path.Combine(AppContext.BaseDirectory, "Content", "Beta");
        return Directory.Exists(packagedFolder) ? packagedFolder : RepositoryDefaultDiscoveryFolder;
    }

    private static ScenarioCatalogResult RebaseManifestEntries(ScenarioCatalogResult catalog, string manifestPath) =>
        catalog with
        {
            Entries = catalog.Entries
                .Select(entry => entry with { ContentPath = RebaseManifestContentPath(entry.ContentPath, manifestPath) })
                .ToList(),
            Sections = (catalog.Sections ?? [])
                .Select(section => section with
                {
                    Entries = section.Entries
                        .Select(entry => entry with { ContentPath = RebaseManifestContentPath(entry.ContentPath, manifestPath) })
                        .ToList()
                })
                .ToList()
        };

    private static IEnumerable<string> EnumerateScenarioFiles(string folderPath) =>
        Directory.EnumerateFiles(folderPath, "*.yaml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(folderPath, "*.yml", SearchOption.AllDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => !string.Equals(Path.GetFileName(path), ManifestFileName, StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> AllowedSectionIds = ["legacy", "delta", "user", "canonical"];

    private static readonly HashSet<string> AllowedStatuses = ["legacy", "active-delta", "user", "canonical-candidate", "canonical"];

    private static bool StatusBelongsInSection(string? status, string sectionId) => status switch
    {
        null or "" => true,
        "legacy" => sectionId == "legacy",
        "active-delta" => sectionId == "delta",
        "user" => sectionId == "user",
        "canonical" => sectionId == "canonical",
        "canonical-candidate" => sectionId is "delta" or "canonical",
        _ => true
    };

    private static string RebaseManifestContentPath(string contentPath, string manifestPath)
    {
        if (File.Exists(contentPath) || Path.IsPathRooted(contentPath))
        {
            return contentPath;
        }

        var manifestDirectory = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
        if (string.IsNullOrWhiteSpace(manifestDirectory))
        {
            return contentPath;
        }

        var manifestRelativePath = Path.Combine(manifestDirectory, contentPath);
        if (File.Exists(manifestRelativePath))
        {
            return manifestRelativePath;
        }

        var normalizedPath = contentPath.Replace('\\', '/');
        const string betaMarker = "GameGameGame.Content/Beta/";
        var markerIndex = normalizedPath.IndexOf(betaMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return contentPath;
        }

        var betaRelativePath = normalizedPath[(markerIndex + betaMarker.Length)..]
            .Replace('/', Path.DirectorySeparatorChar);
        var packagedRelativePath = Path.Combine(manifestDirectory, betaRelativePath);
        return File.Exists(packagedRelativePath) ? packagedRelativePath : contentPath;
    }
}
