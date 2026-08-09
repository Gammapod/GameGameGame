using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed partial class PrototypeActionStepReferenceTests
{
    // Quarantined prototype/legacy Action Step reference coverage.
    // Retired coordinate target movement steps were removed in Phase 6; graph-native
    // TargetPathMove coverage now owns target-relative movement behavior.

    [Fact]
    public void PushFacingMovesBlockerAndActorAndConsumesTurn()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("push-facing"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PushFacing)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 1)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 0)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Action Step PushFacing"));
    }

    [Fact]
    public void DestroyTargetRecursivelyRemovesTargetInventoryAndContainedEntities()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("destroy-target"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DestroyTarget)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.False(world.Entities.ContainsKey(TestWorld.SlimeId));
        Assert.False(world.Entities.ContainsKey(TestWorld.RockId));
        Assert.False(world.Planes.ContainsKey(TestWorld.SlimeInventoryPlaneId));
        Assert.DoesNotContain(TestWorld.SlimeId, world.Occupancy.Values);
        Assert.DoesNotContain(TestWorld.RockId, world.Occupancy.Values);
        Assert.True(TraceContains(result.Trace, "Action Step DestroyTarget"));
    }

    [Fact]
    public void DestroyTargetFailsForMergedLayerContributingOwner()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("shared-interior"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(3, 0))
            ]));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("destroy-target-merged-contributor"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DestroyTarget)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(world.Entities.ContainsKey(TestWorld.SlimeId));
        Assert.True(world.Planes.ContainsKey(TestWorld.SlimeInventoryPlaneId));
        Assert.True(TraceHasReason(result.Trace, FailureReason.InventoryPolicyBlocked));
        Assert.True(TraceDetailContains(result.Trace, "contributes to merged inventory layer shared-interior"));
    }

    [Fact]
    public void CreateFacingCreatesPlaceholderRockEntityInFacingDirection()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("create-facing"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.CreateFacing)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        var created = world.GetOccupant(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)));
        Assert.NotNull(created);
        Assert.Equal("Placeholder Rock", world.Entities[created!.Value].Name);
        Assert.True(TraceContains(result.Trace, "Action Step CreateFacing"));
    }

    [Fact]
    public void TurnFacingTraceFormatterSummarizesFacingReadAndWrite()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("turn-left"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TurnLeft)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.Collection(
            summary,
            line => Assert.Equal("Plan turn-left: Success; consumedTurn=True; continuePlan=False", line),
            line => Assert.Equal("1. TurnLeft: Success; fallback=stopped", line),
            line => Assert.Equal("   reads: Facing=North", line),
            line => Assert.Equal("   writes: Facing=West", line),
            line => Assert.Equal("Terminal: succeeded; consumed turn", line));
    }

    [Fact]
    public void EmptyBehaviorChainResolvesAsNoTurn()
    {
        var world = TestWorld.CreateWorld();
        var plan = new ActionPlanDefinition(
            new ActionPlanId("empty-behavior"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.SlimeId,
            plan,
            new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.False(result.ContinuePlan);
        Assert.Equal(TraceStatus.Success, result.Trace.Status);
        Assert.Contains("no action steps", result.Trace.Detail);
    }

    [Fact]
    public void PickupTargetPrimitiveDefaultsMissingTargetToSelf()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        var plan = new ActionPlanDefinition(
            new ActionPlanId("pickup-self"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget)
            ]));

        _ = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.SlimeId,
            plan,
            context);

        Assert.True(context.TryGet<EntityPlanValue>(ActionPlanSlot.Target, out var target));
        Assert.Equal(TestWorld.SlimeId, target.Value);
    }

    [Fact]
    public void InterpretedEntityActionPlanCanBeScheduledByTurnService()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.West));
        var (wandering, _, registry) = CreateWanderingPlanDefinitions();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [TestWorld.SlimeId] = new InterpretedEntityActionPlan(wandering, context, registry)
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Slime@world(0,1)", world.FormatEntityAddress(TestWorld.SlimeId));
        Assert.True(TraceContains(world.LastTrace!, "Plan wandering"));
        Assert.True(TraceContains(world.LastTrace!, "Move facing"));
    }

    [Fact]
    public void InterpretedWanderingPlanCallsNestedPickupPlanForBlockingCarryableTarget()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Bulk = 4 };
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.West));
        var (wandering, _, registry) = CreateWanderingPlanDefinitions();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [TestWorld.SlimeId] = new InterpretedEntityActionPlan(wandering, context, registry)
            });
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1)));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(TestWorld.SlimeId));
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(TestWorld.RockId));
        Assert.True(TraceContains(world.LastTrace!, "Call plan handleBlocker"));
        Assert.True(TraceContains(world.LastTrace!, "Plan handleBlocker"));
        Assert.True(context.TryGet<EntityPlanValue>("target", out var target));
        Assert.Equal(TestWorld.RockId, target.Value);
    }
}
