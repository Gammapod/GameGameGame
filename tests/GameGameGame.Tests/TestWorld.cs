using GameGameGame.Core;

namespace GameGameGame.Tests;

internal static class TestWorld
{
    public static readonly EntityId PlayerId = new("player");
    public static readonly EntityId SlimeId = new("slime");
    public static readonly EntityId RockId = new("rock");

    public static readonly PlaneId WorldPlaneId = new("world");
    public static readonly PlaneId PlayerInventoryPlaneId = new("player");
    public static readonly PlaneId SlimeInventoryPlaneId = new("slime");

    public static WorldState CreateWorld()
    {
        var world = new WorldState();
        AddPlane(world, new Plane(WorldPlaneId, "World", 5, 5));
        AddPlane(world, new Plane(PlayerInventoryPlaneId, "Player Inventory", 3, 2));
        AddPlane(world, new Plane(SlimeInventoryPlaneId, "Slime Inventory", 1, 1));

        AddEntity(world, PlayerId, "Player", new PlaneCoord(WorldPlaneId, new GridCoord(1, 2)), inventoryWidth: 3, inventoryHeight: 2, bulk: 10, aperture: 5);
        AddEntity(world, SlimeId, "Slime", new PlaneCoord(WorldPlaneId, new GridCoord(1, 1)), inventoryWidth: 1, inventoryHeight: 1, bulk: 3, aperture: 20);
        AddEntity(world, RockId, "Rock", new PlaneCoord(WorldPlaneId, new GridCoord(2, 1)), inventoryWidth: 0, inventoryHeight: 0, bulk: 3, aperture: 3);

        world.RegisterInventoryPlane(PlayerId, PlayerInventoryPlaneId);
        world.RegisterInventoryPlane(SlimeId, SlimeInventoryPlaneId);

        return world;
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
}
