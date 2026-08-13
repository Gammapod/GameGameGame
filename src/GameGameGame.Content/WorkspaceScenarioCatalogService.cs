namespace GameGameGame.Content;

public enum WorkspaceScenarioLaunchKind
{
    Workspace,
    File
}

public sealed record WorkspaceScenarioCatalogEntry(
    string EntryId,
    string ScenarioId,
    string Name,
    string? Description,
    string? Status,
    IReadOnlyList<string> Tags,
    string? SourcePath,
    string? Source,
    WorkspaceScenarioLaunchKind LaunchKind)
{
    public bool IsWorkspaceBacked => LaunchKind == WorkspaceScenarioLaunchKind.Workspace;
}

public sealed record WorkspaceScenarioCatalogResult(
    ContentWorkspace Workspace,
    IReadOnlyList<WorkspaceScenarioCatalogEntry> Entries,
    IReadOnlyList<string> Diagnostics);

public sealed record WorkspaceScenarioCatalogOptions(
    IReadOnlyList<string>? WorkspaceContentPaths = null,
    bool IncludeSingleFileCompatibilityScenarios = true,
    string? CompatibilityScenarioFolder = null)
{
    public IReadOnlyList<string> WorkspaceContentPaths { get; } = WorkspaceContentPaths ?? WorkspaceScenarioCatalogService.DefaultDebugRoomWorkspacePaths;
}

public static class WorkspaceScenarioCatalogService
{
    public static readonly IReadOnlyList<string> DefaultDebugRoomWorkspacePaths =
    [
        Path.Combine("src", "GameGameGame.Content", "Canonical", "Creatures", "DebugPlayer.yaml"),
        Path.Combine("src", "GameGameGame.Content", "Canonical", "Spaces", "DebugRoomRoot.yaml"),
        Path.Combine("src", "GameGameGame.Content", "Debug", "DebugRoom.yaml")
    ];

    public static WorkspaceScenarioCatalogResult BuildDefaultCatalog() =>
        BuildCatalog(new WorkspaceScenarioCatalogOptions());

    public static WorkspaceScenarioCatalogResult BuildCatalog(WorkspaceScenarioCatalogOptions? options = null)
    {
        options ??= new WorkspaceScenarioCatalogOptions();
        var diagnostics = new List<string>();
        var workspace = BuildWorkspace(options.WorkspaceContentPaths, diagnostics);
        var compile = ContentCompiler.Compile(workspace);
        diagnostics.AddRange(compile.Diagnostics.Select(FormatDiagnostic));

        var entries = BuildWorkspaceEntries(workspace)
            .Concat(options.IncludeSingleFileCompatibilityScenarios
                ? BuildCompatibilityEntries(options.CompatibilityScenarioFolder, diagnostics)
                : [])
            .GroupBy(entry => entry.EntryId, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(entry => entry.IsWorkspaceBacked ? 0 : 1)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.ScenarioId, StringComparer.Ordinal)
            .ToList();

        return new WorkspaceScenarioCatalogResult(workspace, entries, diagnostics.Distinct(StringComparer.Ordinal).ToList());
    }

    public static PlayableScenarioSession Launch(WorkspaceScenarioCatalogResult catalog, string entryId)
    {
        var entry = catalog.Entries.FirstOrDefault(entry => string.Equals(entry.EntryId, entryId, StringComparison.Ordinal));
        if (entry is null)
        {
            throw new ArgumentException($"Workspace scenario catalog entry {entryId} was not found.", nameof(entryId));
        }

        return entry.LaunchKind == WorkspaceScenarioLaunchKind.Workspace
            ? PlayableScenarioLauncher.CreateFromWorkspace(catalog.Workspace, entry.ScenarioId)
            : PlayableScenarioLauncher.CreateFromFile(entry.SourcePath ?? string.Empty, entry.ScenarioId);
    }

    private static ContentWorkspace BuildWorkspace(IReadOnlyList<string> contentPaths, List<string> diagnostics)
    {
        var documents = new List<ContentWorkspaceDocument>();
        foreach (var relativePath in contentPaths)
        {
            if (!TryResolveRepositoryFile(relativePath, out var path))
            {
                diagnostics.Add($"Workspace content path {relativePath} was not found.");
                continue;
            }

            try
            {
                var document = EditableContentDocument.LoadYaml(File.ReadAllText(path));
                documents.Add(new ContentWorkspaceDocument(
                    document,
                    CreateDocumentId(relativePath),
                    path,
                    SourceKindFor(relativePath),
                    IsReadOnly: SourceKindFor(relativePath) == ContentWorkspaceSourceKind.Canonical));
            }
            catch (Exception ex)
            {
                diagnostics.Add($"Workspace content path {path} could not load: {ex.Message}");
            }
        }

        return new ContentWorkspace(documents);
    }

    private static IReadOnlyList<WorkspaceScenarioCatalogEntry> BuildWorkspaceEntries(ContentWorkspace workspace) =>
        workspace.Documents
            .SelectMany(document => document.Document.Scenarios.Select(pair => new WorkspaceScenarioCatalogEntry(
                EntryId: $"workspace:{pair.Key}",
                ScenarioId: pair.Key,
                Name: string.IsNullOrWhiteSpace(pair.Value.Name) ? pair.Key : pair.Value.Name!,
                Description: null,
                Status: document.SourceKind == ContentWorkspaceSourceKind.Canonical ? "canonical" : null,
                Tags: [],
                SourcePath: document.SourcePath,
                Source: document.DocumentId,
                LaunchKind: WorkspaceScenarioLaunchKind.Workspace)))
            .ToList();

    private static IReadOnlyList<WorkspaceScenarioCatalogEntry> BuildCompatibilityEntries(string? folderPath, List<string> diagnostics)
    {
        var folder = folderPath ?? ScenarioCatalog.DefaultDiscoveryFolder;
        var manifestPath = Path.Combine(folder, ScenarioCatalog.ManifestFileName);
        var catalog = File.Exists(manifestPath)
            ? ScenarioCatalog.LoadManifest(manifestPath)
            : ScenarioCatalog.DiscoverFolder(folder);
        diagnostics.AddRange(catalog.Diagnostics.Select(diagnostic => $"Compatibility scenario catalog: {diagnostic}"));

        return catalog.Entries.Select(entry => new WorkspaceScenarioCatalogEntry(
            EntryId: $"file:{Path.GetFullPath(entry.ContentPath)}:{entry.ScenarioId}",
            ScenarioId: entry.ScenarioId,
            Name: entry.Name,
            Description: entry.Description,
            Status: entry.Status,
            Tags: entry.Tags ?? [],
            SourcePath: entry.ContentPath,
            Source: entry.Source,
            LaunchKind: WorkspaceScenarioLaunchKind.File)).ToList();
    }

    private static string CreateDocumentId(string relativePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(relativePath);
        if (relativePath.Contains(Path.Combine("Canonical", "Creatures"), StringComparison.OrdinalIgnoreCase))
        {
            return $"canonical.creatures.{ToKebabCase(fileName)}";
        }

        if (relativePath.Contains(Path.Combine("Canonical", "Spaces"), StringComparison.OrdinalIgnoreCase))
        {
            return $"canonical.spaces.{ToKebabCase(fileName)}";
        }

        if (relativePath.Contains(Path.Combine("Debug"), StringComparison.OrdinalIgnoreCase))
        {
            return $"debug.{ToKebabCase(fileName)}";
        }

        return ToKebabCase(fileName);
    }

    private static ContentWorkspaceSourceKind SourceKindFor(string relativePath) =>
        relativePath.Contains(Path.Combine("Canonical"), StringComparison.OrdinalIgnoreCase)
            ? ContentWorkspaceSourceKind.Canonical
            : ContentWorkspaceSourceKind.User;

    private static string ToKebabCase(string value)
    {
        var result = new List<char>();
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (char.IsUpper(current) && i > 0 && result[^1] != '-')
            {
                result.Add('-');
            }

            result.Add(char.ToLowerInvariant(current));
        }

        return new string(result.ToArray());
    }

    private static bool TryResolveRepositoryFile(string relativePath, out string path)
    {
        var directory = Directory.GetCurrentDirectory();
        while (directory is not null)
        {
            var candidate = Path.Combine(directory, relativePath);
            if (File.Exists(candidate))
            {
                path = candidate;
                return true;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        path = relativePath;
        return false;
    }

    private static string FormatDiagnostic(ContentDiagnostic diagnostic) =>
        string.IsNullOrWhiteSpace(diagnostic.SourcePath)
            ? diagnostic.Message
            : $"{diagnostic.SourcePath}: {diagnostic.Message}";
}
