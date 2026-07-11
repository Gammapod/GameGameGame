using GameGameGame.SadConsoleApp.Ui.Styling;
using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsoleApp.Ui.Components;

internal sealed record SelectableListItem(string Id, string Label, string? Detail = null, bool IsEnabled = true);

internal sealed class SelectableListComponent : IUiComponent
{
    private readonly List<SelectableListItem> _items;

    public SelectableListComponent(
        string id,
        string title,
        SadConsoleRect bounds,
        IEnumerable<SelectableListItem> items,
        UiComponentState state = UiComponentState.Unselected,
        int visibleRowCount = 8)
    {
        Id = id;
        Title = title;
        Bounds = bounds;
        State = state;
        VisibleRowCount = Math.Max(1, visibleRowCount);
        _items = items.ToList();
    }

    public string Id { get; }
    public string Title { get; }
    public SadConsoleRect Bounds { get; }
    public UiComponentState State { get; private set; }
    public int SelectedIndex { get; private set; }
    public int ScrollOffset { get; private set; }
    public int VisibleRowCount { get; }
    public IReadOnlyList<SelectableListItem> Items => _items;
    public SelectableListItem? SelectedItem => _items.Count == 0 ? null : _items[SelectedIndex];

    public void SetState(UiComponentState state) => State = state;

    public void MoveSelection(int delta)
    {
        if (_items.Count == 0 || delta == 0)
        {
            return;
        }

        var next = Math.Clamp(SelectedIndex + delta, 0, _items.Count - 1);
        SelectedIndex = next;
        EnsureSelectedVisible();
    }

    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme)
    {
        var rows = new List<string> { $"[{State.BorderColor(theme)}] {Title}" };
        if (_items.Count == 0)
        {
            rows.Add($"({theme.List.EmptyText}) empty");
            return rows;
        }

        foreach (var (item, index) in VisibleItems())
        {
            var marker = index == SelectedIndex ? ">" : " ";
            var color = index == SelectedIndex
                ? State == UiComponentState.Focused ? theme.List.FocusedRowText : theme.List.SelectedRowText
                : theme.List.RowText;
            var enabled = item.IsEnabled ? string.Empty : " disabled";
            var detail = string.IsNullOrWhiteSpace(item.Detail) ? string.Empty : $" - {item.Detail}";
            rows.Add($"{marker} ({color}) {item.Label}{detail}{enabled}");
        }

        if (ScrollOffset > 0 || ScrollOffset + VisibleRowCount < _items.Count)
        {
            rows.Add($"({theme.List.ScrollIndicator}) rows {ScrollOffset + 1}-{Math.Min(_items.Count, ScrollOffset + VisibleRowCount)} of {_items.Count}");
        }

        return rows;
    }

    private IEnumerable<(SelectableListItem Item, int Index)> VisibleItems()
    {
        for (var index = ScrollOffset; index < Math.Min(_items.Count, ScrollOffset + VisibleRowCount); index++)
        {
            yield return (_items[index], index);
        }
    }

    private void EnsureSelectedVisible()
    {
        if (SelectedIndex < ScrollOffset)
        {
            ScrollOffset = SelectedIndex;
        }
        else if (SelectedIndex >= ScrollOffset + VisibleRowCount)
        {
            ScrollOffset = SelectedIndex - VisibleRowCount + 1;
        }
    }
}
