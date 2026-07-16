using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class ControlledActorAffordanceServiceTests
{
    [Fact]
    public void ControlledActorAffordanceQueryReportsValidAndBlockedMovementDirections()
    {
        var world = TestWorld.CreateWorld();
        var query = new ControlledActorAffordanceService(new MovementService());

        var affordances = query.Query(world, TestWorld.PlayerId);

        Assert.Equal(8, affordances.MovementDirections.Count);

        var east = Assert.Single(affordances.MovementDirections, move => move.Direction == Direction.East);
        Assert.True(east.CanExecute);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), east.Destination);

        var northEast = Assert.Single(affordances.MovementDirections, move => move.Direction == Direction.NorthEast);
        Assert.False(northEast.CanExecute);
        Assert.Equal(TestWorld.RockId, northEast.BlockingEntityId);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 1)), northEast.Destination);

        var north = Assert.Single(affordances.MovementDirections, move => move.Direction == Direction.North);
        Assert.False(north.CanExecute);
        Assert.Equal(TestWorld.SlimeId, north.BlockingEntityId);
        Assert.Equal(FailureReason.InvalidPlacement, north.FailureReason);
    }

    [Fact]
    public void ControlledActorAffordanceQueryReportsPickupSourcesAndDestinations()
    {
        var world = TestWorld.CreateWorld();
        var query = new ControlledActorAffordanceService(new MovementService());

        var affordances = query.Query(world, TestWorld.PlayerId);

        var source = Assert.Single(affordances.PickupSources, candidate => candidate.TargetId == TestWorld.SlimeId);
        Assert.True(source.CanExecute);

        var destination = Assert.Single(affordances.PickupDestinations(TestWorld.SlimeId), candidate => candidate.Destination == new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        Assert.True(destination.CanExecute);
    }

    [Fact]
    public void ControlledActorAffordanceQueryReportsDropSourcesAndBlockedDropDestinations()
    {
        var world = TestWorld.CreateWorld();
        new MovementService().TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        var query = new ControlledActorAffordanceService(new MovementService());

        var affordances = query.Query(world, TestWorld.PlayerId);

        var source = Assert.Single(affordances.DropSources, candidate => candidate.TargetId == TestWorld.RockId);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), source.Source);

        var occupiedDestination = Assert.Single(affordances.DropDestinations(TestWorld.RockId), candidate => candidate.Destination == world.GetEntityLocation(TestWorld.SlimeId));
        Assert.False(occupiedDestination.CanExecute);
        Assert.Equal(TestWorld.SlimeId, occupiedDestination.BlockingEntityId);
    }

    [Fact]
    public void ControlledActorAffordanceQueryReportsEnterTargetsAndExitDirections()
    {
        var world = TestWorld.CreateWorld();
        var query = new ControlledActorAffordanceService(new MovementService());

        var affordances = query.Query(world, TestWorld.PlayerId);
        var enter = Assert.Single(affordances.EnterTargets, candidate => candidate.TargetId == TestWorld.SlimeId);
        Assert.True(enter.CanExecute);

        new EnterAction(TestWorld.SlimeId).Execute(world, TestWorld.PlayerId, new MovementService());
        var insideAffordances = query.Query(world, TestWorld.PlayerId);
        var southExit = Assert.Single(insideAffordances.ExitDirections, candidate => candidate.Direction == Direction.South);
        Assert.True(southExit.CanExecute);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), southExit.Destination);
    }
}
