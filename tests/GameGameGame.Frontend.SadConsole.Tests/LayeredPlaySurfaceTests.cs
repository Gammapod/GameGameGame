using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class LayeredPlaySurfaceTests
{
    [Fact]
    public void PlayCameraTransformsWorldCoordinatesIntoViewportCoordinates()
    {
        var camera = new PlayCamera(new PlayWorldCoord(10, 20), 5, 4);

        Assert.True(camera.TryWorldToScreen(new PlayWorldCoord(12, 23), out var screen));
        Assert.Equal(new PlayScreenCoord(2, 3), screen);
        Assert.False(camera.TryWorldToScreen(new PlayWorldCoord(9, 20), out _));
        Assert.False(camera.TryWorldToScreen(new PlayWorldCoord(15, 20), out _));
    }

    [Fact]
    public void LayeredProjectorOrdersBackdropBeforeEntityAndEntityOwnedIndicatorsBeforeHighlights()
    {
        var tileset = TilesetProfileLoader.LoadCandii();
        var frame = StaticPlayRendererExamples.LayeredRoom(tileset);

        var playerCellCommands = LayeredPlaySurfaceProjector.BuildCommands(frame)
            .Where(command => command.WorldCoord == new PlayWorldCoord(5, 4))
            .ToList();

        Assert.Equal([
            PlayRenderLayer.Backdrop,
            PlayRenderLayer.EntitySprite,
            PlayRenderLayer.EntityAccent,
            PlayRenderLayer.EntityStatus,
            PlayRenderLayer.UxHighlight
        ], playerCellCommands.Select(command => command.Layer).ToArray());
    }

    [Fact]
    public void EntityOwnedAccentsAndStatusesProjectAtEntityPosition()
    {
        var entity = new PlayEntityVisualBundle(
            "actor.test",
            new PlayWorldCoord(7, 8),
            new PlayVisualGlyph('@', SadRogue.Primitives.Color.White, SadRogue.Primitives.Color.Black, PlayRenderLayer.EntitySprite, "sprite"),
            [new PlayVisualGlyph('>', SadRogue.Primitives.Color.Yellow, SadRogue.Primitives.Color.Black, PlayRenderLayer.EntityAccent, "facing")],
            [new PlayVisualGlyph('!', SadRogue.Primitives.Color.Orange, SadRogue.Primitives.Color.Black, PlayRenderLayer.EntityStatus, "status")]);
        var frame = new PlayRenderFrame(
            new PlayCamera(new PlayWorldCoord(5, 5), 10, 10),
            [],
            [entity],
            []);

        var commands = LayeredPlaySurfaceProjector.BuildCommands(frame);

        Assert.All(commands, command => Assert.Equal(new PlayScreenCoord(2, 3), command.ScreenCoord));
        Assert.Contains(commands, command => command.SourceId == "facing" && command.Layer == PlayRenderLayer.EntityAccent);
        Assert.Contains(commands, command => command.SourceId == "status" && command.Layer == PlayRenderLayer.EntityStatus);
    }

    [Fact]
    public void HighlightsAreSeparateUxOverlayCommandsNotEntityIdentity()
    {
        var tileset = TilesetProfileLoader.LoadCandii();
        var frame = StaticPlayRendererExamples.LayeredRoom(tileset);

        var selected = LayeredPlaySurfaceProjector.BuildCommands(frame)
            .Where(command => command.SourceId == "highlight:selected-entity")
            .Single();
        var playerSprite = LayeredPlaySurfaceProjector.BuildCommands(frame)
            .Where(command => command.SourceId == "entity:actor.player:sprite")
            .Single();

        Assert.Equal(playerSprite.ScreenCoord, selected.ScreenCoord);
        Assert.Equal(PlayRenderLayer.UxHighlight, selected.Layer);
        Assert.Equal(PlayRenderLayer.EntitySprite, playerSprite.Layer);
        Assert.NotEqual(playerSprite.Glyph, selected.Glyph);
    }

    [Fact]
    public void MoveAnimationInterpolatesBetweenAdjacentCells()
    {
        var animation = StaticPlayRendererExamples.AdjacentMoveSlide();

        var start = animation.PositionAt(TimeSpan.Zero);
        var middle = animation.PositionAt(StaticPlayRendererExamples.MoveSlideDuration / 2);
        var end = animation.PositionAt(StaticPlayRendererExamples.MoveSlideDuration);

        Assert.Equal(new PlayWorldPosition(5, 4), start);
        Assert.Equal(new PlayWorldPosition(5.5, 4), middle);
        Assert.Equal(new PlayWorldPosition(6, 4), end);
    }

    [Fact]
    public void MoveAnimationProjectsEntityOwnedLayersAtSameInterpolatedPosition()
    {
        var camera = new PlayCamera(new PlayWorldCoord(2, 1), 12, 8);
        var entity = StaticPlayRendererExamples.AnimatedPlayer();
        var animation = StaticPlayRendererExamples.AdjacentMoveSlide();

        var commands = LayeredPlaySurfaceProjector.BuildAnimatedEntityCommands(
            camera,
            entity,
            animation,
            StaticPlayRendererExamples.MoveSlideDuration / 2);

        Assert.Equal([
            PlayRenderLayer.EntitySprite,
            PlayRenderLayer.EntityAccent,
            PlayRenderLayer.EntityStatus
        ], commands.Select(command => command.Layer).ToArray());
        Assert.All(commands, command => Assert.Equal(new PlayWorldPosition(5.5, 4), command.WorldPosition));
        Assert.All(commands, command => Assert.Equal(new PlayScreenPosition(3.5, 3), command.ScreenPosition));
    }

    [Fact]
    public void AnimationQueuePlaysOneStepAtATimeInOrder()
    {
        var frame = StaticPlayRendererExamples.LayeredRoom(TilesetProfileLoader.LoadCandii());
        var playback = new PlayAnimationQueuePlayback(StaticPlayRendererExamples.MoveQueueSteps(), frame.Camera);

        Assert.Equal("initiative-01-player-move", playback.ActiveStep?.Id);

        playback.Advance(StaticPlayRendererExamples.MoveSlideDuration - TimeSpan.FromMilliseconds(1));
        Assert.Equal("initiative-01-player-move", playback.ActiveStep?.Id);

        playback.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal("initiative-02-rat-move", playback.ActiveStep?.Id);
    }

    [Fact]
    public void AnimationQueueAppliesSpeedScalarToPlaybackTime()
    {
        var frame = StaticPlayRendererExamples.LayeredRoom(TilesetProfileLoader.LoadCandii());
        var playback = new PlayAnimationQueuePlayback(StaticPlayRendererExamples.MoveQueueSteps(), frame.Camera);

        playback.Advance(StaticPlayRendererExamples.MoveSlideDuration / 2, speed: 2d);

        Assert.Equal("initiative-02-rat-move", playback.ActiveStep?.Id);
        Assert.Equal(TimeSpan.Zero, playback.ActiveElapsed);
    }

    [Fact]
    public void AnimationQueueSkipsStepsThatDoNotIntersectCamera()
    {
        var camera = new PlayCamera(new PlayWorldCoord(100, 100), 5, 5);
        var playback = new PlayAnimationQueuePlayback(StaticPlayRendererExamples.MoveQueueSteps(), camera);

        Assert.True(playback.Completed);
        Assert.Null(playback.ActiveStep);
    }

    [Fact]
    public void AnimationQueueRequiresFinalRedrawAfterDraining()
    {
        var frame = StaticPlayRendererExamples.LayeredRoom(TilesetProfileLoader.LoadCandii());
        var playback = new PlayAnimationQueuePlayback(StaticPlayRendererExamples.MoveQueueSteps(), frame.Camera);

        playback.Advance(StaticPlayRendererExamples.MoveSlideDuration * 2);

        Assert.True(playback.Completed);
        Assert.True(playback.RequiresFinalRedraw);
        Assert.True(playback.Snapshot().RequiresFinalRedraw);
    }
}
