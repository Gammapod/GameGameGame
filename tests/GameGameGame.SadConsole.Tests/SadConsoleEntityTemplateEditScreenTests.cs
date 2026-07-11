using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Screens;
using GameGameGame.SadConsoleApp.Ui.Styling;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsoleEntityTemplateEditScreenTests
{
    [Fact]
    public void EntityTemplateEditComposesPresentationTargetingAndInventoryComponents()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        var components = screen.Components();

        Assert.Contains(components, component => component.Id == "presentation");
        Assert.Contains(components, component => component.Id == "targeting");
        Assert.Contains(components, component => component.Id == "inventory");
        Assert.Equal(UiComponentState.Selected, components.Single(component => component.Id == "presentation").State);
    }

    [Fact]
    public void EntityTemplateEditCancelWithoutFocusReturnsToScenarioEdit()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        var result = screen.Handle(UiComponentCommand.Cancel);

        Assert.Equal(EntityTemplateEditResultKind.ReturnToScenarioEdit, result.Kind);
    }

    [Fact]
    public void EntityTemplateEditEscReleasesFocusedComponentBeforeReturning()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        screen.Handle(UiComponentCommand.Select);
        var release = screen.Handle(UiComponentCommand.Cancel);
        var back = screen.Handle(UiComponentCommand.Cancel);

        Assert.Equal(EntityTemplateEditResultKind.Stay, release.Kind);
        Assert.Null(screen.FocusedComponentId);
        Assert.Equal(EntityTemplateEditResultKind.ReturnToScenarioEdit, back.Kind);
    }

    [Fact]
    public void EntityTemplateEditPresentationFocusCanJumpToDefaultActionPlanPlaceholder()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        screen.Handle(UiComponentCommand.Select);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Equal(EntityTemplateEditResultKind.OpenActionPlan, result.Kind);
        Assert.Equal("wander", result.ActionPlanId);
    }

    [Fact]
    public void EntityTemplateEditTargetingListShowsCompactSlotSummariesAndOpensDetailOverlay()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        Assert.Equal(1, screen.SelectedTargetingSlotIndex);

        var rows = screen.Components().SelectMany(component => component.RenderRows(SadConsoleTheme.Default)).ToList();
        Assert.Contains(rows, row => row.Contains("slot 1: primary Rock"));
        Assert.Contains(rows, row => row.Contains("slot 2: secondary Slime"));
        Assert.DoesNotContain(rows, row => row.Contains("range=3"));

        var open = screen.Handle(UiComponentCommand.Select);
        var overlay = screen.OverlayComponent();

        Assert.Contains("Opened 3.2.1", open.Message);
        Assert.NotNull(overlay);
        Assert.Equal("targeting-slot-detail", overlay.Id);
        var overlayRows = overlay.RenderRows(SadConsoleTheme.Default);
        Assert.Contains(overlayRows, row => row.Contains("target label"));
        Assert.Contains(overlayRows, row => row.Contains("target template/criteria"));
        Assert.Contains(overlayRows, row => row.Contains("target range"));

        var close = screen.Handle(UiComponentCommand.Cancel);
        Assert.Contains("Closed targeting slot detail", close.Message);
        Assert.Null(screen.OverlayComponent());
    }

    [Fact]
    public void EntityTemplateEditInventorySelectionClampsAndRendersPlaceholder()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);

        screen.Handle(UiComponentCommand.Down);
        Assert.Equal(1, screen.SelectedInventoryItemIndex);

        var rows = screen.Components().SelectMany(component => component.RenderRows(SadConsoleTheme.Default)).ToList();
        Assert.Contains(rows, row => row.Contains("3.3.2 inventory-drawing panel: placeholder"));
    }

    private static FrontendEditorSnapshot DemoSnapshot() => new(
        "demo.yaml",
        false,
        [new FrontendEditorScenarioSummary("demo", "Demo Scenario", "root", "player", "player-1", new GridCoord(2, 3))],
        [new FrontendEditorEntityTemplateSummary(
            "player",
            "Player",
            '@',
            PresentationColor.Yellow,
            5,
            4,
            1,
            9,
            "wander",
            new FrontendEditorActionStateDefaultsSummary(Direction.East, null),
            [
                new FrontendEditorTargetingRuleSummary(0, "primary", null, "rock", "Rock", 3),
                new FrontendEditorTargetingRuleSummary(1, "secondary", null, "slime", "Slime", 4)
            ],
            [
                new FrontendEditorCarriedEntitySummary("rock-1", "rock", "Rock", '*', PresentationColor.Gray, new GridCoord(1, 2), []),
                new FrontendEditorCarriedEntitySummary("slime-1", "slime", "Slime", 's', PresentationColor.Green, new GridCoord(2, 2), [])
            ],
            [])],
        [new FrontendEditorActionPlanSummary("wander", "canonical", [new FrontendEditorActionPlanStepSummary(0, default, "Move")], ["Move"])],
        [],
        [],
        "yaml",
        []);
}
