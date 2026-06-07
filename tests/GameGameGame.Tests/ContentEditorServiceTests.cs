using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class ContentEditorServiceTests
{
    [Fact]
    public void ContentEditorServiceListsJoinedEntityPresets()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
            presentations:
              rock:
                glyph: '*'
                color: Earth
            actionPlans: {}
            """));

        var preset = Assert.Single(editor.ListEntityPresets());

        Assert.Equal(new EntityTemplateId("rock"), preset.Id);
        Assert.Equal("Rock", preset.Template.Name);
        Assert.Equal('*', preset.Presentation.Glyph);
        Assert.Equal(PresentationColor.Earth, preset.Presentation.Color);
    }

    [Fact]
    public void ContentEditorServiceUpdatesEntityPresetAndPresentation()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
            presentations:
              rock:
                glyph: '*'
                color: Earth
            actionPlans: {}
            """));
        var id = new EntityTemplateId("rock");

        editor.UpdateEntityPreset(
            id,
            editor.GetEntityPreset(id).Template with
            {
                Name = "Heavy Rock",
                Weight = 5
            },
            new EntityPresentation('R', PresentationColor.Gray));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.Equal("Heavy Rock", registry.EntityTemplates[id].Name);
        Assert.Equal(5, registry.EntityTemplates[id].Weight);
        Assert.Equal('R', registry.Presentations[id].Glyph);
        Assert.Equal(PresentationColor.Gray, registry.Presentations[id].Color);
    }

    [Fact]
    public void ContentEditorServicePlacesAndMovesCarriedEntityInInventoryLayout()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              bag:
                name: Bag
                inventoryWidth: 2
                inventoryHeight: 1
                weight: 1
                carryingCapacity: 10
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
            presentations:
              bag:
                glyph: b
                color: Gray
              rock:
                glyph: '*'
                color: Earth
            actionPlans: {}
            """));
        var bagId = new EntityTemplateId("bag");
        var carriedId = new EntityId("carriedRock");

        editor.PlaceCarriedEntity(bagId, carriedId, new EntityTemplateId("rock"), new GridCoord(0, 0));
        editor.MoveCarriedEntity(bagId, carriedId, new GridCoord(1, 0));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.True(registry.Validate().IsValid);
        var carried = Assert.Single(registry.EntityTemplates[bagId].CarriedEntities!);
        Assert.Equal(carriedId, carried.EntityId);
        Assert.Equal(new EntityTemplateId("rock"), carried.TemplateId);
        Assert.Equal(new GridCoord(1, 0), carried.Coord);
    }

    [Fact]
    public void ContentEditorServiceListsActionPlans()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              wait:
                id: wait
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """));

        var plan = Assert.Single(editor.ListActionPlans());

        Assert.Equal(new ActionPlanTemplateId("wait"), plan.TemplateId);
        Assert.Equal(new ActionPlanId("wait"), plan.Descriptor.Id);
        Assert.Equal("wait", Assert.Single(plan.Descriptor.Steps).Label);
    }

    [Fact]
    public void ContentEditorServiceAddsReordersAndRemovesActionPlanSteps()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              simple:
                id: simple
                steps: []
            """));
        var planId = new ActionPlanTemplateId("simple");

        editor.AddActionPlanStep(planId, new ActionPlanStepDescriptor("first", [], PlanEffectDescriptor.Wait(), OnFailure: null));
        editor.AddActionPlanStep(planId, new ActionPlanStepDescriptor("second", [], PlanEffectDescriptor.Wait(), OnFailure: null));
        editor.MoveActionPlanStep(planId, fromIndex: 1, toIndex: 0);
        editor.RemoveActionPlanStep(planId, index: 1);
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        var step = Assert.Single(registry.ActionPlanDescriptors[planId].Steps);
        Assert.Equal("second", step.Label);
    }

    [Fact]
    public void ContentEditorServiceSetsActionPlanStepChecksAndEffects()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: wandering
                defaultPlanVariables:
                  facing:
                    kind: Direction
                    directionValue: West
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              wandering:
                id: wandering
                steps:
                  - label: move
                    checks: []
                    onSuccess:
                      kind: Wait
            """));
        var planId = new ActionPlanTemplateId("wandering");

        editor.UpdateActionPlanStep(
            planId,
            index: 0,
            new ActionPlanStepDescriptor(
                "move facing",
                [PlanCheckDescriptor.CanMove("facing")],
                PlanEffectDescriptor.Move("facing"),
                OnFailure: null));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.True(registry.Validate().IsValid);
        var step = Assert.Single(registry.ActionPlanDescriptors[planId].Steps);
        Assert.Equal(PlanCheckKind.CanMove, Assert.Single(step.Checks).Kind);
        Assert.Equal(PlanEffectKind.Move, step.OnSuccess!.Kind);
    }

    [Fact]
    public void ContentEditorServiceEditsTemplateDefaultPlanVariables()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultActionPlanId: wandering
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans:
              wandering:
                id: wandering
                steps:
                  - label: move facing
                    checks:
                      - kind: CanMove
                        directionVariable: facing
                    onSuccess:
                      kind: Move
                      directionVariable: facing
            """));
        var slimeId = new EntityTemplateId("slime");

        editor.SetDefaultPlanVariable(slimeId, "facing", PlanValueDescriptor.Direction(Direction.East));
        var registry = EditableContentDocument.LoadYaml(editor.Document.SaveYaml()).ToRegistry();

        Assert.True(registry.Validate().IsValid);
        Assert.Equal(Direction.East, registry.EntityTemplates[slimeId].DefaultPlanVariables!["facing"].DirectionValue);
    }

    [Fact]
    public void ContentEditorServiceValidatesCurrentDocumentAfterEdits()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
            presentations:
              rock:
                glyph: '*'
                color: Earth
            actionPlans: {}
            """));
        var id = new EntityTemplateId("rock");

        editor.UpdateEntityPreset(
            id,
            editor.GetEntityPreset(id).Template with { Name = "Edited Rock" },
            new EntityPresentation('R', PresentationColor.White));
        var result = editor.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ContentEditorServiceValidationReportsCurrentDocumentErrors()
    {
        var editor = new ContentEditorService(EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              bag:
                name: Bag
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 1
                carryingCapacity: 10
              rock:
                name: Rock
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 3
                carryingCapacity: 3
            presentations:
              bag:
                glyph: b
                color: Gray
              rock:
                glyph: '*'
                color: Earth
            actionPlans: {}
            """));

        editor.PlaceCarriedEntity(new EntityTemplateId("bag"), new EntityId("outsideRock"), new EntityTemplateId("rock"), new GridCoord(2, 0));
        var result = editor.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("outsideRock") && error.Contains("outside inventory bounds"));
    }
}
