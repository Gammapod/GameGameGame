using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class TopologyGraphTraversalTests
{
    [Fact]
    public void GraphFloodReachesNodesThroughSourceCellLinksWithoutCoordinateAdjacency()
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

        var flood = TopologyGraphTraversalService.Flood(graph, roomNode.Id, maxDepth: 1);

        Assert.Contains(flood, step =>
            step.NodeId == roomNode.Id &&
            step.SourceCoord == roomDoor &&
            step.Distance == 0 &&
            step.FromNodeId is null &&
            step.Direction is null);
        Assert.Contains(flood, step =>
            step.NodeId == hallwayNode.Id &&
            step.SourceCoord == hallwayDoor &&
            step.Distance == 1 &&
            step.FromNodeId == roomNode.Id &&
            step.Direction == Direction.East &&
            step.Kind == TopologyEdgeKind.SourceCellLink);
    }

    [Fact]
    public void GraphFloodKeepsOverlappingLayoutProjectionNodesDistinct()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("overlap-projection"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(2, 0))
            ]));
        var firstSource = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0));
        var secondSource = new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0));
        world.SourceCellLinks.Add(new SourceCellLink(firstSource, Direction.East, secondSource, Direction.West));
        var graph = TopologyGraphMaterializer.Materialize(world);
        Assert.True(graph.TryGetNode(new TopologyCellRef(firstSource), out var firstNode));
        Assert.True(graph.TryGetNode(new TopologyCellRef(secondSource), out var secondNode));
        Assert.Equal(firstNode.LayoutCoord, secondNode.LayoutCoord);

        var flood = TopologyGraphTraversalService.Flood(graph, firstNode.Id, maxDepth: 1);

        Assert.Contains(flood, step => step.NodeId == firstNode.Id && step.SourceCoord == firstSource);
        Assert.Contains(flood, step => step.NodeId == secondNode.Id && step.SourceCoord == secondSource);
        Assert.Equal(2, flood.Where(step => step.LayoutCoord == firstNode.LayoutCoord).Select(step => step.NodeId).Distinct().Count());
    }

    [Fact]
    public void GraphShortestPathToAnyUsesHalfStepDiagonalCosts()
    {
        var world = new WorldState();
        var planeId = new PlaneId("weighted-path");
        AddPlane(world, new Plane(planeId, "Weighted Path", 3, 3));
        var origin = new PlaneCoord(planeId, new GridCoord(0, 0));
        var diagonalGoal = new PlaneCoord(planeId, new GridCoord(1, 1));
        var graph = TopologyGraphMaterializer.Materialize(world);
        Assert.True(graph.TryGetNode(new TopologyCellRef(origin), out var originNode));
        Assert.True(graph.TryGetNode(new TopologyCellRef(diagonalGoal), out var goalNode));

        var path = TopologyGraphTraversalService.ShortestPathToAny(graph, originNode.Id, new HashSet<TopologyNodeId> { goalNode.Id });
        var distance = TopologyGraphTraversalService.HalfStepDistanceToAny(graph, originNode.Id, new HashSet<TopologyNodeId> { goalNode.Id });

        Assert.NotNull(path);
        var step = Assert.Single(path);
        Assert.Equal(goalNode.Id, step.NodeId);
        Assert.Equal(Direction.SouthEast, step.Direction);
        Assert.Equal(3, step.HalfStepDistance);
        Assert.Equal(3, distance);
    }

    [Fact]
    public void OctagonalDistanceFloodTreatsLegalAdjacencyAsDistanceOneThenExpandsManhattanBands()
    {
        var world = new WorldState();
        var planeId = new PlaneId("octagonal-open-room");
        AddPlane(world, new Plane(planeId, "Octagonal Open Room", 5, 5));
        var origin = new PlaneCoord(planeId, new GridCoord(2, 2));
        var graph = TopologyGraphMaterializer.Materialize(world);
        Assert.True(graph.TryGetNode(new TopologyCellRef(origin), out var originNode));

        var flood = TopologyGraphTraversalService.OctagonalDistanceFlood(graph, originNode.Id, maxDistance: 3);

        Assert.Contains(flood, step => step.SourceCoord == origin && step.Distance == 0);
        Assert.Contains(flood, step => step.SourceCoord == new PlaneCoord(planeId, new GridCoord(3, 3)) && step.Distance == 1);
        Assert.Contains(flood, step => step.SourceCoord == new PlaneCoord(planeId, new GridCoord(4, 2)) && step.Distance == 2);
        Assert.Contains(flood, step => step.SourceCoord == new PlaneCoord(planeId, new GridCoord(4, 3)) && step.Distance == 2);
        Assert.Contains(flood, step => step.SourceCoord == new PlaneCoord(planeId, new GridCoord(4, 4)) && step.Distance == 3);
    }

    [Fact]
    public void OctagonalDistanceFloodDoesNotTreatTwoCornerBlockedDiagonalAsDistanceOne()
    {
        var world = new WorldState();
        var planeId = new PlaneId("octagonal-blocked-corner");
        AddPlane(world, new Plane(planeId, "Octagonal Blocked Corner", 3, 3));
        var origin = new PlaneCoord(planeId, new GridCoord(1, 1));
        AddEntity(world, new EntityId("northBlocker"), "North Blocker", new PlaneCoord(planeId, new GridCoord(1, 0)), inventoryWidth: 0, inventoryHeight: 0, bulk: 1, aperture: 1);
        AddEntity(world, new EntityId("eastBlocker"), "East Blocker", new PlaneCoord(planeId, new GridCoord(2, 1)), inventoryWidth: 0, inventoryHeight: 0, bulk: 1, aperture: 1);
        var graph = TopologyGraphMaterializer.Materialize(world);
        Assert.True(graph.TryGetNode(new TopologyCellRef(origin), out var originNode));

        var flood = TopologyGraphTraversalService.OctagonalDistanceFlood(graph, originNode.Id, maxDistance: 1);

        Assert.DoesNotContain(flood, step => step.SourceCoord == new PlaneCoord(planeId, new GridCoord(2, 0)));
    }

    [Fact]
    public void GraphShortestPathToAnyCanFilterBlockedDestinationNodes()
    {
        var world = new WorldState();
        var planeId = new PlaneId("filtered-path");
        AddPlane(world, new Plane(planeId, "Filtered Path", 3, 1));
        var origin = new PlaneCoord(planeId, new GridCoord(0, 0));
        var blocked = new PlaneCoord(planeId, new GridCoord(1, 0));
        var goal = new PlaneCoord(planeId, new GridCoord(2, 0));
        var graph = TopologyGraphMaterializer.Materialize(world);
        Assert.True(graph.TryGetNode(new TopologyCellRef(origin), out var originNode));
        Assert.True(graph.TryGetNode(new TopologyCellRef(blocked), out var blockedNode));
        Assert.True(graph.TryGetNode(new TopologyCellRef(goal), out var goalNode));

        var path = TopologyGraphTraversalService.ShortestPathToAny(
            graph,
            originNode.Id,
            new HashSet<TopologyNodeId> { goalNode.Id },
            edge => edge.DestinationNodeId != blockedNode.Id);

        Assert.Null(path);
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
