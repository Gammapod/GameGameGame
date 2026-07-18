using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class ActionStepCatalogCompatibilityTests
{
    [Theory]
    [InlineData(ActionPlanBehaviorStepKind.TurnLeft, "Turn Left")]
    [InlineData(ActionPlanBehaviorStepKind.TurnRight, "Turn Right")]
    [InlineData(ActionPlanBehaviorStepKind.ReverseFacing, "Reverse Facing")]
    [InlineData(ActionPlanBehaviorStepKind.AcquireNearestTarget, "Acquire Nearest Target")]
    public void ActionStepCatalogKeepsLegacyMetadataStepsForRuntimeCompatibility(ActionPlanBehaviorStepKind kind, string displayName)
    {
        var step = ActionStepCatalog.Get(kind);

        Assert.Equal(displayName, step.DisplayName);
        Assert.Equal(ActionStepAuthoringTier.Legacy, step.Tier);
        Assert.NotEmpty(step.Description);
    }

    [Theory]
    [InlineData(ActionPlanBehaviorStepKind.TurnLeft, Direction.North, Direction.West)]
    [InlineData(ActionPlanBehaviorStepKind.TurnLeft, Direction.West, Direction.South)]
    [InlineData(ActionPlanBehaviorStepKind.TurnRight, Direction.North, Direction.East)]
    [InlineData(ActionPlanBehaviorStepKind.TurnRight, Direction.East, Direction.South)]
    [InlineData(ActionPlanBehaviorStepKind.ReverseFacing, Direction.North, Direction.South)]
    [InlineData(ActionPlanBehaviorStepKind.ReverseFacing, Direction.East, Direction.West)]
    public void ActionStepCatalogDescribesTurnFacingMetadata(ActionPlanBehaviorStepKind kind, Direction from, Direction to)
    {
        var step = ActionStepCatalog.Get(kind);

        Assert.Contains(step.RequiredState, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);
        Assert.Contains(step.DefaultableState, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);
        Assert.Contains(step.StateWrites, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);

        var world = TestWorld.CreateWorld();
        var start = world.GetEntityLocation(TestWorld.PlayerId);
        world.SetActionFacing(TestWorld.PlayerId, from);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("turn-facing"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(kind)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(to, world.GetActionFacing(TestWorld.PlayerId));
        Assert.Equal(start, world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Null(world.GetActionTarget(TestWorld.PlayerId));
    }
}
