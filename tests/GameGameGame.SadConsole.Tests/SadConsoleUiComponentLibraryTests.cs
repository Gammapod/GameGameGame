using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Navigation;
using GameGameGame.SadConsoleApp.Ui.Rendering;
using GameGameGame.SadConsoleApp.Ui.Styling;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsoleUiComponentLibraryTests
{
    [Fact]
    public void ThemeProvidesSeparateTokensForPanelSelectionStates()
    {
        var theme = SadConsoleTheme.Default;

        Assert.Equal(theme.Panel.BorderUnselected, UiComponentState.Unselected.BorderColor(theme));
        Assert.Equal(theme.Panel.BorderSelected, UiComponentState.Selected.BorderColor(theme));
        Assert.Equal(theme.Panel.BorderFocused, UiComponentState.Focused.BorderColor(theme));
        Assert.Equal(theme.Panel.BorderDisabled, UiComponentState.Disabled.BorderColor(theme));
        Assert.Equal(theme.Panel.BorderError, UiComponentState.Error.BorderColor(theme));
        Assert.NotEqual(theme.Panel.BorderUnselected, theme.Panel.BorderSelected);
        Assert.NotEqual(theme.Panel.BorderSelected, theme.Panel.BorderFocused);
    }

    [Fact]
    public void PanelComponentRendersTitleRowsStatusAndThemeBorderToken()
    {
        var component = new PanelComponent(
            "preview",
            "Scenario Preview",
            new SadConsoleRect(1, 1, 30, 10),
            ["root", "player"],
            UiComponentState.Focused,
            "derived turn-0 runtime preview",
            "T12");

        var rows = component.RenderRows(SadConsoleTheme.Default);

        Assert.Contains("HotPink", rows[0]);
        Assert.Contains("Scenario Preview", rows[0]);
        Assert.Contains("T12", rows[0]);
        Assert.Contains("root", rows);
        Assert.Contains("status: derived turn-0 runtime preview", rows);
    }

    [Fact]
    public void SelectableListMovesSelectionAndScrollsWithoutOwningActivationSemantics()
    {
        var component = new SelectableListComponent(
            "entities",
            "Defined entities",
            new SadConsoleRect(0, 0, 40, 10),
            Enumerable.Range(0, 6).Select(index => new SelectableListItem($"entity-{index}", $"Entity {index}")),
            UiComponentState.Focused,
            visibleRowCount: 3);

        component.MoveSelection(1);
        component.MoveSelection(1);
        component.MoveSelection(1);

        Assert.Equal(3, component.SelectedIndex);
        Assert.Equal("entity-3", component.SelectedItem?.Id);
        Assert.Equal(1, component.ScrollOffset);

        var rows = component.RenderRows(SadConsoleTheme.Default);
        Assert.Contains(rows, row => row.Contains("Entity 3") && row.Contains("HotPink"));
        Assert.Contains(rows, row => row.Contains("rows 2-4 of 6"));
    }

    [Fact]
    public void SelectableListRendersEmptyStateThroughListTheme()
    {
        var component = new SelectableListComponent(
            "action-plans",
            "Action Plans",
            new SadConsoleRect(0, 0, 40, 10),
            [],
            UiComponentState.Selected);

        var rows = component.RenderRows(SadConsoleTheme.Default);

        Assert.Contains(rows, row => row.Contains("empty") && row.Contains(SadConsoleTheme.Default.List.EmptyText));
    }

    [Fact]
    public void StyleTokenStrippingPreservesIntentionalIndentation()
    {
        Assert.Equal("> Entity", ComponentGalleryConsole.StripStyleTokens("> (HotPink) Entity"));
        Assert.Equal("    Description", ComponentGalleryConsole.StripStyleTokens("(White)     Description"));
    }

    [Fact]
    public void EditableFieldTracksReadOnlyEditingDirtyAndInvalidPresentationState()
    {
        var theme = SadConsoleTheme.Default;
        var readOnly = new EditableFieldComponent("root", "scenario root", "root-template", EditableFieldMode.ReadOnly);
        var editing = new EditableFieldComponent("name", "name", "Slime").BeginEdit();
        var dirty = editing.EndEdit("Big Slime");
        var invalid = dirty with { ValidationMessage = "Name is required." };

        Assert.False(readOnly.CanEdit);
        Assert.Contains(theme.Field.ReadOnlyText, readOnly.Render(theme));
        Assert.Contains(theme.Field.EditableText, editing.Render(theme));
        Assert.Contains(theme.Field.DirtyText, dirty.Render(theme));
        Assert.Contains(theme.Field.InvalidText, invalid.Render(theme));
        Assert.Contains("Name is required.", invalid.Render(theme));
    }

    [Fact]
    public void FieldGroupComposesEditableFieldsWithPanelState()
    {
        var group = new FieldGroupComponent(
            "presentation",
            "Presentation",
            new SadConsoleRect(0, 0, 40, 8),
            [
                new EditableFieldComponent("name", "name", "Player"),
                new EditableFieldComponent("glyph", "glyph", "@")
            ],
            UiComponentState.Selected);

        var rows = group.RenderRows(SadConsoleTheme.Default);

        Assert.Contains("Gold", rows[0]);
        Assert.Contains("Presentation", rows[0]);
        Assert.Contains(rows, row => row.Contains("name"));
        Assert.Contains(rows, row => row.Contains("glyph"));
    }

    [Fact]
    public void FocusRouterSelectsFocusesRoutesThenReleasesComponentFocus()
    {
        var router = new FocusRouter([
            new FocusTarget("preview"),
            new FocusTarget("player-start"),
            new FocusTarget("entities")
        ]);

        Assert.Equal("preview", router.SelectedComponentId);
        Assert.Equal(UiComponentState.Selected, router.StateFor("preview"));

        var moved = router.Handle(UiComponentCommand.Right);
        Assert.Equal(FocusRouterResultKind.SelectedComponent, moved.Kind);
        Assert.Equal("player-start", router.SelectedComponentId);

        var focused = router.Handle(UiComponentCommand.Select);
        Assert.Equal(FocusRouterResultKind.FocusedComponent, focused.Kind);
        Assert.Equal(UiComponentState.Focused, router.StateFor("player-start"));

        var routed = router.Handle(UiComponentCommand.Down);
        Assert.Equal(FocusRouterResultKind.RouteToFocusedComponent, routed.Kind);
        Assert.Equal("player-start", routed.ComponentId);
        Assert.Equal(UiComponentCommand.Down, routed.RoutedCommand);

        var released = router.Handle(UiComponentCommand.Cancel);
        Assert.Equal(FocusRouterResultKind.ReleasedFocus, released.Kind);
        Assert.Equal(UiComponentState.Selected, router.StateFor("player-start"));
    }

    [Fact]
    public void FocusRouterSkipsDisabledTargetsAndCancelWithoutFocusExitsScreen()
    {
        var router = new FocusRouter([
            new FocusTarget("preview", IsEnabled: false),
            new FocusTarget("entities"),
            new FocusTarget("plans", IsEnabled: false)
        ]);

        Assert.Equal("entities", router.SelectedComponentId);
        Assert.Equal(UiComponentState.Disabled, router.StateFor("preview"));
        Assert.Equal(UiComponentState.Selected, router.StateFor("entities"));
        Assert.Equal(FocusRouterResultKind.CancelScreen, router.Handle(UiComponentCommand.Cancel).Kind);
    }
}
