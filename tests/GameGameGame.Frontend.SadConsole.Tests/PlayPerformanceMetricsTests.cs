using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class PlayPerformanceMetricsTests
{
    [Fact]
    public void PerformanceMetricsRecordLastAverageAndMaxDurations()
    {
        var metrics = new PlayPerformanceMetrics();

        metrics.Record(PlayPerformanceCounterKind.TurnSubmit, TimeSpan.FromMilliseconds(4));
        metrics.Record(PlayPerformanceCounterKind.TurnSubmit, TimeSpan.FromMilliseconds(10));

        var snapshot = Assert.Single(metrics.Snapshot());
        Assert.Equal(PlayPerformanceCounterKind.TurnSubmit, snapshot.Kind);
        Assert.Equal(2, snapshot.SampleCount);
        Assert.Equal(10, snapshot.LastMilliseconds);
        Assert.Equal(7, snapshot.AverageMilliseconds);
        Assert.Equal(10, snapshot.MaxMilliseconds);
    }

    [Fact]
    public void PerformanceMetricsExposeOverlayRowsOnlyAfterToggle()
    {
        var metrics = new PlayPerformanceMetrics();
        metrics.Record(PlayPerformanceCounterKind.Redraw, TimeSpan.FromMilliseconds(3));

        Assert.False(metrics.OverlayVisible);
        metrics.ToggleOverlay();

        Assert.True(metrics.OverlayVisible);
        Assert.Contains(metrics.OverlayLines(), line => line.Contains("redraw", StringComparison.Ordinal));
    }
}
