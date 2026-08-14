using GameGameGame.Core;
using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class PlayAnimationSettingsTests
{
    [Fact]
    public void PlayMovementUsesFasterDurationThanGalleryDemonstration()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(120), PlayAnimationSettings.Default.MoveDuration);
        Assert.True(PlayAnimationSettings.Default.MoveDuration < StaticPlayRendererExamples.MoveSlideDuration);
    }

    [Fact]
    public void PixelSnapperSnapsToSpritePixelStep()
    {
        Assert.Equal(0, PixelSnapper.SnapToStep(0.9, 2));
        Assert.Equal(2, PixelSnapper.SnapToStep(1.1, 2));
        Assert.Equal(8, PixelSnapper.SnapToStep(7.2, 2));
        Assert.Equal(9, PixelSnapper.SnapToStep(7.6, 3));
    }

    [Fact]
    public void QueuedMovementBufferKeepsOnlyLatestQueuedDirection()
    {
        var buffer = new QueuedMovementBuffer<Direction>();

        buffer.Queue(Direction.East);
        buffer.Queue(Direction.South);

        Assert.True(buffer.TryConsume(out var direction));
        Assert.Equal(Direction.South, direction);
        Assert.False(buffer.HasQueued);
        Assert.False(buffer.TryConsume(out _));
    }
}
