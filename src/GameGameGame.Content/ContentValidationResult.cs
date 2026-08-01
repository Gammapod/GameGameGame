using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed record ContentValidationResult(IReadOnlyList<ContentDiagnostic> Diagnostics)
{
    public IReadOnlyList<string> Errors => Diagnostics
        .Where(diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error)
        .Select(diagnostic => diagnostic.Message)
        .ToList();

    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<ContentDiagnostic> ForEntityTemplate(EntityTemplateId entityTemplateId) =>
        Diagnostics
            .Where(diagnostic => diagnostic.EntityTemplateId == entityTemplateId)
            .ToList();

    public IReadOnlyList<ContentDiagnostic> ForActionPlan(ActionPlanTemplateId actionPlanTemplateId) =>
        Diagnostics
            .Where(diagnostic => diagnostic.ActionPlanTemplateId == actionPlanTemplateId)
            .ToList();

    public IReadOnlyList<ContentDiagnostic> ForActionPlanStep(ActionPlanTemplateId actionPlanTemplateId, int stepIndex) =>
        Diagnostics
            .Where(diagnostic => diagnostic.ActionPlanTemplateId == actionPlanTemplateId && diagnostic.StepIndex == stepIndex)
            .ToList();

    public IReadOnlyList<ContentDiagnostic> ForCarriedEntity(EntityTemplateId entityTemplateId, EntityId carriedEntityId) =>
        Diagnostics
            .Where(diagnostic => diagnostic.EntityTemplateId == entityTemplateId && diagnostic.CarriedEntityId == carriedEntityId)
            .ToList();
}

public enum ContentDiagnosticSeverity
{
    Error,
    Warning,
    Info
}

public enum ContentDiagnosticCode
{
    General,
    MissingPresentation,
    MissingActionPlanReference,
    MissingCalledPlan,
    MissingPlanVariable,
    MissingPlanSlot,
    InvalidMovementDescriptor,
    InvalidActionPlanShape,
    ArbitraryPlanVariableField,
    PlanVariableTypeMismatch,
    InventoryOutOfBounds,
    InventoryOverlap,
    DuplicateCarriedEntityId,
    CarriedEntityWithoutUsableInventory,
    InvalidScenarioDefinition,
    InvalidTargetingRule,
    MissingTargetTemplateReference,
    InvalidActionStepTargetSlot,
    InvalidActionStepTargetReference,
    InvalidActionStepField,
    UnknownPresentationId,
    UnknownPaletteId
}

public sealed record ContentDiagnostic(
    ContentDiagnosticSeverity Severity,
    ContentDiagnosticCode Code,
    string Message,
    EntityTemplateId? EntityTemplateId = null,
    ActionPlanTemplateId? ActionPlanTemplateId = null,
    ActionPlanId? ActionPlanId = null,
    ActionPlanId? ReferencedActionPlanId = null,
    int? StepIndex = null,
    string? VariableName = null,
    ActionPlanSlot? ActionPlanSlot = null,
    PlanValueKind? ExpectedValueKind = null,
    PlanValueKind? ActualValueKind = null,
    EntityId? CarriedEntityId = null,
    EntityId? RelatedEntityId = null,
    GridCoord? Coord = null)
{
    public static ContentDiagnostic Error(
        ContentDiagnosticCode code,
        string message,
        EntityTemplateId? entityTemplateId = null,
        ActionPlanTemplateId? actionPlanTemplateId = null,
        ActionPlanId? actionPlanId = null,
        ActionPlanId? referencedActionPlanId = null,
        int? stepIndex = null,
        string? variableName = null,
        ActionPlanSlot? actionPlanSlot = null,
        PlanValueKind? expectedValueKind = null,
        PlanValueKind? actualValueKind = null,
        EntityId? carriedEntityId = null,
        EntityId? relatedEntityId = null,
        GridCoord? coord = null) =>
        new(
            ContentDiagnosticSeverity.Error,
            code,
            message,
            entityTemplateId,
            actionPlanTemplateId,
            actionPlanId,
            referencedActionPlanId,
            stepIndex,
            variableName,
            actionPlanSlot,
            expectedValueKind,
            actualValueKind,
            carriedEntityId,
            relatedEntityId,
            coord);
}
