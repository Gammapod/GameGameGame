using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class PrimitiveActionPlanInterpreterTests
{
    [Fact]
    public void PrimitiveBackedPlanWithoutFallbackTerminatesRootTurnWhenPrimitiveFails()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("move-into-wall"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.False(result.ContinuePlan);
        Assert.True(TraceContains(result.Trace, "Primitive MoveFacing"));
    }

    [Fact]
    public void PrimitiveMoveFacingMovesUsingPersistentActorFacing()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.South);
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("move-facing"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "Read slot Facing"));
        Assert.True(TraceContains(result.Trace, "Primitive MoveFacing"));
    }

    [Fact]
    public void PrimitiveMoveFacingStoresBlockingEntityAsPersistentTargetBeforeFallback()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var fallback = new ActionPlanDescriptor(
            new ActionPlanId("wait"),
            [new ActionPlanStepDescriptor("wait", [], PlanEffectDescriptor.Wait(), OnFailure: null)]);
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("move-then-wait"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing, fallback.Id));
        var registry = new Dictionary<ActionPlanId, ActionPlanDefinition>
        {
            [fallback.Id] = fallback.Materialize()
        };

        var result = new ActionPlanInterpreter(new MovementService(), registry).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "Set slot Target"));
        Assert.True(TraceContains(result.Trace, "Wait"));
    }

    [Fact]
    public void PrimitiveBackedPlanUsesExplicitFallbackWhenPrimitiveFails()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var fallback = new ActionPlanDescriptor(
            new ActionPlanId("wait"),
            [
                new ActionPlanStepDescriptor(
                    "wait",
                    [],
                    PlanEffectDescriptor.Wait(),
                    OnFailure: null)
            ]);
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("move-then-wait"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing, fallback.Id));
        var registry = new Dictionary<ActionPlanId, ActionPlanDefinition>
        {
            [fallback.Id] = fallback.Materialize()
        };

        var result = new ActionPlanInterpreter(new MovementService(), registry).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.True(TraceContains(result.Trace, "Call plan wait"));
        Assert.True(TraceContains(result.Trace, "Wait"));
    }

    [Fact]
    public void PrimitiveFallbackCyclesUsePlanCallDepthGuard()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var first = new ActionPlanDescriptor(
            new ActionPlanId("first"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing, new ActionPlanId("second")));
        var second = new ActionPlanDescriptor(
            new ActionPlanId("second"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing, new ActionPlanId("first")));
        var registry = new Dictionary<ActionPlanId, ActionPlanDefinition>
        {
            [first.Id] = first.Materialize(),
            [second.Id] = second.Materialize()
        };

        var result = new ActionPlanInterpreter(new MovementService(), registry, maxCallDepth: 2).Execute(
            world,
            TestWorld.PlayerId,
            first.Materialize(),
            new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.True(TraceContains(result.Trace, "Plan call depth exceeded"));
    }

    [Fact]
    public void PrimitivePickupTargetPicksUpPersistentActorTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("pickup-target"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PickupTarget));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Primitive PickupTarget"));
        Assert.True(TraceContains(result.Trace, "Read slot Target"));
    }

    [Fact]
    public void PrimitivePickupTargetUsesFirstAvailableInventoryCoordinateRowMajor()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 20 };
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2))));
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("pickup-target"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PickupTarget));
        var interpreter = new ActionPlanInterpreter(movement);

        world.SetActionTarget(TestWorld.PlayerId, TestWorld.RockId);
        var first = interpreter.Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var second = interpreter.Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.True(TraceContains(second.Trace, "Pickup slime -> player(0,0)"));
        Assert.True(TraceDetailContains(second.Trace, "first available inventory coordinate (1,0)"));
    }

    [Fact]
    public void PrimitiveMoveFacingCanFallbackToPickupTargetUsingBlockedEntityTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var pickup = new ActionPlanDescriptor(
            new ActionPlanId("pickupTarget"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PickupTarget));
        var move = new ActionPlanDescriptor(
            new ActionPlanId("moveThenPickup"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing, pickup.Id));
        var registry = new Dictionary<ActionPlanId, ActionPlanDefinition>
        {
            [pickup.Id] = pickup.Materialize()
        };

        var result = new ActionPlanInterpreter(new MovementService(), registry).Execute(
            world,
            TestWorld.PlayerId,
            move.Materialize(),
            new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Set slot Target"));
        Assert.True(TraceContains(result.Trace, "Primitive PickupTarget"));
    }

    [Fact]
    public void PrimitivePickupTargetWithoutFallbackTerminatesRootTurnWhenPickupFails()
    {
        var world = TestWorld.CreateWorld();
        Assert.True(new MovementService().TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.RockId);
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("pickup-target"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PickupTarget));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.False(result.ContinuePlan);
        Assert.True(TraceContains(result.Trace, "Primitive PickupTarget"));
    }

    private static bool TraceContains(TraceNode trace, string label)
    {
        return trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
    }

    private static bool TraceDetailContains(TraceNode trace, string detail)
    {
        return trace.Detail?.Contains(detail, StringComparison.Ordinal) == true
            || trace.Children.Any(child => TraceDetailContains(child, detail));
    }
}
