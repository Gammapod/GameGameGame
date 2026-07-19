namespace GameGameGame.Documentation;

public sealed record MarkdownDocument(string Path, DocumentMetadata Metadata, string Body);

public sealed record DocumentMetadata(
    string? Id,
    string? Title,
    string? Kind,
    string? Subkind,
    string? Status,
    IReadOnlyList<string> Owners,
    IReadOnlyList<string> Audience,
    string? Lane,
    IReadOnlyList<string> ReadWhen,
    IReadOnlyList<string> DoNotReadWhen,
    IReadOnlyList<string> Related,
    IReadOnlyList<string> Supersedes,
    string? SupersededBy,
    IReadOnlyList<string> CodeRefs,
    IReadOnlyList<string> TestRefs,
    string? TruthRankRaw,
    int? TruthRank,
    IReadOnlyList<string> TruthDomains,
    IReadOnlyDictionary<string, IReadOnlyList<string>> RawFields)
{
    public bool HasField(string name) => RawFields.ContainsKey(name);
}

public sealed record DocumentationDiagnostic(string Code, string Path, string Message);

public sealed record DocumentationLintResult(IReadOnlyList<DocumentationDiagnostic> Diagnostics)
{
    public bool Success => Diagnostics.Count == 0;
}

public static class DocumentationDiagnosticCodes
{
    public const string MissingFrontmatter = nameof(MissingFrontmatter);
    public const string MissingRequiredMetadata = nameof(MissingRequiredMetadata);
    public const string DuplicateDocumentId = nameof(DuplicateDocumentId);
    public const string UnknownRole = nameof(UnknownRole);
    public const string UnknownKind = nameof(UnknownKind);
    public const string MissingSourceOfTruthMetadata = nameof(MissingSourceOfTruthMetadata);
    public const string UnknownLane = nameof(UnknownLane);
    public const string UnresolvedDocumentReference = nameof(UnresolvedDocumentReference);
    public const string UnresolvedMarkdownLink = nameof(UnresolvedMarkdownLink);
    public const string UnresolvedPathReference = nameof(UnresolvedPathReference);
    public const string SourceOfTruthPathKindMismatch = nameof(SourceOfTruthPathKindMismatch);
    public const string ArchivedPathStatusMismatch = nameof(ArchivedPathStatusMismatch);
    public const string ActivePlanInArchive = nameof(ActivePlanInArchive);
    public const string InvalidTruthRank = nameof(InvalidTruthRank);
    public const string UnknownTruthDomain = nameof(UnknownTruthDomain);
}

public sealed record TraversalProfile(string Id, IReadOnlyList<string> Lanes, IReadOnlyList<MarkdownDocument> Documents);

public sealed record TraversalCoverageMetric(string DocumentId, string Path, int ProfileCount);

public sealed record DocumentationGraphEdge(MarkdownDocument From, MarkdownDocument To, string Kind, string Label);
