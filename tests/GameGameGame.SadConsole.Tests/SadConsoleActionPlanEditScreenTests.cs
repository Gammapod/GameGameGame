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

        var component = screen.Components().Single(component => component.Id == "action-plan-steps");

        Assert.Equal("action-plan-steps", component.Id);
        Assert.Equal("4.1 Action steps", component.Title);
        Assert.Equal(UiComponentState.Selected, component.State);
        Assert.Contains(screen.Components(), component => component.Id == "highlighted-action-step");
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
    public void ActionPlanEditFocusesStepsAndSelectsRows()
    {
        var screen = ActionPlanEditScreen.FromSnapshot(DemoSnapshot(), "wander", ActionPlanEditReturnDestination.ScenarioEdit);

        screen.Handle(UiComponentCommand.Select);
        var result = screen.Handle(UiComponentCommand.Down);

        Assert.Equal("action-plan-steps", screen.FocusedComponentId);
        Assert.Equal(1, screen.SelectedStepIndex);
        Assert.Contains("Highlighted step 2", result.Message);
    }

    [Fact]
    public void ActionPlanEditSelectOnExistingStepOpensReplacementPicker()
    {
        var screen = ActionPlanEditScreen.FromSnapshot(ServiceSnapshotWithPlan(out _, out var planId), planId, ActionPlanEditReturnDestination.ScenarioEdit);

        screen.Handle(UiComponentCommand.Select);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Opened replacement picker", result.Message);
        Assert.Equal("action-step-primitive-picker", screen.OverlayComponent()?.Id);
        Assert.Contains(screen.Components().Single(component => component.Id == "highlighted-action-step").RenderRows(GameGameGame.SadConsoleApp.Ui.Styling.SadConsoleTheme.Default), row => row.Contains("highlighting primitive"));
    }

    [Fact]
    public void ActionPlanEditReplaceDeleteInsertAndMoveMutateThroughEditorService()
    {
        var snapshot = ServiceSnapshotWithPlan(out var service, out var planId);
        var screen = ActionPlanEditScreen.FromSnapshot(snapshot, planId, ActionPlanEditReturnDestination.ScenarioEdit, service);

        screen.Handle(UiComponentCommand.Select);
        var insertOpen = screen.Handle(ActionPlanEditCommand.Insert);
        Assert.Contains("Opened insert action-step picker", insertOpen.Message);
        Assert.Equal("action-step-primitive-picker", screen.OverlayComponent()?.Id);
        var insert = screen.Handle(UiComponentCommand.Select);
        Assert.Contains("Inserted", insert.Message);
        Assert.Single(service.GetSnapshot().ActionPlans.Single(plan => plan.ActionPlanId == planId).ActionSteps);

        screen.Handle(UiComponentCommand.Select);
        var replace = screen.Handle(UiComponentCommand.Select);
        Assert.Contains("Replaced", replace.Message);

        var secondInsertOpen = screen.Handle(ActionPlanEditCommand.Insert);
        Assert.Contains("insert-position picker", secondInsertOpen.Message);
        Assert.Equal("action-step-insert-position", screen.OverlayComponent()?.Id);
        screen.Handle(UiComponentCommand.Select);
        Assert.Equal("action-step-primitive-picker", screen.OverlayComponent()?.Id);
        screen.Handle(UiComponentCommand.Select);
        Assert.Equal(2, service.GetSnapshot().ActionPlans.Single(plan => plan.ActionPlanId == planId).ActionSteps.Count);

        var moveMode = screen.Handle(ActionPlanEditCommand.ToggleMoveMode);
        Assert.True(screen.IsMoveMode);
        Assert.Contains("Move mode", moveMode.Message);
        var move = screen.Handle(UiComponentCommand.Down);
        Assert.Contains("Moved", move.Message);
        var place = screen.Handle(ActionPlanEditCommand.ToggleMoveMode);
        Assert.Contains("Placed", place.Message);
        Assert.False(screen.IsMoveMode);

        var delete = screen.Handle(ActionPlanEditCommand.Delete);
        Assert.Contains("Removed", delete.Message);
        Assert.Single(service.GetSnapshot().ActionPlans.Single(plan => plan.ActionPlanId == planId).ActionSteps);
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
        [
            new FrontendEditorAvailableActionStepSummary(ActionPlanBehaviorStepKind.FleeTarget, "Flee Target", "Flee a target."),
            new FrontendEditorAvailableActionStepSummary(ActionPlanBehaviorStepKind.SeekTarget, "Seek Target", "Seek a target.")
        ],
        [],
        "yaml",
        []);

    private static FrontendEditorSnapshot ServiceSnapshotWithPlan(out FrontendEditorService service, out string planId)
    {
        service = FrontendEditorService.CreateNew();
        var plan = service.CreatePassiveActionPlan("Patrol");
        planId = plan.Snapshot.ActionPlans.Single().ActionPlanId;
        return service.GetSnapshot();
    }
}
