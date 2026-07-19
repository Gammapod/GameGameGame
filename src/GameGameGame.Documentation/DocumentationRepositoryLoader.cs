namespace GameGameGame.Documentation;

public static class DocumentationRepositoryLoader
{
    public static IReadOnlyList<MarkdownDocument> LoadCompiledDocuments(string repositoryRoot)
    {
        var docsRoot = Path.Combine(repositoryRoot, "docs");
        if (!Directory.Exists(docsRoot))
        {
            return [];
        }

        return Directory.EnumerateFiles(docsRoot, "*.md", SearchOption.AllDirectories)
            .Select(path => MarkdownFrontmatterParser.Parse(RelativePath(repositoryRoot, path), File.ReadAllText(path)))
            .Where(document => document.Metadata.Id is not null)
            .OrderBy(document => document.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string RelativePath(string root, string path)
    {
        return Path.GetRelativePath(root, path).Replace('\\', '/');
    }
}
