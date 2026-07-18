using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class TargetRelativeMovementActionStepTests
{
    [Fact]
    public void SeekTargetBlockedByIncidentalEntityPreservesGoalTarget()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 4))));
        world.SetActionTarget(TestWorld.SlimeId, TestWorld.RockId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("seek-blocked"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.SeekTarget)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.RockId, world.GetActionTarget(TestWorld.SlimeId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 1)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.Contains(summary, line => line == "1. SeekTarget: Failure; reason=InvalidPlacement; fallback=stopped");
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void FleeTargetMovesAwayFromTargetAndPreservesTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("flee-target"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.FleeTarget)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 3)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. FleeTarget: Success; fallback=stopped");
        Assert.Contains(summary, line => line == "   reads: Target=slime");
        Assert.Contains(summary, line => line.Contains("moved South away from slime; distance 1->2", StringComparison.Ordinal));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void FleeTargetSkipsBlockedIncreasingCandidateAndReportsBlocker()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 3))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("flee-target-blocked-candidate"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.FleeTarget)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 2)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Contains(summary, line => line.Contains("moved West away from slime; distance 1->2", StringComparison.Ordinal));
        Assert.True(TraceDetailContains(result.Trace, "South blocked"));
    }

    [Fact]
    public void FleeTargetFallsThroughWhenNoValidIncreasingMoveExists()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("flee-target-trapped-by-corner"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.FleeTarget)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. FleeTarget: Failure; reason=InvalidPlacement; fallback=stopped");
        Assert.Contains(summary, line => line.Contains("no valid distance-increasing flee step", StringComparison.Ordinal));
        Assert.True(TraceDetailContains(result.Trace, "North blocked"));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void FleeTargetInvalidTargetFallsThroughAndPreservesTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.PlayerId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("flee-target-self"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.FleeTarget)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.PlayerId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. FleeTarget: Failure; reason=TargetIsActor; fallback=stopped");
        Assert.Contains(summary, line => line.Contains("FleeTarget cannot flee self", StringComparison.Ordinal));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    private static bool TraceDetailContains(TraceNode trace, string detail)
    {
        return trace.Detail?.Contains(detail, StringComparison.Ordinal) == true
            || trace.Children.Any(child => TraceDetailContains(child, detail));
    }
}
