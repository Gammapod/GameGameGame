using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed record ContentCompileOptions(
    string? DocumentId = null,
    string? SourcePath = null);

public enum ContentWorkspaceSourceKind
{
    Unknown,
    Canonical,
    User,
    Generated,
    Test,
    Compatibility
}

public sealed class ContentWorkspaceDocument(
    EditableContentDocument Document,
    string? DocumentId = null,
    string? SourcePath = null,
    ContentWorkspaceSourceKind SourceKind = ContentWorkspaceSourceKind.Unknown,
    bool IsReadOnly = false,
    bool IsDirty = false,
    bool HasProtectedMutation = false)
{
    public EditableContentDocument Document { get; } = Document;

    public string? DocumentId { get; } = DocumentId;

    public string? SourcePath { get; } = SourcePath;

    public ContentWorkspaceSourceKind SourceKind { get; } = SourceKind;

    public bool IsReadOnly { get; } = IsReadOnly;

    public bool IsDirty { get; set; } = IsDirty;

    public bool HasProtectedMutation { get; set; } = HasProtectedMutation;
}

public sealed record ContentWorkspace(IReadOnlyList<ContentWorkspaceDocument> Documents);

public sealed record ContentWorkspaceDocumentSummary(
    string? DocumentId,
    string? SourcePath,
    ContentWorkspaceSourceKind SourceKind,
    bool IsReadOnly,
    bool IsDirty,
    int LoadOrder,
    bool HasProtectedMutation = false);

public sealed record ContentCompileResult(
    PrototypeContentRegistry? Registry,
    ContentValidationResult Validation,
    IReadOnlyList<ContentSymbol>? Symbols = null,
    IReadOnlyList<ContentReference>? References = null,
    IReadOnlyList<ContentWorkspaceDocumentSummary>? WorkspaceDocuments = null)
{
    public IReadOnlyList<ContentDiagnostic> Diagnostics => Validation.Diagnostics;

    public IReadOnlyList<ContentSymbol> Symbols { get; } = Symbols ?? [];

    public IReadOnlyList<ContentReference> References { get; } = References ?? [];

    public IReadOnlyList<ContentWorkspaceDocumentSummary> WorkspaceDocuments { get; init; } = WorkspaceDocuments ?? [];
}

public enum ContentSymbolKind
{
    EntityTemplate,
    ActionPlan,
    Scenario,
    Presentation,
    PresentationDefinition,
    Palette,
    MergedInventoryLayer,
    AuthoredEntityInstance
}

public enum ContentReferenceKind
{
    DefaultActionPlan,
    CarriedEntityTemplate,
    ScenarioRootTemplate,
    ScenarioPlayerTemplate,
    BehaviorStepPlan,
    BehaviorStepTemplate,
    BehaviorStepCostTemplate,
    TargetingTargetTemplate,
    MergedLayerOwner,
    PresentationForTemplate,
    PresentationId,
    PaletteId
}

public enum ContentReferenceResolution
{
    Resolved,
    Missing,
    Ambiguous
}

public sealed record ContentSymbol(
    ContentSymbolKind Kind,
    string Id,
    string DisplayName,
    string? DocumentId = null,
    string? SourcePath = null);

public sealed record ContentReference(
    ContentReferenceKind Kind,
    ContentSymbolKind SourceKind,
    string SourceId,
    ContentSymbolKind TargetKind,
    string TargetId,
    ContentReferenceResolution Resolution,
    int? StepIndex = null,
    EntityId? RelatedEntityId = null,
    string? DocumentId = null,
    string? SourcePath = null);
