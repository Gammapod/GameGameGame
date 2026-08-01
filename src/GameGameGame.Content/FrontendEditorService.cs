using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed class FrontendEditorService(ContentEditorSession session)
{
    public ContentEditorSession Session { get; } = session;

    public static FrontendEditorOpenResult OpenFile(string path)
    {
        var result = ContentEditorSession.OpenFile(path);
        return result.IsSuccess
            ? FrontendEditorOpenResult.Success(new FrontendEditorService(result.Session!))
            : FrontendEditorOpenResult.Failure(result.ErrorMessage ?? $"Could not open content file {path}.");
    }

    public static FrontendEditorService CreateNew() => new(ContentEditorSession.CreateNew());

    public FrontendEditorMutationResult CreateEntityTemplate(string name)
        => new FrontendEntityTemplateMutationService(Session, GetSnapshot)
            .CreateEntityTemplate(name);

    public FrontendEditorMutationResult DuplicateEntityTemplate(string sourceTemplateId, string name)
        => new FrontendEntityTemplateMutationService(Session, GetSnapshot)
            .DuplicateEntityTemplate(sourceTemplateId, name);

    public FrontendEditorMutationResult DeleteEntityTemplate(string templateId)
        => new FrontendEntityTemplateMutationService(Session, GetSnapshot)
            .DeleteEntityTemplate(templateId);

    public FrontendEditorMutationResult CreateActionPlan(string name)
        => new FrontendActionPlanMutationService(Session, GetSnapshot)
            .CreateActionPlan(name);

    public FrontendEditorMutationResult CreatePassiveActionPlan(string name)
        => new FrontendActionPlanMutationService(Session, GetSnapshot)
            .CreatePassiveActionPlan(name);

    public FrontendEditorMutationResult DuplicateActionPlan(string sourceActionPlanId, string name)
        => new FrontendActionPlanMutationService(Session, GetSnapshot)
            .DuplicateActionPlan(sourceActionPlanId, name);

    public FrontendEditorMutationResult DeleteActionPlan(string actionPlanId)
        => new FrontendActionPlanMutationService(Session, GetSnapshot)
            .DeleteActionPlan(actionPlanId);

    public FrontendEditorMutationResult UpdateTemplatePresentation(
        string templateId,
        FrontendEditorTemplatePresentationUpdate update)
        => new FrontendEntityTemplateMutationService(Session, GetSnapshot)
            .UpdateTemplatePresentation(templateId, update);

    public FrontendEditorMutationResult UpdateTemplateMetadata(
        string templateId,
        FrontendEditorTemplateMetadataUpdate update)
        => new FrontendEntityTemplateMutationService(Session, GetSnapshot)
            .UpdateTemplateMetadata(templateId, update);

    public FrontendEditorMutationResult SetTemplateInitialFacing(string templateId, Direction facing)
        => new FrontendEntityTemplateMutationService(Session, GetSnapshot)
            .SetTemplateInitialFacing(templateId, facing);

    public FrontendEditorMutationResult ClearTemplateInitialFacing(string templateId)
        => new FrontendEntityTemplateMutationService(Session, GetSnapshot)
            .ClearTemplateInitialFacing(templateId);

    public FrontendEditorMutationResult SetTemplateEnterPolicy(string templateId, EntityEnterPolicy enterPolicy)
        => new FrontendEntityTemplateMutationService(Session, GetSnapshot)
            .SetTemplateEnterPolicy(templateId, enterPolicy);

    public FrontendEditorMutationResult ClearTemplateEnterPolicy(string templateId)
        => new FrontendEntityTemplateMutationService(Session, GetSnapshot)
            .ClearTemplateEnterPolicy(templateId);

    public FrontendEditorMutationResult SetTemplateExitPolicy(string templateId, EntityExitPolicy exitPolicy)
        => new FrontendEntityTemplateMutationService(Session, GetSnapshot)
            .SetTemplateExitPolicy(templateId, exitPolicy);

    public FrontendEditorMutationResult ClearTemplateExitPolicy(string templateId)
        => new FrontendEntityTemplateMutationService(Session, GetSnapshot)
            .ClearTemplateExitPolicy(templateId);

    public FrontendEditorMutationResult SetTemplateTopologyPolicy(string templateId, EntityTopologyPolicy topologyPolicy)
        => new FrontendEntityTemplateMutationService(Session, GetSnapshot)
            .SetTemplateTopologyPolicy(templateId, topologyPolicy);

    public FrontendEditorMutationResult Save()
    {
        if (Session.FilePath is null)
        {
            return FrontendEditorMutationResult.Failure(
                "Cannot save yet because this editor context has no file path. Save As is not implemented in SadConsole Editor MVP.",
                GetSnapshot());
        }

        var result = Session.Save();
        return result.IsSuccess
            ? FrontendEditorMutationResult.Success($"Saved {Session.FilePath}.", GetSnapshot())
            : FrontendEditorMutationResult.Failure(result.ErrorMessage ?? "Save failed.", GetSnapshot());
    }

    public FrontendEditorMutationResult SetTemplateDefaultActionPlan(string templateId, string actionPlanId)
        => new FrontendEntityTemplateMutationService(Session, GetSnapshot)
            .SetTemplateDefaultActionPlan(templateId, actionPlanId);

    public FrontendEditorMutationResult ClearTemplateDefaultActionPlan(string templateId)
        => new FrontendEntityTemplateMutationService(Session, GetSnapshot)
            .ClearTemplateDefaultActionPlan(templateId);

    public FrontendEditorMutationResult SetTemplateTargetingRule(
        string templateId,
        FrontendEditorTargetingRuleUpdate update)
        => new FrontendTargetingRuleMutationService(Session, GetSnapshot)
            .SetTemplateTargetingRule(templateId, update);

    public FrontendEditorMutationResult SetTemplateTargetingProfileRule(
        string templateId,
        FrontendEditorTargetingProfileRuleUpdate update)
        => new FrontendTargetingRuleMutationService(Session, GetSnapshot)
            .SetTemplateTargetingProfileRule(templateId, update);

    public FrontendEditorMutationResult SetTemplateTargetingDefaultLocality(
        string templateId,
        IReadOnlyList<TargetingLocalityOrigin> origins)
        => new FrontendTargetingRuleMutationService(Session, GetSnapshot)
            .SetTemplateTargetingDefaultLocality(templateId, origins);

    public FrontendEditorMutationResult ClearTemplateTargetingRule(string templateId, int slot)
        => new FrontendTargetingRuleMutationService(Session, GetSnapshot)
            .ClearTemplateTargetingRule(templateId, slot);

    public FrontendEditorMutationResult PlaceTemplateInInventory(
        string parentTemplateId,
        string brushTemplateId,
        GridCoord coord)
        => new FrontendCarriedLayoutMutationService(Session, GetSnapshot)
            .PlaceTemplateInInventory(parentTemplateId, brushTemplateId, coord);

    public FrontendEditorMutationResult RemoveCarriedEntity(string parentTemplateId, string entityId)
        => new FrontendCarriedLayoutMutationService(Session, GetSnapshot)
            .RemoveCarriedEntity(parentTemplateId, entityId);

    public FrontendEditorMutationResult MoveCarriedEntity(string parentTemplateId, string entityId, GridCoord coord)
        => new FrontendCarriedLayoutMutationService(Session, GetSnapshot)
            .MoveCarriedEntity(parentTemplateId, entityId, coord);

    public FrontendEditorMutationResult ReplaceCarriedEntityTemplate(
        string parentTemplateId,
        string entityId,
        string brushTemplateId)
        => new FrontendCarriedLayoutMutationService(Session, GetSnapshot)
            .ReplaceCarriedEntityTemplate(parentTemplateId, entityId, brushTemplateId);

    public FrontendEditorMutationResult SetCarriedEntityController(
        string parentTemplateId,
        string entityId,
        EntityController? controller)
        => new FrontendCarriedLayoutMutationService(Session, GetSnapshot)
            .SetCarriedEntityController(parentTemplateId, entityId, controller);

    public FrontendEditorMutationResult OverwriteTemplateInInventory(
        string parentTemplateId,
        string brushTemplateId,
        GridCoord coord)
        => new FrontendCarriedLayoutMutationService(Session, GetSnapshot)
            .OverwriteTemplateInInventory(parentTemplateId, brushTemplateId, coord);

    public FrontendEditorMutationResult ReplaceActionPlanStep(
        string actionPlanId,
        int stepIndex,
        ActionPlanBehaviorStepKind kind)
        => new FrontendActionPlanMutationService(Session, GetSnapshot)
            .ReplaceActionPlanStep(actionPlanId, stepIndex, kind);

    public FrontendEditorMutationResult InsertActionPlanStep(
        string actionPlanId,
        int insertIndex,
        ActionPlanBehaviorStepKind kind)
        => new FrontendActionPlanMutationService(Session, GetSnapshot)
            .InsertActionPlanStep(actionPlanId, insertIndex, kind);

    public FrontendEditorMutationResult RemoveActionPlanStep(string actionPlanId, int stepIndex)
        => new FrontendActionPlanMutationService(Session, GetSnapshot)
            .RemoveActionPlanStep(actionPlanId, stepIndex);

    public FrontendEditorMutationResult MoveActionPlanStep(string actionPlanId, int fromIndex, int toIndex)
        => new FrontendActionPlanMutationService(Session, GetSnapshot)
            .MoveActionPlanStep(actionPlanId, fromIndex, toIndex);

    public FrontendEditorMutationResult SetActionPlanStepTargetLabel(
        string actionPlanId,
        int stepIndex,
        string? targetLabel)
        => new FrontendActionPlanMutationService(Session, GetSnapshot)
            .SetActionPlanStepTargetLabel(actionPlanId, stepIndex, targetLabel);

    public FrontendEditorMutationResult SetActionPlanStepCosts(
        string actionPlanId,
        int stepIndex,
        IReadOnlyList<ActionStepCostDescriptor> costs)
        => new FrontendActionPlanMutationService(Session, GetSnapshot)
            .SetActionPlanStepCosts(actionPlanId, stepIndex, costs);

    public FrontendEditorMutationResult SetActionPlanStepTargetPathMode(
        string actionPlanId,
        int stepIndex,
        ActionPlanTargetPathMode? pathMode)
        => new FrontendActionPlanMutationService(Session, GetSnapshot)
            .SetActionPlanStepTargetPathMode(actionPlanId, stepIndex, pathMode);

    public FrontendEditorMutationResult SetActionPlanStepDesiredDistance(
        string actionPlanId,
        int stepIndex,
        int? desiredDistance)
        => new FrontendActionPlanMutationService(Session, GetSnapshot)
            .SetActionPlanStepDesiredDistance(actionPlanId, stepIndex, desiredDistance);

    public FrontendEditorMutationResult SetActionPlanStepOrbitDirection(
        string actionPlanId,
        int stepIndex,
        ActionPlanOrbitDirection? orbitDirection)
        => new FrontendActionPlanMutationService(Session, GetSnapshot)
            .SetActionPlanStepOrbitDirection(actionPlanId, stepIndex, orbitDirection);

    public FrontendEditorSnapshot GetSnapshot()
        => new FrontendEditorSnapshotBuilder(Session).Build();

    public FrontendEditorScenarioPreview PreviewScenario(string scenarioId)
    {
        var session = PlayableScenarioLauncher.CreateFromDocument(Session.Document, scenarioId);

        return new FrontendEditorScenarioPreview(
            session.ScenarioId,
            session.Name,
            IsDerivedRuntimeState: true,
            session.CanPlay,
            session,
            session.ValidationDiagnostics,
            session.RuntimeFailures,
            session.CapabilityGaps);
    }

}

public sealed record FrontendEditorOpenResult(FrontendEditorService? Service, string? ErrorMessage)
{
    public bool IsSuccess => Service is not null;

    public static FrontendEditorOpenResult Success(FrontendEditorService service) => new(service, ErrorMessage: null);

    public static FrontendEditorOpenResult Failure(string errorMessage) => new(Service: null, errorMessage);
}

public sealed record FrontendEditorTemplatePresentationUpdate(
    string Name,
    string? GlyphText,
    PresentationColor Color,
    PresentationId? PresentationId = null,
    PaletteId? PaletteId = null);

public sealed record FrontendEditorTemplateMetadataUpdate(
    int InventoryWidth,
    int InventoryHeight,
    int Bulk,
    int Aperture);

public sealed record FrontendEditorTargetingRuleUpdate(
    int Slot,
    string Label,
    string? TargetTemplateId,
    int Range,
    IReadOnlyList<ActionPlanBehaviorStepKind>? TargetCapabilities = null)
{
    public IReadOnlyList<ActionPlanBehaviorStepKind> TargetCapabilities { get; } = TargetCapabilities ?? [];
}

public sealed record FrontendEditorTargetingProfileRuleUpdate(
    int Range,
    int Slot,
    string Label,
    string? TargetTemplateId,
    IReadOnlyList<TargetingLocalityOrigin>? LocalityOrigins = null,
    IReadOnlyList<ActionPlanBehaviorStepKind>? TargetCapabilities = null)
{
    public IReadOnlyList<ActionPlanBehaviorStepKind> TargetCapabilities { get; } = TargetCapabilities ?? [];
}

public enum FrontendEditorTargetingSource
{
    None,
    TargetingProfile,
    LegacyTargetingRules
}

public sealed record FrontendEditorMutationResult(
    bool IsSuccess,
    string StatusMessage,
    FrontendEditorSnapshot Snapshot)
{
    public static FrontendEditorMutationResult Success(string statusMessage, FrontendEditorSnapshot snapshot) =>
        new(IsSuccess: true, statusMessage, snapshot);

    public static FrontendEditorMutationResult Failure(string statusMessage, FrontendEditorSnapshot snapshot) =>
        new(IsSuccess: false, statusMessage, snapshot);
}

public sealed record FrontendEditorSnapshot(
    string? FilePath,
    bool IsDirty,
    IReadOnlyList<FrontendEditorScenarioSummary> Scenarios,
    IReadOnlyList<FrontendEditorEntityTemplateSummary> EntityTemplates,
    IReadOnlyList<FrontendEditorActionPlanSummary> ActionPlans,
    IReadOnlyList<FrontendEditorAvailableActionStepSummary> AvailableActionSteps,
    IReadOnlyList<FrontendEditorDiagnostic> ValidationDiagnostics,
    string YamlPreview,
    IReadOnlyList<string> YamlDiffLines);

public sealed record FrontendEditorScenarioSummary(
    string ScenarioId,
    string Name,
    string ScenarioRootEntityTemplateId,
    string? PlayerEntityTemplateId,
    string? PlayerEntityId,
    GridCoord PlayerStart,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? PlayerControls = null)
{
    public GridCoord? AuthoredPlayerStart { get; init; }
}

public sealed record FrontendEditorEntityTemplateSummary(
    string TemplateId,
    string Name,
    char Glyph,
    PresentationColor Color,
    int InventoryWidth,
    int InventoryHeight,
    int Bulk,
    int Aperture,
    string? DefaultActionPlanId,
    FrontendEditorActionStateDefaultsSummary ActionStateDefaults,
    IReadOnlyList<FrontendEditorTargetingRuleSummary> TargetingRules,
    IReadOnlyList<FrontendEditorCarriedEntitySummary> CarriedEntities,
    IReadOnlyList<FrontendEditorDiagnostic> Diagnostics)
{
    public PresentationId PresentationId { get; init; }

    public PaletteId PaletteId { get; init; }

    public EntityEnterPolicy? EnterPolicy { get; init; }

    public EntityEnterPolicy EffectiveEnterPolicy { get; init; } = EntityEnterPolicy.FirstUnoccupiedRowMajor;

    public EntityExitPolicy? ExitPolicy { get; init; }

    public EntityExitPolicy EffectiveExitPolicy { get; init; } = EntityExitPolicy.AnyCell;

    public EntityTopologyPolicy TopologyPolicy { get; init; } = EntityTopologyPolicy.None;

    public IReadOnlyList<FrontendEditorTargetingRequirementSummary> TargetingRequirements { get; init; } = [];

    public IReadOnlyList<FrontendEditorTargetingRuleSummary> OrphanedTargetingRules { get; init; } = [];

    public FrontendEditorTargetingSource TargetingSource { get; init; } = FrontendEditorTargetingSource.None;

    public FrontendEditorTargetingProfileSummary? TargetingProfile { get; init; }
}

public sealed record FrontendEditorActionStateDefaultsSummary(
    Direction? Facing,
    string? TargetEntityId);

public sealed record FrontendEditorTargetingRuleSummary(
    int Slot,
    string? Label,
    string? Hint,
    string? TargetTemplateId,
    string? TargetTemplateName,
    int Range,
    IReadOnlyList<ActionPlanBehaviorStepKind>? TargetCapabilities = null,
    IReadOnlyList<TargetingLocalityOrigin>? LocalityOrigins = null,
    IReadOnlyList<TargetingLocalityOrigin>? EffectiveLocalityOrigins = null)
{
    public IReadOnlyList<ActionPlanBehaviorStepKind> TargetCapabilities { get; } = TargetCapabilities ?? [];

    public IReadOnlyList<TargetingLocalityOrigin>? LocalityOrigins { get; } = LocalityOrigins;

    public IReadOnlyList<TargetingLocalityOrigin> EffectiveLocalityOrigins { get; } = EffectiveLocalityOrigins ?? [TargetingLocalityOrigin.CurrentPlace];
}

public sealed record FrontendEditorTargetingProfileSummary(
    int Range,
    IReadOnlyList<TargetingLocalityOrigin> DefaultLocalityOrigins);

public sealed record FrontendEditorTargetingRequirementSummary(
    string Label,
    IReadOnlyList<int> StepIndexes,
    IReadOnlyList<ActionPlanBehaviorStepKind> StepKinds,
    bool IsConfigured,
    FrontendEditorTargetingRuleSummary? Rule);

public sealed record FrontendEditorCarriedEntitySummary(
    string EntityId,
    string? TemplateId,
    string? TemplateName,
    char? Glyph,
    PresentationColor? Color,
    GridCoord Coord,
    IReadOnlyList<FrontendEditorDiagnostic> Diagnostics,
    EntityController? Controller = null)
{
    public PresentationId? PresentationId { get; init; }

    public PaletteId? PaletteId { get; init; }
}

public sealed record FrontendEditorActionPlanSummary(
    string ActionPlanId,
    string Shape,
    IReadOnlyList<FrontendEditorActionPlanStepSummary> ActionSteps,
    IReadOnlyList<string> ActionStepNames)
{
    public IReadOnlyList<FrontendEditorActionPlanTargetLabelRequirementSummary> TargetLabelRequirements { get; init; } = [];
}

public sealed record FrontendEditorActionPlanTargetLabelRequirementSummary(
    string Label,
    IReadOnlyList<int> StepIndexes,
    IReadOnlyList<ActionPlanBehaviorStepKind> StepKinds);

public sealed record FrontendEditorActionPlanStepSummary(
    int Index,
    ActionPlanBehaviorStepKind Kind,
    string DisplayName,
    string? TargetLabel = null,
    int? TargetSlot = null,
    bool TargetSelf = false,
    bool ConsumesTargetReference = false,
    IReadOnlyList<ActionStepCostDescriptor>? Costs = null,
    string? CostSummary = null,
    ActionPlanTargetPathMode? PathMode = null,
    int? DesiredDistance = null,
    ActionPlanOrbitDirection? OrbitDirection = null)
{
    public IReadOnlyList<ActionStepCostDescriptor> Costs { get; } = Costs ?? [];
}

public sealed record FrontendEditorAvailableActionStepSummary(
    ActionPlanBehaviorStepKind Kind,
    string DisplayName,
    string Hint);

public sealed record FrontendEditorDiagnostic(
    ContentDiagnosticSeverity Severity,
    ContentDiagnosticCode Code,
    string Message,
    string? EntityTemplateId,
    string? ActionPlanId,
    int? StepIndex,
    string? CarriedEntityId,
    GridCoord? Coord)
{
    public static FrontendEditorDiagnostic From(ContentDiagnostic diagnostic) =>
        new(
            diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.EntityTemplateId?.Value,
            diagnostic.ActionPlanTemplateId?.Value,
            diagnostic.StepIndex,
            diagnostic.CarriedEntityId?.Value,
            diagnostic.Coord);
}

public sealed record FrontendEditorScenarioPreview(
    string ScenarioId,
    string Name,
    bool IsDerivedRuntimeState,
    bool CanPlay,
    PlayableScenarioSession Session,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps);
