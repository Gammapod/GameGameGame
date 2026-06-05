namespace GameGameGame.Core;

public static class WorldBuilder
{
    public static readonly EntityId GameId = new("game");
    public static readonly EntityId PlayerId = new("player");
    public static readonly PlaneId GamePlaneId = new("gamePlane");
    public static readonly PlaneId GameInventoryPlaneId = new("gameInventory");

    public static WorldState CreateFirstSliceWorld()
    {
        var world = new WorldState();

        AddRectangularPlane(world, new Plane(GamePlaneId, "Game Plane", 1, 1));
        AddRectangularPlane(world, new Plane(GameInventoryPlaneId, "Game Inventory", 5, 5));

        var gameNode = world.GetNodeId(new PlaneCoord(GamePlaneId, new GridCoord(0, 0)));
        var playerNode = world.GetNodeId(new PlaneCoord(GameInventoryPlaneId, new GridCoord(2, 2)));

        world.Entities.Add(GameId, new Entity(
            GameId,
            "Game",
            'G',
            ConsoleColor.Yellow,
            gameNode,
            GameInventoryPlaneId));

        world.Entities.Add(PlayerId, new Entity(
            PlayerId,
            "Player",
            '@',
            ConsoleColor.Cyan,
            playerNode,
            null));

        world.Occupancy.Add(gameNode, GameId);
        world.Occupancy.Add(playerNode, PlayerId);

        return world;
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
