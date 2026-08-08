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
    public void MergedInventoryLayerSeamsSupportPacmanStyleSelfWrapping()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("pacman-wrap"),
            [new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0))],
            [
                new MergedInventoryLayerSeam(
                    new MergedInventoryLayerEdge(TestWorld.PlayerId, Direction.East),
                    new MergedInventoryLayerEdge(TestWorld.PlayerId, Direction.West)),
                new MergedInventoryLayerSeam(
                    new MergedInventoryLayerEdge(TestWorld.PlayerId, Direction.North),
                    new MergedInventoryLayerEdge(TestWorld.PlayerId, Direction.South))
            ]));
        var movement = new MovementService();

        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 1))));
        Assert.True(movement.TryMove(world, TestWorld.RockId, Direction.East));
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 1)), world.GetEntityLocation(TestWorld.RockId));

        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0))));
        Assert.True(movement.TryMove(world, TestWorld.RockId, Direction.North));
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 1)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void MergedInventoryLayerSeamsSupportRotationalSelfMapping()
    {
        var world = TestWorld.CreateWorld();
        var gateId = new EntityId("rotating-room");
        var gatePlaneId = new PlaneId("rotating-room-inventory");
        AddPlane(world, new Plane(gatePlaneId, "Rotating Room Inventory", 3, 3));
        AddEntity(world, gateId, "Rotating Room", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)), inventoryWidth: 3, inventoryHeight: 3, bulk: 1, aperture: 10);
        world.RegisterInventoryPlane(gateId, gatePlaneId);
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("rotational"),
            [new MergedInventorySpaceContribution(gateId, new GridCoord(0, 0))],
            [new MergedInventoryLayerSeam(
                new MergedInventoryLayerEdge(gateId, Direction.East),
                new MergedInventoryLayerEdge(gateId, Direction.North))]));
        var movement = new MovementService();

        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(gatePlaneId, new GridCoord(2, 1))));
        Assert.True(movement.TryMove(world, TestWorld.RockId, Direction.East));
        Assert.Equal(new PlaneCoord(gatePlaneId, new GridCoord(1, 0)), world.GetEntityLocation(TestWorld.RockId));

        Assert.True(movement.TryMove(world, TestWorld.RockId, Direction.North));
        Assert.Equal(new PlaneCoord(gatePlaneId, new GridCoord(2, 1)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void MergedInventoryLayerSeamsSupportMultipleEdgesConnectingSameTwoSpaces()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("multi-edge"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(10, 10))
            ],
            [
                new MergedInventoryLayerSeam(
                    new MergedInventoryLayerEdge(TestWorld.PlayerId, Direction.East),
                    new MergedInventoryLayerEdge(TestWorld.SlimeId, Direction.West)),
                new MergedInventoryLayerSeam(
                    new MergedInventoryLayerEdge(TestWorld.PlayerId, Direction.North),
                    new MergedInventoryLayerEdge(TestWorld.SlimeId, Direction.South))
            ]));
        var movement = new MovementService();

        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0))));
        Assert.True(movement.TryMove(world, TestWorld.RockId, Direction.East));
        Assert.Equal(new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));

        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        Assert.True(movement.TryMove(world, TestWorld.RockId, Direction.North));
        Assert.Equal(new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void MergedInventoryLayerOverlappingLayoutLoopTraversesExplicitSeamsBackToStart()
    {
        var world = TestWorld.CreateWorld();
        var roomIds = Enumerable.Range(0, 8).Select(index => new EntityId($"loop-room-{(char)('A' + index)}")).ToArray();
        var planeIds = Enumerable.Range(0, 8).Select(index => new PlaneId($"loop-room-{(char)('A' + index)}-inventory")).ToArray();
        for (var index = 0; index < roomIds.Length; index++)
        {
            AddPlane(world, new Plane(planeIds[index], $"Loop Room {(char)('A' + index)} Inventory", 1, 1));
            AddEntity(
                world,
                roomIds[index],
                $"Loop Room {(char)('A' + index)}",
                new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(index % 5, 4 - (index / 5))),
                inventoryWidth: 1,
                inventoryHeight: 1,
                bulk: 1,
                aperture: 10);
            world.RegisterInventoryPlane(roomIds[index], planeIds[index]);
        }

        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("overlap-loop"),
            roomIds.Select(roomId => new MergedInventorySpaceContribution(roomId, new GridCoord(0, 0))).ToList(),
            [
                Seam(0, Direction.East, 1, Direction.West),
                Seam(1, Direction.South, 2, Direction.North),
                Seam(2, Direction.West, 3, Direction.East),
                Seam(3, Direction.North, 4, Direction.South),
                Seam(4, Direction.East, 5, Direction.West),
                Seam(5, Direction.South, 6, Direction.North),
                Seam(6, Direction.West, 7, Direction.East),
                Seam(7, Direction.North, 0, Direction.South)
            ],
            AllowLayoutOverlap: true));
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(planeIds[0], new GridCoord(0, 0))));

        foreach (var direction in new[] { Direction.East, Direction.South, Direction.West, Direction.North, Direction.East, Direction.South, Direction.West, Direction.North })
        {
            Assert.True(movement.TryMove(world, TestWorld.RockId, direction));
        }

        Assert.Equal(new PlaneCoord(planeIds[0], new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));

        MergedInventoryLayerSeam Seam(int fromIndex, Direction fromEdge, int toIndex, Direction toEdge) =>
            new(new MergedInventoryLayerEdge(roomIds[fromIndex], fromEdge), new MergedInventoryLayerEdge(roomIds[toIndex], toEdge));
    }

    [Fact]
    public void MergedInventoryLayerCellLinksConnectMismatchedRoomAndHallwayDoorways()
    {
        var world = TestWorld.CreateWorld();
        var roomId = new EntityId("room-a");
        var roomPlaneId = new PlaneId("room-a-inventory");
        var hallId = new EntityId("hall-ab");
        var hallPlaneId = new PlaneId("hall-ab-inventory");
        AddPlane(world, new Plane(roomPlaneId, "Room A Inventory", 3, 3));
        AddPlane(world, new Plane(hallPlaneId, "Hall AB Inventory", 5, 1));
        AddEntity(world, roomId, "Room A", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 4)), inventoryWidth: 3, inventoryHeight: 3, bulk: 1, aperture: 10);
        AddEntity(world, hallId, "Hall AB", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)), inventoryWidth: 5, inventoryHeight: 1, bulk: 1, aperture: 10);
        world.RegisterInventoryPlane(roomId, roomPlaneId);
        world.RegisterInventoryPlane(hallId, hallPlaneId);
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("room-hall-link"),
            [
                new MergedInventorySpaceContribution(roomId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(hallId, new GridCoord(10, 10))
            ],
            CellLinks:
            [
                new MergedInventoryLayerCellLink(
                    new MergedInventoryLayerCellEndpoint(roomId, new GridCoord(2, 1)), Direction.East,
                    new MergedInventoryLayerCellEndpoint(hallId, new GridCoord(0, 0)), Direction.West)
            ]));
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(roomPlaneId, new GridCoord(2, 1))));

        Assert.True(movement.TryMove(world, TestWorld.RockId, Direction.East));
        Assert.Equal(new PlaneCoord(hallPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));

        Assert.True(movement.TryMove(world, TestWorld.RockId, Direction.West));
        Assert.Equal(new PlaneCoord(roomPlaneId, new GridCoord(2, 1)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void MergedInventoryLayerOverlapModePreservesInternalContributorMovement()
    {
        var world = TestWorld.CreateWorld();
        var hallId = new EntityId("overlap-hall");
        var hallPlaneId = new PlaneId("overlap-hall-inventory");
        AddPlane(world, new Plane(hallPlaneId, "Overlap Hall Inventory", 5, 1));
        AddEntity(world, hallId, "Overlap Hall", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)), inventoryWidth: 5, inventoryHeight: 1, bulk: 1, aperture: 10);
        world.RegisterInventoryPlane(hallId, hallPlaneId);
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("overlap-hall-layer"),
            [new MergedInventorySpaceContribution(hallId, new GridCoord(0, 0))],
            AllowLayoutOverlap: true,
            CellLinks:
            [
                new MergedInventoryLayerCellLink(
                    new MergedInventoryLayerCellEndpoint(hallId, new GridCoord(4, 0)), Direction.East,
                    new MergedInventoryLayerCellEndpoint(hallId, new GridCoord(0, 0)), Direction.West)
            ]));
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(hallPlaneId, new GridCoord(0, 0))));

        Assert.True(movement.TryMove(world, TestWorld.RockId, Direction.East));
        Assert.Equal(new PlaneCoord(hallPlaneId, new GridCoord(1, 0)), world.GetEntityLocation(TestWorld.RockId));
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
