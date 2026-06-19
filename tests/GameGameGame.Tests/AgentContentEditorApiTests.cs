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
