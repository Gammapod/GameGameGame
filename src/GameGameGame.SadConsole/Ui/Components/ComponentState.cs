using GameGameGame.SadConsoleApp.Ui.Styling;
using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsoleApp.Ui.Components;

internal enum UiComponentState
{
    Unselected,
    Selected,
    Focused,
    Disabled,
    Error
}

internal enum UiComponentCommand
{
    Up,
    Down,
    Left,
    Right,
    Select,
    Cancel
}

internal interface IUiComponent
{
    string Id { get; }
    string Title { get; }
    SadConsoleRect Bounds { get; }
    UiComponentState State { get; }
    IReadOnlyList<string> RenderRows(SadConsoleTheme theme);
}

internal static class UiComponentStateExtensions
{
    public static string BorderColor(this UiComponentState state, SadConsoleTheme theme) => state switch
    {
        UiComponentState.Unselected => theme.Panel.BorderUnselected,
        UiComponentState.Selected => theme.Panel.BorderSelected,
        UiComponentState.Focused => theme.Panel.BorderFocused,
        UiComponentState.Disabled => theme.Panel.BorderDisabled,
        UiComponentState.Error => theme.Panel.BorderError,
        _ => theme.Panel.BorderUnselected
    };

    public static bool CanReceiveFocus(this UiComponentState state) => state is not UiComponentState.Disabled;
}
