namespace GameGameGame.Content;

public static class ContentWorkspaceSurfaceService
{
    public static ContentWorkspaceSurface Build(EditableContentDocument document, ContentCompileOptions? options = null)
    {
        var compileResult = ContentCompiler.Compile(document, options);
        var symbols = compileResult.Symbols;
        var references = compileResult.References;

        return new ContentWorkspaceSurface(
            new ContentWorkspaceSourceSummary(
                options?.DocumentId,
                options?.SourcePath,
                IsSingleDocument: true),
            ItemsFor(symbols, ContentSymbolKind.Scenario),
            ItemsFor(symbols, ContentSymbolKind.EntityTemplate),
            ItemsFor(symbols, ContentSymbolKind.ActionPlan),
            ItemsFor(symbols, ContentSymbolKind.Presentation),
            ItemsFor(symbols, ContentSymbolKind.PresentationDefinition),
            ItemsFor(symbols, ContentSymbolKind.Palette),
            ItemsFor(symbols, ContentSymbolKind.MergedInventoryLayer),
            compileResult.Diagnostics,
            symbols,
            references,
            references.Where(reference => reference.Resolution == ContentReferenceResolution.Missing).ToList());
    }

    private static IReadOnlyList<ContentWorkspaceSymbolSummary> ItemsFor(
        IReadOnlyList<ContentSymbol> symbols,
        ContentSymbolKind kind) =>
        symbols
            .Where(symbol => symbol.Kind == kind)
            .OrderBy(symbol => symbol.Id, StringComparer.Ordinal)
            .Select(symbol => new ContentWorkspaceSymbolSummary(
                symbol.Kind,
                symbol.Id,
                symbol.DisplayName,
                symbol.DocumentId,
                symbol.SourcePath))
            .ToList();
}

public sealed record ContentWorkspaceSurface(
    ContentWorkspaceSourceSummary Source,
    IReadOnlyList<ContentWorkspaceSymbolSummary> Scenarios,
    IReadOnlyList<ContentWorkspaceSymbolSummary> EntityTemplates,
    IReadOnlyList<ContentWorkspaceSymbolSummary> ActionPlans,
    IReadOnlyList<ContentWorkspaceSymbolSummary> Presentations,
    IReadOnlyList<ContentWorkspaceSymbolSummary> PresentationDefinitions,
    IReadOnlyList<ContentWorkspaceSymbolSummary> Palettes,
    IReadOnlyList<ContentWorkspaceSymbolSummary> MergedLayers,
    IReadOnlyList<ContentDiagnostic> Diagnostics,
    IReadOnlyList<ContentSymbol> Symbols,
    IReadOnlyList<ContentReference> References,
    IReadOnlyList<ContentReference> MissingReferences)
{
    public bool IsValid => Diagnostics.All(diagnostic => diagnostic.Severity != ContentDiagnosticSeverity.Error);
}

public sealed record ContentWorkspaceSourceSummary(
    string? DocumentId,
    string? SourcePath,
    bool IsSingleDocument);

public sealed record ContentWorkspaceSymbolSummary(
    ContentSymbolKind Kind,
    string Id,
    string DisplayName,
    string? DocumentId,
    string? SourcePath);
