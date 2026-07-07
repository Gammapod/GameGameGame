using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class CoreInvariantTests
{
    [Fact]
    public void EntityLocationsAreRepresentedByNodeOccupancy()
    {
        var world = TestWorld.CreateWorld();
        var playerLocation = world.GetEntityLocation(TestWorld.PlayerId);
        var playerNodeId = world.GetNodeId(playerLocation);

        Assert.Equal(world.Entities[TestWorld.PlayerId].OccupiedNodeId, playerNodeId);
        Assert.Equal(TestWorld.PlayerId, world.Occupancy[playerNodeId]);
    }

    [Fact]
    public void MovementCannotPlaceEntityOnOccupiedNode()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var occupiedLocation = world.GetEntityLocation(TestWorld.PlayerId);
        var originalRockLocation = world.GetEntityLocation(TestWorld.RockId);

        var placed = movement.TryPlace(world, TestWorld.RockId, occupiedLocation);

        Assert.False(placed);
        Assert.Equal(originalRockLocation, world.GetEntityLocation(TestWorld.RockId));
        Assert.Equal(TestWorld.PlayerId, world.GetOccupant(occupiedLocation));
    }

    [Fact]
    public void ZeroInventoryDimensionMakesInventoryUnusable()
    {
        var zeroWidth = new Entity(new EntityId("zeroWidth"), "Zero Width", new NodeId("node"), 0, 1, 0, 0);
        var zeroHeight = new Entity(new EntityId("zeroHeight"), "Zero Height", new NodeId("node"), 1, 0, 0, 0);

        Assert.False(zeroWidth.HasUsableInventory);
        Assert.False(zeroHeight.HasUsableInventory);
    }

    [Fact]
    public void PickupFailsWhenTargetBulkExceedsAperture()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 2 };
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Bulk = 3 };
        var action = new PickupAction(TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));

        var evaluation = action.Evaluate(world, TestWorld.PlayerId, movement);

        Assert.False(evaluation.CanExecute);
        Assert.Equal(FailureReason.ApertureBlocked, evaluation.Trace.Reason);
    }

    [Fact]
    public void TurnServiceOnlySchedulesEntitiesWithActionPlans()
    {
        var world = TestWorld.CreateWorld();
        var slimePlan = new RecordingEntityActionPlan(new WaitAction());
        var turns = new TurnService(
            new MovementService(),
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [TestWorld.SlimeId] = slimePlan
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal(1, slimePlan.TurnsPlanned);
        Assert.DoesNotContain(world.LastTrace!.Children, child => child.Label.Contains("Player"));
    }

    [Fact]
    public void ActorUsesBehaviorProviderDefaultPlanWhenOverrideIsAssigned()
    {
        var world = TestWorld.CreateWorld();
        var actorPlan = new RecordingEntityActionPlan(new RecordingIntent("actor plan"));
        var providerPlan = new RecordingEntityActionPlan(new RecordingIntent("provider plan"));
        world.SetBehaviorProvider(TestWorld.SlimeId, TestWorld.RockId);
        var turns = new TurnService(
            new MovementService(),
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [TestWorld.SlimeId] = actorPlan,
                [TestWorld.RockId] = providerPlan
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal(0, actorPlan.TurnsPlanned);
        Assert.Equal(1, providerPlan.TurnsPlanned);
        Assert.Equal(TestWorld.SlimeId, providerPlan.LastPlannedFor);
        Assert.Contains(world.LastTrace!.Children, child => child.Label.Contains("Slime") && TraceContains(child, "provider plan"));
    }

    [Fact]
    public void BehaviorProviderIsNotScheduledIndependentlyWhileAssigned()
    {
        var world = TestWorld.CreateWorld();
        var actorPlan = new RecordingEntityActionPlan(new RecordingIntent("actor plan"));
        var providerPlan = new RecordingEntityActionPlan(new RecordingIntent("provider plan"));
        world.SetBehaviorProvider(TestWorld.SlimeId, TestWorld.RockId);
        var turns = new TurnService(
            new MovementService(),
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [TestWorld.SlimeId] = actorPlan,
                [TestWorld.RockId] = providerPlan
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal(1, providerPlan.TurnsPlanned);
        Assert.DoesNotContain(world.LastTrace!.Children, child => child.Label.Contains("Rock"));
    }

    [Fact]
    public void RemovingBehaviorProviderRestoresActorDefaultPlan()
    {
        var world = TestWorld.CreateWorld();
        var actorPlan = new RecordingEntityActionPlan(new RecordingIntent("actor plan"));
        var providerPlan = new RecordingEntityActionPlan(new RecordingIntent("provider plan"));
        var turns = new TurnService(
            new MovementService(),
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [TestWorld.SlimeId] = actorPlan,
                [TestWorld.RockId] = providerPlan
            });
        world.SetBehaviorProvider(TestWorld.SlimeId, TestWorld.RockId);
        turns.AdvanceAfterPlayerTurn(world);

        world.ClearBehaviorProvider(TestWorld.SlimeId);
        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal(1, actorPlan.TurnsPlanned);
        Assert.Contains(world.LastTrace!.Children, child => child.Label.Contains("Slime") && TraceContains(child, "actor plan"));
    }

    [Fact]
    public void BehaviorOverridePreservesActorTargetingAndActionState()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.SlimeId, Direction.East);
        world.SetActionFacing(TestWorld.RockId, Direction.West);
        var providerPlan = new RecordingEntityActionPlan(new ReadFacingIntent(Direction.East));
        world.SetBehaviorProvider(TestWorld.SlimeId, TestWorld.RockId);
        var turns = new TurnService(
            new MovementService(),
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [TestWorld.RockId] = providerPlan
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal(TestWorld.SlimeId, providerPlan.LastPlannedFor);
        Assert.True(world.LastTurnReport!.Actions.Single().Succeeded);
    }

    [Fact]
    public void TurnServiceUpdatesFacingAfterSuccessfulDirectionalMovement()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var turns = new TurnService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        var acted = turns.ResolvePlan(world, TestWorld.PlayerId, PlannedActionPlan.Single(new MoveAction(Direction.East)));

        Assert.True(acted);
        Assert.Equal(Direction.East, world.GetActionFacing(TestWorld.PlayerId));
    }

    [Fact]
    public void TurnServiceDoesNotUpdateFacingAfterFailedDirectionalMovement()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var turns = new TurnService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        var acted = turns.ResolvePlan(world, TestWorld.PlayerId, PlannedActionPlan.Single(new MoveAction(Direction.North)));

        Assert.False(acted);
        Assert.Equal(Direction.North, world.GetActionFacing(TestWorld.PlayerId));
    }

    private static bool TraceContains(TraceNode trace, string label)
    {
        return trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
    }

    private sealed class RecordingEntityActionPlan(IActionIntent action) : IEntityActionPlan
    {
        public int TurnsPlanned { get; private set; }

        public EntityId? LastPlannedFor { get; private set; }

        public PlannedActionPlan PlanTurn(WorldState world, EntityId entityId, MovementService movement)
        {
            TurnsPlanned++;
            LastPlannedFor = entityId;

            return PlannedActionPlan.Single(action);
        }
    }

    private sealed class RecordingIntent(string label) : IActionIntent
    {
        public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement) =>
            new(true, TraceNode.Success(label));

        public void Execute(WorldState world, EntityId actorId, MovementService movement)
        {
        }

        public ActionResolution Resolve(WorldState world, EntityId actorId, MovementService movement) =>
            new(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success(label));
    }

    private sealed class ReadFacingIntent(Direction expectedFacing) : IActionIntent
    {
        public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement) =>
            new(true, TraceNode.Success("read actor facing"));

        public void Execute(WorldState world, EntityId actorId, MovementService movement)
        {
        }

        public ActionResolution Resolve(WorldState world, EntityId actorId, MovementService movement)
        {
            var actualFacing = world.GetActionFacing(actorId);
            return actualFacing == expectedFacing
                ? new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success("read actor facing", actualFacing.ToString()))
                : new ActionResolution(false, ConsumesTurn: true, ContinuePlan: false, TraceNode.Failure("read actor facing", FailureReason.None, actualFacing?.ToString() ?? "missing"));
        }
    }
}
