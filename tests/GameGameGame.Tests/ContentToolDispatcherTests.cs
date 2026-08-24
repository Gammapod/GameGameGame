using GameGameGame.Content;
using GameGameGame.Content.Tools;
using GameGameGame.Core;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GameGameGame.Tests;

public sealed class ContentToolDispatcherTests
{
    [Fact]
    public void ContentToolCatalogExposesAgentDiscoveryTools()
    {
        Assert.Contains("ggg_content_get_authoring_guide", ContentToolCatalog.Names);
        Assert.Contains("ggg_content_describe_schema", ContentToolCatalog.Names);
        Assert.Contains("ggg_content_list_workflows", ContentToolCatalog.Names);
        Assert.Contains("ggg_content_list_examples", ContentToolCatalog.Names);

        var describeSchema = ContentToolCatalog.InputSchema("ggg_content_describe_schema");
        Assert.Equal("string", describeSchema["properties"]!["concept"]!["type"]!.GetValue<string>());
        var conceptEnum = Assert.IsType<JsonArray>(describeSchema["properties"]!["concept"]!["enum"]);
        Assert.Contains(conceptEnum, value => value?.GetValue<string>() == "entityTemplateUpdate");
        Assert.Contains(conceptEnum, value => value?.GetValue<string>() == "scenario");
        Assert.Contains(conceptEnum, value => value?.GetValue<string>() == "behaviorStep");
    }

    [Fact]
    public void ContentToolCatalogDescribesOpaqueSchemasWithEnumsAndObjectFields()
    {
        var updateSchema = ContentToolCatalog.InputSchema(ContentToolNames.UpdateEntityTemplate);
        var update = updateSchema["properties"]!["update"]!;
        Assert.NotNull(update["properties"]!["name"]);
        Assert.NotNull(update["properties"]!["inventoryWidth"]);
        Assert.NotNull(update["properties"]!["enterPolicy"]!["enum"]);
        Assert.NotNull(update["properties"]!["topologyPolicy"]!["enum"]);
        Assert.Equal("Set nullable fields to null to clear them where listed.", update["description"]!.GetValue<string>());

        var placeSchema = ContentToolCatalog.InputSchema(ContentToolNames.PlaceCarriedEntity);
        Assert.Equal("integer", placeSchema["properties"]!["coord"]!["properties"]!["x"]!["type"]!.GetValue<string>());
        Assert.Equal("integer", placeSchema["properties"]!["coord"]!["properties"]!["y"]!["type"]!.GetValue<string>());

        var costsSchema = ContentToolCatalog.InputSchema(ContentToolNames.SetBehaviorStepCosts);
        var costItem = costsSchema["properties"]!["costs"]!["items"]!;
        Assert.Equal("object", costItem["type"]!.GetValue<string>());
        Assert.NotNull(costItem["properties"]!["templateId"]);
        Assert.NotNull(costItem["properties"]!["quantity"]);

        var addStepSchema = ContentToolCatalog.InputSchema(ContentToolNames.AddActionPlanBehaviorStep);
        var kindEnum = Assert.IsType<JsonArray>(addStepSchema["properties"]!["kind"]!["enum"]);
        Assert.Contains(kindEnum, value => value?.GetValue<string>() == nameof(ActionPlanBehaviorStepKind.Move));
        Assert.Contains(kindEnum, value => value?.GetValue<string>() == nameof(ActionPlanBehaviorStepKind.TargetPathMove));
    }

    [Fact]
    public void ContentToolDispatcherReturnsAuthoringGuideAndSchemaHelp()
    {
        var dispatcher = new ContentToolDispatcher(new ContentToolSessionRegistry());

        var guide = dispatcher.Invoke("ggg_content_get_authoring_guide", new { });
        var schema = dispatcher.Invoke("ggg_content_describe_schema", new { concept = "scenario" });
        var workflows = dispatcher.Invoke("ggg_content_list_workflows", new { });
        var examples = dispatcher.Invoke("ggg_content_list_examples", new { });

        Assert.True(guide.Ok, guide.Error?.Message);
        var guideData = Assert.IsType<ContentToolAuthoringGuide>(guide.Data);
        Assert.Contains("open/create", guideData.StartHere, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(guideData.AuthoritativeDocs, path => path.EndsWith("Content-Authoring-Manual.md", StringComparison.Ordinal));

        Assert.True(schema.Ok, schema.Error?.Message);
        var schemaData = Assert.IsType<ContentToolSchemaDescription>(schema.Data);
        Assert.Equal("scenario", schemaData.Concept);
        Assert.Contains(schemaData.Fields, field => field.Name == "scenarioRootEntityTemplateId" && field.Required);
        Assert.Contains(schemaData.Examples, example => example.Contains("scenarioId", StringComparison.Ordinal));

        Assert.True(workflows.Ok, workflows.Error?.Message);
        Assert.Contains(Assert.IsAssignableFrom<IReadOnlyList<ContentToolWorkflowRecipe>>(workflows.Data), workflow => workflow.Id == "safe-save-loop");

        Assert.True(examples.Ok, examples.Error?.Message);
        Assert.Contains(Assert.IsAssignableFrom<IReadOnlyList<ContentToolExampleReference>>(examples.Data), example => example.Purpose.Contains("scenario", StringComparison.OrdinalIgnoreCase));
    }

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
    public void ContentToolDispatcherSetsCanonicalMoveDirectionMode()
    {
        var dispatcher = new ContentToolDispatcher(new ContentToolSessionRegistry());
        var sessionId = Assert.IsType<ContentToolSessionOpened>(
            dispatcher.Invoke(ContentToolNames.CreateNew, new ContentToolCreateNewRequest()).Data).SessionId;
        var planId = Assert.IsType<ContentToolCreatedActionPlan>(
            dispatcher.Invoke(ContentToolNames.CreateActionPlan, new ContentToolCreateActionPlanRequest(sessionId, "Canonical Move")).Data).ActionPlanTemplateId;

        var add = dispatcher.Invoke(ContentToolNames.AddActionPlanBehaviorStep, new ContentToolAddActionPlanBehaviorStepRequest(sessionId, planId, ActionPlanBehaviorStepKind.Move));
        var setMode = dispatcher.Invoke(ContentToolNames.SetBehaviorStepDirectionMode, new ContentToolSetBehaviorStepDirectionModeRequest(sessionId, planId, 0, ActionPlanMoveDirectionMode.ForwardLeft));
        var plans = dispatcher.Invoke(ContentToolNames.ListActionPlans, new ContentToolSessionRequest(sessionId));

        Assert.True(add.Ok, add.Error?.Message);
        Assert.True(setMode.Ok, setMode.Error?.Message);
        var plan = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ContentToolActionPlanSummary>>(plans.Data));
        var step = Assert.Single(plan.BehaviorSteps);
        Assert.Equal(ActionPlanBehaviorStepKind.Move, step.Kind);
        Assert.Equal(ActionPlanMoveDirectionMode.ForwardLeft, step.DirectionMode);
    }

    [Fact]
    public void ContentToolDispatcherAuthorsBehaviorStepCosts()
    {
        var dispatcher = new ContentToolDispatcher(new ContentToolSessionRegistry());
        var sessionId = Assert.IsType<ContentToolSessionOpened>(
            dispatcher.Invoke(ContentToolNames.CreateNew, new ContentToolCreateNewRequest()).Data).SessionId;
        var scrapId = Assert.IsType<ContentToolCreatedEntityTemplate>(
            dispatcher.Invoke(ContentToolNames.CreateEntityTemplate, new ContentToolCreateEntityTemplateRequest(sessionId, "Scrap")).Data).EntityTemplateId;
        var planId = Assert.IsType<ContentToolCreatedActionPlan>(
            dispatcher.Invoke(ContentToolNames.CreateActionPlan, new ContentToolCreateActionPlanRequest(sessionId, "Costly Move")).Data).ActionPlanTemplateId;

        Assert.True(dispatcher.Invoke(ContentToolNames.AddActionPlanBehaviorStep, new ContentToolAddActionPlanBehaviorStepRequest(sessionId, planId, ActionPlanBehaviorStepKind.MoveFacing)).Ok);
        var setCosts = dispatcher.Invoke(ContentToolNames.SetBehaviorStepCosts, new ContentToolSetBehaviorStepCostsRequest(
            sessionId,
            planId,
            0,
            [new ActionStepCostDescriptor(scrapId.Value, 3)]));
        var preview = dispatcher.Invoke(ContentToolNames.PreviewActionPlan, new ContentToolPreviewActionPlanRequest(sessionId, planId));

        Assert.True(setCosts.Ok, setCosts.Error?.Message);
        var previewData = Assert.IsType<ActionPlanPreview>(preview.Data);
        Assert.Equal("Cost: 3× Scrap", Assert.Single(previewData.ActionSteps).CostSummary);
    }

    [Fact]
    public void ContentToolDispatcherAuthorsTargetPathMoveFields()
    {
        var dispatcher = new ContentToolDispatcher(new ContentToolSessionRegistry());
        var sessionId = Assert.IsType<ContentToolSessionOpened>(
            dispatcher.Invoke(ContentToolNames.CreateNew, new ContentToolCreateNewRequest()).Data).SessionId;
        var planId = Assert.IsType<ContentToolCreatedActionPlan>(
            dispatcher.Invoke(ContentToolNames.CreateActionPlan, new ContentToolCreateActionPlanRequest(sessionId, "Orbit Target")).Data).ActionPlanTemplateId;

        Assert.True(dispatcher.Invoke(ContentToolNames.AddActionPlanBehaviorStep, new ContentToolAddActionPlanBehaviorStepRequest(sessionId, planId, ActionPlanBehaviorStepKind.TargetPathMove)).Ok);
        Assert.True(dispatcher.Invoke(ContentToolNames.SetBehaviorStepTargetPathMode, new ContentToolSetBehaviorStepTargetPathModeRequest(sessionId, planId, 0, ActionPlanTargetPathMode.Orbit)).Ok);
        Assert.True(dispatcher.Invoke(ContentToolNames.SetBehaviorStepDesiredDistance, new ContentToolSetBehaviorStepDesiredDistanceRequest(sessionId, planId, 0, 6)).Ok);
        Assert.True(dispatcher.Invoke(ContentToolNames.SetBehaviorStepOrbitDirection, new ContentToolSetBehaviorStepOrbitDirectionRequest(sessionId, planId, 0, ActionPlanOrbitDirection.Clockwise)).Ok);
        var preview = dispatcher.Invoke(ContentToolNames.PreviewActionPlan, new ContentToolPreviewActionPlanRequest(sessionId, planId));

        Assert.True(preview.Ok, preview.Error?.Message);
        var step = Assert.Single(Assert.IsType<ActionPlanPreview>(preview.Data).ActionSteps);
        Assert.Equal("Path: Orbit; desiredDistance=6; orbitDirection=Clockwise", step.TargetPathSummary);
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
    public void ContentToolDispatcherRunsScenarioPlayerNarrativeLogById()
    {
        var sessions = new ContentToolSessionRegistry();
        var dispatcher = new ContentToolDispatcher(sessions);
        var sessionId = Assert.IsType<ContentToolSessionOpened>(
            dispatcher.Invoke(ContentToolNames.CreateNew, new ContentToolCreateNewRequest()).Data).SessionId;
        var roomId = Assert.IsType<ContentToolCreatedEntityTemplate>(
            dispatcher.Invoke(ContentToolNames.CreateEntityTemplate, new ContentToolCreateEntityTemplateRequest(sessionId, "Tool Log Room")).Data).EntityTemplateId;
        Assert.True(dispatcher.Invoke(ContentToolNames.UpdateEntityTemplate, new ContentToolUpdateEntityTemplateRequest(
            sessionId,
            roomId,
            new AgentEntityTemplateUpdate(InventoryWidth: 3, InventoryHeight: 2, Bulk: 100, Aperture: 100, Glyph: '#', Color: PresentationColor.Gray))).Ok);
        var playerTemplateId = Assert.IsType<ContentToolCreatedEntityTemplate>(
            dispatcher.Invoke(ContentToolNames.CreateEntityTemplate, new ContentToolCreateEntityTemplateRequest(sessionId, "Tool Log Player")).Data).EntityTemplateId;
        Assert.True(dispatcher.Invoke(ContentToolNames.UpdateEntityTemplate, new ContentToolUpdateEntityTemplateRequest(
            sessionId,
            playerTemplateId,
            new AgentEntityTemplateUpdate(InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 5, Glyph: '@', Color: PresentationColor.Yellow))).Ok);
        var planId = Assert.IsType<ContentToolCreatedActionPlan>(
            dispatcher.Invoke(ContentToolNames.CreateActionPlan, new ContentToolCreateActionPlanRequest(sessionId, "Tool Log Move")).Data).ActionPlanTemplateId;
        Assert.True(dispatcher.Invoke(ContentToolNames.SetActionPlanBehavior, new ContentToolSetActionPlanBehaviorRequest(sessionId, planId, [ActionPlanBehaviorStepKind.MoveFacing])).Ok);
        var api = sessions.Get(sessionId).Value!;
        Assert.True(api.SetInitialFacing(playerTemplateId, Direction.East).IsSuccess);
        Assert.True(api.SetDefaultActionPlan(playerTemplateId, planId).IsSuccess);
        Assert.True(dispatcher.Invoke(ContentToolNames.UpsertScenario, new ContentToolUpsertScenarioRequest(
            sessionId,
            new AgentAlphaScenarioDefinition("tool-player-log-run", "Tool Player Log Run", roomId, playerTemplateId, new EntityId("toolLogPlayer"), new GridCoord(0, 1)))).Ok);

        var result = dispatcher.Invoke(ContentToolNames.RunScenarioPlayerLogById, new ContentToolRunScenarioPlayerLogByIdRequest(sessionId, "tool-player-log-run", 1));

        Assert.True(result.Ok, result.Error?.Message);
        var data = Assert.IsType<AgentScenarioPlayerLogReport>(result.Data);
        Assert.Equal(new EntityId("toolLogPlayer"), data.ObserverEntityId);
        Assert.Equal("player narrative projection", data.ProjectionKind);
        Assert.Empty(data.Turns);
        Assert.Empty(data.Rows);
        var serialized = JsonSerializer.Serialize(result, ContentToolJson.Options);
        Assert.DoesNotContain("traceLines", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("finalStateLines", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inventorySummaryLines", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContentToolDispatcherOpensScansAndValidatesCuratedScenarioManifest()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ggg-content-tool-manifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var contentPath = Path.Combine(directory, "Delta.yaml");
            var manifestPath = Path.Combine(directory, ScenarioCatalog.ManifestFileName);
            var document = new EditableContentDocument();
            var roomId = document.AddEntityTemplate("Room", new EntityTemplate("Room", InventoryWidth: 2, InventoryHeight: 2, Bulk: 100, Aperture: 100), new EntityPresentation('#', PresentationColor.Gray));
            var playerId = document.AddEntityTemplate("Player", new EntityTemplate("Player", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 5), new EntityPresentation('@', PresentationColor.Yellow));
            document.UpsertScenario(new ScenarioDefinition("delta-canonical-move-outcomes", "Delta Canonical Move Outcomes", roomId, playerId, new EntityId("player"), new GridCoord(0, 0)));
            File.WriteAllText(contentPath, document.SaveYaml());
            File.WriteAllText(manifestPath, """
                sections:
                - id: delta
                  name: Delta
                  description: Vertical-slice requirements scenarios.
                  entries:
                  - contentPath: Delta.yaml
                    scenarioId: delta-canonical-move-outcomes
                    name: Delta Canonical Move Outcomes
                    description: Demonstrates canonical movement outcomes for review; Delta vertical-slice provenance with no known caveats.
                    status: active-delta
                """);
            var dispatcher = new ContentToolDispatcher(new ContentToolSessionRegistry());

            var opened = dispatcher.Invoke(ContentToolNames.OpenScenarioManifest, new ContentToolScenarioManifestRequest(manifestPath));
            var scanned = dispatcher.Invoke(ContentToolNames.ScanScenarioManifestCandidates, new ContentToolScenarioManifestScanRequest(directory));
            var validated = dispatcher.Invoke(ContentToolNames.ValidateScenarioManifest, new ContentToolScenarioManifestValidateRequest(manifestPath, directory));

            Assert.True(opened.Ok, opened.Error?.Message);
            Assert.Single(Assert.IsType<ScenarioCatalogResult>(opened.Data).Sections!);
            Assert.True(scanned.Ok, scanned.Error?.Message);
            Assert.Equal("delta-canonical-move-outcomes", Assert.Single(Assert.IsType<ScenarioCatalogResult>(scanned.Data).Entries).ScenarioId);
            Assert.True(validated.Ok, validated.Error?.Message);
            Assert.True(Assert.IsType<ContentToolScenarioManifestValidationSummary>(validated.Data).IsValid);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
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
    public void ContentToolDispatcherAcceptsStringIdsAndMaterialFromToolJson()
    {
        var dispatcher = new ContentToolDispatcher(new ContentToolSessionRegistry());
        var sessionId = Assert.IsType<ContentToolSessionOpened>(
            dispatcher.Invoke(ContentToolNames.CreateNew, new ContentToolCreateNewRequest()).Data).SessionId;
        var templateId = Assert.IsType<ContentToolCreatedEntityTemplate>(
            dispatcher.Invoke(ContentToolNames.CreateEntityTemplate, new ContentToolCreateEntityTemplateRequest(sessionId, "Chest")).Data).EntityTemplateId;

        var planId = Assert.IsType<ContentToolCreatedActionPlan>(
            dispatcher.Invoke(ContentToolNames.CreateActionPlan, new ContentToolCreateActionPlanRequest(sessionId, "Walk")).Data).ActionPlanTemplateId;
        var args = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
        {
            sessionId,
            actionPlanTemplateId = planId.Value,
            steps = new[] { "MoveFacing" }
        }));

        var result = dispatcher.Invoke(ContentToolNames.SetActionPlanBehavior, args);
        var updateArgs = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new
        {
            sessionId,
            entityTemplateId = templateId.Value,
            update = new { material = "wood" }
        }));
        var update = dispatcher.Invoke(ContentToolNames.UpdateEntityTemplate, updateArgs);

        Assert.True(result.Ok, result.Error?.Message);
        Assert.True(update.Ok, update.Error?.Message);
        Assert.Contains("material: wood", dispatcher.Invoke(ContentToolNames.Snapshot, new ContentToolSessionRequest(sessionId)).Data!.ToString());
    }
}
