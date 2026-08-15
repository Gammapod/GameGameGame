using GameGameGame.Content;
using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class PlayGridViewModelTests
{
    [Fact]
    public void PlayGridViewModelBuildsDebugRoomStartingLocationWithBackdropUnderEveryCell()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var tileset = TilesetProfileLoader.LoadCandii();

        var grid = PlayGridViewModel.FromSession(session, tileset);

        Assert.Equal(9, grid.Width);
        Assert.Equal(7, grid.Height);
        Assert.Equal(new GameGameGame.Core.EntityId("debugStartRoom"), grid.ContainerEntityId);
        Assert.Equal(63, grid.Cells.Count);
        Assert.All(grid.Cells, cell => Assert.Equal(tileset.Roles.DefaultBackdrop, cell.BackdropGlyph));
        Assert.Equal(new GameGameGame.Core.GridCoord(4, 3), grid.ControlledEntityCoord);
        var playerCell = grid.CellAt(4, 3);
        Assert.Equal(session.PlayerEntityId, playerCell.EntityId);
        Assert.Equal(219, playerCell.EntityGlyph);
        Assert.Equal(252, playerCell.FacingGlyph);
        Assert.Equal(global::SadConsole.Mirror.None, playerCell.FacingMirror);
    }
}
