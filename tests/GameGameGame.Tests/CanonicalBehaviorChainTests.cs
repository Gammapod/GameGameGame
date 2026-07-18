using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class CanonicalBehaviorChainTests
{
    [Fact]
    public void BehaviorChainRunsMoveFacingThenPickupTargetWithoutLinkedFallbackPlan()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("behavior-chain"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget)
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(TestWorld.SlimeId));
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(TestWorld.RockId));
        Assert.Equal(TestWorld.RockId, world.GetActionTarget(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Action Step MoveFacing"));
        Assert.True(TraceContains(result.Trace, "Action Step PickupTarget"));
        Assert.False(TraceContains(result.Trace, "Fallback plan"));
    }

    [Fact]
    public void TransformAdjacentToInventoryBehaviorUsesPickupSemantics()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = CreateBehaviorPlan("transform-adjacent-to-inventory", ActionPlanBehaviorStepKind.TransformAdjacentToInventory);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Action Step TransformAdjacentToInventory"));
        Assert.True(TraceContains(result.Trace, "Primitive PickupTarget"));
    }

    [Fact]
    public void BehaviorChainTraceFormatterSummarizesFallbackStateAndTerminalOutcome()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("behavior-chain"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget)
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, context);
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.Collection(
            summary,
            line => Assert.Equal("Plan behavior-chain: Success; consumedTurn=True; continuePlan=False", line),
            line => Assert.Equal("1. MoveFacing: Failure; reason=InvalidPlacement; fallback=continued", line),
            line => Assert.Equal("   reads: Facing=West", line),
            line => Assert.Equal("   writes: Target=rock", line),
            line => Assert.Equal("2. PickupTarget: Success; fallback=stopped", line),
            line => Assert.Equal("   reads: Target=rock", line),
            line => Assert.Equal("   results: picked up rock into first available inventory coordinate (0,0)", line),
            line => Assert.Equal("Terminal: succeeded; consumed turn", line));
    }

    [Fact]
    public void BehaviorChainStopsAfterFirstSuccessfulActionStep()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("behavior-chain"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget)
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Slime@world(0,1)", world.FormatEntityAddress(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Action Step MoveFacing"));
        Assert.False(TraceContains(result.Trace, "Action Step PickupTarget"));
    }

    private static ActionPlanDefinition CreateBehaviorPlan(string id, ActionPlanBehaviorStepKind stepKind) =>
        new(
            new ActionPlanId(id),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(stepKind)]));

    private static bool TraceContains(TraceNode trace, string label)
    {
        return trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
    }
}
