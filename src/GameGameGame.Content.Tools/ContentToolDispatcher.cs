using System.Text.Json;
using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Content.Tools;

public sealed class ContentToolDispatcher(ContentToolSessionRegistry sessions)
{
    private static readonly JsonSerializerOptions JsonOptions = ContentToolJson.Options;

    public ContentToolResponse Invoke(string toolName, JsonElement arguments) =>
        Invoke(toolName, Deserialize(toolName, arguments));

    public ContentToolResponse Invoke(string toolName, object request)
    {
        try
        {
            return toolName switch
            {
                ContentToolNames.CreateNew => CreateNew(),
                ContentToolNames.OpenFile => OpenFile((ContentToolOpenFileRequest)request),
                ContentToolNames.Snapshot => WithSession((ContentToolSessionRequest)request, api => ContentToolResponse.Success(api.GetDocumentSnapshot(), BuildSummary(api))),
                ContentToolNames.Validate => WithSession((ContentToolSessionRequest)request, api => FromAgentResult(api, api.Validate())),
                ContentToolNames.ValidateCanonicalAuthoring => WithSession((ContentToolSessionRequest)request, api => FromAgentResult(api, api.ValidateCanonicalAuthoring())),
                ContentToolNames.Save => WithSession((ContentToolSessionRequest)request, api => FromAgentResult(api, api.Save())),
                ContentToolNames.SaveAs => WithSession((ContentToolSaveAsRequest)request, api => FromAgentResult(api, api.SaveAs(((ContentToolSaveAsRequest)request).Path))),
                ContentToolNames.Close => Close((ContentToolSessionRequest)request),
                ContentToolNames.ListEntityTemplates => WithSession((ContentToolSessionRequest)request, api => ContentToolResponse.Success(ListEntityTemplates(api), BuildSummary(api))),
                ContentToolNames.GetEntityTemplate => WithSession((ContentToolEntityTemplateRequest)request, api => ContentToolResponse.Success(ListEntityTemplates(api).Single(template => template.EntityTemplateId == ((ContentToolEntityTemplateRequest)request).EntityTemplateId), BuildSummary(api))),
                ContentToolNames.CreateEntityTemplate => WithSession((ContentToolCreateEntityTemplateRequest)request, api => FromAgentResult(api, api.CreateEntityTemplate(((ContentToolCreateEntityTemplateRequest)request).Name), id => new ContentToolCreatedEntityTemplate(id))),
                ContentToolNames.UpdateEntityTemplate => WithSession((ContentToolUpdateEntityTemplateRequest)request, api => FromAgentResult(api, api.UpdateEntityTemplate(((ContentToolUpdateEntityTemplateRequest)request).EntityTemplateId, ((ContentToolUpdateEntityTemplateRequest)request).Update))),
                ContentToolNames.PlaceCarriedEntity => WithSession((ContentToolPlaceCarriedEntityRequest)request, api => FromAgentResult(api, api.PlaceCarriedEntity(((ContentToolPlaceCarriedEntityRequest)request).ParentTemplateId, ((ContentToolPlaceCarriedEntityRequest)request).CarriedTemplateId, ((ContentToolPlaceCarriedEntityRequest)request).Coord), id => new ContentToolPlacedCarriedEntity(id))),
                ContentToolNames.ListCarriedEntities => WithSession((ContentToolEntityTemplateRequest)request, api => ContentToolResponse.Success(api.Session.Editor.ListCarriedEntities(((ContentToolEntityTemplateRequest)request).EntityTemplateId), BuildSummary(api))),
                ContentToolNames.ListActionPlans => WithSession((ContentToolSessionRequest)request, api => ContentToolResponse.Success(ListActionPlans(api), BuildSummary(api))),
                ContentToolNames.GetActionPlan => WithSession((ContentToolActionPlanRequest)request, api => ContentToolResponse.Success(ListActionPlans(api).Single(plan => plan.ActionPlanTemplateId == ((ContentToolActionPlanRequest)request).ActionPlanTemplateId), BuildSummary(api))),
                ContentToolNames.CreateActionPlan => WithSession((ContentToolCreateActionPlanRequest)request, api => FromAgentResult(api, api.CreateActionPlan(((ContentToolCreateActionPlanRequest)request).Name), id => new ContentToolCreatedActionPlan(id))),
                ContentToolNames.SetActionPlanBehavior => WithSession((ContentToolSetActionPlanBehaviorRequest)request, api => FromAgentResult(api, api.SetActionPlanBehavior(((ContentToolSetActionPlanBehaviorRequest)request).ActionPlanTemplateId, ((ContentToolSetActionPlanBehaviorRequest)request).Steps))),
                ContentToolNames.AddActionPlanBehaviorStep => WithSession((ContentToolAddActionPlanBehaviorStepRequest)request, api => FromAgentResult(api, api.AddActionPlanBehaviorStep(((ContentToolAddActionPlanBehaviorStepRequest)request).ActionPlanTemplateId, ((ContentToolAddActionPlanBehaviorStepRequest)request).Kind))),
                ContentToolNames.MoveActionPlanBehaviorStep => WithSession((ContentToolMoveActionPlanBehaviorStepRequest)request, api => FromAgentResult(api, api.MoveActionPlanBehaviorStep(((ContentToolMoveActionPlanBehaviorStepRequest)request).ActionPlanTemplateId, ((ContentToolMoveActionPlanBehaviorStepRequest)request).FromIndex, ((ContentToolMoveActionPlanBehaviorStepRequest)request).ToIndex))),
                ContentToolNames.RemoveActionPlanBehaviorStep => WithSession((ContentToolRemoveActionPlanBehaviorStepRequest)request, api => FromAgentResult(api, api.RemoveActionPlanBehaviorStep(((ContentToolRemoveActionPlanBehaviorStepRequest)request).ActionPlanTemplateId, ((ContentToolRemoveActionPlanBehaviorStepRequest)request).StepIndex))),
                ContentToolNames.SetBehaviorStepTargetLabel => WithSession((ContentToolSetBehaviorStepTargetLabelRequest)request, api => FromAgentResult(api, api.SetActionPlanBehaviorStepTargetLabel(((ContentToolSetBehaviorStepTargetLabelRequest)request).ActionPlanTemplateId, ((ContentToolSetBehaviorStepTargetLabelRequest)request).StepIndex, ((ContentToolSetBehaviorStepTargetLabelRequest)request).TargetLabel))),
                ContentToolNames.SetBehaviorStepTargetSlot => WithSession((ContentToolSetBehaviorStepTargetSlotRequest)request, api => FromAgentResult(api, api.SetActionPlanBehaviorStepTargetSlot(((ContentToolSetBehaviorStepTargetSlotRequest)request).ActionPlanTemplateId, ((ContentToolSetBehaviorStepTargetSlotRequest)request).StepIndex, ((ContentToolSetBehaviorStepTargetSlotRequest)request).TargetSlot))),
                ContentToolNames.SetBehaviorStepPlanId => WithSession((ContentToolSetBehaviorStepPlanIdRequest)request, api => FromAgentResult(api, api.SetActionPlanBehaviorStepPlanId(((ContentToolSetBehaviorStepPlanIdRequest)request).ActionPlanTemplateId, ((ContentToolSetBehaviorStepPlanIdRequest)request).StepIndex, ((ContentToolSetBehaviorStepPlanIdRequest)request).PlanId))),
                ContentToolNames.ListActionSteps => WithSession((ContentToolSessionRequest)request, api => FromAgentResult(api, api.ListActionSteps())),
                ContentToolNames.PreviewActionPlan => WithSession((ContentToolPreviewActionPlanRequest)request, api => FromAgentResult(api, api.PreviewActionPlan(((ContentToolPreviewActionPlanRequest)request).ActionPlanTemplateId, ((ContentToolPreviewActionPlanRequest)request).EntityTemplateId))),
                ContentToolNames.ListScenarios => WithSession((ContentToolSessionRequest)request, api => ContentToolResponse.Success(ListScenarios(api), BuildSummary(api))),
                ContentToolNames.GetScenario => WithSession((ContentToolScenarioRequest)request, api => ContentToolResponse.Success(ListScenarios(api).Single(scenario => scenario.ScenarioId == ((ContentToolScenarioRequest)request).ScenarioId), BuildSummary(api))),
                ContentToolNames.UpsertScenario => WithSession((ContentToolUpsertScenarioRequest)request, api => FromAgentResult(api, api.UpsertScenario(((ContentToolUpsertScenarioRequest)request).Scenario))),
                ContentToolNames.MaterializeScenario => WithSession((ContentToolScenarioRequest)request, api => FromAgentResult(api, api.MaterializeScenario(((ContentToolScenarioRequest)request).ScenarioId))),
                ContentToolNames.RunScenarioById => WithSession((ContentToolRunScenarioByIdRequest)request, api => FromAgentResult(api, api.RunScenarioById(((ContentToolRunScenarioByIdRequest)request).ScenarioId, ((ContentToolRunScenarioByIdRequest)request).TurnCount))),
                ContentToolNames.RunScenarioPlayerLogById => WithSession((ContentToolRunScenarioPlayerLogByIdRequest)request, api => FromAgentResult(api, api.RunScenarioPlayerLogById(((ContentToolRunScenarioPlayerLogByIdRequest)request).ScenarioId, ((ContentToolRunScenarioPlayerLogByIdRequest)request).TurnCount, ((ContentToolRunScenarioPlayerLogByIdRequest)request).ObserverEntityId))),
                ContentToolNames.PreviewAndRunScenarioById => WithSession((ContentToolRunScenarioByIdRequest)request, api => FromAgentResult(api, api.PreviewAndRunScenarioById(((ContentToolRunScenarioByIdRequest)request).ScenarioId, ((ContentToolRunScenarioByIdRequest)request).TurnCount))),
                _ => ContentToolResponse.Failure(new AgentApiError("UnknownTool", $"Unknown content tool '{toolName}'.", Recoverable: true))
            };
        }
        catch (Exception ex)
        {
            return ContentToolResponse.Failure(AgentApiError.FromException("ToolDispatchFailed", ex));
        }
    }

    private ContentToolResponse CreateNew()
    {
        var opened = sessions.CreateNew();
        return ContentToolResponse.Success(opened);
    }

    private ContentToolResponse OpenFile(ContentToolOpenFileRequest request)
    {
        var result = sessions.OpenFile(request.Path);
        return result.IsSuccess ? ContentToolResponse.Success(result.Value) : ContentToolResponse.Failure(result.Error!);
    }

    private ContentToolResponse Close(ContentToolSessionRequest request) =>
        ContentToolResponse.Success(new { closed = sessions.Close(request.SessionId) });

    private ContentToolResponse WithSession(IContentToolSessionRequest request, Func<AgentContentEditorApi, ContentToolResponse> operation) =>
        WithSession(request.SessionId, operation);

    private ContentToolResponse WithSession(string sessionId, Func<AgentContentEditorApi, ContentToolResponse> operation)
    {
        var result = sessions.Get(sessionId);
        return result.IsSuccess ? operation(result.Value!) : ContentToolResponse.Failure(result.Error!);
    }

    private ContentToolResponse FromAgentResult(AgentContentEditorApi api, AgentApiResult result) =>
        result.IsSuccess ? ContentToolResponse.Success(summary: BuildSummary(api)) : ContentToolResponse.Failure(result.Error!, BuildSummary(api));

    private ContentToolResponse FromAgentResult<T>(AgentContentEditorApi api, AgentApiResult<T> result) =>
        result.IsSuccess ? ContentToolResponse.Success(result.Value, BuildSummary(api)) : ContentToolResponse.Failure(result.Error!, BuildSummary(api));

    private ContentToolResponse FromAgentResult<T, TMapped>(AgentContentEditorApi api, AgentApiResult<T> result, Func<T, TMapped> map) =>
        result.IsSuccess ? ContentToolResponse.Success(map(result.Value!), BuildSummary(api)) : ContentToolResponse.Failure(result.Error!, BuildSummary(api));

    private static ContentToolMutationSummary BuildSummary(AgentContentEditorApi api)
    {
        var snapshot = api.GetDocumentSnapshot();
        return new ContentToolMutationSummary(
            snapshot.FilePath,
            snapshot.IsDirty,
            snapshot.Validation.IsValid,
            snapshot.CanonicalValidation.IsValid,
            snapshot.Validation.Diagnostics.Select(diagnostic => diagnostic.Message).ToList(),
            snapshot.CanonicalValidation.Diagnostics.Select(diagnostic => diagnostic.Message).ToList(),
            snapshot.YamlDiffLines.Take(80).ToList());
    }

    private static IReadOnlyList<ContentToolEntityTemplateSummary> ListEntityTemplates(AgentContentEditorApi api)
    {
        var validation = api.Session.Editor.Validate();
        return api.Session.Editor.ListEntityPresets()
            .Select(preset => new ContentToolEntityTemplateSummary(
                preset.Id,
                preset.Template.Name,
                preset.Template.InventoryWidth,
                preset.Template.InventoryHeight,
                preset.Template.Bulk,
                preset.Template.Aperture,
                preset.Presentation.Glyph,
                preset.Presentation.Color,
                preset.Template.DefaultActionPlanId,
                validation.ForEntityTemplate(preset.Id)))
            .ToList();
    }

    private static IReadOnlyList<ContentToolActionPlanSummary> ListActionPlans(AgentContentEditorApi api)
    {
        var validation = api.Session.Editor.Validate();
        return api.Session.Editor.ListActionPlans()
            .Select(plan => new ContentToolActionPlanSummary(
                plan.TemplateId,
                plan.Descriptor.Id,
                plan.Descriptor.Id.Value,
                ContentEditorService.FormatActionPlanShape(ActionPlanShapeClassifier.Classify(plan.Descriptor)),
                plan.Descriptor.Behavior?.Steps ?? [],
                validation.ForActionPlan(plan.TemplateId)))
            .ToList();
    }

    private static IReadOnlyList<ContentToolScenarioSummary> ListScenarios(AgentContentEditorApi api) =>
        api.Session.Document.Scenarios
            .OrderBy(entry => entry.Key)
            .Select(entry => api.Session.Editor.GetScenario(entry.Key))
            .Select(scenario => new ContentToolScenarioSummary(
                scenario.ScenarioId,
                scenario.Name,
                scenario.ScenarioRootEntityTemplateId,
                scenario.PlayerEntityTemplateId,
                scenario.PlayerEntityId,
                scenario.PlayerStart))
            .ToList();

    private static object Deserialize(string toolName, JsonElement arguments) => toolName switch
    {
        ContentToolNames.CreateNew => new ContentToolCreateNewRequest(),
        ContentToolNames.OpenFile => arguments.Deserialize<ContentToolOpenFileRequest>(JsonOptions)!,
        ContentToolNames.Snapshot or ContentToolNames.Validate or ContentToolNames.ValidateCanonicalAuthoring or ContentToolNames.Save or ContentToolNames.Close or ContentToolNames.ListEntityTemplates or ContentToolNames.ListActionPlans or ContentToolNames.ListActionSteps or ContentToolNames.ListScenarios => arguments.Deserialize<ContentToolSessionRequest>(JsonOptions)!,
        ContentToolNames.SaveAs => arguments.Deserialize<ContentToolSaveAsRequest>(JsonOptions)!,
        ContentToolNames.GetEntityTemplate or ContentToolNames.ListCarriedEntities => arguments.Deserialize<ContentToolEntityTemplateRequest>(JsonOptions)!,
        ContentToolNames.CreateEntityTemplate => arguments.Deserialize<ContentToolCreateEntityTemplateRequest>(JsonOptions)!,
        ContentToolNames.UpdateEntityTemplate => arguments.Deserialize<ContentToolUpdateEntityTemplateRequest>(JsonOptions)!,
        ContentToolNames.PlaceCarriedEntity => arguments.Deserialize<ContentToolPlaceCarriedEntityRequest>(JsonOptions)!,
        ContentToolNames.GetActionPlan => arguments.Deserialize<ContentToolActionPlanRequest>(JsonOptions)!,
        ContentToolNames.CreateActionPlan => arguments.Deserialize<ContentToolCreateActionPlanRequest>(JsonOptions)!,
        ContentToolNames.SetActionPlanBehavior => arguments.Deserialize<ContentToolSetActionPlanBehaviorRequest>(JsonOptions)!,
        ContentToolNames.AddActionPlanBehaviorStep => arguments.Deserialize<ContentToolAddActionPlanBehaviorStepRequest>(JsonOptions)!,
        ContentToolNames.MoveActionPlanBehaviorStep => arguments.Deserialize<ContentToolMoveActionPlanBehaviorStepRequest>(JsonOptions)!,
        ContentToolNames.RemoveActionPlanBehaviorStep => arguments.Deserialize<ContentToolRemoveActionPlanBehaviorStepRequest>(JsonOptions)!,
        ContentToolNames.SetBehaviorStepTargetLabel => arguments.Deserialize<ContentToolSetBehaviorStepTargetLabelRequest>(JsonOptions)!,
        ContentToolNames.SetBehaviorStepTargetSlot => arguments.Deserialize<ContentToolSetBehaviorStepTargetSlotRequest>(JsonOptions)!,
        ContentToolNames.SetBehaviorStepPlanId => arguments.Deserialize<ContentToolSetBehaviorStepPlanIdRequest>(JsonOptions)!,
        ContentToolNames.PreviewActionPlan => arguments.Deserialize<ContentToolPreviewActionPlanRequest>(JsonOptions)!,
        ContentToolNames.GetScenario or ContentToolNames.MaterializeScenario => arguments.Deserialize<ContentToolScenarioRequest>(JsonOptions)!,
        ContentToolNames.UpsertScenario => arguments.Deserialize<ContentToolUpsertScenarioRequest>(JsonOptions)!,
        ContentToolNames.RunScenarioById or ContentToolNames.PreviewAndRunScenarioById => arguments.Deserialize<ContentToolRunScenarioByIdRequest>(JsonOptions)!,
        ContentToolNames.RunScenarioPlayerLogById => arguments.Deserialize<ContentToolRunScenarioPlayerLogByIdRequest>(JsonOptions)!,
        _ => new { }
    };
}
