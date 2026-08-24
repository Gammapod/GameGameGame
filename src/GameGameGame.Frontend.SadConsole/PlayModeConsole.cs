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
    private readonly PlayActionWorkflowController _actionWorkflow;
    private readonly PlayMovementAnimationPresenter _animationPresenter;
    private readonly Action _returnToBrowser;
    private PlayGridViewModel _grid;
    private bool _showOutsidePointOfViewDebug;
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
        _actionWorkflow = new PlayActionWorkflowController(_actionSession);
        _animationPresenter = new PlayMovementAnimationPresenter(this, shell, tilesetProfile, PlayAnimationSettings.Default, session.PlayerEntityId);
        _grid = BuildGrid();
        UseKeyboard = true;
        IsFocused = true;
        FocusedMode = global::SadConsole.FocusBehavior.Set;
        Redraw();
    }

    private PlayGridViewModel BuildGrid(PlaneId? preferredPlaneId = null)
    {
        var topologyVisibility = new TopologyVisibilityProjectionService().Project(
            _session.World,
            _actionSession.ControlledActorId,
            TopologyVisibilityProjectionService.DefaultPlayPovDepth,
            TopologyVisibilityProjectionService.DefaultPlayContextDepth);

        return PlayGridViewModel.FromSession(
            _session,
            _tilesetProfile,
            preferredPlaneId,
            topologyVisibility,
            showOutsidePointOfViewContext: _showOutsidePointOfViewDebug);
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyReleased(Keys.F8))
        {
            ToggleOutsidePointOfViewDebug();
            return true;
        }

        if (_selectionStack.TopKind == PlaySelectionFrameKind.CellSelection)
        {
            HandleActionWorkflowKeyboard(keyboard);
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

    private void HandleActionWorkflowKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyReleased(Keys.Escape))
        {
            if (_actionWorkflow.CancelDropDestinationToSource())
            {
                _message = "Drop destination cancelled. Choose droppable inventory item.";
                _playerPanel.MarkWorldChanged();
                Redraw();
                return;
            }

            var wasExitSelection = _actionWorkflow.IsExitDestinationSelection;
            _actionWorkflow.Cancel();
            if (wasExitSelection)
            {
                _grid = BuildGrid();
            }
            _selectionStack.PopToActionOrAdjacent();
            _message = "Inventory selection cancelled.";
            _playerPanel.MarkWorldChanged();
            Redraw();
            return;
        }

        if (MovementPreviewKeyboardReader.IsConfirmReleased(keyboard))
        {
            if (_actionWorkflow.IsTransferItemSelection)
            {
                var transferResult = _actionWorkflow.ConfirmCurrentSubmission();
                if (transferResult is null)
                {
                    _message = "Choose a transferable item.";
                    Redraw();
                    return;
                }

                CompleteActionSubmission(transferResult, "Transferred.", "Transfer blocked");
                Redraw();
                return;
            }

            if (_actionWorkflow.IsDropSourceSelection)
            {
                if (_actionWorkflow.ConfirmDropSource())
                {
                    _message = "Choose adjacent drop destination. Enter drops, Esc cancels.";
                    _playerPanel.MarkWorldChanged();
                    Redraw();
                    return;
                }

                _message = "Choose a droppable inventory item.";
                Redraw();
                return;
            }

            if (_actionWorkflow.IsDropDestinationSelection)
            {
                var dropResult = _actionWorkflow.ConfirmCurrentSubmission();
                if (dropResult is null)
                {
                    _message = "Choose an empty valid adjacent cell.";
                    Redraw();
                    return;
                }

                CompleteActionSubmission(dropResult, "Dropped.", "Drop blocked");
                Redraw();
                return;
            }

            if (_actionWorkflow.IsPushDirectionSelection)
            {
                var pushResult = _actionWorkflow.ConfirmCurrentSubmission();
                if (pushResult is null)
                {
                    _message = "Choose a valid push direction.";
                    Redraw();
                    return;
                }

                CompleteActionSubmission(pushResult, "Pushed.", "Push blocked");
                Redraw();
                return;
            }

            if (_actionWorkflow.IsExitDestinationSelection)
            {
                var exitResult = _actionWorkflow.ConfirmCurrentSubmission();
                if (exitResult is null)
                {
                    _message = "Choose a valid exit direction.";
                    Redraw();
                    return;
                }

                CompleteActionSubmission(exitResult, "Exited.", "Exit blocked");
                Redraw();
                return;
            }

            var result = _actionWorkflow.ConfirmCurrentSubmission();
            if (result is null)
            {
                _message = "Choose an empty valid inventory cell.";
                Redraw();
                return;
            }

            CompleteActionSubmission(result, "Picked up.", "Pickup blocked");
            Redraw();
            return;
        }

        if (_actionWorkflow.IsTransferItemSelection)
        {
            var itemIntent = PlayInspectionInputController.Read(keyboard);
            switch (itemIntent.Kind)
            {
                case PlayInspectionInputIntentKind.PreviousAction:
                    if (_actionWorkflow.SelectNextOption(-1))
                    {
                        _message = _actionWorkflow.TransferItemSummary();
                        _inspection.MarkWorldChanged();
                        _playerPanel.MarkWorldChanged();
                        Redraw();
                    }
                    return;
                case PlayInspectionInputIntentKind.NextAction:
                    if (_actionWorkflow.SelectNextOption(1))
                    {
                        _message = _actionWorkflow.TransferItemSummary();
                        _inspection.MarkWorldChanged();
                        _playerPanel.MarkWorldChanged();
                        Redraw();
                    }
                    return;
                default:
                    return;
            }
        }

        if (PlayActionWorkflowInputController.ReadDirection(keyboard) is { } direction && _actionWorkflow.SelectDirection(direction))
        {
            _message = _actionWorkflow.IsDropSourceSelection
                ? _actionWorkflow.IsSelectedDropSourceValid() ? "Choose droppable item. Enter selects source." : "Inventory item cannot be dropped."
                : _actionWorkflow.IsDropDestinationSelection
                ? _actionWorkflow.IsSelectedDropDestinationValid() ? "Choose drop destination. Enter drops." : "Drop destination unavailable."
                : _actionWorkflow.IsPushDirectionSelection
                ? _actionWorkflow.IsSelectedPushDirectionValid() ? "Choose push direction. Enter pushes." : "Push direction unavailable."
                : _actionWorkflow.IsExitDestinationSelection
                ? _actionWorkflow.IsSelectedExitDestinationValid() ? "Choose exit destination. Enter exits." : "Exit destination unavailable."
                : _actionWorkflow.IsSelectedPickupDestinationValid()
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
                _message = TryBeginDropSourceSelection()
                    ? "Choose droppable inventory item. Enter selects source, Esc cancels."
                    : TryBeginExitDestinationSelection()
                    ? "Choose exit direction. Enter exits, Esc cancels."
                    : _playerPanel.ConfirmSelectedActionMessage();
                Redraw();
                break;
            case PlayInspectionInputIntentKind.Consume:
            default:
                break;
        }
    }

    private void CompleteActionSubmission(ControlledActorCommandResult result, string successVerb, string failureVerb)
    {
        if (result.Succeeded)
        {
            _grid = BuildGrid();
            _inspection.MarkWorldChanged();
            _playerPanel.MarkWorldChanged();
            _movementPreview.Clear();
            _selectionStack.ClearToAdjacentSelection();
            _inspection.ReturnToGrid();
            _playerPanel.ReturnToGrid();
        }

        _message = result.Succeeded ? successVerb : $"{failureVerb}: {result.FailureDetail ?? result.FailureReason?.ToString() ?? "unknown"}";
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
                if (TrySubmitEnterAction())
                {
                    Redraw();
                    break;
                }

                _message = TryBeginPickupInventorySelection()
                    ? "Choose pickup destination. Enter picks up, Esc cancels."
                    : TryBeginPushDirectionSelection()
                    ? "Choose push direction. Enter pushes, Esc cancels."
                    : TryBeginTransferItemSelection()
                    ? _actionWorkflow.TransferItemSummary()
                    : _inspection.ConfirmSelectedActionMessage();
                Redraw();
                break;
            case PlayInspectionInputIntentKind.Consume:
            default:
                break;
        }
    }

    private bool TrySubmitEnterAction()
    {
        var row = _inspection.SelectedActionRow;
        if (row is not { Selectable: true, Candidate.Kind: ActionChoiceKind.Enter, Candidate.Source.EntityId: { } targetId })
        {
            return false;
        }

        CompleteActionSubmission(_actionSession.SubmitEnter(targetId), "Entered.", "Enter blocked");
        return true;
    }

    private bool TryBeginTransferItemSelection()
    {
        var row = _inspection.SelectedActionRow;
        if (row is not { Selectable: true, Candidate.Kind: ActionChoiceKind.Transfer, Candidate.Source.EntityId: { } counterpartyId })
        {
            return false;
        }

        if (!_actionWorkflow.TryBeginTransferItems(counterpartyId))
        {
            return false;
        }

        _selectionStack.EnterCellSelection();
        _playerPanel.MarkWorldChanged();
        return true;
    }

    private bool TryBeginPushDirectionSelection()
    {
        var row = _inspection.SelectedActionRow;
        if (row is not { Selectable: true, Candidate.Kind: ActionChoiceKind.Push, Candidate.Source.EntityId: { } targetId })
        {
            return false;
        }

        if (!_actionWorkflow.TryBeginPushDirection(targetId))
        {
            return false;
        }

        _selectionStack.EnterCellSelection();
        _playerPanel.MarkWorldChanged();
        return true;
    }

    private bool TryBeginPickupInventorySelection()
    {
        var row = _inspection.SelectedActionRow;
        if (row is not { Selectable: true, Candidate.Kind: ActionChoiceKind.Pickup, Candidate.Source.EntityId: { } targetId })
        {
            return false;
        }

        if (!_actionWorkflow.TryBeginPickup(targetId))
        {
            return false;
        }

        _selectionStack.EnterCellSelection();
        _playerPanel.MarkWorldChanged();
        return true;
    }

    private bool TryBeginDropSourceSelection()
    {
        var row = _playerPanel.SelectedActionRow;
        if (row is not { Selectable: true, Candidate.Kind: ActionChoiceKind.Drop })
        {
            return false;
        }

        if (!_actionWorkflow.TryBeginDropSource())
        {
            return false;
        }

        _selectionStack.EnterCellSelection();
        _playerPanel.MarkWorldChanged();
        return true;
    }

    private bool TryBeginExitDestinationSelection()
    {
        var row = _playerPanel.SelectedActionRow;
        if (row is not { Selectable: true, Candidate.Kind: ActionChoiceKind.Exit })
        {
            return false;
        }

        if (!_actionWorkflow.TryBeginExitDestination())
        {
            return false;
        }

        if (_actionWorkflow.ExitDestinationPlaneId() is { } exitPlaneId)
        {
            _grid = BuildGrid(exitPlaneId);
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

        if (ResolveMovementPreviewCell(_session.World, _actionSession.ControlledActorId, _movementPreview.Direction, _grid) is { } destinationCell
            && destinationCell.EntityId is { } entityId
            && entityId != _session.PlayerEntityId)
        {
            var destination = new GridCoord(destinationCell.X, destinationCell.Y);
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
                _grid = BuildGrid();
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
        var beforeDisplayCoord = beforeGrid.TryDisplayCoordForSource(result.BeforeSourceCoord);
        var afterDisplayCoord = BuildGrid().TryDisplayCoordForSource(result.AfterSourceCoord);
        if (beforeDisplayCoord is { } beforeDisplay
            && afterDisplayCoord is { } afterDisplay
            && IsAdjacentDisplayMove(beforeDisplay, afterDisplay)
            && beforeGrid.TryCellAt(beforeDisplay.X, beforeDisplay.Y)?.EntityId == _actionSession.ControlledActorId)
        {
            _animationPresenter.Start(beforeGrid, beforeDisplay, afterDisplay, direction);
            Redraw();
            return;
        }

        _movement.CompletePendingPostSubmitRefresh();
        using (_performance.Measure(PlayPerformanceCounterKind.GridRebuild))
        {
            _grid = BuildGrid();
        }

        Redraw();
    }

    private static bool IsAdjacentDisplayMove(GridCoord before, GridCoord after) =>
        before != after
        && Math.Abs(after.X - before.X) <= 1
        && Math.Abs(after.Y - before.Y) <= 1;

    private void EndAnimationAndRedrawFinalState()
    {
        _animationPresenter.Clear();
        _movement.CompletePendingPostSubmitRefresh();
        using (_performance.Measure(PlayPerformanceCounterKind.GridRebuild))
        {
            _grid = BuildGrid();
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
            ClearSurfaceRegion(actualGridBounds);
            _gridPresenter.Invalidate();
            DrawBorder();
            Print(2, 1, $"Play: {_session.Name} [{_session.ScenarioId}]", Color.White);
            Print(2, 2, $"Current place: {CurrentPlaceLabel()} | Player: {_actionSession.ControlledActorId} | {_message}", Color.Gray);
            var hidden = _animationPresenter.IsAnimating ? new HashSet<EntityId> { _session.PlayerEntityId } : null;
            var previewCoord = ResolveAdjacentSelectionCoord();
            var movementPreviewCoord = _selectionStack.IsAdjacentSelection
                && ResolveMovementPreviewCell(_session.World, _actionSession.ControlledActorId, _movementPreview.Direction, _grid) is { } movementPreviewCell
                    ? new GridCoord(movementPreviewCell.X, movementPreviewCell.Y)
                    : (GridCoord?)null;
            var gridSelectionHighlight = _actionWorkflow.GridHighlight();
            var highlight = gridSelectionHighlight ?? ResolveHighlight(previewCoord);
            var inspectedCell = _inspection.ResolveInspectedCell(_grid, previewCoord);
            _gridPresenter.Draw(this, layout.GridBounds, visibleGrid, hidden, gridSelectionHighlight?.Coord ?? movementPreviewCoord ?? previewCoord, highlight?.Presentation(_tilesetProfile));
            var playerInventoryHighlight = _actionWorkflow.InventoryHighlight()
                ?? _actionWorkflow.TransferInventoryHighlightFor(_actionSession.ControlledActorId);
            var inspectedInventoryHighlight = inspectedCell?.EntityId is { } inspectedEntityId
                ? _actionWorkflow.TransferInventoryHighlightFor(inspectedEntityId)
                : null;
            _playerPanel.Draw(layout.PlayerPanelBounds, _grid, highlight, playerInventoryHighlight);
            _inspection.Draw(layout.InspectionBounds, _grid, inspectedCell, highlight, inspectedInventoryHighlight);
            DrawTransferSelectionPopup(layout);
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

    private void DrawTransferSelectionPopup(PlayModeInspectionLayout layout)
    {
        if (!_actionWorkflow.IsTransferItemSelection)
        {
            return;
        }

        var rows = _actionWorkflow.TransferSelectionRows();
        if (rows.Count == 0)
        {
            return;
        }

        var width = Math.Min(Math.Max(24, rows.Max(row => row.Verb.Length + row.EntityName.Length + 6)), Math.Max(24, layout.GridBounds.Width - 2));
        var height = Math.Min(rows.Count + 3, Math.Max(5, layout.GridBounds.Height));
        var x = layout.GridBounds.X + Math.Max(0, (layout.GridBounds.Width - width) / 2);
        var y = layout.GridBounds.Y + Math.Max(0, layout.GridBounds.Height - height - 1);
        var bounds = new FrontendRect(x, y, width, height);
        PanelRenderer.DrawPanel(this, bounds, _tilesetProfile.Roles.PanelBorder, Color.Gold, Color.Black);
        Print(x + 2, y, "Transfer", Color.White);
        Print(x + 1, y + 1, "Up/Down choose, Enter transfer", Color.Gray);
        for (var index = 0; index < rows.Count && index + 2 < height - 1; index++)
        {
            var row = rows[index];
            var prefix = row.IsSelected ? "> " : "  ";
            var text = prefix + row.Verb + ": " + row.EntityName;
            if (text.Length > width - 2)
            {
                text = text[..(width - 2)];
            }

            Print(x + 1, y + 2 + index, text, row.IsSelected ? Color.LightCyan : Color.Cyan);
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

    private CellHighlightKind ResolveEntityTargetHighlightKind() => _actionWorkflow.IsActive
        ? _actionWorkflow.TargetHighlightKind
        : _selectionStack.TopKind == PlaySelectionFrameKind.ActionSelection
        ? _inspection.FocusedActionHighlightKind()
        : CellHighlightKind.EntityTarget;

    private GridCoord? ResolveAdjacentSelectionCoord() => _selectionStack.IsAdjacentSelection
        ? ResolveMovementPreviewCell(_session.World, _actionSession.ControlledActorId, _movementPreview.Direction, _grid) is { } cell
            ? new GridCoord(cell.X, cell.Y)
            : null
        : _selectionStack.LockedAdjacentCoord;

    internal static PlayCellVisual? ResolveMovementPreviewCell(WorldState world, EntityId actorId, CoreDirection? direction, PlayGridViewModel grid)
    {
        if (direction is not { } resolvedDirection)
        {
            return null;
        }

        return new MovementService().TryGetMovementEdge(world, actorId, resolvedDirection, out var edge)
            ? grid.TryCellForSource(edge.Destination)
            : null;
    }

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

    private void ClearSurfaceRegion(FrontendRect region)
    {
        var minX = Math.Max(0, region.X);
        var minY = Math.Max(0, region.Y);
        var maxX = Math.Min(Width, region.X + region.Width);
        var maxY = Math.Min(Height, region.Y + region.Height);
        for (var y = minY; y < maxY; y++)
        {
            for (var x = minX; x < maxX; x++)
            {
                SetGlyph(x, y, _tilesetProfile.Blank, Color.White, Color.Black);
            }
        }
    }

    private void ToggleOutsidePointOfViewDebug()
    {
        _showOutsidePointOfViewDebug = !_showOutsidePointOfViewDebug;
        using (_performance.Measure(PlayPerformanceCounterKind.GridRebuild))
        {
            _grid = BuildGrid();
        }

        _message = _showOutsidePointOfViewDebug
            ? "Outside-POV debug context visible (dim). F8 hides it."
            : "Outside-POV cells hidden. F8 shows dim debug context.";
        _gridPresenter.Invalidate();
        Redraw();
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
