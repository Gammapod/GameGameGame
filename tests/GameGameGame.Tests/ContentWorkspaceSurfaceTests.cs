using GameGameGame.Content;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class ContentWorkspaceSurfaceTests
{
    [Fact]
    public void ContentWorkspaceSurfaceGroupsContentByType()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            actionPlans:
              wait:
                id: wait
                behavior:
                  steps:
                    - kind: MoveFacing
            presentations:
              room: { glyph: '#', color: Gray }
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 2
                inventoryHeight: 2
                weight: 100
                carryingCapacity: 100
                defaultActionPlanId: wait
            scenarios:
              smoke:
                name: Smoke
                scenarioRootEntityTemplateId: room
            mergedLayers:
              smokeLayer:
                spaces:
                  - owner: actor1
                    origin: { x: 0, y: 0 }
            """);

        var surface = ContentWorkspaceSurfaceService.Build(document, new ContentCompileOptions("doc-1", "content.yaml"));

        Assert.Equal("doc-1", surface.Source.DocumentId);
        Assert.Equal("content.yaml", surface.Source.SourcePath);
        Assert.Contains(surface.EntityTemplates, item => item.Id == "room" && item.DisplayName == "Room");
        Assert.Contains(surface.ActionPlans, item => item.Id == "wait");
        Assert.Contains(surface.Scenarios, item => item.Id == "smoke" && item.DisplayName == "Smoke");
        Assert.Contains(surface.Presentations, item => item.Id == "room");
        Assert.Contains(surface.MergedLayers, item => item.Id == "smokeLayer");
    }

    [Fact]
    public void ContentWorkspaceSurfaceCarriesCompilerDiagnosticsAndSource()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              actor:
                name: Actor
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 1
                carryingCapacity: 1
                defaultActionPlanId: missingPlan
            presentations:
              actor: { glyph: a, color: Green }
            actionPlans: {}
            """);

        var surface = ContentWorkspaceSurfaceService.Build(document, new ContentCompileOptions("doc-2", "broken.yaml"));

        var diagnostic = Assert.Single(surface.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingActionPlanReference);
        Assert.Equal("doc-2", diagnostic.DocumentId);
        Assert.Equal("broken.yaml", diagnostic.SourcePath);
        Assert.False(surface.IsValid);
    }

    [Fact]
    public void ContentWorkspaceSurfaceProjectsMissingReferences()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              actor:
                name: Actor
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 1
                carryingCapacity: 1
                defaultActionPlanId: missingPlan
            presentations:
              actor: { glyph: a, color: Green }
            actionPlans: {}
            scenarios:
              smoke:
                name: Smoke
                scenarioRootEntityTemplateId: missingRoom
            """);

        var surface = ContentWorkspaceSurfaceService.Build(document, new ContentCompileOptions("doc-3", "refs.yaml"));

        Assert.Contains(surface.MissingReferences, reference =>
            reference.Kind == ContentReferenceKind.DefaultActionPlan
            && reference.SourceId == "actor"
            && reference.TargetId == "missingPlan"
            && reference.DocumentId == "doc-3"
            && reference.SourcePath == "refs.yaml");
        Assert.Contains(surface.MissingReferences, reference =>
            reference.Kind == ContentReferenceKind.ScenarioRootTemplate
            && reference.SourceId == "smoke"
            && reference.TargetId == "missingRoom");
    }
}
