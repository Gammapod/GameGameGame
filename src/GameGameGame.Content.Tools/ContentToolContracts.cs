using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Content.Tools;

public sealed record ContentToolResponse(bool Ok, object? Data, AgentApiError? Error, ContentToolMutationSummary? Summary)
{
    public static ContentToolResponse Success(object? data = null, ContentToolMutationSummary? summary = null) => new(true, data, null, summary);

    public static ContentToolResponse Failure(AgentApiError error, ContentToolMutationSummary? summary = null) => new(false, null, error, summary);
}

public sealed record ContentToolMutationSummary(
    string? FilePath,
    bool IsDirty,
    bool ValidationIsValid,
    bool CanonicalValidationIsValid,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> CanonicalValidationDiagnostics,
    IReadOnlyList<string> YamlDiffLines);

public interface IContentToolSessionRequest
{
    string SessionId { get; }
}

public sealed record ContentToolCreateNewRequest;
public sealed record ContentToolOpenFileRequest(string Path);
public sealed record ContentToolSaveAsRequest(string SessionId, string Path) : IContentToolSessionRequest;
public sealed record ContentToolSessionRequest(string SessionId) : IContentToolSessionRequest;
public sealed record ContentToolEntityTemplateRequest(string SessionId, EntityTemplateId EntityTemplateId) : IContentToolSessionRequest;
public sealed record ContentToolCreateEntityTemplateRequest(string SessionId, string Name) : IContentToolSessionRequest;
public sealed record ContentToolUpdateEntityTemplateRequest(string SessionId, EntityTemplateId EntityTemplateId, AgentEntityTemplateUpdate Update) : IContentToolSessionRequest;
public sealed record ContentToolPlaceCarriedEntityRequest(string SessionId, EntityTemplateId ParentTemplateId, EntityTemplateId CarriedTemplateId, GridCoord Coord) : IContentToolSessionRequest;
public sealed record ContentToolActionPlanRequest(string SessionId, ActionPlanTemplateId ActionPlanTemplateId) : IContentToolSessionRequest;
public sealed record ContentToolCreateActionPlanRequest(string SessionId, string Name) : IContentToolSessionRequest;
public sealed record ContentToolSetActionPlanBehaviorRequest(string SessionId, ActionPlanTemplateId ActionPlanTemplateId, IReadOnlyList<ActionPlanBehaviorStepKind> Steps) : IContentToolSessionRequest;
public sealed record ContentToolAddActionPlanBehaviorStepRequest(string SessionId, ActionPlanTemplateId ActionPlanTemplateId, ActionPlanBehaviorStepKind Kind) : IContentToolSessionRequest;
public sealed record ContentToolMoveActionPlanBehaviorStepRequest(string SessionId, ActionPlanTemplateId ActionPlanTemplateId, int FromIndex, int ToIndex) : IContentToolSessionRequest;
public sealed record ContentToolRemoveActionPlanBehaviorStepRequest(string SessionId, ActionPlanTemplateId ActionPlanTemplateId, int StepIndex) : IContentToolSessionRequest;
public sealed record ContentToolSetBehaviorStepTargetLabelRequest(string SessionId, ActionPlanTemplateId ActionPlanTemplateId, int StepIndex, string? TargetLabel) : IContentToolSessionRequest;
public sealed record ContentToolSetBehaviorStepTargetSlotRequest(string SessionId, ActionPlanTemplateId ActionPlanTemplateId, int StepIndex, int? TargetSlot) : IContentToolSessionRequest;
public sealed record ContentToolSetBehaviorStepPlanIdRequest(string SessionId, ActionPlanTemplateId ActionPlanTemplateId, int StepIndex, ActionPlanId? PlanId) : IContentToolSessionRequest;
public sealed record ContentToolSetBehaviorStepDirectionModeRequest(string SessionId, ActionPlanTemplateId ActionPlanTemplateId, int StepIndex, ActionPlanMoveDirectionMode? DirectionMode) : IContentToolSessionRequest;
public sealed record ContentToolPreviewActionPlanRequest(string SessionId, ActionPlanTemplateId ActionPlanTemplateId, EntityTemplateId? EntityTemplateId = null) : IContentToolSessionRequest;
public sealed record ContentToolScenarioRequest(string SessionId, string ScenarioId) : IContentToolSessionRequest;
public sealed record ContentToolUpsertScenarioRequest(string SessionId, AgentAlphaScenarioDefinition Scenario) : IContentToolSessionRequest;
public sealed record ContentToolRunScenarioByIdRequest(string SessionId, string ScenarioId, int TurnCount) : IContentToolSessionRequest;
public sealed record ContentToolRunScenarioPlayerLogByIdRequest(string SessionId, string ScenarioId, int TurnCount, EntityId? ObserverEntityId = null) : IContentToolSessionRequest;
public sealed record ContentToolScenarioManifestRequest(string Path);
public sealed record ContentToolScenarioManifestScanRequest(string FolderPath);
public sealed record ContentToolScenarioManifestValidateRequest(string Path, string FolderPath);

public sealed record ContentToolSessionOpened(string SessionId, string? FilePath, bool IsDirty);
public sealed record ContentToolCreatedEntityTemplate(EntityTemplateId EntityTemplateId);
public sealed record ContentToolCreatedActionPlan(ActionPlanTemplateId ActionPlanTemplateId);
public sealed record ContentToolPlacedCarriedEntity(EntityId EntityId);
public sealed record ContentToolEntityTemplateSummary(
    EntityTemplateId EntityTemplateId,
    string Name,
    int InventoryWidth,
    int InventoryHeight,
    int Bulk,
    int Aperture,
    char Glyph,
    PresentationColor Color,
    ActionPlanTemplateId? DefaultActionPlanId,
    IReadOnlyList<ContentDiagnostic> Diagnostics);

public sealed record ContentToolActionPlanSummary(
    ActionPlanTemplateId ActionPlanTemplateId,
    ActionPlanId ActionPlanId,
    string Name,
    string Shape,
    IReadOnlyList<ActionPlanBehaviorStepDescriptor> BehaviorSteps,
    IReadOnlyList<ContentDiagnostic> Diagnostics);

public sealed record ContentToolScenarioSummary(
    string ScenarioId,
    string Name,
    EntityTemplateId ScenarioRootEntityTemplateId,
    EntityTemplateId PlayerEntityTemplateId,
    EntityId PlayerEntityId,
    GridCoord PlayerStart,
    IReadOnlyDictionary<string, IReadOnlyList<EntityId>>? PlayerControls = null);

public sealed record ContentToolScenarioManifestValidationSummary(bool IsValid, IReadOnlyList<string> Diagnostics);
