using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class ContentCompilerTests
{
    [Fact]
    public void ContentCompilerCompilesEditableDocumentToRegistry()
    {
        var document = LoadMinimalValidDocument();

        var result = ContentCompiler.Compile(document);

        Assert.NotNull(result.Registry);
        Assert.True(result.Validation.IsValid, string.Join(Environment.NewLine, result.Validation.Errors));
        Assert.True(result.Registry.EntityTemplates.ContainsKey(new EntityTemplateId("room")));
        Assert.True(result.Registry.ActionPlanDescriptors.ContainsKey(new ActionPlanTemplateId("wait")));
    }

    [Fact]
    public void ContentCompilerReturnsRegistryValidationDiagnostics()
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
              actor:
                glyph: a
                color: Green
            actionPlans: {}
            """);

        var result = ContentCompiler.Compile(document);

        Assert.NotNull(result.Registry);
        Assert.False(result.Validation.IsValid);
        var diagnostic = Assert.Single(result.Validation.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingActionPlanReference);
        Assert.Equal(new EntityTemplateId("actor"), diagnostic.EntityTemplateId);
        Assert.Equal(new ActionPlanTemplateId("missingPlan"), diagnostic.ActionPlanTemplateId);
    }

    [Fact]
    public void ContentCompilerReturnsMaterializationFailureAsDiagnostic()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 100
                carryingCapacity: 100
                carriedEntities:
                  - entityId: broken
                    coord:
                      x: 0
                      y: 0
            presentations:
              room:
                glyph: '#'
                color: Gray
            actionPlans: {}
            """);

        var result = ContentCompiler.Compile(document);

        Assert.Null(result.Registry);
        Assert.False(result.Validation.IsValid);
        var diagnostic = Assert.Single(result.Validation.Diagnostics);
        Assert.Equal(ContentDiagnosticCode.General, diagnostic.Code);
        Assert.Contains("could not be compiled", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TemplateId", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContentCompilerIncludesCanonicalAuthoringDiagnostics()
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
                defaultPlanVariables:
                  mood:
                    kind: Int
                    intValue: 1
            presentations:
              actor:
                glyph: a
                color: Green
            actionPlans: {}
            """);

        var result = ContentCompiler.Compile(document);

        Assert.False(result.Validation.IsValid);
        var diagnostic = Assert.Single(result.Validation.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.ArbitraryPlanVariableField);
        Assert.Equal(new EntityTemplateId("actor"), diagnostic.EntityTemplateId);
        Assert.Equal("mood", diagnostic.VariableName);
    }

    [Fact]
    public void ContentCompilerIncludesScenarioAuthoringDiagnostics()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Compiler Scenario Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Compiler Scenario Room", InventoryWidth: 2, InventoryHeight: 2, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var playerTemplateId = editor.CreateEntityPreset("Compiler Scenario Player");

        editor.UpsertScenario(new ScenarioDefinition(
            "bad-control",
            "Bad Control",
            scenarioRootId,
            playerTemplateId,
            new EntityId("insertedPlayer"),
            new GridCoord(0, 0),
            new Dictionary<string, IReadOnlyList<EntityId>>
            {
                ["player-1"] = [new EntityId("missingActor")]
            }));

        var result = ContentCompiler.Compile(document);

        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Diagnostics, diagnostic =>
            diagnostic.Code == ContentDiagnosticCode.InvalidScenarioDefinition
            && diagnostic.RelatedEntityId == new EntityId("missingActor")
            && diagnostic.Message.Contains("bad-control", StringComparison.Ordinal));
    }

    [Fact]
    public void ContentCompilerDeduplicatesRegistryAndCanonicalAuthoringDiagnostics()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates: {}
            presentations: {}
            actionPlans:
              mixed:
                id: mixed
                behavior:
                  steps:
                    - kind: MoveFacing
                steps:
                  - label: wait
                    checks: []
                    onSuccess:
                      kind: Wait
            """);

        var result = ContentCompiler.Compile(document);

        Assert.False(result.Validation.IsValid);
        Assert.Single(result.Validation.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.InvalidActionPlanShape);
    }

    [Fact]
    public void ContentCompilerAnnotatesDiagnosticsWithDocumentSource()
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
              actor:
                glyph: a
                color: Green
            actionPlans: {}
            """);

        var result = ContentCompiler.Compile(document, new ContentCompileOptions(
            DocumentId: "doc-1",
            SourcePath: "content/Scenario.yaml"));

        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == ContentDiagnosticCode.MissingActionPlanReference);
        Assert.Equal("doc-1", diagnostic.DocumentId);
        Assert.Equal("content/Scenario.yaml", diagnostic.SourcePath);
    }

    [Fact]
    public void ContentCompilerBuildsSymbolsGroupedByType()
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
              room:
                name: Room
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 10
                carryingCapacity: 10
            presentations:
              actor: { glyph: a, color: Green }
              room: { glyph: '#', color: Gray }
            actionPlans:
              move:
                id: move
                behavior:
                  steps:
                    - kind: MoveFacing
            scenarios:
              smoke:
                name: Smoke
                scenarioRootEntityTemplateId: room
            """);

        var result = ContentCompiler.Compile(document, new ContentCompileOptions("doc-1", "content.yaml"));

        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.EntityTemplate && symbol.Id == "actor" && symbol.DisplayName == "Actor");
        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.EntityTemplate && symbol.Id == "room" && symbol.DisplayName == "Room");
        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.ActionPlan && symbol.Id == "move");
        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.Scenario && symbol.Id == "smoke" && symbol.DisplayName == "Smoke");
        Assert.Contains(result.Symbols, symbol => symbol.Kind == ContentSymbolKind.Presentation && symbol.Id == "actor");
        Assert.All(result.Symbols, symbol =>
        {
            Assert.Equal("doc-1", symbol.DocumentId);
            Assert.Equal("content.yaml", symbol.SourcePath);
        });
    }

    [Fact]
    public void ContentCompilerIndexesTemplateActionPlanAndScenarioReferences()
    {
        var document = EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 10
                carryingCapacity: 10
                defaultActionPlanId: idle
                carriedEntities:
                  - entityId: actor1
                    templateId: actor
                    coord: { x: 0, y: 0 }
              actor:
                name: Actor
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
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
                playerEntityId: player
                playerStart: { x: 0, y: 0 }
            """);

        var result = ContentCompiler.Compile(document);

        Assert.Contains(result.References, reference =>
            reference.Kind == ContentReferenceKind.DefaultActionPlan
            && reference.SourceKind == ContentSymbolKind.EntityTemplate
            && reference.SourceId == "room"
            && reference.TargetKind == ContentSymbolKind.ActionPlan
            && reference.TargetId == "idle"
            && reference.Resolution == ContentReferenceResolution.Resolved);
        Assert.Contains(result.References, reference =>
            reference.Kind == ContentReferenceKind.CarriedEntityTemplate
            && reference.SourceId == "room"
            && reference.TargetKind == ContentSymbolKind.EntityTemplate
            && reference.TargetId == "actor"
            && reference.RelatedEntityId == new EntityId("actor1"));
        Assert.Contains(result.References, reference =>
            reference.Kind == ContentReferenceKind.ScenarioRootTemplate
            && reference.SourceKind == ContentSymbolKind.Scenario
            && reference.SourceId == "smoke"
            && reference.TargetId == "room");
        Assert.Contains(result.References, reference =>
            reference.Kind == ContentReferenceKind.ScenarioPlayerTemplate
            && reference.SourceId == "smoke"
            && reference.TargetId == "actor");
    }

    [Fact]
    public void ContentCompilerIndexesBehaviorStepTemplatePlanAndCostReferences()
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
              seed:
                name: Seed
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
              sprout:
                name: Sprout
                inventoryWidth: 0
                inventoryHeight: 0
                weight: 1
                carryingCapacity: 0
            presentations:
              actor: { glyph: a, color: Green }
              seed: { glyph: s, color: Yellow }
              sprout: { glyph: t, color: Green }
            actionPlans:
              child:
                id: child
                behavior:
                  steps:
                    - kind: MoveFacing
              parent:
                id: parent
                behavior:
                  steps:
                    - kind: ApplyPrePlan
                      planId: child
                    - kind: CreateEntity
                      templateId: sprout
                      costs:
                        - templateId: seed
                          quantity: 1
            """);

        var result = ContentCompiler.Compile(document);

        Assert.Contains(result.References, reference =>
            reference.Kind == ContentReferenceKind.BehaviorStepPlan
            && reference.SourceKind == ContentSymbolKind.ActionPlan
            && reference.SourceId == "parent"
            && reference.TargetKind == ContentSymbolKind.ActionPlan
            && reference.TargetId == "child"
            && reference.StepIndex == 0);
        Assert.Contains(result.References, reference =>
            reference.Kind == ContentReferenceKind.BehaviorStepTemplate
            && reference.SourceId == "parent"
            && reference.TargetKind == ContentSymbolKind.EntityTemplate
            && reference.TargetId == "sprout"
            && reference.StepIndex == 1);
        Assert.Contains(result.References, reference =>
            reference.Kind == ContentReferenceKind.BehaviorStepCostTemplate
            && reference.SourceId == "parent"
            && reference.TargetKind == ContentSymbolKind.EntityTemplate
            && reference.TargetId == "seed"
            && reference.StepIndex == 1);
    }

    [Fact]
    public void ContentCompilerMarksMissingReferencesWithoutResolvingAcrossFiles()
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
                targetingRules:
                  - slot: 1
                    label: wants
                    targetTemplateId: missingTarget
                    range: 3
            presentations:
              actor: { glyph: a, color: Green }
            actionPlans:
              createMissing:
                id: createMissing
                behavior:
                  steps:
                    - kind: CreateEntity
                      templateId: missingSpawn
                      costs:
                        - templateId: missingCost
                          quantity: 1
            scenarios:
              missingScenario:
                name: Missing Scenario
                scenarioRootEntityTemplateId: missingRoom
            """);

        var result = ContentCompiler.Compile(document);

        Assert.Contains(result.References, reference => reference.Kind == ContentReferenceKind.DefaultActionPlan && reference.TargetId == "missingPlan" && reference.Resolution == ContentReferenceResolution.Missing);
        Assert.Contains(result.References, reference => reference.Kind == ContentReferenceKind.TargetingTargetTemplate && reference.TargetId == "missingTarget" && reference.Resolution == ContentReferenceResolution.Missing);
        Assert.Contains(result.References, reference => reference.Kind == ContentReferenceKind.BehaviorStepTemplate && reference.TargetId == "missingSpawn" && reference.Resolution == ContentReferenceResolution.Missing);
        Assert.Contains(result.References, reference => reference.Kind == ContentReferenceKind.BehaviorStepCostTemplate && reference.TargetId == "missingCost" && reference.Resolution == ContentReferenceResolution.Missing);
        Assert.Contains(result.References, reference => reference.Kind == ContentReferenceKind.ScenarioRootTemplate && reference.TargetId == "missingRoom" && reference.Resolution == ContentReferenceResolution.Missing);
        Assert.DoesNotContain(result.References, reference => reference.Resolution == ContentReferenceResolution.Ambiguous);
    }

    [Fact]
    public void ContentCompilerMarksUnknownPresentationAndPaletteReferencesMissing()
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
            presentations:
              actor:
                presentationId: creature.unknown
                paletteId: palette.unknown
                glyph: a
                color: Green
            presentationCatalog:
              creature.actor:
                name: Actor
            palettes:
              palette.actor:
                name: Actor Palette
            actionPlans: {}
            """);

        var result = ContentCompiler.Compile(document);

        Assert.Contains(result.References, reference =>
            reference.Kind == ContentReferenceKind.PresentationId
            && reference.SourceId == "actor"
            && reference.TargetId == "creature.unknown"
            && reference.Resolution == ContentReferenceResolution.Missing);
        Assert.Contains(result.References, reference =>
            reference.Kind == ContentReferenceKind.PaletteId
            && reference.SourceId == "actor"
            && reference.TargetId == "palette.unknown"
            && reference.Resolution == ContentReferenceResolution.Missing);
    }

    private static EditableContentDocument LoadMinimalValidDocument() =>
        EditableContentDocument.LoadYaml(
            """
            entityTemplates:
              room:
                name: Room
                inventoryWidth: 1
                inventoryHeight: 1
                weight: 100
                carryingCapacity: 100
                defaultActionPlanId: wait
            presentations:
              room:
                glyph: '#'
                color: Gray
            actionPlans:
              wait:
                id: wait
                behavior:
                  steps:
                    - kind: MoveFacing
            """);
}
