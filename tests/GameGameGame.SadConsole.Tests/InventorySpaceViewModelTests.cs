using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Screens;

namespace GameGameGame.SadConsole.Tests;

public sealed class InventorySpaceViewModelTests
{
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
        Assert.Equal(160, view.Backdrop.Tile.Glyph);
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
            InventorySpaceRenderOptions.FramedDebug);

        var rows = component.RenderRows(GameGameGame.SadConsoleApp.Ui.Styling.SadConsoleTheme.Default);

        Assert.Same(view, component.View);
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
