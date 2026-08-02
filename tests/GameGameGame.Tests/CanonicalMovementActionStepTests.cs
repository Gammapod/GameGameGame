using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class CanonicalMovementActionStepTests
{
    [Fact]
    public void BackstepMovesOppositeFacingConsumesTurnAndPreservesFacing()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("backstep"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Backstep)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.Equal(Direction.North, world.GetActionFacing(TestWorld.PlayerId));
        Assert.Null(world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. Backstep: Success; fallback=stopped");
        Assert.Contains(summary, line => line == "   reads: Facing=North");
        Assert.Contains(summary, line => line == "   results: moved South; preserved Facing=North");
        Assert.True(TraceContains(result.Trace, "Move South"));
        Assert.True(TraceContains(result.Trace, "Preserve Facing"));
    }

    [Fact]
    public void BackstepBlockedByEntityWritesTargetAndFallsThrough()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Aperture = 9 };
        world.SetActionFacing(TestWorld.PlayerId, Direction.South);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("backstep-then-wait"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Backstep),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget)
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,2)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.Equal(Direction.South, world.GetActionFacing(TestWorld.PlayerId));
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. Backstep: Failure; reason=InvalidPlacement; fallback=continued");
        Assert.Contains(summary, line => line == "   reads: Facing=South");
        Assert.Contains(summary, line => line == "   writes: Target=slime");
        Assert.True(TraceContains(result.Trace, "Action Step PickupTarget"));
    }

    [Fact]
    public void BackstepOutOfBoundsFailsWithoutMeaningfulTargetWrite()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0))));
        world.SetActionFacing(TestWorld.PlayerId, Direction.South);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("backstep-out-of-bounds"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Backstep)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(0,0)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.Equal(Direction.South, world.GetActionFacing(TestWorld.PlayerId));
        Assert.Null(world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. Backstep: Failure; reason=MoveOutOfBounds; fallback=stopped");
        Assert.Contains(summary, line => line == "   reads: Facing=South");
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void CanonicalMoveRelativeBackSetsFacingToActualMovedDirection()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("canonical-move-back"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Move, DirectionMode: ActionPlanMoveDirectionMode.Back)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.Equal(Direction.South, world.GetActionFacing(TestWorld.PlayerId));
        Assert.Null(world.GetActionTarget(TestWorld.PlayerId));
    }

    [Fact]
    public void CanonicalMoveBlockedByEntityDoesNotWriteTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.RockId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("canonical-move-blocked"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Move, DirectionMode: ActionPlanMoveDirectionMode.Forward)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,2)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.Equal(Direction.North, world.GetActionFacing(TestWorld.PlayerId));
        Assert.Equal(TestWorld.RockId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void CanonicalMoveDiagonalAllowsOneBlockedCorner()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("canonical-move-diagonal-one-corner"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Move, DirectionMode: ActionPlanMoveDirectionMode.NorthEast)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(2,1)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.Equal(Direction.NorthEast, world.GetActionFacing(TestWorld.PlayerId));
    }

    [Fact]
    public void CanonicalMoveDiagonalRejectsTwoBlockedCorners()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2))));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("canonical-move-diagonal-two-corners"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Move, DirectionMode: ActionPlanMoveDirectionMode.NorthEast)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,2)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.Null(world.GetActionFacing(TestWorld.PlayerId));
        Assert.Null(world.GetActionTarget(TestWorld.PlayerId));
    }

    private static bool TraceContains(TraceNode trace, string label)
    {
        return trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
    }
}
