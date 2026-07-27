using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;

namespace GameGameGame.SadConsole.Tests;

public sealed class LinkedInventorySpaceLayoutTests
{
    [Fact]
    public void LinkedInventorySpaceLayoutPlacesCurrentPlaceAndLinkedInspectedSpaceInsideDrawableBounds()
    {
        var drawable = SadConsoleRect.FromSize(1, 1, 80, 30);
        var parent = Component("parent", 5, 4);
        var child = Component("child", 3, 3);

        var layout = LinkedInventorySpaceLayout.Resolve(drawable, parent, child, new GridCoord(2, 1));

        Assert.Equal(LinkedInventorySpaceLayoutStatus.LinkedTwoSpace, layout.Status);
        Assert.Collection(
            layout.Nodes,
            current =>
            {
                Assert.Equal("current-place", current.Id);
                Assert.Equal(LinkedInventorySpaceNodeRole.CurrentPlace, current.Role);
                Assert.False(current.IsClipped);
                AssertInside(drawable, current.Bounds);
            },
            inspected =>
            {
                Assert.Equal("linked-inspected-space", inspected.Id);
                Assert.Equal(LinkedInventorySpaceNodeRole.LinkedInspectedSpace, inspected.Role);
                Assert.False(inspected.IsClipped);
                AssertInside(drawable, inspected.Bounds);
            });
        Assert.NotNull(layout.Connector);
        Assert.Equal(3, layout.Nodes[1].Bounds.Left - (layout.Nodes[0].Bounds.Left + layout.Nodes[0].Bounds.Width));
    }

    [Fact]
    public void LinkedInventorySpaceLayoutDerivesConnectorEndpointFromParentCellGeometry()
    {
        var drawable = SadConsoleRect.FromSize(1, 1, 80, 30);
        var parent = Component("parent", 5, 4);
        var child = Component("child", 3, 3);

        var layout = LinkedInventorySpaceLayout.Resolve(drawable, parent, child, new GridCoord(2, 1));

        var parentNode = layout.Nodes.Single(node => node.Role == LinkedInventorySpaceNodeRole.CurrentPlace);
        var reboundParent = Component("parent", 5, 4, parentNode.Bounds);
        var expectedCell = reboundParent.CellBounds(new GridCoord(2, 1));
        var connector = Assert.Single(layout.Connector!.Segments);

        Assert.Equal(expectedCell, layout.ParentCellBounds);
        Assert.Equal(expectedCell.Left, connector.Start.CellX);
        Assert.Equal(expectedCell.Top, connector.Start.CellY);
        Assert.Equal("parent-entity-cell", connector.Start.Id);
        Assert.Equal("linked-inspected-space-node-left-edge", connector.End.Id);
        Assert.Equal(0.5f, connector.Start.AnchorX);
        Assert.Equal(0.5f, connector.Start.AnchorY);
        Assert.Equal(0f, connector.End.AnchorX);
        Assert.Equal(0.5f, connector.End.AnchorY);
    }

    [Fact]
    public void LinkedInventorySpaceLayoutFallsBackToSingleNodeWhenNoChildIsSelected()
    {
        var drawable = SadConsoleRect.FromSize(1, 1, 40, 20);
        var parent = Component("parent", 5, 4);

        var layout = LinkedInventorySpaceLayout.Resolve(drawable, parent, childSizing: null, parentEntityCoord: null);

        Assert.Equal(LinkedInventorySpaceLayoutStatus.SingleNode, layout.Status);
        var node = Assert.Single(layout.Nodes);
        Assert.Equal(LinkedInventorySpaceNodeRole.CurrentPlace, node.Role);
        Assert.Null(layout.Connector);
        Assert.Null(layout.ParentCellBounds);
        Assert.Single(layout.HitRegions);
    }

    [Fact]
    public void LinkedInventorySpaceLayoutOmitsChildWhenTwoNodesCannotFitWithoutClipping()
    {
        var drawable = SadConsoleRect.FromSize(1, 1, 9, 8);
        var parent = Component("parent", 5, 4);
        var child = Component("child", 5, 4);

        var layout = LinkedInventorySpaceLayout.Resolve(drawable, parent, child, new GridCoord(0, 0));

        Assert.Equal(LinkedInventorySpaceLayoutStatus.ChildOmitted, layout.Status);
        Assert.Single(layout.Nodes);
        Assert.Null(layout.Connector);
        Assert.All(layout.Nodes, node => AssertInside(drawable, node.Bounds));
    }

    [Fact]
    public void LinkedInventorySpaceLayoutReportsClippedWhenParentCannotFitDrawable()
    {
        var drawable = SadConsoleRect.FromSize(1, 1, 3, 3);
        var parent = Component("parent", 5, 4);

        var layout = LinkedInventorySpaceLayout.Resolve(drawable, parent, childSizing: null, parentEntityCoord: null);

        Assert.Equal(LinkedInventorySpaceLayoutStatus.Clipped, layout.Status);
        var node = Assert.Single(layout.Nodes);
        Assert.True(node.IsClipped);
        Assert.Equal(3, node.Bounds.Width);
        Assert.Equal(3, node.Bounds.Height);
    }

    [Fact]
    public void LinkedInventorySpaceLayoutExposesNodeAndParentCellHitRegionsForFutureMouseInspection()
    {
        var drawable = SadConsoleRect.FromSize(1, 1, 80, 30);
        var layout = LinkedInventorySpaceLayout.Resolve(drawable, Component("parent", 5, 4), Component("child", 3, 3), new GridCoord(1, 1));

        Assert.Contains(layout.HitRegions, region => region.Id == "current-place" && region.Kind == LinkedInventorySpaceHitRegionKind.Node);
        Assert.Contains(layout.HitRegions, region => region.Id == "linked-inspected-space" && region.Kind == LinkedInventorySpaceHitRegionKind.Node);
        Assert.Contains(layout.HitRegions, region => region.Id == "parent-cell" && region.Kind == LinkedInventorySpaceHitRegionKind.InventoryCell && region.NodeId == "current-place");
    }

    private static InventorySpaceComponent Component(string id, int width, int height, SadConsoleRect? bounds = null)
    {
        var view = new InventorySpaceViewModel(
            $"{id}.view",
            id,
            new PlaneId($"{id}.plane"),
            width,
            height,
            InventorySpaceCellMetrics.Default,
            InventorySpaceViewport.Full(width, height),
            new InventorySpaceBackdropLayer(new InventorySpaceVisualLayer(160, PresentationColor.Gray)),
            [],
            [],
            new InventorySpaceFrame(Visible: false, Title: id, Color: PresentationColor.Yellow));

        return new InventorySpaceComponent(
            id,
            id,
            bounds ?? SadConsoleRect.FromSize(0, 0, 1, 1),
            view,
            options: InventorySpaceRenderOptions.Bare);
    }

    private static void AssertInside(SadConsoleRect outer, SadConsoleRect inner)
    {
        Assert.True(inner.Left >= outer.Left);
        Assert.True(inner.Top >= outer.Top);
        Assert.True(inner.Left + inner.Width <= outer.Left + outer.Width);
        Assert.True(inner.Bottom <= outer.Bottom);
    }
}
