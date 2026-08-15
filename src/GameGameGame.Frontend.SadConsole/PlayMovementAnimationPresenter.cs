using GameGameGame.Core;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using CoreDirection = GameGameGame.Core.Direction;

namespace GameGameGame.Frontend.SadConsole;

internal sealed class PlayMovementAnimationPresenter(
    Console owner,
    FrontendDisplayShell shell,
    TilesetProfile tilesetProfile,
    PlayAnimationSettings settings,
    EntityId entityId)
{
    private PlayAnimationQueuePlayback? _playback;
    private PixelGlyphSpriteConsole? _sprite;

    public bool IsAnimating => _playback is not null;

    public PlayGridViewModel? BaseGrid { get; private set; }

    public bool Advance(TimeSpan delta)
    {
        if (_playback is null)
        {
            return false;
        }

        _playback.Advance(delta, settings.Speed);
        return _playback.Completed;
    }

    public void Start(PlayGridViewModel beforeGrid, GridCoord before, GridCoord after, CoreDirection direction)
    {
        BaseGrid = beforeGrid;
        var facing = tilesetProfile.Roles.FacingGlyph(direction);
        var entity = new PlayEntityVisualBundle(
            entityId.Value,
            new PlayWorldCoord(before.X, before.Y),
            new PlayVisualGlyph(beforeGrid.CellAt(before.X, before.Y).EntityGlyph ?? '?', Color.Yellow, Color.Black, PlayRenderLayer.EntitySprite, $"entity:{entityId}:sprite"),
            [new PlayVisualGlyph(facing.Glyph, Color.LightYellow, Color.Black, PlayRenderLayer.EntityAccent, $"entity:{entityId}:facing")],
            []);
        var move = new PlayMoveAnimation(entityId.Value, new PlayWorldCoord(before.X, before.Y), new PlayWorldCoord(after.X, after.Y), settings.MoveDuration);
        _playback = new PlayAnimationQueuePlayback([new PlayAnimationStep("player-move", move, entity)], new PlayCamera(new PlayWorldCoord(0, 0), beforeGrid.Width, beforeGrid.Height));
    }

    public void Draw(FrontendRect gridBounds, CoreDirection currentFacing)
    {
        var commands = _playback?.ActiveCommands();
        var command = commands?.FirstOrDefault(command => command.Layer == PlayRenderLayer.EntitySprite);
        if (commands is null || command is null)
        {
            Clear();
            return;
        }

        var facing = commands.FirstOrDefault(command => command.Layer == PlayRenderLayer.EntityAccent);
        var decorator = facing is null ? null : (global::SadConsole.CellDecorator?)new global::SadConsole.CellDecorator(
            facing.Foreground,
            facing.Glyph,
            tilesetProfile.Roles.FacingGlyph(currentFacing).Mirror);

        var tileWidth = shell.PixelWidth / owner.Width;
        var tileHeight = shell.PixelHeight / owner.Height;
        var pixelX = PixelSnapper.SnapToStep((gridBounds.X + command.ScreenPosition.X) * tileWidth, tileWidth / tilesetProfile.TileWidth);
        var pixelY = PixelSnapper.SnapToStep((gridBounds.Y + command.ScreenPosition.Y) * tileHeight, tileHeight / tilesetProfile.TileHeight);
        if (_sprite is null)
        {
            _sprite = new PixelGlyphSpriteConsole(command.Glyph, command.Foreground, command.Background);
            owner.Children.Add(_sprite);
        }

        _sprite.SetGlyph(command.Glyph, command.Foreground, command.Background, decorator);
        _sprite.Position = new Point(pixelX, pixelY);
        _sprite.Surface.IsDirty = true;
    }

    public void Clear()
    {
        if (_sprite is not null)
        {
            owner.Children.Remove(_sprite);
            _sprite = null;
        }

        _playback = null;
        BaseGrid = null;
    }
}
