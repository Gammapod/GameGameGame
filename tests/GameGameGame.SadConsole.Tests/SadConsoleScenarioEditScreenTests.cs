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
        Assert.Contains(components, component => component.Id == "save-status");
        Assert.Equal(UiComponentState.Selected, components.Single(component => component.Id == "scenario-preview").State);
        Assert.Equal(UiComponentState.Saved, components.Single(component => component.Id == "save-status").State);
    }

    [Fact]
    public void ScenarioEditDirtySnapshotShowsUnsavedStatus()
    {
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), DemoSnapshot(isDirty: true));

        var saveStatus = screen.Components().Single(component => component.Id == "save-status");

        Assert.Equal(UiComponentState.Dirty, saveStatus.State);
        Assert.Contains(saveStatus.RenderRows(GameGameGame.SadConsoleApp.Ui.Styling.SadConsoleTheme.Default), row => row.Contains("status: dirty"));
    }

    [Fact]
    public void ScenarioEditCancelWithoutFocusReturnsToScenarioSelection()
    {
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), DemoSnapshot());

        var result = screen.Handle(UiComponentCommand.Cancel);

        Assert.Equal(ScenarioEditResultKind.ReturnToScenarioSelection, result.Kind);
    }

    [Fact]
    public void ScenarioEditCancelWithoutFocusWhenDirtyOpensUnsavedExitModal()
    {
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), DemoSnapshot(isDirty: true));

        var result = screen.Handle(UiComponentCommand.Cancel);

        Assert.Equal(ScenarioEditResultKind.Stay, result.Kind);
        Assert.Equal("unsaved-exit-confirmation", screen.OverlayComponent()?.Id);
        Assert.Contains(screen.OverlayComponent()!.RenderRows(GameGameGame.SadConsoleApp.Ui.Styling.SadConsoleTheme.Default), row => row.Contains("Save & Exit"));
    }

    [Fact]
    public void ScenarioEditUnsavedExitModalEscReturnsToEditing()
    {
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), DemoSnapshot(isDirty: true));

        screen.Handle(UiComponentCommand.Cancel);
        var result = screen.Handle(UiComponentCommand.Cancel);

        Assert.Equal(ScenarioEditResultKind.Stay, result.Kind);
        Assert.Null(screen.OverlayComponent());
        Assert.Contains("Back to editing", result.Message);
    }

    [Fact]
    public void ScenarioEditUnsavedExitModalCanExitWithoutSaving()
    {
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), DemoSnapshot(isDirty: true));

        screen.Handle(UiComponentCommand.Cancel);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Equal(ScenarioEditResultKind.ReturnToScenarioSelection, result.Kind);
        Assert.Contains("without saving", result.Message);
    }

    [Fact]
    public void ScenarioEditFocusesEntityListAndOpensEntityActionModalThenEditRoutes()
    {
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), DemoSnapshot());

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        var modal = screen.Handle(UiComponentCommand.Select);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Equal("entity-list", screen.FocusedComponentId);
        Assert.Equal(2, screen.SelectedEntityIndex);
        Assert.Contains("2.3.1", modal.Message);
        Assert.Equal(ScenarioEditResultKind.OpenEntityTemplate, result.Kind);
        Assert.Equal("rock", result.EntityTemplateId);
    }

    [Fact]
    public void ScenarioEditFocusesActionPlanListAndOpensActionPlanActionModalThenEditRoutes()
    {
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), DemoSnapshot());

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        var modal = screen.Handle(UiComponentCommand.Select);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Equal("action-plan-list", screen.FocusedComponentId);
        Assert.Contains("2.4.1", modal.Message);
        Assert.Equal(ScenarioEditResultKind.OpenActionPlan, result.Kind);
        Assert.Equal("wander", result.ActionPlanId);
    }

    [Fact]
    public void ScenarioEditPinnedCreateTemplateOpensNameEntryAndCreatedTemplate()
    {
        var service = FrontendEditorService.CreateNew();
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), service.GetSnapshot(), service);

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        var open = screen.Handle(UiComponentCommand.Select);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Create new template", open.Message);
        Assert.Equal(ScenarioEditResultKind.OpenEntityTemplate, result.Kind);
        Assert.Contains(service.GetSnapshot().EntityTemplates, template => template.TemplateId == result.EntityTemplateId);
    }

    [Fact]
    public void ScenarioEditDuplicateTemplateUsesNameEntryAndOpensDuplicate()
    {
        var service = FrontendEditorService.CreateNew();
        service.CreateEntityTemplate("Rock");
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), service.GetSnapshot(), service);

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        var openText = screen.Handle(UiComponentCommand.Select);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Duplicate", openText.Message);
        Assert.Equal(ScenarioEditResultKind.OpenEntityTemplate, result.Kind);
        Assert.Equal(2, service.GetSnapshot().EntityTemplates.Count);
    }

    [Fact]
    public void ScenarioEditDeleteTemplateUsesConfirmModal()
    {
        var service = FrontendEditorService.CreateNew();
        service.CreateEntityTemplate("Rock");
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), service.GetSnapshot(), service);

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        var confirm = screen.Handle(UiComponentCommand.Select);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Confirm delete", confirm.Message);
        Assert.Contains("Deleted template", result.Message);
        Assert.Empty(service.GetSnapshot().EntityTemplates);
    }

    [Fact]
    public void ScenarioEditPinnedCreateActionPlanOpensNameEntryAndCreatedPlan()
    {
        var service = FrontendEditorService.CreateNew();
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), service.GetSnapshot(), service);

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        var open = screen.Handle(UiComponentCommand.Select);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Create new action plan", open.Message);
        Assert.Equal(ScenarioEditResultKind.OpenActionPlan, result.Kind);
        Assert.Contains(service.GetSnapshot().ActionPlans, plan => plan.ActionPlanId == result.ActionPlanId);
    }

    [Fact]
    public void ScenarioEditDuplicateActionPlanUsesNameEntryAndOpensDuplicate()
    {
        var service = FrontendEditorService.CreateNew();
        service.CreateActionPlan("Wander");
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), service.GetSnapshot(), service);

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        var openText = screen.Handle(UiComponentCommand.Select);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Duplicate", openText.Message);
        Assert.Equal(ScenarioEditResultKind.OpenActionPlan, result.Kind);
        Assert.Equal(2, service.GetSnapshot().ActionPlans.Count);
    }

    [Fact]
    public void ScenarioEditDeleteActionPlanUsesConfirmModal()
    {
        var service = FrontendEditorService.CreateNew();
        service.CreateActionPlan("Wander");
        var screen = ScenarioEditScreen.FromSnapshot(DemoEntry(), service.GetSnapshot(), service);

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        var confirm = screen.Handle(UiComponentCommand.Select);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Confirm delete", confirm.Message);
        Assert.Contains("Deleted action plan", result.Message);
        Assert.Empty(service.GetSnapshot().ActionPlans);
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

    private static FrontendEditorSnapshot DemoSnapshot(bool isDirty = false) => new(
        "demo.yaml",
        isDirty,
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
        [],
        "yaml",
        []);
}
