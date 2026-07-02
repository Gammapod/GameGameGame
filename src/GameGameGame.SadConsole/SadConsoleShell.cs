using GameGameGame.Content;
using GameGameGame.Core;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using GggDirection = GameGameGame.Core.Direction;
using GggColor = GameGameGame.Content.PresentationColor;

namespace GameGameGame.SadConsoleApp;

internal sealed class SadConsoleShell : Console
{
    public const int ScreenWidth = 120;
    public const int ScreenHeight = 42;
    private const int PanelTop = 6;
    private const int GlobalLogTop = 33;

    private readonly MovementService _movement = new();
    private readonly EntityPanelProjectionService _panelProjection;
    private readonly ControlledActorAffordanceService _affordances;
    private readonly ScenarioCatalogResult? _catalog;
    private PlayableScenarioSession? _session;
    private ControlledActorCommandService? _commands;
    private ActionLogProjection? _actionLog;
    private readonly List<ActionOutcome> _outcomes = [];
    private ShellMode _mode;
    private int _selectedScenarioIndex;
    private GridCoord _worldCursor = new(0, 0);
    private GridCoord _inventoryCursor = new(0, 0);
    private EntityId? _selectedEntity;
    private EntityId? _inspectedEntity;
    private string _message;

    public SadConsoleShell(SadConsoleStartup startup) : base(ScreenWidth, ScreenHeight)
    {
        UseKeyboard = true;
        IsFocused = true;
        FocusedMode = FocusBehavior.Set;

        _catalog = startup.Catalog;
        _message = startup.Error ?? "Enter launches. Up/Down selects. Esc quits.";
        _panelProjection = new EntityPanelProjectionService(entityId =>
            _session?.Registry.GetPresentationForEntity(entityId).ToInspectionAppearance()
            ?? new EntityInspectionAppearance('?', GggColor.Gray));
        _affordances = new ControlledActorAffordanceService(_movement);

        if (startup.DirectSession is { } direct)
        {
            StartSession(direct);
        }
        else if (startup.Error is null && (_catalog?.Entries.Count ?? 0) == 0)
        {
            _message = "No scenarios found. Esc quits.";
        }

        Redraw();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyReleased(Keys.Escape))
        {
            if (_mode == ShellMode.Menu)
            {
                SadConsole.Game.Instance.MonoGameInstance.Exit();
            }
            else if (_mode == ShellMode.Play)
            {
                ReturnToMenuOrExit();
            }
            else
            {
                _mode = ShellMode.Play;
                _selectedEntity = null;
                _message = "Selection cancelled.";
            }

            Redraw();
            return true;
        }

        if (_mode == ShellMode.Menu)
        {
            HandleMenuInput(keyboard);
        }
        else
        {
            HandleSessionInput(keyboard);
        }

        Redraw();
        return true;
    }

    private void HandleMenuInput(Keyboard keyboard)
    {
        if (_catalog is null || _catalog.Entries.Count == 0)
        {
            return;
        }

        if (keyboard.IsKeyReleased(Keys.Up))
        {
            _selectedScenarioIndex = Math.Max(0, _selectedScenarioIndex - 1);
        }
        else if (keyboard.IsKeyReleased(Keys.Down))
        {
            _selectedScenarioIndex = Math.Min(_catalog.Entries.Count - 1, _selectedScenarioIndex + 1);
        }
        else if (keyboard.IsKeyReleased(Keys.Enter))
        {
            var entry = _catalog.Entries[_selectedScenarioIndex];
            try
            {
                StartSession(PlayableScenarioLauncher.CreateFromCatalogEntry(entry));
            }
            catch (Exception ex)
            {
                _message = $"Could not launch {entry.ScenarioId}: {ex.Message}";
            }
        }
    }

    private void HandleSessionInput(Keyboard keyboard)
    {
        if (_session is null || _commands is null)
        {
            return;
        }

        switch (_mode)
        {
            case ShellMode.Play:
                HandlePlayInput(keyboard);
                break;
            case ShellMode.InspectSource:
                HandleInspectSourceInput(keyboard);
                break;
            case ShellMode.PickupSource:
                HandlePickupSourceInput(keyboard);
                break;
            case ShellMode.PickupDestination:
                HandlePickupDestinationInput(keyboard);
                break;
            case ShellMode.DropSource:
                HandleDropSourceInput(keyboard);
                break;
            case ShellMode.DropDestination:
                HandleDropDestinationInput(keyboard);
                break;
            case ShellMode.EnterSource:
                HandleEnterSourceInput(keyboard);
                break;
            case ShellMode.ExitDirection:
                HandleExitDirectionInput(keyboard);
                break;
        }
    }

    private void HandlePlayInput(Keyboard keyboard)
    {
        var direction = ReadDirection(keyboard);
        if (direction is { } moveDirection)
        {
            Execute(ControlledActorCommand.Move(moveDirection), "Player acted.");
            return;
        }

        if (keyboard.IsKeyReleased(Keys.I))
        {
            _worldCursor = PlayerLocation().Coord;
            _mode = ShellMode.InspectSource;
            _message = "Inspect mode: move cursor in current container, Enter inspects.";
        }
        else if (keyboard.IsKeyReleased(Keys.P))
        {
            _worldCursor = PlayerLocation().Coord;
            _mode = ShellMode.PickupSource;
            _message = "Pickup mode: choose adjacent source, Enter selects.";
        }
        else if (keyboard.IsKeyReleased(Keys.D))
        {
            _inventoryCursor = new GridCoord(0, 0);
            _inspectedEntity = _session!.PlayerEntityId;
            _mode = ShellMode.DropSource;
            _message = "Drop mode: choose carried item, Enter selects.";
        }
        else if (keyboard.IsKeyReleased(Keys.E))
        {
            _worldCursor = PlayerLocation().Coord;
            _mode = ShellMode.EnterSource;
            _message = "Enter mode: choose adjacent entity, Enter enters.";
        }
        else if (keyboard.IsKeyReleased(Keys.X))
        {
            _mode = ShellMode.ExitDirection;
            _message = "Exit mode: choose exit direction with an arrow key.";
        }
    }

    private void HandleInspectSourceInput(Keyboard keyboard)
    {
        var planeId = _session!.World.GetInventoryPlaneId(CurrentContainerEntityId());
        if (planeId is null)
        {
            _mode = ShellMode.Play;
            _message = "Current container has no inspectable inventory.";
            return;
        }

        MoveCursor(keyboard, planeId.Value, ref _worldCursor);
        if (!keyboard.IsKeyReleased(Keys.Enter)) return;

        var target = _session.World.GetOccupant(new PlaneCoord(planeId.Value, _worldCursor));
        if (target is null)
        {
            _message = "No entity at that cell.";
            return;
        }

        _inspectedEntity = target;
        _mode = ShellMode.Play;
        _message = $"Inspecting {_session.World.Entities[target.Value].Name}.";
    }

    private void HandlePickupSourceInput(Keyboard keyboard)
    {
        var playerPlaneId = PlayerLocation().PlaneId;
        MoveCursor(keyboard, playerPlaneId, ref _worldCursor);
        if (!keyboard.IsKeyReleased(Keys.Enter)) return;

        var target = _session!.World.GetOccupant(new PlaneCoord(playerPlaneId, _worldCursor));
        if (target is null || target == _session.PlayerEntityId)
        {
            _message = "No pickup target at that cell.";
            return;
        }

        _selectedEntity = target;
        _inventoryCursor = new GridCoord(0, 0);
        _inspectedEntity = _session.PlayerEntityId;
        _mode = ShellMode.PickupDestination;
        _message = $"Choose inventory destination for {_session.World.Entities[target.Value].Name}.";
    }

    private void HandlePickupDestinationInput(Keyboard keyboard)
    {
        var inventoryPlaneId = _session!.World.GetInventoryPlaneId(_session.PlayerEntityId);
        if (inventoryPlaneId is null)
        {
            _mode = ShellMode.Play;
            _message = "Player has no inventory.";
            return;
        }

        MoveCursor(keyboard, inventoryPlaneId.Value, ref _inventoryCursor);
        if (!keyboard.IsKeyReleased(Keys.Enter) || _selectedEntity is not { } target) return;
        Execute(ControlledActorCommand.Pickup(target, new PlaneCoord(inventoryPlaneId.Value, _inventoryCursor)), "Picked up entity.");
    }

    private void HandleDropSourceInput(Keyboard keyboard)
    {
        var inventoryPlaneId = _session!.World.GetInventoryPlaneId(_session.PlayerEntityId);
        if (inventoryPlaneId is null)
        {
            _mode = ShellMode.Play;
            _message = "Player has no inventory.";
            return;
        }

        MoveCursor(keyboard, inventoryPlaneId.Value, ref _inventoryCursor);
        if (!keyboard.IsKeyReleased(Keys.Enter)) return;

        var target = _session.World.GetOccupant(new PlaneCoord(inventoryPlaneId.Value, _inventoryCursor));
        if (target is null)
        {
            _message = "No carried entity at that inventory cell.";
            return;
        }

        _selectedEntity = target;
        _worldCursor = PlayerLocation().Coord;
        _mode = ShellMode.DropDestination;
        _message = $"Choose world destination for {_session.World.Entities[target.Value].Name}.";
    }

    private void HandleDropDestinationInput(Keyboard keyboard)
    {
        var playerPlaneId = PlayerLocation().PlaneId;
        MoveCursor(keyboard, playerPlaneId, ref _worldCursor);
        if (!keyboard.IsKeyReleased(Keys.Enter) || _selectedEntity is not { } target) return;
        Execute(ControlledActorCommand.Drop(target, new PlaneCoord(playerPlaneId, _worldCursor)), "Dropped entity.");
    }

    private void HandleEnterSourceInput(Keyboard keyboard)
    {
        var playerPlaneId = PlayerLocation().PlaneId;
        MoveCursor(keyboard, playerPlaneId, ref _worldCursor);
        if (!keyboard.IsKeyReleased(Keys.Enter)) return;

        var target = _session!.World.GetOccupant(new PlaneCoord(playerPlaneId, _worldCursor));
        if (target is null || target == _session.PlayerEntityId)
        {
            _message = "No enter target at that cell.";
            return;
        }

        Execute(ControlledActorCommand.Enter(target.Value), "Entered entity.");
        _inspectedEntity = _session.PlayerEntityId;
    }

    private void HandleExitDirectionInput(Keyboard keyboard)
    {
        var direction = ReadDirection(keyboard);
        if (direction is { } exitDirection)
        {
            Execute(ControlledActorCommand.Exit(exitDirection), "Exited entity.");
            _inspectedEntity = _session!.PlayerEntityId;
        }
    }

    private void Execute(ControlledActorCommand command, string successMessage)
    {
        var result = _commands!.Execute(_session!.World, _session.PlayerEntityId, command);
        _outcomes.Add(ActionOutcomeProjection.FromCommandResult(_session.World, result));
        _actionLog = ActionLogProjection.FromOutcomes(_outcomes);
        _mode = ShellMode.Play;
        _selectedEntity = null;
        _worldCursor = PlayerLocation().Coord;
        _message = result.Succeeded ? successMessage : FormatFailure(result);
    }

    private void StartSession(PlayableScenarioSession session)
    {
        _session = session;
        _commands = new ControlledActorCommandService(_movement, session.ActionPlans, (world, entityId) => TargetingService.RefreshTargets(world, session.Registry, entityId));
        _mode = ShellMode.Play;
        _selectedEntity = null;
        _inspectedEntity = session.PlayerEntityId;
        _worldCursor = PlayerLocation().Coord;
        _inventoryCursor = new GridCoord(0, 0);
        _outcomes.Clear();
        _actionLog = null;
        _message = session.ValidationDiagnostics.Count == 0 && session.RuntimeFailures.Count == 0
            ? $"Scenario {session.ScenarioId}. Arrows move. I inspect. P pickup. D drop. E enter. X exit. Esc returns."
            : $"Scenario {session.ScenarioId} diagnostics: {string.Join(" | ", session.ValidationDiagnostics.Concat(session.RuntimeFailures))}";
    }

    private void ReturnToMenuOrExit()
    {
        if (_catalog is null)
        {
            SadConsole.Game.Instance.MonoGameInstance.Exit();
            return;
        }

        _session = null;
        _commands = null;
        _outcomes.Clear();
        _actionLog = null;
        _mode = ShellMode.Menu;
        _message = "Returned to scenario list. Enter launches. Esc quits.";
    }

    private void Redraw()
    {
        ClearSurface();
        if (_mode == ShellMode.Menu)
        {
            DrawMenu();
        }
        else
        {
            DrawSession();
        }
        Surface.IsDirty = true;
    }

    private void DrawMenu()
    {
        PrintText(1, 0, "GameGameGame SadConsole debug browser", Color.Yellow);
        PrintClipped(1, 1, Width - 2, _message, Color.White);

        if (_catalog is null)
        {
            return;
        }

        var maxEntries = Height - 6;
        var first = Math.Max(0, Math.Min(_selectedScenarioIndex - maxEntries / 2, Math.Max(0, _catalog.Entries.Count - maxEntries)));
        for (var index = first; index < _catalog.Entries.Count && index < first + maxEntries; index++)
        {
            var entry = _catalog.Entries[index];
            var y = 4 + index - first;
            var selected = index == _selectedScenarioIndex;
            PrintClipped(2, y, Width - 4, $"{(selected ? '>' : ' ')} {entry.Name} ({entry.ScenarioId}) - {entry.ContentPath}", selected ? Color.Yellow : Color.White);
            if (!string.IsNullOrWhiteSpace(entry.Description) && y + 1 < Height)
            {
                PrintClipped(6, ++y, Width - 8, entry.Description, Color.Gray);
            }
        }

        var diagnosticY = Height - Math.Min(3, _catalog.Diagnostics.Count);
        foreach (var diagnostic in _catalog.Diagnostics.Take(3))
        {
            PrintClipped(1, diagnosticY++, Width - 2, $"Catalog diagnostic: {diagnostic}", Color.Orange);
        }
    }

    private void DrawSession()
    {
        if (_session is null)
        {
            return;
        }

        var world = _session.World;
        PrintText(1, 0, $"GameGameGame SadConsole | { _session.Name } | Turn {world.TurnNumber} | Mode {_mode}", Color.Yellow);
        PrintClipped(1, 1, Width - 2, _message, Color.White);

        var affordances = _affordances.Query(world, _session.PlayerEntityId);
        PrintClipped(1, 2, Width - 2, FormatAffordances(affordances), Color.Gray);
        PrintClipped(1, 3, Width - 2, _selectedEntity is { } selected ? $"Selected: {world.FormatEntityAddress(selected)}" : "Selected: none", Color.Gray);
        PrintClipped(1, 4, Width - 2, FormatPromptHint(affordances), Color.DarkGray);

        var containerProjection = _panelProjection.Project(world, CurrentContainerEntityId(), _session.ActionPlans, _session.PlayerEntityId, _actionLog);
        var inspectedProjection = _panelProjection.Project(world, _inspectedEntity ?? _session.PlayerEntityId, _session.ActionPlans, _session.PlayerEntityId, _actionLog);
        DrawPanel(containerProjection, 1, PanelTop, 56, GlobalLogTop - 1, "Current Container", UsesWorldCursor() ? _worldCursor : null, affordances);
        DrawPanel(inspectedProjection, 60, PanelTop, 58, GlobalLogTop - 1, "Inspection", UsesInventoryCursor() ? _inventoryCursor : null, affordances);

        DrawGlobalLog();
    }

    private void DrawPanel(
        EntityPanelProjection panel,
        int left,
        int top,
        int width,
        int bottom,
        string title,
        GridCoord? cursor,
        ControlledActorAffordances affordances)
    {
        PrintClipped(left, top, width, $"{title}: {panel.Glyph} {panel.Name} {panel.EntityId}", Color.Yellow);
        PrintClipped(left, top + 1, width, $"Path: {FormatBreadcrumb(panel.Breadcrumb)}", Color.Gray);
        PrintClipped(left, top + 2, width, $"Location: {panel.Location} | Facing: {panel.ActionState.Facing?.ToString() ?? "none"} | Target: {panel.ActionState.Target?.ToString() ?? "none"}", Color.Gray);

        var y = top + 3;
        foreach (var property in panel.Properties.Take(4))
        {
            PrintClipped(left, y++, width, $"{property.Name}: {property.Value}", Color.White);
        }

        if (panel.ActionPlanSummary is { } actionPlan)
        {
            PrintClipped(left, y++, width, $"Plan: {actionPlan}", Color.White);
        }

        if (panel.InventoryGrid is not { } grid)
        {
            PrintClipped(left, y, width, "Inventory: none", Color.Gray);
            return;
        }

        PrintClipped(left, y++, width, $"Inventory: {grid.PlaneId} ({grid.Width}x{grid.Height})", Color.White);
        var highlights = BuildHighlights(grid.PlaneId, affordances);
        for (var row = 0; row < grid.Height && y < bottom - 8; row++, y++)
        {
            for (var x = 0; x < grid.Width && x < width - 1; x++)
            {
                var coord = new GridCoord(x, row);
                var cell = grid.Cells.Single(cell => cell.Coord == coord);
                var occupant = cell.EntityId;
                var foreground = ToSadColor(cell.Color);
                var background = BackgroundForCell(grid.PlaneId, coord, occupant, cursor, highlights);
                SetCell(left + x, y, cell.Glyph, foreground, background);
            }
        }

        if (y >= bottom)
        {
            return;
        }

        PrintClipped(left, y++, width, "Contents", Color.Yellow);
        foreach (var row in panel.Contents.Take(6))
        {
            if (y >= bottom) return;
            PrintClipped(left, y++, width, $"{row.Order}. {row.Glyph} {row.EntityName}{FormatEntityStateSuffix(row.EntityId)} [{row.Participation}] {row.PreviousAction}", Color.White);
        }

        if (y < bottom && panel.LocalLog.Count > 0)
        {
            PrintClipped(left, y++, width, "Local log", Color.Yellow);
        }

        foreach (var outcome in panel.LocalLog.TakeLast(Math.Max(0, bottom - y)))
        {
            if (y >= bottom) return;
            PrintClipped(left, y++, width, outcome.Sentence, outcome.Succeeded ? Color.LightGreen : Color.Orange);
        }
    }

    private void DrawGlobalLog()
    {
        PrintText(1, GlobalLogTop, "Global controlled-command log", Color.Yellow);
        if (_actionLog is null || _actionLog.Chronological.Count == 0)
        {
            PrintClipped(1, GlobalLogTop + 1, Width - 2, "No controlled commands submitted yet.", Color.DarkGray);
            return;
        }

        var y = GlobalLogTop + 1;
        foreach (var outcome in _actionLog.Chronological.TakeLast(ScreenHeight - GlobalLogTop - 1))
        {
            var turn = outcome.TurnNumber is { } turnNumber ? $"T{turnNumber}: " : string.Empty;
            PrintClipped(1, y++, Width - 2, $"{turn}{outcome.Sentence}", outcome.Succeeded ? Color.LightGreen : Color.Orange);
        }
    }

    private IReadOnlyDictionary<GridCoord, CellHighlight> BuildHighlights(PlaneId planeId, ControlledActorAffordances affordances)
    {
        var highlights = new Dictionary<GridCoord, CellHighlight>();

        switch (_mode)
        {
            case ShellMode.Play:
                foreach (var movement in affordances.MovementDirections.Where(affordance => affordance.Destination?.PlaneId == planeId))
                {
                    AddHighlight(highlights, movement.Destination!.Value.Coord, movement.CanExecute ? CellHighlight.Valid : CellHighlight.Invalid);
                }

                foreach (var source in affordances.PickupSources.Concat(affordances.EnterTargets).Where(affordance => affordance.Source?.PlaneId == planeId && affordance.CanExecute))
                {
                    AddHighlight(highlights, source.Source!.Value.Coord, CellHighlight.Valid);
                }
                break;

            case ShellMode.PickupSource:
                foreach (var source in affordances.PickupSources.Where(affordance => affordance.Source?.PlaneId == planeId))
                {
                    AddHighlight(highlights, source.Source!.Value.Coord, source.CanExecute ? CellHighlight.Valid : CellHighlight.Invalid);
                }
                break;

            case ShellMode.PickupDestination when _selectedEntity is { } pickupTarget:
                foreach (var destination in affordances.PickupDestinations(pickupTarget).Where(affordance => affordance.Destination.PlaneId == planeId))
                {
                    AddHighlight(highlights, destination.Destination.Coord, destination.CanExecute ? CellHighlight.Valid : CellHighlight.Invalid);
                }
                break;

            case ShellMode.DropSource:
                foreach (var source in affordances.DropSources.Where(affordance => affordance.Source?.PlaneId == planeId))
                {
                    AddHighlight(highlights, source.Source!.Value.Coord, source.CanExecute ? CellHighlight.Valid : CellHighlight.Invalid);
                }
                break;

            case ShellMode.DropDestination when _selectedEntity is { } dropTarget:
                foreach (var destination in affordances.DropDestinations(dropTarget).Where(affordance => affordance.Destination.PlaneId == planeId))
                {
                    AddHighlight(highlights, destination.Destination.Coord, destination.CanExecute ? CellHighlight.Valid : CellHighlight.Invalid);
                }
                break;

            case ShellMode.EnterSource:
                foreach (var source in affordances.EnterTargets.Where(affordance => affordance.Source?.PlaneId == planeId))
                {
                    AddHighlight(highlights, source.Source!.Value.Coord, source.CanExecute ? CellHighlight.Valid : CellHighlight.Invalid);
                }
                break;

            case ShellMode.ExitDirection:
                foreach (var exit in affordances.ExitDirections.Where(affordance => affordance.Destination?.PlaneId == planeId))
                {
                    AddHighlight(highlights, exit.Destination!.Value.Coord, exit.CanExecute ? CellHighlight.Valid : CellHighlight.Invalid);
                }
                break;
        }

        return highlights;
    }

    private static void AddHighlight(Dictionary<GridCoord, CellHighlight> highlights, GridCoord coord, CellHighlight highlight)
    {
        if (!highlights.TryGetValue(coord, out var existing) || existing != CellHighlight.Valid)
        {
            highlights[coord] = highlight;
        }
    }

    private Color BackgroundForCell(
        PlaneId planeId,
        GridCoord coord,
        EntityId? occupant,
        GridCoord? cursor,
        IReadOnlyDictionary<GridCoord, CellHighlight> highlights)
    {
        if (cursor == coord)
        {
            return Color.DarkGoldenrod;
        }

        if (occupant == _session?.PlayerEntityId)
        {
            return Color.DarkBlue;
        }

        if (occupant is { } entityId && entityId == _selectedEntity)
        {
            return Color.DarkMagenta;
        }

        if (occupant is { } targetId && _session?.World.GetActionTarget(_session.PlayerEntityId) == targetId)
        {
            return Color.Purple;
        }

        if (highlights.TryGetValue(coord, out var highlight))
        {
            return highlight == CellHighlight.Valid ? Color.DarkGreen : Color.DarkRed;
        }

        return Color.Black;
    }

    private string FormatEntityStateSuffix(EntityId entityId)
    {
        if (_session is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (_session.World.GetActionFacing(entityId) is { } facing)
        {
            parts.Add($"F:{FacingArrow(facing)}");
        }

        if (_session.World.GetActionTarget(entityId) is { } target)
        {
            parts.Add($"T:{FormatEntityShortName(target)}");
        }

        return parts.Count == 0 ? string.Empty : $" ({string.Join(' ', parts)})";
    }

    private string FormatEntityShortName(EntityId entityId) =>
        _session is { } session && session.World.Entities.TryGetValue(entityId, out var entity)
            ? entity.Name
            : entityId.Value;

    private static string FacingArrow(GggDirection direction) => direction switch
    {
        GggDirection.North => "N↑",
        GggDirection.South => "S↓",
        GggDirection.West => "W←",
        GggDirection.East => "E→",
        _ => direction.ToString()
    };

    private EntityId CurrentContainerEntityId()
    {
        var playerPlaneId = PlayerLocation().PlaneId;
        return InventoryPlaneOwnership.TryFindOwner(_session!.World, playerPlaneId, out var containerId)
            ? containerId
            : _session.PlayerEntityId;
    }

    private PlaneCoord PlayerLocation() => _session!.World.GetEntityLocation(_session.PlayerEntityId);

    private void MoveCursor(Keyboard keyboard, PlaneId planeId, ref GridCoord cursor)
    {
        if (ReadDirection(keyboard) is not { } direction || !_session!.World.Planes.TryGetValue(planeId, out var plane)) return;
        var next = cursor.Offset(direction);
        if (plane.Contains(next)) cursor = next;
    }

    private static GggDirection? ReadDirection(Keyboard keyboard) =>
        keyboard.IsKeyReleased(Keys.Up) ? GggDirection.North :
        keyboard.IsKeyReleased(Keys.Down) ? GggDirection.South :
        keyboard.IsKeyReleased(Keys.Left) ? GggDirection.West :
        keyboard.IsKeyReleased(Keys.Right) ? GggDirection.East :
        null;

    private bool UsesWorldCursor() => _mode is ShellMode.PickupSource or ShellMode.DropDestination or ShellMode.InspectSource or ShellMode.EnterSource;
    private bool UsesInventoryCursor() => _mode is ShellMode.PickupDestination or ShellMode.DropSource;

    private void PrintClipped(int x, int y, int width, string text, Color color)
    {
        if (y < 0 || y >= Height || x >= Width) return;
        var clipped = text.Length <= width ? text : text[..Math.Max(0, width - 1)];
        PrintText(x, y, clipped.PadRight(Math.Max(0, width)), color);
    }

    private void ClearSurface()
    {
        for (var y = 0; y < ScreenHeight; y++)
        {
            for (var x = 0; x < ScreenWidth; x++)
            {
                SetCell(x, y, ' ', Color.White, Color.Black);
            }
        }
    }

    private void PrintText(int x, int y, string text, Color foreground)
    {
        for (var index = 0; index < text.Length && x + index < ScreenWidth; index++)
        {
            SetCell(x + index, y, text[index], foreground, Color.Black);
        }
    }

    private void SetCell(int x, int y, int glyph, Color foreground, Color background)
    {
        if (x < 0 || y < 0 || x >= ScreenWidth || y >= ScreenHeight)
        {
            return;
        }

        var cell = Surface[x, y];
        cell.Glyph = glyph;
        cell.Foreground = foreground;
        cell.Background = background;
        cell.IsDirty = true;
    }

    private static string FormatBreadcrumb(EntityContainmentPath path)
    {
        var text = path.Segments.Count == 0
            ? path.RequestedEntityId.ToString()
            : string.Join(" > ", path.Segments.Select(segment => segment.EntityId.Value));
        return path.Status == EntityContainmentPathStatus.Complete ? text : $"{text} [{path.Status}]";
    }

    private static string FormatAffordances(ControlledActorAffordances affordances)
    {
        var moves = string.Join(", ", affordances.MovementDirections.Where(a => a.CanExecute).Select(a => a.Direction));
        return $"Valid moves: {(string.IsNullOrWhiteSpace(moves) ? "none" : moves)} | pickups: {affordances.PickupSources.Count(a => a.CanExecute)} | drops: {affordances.DropSources.Count(a => a.CanExecute)} | enter: {affordances.EnterTargets.Count(a => a.CanExecute)} | exit: {affordances.ExitDirections.Count(a => a.CanExecute)}";
    }

    private string FormatPromptHint(ControlledActorAffordances affordances)
    {
        return _mode switch
        {
            ShellMode.Play => "Highlights: green valid action target, red blocked move, blue controlled entity, purple current target, gold cursor. Facing/target appear in text.",
            ShellMode.PickupSource => FormatEntityAffordanceHint("Pickup source", affordances.PickupSources),
            ShellMode.PickupDestination when _selectedEntity is { } target => FormatDestinationAffordanceHint("Pickup destination", affordances.PickupDestinations(target)),
            ShellMode.DropSource => FormatEntityAffordanceHint("Drop source", affordances.DropSources),
            ShellMode.DropDestination when _selectedEntity is { } target => FormatDestinationAffordanceHint("Drop destination", affordances.DropDestinations(target)),
            ShellMode.EnterSource => FormatEntityAffordanceHint("Enter target", affordances.EnterTargets),
            ShellMode.ExitDirection => FormatDirectionAffordanceHint("Exit", affordances.ExitDirections),
            ShellMode.InspectSource => "Inspect: gold cursor selects visible entities in the current container panel.",
            _ => string.Empty
        };
    }

    private static string FormatEntityAffordanceHint(string label, IReadOnlyList<ControlledActorEntityAffordance> affordances)
    {
        var valid = affordances.Count(affordance => affordance.CanExecute);
        var blocked = affordances.FirstOrDefault(affordance => !affordance.CanExecute && !string.IsNullOrWhiteSpace(affordance.FailureDetail));
        return blocked is null
            ? $"{label}: {valid} valid highlighted target(s)."
            : $"{label}: {valid} valid target(s). Blocked: {blocked.FailureReason} {blocked.FailureDetail}";
    }

    private static string FormatDestinationAffordanceHint(string label, IReadOnlyList<ControlledActorDestinationAffordance> affordances)
    {
        var valid = affordances.Count(affordance => affordance.CanExecute);
        var blocked = affordances.FirstOrDefault(affordance => !affordance.CanExecute && !string.IsNullOrWhiteSpace(affordance.FailureDetail));
        return blocked is null
            ? $"{label}: {valid} valid highlighted cell(s)."
            : $"{label}: {valid} valid cell(s). Blocked: {blocked.FailureReason} {blocked.FailureDetail}";
    }

    private static string FormatDirectionAffordanceHint(string label, IReadOnlyList<ControlledActorDirectionAffordance> affordances)
    {
        var valid = affordances.Count(affordance => affordance.CanExecute);
        var blocked = affordances.FirstOrDefault(affordance => !affordance.CanExecute && !string.IsNullOrWhiteSpace(affordance.FailureDetail));
        return blocked is null
            ? $"{label}: {valid} valid highlighted direction(s)."
            : $"{label}: {valid} valid direction(s). Blocked: {blocked.FailureReason} {blocked.FailureDetail}";
    }

    private static string FormatFailure(ControlledActorCommandResult result) =>
        string.IsNullOrWhiteSpace(result.FailureDetail)
            ? $"Action failed: {result.FailureReason?.ToString() ?? "failed"}."
            : $"Action failed: {result.FailureReason?.ToString() ?? "failed"}. {result.FailureDetail}";

    private static Color ToSadColor(GggColor color) => color switch
    {
        GggColor.White => Color.White,
        GggColor.Yellow => Color.Yellow,
        GggColor.Cyan => Color.Cyan,
        GggColor.Green => Color.Green,
        GggColor.DarkGreen => Color.DarkGreen,
        GggColor.Earth => Color.SaddleBrown,
        GggColor.Gray => Color.Gray,
        _ => Color.White
    };
}

internal enum ShellMode
{
    Menu,
    Play,
    InspectSource,
    PickupSource,
    PickupDestination,
    DropSource,
    DropDestination,
    EnterSource,
    ExitDirection
}

internal enum CellHighlight
{
    Invalid,
    Valid
}
