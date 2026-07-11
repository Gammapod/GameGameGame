using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Screens;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsoleActionPlanEditScreenTests
{
    [Fact]
    public void ActionPlanEditComposesActionStepList()
    {
        var screen = ActionPlanEditScreen.FromSnapshot(DemoSnapshot(), "wander", ActionPlanEditReturnDestination.ScenarioEdit);

        var component = Assert.Single(screen.Components());

        Assert.Equal("action-plan-steps", component.Id);
        Assert.Equal("4.1 Action steps", component.Title);
        Assert.Equal(UiComponentState.Selected, component.State);
    }

    [Fact]
    public void ActionPlanEditCancelReturnsToScenarioEditWhenOpenedFromScenario()
    {
        var screen = ActionPlanEditScreen.FromSnapshot(DemoSnapshot(), "wander", ActionPlanEditReturnDestination.ScenarioEdit);

        var result = screen.Handle(UiComponentCommand.Cancel);

        Assert.Equal(ActionPlanEditResultKind.Return, result.Kind);
        Assert.Equal(ActionPlanEditReturnDestination.ScenarioEdit, result.ReturnDestination);
    }

    [Fact]
    public void ActionPlanEditCancelReturnsToEntityTemplateWhenOpenedFromEntity()
    {
        var screen = ActionPlanEditScreen.FromSnapshot(DemoSnapshot(), "wander", ActionPlanEditReturnDestination.EntityTemplateEdit);

        var result = screen.Handle(UiComponentCommand.Cancel);

        Assert.Equal(ActionPlanEditResultKind.Return, result.Kind);
        Assert.Equal(ActionPlanEditReturnDestination.EntityTemplateEdit, result.ReturnDestination);
    }

    [Fact]
    public void ActionPlanEditFocusesStepsSelectsRowsAndReportsEditPlaceholder()
    {
        var screen = ActionPlanEditScreen.FromSnapshot(DemoSnapshot(), "wander", ActionPlanEditReturnDestination.ScenarioEdit);

        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Equal("action-plan-steps", screen.FocusedComponentId);
        Assert.Equal(1, screen.SelectedStepIndex);
        Assert.Contains("Action-step edit placeholder", result.Message);
        Assert.Contains("Wait", result.Message);
    }

    [Fact]
    public void ScenarioEditCanCreateActionPlanEditScreenWithScenarioReturnDestination()
    {
        var scenario = ScenarioEditScreen.FromSnapshot(new ScenarioCatalogEntry("demo.yaml", "demo", "Demo"), DemoSnapshot());

        var actionPlan = scenario.OpenActionPlanEditScreen("wander", ActionPlanEditReturnDestination.ScenarioEdit);

        Assert.NotNull(actionPlan);
        Assert.Equal(ActionPlanEditReturnDestination.ScenarioEdit, actionPlan.ReturnDestination);
    }

    private static FrontendEditorSnapshot DemoSnapshot() => new(
        "demo.yaml",
        false,
        [new FrontendEditorScenarioSummary("demo", "Demo Scenario", "root", "player", "player-1", new GridCoord(2, 3))],
        [],
        [new FrontendEditorActionPlanSummary(
            "wander",
            "canonical",
            [
                new FrontendEditorActionPlanStepSummary(0, default, "Move"),
                new FrontendEditorActionPlanStepSummary(1, default, "Wait")
            ],
            ["Move", "Wait"])],
        [],
        [],
        "yaml",
        []);
}
