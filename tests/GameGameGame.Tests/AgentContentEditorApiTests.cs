using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Editor)]
public sealed class AgentContentEditorApiTests
{
    [Fact]
    public void AgentContentEditorApiAuthorsMovementCapableContent()
    {
        var api = AgentContentEditorApi.CreateNew();

        var actorId = AssertSuccess(api.CreateEntityTemplate("Agent Actor"));
        AssertSuccess(api.UpdateEntityTemplate(
            actorId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 2,
                InventoryHeight: 2,
                Bulk: 5,
                Aperture: 10,
                PresentationId: new PresentationId("actor.player"),
                PaletteId: new PaletteId("actor.player.default"),
                Glyph: '@',
                Color: PresentationColor.Cyan)));
        AssertSuccess(api.SetInitialFacing(actorId, Direction.East));

        var planId = AssertSuccess(api.CreateActionPlan("Agent Patrol"));
        AssertSuccess(api.UpdateActionPlanStep(
            planId,
            stepIndex: 0,
            new AgentActionPlanStepRequest(
                "move east when possible",
                [PlanCheckDescriptor.CanMove()],
                PlanEffectDescriptor.Move(),
                PlanEffectDescriptor.ReverseDirection(consumesTurn: false, continuePlan: false))));
        AssertSuccess(api.AddActionPlanStep(
            planId,
            new AgentActionPlanStepRequest(
                "advanced teleport exercise",
                OnSuccess: PlanEffectDescriptor.Teleport(
                    MovementTargetDescriptor.Self(),
                    MovementDestinationDescriptor.AdjacentToSelf(Direction.East)))));
        AssertSuccess(api.SetDefaultActionPlan(actorId, planId));

        var snapshot = api.GetDocumentSnapshot();

        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));
        Assert.Contains("agentActor:", snapshot.YamlPreview);
        Assert.Contains("presentationId: actor.player", snapshot.YamlPreview);
        Assert.Contains("paletteId: actor.player.default", snapshot.YamlPreview);
        Assert.Contains("defaultActionPlanId: agentPatrol", snapshot.YamlPreview);
        Assert.Contains("actionStateDefaults:", snapshot.YamlPreview);
        Assert.Contains("facing: East", snapshot.YamlPreview);
        Assert.Contains("kind: CanMove", snapshot.YamlPreview);
        Assert.Contains("kind: Move", snapshot.YamlPreview);
        Assert.Contains("kind: Teleport", snapshot.YamlPreview);
    }

    [Fact]
    public void AgentContentEditorApiValidatesMultiDocumentWorkspace()
    {
        var api = new AgentContentWorkspaceApi(CreateWorkspaceDocuments());

        var result = api.ValidateWorkspace();

        var report = AssertSuccess(result);
        Assert.True(report.Validation.IsValid, string.Join(Environment.NewLine, report.Validation.Errors));
        Assert.Equal(2, report.Documents.Count);
        Assert.Contains(report.Documents, document => document.DocumentId == "debug.scenario" && !document.IsProtected);
        Assert.Contains(report.Documents, document => document.DocumentId == "canonical.templates" && document.IsProtected);
    }

    [Fact]
    public void AgentContentEditorApiListsWorkspaceDocumentsAndSymbols()
    {
        var api = new AgentContentWorkspaceApi(CreateWorkspaceDocuments());

        var result = api.ListWorkspace();

        var report = AssertSuccess(result);
        Assert.Contains(report.Documents, document =>
            document.DocumentId == "canonical.templates"
            && document.SourcePath == "templates.yaml"
            && document.SourceKind == ContentWorkspaceSourceKind.Canonical
            && document.IsProtected);
        Assert.Contains(report.Symbols, symbol =>
            symbol.Kind == ContentSymbolKind.Scenario
            && symbol.Id == "debugRoom"
            && symbol.DocumentId == "debug.scenario");
        Assert.Contains(report.Symbols, symbol =>
            symbol.Kind == ContentSymbolKind.EntityTemplate
            && symbol.Id == "debugRoot"
            && symbol.DocumentId == "canonical.templates");
    }

    [Fact]
    public void AgentContentEditorApiRunsWorkspaceScenarioById()
    {
        var api = new AgentContentWorkspaceApi(CreateWorkspaceDocuments());

        var result = api.RunWorkspaceScenarioById("debugRoom", turnCount: 0);

        var report = AssertSuccess(result);
        Assert.Equal(new EntityTemplateId("debugRoot"), report.ScenarioRootEntityTemplateId);
        Assert.Empty(report.ValidationDiagnostics);
        Assert.Contains("Run mode: Workspace persisted scenario simulation", report.SetupLines);
        Assert.Contains(report.FinalStateLines, line => line.Contains("Player: scenarioRoot(0,0)", StringComparison.Ordinal));
    }

    [Fact]
    public void AgentContentEditorApiAuthorsTargetingRulesAndBehaviorTargetSlots()
    {
        var api = AgentContentEditorApi.CreateNew();
        var mouseId = AssertSuccess(api.CreateEntityTemplate("Mouse"));
        var catId = AssertSuccess(api.CreateEntityTemplate("Cat"));
        AssertSuccess(api.UpdateEntityTemplate(mouseId, new AgentEntityTemplateUpdate(Glyph: 'm', Color: PresentationColor.Gray)));
        AssertSuccess(api.UpdateEntityTemplate(catId, new AgentEntityTemplateUpdate(Glyph: 'c', Color: PresentationColor.Earth)));
        AssertSuccess(api.SetTargetingRule(mouseId, new EntityTargetingRule(2, catId, Range: 4, Hint: "Danger", Label: "danger")));
        var planId = AssertSuccess(api.CreateActionPlan("Mouse Behavior"));
        AssertSuccess(api.ClearActionPlanBehavior(planId));
        AssertSuccess(api.AddActionPlanBehaviorStep(planId, ActionPlanBehaviorStepKind.TargetPathMove));
        AssertSuccess(api.SetActionPlanBehaviorStepTargetLabel(planId, stepIndex: 0, targetLabel: "danger"));
        AssertSuccess(api.SetDefaultActionPlan(mouseId, planId));

        var rules = AssertSuccess(api.ListTargetingRules(mouseId));
        var snapshot = api.GetDocumentSnapshot();

        var rule = Assert.Single(rules);
        Assert.Equal(2, rule.Slot);
        Assert.Equal("danger", rule.Label);
        Assert.Equal(catId, rule.TargetTemplateId);
        Assert.Contains("targetingRules:", snapshot.YamlPreview);
        Assert.Contains("label: danger", snapshot.YamlPreview);
        Assert.Contains("targetLabel: danger", snapshot.YamlPreview);
    }

    [Fact]
    public void AgentContentEditorApiAuthorsCanonicalMoveDirectionMode()
    {
        var api = AgentContentEditorApi.CreateNew();
        var planId = AssertSuccess(api.CreateActionPlan("Canonical Move"));

        AssertSuccess(api.AddActionPlanBehaviorStep(planId, ActionPlanBehaviorStepKind.Move));
        AssertSuccess(api.SetActionPlanBehaviorStepDirectionMode(planId, stepIndex: 0, ActionPlanMoveDirectionMode.BackLeft));

        var preview = AssertSuccess(api.PreviewActionPlan(planId));
        var step = Assert.Single(preview.ActionSteps);
        var snapshot = api.GetDocumentSnapshot();

        Assert.Equal(ActionPlanBehaviorStepKind.Move, step.Kind);
        Assert.Equal(ActionPlanMoveDirectionMode.BackLeft, step.DirectionMode);
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.Contains("kind: Move", snapshot.YamlPreview);
        Assert.Contains("directionMode: BackLeft", snapshot.YamlPreview);
    }

    [Fact]
    public void AgentContentEditorApiAuthorsCanonicalTransferBehavior()
    {
        var api = AgentContentEditorApi.CreateNew();
        var planId = AssertSuccess(api.CreateActionPlan("Canonical Transfer"));

        AssertSuccess(api.AddActionPlanBehaviorStep(planId, ActionPlanBehaviorStepKind.Transfer));
        AssertSuccess(api.SetActionPlanBehaviorStepTargetLabel(planId, stepIndex: 0, "offers"));
        AssertSuccess(api.SetActionPlanBehaviorStepDirectionMode(planId, stepIndex: 0, ActionPlanMoveDirectionMode.Forward));
        AssertSuccess(api.SetActionPlanBehaviorStepTransferDirection(planId, stepIndex: 0, TransferDirection.ActorToTarget));

        var preview = AssertSuccess(api.PreviewActionPlan(planId));
        var step = Assert.Single(preview.ActionSteps);
        var snapshot = api.GetDocumentSnapshot();

        Assert.Equal(ActionPlanBehaviorStepKind.Transfer, step.Kind);
        Assert.Equal("offers", step.TargetLabel);
        Assert.Equal(ActionPlanMoveDirectionMode.Forward, step.DirectionMode);
        Assert.Equal(TransferDirection.ActorToTarget, step.TransferDirection);
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.Contains("kind: Transfer", snapshot.YamlPreview);
        Assert.Contains("targetLabel: offers", snapshot.YamlPreview);
        Assert.Contains("directionMode: Forward", snapshot.YamlPreview);
        Assert.Contains("transferDirection: ActorToTarget", snapshot.YamlPreview);
    }

    [Fact]
    public void AgentContentEditorApiAuthorsBehaviorStepCosts()
    {
        var api = AgentContentEditorApi.CreateNew();
        var scrapId = AssertSuccess(api.CreateEntityTemplate("Scrap"));
        AssertSuccess(api.UpdateEntityTemplate(
            scrapId,
            new AgentEntityTemplateUpdate(InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 1, Glyph: 's', Color: PresentationColor.Gray)));
        var planId = AssertSuccess(api.CreateActionPlan("Costly Move"));

        AssertSuccess(api.AddActionPlanBehaviorStep(planId, ActionPlanBehaviorStepKind.MoveFacing));
        AssertSuccess(api.SetActionPlanBehaviorStepCosts(planId, 0, [new ActionStepCostDescriptor(scrapId.Value, 3)]));

        var preview = AssertSuccess(api.PreviewActionPlan(planId));
        var step = Assert.Single(preview.ActionSteps);
        var snapshot = api.GetDocumentSnapshot();

        Assert.Equal("Cost: 3× Scrap", step.CostSummary);
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.Contains("costs:", snapshot.YamlPreview);
        Assert.Contains("templateId: scrap", snapshot.YamlPreview);
        Assert.Contains("quantity: 3", snapshot.YamlPreview);
    }

    [Fact]
    public void AgentContentEditorApiAuthorsTargetPathMoveBehavior()
    {
        var api = AgentContentEditorApi.CreateNew();
        var planId = AssertSuccess(api.CreateActionPlan("Orbit Target"));

        AssertSuccess(api.AddActionPlanBehaviorStep(planId, ActionPlanBehaviorStepKind.TargetPathMove));
        AssertSuccess(api.SetActionPlanBehaviorStepTargetLabel(planId, 0, "enemy"));
        AssertSuccess(api.SetActionPlanBehaviorStepTargetPathMode(planId, 0, ActionPlanTargetPathMode.Orbit));
        AssertSuccess(api.SetActionPlanBehaviorStepDesiredDistance(planId, 0, 6));
        AssertSuccess(api.SetActionPlanBehaviorStepOrbitDirection(planId, 0, ActionPlanOrbitDirection.Anticlockwise));

        var preview = AssertSuccess(api.PreviewActionPlan(planId));
        var step = Assert.Single(preview.ActionSteps);
        var snapshot = api.GetDocumentSnapshot();

        Assert.Equal(ActionPlanTargetPathMode.Orbit, step.PathMode);
        Assert.Equal(6, step.DesiredDistance);
        Assert.Equal(ActionPlanOrbitDirection.Anticlockwise, step.OrbitDirection);
        Assert.Contains("pathMode: Orbit", snapshot.YamlPreview);
        Assert.Contains("desiredDistance: 6", snapshot.YamlPreview);
        Assert.Contains("orbitDirection: Anticlockwise", snapshot.YamlPreview);
    }

    [Fact]
    public void AgentContentEditorApiAuthorsInventoryBoundaryPolicies()
    {
        var api = AgentContentEditorApi.CreateNew();
        var roomId = AssertSuccess(api.CreateEntityTemplate("Policy Room"));

        AssertSuccess(api.UpdateEntityTemplate(
            roomId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 3,
                InventoryHeight: 3,
                Bulk: 1,
                Aperture: 10,
                EnterPolicy: EntityEnterPolicy.FarthestFromOccupied,
                ExitPolicy: EntityExitPolicy.EdgeAlignedWithExitDirection,
                TopologyPolicy: EntityTopologyPolicy.ConnectsOutward)));

        var snapshot = api.GetDocumentSnapshot();

        Assert.Contains("enterPolicy: FarthestFromOccupied", snapshot.YamlPreview);
        Assert.Contains("exitPolicy: EdgeAlignedWithExitDirection", snapshot.YamlPreview);
        Assert.Contains("topologyPolicy: ConnectsOutward", snapshot.YamlPreview);
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));

        AssertSuccess(api.UpdateEntityTemplate(roomId, new AgentEntityTemplateUpdate(ClearEnterPolicy: true, ClearExitPolicy: true, TopologyPolicy: EntityTopologyPolicy.None)));
        snapshot = api.GetDocumentSnapshot();

        Assert.DoesNotContain("enterPolicy:", snapshot.YamlPreview);
        Assert.DoesNotContain("exitPolicy:", snapshot.YamlPreview);
        Assert.DoesNotContain("topologyPolicy:", snapshot.YamlPreview);
    }

    [Fact]
    public void AgentContentEditorApiAuthorsAndClearsTemplateMaterial()
    {
        var api = AgentContentEditorApi.CreateNew();
        var id = api.CreateEntityTemplate("Wooden Chest").Value;

        api.UpdateEntityTemplate(id, new AgentEntityTemplateUpdate(Material: new EntityMaterial("wood")));
        var authored = api.Session.Editor.GetEntityPreset(id).Template;

        Assert.Equal(new EntityMaterial("wood"), authored.Material);
        Assert.Contains("material: wood", api.Session.GetYamlPreview());

        api.UpdateEntityTemplate(id, new AgentEntityTemplateUpdate(ClearMaterial: true));

        Assert.Null(api.Session.Editor.GetEntityPreset(id).Template.Material);
        Assert.DoesNotContain("material:", api.Session.GetYamlPreview());
    }

    [Fact]
    public void AgentContentEditorApiAuthorsMergedInventoryLayerPlacements()
    {
        var api = AgentContentEditorApi.CreateNew();
        var roomId = AssertSuccess(api.CreateEntityTemplate("Merged Room"));
        var spaceAId = AssertSuccess(api.CreateEntityTemplate("Space A"));
        var spaceBId = AssertSuccess(api.CreateEntityTemplate("Space B"));
        AssertSuccess(api.UpdateEntityTemplate(roomId, new AgentEntityTemplateUpdate(InventoryWidth: 3, InventoryHeight: 3, Bulk: 10, Aperture: 10)));
        AssertSuccess(api.UpdateEntityTemplate(spaceAId, new AgentEntityTemplateUpdate(InventoryWidth: 3, InventoryHeight: 3, Bulk: 1, Aperture: 10)));
        AssertSuccess(api.UpdateEntityTemplate(spaceBId, new AgentEntityTemplateUpdate(InventoryWidth: 2, InventoryHeight: 2, Bulk: 1, Aperture: 10)));
        AssertSuccess(api.PlaceCarriedEntity(roomId, new EntityId("entityA"), spaceAId, new GridCoord(0, 0)));
        AssertSuccess(api.PlaceCarriedEntity(roomId, new EntityId("entityB"), spaceBId, new GridCoord(1, 0)));

        AssertSuccess(api.UpsertMergedInventoryLayer(new AgentMergedInventoryLayerDefinition(
            new MergedInventoryLayerId("sharedInterior"),
            [
                new AgentMergedInventorySpaceContribution(new EntityId("entityA"), new GridCoord(0, 0)),
                new AgentMergedInventorySpaceContribution(new EntityId("entityB"), new GridCoord(3, 0))
            ],
            [
                new AgentMergedInventoryAlignedJoin(
                    new AgentMergedInventoryJoinEndpoint(new EntityId("entityA"), Direction.East),
                    new AgentMergedInventoryJoinEndpoint(new EntityId("entityB"), Direction.West),
                    MergedInventoryJoinAlignment.Center)
            ])));
        var snapshot = api.GetDocumentSnapshot();

        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        var layer = Assert.Single(snapshot.MergedInventoryLayers, layer => layer.Id == new MergedInventoryLayerId("sharedInterior"));
        var join = Assert.Single(layer.Joins!);
        Assert.Equal(new EntityId("entityA"), join.From.OwnerId);
        Assert.Equal(Direction.East, join.From.Edge);
        Assert.Equal(new EntityId("entityB"), join.To.OwnerId);
        Assert.Equal(Direction.West, join.To.Edge);
        Assert.Contains("mergedLayers:", snapshot.YamlPreview);
        Assert.Contains("owner: entityB", snapshot.YamlPreview);
        Assert.Contains("x: 3", snapshot.YamlPreview);
        Assert.Contains("joins:", snapshot.YamlPreview);
        Assert.Contains("edge: East", snapshot.YamlPreview);
    }

    [Fact]
    public void AgentContentEditorApiRejectsLegacySetVariableAuthoring()
    {
        var api = AgentContentEditorApi.CreateNew();
        var planId = AssertSuccess(api.CreateActionPlan("Legacy Attempt"));

        var result = api.SetActionPlanStepSuccessEffect(
            planId,
            stepIndex: 0,
            PlanEffectDescriptor.SetVariable(
                "facing",
                new DirectionPlanValue(Direction.West),
                consumesTurn: false,
                continuePlan: false));

        Assert.False(result.IsSuccess);
        Assert.Equal("UnsupportedEffectForAuthoring", result.Error!.Code);
    }

    [Fact]
    public void AgentContentEditorApiAuthorsSimpleWanderingActorWithPrimitiveHelper()
    {
        var api = AgentContentEditorApi.CreateNew();
        var ratId = AssertSuccess(api.CreateEntityTemplate("Rat"));
        AssertSuccess(api.UpdateEntityTemplate(
            ratId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 1,
                InventoryHeight: 1,
                Bulk: 1,
                Aperture: 3,
                Glyph: 'r',
                Color: PresentationColor.Green)));
        AssertSuccess(api.SetInitialFacing(ratId, Direction.West));

        var plans = AssertSuccess(api.CreateMoveFacingPickupTargetChain("Rat Wander", "Rat Pickup"));
        AssertSuccess(api.SetDefaultActionPlan(ratId, plans.MoveFacingPlanId));

        var snapshot = api.GetDocumentSnapshot();

        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));
        Assert.Contains("kind: MoveFacing", snapshot.YamlPreview);
        Assert.Contains("kind: PickupTarget", snapshot.YamlPreview);
        Assert.Contains("fallbackPlanId: ratPickup", snapshot.YamlPreview);
        Assert.DoesNotContain("kind: CanMove", snapshot.YamlPreview);
        Assert.DoesNotContain("kind: BlockingEntity", snapshot.YamlPreview);
        Assert.DoesNotContain("kind: CallPlan", snapshot.YamlPreview);
        Assert.DoesNotContain("kind: SetVariable", snapshot.YamlPreview);
    }

    [Fact]
    public void AgentContentEditorApiAuthorsCanonicalGiveTakeBehavior()
    {
        var api = AgentContentEditorApi.CreateNew();
        var traderId = AssertSuccess(api.CreateEntityTemplate("Transfer Trader"));
        AssertSuccess(api.UpdateEntityTemplate(
            traderId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 2,
                InventoryHeight: 1,
                Bulk: 1,
                Aperture: 10,
                Glyph: 't',
                Color: PresentationColor.Yellow)));
        var planId = AssertSuccess(api.CreateActionPlan("Transfer Behavior"));

        AssertSuccess(api.SetActionPlanBehavior(planId, [ActionPlanBehaviorStepKind.GiveTarget, ActionPlanBehaviorStepKind.TakeTarget]));
        AssertSuccess(api.SetDefaultActionPlan(traderId, planId));
        var steps = AssertSuccess(api.ListActionSteps());
        var snapshot = api.GetDocumentSnapshot();

        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.GiveTarget);
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.TakeTarget);
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.Contains("kind: GiveTarget", snapshot.YamlPreview);
        Assert.Contains("kind: TakeTarget", snapshot.YamlPreview);
    }

    [Fact]
    public void AgentContentEditorApiAuthorsCanonicalEnterExitBehavior()
    {
        var api = AgentContentEditorApi.CreateNew();
        var explorerId = AssertSuccess(api.CreateEntityTemplate("Containment Explorer"));
        AssertSuccess(api.UpdateEntityTemplate(
            explorerId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 1,
                InventoryHeight: 1,
                Bulk: 1,
                Aperture: 10,
                Glyph: 'e',
                Color: PresentationColor.Cyan)));
        AssertSuccess(api.SetInitialFacing(explorerId, Direction.West));
        var planId = AssertSuccess(api.CreateActionPlan("Enter Exit Behavior"));

        AssertSuccess(api.SetActionPlanBehavior(planId, [ActionPlanBehaviorStepKind.EnterTarget, ActionPlanBehaviorStepKind.ExitFacing]));
        AssertSuccess(api.SetDefaultActionPlan(explorerId, planId));
        var steps = AssertSuccess(api.ListActionSteps());
        var snapshot = api.GetDocumentSnapshot();

        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.EnterTarget);
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.ExitFacing);
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.Contains("kind: EnterTarget", snapshot.YamlPreview);
        Assert.Contains("kind: ExitFacing", snapshot.YamlPreview);
    }

    [Fact]
    public void AgentContentEditorApiAuthorsApplyPrePlanBehavior()
    {
        var api = AgentContentEditorApi.CreateNew();
        var fearPlanId = AssertSuccess(api.CreateActionPlan("Fear"));
        AssertSuccess(api.SetActionPlanBehavior(fearPlanId, [ActionPlanBehaviorStepKind.Backstep]));
        var casterPlanId = AssertSuccess(api.CreateActionPlan("Caster"));
        AssertSuccess(api.ClearActionPlanBehavior(casterPlanId));
        AssertSuccess(api.AddActionPlanBehaviorStep(casterPlanId, ActionPlanBehaviorStepKind.ApplyPrePlan));
        AssertSuccess(api.SetActionPlanBehaviorStepTargetSlot(casterPlanId, stepIndex: 0, targetSlot: 2));
        AssertSuccess(api.SetActionPlanBehaviorStepPlanId(casterPlanId, stepIndex: 0, new ActionPlanId(fearPlanId.Value)));

        var snapshot = api.GetDocumentSnapshot();

        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.Contains("kind: ApplyPrePlan", snapshot.YamlPreview);
        Assert.Contains("targetSlot: 2", snapshot.YamlPreview);
        Assert.Contains("planId: fear", snapshot.YamlPreview);
    }

    [Theory]
    [InlineData(ActionPlanBehaviorStepKind.ApplyMainPlan, "kind: ApplyMainPlan")]
    [InlineData(ActionPlanBehaviorStepKind.ApplyPostPlan, "kind: ApplyPostPlan")]
    public void AgentContentEditorApiAuthorsMainAndPostPlanOverrideBehavior(ActionPlanBehaviorStepKind stepKind, string yamlKind)
    {
        var api = AgentContentEditorApi.CreateNew();
        var overridePlanId = AssertSuccess(api.CreateActionPlan("Override Plan"));
        AssertSuccess(api.SetActionPlanBehavior(overridePlanId, [ActionPlanBehaviorStepKind.Backstep]));
        var casterPlanId = AssertSuccess(api.CreateActionPlan("Override Caster"));
        AssertSuccess(api.ClearActionPlanBehavior(casterPlanId));
        AssertSuccess(api.AddActionPlanBehaviorStep(casterPlanId, stepKind));
        AssertSuccess(api.SetActionPlanBehaviorStepPlanId(casterPlanId, stepIndex: 0, new ActionPlanId(overridePlanId.Value)));

        var snapshot = api.GetDocumentSnapshot();

        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.Contains(yamlKind, snapshot.YamlPreview);
        Assert.Contains("planId: overridePlan", snapshot.YamlPreview);
    }

    [Fact]
    public void AgentContentEditorApiRunsPersistedScenarioById()
    {
        var api = AgentContentEditorApi.CreateNew();
        var roomId = AssertSuccess(api.CreateEntityTemplate("API Scenario Room"));
        AssertSuccess(api.UpdateEntityTemplate(
            roomId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 3,
                InventoryHeight: 2,
                Bulk: 100,
                Aperture: 100,
                Glyph: '#',
                Color: PresentationColor.Gray)));
        var playerTemplateId = AssertSuccess(api.CreateEntityTemplate("API Scenario Player"));
        AssertSuccess(api.UpdateEntityTemplate(
            playerTemplateId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 0,
                InventoryHeight: 0,
                Bulk: 1,
                Aperture: 5,
                Glyph: '@',
                Color: PresentationColor.Yellow)));
        AssertSuccess(api.SetInitialFacing(playerTemplateId, Direction.East));
        var planId = AssertSuccess(api.CreateActionPlan("API Player Move"));
        AssertSuccess(api.SetActionPlanBehavior(planId, [ActionPlanBehaviorStepKind.MoveFacing]));
        AssertSuccess(api.SetDefaultActionPlan(playerTemplateId, planId));
        AssertSuccess(api.UpsertScenario(new AgentAlphaScenarioDefinition(
            "api-persisted-run",
            "API Persisted Run",
            roomId,
            playerTemplateId,
            new EntityId("apiPlayer"),
            new GridCoord(0, 1))));

        var report = AssertSuccess(api.RunScenarioById("api-persisted-run", turnCount: 1));

        Assert.Contains("Run mode: Persisted scenario simulation", report.SetupLines);
        Assert.Contains("Player: API Scenario Player apiPlayer at scenarioRoot(0,1), facing East, target none", report.SetupLines);
        Assert.Empty(report.Turns);
        Assert.Contains("API Scenario Player: scenarioRoot(0,1), facing East, target none", report.FinalStateLines);
        Assert.Contains(report.RuntimeObservations, observation => observation.Contains("API Scenario Player is awaiting PlayerChoice input", StringComparison.Ordinal));
        Assert.Equal([new EntityId("apiPlayer")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Empty(report.InventorySummaryLines);
        Assert.Empty(report.ValidationDiagnostics);
    }

    [Fact]
    public void AgentContentEditorApiRunsPersistedScenarioPlayerNarrativeLogById()
    {
        var api = AgentContentEditorApi.CreateNew();
        var roomId = AssertSuccess(api.CreateEntityTemplate("Narrative Room"));
        AssertSuccess(api.UpdateEntityTemplate(roomId, new AgentEntityTemplateUpdate(InventoryWidth: 3, InventoryHeight: 2, Bulk: 100, Aperture: 100, Glyph: '#', Color: PresentationColor.Gray)));
        var playerTemplateId = AssertSuccess(api.CreateEntityTemplate("Narrative Player"));
        AssertSuccess(api.UpdateEntityTemplate(playerTemplateId, new AgentEntityTemplateUpdate(InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 5, Glyph: '@', Color: PresentationColor.Yellow)));
        AssertSuccess(api.SetInitialFacing(playerTemplateId, Direction.East));
        var planId = AssertSuccess(api.CreateActionPlan("Narrative Player Move"));
        AssertSuccess(api.SetActionPlanBehavior(planId, [ActionPlanBehaviorStepKind.MoveFacing]));
        AssertSuccess(api.SetDefaultActionPlan(playerTemplateId, planId));
        AssertSuccess(api.UpsertScenario(new AgentAlphaScenarioDefinition(
            "api-player-log-run",
            "API Player Log Run",
            roomId,
            playerTemplateId,
            new EntityId("narrativePlayer"),
            new GridCoord(0, 1))));

        var report = AssertSuccess(api.RunScenarioPlayerLogById("api-player-log-run", turnCount: 1));

        Assert.Equal("api-player-log-run", report.ScenarioId);
        Assert.Equal("API Player Log Run", report.ScenarioName);
        Assert.Equal(new EntityId("narrativePlayer"), report.ObserverEntityId);
        Assert.Equal("player narrative projection", report.ProjectionKind);
        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Empty(report.Turns);
        Assert.Empty(report.Rows);
        Assert.Contains(report.FollowUps, item => item.Contains("line-of-sight", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AgentContentEditorApiCreatesCombinedPersistedScenarioReport()
    {
        var api = AgentContentEditorApi.CreateNew();
        var roomId = AssertSuccess(api.CreateEntityTemplate("Review Scenario Room"));
        AssertSuccess(api.UpdateEntityTemplate(
            roomId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 3,
                InventoryHeight: 2,
                Bulk: 100,
                Aperture: 100,
                Glyph: '#',
                Color: PresentationColor.Gray)));
        var playerTemplateId = AssertSuccess(api.CreateEntityTemplate("Review Player"));
        AssertSuccess(api.UpdateEntityTemplate(
            playerTemplateId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 0,
                InventoryHeight: 0,
                Bulk: 1,
                Aperture: 5,
                Glyph: '@',
                Color: PresentationColor.Yellow)));
        AssertSuccess(api.SetInitialFacing(playerTemplateId, Direction.East));
        var planId = AssertSuccess(api.CreateActionPlan("Review Move"));
        AssertSuccess(api.SetActionPlanBehavior(planId, [ActionPlanBehaviorStepKind.MoveFacing]));
        AssertSuccess(api.SetDefaultActionPlan(playerTemplateId, planId));
        AssertSuccess(api.UpsertScenario(new AgentAlphaScenarioDefinition(
            "review-persisted-run",
            "Review Persisted Run",
            roomId,
            playerTemplateId,
            new EntityId("reviewPlayer"),
            new GridCoord(0, 1))));

        var report = AssertSuccess(api.PreviewAndRunScenarioById("review-persisted-run", turnCount: 1));

        Assert.Equal("review-persisted-run", report.ScenarioId);
        Assert.True(report.DocumentValidation.IsValid, string.Join(Environment.NewLine, report.DocumentValidation.Errors));
        Assert.True(report.CanonicalValidation.IsValid, string.Join(Environment.NewLine, report.CanonicalValidation.Errors));
        var preview = Assert.Single(report.ActionPlanPreviews, item => item.PlanId == planId);
        Assert.Equal("Canonical Behavior Chain", preview.Shape);
        Assert.Contains(preview.ActionSteps, step => step.Kind == ActionPlanBehaviorStepKind.MoveFacing);
        Assert.DoesNotContain(
            preview.GetType().GetProperties(),
            property => property.Name == "YamlPreview");
        Assert.Equal(new EntityId("reviewPlayer"), report.Materialization.PlayerEntityId);
        Assert.Contains("Run mode: Persisted scenario simulation", report.RunReport.SetupLines);
        Assert.Contains("Review Player: scenarioRoot(0,1), facing East, target none", report.RunReport.FinalStateLines);
        Assert.Contains(report.RunReport.RuntimeObservations, observation => observation.Contains("Review Player is awaiting PlayerChoice input", StringComparison.Ordinal));
    }

    private static void AssertSuccess(AgentApiResult result)
    {
        Assert.True(result.IsSuccess, result.Error?.Message);
    }

    private static T AssertSuccess<T>(AgentApiResult<T> result)
    {
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value!;
    }

    private static IReadOnlyList<ContentWorkspaceDocument> CreateWorkspaceDocuments()
    {
        var scenario = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans: {}
            scenarios:
              debugRoom:
                name: Debug Room
                scenarioRootEntityTemplateId: debugRoot
                playerEntityTemplateId: player
                playerEntityId: debugPlayer
                playerStart: { x: 0, y: 0 }
            """);
        var templates = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              debugRoot:
                name: Debug Root
                inventoryWidth: 2
                inventoryHeight: 2
                weight: 100
                carryingCapacity: 100
              player:
                name: Player
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
            presentations:
              debugRoot: { glyph: '#', color: Gray }
              player: { glyph: '@', color: White }
            actionPlans: {}
            """);

        return [
            new ContentWorkspaceDocument(scenario, "debug.scenario", "debug-room.yaml", ContentWorkspaceSourceKind.User),
            new ContentWorkspaceDocument(templates, "canonical.templates", "templates.yaml", ContentWorkspaceSourceKind.Canonical, IsReadOnly: true)
        ];
    }
}
