using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class TargetingActionStepTests
{
    [Fact]
    public void TargetPathMoveAdjacentFallsThroughAndPreservesTargetForDestroyTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("target-path-then-destroy"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TargetPathMove, PathMode: ActionPlanTargetPathMode.SeekAdjacency),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DestroyTarget)
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.False(world.Entities.ContainsKey(TestWorld.SlimeId));
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. TargetPathMove: Failure; reason=TargetNotAdjacent; fallback=continued");
        Assert.Contains(summary, line => line == "2. DestroyTarget: Success; fallback=stopped");
    }

    [Fact]
    public void TargetConsumingBehaviorDefaultsToTargetSlotOne()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, slot: 1, TestWorld.SlimeId);
        world.SetActionTarget(TestWorld.PlayerId, slot: 2, TestWorld.RockId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("destroy-default-target"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DestroyTarget)
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.False(world.Entities.ContainsKey(TestWorld.SlimeId));
        Assert.True(world.Entities.ContainsKey(TestWorld.RockId));
    }

    [Fact]
    public void TargetConsumingBehaviorCanReadExplicitTargetSlot()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, slot: 1, TestWorld.RockId);
        world.SetActionTarget(TestWorld.PlayerId, slot: 2, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("destroy-slot-two-target"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DestroyTarget, TargetSlot: 2)
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.False(world.Entities.ContainsKey(TestWorld.SlimeId));
        Assert.True(world.Entities.ContainsKey(TestWorld.RockId));
        Assert.Equal(TestWorld.RockId, world.GetActionTarget(TestWorld.PlayerId, slot: 1));
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId, slot: 2));
    }

    [Fact]
    public void TargetConsumingBehaviorCanReadTargetLabelFromExecutingActor()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, slot: 1, TestWorld.RockId);
        world.SetActionTarget(TestWorld.PlayerId, label: "fears", TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("destroy-feared-target"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DestroyTarget, TargetLabel: "fears")
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.False(world.Entities.ContainsKey(TestWorld.SlimeId));
        Assert.True(world.Entities.ContainsKey(TestWorld.RockId));
        Assert.Equal(TestWorld.RockId, world.GetActionTarget(TestWorld.PlayerId, slot: 1));
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId, label: "fears"));
    }

    [Fact]
    public void TargetConsumingBehaviorFailsWhenTargetLabelHasNoCurrentTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, slot: 1, TestWorld.RockId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("destroy-missing-feared-target"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DestroyTarget, TargetLabel: "fears")
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(world.Entities.ContainsKey(TestWorld.RockId));
        Assert.True(TraceContainsDetail(result.Trace, "target label fears"));
    }

    private static bool TraceContainsDetail(TraceNode node, string detail) =>
        (node.Detail?.Contains(detail, StringComparison.OrdinalIgnoreCase) ?? false)
        || node.Children.Any(child => TraceContainsDetail(child, detail));
}
