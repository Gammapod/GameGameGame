using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class CoreRelocationTests
{
    [Fact]
    public void RelocationMovesEntityToExplicitPlaneCoordinate()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var destination = MovementDestination.Plane(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0)));

        var moved = movement.TryRelocate(world, TestWorld.RockId, destination);

        Assert.True(moved);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void RelocationMovesEntityToInventorySlot()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var destination = MovementDestination.InventorySlot(TestWorld.PlayerId, new GridCoord(0, 0));

        var moved = movement.TryRelocate(world, TestWorld.RockId, destination);

        Assert.True(moved);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void RelocationMovesEntityToAdjacentDestination()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var destination = MovementDestination.AdjacentTo(TestWorld.PlayerId, Direction.East);

        var moved = movement.TryRelocate(world, TestWorld.PlayerId, destination);

        Assert.True(moved);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), world.GetEntityLocation(TestWorld.PlayerId));
    }

    [Fact]
    public void RelocationEvaluationReportsOccupiedDestinationWithoutMovingEntity()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var originalLocation = world.GetEntityLocation(TestWorld.RockId);
        var destination = MovementDestination.Plane(world.GetEntityLocation(TestWorld.PlayerId));

        var evaluation = movement.EvaluateRelocation(world, TestWorld.RockId, destination);
        var moved = movement.TryRelocate(world, TestWorld.RockId, destination);

        Assert.False(evaluation.CanRelocate);
        Assert.Equal(FailureReason.InvalidPlacement, evaluation.Trace.Reason);
        Assert.False(moved);
        Assert.Equal(originalLocation, world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void RelocationEvaluationReportsInvalidInventoryDestination()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var destination = MovementDestination.InventorySlot(TestWorld.RockId, new GridCoord(0, 0));

        var evaluation = movement.EvaluateRelocation(world, TestWorld.SlimeId, destination);

        Assert.False(evaluation.CanRelocate);
        Assert.Equal(FailureReason.ActorHasNoInventory, evaluation.Trace.Reason);
    }
}
