using GameGameGame.Content;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class ContentReferenceQueryTests
{
    [Fact]
    public void ContentReferenceQueryListsReferencesFromSymbol()
    {
        var surface = ContentWorkspaceSurfaceService.Build(LoadReferenceDocument(), new ContentCompileOptions("doc-1", "refs.yaml"));

        var references = ContentReferenceQuery.ListReferencesFrom(surface, ContentSymbolKind.EntityTemplate, "room");

        Assert.Contains(references, reference => reference.Kind == ContentReferenceKind.DefaultActionPlan && reference.TargetId == "idle");
        Assert.Contains(references, reference => reference.Kind == ContentReferenceKind.CarriedEntityTemplate && reference.TargetId == "actor");
        Assert.All(references, reference =>
        {
            Assert.Equal(ContentSymbolKind.EntityTemplate, reference.SourceKind);
            Assert.Equal("room", reference.SourceId);
        });
    }

    [Fact]
    public void ContentReferenceQueryListsReferencesToSymbol()
    {
        var surface = ContentWorkspaceSurfaceService.Build(LoadReferenceDocument(), new ContentCompileOptions("doc-2", "refs.yaml"));

        var references = ContentReferenceQuery.ListReferencesTo(surface, ContentSymbolKind.EntityTemplate, "actor");

        Assert.Contains(references, reference => reference.Kind == ContentReferenceKind.CarriedEntityTemplate && reference.SourceId == "room");
        Assert.Contains(references, reference => reference.Kind == ContentReferenceKind.ScenarioPlayerTemplate && reference.SourceId == "smoke");
        Assert.All(references, reference =>
        {
            Assert.Equal(ContentSymbolKind.EntityTemplate, reference.TargetKind);
            Assert.Equal("actor", reference.TargetId);
        });
    }

    [Fact]
    public void ContentReferenceQueryListsMissingReferencesWithProvenance()
    {
        var surface = ContentWorkspaceSurfaceService.Build(LoadMissingReferenceDocument(), new ContentCompileOptions("doc-3", "missing.yaml"));

        var references = ContentReferenceQuery.ListMissingReferences(surface);

        Assert.Contains(references, reference =>
            reference.Kind == ContentReferenceKind.DefaultActionPlan
            && reference.SourceId == "actor"
            && reference.TargetId == "missingPlan"
            && reference.DocumentId == "doc-3"
            && reference.SourcePath == "missing.yaml");
        Assert.Contains(references, reference =>
            reference.Kind == ContentReferenceKind.ScenarioRootTemplate
            && reference.SourceId == "smoke"
            && reference.TargetId == "missingRoom"
            && reference.DocumentId == "doc-3"
            && reference.SourcePath == "missing.yaml");
    }

    [Fact]
    public void ContentReferenceQuerySummarizesUsedByRelationships()
    {
        var surface = ContentWorkspaceSurfaceService.Build(LoadReferenceDocument(), new ContentCompileOptions("doc-4", "refs.yaml"));

        var usedBy = ContentReferenceQuery.SummarizeUsedBy(surface, ContentSymbolKind.ActionPlan, "idle");

        Assert.Contains(usedBy, summary =>
            summary.SourceKind == ContentSymbolKind.EntityTemplate
            && summary.SourceId == "room"
            && summary.ReferenceKind == ContentReferenceKind.DefaultActionPlan
            && summary.DocumentId == "doc-4"
            && summary.SourcePath == "refs.yaml");
    }

    private static EditableContentDocument LoadReferenceDocument() =>
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
            presentations:
              room: { glyph: '#', color: Gray }
              actor: { glyph: a, color: Green }
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
            """);

    private static EditableContentDocument LoadMissingReferenceDocument() =>
        EditableContentDocument.LoadYaml(
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
}
