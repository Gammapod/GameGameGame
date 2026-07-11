using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Screens;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsoleScenarioEditScreenTests
{
    [Fact]
    public void ScenarioEditComposesPreviewPlayerStartEntityAndActionPlanComponents()
    {
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), DemoSnapshot());

        var components = screen.Components();

        Assert.Contains(components, component => component.Id == "scenario-preview");
        Assert.Contains(components, component => component.Id == "player-start");
        Assert.Contains(components, component => component.Id == "entity-list");
        Assert.Contains(components, component => component.Id == "action-plan-list");
        Assert.Equal(UiComponentState.Selected, components.Single(component => component.Id == "scenario-preview").State);
    }

    [Fact]
    public void ScenarioEditCancelWithoutFocusReturnsToScenarioSelection()
    {
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), DemoSnapshot());

        var result = screen.Handle(UiComponentCommand.Cancel);

        Assert.Equal(ScenarioEditResultKind.ReturnToScenarioSelection, result.Kind);
    }

    [Fact]
    public void ScenarioEditFocusesEntityListAndRoutesEntityOpenPlaceholder()
    {
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), DemoSnapshot());

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Equal("entity-list", screen.FocusedComponentId);
        Assert.Equal(1, screen.SelectedEntityIndex);
        Assert.Equal(ScenarioEditResultKind.OpenEntityTemplate, result.Kind);
        Assert.Equal("rock", result.EntityTemplateId);
    }

    [Fact]
    public void ScenarioEditFocusesActionPlanListAndRoutesActionPlanOpenPlaceholder()
    {
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), DemoSnapshot());

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Equal("action-plan-list", screen.FocusedComponentId);
        Assert.Equal(ScenarioEditResultKind.OpenActionPlan, result.Kind);
        Assert.Equal("wander", result.ActionPlanId);
    }

    [Fact]
    public void ScenarioEditEscReleasesFocusedComponentBeforeReturningToScenarioSelection()
    {
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), DemoSnapshot());

        screen.Handle(UiComponentCommand.Select);
        var release = screen.Handle(UiComponentCommand.Cancel);
        var back = screen.Handle(UiComponentCommand.Cancel);

        Assert.Equal(ScenarioEditResultKind.Stay, release.Kind);
        Assert.Null(screen.FocusedComponentId);
        Assert.Equal(ScenarioEditResultKind.ReturnToScenarioSelection, back.Kind);
    }

    private static ScenarioCatalogEntry DemoEntry() => new("demo.yaml", "demo", "Demo Scenario", "Demo description");

    private static FrontendEditorSnapshot DemoSnapshot() => new(
        "demo.yaml",
        false,
        [new FrontendEditorScenarioSummary("demo", "Demo Scenario", "root", "player", "player-1", new GridCoord(2, 3))],
        [
            new FrontendEditorEntityTemplateSummary(
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
                [],
                [],
                []),
            new FrontendEditorEntityTemplateSummary(
                "rock",
                "Rock",
                '*',
                PresentationColor.Gray,
                1,
                1,
                1,
                1,
                null,
                new FrontendEditorActionStateDefaultsSummary(null, null),
                [],
                [],
                [])
        ],
        [new FrontendEditorActionPlanSummary("wander", "canonical", [new FrontendEditorActionPlanStepSummary(0, default, "Move")], ["Move"])],
        [],
        [],
        "yaml",
        []);
}
