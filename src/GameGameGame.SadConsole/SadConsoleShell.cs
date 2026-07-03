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
    private readonly MovementService _movement = new();
    private readonly EntityPanelProjectionService _panelProjection;
    private readonly ControlledActorAffordanceService _affordances;
    private readonly SadConsoleSessionViewBuilder _sessionViewBuilder;
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
    private GggDirection? _selectedExitDirection;
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
        _sessionViewBuilder = new SadConsoleSessionViewBuilder(_panelProjection, _affordances);

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
            MoveWorldCursorToFirstValid(InspectCandidates(PlayerLocation().PlaneId));
            _message = "Inspect mode: Tab cycles visible entities, arrows move cursor, Enter inspects.";
        }
        else if (keyboard.IsKeyReleased(Keys.P))
        {
            _worldCursor = PlayerLocation().Coord;
            MoveWorldCursorToFirstValid(_affordances.Query(_session!.World, _session.PlayerEntityId).PickupSources.Select(source => source.Source));
            _mode = ShellMode.PickupSource;
            _message = "Pickup mode: Tab cycles valid sources, arrows move cursor, Enter selects.";
        }
        else if (keyboard.IsKeyReleased(Keys.D))
        {
            _inventoryCursor = new GridCoord(0, 0);
            _inspectedEntity = _session!.PlayerEntityId;
            MoveInventoryCursorToFirstValid(_affordances.Query(_session.World, _session.PlayerEntityId).DropSources.Select(source => source.Source));
            _mode = ShellMode.DropSource;
            _message = "Drop mode: Tab cycles carried items, arrows move cursor, Enter selects.";
        }
        else if (keyboard.IsKeyReleased(Keys.E))
        {
            _worldCursor = PlayerLocation().Coord;
            MoveWorldCursorToFirstValid(_affordances.Query(_session!.World, _session.PlayerEntityId).EnterTargets.Select(source => source.Source));
            _mode = ShellMode.EnterSource;
            _message = "Enter mode: Tab cycles valid targets, arrows move cursor, Enter enters.";
        }
        else if (keyboard.IsKeyReleased(Keys.X))
        {
            var exits = _affordances.Query(_session!.World, _session.PlayerEntityId).ExitDirections;
            _selectedExitDirection = exits.FirstOrDefault(exit => exit.CanExecute)?.Direction;
            MoveWorldCursorToExitDestination(exits, _selectedExitDirection);
            _mode = ShellMode.ExitDirection;
            _message = "Exit mode: Tab cycles valid exits, Enter exits, arrows still choose a direction.";
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

        if (CycleWorldCursor(keyboard, InspectCandidates(planeId.Value), planeId.Value, "Inspect target")) return;

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
        var affordances = _affordances.Query(_session!.World, _session.PlayerEntityId);
        if (CycleWorldCursor(keyboard, affordances.PickupSources.Where(source => source.CanExecute).Select(source => source.Source), playerPlaneId, "Pickup source")) return;

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
        MoveInventoryCursorToFirstValid(affordances.PickupDestinations(target.Value).Where(destination => destination.CanExecute).Select(destination => (PlaneCoord?)destination.Destination));
        _mode = ShellMode.PickupDestination;
        _message = $"Choose inventory destination for {_session.World.Entities[target.Value].Name}. Tab cycles valid cells.";
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

        if (_selectedEntity is { } pickupTarget && CycleInventoryCursor(keyboard, _affordances.Query(_session.World, _session.PlayerEntityId).PickupDestinations(pickupTarget).Where(destination => destination.CanExecute).Select(destination => (PlaneCoord?)destination.Destination), inventoryPlaneId.Value, "Pickup destination")) return;

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

        var affordances = _affordances.Query(_session!.World, _session.PlayerEntityId);
        if (CycleInventoryCursor(keyboard, affordances.DropSources.Where(source => source.CanExecute).Select(source => source.Source), inventoryPlaneId.Value, "Drop source")) return;

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
        MoveWorldCursorToFirstValid(affordances.DropDestinations(target.Value).Where(destination => destination.CanExecute).Select(destination => (PlaneCoord?)destination.Destination));
        _mode = ShellMode.DropDestination;
        _message = $"Choose world destination for {_session.World.Entities[target.Value].Name}. Tab cycles valid cells.";
    }

    private void HandleDropDestinationInput(Keyboard keyboard)
    {
        var playerPlaneId = PlayerLocation().PlaneId;
        if (_selectedEntity is { } dropTarget && CycleWorldCursor(keyboard, _affordances.Query(_session!.World, _session.PlayerEntityId).DropDestinations(dropTarget).Where(destination => destination.CanExecute).Select(destination => (PlaneCoord?)destination.Destination), playerPlaneId, "Drop destination")) return;

        MoveCursor(keyboard, playerPlaneId, ref _worldCursor);
        if (!keyboard.IsKeyReleased(Keys.Enter) || _selectedEntity is not { } target) return;
        Execute(ControlledActorCommand.Drop(target, new PlaneCoord(playerPlaneId, _worldCursor)), "Dropped entity.");
    }

    private void HandleEnterSourceInput(Keyboard keyboard)
    {
        var playerPlaneId = PlayerLocation().PlaneId;
        var affordances = _affordances.Query(_session!.World, _session.PlayerEntityId);
        if (CycleWorldCursor(keyboard, affordances.EnterTargets.Where(source => source.CanExecute).Select(source => source.Source), playerPlaneId, "Enter target")) return;

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
        var exits = _affordances.Query(_session!.World, _session.PlayerEntityId).ExitDirections;
        if (keyboard.IsKeyReleased(Keys.Tab))
        {
            CycleExitDirection(exits);
            return;
        }

        if (keyboard.IsKeyReleased(Keys.Enter) && _selectedExitDirection is { } selectedExit)
        {
            Execute(ControlledActorCommand.Exit(selectedExit), "Exited entity.");
            _inspectedEntity = _session.PlayerEntityId;
            _selectedExitDirection = null;
            return;
        }

        var direction = ReadDirection(keyboard);
        if (direction is { } exitDirection)
        {
            Execute(ControlledActorCommand.Exit(exitDirection), "Exited entity.");
            _inspectedEntity = _session!.PlayerEntityId;
            _selectedExitDirection = null;
        }
    }

    private void Execute(ControlledActorCommand command, string successMessage)
    {
        var result = _commands!.Execute(_session!.World, _session.PlayerEntityId, command);
        _outcomes.Add(ActionOutcomeProjection.FromCommandResult(_session.World, result));
        _actionLog = ActionLogProjection.FromOutcomes(_outcomes);
        _mode = ShellMode.Play;
        _selectedEntity = null;
        _selectedExitDirection = null;
        _worldCursor = PlayerLocation().Coord;
        _message = result.Succeeded ? successMessage : FormatFailure(result);
    }

    private void StartSession(PlayableScenarioSession session)
    {
        _session = session;
        _commands = new ControlledActorCommandService(_movement, session.ActionPlans, (world, entityId) => TargetingService.RefreshTargets(world, session.Registry, entityId));
        _mode = ShellMode.Play;
        _selectedEntity = null;
        _selectedExitDirection = null;
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

        var view = BuildSessionView();
        PrintText(1, 0, view.Header, Color.Yellow);
        PrintClipped(1, 1, Width - 2, view.Message, Color.White);
        PrintClipped(1, 2, Width - 2, view.AffordanceSummary, Color.Gray);
        PrintClipped(1, 3, Width - 2, view.SelectedSummary, Color.Gray);
        PrintClipped(1, 4, Width - 2, view.PromptHint, Color.DarkGray);

        foreach (var panel in view.Panels)
        {
            DrawPanel(panel, view.Affordances);
        }

        DrawGlobalLog(view.GlobalLog);
    }

    private SadConsoleSessionView BuildSessionView()
    {
        return _sessionViewBuilder.Build(
            _session!,
            new SadConsoleSessionViewBuilderState(
                _mode,
                _message,
                _selectedEntity,
                _inspectedEntity,
                _worldCursor,
                _inventoryCursor,
                _actionLog));
    }

    private void DrawPanel(SadConsolePanelView view, ControlledActorAffordances affordances)
    {
        if (view.IsCollapsed)
        {
            DrawCollapsedPanel(view);
            return;
        }

        DrawPanel(view.Projection, view.Bounds, view.Title, view.Cursor, affordances);
    }

    private void DrawCollapsedPanel(SadConsolePanelView view)
    {
        var panel = view.Projection;
        var bounds = view.Bounds;
        PrintClipped(bounds.Left, bounds.Top, bounds.Width, view.Title, Color.Yellow);
        PrintClipped(bounds.Left, bounds.Top + 1, bounds.Width, $"{panel.Glyph} {panel.Name}", Color.White);
        PrintClipped(bounds.Left, bounds.Top + 2, bounds.Width, panel.EntityId.Value, Color.Gray);
        PrintClipped(bounds.Left, bounds.Top + 3, bounds.Width, "collapsed", Color.DarkGray);
    }

    private void DrawPanel(
        EntityPanelProjection panel,
        SadConsoleRect bounds,
        string title,
        GridCoord? cursor,
        ControlledActorAffordances affordances)
    {
        var left = bounds.Left;
        var top = bounds.Top;
        var width = bounds.Width;
        var bottom = bounds.Bottom;

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

        DrawLocalActivity(panel, left, width, bottom, ref y);
    }

    private void DrawLocalActivity(EntityPanelProjection panel, int left, int width, int bottom, ref int y)
    {
        foreach (var row in LocalActivityViewBuilder.Build(panel, bottom - y))
        {
            var x = row.IsHeader ? left : row.Text.StartsWith('└') ? left + 2 : left;
            var rowWidth = row.Text.StartsWith('└') ? Math.Max(0, width - 2) : width;
            var text = row.Text;
            if (!row.IsHeader && !row.Text.StartsWith('└'))
            {
                var content = panel.Contents.FirstOrDefault(contentRow => row.Text.StartsWith($"{contentRow.Order}. {contentRow.Glyph} {contentRow.EntityName}"));
                if (content is not null)
                {
                    text = $"{content.Order}. {content.Glyph} {content.EntityName}{FormatEntityStateSuffix(content.EntityId)} [{content.Participation}]";
                }
            }

            var color = row.IsHeader ? Color.Yellow : row.IsPositive ? Color.LightGreen : row.IsWarning ? Color.Orange : row.IsMuted ? Color.DarkGray : Color.White;
            PrintClipped(x, y++, rowWidth, text, color);
        }
    }

    private void DrawGlobalLog(SadConsoleLogView log)
    {
        PrintText(log.Bounds.Left, log.Bounds.Top, log.Title, Color.Yellow);
        if (log.Rows.Count == 0)
        {
            PrintClipped(log.Bounds.Left, log.Bounds.Top + 1, log.Bounds.Width, log.EmptyText, Color.DarkGray);
            return;
        }

        var y = log.Bounds.Top + 1;
        foreach (var outcome in log.Rows.TakeLast(log.Bounds.Height - 1))
        {
            var turn = outcome.TurnNumber is { } turnNumber ? $"T{turnNumber}: " : string.Empty;
            PrintClipped(log.Bounds.Left, y++, log.Bounds.Width, $"{turn}{outcome.Sentence}", outcome.Succeeded ? Color.LightGreen : Color.Orange);
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

            case ShellMode.InspectSource:
                foreach (var coord in InspectCandidates(planeId).Where(candidate => candidate is not null).Select(candidate => candidate!.Value.Coord))
                {
                    AddHighlight(highlights, coord, CellHighlight.Valid);
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

    private IReadOnlyList<PlaneCoord?> InspectCandidates(PlaneId planeId)
    {
        if (_session is null)
        {
            return [];
        }

        return _session.World.Occupancy
            .Where(entry => _session.World.Nodes.TryGetValue(entry.Key, out var node) && node.PlaneId == planeId)
            .Select(entry => (PlaneCoord?)_session.World.GetEntityLocation(entry.Value))
            .OrderBy(coord => coord!.Value.Coord.Y)
            .ThenBy(coord => coord!.Value.Coord.X)
            .ToList();
    }

    private bool CycleWorldCursor(Keyboard keyboard, IEnumerable<PlaneCoord?> candidates, PlaneId planeId, string label)
    {
        if (!keyboard.IsKeyReleased(Keys.Tab))
        {
            return false;
        }

        return CycleCursor(candidates, planeId, ref _worldCursor, label);
    }

    private bool CycleInventoryCursor(Keyboard keyboard, IEnumerable<PlaneCoord?> candidates, PlaneId planeId, string label)
    {
        if (!keyboard.IsKeyReleased(Keys.Tab))
        {
            return false;
        }

        return CycleCursor(candidates, planeId, ref _inventoryCursor, label);
    }

    private bool CycleCursor(IEnumerable<PlaneCoord?> candidates, PlaneId planeId, ref GridCoord cursor, string label)
    {
        var result = PromptChoiceCycler.Cycle(candidates, planeId, cursor, label);
        cursor = result.Cursor;
        _message = result.Message;
        return true;
    }

    private void MoveWorldCursorToFirstValid(IEnumerable<PlaneCoord?> candidates)
    {
        if (FirstValidCoord(candidates, PlayerLocation().PlaneId) is { } coord)
        {
            _worldCursor = coord;
        }
    }

    private void MoveInventoryCursorToFirstValid(IEnumerable<PlaneCoord?> candidates)
    {
        if (_session?.World.GetInventoryPlaneId(_session.PlayerEntityId) is { } inventoryPlaneId && FirstValidCoord(candidates, inventoryPlaneId) is { } coord)
        {
            _inventoryCursor = coord;
        }
    }

    private static GridCoord? FirstValidCoord(IEnumerable<PlaneCoord?> candidates, PlaneId planeId) =>
        PromptChoiceCycler.FirstValidCoord(candidates, planeId);

    private void CycleExitDirection(IReadOnlyList<ControlledActorDirectionAffordance> exits)
    {
        var validExits = exits.Where(exit => exit.CanExecute).ToList();
        if (validExits.Count == 0)
        {
            _message = "Exit: no valid exits.";
            return;
        }

        var index = _selectedExitDirection is { } current
            ? validExits.FindIndex(exit => exit.Direction == current)
            : -1;
        var selected = validExits[(index + 1 + validExits.Count) % validExits.Count];
        _selectedExitDirection = selected.Direction;
        MoveWorldCursorToExitDestination(exits, _selectedExitDirection);
        _message = $"Exit: selected {selected.Direction}. Tab cycles, Enter exits.";
    }

    private void MoveWorldCursorToExitDestination(IReadOnlyList<ControlledActorDirectionAffordance> exits, GggDirection? direction)
    {
        if (direction is null)
        {
            return;
        }

        var destination = exits.FirstOrDefault(exit => exit.Direction == direction)?.Destination;
        if (destination?.PlaneId == PlayerLocation().PlaneId)
        {
            _worldCursor = destination.Value.Coord;
        }
    }

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
