using GameGameGame.SadConsoleApp.Ui.Styling;

namespace GameGameGame.SadConsoleApp.Ui.Components;

internal sealed record PlayEntityTooltipComponent(
    string Id,
    string Title,
    SadConsoleRect Bounds,
    IReadOnlyList<string> BodyRows,
    byte BackgroundAlpha = 192) : IUiComponent
{
    public UiComponentState State => UiComponentState.Unselected;

    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme) => BodyRows;
}
