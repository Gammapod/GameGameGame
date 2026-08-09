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
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Aperture = 9 };
        var query = new ControlledActorAffordanceService(new MovementService());

        var affordances = query.Query(world, TestWorld.PlayerId);

        var source = Assert.Single(affordances.PickupSources, candidate => candidate.TargetId == TestWorld.SlimeId);
        Assert.True(source.CanExecute);

        var diagonalSource = Assert.Single(affordances.PickupSources, candidate => candidate.TargetId == TestWorld.RockId);
        Assert.True(diagonalSource.CanExecute);

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
    public void ControlledActorAffordanceQueryReportsIntercardinalDropBlockedByTwoCorners()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        AddEntity(world, new EntityId("east-corner-blocker"), "East Corner Blocker", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)));
        var query = new ControlledActorAffordanceService(movement);

        var affordances = query.Query(world, TestWorld.PlayerId);

        var northEast = Assert.Single(affordances.DropDestinations(TestWorld.RockId), candidate => candidate.Destination == new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 1)));
        Assert.False(northEast.CanExecute);
        Assert.Equal(FailureReason.MoveBlocked, northEast.FailureReason);
        Assert.Contains("blocked by both orthogonal corners", northEast.FailureDetail);
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

    [Fact]
    public void ControlledActorAffordanceExitDirectionsUseTopologyNeighborDestinations()
    {
        var world = TestWorld.CreateWorld();
        var topologyDestination = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 3));
        world.SourceCellLinks.Add(new SourceCellLink(world.GetEntityLocation(TestWorld.SlimeId), Direction.South, topologyDestination, Direction.North));
        var movement = new MovementService();
        Assert.True(new EnterAction(TestWorld.SlimeId).ExecuteForTest(world, TestWorld.PlayerId, movement));
        var query = new ControlledActorAffordanceService(movement);

        var affordances = query.Query(world, TestWorld.PlayerId);

        var southExit = Assert.Single(affordances.ExitDirections, candidate => candidate.Direction == Direction.South);
        Assert.True(southExit.CanExecute);
        Assert.Equal(topologyDestination, southExit.Destination);
    }

    [Fact]
    public void ControlledActorAffordanceMovementReportsEntityTopologyOutwardDestination()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { TopologyPolicy = EntityTopologyPolicy.ConnectsOutward };
        var movement = new MovementService();
        var inventoryEastEdge = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 1));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, inventoryEastEdge));
        var query = new ControlledActorAffordanceService(movement);

        var affordances = query.Query(world, TestWorld.RockId);

        var east = Assert.Single(affordances.MovementDirections, option => option.Direction == Direction.East);
        Assert.True(east.CanExecute);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), east.Destination);
        Assert.Equal(new TopologyNodeId("world:2,2"), east.DestinationNodeId);
        Assert.Equal(TopologyEdgeKind.EntityTopologyPolicy, east.EdgeKind);
    }

    [Fact]
    public void ControlledActorAffordancePickupSourceReportsGraphAdjacencyFacts()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Aperture = 9 };
        var movement = new MovementService();
        var remoteSlime = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4));
        Assert.True(movement.TryPlace(world, TestWorld.SlimeId, remoteSlime));
        world.SourceCellLinks.Add(new SourceCellLink(world.GetEntityLocation(TestWorld.PlayerId), Direction.East, remoteSlime, Direction.West));
        var query = new ControlledActorAffordanceService(movement);

        var affordances = query.Query(world, TestWorld.PlayerId);

        var source = Assert.Single(affordances.PickupSources, candidate => candidate.TargetId == TestWorld.SlimeId);
        Assert.True(source.CanExecute);
        Assert.Equal(remoteSlime, source.Source);
        Assert.Equal(new TopologyNodeId("world:4,4"), source.SourceNodeId);
        Assert.Equal(TopologyEdgeKind.SourceCellLink, source.EdgeKind);
    }

    [Fact]
    public void ControlledActorAffordanceMovementReportsEntityTopologyInwardDestinationInsteadOfContainerBump()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { TopologyPolicy = EntityTopologyPolicy.ConnectsInward };
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2))));
        var query = new ControlledActorAffordanceService(movement);

        var affordances = query.Query(world, TestWorld.RockId);

        var west = Assert.Single(affordances.MovementDirections, option => option.Direction == Direction.West);
        Assert.True(west.CanExecute);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 1)), west.Destination);
        Assert.Null(west.BlockingEntityId);
    }

    private static void AddEntity(WorldState world, EntityId entityId, string name, PlaneCoord location)
    {
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, 0, 0, 1, 1));
        world.Occupancy.Add(nodeId, entityId);
    }

}

file static class ControlledActorAffordanceTestExtensions
{
    public static bool ExecuteForTest(this IActionIntent action, WorldState world, EntityId actorId, MovementService movement)
    {
        var evaluation = action.Evaluate(world, actorId, movement);
        if (!evaluation.CanExecute)
        {
            return false;
        }

        action.Execute(world, actorId, movement);
        return true;
    }
}
