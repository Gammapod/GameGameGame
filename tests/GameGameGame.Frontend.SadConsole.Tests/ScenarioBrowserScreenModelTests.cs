using GameGameGame.Content;
using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class ScenarioBrowserScreenModelTests
{
    [Fact]
    public void ScenarioBrowserShowsWorkspaceDebugRoomCatalogEntry()
    {
        var catalog = WorkspaceScenarioCatalogService.BuildDefaultCatalog();

        var model = new ScenarioBrowserScreenModel(catalog);

        Assert.Contains(model.Entries, entry => entry.ScenarioId == "debug-room" && entry.IsWorkspaceBacked);
    }

    [Fact]
    public void ScenarioBrowserSelectionCreatesLaunchRequestWithoutLaunchingSemantics()
    {
        var catalog = WorkspaceScenarioCatalogService.BuildDefaultCatalog();
        var model = new ScenarioBrowserScreenModel(catalog);
        while (model.SelectedEntry?.ScenarioId != "debug-room")
        {
            model.Handle(ScenarioBrowserCommand.Down);
        }

        var result = model.Handle(ScenarioBrowserCommand.Select);

        Assert.Equal(ScenarioBrowserResultKind.LaunchRequested, result.Kind);
        Assert.Equal("debug-room", result.Entry?.ScenarioId);
        Assert.NotNull(result.Entry?.EntryId);
    }

    [Fact]
    public void ScenarioBrowserPreservesDiagnosticsAsDisplayData()
    {
        var catalog = new WorkspaceScenarioCatalogResult(
            new ContentWorkspace([]),
            [],
            ["Workspace content path missing.yaml was not found."]);

        var model = new ScenarioBrowserScreenModel(catalog);

        Assert.Equal(["Workspace content path missing.yaml was not found."], model.Diagnostics);
        Assert.Empty(model.Entries);
        Assert.Equal(ScenarioBrowserResultKind.Stay, model.Handle(ScenarioBrowserCommand.Select).Kind);
    }
}
