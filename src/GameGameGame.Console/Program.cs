using GameGameGame.Core;

var world = WorldBuilder.CreateFirstSliceWorld();
var movement = new MovementService();
var running = true;

Console.CursorVisible = false;

while (running)
{
    Render(world);

    var key = Console.ReadKey(intercept: true).Key;
    var direction = key switch
    {
        ConsoleKey.UpArrow => Direction.North,
        ConsoleKey.DownArrow => Direction.South,
        ConsoleKey.LeftArrow => Direction.West,
        ConsoleKey.RightArrow => Direction.East,
        _ => (Direction?)null
    };

    if (key is ConsoleKey.Escape or ConsoleKey.Q)
    {
        running = false;
    }
    else if (direction is { } moveDirection)
    {
        movement.TryMove(world, WorldBuilder.PlayerId, moveDirection);
    }
}

Console.ResetColor();
Console.CursorVisible = true;
Console.Clear();

static void Render(WorldState world)
{
    var plane = world.Planes[WorldBuilder.GameInventoryPlaneId];

    Console.SetCursorPosition(0, 0);
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine("GameGameGame prototype");
    Console.WriteLine("Arrow keys move. Q or Esc quits.");
    Console.WriteLine();
    Console.WriteLine($"Currently simulated plane: {plane.Name} ({plane.Width}x{plane.Height})");
    Console.WriteLine();

    for (var y = 0; y < plane.Height; y++)
    {
        for (var x = 0; x < plane.Width; x++)
        {
            var nodeId = world.GetNodeId(new PlaneCoord(plane.Id, new GridCoord(x, y)));

            if (world.Occupancy.TryGetValue(nodeId, out var entityId))
            {
                var entity = world.Entities[entityId];
                Console.ForegroundColor = entity.Color;
                Console.Write(entity.Glyph);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write('.');
            }
        }

        Console.WriteLine();
    }

    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine();
    Console.WriteLine(world.FormatEntityAddress(WorldBuilder.PlayerId).PadRight(Console.WindowWidth - 1));
}
