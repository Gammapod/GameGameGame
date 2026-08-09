using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class MovementServiceGraphNodeTests
{
    [Fact]
    public void GraphNodeMoveChangesEntityOccupancyFromOneTopologyNodeToAnother()
    {
        var world = TestWorld.CreateWorld();
        var graph = TopologyGraphMaterializer.Materialize(world);
        var origin = world.GetEntityLocation(TestWorld.PlayerId);
        Assert.True(graph.TryGetNeighbor(new TopologyCellRef(origin), Direction.East, out var east));
        Assert.True(graph.TryGetNode(new TopologyCellRef(east.Destination), out var destinationNode));
        var movement = new MovementService();

        var moved = movement.TryMove(world, TestWorld.PlayerId, destinationNode.Id);

        Assert.True(moved);
        Assert.Equal(east.Destination, world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Equal(new NodeId(destinationNode.Id.Value), world.Entities[TestWorld.PlayerId].OccupiedNodeId);
        Assert.Equal(TestWorld.PlayerId, world.Occupancy[new NodeId(destinationNode.Id.Value)]);
    }

    [Fact]
    public void CoordinateMoveDestinationDelegatesToGraphNodeDestination()
    {
        var world = TestWorld.CreateWorld();
        var graph = TopologyGraphMaterializer.Materialize(world);
        var origin = world.GetEntityLocation(TestWorld.PlayerId);
        Assert.True(graph.TryGetNeighbor(new TopologyCellRef(origin), Direction.East, out var east));
        Assert.True(graph.TryGetNode(new TopologyCellRef(east.Destination), out var expectedNode));
        var movement = new MovementService();

        var found = movement.TryGetMoveDestinationNode(world, TestWorld.PlayerId, Direction.East, out var destinationNodeId);

        Assert.True(found);
        Assert.Equal(expectedNode.Id, destinationNodeId);
        Assert.True(movement.TryMove(world, TestWorld.PlayerId, Direction.East));
        Assert.Equal(east.Destination, world.GetEntityLocation(TestWorld.PlayerId));
    }

    [Fact]
    public void GraphMovementEdgeReportsNodeIdentityAndProjectionFacts()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { TopologyPolicy = EntityTopologyPolicy.ConnectsOutward };
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 1))));
        var source = world.GetEntityLocation(TestWorld.RockId);
        var graph = TopologyGraphMaterializer.Materialize(world);
        Assert.True(graph.TryGetNode(new TopologyCellRef(source), out var sourceNode));
        var expectedDestination = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2));
        Assert.True(graph.TryGetNode(new TopologyCellRef(expectedDestination), out var destinationNode));

        var found = movement.TryGetMovementEdge(world, TestWorld.RockId, Direction.East, out var edge);

        Assert.True(found);
        Assert.Equal(TestWorld.RockId, edge.EntityId);
        Assert.Equal(sourceNode.Id, edge.SourceNodeId);
        Assert.Equal(source, edge.Source);
        Assert.Equal(sourceNode.LayoutCoord, edge.SourceLayoutCoord);
        Assert.Equal(destinationNode.Id, edge.DestinationNodeId);
        Assert.Equal(expectedDestination, edge.Destination);
        Assert.Equal(destinationNode.LayoutCoord, edge.DestinationLayoutCoord);
        Assert.Equal(Direction.East, edge.Direction);
        Assert.Equal(TopologyEdgeKind.EntityTopologyPolicy, edge.Kind);
        Assert.False(edge.IsBlocked);
        Assert.Null(edge.FailureReason);
        Assert.Null(edge.FailureDetail);
    }

    [Fact]
    public void CoordinateMoveDestinationAdaptersUseGraphMovementEdge()
    {
        var world = TestWorld.CreateWorld();
        world.SourceCellLinks.Add(new SourceCellLink(
            world.GetEntityLocation(TestWorld.PlayerId),
            Direction.East,
            new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)),
            Direction.West));
        var movement = new MovementService();

        var foundEdge = movement.TryGetMovementEdge(world, TestWorld.PlayerId, Direction.East, out var edge);
        var foundCoord = movement.TryGetMoveDestination(world, TestWorld.PlayerId, Direction.East, out var destination);
        var foundNode = movement.TryGetMoveDestinationNode(world, TestWorld.PlayerId, Direction.East, out var destinationNodeId);

        Assert.True(foundEdge);
        Assert.True(foundCoord);
        Assert.True(foundNode);
        Assert.Equal(edge.Destination, destination);
        Assert.Equal(edge.DestinationNodeId, destinationNodeId);
        Assert.Equal(TopologyEdgeKind.SourceCellLink, edge.Kind);
    }

    [Fact]
    public void OverlappingLayoutProjectionsDoNotCollapseGraphNodeOccupancy()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("overlap-projection"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(2, 0))
            ]));
        var playerSource = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0));
        var slimeSource = new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0));
        var graph = TopologyGraphMaterializer.Materialize(world);
        Assert.True(graph.TryGetNode(new TopologyCellRef(playerSource), out var playerNode));
        Assert.True(graph.TryGetNode(new TopologyCellRef(slimeSource), out var slimeNode));
        Assert.NotEqual(playerNode.Id, slimeNode.Id);
        Assert.Equal(playerNode.LayoutCoord, slimeNode.LayoutCoord);
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, playerNode.Id));

        var moved = movement.TryMove(world, TestWorld.RockId, slimeNode.Id);

        Assert.True(moved);
        Assert.Equal(slimeSource, world.GetEntityLocation(TestWorld.RockId));
        Assert.Equal(TestWorld.RockId, world.Occupancy[new NodeId(slimeNode.Id.Value)]);
        Assert.False(world.Occupancy.ContainsKey(new NodeId(playerNode.Id.Value)));
    }
}
