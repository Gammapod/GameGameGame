using System.Diagnostics;

namespace GameGameGame.Frontend.SadConsole;

internal enum PlayPerformanceCounterKind
{
    TurnSubmit,
    SubmitCore,
    SubmitRefresh,
    SubmitAdvance,
    SubmitAdvanceStepper,
    SubmitAdvanceFacts,
    SubmitRuntimeFacts,
    SubmitActionLog,
    SubmitDisplayTargets,
    SubmitActionChoices,
    GridRebuild,
    Redraw,
    AnimationFrame,
    RenderFrame
}

internal readonly record struct PlayPerformanceCounterSnapshot(
    PlayPerformanceCounterKind Kind,
    int SampleCount,
    double LastMilliseconds,
    double AverageMilliseconds,
    double MaxMilliseconds);

internal sealed class PlayPerformanceMetrics
{
    private readonly Dictionary<PlayPerformanceCounterKind, Counter> _counters = [];

    public bool OverlayVisible { get; private set; }

    public void ToggleOverlay() => OverlayVisible = !OverlayVisible;

    public IDisposable Measure(PlayPerformanceCounterKind kind) => new Scope(this, kind, Stopwatch.GetTimestamp());

    public void Record(PlayPerformanceCounterKind kind, TimeSpan elapsed)
    {
        if (!_counters.TryGetValue(kind, out var counter))
        {
            counter = new Counter();
            _counters.Add(kind, counter);
        }

        counter.Record(elapsed.TotalMilliseconds);
    }

    public IReadOnlyList<PlayPerformanceCounterSnapshot> Snapshot() => _counters
        .OrderBy(entry => entry.Key)
        .Select(entry => entry.Value.Snapshot(entry.Key))
        .ToList();

    public IReadOnlyList<string> OverlayLines()
    {
        var rows = new List<string> { "Perf F9" };
        rows.AddRange(Snapshot().Select(snapshot =>
            $"{Label(snapshot.Kind),-9} {snapshot.LastMilliseconds,5:0.0}ms avg {snapshot.AverageMilliseconds,5:0.0} max {snapshot.MaxMilliseconds,5:0.0}"));
        return rows;
    }

    private static string Label(PlayPerformanceCounterKind kind) => kind switch
    {
        PlayPerformanceCounterKind.TurnSubmit => "submit",
        PlayPerformanceCounterKind.SubmitCore => " core",
        PlayPerformanceCounterKind.SubmitRefresh => " refresh",
        PlayPerformanceCounterKind.SubmitAdvance => " advance",
        PlayPerformanceCounterKind.SubmitAdvanceStepper => " stepper",
        PlayPerformanceCounterKind.SubmitAdvanceFacts => " advfacts",
        PlayPerformanceCounterKind.SubmitRuntimeFacts => " facts",
        PlayPerformanceCounterKind.SubmitActionLog => " log",
        PlayPerformanceCounterKind.SubmitDisplayTargets => " targets",
        PlayPerformanceCounterKind.SubmitActionChoices => " choices",
        PlayPerformanceCounterKind.GridRebuild => "grid",
        PlayPerformanceCounterKind.Redraw => "redraw",
        PlayPerformanceCounterKind.AnimationFrame => "anim",
        PlayPerformanceCounterKind.RenderFrame => "render",
        _ => kind.ToString()
    };

    private sealed class Counter
    {
        private int _sampleCount;
        private double _lastMilliseconds;
        private double _totalMilliseconds;
        private double _maxMilliseconds;

        public void Record(double milliseconds)
        {
            _sampleCount++;
            _lastMilliseconds = milliseconds;
            _totalMilliseconds += milliseconds;
            _maxMilliseconds = Math.Max(_maxMilliseconds, milliseconds);
        }

        public PlayPerformanceCounterSnapshot Snapshot(PlayPerformanceCounterKind kind) => new(
            kind,
            _sampleCount,
            _lastMilliseconds,
            _sampleCount == 0 ? 0 : _totalMilliseconds / _sampleCount,
            _maxMilliseconds);
    }

    private sealed class Scope(PlayPerformanceMetrics owner, PlayPerformanceCounterKind kind, long startTimestamp) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            owner.Record(kind, Stopwatch.GetElapsedTime(startTimestamp));
        }
    }
}
