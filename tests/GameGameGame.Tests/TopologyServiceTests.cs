using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class TopologyServiceTests
{
    [Fact]
    public void DefaultTopologyReturnsCardinalNeighborAndReportsOutOfBounds()
    {
        var world = TestWorld.CreateWorld();
        ITopologyService topology = new DefaultTopologyService();
        var origin = world.GetEntityLocation(TestWorld.PlayerId);

        var eastFound = topology.TryGetNeighbor(world, origin, Direction.East, out var east);
        var westFound = topology.TryGetNeighbor(world, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0)), Direction.West, out var west);

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
        ITopologyService topology = new DefaultTopologyService();
        var origin = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0));

        var found = topology.TryGetNeighbor(world, origin, Direction.West, out var neighbor);
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
    public void TopologyEdgeFactRoundTripsDirectedOverlayNeighborFactsWithoutChangingSemantics()
    {
        var world = TestWorld.CreateWorld();
        var origin = world.GetEntityLocation(TestWorld.PlayerId);
        var remote = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4));
        ITopologyService topology = new DirectedOverlayTopologyService(
            new DefaultTopologyService(),
            [new DirectedTopologyEdge(origin, Direction.North, remote)]);

        var found = topology.TryGetNeighbor(world, origin, Direction.North, out var neighbor);
        var fact = TopologyEdgeFact.FromNeighbor(origin, neighbor);

        Assert.True(found);
        Assert.Equal(new TopologyCellRef(origin), fact.Source);
        Assert.Equal(new TopologyCellRef(remote), fact.Destination);
        Assert.Equal(Direction.North, fact.Direction);
        Assert.Equal(TopologyEdgeKind.DirectedOverlay, fact.Kind);
        Assert.False(fact.IsBlocked);
        Assert.Null(fact.FailureReason);
        Assert.Null(fact.FailureDetail);
        Assert.Equal(neighbor, fact.ToNeighbor());
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
    public void DefaultTopologyReturnsUnblockedIntercardinalNeighbor()
    {
        var world = TestWorld.CreateWorld();
        ITopologyService topology = new DefaultTopologyService();
        var origin = world.GetEntityLocation(TestWorld.PlayerId);

        var found = topology.TryGetNeighbor(world, origin, Direction.NorthEast, out var neighbor);

        Assert.True(found);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 1)), neighbor.Destination);
        Assert.Equal(Direction.NorthEast, neighbor.Direction);
        Assert.Equal(TopologyEdgeKind.DefaultGrid, neighbor.Kind);
        Assert.False(neighbor.IsBlocked);
    }

    [Fact]
    public void DefaultTopologyReportsTwoCornerIntercardinalBlock()
    {
        var world = TestWorld.CreateWorld();
        AddEntity(world, new EntityId("east-corner-blocker"), "East Corner Blocker", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)));
        ITopologyService topology = new DefaultTopologyService();
        var origin = world.GetEntityLocation(TestWorld.PlayerId);

        var found = topology.TryGetNeighbor(world, origin, Direction.NorthEast, out var neighbor);

        Assert.False(found);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 1)), neighbor.Destination);
        Assert.Equal(Direction.NorthEast, neighbor.Direction);
        Assert.Equal(TopologyEdgeKind.DefaultGrid, neighbor.Kind);
        Assert.True(neighbor.IsBlocked);
        Assert.Equal(FailureReason.MoveBlocked, neighbor.FailureReason);
        Assert.Contains("blocked by both orthogonal corners", neighbor.FailureDetail);
    }

    [Fact]
    public void DefaultTopologyEnumeratesEightDirectionsInStableOrder()
    {
        var world = TestWorld.CreateWorld();
        ITopologyService topology = new DefaultTopologyService();
        var origin = world.GetEntityLocation(TestWorld.PlayerId);

        var neighbors = topology.GetNeighbors(world, origin);

        Assert.Equal(DirectionMath.AllDirections, neighbors.Select(neighbor => neighbor.Direction));
        Assert.Equal(8, neighbors.Count);
        Assert.All(neighbors, neighbor => Assert.Equal(TopologyEdgeKind.DefaultGrid, neighbor.Kind));
    }

    [Fact]
    public void DirectedOverlayTopologyMakesRemoteNodeAdjacentInChosenDirection()
    {
        var world = TestWorld.CreateWorld();
        var origin = world.GetEntityLocation(TestWorld.PlayerId);
        var remote = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4));
        ITopologyService topology = new DirectedOverlayTopologyService(
            new DefaultTopologyService(),
            [new DirectedTopologyEdge(origin, Direction.North, remote)]);

        var found = topology.TryGetNeighbor(world, origin, Direction.North, out var neighbor);
        var adjacency = topology.EvaluateAdjacency(world, origin, remote);

        Assert.True(found);
        Assert.Equal(remote, neighbor.Destination);
        Assert.Equal(Direction.North, neighbor.Direction);
        Assert.Equal(TopologyEdgeKind.DirectedOverlay, neighbor.Kind);
        Assert.False(neighbor.IsBlocked);
        Assert.True(adjacency.AreAdjacent);
        Assert.Equal(Direction.North, adjacency.Direction);
        Assert.False(adjacency.IsIntercardinal);
    }

    [Fact]
    public void MovementFollowsDirectedOverlayTopologyAndStillRejectsOccupiedDestination()
    {
        var world = TestWorld.CreateWorld();
        var origin = world.GetEntityLocation(TestWorld.PlayerId);
        var remote = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4));
        var occupied = world.GetEntityLocation(TestWorld.SlimeId);
        var movement = new MovementService(new DirectedOverlayTopologyService(
            new DefaultTopologyService(),
            [
                new DirectedTopologyEdge(origin, Direction.North, remote),
                new DirectedTopologyEdge(origin, Direction.South, occupied)
            ]));

        var canMoveSouth = movement.CanMove(world, TestWorld.PlayerId, Direction.South);
        var movedNorth = movement.TryMove(world, TestWorld.PlayerId, Direction.North);

        Assert.False(canMoveSouth);
        Assert.True(movedNorth);
        Assert.Equal(remote, world.GetEntityLocation(TestWorld.PlayerId));
    }

    [Fact]
    public void DirectedOverlayTopologyRejectsCrossPlaneEdgeForFirstSlice()
    {
        var world = TestWorld.CreateWorld();
        var origin = world.GetEntityLocation(TestWorld.PlayerId);
        var inventoryDestination = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0));
        ITopologyService topology = new DirectedOverlayTopologyService(
            new DefaultTopologyService(),
            [new DirectedTopologyEdge(origin, Direction.North, inventoryDestination)]);

        var found = topology.TryGetNeighbor(world, origin, Direction.North, out var neighbor);
        var adjacency = topology.EvaluateAdjacency(world, origin, inventoryDestination);

        Assert.False(found);
        Assert.Equal(inventoryDestination, neighbor.Destination);
        Assert.Equal(TopologyEdgeKind.DirectedOverlay, neighbor.Kind);
        Assert.True(neighbor.IsBlocked);
        Assert.Equal(FailureReason.TargetNotAdjacent, neighbor.FailureReason);
        Assert.Contains("cross-plane", neighbor.FailureDetail);
        Assert.False(adjacency.AreAdjacent);
        Assert.Equal(Direction.North, adjacency.Direction);
        Assert.Equal(FailureReason.TargetNotAdjacent, adjacency.FailureReason);
    }

    [Fact]
    public void TopologicalRayFollowsDirectedOverlayThenContinuesInSameDirection()
    {
        var world = TestWorld.CreateWorld();
        var origin = world.GetEntityLocation(TestWorld.PlayerId);
        var remote = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4));
        var topology = new DirectedOverlayTopologyService(
            new DefaultTopologyService(),
            [new DirectedTopologyEdge(origin, Direction.North, remote)]);
        var traversal = new TopologyTraversalService(topology);

        var ray = traversal.CastDirectionalRay(world, origin, Direction.North, maxSteps: 2);

        Assert.Equal(2, ray.Count);
        Assert.Equal(origin, ray[0].Origin);
        Assert.Equal(remote, ray[0].Destination);
        Assert.Equal(TopologyEdgeKind.DirectedOverlay, ray[0].Kind);
        Assert.Equal(remote, ray[1].Origin);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 3)), ray[1].Destination);
        Assert.Equal(TopologyEdgeKind.DefaultGrid, ray[1].Kind);
    }

    [Fact]
    public void TopologicalRayStopsBeforeBlockedOrOutOfBoundsNeighbor()
    {
        var world = TestWorld.CreateWorld();
        var origin = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0));
        var traversal = new TopologyTraversalService(new DefaultTopologyService());

        var ray = traversal.CastDirectionalRay(world, origin, Direction.West, maxSteps: 3);

        Assert.Empty(ray);
    }

    [Fact]
    public void TopologicalFloodIncludesOriginAndBoundedReachableNeighbors()
    {
        var world = TestWorld.CreateWorld();
        var origin = world.GetEntityLocation(TestWorld.PlayerId);
        var remote = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4));
        var topology = new DirectedOverlayTopologyService(
            new DefaultTopologyService(),
            [new DirectedTopologyEdge(origin, Direction.North, remote)]);
        var traversal = new TopologyTraversalService(topology);

        var flood = traversal.Flood(world, origin, maxDepth: 1);

        Assert.Contains(flood, step =>
            step.Coord == origin &&
            step.Distance == 0 &&
            step.From is null &&
            step.Direction is null &&
            step.Kind is null);
        Assert.Contains(flood, step =>
            step.Coord == remote &&
            step.Distance == 1 &&
            step.From == origin &&
            step.Direction == Direction.North &&
            step.Kind == TopologyEdgeKind.DirectedOverlay);
        Assert.DoesNotContain(flood, step => step.Distance > 1);
    }

    [Fact]
    public void TopologicalFloodDoesNotRevisitNodesThroughCycles()
    {
        var world = TestWorld.CreateWorld();
        var origin = world.GetEntityLocation(TestWorld.PlayerId);
        var remote = new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4));
        var topology = new DirectedOverlayTopologyService(
            new DefaultTopologyService(),
            [
                new DirectedTopologyEdge(origin, Direction.North, remote),
                new DirectedTopologyEdge(remote, Direction.South, origin)
            ]);
        var traversal = new TopologyTraversalService(topology);

        var flood = traversal.Flood(world, origin, maxDepth: 3);

        Assert.Equal(flood.Select(step => step.Coord).Distinct().Count(), flood.Count);
        Assert.Single(flood, step => step.Coord == origin);
        Assert.Single(flood, step => step.Coord == remote);
    }

    [Fact]
    public void MergedInventoryLayerConnectsTwoPlacedInventorySpacesAsOneTopology()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("shared-interior"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(3, 0))
            ]));
        ITopologyService topology = new MergedInventoryLayerTopologyService(new DefaultTopologyService());
        var playerEastEdge = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0));
        var slimeWestEdge = new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0));

        var found = topology.TryGetNeighbor(world, playerEastEdge, Direction.East, out var neighbor);
        var adjacency = topology.EvaluateAdjacency(world, playerEastEdge, slimeWestEdge);
        var notAdjacent = topology.EvaluateAdjacency(world, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), slimeWestEdge);

        Assert.True(found);
        Assert.Equal(slimeWestEdge, neighbor.Destination);
        Assert.Equal(Direction.East, neighbor.Direction);
        Assert.Equal(TopologyEdgeKind.MergedInventoryLayer, neighbor.Kind);
        Assert.False(neighbor.IsBlocked);
        Assert.True(adjacency.AreAdjacent);
        Assert.Equal(Direction.East, adjacency.Direction);
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
    public void MergedInventoryLayerDistanceTreatsPlacedSpacesAsOneRigidLayer()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("shared-interior"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(3, 0))
            ]));
        ITopologyService topology = new MergedInventoryLayerTopologyService(new DefaultTopologyService());
        var traversal = new TopologyTraversalService(topology);
        var origin = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0));
        var destination = new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0));

        var beforeMove = traversal.Flood(world, origin, maxDepth: 3);
        var movedExternally = new MovementService().TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)));
        var afterMove = traversal.Flood(world, origin, maxDepth: 3);

        Assert.Contains(beforeMove, step => step.Coord == destination && step.Distance == 3);
        Assert.True(movedExternally);
        Assert.Contains(afterMove, step => step.Coord == destination && step.Distance == 3);
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
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 1))));

        var moved = movement.TryMove(world, TestWorld.RockId, Direction.East);

        Assert.True(moved);
        Assert.Equal(new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
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
