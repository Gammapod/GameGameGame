namespace GameGameGame.Frontend.SadConsole;

internal sealed record ScenarioBrowserDebugOverlay(
    bool IsVisible,
    IReadOnlyList<string> Rows)
{
    public static ScenarioBrowserDebugOverlay Hidden { get; } = new(false, []);

    public static ScenarioBrowserDebugOverlay Build(
        ScenarioBrowserScreenModel model,
        FrontendDisplayShell shell,
        ScenarioBrowserLayout layout,
        FrontendWindowMode windowMode) =>
        new(true,
        [
            "F12 layout debug",
            $"screen cells: {shell.LogicalWidth}x{shell.LogicalHeight} from {shell.PixelWidth}x{shell.PixelHeight}px",
            $"drawable: x={shell.DrawableBounds.X} y={shell.DrawableBounds.Y} w={shell.DrawableBounds.Width} h={shell.DrawableBounds.Height}",
            $"scenario list: y={layout.ListY} h={layout.ListHeight} selected={model.SelectedIndex + 1}/{model.Entries.Count} viewport={model.Viewport(layout.ListHeight).StartIndex + 1}-{model.Viewport(layout.ListHeight).EndIndexExclusive}",
            $"selected: {model.SelectedEntry?.ScenarioId ?? "none"} ({(model.SelectedEntry?.IsWorkspaceBacked == true ? "workspace" : "file/none")})",
            $"selector: {(model.ActionSelectorOpen ? "open" : "closed")} option={model.SelectedActionOption}",
            $"diagnostics: {model.Diagnostics.Count} | window: {windowMode} | input: {model.ActiveInputMode} | F11 toggles fullscreen/windowed"
        ]);
}

internal sealed class ScenarioBrowserChromeState(FrontendWindowMode windowMode = FrontendWindowMode.Fullscreen, bool layoutDebugVisible = false)
{
    public bool LayoutDebugVisible { get; private set; } = layoutDebugVisible;
    public FrontendWindowMode WindowMode { get; private set; } = windowMode;

    public bool ToggleLayoutDebug()
    {
        LayoutDebugVisible = !LayoutDebugVisible;
        return LayoutDebugVisible;
    }

    public FrontendWindowMode ToggleWindowMode()
    {
        WindowMode = WindowMode == FrontendWindowMode.Fullscreen
            ? FrontendWindowMode.Windowed
            : FrontendWindowMode.Fullscreen;
        return WindowMode;
    }
}
