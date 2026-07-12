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
    public void EntityTemplateEditActionPlanPickerCanOpenCurrentActionPlanForEdit()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        var openPicker = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Opened editor for presentation action plan", openPicker.Message);
        Assert.Equal("presentation-action-plan-editor", screen.OverlayComponent()?.Id);

        screen.Handle(UiComponentCommand.Down);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Equal(EntityTemplateEditResultKind.OpenActionPlan, result.Kind);
        Assert.Equal("wander", result.ActionPlanId);
    }

    [Fact]
    public void EntityTemplateEditActionPlanPickerAssignsDefaultActionPlanThroughEditorService()
    {
        var service = FrontendEditorService.CreateNew();
        var create = service.CreateEntityTemplate("Mouse");
        var templateId = create.Snapshot.EntityTemplates.Single().TemplateId;
        var plan = service.CreatePassiveActionPlan("Patrol Plan");
        var planId = plan.Snapshot.ActionPlans.Single().ActionPlanId;
        var screen = EntityTemplateEditScreen.FromSnapshot(service.GetSnapshot(), templateId, service);

        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Assigned default action plan", result.Message);
        Assert.Null(screen.OverlayComponent());
        Assert.Equal(planId, service.GetSnapshot().EntityTemplates.Single().DefaultActionPlanId);
        Assert.Contains(screen.Components().SelectMany(component => component.RenderRows(SadConsoleTheme.Default)), row => row.Contains(planId));
    }

    [Fact]
    public void EntityTemplateEditActionPlanPickerCanClearDefaultActionPlan()
    {
        var service = FrontendEditorService.CreateNew();
        var create = service.CreateEntityTemplate("Mouse");
        var templateId = create.Snapshot.EntityTemplates.Single().TemplateId;
        var plan = service.CreatePassiveActionPlan("Patrol Plan");
        var planId = plan.Snapshot.ActionPlans.Single().ActionPlanId;
        service.SetTemplateDefaultActionPlan(templateId, planId);
        var screen = EntityTemplateEditScreen.FromSnapshot(service.GetSnapshot(), templateId, service);

        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Up);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Cleared default action plan", result.Message);
        Assert.Null(service.GetSnapshot().EntityTemplates.Single().DefaultActionPlanId);
    }

    [Fact]
    public void EntityTemplateEditPresentationNameOpensTextEntryOverlay()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        screen.Handle(UiComponentCommand.Select);
        var open = screen.Handle(UiComponentCommand.Select);
        var overlay = screen.OverlayComponent();

        Assert.Contains("Opened editor for presentation name", open.Message);
        Assert.True(screen.IsTextEntryOverlayActive);
        Assert.NotNull(overlay);
        Assert.Equal("presentation-name-editor", overlay.Id);
        Assert.Equal(7, overlay.Bounds.Height);
        Assert.Contains(overlay.RenderRows(SadConsoleTheme.Default), row => row.Contains("Enter the text for name:"));
    }

    [Fact]
    public void EntityTemplateEditPresentationGlyphTextOverlayHasPositiveHeight()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        var open = screen.Handle(UiComponentCommand.Select);
        var overlay = screen.OverlayComponent();

        Assert.Contains("Opened editor for presentation glyph", open.Message);
        Assert.NotNull(overlay);
        Assert.Equal("presentation-glyph-editor", overlay.Id);
        Assert.Equal(7, overlay.Bounds.Height);
    }

    [Fact]
    public void EntityTemplateEditPresentationColorOverlayFitsAllColorChoices()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        var open = screen.Handle(UiComponentCommand.Select);
        var overlay = screen.OverlayComponent();

        Assert.Contains("Opened editor for presentation color", open.Message);
        Assert.NotNull(overlay);
        Assert.Equal("presentation-color-editor", overlay.Id);
        Assert.Equal(12, overlay.Bounds.Height);
        Assert.True(overlay.RenderRows(SadConsoleTheme.Default).Count <= overlay.Bounds.Height - 2);
        Assert.Contains(overlay.RenderRows(SadConsoleTheme.Default), row => row.Contains("■ Earth"));
    }

    [Fact]
    public void EntityTemplateEditPresentationNameConfirmMutatesThroughEditorService()
    {
        var service = FrontendEditorService.CreateNew();
        var create = service.CreateEntityTemplate("Rock");
        var templateId = create.Snapshot.EntityTemplates.Single().TemplateId;
        var screen = EntityTemplateEditScreen.FromSnapshot(create.Snapshot, templateId, service);

        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Select);
        screen.InsertText("y");
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Updated template", result.Message);
        Assert.Null(screen.OverlayComponent());
        Assert.Contains(screen.Components().SelectMany(component => component.RenderRows(SadConsoleTheme.Default)), row => row.Contains("Rocky"));
        Assert.Equal("Rocky", service.GetSnapshot().EntityTemplates.Single().Name);
    }

    [Fact]
    public void EntityTemplateEditTargetingListUsesActionPlanRequirementsAndOpensDetailOverlay()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        Assert.Equal(1, screen.SelectedTargetingSlotIndex);

        var rows = screen.Components().SelectMany(component => component.RenderRows(SadConsoleTheme.Default)).ToList();
        Assert.Contains(rows, row => row.Contains("primary: Rock range 3"));
        Assert.Contains(rows, row => row.Contains("secondary: Slime range 4"));
        Assert.DoesNotContain(rows, row => row.Contains("slot 1"));

        var open = screen.Handle(UiComponentCommand.Select);
        var overlay = screen.OverlayComponent();

        Assert.Contains("Opened 3.2.1", open.Message);
        Assert.NotNull(overlay);
        Assert.Equal("targeting-slot-detail", overlay.Id);
        var overlayRows = overlay.RenderRows(SadConsoleTheme.Default);
        Assert.Contains(overlayRows, row => row.Contains("target label"));
        Assert.Contains(overlayRows, row => row.Contains("target template"));
        Assert.Contains(overlayRows, row => row.Contains("target range"));

        var close = screen.Handle(UiComponentCommand.Cancel);
        Assert.Contains("Closed targeting slot detail", close.Message);
        Assert.Null(screen.OverlayComponent());
    }

    [Fact]
    public void EntityTemplateEditTargetingWithoutRequirementsAsksForActionPlan()
    {
        var snapshot = DemoSnapshot() with
        {
            EntityTemplates = DemoSnapshot().EntityTemplates.Select(template => template.TemplateId == "player"
                ? template with
                {
                    DefaultActionPlanId = null,
                    TargetingRequirements = [],
                    OrphanedTargetingRules = []
                }
                : template).ToList()
        };
        var screen = EntityTemplateEditScreen.FromSnapshot(snapshot, "player");

        screen.Handle(UiComponentCommand.Down);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("inventory", result.Message);
        Assert.Null(screen.OverlayComponent());
        Assert.Equal(UiComponentState.Disabled, screen.Components().Single(component => component.Id == "targeting").State);
        Assert.Contains(screen.Components().SelectMany(component => component.RenderRows(SadConsoleTheme.Default)), row => row.Contains("Choose an Action Plan"));
    }

    [Fact]
    public void EntityTemplateEditTargetingDetailCanOpenTemplatePickerAndRangeSetter()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Select);
        var templateOpen = screen.Handle(UiComponentCommand.Select);
        var templateOverlay = screen.OverlayComponent();

        Assert.Contains("target-template picker", templateOpen.Message);
        Assert.NotNull(templateOverlay);
        Assert.Equal("target-template-editor", templateOverlay.Id);
        Assert.Contains(templateOverlay.RenderRows(SadConsoleTheme.Default), row => row.Contains("Rock"));

        screen.Handle(UiComponentCommand.Cancel);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        var rangeOpen = screen.Handle(UiComponentCommand.Select);
        var rangeOverlay = screen.OverlayComponent();

        Assert.Contains("target-range editor", rangeOpen.Message);
        Assert.NotNull(rangeOverlay);
        Assert.Equal("target-range-editor", rangeOverlay.Id);
        Assert.Contains(rangeOverlay.RenderRows(SadConsoleTheme.Default), row => row.Contains("between 0 and 10"));
    }

    [Fact]
    public void EntityTemplateEditTargetingAdjectivesDeriveFromCurrentActionPlanAndToggleThroughService()
    {
        var service = FrontendEditorService.CreateNew();
        var createActor = service.CreateEntityTemplate("Thief");
        var actorId = createActor.Snapshot.EntityTemplates.Single().TemplateId;
        var plan = service.CreatePassiveActionPlan("Thief Action Plan");
        var planId = plan.Snapshot.ActionPlans.Single().ActionPlanId;
        service.InsertActionPlanStep(planId, 0, ActionPlanBehaviorStepKind.PickupTarget);
        service.SetActionPlanStepTargetLabel(planId, 0, "wants");
        service.SetTemplateDefaultActionPlan(actorId, planId);
        var screen = EntityTemplateEditScreen.FromSnapshot(service.GetSnapshot(), actorId, service);

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        var open = screen.Handle(UiComponentCommand.Select);
        var overlay = screen.OverlayComponent();

        Assert.Contains("target-adjectives picker", open.Message);
        Assert.NotNull(overlay);
        Assert.Equal("target-adjectives-editor", overlay.Id);
        Assert.Contains(overlay.RenderRows(SadConsoleTheme.Default), row => row.Contains("PickupTarget"));

        var toggle = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Updated targeting rule", toggle.Message);
        var rule = service.GetSnapshot().EntityTemplates.Single(template => template.TemplateId == actorId).TargetingRules.Single();
        Assert.Equal("wants", rule.Label);
        Assert.Null(rule.TargetTemplateId);
        Assert.Equal([ActionPlanBehaviorStepKind.PickupTarget], rule.TargetCapabilities);
    }

    [Fact]
    public void EntityTemplateEditTargetingTemplateConfirmMutatesThroughEditorService()
    {
        var service = FrontendEditorService.CreateNew();
        var createActor = service.CreateEntityTemplate("Thief");
        var actorId = createActor.Snapshot.EntityTemplates.Single().TemplateId;
        service.CreateEntityTemplate("Treasure");
        var plan = service.CreatePassiveActionPlan("Thief Action Plan");
        var planId = plan.Snapshot.ActionPlans.Single().ActionPlanId;
        service.InsertActionPlanStep(planId, 0, ActionPlanBehaviorStepKind.SeekTarget);
        service.Session.Editor.SetActionPlanBehaviorStepTargetLabel(new ActionPlanTemplateId(planId), 0, "loves");
        service.SetTemplateDefaultActionPlan(actorId, planId);
        var snapshot = service.GetSnapshot();
        var screen = EntityTemplateEditScreen.FromSnapshot(snapshot, actorId, service);

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Updated targeting rule", result.Message);
        var rule = service.GetSnapshot().EntityTemplates.Single(template => template.TemplateId == actorId).TargetingRules.Single();
        Assert.Equal("loves", rule.Label);
        Assert.Equal("treasure", rule.TargetTemplateId);
    }

    [Fact]
    public void EntityTemplateEditInventoryRendersMetadataGridEditorEntryAndReadOnlyPreviewData()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);

        var rows = screen.Components().SelectMany(component => component.RenderRows(SadConsoleTheme.Default)).ToList();
        Assert.Contains(rows, row => row.Contains("inventory width: 5"));
        Assert.Contains(rows, row => row.Contains("inventory height: 4"));
        Assert.Contains(rows, row => row.Contains("3.3.2 inventory grid editor"));
        Assert.DoesNotContain(rows, row => row.Contains("brush selection"));
        Assert.DoesNotContain(rows, row => row.Contains("carried entries"));
        Assert.DoesNotContain(rows, row => row.Contains("inventory drawing opens"));

        var inventory = Assert.IsType<InventorySummaryComponent>(screen.Components().Single(component => component.Id == "inventory"));
        Assert.Equal(5, inventory.GridWidth);
        Assert.Equal(4, inventory.GridHeight);
        Assert.Contains(inventory.Cells, cell => cell.Coord == new GridCoord(1, 2) && cell.Glyph == '*');
    }

    [Fact]
    public void EntityTemplateEditInventoryFocusSelectsMetadataFieldsAndOpensIntOverlay()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        var open = screen.Handle(UiComponentCommand.Select);
        var overlay = screen.OverlayComponent();

        Assert.Equal(1, screen.SelectedInventoryMetadataFieldIndex);
        Assert.Contains("Opened editor for inventory inventory height", open.Message);
        Assert.NotNull(overlay);
        Assert.Equal("inventory-height-editor", overlay.Id);
        Assert.Contains(overlay.RenderRows(SadConsoleTheme.Default), row => row.Contains("Set inventory height between 0 and 99:"));
    }

    [Fact]
    public void EntityTemplateEditInventoryMetadataConfirmMutatesThroughEditorService()
    {
        var service = FrontendEditorService.CreateNew();
        var create = service.CreateEntityTemplate("Box");
        var templateId = create.Snapshot.EntityTemplates.Single().TemplateId;
        var originalWidth = create.Snapshot.EntityTemplates.Single().InventoryWidth;
        var screen = EntityTemplateEditScreen.FromSnapshot(create.Snapshot, templateId, service);

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Right);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Updated metadata", result.Message);
        Assert.Null(screen.OverlayComponent());
        Assert.Equal(originalWidth + 1, service.GetSnapshot().EntityTemplates.Single().InventoryWidth);
        Assert.Contains(screen.Components().SelectMany(component => component.RenderRows(SadConsoleTheme.Default)), row => row.Contains($"inventory width: {originalWidth + 1}"));
    }

    [Fact]
    public void EntityTemplateEditInventoryGridEntryOpensDedicatedGridScreen()
    {
        var screen = EntityTemplateEditScreen.FromSnapshot(DemoSnapshot(), "player");

        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Select);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        screen.Handle(UiComponentCommand.Down);
        var result = screen.Handle(UiComponentCommand.Select);

        Assert.Equal(EntityTemplateEditResultKind.OpenInventoryGrid, result.Kind);
    }

    [Fact]
    public void InventoryGridEditPlacesBrushAtCursorWithEnterAndOverwritesOccupant()
    {
        var service = InventoryGridService();
        var screen = InventoryGridEditScreen.FromSnapshot(service.GetSnapshot(), "box", service);

        screen.Handle(UiComponentCommand.Right);
        var place = screen.Handle(UiComponentCommand.Select);

        Assert.Contains("Overwrote", place.Message);
        var carried = service.GetSnapshot().EntityTemplates.Single(template => template.TemplateId == "box").CarriedEntities.Single(entity => entity.Coord == new GridCoord(1, 0));
        Assert.Equal("coin", carried.TemplateId);
    }

    [Fact]
    public void InventoryGridEditDeleteRemovesEntityAtCursor()
    {
        var service = InventoryGridService();
        var screen = InventoryGridEditScreen.FromSnapshot(service.GetSnapshot(), "box", service);

        var result = screen.Handle(InventoryGridEditCommand.Delete);

        Assert.Contains("Removed carried entity", result.Message);
        Assert.DoesNotContain(service.GetSnapshot().EntityTemplates.Single(template => template.TemplateId == "box").CarriedEntities, entity => entity.Coord == new GridCoord(0, 0));
    }

    [Fact]
    public void InventoryGridEditSpaceMovesEntityAndCanCancelMove()
    {
        var service = InventoryGridService();
        var screen = InventoryGridEditScreen.FromSnapshot(service.GetSnapshot(), "box", service);

        var pickUp = screen.Handle(InventoryGridEditCommand.Move);
        screen.Handle(UiComponentCommand.Right);
        var cancel = screen.Handle(UiComponentCommand.Cancel);

        Assert.Contains("picked up", pickUp.Message);
        Assert.Contains("Cancelled move", cancel.Message);
        Assert.Null(screen.MovingEntityId);
        Assert.Contains(service.GetSnapshot().EntityTemplates.Single(template => template.TemplateId == "box").CarriedEntities, entity => entity.Coord == new GridCoord(0, 0));
    }

    [Fact]
    public void InventoryGridEditSpacePlacesMovingEntityAndSilentlyReplacesDestination()
    {
        var service = InventoryGridService();
        var screen = InventoryGridEditScreen.FromSnapshot(service.GetSnapshot(), "box", service);

        screen.Handle(InventoryGridEditCommand.Move);
        screen.Handle(UiComponentCommand.Right);
        var result = screen.Handle(InventoryGridEditCommand.Move);

        Assert.Contains("Moved carried entity", result.Message);
        var carried = service.GetSnapshot().EntityTemplates.Single(template => template.TemplateId == "box").CarriedEntities;
        Assert.Single(carried, entity => entity.Coord == new GridCoord(1, 0));
        Assert.DoesNotContain(carried, entity => entity.Coord == new GridCoord(0, 0));
    }

    [Fact]
    public void InventoryGridEditCopySetsBrushFromCursorAndTabOpensBrushPicker()
    {
        var service = InventoryGridService();
        var screen = InventoryGridEditScreen.FromSnapshot(service.GetSnapshot(), "box", service);

        var copy = screen.Handle(InventoryGridEditCommand.Copy);
        var openPicker = screen.Handle(InventoryGridEditCommand.OpenBrushPicker);

        Assert.Contains("Copied brush", copy.Message);
        Assert.Equal("rock", screen.BrushTemplateId);
        Assert.Contains("Opened inventory brush picker", openPicker.Message);
        Assert.Equal("inventory-brush-picker", screen.OverlayComponent()?.Id);
    }

    [Fact]
    public void InventoryGridEditAlwaysShowsCurrentCellInspectionPanel()
    {
        var service = InventoryGridService();
        var screen = InventoryGridEditScreen.FromSnapshot(service.GetSnapshot(), "box", service);

        var rows = screen.Components().Single(component => component.Id == "inventory-grid-inspection").RenderRows(SadConsoleTheme.Default);

        Assert.Contains(rows, row => row.Contains("cell: 0,0"));
        Assert.Contains(rows, row => row.Contains("template: Rock"));

        screen.Handle(UiComponentCommand.Right);
        rows = screen.Components().Single(component => component.Id == "inventory-grid-inspection").RenderRows(SadConsoleTheme.Default);

        Assert.Contains(rows, row => row.Contains("cell: 1,0"));
        Assert.Contains(rows, row => row.Contains("template: Coin"));
    }

    private static FrontendEditorSnapshot DemoSnapshot() => new(
        "demo.yaml",
        false,
        [new FrontendEditorScenarioSummary("demo", "Demo Scenario", "root", "player", "player-1", new GridCoord(2, 3))],
        [DemoTemplate(), DemoTemplate("rock", "Rock", '*', PresentationColor.Gray), DemoTemplate("slime", "Slime", 's', PresentationColor.Green)],
        [new FrontendEditorActionPlanSummary("wander", "canonical", [new FrontendEditorActionPlanStepSummary(0, ActionPlanBehaviorStepKind.SeekTarget, "Seek Target")], ["Seek Target"])
        {
            TargetLabelRequirements =
            [
                new FrontendEditorActionPlanTargetLabelRequirementSummary("primary", [0], [ActionPlanBehaviorStepKind.SeekTarget]),
                new FrontendEditorActionPlanTargetLabelRequirementSummary("secondary", [1], [ActionPlanBehaviorStepKind.FleeTarget])
            ]
        }],
        [],
        [],
        "yaml",
        []);

    private static FrontendEditorEntityTemplateSummary DemoTemplate()
    {
        var primary = new FrontendEditorTargetingRuleSummary(0, "primary", null, "rock", "Rock", 3);
        var secondary = new FrontendEditorTargetingRuleSummary(1, "secondary", null, "slime", "Slime", 4);
        return new FrontendEditorEntityTemplateSummary(
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
            [primary, secondary],
            [
                new FrontendEditorCarriedEntitySummary("rock-1", "rock", "Rock", '*', PresentationColor.Gray, new GridCoord(1, 2), []),
                new FrontendEditorCarriedEntitySummary("slime-1", "slime", "Slime", 's', PresentationColor.Green, new GridCoord(2, 2), [])
            ],
            [])
        {
            TargetingRequirements =
            [
                new FrontendEditorTargetingRequirementSummary("primary", [0], [ActionPlanBehaviorStepKind.SeekTarget], true, primary),
                new FrontendEditorTargetingRequirementSummary("secondary", [1], [ActionPlanBehaviorStepKind.FleeTarget], true, secondary)
            ],
            OrphanedTargetingRules = []
        };
    }

    private static FrontendEditorEntityTemplateSummary DemoTemplate(string id, string name, char glyph, PresentationColor color) => new(
        id,
        name,
        glyph,
        color,
        0,
        0,
        1,
        1,
        null,
        new FrontendEditorActionStateDefaultsSummary(null, null),
        [],
        [],
        []);

    private static FrontendEditorService InventoryGridService()
    {
        var service = FrontendEditorService.CreateNew();
        service.CreateEntityTemplate("Box");
        service.CreateEntityTemplate("Rock");
        service.CreateEntityTemplate("Coin");
        service.UpdateTemplateMetadata("box", new FrontendEditorTemplateMetadataUpdate(3, 2, 9, 1));
        service.OverwriteTemplateInInventory("box", "rock", new GridCoord(0, 0));
        service.OverwriteTemplateInInventory("box", "coin", new GridCoord(1, 0));
        return service;
    }
}
