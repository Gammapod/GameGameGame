using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class EditableContentDocumentTests
{
    [Fact]
    public void EditableContentDocumentCanLoadMaterializeSaveAndReloadYaml()
    {
        var document = EditableContentDocument.LoadYaml(
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
            actionPlans:
              wait:
                id: wait
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """);

        var registry = document.ToRegistry();
        Assert.True(registry.Validate().IsValid);
        Assert.Equal("Rock", registry.EntityTemplates[new EntityTemplateId("rock")].Name);

        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();

        Assert.True(reloaded.Validate().IsValid);
        Assert.Equal("Rock", reloaded.EntityTemplates[new EntityTemplateId("rock")].Name);
        Assert.Equal('*', reloaded.Presentations[new EntityTemplateId("rock")].Glyph);
        Assert.Equal(PlanEffectKind.Wait, reloaded.ActionPlanDescriptors[new ActionPlanTemplateId("wait")].Steps.Single().OnSuccess!.Kind);
    }

    [Fact]
    public void EditableContentDocumentCanCreateEntityTemplateWithGeneratedStableId()
    {
        var document = EditableContentDocument.LoadYaml(
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
            """);

        var id = document.AddEntityTemplate(
            "Giant Slime",
            new EntityTemplate(
                "Giant Slime",
                InventoryWidth: 3,
                InventoryHeight: 3,
                Weight: 20,
                CarryingCapacity: 20),
            new EntityPresentation('S', PresentationColor.DarkGreen));

        var registry = EditableContentDocument.LoadYaml(document.SaveYaml()).ToRegistry();

        Assert.Equal(new EntityTemplateId("giantSlime"), id);
        Assert.Equal("Giant Slime", registry.EntityTemplates[id].Name);
        Assert.Equal('S', registry.Presentations[id].Glyph);
    }

    [Fact]
    public void EditableContentDocumentCanonicalizesLegacyActionPlanVariableFieldsOnSave()
    {
        var document = EditableContentDocument.LoadYaml(
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
                  - label: move facing
                    checks:
                      - kind: CanMove
                        directionVariable: facing
                    onSuccess:
                      kind: Move
                      directionVariable: facing
                  - label: handle blocker
                    checks:
                      - kind: BlockingEntity
                        directionVariable: facing
                        targetVariable: target
                    onSuccess:
                      kind: Pickup
                      targetVariable: target
                      inventoryCoord:
                        x: 0
                        y: 0
                    onFailure:
                      kind: ReverseDirection
                      directionVariable: facing
                      consumesTurn: false
                      continuePlan: true
            """);

        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();
        var descriptor = reloaded.GetActionPlanDescriptor(new ActionPlanTemplateId("wandering"));

        Assert.DoesNotContain("directionVariable", saved);
        Assert.DoesNotContain("targetVariable", saved);
        Assert.True(reloaded.Validate().IsValid);
        Assert.Null(descriptor.Steps[0].Checks.Single().DirectionVariable);
        Assert.Null(descriptor.Steps[0].OnSuccess!.DirectionVariable);
        Assert.Null(descriptor.Steps[1].Checks.Single().DirectionVariable);
        Assert.Null(descriptor.Steps[1].Checks.Single().TargetVariable);
        Assert.Null(descriptor.Steps[1].OnSuccess!.TargetVariable);
        Assert.Null(descriptor.Steps[1].OnFailure!.DirectionVariable);
    }

    [Fact]
    public void EditableContentDocumentCanonicalizesLegacyFacingDefaultOnSave()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultPlanVariables:
                  facing:
                    kind: Direction
                    directionValue: West
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans: {}
            """);

        var saved = document.SaveYaml();
        var reloaded = EditableContentDocument.LoadYaml(saved).ToRegistry();
        var template = reloaded.GetEntityTemplate(new EntityTemplateId("slime"));

        Assert.DoesNotContain("defaultPlanVariables", saved);
        Assert.Contains("actionStateDefaults", saved);
        Assert.Contains("facing: West", saved);
        Assert.Equal(Direction.West, template.ActionStateDefaults!.Facing);
        Assert.Null(template.DefaultPlanVariables);
    }

    [Fact]
    public void EditableContentDocumentCanonicalAuthoringValidationReportsArbitraryVariableFields()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              wandering:
                id: wandering
                steps:
                  - label: move turn direction
                    checks:
                      - kind: CanMove
                        directionVariable: turnDirection
                    onSuccess:
                      kind: Move
                      directionVariable: turnDirection
            """);

        var result = document.ValidateCanonicalAuthoring();

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == ContentDiagnosticCode.ArbitraryPlanVariableField
            && diagnostic.ActionPlanTemplateId == new ActionPlanTemplateId("wandering")
            && diagnostic.StepIndex == 0
            && diagnostic.VariableName == "turnDirection");
    }

    [Fact]
    public void EditableContentDocumentCanonicalAuthoringValidationReportsDefaultPlanVariables()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              slime:
                name: Slime
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 3
                carryingCapacity: 20
                defaultPlanVariables:
                  mood:
                    kind: Int
                    intValue: 1
            presentations:
              slime:
                glyph: s
                color: Green
            actionPlans: {}
            """);

        var result = document.ValidateCanonicalAuthoring();

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.ArbitraryPlanVariableField);
        Assert.Equal(new EntityTemplateId("slime"), diagnostic.EntityTemplateId);
        Assert.Equal("mood", diagnostic.VariableName);
    }
}
