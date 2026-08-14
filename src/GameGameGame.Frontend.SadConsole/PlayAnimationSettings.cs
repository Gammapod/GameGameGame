namespace GameGameGame.Frontend.SadConsole;

internal sealed record PlayAnimationSettings(TimeSpan MoveDuration, double Speed = 1d)
{
    public static PlayAnimationSettings Default { get; } = new(TimeSpan.FromMilliseconds(120));
}

internal static class PixelSnapper
{
    public static int SnapToStep(double value, int step)
    {
        step = Math.Max(1, step);
        return (int)Math.Round(value / step) * step;
    }
}

internal sealed class QueuedMovementBuffer<TDirection>
{
    private bool _hasQueued;
    private TDirection? _queued;

    public bool HasQueued => _hasQueued;

    public void Queue(TDirection direction)
    {
        _queued = direction;
        _hasQueued = true;
    }

    public bool TryConsume(out TDirection direction)
    {
        if (_hasQueued && _queued is { } queued)
        {
            direction = queued;
            _queued = default;
            _hasQueued = false;
            return true;
        }

        direction = default!;
        return false;
    }
}
