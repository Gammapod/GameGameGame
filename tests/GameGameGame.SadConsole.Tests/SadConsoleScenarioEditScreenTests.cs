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
    public void ScenarioEditOpenConsumesScenarioSurfaceWithoutChangingEditorShape()
    {
        var path = WriteTempContentFile(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 2
                inventoryHeight: 2
                weight: 100
                carryingCapacity: 100
                carriedEntities:
                  - entityId: actor1
                    templateId: actor
                    coord: { x: 0, y: 0 }
              actor:
                name: Actor
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 1
                carryingCapacity: 1
              unused:
                name: Unused
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 1
                carryingCapacity: 1
            presentations:
              room: { glyph: '#', color: Gray }
              actor: { glyph: a, color: Green }
              unused: { glyph: u, color: Yellow }
            actionPlans: {}
            scenarios:
              smoke:
                name: Smoke
                scenarioRootEntityTemplateId: room
                playerEntityTemplateId: actor
                playerEntityId: player
                playerStart: { x: 0, y: 0 }
            """);
        try
        {
            var screen = ScenarioEditScreen.Open(new ScenarioCatalogEntry(path, "smoke", "Smoke", "Smoke scenario")).Screen;

            var previewRows = screen.Components().Single(component => component.Id == "scenario-preview").RenderRows(GameGameGame.SadConsoleApp.Ui.Styling.SadConsoleTheme.Default);
            var playerRows = screen.Components().Single(component => component.Id == "player-start").RenderRows(GameGameGame.SadConsoleApp.Ui.Styling.SadConsoleTheme.Default);

            Assert.Contains(previewRows, row => row.Contains("Type-first surface: 1 scenarios, 3 templates, 0 action plans"));
            Assert.Contains(previewRows, row => row.Contains("Scenario refs: 2 | dependencies: 2"));
            Assert.Contains(playerRows, row => row.Contains("scenario root") && row.Contains("room"));
            Assert.Contains(playerRows, row => row.Contains("player X position") && row.Contains("0"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ScenarioEditOpenShowsSurfaceMissingReferencesInDiagnostics()
    {
        var path = WriteTempContentFile(
            """
            entityTemplates:
              actor:
                name: Actor
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 1
                carryingCapacity: 1
            presentations:
              actor: { glyph: a, color: Green }
            actionPlans: {}
            scenarios:
              broken:
                name: Broken
                scenarioRootEntityTemplateId: missingRoom
            """);
        try
        {
            var screen = ScenarioEditScreen.Open(new ScenarioCatalogEntry(path, "broken", "Broken", "Broken scenario")).Screen;

            var diagnostics = screen.Components().Single(component => component.Id == "scenario-edit-diagnostics").RenderRows(GameGameGame.SadConsoleApp.Ui.Styling.SadConsoleTheme.Default);

            Assert.Contains(diagnostics, row => row.Contains("Missing ScenarioRootTemplate: broken -> missingRoom"));
        }
        finally
        {
            File.Delete(path);
        }
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

    private static string WriteTempContentFile(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ggg-sadconsole-scenario-edit-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(path, yaml);
        return path;
    }

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
