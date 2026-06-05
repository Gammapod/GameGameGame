namespace GameGameGame.Core;

public static class WorldBuilder
{
    public static readonly EntityId GameId = new("game");
    public static readonly EntityId PlayerId = new("player");
    public static readonly EntityId SlimeId = new("slime");
    public static readonly PlaneId GamePlaneId = new("gamePlane");
    public static readonly PlaneId GameInventoryPlaneId = new("world");
    public static readonly PlaneId PlayerInventoryPlaneId = new("player");

    public static WorldState CreateFirstSliceWorld()
    {
        var world = new WorldState();

        AddRectangularPlane(world, new Plane(GamePlaneId, "Game Plane", 1, 1));
        AddRectangularPlane(world, new Plane(GameInventoryPlaneId, "World", 5, 5));
        AddRectangularPlane(world, new Plane(PlayerInventoryPlaneId, "Player Inventory", 3, 2));

        var gameNode = world.GetNodeId(new PlaneCoord(GamePlaneId, new GridCoord(0, 0)));
        var playerNode = world.GetNodeId(new PlaneCoord(GameInventoryPlaneId, new GridCoord(2, 2)));
        var slimeNode = world.GetNodeId(new PlaneCoord(GameInventoryPlaneId, new GridCoord(1, 1)));

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
            PlayerInventoryPlaneId));

        world.Entities.Add(SlimeId, new Entity(
            SlimeId,
            "Slime",
            's',
            ConsoleColor.Green,
            slimeNode,
            null));

        world.Occupancy.Add(gameNode, GameId);
        world.Occupancy.Add(playerNode, PlayerId);
        world.Occupancy.Add(slimeNode, SlimeId);

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
