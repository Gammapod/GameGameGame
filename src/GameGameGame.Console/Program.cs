using GameGameGame.Core;
using GameGameGame.Content;
using GameGameGame.ConsoleApp;
using GameGameGame.Headless;

if (args.Length > 0 && args[0] == "record-scenario")
{
    return RecordScenarioCommand(args);
}

if (args.Length > 0 && args[0] == "scan-scenarios")
{
    return ScanScenariosCommand(args);
}

ConsoleGameSession game = null!;
WorldState world = null!;
PrototypeContentRegistry registry = null!;
EntityId playerId = default;
MovementService movement = null!;
EntityInspectionService inspector = null!;
TurnService turns = null!;
var running = false;
var mode = InputMode.Play;
var worldCursor = new GridCoord(0, 0);
var inventoryCursor = new GridCoord(0, 0);
EntityId? selectedEntity = null;
EntityId inspectedEntity = default;
var message = string.Empty;

if (TryCreateDirectSession(args, out var directSession, out var directError))
{
    RunGameSession(directSession);
    return 0;
}

if (directError is not null)
{
    Console.Error.WriteLine(directError);
    return 2;
}

var catalog = ResolveScenarioCatalog(args);
if (catalog.Diagnostics.Count > 0)
{
    foreach (var diagnostic in catalog.Diagnostics)
    {
        Console.Error.WriteLine(diagnostic);
    }
}

if (catalog.Entries.Count == 0)
{
    Console.Error.WriteLine("No scenarios found.");
    return 1;
}

RunScenarioMenu(catalog);

return 0;

int ScanScenariosCommand(string[] commandArgs)
{
    var folder = commandArgs.Length >= 2 && !commandArgs[1].StartsWith("--", StringComparison.Ordinal)
        ? commandArgs[1]
        : ScenarioCatalog.DefaultDiscoveryFolder;
    var output = ScenarioCatalog.DefaultManifestPath;

    for (var index = 1; index < commandArgs.Length; index++)
    {
        switch (commandArgs[index])
        {
            case "--output" when index + 1 < commandArgs.Length:
                output = commandArgs[index + 1];
                index++;
                break;
            case "--output":
                Console.Error.WriteLine("Missing value for --output.");
                return 2;
        }
    }

    var catalog = ScenarioCatalog.DiscoverFolder(folder);
    ScenarioCatalog.SaveManifest(catalog, output);
    Console.WriteLine($"Wrote {catalog.Entries.Count} scenario entries to {output}.");
    foreach (var diagnostic in catalog.Diagnostics)
    {
        Console.Error.WriteLine(diagnostic);
    }

    return catalog.Entries.Count == 0 ? 1 : 0;
}

static bool TryCreateDirectSession(string[] commandArgs, out ConsoleGameSession session, out string? error)
{
    session = null!;
    error = null;

    if (commandArgs.Length >= 2 && !commandArgs[0].StartsWith("--", StringComparison.Ordinal))
    {
        session = ConsoleScenarioLauncher.CreateFromFile(commandArgs[0], commandArgs[1]);
        return true;
    }

    if (commandArgs.Length == 1 && !commandArgs[0].StartsWith("--", StringComparison.Ordinal))
    {
        error = "Usage: GameGameGame.Console <content-file> <scenario-id>, --content <file>, --discover <folder>, --manifest <manifest>, or scan-scenarios <folder> --output <manifest>.";
    }

    return false;
}

static ScenarioCatalogResult ResolveScenarioCatalog(string[] commandArgs)
{
    if (commandArgs.Length == 0)
    {
        return File.Exists(ScenarioCatalog.DefaultManifestPath)
            ? ScenarioCatalog.LoadManifest(ScenarioCatalog.DefaultManifestPath)
            : ScenarioCatalog.DiscoverFolder(ScenarioCatalog.DefaultDiscoveryFolder);
    }

    if (commandArgs.Length >= 2 && commandArgs[0] == "--content")
    {
        try
        {
            return ScenarioCatalog.BuildFromDocument(commandArgs[1], EditableContentDocument.LoadYaml(File.ReadAllText(commandArgs[1])));
        }
        catch (Exception ex)
        {
            return new ScenarioCatalogResult([], [$"{commandArgs[1]}: {ex.Message}"]);
        }
    }

    if (commandArgs.Length >= 2 && commandArgs[0] == "--discover")
    {
        return ScenarioCatalog.DiscoverFolder(commandArgs[1]);
    }

    if (commandArgs.Length >= 2 && commandArgs[0] == "--manifest")
    {
        return ScenarioCatalog.LoadManifest(commandArgs[1]);
    }

    return new ScenarioCatalogResult([], ["Usage: GameGameGame.Console <content-file> <scenario-id>, --content <file>, --discover <folder>, --manifest <manifest>, or scan-scenarios <folder> --output <manifest>."]);
}

void RunScenarioMenu(ScenarioCatalogResult catalog)
{
    var selectedIndex = 0;
    var menuMessage = "Enter launches. Up/Down selects. Q or Esc quits.";
    Console.CursorVisible = false;
    try
    {
        while (true)
        {
            RenderScenarioMenu(catalog, selectedIndex, menuMessage);
            var key = Console.ReadKey(intercept: true).Key;

            if (key is ConsoleKey.Q or ConsoleKey.Escape)
            {
                return;
            }

            if (key is ConsoleKey.UpArrow)
            {
                selectedIndex = Math.Max(0, selectedIndex - 1);
                continue;
            }

            if (key is ConsoleKey.DownArrow)
            {
                selectedIndex = Math.Min(catalog.Entries.Count - 1, selectedIndex + 1);
                continue;
            }

            if (key is not ConsoleKey.Enter)
            {
                continue;
            }

            var entry = catalog.Entries[selectedIndex];
            try
            {
                RunGameSession(ConsoleScenarioLauncher.CreateFromCatalogEntry(entry));
                menuMessage = $"Returned from {entry.ScenarioId}. Enter launches. Q quits.";
            }
            catch (Exception ex)
            {
                menuMessage = $"Could not launch {entry.ScenarioId}: {ex.Message}";
            }
        }
    }
    finally
    {
        Console.ResetColor();
        Console.CursorVisible = true;
        Console.Clear();
    }
}

void RunGameSession(ConsoleGameSession session)
{
    game = session;
    world = game.World;
    registry = game.Registry;
    playerId = game.PlayerEntityId;
    movement = new MovementService();
    inspector = new EntityInspectionService(entityId => registry.GetPresentationForEntity(entityId).ToInspectionAppearance());
    turns = new TurnService(movement, game.ActionPlans, (world, entityId) => TargetingService.RefreshTargets(world, registry, entityId));
    running = true;
    mode = InputMode.Play;
    worldCursor = new GridCoord(0, 0);
    inventoryCursor = new GridCoord(0, 0);
    selectedEntity = null;
    inspectedEntity = playerId;
    message = game.ValidationDiagnostics.Count == 0 && game.RuntimeFailures.Count == 0
        ? $"Scenario {game.ScenarioId}. Arrow keys move. I inspect. P picks up. D drops. Q or Esc returns to scenario list."
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
            case InputMode.EnterSource:
                HandleEnterSourceInput(key);
                break;
            case InputMode.ExitDirection:
                HandleExitDirectionInput(key);
                break;
        }
    }

    Console.ResetColor();
    Console.Clear();
}

int RecordScenarioCommand(string[] commandArgs)
{
    if (commandArgs.Length < 4)
    {
        Console.Error.WriteLine("Usage: record-scenario <content-file> <scenario-id> --turns <N> --output <directory>");
        return 2;
    }

    var contentFile = commandArgs[1];
    var scenarioId = commandArgs[2];
    var turns = 1;
    var outputDirectory = commandArgs[3];

    for (var index = 3; index < commandArgs.Length; index++)
    {
        switch (commandArgs[index])
        {
            case "--turns" when index + 1 < commandArgs.Length && int.TryParse(commandArgs[index + 1], out var parsedTurns):
                turns = parsedTurns;
                index++;
                break;
            case "--output" when index + 1 < commandArgs.Length:
                outputDirectory = commandArgs[index + 1];
                index++;
                break;
            case "--turns":
            case "--output":
                Console.Error.WriteLine($"Missing value for {commandArgs[index]}.");
                return 2;
        }
    }

    Directory.CreateDirectory(outputDirectory);
    EditableContentDocument document;
    try
    {
        document = EditableContentDocument.LoadYaml(File.ReadAllText(contentFile));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }

    var report = ScenarioRecordingService.Record(document, new ScenarioRecordingRequest(scenarioId, turns, outputDirectory));
    foreach (var diagnostic in report.ValidationDiagnostics.Concat(report.RuntimeFailures).Concat(report.CapabilityGaps))
    {
        Console.Error.WriteLine(diagnostic);
    }

    if (report.ValidationDiagnostics.Count > 0 || report.RuntimeFailures.Count > 0)
    {
        return 1;
    }

    Console.WriteLine($"Recorded {report.ScenarioId} ({report.Name})");
    Console.WriteLine($"Frames: {report.Frames.Count}");
    foreach (var frame in report.Frames)
    {
        Console.WriteLine($"  {frame.FrameIndex}: turn {frame.TurnNumber} -> {frame.PngPath}");
    }

    if (report.GifPath is { } gifPath)
    {
        Console.WriteLine($"GIF: {gifPath}");
    }

    return 0;
}

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
        return;
    }

    switch (ConsolePlayerControls.GetCommand(key))
    {
        case ConsolePlayerCommand.Enter:
            worldCursor = world.GetEntityLocation(playerId).Coord;
            selectedEntity = null;
            mode = InputMode.EnterSource;
            message = "Pick an adjacent entity to enter. Enter selects. Esc cancels.";
            return;
        case ConsolePlayerCommand.Exit:
            selectedEntity = null;
            mode = InputMode.ExitDirection;
            message = "Choose an exit direction with an arrow key. Esc cancels.";
            return;
    }
}

void HandleEnterSourceInput(ConsoleKey key)
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
        message = "There is no enter target at that world cell.";
        return;
    }

    var action = ConsolePlayerControls.CreateEnterAction(target.Value);
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
    worldCursor = world.GetEntityLocation(playerId).Coord;
    inspectedEntity = playerId;
    message = "Entered entity. Other entities took their turns.";
}

void HandleExitDirectionInput(ConsoleKey key)
{
    if (CancelSelection(key))
    {
        return;
    }

    var direction = KeyToDirection(key);
    if (direction is null)
    {
        return;
    }

    var action = ConsolePlayerControls.CreateExitAction(direction.Value);
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
    worldCursor = world.GetEntityLocation(playerId).Coord;
    inspectedEntity = playerId;
    message = "Exited entity. Other entities took their turns.";
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
    var containmentPaths = new EntityContainmentPathService();
    var playerPath = containmentPaths.GetUpwardPath(world, playerId);
    var inspectedPath = containmentPaths.GetUpwardPath(world, inspectedEntity);
    var getGlyph = (EntityId entityId) => registry.GetPresentationForEntity(entityId).Glyph;

    Console.WriteLine(TrimToWidth($"Player path: {ConsoleInspectionDisplayFormatter.FormatBreadcrumb(world, playerPath, getGlyph)}", Console.WindowWidth - 1));
    Console.WriteLine(TrimToWidth($"Inspecting: {ConsoleInspectionDisplayFormatter.FormatBreadcrumb(world, inspectedPath, getGlyph)}", Console.WindowWidth - 1));
    Console.WriteLine();

    var currentContainerPanel = inspector.Inspect(world, containerId);
    var currentContainerPath = containmentPaths.GetUpwardPath(world, containerId);
    var currentContainerTurnOrder = CreateLocalTurnOrderReport(world, currentContainerPanel, actionPlans, playerId, registry);
    DrawInspectionPanel(
        world,
        currentContainerPanel,
        left: 0,
        top: 9,
        width: 38,
        title: "Current Container",
        cursor: mode is InputMode.PickupSource or InputMode.DropDestination or InputMode.InspectSource or InputMode.EnterSource ? worldCursor : null,
        turnOrderReport: currentContainerTurnOrder,
        path: currentContainerPath,
        getGlyph: getGlyph);

    var selectedInspectionPanel = inspector.Inspect(world, inspectedEntity);
    var selectedTurnOrder = CreateLocalTurnOrderReport(world, selectedInspectionPanel, actionPlans, playerId, registry);
    DrawInspectionPanel(
        world,
        selectedInspectionPanel,
        left: 40,
        top: 9,
        width: 38,
        title: "Selected Inspection",
        cursor: mode is InputMode.PickupDestination or InputMode.DropSource ? inventoryCursor : null,
        turnOrderReport: selectedTurnOrder,
        path: inspectedPath,
        getGlyph: getGlyph);

}

static void RenderScenarioMenu(ScenarioCatalogResult catalog, int selectedIndex, string message)
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine("GameGameGame scenarios");
    Console.WriteLine(message.PadRight(Console.WindowWidth - 1));
    Console.WriteLine();

    var maxEntries = Math.Max(1, Console.WindowHeight - 6);
    var first = Math.Max(0, Math.Min(selectedIndex - maxEntries / 2, Math.Max(0, catalog.Entries.Count - maxEntries)));

    for (var index = first; index < catalog.Entries.Count && index < first + maxEntries; index++)
    {
        var entry = catalog.Entries[index];
        Console.ForegroundColor = index == selectedIndex ? ConsoleColor.Yellow : ConsoleColor.Gray;
        var marker = index == selectedIndex ? ">" : " ";
        Console.WriteLine(TrimToWidth($"{marker} {entry.Name} ({entry.ScenarioId}) - {entry.ContentPath}", Console.WindowWidth - 1));
        if (!string.IsNullOrWhiteSpace(entry.Description))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(TrimToWidth($"    {entry.Description}", Console.WindowWidth - 1));
        }
    }

    if (catalog.Diagnostics.Count == 0)
    {
        return;
    }

    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine();
    foreach (var diagnostic in catalog.Diagnostics.Take(3))
    {
        Console.WriteLine(TrimToWidth($"Catalog diagnostic: {diagnostic}", Console.WindowWidth - 1));
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
    WorldState world,
    EntityInspectionPanel panel,
    int left,
    int top,
    int width,
    string title,
    GridCoord? cursor,
    LocalTurnOrderReport? turnOrderReport,
    EntityContainmentPath path,
    Func<EntityId, char> getGlyph)
{
    Console.SetCursorPosition(left, top);
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.Write(TrimToWidth($"{title}: {panel.Name} {panel.Address}", width));

    Console.SetCursorPosition(left, top + 1);
    Console.ForegroundColor = ToConsoleColor(panel.Color);
    Console.Write(TrimToWidth($"{panel.Glyph} {panel.EntityId}", width));

    var propertyLine = 0;
    var properties = ConsoleInspectionDisplayFormatter.BuildPanelProperties(
        world,
        panel,
        path,
        turnOrderReport,
        getGlyph);
    foreach (var property in properties.Take(7))
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
    InspectSource,
    EnterSource,
    ExitDirection
}
