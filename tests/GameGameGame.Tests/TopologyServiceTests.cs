using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class TopologyServiceTests
{
    [Fact]
    public void TopologyGraphReturnsCardinalNeighborAndReportsOutOfBounds()
    {
        var world = TestWorld.CreateWorld();
        var graph = TopologyGraphMaterializer.Materialize(world);
        var origin = world.GetEntityLocation(TestWorld.PlayerId);

        var eastFound = graph.TryGetNeighbor(new TopologyCellRef(origin), Direction.East, out var east);
        var westFound = graph.TryGetNeighbor(new TopologyCellRef(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0))), Direction.West, out var west);

        Assert.True(eastFound);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), east.Destination);
        Assert.Equal(Direction.East, east.Direction);
        Assert.False(east.IsBlocked);
        Assert.Null(east.FailureReason);

        Assert.False(westFound);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(-1, 0)), west.Destination);
        Assert.Equal(Direction.West, west.Direction);
        Assert.True(west.IsBlocked);
        Assert.Equal(FailureReason.MoveOutOfBounds, west.FailureReason);
    }

    [Fact]
    public void TopologyEdgeFactRoundTripsDefaultNeighborFactsWithoutChangingSemantics()
    {
        var world = TestWorld.CreateWorld();
        var graph = TopologyGraphMaterializer.Materialize(world);
        var origin = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0));

        var found = graph.TryGetNeighbor(new TopologyCellRef(origin), Direction.West, out var neighbor);
        var fact = TopologyEdgeFact.FromNeighbor(origin, neighbor);
        var roundTripped = fact.ToNeighbor();

        Assert.False(found);
        Assert.Equal(new TopologyCellRef(origin), fact.Source);
        Assert.Equal(new TopologyCellRef(neighbor.Destination), fact.Destination);
        Assert.Equal(neighbor.Direction, fact.Direction);
        Assert.Equal(neighbor.Kind, fact.Kind);
        Assert.Equal(neighbor.IsBlocked, fact.IsBlocked);
        Assert.Equal(neighbor.FailureReason, fact.FailureReason);
        Assert.Equal(neighbor.FailureDetail, fact.FailureDetail);
        Assert.Equal(neighbor, roundTripped);
    }

    [Fact]
    public void TopologyDirectionalUniquenessAcceptsUniqueAndDuplicateIdenticalEdges()
    {
        var source = new TopologyCellRef(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 1)));
        var destination = new TopologyCellRef(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 1)));
        var northDestination = new TopologyCellRef(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 0)));
        var edges = new[]
        {
            new TopologyDirectedEdgeFact(source, Direction.East, destination),
            new TopologyDirectedEdgeFact(source, Direction.East, destination),
            new TopologyDirectedEdgeFact(source, Direction.North, northDestination)
        };

        var result = TopologyDirectionalUniqueness.Validate(edges);

        Assert.True(result.IsValid);
        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public void TopologyDirectionalUniquenessRejectsConflictingDestinationsForSameCellAndDirection()
    {
        var source = new TopologyCellRef(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 1)));
        var firstDestination = new TopologyCellRef(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 1)));
        var secondDestination = new TopologyCellRef(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)));
        var edges = new[]
        {
            new TopologyDirectedEdgeFact(source, Direction.East, firstDestination),
            new TopologyDirectedEdgeFact(source, Direction.East, secondDestination)
        };

        var result = TopologyDirectionalUniqueness.Validate(edges);
        var conflict = Assert.Single(result.Conflicts);

        Assert.False(result.IsValid);
        Assert.Equal(source, conflict.Source);
        Assert.Equal(Direction.East, conflict.Direction);
        Assert.Equal(firstDestination, conflict.FirstDestination);
        Assert.Equal(secondDestination, conflict.ConflictingDestination);
    }

    [Fact]
    public void TopologyCoordinateVocabularyDistinguishesSourceLayoutAndDisplayCoordinates()
    {
        var source = new TopologyCellRef(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)));
        var layout = new TopologyLayoutCoord(new GridCoord(1, 2));
        var display = new TopologyDisplayCoord(new GridCoord(1, 2));

        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), source.SourceCoord);
        Assert.Equal(new GridCoord(1, 2), layout.Coord);
        Assert.Equal(new GridCoord(1, 2), display.Coord);
        Assert.Equal("world(1,2)", source.ToString());
        Assert.Equal("layout(1,2)", layout.ToString());
        Assert.Equal("display(1,2)", display.ToString());
    }

    [Fact]
    public void TopologyGraphReturnsUnblockedIntercardinalNeighbor()
    {
        var world = TestWorld.CreateWorld();
        var graph = TopologyGraphMaterializer.Materialize(world);
        var origin = world.GetEntityLocation(TestWorld.PlayerId);

        var found = graph.TryGetNeighbor(new TopologyCellRef(origin), Direction.NorthEast, out var neighbor);

        Assert.True(found);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 1)), neighbor.Destination);
        Assert.Equal(Direction.NorthEast, neighbor.Direction);
        Assert.Equal(TopologyEdgeKind.DefaultGrid, neighbor.Kind);
        Assert.False(neighbor.IsBlocked);
    }

    [Fact]
    public void TopologyGraphReportsTwoCornerIntercardinalBlock()
    {
        var world = TestWorld.CreateWorld();
        AddEntity(world, new EntityId("east-corner-blocker"), "East Corner Blocker", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)));
        var graph = TopologyGraphMaterializer.Materialize(world);
        var origin = world.GetEntityLocation(TestWorld.PlayerId);

        var found = graph.TryGetNeighbor(new TopologyCellRef(origin), Direction.NorthEast, out var neighbor);

        Assert.False(found);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 1)), neighbor.Destination);
        Assert.Equal(Direction.NorthEast, neighbor.Direction);
        Assert.Equal(TopologyEdgeKind.DefaultGrid, neighbor.Kind);
        Assert.True(neighbor.IsBlocked);
        Assert.Equal(FailureReason.MoveBlocked, neighbor.FailureReason);
        Assert.Contains("blocked by both orthogonal corners", neighbor.FailureDetail);
    }

    [Fact]
    public void TopologyGraphEnumeratesEightDirectionsInStableOrder()
    {
        var world = TestWorld.CreateWorld();
        var graph = TopologyGraphMaterializer.Materialize(world);
        var origin = world.GetEntityLocation(TestWorld.PlayerId);
        Assert.True(graph.TryGetNode(new TopologyCellRef(origin), out var originNode));

        var neighbors = DirectionMath.AllDirections.Select(direction =>
        {
            graph.TryGetNeighbor(new TopologyCellRef(origin), direction, out var neighbor);
            return neighbor;
        }).ToList();

        Assert.Equal(DirectionMath.AllDirections, neighbors.Select(neighbor => neighbor.Direction));
        Assert.Equal(8, neighbors.Count);
        Assert.All(neighbors, neighbor => Assert.Equal(TopologyEdgeKind.DefaultGrid, neighbor.Kind));
    }

    [Fact]
    public void TopologicalRayStopsBeforeBlockedOrOutOfBoundsNeighbor()
    {
        var world = TestWorld.CreateWorld();
        var origin = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0));
        var traversal = new TopologyTraversalService();

        var ray = traversal.CastDirectionalRay(world, origin, Direction.West, maxSteps: 3);

        Assert.Empty(ray);
    }

    [Fact]
    public void TopologicalRayUsesMaterializedGraphSourceCellLinksWithoutSourceLinkWrapper()
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
        var traversal = new TopologyTraversalService();

        var ray = traversal.CastDirectionalRay(world, roomDoor, Direction.East, maxSteps: 1);

        var step = Assert.Single(ray);
        Assert.Equal(roomDoor, step.Origin);
        Assert.Equal(hallwayDoor, step.Destination);
        Assert.Equal(Direction.East, step.Direction);
        Assert.Equal(TopologyEdgeKind.SourceCellLink, step.Kind);
    }

    [Fact]
    public void TopologicalFloodIncludesOriginAndBoundedReachableNeighbors()
    {
        var world = TestWorld.CreateWorld();
        var origin = world.GetEntityLocation(TestWorld.PlayerId);
        var north = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 1));
        var traversal = new TopologyTraversalService();

        var flood = traversal.Flood(world, origin, maxDepth: 1);

        Assert.Contains(flood, step =>
            step.Coord == origin &&
            step.Distance == 0 &&
            step.From is null &&
            step.Direction is null &&
            step.Kind is null);
        Assert.Contains(flood, step =>
            step.Coord == north &&
            step.Distance == 1 &&
            step.From == origin &&
            step.Direction == Direction.North &&
            step.Kind == TopologyEdgeKind.DefaultGrid);
        Assert.DoesNotContain(flood, step => step.Distance > 1);
    }

    [Fact]
    public void TopologicalFloodUsesMaterializedGraphSourceCellLinksWithoutSourceLinkWrapper()
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
        var traversal = new TopologyTraversalService();

        var flood = traversal.Flood(world, roomDoor, maxDepth: 1);

        Assert.Contains(flood, step =>
            step.Coord == hallwayDoor &&
            step.Distance == 1 &&
            step.From == roomDoor &&
            step.Direction == Direction.East &&
            step.Kind == TopologyEdgeKind.SourceCellLink);
    }

    [Fact]
    public void MergedInventoryLayerProjectionDoesNotConnectTwoPlacedInventorySpacesWithoutExplicitLink()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("shared-interior"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(3, 0))
            ]));
        var graph = TopologyGraphMaterializer.Materialize(world);
        var movement = new MovementService();
        var playerEastEdge = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0));
        var slimeWestEdge = new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0));

        var found = graph.TryGetNeighbor(new TopologyCellRef(playerEastEdge), Direction.East, out var neighbor);
        var adjacency = movement.EvaluateAdjacency(world, playerEastEdge, slimeWestEdge);
        var notAdjacent = movement.EvaluateAdjacency(world, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), slimeWestEdge);

        Assert.False(found && neighbor.Destination == slimeWestEdge);
        Assert.False(adjacency.AreAdjacent);
        Assert.False(notAdjacent.AreAdjacent);
    }

    [Fact]
    public void TopologyGraphDoesNotResolveMergedInventoryLayerProjectionAsTopologyEdge()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("shared-interior"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(3, 0))
            ]));
        var graph = TopologyGraphMaterializer.Materialize(world);
        var movement = new MovementService();
        var playerEastEdge = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0));
        var slimeWestEdge = new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0));

        var found = graph.TryGetNeighbor(new TopologyCellRef(playerEastEdge), Direction.East, out var neighbor);
        var adjacency = movement.EvaluateAdjacency(world, playerEastEdge, slimeWestEdge);
        var notAdjacent = movement.EvaluateAdjacency(world, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), slimeWestEdge);

        Assert.False(found && neighbor.Destination == slimeWestEdge);
        Assert.False(adjacency.AreAdjacent);
        Assert.False(notAdjacent.AreAdjacent);
    }

    [Fact]
    public void SourceCellLinksConnectAuthoredInventoryCellsBidirectionally()
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
        AddEntity(world, TestWorld.RockId, "Rock", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)), inventoryWidth: 0, inventoryHeight: 0, bulk: 1, aperture: 1);
        world.RegisterInventoryPlane(roomId, roomPlaneId);
        world.RegisterInventoryPlane(hallwayId, hallwayPlaneId);
        var roomDoor = new PlaneCoord(roomPlaneId, new GridCoord(2, 1));
        var hallwayDoor = new PlaneCoord(hallwayPlaneId, new GridCoord(0, 0));
        world.SourceCellLinks.Add(new SourceCellLink(roomDoor, Direction.East, hallwayDoor, Direction.West));
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, roomDoor));

        var movedEast = movement.TryMove(world, TestWorld.RockId, Direction.East);

        Assert.True(movedEast);
        Assert.Equal(hallwayDoor, world.GetEntityLocation(TestWorld.RockId));
        var movedWest = movement.TryMove(world, TestWorld.RockId, Direction.West);

        Assert.True(movedWest);
        Assert.Equal(roomDoor, world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void DiagonalMovementCanCrossExplicitSeamWhenOneOrthogonalRouteExists()
    {
        var world = new WorldState();
        var roomId = new EntityId("room-a");
        var hallwayId = new EntityId("hall-ab");
        var actorId = new EntityId("actor");
        var roomPlaneId = new PlaneId("room-a-inventory");
        var hallwayPlaneId = new PlaneId("hall-ab-inventory");
        AddPlane(world, new Plane(TestWorld.WorldPlaneId, "World", 5, 5));
        AddPlane(world, new Plane(roomPlaneId, "Room A Inventory", 3, 3));
        AddPlane(world, new Plane(hallwayPlaneId, "Hall AB Inventory", 5, 1));
        AddEntity(world, roomId, "Room A", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0)), inventoryWidth: 3, inventoryHeight: 3, bulk: 100, aperture: 100);
        AddEntity(world, hallwayId, "Hall AB", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 0)), inventoryWidth: 5, inventoryHeight: 1, bulk: 100, aperture: 100);
        AddEntity(world, actorId, "Actor", new PlaneCoord(roomPlaneId, new GridCoord(2, 2)), inventoryWidth: 0, inventoryHeight: 0, bulk: 1, aperture: 1);
        world.RegisterInventoryPlane(roomId, roomPlaneId);
        world.RegisterInventoryPlane(hallwayId, hallwayPlaneId);
        var roomDoor = new PlaneCoord(roomPlaneId, new GridCoord(2, 1));
        var hallwayDoor = new PlaneCoord(hallwayPlaneId, new GridCoord(0, 0));
        world.SourceCellLinks.Add(new SourceCellLink(roomDoor, Direction.East, hallwayDoor, Direction.West));
        var movement = new MovementService();

        var found = movement.TryGetMovementEdge(world, actorId, Direction.NorthEast, out var edge);
        var moved = movement.TryMove(world, actorId, Direction.NorthEast);

        Assert.True(found);
        Assert.False(edge.IsBlocked);
        Assert.Equal(hallwayDoor, edge.Destination);
        Assert.Equal(TopologyEdgeKind.SourceCellLink, edge.Kind);
        Assert.True(moved);
        Assert.Equal(hallwayDoor, world.GetEntityLocation(actorId));
    }

    [Fact]
    public void TopologyGraphResolvesSourceCellLinksWithoutSourceCellLinkWrapper()
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
        var movement = new MovementService();

        var foundEast = graph.TryGetNeighbor(new TopologyCellRef(roomDoor), Direction.East, out var east);
        var foundWest = graph.TryGetNeighbor(new TopologyCellRef(hallwayDoor), Direction.West, out var west);
        var adjacency = movement.EvaluateAdjacency(world, roomDoor, hallwayDoor);

        Assert.True(foundEast);
        Assert.Equal(hallwayDoor, east.Destination);
        Assert.Equal(Direction.East, east.Direction);
        Assert.Equal(TopologyEdgeKind.SourceCellLink, east.Kind);
        Assert.False(east.IsBlocked);
        Assert.True(foundWest);
        Assert.Equal(roomDoor, west.Destination);
        Assert.Equal(Direction.West, west.Direction);
        Assert.Equal(TopologyEdgeKind.SourceCellLink, west.Kind);
        Assert.False(west.IsBlocked);
        Assert.True(adjacency.AreAdjacent);
        Assert.Equal(Direction.East, adjacency.Direction);
        Assert.Equal(TopologyEdgeKind.SourceCellLink, adjacency.EdgeKind);
        Assert.Equal(new TopologyNodeId("room-a-inventory:2,1"), adjacency.SourceNodeId);
        Assert.Equal(new TopologyNodeId("hall-ab-inventory:0,0"), adjacency.DestinationNodeId);
    }

    [Fact]
    public void MergedInventoryLayerDistanceDoesNotCrossProjectionWithoutExplicitLink()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("shared-interior"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(3, 0))
            ]));
        var traversal = new TopologyTraversalService();
        var origin = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0));
        var destination = new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0));

        var beforeMove = traversal.Flood(world, origin, maxDepth: 3);
        var movedExternally = new MovementService().TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)));
        var afterMove = traversal.Flood(world, origin, maxDepth: 3);

        Assert.DoesNotContain(beforeMove, step => step.Coord == destination);
        Assert.True(movedExternally);
        Assert.DoesNotContain(afterMove, step => step.Coord == destination);
    }

    [Fact]
    public void MergedInventoryLayerMovementCrossesSeamAndUpdatesLocalOwner()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("shared-interior"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(3, 0))
            ]));
        var movement = new MovementService();
        var origin = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0));
        var destination = new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0));
        world.SourceCellLinks.Add(new SourceCellLink(origin, Direction.East, destination, Direction.West));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, origin));

        var moved = movement.TryMove(world, TestWorld.RockId, Direction.East);
        var resolved = MergedInventoryLayerResolver.TryResolveCell(world, world.GetEntityLocation(TestWorld.RockId), out var cell);

        Assert.True(moved);
        Assert.Equal(destination, world.GetEntityLocation(TestWorld.RockId));
        Assert.True(resolved);
        Assert.Equal(TestWorld.SlimeId, cell.Space.OwnerId);
        Assert.Equal(new GridCoord(3, 0), cell.LayerCoord);
    }

    [Fact]
    public void MergedInventoryLayerSupportsDifferentInventoryDimensionsAndOffsets()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("different-dimensions"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(3, 1))
            ]));
        var movement = new MovementService();
        var origin = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 1));
        var destination = new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0));
        world.SourceCellLinks.Add(new SourceCellLink(origin, Direction.East, destination, Direction.West));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, origin));

        var moved = movement.TryMove(world, TestWorld.RockId, Direction.East);

        Assert.True(moved);
        Assert.Equal(destination, world.GetEntityLocation(TestWorld.RockId));
        Assert.True(MergedInventoryLayerResolver.TryResolveCell(world, world.GetEntityLocation(TestWorld.RockId), out var cell));
        Assert.Equal(TestWorld.SlimeId, cell.Space.OwnerId);
        Assert.Equal(new GridCoord(3, 1), cell.LayerCoord);
    }

    [Fact]
    public void MergedInventoryLayerSupportsOwnersInDifferentExteriorRooms()
    {
        var world = TestWorld.CreateWorld();
        var otherRoomId = new PlaneId("other-room");
        AddPlane(world, new Plane(otherRoomId, "Other Room", 5, 5));
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(otherRoomId, new GridCoord(2, 2))));
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("different-rooms"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(3, 0))
            ]));
        world.SourceCellLinks.Add(new SourceCellLink(
            new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0)),
            Direction.East,
            new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)),
            Direction.West));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0))));
        Assert.True(movement.TryMove(world, TestWorld.RockId, Direction.East));

        var exit = new ExitAction(Direction.East).Resolve(world, TestWorld.RockId, movement);

        Assert.True(exit.Succeeded);
        Assert.Equal(new PlaneCoord(otherRoomId, new GridCoord(3, 2)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void MergedInventoryLayerCoreTopologySupportsThreeContributingSpaces()
    {
        var world = TestWorld.CreateWorld();
        var gateCId = new EntityId("gate-c");
        var gateCPlaneId = new PlaneId("gate-c-inventory");
        AddPlane(world, new Plane(gateCPlaneId, "Gate C Inventory", 1, 1));
        AddEntity(world, gateCId, "Gate C", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)), inventoryWidth: 1, inventoryHeight: 1, bulk: 1, aperture: 10);
        world.RegisterInventoryPlane(gateCId, gateCPlaneId);
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("three-space-core"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(3, 0)),
                new MergedInventorySpaceContribution(gateCId, new GridCoord(4, 0))
            ]));
        world.SourceCellLinks.Add(new SourceCellLink(
            new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0)),
            Direction.East,
            new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)),
            Direction.West));
        world.SourceCellLinks.Add(new SourceCellLink(
            new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)),
            Direction.East,
            new PlaneCoord(gateCPlaneId, new GridCoord(0, 0)),
            Direction.West));
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0))));

        Assert.True(movement.TryMove(world, TestWorld.RockId, Direction.East));
        Assert.True(movement.TryMove(world, TestWorld.RockId, Direction.East));

        Assert.Equal(new PlaneCoord(gateCPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
        Assert.True(MergedInventoryLayerResolver.TryResolveCell(world, world.GetEntityLocation(TestWorld.RockId), out var cell));
        Assert.Equal(gateCId, cell.Space.OwnerId);
    }

    [Fact]
    public void MergedInventoryLayerRemainsRigidWhenBothOwnersMoveExternally()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("moving-owners"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(3, 0))
            ]));
        world.SourceCellLinks.Add(new SourceCellLink(
            new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0)),
            Direction.East,
            new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)),
            Direction.West));
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 4))));
        Assert.True(movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4))));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0))));

        Assert.True(movement.TryMove(world, TestWorld.RockId, Direction.East));
        var exit = new ExitAction(Direction.West).Resolve(world, TestWorld.RockId, movement);

        Assert.True(exit.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 4)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void EntityTopologyPolicyConnectsInventoryEdgeOutwardToExteriorAdjacency()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { TopologyPolicy = EntityTopologyPolicy.ConnectsOutward };
        var movement = new MovementService();
        var inventoryEastEdge = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 1));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, inventoryEastEdge));

        var adjacency = movement.EvaluateAdjacency(world, inventoryEastEdge, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)));
        var moved = movement.TryMove(world, TestWorld.RockId, Direction.East);

        Assert.True(adjacency.AreAdjacent);
        Assert.Equal(Direction.East, adjacency.Direction);
        Assert.True(moved);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void TopologyGraphResolvesEntityTopologyPolicyWithoutEntityTopologyWrapper()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { TopologyPolicy = EntityTopologyPolicy.ConnectsOutward };
        var inventoryEastEdge = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 1));
        var exteriorEast = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2));
        var graph = TopologyGraphMaterializer.Materialize(world);
        var movement = new MovementService();

        var found = graph.TryGetNeighbor(new TopologyCellRef(inventoryEastEdge), Direction.East, out var neighbor);
        var adjacency = movement.EvaluateAdjacency(world, inventoryEastEdge, exteriorEast);

        Assert.True(found);
        Assert.Equal(exteriorEast, neighbor.Destination);
        Assert.Equal(Direction.East, neighbor.Direction);
        Assert.Equal(TopologyEdgeKind.EntityTopologyPolicy, neighbor.Kind);
        Assert.False(neighbor.IsBlocked);
        Assert.True(adjacency.AreAdjacent);
        Assert.Equal(Direction.East, adjacency.Direction);
        Assert.Equal(TopologyEdgeKind.EntityTopologyPolicy, adjacency.EdgeKind);
        Assert.Equal(new TopologyNodeId("player:2,1"), adjacency.SourceNodeId);
        Assert.Equal(new TopologyNodeId("world:2,2"), adjacency.DestinationNodeId);
    }

    [Fact]
    public void EntityTopologyPolicyConnectsExteriorAdjacencyInwardToPreferredInventoryEdgeCell()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { TopologyPolicy = EntityTopologyPolicy.ConnectsInward };
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2))));

        var expectedInventoryCell = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 1));
        var adjacency = movement.EvaluateAdjacency(world, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), expectedInventoryCell);
        var moved = movement.TryMove(world, TestWorld.RockId, Direction.West);

        Assert.True(adjacency.AreAdjacent);
        Assert.Equal(Direction.West, adjacency.Direction);
        Assert.True(moved);
        Assert.Equal(expectedInventoryCell, world.GetEntityLocation(TestWorld.RockId));
        Assert.False(movement.EvaluateAdjacency(world, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), world.GetEntityLocation(TestWorld.PlayerId)).AreAdjacent);
    }

    [Fact]
    public void EntityTopologyPolicyConnectsIntercardinalExteriorAdjacencyToInventoryCorners()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { TopologyPolicy = EntityTopologyPolicy.ConnectsInwardAndOutward };
        var movement = new MovementService();
        var topRightInventoryCorner = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0));
        var northEastExterior = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 1));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, topRightInventoryCorner));

        Assert.True(movement.EvaluateAdjacency(world, topRightInventoryCorner, northEastExterior).AreAdjacent);
        Assert.True(movement.EvaluateAdjacency(world, northEastExterior, topRightInventoryCorner).AreAdjacent);
    }

    [Fact]
    public void EntityTopologyPolicyOutwardAdjacencySupportsPickupAcrossInventoryBoundary()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { TopologyPolicy = EntityTopologyPolicy.ConnectsOutward };
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Bulk = 4 };
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 1))));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2))));
        var destination = new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0));
        var pickup = new PickupAction(TestWorld.RockId, destination);

        var evaluation = pickup.Evaluate(world, TestWorld.SlimeId, movement);
        pickup.Execute(world, TestWorld.SlimeId, movement);

        Assert.True(evaluation.CanExecute);
        Assert.Equal(destination, world.GetEntityLocation(TestWorld.RockId));
    }

    private static void AddEntity(WorldState world, EntityId entityId, string name, PlaneCoord location)
    {
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, 0, 0, 1, 1));
        world.Occupancy.Add(nodeId, entityId);
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
