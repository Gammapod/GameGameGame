using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Editor;

public sealed class AgentContentEditorApi(ContentEditorSession session)
{
    private static readonly HashSet<PlanEffectKind> SupportedAuthoringEffects =
    [
        PlanEffectKind.Teleport,
        PlanEffectKind.Move,
        PlanEffectKind.Pickup,
        PlanEffectKind.Drop,
        PlanEffectKind.ReverseDirection,
        PlanEffectKind.Wait,
        PlanEffectKind.CallPlan
    ];

    public ContentEditorSession Session { get; } = session;

    public static AgentContentEditorApi CreateNew() => new(ContentEditorSession.CreateNew());

    public static AgentApiResult<AgentContentEditorApi> OpenFile(string path)
    {
        var result = ContentEditorSession.OpenFile(path);

        return result.IsSuccess
            ? AgentApiResult<AgentContentEditorApi>.Success(new AgentContentEditorApi(result.Session!))
            : AgentApiResult<AgentContentEditorApi>.Failure(AgentApiError.FromMessage("OpenFileFailed", result.ErrorMessage));
    }

    public AgentApiResult Save() => FromFileOperation("SaveFailed", Session.Save());

    public AgentApiResult SaveAs(string path) => FromFileOperation("SaveAsFailed", Session.SaveAs(path));

    public AgentApiResult Reload() => FromFileOperation("ReloadFailed", Session.Reload());

    public AgentDocumentSnapshot GetDocumentSnapshot() =>
        new(
            Session.FilePath,
            Session.IsDirty,
            Session.GetYamlPreview(),
            Session.GetYamlDiff().Lines,
            Session.Editor.Validate(),
            Session.Document.ValidateCanonicalAuthoring());

    public AgentApiResult<ContentValidationResult> Validate() =>
        AgentApiResult<ContentValidationResult>.Success(Session.Editor.Validate());

    public AgentApiResult<ContentValidationResult> ValidateCanonicalAuthoring() =>
        AgentApiResult<ContentValidationResult>.Success(Session.Document.ValidateCanonicalAuthoring());

    public AgentApiResult<EntityTemplateId> CreateEntityTemplate(string name) =>
        Try("CreateEntityTemplateFailed", () => Session.Editor.CreateEntityPreset(name));

    public AgentApiResult UpdateEntityTemplate(EntityTemplateId id, AgentEntityTemplateUpdate update) =>
        Try("UpdateEntityTemplateFailed", () =>
        {
            var preset = Session.Editor.GetEntityPreset(id);
            var template = preset.Template with
            {
                Name = update.Name ?? preset.Template.Name,
                InventoryWidth = update.InventoryWidth ?? preset.Template.InventoryWidth,
                InventoryHeight = update.InventoryHeight ?? preset.Template.InventoryHeight,
                Weight = update.Weight ?? preset.Template.Weight,
                CarryingCapacity = update.CarryingCapacity ?? preset.Template.CarryingCapacity
            };
            var presentation = preset.Presentation with
            {
                Glyph = update.Glyph ?? preset.Presentation.Glyph,
                Color = update.Color ?? preset.Presentation.Color
            };

            Session.Editor.UpdateEntityPreset(id, template, presentation);
        });

    public AgentApiResult SetDefaultActionPlan(EntityTemplateId entityTemplateId, ActionPlanTemplateId actionPlanId) =>
        Try("SetDefaultActionPlanFailed", () => Session.Editor.SetDefaultActionPlan(entityTemplateId, actionPlanId));

    public AgentApiResult ClearDefaultActionPlan(EntityTemplateId entityTemplateId) =>
        Try("ClearDefaultActionPlanFailed", () => Session.Editor.ClearDefaultActionPlan(entityTemplateId));

    public AgentApiResult SetInitialFacing(EntityTemplateId entityTemplateId, Direction facing) =>
        Try("SetInitialFacingFailed", () => Session.Editor.SetInitialFacing(entityTemplateId, facing));

    public AgentApiResult ClearInitialFacing(EntityTemplateId entityTemplateId) =>
        Try("ClearInitialFacingFailed", () => Session.Editor.ClearInitialFacing(entityTemplateId));

    public AgentApiResult<EntityId> PlaceCarriedEntity(EntityTemplateId parentTemplateId, EntityTemplateId carriedTemplateId, GridCoord coord) =>
        Try("PlaceCarriedEntityFailed", () => Session.Editor.PlaceCarriedEntity(parentTemplateId, carriedTemplateId, coord));

    public AgentApiResult<EntityId> PlaceCarriedEntity(EntityTemplateId parentTemplateId, EntityId entityId, EntityTemplateId carriedTemplateId, GridCoord coord) =>
        Try("PlaceCarriedEntityFailed", () =>
        {
            Session.Editor.PlaceCarriedEntity(parentTemplateId, entityId, carriedTemplateId, coord);
            return entityId;
        });

    public AgentApiResult<ActionPlanTemplateId> CreateActionPlan(string name) =>
        Try("CreateActionPlanFailed", () => Session.Editor.CreateActionPlan(name));

    public AgentApiResult<IReadOnlyList<ActionStepDescriptor>> ListActionSteps() =>
        AgentApiResult<IReadOnlyList<ActionStepDescriptor>>.Success(Session.Editor.ListActionSteps());

    public AgentApiResult<ActionPlanPreview> PreviewActionPlan(ActionPlanTemplateId planId, EntityTemplateId? entityTemplateId = null) =>
        Try("PreviewActionPlanFailed", () => Session.Editor.PreviewActionPlan(planId, entityTemplateId));

    public AgentApiResult SetActionPlanPrimitive(ActionPlanTemplateId planId, ActionPlanPrimitiveKind kind, ActionPlanId? fallbackPlanId = null) =>
        Try("SetActionPlanPrimitiveFailed", () => Session.Editor.SetActionPlanPrimitive(planId, kind, fallbackPlanId));

    public AgentApiResult ClearActionPlanPrimitive(ActionPlanTemplateId planId) =>
        Try("ClearActionPlanPrimitiveFailed", () => Session.Editor.ClearActionPlanPrimitive(planId));

    public AgentApiResult<PrimitiveActionPlanChain> CreateMoveFacingPickupTargetChain(string moveFacingPlanName, string pickupTargetPlanName) =>
        Try("CreateMoveFacingPickupTargetChainFailed", () => Session.Editor.CreateMoveFacingPickupTargetChain(moveFacingPlanName, pickupTargetPlanName));

    public AgentApiResult<ActionPlanTemplateId> CreateMoveFacingPickupTargetBehavior(string behaviorPlanName) =>
        Try("CreateMoveFacingPickupTargetBehaviorFailed", () => Session.Editor.CreateMoveFacingPickupTargetBehavior(behaviorPlanName));

    public AgentApiResult SetActionPlanBehavior(ActionPlanTemplateId planId, IReadOnlyList<ActionPlanBehaviorStepKind> steps) =>
        Try("SetActionPlanBehaviorFailed", () => Session.Editor.SetActionPlanBehavior(planId, steps));

    public AgentApiResult ClearActionPlanBehavior(ActionPlanTemplateId planId) =>
        Try("ClearActionPlanBehaviorFailed", () => Session.Editor.ClearActionPlanBehavior(planId));

    public AgentApiResult AddActionPlanBehaviorStep(ActionPlanTemplateId planId, ActionPlanBehaviorStepKind kind) =>
        Try("AddActionPlanBehaviorStepFailed", () => Session.Editor.AddActionPlanBehaviorStep(planId, kind));

    public AgentApiResult MoveActionPlanBehaviorStep(ActionPlanTemplateId planId, int fromIndex, int toIndex) =>
        Try("MoveActionPlanBehaviorStepFailed", () => Session.Editor.MoveActionPlanBehaviorStep(planId, fromIndex, toIndex));

    public AgentApiResult RemoveActionPlanBehaviorStep(ActionPlanTemplateId planId, int stepIndex) =>
        Try("RemoveActionPlanBehaviorStepFailed", () => Session.Editor.RemoveActionPlanBehaviorStep(planId, stepIndex));

    public AgentApiResult AddActionPlanStep(ActionPlanTemplateId planId, AgentActionPlanStepRequest step) =>
        Try("AddActionPlanStepFailed", () => Session.Editor.AddActionPlanStep(planId, step.ToDescriptor()));

    public AgentApiResult UpdateActionPlanStep(ActionPlanTemplateId planId, int stepIndex, AgentActionPlanStepRequest step) =>
        Try("UpdateActionPlanStepFailed", () => Session.Editor.UpdateActionPlanStep(planId, stepIndex, step.ToDescriptor()));

    public AgentApiResult MoveActionPlanStep(ActionPlanTemplateId planId, int fromIndex, int toIndex) =>
        Try("MoveActionPlanStepFailed", () => Session.Editor.MoveActionPlanStep(planId, fromIndex, toIndex));

    public AgentApiResult RemoveActionPlanStep(ActionPlanTemplateId planId, int stepIndex) =>
        Try("RemoveActionPlanStepFailed", () => Session.Editor.RemoveActionPlanStep(planId, stepIndex));

    public AgentApiResult AddActionPlanCheck(ActionPlanTemplateId planId, int stepIndex, PlanCheckKind kind) =>
        Try("AddActionPlanCheckFailed", () => Session.Editor.AddActionPlanCheck(planId, stepIndex, kind));

    public AgentApiResult UpdateActionPlanCheck(ActionPlanTemplateId planId, int stepIndex, int checkIndex, PlanCheckKind kind) =>
        Try("UpdateActionPlanCheckFailed", () => Session.Editor.UpdateActionPlanCheck(planId, stepIndex, checkIndex, kind));

    public AgentApiResult SetActionPlanStepSuccessEffect(ActionPlanTemplateId planId, int stepIndex, PlanEffectDescriptor effect) =>
        SetActionPlanStepEffect(planId, stepIndex, effect, updateSuccess: true);

    public AgentApiResult SetActionPlanStepFailureEffect(ActionPlanTemplateId planId, int stepIndex, PlanEffectDescriptor effect) =>
        SetActionPlanStepEffect(planId, stepIndex, effect, updateSuccess: false);

    private AgentApiResult SetActionPlanStepEffect(ActionPlanTemplateId planId, int stepIndex, PlanEffectDescriptor effect, bool updateSuccess)
    {
        if (!SupportedAuthoringEffects.Contains(effect.Kind))
        {
            return AgentApiResult.Failure(new AgentApiError(
                "UnsupportedEffectForAuthoring",
                $"Effect {effect.Kind} is not supported by canonical agent authoring.",
                Recoverable: true));
        }

        return Try(updateSuccess ? "SetSuccessEffectFailed" : "SetFailureEffectFailed", () =>
        {
            if (updateSuccess)
            {
                Session.Editor.SetActionPlanStepSuccessEffect(planId, stepIndex, effect);
            }
            else
            {
                Session.Editor.SetActionPlanStepFailureEffect(planId, stepIndex, effect);
            }
        });
    }

    private static AgentApiResult FromFileOperation(string code, ContentEditorFileOperationResult result) =>
        result.IsSuccess
            ? AgentApiResult.Success()
            : AgentApiResult.Failure(AgentApiError.FromMessage(code, result.ErrorMessage));

    private static AgentApiResult Try(string code, Action operation)
    {
        try
        {
            operation();
            return AgentApiResult.Success();
        }
        catch (Exception ex)
        {
            return AgentApiResult.Failure(AgentApiError.FromException(code, ex));
        }
    }

    private static AgentApiResult<T> Try<T>(string code, Func<T> operation)
    {
        try
        {
            return AgentApiResult<T>.Success(operation());
        }
        catch (Exception ex)
        {
            return AgentApiResult<T>.Failure(AgentApiError.FromException(code, ex));
        }
    }
}

public sealed record AgentDocumentSnapshot(
    string? FilePath,
    bool IsDirty,
    string YamlPreview,
    IReadOnlyList<string> YamlDiffLines,
    ContentValidationResult Validation,
    ContentValidationResult CanonicalValidation);

public sealed record AgentEntityTemplateUpdate(
    string? Name = null,
    int? InventoryWidth = null,
    int? InventoryHeight = null,
    int? Weight = null,
    int? CarryingCapacity = null,
    char? Glyph = null,
    PresentationColor? Color = null);

public sealed record AgentActionPlanStepRequest(
    string Label,
    IReadOnlyList<PlanCheckDescriptor>? Checks = null,
    PlanEffectDescriptor? OnSuccess = null,
    PlanEffectDescriptor? OnFailure = null)
{
    public ActionPlanStepDescriptor ToDescriptor() =>
        new(Label, Checks ?? [], OnSuccess, OnFailure);
}

public sealed record AgentApiError(
    string Code,
    string Message,
    bool Recoverable = true,
    IReadOnlyList<string>? SuggestedActions = null)
{
    public static AgentApiError FromMessage(string code, string? message) =>
        new(code, string.IsNullOrWhiteSpace(message) ? "Operation failed." : message);

    public static AgentApiError FromException(string code, Exception exception) =>
        new(code, exception.Message);
}

public record AgentApiResult(AgentApiError? Error)
{
    public bool IsSuccess => Error is null;

    public static AgentApiResult Success() => new(Error: null);

    public static AgentApiResult Failure(AgentApiError error) => new(error);
}

public sealed record AgentApiResult<T>(T? Value, AgentApiError? Error) : AgentApiResult(Error)
{
    public static AgentApiResult<T> Success(T value) => new(value, Error: null);

    public new static AgentApiResult<T> Failure(AgentApiError error) => new(default, error);
}
