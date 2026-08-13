namespace GameGameGame.Frontend.SadConsole;

internal sealed record SadConsoleDisplayHostResult(
    FrontendWindowMode WindowMode,
    int PixelWidth,
    int PixelHeight,
    string Message);

internal static class SadConsoleDisplayHost
{
    public static SadConsoleDisplayHostResult ApplyWindowMode(FrontendWindowMode mode, SadConsoleDisplaySettings displaySettings)
    {
        try
        {
            if (mode == FrontendWindowMode.Fullscreen)
            {
                var (widthPixels, heightPixels) = ResolveDevicePixels(displaySettings);
                global::SadConsole.Game.Instance.ResizeWindow(widthPixels, heightPixels, resizeOutputSurface: true);

                if (global::SadConsole.Host.Global.GraphicsDeviceManager is not null
                    && !global::SadConsole.Host.Global.GraphicsDeviceManager.IsFullScreen)
                {
                    global::SadConsole.Game.Instance.ToggleFullScreen();
                }

                global::SadConsole.Host.Global.RecreateRenderOutput?.Invoke(widthPixels, heightPixels);
                global::SadConsole.Host.Global.ResetRendering?.Invoke();
                return new SadConsoleDisplayHostResult(mode, widthPixels, heightPixels, $"Fullscreen {widthPixels}x{heightPixels}px.");
            }

            if (global::SadConsole.Host.Global.GraphicsDeviceManager is not null
                && global::SadConsole.Host.Global.GraphicsDeviceManager.IsFullScreen)
            {
                global::SadConsole.Game.Instance.ToggleFullScreen();
            }

            global::SadConsole.Game.Instance.ResizeWindow(displaySettings.StartupWindowWidthPixels, displaySettings.StartupWindowHeightPixels, resizeOutputSurface: true);
            global::SadConsole.Host.Global.RecreateRenderOutput?.Invoke(displaySettings.StartupWindowWidthPixels, displaySettings.StartupWindowHeightPixels);
            global::SadConsole.Host.Global.ResetRendering?.Invoke();
            return new SadConsoleDisplayHostResult(mode, displaySettings.StartupWindowWidthPixels, displaySettings.StartupWindowHeightPixels, $"Windowed {displaySettings.StartupWindowWidthPixels}x{displaySettings.StartupWindowHeightPixels}px.");
        }
        catch (Exception ex)
        {
            return new SadConsoleDisplayHostResult(mode, displaySettings.StartupWindowWidthPixels, displaySettings.StartupWindowHeightPixels, $"Display mode change failed: {ex.Message}");
        }
    }

    public static (int Width, int Height) ResolveDevicePixels(SadConsoleDisplaySettings displaySettings)
    {
        try
        {
            global::SadConsole.Game.Instance.GetDeviceScreenSize(out var widthPixels, out var heightPixels);
            if (widthPixels > 0 && heightPixels > 0)
            {
                return (widthPixels, heightPixels);
            }
        }
        catch
        {
            // Fall through to configured startup dimensions.
        }

        return (displaySettings.StartupWindowWidthPixels, displaySettings.StartupWindowHeightPixels);
    }
}
