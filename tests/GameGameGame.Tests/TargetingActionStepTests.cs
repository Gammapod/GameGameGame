using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class TargetingActionStepTests
{
    [Fact]
    public void AcquireNearestTargetSelectsNearestSamePlaneTargetAndWritesTarget()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 3))));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("acquire-nearest"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.AcquireNearestTarget)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "   writes: Target=slime");
        Assert.Contains(summary, line => line.Contains("distance=1", StringComparison.Ordinal));
        Assert.Contains(summary, line => line.Contains("tieBreak=row-major", StringComparison.Ordinal));
    }

    [Fact]
    public void AcquireNearestTargetFallsThroughWithoutOverwritingWhenNoCandidateExists()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.RockId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("acquire-none"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.AcquireNearestTarget)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.RockId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line.Contains("no same-plane target found", StringComparison.Ordinal));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void AcquireNearestTargetContinuesToSeekTargetInSameTurn()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 1))));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("acquire-then-seek"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.AcquireNearestTarget),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.SeekTarget)
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.RockId, world.GetActionTarget(TestWorld.SlimeId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 1)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.Contains(summary, line => line == "1. AcquireNearestTarget: Success; fallback=continued");
        Assert.Contains(summary, line => line == "2. SeekTarget: Success; fallback=stopped");
        Assert.Contains(summary, line => line.Contains("moved East toward rock", StringComparison.Ordinal));
    }

    [Fact]
    public void SeekTargetAdjacentFallsThroughAndPreservesTargetForDestroyTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("seek-then-destroy"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.SeekTarget),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DestroyTarget)
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.False(world.Entities.ContainsKey(TestWorld.SlimeId));
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. SeekTarget: Failure; reason=TargetNotAdjacent; fallback=continued");
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
