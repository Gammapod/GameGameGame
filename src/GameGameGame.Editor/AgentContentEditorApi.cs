using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Headless;

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

    public AgentApiResult<AgentScenarioRunReport> RunScenario(AgentScenarioRunRequest request) =>
        Try("RunScenarioFailed", () => ToAgentReport(ScenarioRunService.Run(
            Session.Document,
            new ScenarioRunRequest(request.ScenarioRootEntityTemplateId, request.TurnCount))));

    public AgentApiResult<AgentScenarioRunReport> RunScenarioById(string scenarioId, int turnCount) =>
        Try("RunScenarioByIdFailed", () => ToAgentReport(ScenarioRunService.Run(
            Session.Document,
            new PersistedScenarioRunRequest(scenarioId, turnCount))));

    public AgentApiResult<AgentScenarioRecordingReport> RecordScenario(AgentScenarioRecordingRequest request) =>
        Try("RecordScenarioFailed", () => ToAgentReport(ScenarioRecordingService.Record(
            Session.Document,
            new ScenarioRecordingRequest(request.ScenarioId, request.TurnCount, request.OutputDirectory))));

    public AgentApiResult<AgentScenarioMaterializationReport> MaterializeScenario(AgentAlphaScenarioDefinition definition) =>
        Try("MaterializeScenarioFailed", () => AlphaScenarioMaterializer.Materialize(Session, definition).ToAgentReport());

    public AgentApiResult<AgentScenarioMaterializationReport> MaterializeScenario(string scenarioId) =>
        Try("MaterializeScenarioFailed", () => AlphaScenarioMaterializer.Materialize(Session, ToAgentDefinition(Session.Editor.GetScenario(scenarioId))).ToAgentReport());

    public AgentApiResult UpsertScenario(AgentAlphaScenarioDefinition definition) =>
        Try("UpsertScenarioFailed", () => Session.Editor.UpsertScenario(ToContentDefinition(definition)));

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
                Bulk = update.Bulk ?? preset.Template.Bulk,
                Aperture = update.Aperture ?? preset.Template.Aperture
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

    public AgentApiResult<IReadOnlyList<EntityTargetingRule>> ListTargetingRules(EntityTemplateId entityTemplateId) =>
        Try("ListTargetingRulesFailed", () => Session.Editor.ListTargetingRules(entityTemplateId));

    public AgentApiResult SetTargetingRule(EntityTemplateId entityTemplateId, EntityTargetingRule rule) =>
        Try("SetTargetingRuleFailed", () => Session.Editor.SetTargetingRule(entityTemplateId, rule));

    public AgentApiResult RemoveTargetingRule(EntityTemplateId entityTemplateId, int slot) =>
        Try("RemoveTargetingRuleFailed", () => Session.Editor.RemoveTargetingRule(entityTemplateId, slot));

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

    public AgentApiResult<AgentScenarioPreviewRunReport> PreviewAndRunScenarioById(string scenarioId, int turnCount) =>
        Try("PreviewAndRunScenarioByIdFailed", () =>
        {
            var validation = Session.Editor.Validate();
            var canonicalValidation = Session.Document.ValidateCanonicalAuthoring();
            var scenario = Session.Editor.GetScenario(scenarioId);
            var materialization = AlphaScenarioMaterializer.Materialize(Session, ToAgentDefinition(scenario)).ToAgentReport();
            var previews = Session.Editor.ListActionPlans()
                .Select(plan => Session.Editor.PreviewActionPlan(
                    plan.TemplateId,
                    Session.Editor.ListActionPlanReferences(plan.TemplateId)
                        .FirstOrDefault(reference => reference.EntityTemplateId is not null)
                        ?.EntityTemplateId))
                .ToList();
            var runReport = ToAgentReport(ScenarioRunService.Run(
                Session.Document,
                new PersistedScenarioRunRequest(scenarioId, turnCount)));

            return new AgentScenarioPreviewRunReport(
                scenarioId,
                validation,
                canonicalValidation,
                previews,
                materialization,
                runReport);
        });

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

    public AgentApiResult SetActionPlanBehaviorStepTargetSlot(ActionPlanTemplateId planId, int stepIndex, int? targetSlot) =>
        Try("SetActionPlanBehaviorStepTargetSlotFailed", () => Session.Editor.SetActionPlanBehaviorStepTargetSlot(planId, stepIndex, targetSlot));

    public AgentApiResult SetActionPlanBehaviorStepPlanId(ActionPlanTemplateId planId, int stepIndex, ActionPlanId? referencedPlanId) =>
        Try("SetActionPlanBehaviorStepPlanIdFailed", () => Session.Editor.SetActionPlanBehaviorStepPlanId(planId, stepIndex, referencedPlanId));

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

    private static ScenarioDefinition ToContentDefinition(AgentAlphaScenarioDefinition definition) =>
        new(
            definition.ScenarioId,
            definition.Name,
            definition.ScenarioRootEntityTemplateId,
            definition.PlayerEntityTemplateId,
            definition.PlayerEntityId,
            definition.PlayerStart);

    private static AgentAlphaScenarioDefinition ToAgentDefinition(ScenarioDefinition definition) =>
        new(
            definition.ScenarioId,
            definition.Name,
            definition.ScenarioRootEntityTemplateId,
            definition.PlayerEntityTemplateId,
            definition.PlayerEntityId,
            definition.PlayerStart);

    private static AgentScenarioRunReport ToAgentReport(ScenarioRunReport report) =>
        new(
            report.ScenarioRootEntityTemplateId,
            report.ScenarioRootEntityId,
            report.ScenarioPlaneId,
            report.ActorOrder
                .Select(actor => new AgentScenarioActorSummary(actor.EntityId, actor.Name, actor.Location))
                .ToList(),
            report.Turns
                .Select(turn => new AgentScenarioTurnReport(turn.TurnNumber, turn.InitiativeIndex, turn.ActorId, turn.ActorName, turn.TraceLines))
                .ToList(),
            report.SetupLines,
            report.FinalStateLines,
            report.InventorySummaryLines,
            report.ValidationDiagnostics,
            report.RuntimeObservations,
            report.RuntimeFailures,
            report.CapabilityGaps);

    private static AgentScenarioRecordingReport ToAgentReport(ScenarioRecordingReport report) =>
        new(
            report.ScenarioId,
            report.Name,
            report.ScenarioPlaneId,
            report.PlayerEntityId,
            report.Frames
                .Select(frame => new AgentScenarioRecordingFrame(frame.FrameIndex, frame.TurnNumber, frame.PngPath))
                .ToList(),
            report.GifPath,
            report.ValidationDiagnostics,
            report.RuntimeObservations,
            report.RuntimeFailures,
            report.CapabilityGaps);

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
    int? Bulk = null,
    int? Aperture = null,
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

public sealed record AgentScenarioRunRequest(
    EntityTemplateId ScenarioRootEntityTemplateId,
    int TurnCount);

public sealed record AgentScenarioRecordingRequest(
    string ScenarioId,
    int TurnCount,
    string OutputDirectory);

public sealed record AgentScenarioRecordingFrame(
    int FrameIndex,
    int TurnNumber,
    string PngPath);

public sealed record AgentScenarioRecordingReport(
    string ScenarioId,
    string Name,
    PlaneId ScenarioPlaneId,
    EntityId? PlayerEntityId,
    IReadOnlyList<AgentScenarioRecordingFrame> Frames,
    string? GifPath,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeObservations,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps);

public sealed record AgentAlphaScenarioDefinition(
    string ScenarioId,
    string Name,
    EntityTemplateId ScenarioRootEntityTemplateId,
    EntityTemplateId PlayerEntityTemplateId,
    EntityId PlayerEntityId,
    GridCoord PlayerStart);

public sealed record AgentScenarioMaterializationReport(
    string ScenarioId,
    string Name,
    EntityTemplateId ScenarioRootEntityTemplateId,
    EntityId ScenarioRootEntityId,
    EntityTemplateId? PlayerEntityTemplateId,
    EntityId? PlayerEntityId,
    PlaneId ScenarioPlaneId,
    PlaneCoord? PlayerLocation,
    IReadOnlyList<string> SetupLines,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps);

public sealed record AgentScenarioActorSummary(
    EntityId EntityId,
    string Name,
    PlaneCoord Location);

public sealed record AgentScenarioTurnReport(
    int TurnNumber,
    int InitiativeIndex,
    EntityId ActorId,
    string ActorName,
    IReadOnlyList<string> TraceLines);

public sealed record AgentScenarioRunReport(
    EntityTemplateId ScenarioRootEntityTemplateId,
    EntityId ScenarioRootEntityId,
    PlaneId ScenarioPlaneId,
    IReadOnlyList<AgentScenarioActorSummary> ActorOrder,
    IReadOnlyList<AgentScenarioTurnReport> Turns,
    IReadOnlyList<string> SetupLines,
    IReadOnlyList<string> FinalStateLines,
    IReadOnlyList<string> InventorySummaryLines,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeObservations,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps);

public sealed record AgentScenarioPreviewRunReport(
    string ScenarioId,
    ContentValidationResult DocumentValidation,
    ContentValidationResult CanonicalValidation,
    IReadOnlyList<ActionPlanPreview> ActionPlanPreviews,
    AgentScenarioMaterializationReport Materialization,
    AgentScenarioRunReport RunReport);

public sealed record AlphaScenarioMaterializationResult(
    string ScenarioId,
    string Name,
    EntityTemplateId ScenarioRootEntityTemplateId,
    EntityId ScenarioRootEntityId,
    EntityTemplateId? PlayerEntityTemplateId,
    EntityId? PlayerEntityId,
    PlaneId? ScenarioPlaneId,
    PlaneCoord? PlayerLocation,
    WorldState World,
    IReadOnlyDictionary<EntityId, IEntityActionPlan> ActionPlans,
    PrototypeContentRegistry? Registry,
    IReadOnlyList<string> SetupLines,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps)
{
    public bool CanSimulate => ValidationDiagnostics.Count == 0 && RuntimeFailures.Count == 0 && ScenarioPlaneId is not null;

    public AgentScenarioMaterializationReport ToAgentReport() =>
        new(
            ScenarioId,
            Name,
            ScenarioRootEntityTemplateId,
            ScenarioRootEntityId,
            PlayerEntityTemplateId,
            PlayerEntityId,
            ScenarioPlaneId ?? AlphaScenarioMaterializer.DefaultScenarioPlaneId,
            PlayerLocation,
            SetupLines,
            ValidationDiagnostics,
            RuntimeFailures,
            CapabilityGaps);
}

public static class AlphaScenarioMaterializer
{
    public static readonly EntityId DefaultScenarioRootEntityId = ScenarioMaterializer.DefaultScenarioRootEntityId;
    public static readonly PlaneId DefaultScenarioHostPlaneId = ScenarioMaterializer.DefaultScenarioHostPlaneId;
    public static readonly PlaneId DefaultScenarioPlaneId = ScenarioMaterializer.DefaultScenarioPlaneId;

    public static AlphaScenarioMaterializationResult Materialize(ContentEditorSession session, AgentAlphaScenarioDefinition definition) =>
        FromContentResult(ScenarioMaterializer.Materialize(
            session.Document,
            new ScenarioDefinition(
                definition.ScenarioId,
                definition.Name,
                definition.ScenarioRootEntityTemplateId,
                definition.PlayerEntityTemplateId,
                definition.PlayerEntityId,
                definition.PlayerStart)));

    internal static AlphaScenarioMaterializationResult MaterializeRootOnly(
        ContentEditorSession session,
        string scenarioId,
        string name,
        EntityTemplateId scenarioRootEntityTemplateId,
        EntityId scenarioRootEntityId,
        PlaneId scenarioPlaneId) =>
        FromContentResult(ScenarioMaterializer.MaterializeRootOnly(
            session.Document,
            scenarioId,
            name,
            scenarioRootEntityTemplateId,
            scenarioRootEntityId,
            scenarioPlaneId));

    private static AlphaScenarioMaterializationResult FromContentResult(ScenarioMaterializationResult result) =>
        new(
            result.ScenarioId,
            result.Name,
            result.ScenarioRootEntityTemplateId,
            result.ScenarioRootEntityId,
            result.PlayerEntityTemplateId,
            result.PlayerEntityId,
            result.ScenarioPlaneId,
            result.PlayerLocation,
            result.World,
            result.ActionPlans,
            result.Registry,
            result.SetupLines,
            result.ValidationDiagnostics,
            result.RuntimeFailures,
            result.CapabilityGaps);
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
