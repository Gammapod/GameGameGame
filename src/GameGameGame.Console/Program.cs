using GameGameGame.Core;
using GameGameGame.Content;
using GameGameGame.ConsoleApp;

var game = args.Length >= 2
    ? ConsoleScenarioLauncher.CreateFromFile(args[0], args[1])
    : ConsoleScenarioLauncher.CreatePrototype();
var world = game.World;
var registry = game.Registry;
var playerId = game.PlayerEntityId;
var movement = new MovementService();
var inspector = new EntityInspectionService(entityId => registry.GetPresentationForEntity(entityId).ToInspectionAppearance());
var turns = new TurnService(movement, game.ActionPlans);

var running = true;
var mode = InputMode.Play;
var worldCursor = new GridCoord(0, 0);
var inventoryCursor = new GridCoord(0, 0);
EntityId? selectedEntity = null;
var inspectedEntity = playerId;
var message = game.ValidationDiagnostics.Count == 0 && game.RuntimeFailures.Count == 0
    ? $"Scenario {game.ScenarioId}. Arrow keys move. I inspect. P picks up. D drops. Q or Esc quits."
    : $"Scenario {game.ScenarioId} has diagnostics: {string.Join(" | ", game.ValidationDiagnostics.Concat(game.RuntimeFailures))}";

Console.CursorVisible = false;

while (running)
{
    Render(world, inspector, registry, mode, worldCursor, inventoryCursor, selectedEntity, inspectedEntity, playerId, game.ActionPlans, message);

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
        turns.TakeActorTurnThenAdvance(world, playerId, PlannedActionPlan.Single(new MoveAction(moveDirection)));
        message = "Player acted. Other entities took their turns.";
        return;
    }

    if (key is ConsoleKey.P)
    {
        worldCursor = world.GetEntityLocation(playerId).Coord;
        selectedEntity = null;
        mode = InputMode.PickupSource;
        message = "Pick an adjacent entity in the world pane. Enter selects. Esc cancels.";
        return;
    }

    if (key is ConsoleKey.D)
    {
        inventoryCursor = new GridCoord(0, 0);
        inspectedEntity = playerId;
        selectedEntity = null;
        mode = InputMode.DropSource;
        message = "Pick an entity in the inventory pane. Enter selects. Esc cancels.";
        return;
    }

    if (key is ConsoleKey.I)
    {
        worldCursor = world.GetEntityLocation(playerId).Coord;
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

    var playerPlaneId = world.GetEntityLocation(playerId).PlaneId;
    worldCursor = MoveCursor(worldCursor, key, world.Planes[playerPlaneId]);

    if (key is not ConsoleKey.Enter)
    {
        return;
    }

    var target = world.GetOccupant(new PlaneCoord(playerPlaneId, worldCursor));

    if (target is null || target == playerId)
    {
        message = "There is no pickup target at that world cell.";
        return;
    }

    selectedEntity = target;
    inventoryCursor = new GridCoord(0, 0);
    inspectedEntity = playerId;
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

    var evaluation = action.Evaluate(world, playerId, movement);

    if (!evaluation.CanExecute)
    {
        world.RecordTrace(evaluation.Trace);
        message = FormatFailure(evaluation.Trace);
        return;
    }

    turns.TakeActorTurnThenAdvance(world, playerId, PlannedActionPlan.Single(action));
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

    var playerPlaneId = world.GetEntityLocation(playerId).PlaneId;
    worldCursor = MoveCursor(worldCursor, key, world.Planes[playerPlaneId]);

    if (key is not ConsoleKey.Enter || selectedEntity is not { } target)
    {
        return;
    }

    var action = new DropAction(target, new PlaneCoord(playerPlaneId, worldCursor));

    var evaluation = action.Evaluate(world, playerId, movement);

    if (!evaluation.CanExecute)
    {
        world.RecordTrace(evaluation.Trace);
        message = FormatFailure(evaluation.Trace);
        return;
    }

    turns.TakeActorTurnThenAdvance(world, playerId, PlannedActionPlan.Single(action));
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

    if (world.GetInventoryPlaneId(containerId) is not { } planeId)
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
    world.GetInventoryPlaneId(playerId)
    ?? throw new InvalidOperationException("Player does not have an inventory plane.");

EntityId GetPlayerContainerEntityId()
{
    var playerPlaneId = world.GetEntityLocation(playerId).PlaneId;
    return inspector.FindEntityContainingPlane(world, playerPlaneId) ?? playerId;
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
    EntityInspectionService inspector,
    PrototypeContentRegistry registry,
    InputMode mode,
    GridCoord worldCursor,
    GridCoord inventoryCursor,
    EntityId? selectedEntity,
    EntityId inspectedEntity,
    EntityId playerId,
    IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
    string message)
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine("GameGameGame prototype");
    Console.WriteLine("Arrow keys move/select. I inspect. P pickup. D drop. Enter confirms. Esc cancels/quits. Q quits.");
    Console.WriteLine($"Turn: {world.TurnNumber} | Mode: {mode}");
    Console.WriteLine(message.PadRight(Console.WindowWidth - 1));

    if (selectedEntity is { } entityId)
    {
        Console.WriteLine($"Selected: {world.FormatEntityAddress(entityId)}");
    }
    else
    {
        Console.WriteLine("Selected: none");
    }

    Console.WriteLine();

    var playerPlaneId = world.GetEntityLocation(playerId).PlaneId;
    var containerId = inspector.FindEntityContainingPlane(world, playerPlaneId) ?? playerId;

    var currentContainerPanel = inspector.Inspect(world, containerId);
    DrawInspectionPanel(
        currentContainerPanel,
        left: 0,
        top: 6,
        width: 38,
        title: "Current Container",
        cursor: mode is InputMode.PickupSource or InputMode.DropDestination or InputMode.InspectSource ? worldCursor : null,
        turnOrderReport: CreateLocalTurnOrderReport(world, currentContainerPanel, actionPlans, playerId, registry));

    var selectedInspectionPanel = inspector.Inspect(world, inspectedEntity);
    DrawInspectionPanel(
        selectedInspectionPanel,
        left: 40,
        top: 6,
        width: 38,
        title: "Selected Inspection",
        cursor: mode is InputMode.PickupDestination or InputMode.DropSource ? inventoryCursor : null,
        turnOrderReport: CreateLocalTurnOrderReport(world, selectedInspectionPanel, actionPlans, playerId, registry));

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

static LocalTurnOrderReport? CreateLocalTurnOrderReport(
    WorldState world,
    EntityInspectionPanel panel,
    IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
    EntityId playerId,
    PrototypeContentRegistry registry) =>
    panel.InventoryGrid is { } grid
        ? LocalTurnOrderReport.Create(
            world,
            grid.PlaneId,
            actionPlans,
            playerId,
            entityId => registry.GetPresentationForEntity(entityId).Glyph)
        : null;

static void DrawInspectionPanel(
    EntityInspectionPanel panel,
    int left,
    int top,
    int width,
    string title,
    GridCoord? cursor,
    LocalTurnOrderReport? turnOrderReport)
{
    Console.SetCursorPosition(left, top);
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.Write(TrimToWidth($"{title}: {panel.Name} {panel.Address}", width));

    Console.SetCursorPosition(left, top + 1);
    Console.ForegroundColor = ToConsoleColor(panel.Color);
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
            Console.ForegroundColor = ToConsoleColor(cell.Color);
            Console.Write(cell.Glyph);

            Console.ResetColor();
        }
    }

    if (turnOrderReport is null)
    {
        return;
    }

    var reportTop = top + grid.Height + 11;
    var reportLine = 0;
    foreach (var line in LocalTurnOrderReportFormatter.Format(turnOrderReport).Take(8))
    {
        Console.SetCursorPosition(left, reportTop + reportLine);
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(TrimToWidth(line, width));
        reportLine++;
    }
}

static string TrimToWidth(string text, int width) =>
    text.Length <= width ? text.PadRight(width) : text[..Math.Max(0, width - 1)] + " ";

static ConsoleColor ToConsoleColor(PresentationColor color) => color switch
{
    PresentationColor.White => ConsoleColor.White,
    PresentationColor.Yellow => ConsoleColor.Yellow,
    PresentationColor.Cyan => ConsoleColor.Cyan,
    PresentationColor.Green => ConsoleColor.Green,
    PresentationColor.DarkGreen => ConsoleColor.DarkGreen,
    PresentationColor.Earth => ConsoleColor.DarkYellow,
    PresentationColor.Gray => ConsoleColor.DarkGray,
    _ => ConsoleColor.Gray
};

internal enum InputMode
{
    Play,
    PickupSource,
    PickupDestination,
    DropSource,
    DropDestination,
    InspectSource
}
