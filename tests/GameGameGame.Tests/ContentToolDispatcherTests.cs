using GameGameGame.Content;
using GameGameGame.Content.Tools;
using GameGameGame.Core;
using System.Text.Json;

namespace GameGameGame.Tests;

public sealed class ContentToolDispatcherTests
{
    [Fact]
    public void ContentToolDispatcherKeepsSessionAcrossSemanticEditCalls()
    {
        var dispatcher = new ContentToolDispatcher(new ContentToolSessionRegistry());

        var create = dispatcher.Invoke(ContentToolNames.CreateNew, new ContentToolCreateNewRequest());
        Assert.True(create.Ok, create.Error?.Message);
        var sessionId = Assert.IsType<ContentToolSessionOpened>(create.Data).SessionId;

        var createEntity = dispatcher.Invoke(ContentToolNames.CreateEntityTemplate, new ContentToolCreateEntityTemplateRequest(sessionId, "Wanderer"));
        Assert.True(createEntity.Ok, createEntity.Error?.Message);
        var entityId = Assert.IsType<ContentToolCreatedEntityTemplate>(createEntity.Data).EntityTemplateId;

        var updateEntity = dispatcher.Invoke(ContentToolNames.UpdateEntityTemplate, new ContentToolUpdateEntityTemplateRequest(
            sessionId,
            entityId,
            new AgentEntityTemplateUpdate(InventoryWidth: 3, InventoryHeight: 2, Bulk: 1, Aperture: 2, Glyph: '@', Color: PresentationColor.Yellow)));

        Assert.True(updateEntity.Ok, updateEntity.Error?.Message);
        Assert.True(updateEntity.Summary?.IsDirty);
        Assert.NotEmpty(updateEntity.Summary?.YamlDiffLines ?? []);

        var list = dispatcher.Invoke(ContentToolNames.ListEntityTemplates, new ContentToolSessionRequest(sessionId));
        Assert.True(list.Ok, list.Error?.Message);
        var listed = Assert.IsAssignableFrom<IReadOnlyList<ContentToolEntityTemplateSummary>>(list.Data);
        var template = Assert.Single(listed);
        Assert.Equal(entityId, template.EntityTemplateId);
        Assert.Equal("Wanderer", template.Name);
        Assert.Equal('@', template.Glyph);
        Assert.Equal(PresentationColor.Yellow, template.Color);
        Assert.True(template.Diagnostics.Count == 0);
    }

    [Fact]
    public void ContentToolDispatcherCreatesBehaviorPlanAndPreviewWithValidationSummary()
    {
        var dispatcher = new ContentToolDispatcher(new ContentToolSessionRegistry());
        var sessionId = Assert.IsType<ContentToolSessionOpened>(
            dispatcher.Invoke(ContentToolNames.CreateNew, new ContentToolCreateNewRequest()).Data).SessionId;

        var createPlan = dispatcher.Invoke(ContentToolNames.CreateActionPlan, new ContentToolCreateActionPlanRequest(sessionId, "Walk"));
        Assert.True(createPlan.Ok, createPlan.Error?.Message);
        var planId = Assert.IsType<ContentToolCreatedActionPlan>(createPlan.Data).ActionPlanTemplateId;

        var setBehavior = dispatcher.Invoke(ContentToolNames.SetActionPlanBehavior, new ContentToolSetActionPlanBehaviorRequest(
            sessionId,
            planId,
            [ActionPlanBehaviorStepKind.MoveFacing]));

        Assert.True(setBehavior.Ok, setBehavior.Error?.Message);
        Assert.True(setBehavior.Summary?.IsDirty);

        var plans = dispatcher.Invoke(ContentToolNames.ListActionPlans, new ContentToolSessionRequest(sessionId));
        var plan = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ContentToolActionPlanSummary>>(plans.Data));
        Assert.Equal(planId, plan.ActionPlanTemplateId);
        Assert.Equal("walk", plan.Name);
        Assert.Equal([ActionPlanBehaviorStepKind.MoveFacing], plan.BehaviorSteps.Select(step => step.Kind));

        var preview = dispatcher.Invoke(ContentToolNames.PreviewActionPlan, new ContentToolPreviewActionPlanRequest(sessionId, planId));
        Assert.True(preview.Ok, preview.Error?.Message);
        var previewData = Assert.IsType<ActionPlanPreview>(preview.Data);
        Assert.Equal(planId, previewData.PlanId);
        Assert.Contains(previewData.ActionSteps, step => step.Kind == ActionPlanBehaviorStepKind.MoveFacing);
        Assert.NotNull(preview.Summary);
    }

    [Fact]
    public void ContentToolDispatcherPreviewAndRunScenarioOmitsRepeatedYamlPreviews()
    {
        var sessions = new ContentToolSessionRegistry();
        var dispatcher = new ContentToolDispatcher(sessions);
        var sessionId = Assert.IsType<ContentToolSessionOpened>(
            dispatcher.Invoke(ContentToolNames.CreateNew, new ContentToolCreateNewRequest()).Data).SessionId;
        var roomId = Assert.IsType<ContentToolCreatedEntityTemplate>(
            dispatcher.Invoke(ContentToolNames.CreateEntityTemplate, new ContentToolCreateEntityTemplateRequest(sessionId, "Tool Review Room")).Data).EntityTemplateId;
        Assert.True(dispatcher.Invoke(ContentToolNames.UpdateEntityTemplate, new ContentToolUpdateEntityTemplateRequest(
            sessionId,
            roomId,
            new AgentEntityTemplateUpdate(InventoryWidth: 3, InventoryHeight: 2, Bulk: 100, Aperture: 100, Glyph: '#', Color: PresentationColor.Gray))).Ok);
        var playerTemplateId = Assert.IsType<ContentToolCreatedEntityTemplate>(
            dispatcher.Invoke(ContentToolNames.CreateEntityTemplate, new ContentToolCreateEntityTemplateRequest(sessionId, "Tool Review Player")).Data).EntityTemplateId;
        Assert.True(dispatcher.Invoke(ContentToolNames.UpdateEntityTemplate, new ContentToolUpdateEntityTemplateRequest(
            sessionId,
            playerTemplateId,
            new AgentEntityTemplateUpdate(InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 5, Glyph: '@', Color: PresentationColor.Yellow))).Ok);
        var planId = Assert.IsType<ContentToolCreatedActionPlan>(
            dispatcher.Invoke(ContentToolNames.CreateActionPlan, new ContentToolCreateActionPlanRequest(sessionId, "Tool Review Move")).Data).ActionPlanTemplateId;
        Assert.True(dispatcher.Invoke(ContentToolNames.SetActionPlanBehavior, new ContentToolSetActionPlanBehaviorRequest(
            sessionId,
            planId,
            [ActionPlanBehaviorStepKind.MoveFacing])).Ok);
        var api = sessions.Get(sessionId).Value!;
        Assert.True(api.SetInitialFacing(playerTemplateId, Direction.East).IsSuccess);
        Assert.True(api.SetDefaultActionPlan(playerTemplateId, planId).IsSuccess);

        Assert.True(dispatcher.Invoke(ContentToolNames.UpsertScenario, new ContentToolUpsertScenarioRequest(
            sessionId,
            new AgentAlphaScenarioDefinition(
                "tool-review-run",
                "Tool Review Run",
                roomId,
                playerTemplateId,
                new EntityId("toolReviewPlayer"),
                new GridCoord(0, 1)))).Ok);

        var result = dispatcher.Invoke(ContentToolNames.PreviewAndRunScenarioById, new ContentToolRunScenarioByIdRequest(sessionId, "tool-review-run", 1));

        Assert.True(result.Ok, result.Error?.Message);
        var data = Assert.IsType<AgentScenarioPreviewRunReport>(result.Data);
        Assert.Single(data.ActionPlanPreviews, item => item.PlanId == planId);
        var serialized = JsonSerializer.Serialize(result, ContentToolJson.Options);
        Assert.DoesNotContain("yamlPreview", serialized, StringComparison.Ordinal);
        Assert.Contains("runReport", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void ContentToolDispatcherRejectsInvalidSessionWithRecoverableError()
    {
        var dispatcher = new ContentToolDispatcher(new ContentToolSessionRegistry());

        var result = dispatcher.Invoke(ContentToolNames.Validate, new ContentToolSessionRequest("missing-session"));

        Assert.False(result.Ok);
        Assert.Equal("InvalidSession", result.Error?.Code);
        Assert.True(result.Error?.Recoverable);
    }

    [Fact]
    public void ContentToolDispatcherAcceptsStringIdsFromToolJson()
    {
        var dispatcher = new ContentToolDispatcher(new ContentToolSessionRegistry());
        var sessionId = Assert.IsType<ContentToolSessionOpened>(
            dispatcher.Invoke(ContentToolNames.CreateNew, new ContentToolCreateNewRequest()).Data).SessionId;

        var planId = Assert.IsType<ContentToolCreatedActionPlan>(
            dispatcher.Invoke(ContentToolNames.CreateActionPlan, new ContentToolCreateActionPlanRequest(sessionId, "Walk")).Data).ActionPlanTemplateId;
        var args = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
        {
            sessionId,
            actionPlanTemplateId = planId.Value,
            steps = new[] { "MoveFacing" }
        }));

        var result = dispatcher.Invoke(ContentToolNames.SetActionPlanBehavior, args);

        Assert.True(result.Ok, result.Error?.Message);
    }
}
