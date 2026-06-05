using GameGameGame.Core;

var world = WorldBuilder.CreateFirstSliceWorld();
var movement = new MovementService();
var turns = new TurnService(
    movement,
    new Dictionary<EntityId, IEntityBehavior>
    {
        [WorldBuilder.SlimeId] = new AlternatingHorizontalBehavior()
    });

var running = true;
var mode = InputMode.Play;
var worldCursor = new GridCoord(0, 0);
var inventoryCursor = new GridCoord(0, 0);
EntityId? selectedEntity = null;
var message = "Arrow keys move. P picks up. D drops. Q or Esc quits.";

Console.CursorVisible = false;

while (running)
{
    Render(world, mode, worldCursor, inventoryCursor, selectedEntity, message);

    var key = Console.ReadKey(intercept: true).Key;

    if (key is ConsoleKey.Q)
    {
        running = false;
        continue;
    }

    switch (mode)
    {
        case InputMode.Play:
            HandlePlayInput(key);
            break;
        case InputMode.PickupSource:
            HandlePickupSourceInput(key);
            break;
        case InputMode.PickupDestination:
            HandlePickupDestinationInput(key);
            break;
        case InputMode.DropSource:
            HandleDropSourceInput(key);
            break;
        case InputMode.DropDestination:
            HandleDropDestinationInput(key);
            break;
    }
}

Console.ResetColor();
Console.CursorVisible = true;
Console.Clear();

void HandlePlayInput(ConsoleKey key)
{
    if (key is ConsoleKey.Escape)
    {
        running = false;
        return;
    }

    var direction = KeyToDirection(key);

    if (direction is { } moveDirection)
    {
        turns.TakePlayerTurn(world, PlannedActionPlan.Single(new MoveAction(moveDirection)));
        message = "Player acted. Other entities took their turns.";
        return;
    }

    if (key is ConsoleKey.P)
    {
        worldCursor = world.GetEntityLocation(WorldBuilder.PlayerId).Coord;
        selectedEntity = null;
        mode = InputMode.PickupSource;
        message = "Pick an adjacent entity in the world pane. Enter selects. Esc cancels.";
        return;
    }

    if (key is ConsoleKey.D)
    {
        inventoryCursor = new GridCoord(0, 0);
        selectedEntity = null;
        mode = InputMode.DropSource;
        message = "Pick an entity in the inventory pane. Enter selects. Esc cancels.";
    }
}

void HandlePickupSourceInput(ConsoleKey key)
{
    if (CancelSelection(key))
    {
        return;
    }

    var playerPlaneId = world.GetEntityLocation(WorldBuilder.PlayerId).PlaneId;
    worldCursor = MoveCursor(worldCursor, key, world.Planes[playerPlaneId]);

    if (key is not ConsoleKey.Enter)
    {
        return;
    }

    var target = world.GetOccupant(new PlaneCoord(playerPlaneId, worldCursor));

    if (target is null || target == WorldBuilder.PlayerId)
    {
        message = "There is no pickup target at that world cell.";
        return;
    }

    selectedEntity = target;
    inventoryCursor = new GridCoord(0, 0);
    mode = InputMode.PickupDestination;
    message = $"Choose an inventory destination for {world.Entities[target.Value].Name}.";
}

void HandlePickupDestinationInput(ConsoleKey key)
{
    if (CancelSelection(key))
    {
        return;
    }

    var inventoryPlaneId = GetPlayerInventoryPlaneId();
    inventoryCursor = MoveCursor(inventoryCursor, key, world.Planes[inventoryPlaneId]);

    if (key is not ConsoleKey.Enter || selectedEntity is not { } target)
    {
        return;
    }

    var action = new PickupAction(target, new PlaneCoord(inventoryPlaneId, inventoryCursor));

    if (!action.CanExecute(world, WorldBuilder.PlayerId, movement))
    {
        message = "That pickup is not legal. Target must be adjacent and destination must be empty.";
        return;
    }

    turns.TakePlayerTurn(world, PlannedActionPlan.Single(action));
    mode = InputMode.Play;
    selectedEntity = null;
    message = "Picked up entity. Other entities took their turns.";
}

void HandleDropSourceInput(ConsoleKey key)
{
    if (CancelSelection(key))
    {
        return;
    }

    var inventoryPlaneId = GetPlayerInventoryPlaneId();
    inventoryCursor = MoveCursor(inventoryCursor, key, world.Planes[inventoryPlaneId]);

    if (key is not ConsoleKey.Enter)
    {
        return;
    }

    var target = world.GetOccupant(new PlaneCoord(inventoryPlaneId, inventoryCursor));

    if (target is null)
    {
        message = "There is no entity at that inventory cell.";
        return;
    }

    selectedEntity = target;
    worldCursor = new GridCoord(0, 0);
    mode = InputMode.DropDestination;
    message = $"Choose a world destination for {world.Entities[target.Value].Name}.";
}

void HandleDropDestinationInput(ConsoleKey key)
{
    if (CancelSelection(key))
    {
        return;
    }

    var playerPlaneId = world.GetEntityLocation(WorldBuilder.PlayerId).PlaneId;
    worldCursor = MoveCursor(worldCursor, key, world.Planes[playerPlaneId]);

    if (key is not ConsoleKey.Enter || selectedEntity is not { } target)
    {
        return;
    }

    var action = new DropAction(target, new PlaneCoord(playerPlaneId, worldCursor));

    if (!action.CanExecute(world, WorldBuilder.PlayerId, movement))
    {
        message = "That drop is not legal. Destination must be empty on the player's plane.";
        return;
    }

    turns.TakePlayerTurn(world, PlannedActionPlan.Single(action));
    mode = InputMode.Play;
    selectedEntity = null;
    message = "Dropped entity. Other entities took their turns.";
}

bool CancelSelection(ConsoleKey key)
{
    if (key is not ConsoleKey.Escape)
    {
        return false;
    }

    mode = InputMode.Play;
    selectedEntity = null;
    message = "Selection cancelled.";
    return true;
}

PlaneId GetPlayerInventoryPlaneId() =>
    world.Entities[WorldBuilder.PlayerId].InventoryPlaneId
    ?? throw new InvalidOperationException("Player does not have an inventory plane.");

static Direction? KeyToDirection(ConsoleKey key) => key switch
{
    ConsoleKey.UpArrow => Direction.North,
    ConsoleKey.DownArrow => Direction.South,
    ConsoleKey.LeftArrow => Direction.West,
    ConsoleKey.RightArrow => Direction.East,
    _ => null
};

static GridCoord MoveCursor(GridCoord cursor, ConsoleKey key, Plane plane)
{
    var direction = KeyToDirection(key);

    if (direction is null)
    {
        return cursor;
    }

    var next = cursor.Offset(direction.Value);

    return plane.Contains(next) ? next : cursor;
}

static void Render(
    WorldState world,
    InputMode mode,
    GridCoord worldCursor,
    GridCoord inventoryCursor,
    EntityId? selectedEntity,
    string message)
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine("GameGameGame prototype");
    Console.WriteLine("Arrow keys move/select. P pickup. D drop. Enter confirms. Esc cancels/quits. Q quits.");
    Console.WriteLine($"Turn: {world.TurnNumber} | Mode: {mode}");

    if (selectedEntity is { } entityId)
    {
        Console.WriteLine($"Selected: {world.FormatEntityAddress(entityId)}");
    }
    else
    {
        Console.WriteLine("Selected: none");
    }

    Console.WriteLine();

    var playerPlaneId = world.GetEntityLocation(WorldBuilder.PlayerId).PlaneId;
    var inventoryPlaneId = world.Entities[WorldBuilder.PlayerId].InventoryPlaneId;

    DrawPlane(
        world,
        world.Planes[playerPlaneId],
        left: 0,
        top: 6,
        title: "Player Plane",
        cursor: mode is InputMode.PickupSource or InputMode.DropDestination ? worldCursor : null);

    if (inventoryPlaneId is { } planeId)
    {
        DrawPlane(
            world,
            world.Planes[planeId],
            left: 24,
            top: 6,
            title: "Player Inventory",
            cursor: mode is InputMode.PickupDestination or InputMode.DropSource ? inventoryCursor : null);
    }

    Console.SetCursorPosition(0, 15);
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine(world.FormatEntityAddress(WorldBuilder.PlayerId).PadRight(Console.WindowWidth - 1));
    Console.WriteLine(world.FormatEntityAddress(WorldBuilder.SlimeId).PadRight(Console.WindowWidth - 1));
    Console.WriteLine(message.PadRight(Console.WindowWidth - 1));
}

static void DrawPlane(WorldState world, Plane plane, int left, int top, string title, GridCoord? cursor)
{
    Console.SetCursorPosition(left, top);
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine($"{title}: {plane.Id} ({plane.Width}x{plane.Height})");

    for (var y = 0; y < plane.Height; y++)
    {
        Console.SetCursorPosition(left, top + y + 1);

        for (var x = 0; x < plane.Width; x++)
        {
            var coord = new GridCoord(x, y);
            var isCursor = cursor == coord;
            var nodeId = world.GetNodeId(new PlaneCoord(plane.Id, coord));

            Console.BackgroundColor = isCursor ? ConsoleColor.DarkYellow : ConsoleColor.Black;

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

            Console.ResetColor();
        }
    }
}

internal enum InputMode
{
    Play,
    PickupSource,
    PickupDestination,
    DropSource,
    DropDestination
}
