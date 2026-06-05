using GameGameGame.Core;

var world = WorldBuilder.CreateFirstSliceWorld();
var movement = new MovementService();
var inspector = new EntityInspectionService();
var turns = new TurnService(
    movement,
    new Dictionary<EntityId, IEntityActionPlan>
    {
        [WorldBuilder.SlimeId] = new WanderingSlimeActionPlan(),
        [WorldBuilder.GiantSlimeId] = new WanderingSlimeActionPlan()
    });

var running = true;
var mode = InputMode.Play;
var worldCursor = new GridCoord(0, 0);
var inventoryCursor = new GridCoord(0, 0);
EntityId? selectedEntity = null;
var inspectedEntity = WorldBuilder.PlayerId;
var message = "Arrow keys move. I inspect. P picks up. D drops. Q or Esc quits.";

Console.CursorVisible = false;

while (running)
{
    Render(world, mode, worldCursor, inventoryCursor, selectedEntity, inspectedEntity, message);

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
        case InputMode.InspectSource:
            HandleInspectSourceInput(key);
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
        inspectedEntity = WorldBuilder.PlayerId;
        selectedEntity = null;
        mode = InputMode.DropSource;
        message = "Pick an entity in the inventory pane. Enter selects. Esc cancels.";
        return;
    }

    if (key is ConsoleKey.I)
    {
        worldCursor = world.GetEntityLocation(WorldBuilder.PlayerId).Coord;
        selectedEntity = null;
        mode = InputMode.InspectSource;
        message = "Pick an entity in the left panel inventory grid. Enter inspects. Esc cancels.";
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
    inspectedEntity = WorldBuilder.PlayerId;
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

    var evaluation = action.Evaluate(world, WorldBuilder.PlayerId, movement);

    if (!evaluation.CanExecute)
    {
        world.RecordTrace(evaluation.Trace);
        message = FormatFailure(evaluation.Trace);
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

    var evaluation = action.Evaluate(world, WorldBuilder.PlayerId, movement);

    if (!evaluation.CanExecute)
    {
        world.RecordTrace(evaluation.Trace);
        message = FormatFailure(evaluation.Trace);
        return;
    }

    turns.TakePlayerTurn(world, PlannedActionPlan.Single(action));
    mode = InputMode.Play;
    selectedEntity = null;
    message = "Dropped entity. Other entities took their turns.";
}

void HandleInspectSourceInput(ConsoleKey key)
{
    if (CancelSelection(key))
    {
        return;
    }

    var containerId = GetPlayerContainerEntityId();
    var container = world.Entities[containerId];

    if (container.InventoryPlaneId is not { } planeId)
    {
        message = "The current container has no inspectable inventory plane.";
        mode = InputMode.Play;
        return;
    }

    worldCursor = MoveCursor(worldCursor, key, world.Planes[planeId]);

    if (key is not ConsoleKey.Enter)
    {
        return;
    }

    var target = world.GetOccupant(new PlaneCoord(planeId, worldCursor));

    if (target is null)
    {
        message = "There is no entity at that cell to inspect.";
        return;
    }

    inspectedEntity = target.Value;
    mode = InputMode.Play;
    message = $"Inspecting {world.Entities[target.Value].Name}.";
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

EntityId GetPlayerContainerEntityId()
{
    var playerPlaneId = world.GetEntityLocation(WorldBuilder.PlayerId).PlaneId;
    return inspector.FindEntityContainingPlane(world, playerPlaneId) ?? WorldBuilder.PlayerId;
}

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
    EntityId inspectedEntity,
    string message)
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine("GameGameGame prototype");
    Console.WriteLine("Arrow keys move/select. I inspect. P pickup. D drop. Enter confirms. Esc cancels/quits. Q quits.");
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

    var inspector = new EntityInspectionService();
    var playerPlaneId = world.GetEntityLocation(WorldBuilder.PlayerId).PlaneId;
    var containerId = inspector.FindEntityContainingPlane(world, playerPlaneId) ?? WorldBuilder.PlayerId;

    DrawInspectionPanel(
        inspector.Inspect(world, containerId),
        left: 0,
        top: 6,
        width: 38,
        title: "Current Container",
        cursor: mode is InputMode.PickupSource or InputMode.DropDestination or InputMode.InspectSource ? worldCursor : null);

    DrawInspectionPanel(
        inspector.Inspect(world, inspectedEntity),
        left: 40,
        top: 6,
        width: 38,
        title: "Selected Inspection",
        cursor: mode is InputMode.PickupDestination or InputMode.DropSource ? inventoryCursor : null);

    Console.SetCursorPosition(0, 21);
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine(world.FormatEntityAddress(WorldBuilder.PlayerId).PadRight(Console.WindowWidth - 1));
    Console.WriteLine(world.FormatEntityAddress(WorldBuilder.SlimeId).PadRight(Console.WindowWidth - 1));
    Console.WriteLine(world.FormatEntityAddress(WorldBuilder.RockId).PadRight(Console.WindowWidth - 1));
    Console.WriteLine(message.PadRight(Console.WindowWidth - 1));

    if (world.LastTrace is { } trace)
    {
        Console.WriteLine("Last trace:".PadRight(Console.WindowWidth - 1));
        var line = 0;
        WriteTrace(trace, depth: 0, line, maxLines: 8);
    }
}

static int WriteTrace(TraceNode trace, int depth, int line, int maxLines)
{
    if (line >= maxLines)
    {
        return line;
    }

    var indent = new string(' ', depth * 2);
    var reason = trace.Reason == FailureReason.None ? string.Empty : $" [{trace.Reason}]";
    var detail = string.IsNullOrWhiteSpace(trace.Detail) ? string.Empty : $": {trace.Detail}";
    var text = $"{indent}{trace.Status}: {trace.Label}{reason}{detail}";

    Console.WriteLine(text.PadRight(Console.WindowWidth - 1));
    line++;

    foreach (var child in trace.Children)
    {
        line = WriteTrace(child, depth + 1, line, maxLines);

        if (line >= maxLines)
        {
            break;
        }
    }

    return line;
}

static string FormatFailure(TraceNode trace)
{
    var failure = FindFailure(trace) ?? trace;
    var reason = failure.Reason == FailureReason.None ? "failed" : failure.Reason.ToString();

    return string.IsNullOrWhiteSpace(failure.Detail)
        ? $"Action failed: {reason}."
        : $"Action failed: {reason}. {failure.Detail}";
}

static TraceNode? FindFailure(TraceNode trace)
{
    if (trace.Status == TraceStatus.Failure)
    {
        return trace;
    }

    foreach (var child in trace.Children)
    {
        var failure = FindFailure(child);

        if (failure is not null)
        {
            return failure;
        }
    }

    return null;
}

static void DrawInspectionPanel(EntityInspectionPanel panel, int left, int top, int width, string title, GridCoord? cursor)
{
    Console.SetCursorPosition(left, top);
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.Write(TrimToWidth($"{title}: {panel.Name} {panel.Address}", width));

    Console.SetCursorPosition(left, top + 1);
    Console.ForegroundColor = panel.Color;
    Console.Write(TrimToWidth($"{panel.Glyph} {panel.EntityId}", width));

    var propertyLine = 0;
    foreach (var property in panel.Properties.Take(6))
    {
        Console.SetCursorPosition(left, top + 2 + propertyLine);
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(TrimToWidth($"{property.Name}: {property.Value}", width));
        propertyLine++;
    }

    Console.SetCursorPosition(left, top + 9);
    Console.ForegroundColor = ConsoleColor.Gray;

    if (panel.InventoryGrid is not { } grid)
    {
        Console.Write(TrimToWidth("Inventory: none", width));
        return;
    }

    Console.Write(TrimToWidth($"Inventory: {grid.PlaneId} ({grid.Width}x{grid.Height})", width));

    for (var y = 0; y < grid.Height; y++)
    {
        Console.SetCursorPosition(left, top + y + 10);

        for (var x = 0; x < grid.Width; x++)
        {
            var coord = new GridCoord(x, y);
            var isCursor = cursor == coord;
            var cell = grid.Cells.Single(cell => cell.Coord == coord);

            Console.BackgroundColor = isCursor ? ConsoleColor.DarkYellow : ConsoleColor.Black;
            Console.ForegroundColor = cell.Color;
            Console.Write(cell.Glyph);

            Console.ResetColor();
        }
    }
}

static string TrimToWidth(string text, int width) =>
    text.Length <= width ? text.PadRight(width) : text[..Math.Max(0, width - 1)] + " ";

internal enum InputMode
{
    Play,
    PickupSource,
    PickupDestination,
    DropSource,
    DropDestination,
    InspectSource
}
