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
    public void EntityInspectionPanelReservesEnoughActionRowsForThreeActionsAndOverflowAffordances()
    {
        var layout = EntityInspectionPanelLayout.Resolve(new FrontendRect(2, 3, 32, 24), showInventory: true);

        Assert.Equal(EntityInspectionPanelLayout.MinimumActionRegionHeight, layout.ActionsRegion.Height);
    }

    [Fact]
    public void EntityInspectionAdaptiveLayoutExpandsActionRegionBeforeInventoryWhenSpaceAllows()
    {
        var model = EntityInspectionPanelModel.ResponsiveStressGalleryExample();

        var layout = EntityInspectionPanelLayout.ResolveAdaptive(new FrontendRect(2, 3, 32, 24), model);

        Assert.Equal(7, layout.ActionsRegion.Height);
        Assert.NotNull(layout.InventoryRegion);
        Assert.Equal(7, layout.InventoryRegion!.Height);
    }

    [Fact]
    public void EntityInspectionAdaptiveLayoutPreservesActionMinimumByShrinkingInventoryFirst()
    {
        var model = EntityInspectionPanelModel.ResponsiveStressGalleryExample();

        var layout = EntityInspectionPanelLayout.ResolveAdaptive(new FrontendRect(2, 3, 32, 18), model);

        Assert.Equal(EntityInspectionPanelLayout.MinimumActionRegionHeight, layout.ActionsRegion.Height);
        Assert.NotNull(layout.InventoryRegion);
        Assert.Equal(EntityInspectionPanelLayout.MinimumInventoryRegionHeight, layout.InventoryRegion!.Height);
    }

    [Fact]
    public void EntityInspectionAdaptiveLayoutCollapsesInventoryWhenActionMinimumCannotShareSpace()
    {
        var model = EntityInspectionPanelModel.ResponsiveStressGalleryExample();

        var layout = EntityInspectionPanelLayout.ResolveAdaptive(new FrontendRect(2, 3, 32, 17), model);

        Assert.True(layout.ActionsRegion.Height >= EntityInspectionPanelLayout.MinimumActionRegionHeight);
        Assert.Null(layout.InventoryRegion);
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

        var text = FrontendTextResolver.InspectionPrototype;
        Assert.Contains(model.Actions, action => action.Selectable && text.Resolve(action.Text).Contains("Push"));
        Assert.Contains(model.Actions, action => !action.Selectable && action.FailureReason?.Id == "inspection.failure.nonPortable");
    }

    [Fact]
    public void FrontendTextResolverUsesTemplatesAndFallsBackToIds()
    {
        var text = FrontendTextResolver.InspectionPrototype;

        Assert.Equal("Aperture.text.id: 5", text.Resolve(FrontendTextMessage.Create(FrontendTextIds.InspectionStatAperture, ("value", 5))));
        Assert.Equal("unknown.text.id value=7", text.Resolve(FrontendTextMessage.Create("unknown.text.id", ("value", 7))));
    }

    [Fact]
    public void EntityInspectionGalleryModelProvidesThreeByThreePortraitCells()
    {
        var model = EntityInspectionPanelModel.GalleryExample();

        Assert.Equal(9, model.PortraitCells.Count);
        Assert.Contains(model.PortraitCells, cell => cell.X == 1 && cell.Y == 1 && cell.EntityGlyph is not null);
    }

    [Fact]
    public void EntityInspectionResponsiveBoundsUsePreferredNarrowWidthAndStableRightAnchor()
    {
        var available = new FrontendRect(1, 10, 100, 40);

        var bounds = EntityInspectionPanelLayout.ResolveResponsiveBounds(available, anchorRightPadding: 4);

        Assert.Equal(EntityInspectionPanelLayout.PreferredWidth, bounds.Width);
        Assert.Equal(available.Right - EntityInspectionPanelLayout.PreferredWidth - 4, bounds.X);
        Assert.Equal(EntityInspectionPanelLayout.MaximumHeight, bounds.Height);
    }

    [Fact]
    public void EntityInspectionResponsiveBoundsRespectViewportFractionCap()
    {
        var available = new FrontendRect(0, 0, 70, 30);

        var bounds = EntityInspectionPanelLayout.ResolveResponsiveBounds(available, anchorRightPadding: 1);

        Assert.Equal(28, bounds.Width);
        Assert.True(bounds.Width <= Math.Floor(available.Width * EntityInspectionPanelLayout.MaximumViewportWidthFraction));
    }

    [Fact]
    public void EntityInspectionTextWrapsWithinRegionWidth()
    {
        var lines = EntityInspectionPanelRenderer.WrapText("alpha beta gamma", width: 8);

        Assert.Equal(["alpha", "beta", "gamma"], lines);
        Assert.All(lines, line => Assert.True(line.Length <= 8));
    }

    [Fact]
    public void EntityInspectionClippedTextUsesTilesetEllipsisGlyph()
    {
        var profile = TilesetProfileLoader.LoadCandii();

        var glyphs = FrontendTextClipping.ToClippedGlyphs("abcdef", width: 4, profile);

        Assert.Equal([(int)'a', (int)'b', (int)'c', profile.Roles.Ellipsis], glyphs);
    }

    [Fact]
    public void EntityInspectionInventoryOverlayClipsToReservedVisibleCandii16Cells()
    {
        var model = EntityInspectionPanelModel.ResponsiveStressGalleryExample();
        var layout = EntityInspectionPanelLayout.Resolve(new FrontendRect(2, 3, 58, 24), showInventory: true);

        var visible = EntityInspectionPlayspaceOverlayPresenter.ResolveVisibleInventoryChildSize(layout.InventoryRegion!, model.InventoryCells);

        Assert.Equal((4, 2), visible);
        Assert.Contains(model.InventoryCells, cell => cell.X >= visible.Width || cell.Y >= visible.Height);
    }

    [Fact]
    public void EntityInspectionStatusOverflowCanBeDetectedForAffordanceRendering()
    {
        var model = EntityInspectionPanelModel.ResponsiveStressGalleryExample();
        var layout = EntityInspectionPanelLayout.Resolve(new FrontendRect(2, 3, 32, 24), showInventory: true);

        var lines = EntityInspectionPanelRenderer.ResolveStatusLines(model, FrontendTextResolver.InspectionPrototype, layout.StatusRegion.Width);

        Assert.True(lines.Count > layout.StatusRegion.Height);
    }

    [Fact]
    public void EntityInspectionActionOverflowCountsReservedAffordanceRow()
    {
        var hidden = EntityInspectionPanelRenderer.ResolveHiddenActionAffordanceCount(actionCount: 6, regionHeight: 4);

        Assert.Equal(4, hidden);
    }

    [Fact]
    public void EntityInspectionActionViewportKeepsMiddleSelectionVisibleWithAboveAndBelowIndicators()
    {
        var viewport = EntityInspectionPanelRenderer.ResolveActionViewport(actionCount: 6, regionHeight: 4, selectedIndex: 3, showOverflowAffordances: true);

        Assert.True(viewport.HasItemsAbove);
        Assert.True(viewport.HasItemsBelow);
        Assert.InRange(3, viewport.StartIndex, viewport.StartIndex + viewport.ActionCount - 1);
        Assert.Equal(3 - viewport.StartIndex, viewport.SelectedVisibleIndex);
    }

    [Fact]
    public void EntityInspectionActionViewportShowsUpwardOverflowWhenSelectionIsNearEnd()
    {
        var viewport = EntityInspectionPanelRenderer.ResolveActionViewport(actionCount: 6, regionHeight: 4, selectedIndex: 5, showOverflowAffordances: true);

        Assert.True(viewport.HasItemsAbove);
        Assert.False(viewport.HasItemsBelow);
        Assert.InRange(5, viewport.StartIndex, viewport.StartIndex + viewport.ActionCount - 1);
    }

    [Fact]
    public void EntityInspectionInventoryOverflowCountsHiddenCells()
    {
        var model = EntityInspectionPanelModel.ResponsiveStressGalleryExample();
        var layout = EntityInspectionPanelLayout.Resolve(new FrontendRect(2, 3, 32, 24), showInventory: true);

        var overflow = EntityInspectionPanelRenderer.ResolveInventoryOverflow(layout.InventoryRegion!, model.InventoryCells);

        Assert.Equal(8, overflow.VisibleCells);
        Assert.Equal(model.InventoryCells.Count - 8, overflow.HiddenCells);
        Assert.True(overflow.HasHiddenRight);
        Assert.True(overflow.HasHiddenBelow);
    }

    [Fact]
    public void EntityInspectionLargeInventoryGalleryModelProvidesTwentyByTwentyInventoryContent()
    {
        var model = EntityInspectionPanelModel.LargeInventoryGalleryExample();

        Assert.Equal(400, model.InventoryCells.Count);
        Assert.Equal(19, model.InventoryCells.Max(cell => cell.X));
        Assert.Equal(19, model.InventoryCells.Max(cell => cell.Y));
    }

    [Fact]
    public void EntityInspectionLargeInventoryGalleryLayoutUsesNarrowWidthAndInventoryHeightRatio()
    {
        var model = EntityInspectionPanelModel.LargeInventoryGalleryExample();

        var layout = EntityInspectionPanelLayout.ResolveAdaptive(new FrontendRect(2, 3, 32, 54), model);
        var visible = EntityInspectionPlayspaceOverlayPresenter.ResolveVisibleInventoryChildSize(layout.InventoryRegion!, model.InventoryCells);
        var overflow = EntityInspectionPanelRenderer.ResolveInventoryOverflow(layout.InventoryRegion!, model.InventoryCells);

        Assert.Equal(30, layout.InventoryRegion!.Width);
        Assert.Equal(24, layout.InventoryRegion.Height);
        Assert.Equal((14, 11), visible);
        Assert.True(overflow.HasHiddenRight);
        Assert.True(overflow.HasHiddenBelow);
    }

    [Fact]
    public void EntityInspectionInventoryHeightFitsAllRowsWhenAllColumnsFitAvailableWidth()
    {
        var inventory = new List<EntityInspectionPortraitCell>();
        for (var y = 0; y < 6; y++)
        for (var x = 0; x < 5; x++)
        {
            inventory.Add(new EntityInspectionPortraitCell(x, y, 160, SadRogue.Primitives.Color.DimGray, SadRogue.Primitives.Color.Black));
        }

        var model = EntityInspectionPanelModel.GalleryExample() with { InventoryCells = inventory };

        var height = EntityInspectionPanelLayout.ResolveDesiredInventoryRegionHeight(model, inventoryRegionWidth: 30);

        Assert.Equal(12, height);
    }
}
