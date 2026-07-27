using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Styling;

namespace GameGameGame.SadConsoleApp.Ui.Components;

internal sealed record LinkedInventorySpaceLayout(
    SadConsoleRect DrawableBounds,
    IReadOnlyList<LinkedInventorySpaceNode> Nodes,
    ConnectorLineViewModel? Connector,
    SadConsoleRect? ParentCellBounds,
    IReadOnlyList<LinkedInventorySpaceHitRegion> HitRegions,
    LinkedInventorySpaceLayoutStatus Status)
{
    private const int DefaultGap = 3;

    public static LinkedInventorySpaceLayout Resolve(
        SadConsoleRect drawableBounds,
        InventorySpaceComponent parentSizing,
        InventorySpaceComponent? childSizing,
        GridCoord? parentEntityCoord,
        int gap = DefaultGap)
    {
        var parentWidth = Math.Min(parentSizing.RequiredWidth, drawableBounds.Width);
        var parentHeight = Math.Min(parentSizing.RequiredHeight, drawableBounds.Height);
        var parentOnly = childSizing is null || parentEntityCoord is null;
        if (parentOnly)
        {
            var parentBounds = Centered(drawableBounds, parentWidth, parentHeight);
            var singleNode = new LinkedInventorySpaceNode("current-place", LinkedInventorySpaceNodeRole.CurrentPlace, parentBounds, IsClipped(parentSizing, parentBounds));
            return new LinkedInventorySpaceLayout(
                drawableBounds,
                [singleNode],
                Connector: null,
                ParentCellBounds: null,
                HitRegions: [LinkedInventorySpaceHitRegion.Node(singleNode)],
                singleNode.IsClipped ? LinkedInventorySpaceLayoutStatus.Clipped : LinkedInventorySpaceLayoutStatus.SingleNode);
        }

        var child = childSizing!;
        var parentCoord = parentEntityCoord!.Value;
        var childWidth = Math.Min(child.RequiredWidth, drawableBounds.Width);
        var childHeight = Math.Min(child.RequiredHeight, drawableBounds.Height);
        var totalWidth = parentWidth + gap + childWidth;
        var bothFit = totalWidth <= drawableBounds.Width && Math.Max(parentHeight, childHeight) <= drawableBounds.Height;
        if (!bothFit)
        {
            var parentBounds = Centered(drawableBounds, parentWidth, parentHeight);
            var omittedChildNode = new LinkedInventorySpaceNode("current-place", LinkedInventorySpaceNodeRole.CurrentPlace, parentBounds, IsClipped(parentSizing, parentBounds));
            return new LinkedInventorySpaceLayout(
                drawableBounds,
                [omittedChildNode],
                Connector: null,
                ParentCellBounds: null,
                HitRegions: [LinkedInventorySpaceHitRegion.Node(omittedChildNode)],
                omittedChildNode.IsClipped ? LinkedInventorySpaceLayoutStatus.Clipped : LinkedInventorySpaceLayoutStatus.ChildOmitted);
        }

        var left = drawableBounds.Left + Math.Max(0, (drawableBounds.Width - totalWidth) / 2);
        var parentTop = drawableBounds.Top + Math.Max(0, (drawableBounds.Height - parentHeight) / 2);
        var childTop = drawableBounds.Top + Math.Max(0, (drawableBounds.Height - childHeight) / 2);
        var parentNodeBounds = SadConsoleRect.FromSize(left, parentTop, parentWidth, parentHeight);
        var childNodeBounds = SadConsoleRect.FromSize(left + parentWidth + gap, childTop, childWidth, childHeight);
        var parentNode = new LinkedInventorySpaceNode("current-place", LinkedInventorySpaceNodeRole.CurrentPlace, parentNodeBounds, IsClipped(parentSizing, parentNodeBounds));
        var childNode = new LinkedInventorySpaceNode("linked-inspected-space", LinkedInventorySpaceNodeRole.LinkedInspectedSpace, childNodeBounds, IsClipped(child, childNodeBounds));
        var parentComponent = Rebound(parentSizing, parentNodeBounds);
        var parentCell = parentComponent.CellBounds(parentCoord);
        var parentAnchor = CenterOf(parentCell);
        var childAnchor = new ConnectorLineEndpoint("linked-inspected-space-node-left-edge", childNodeBounds.Left, childNodeBounds.Top + (childNodeBounds.Height / 2), AnchorX: 0f, AnchorY: 0.5f);
        var connector = new ConnectorLineViewModel(
            "linked-inventory-space.connector",
            "Current place to inspected child",
            [new ConnectorLineSegment("current-place-to-linked-inspected-space", parentAnchor, childAnchor, PresentationColor.Cyan, Layer: 1)],
            ConnectorLineFallbackGlyphs.Ascii);

        return new LinkedInventorySpaceLayout(
            drawableBounds,
            [parentNode, childNode],
            connector,
            parentCell,
            [LinkedInventorySpaceHitRegion.Node(parentNode), LinkedInventorySpaceHitRegion.Node(childNode), new LinkedInventorySpaceHitRegion("parent-cell", parentCell, LinkedInventorySpaceHitRegionKind.InventoryCell, parentNode.Id)],
            parentNode.IsClipped || childNode.IsClipped ? LinkedInventorySpaceLayoutStatus.Clipped : LinkedInventorySpaceLayoutStatus.LinkedTwoSpace);
    }

    private static SadConsoleRect Centered(SadConsoleRect bounds, int width, int height) =>
        SadConsoleRect.FromSize(
            bounds.Left + Math.Max(0, (bounds.Width - width) / 2),
            bounds.Top + Math.Max(0, (bounds.Height - height) / 2),
            width,
            height);

    private static bool IsClipped(InventorySpaceComponent sizing, SadConsoleRect bounds) =>
        bounds.Width < sizing.RequiredWidth || bounds.Height < sizing.RequiredHeight;

    private static InventorySpaceComponent Rebound(InventorySpaceComponent component, SadConsoleRect bounds) =>
        new(component.Id, component.Title, bounds, component.View, component.BodyRows, component.State, component.Options);

    private static ConnectorLineEndpoint CenterOf(SadConsoleRect bounds) =>
        new("parent-entity-cell", bounds.Left + (bounds.Width / 2), bounds.Top + (bounds.Height / 2));
}

internal sealed record LinkedInventorySpaceNode(
    string Id,
    LinkedInventorySpaceNodeRole Role,
    SadConsoleRect Bounds,
    bool IsClipped);

internal enum LinkedInventorySpaceNodeRole
{
    CurrentPlace,
    LinkedInspectedSpace
}

internal enum LinkedInventorySpaceLayoutStatus
{
    SingleNode,
    LinkedTwoSpace,
    ChildOmitted,
    Clipped
}

internal sealed record LinkedInventorySpaceHitRegion(
    string Id,
    SadConsoleRect Bounds,
    LinkedInventorySpaceHitRegionKind Kind,
    string? NodeId = null)
{
    public static LinkedInventorySpaceHitRegion Node(LinkedInventorySpaceNode node) =>
        new(node.Id, node.Bounds, LinkedInventorySpaceHitRegionKind.Node, node.Id);
}

internal enum LinkedInventorySpaceHitRegionKind
{
    Node,
    InventoryCell
}
