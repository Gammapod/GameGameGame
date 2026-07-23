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

    private static void AddEntity(WorldState world, EntityId entityId, string name, PlaneCoord location)
    {
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, 0, 0, 1, 1));
        world.Occupancy.Add(nodeId, entityId);
    }
}
