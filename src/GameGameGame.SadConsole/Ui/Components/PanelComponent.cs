using GameGameGame.SadConsoleApp.Ui.Styling;
using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsoleApp.Ui.Components;

internal sealed record PanelComponent(
    string Id,
    string Title,
    SadConsoleRect Bounds,
    IReadOnlyList<string> BodyRows,
    UiComponentState State = UiComponentState.Unselected,
    string? Status = null,
    string? HeaderRight = null) : IUiComponent
{
    public string BorderColor(SadConsoleTheme theme) => State.BorderColor(theme);

    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme)
    {
        var rows = new List<string>
        {
            string.IsNullOrWhiteSpace(HeaderRight)
                ? $"[{BorderColor(theme)}] {Title}"
                : $"[{BorderColor(theme)}] {Title} {HeaderRight}"
        };
        rows.AddRange(BodyRows.Count == 0 ? [$"({theme.Panel.MutedText}) empty"] : BodyRows);
        if (!string.IsNullOrWhiteSpace(Status))
        {
            rows.Add($"status: {Status}");
        }

        return rows;
    }
}
