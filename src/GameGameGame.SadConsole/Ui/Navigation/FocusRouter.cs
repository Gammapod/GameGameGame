using GameGameGame.SadConsoleApp.Ui.Components;

namespace GameGameGame.SadConsoleApp.Ui.Navigation;

internal sealed record FocusTarget(string ComponentId, bool IsEnabled = true);

internal sealed class FocusRouter
{
    private readonly List<FocusTarget> _targets;

    public FocusRouter(IEnumerable<FocusTarget> targets)
    {
        _targets = targets.ToList();
        SelectedIndex = FirstEnabledIndex();
    }

    public int SelectedIndex { get; private set; }
    public bool HasFocusedComponent { get; private set; }
    public string? SelectedComponentId => SelectedIndex < 0 || SelectedIndex >= _targets.Count ? null : _targets[SelectedIndex].ComponentId;
    public string? FocusedComponentId => HasFocusedComponent ? SelectedComponentId : null;

    public FocusRouterResult Handle(UiComponentCommand command)
    {
        if (_targets.Count == 0 || SelectedIndex < 0)
        {
            return FocusRouterResult.Ignored;
        }

        if (HasFocusedComponent)
        {
            if (command == UiComponentCommand.Cancel)
            {
                HasFocusedComponent = false;
                return FocusRouterResult.ReleasedFocus(SelectedComponentId!);
            }

            return FocusRouterResult.RouteToFocusedComponent(SelectedComponentId!, command);
        }

        switch (command)
        {
            case UiComponentCommand.Up:
            case UiComponentCommand.Left:
                MoveSelection(-1);
                return FocusRouterResult.Selected(SelectedComponentId!);
            case UiComponentCommand.Down:
            case UiComponentCommand.Right:
                MoveSelection(1);
                return FocusRouterResult.Selected(SelectedComponentId!);
            case UiComponentCommand.Select:
                HasFocusedComponent = true;
                return FocusRouterResult.Focused(SelectedComponentId!);
            case UiComponentCommand.Cancel:
                return FocusRouterResult.CancelScreen;
            default:
                return FocusRouterResult.Ignored;
        }
    }

    public UiComponentState StateFor(string componentId)
    {
        var index = _targets.FindIndex(target => target.ComponentId == componentId);
        if (index < 0 || !_targets[index].IsEnabled)
        {
            return UiComponentState.Disabled;
        }

        if (index != SelectedIndex)
        {
            return UiComponentState.Unselected;
        }

        return HasFocusedComponent ? UiComponentState.Focused : UiComponentState.Selected;
    }

    private void MoveSelection(int delta)
    {
        if (_targets.Count == 0)
        {
            return;
        }

        var index = SelectedIndex;
        for (var attempts = 0; attempts < _targets.Count; attempts++)
        {
            index = Math.Clamp(index + delta, 0, _targets.Count - 1);
            if (_targets[index].IsEnabled)
            {
                SelectedIndex = index;
                return;
            }

            if (index == 0 || index == _targets.Count - 1)
            {
                return;
            }
        }
    }

    private int FirstEnabledIndex() => _targets.FindIndex(target => target.IsEnabled);
}

internal sealed record FocusRouterResult(FocusRouterResultKind Kind, string? ComponentId = null, UiComponentCommand? RoutedCommand = null)
{
    public static FocusRouterResult Ignored { get; } = new(FocusRouterResultKind.Ignored);
    public static FocusRouterResult CancelScreen { get; } = new(FocusRouterResultKind.CancelScreen);
    public static FocusRouterResult Selected(string componentId) => new(FocusRouterResultKind.SelectedComponent, componentId);
    public static FocusRouterResult Focused(string componentId) => new(FocusRouterResultKind.FocusedComponent, componentId);
    public static FocusRouterResult ReleasedFocus(string componentId) => new(FocusRouterResultKind.ReleasedFocus, componentId);
    public static FocusRouterResult RouteToFocusedComponent(string componentId, UiComponentCommand command) => new(FocusRouterResultKind.RouteToFocusedComponent, componentId, command);
}

internal enum FocusRouterResultKind
{
    Ignored,
    SelectedComponent,
    FocusedComponent,
    ReleasedFocus,
    RouteToFocusedComponent,
    CancelScreen
}
