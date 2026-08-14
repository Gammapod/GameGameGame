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
    private readonly PlayAnimationSettings _animationSettings;
    private readonly QueuedMovementBuffer<CoreDirection> _queuedMovement = new();
    private readonly MovementPreviewState _movementPreview = new();
    private readonly Action _returnToBrowser;
    private PlayGridViewModel _grid;
    private PlayGridViewModel? _animationBaseGrid;
    private PlayAnimationQueuePlayback? _animationPlayback;
    private PixelGlyphSpriteConsole? _animationSprite;
    private string _message = "Numpad/arrows/WASD: aim  Space/Enter: move  Esc: return";

    public PlayModeConsole(
        PlayableScenarioSession session,
        FrontendDisplayShell shell,
        TilesetProfile tilesetProfile,
        Action returnToBrowser)
        : base(shell.LogicalWidth, shell.LogicalHeight)
    {
        _session = session;
        _shell = shell;
        _tilesetProfile = tilesetProfile;
        _returnToBrowser = returnToBrowser;
        _movement = new PlayMovementController(session);
        _animationSettings = PlayAnimationSettings.Default;
        _grid = PlayGridViewModel.FromSession(session, tilesetProfile);
        UseKeyboard = true;
        IsFocused = true;
        FocusedMode = global::SadConsole.FocusBehavior.Set;
        Redraw();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyReleased(Keys.Escape))
        {
            _returnToBrowser();
            return true;
        }

        var heldDirection = MovementPreviewKeyboardReader.ReadHeldDirection(keyboard.KeysDown.Select(key => key.Key));
        if (heldDirection is { } direction)
        {
            _movementPreview.Set(direction);
            _message = $"Aiming {direction}. Space/Enter to move.";
        }

        if (MovementPreviewKeyboardReader.IsConfirmReleased(keyboard))
        {
            ConfirmMovePreview();
            return true;
        }

        if (heldDirection is not null)
        {
            Redraw();
            return true;
        }

        if (_movementPreview.HasPreview && keyboard.KeysReleased.Any(key => MovementPreviewKeyboardReader.IsMovementKey(key.Key)))
        {
            _movementPreview.Clear();
            _message = "Movement aim cleared.";
            Redraw();
            return true;
        }

        return false;
    }

    public override void Render(TimeSpan delta)
    {
        if (_animationPlayback is not null)
        {
            _animationPlayback.Advance(delta, _animationSettings.Speed);
            if (_animationPlayback.Completed)
            {
                EndAnimationAndRedrawFinalState();
            }
            else
            {
                Redraw();
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

        if (_animationPlayback is not null)
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
            StartMoveAnimation(beforeGrid, result.BeforeCoord, result.AfterCoord, direction);
            return;
        }

        Redraw();
    }

    private void StartMoveAnimation(PlayGridViewModel beforeGrid, GridCoord before, GridCoord after, CoreDirection direction)
    {
        _animationBaseGrid = beforeGrid;
        var facing = _tilesetProfile.Roles.FacingGlyph(direction);
        var entity = new PlayEntityVisualBundle(
            _session.PlayerEntityId.Value,
            new PlayWorldCoord(before.X, before.Y),
            new PlayVisualGlyph(beforeGrid.CellAt(before.X, before.Y).EntityGlyph ?? '?', Color.Yellow, Color.Black, PlayRenderLayer.EntitySprite, $"entity:{_session.PlayerEntityId}:sprite"),
            [new PlayVisualGlyph(facing.Glyph, Color.LightYellow, Color.Black, PlayRenderLayer.EntityAccent, $"entity:{_session.PlayerEntityId}:facing")],
            []);
        var move = new PlayMoveAnimation(_session.PlayerEntityId.Value, new PlayWorldCoord(before.X, before.Y), new PlayWorldCoord(after.X, after.Y), _animationSettings.MoveDuration);
        _animationPlayback = new PlayAnimationQueuePlayback([
            new PlayAnimationStep("player-move", move, entity)
        ], new PlayCamera(new PlayWorldCoord(0, 0), beforeGrid.Width, beforeGrid.Height));
        Redraw();
    }

    private void EndAnimationAndRedrawFinalState()
    {
        HideAnimationSprite();
        _animationPlayback = null;
        _animationBaseGrid = null;
        _grid = PlayGridViewModel.FromSession(_session, _tilesetProfile);
        Redraw();
        if (_queuedMovement.TryConsume(out var queuedDirection))
        {
            TryMove(queuedDirection);
        }
    }

    private void Redraw()
    {
        ClearSurface();
        DrawBorder();
        Print(2, 1, $"Play: {_session.Name} [{_session.ScenarioId}]", Color.White);
        Print(2, 2, $"Current place: {_session.ActiveContainerEntityId} | Player: {_session.PlayerEntityId} | {_message}", Color.Gray);
        var visibleGrid = _animationBaseGrid ?? _grid;
        var hidden = _animationPlayback is null ? null : new HashSet<EntityId> { _session.PlayerEntityId };
        var previewCoord = _movementPreview.TryDestination(_grid.ControlledEntityCoord ?? new GridCoord(0, 0), out var destination)
            ? destination
            : (GridCoord?)null;
        PlayGridRenderer.Draw(this, _shell.DrawableBounds, visibleGrid, hidden, previewCoord, CellHighlightPresentation.MovePreview(_tilesetProfile));
        if (_animationPlayback is not null)
        {
            ShowAnimationSprite(PlayGridRenderer.ResolveGridBounds(_shell.DrawableBounds, visibleGrid));
        }
        else
        {
            HideAnimationSprite();
        }
        Surface.IsDirty = true;
    }

    private void ShowAnimationSprite(FrontendRect gridBounds)
    {
        var commands = _animationPlayback?.ActiveCommands();
        var command = commands?.FirstOrDefault(command => command.Layer == PlayRenderLayer.EntitySprite);
        if (commands is null || command is null)
        {
            HideAnimationSprite();
            return;
        }

        var facing = commands.FirstOrDefault(command => command.Layer == PlayRenderLayer.EntityAccent);
        global::SadConsole.CellDecorator? decorator = facing is null
            ? null
            : new global::SadConsole.CellDecorator(facing.Foreground, facing.Glyph, _tilesetProfile.Roles.FacingGlyph(_session.World.GetActionFacing(_session.PlayerEntityId) ?? CoreDirection.North).Mirror);

        var tileWidth = _shell.PixelWidth / Width;
        var tileHeight = _shell.PixelHeight / Height;
        var pixelX = PixelSnapper.SnapToStep((gridBounds.X + command.ScreenPosition.X) * tileWidth, tileWidth / _tilesetProfile.TileWidth);
        var pixelY = PixelSnapper.SnapToStep((gridBounds.Y + command.ScreenPosition.Y) * tileHeight, tileHeight / _tilesetProfile.TileHeight);
        if (_animationSprite is null)
        {
            _animationSprite = new PixelGlyphSpriteConsole(command.Glyph, command.Foreground, command.Background);
            Children.Add(_animationSprite);
        }

        _animationSprite.SetGlyph(command.Glyph, command.Foreground, command.Background, decorator);
        _animationSprite.Position = new Point(pixelX, pixelY);
        _animationSprite.Surface.IsDirty = true;
    }

    private void HideAnimationSprite()
    {
        if (_animationSprite is null) return;
        Children.Remove(_animationSprite);
        _animationSprite = null;
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
