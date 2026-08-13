using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class WorkspaceScenarioCatalogServiceTests
{
    [Fact]
    public void WorkspaceScenarioCatalogDiscoversDebugRoomScenario()
    {
        var catalog = WorkspaceScenarioCatalogService.BuildDefaultCatalog();

        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        Assert.Equal("Debug Room", entry.Name);
        Assert.True(entry.IsWorkspaceBacked);
        Assert.Equal(WorkspaceScenarioLaunchKind.Workspace, entry.LaunchKind);
        Assert.Contains(catalog.Workspace.Documents, document => document.DocumentId == "canonical.creatures.debug-player" && document.IsReadOnly);
        Assert.Contains(catalog.Workspace.Documents, document => document.DocumentId == "canonical.spaces.debug-room-root" && document.IsReadOnly);
        Assert.Contains(catalog.Workspace.Documents, document => document.DocumentId == "debug.debug-room");
        Assert.DoesNotContain(catalog.Diagnostics, diagnostic => diagnostic.Contains("debug-room", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WorkspaceScenarioLaunchCreatesPlayableSessionFromDebugRoom()
    {
        var catalog = WorkspaceScenarioCatalogService.BuildDefaultCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");

        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);

        Assert.Equal("debug-room", session.ScenarioId);
        Assert.Equal("Debug Room", session.Name);
        Assert.True(session.CanPlay, string.Join(Environment.NewLine, session.ValidationDiagnostics.Concat(session.RuntimeFailures).Concat(session.CapabilityGaps)));
        Assert.Equal(new EntityId("debugPlayer"), session.PlayerEntityId);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(4, 3)), session.World.GetEntityLocation(session.PlayerEntityId));
    }

    [Fact]
    public void WorkspaceScenarioLaunchUsesPlayableScenarioLauncherCreateFromWorkspace()
    {
        var catalog = WorkspaceScenarioCatalogService.BuildDefaultCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");

        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);

        Assert.Empty(session.ValidationDiagnostics);
        Assert.True(session.Registry.EntityTemplates.ContainsKey(new EntityTemplateId("debugPlayer")));
        Assert.True(session.Registry.EntityTemplates.ContainsKey(new EntityTemplateId("debugRoomRoot")));
    }
}
