using GameGameGame.Content;
using GameGameGame.Core;
using SadConsole.Input;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using CoreDirection = GameGameGame.Core.Direction;

namespace GameGameGame.Frontend.SadConsole;

internal sealed class PlayModeConsole : Console
{
    private readonly PlayableScenarioSession _session;
    private readonly FrontendDisplayShell _shell;
    private readonly TilesetProfile _tilesetProfile;
    private readonly PlayActionSessionController _actionSession;
    private readonly PlayMovementController _movement;
    private readonly QueuedMovementBuffer<CoreDirection> _queuedMovement = new();
    private readonly MovementPreviewState _movementPreview = new();
    private readonly PlaySelectionStack _selectionStack = new();
    private readonly PlayGridSurfacePresenter _gridPresenter = new();
    private readonly PlayPerformanceMetrics _performance = new();
    private readonly PlayInspectionController _inspection;
    private readonly PlayPlayerPanelController _playerPanel;
    private readonly PlayInventorySelectionController _inventorySelection;
    private readonly PlayMovementAnimationPresenter _animationPresenter;
    private readonly Action _returnToBrowser;
    private PlayGridViewModel _grid;
    private string _message = "Numpad/arrows/WASD: aim  Space/Enter: move  Esc: return";

    public PlayModeConsole(
        PlayableScenarioSession session,
        FrontendDisplayShell shell,
        SadConsoleDisplaySettings displaySettings,
        TilesetProfile tilesetProfile,
        Action returnToBrowser)
        : base(shell.LogicalWidth, shell.LogicalHeight)
    {
        _session = session;
        _shell = shell;
        _tilesetProfile = tilesetProfile;
        _returnToBrowser = returnToBrowser;
        _actionSession = new PlayActionSessionController(session, _performance);
        _movement = new PlayMovementController(_actionSession);
        _inspection = new PlayInspectionController(this, session, _actionSession, displaySettings, tilesetProfile);
        _playerPanel = new PlayPlayerPanelController(this, session, _actionSession, displaySettings, tilesetProfile);
        _inventorySelection = new PlayInventorySelectionController(_actionSession);
        _animationPresenter = new PlayMovementAnimationPresenter(this, shell, tilesetProfile, PlayAnimationSettings.Default, session.PlayerEntityId);
        _grid = PlayGridViewModel.FromSession(session, tilesetProfile);
        UseKeyboard = true;
        IsFocused = true;
        FocusedMode = global::SadConsole.FocusBehavior.Set;
        Redraw();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (_selectionStack.TopKind == PlaySelectionFrameKind.CellSelection)
        {
            HandleInventorySelectionKeyboard(keyboard);
            return true;
        }

        if (_playerPanel.IsFocused)
        {
            HandlePlayerPanelKeyboard(PlayInspectionInputController.Read(keyboard), keyboard.IsKeyReleased(Keys.I));
            return true;
        }

        if (_selectionStack.TopKind == PlaySelectionFrameKind.ActionSelection)
        {
            HandleInspectionKeyboard(PlayInspectionInputController.Read(keyboard));
            return true;
        }

        var intent = PlayInputController.Read(keyboard, _movementPreview.HasPreview);
        if (keyboard.IsKeyReleased(Keys.F9))
        {
            _performance.ToggleOverlay();
            _message = _performance.OverlayVisible ? "Performance overlay enabled." : "Performance overlay hidden.";
            Redraw();
            return true;
        }

        switch (intent.Kind)
        {
            case PlayControlIntentKind.Cancel:
                _returnToBrowser();
                return true;
            case PlayControlIntentKind.TogglePlayerPanel:
                _playerPanel.ToggleFocus();
                _message = _playerPanel.IsFocused ? "Player panel focused. I/Esc returns." : "Returned to movement focus.";
                Redraw();
                return true;
            case PlayControlIntentKind.AimMove when intent.Direction is { } aim:
                if (!_movementPreview.Set(aim))
                {
                    return true;
                }

                _message = $"Aiming {aim}. Space/Enter to move.";
                Redraw();
                return true;
            case PlayControlIntentKind.ConfirmMove:
                if (intent.Direction is { } confirmAim)
                {
                    _movementPreview.Set(confirmAim);
                }

                ConfirmMovePreview();
                return true;
            case PlayControlIntentKind.ClearMoveAim:
                if (!_movementPreview.Clear())
                {
                    return true;
                }

                _message = "Movement aim cleared.";
                Redraw();
                return true;
            default:
                return false;
        }
    }

    private void HandleInventorySelectionKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyReleased(Keys.Escape))
        {
            _inventorySelection.Cancel();
            _selectionStack.PopToActionOrAdjacent();
            _message = "Inventory selection cancelled.";
            _playerPanel.MarkWorldChanged();
            Redraw();
            return;
        }

        if (MovementPreviewKeyboardReader.IsConfirmReleased(keyboard))
        {
            var result = _inventorySelection.ConfirmPickup();
            if (result is null)
            {
                _message = "Choose an empty valid inventory cell.";
                Redraw();
                return;
            }

            if (result.Succeeded)
            {
                _grid = PlayGridViewModel.FromSession(_session, _tilesetProfile);
                _inspection.MarkWorldChanged();
                _playerPanel.MarkWorldChanged();
                _movementPreview.Clear();
                _selectionStack.ClearToAdjacentSelection();
                _inspection.ReturnToGrid();
                _playerPanel.ReturnToGrid();
            }

            _message = result.Succeeded ? "Picked up." : $"Pickup blocked: {result.FailureDetail ?? result.FailureReason?.ToString() ?? "unknown"}";
            Redraw();
            return;
        }

        if (PlayInventorySelectionInputController.ReadDirection(keyboard) is { } direction && _inventorySelection.Move(direction))
        {
            _message = _inventorySelection.IsSelectedPickupDestinationValid()
                ? "Choose pickup destination. Enter picks up."
                : "Inventory cell unavailable.";
            _playerPanel.MarkWorldChanged();
            Redraw();
        }
    }

    private void HandlePlayerPanelKeyboard(PlayInspectionInputIntent intent, bool toggleReleased)
    {
        if (toggleReleased || intent.Kind == PlayInspectionInputIntentKind.ReturnToGrid)
        {
            _playerPanel.ReturnToGrid();
            _selectionStack.ClearToAdjacentSelection();
            _message = "Returned to movement focus.";
            Redraw();
            return;
        }

        switch (intent.Kind)
        {
            case PlayInspectionInputIntentKind.PreviousAction:
                _message = _playerPanel.MoveSelection(-1);
                Redraw();
                break;
            case PlayInspectionInputIntentKind.NextAction:
                _message = _playerPanel.MoveSelection(1);
                Redraw();
                break;
            case PlayInspectionInputIntentKind.ConfirmAction:
                _message = _playerPanel.ConfirmSelectedActionMessage();
                Redraw();
                break;
            case PlayInspectionInputIntentKind.Consume:
            default:
                break;
        }
    }

    private void HandleInspectionKeyboard(PlayInspectionInputIntent intent)
    {
        switch (intent.Kind)
        {
            case PlayInspectionInputIntentKind.ReturnToGrid:
                _inspection.ReturnToGrid();
                _selectionStack.ClearToAdjacentSelection();
                _message = "Returned to movement focus.";
                Redraw();
                break;
            case PlayInspectionInputIntentKind.PreviousAction:
                _message = _inspection.MoveSelection(-1);
                Redraw();
                break;
            case PlayInspectionInputIntentKind.NextAction:
                _message = _inspection.MoveSelection(1);
                Redraw();
                break;
            case PlayInspectionInputIntentKind.ConfirmAction:
                _message = TryBeginPickupInventorySelection()
                    ? "Choose pickup destination. Enter picks up, Esc cancels."
                    : _inspection.ConfirmSelectedActionMessage();
                Redraw();
                break;
            case PlayInspectionInputIntentKind.Consume:
            default:
                break;
        }
    }

    private bool TryBeginPickupInventorySelection()
    {
        var row = _inspection.SelectedActionRow;
        if (row is not { Selectable: true, Candidate.Kind: ActionChoiceKind.Pickup, Candidate.Source.EntityId: { } targetId })
        {
            return false;
        }

        if (!_inventorySelection.TryBeginPickup(targetId))
        {
            return false;
        }

        _selectionStack.EnterCellSelection();
        _playerPanel.MarkWorldChanged();
        return true;
    }

    public override void Render(TimeSpan delta)
    {
        using (_performance.Measure(PlayPerformanceCounterKind.RenderFrame))
        {
            if (_animationPresenter.IsAnimating)
            {
                if (_animationPresenter.Advance(delta))
                {
                    EndAnimationAndRedrawFinalState();
                }
                else
                {
                    DrawActiveAnimationFrame();
                }
            }

            base.Render(delta);
        }
    }

    private void ConfirmMovePreview()
    {
        var direction = MovementPreviewConfirmation.ResolveDirection(_movementPreview, _session.World.GetActionFacing(_session.PlayerEntityId));
        if (direction is not { } confirmedDirection)
        {
            _message = "Choose a movement direction first; no facing direction is available.";
            Redraw();
            return;
        }

        if (_movementPreview.TryDestination(_grid.ControlledEntityCoord ?? new GridCoord(0, 0), out var destination)
            && _grid.TryCellAt(destination.X, destination.Y)?.EntityId is { } entityId
            && entityId != _session.PlayerEntityId)
        {
            _selectionStack.EnterActionSelection(destination);
            _inspection.FocusActions();
            _message = "Inspection actions focused. Up/Down choose, Esc returns.";
            Redraw();
            return;
        }

        if (_animationPresenter.IsAnimating)
        {
            _queuedMovement.Queue(confirmedDirection);
            _message = $"Queued {confirmedDirection}.";
            Redraw();
            return;
        }

        TryMove(confirmedDirection);
    }

    private void TryMove(CoreDirection direction)
    {
        var beforeGrid = _grid;
        PlayMovementResult result;
        using (_performance.Measure(PlayPerformanceCounterKind.TurnSubmit))
        {
            result = _movement.MoveAndDeferRefresh(direction);
        }

        if (!result.CommandResult.Succeeded)
        {
            using (_performance.Measure(PlayPerformanceCounterKind.GridRebuild))
            {
                _grid = PlayGridViewModel.FromSession(_session, _tilesetProfile);
            }

            _message = result.CommandResult.FailureDetail is { Length: > 0 } detail
                ? $"Move blocked: {detail}"
                : $"Move blocked: {result.CommandResult.FailureReason?.ToString() ?? "unknown"}";
            Redraw();
            return;
        }

        _inspection.MarkWorldChanged();
        _playerPanel.MarkWorldChanged();
        _message = $"Moved {direction}.";
        _movementPreview.Clear();
        if (result.MovedOneCell)
        {
            _animationPresenter.Start(beforeGrid, result.BeforeCoord, result.AfterCoord, direction);
            Redraw();
            return;
        }

        _movement.CompletePendingPostSubmitRefresh();
        using (_performance.Measure(PlayPerformanceCounterKind.GridRebuild))
        {
            _grid = PlayGridViewModel.FromSession(_session, _tilesetProfile);
        }

        Redraw();
    }

    private void EndAnimationAndRedrawFinalState()
    {
        _animationPresenter.Clear();
        _movement.CompletePendingPostSubmitRefresh();
        using (_performance.Measure(PlayPerformanceCounterKind.GridRebuild))
        {
            _grid = PlayGridViewModel.FromSession(_session, _tilesetProfile);
        }
        Redraw();
        if (_queuedMovement.TryConsume(out var queuedDirection))
        {
            TryMove(queuedDirection);
        }
    }

    private void DrawActiveAnimationFrame()
    {
        using (_performance.Measure(PlayPerformanceCounterKind.AnimationFrame))
        {
            var baseGrid = _animationPresenter.BaseGrid ?? _grid;
            var layout = PlayModeInspectionLayout.Resolve(_shell.DrawableBounds);
            _animationPresenter.Draw(
                PlayGridRenderer.ResolveGridBounds(layout.GridBounds, baseGrid),
                _session.World.GetActionFacing(_session.PlayerEntityId) ?? CoreDirection.North);
        }
    }

    private void Redraw()
    {
        using (_performance.Measure(PlayPerformanceCounterKind.Redraw))
        {
            var layout = PlayModeInspectionLayout.Resolve(_shell.DrawableBounds);
            var visibleGrid = _animationPresenter.BaseGrid ?? _grid;
            var actualGridBounds = PlayGridRenderer.ResolveGridBounds(layout.GridBounds, visibleGrid);
            ClearSurfaceExcept(actualGridBounds);
            DrawBorder();
            Print(2, 1, $"Play: {_session.Name} [{_session.ScenarioId}]", Color.White);
            Print(2, 2, $"Current place: {CurrentPlaceLabel()} | Player: {_actionSession.ControlledActorId} | {_message}", Color.Gray);
            var hidden = _animationPresenter.IsAnimating ? new HashSet<EntityId> { _session.PlayerEntityId } : null;
            var previewCoord = ResolveAdjacentSelectionCoord();
            var movementPreviewCoord = _selectionStack.IsAdjacentSelection
                && _movementPreview.TryDestination(_grid.ControlledEntityCoord ?? new GridCoord(0, 0), out var destination)
                    ? destination
                    : (GridCoord?)null;
            var highlight = ResolveHighlight(previewCoord);
            var inspectedCell = _inspection.ResolveInspectedCell(_grid, previewCoord);
            _gridPresenter.Draw(this, layout.GridBounds, visibleGrid, hidden, movementPreviewCoord ?? previewCoord, highlight?.Presentation(_tilesetProfile));
            _playerPanel.Draw(layout.PlayerPanelBounds, _grid, highlight, _inventorySelection.InventoryHighlight());
            _inspection.Draw(layout.InspectionBounds, _grid, inspectedCell, highlight);
            if (_performance.OverlayVisible)
            {
                DrawPerformanceOverlay();
            }

            if (_animationPresenter.IsAnimating)
            {
                _animationPresenter.Draw(
                    PlayGridRenderer.ResolveGridBounds(layout.GridBounds, visibleGrid),
                    _session.World.GetActionFacing(_session.PlayerEntityId) ?? CoreDirection.North);
            }
            else
            {
                _animationPresenter.Clear();
            }

            Surface.IsDirty = true;
        }
    }

    private void DrawPerformanceOverlay()
    {
        var lines = _performance.OverlayLines();
        var width = Math.Min(46, Math.Max(12, lines.Max(line => line.Length) + 2));
        var x = 1;
        var y = 1;
        for (var row = 0; row < lines.Count && y + row < Height - 1; row++)
        {
            var text = lines[row].Length > width - 2 ? lines[row][..(width - 2)] : lines[row];
            for (var column = 0; column < width; column++)
            {
                SetGlyph(x + column, y + row, _tilesetProfile.Blank, Color.Black, Color.DarkSlateGray);
            }

            Print(x + 1, y + row, text, row == 0 ? Color.Yellow : Color.White);
        }
    }

    private PlayHighlightState? ResolveHighlight(GridCoord? coord)
    {
        if (coord is not { } value)
        {
            return null;
        }

        var kind = _grid.TryCellAt(value.X, value.Y)?.EntityId is { } entityId && entityId != _session.PlayerEntityId
            ? ResolveEntityTargetHighlightKind()
            : CellHighlightKind.MovePreview;
        return new PlayHighlightState(value, kind);
    }

    private CellHighlightKind ResolveEntityTargetHighlightKind() => _inventorySelection.IsActive
        ? _inventorySelection.TargetHighlightKind
        : _selectionStack.TopKind == PlaySelectionFrameKind.ActionSelection
        ? _inspection.FocusedActionHighlightKind()
        : CellHighlightKind.EntityTarget;

    private GridCoord? ResolveAdjacentSelectionCoord() => _selectionStack.IsAdjacentSelection
        ? _movementPreview.TryDestination(_grid.ControlledEntityCoord ?? new GridCoord(0, 0), out var destination)
            ? destination
            : null
        : _selectionStack.LockedAdjacentCoord;

    private void ClearSurfaceExcept(FrontendRect preservedRegion)
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (preservedRegion.Contains(x, y))
                {
                    continue;
                }

                SetGlyph(x, y, _tilesetProfile.Blank, Color.White, Color.Black);
            }
        }
    }

    private string CurrentPlaceLabel()
    {
        var placeId = _grid.ContainerEntityId;
        if (placeId is null)
        {
            return _grid.PlaneId.ToString();
        }

        return _session.World.Entities.TryGetValue(placeId.Value, out var entity) && !string.IsNullOrWhiteSpace(entity.Name)
            ? $"{entity.Name} ({placeId})"
            : placeId.Value.ToString();
    }

    private void DrawBorder()
    {
        for (var x = 0; x < Width; x++)
        {
            SetGlyph(x, 0, 181, Color.Black, Color.Black);
            SetGlyph(x, Height - 1, 181, Color.Black, Color.Black);
        }

        for (var y = 0; y < Height; y++)
        {
            SetGlyph(0, y, 181, Color.Black, Color.Black);
            SetGlyph(Width - 1, y, 181, Color.Black, Color.Black);
        }
    }

    private void Print(int x, int y, string text, Color color)
    {
        for (var index = 0; index < text.Length && x + index < Width; index++)
        {
            SetGlyph(x + index, y, _tilesetProfile.ResolveTextGlyph(text[index]), color, Color.Black);
        }
    }

    private void SetGlyph(int x, int y, int glyph, Color foreground, Color background)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return;
        }

        Surface[x, y].Glyph = glyph;
        Surface[x, y].Foreground = foreground;
        Surface[x, y].Background = background;
        Surface[x, y].Mirror = global::SadConsole.Mirror.None;
        Surface[x, y].Decorators = null;
    }
}
