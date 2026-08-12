using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed record ContentCompileOptions(
    string? DocumentId = null,
    string? SourcePath = null);

public sealed record ContentCompileResult(
    PrototypeContentRegistry? Registry,
    ContentValidationResult Validation,
    IReadOnlyList<ContentSymbol>? Symbols = null,
    IReadOnlyList<ContentReference>? References = null)
{
    public IReadOnlyList<ContentDiagnostic> Diagnostics => Validation.Diagnostics;

    public IReadOnlyList<ContentSymbol> Symbols { get; } = Symbols ?? [];

    public IReadOnlyList<ContentReference> References { get; } = References ?? [];
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
