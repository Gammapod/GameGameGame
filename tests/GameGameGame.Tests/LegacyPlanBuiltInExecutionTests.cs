using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class LegacyPlanBuiltInExecutionTests
{
    [Fact]
    public void BuiltInCanMoveCheckAndMoveEffectMoveActorUsingDirectionVariable()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.South));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("move-from-variable"),
            [new ActionPlanStep("move facing", [new CanMoveCheck("facing")], new MoveEffect("facing"), onFailure: null)]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "Can move facing"));
        Assert.True(TraceContains(result.Trace, "Move facing"));
    }

    [Fact]
    public void BuiltInCanMoveCheckAndMoveEffectMoveActorUsingCanonicalFacingSlot()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.South));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("move-from-facing-slot"),
            [new ActionPlanStep("move facing", [new CanMoveCheck()], new MoveEffect(), onFailure: null)]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "Read slot Facing"));
        Assert.True(TraceContains(result.Trace, "Can move Facing"));
        Assert.True(TraceContains(result.Trace, "Move Facing"));
    }

    [Fact]
    public void BuiltInCanMoveCheckFailureFallsThroughToSetVariableEffect()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.South));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("turn-around"),
            [
                new ActionPlanStep("try blocked move", [new CanMoveCheck("facing")], new MoveEffect("facing"), onFailure: null),
                new ActionPlanStep("turn north", [], new SetVariableEffect("facing", new DirectionPlanValue(Direction.North), consumesTurn: false, continuePlan: false), onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(TestWorld.SlimeId));
        Assert.True(context.TryGet<DirectionPlanValue>("facing", out var facing));
        Assert.Equal(Direction.North, facing.Value);
        Assert.True(TraceContains(result.Trace, "Set variable facing"));
    }

    [Fact]
    public void CallPlanEffectRunsNestedPlanWithSharedContextAndTrace()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        var childId = new ActionPlanId("child");
        var parent = new ActionPlanDefinition(new ActionPlanId("parent"), [new ActionPlanStep("call child", [], new CallPlanEffect(childId), onFailure: null)]);
        var child = new ActionPlanDefinition(
            childId,
            [new ActionPlanStep("set nested variable", [], new SetVariableEffect("facing", new DirectionPlanValue(Direction.East), consumesTurn: true, continuePlan: false), onFailure: null)]);
        var interpreter = new ActionPlanInterpreter(new MovementService(), new Dictionary<ActionPlanId, ActionPlanDefinition> { [parent.Id] = parent, [child.Id] = child });

        var result = interpreter.Execute(world, TestWorld.PlayerId, parent, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.True(context.TryGet<DirectionPlanValue>("facing", out var facing));
        Assert.Equal(Direction.East, facing.Value);
        Assert.True(TraceContains(result.Trace, "Call plan child"));
        Assert.True(TraceContains(result.Trace, "Plan child"));
    }

    [Fact]
    public void CallPlanEffectFailsWithTraceWhenDepthGuardIsExceeded()
    {
        var world = TestWorld.CreateWorld();
        var recursiveId = new ActionPlanId("recursive");
        var recursive = new ActionPlanDefinition(recursiveId, [new ActionPlanStep("call self", [], new CallPlanEffect(recursiveId), onFailure: null)]);
        var interpreter = new ActionPlanInterpreter(new MovementService(), new Dictionary<ActionPlanId, ActionPlanDefinition> { [recursive.Id] = recursive }, maxCallDepth: 2);

        var result = interpreter.Execute(world, TestWorld.PlayerId, recursive, new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.Equal(TraceStatus.Failure, result.Trace.Status);
        Assert.True(TraceContains(result.Trace, "Plan call depth exceeded"));
    }

    [Fact]
    public void BlockingEntityCheckWritesTargetWhenFacingBlockedEntity()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.South));
        var result = new BlockingEntityCheck("facing", "target").Evaluate(world, TestWorld.SlimeId, context, new MovementService());

        Assert.True(result.Passed);
        Assert.Equal(TraceStatus.Success, result.Trace.Status);
        Assert.True(result.VariableWrites.TryGetValue("target", out var target));
        Assert.Equal(TestWorld.PlayerId, Assert.IsType<EntityPlanValue>(target).Value);
        Assert.Contains("player", result.Trace.Detail);
    }

    [Fact]
    public void BlockingEntityCheckWritesCanonicalTargetWhenCanonicalFacingIsBlocked()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.South));
        var result = new BlockingEntityCheck().Evaluate(world, TestWorld.SlimeId, context, new MovementService());

        Assert.True(result.Passed);
        Assert.Equal(TraceStatus.Success, result.Trace.Status);
        Assert.Empty(result.VariableWrites);
        Assert.NotNull(result.SlotWrites);
        Assert.True(result.SlotWrites.TryGetValue(ActionPlanSlot.Target, out var target));
        Assert.Equal(TestWorld.PlayerId, Assert.IsType<EntityPlanValue>(target).Value);
        Assert.Contains("Target=player", result.Trace.Detail);
    }

    [Fact]
    public void BlockingEntityCheckFailsWithoutWritingTargetWhenNoBlockerExists()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.North));
        var result = new BlockingEntityCheck("facing", "target").Evaluate(world, TestWorld.SlimeId, context, new MovementService());

        Assert.False(result.Passed);
        Assert.Equal(TraceStatus.Failure, result.Trace.Status);
        Assert.Empty(result.VariableWrites);
        Assert.False(context.TryGet<EntityPlanValue>("target", out _));
    }

    [Fact]
    public void CanPickupCheckPassesForBoundAdjacentCarryableTarget()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set("target", new EntityPlanValue(TestWorld.RockId));
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1)));

        var result = new CanPickupCheck("target", new GridCoord(0, 0)).Evaluate(world, TestWorld.SlimeId, context, movement);

        Assert.True(result.Passed);
        Assert.Equal(TraceStatus.Success, result.Trace.Status);
        Assert.True(TraceContains(result.Trace, "Pickup rock -> slime(0,0)"));
    }

    [Fact]
    public void PickupEffectPicksUpBoundTargetIntoActorInventory()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set("target", new EntityPlanValue(TestWorld.RockId));
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("pickup-bound-target"),
            [new ActionPlanStep("pickup target", [new CanPickupCheck("target", new GridCoord(0, 0))], new PickupEffect("target", new GridCoord(0, 0)), onFailure: null)]);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(TestWorld.RockId));
        Assert.True(TraceContains(result.Trace, "Pickup target"));
    }

    [Fact]
    public void CanPickupCheckAndPickupEffectUseCanonicalTargetSlot()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Target, new EntityPlanValue(TestWorld.RockId));
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("pickup-canonical-target"),
            [new ActionPlanStep("pickup target", [new CanPickupCheck(new GridCoord(0, 0))], new PickupEffect(new GridCoord(0, 0)), onFailure: null)]);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(TestWorld.RockId));
        Assert.True(TraceContains(result.Trace, "Read slot Target"));
        Assert.True(TraceContains(result.Trace, "Pickup Target"));
    }

    [Fact]
    public void ReverseDirectionEffectUpdatesDirectionVariableWithoutConsumingTurn()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.West));
        var result = new ReverseDirectionEffect("facing", consumesTurn: false, continuePlan: false).Apply(world, TestWorld.SlimeId, context, new MovementService());

        Assert.True(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.True(context.TryGet<DirectionPlanValue>("facing", out var facing));
        Assert.Equal(Direction.East, facing.Value);
        Assert.True(TraceContains(result.Trace, "Set variable facing"));
    }

    [Fact]
    public void ReverseDirectionEffectUpdatesCanonicalFacingSlotWithoutConsumingTurn()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        var result = new ReverseDirectionEffect(consumesTurn: false, continuePlan: false).Apply(world, TestWorld.SlimeId, context, new MovementService());

        Assert.True(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.True(context.TryGet<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing));
        Assert.Equal(Direction.East, facing.Value);
        Assert.True(TraceContains(result.Trace, "Read slot Facing"));
        Assert.True(TraceContains(result.Trace, "Set slot Facing"));
    }

    [Fact]
    public void CallPlanEffectSharesCanonicalSlotsWithNestedPlan()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        var childId = new ActionPlanId("child-canonical");
        var parent = new ActionPlanDefinition(new ActionPlanId("parent-canonical"), [new ActionPlanStep("call child", [], new CallPlanEffect(childId), onFailure: null)]);
        var child = new ActionPlanDefinition(childId, [new ActionPlanStep("reverse facing", [], new ReverseDirectionEffect(consumesTurn: true, continuePlan: false), onFailure: null)]);
        var interpreter = new ActionPlanInterpreter(new MovementService(), new Dictionary<ActionPlanId, ActionPlanDefinition> { [child.Id] = child });

        var result = interpreter.Execute(world, TestWorld.PlayerId, parent, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.True(context.TryGet<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing));
        Assert.Equal(Direction.East, facing.Value);
        Assert.True(TraceContains(result.Trace, "Call plan child-canonical"));
        Assert.True(TraceContains(result.Trace, "Set slot Facing"));
    }

    [Fact]
    public void WaitEffectConsumesTurnWithoutChangingWorldPosition()
    {
        var world = TestWorld.CreateWorld();
        var plan = new ActionPlanDefinition(new ActionPlanId("wait"), [new ActionPlanStep("wait", [], new WaitEffect(), onFailure: null)]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.SlimeId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Wait"));
    }

    private static bool TraceContains(TraceNode trace, string label)
    {
        return trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
    }
}
