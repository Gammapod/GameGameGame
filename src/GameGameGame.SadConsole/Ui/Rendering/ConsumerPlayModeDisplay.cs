using GameGameGame.SadConsoleApp.Ui.Screens;
using GameGameGame.SadConsoleApp.Ui.Tiles;
using SadConsole.Host;

namespace GameGameGame.SadConsoleApp.Ui.Rendering;

internal interface IConsumerPlayModeDisplay
{
    ConsumerPlayModeLayout EnterFullscreenAndResolveLayout(SadConsoleDisplaySettings displaySettings, bool debugVisible = false);
}

internal sealed class SadConsoleConsumerPlayModeDisplay : IConsumerPlayModeDisplay
{
    public ConsumerPlayModeLayout EnterFullscreenAndResolveLayout(SadConsoleDisplaySettings displaySettings, bool debugVisible = false)
    {
        var (widthPixels, heightPixels) = ResolveDevicePixels(displaySettings);
        TryEnterFullscreen(widthPixels, heightPixels);
        return ConsumerPlayModeLayout.FromPixels(widthPixels, heightPixels, displaySettings, debugVisible);
    }

    private static (int Width, int Height) ResolveDevicePixels(SadConsoleDisplaySettings displaySettings)
    {
        try
        {
            SadConsole.Game.Instance.GetDeviceScreenSize(out var widthPixels, out var heightPixels);
            if (widthPixels > 0 && heightPixels > 0)
            {
                return (widthPixels, heightPixels);
            }
        }
        catch
        {
            // Fall back to the configured startup window if the host cannot report a device size.
        }

        return (displaySettings.WindowWidthPixels, displaySettings.WindowHeightPixels);
    }

    private static void TryEnterFullscreen(int widthPixels, int heightPixels)
    {
        try
        {
            SadConsole.Game.Instance.ResizeWindow(widthPixels, heightPixels, resizeOutputSurface: true);

            if (Global.GraphicsDeviceManager is not null && !Global.GraphicsDeviceManager.IsFullScreen)
            {
                SadConsole.Game.Instance.ToggleFullScreen();
            }

            Global.RecreateRenderOutput?.Invoke(widthPixels, heightPixels);
            Global.ResetRendering?.Invoke();
        }
        catch
        {
            // Fullscreen is best-effort; the pure play layout remains usable if the host rejects the mode switch.
        }
    }
}
