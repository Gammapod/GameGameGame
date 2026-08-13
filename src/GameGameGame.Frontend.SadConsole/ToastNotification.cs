using SadRogue.Primitives;

namespace GameGameGame.Frontend.SadConsole;

internal sealed class ToastNotificationState
{
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(4);

    private TimeSpan _elapsed;

    public ToastNotificationState(IReadOnlyList<string> rows, TimeSpan? duration = null)
    {
        Rows = rows;
        Duration = duration ?? DefaultDuration;
    }

    public IReadOnlyList<string> Rows { get; }
    public TimeSpan Duration { get; }
    public bool IsExpired => _elapsed >= Duration;

    public bool Advance(TimeSpan delta)
    {
        if (IsExpired)
        {
            return false;
        }

        _elapsed += delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        return IsExpired;
    }
}

internal static class ToastNotificationPresenter
{
    public static ToastNotificationState LaunchWarning(ScenarioLaunchFailurePresentation failure)
    {
        var rows = new List<string>
        {
            "Warning",
            failure.Summary
        };
        rows.AddRange(failure.Details.Take(3));
        return new ToastNotificationState(rows);
    }

    public static OverlayPanelModel ToOverlay(
        ToastNotificationState toast,
        ScenarioBrowserLayout layout,
        SadConsoleDisplaySettings displaySettings)
    {
        var width = Math.Min(Math.Max(0, layout.TextWidth - 2), 78);
        var height = Math.Min(Math.Max(4, toast.Rows.Count + 2), Math.Max(4, layout.MessageY - layout.ListY));
        var background = new Color((byte)48, (byte)28, (byte)0, (byte)225);

        return new OverlayPanelModel(
            OverlayPanelGeometry.HalfTileOffset(new FrontendRect(layout.TextX + 1, layout.ListY, width + 2, height), displaySettings),
            toast.Rows,
            Color.Orange,
            Color.White,
            background);
    }
}
