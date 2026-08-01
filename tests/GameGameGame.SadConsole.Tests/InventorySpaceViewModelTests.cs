using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Screens;
using SadMirror = SadConsole.Mirror;

namespace GameGameGame.SadConsole.Tests;

public sealed class InventorySpaceViewModelTests
{
    [Fact]
    public void InventorySpaceDisplayProfilesMapRelationshipTiersToInitialSpaceZooms()
    {
        var currentLocation = InventorySpaceDisplayProfile.ForRelationshipTier(InventorySpaceRelationshipTier.CurrentLocation);
        var playerInventory = InventorySpaceDisplayProfile.ForRelationshipTier(InventorySpaceRelationshipTier.PlayerInventory);
        var immediateParent = InventorySpaceDisplayProfile.ForRelationshipTier(InventorySpaceRelationshipTier.ImmediateParent);
        var grandparent = InventorySpaceDisplayProfile.ForRelationshipTier(InventorySpaceRelationshipTier.Grandparent);
        var greatGrandparent = InventorySpaceDisplayProfile.ForRelationshipTier(InventorySpaceRelationshipTier.GreatGrandparentOrBeyond);

        Assert.Equal(InventorySpaceZoom.Huge32, currentLocation.SpaceZoom);
        Assert.Equal(32, currentLocation.CellPixelSize);
        Assert.Equal(4, currentLocation.CandiiScale);
        Assert.True(currentLocation.UsesCandiiFont);
        Assert.True(currentLocation.ShowFacingDecorators);
        Assert.True(currentLocation.CanRenderGlyphFacingDecorators);

        Assert.Equal(InventorySpaceZoom.Large24, playerInventory.SpaceZoom);
        Assert.Equal(24, playerInventory.CellPixelSize);
        Assert.Equal(1, playerInventory.CellGapPixels);
        Assert.Equal(3, playerInventory.CandiiScale);
        Assert.True(playerInventory.ShowFacingDecorators);
        Assert.True(playerInventory.CanRenderGlyphFacingDecorators);

        Assert.Equal(InventorySpaceZoom.Normal16, immediateParent.SpaceZoom);
        Assert.Equal(16, immediateParent.CellPixelSize);
        Assert.Equal(2, immediateParent.CandiiScale);
        Assert.True(immediateParent.ShowFacingDecorators);
        Assert.True(immediateParent.CanRenderGlyphFacingDecorators);

        Assert.Equal(InventorySpaceZoom.Small8, grandparent.SpaceZoom);
        Assert.Equal(8, grandparent.CellPixelSize);
        Assert.Equal(1, grandparent.CandiiScale);
        Assert.True(grandparent.ShowFacingDecorators);
        Assert.True(grandparent.CanRenderGlyphFacingDecorators);

        Assert.Equal(InventorySpaceZoom.Micro4, greatGrandparent.SpaceZoom);
        Assert.Equal(4, greatGrandparent.CellPixelSize);
        Assert.False(greatGrandparent.UsesCandiiFont);
        Assert.Null(greatGrandparent.CandiiScale);
        Assert.True(greatGrandparent.ShowFacingDecorators);
        Assert.False(greatGrandparent.CanRenderGlyphFacingDecorators);
    }

    [Fact]
    public void FacingGlyphUsesCandiiDirectionalArrowsAndSadConsoleMirrors()
    {
        Assert.Equal((252, SadMirror.None), InventorySpaceViewModel.FacingGlyph(Direction.North));
        Assert.Equal((252, SadMirror.Vertical), InventorySpaceViewModel.FacingGlyph(Direction.South));
        Assert.Equal((253, SadMirror.None), InventorySpaceViewModel.FacingGlyph(Direction.East));
        Assert.Equal((253, SadMirror.Horizontal), InventorySpaceViewModel.FacingGlyph(Direction.West));
        Assert.Equal((251, SadMirror.None), InventorySpaceViewModel.FacingGlyph(Direction.NorthWest));
        Assert.Equal((251, SadMirror.Horizontal), InventorySpaceViewModel.FacingGlyph(Direction.NorthEast));
        Assert.Equal((251, SadMirror.Vertical), InventorySpaceViewModel.FacingGlyph(Direction.SouthWest));
        Assert.Equal((251, SadMirror.Horizontal | SadMirror.Vertical), InventorySpaceViewModel.FacingGlyph(Direction.SouthEast));
    }

    [Fact]
    public void InventorySpaceDisplayProfileComputesRequiredPixelSizeIncludingGaps()
    {
        var playerInventory = InventorySpaceDisplayProfile.ForRelationshipTier(InventorySpaceRelationshipTier.PlayerInventory);

        Assert.Equal(99, playerInventory.RequiredPixelWidth(viewportWidth: 4));
        Assert.Equal(49, playerInventory.RequiredPixelHeight(viewportHeight: 2));
        Assert.Equal(0, playerInventory.RequiredPixelWidth(viewportWidth: 0));
        Assert.Equal(0, playerInventory.RequiredPixelHeight(viewportHeight: -1));
    }

    [Fact]
    public void InventorySpaceViewModelSeparatesBackdropEntityVisualsAndDecorators()
    {
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), PlayableScenarioLauncher.CreatePrototype());
        Assert.NotNull(screen.CurrentPlaceProjection);
        var projection = screen.CurrentPlaceProjection!;

        var view = InventorySpaceViewModel.FromProjection(
            "space",
            projection,
            screen.Session!.PlayerEntityId,
            selectedCoord: new GridCoord(0, 0),
            focusedCoord: new GridCoord(1, 0));

        Assert.Equal("space", view.Id);
        Assert.Equal(223, view.Backdrop.Tile.Glyph);
        Assert.Equal(0x808080, view.Backdrop.Tile.ForegroundRgb);
        Assert.Equal(0x404040, view.Backdrop.Tile.BackgroundRgb);
        Assert.Equal(projection.InventoryGrid!.PlaneId, view.PlaneId);
        Assert.Equal(projection.InventoryGrid.Width, view.Width);
        Assert.Equal(projection.InventoryGrid.Height, view.Height);
        Assert.Equal(projection.InventoryGrid.Cells.Count(cell => cell.EntityId is not null), view.Entities.Count);
        Assert.All(view.Entities, entity => Assert.Null(entity.Accent));
        Assert.Contains(view.Decorators, decorator => decorator.Role == InventorySpaceDecoratorRole.Controlled && decorator.EntityId == screen.Session.PlayerEntityId);
        Assert.Contains(view.Decorators, decorator => decorator.Role == InventorySpaceDecoratorRole.Selected && decorator.Coord == new GridCoord(0, 0));
        Assert.Contains(view.Decorators, decorator => decorator.Role == InventorySpaceDecoratorRole.Focused && decorator.Coord == new GridCoord(1, 0));
        Assert.True(view.Frame.Visible);
    }

    [Fact]
    public void InventorySpaceViewModelAddsFacingDecoratorFromProjectedFacingFactsWithoutReplacingEntityGlyph()
    {
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), PlayableScenarioLauncher.CreatePrototype());
        Assert.NotNull(screen.CurrentPlaceProjection);
        var projection = screen.CurrentPlaceProjection!;
        var playerId = screen.Session!.PlayerEntityId;
        var playerCell = projection.InventoryGrid!.Cells.Single(cell => cell.EntityId == playerId);

        var view = InventorySpaceViewModel.FromProjection(
            "space",
            projection,
            playerId,
            facingByEntityId: new Dictionary<EntityId, Direction> { [playerId] = Direction.NorthEast });

        var playerVisual = Assert.Single(view.Entities, entity => entity.EntityId == playerId);
        var facing = Assert.Single(view.Decorators, decorator => decorator.Role == InventorySpaceDecoratorRole.Facing && decorator.EntityId == playerId);
        Assert.Equal(playerCell.Glyph, playerVisual.Primary.Glyph);
        Assert.Equal(playerCell.Coord, facing.Coord);
        Assert.Equal(251, facing.Style.Glyph);
        Assert.Equal(SadMirror.Horizontal, facing.Style.Mirror);
        Assert.Equal(PresentationColor.Yellow, facing.Style.Foreground);
    }

    [Fact]
    public void InventorySpaceViewModelComputesStableViewportCellBoundsWithGap()
    {
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), PlayableScenarioLauncher.CreatePrototype());
        Assert.NotNull(screen.CurrentPlaceProjection);
        var projection = screen.CurrentPlaceProjection!;
        var view = InventorySpaceViewModel.FromProjection(
            "space",
            projection,
            cellMetrics: new InventorySpaceCellMetrics(2, 3, 1),
            viewport: new InventorySpaceViewport(new GridCoord(1, 1), 2, 2));

        Assert.True(view.IsVisible(new GridCoord(1, 1)));
        Assert.True(view.IsVisible(new GridCoord(2, 2)));
        Assert.False(view.IsVisible(new GridCoord(0, 0)));
        Assert.Equal(new SadConsoleRect(0, 0, 2, 3), view.CellBounds(new GridCoord(1, 1)));
        Assert.Equal(new SadConsoleRect(3, 4, 2, 7), view.CellBounds(new GridCoord(2, 2)));
        Assert.Equal([new GridCoord(1, 1), new GridCoord(2, 1), new GridCoord(1, 2), new GridCoord(2, 2)], view.VisibleCoords());
    }

    [Fact]
    public void InventorySpaceComponentCarriesViewModelForRendererWithoutFlatteningLayers()
    {
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), PlayableScenarioLauncher.CreatePrototype());
        Assert.NotNull(screen.CurrentSpaceView);
        var view = screen.CurrentSpaceView!;
        var component = new InventorySpaceComponent(
            "space-component",
            "Current space",
            SadConsoleRect.FromSize(1, 1, 40, 12),
            view,
            ["summary row"],
            UiComponentState.Focused,
            InventorySpaceRenderOptions.FramedDebug,
            InventorySpaceDisplayProfile.ForRelationshipTier(InventorySpaceRelationshipTier.CurrentLocation));

        var rows = component.RenderRows(GameGameGame.SadConsoleApp.Ui.Styling.SadConsoleTheme.Default);

        Assert.Same(view, component.View);
        Assert.Equal(InventorySpaceZoom.Huge32, component.DisplayProfile?.SpaceZoom);
        Assert.Contains("Current space", rows[0]);
        Assert.Contains("summary row", rows);
        Assert.Contains(rows, row => row.Contains("inventory-space cells are rendered by the SadConsole renderer"));
        Assert.Contains(rows, row => row.Contains("layers: backdrop"));
    }

    [Fact]
    public void InventorySpaceRenderProfilesControlRequiredSizeAndDebugRows()
    {
        var screen = ConsumerPlayModeScreen.FromSession(DemoEntry(), PlayableScenarioLauncher.CreatePrototype());
        Assert.NotNull(screen.CurrentSpaceView);
        var view = screen.CurrentSpaceView!;
        var debugRows = new[] { "plane", "size" };

        var bare = new InventorySpaceComponent(
            "bare",
            "Bare",
            SadConsoleRect.FromSize(0, 0, 10, 10),
            view,
            debugRows,
            options: InventorySpaceRenderOptions.Bare);
        var labeled = new InventorySpaceComponent(
            "labeled",
            "Labeled",
            SadConsoleRect.FromSize(0, 0, 10, 10),
            view,
            debugRows,
            options: InventorySpaceRenderOptions.Labeled);
        var framedDebug = new InventorySpaceComponent(
            "framed-debug",
            "Framed debug",
            SadConsoleRect.FromSize(0, 0, 20, 20),
            view,
            debugRows,
            options: InventorySpaceRenderOptions.FramedDebug);

        Assert.False(bare.Options.ShowFrame);
        Assert.False(bare.Options.ShowRowLabels);
        Assert.DoesNotContain(bare.RenderRows(GameGameGame.SadConsoleApp.Ui.Styling.SadConsoleTheme.Default), row => row.Contains("plane"));
        Assert.Equal(view.Viewport.Height, bare.RequiredHeight);
        Assert.Equal(view.Viewport.Width, bare.RequiredWidth);

        Assert.True(labeled.Options.ShowRowLabels);
        Assert.True(labeled.Options.ShowColumnLabels);
        Assert.Equal(view.Viewport.Height + 1, labeled.RequiredHeight);
        Assert.Equal(view.Viewport.Width + 4, labeled.RequiredWidth);

        Assert.True(framedDebug.Options.ShowFrame);
        Assert.True(framedDebug.Options.ShowDebugRows);
        Assert.Contains("plane", framedDebug.RenderRows(GameGameGame.SadConsoleApp.Ui.Styling.SadConsoleTheme.Default));
        Assert.True(framedDebug.RequiredHeight > labeled.RequiredHeight);
        Assert.True(framedDebug.RequiredWidth > labeled.RequiredWidth);
    }

    private static ScenarioCatalogEntry DemoEntry() => new("prototype", "prototype", "Prototype", "Prototype session");
}
