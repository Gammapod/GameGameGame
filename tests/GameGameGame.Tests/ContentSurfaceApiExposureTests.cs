using GameGameGame.Content;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class ContentSurfaceApiExposureTests
{
    [Fact]
    public void ContentEditorServiceBuildsWorkspaceSurface()
    {
        var editor = new ContentEditorService(LoadSurfaceDocument());

        var surface = editor.BuildWorkspaceSurface(new ContentCompileOptions("doc-1", "content.yaml"));

        Assert.Equal("doc-1", surface.Source.DocumentId);
        Assert.Equal("content.yaml", surface.Source.SourcePath);
        Assert.Contains(surface.Scenarios, scenario => scenario.Id == "smoke");
        Assert.Contains(surface.EntityTemplates, template => template.Id == "room");
    }

    [Fact]
    public void ContentEditorServiceBuildsScenarioSurface()
    {
        var editor = new ContentEditorService(LoadSurfaceDocument());

        var surface = editor.BuildScenarioSurface("smoke", new ContentCompileOptions("doc-2", "scenario.yaml"));

        Assert.Equal("smoke", surface.SelectedScenario.Id);
        Assert.Equal("room", surface.RootTemplateId);
        Assert.Equal("actor", surface.PlayerTemplateId);
        Assert.Contains(surface.Workspace.EntityTemplates, template => template.Id == "unused");
    }

    [Fact]
    public void ContentEditorServiceSurfaceQueriesDoNotMarkSessionDirtyOrChangeYaml()
    {
        var path = WriteTempContentFile(LoadSurfaceDocument().SaveYaml());
        try
        {
            var open = ContentEditorSession.OpenFile(path);
            Assert.True(open.IsSuccess, open.ErrorMessage);
            var session = open.Session!;
            var yamlBefore = session.GetYamlPreview();

            _ = session.Editor.BuildWorkspaceSurface(new ContentCompileOptions("doc-3", path));
            _ = session.Editor.BuildScenarioSurface("smoke", new ContentCompileOptions("doc-3", path));

            Assert.False(session.IsDirty);
            Assert.Equal(yamlBefore, session.GetYamlPreview());
            Assert.Empty(session.GetYamlDiff().Lines);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AgentContentEditorApiReturnsWorkspaceSurface()
    {
        var api = new AgentContentEditorApi(ContentEditorSession.CreateNew());
        var roomId = api.Session.Editor.CreateEntityPreset("Room");

        var result = api.BuildWorkspaceSurface(new ContentCompileOptions("agent-doc", "agent.yaml"));

        Assert.True(result.IsSuccess, result.Error?.Message);
        var surface = result.Value!;
        Assert.Equal("agent-doc", surface.Source.DocumentId);
        Assert.Contains(surface.EntityTemplates, template => template.Id == roomId.Value);
    }

    [Fact]
    public void AgentContentEditorApiReturnsScenarioSurface()
    {
        var api = new AgentContentEditorApi(ContentEditorSession.CreateNew());
        var roomId = api.Session.Editor.CreateEntityPreset("Room");
        api.Session.Editor.UpsertScenario(new ScenarioDefinition(
            "agent-scenario",
            "Agent Scenario",
            roomId,
            PlayerEntityTemplateId: null,
            PlayerEntityId: null,
            PlayerStart: null));

        var result = api.BuildScenarioSurface("agent-scenario", new ContentCompileOptions("agent-doc", "agent.yaml"));

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("agent-scenario", result.Value!.SelectedScenario.Id);
        Assert.Equal(roomId.Value, result.Value.RootTemplateId);
    }

    [Fact]
    public void FrontendEditorServiceCanQueryScenarioSurfaceWithoutMutatingDocument()
    {
        var path = WriteTempContentFile(LoadSurfaceDocument().SaveYaml());
        try
        {
            var open = FrontendEditorService.OpenFile(path);
            Assert.True(open.IsSuccess, open.ErrorMessage);
            var service = open.Service!;
            var yamlBefore = service.Session.GetYamlPreview();

            var surface = service.BuildScenarioSurface("smoke", new ContentCompileOptions("frontend-doc", path));

            Assert.Equal("smoke", surface.SelectedScenario.Id);
            Assert.Equal("frontend-doc", surface.Workspace.Source.DocumentId);
            Assert.False(service.Session.IsDirty);
            Assert.Equal(yamlBefore, service.Session.GetYamlPreview());
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteTempContentFile(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ggg-content-surface-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(path, yaml);
        return path;
    }

    private static EditableContentDocument LoadSurfaceDocument() =>
        EditableContentDocument.LoadYaml(
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
}
