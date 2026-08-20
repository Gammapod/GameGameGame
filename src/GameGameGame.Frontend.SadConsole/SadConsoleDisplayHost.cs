namespace GameGameGame.Frontend.SadConsole;

internal sealed record SadConsoleDisplayHostResult(
    FrontendWindowMode WindowMode,
    int PixelWidth,
    int PixelHeight,
    string Message);

internal static class SadConsoleDisplayHost
{
    private const int OverlaySafeBorderlessInsetPixels = 2;

    public static SadConsoleDisplayHostResult ApplyWindowMode(FrontendWindowMode mode, SadConsoleDisplaySettings displaySettings)
    {
        try
        {
            if (mode == FrontendWindowMode.Fullscreen)
            {
                EnsureWindowBorderless(false);

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

            if (mode == FrontendWindowMode.BorderlessWindowed)
            {
                EnsureExclusiveFullscreen(false);

                var (widthPixels, heightPixels) = ResolveDevicePixels(displaySettings);
                global::SadConsole.Game.Instance.ResizeWindow(widthPixels, heightPixels, resizeOutputSurface: true);
                EnsureWindowBorderless(true);

                global::SadConsole.Host.Global.RecreateRenderOutput?.Invoke(widthPixels, heightPixels);
                global::SadConsole.Host.Global.ResetRendering?.Invoke();
                return new SadConsoleDisplayHostResult(mode, widthPixels, heightPixels, $"Borderless {widthPixels}x{heightPixels}px.");
            }

            if (mode == FrontendWindowMode.OverlaySafeBorderlessWindowed)
            {
                EnsureExclusiveFullscreen(false);

                var (deviceWidthPixels, deviceHeightPixels) = ResolveDevicePixels(displaySettings);
                var widthPixels = Math.Max(1, deviceWidthPixels - OverlaySafeBorderlessInsetPixels);
                var heightPixels = Math.Max(1, deviceHeightPixels - OverlaySafeBorderlessInsetPixels);
                global::SadConsole.Game.Instance.ResizeWindow(widthPixels, heightPixels, resizeOutputSurface: true);
                EnsureWindowBorderless(true);

                global::SadConsole.Host.Global.RecreateRenderOutput?.Invoke(widthPixels, heightPixels);
                global::SadConsole.Host.Global.ResetRendering?.Invoke();
                return new SadConsoleDisplayHostResult(mode, widthPixels, heightPixels, $"Overlay-safe borderless {widthPixels}x{heightPixels}px.");
            }

            // Windowed
            EnsureExclusiveFullscreen(false);
            EnsureWindowBorderless(false);

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

    private static void EnsureExclusiveFullscreen(bool desired)
    {
        if (global::SadConsole.Host.Global.GraphicsDeviceManager is not null
            && global::SadConsole.Host.Global.GraphicsDeviceManager.IsFullScreen != desired)
        {
            global::SadConsole.Game.Instance.ToggleFullScreen();
        }
    }

    private static void EnsureWindowBorderless(bool desired)
    {
        global::SadConsole.Game.Instance.MonoGameInstance.Window.IsBorderless = desired;
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
