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
    private readonly PlayMovementController _movement;
    private readonly QueuedMovementBuffer<CoreDirection> _queuedMovement = new();
    private readonly MovementPreviewState _movementPreview = new();
    private readonly PlayInspectionController _inspection;
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
        _movement = new PlayMovementController(session);
        _inspection = new PlayInspectionController(this, session, displaySettings, tilesetProfile);
        _animationPresenter = new PlayMovementAnimationPresenter(this, shell, tilesetProfile, PlayAnimationSettings.Default, session.PlayerEntityId);
        _grid = PlayGridViewModel.FromSession(session, tilesetProfile);
        UseKeyboard = true;
        IsFocused = true;
        FocusedMode = global::SadConsole.FocusBehavior.Set;
        Redraw();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (_inspection.FocusMode == PlayFocusMode.InspectionActions)
        {
            if (keyboard.IsKeyReleased(Keys.Escape) || keyboard.IsKeyReleased(Keys.Left))
            {
                _inspection.ReturnToGrid();
                _message = "Returned to movement focus.";
                Redraw();
                return true;
            }

            if (keyboard.IsKeyReleased(Keys.Up))
            {
                _message = _inspection.MoveSelection(-1);
                Redraw();
                return true;
            }

            if (keyboard.IsKeyReleased(Keys.Down))
            {
                _message = _inspection.MoveSelection(1);
                Redraw();
                return true;
            }

            if (keyboard.IsKeyReleased(Keys.Enter) || keyboard.IsKeyReleased(Keys.Space))
            {
                _message = _inspection.ConfirmSelectedActionMessage();
                Redraw();
                return true;
            }
        }

        var intent = PlayInputController.Read(keyboard, _movementPreview.HasPreview);
        switch (intent.Kind)
        {
            case PlayControlIntentKind.Cancel:
                _returnToBrowser();
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

    public override void Render(TimeSpan delta)
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
        var result = _movement.Move(direction);
        _grid = PlayGridViewModel.FromSession(_session, _tilesetProfile);
        _inspection.MarkWorldChanged();

        if (!result.CommandResult.Succeeded)
        {
            _message = result.CommandResult.FailureDetail is { Length: > 0 } detail
                ? $"Move blocked: {detail}"
                : $"Move blocked: {result.CommandResult.FailureReason?.ToString() ?? "unknown"}";
            Redraw();
            return;
        }

        _message = $"Moved {direction}.";
        _movementPreview.Clear();
        if (result.MovedOneCell)
        {
            _animationPresenter.Start(beforeGrid, result.BeforeCoord, result.AfterCoord, direction);
            Redraw();
            return;
        }

        Redraw();
    }

    private void EndAnimationAndRedrawFinalState()
    {
        _animationPresenter.Clear();
        _grid = PlayGridViewModel.FromSession(_session, _tilesetProfile);
        _inspection.MarkWorldChanged();
        Redraw();
        if (_queuedMovement.TryConsume(out var queuedDirection))
        {
            TryMove(queuedDirection);
        }
    }

    private void DrawActiveAnimationFrame()
    {
        var baseGrid = _animationPresenter.BaseGrid ?? _grid;
        var layout = PlayModeInspectionLayout.Resolve(_shell.DrawableBounds);
        _animationPresenter.Draw(
            PlayGridRenderer.ResolveGridBounds(layout.GridBounds, baseGrid),
            _session.World.GetActionFacing(_session.PlayerEntityId) ?? CoreDirection.North);
    }

    private void Redraw()
    {
        ClearSurface();
        DrawBorder();
        Print(2, 1, $"Play: {_session.Name} [{_session.ScenarioId}]", Color.White);
        Print(2, 2, $"Current place: {CurrentPlaceLabel()} | Player: {_session.PlayerEntityId} | {_message}", Color.Gray);
        var visibleGrid = _animationPresenter.BaseGrid ?? _grid;
        var hidden = _animationPresenter.IsAnimating ? new HashSet<EntityId> { _session.PlayerEntityId } : null;
        var previewCoord = _movementPreview.TryDestination(_grid.ControlledEntityCoord ?? new GridCoord(0, 0), out var destination)
            ? destination
            : (GridCoord?)null;
        var layout = PlayModeInspectionLayout.Resolve(_shell.DrawableBounds);
        var highlight = ResolveHighlight(previewCoord);
        var inspectedCell = _inspection.ResolveInspectedCell(_grid, previewCoord);
        PlayGridRenderer.Draw(this, layout.GridBounds, visibleGrid, hidden, previewCoord, highlight?.Presentation(_tilesetProfile));
        _inspection.Draw(layout.InspectionBounds, _grid, inspectedCell, highlight);
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

    private PlayHighlightState? ResolveHighlight(GridCoord? coord)
    {
        if (coord is not { } value)
        {
            return null;
        }

        var kind = _grid.TryCellAt(value.X, value.Y)?.EntityId is { } entityId && entityId != _session.PlayerEntityId
            ? CellHighlightKind.EntityTarget
            : CellHighlightKind.MovePreview;
        return new PlayHighlightState(value, kind);
    }

    private void ClearSurface()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
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
    }
}
