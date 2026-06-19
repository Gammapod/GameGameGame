using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Editor;

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
                Weight: 5,
                CarryingCapacity: 10,
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
        Assert.Contains("defaultActionPlanId: agentPatrol", snapshot.YamlPreview);
        Assert.Contains("actionStateDefaults:", snapshot.YamlPreview);
        Assert.Contains("facing: East", snapshot.YamlPreview);
        Assert.Contains("kind: CanMove", snapshot.YamlPreview);
        Assert.Contains("kind: Move", snapshot.YamlPreview);
        Assert.Contains("kind: Teleport", snapshot.YamlPreview);
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
    public void AgentContentEditorApiAuthorsPrimitiveBackedPlan()
    {
        var api = AgentContentEditorApi.CreateNew();
        var actorId = AssertSuccess(api.CreateEntityTemplate("Primitive Actor"));
        var fallbackId = AssertSuccess(api.CreateActionPlan("Fallback Wait"));
        var planId = AssertSuccess(api.CreateActionPlan("Primitive Move"));

        AssertSuccess(api.SetInitialFacing(actorId, Direction.North));
        AssertSuccess(api.SetActionPlanPrimitive(planId, ActionPlanPrimitiveKind.MoveFacing, new ActionPlanId(fallbackId.Value)));
        AssertSuccess(api.SetDefaultActionPlan(actorId, planId));

        var snapshot = api.GetDocumentSnapshot();

        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.Contains("primitive:", snapshot.YamlPreview);
        Assert.Contains("kind: MoveFacing", snapshot.YamlPreview);
        Assert.Contains("fallbackPlanId: fallbackWait", snapshot.YamlPreview);
    }

    [Fact]
    public void AgentContentEditorApiAuthorsMoveFacingToPickupTargetChain()
    {
        var api = AgentContentEditorApi.CreateNew();
        var actorId = AssertSuccess(api.CreateEntityTemplate("Primitive Collector"));
        var pickupId = AssertSuccess(api.CreateActionPlan("Pickup Target"));
        var moveId = AssertSuccess(api.CreateActionPlan("Move Facing"));

        AssertSuccess(api.SetInitialFacing(actorId, Direction.North));
        AssertSuccess(api.SetActionPlanPrimitive(pickupId, ActionPlanPrimitiveKind.PickupTarget));
        AssertSuccess(api.SetActionPlanPrimitive(moveId, ActionPlanPrimitiveKind.MoveFacing, new ActionPlanId(pickupId.Value)));
        AssertSuccess(api.SetDefaultActionPlan(actorId, moveId));

        var snapshot = api.GetDocumentSnapshot();

        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));
        Assert.Contains("kind: MoveFacing", snapshot.YamlPreview);
        Assert.Contains("fallbackPlanId: pickupTarget", snapshot.YamlPreview);
        Assert.Contains("kind: PickupTarget", snapshot.YamlPreview);
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
                Weight: 1,
                CarryingCapacity: 3,
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
    public void AgentContentEditorApiListsCanonicalActionStepMetadata()
    {
        var api = AgentContentEditorApi.CreateNew();

        var steps = AssertSuccess(api.ListActionSteps());

        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.MoveFacing && step.DisplayName == "Move Facing");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.PickupTarget && step.DisplayName == "Pickup Target");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.DropFacing && step.DisplayName == "Drop Facing");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.PushFacing && step.DisplayName == "Push Facing");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.DestroyTarget && step.DisplayName == "Destroy Target");
        Assert.Contains(steps, step => step.Kind == ActionPlanBehaviorStepKind.CreateFacing && step.DisplayName == "Create Facing");
    }

    [Fact]
    public void AgentContentEditorApiAuthorsCanonicalBehaviorChain()
    {
        var api = AgentContentEditorApi.CreateNew();
        var ratId = AssertSuccess(api.CreateEntityTemplate("Behavior Rat"));
        AssertSuccess(api.UpdateEntityTemplate(
            ratId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 1,
                InventoryHeight: 1,
                Weight: 1,
                CarryingCapacity: 3,
                Glyph: 'r',
                Color: PresentationColor.Green)));
        AssertSuccess(api.SetInitialFacing(ratId, Direction.West));

        var planId = AssertSuccess(api.CreateActionPlan("Rat Behavior"));
        AssertSuccess(api.SetActionPlanBehavior(
            planId,
            [ActionPlanBehaviorStepKind.MoveFacing, ActionPlanBehaviorStepKind.PickupTarget]));
        AssertSuccess(api.SetDefaultActionPlan(ratId, planId));

        var snapshot = api.GetDocumentSnapshot();

        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));
        Assert.Contains("behavior:", snapshot.YamlPreview);
        Assert.Contains("kind: MoveFacing", snapshot.YamlPreview);
        Assert.Contains("kind: PickupTarget", snapshot.YamlPreview);
        Assert.DoesNotContain("primitive:", snapshot.YamlPreview);
        Assert.DoesNotContain("fallbackPlanId:", snapshot.YamlPreview);
        Assert.DoesNotContain("kind: CanMove", snapshot.YamlPreview);
        Assert.DoesNotContain("kind: CallPlan", snapshot.YamlPreview);
        Assert.DoesNotContain("kind: SetVariable", snapshot.YamlPreview);
    }

    [Fact]
    public void AgentContentEditorApiPreviewsCanonicalBehaviorPlan()
    {
        var api = AgentContentEditorApi.CreateNew();
        var ratId = AssertSuccess(api.CreateEntityTemplate("Preview Rat"));
        AssertSuccess(api.UpdateEntityTemplate(
            ratId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 1,
                InventoryHeight: 1,
                Weight: 1,
                CarryingCapacity: 3,
                Glyph: 'r',
                Color: PresentationColor.Green)));
        var planId = AssertSuccess(api.CreateMoveFacingPickupTargetBehavior("Preview Rat Behavior"));
        AssertSuccess(api.SetDefaultActionPlan(ratId, planId));

        var preview = AssertSuccess(api.PreviewActionPlan(planId, ratId));

        Assert.Equal("Canonical Behavior Chain", preview.Shape);
        Assert.Equal([ActionPlanBehaviorStepKind.MoveFacing, ActionPlanBehaviorStepKind.PickupTarget], preview.ActionSteps.Select(step => step.Kind).ToArray());
        Assert.Contains("Facing=West", preview.StateHints);
        Assert.Contains("Target=Self (defaultable)", preview.StateHints);
        Assert.Empty(preview.ValidationDiagnostics);
    }

    [Fact]
    public void AgentContentEditorApiRunsScenarioRootInventoryActorsInInitiativeOrder()
    {
        var api = AgentContentEditorApi.CreateNew();
        var scenarioRootId = AssertSuccess(api.CreateEntityTemplate("Scenario Room"));
        AssertSuccess(api.UpdateEntityTemplate(
            scenarioRootId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 3,
                InventoryHeight: 2,
                Weight: 100,
                CarryingCapacity: 100,
                Glyph: '#',
                Color: PresentationColor.Gray)));

        var eastWalkerId = AssertSuccess(api.CreateEntityTemplate("East Walker"));
        AssertSuccess(api.UpdateEntityTemplate(
            eastWalkerId,
            new AgentEntityTemplateUpdate(Weight: 1, CarryingCapacity: 1, Glyph: 'e', Color: PresentationColor.Green)));
        AssertSuccess(api.SetInitialFacing(eastWalkerId, Direction.East));
        var eastPlanId = AssertSuccess(api.CreateActionPlan("East Walker Behavior"));
        AssertSuccess(api.SetActionPlanBehavior(eastPlanId, [ActionPlanBehaviorStepKind.MoveFacing]));
        AssertSuccess(api.SetDefaultActionPlan(eastWalkerId, eastPlanId));

        var southWalkerId = AssertSuccess(api.CreateEntityTemplate("South Walker"));
        AssertSuccess(api.UpdateEntityTemplate(
            southWalkerId,
            new AgentEntityTemplateUpdate(Weight: 1, CarryingCapacity: 1, Glyph: 's', Color: PresentationColor.Cyan)));
        AssertSuccess(api.SetInitialFacing(southWalkerId, Direction.South));
        var southPlanId = AssertSuccess(api.CreateActionPlan("South Walker Behavior"));
        AssertSuccess(api.SetActionPlanBehavior(southPlanId, [ActionPlanBehaviorStepKind.MoveFacing]));
        AssertSuccess(api.SetDefaultActionPlan(southWalkerId, southPlanId));

        AssertSuccess(api.PlaceCarriedEntity(scenarioRootId, new EntityId("eastWalker"), eastWalkerId, new GridCoord(0, 0)));
        AssertSuccess(api.PlaceCarriedEntity(scenarioRootId, new EntityId("southWalker"), southWalkerId, new GridCoord(2, 0)));

        var report = AssertSuccess(api.RunScenario(new AgentScenarioRunRequest(scenarioRootId, TurnCount: 1)));

        Assert.Equal([new EntityId("eastWalker"), new EntityId("southWalker")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Equal(["East Walker", "South Walker"], report.Turns.Select(turn => turn.ActorName).ToArray());
        Assert.Contains("East Walker: scenarioRoot(1,0), facing East, target none", report.FinalStateLines);
        Assert.Contains("South Walker: scenarioRoot(2,1), facing South, target none", report.FinalStateLines);
        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Empty(report.CapabilityGaps);
    }

    [Fact]
    public void AgentContentEditorApiScenarioReportShowsBehaviorStepsAndTreatsNoActionAsObservation()
    {
        var api = AgentContentEditorApi.CreateNew();
        var scenarioRootId = AssertSuccess(api.CreateEntityTemplate("Scenario Duel Room"));
        AssertSuccess(api.UpdateEntityTemplate(
            scenarioRootId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 3,
                InventoryHeight: 1,
                Weight: 100,
                CarryingCapacity: 100,
                Glyph: '#',
                Color: PresentationColor.Gray)));

        var passiveId = AssertSuccess(api.CreateEntityTemplate("Passive Walker"));
        AssertSuccess(api.UpdateEntityTemplate(passiveId, new AgentEntityTemplateUpdate(Weight: 1, CarryingCapacity: 1, Glyph: 'p', Color: PresentationColor.Green)));
        AssertSuccess(api.SetInitialFacing(passiveId, Direction.East));
        var passivePlanId = AssertSuccess(api.CreateActionPlan("Passive Walker Behavior"));
        AssertSuccess(api.SetActionPlanBehavior(passivePlanId, [ActionPlanBehaviorStepKind.MoveFacing]));
        AssertSuccess(api.SetDefaultActionPlan(passiveId, passivePlanId));

        var destroyerId = AssertSuccess(api.CreateEntityTemplate("Destroyer Walker"));
        AssertSuccess(api.UpdateEntityTemplate(destroyerId, new AgentEntityTemplateUpdate(Weight: 1, CarryingCapacity: 1, Glyph: 'd', Color: PresentationColor.Yellow)));
        AssertSuccess(api.SetInitialFacing(destroyerId, Direction.West));
        var destroyerPlanId = AssertSuccess(api.CreateActionPlan("Destroyer Walker Behavior"));
        AssertSuccess(api.SetActionPlanBehavior(destroyerPlanId, [ActionPlanBehaviorStepKind.MoveFacing, ActionPlanBehaviorStepKind.DestroyTarget]));
        AssertSuccess(api.SetDefaultActionPlan(destroyerId, destroyerPlanId));

        AssertSuccess(api.PlaceCarriedEntity(scenarioRootId, new EntityId("passive"), passiveId, new GridCoord(0, 0)));
        AssertSuccess(api.PlaceCarriedEntity(scenarioRootId, new EntityId("destroyer"), destroyerId, new GridCoord(1, 0)));

        var report = AssertSuccess(api.RunScenario(new AgentScenarioRunRequest(scenarioRootId, TurnCount: 1)));

        Assert.Empty(report.RuntimeFailures);
        Assert.Contains(report.RuntimeObservations, observation => observation.Contains("Passive Walker", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. MoveFacing: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("1. MoveFacing: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("2. DestroyTarget: Success", StringComparison.Ordinal));
        Assert.Contains("   writes: Target=passive", report.Turns[1].TraceLines);
        Assert.DoesNotContain(report.FinalStateLines, line => line.StartsWith("Passive Walker:", StringComparison.Ordinal));
        Assert.Contains("Destroyer Walker: scenarioRoot(1,0), facing West, target passive", report.FinalStateLines);
    }

    [Fact]
    public void AgentContentEditorApiMaterializesAlphaScenarioWithPlayerInsertion()
    {
        var api = AgentContentEditorApi.CreateNew();
        var scenarioRootId = AssertSuccess(api.CreateEntityTemplate("Alpha Room"));
        AssertSuccess(api.UpdateEntityTemplate(
            scenarioRootId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 4,
                InventoryHeight: 3,
                Weight: 100,
                CarryingCapacity: 100,
                Glyph: '#',
                Color: PresentationColor.Gray)));

        var playerTemplateId = AssertSuccess(api.CreateEntityTemplate("Alpha Player"));
        AssertSuccess(api.UpdateEntityTemplate(
            playerTemplateId,
            new AgentEntityTemplateUpdate(Weight: 1, CarryingCapacity: 5, Glyph: '@', Color: PresentationColor.Yellow)));
        AssertSuccess(api.SetInitialFacing(playerTemplateId, Direction.North));

        var materialization = AssertSuccess(api.MaterializeScenario(new AgentAlphaScenarioDefinition(
            ScenarioId: "alpha-smoke",
            Name: "Alpha Smoke",
            ScenarioRootEntityTemplateId: scenarioRootId,
            PlayerEntityTemplateId: playerTemplateId,
            PlayerEntityId: new EntityId("playerOne"),
            PlayerStart: new GridCoord(2, 1))));

        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Equal("alpha-smoke", materialization.ScenarioId);
        Assert.Equal(new EntityId("scenarioRoot"), materialization.ScenarioRootEntityId);
        Assert.Equal(new EntityId("playerOne"), materialization.PlayerEntityId);
        Assert.Equal(new PlaneId("scenarioRoot"), materialization.ScenarioPlaneId);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(2, 1)), materialization.PlayerLocation);
        Assert.Contains("Scenario: alpha-smoke (Alpha Smoke)", materialization.SetupLines);
        Assert.Contains("Player: Alpha Player playerOne at scenarioRoot(2,1), facing North, target none", materialization.SetupLines);
    }

    [Fact]
    public void AgentContentEditorApiMaterializeScenarioReportsAuthoringDiagnostics()
    {
        var api = AgentContentEditorApi.CreateNew();
        var scenarioRootId = AssertSuccess(api.CreateEntityTemplate("Blocked Alpha Room"));
        AssertSuccess(api.UpdateEntityTemplate(
            scenarioRootId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 2,
                InventoryHeight: 1,
                Weight: 100,
                CarryingCapacity: 100,
                Glyph: '#',
                Color: PresentationColor.Gray)));
        var blockerTemplateId = AssertSuccess(api.CreateEntityTemplate("Blocker"));
        AssertSuccess(api.PlaceCarriedEntity(scenarioRootId, new EntityId("blocker"), blockerTemplateId, new GridCoord(0, 0)));
        var playerTemplateId = AssertSuccess(api.CreateEntityTemplate("Blocked Player"));

        var missingRoot = AssertSuccess(api.MaterializeScenario(new AgentAlphaScenarioDefinition(
            "missing-root",
            "Missing Root",
            new EntityTemplateId("missingRoot"),
            playerTemplateId,
            new EntityId("player"),
            new GridCoord(0, 0))));
        var missingPlayer = AssertSuccess(api.MaterializeScenario(new AgentAlphaScenarioDefinition(
            "missing-player",
            "Missing Player",
            scenarioRootId,
            new EntityTemplateId("missingPlayer"),
            new EntityId("player"),
            new GridCoord(0, 0))));
        var invalidStart = AssertSuccess(api.MaterializeScenario(new AgentAlphaScenarioDefinition(
            "invalid-start",
            "Invalid Start",
            scenarioRootId,
            playerTemplateId,
            new EntityId("player"),
            new GridCoord(3, 0))));
        var occupiedStart = AssertSuccess(api.MaterializeScenario(new AgentAlphaScenarioDefinition(
            "occupied-start",
            "Occupied Start",
            scenarioRootId,
            playerTemplateId,
            new EntityId("player"),
            new GridCoord(0, 0))));

        Assert.Contains(missingRoot.ValidationDiagnostics, diagnostic => diagnostic.Contains("missing scenario root template missingRoot", StringComparison.Ordinal));
        Assert.Contains(missingPlayer.ValidationDiagnostics, diagnostic => diagnostic.Contains("missing player template missingPlayer", StringComparison.Ordinal));
        Assert.Contains(invalidStart.ValidationDiagnostics, diagnostic => diagnostic.Contains("player start scenarioRoot(3,0) is outside scenario plane", StringComparison.Ordinal));
        Assert.Contains(occupiedStart.ValidationDiagnostics, diagnostic => diagnostic.Contains("player start scenarioRoot(0,0) is occupied by blocker", StringComparison.Ordinal));
        Assert.Empty(missingRoot.RuntimeFailures);
        Assert.Empty(missingPlayer.RuntimeFailures);
        Assert.Empty(invalidStart.RuntimeFailures);
        Assert.Empty(occupiedStart.RuntimeFailures);
    }

    [Fact]
    public void AgentContentEditorApiPersistsAndMaterializesAlphaScenarioDefinitionById()
    {
        var api = AgentContentEditorApi.CreateNew();
        var scenarioRootId = AssertSuccess(api.CreateEntityTemplate("Persisted Alpha Room"));
        AssertSuccess(api.UpdateEntityTemplate(
            scenarioRootId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 3,
                InventoryHeight: 2,
                Weight: 100,
                CarryingCapacity: 100,
                Glyph: '#',
                Color: PresentationColor.Gray)));
        var playerTemplateId = AssertSuccess(api.CreateEntityTemplate("Persisted Player"));
        AssertSuccess(api.UpdateEntityTemplate(playerTemplateId, new AgentEntityTemplateUpdate(Weight: 1, CarryingCapacity: 5, Glyph: '@', Color: PresentationColor.Yellow)));

        AssertSuccess(api.UpsertScenario(new AgentAlphaScenarioDefinition(
            "persisted-alpha",
            "Persisted Alpha",
            scenarioRootId,
            playerTemplateId,
            new EntityId("persistedPlayer"),
            new GridCoord(1, 1))));

        var snapshot = api.GetDocumentSnapshot();
        Assert.Contains("scenarios:", snapshot.YamlPreview);
        Assert.Contains("persisted-alpha:", snapshot.YamlPreview);
        Assert.Contains("scenarioRootEntityTemplateId: persistedAlphaRoom", snapshot.YamlPreview);
        Assert.Contains("playerEntityTemplateId: persistedPlayer", snapshot.YamlPreview);
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("persisted-alpha"));

        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Equal("persisted-alpha", materialization.ScenarioId);
        Assert.Equal(new EntityId("persistedPlayer"), materialization.PlayerEntityId);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(1, 1)), materialization.PlayerLocation);
    }

    [Fact]
    public void AgentContentEditorApiValidatesPersistedAlphaScenarioDefinitions()
    {
        var api = AgentContentEditorApi.CreateNew();

        AssertSuccess(api.UpsertScenario(new AgentAlphaScenarioDefinition(
            "broken-alpha",
            "Broken Alpha",
            new EntityTemplateId("missingRoom"),
            new EntityTemplateId("missingPlayer"),
            new EntityId("player"),
            new GridCoord(0, 0))));

        var snapshot = api.GetDocumentSnapshot();

        Assert.False(snapshot.CanonicalValidation.IsValid);
        Assert.Contains(snapshot.CanonicalValidation.Errors, error => error.Contains("Scenario broken-alpha references missing scenario root template missingRoom", StringComparison.Ordinal));
        Assert.Contains(snapshot.CanonicalValidation.Errors, error => error.Contains("Scenario broken-alpha references missing player template missingPlayer", StringComparison.Ordinal));
    }

    [Fact]
    public void AgentContentEditorApiAuthorsCanonicalBehaviorChainWithHelper()
    {
        var api = AgentContentEditorApi.CreateNew();
        var ratId = AssertSuccess(api.CreateEntityTemplate("Helper Rat"));
        AssertSuccess(api.UpdateEntityTemplate(
            ratId,
            new AgentEntityTemplateUpdate(
                InventoryWidth: 1,
                InventoryHeight: 1,
                Weight: 1,
                CarryingCapacity: 3,
                Glyph: 'r',
                Color: PresentationColor.Green)));

        var planId = AssertSuccess(api.CreateMoveFacingPickupTargetBehavior("Helper Rat Behavior"));
        AssertSuccess(api.SetDefaultActionPlan(ratId, planId));

        var snapshot = api.GetDocumentSnapshot();

        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));
        Assert.Contains("behavior:", snapshot.YamlPreview);
        Assert.Contains("kind: MoveFacing", snapshot.YamlPreview);
        Assert.Contains("kind: PickupTarget", snapshot.YamlPreview);
        Assert.Contains("facing: West", snapshot.YamlPreview);
        Assert.DoesNotContain("primitive:", snapshot.YamlPreview);
        Assert.DoesNotContain("fallbackPlanId:", snapshot.YamlPreview);
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
}
