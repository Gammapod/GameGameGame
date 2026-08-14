using SadRogue.Primitives;

namespace GameGameGame.Frontend.SadConsole;

internal readonly record struct PlayWorldCoord(int X, int Y);

internal readonly record struct PlayScreenCoord(int X, int Y);

internal enum PlayCameraMode
{
    Fixed,
    FollowEntity,
    ManualPan
}

internal sealed record PlayCamera(
    PlayWorldCoord Origin,
    int ViewportWidth,
    int ViewportHeight,
    PlayCameraMode Mode = PlayCameraMode.Fixed,
    string? FollowEntityId = null)
{
    public bool TryWorldToScreen(PlayWorldCoord world, out PlayScreenCoord screen)
    {
        var x = world.X - Origin.X;
        var y = world.Y - Origin.Y;
        if (x < 0 || y < 0 || x >= ViewportWidth || y >= ViewportHeight)
        {
            screen = default;
            return false;
        }

        screen = new PlayScreenCoord(x, y);
        return true;
    }
}

internal enum PlayRenderLayer
{
    Backdrop = 0,
    CellFeature = 10,
    EntitySprite = 20,
    EntityAccent = 30,
    EntityStatus = 40,
    AnimationFx = 50,
    UxHighlight = 60,
    Debug = 70
}

internal sealed record PlayVisualGlyph(
    int Glyph,
    Color Foreground,
    Color Background,
    PlayRenderLayer Layer,
    string SourceId);

internal sealed record PlayBackdropVisual(PlayWorldCoord Coord, PlayVisualGlyph Visual);

internal sealed record PlayEntityVisualBundle(
    string EntityId,
    PlayWorldCoord Coord,
    PlayVisualGlyph Sprite,
    IReadOnlyList<PlayVisualGlyph> Accents,
    IReadOnlyList<PlayVisualGlyph> StatusIcons);

internal sealed record PlayCellOverlayVisual(PlayWorldCoord Coord, PlayVisualGlyph Visual);

internal sealed record PlayRenderFrame(
    PlayCamera Camera,
    IReadOnlyList<PlayBackdropVisual> Backdrops,
    IReadOnlyList<PlayEntityVisualBundle> Entities,
    IReadOnlyList<PlayCellOverlayVisual> Overlays);

internal sealed record PlayRenderCommand(
    PlayScreenCoord ScreenCoord,
    PlayWorldCoord WorldCoord,
    int Glyph,
    Color Foreground,
    Color Background,
    PlayRenderLayer Layer,
    string SourceId);

internal readonly record struct PlayWorldPosition(double X, double Y);

internal readonly record struct PlayScreenPosition(double X, double Y);

internal sealed record PlayMoveAnimation(
    string EntityId,
    PlayWorldCoord From,
    PlayWorldCoord To,
    TimeSpan Duration)
{
    public double ProgressAt(TimeSpan elapsed)
    {
        if (Duration <= TimeSpan.Zero)
        {
            return 1d;
        }

        return Math.Clamp(elapsed.TotalMilliseconds / Duration.TotalMilliseconds, 0d, 1d);
    }

    public PlayWorldPosition PositionAt(TimeSpan elapsed)
    {
        var progress = ProgressAt(elapsed);
        return new PlayWorldPosition(
            From.X + (To.X - From.X) * progress,
            From.Y + (To.Y - From.Y) * progress);
    }
}

internal sealed record PlayAnimatedRenderCommand(
    PlayScreenPosition ScreenPosition,
    PlayWorldPosition WorldPosition,
    int Glyph,
    Color Foreground,
    Color Background,
    PlayRenderLayer Layer,
    string SourceId);

internal sealed record PlayAnimationStep(
    string Id,
    PlayMoveAnimation Move,
    PlayEntityVisualBundle Entity,
    bool RequiresFinalRedraw = true);

internal sealed record PlayAnimationPlaybackSnapshot(
    PlayAnimationStep? ActiveStep,
    TimeSpan ActiveElapsed,
    bool Completed,
    bool RequiresFinalRedraw);

internal sealed class PlayAnimationQueuePlayback
{
    private readonly Queue<PlayAnimationStep> _pending;
    private PlayAnimationStep? _active;
    private TimeSpan _activeElapsed;

    public PlayAnimationQueuePlayback(IEnumerable<PlayAnimationStep> steps, PlayCamera camera)
    {
        Camera = camera;
        _pending = new Queue<PlayAnimationStep>(steps.Where(step => IsVisible(camera, step.Move)));
        StartNext();
    }

    public PlayCamera Camera { get; }
    public bool Completed => _active is null && _pending.Count == 0;
    public bool RequiresFinalRedraw { get; private set; }
    public PlayAnimationStep? ActiveStep => _active;
    public TimeSpan ActiveElapsed => _activeElapsed;

    public PlayAnimationPlaybackSnapshot Snapshot() => new(_active, _activeElapsed, Completed, RequiresFinalRedraw);

    public void Advance(TimeSpan delta, double speed = 1d)
    {
        if (Completed)
        {
            return;
        }

        var scaled = Scale(delta < TimeSpan.Zero ? TimeSpan.Zero : delta, Math.Max(0d, speed));
        while (scaled > TimeSpan.Zero && _active is not null)
        {
            var remaining = _active.Move.Duration - _activeElapsed;
            if (remaining <= TimeSpan.Zero || scaled >= remaining)
            {
                scaled -= remaining <= TimeSpan.Zero ? TimeSpan.Zero : remaining;
                RequiresFinalRedraw |= _active.RequiresFinalRedraw;
                StartNext();
            }
            else
            {
                _activeElapsed += scaled;
                scaled = TimeSpan.Zero;
            }
        }
    }

    public IReadOnlyList<PlayAnimatedRenderCommand> ActiveCommands() => _active is null
        ? []
        : LayeredPlaySurfaceProjector.BuildAnimatedEntityCommands(Camera, _active.Entity, _active.Move, _activeElapsed);

    private void StartNext()
    {
        _active = _pending.Count == 0 ? null : _pending.Dequeue();
        _activeElapsed = TimeSpan.Zero;
    }

    private static bool IsVisible(PlayCamera camera, PlayMoveAnimation move) =>
        camera.TryWorldToScreen(move.From, out _) || camera.TryWorldToScreen(move.To, out _);

    private static TimeSpan Scale(TimeSpan delta, double speed) => speed <= 0d
        ? TimeSpan.Zero
        : TimeSpan.FromTicks((long)Math.Round(delta.Ticks * speed));
}

internal static class LayeredPlaySurfaceProjector
{
    public static IReadOnlyList<PlayRenderCommand> BuildCommands(PlayRenderFrame frame)
    {
        var commands = new List<PlayRenderCommand>();

        foreach (var backdrop in frame.Backdrops)
        {
            AddIfVisible(commands, frame.Camera, backdrop.Coord, backdrop.Visual);
        }

        foreach (var entity in frame.Entities)
        {
            AddIfVisible(commands, frame.Camera, entity.Coord, entity.Sprite);
            foreach (var accent in entity.Accents)
            {
                AddIfVisible(commands, frame.Camera, entity.Coord, accent);
            }

            foreach (var status in entity.StatusIcons)
            {
                AddIfVisible(commands, frame.Camera, entity.Coord, status);
            }
        }

        foreach (var overlay in frame.Overlays)
        {
            AddIfVisible(commands, frame.Camera, overlay.Coord, overlay.Visual);
        }

        return commands
            .OrderBy(command => command.ScreenCoord.Y)
            .ThenBy(command => command.ScreenCoord.X)
            .ThenBy(command => command.Layer)
            .ToList();
    }

    public static IReadOnlyList<PlayAnimatedRenderCommand> BuildAnimatedEntityCommands(
        PlayCamera camera,
        PlayEntityVisualBundle entity,
        PlayMoveAnimation animation,
        TimeSpan elapsed)
    {
        if (!string.Equals(entity.EntityId, animation.EntityId, StringComparison.Ordinal))
        {
            return [];
        }

        var world = animation.PositionAt(elapsed);
        var screen = new PlayScreenPosition(world.X - camera.Origin.X, world.Y - camera.Origin.Y);
        if (screen.X < -1 || screen.Y < -1 || screen.X >= camera.ViewportWidth || screen.Y >= camera.ViewportHeight)
        {
            return [];
        }

        var visuals = new[] { entity.Sprite }
            .Concat(entity.Accents)
            .Concat(entity.StatusIcons);
        return visuals
            .OrderBy(visual => visual.Layer)
            .Select(visual => new PlayAnimatedRenderCommand(
                screen,
                world,
                visual.Glyph,
                visual.Foreground,
                visual.Background,
                visual.Layer,
                visual.SourceId))
            .ToList();
    }

    private static void AddIfVisible(List<PlayRenderCommand> commands, PlayCamera camera, PlayWorldCoord coord, PlayVisualGlyph visual)
    {
        if (!camera.TryWorldToScreen(coord, out var screen))
        {
            return;
        }

        commands.Add(new PlayRenderCommand(screen, coord, visual.Glyph, visual.Foreground, visual.Background, visual.Layer, visual.SourceId));
    }
}

internal static class StaticPlayRendererExamples
{
    public static readonly TimeSpan MoveSlideDuration = TimeSpan.FromMilliseconds(750);

    public static PlayRenderFrame LayeredRoom(TilesetProfile tilesetProfile)
    {
        var camera = new PlayCamera(new PlayWorldCoord(2, 1), 12, 8);
        var backdrops = new List<PlayBackdropVisual>();
        for (var y = 0; y < 10; y++)
        {
            for (var x = 0; x < 16; x++)
            {
                backdrops.Add(new PlayBackdropVisual(
                    new PlayWorldCoord(x, y),
                    new PlayVisualGlyph(tilesetProfile.Roles.DefaultBackdrop, Color.DimGray, Color.Black, PlayRenderLayer.Backdrop, $"backdrop:{x},{y}")));
            }
        }

        var player = new PlayEntityVisualBundle(
            "actor.player",
            new PlayWorldCoord(5, 4),
            new PlayVisualGlyph(219, Color.Yellow, Color.Black, PlayRenderLayer.EntitySprite, "entity:actor.player:sprite"),
            [new PlayVisualGlyph('>', Color.LightYellow, Color.Black, PlayRenderLayer.EntityAccent, "entity:actor.player:facing")],
            [new PlayVisualGlyph('!', Color.Orange, Color.Black, PlayRenderLayer.EntityStatus, "entity:actor.player:alert")]);

        var item = new PlayEntityVisualBundle(
            "item.coin",
            new PlayWorldCoord(9, 5),
            new PlayVisualGlyph(128, Color.Gold, Color.Black, PlayRenderLayer.EntitySprite, "entity:item.coin:sprite"),
            [],
            []);

        var overlays = new[]
        {
            new PlayCellOverlayVisual(new PlayWorldCoord(5, 4), new PlayVisualGlyph(176, Color.Cyan, Color.Black, PlayRenderLayer.UxHighlight, "highlight:selected-entity")),
            new PlayCellOverlayVisual(new PlayWorldCoord(10, 5), new PlayVisualGlyph(177, Color.LightCyan, Color.Black, PlayRenderLayer.UxHighlight, "highlight:hover-cell"))
        };

        return new PlayRenderFrame(camera, backdrops, [player, item], overlays);
    }

    public static PlayMoveAnimation AdjacentMoveSlide() => new(
        "actor.player",
        new PlayWorldCoord(5, 4),
        new PlayWorldCoord(6, 4),
        MoveSlideDuration);

    public static PlayEntityVisualBundle AnimatedPlayer() => new(
        "actor.player",
        new PlayWorldCoord(5, 4),
        new PlayVisualGlyph(219, Color.Yellow, Color.Black, PlayRenderLayer.EntitySprite, "entity:actor.player:sprite"),
        [new PlayVisualGlyph('>', Color.LightYellow, Color.Black, PlayRenderLayer.EntityAccent, "entity:actor.player:facing")],
        [new PlayVisualGlyph('!', Color.Orange, Color.Black, PlayRenderLayer.EntityStatus, "entity:actor.player:alert")]);

    public static PlayEntityVisualBundle AnimatedRat() => new(
        "creature.rat",
        new PlayWorldCoord(9, 5),
        new PlayVisualGlyph(126, Color.LightGray, Color.Black, PlayRenderLayer.EntitySprite, "entity:creature.rat:sprite"),
        [new PlayVisualGlyph('v', Color.White, Color.Black, PlayRenderLayer.EntityAccent, "entity:creature.rat:facing")],
        []);

    public static IReadOnlyList<PlayAnimationStep> MoveQueueSteps() =>
    [
        new("initiative-01-player-move", AdjacentMoveSlide(), AnimatedPlayer()),
        new(
            "initiative-02-rat-move",
            new PlayMoveAnimation("creature.rat", new PlayWorldCoord(9, 5), new PlayWorldCoord(9, 6), MoveSlideDuration),
            AnimatedRat())
    ];
}

internal static class LayeredPlaySurfaceRenderer
{
    public static void Draw(global::SadConsole.Console target, FrontendRect bounds, PlayRenderFrame frame, TilesetProfile tilesetProfile)
    {
        for (var y = 0; y < bounds.Height; y++)
        {
            for (var x = 0; x < bounds.Width; x++)
            {
                SetGlyph(target, bounds.X + x, bounds.Y + y, tilesetProfile.Blank, Color.White, Color.Black);
            }
        }

        foreach (var command in LayeredPlaySurfaceProjector.BuildCommands(frame))
        {
            SetGlyph(
                target,
                bounds.X + command.ScreenCoord.X,
                bounds.Y + command.ScreenCoord.Y,
                command.Glyph,
                command.Foreground,
                command.Background);
        }
    }

    private static void SetGlyph(global::SadConsole.Console target, int x, int y, int glyph, Color foreground, Color background)
    {
        if (x < 0 || y < 0 || x >= target.Width || y >= target.Height)
        {
            return;
        }

        target.Surface[x, y].Glyph = glyph;
        target.Surface[x, y].Foreground = foreground;
        target.Surface[x, y].Background = background;
    }
}
