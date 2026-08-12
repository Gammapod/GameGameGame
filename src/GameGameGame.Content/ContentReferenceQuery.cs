namespace GameGameGame.Content;

public static class ContentReferenceQuery
{
    public static IReadOnlyList<ContentReference> ListReferencesFrom(
        ContentWorkspaceSurface surface,
        ContentSymbolKind sourceKind,
        string sourceId) =>
        surface.References
            .Where(reference =>
                reference.SourceKind == sourceKind
                && string.Equals(reference.SourceId, sourceId, StringComparison.Ordinal))
            .OrderBy(reference => reference.Kind)
            .ThenBy(reference => reference.TargetKind)
            .ThenBy(reference => reference.TargetId, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<ContentReference> ListReferencesTo(
        ContentWorkspaceSurface surface,
        ContentSymbolKind targetKind,
        string targetId) =>
        surface.References
            .Where(reference =>
                reference.TargetKind == targetKind
                && string.Equals(reference.TargetId, targetId, StringComparison.Ordinal))
            .OrderBy(reference => reference.Kind)
            .ThenBy(reference => reference.SourceKind)
            .ThenBy(reference => reference.SourceId, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<ContentReference> ListMissingReferences(ContentWorkspaceSurface surface) =>
        surface.References
            .Where(reference => reference.Resolution == ContentReferenceResolution.Missing)
            .OrderBy(reference => reference.SourceKind)
            .ThenBy(reference => reference.SourceId, StringComparer.Ordinal)
            .ThenBy(reference => reference.Kind)
            .ThenBy(reference => reference.TargetId, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<ContentUsedBySummary> SummarizeUsedBy(
        ContentWorkspaceSurface surface,
        ContentSymbolKind targetKind,
        string targetId) =>
        ListReferencesTo(surface, targetKind, targetId)
            .Select(reference => new ContentUsedBySummary(
                reference.SourceKind,
                reference.SourceId,
                reference.Kind,
                reference.Resolution,
                reference.StepIndex,
                reference.RelatedEntityId?.Value,
                reference.DocumentId,
                reference.SourcePath))
            .ToList();
}

public sealed record ContentUsedBySummary(
    ContentSymbolKind SourceKind,
    string SourceId,
    ContentReferenceKind ReferenceKind,
    ContentReferenceResolution Resolution,
    int? StepIndex,
    string? RelatedEntityId,
    string? DocumentId,
    string? SourcePath);
