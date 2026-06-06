using GameGameGame.Core;

namespace GameGameGame.Content;

public static class PrototypeContent
{
    public static readonly EntityId GameId = new("game");
    public static readonly EntityId PlayerId = new("player");
    public static readonly EntityId SlimeId = new("slime");
    public static readonly EntityId GiantSlimeId = new("giantSlime");
    public static readonly EntityId RockId = new("rock");

    public static readonly PlaneId GamePlaneId = new("gamePlane");
    public static readonly PlaneId GameInventoryPlaneId = new("world");
    public static readonly PlaneId PlayerInventoryPlaneId = new("player");
    public static readonly PlaneId SlimeInventoryPlaneId = new("slime");
    public static readonly PlaneId GiantSlimeInventoryPlaneId = new("giantSlime");

    public static WorldState CreateFirstSliceWorld()
    {
        var world = new WorldState();

        AddRectangularPlane(world, new Plane(GamePlaneId, "Game Plane", 1, 1));
        AddEntity(world, new Entity(GameId, "Game", 'G', ConsoleColor.Yellow, world.GetNodeId(new PlaneCoord(GamePlaneId, new GridCoord(0, 0))), 5, 5, 0, 0), GameInventoryPlaneId, "World");
        AddEntity(world, new Entity(PlayerId, "Player", '@', ConsoleColor.Cyan, world.GetNodeId(new PlaneCoord(GameInventoryPlaneId, new GridCoord(1, 2))), 3, 2, 10, 5), PlayerInventoryPlaneId, "Player Inventory");
        AddEntity(world, new Entity(SlimeId, "Slime", 's', ConsoleColor.Green, world.GetNodeId(new PlaneCoord(GameInventoryPlaneId, new GridCoord(1, 1))), 1, 1, 3, 3), SlimeInventoryPlaneId, "Slime Inventory");
        AddEntity(world, new Entity(RockId, "Rock", '*', ConsoleColor.DarkYellow, world.GetNodeId(new PlaneCoord(GameInventoryPlaneId, new GridCoord(2, 1))), 0, 0, 3, 3));
        AddEntity(world, new Entity(GiantSlimeId, "Giant Slime", 'S', ConsoleColor.DarkGreen, world.GetNodeId(new PlaneCoord(GameInventoryPlaneId, new GridCoord(3, 3))), 3, 3, 20, 20), GiantSlimeInventoryPlaneId, "Giant Slime Inventory");

        return world;
    }

    public static IReadOnlyDictionary<EntityId, IEntityActionPlan> CreatePrototypeActionPlans() =>
        new Dictionary<EntityId, IEntityActionPlan>
        {
            [SlimeId] = new WanderingSlimeActionPlan(),
            [GiantSlimeId] = new WanderingSlimeActionPlan()
        };

    private static void AddEntity(WorldState world, Entity entity, PlaneId? inventoryPlaneId = null, string? inventoryPlaneName = null)
    {
        if (entity.HasUsableInventory)
        {
            if (inventoryPlaneId is not { } planeId)
            {
                planeId = new PlaneId(entity.Id.Value);
            }

            AddRectangularPlane(world, new Plane(planeId, inventoryPlaneName ?? $"{entity.Name} Inventory", entity.InventoryWidth, entity.InventoryHeight));
            world.RegisterInventoryPlane(entity.Id, planeId);
        }

        world.Entities.Add(entity.Id, entity);
        world.Occupancy.Add(entity.OccupiedNodeId, entity.Id);
    }

    private static void AddRectangularPlane(WorldState world, Plane plane)
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
