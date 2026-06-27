using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class EntityContainmentPathServiceTests
{
    private static readonly EntityId ChestId = new("chest");
    private static readonly EntityId GemId = new("gem");
    private static readonly PlaneId ChestInventoryPlaneId = new("chest");

    [Fact]
    public void EntityContainmentPathServiceBuildsUpwardPathForNestedEntity()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0)));
        var service = new EntityContainmentPathService();

        var path = service.GetUpwardPath(world, TestWorld.RockId);

        Assert.Equal(EntityContainmentPathStatus.Complete, path.Status);
        Assert.Equal(TestWorld.RockId, path.RequestedEntityId);
        Assert.Empty(path.Cycles);
        Assert.Empty(path.Diagnostics);
        Assert.Equal([TestWorld.PlayerId, TestWorld.SlimeId, TestWorld.RockId], path.Segments.Select(segment => segment.EntityId).ToArray());
        Assert.Null(path.Segments[0].ContainingPlaneId);
        Assert.Null(path.Segments[0].CoordinateInContainingPlane);
        Assert.Null(path.Segments[0].ContainerEntityId);
        Assert.Equal(TestWorld.PlayerInventoryPlaneId, path.Segments[1].ContainingPlaneId);
        Assert.Equal(new GridCoord(1, 0), path.Segments[1].CoordinateInContainingPlane);
        Assert.Equal(TestWorld.PlayerId, path.Segments[1].ContainerEntityId);
        Assert.Equal(TestWorld.SlimeInventoryPlaneId, path.Segments[2].ContainingPlaneId);
        Assert.Equal(new GridCoord(0, 0), path.Segments[2].CoordinateInContainingPlane);
        Assert.Equal(TestWorld.SlimeId, path.Segments[2].ContainerEntityId);
    }

    [Fact]
    public void EntityContainmentPathServiceLimitsUpwardPathByMaxDepth()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0)));
        var service = new EntityContainmentPathService();

        var path = service.GetUpwardPath(world, TestWorld.RockId, maxDepth: 2);

        Assert.Equal(EntityContainmentPathStatus.Truncated, path.Status);
        Assert.Equal([TestWorld.SlimeId, TestWorld.RockId], path.Segments.Select(segment => segment.EntityId).ToArray());
        Assert.Contains(path.Diagnostics, diagnostic => diagnostic.Contains("Max depth 2", StringComparison.Ordinal));
    }

    [Fact]
    public void EntityContainmentPathServiceReportsMissingEntity()
    {
        var world = TestWorld.CreateWorld();
        var missingEntityId = new EntityId("missing");
        var service = new EntityContainmentPathService();

        var path = service.GetUpwardPath(world, missingEntityId);

        Assert.Equal(EntityContainmentPathStatus.RequestedEntityNotFound, path.Status);
        Assert.Equal(missingEntityId, path.RequestedEntityId);
        Assert.Empty(path.Segments);
        Assert.Empty(path.Cycles);
        Assert.Contains(path.Diagnostics, diagnostic => diagnostic.Contains("missing", StringComparison.Ordinal));
    }

    [Fact]
    public void EntityContainmentPathServiceDetectsContainmentCycle()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        var service = new EntityContainmentPathService();

        var path = service.GetUpwardPath(world, TestWorld.PlayerId);

        Assert.Equal(EntityContainmentPathStatus.CycleDetected, path.Status);
        Assert.Equal([TestWorld.SlimeId, TestWorld.PlayerId], path.Segments.Select(segment => segment.EntityId).ToArray());
        Assert.Single(path.Cycles);
        Assert.Contains(path.Diagnostics, diagnostic => diagnostic.Contains("Cycle detected", StringComparison.Ordinal));
    }

    [Fact]
    public void EntityContainmentPathServiceReportsCycleEdgesWithDirection()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        var service = new EntityContainmentPathService();

        var path = service.GetUpwardPath(world, TestWorld.PlayerId);

        var cycle = Assert.Single(path.Cycles);
        Assert.Contains(
            new EntityContainmentPathCycleEdge(TestWorld.SlimeId, TestWorld.PlayerId, TestWorld.SlimeInventoryPlaneId),
            cycle.Edges);
        Assert.Contains(
            new EntityContainmentPathCycleEdge(TestWorld.PlayerId, TestWorld.SlimeId, TestWorld.PlayerInventoryPlaneId),
            cycle.Edges);
    }

    [Fact]
    public void EntityContainmentPathServiceBuildsPathFromKnownRoot()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0)));
        var service = new EntityContainmentPathService();

        var path = service.GetPathFromRoot(world, TestWorld.PlayerId, TestWorld.RockId);

        Assert.Equal(EntityContainmentPathStatus.Complete, path.Status);
        Assert.Equal(TestWorld.RockId, path.RequestedEntityId);
        Assert.Equal([TestWorld.PlayerId, TestWorld.SlimeId, TestWorld.RockId], path.Segments.Select(segment => segment.EntityId).ToArray());
        Assert.Empty(path.Cycles);
        Assert.Empty(path.Diagnostics);
    }

    [Fact]
    public void EntityContainmentPathServiceReportsEntityNotUnderRoot()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        var service = new EntityContainmentPathService();

        var path = service.GetPathFromRoot(world, TestWorld.PlayerId, TestWorld.RockId);

        Assert.Equal(EntityContainmentPathStatus.NotUnderRoot, path.Status);
        Assert.Equal([TestWorld.SlimeId, TestWorld.RockId], path.Segments.Select(segment => segment.EntityId).ToArray());
        Assert.Contains(path.Diagnostics, diagnostic => diagnostic.Contains("not contained by root player", StringComparison.Ordinal));
    }

    [Fact]
    public void EntityContainmentPathServiceCanLimitRootRelativePathByMaxDepth()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0)));
        var service = new EntityContainmentPathService();

        var path = service.GetPathFromRoot(world, TestWorld.PlayerId, TestWorld.RockId, maxDepth: 2);

        Assert.Equal(EntityContainmentPathStatus.Truncated, path.Status);
        Assert.Equal([TestWorld.SlimeId, TestWorld.RockId], path.Segments.Select(segment => segment.EntityId).ToArray());
        Assert.Contains(path.Diagnostics, diagnostic => diagnostic.Contains("Max depth 2", StringComparison.Ordinal));
    }

    [Fact]
    public void EntityContainmentPathServiceFindsSharedRootForTwoEntities()
    {
        var world = CreateTwoBranchWorld();
        var service = new EntityContainmentPathService();

        var path = service.GetSharedRootPath(world, TestWorld.RockId, GemId);

        Assert.Equal(EntityContainmentPathStatus.Complete, path.Status);
        Assert.Equal(TestWorld.PlayerId, path.SharedRootEntityId);
        Assert.Empty(path.Cycles);
        Assert.Empty(path.Diagnostics);
    }

    [Fact]
    public void EntityContainmentPathServiceReturnsTwoBranchesFromSharedRoot()
    {
        var world = CreateTwoBranchWorld();
        var service = new EntityContainmentPathService();

        var path = service.GetSharedRootPath(world, TestWorld.RockId, GemId);

        Assert.Equal([TestWorld.PlayerId, TestWorld.SlimeId, TestWorld.RockId], path.SharedRootToFirst.Select(segment => segment.EntityId).ToArray());
        Assert.Equal([TestWorld.PlayerId, ChestId, GemId], path.SharedRootToSecond.Select(segment => segment.EntityId).ToArray());
    }

    [Fact]
    public void EntityContainmentPathServiceReportsNoSharedRoot()
    {
        var world = TestWorld.CreateWorld();
        var service = new EntityContainmentPathService();

        var path = service.GetSharedRootPath(world, TestWorld.PlayerId, TestWorld.SlimeId);

        Assert.Equal(EntityContainmentPathStatus.NoSharedRoot, path.Status);
        Assert.Null(path.SharedRootEntityId);
        Assert.Empty(path.SharedRootToFirst);
        Assert.Empty(path.SharedRootToSecond);
        Assert.Contains(path.Diagnostics, diagnostic => diagnostic.Contains("No shared containment root", StringComparison.Ordinal));
    }

    [Fact]
    public void EntityContainmentPathServiceSharedRootPathIsCycleSafe()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        var service = new EntityContainmentPathService();

        var path = service.GetSharedRootPath(world, TestWorld.PlayerId, TestWorld.RockId);

        Assert.Equal(EntityContainmentPathStatus.CycleDetected, path.Status);
        Assert.NotEmpty(path.Cycles);
        Assert.Contains(path.Diagnostics, diagnostic => diagnostic.Contains("Cycle detected", StringComparison.Ordinal));
    }

    private static WorldState CreateTwoBranchWorld()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        AddPlane(world, ChestInventoryPlaneId, width: 1, height: 1);
        AddEntity(world, ChestId, "Chest", new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 1)), inventoryWidth: 1, inventoryHeight: 1);
        AddEntity(world, GemId, "Gem", new PlaneCoord(ChestInventoryPlaneId, new GridCoord(0, 0)), inventoryWidth: 0, inventoryHeight: 0);
        world.RegisterInventoryPlane(ChestId, ChestInventoryPlaneId);
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0)));
        return world;
    }

    private static void AddPlane(WorldState world, PlaneId planeId, int width, int height)
    {
        world.Planes.Add(planeId, new Plane(planeId, planeId.Value, width, height));
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                world.AddNode(planeId, new GridCoord(x, y));
            }
        }
    }

    private static void AddEntity(WorldState world, EntityId entityId, string name, PlaneCoord location, int inventoryWidth, int inventoryHeight)
    {
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, inventoryWidth, inventoryHeight, Weight: 1, CarryingCapacity: 10));
        world.Occupancy.Add(nodeId, entityId);
    }
}
