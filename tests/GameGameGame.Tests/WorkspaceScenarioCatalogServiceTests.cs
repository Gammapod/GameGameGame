using GameGameGame.Content;
using GameGameGame.Core;
using System.Runtime.CompilerServices;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class WorkspaceScenarioCatalogServiceTests
{
    [Fact]
    public void WorkspaceScenarioCatalogDiscoversDebugRoomScenario()
    {
        var catalog = BuildRepositoryCatalog();

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
        var catalog = BuildRepositoryCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");

        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);

        Assert.Equal("debug-room", session.ScenarioId);
        Assert.Equal("Debug Room", session.Name);
        Assert.True(session.CanPlay, string.Join(Environment.NewLine, session.ValidationDiagnostics.Concat(session.RuntimeFailures).Concat(session.CapabilityGaps)));
        Assert.Equal(new EntityId("debugPlayer"), session.PlayerEntityId);
        Assert.Equal(new PlaneCoord(new PlaneId("debugStartRoom"), new GridCoord(4, 3)), session.World.GetEntityLocation(session.PlayerEntityId));
    }

    [Fact]
    public void WorkspaceScenarioLaunchUsesPlayableScenarioLauncherCreateFromWorkspace()
    {
        var catalog = BuildRepositoryCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");

        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);

        Assert.Empty(session.ValidationDiagnostics);
        Assert.True(session.Registry.EntityTemplates.ContainsKey(new EntityTemplateId("debugPlayer")));
        Assert.True(session.Registry.EntityTemplates.ContainsKey(new EntityTemplateId("debugRoomRoot")));
    }

    [Fact]
    public void WorkspaceScenarioCatalogResolvesDefaultsFromExplicitRepositoryRootWhenCurrentDirectoryIsElsewhere()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempDirectory = Directory.CreateTempSubdirectory("ggg-catalog-cwd-");
        try
        {
            Directory.SetCurrentDirectory(tempDirectory.FullName);

            var catalog = WorkspaceScenarioCatalogService.BuildCatalog(new WorkspaceScenarioCatalogOptions(
                RepositoryRoot: RepositoryRoot(),
                IncludeSingleFileCompatibilityScenarios: false));

            Assert.Contains(catalog.Entries, entry => entry.ScenarioId == "debug-room" && entry.IsWorkspaceBacked);
            Assert.DoesNotContain(catalog.Diagnostics, diagnostic => diagnostic.Contains("Workspace content path", StringComparison.Ordinal));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void WorkspaceScenarioCatalogResolvesCompatibilityFolderFromExplicitRepositoryRootWhenCurrentDirectoryIsElsewhere()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempDirectory = Directory.CreateTempSubdirectory("ggg-catalog-cwd-");
        try
        {
            Directory.SetCurrentDirectory(tempDirectory.FullName);

            var catalog = WorkspaceScenarioCatalogService.BuildCatalog(new WorkspaceScenarioCatalogOptions(
                WorkspaceContentPaths: [],
                CompatibilityScenarioFolder: Path.Combine("src", "GameGameGame.Content", "Beta"),
                RepositoryRoot: RepositoryRoot()));

            Assert.Contains(catalog.Entries, entry => entry.LaunchKind == WorkspaceScenarioLaunchKind.File);
            Assert.DoesNotContain(catalog.Diagnostics, diagnostic => diagnostic.Contains("Compatibility scenario catalog", StringComparison.Ordinal)
                && diagnostic.Contains("not found", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            tempDirectory.Delete(recursive: true);
        }
    }

    private static string RepositoryRoot([CallerFilePath] string sourcePath = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath)!, "..", ".."));

    private static WorkspaceScenarioCatalogResult BuildRepositoryCatalog() =>
        WorkspaceScenarioCatalogService.BuildCatalog(new WorkspaceScenarioCatalogOptions(RepositoryRoot: RepositoryRoot()));
}
