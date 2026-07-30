using GameGameGame.SadConsoleApp.Ui.Screens;

namespace GameGameGame.SadConsoleApp.Ui.Rendering;

internal sealed record GameplayCapturePolicy(
    GameplayCaptureTiming Timing,
    GameplayCaptureRegionScope RegionScope,
    bool IncludePromptOverlays)
{
    public static GameplayCapturePolicy Default { get; } = new(
        GameplayCaptureTiming.PlayerControlFrames,
        GameplayCaptureRegionScope.FullBackBuffer,
        IncludePromptOverlays: false);

    public bool ShouldQueueAfterPlayerSubmission(GameplayRuntimeSubmission? submission, ConsumerPlayModeRenderFrame frame) =>
        Timing == GameplayCaptureTiming.PlayerControlFrames
        && submission is { Succeeded: true }
        && (IncludePromptOverlays || !frame.PromptOverlayActive);

    public string DebugSummary => $"Capture policy: {Timing}; region {RegionScope}; prompts {(IncludePromptOverlays ? "included" : "excluded")}";
}

internal enum GameplayCaptureTiming
{
    PlayerControlFrames
}

internal enum GameplayCaptureRegionScope
{
    FullBackBuffer
}
