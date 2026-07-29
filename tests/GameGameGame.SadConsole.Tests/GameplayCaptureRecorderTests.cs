using GameGameGame.SadConsoleApp.Ui.Rendering;

namespace GameGameGame.SadConsole.Tests;

public sealed class GameplayCaptureRecorderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ggg-capture-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void StartCapturesCurrentTurnIntoScenarioTimestampFolder()
    {
        var sink = new RecordingCaptureSink();
        var recorder = new GameplayCaptureRecorder(_root, () => new DateTimeOffset(2026, 7, 29, 12, 34, 56, TimeSpan.Zero));

        var result = recorder.Start("Bad/Scenario:Name", worldTurn: 7, sink);

        Assert.True(result.Succeeded);
        Assert.True(recorder.IsRecording);
        Assert.Single(recorder.Frames);
        Assert.EndsWith(Path.Combine("Bad-Scenario-Name", "20260729-123456", "turn-000007-frame-0001.jpg"), recorder.Frames[0]);
        Assert.Equal(recorder.Frames, sink.Jpegs);
        Assert.Contains("recording 1 frame", recorder.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void StopWithMultipleFramesWritesGif()
    {
        var sink = new RecordingCaptureSink();
        var recorder = new GameplayCaptureRecorder(_root, () => new DateTimeOffset(2026, 7, 29, 12, 34, 56, TimeSpan.Zero), gifFrameDelayCentiseconds: 12);

        recorder.Start("Demo", worldTurn: 1, sink);
        recorder.CaptureFrame(worldTurn: 2, sink);
        var result = recorder.Stop(sink);

        Assert.True(result.Succeeded);
        Assert.False(recorder.IsRecording);
        Assert.Equal(2, result.FrameCount);
        Assert.NotNull(result.GifPath);
        Assert.EndsWith(Path.Combine("Demo", "20260729-123456", "gameplay.gif"), result.GifPath);
        Assert.Single(sink.Gifs);
        Assert.Equal(recorder.Frames, sink.Gifs[0].Frames);
        Assert.Equal(12, sink.Gifs[0].DelayCentiseconds);
        Assert.Contains("gameplay.gif", recorder.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void StopWithOneFrameKeepsStillWithoutGif()
    {
        var sink = new RecordingCaptureSink();
        var recorder = new GameplayCaptureRecorder(_root, () => new DateTimeOffset(2026, 7, 29, 12, 34, 56, TimeSpan.Zero));

        recorder.Start("Demo", worldTurn: 1, sink);
        var result = recorder.Stop(sink);

        Assert.True(result.Succeeded);
        Assert.Null(result.GifPath);
        Assert.Empty(sink.Gifs);
        Assert.Contains("kept 1 still frame", recorder.StatusText, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class RecordingCaptureSink : IGameplayCaptureSink
    {
        public List<string> Jpegs { get; } = [];
        public List<(IReadOnlyList<string> Frames, string GifPath, int DelayCentiseconds)> Gifs { get; } = [];

        public void SaveJpeg(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "fake jpg");
            Jpegs.Add(path);
        }

        public void SaveGif(IReadOnlyList<string> framePaths, string gifPath, int frameDelayCentiseconds)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(gifPath)!);
            File.WriteAllText(gifPath, "fake gif");
            Gifs.Add((framePaths.ToList(), gifPath, frameDelayCentiseconds));
        }
    }
}
