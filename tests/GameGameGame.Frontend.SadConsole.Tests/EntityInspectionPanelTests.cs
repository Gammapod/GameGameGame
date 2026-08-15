using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class EntityInspectionPanelTests
{
    [Fact]
    public void EntityInspectionPanelReservesSixBySixCandiiCellsForThreeByThreeCandii16Portrait()
    {
        var layout = EntityInspectionPanelLayout.Resolve(new FrontendRect(2, 3, 58, 24), showInventory: true);

        Assert.Equal(new FrontendRect(3, 4, 6, 6), layout.PortraitRegion);
        Assert.Equal(layout.PortraitRegion.Right + 1, layout.VerticalSeparatorX);
        Assert.True(layout.StatusRegion.X > layout.PortraitRegion.Right);
    }

    [Fact]
    public void EntityInspectionPanelBoundsInventoryPreviewToFiveByThreeCandii16Cells()
    {
        var layout = EntityInspectionPanelLayout.Resolve(new FrontendRect(2, 3, 58, 24), showInventory: true);

        Assert.NotNull(layout.InventoryRegion);
        Assert.Equal(10, layout.InventoryRegion!.Width);
        Assert.Equal(6, layout.InventoryRegion.Height);
    }

    [Fact]
    public void MixedTilesetOverlayPositionsCandii16ChildConsoleOverReservedParentCells()
    {
        var display = SadConsoleDisplaySettings.FromSettings(FrontendSadConsoleSettings.Default);
        var geometry = MixedTilesetPlayspaceOverlayGeometry.FromParentRegion(
            new FrontendRect(3, 4, 6, 6),
            childWidth: 3,
            childHeight: 3,
            display);

        Assert.Equal(3, geometry.ChildWidth);
        Assert.Equal(3, geometry.ChildHeight);
        Assert.Equal(3 * display.ScaledTileWidth, geometry.PixelX);
        Assert.Equal(4 * display.ScaledTileHeight, geometry.PixelY);
    }

    [Fact]
    public void EntityInspectionGalleryModelIncludesSelectableAndDisabledActions()
    {
        var model = EntityInspectionPanelModel.GalleryExample();

        Assert.Contains(model.Actions, action => action.Selectable && action.Text.Contains("Push"));
        Assert.Contains(model.Actions, action => !action.Selectable && action.FailureReason == "non-portable");
    }

    [Fact]
    public void EntityInspectionGalleryModelProvidesThreeByThreePortraitCells()
    {
        var model = EntityInspectionPanelModel.GalleryExample();

        Assert.Equal(9, model.PortraitCells.Count);
        Assert.Contains(model.PortraitCells, cell => cell.X == 1 && cell.Y == 1 && cell.EntityGlyph is not null);
    }
}
