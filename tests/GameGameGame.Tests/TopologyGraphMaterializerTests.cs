using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class TopologyGraphMaterializerTests
{
    [Fact]
    public void MaterializeDistinguishesSourceNodesWhenLayoutProjectionCollides()
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
        Assert.Equal(playerSource, playerNode.SourceCoord);
        Assert.Equal(slimeSource, slimeNode.SourceCoord);
        Assert.Equal(playerNode.LayoutCoord, slimeNode.LayoutCoord);
    }

    [Fact]
    public void MaterializeDoesNotCreateCrossContributorEdgesFromMergedLayerLayoutAdjacency()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("projection-only"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(3, 0))
            ]));
        var playerEastEdge = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0));
        var slimeWestEdge = new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0));

        var graph = TopologyGraphMaterializer.Materialize(world);

        Assert.True(graph.TryGetNode(new TopologyCellRef(playerEastEdge), out var playerNode));
        Assert.True(graph.TryGetNode(new TopologyCellRef(slimeWestEdge), out var slimeNode));
        Assert.NotEqual(playerNode.Id, slimeNode.Id);
        Assert.Equal(new TopologyLayoutCoord(new GridCoord(2, 0)), playerNode.LayoutCoord);
        Assert.Equal(new TopologyLayoutCoord(new GridCoord(3, 0)), slimeNode.LayoutCoord);
        Assert.False(graph.TryGetNeighbor(new TopologyCellRef(playerEastEdge), Direction.East, out var neighbor)
            && neighbor.Destination == slimeWestEdge);
    }

    [Fact]
    public void MaterializeEmitsDefaultGridEdgesFromSourceNodeProjection()
    {
        var world = TestWorld.CreateWorld();
        var origin = world.GetEntityLocation(TestWorld.PlayerId);

        var graph = TopologyGraphMaterializer.Materialize(world);

        Assert.True(graph.TryGetNode(new TopologyCellRef(origin), out var originNode));

        foreach (var direction in DirectionMath.AllDirections)
        {
            var expectedDestination = new PlaneCoord(origin.PlaneId, origin.Coord.Offset(direction));

            var edge = Assert.Single(graph.GetOutgoingEdges(originNode.Id, direction));
            Assert.Equal(TopologyEdgeKind.DefaultGrid, edge.Kind);
            Assert.Equal(direction, edge.Direction);
            Assert.True(graph.TryGetNode(new TopologyCellRef(expectedDestination), out var destinationNode));
            Assert.Equal(destinationNode.Id, edge.DestinationNodeId);
            Assert.False(edge.IsBlocked);
            Assert.Null(edge.FailureReason);
            Assert.Null(edge.FailureDetail);
        }
    }

    [Fact]
    public void MaterializeEmitsSourceCellLinkEdgesEquivalentToCurrentSourceCellLinks()
    {
        var world = new WorldState();
        var roomId = new EntityId("room-a");
        var hallwayId = new EntityId("hall-ab");
        var roomPlaneId = new PlaneId("room-a-inventory");
        var hallwayPlaneId = new PlaneId("hall-ab-inventory");
        AddPlane(world, new Plane(TestWorld.WorldPlaneId, "World", 5, 5));
        AddPlane(world, new Plane(roomPlaneId, "Room A Inventory", 3, 3));
        AddPlane(world, new Plane(hallwayPlaneId, "Hall AB Inventory", 5, 1));
        AddEntity(world, roomId, "Room A", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0)), inventoryWidth: 3, inventoryHeight: 3, bulk: 100, aperture: 100);
        AddEntity(world, hallwayId, "Hall AB", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 0)), inventoryWidth: 5, inventoryHeight: 1, bulk: 100, aperture: 100);
        world.RegisterInventoryPlane(roomId, roomPlaneId);
        world.RegisterInventoryPlane(hallwayId, hallwayPlaneId);
        var roomDoor = new PlaneCoord(roomPlaneId, new GridCoord(2, 1));
        var hallwayDoor = new PlaneCoord(hallwayPlaneId, new GridCoord(0, 0));
        world.SourceCellLinks.Add(new SourceCellLink(roomDoor, Direction.East, hallwayDoor, Direction.West));

        var graph = TopologyGraphMaterializer.Materialize(world);

        Assert.True(graph.TryGetNode(new TopologyCellRef(roomDoor), out var roomNode));
        Assert.True(graph.TryGetNode(new TopologyCellRef(hallwayDoor), out var hallwayNode));
        var east = Assert.Single(graph.GetOutgoingEdges(roomNode.Id, Direction.East));
        var west = Assert.Single(graph.GetOutgoingEdges(hallwayNode.Id, Direction.West));
        Assert.Equal(TopologyEdgeKind.SourceCellLink, east.Kind);
        Assert.Equal(hallwayNode.Id, east.DestinationNodeId);
        Assert.False(east.IsBlocked);
        Assert.Null(east.FailureReason);
        Assert.Equal(TopologyEdgeKind.SourceCellLink, west.Kind);
        Assert.Equal(roomNode.Id, west.DestinationNodeId);
        Assert.False(west.IsBlocked);
        Assert.Null(west.FailureReason);
    }

    [Fact]
    public void MaterializedGraphProjectsDirectedEdgeFactsForDirectionalUniquenessValidation()
    {
        var world = new WorldState();
        var roomId = new EntityId("room-a");
        var hallwayId = new EntityId("hall-ab");
        var roomPlaneId = new PlaneId("room-a-inventory");
        var hallwayPlaneId = new PlaneId("hall-ab-inventory");
        AddPlane(world, new Plane(TestWorld.WorldPlaneId, "World", 5, 5));
        AddPlane(world, new Plane(roomPlaneId, "Room A Inventory", 3, 3));
        AddPlane(world, new Plane(hallwayPlaneId, "Hall AB Inventory", 5, 1));
        AddEntity(world, roomId, "Room A", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0)), inventoryWidth: 3, inventoryHeight: 3, bulk: 100, aperture: 100);
        AddEntity(world, hallwayId, "Hall AB", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 0)), inventoryWidth: 5, inventoryHeight: 1, bulk: 100, aperture: 100);
        world.RegisterInventoryPlane(roomId, roomPlaneId);
        world.RegisterInventoryPlane(hallwayId, hallwayPlaneId);
        var roomDoor = new PlaneCoord(roomPlaneId, new GridCoord(2, 1));
        var hallwayDoor = new PlaneCoord(hallwayPlaneId, new GridCoord(0, 0));
        world.SourceCellLinks.Add(new SourceCellLink(roomDoor, Direction.East, hallwayDoor, Direction.West));

        var graph = TopologyGraphMaterializer.Materialize(world);
        var directedFacts = graph.ToDirectedEdgeFacts();

        Assert.Contains(directedFacts, fact =>
            fact.Source == new TopologyCellRef(roomDoor) &&
            fact.Direction == Direction.East &&
            fact.Destination == new TopologyCellRef(hallwayDoor));
        Assert.Contains(directedFacts, fact =>
            fact.Source == new TopologyCellRef(hallwayDoor) &&
            fact.Direction == Direction.West &&
            fact.Destination == new TopologyCellRef(roomDoor));
        Assert.True(TopologyDirectionalUniqueness.Validate(directedFacts).IsValid);
    }

    [Fact]
    public void MaterializedGraphCanReconstructNeighborFactForSourceAndDirection()
    {
        var world = TestWorld.CreateWorld();
        var origin = world.GetEntityLocation(TestWorld.PlayerId);
        var expectedEast = new TopologyNeighbor(
            new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)),
            Direction.East,
            TopologyEdgeKind.DefaultGrid,
            IsBlocked: false,
            FailureReason: null,
            FailureDetail: null);

        var graph = TopologyGraphMaterializer.Materialize(world);

        Assert.True(graph.TryGetNeighbor(new TopologyCellRef(origin), Direction.East, out var graphEast));
        Assert.Equal(expectedEast, graphEast);
        Assert.False(graph.TryGetNeighbor(new TopologyCellRef(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0))), Direction.West, out var graphWest));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(-1, 0)), graphWest.Destination);
        Assert.Equal(Direction.West, graphWest.Direction);
        Assert.Equal(TopologyEdgeKind.DefaultGrid, graphWest.Kind);
        Assert.True(graphWest.IsBlocked);
        Assert.Equal(FailureReason.MoveOutOfBounds, graphWest.FailureReason);
    }

    [Fact]
    public void ScopedMaterializationCacheReusesGraphUntilMovementInvalidatesWorldTopology()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();

        using var cacheScope = TopologyGraphMaterializer.BeginCacheScope();

        var first = TopologyGraphMaterializer.Materialize(world);
        var second = TopologyGraphMaterializer.Materialize(world);

        Assert.Same(first, second);

        Assert.True(movement.TryMove(world, TestWorld.PlayerId, Direction.East));

        var afterMove = TopologyGraphMaterializer.Materialize(world);
        Assert.NotSame(first, afterMove);
    }

    private static void AddEntity(
        WorldState world,
        EntityId entityId,
        string name,
        PlaneCoord location,
        int inventoryWidth,
        int inventoryHeight,
        int bulk,
        int aperture)
    {
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, inventoryWidth, inventoryHeight, bulk, aperture));
        world.Occupancy.Add(nodeId, entityId);
    }

    private static void AddPlane(WorldState world, Plane plane)
    {
        world.Planes.Add(plane.Id, plane);
        for (var y = 0; y < plane.Height; y++)
        {
            for (var x = 0; x < plane.Width; x++)
            {
                world.AddNode(plane.Id, new GridCoord(x, y));
            }
        }
    }
}
