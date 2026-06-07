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
}
