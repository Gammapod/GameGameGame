using GameGameGame.Content;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class ContentScenarioSurfaceTests
{
    [Fact]
    public void ContentScenarioSurfaceShowsSelectedScenarioAndSharedContentByType()
    {
        var document = LoadScenarioDocument();

        var surface = ContentScenarioSurfaceService.Build(document, "smoke", new ContentCompileOptions("doc-1", "scenario.yaml"));

        Assert.Equal("smoke", surface.SelectedScenario.Id);
        Assert.Equal("Smoke", surface.SelectedScenario.DisplayName);
        Assert.Equal("room", surface.RootTemplateId);
        Assert.Equal("actor", surface.PlayerTemplateId);
        Assert.Equal(new ContentSurfaceGridCoord(0, 0), surface.PlayerStart);
        Assert.Contains(surface.Workspace.EntityTemplates, item => item.Id == "room");
        Assert.Contains(surface.Workspace.EntityTemplates, item => item.Id == "unused");
        Assert.Contains(surface.Workspace.ActionPlans, item => item.Id == "idle");
    }

    [Fact]
    public void ContentScenarioSurfaceGroupsSelectedScenarioDiagnostics()
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
              bad:
                name: Bad
                scenarioRootEntityTemplateId: missingRoom
              other:
                name: Other
                scenarioRootEntityTemplateId: actor
            """);

        var surface = ContentScenarioSurfaceService.Build(document, "bad", new ContentCompileOptions("doc-2", "bad.yaml"));

        Assert.Contains(surface.SelectedScenarioReferences, reference =>
            reference.Kind == ContentReferenceKind.ScenarioRootTemplate
            && reference.SourceId == "bad"
            && reference.TargetId == "missingRoom"
            && reference.Resolution == ContentReferenceResolution.Missing);
        Assert.Contains(surface.SelectedScenarioDiagnostics, diagnostic =>
            diagnostic.Code == ContentDiagnosticCode.InvalidScenarioDefinition
            && diagnostic.Message.Contains("bad", StringComparison.Ordinal));
        Assert.DoesNotContain(surface.SelectedScenarioDiagnostics, diagnostic =>
            diagnostic.Code == ContentDiagnosticCode.MissingActionPlanReference);
        Assert.Contains(surface.GlobalDiagnostics, diagnostic =>
            diagnostic.Code == ContentDiagnosticCode.MissingActionPlanReference);
    }

    [Fact]
    public void ContentScenarioSurfaceCarriesDependencyClosureWithoutFilteringSharedContent()
    {
        var document = LoadScenarioDocument();

        var surface = ContentScenarioSurfaceService.Build(document, "smoke", new ContentCompileOptions("doc-3", "deps.yaml"));

        Assert.Contains(surface.DependencySymbols, symbol => symbol.Kind == ContentSymbolKind.EntityTemplate && symbol.Id == "room");
        Assert.Contains(surface.DependencySymbols, symbol => symbol.Kind == ContentSymbolKind.EntityTemplate && symbol.Id == "actor");
        Assert.Contains(surface.DependencySymbols, symbol => symbol.Kind == ContentSymbolKind.ActionPlan && symbol.Id == "idle");
        Assert.DoesNotContain(surface.DependencySymbols, symbol => symbol.Kind == ContentSymbolKind.EntityTemplate && symbol.Id == "unused");
        Assert.Contains(surface.Workspace.EntityTemplates, item => item.Id == "unused");
    }

    private static EditableContentDocument LoadScenarioDocument() =>
        EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 2
                inventoryHeight: 2
                weight: 100
                carryingCapacity: 100
                defaultActionPlanId: idle
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
            actionPlans:
              idle:
                id: idle
                behavior:
                  steps:
                    - kind: MoveFacing
            scenarios:
              smoke:
                name: Smoke
                scenarioRootEntityTemplateId: room
                playerEntityTemplateId: actor
                playerEntityId: player
                playerStart: { x: 0, y: 0 }
            """);
}
