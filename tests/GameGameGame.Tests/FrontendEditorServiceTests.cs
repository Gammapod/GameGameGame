using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class FrontendEditorServiceTests
{
    [Fact]
    public void OpenFileBuildsReadOnlyEditorSnapshotFromContentEditorSession()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var open = FrontendEditorService.OpenFile(path);

            Assert.True(open.IsSuccess, open.ErrorMessage);
            var service = open.Service!;
            var snapshot = service.GetSnapshot();

            Assert.Equal(path, snapshot.FilePath);
            Assert.False(snapshot.IsDirty);
            Assert.Single(snapshot.Scenarios);
            Assert.Equal("editor-smoke", snapshot.Scenarios[0].ScenarioId);
            Assert.Equal("Editor Smoke", snapshot.Scenarios[0].Name);
            Assert.Equal("editorRoom", snapshot.Scenarios[0].ScenarioRootEntityTemplateId);
            Assert.Equal("editorPlayer", snapshot.Scenarios[0].PlayerEntityTemplateId);
            Assert.Equal(new GridCoord(1, 1), snapshot.Scenarios[0].PlayerStart);

            var room = Assert.Single(snapshot.EntityTemplates, template => template.TemplateId == "editorRoom");
            Assert.Equal("Editor Room", room.Name);
            Assert.Equal('#', room.Glyph);
            Assert.Equal(PresentationColor.Gray, room.Color);
            Assert.Equal(2, room.CarriedEntities.Count);
            Assert.Contains(room.CarriedEntities, carried => carried.EntityId == "northWall" && carried.TemplateId == "wall");

            var player = Assert.Single(snapshot.EntityTemplates, template => template.TemplateId == "editorPlayer");
            Assert.Equal("moveEast", player.DefaultActionPlanId);

            var plan = Assert.Single(snapshot.ActionPlans);
            Assert.Equal("moveEast", plan.ActionPlanId);
            Assert.Equal("Canonical Behavior Chain", plan.Shape);
            Assert.Equal(["Move Facing"], plan.ActionStepNames);

            Assert.DoesNotContain(snapshot.ValidationDiagnostics, diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error);
            Assert.Contains("editor-smoke", snapshot.YamlPreview);
            Assert.True(snapshot.YamlDiffLines.Count == 0);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void PreviewScenarioMaterializesTurnZeroRuntimeStateWithoutMutatingDocument()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;

            var preview = service.PreviewScenario("editor-smoke");

            Assert.True(preview.CanPlay, string.Join(" | ", preview.ValidationDiagnostics.Concat(preview.RuntimeFailures)));
            Assert.True(preview.IsDerivedRuntimeState);
            Assert.Equal("editor-smoke", preview.ScenarioId);
            Assert.Equal("Editor Smoke", preview.Name);
            Assert.Equal(new EntityId("editorPlayer"), preview.Session.PlayerEntityId);
            Assert.Equal(new EntityId("scenarioRoot"), preview.Session.ActiveContainerEntityId);
            Assert.Equal("editor-smoke", service.GetSnapshot().Scenarios.Single().ScenarioId);
            Assert.False(service.GetSnapshot().IsDirty);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SnapshotReflectsServiceBackedEditsThroughSharedSession()
    {
        var path = WriteTempContentFile(EditorFixtureYaml());

        try
        {
            var service = FrontendEditorService.OpenFile(path).Service!;
            var session = service.Session;

            session.Editor.UpdateEntityPreset(
                new EntityTemplateId("wall"),
                session.Editor.GetEntityPreset(new EntityTemplateId("wall")).Template with { Name = "Renamed Wall" },
                new EntityPresentation('W', PresentationColor.White));

            var snapshot = service.GetSnapshot();
            var wall = Assert.Single(snapshot.EntityTemplates, template => template.TemplateId == "wall");
            Assert.Equal("Renamed Wall", wall.Name);
            Assert.Equal('W', wall.Glyph);
            Assert.True(snapshot.IsDirty);
            Assert.Contains(snapshot.YamlDiffLines, line => line.Contains("Renamed Wall", StringComparison.Ordinal));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private static string EditorFixtureYaml() =>
        """
        entityTemplates:
          editorRoom:
            name: Editor Room
            inventoryWidth: 3
            inventoryHeight: 2
            weight: 100
            carryingCapacity: 100
            carriedEntities:
            - entityId: northWall
              templateId: wall
              coord:
                x: 0
                y: 0
            - entityId: floorRock
              templateId: rock
              coord:
                x: 2
                y: 1
          editorPlayer:
            name: Editor Player
            inventoryWidth: 1
            inventoryHeight: 1
            weight: 1
            carryingCapacity: 5
            defaultActionPlanId: moveEast
            actionStateDefaults:
              facing: East
          wall:
            name: Wall
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 10
            carryingCapacity: 0
          rock:
            name: Rock
            inventoryWidth: 0
            inventoryHeight: 0
            weight: 1
            carryingCapacity: 0
        presentations:
          editorRoom:
            glyph: '#'
            color: Gray
          editorPlayer:
            glyph: '@'
            color: Yellow
          wall:
            glyph: '#'
            color: Earth
          rock:
            glyph: '*'
            color: Earth
        actionPlans:
          moveEast:
            id: moveEast
            behavior:
              steps:
              - kind: MoveFacing
        scenarios:
          editor-smoke:
            name: Editor Smoke
            scenarioRootEntityTemplateId: editorRoom
            playerEntityTemplateId: editorPlayer
            playerEntityId: editorPlayer
            playerStart:
              x: 1
              y: 1
        """;

    private static string WriteTempContentFile(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), $"frontend-editor-service-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(path, yaml);
        return path;
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
