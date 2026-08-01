using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;

namespace GameGameGame.SadConsole.Tests;

public sealed class InventorySpacePresentationGeometryTests
{
    [Fact]
    public void GeometryResolvesProfiledCellAndEntityPixelBoundsFromComponentBounds()
    {
        var component = Component(InventorySpaceRelationshipTier.CurrentLocation, SadConsoleRect.FromSize(10, 5, 20, 12));

        var geometry = InventorySpacePresentationGeometry.FromComponent(component, rootCellWidthPixels: 16, rootCellHeightPixels: 16);

        Assert.Equal("space", geometry.ComponentId);
        Assert.Equal(InventorySpaceZoom.Huge32, geometry.Profile.SpaceZoom);
        Assert.Equal(new PixelRect(160, 80, 320, 192), geometry.SpacePixelBounds);
        Assert.Equal(new PixelRect(160, 80, 32, 32), geometry.CellPixelBounds(new GridCoord(0, 0)));
        Assert.Equal(new PixelRect(224, 112, 32, 32), geometry.CellPixelBounds(new GridCoord(2, 1)));
        Assert.Equal(new PixelPoint(240, 128), geometry.CellCenter(new GridCoord(2, 1)));
        Assert.Equal(new PixelPoint(240, 128), geometry.EntityCenter(new EntityId("box")));
    }

    [Fact]
    public void GeometryIncludesPlayerInventoryOnePixelGapInHitRegions()
    {
        var component = Component(InventorySpaceRelationshipTier.PlayerInventory, SadConsoleRect.FromSize(2, 3, 12, 8));

        var geometry = InventorySpacePresentationGeometry.FromComponent(component, rootCellWidthPixels: 16, rootCellHeightPixels: 16);

        var first = geometry.CellPixelBounds(new GridCoord(0, 0));
        var second = geometry.CellPixelBounds(new GridCoord(1, 0));

        Assert.Equal(24, first.Width);
        Assert.Equal(first.Right + 1, second.Left);
        Assert.Null(geometry.HitTest(first.Right, first.Top));
        Assert.Equal(new GridCoord(1, 0), geometry.HitTest(second.Left, second.Top)?.Coord);
    }

    [Fact]
    public void PixelRectIntersectionSupportsOverlayOcclusionChecks()
    {
        var cell = new PixelRect(32, 48, 24, 24);

        Assert.True(cell.Intersects(new PixelRect(55, 60, 10, 10)));
        Assert.True(cell.Intersects(new PixelRect(32, 48, 24, 24)));
        Assert.False(cell.Intersects(new PixelRect(56, 48, 10, 10)));
        Assert.False(cell.Intersects(new PixelRect(0, 0, 10, 10)));
    }

    [Fact]
    public void GeometryAccountsForFrameLabelsAndDebugRowsWhenResolvingGridOrigin()
    {
        var component = new InventorySpaceComponent(
            "space",
            "Space",
            SadConsoleRect.FromSize(4, 6, 20, 20),
            View(),
            ["debug"],
            options: InventorySpaceRenderOptions.FramedDebug,
            displayProfile: InventorySpaceDisplayProfile.ForRelationshipTier(InventorySpaceRelationshipTier.ImmediateParent));

        var geometry = InventorySpacePresentationGeometry.FromComponent(component, 16, 16);

        Assert.Equal(new PixelRect((4 + 1 + 4) * 16, (6 + 1 + 2 + 1) * 16, 16, 16), geometry.CellPixelBounds(new GridCoord(0, 0)));
    }

    [Fact]
    public void RegistryProvidesSharedHitTestingAcrossSpaces()
    {
        var registry = new InventorySpacePresentationGeometryRegistry();
        var current = registry.Register(Component(InventorySpaceRelationshipTier.CurrentLocation, SadConsoleRect.FromSize(1, 1, 20, 12)), 16, 16);
        registry.Register(Component(InventorySpaceRelationshipTier.PlayerInventory, SadConsoleRect.FromSize(30, 20, 12, 8), id: "inventory"), 16, 16);

        var boxCenter = current.EntityCenter(new EntityId("box"))!.Value;
        var hit = registry.HitTest(boxCenter.X, boxCenter.Y);

        Assert.Equal("space", hit?.ComponentId);
        Assert.Equal(new EntityId("box"), hit?.EntityId);
        Assert.Equal("Box", hit?.DisplayName);
        Assert.NotNull(registry.ForComponent("inventory"));
    }

    private static InventorySpaceComponent Component(InventorySpaceRelationshipTier tier, SadConsoleRect bounds, string id = "space") =>
        new(
            id,
            "Space",
            bounds,
            View(),
            options: InventorySpaceRenderOptions.Bare,
            displayProfile: InventorySpaceDisplayProfile.ForRelationshipTier(tier));

    private static InventorySpaceViewModel View() => new(
        "view",
        "Space",
        new PlaneId("plane"),
        Width: 3,
        Height: 2,
        InventorySpaceCellMetrics.Default,
        InventorySpaceViewport.Full(3, 2),
        new InventorySpaceBackdropLayer(new InventorySpaceVisualLayer(160, PresentationColor.Gray, ForegroundRgb: 0x808080, BackgroundRgb: 0x202020)),
        [
            new InventorySpaceEntityVisual(new GridCoord(0, 0), new EntityId("actor"), new InventorySpaceVisualLayer('@', PresentationColor.Yellow), Accent: null, InventorySpaceVisualPlacement.Default, DisplayName: "Actor"),
            new InventorySpaceEntityVisual(new GridCoord(2, 1), new EntityId("box"), new InventorySpaceVisualLayer('B', PresentationColor.Earth), Accent: null, InventorySpaceVisualPlacement.Default, DisplayName: "Box")
        ],
        [],
        new InventorySpaceFrame(Visible: false, Title: "Space", Color: PresentationColor.Yellow));
}
