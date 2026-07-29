using Microsoft.Xna.Framework.Graphics;
using SadConsole.Host;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace GameGameGame.SadConsoleApp.Ui.Rendering;

internal sealed class GameplayCaptureRecorder
{
    private readonly string _rootDirectory;
    private readonly Func<DateTimeOffset> _clock;
    private readonly int _gifFrameDelayCentiseconds;
    private readonly List<string> _frames = [];
    private string? _sessionDirectory;

    public GameplayCaptureRecorder(
        string? rootDirectory = null,
        Func<DateTimeOffset>? clock = null,
        int gifFrameDelayCentiseconds = 18)
    {
        _rootDirectory = rootDirectory ?? Path.GetFullPath("captures");
        _clock = clock ?? (() => DateTimeOffset.Now);
        _gifFrameDelayCentiseconds = Math.Max(1, gifFrameDelayCentiseconds);
    }

    public bool IsRecording { get; private set; }
    public IReadOnlyList<string> Frames => _frames;
    public string? SessionDirectory => _sessionDirectory;
    public string StatusText { get; private set; } = "Capture: off";

    public GameplayCaptureResult Start(string scenarioName, int worldTurn, IGameplayCaptureSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        if (IsRecording)
        {
            return new GameplayCaptureResult(false, StatusText, null, null, _frames.Count);
        }

        _frames.Clear();
        var startedAt = _clock();
        _sessionDirectory = Path.Combine(
            _rootDirectory,
            SanitizePathSegment(scenarioName),
            startedAt.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(_sessionDirectory);
        IsRecording = true;
        CaptureFrame(worldTurn, sink);
        StatusText = $"Capture: recording {_frames.Count} frame(s) -> {_sessionDirectory}";
        return new GameplayCaptureResult(true, StatusText, _sessionDirectory, null, _frames.Count);
    }

    public GameplayCaptureResult CaptureFrame(int worldTurn, IGameplayCaptureSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        if (!IsRecording || _sessionDirectory is null)
        {
            return new GameplayCaptureResult(false, StatusText, _sessionDirectory, null, _frames.Count);
        }

        var fileName = $"turn-{Math.Max(0, worldTurn):000000}-frame-{_frames.Count + 1:0000}.jpg";
        var path = Path.Combine(_sessionDirectory, fileName);
        sink.SaveJpeg(path);
        _frames.Add(path);
        StatusText = $"Capture: recording {_frames.Count} frame(s) -> {_sessionDirectory}";
        return new GameplayCaptureResult(true, StatusText, _sessionDirectory, null, _frames.Count);
    }

    public GameplayCaptureResult Stop(IGameplayCaptureSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        if (!IsRecording)
        {
            return new GameplayCaptureResult(false, StatusText, _sessionDirectory, null, _frames.Count);
        }

        IsRecording = false;
        string? gifPath = null;
        if (_frames.Count > 1 && _sessionDirectory is not null)
        {
            gifPath = Path.Combine(_sessionDirectory, "gameplay.gif");
            sink.SaveGif(_frames, gifPath, _gifFrameDelayCentiseconds);
            StatusText = $"Capture: stopped; wrote {_frames.Count} frames and gameplay.gif";
        }
        else
        {
            StatusText = _frames.Count == 1
                ? "Capture: stopped; kept 1 still frame"
                : "Capture: stopped; no frames captured";
        }

        return new GameplayCaptureResult(true, StatusText, _sessionDirectory, gifPath, _frames.Count);
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var sanitized = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray()).Trim('.', ' ');
        return string.IsNullOrWhiteSpace(sanitized) ? "scenario" : sanitized;
    }
}

internal interface IGameplayCaptureSink
{
    void SaveJpeg(string path);
    void SaveGif(IReadOnlyList<string> framePaths, string gifPath, int frameDelayCentiseconds);
}

internal sealed class MonoGameGameplayCaptureSink : IGameplayCaptureSink
{
    public void SaveJpeg(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var graphicsDevice = Global.GraphicsDevice;
        var width = graphicsDevice.PresentationParameters.BackBufferWidth;
        var height = graphicsDevice.PresentationParameters.BackBufferHeight;
        var data = new XnaColor[width * height];
        graphicsDevice.GetBackBufferData(data);

        using var texture = new Texture2D(graphicsDevice, width, height, mipmap: false, SurfaceFormat.Color);
        texture.SetData(data);
        using var stream = File.Create(path);
        texture.SaveAsJpeg(stream, width, height);
    }

    public void SaveGif(IReadOnlyList<string> framePaths, string gifPath, int frameDelayCentiseconds)
    {
        if (framePaths.Count == 0)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(gifPath)!);
        using var gif = Image.Load<Rgba32>(framePaths[0]);
        gif.Metadata.GetGifMetadata().RepeatCount = 0;
        gif.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = frameDelayCentiseconds;

        foreach (var framePath in framePaths.Skip(1))
        {
            using var frameImage = Image.Load<Rgba32>(framePath);
            gif.Frames.AddFrame(frameImage.Frames.RootFrame);
            gif.Frames[^1].Metadata.GetGifMetadata().FrameDelay = frameDelayCentiseconds;
        }

        gif.SaveAsGif(gifPath, new GifEncoder());
    }
}

internal sealed record GameplayCaptureResult(bool Succeeded, string Message, string? SessionDirectory, string? GifPath, int FrameCount);
