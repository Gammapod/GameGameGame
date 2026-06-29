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

        public PlannedActionPlan PlanTurn(WorldState world, EntityId entityId, MovementService movement)
        {
            TurnsPlanned++;

            return PlannedActionPlan.Single(action);
        }
    }
}
