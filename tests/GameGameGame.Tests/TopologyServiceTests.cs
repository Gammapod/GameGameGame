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
}
