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
    private readonly SadConsoleDisplaySettings _displaySettings;
    private readonly TilesetProfile _tilesetProfile;
    private readonly PlayMovementController _movement;
    private readonly QueuedMovementBuffer<CoreDirection> _queuedMovement = new();
    private readonly MovementPreviewState _movementPreview = new();
    private readonly PlayInspectionState _inspection = new();
    private readonly PlayMovementAnimationPresenter _animationPresenter;
    private readonly Action _returnToBrowser;
    private PlayGridViewModel _grid;
    private EntityInspectionOverlayConsole? _inspectionOverlay;
    private EntityId? _cachedInspectionEntityId;
    private EntityInspectionPanelModel? _cachedInspectionModel;
    private GridCoord? _cachedInspectionHighlightCoord;
    private EntityId? _drawnInspectionEntityId;
    private FrontendRect? _cachedInspectionBounds;
    private bool _cachedInspectionEmpty;
    private bool _inspectionDirty = true;
    private bool _inspectionModelChanged = true;
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
        _displaySettings = displaySettings;
        _tilesetProfile = tilesetProfile;
        _returnToBrowser = returnToBrowser;
        _movement = new PlayMovementController(session);
        _animationPresenter = new PlayMovementAnimationPresenter(this, shell, tilesetProfile, PlayAnimationSettings.Default, session.PlayerEntityId);
        _grid = PlayGridViewModel.FromSession(session, tilesetProfile);
        UseKeyboard = true;
        IsFocused = true;
        FocusedMode = global::SadConsole.FocusBehavior.Set;
        Redraw();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
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
        _inspectionDirty = true;

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
        _inspectionDirty = true;
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
        var inspectedCell = _inspection.ResolveInspectedCell(_grid, previewCoord);
        var inspectionModel = ResolveInspectionModel(inspectedCell, previewCoord);
        PlayGridRenderer.Draw(this, layout.GridBounds, visibleGrid, hidden, previewCoord, CellHighlightPresentation.MovePreview(_tilesetProfile));
        DrawInspectionPanel(layout.InspectionBounds, inspectedCell?.EntityId, inspectionModel);
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

    private EntityInspectionPanelModel? ResolveInspectionModel(PlayCellVisual? inspectedCell, GridCoord? highlightedCoord)
    {
        var entityId = inspectedCell?.EntityId;
        if (entityId is null)
        {
            _inspectionModelChanged = _cachedInspectionModel is not null || _cachedInspectionEntityId is not null;
            _cachedInspectionEntityId = null;
            _cachedInspectionHighlightCoord = null;
            _cachedInspectionModel = null;
            _inspectionDirty = false;
            return null;
        }

        if (!_inspectionDirty && _cachedInspectionEntityId == entityId && _cachedInspectionHighlightCoord == highlightedCoord && _cachedInspectionModel is not null)
        {
            _inspectionModelChanged = false;
            return _cachedInspectionModel;
        }

        _cachedInspectionEntityId = entityId;
        _cachedInspectionHighlightCoord = highlightedCoord;
        _cachedInspectionModel = EntityInspectionPanelModelFactory.FromEntity(_session, _grid, inspectedCell!, highlightedCoord);
        _inspectionDirty = false;
        _inspectionModelChanged = true;
        return _cachedInspectionModel;
    }

    private void DrawInspectionPanel(FrontendRect? bounds, EntityId? entityId, EntityInspectionPanelModel? model)
    {
        if (bounds is null)
        {
            ClearInspectionOverlay();
            return;
        }

        var geometry = OverlayPanelGeometry.HalfTileOffset(bounds, _displaySettings);
        var needsRedraw = _inspectionOverlay is null
            || _inspectionOverlay.Width != bounds.Width
            || _inspectionOverlay.Height != bounds.Height
            || _cachedInspectionBounds != bounds
            || _inspectionModelChanged
            || (model is null && !_cachedInspectionEmpty)
            || (model is not null && (_cachedInspectionEmpty || _drawnInspectionEntityId != entityId));

        if (_inspectionOverlay is null || _inspectionOverlay.Width != bounds.Width || _inspectionOverlay.Height != bounds.Height)
        {
            ClearInspectionOverlay();
            _inspectionOverlay = new EntityInspectionOverlayConsole(geometry, _displaySettings, _tilesetProfile);
            Children.Add(_inspectionOverlay);
            needsRedraw = true;
        }

        _inspectionOverlay.MoveTo(geometry);
        if (needsRedraw)
        {
            _inspectionOverlay.Draw(model);
            _cachedInspectionBounds = bounds;
            _cachedInspectionEmpty = model is null;
            _drawnInspectionEntityId = model is null ? null : entityId;
            _inspectionModelChanged = false;
        }
    }

    private void ClearInspectionOverlay()
    {
        if (_inspectionOverlay is null) return;
        Children.Remove(_inspectionOverlay);
        _inspectionOverlay = null;
        _cachedInspectionBounds = null;
        _cachedInspectionEmpty = false;
        _drawnInspectionEntityId = null;
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
