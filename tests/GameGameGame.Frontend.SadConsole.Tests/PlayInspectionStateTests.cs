using GameGameGame.Core;
using GameGameGame.Frontend.SadConsole;
using SadRogue.Primitives;
using SadMirror = SadConsole.Mirror;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class PlayInspectionStateTests
{
    [Fact]
    public void InspectionStateShowsHighlightedOccupiedCell()
    {
        var player = new EntityId("player");
        var block = new EntityId("block");
        var grid = Grid(player, block);
        var state = new PlayInspectionState();

        var inspected = state.ResolveInspectedCell(grid, new GridCoord(2, 1));

        Assert.Equal(block, inspected?.EntityId);
        Assert.Equal(block, state.LastInspectedEntityId);
    }

    [Fact]
    public void InspectionStateKeepsLastEntityWhenHighlightMovesToEmptyCell()
    {
        var player = new EntityId("player");
        var block = new EntityId("block");
        var grid = Grid(player, block);
        var state = new PlayInspectionState();

        state.ResolveInspectedCell(grid, new GridCoord(2, 1));
        var inspected = state.ResolveInspectedCell(grid, new GridCoord(0, 0));

        Assert.Equal(block, inspected?.EntityId);
    }

    [Fact]
    public void PlayModeInspectionLayoutPlacesOverlayWithoutReducingGridBoundsWhenSpaceAllows()
    {
        var drawable = new FrontendRect(1, 1, 100, 40);
        var layout = PlayModeInspectionLayout.Resolve(drawable);

        Assert.NotNull(layout.InspectionBounds);
        Assert.NotNull(layout.PlayerPanelBounds);
        Assert.Equal(drawable, layout.GridBounds);
        Assert.True(layout.InspectionBounds!.Right <= drawable.Right);
        Assert.Equal(42, layout.InspectionBounds.Width);
        Assert.Equal(drawable.X, layout.PlayerPanelBounds!.X);
        Assert.Equal(drawable.Bottom, layout.PlayerPanelBounds.Bottom);
    }

    [Fact]
    public void InspectionStateIsBlankWhenNoEntityIsAdjacentToPlayer()
    {
        var player = new EntityId("player");
        var block = new EntityId("block");
        var state = new PlayInspectionState();

        state.ResolveInspectedCell(Grid(player, block), new GridCoord(2, 1));
        var inspected = state.ResolveInspectedCell(GridWithDistantBlock(player, block), null);

        Assert.Null(inspected);
        Assert.Null(state.LastInspectedEntityId);
    }

    [Fact]
    public void InspectionPanelModelBuildsPortraitFromActualSurroundingPlayCells()
    {
        var tileset = TilesetProfileLoader.LoadCandii();
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = GameGameGame.Content.WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var actionSession = new PlayActionSessionController(session);
        var grid = PlayGridViewModel.FromSession(session, tileset);
        var inspected = Assert.Single(grid.Cells, cell => cell.EntityId?.Value == "debugPushBlock");

        var model = EntityInspectionPanelModelFactory.FromEntity(session, grid, inspected, actionSession.CurrentActionChoiceRequest, tileset);

        Assert.Equal(9, model.PortraitCells.Count);
        Assert.Contains(model.PortraitCells, cell => cell.X == 1 && cell.Y == 1 && cell.EntityGlyph == inspected.EntityGlyph);
        Assert.Contains(model.PortraitCells, cell => cell.X == 1 && cell.Y == 0 && cell.EntityGlyph is not null && cell.FacingGlyph is not null);
    }

    [Fact]
    public void InspectionPanelModelMarksHighlightedRepresentedCellInPortrait()
    {
        var player = new EntityId("player");
        var block = new EntityId("block");
        var grid = Grid(player, block);
        var inspected = grid.CellAt(2, 1);

        var cells = InspectionPortraitProjector.Project(grid, inspected, new PlayHighlightState(new GridCoord(1, 1), CellHighlightKind.EntityTarget));

        Assert.Contains(cells, cell => cell.X == 0 && cell.Y == 1 && cell.HighlightKind == CellHighlightKind.EntityTarget);
    }

    [Fact]
    public void InspectionPanelModelShowsCoreAuthoredActionChoicesForInspectedEntity()
    {
        var tileset = TilesetProfileLoader.LoadCandii();
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = GameGameGame.Content.WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var actionSession = new PlayActionSessionController(session);
        var grid = PlayGridViewModel.FromSession(session, tileset);
        var inspected = Assert.Single(grid.Cells, cell => cell.EntityId?.Value == "debugPushBlock");

        var model = EntityInspectionPanelModelFactory.FromEntity(session, grid, inspected, actionSession.CurrentActionChoiceRequest, tileset);

        var text = FrontendTextResolver.InspectionPrototype;
        Assert.Contains(model.Actions, action => !action.Selectable && text.Resolve(action.Text).Contains("Pickup") && action.FailureReason is not null);
        Assert.DoesNotContain(model.Actions, action => text.Resolve(action.Text).Contains("Display only"));
    }

    [Fact]
    public void InspectionInventoryProjectorShowsPlayerInventoryCells()
    {
        var tileset = TilesetProfileLoader.LoadCandii();
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = GameGameGame.Content.WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);

        var cells = InspectionInventoryProjector.Project(session, session.PlayerEntityId, tileset);

        Assert.Equal(4, cells.Count);
        Assert.Contains(cells, cell => cell.X == 0 && cell.Y == 0);
        Assert.Contains(cells, cell => cell.X == 1 && cell.Y == 1);
    }

    [Fact]
    public void PlayerPanelPortraitCanShowCurrentAimHighlight()
    {
        var tileset = TilesetProfileLoader.LoadCandii();
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = GameGameGame.Content.WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var actionSession = new PlayActionSessionController(session);
        var grid = PlayGridViewModel.FromSession(session, tileset);
        var player = Assert.Single(grid.Cells, cell => cell.EntityId == session.PlayerEntityId);

        var model = EntityInspectionPanelModelFactory.FromEntity(
            session,
            grid,
            player,
            actionSession.CurrentActionChoiceRequest,
            tileset,
            new PlayHighlightState(new GridCoord(player.X, player.Y - 1), CellHighlightKind.MovePreview));

        Assert.Contains(model.PortraitCells, cell => cell.X == 1 && cell.Y == 0 && cell.HighlightKind == CellHighlightKind.MovePreview);
    }

    private static PlayGridViewModel Grid(EntityId player, EntityId block) => new(
        "test",
        4,
        3,
        [
            Cell(0, 0), Cell(1, 0), Cell(2, 0), Cell(3, 0),
            Cell(0, 1), Cell(1, 1, player), Cell(2, 1, block), Cell(3, 1),
            Cell(0, 2), Cell(1, 2), Cell(2, 2), Cell(3, 2)
        ],
        player,
        new GridCoord(1, 1),
        new PlaneId("plane"),
        null,
        []);

    private static PlayGridViewModel GridWithDistantBlock(EntityId player, EntityId block) => new(
        "test",
        5,
        5,
        [
            Cell(0, 0), Cell(1, 0), Cell(2, 0), Cell(3, 0), Cell(4, 0),
            Cell(0, 1), Cell(1, 1, player), Cell(2, 1), Cell(3, 1), Cell(4, 1),
            Cell(0, 2), Cell(1, 2), Cell(2, 2), Cell(3, 2), Cell(4, 2),
            Cell(0, 3), Cell(1, 3), Cell(2, 3), Cell(3, 3), Cell(4, 3),
            Cell(0, 4), Cell(1, 4), Cell(2, 4), Cell(3, 4), Cell(4, 4, block)
        ],
        player,
        new GridCoord(1, 1),
        new PlaneId("plane"),
        null,
        []);

    private static PlayCellVisual Cell(int x, int y, EntityId? entityId = null) => new(
        x,
        y,
        160,
        Color.Gray,
        Color.Black,
        entityId is null ? null : 254,
        entityId is null ? null : Color.White,
        entityId,
        entityId is null ? null : 252,
        SadMirror.None);
}
