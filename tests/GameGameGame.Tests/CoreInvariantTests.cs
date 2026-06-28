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
    public void TraversingRecursiveInventoryWeightIsCycleSafe()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var weight = new WeightService();
        movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));

        var totalWeight = weight.GetTotalWeight(world, TestWorld.PlayerId);
        var trace = weight.TraceTotalWeight(world, TestWorld.PlayerId);

        Assert.Equal(13, totalWeight);
        Assert.True(TraceContains(trace, "Player already counted"));
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
    public void MissingInventoryPlaneContributesNoCarriedWeight()
    {
        var world = TestWorld.CreateWorld();
        var weight = new WeightService();

        Assert.False(world.Entities[TestWorld.RockId].HasUsableInventory);
        Assert.Equal(0, weight.GetCarriedWeight(world, TestWorld.RockId));
    }

    [Fact]
    public void OwnWeightDoesNotCountAgainstOwnCarryingCapacity()
    {
        var world = TestWorld.CreateWorld();
        var weight = new WeightService();

        Assert.True(world.Entities[TestWorld.PlayerId].Weight > world.Entities[TestWorld.PlayerId].CarryingCapacity);
        Assert.Equal(0, weight.GetCarriedWeight(world, TestWorld.PlayerId));
    }

    [Fact]
    public void RecursiveCarriedWeightIncludesNestedInventoryContents()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var weight = new WeightService();
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));

        Assert.Equal(6, weight.GetTotalWeight(world, TestWorld.SlimeId));
        Assert.Equal(6, weight.GetCarriedWeight(world, TestWorld.PlayerId));
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
