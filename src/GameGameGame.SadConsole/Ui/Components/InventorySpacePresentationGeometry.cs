using GameGameGame.Core;

namespace GameGameGame.SadConsoleApp.Ui.Components;

internal readonly record struct PixelPoint(int X, int Y);

internal readonly record struct PixelRect(int Left, int Top, int Width, int Height)
{
    public int Right => Left + Width;
    public int Bottom => Top + Height;
    public PixelPoint Center => new(Left + (Width / 2), Top + (Height / 2));

    public bool Contains(int x, int y) => x >= Left && x < Right && y >= Top && y < Bottom;

    public bool Intersects(PixelRect other) =>
        Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;
}

internal sealed record InventorySpaceEntityHitRegion(EntityId EntityId, GridCoord Coord, PixelRect Bounds, string? DisplayName);

internal sealed record InventorySpaceHitTestResult(string ComponentId, GridCoord Coord, EntityId? EntityId, string? DisplayName, PixelRect Bounds);

internal sealed record InventorySpacePresentationGeometry(
    string ComponentId,
    InventorySpaceDisplayProfile Profile,
    SadConsoleRect RootCellBounds,
    PixelRect SpacePixelBounds,
    PixelPoint GridOriginPixels,
    int RootCellWidthPixels,
    int RootCellHeightPixels,
    IReadOnlyList<InventorySpaceEntityHitRegion> EntityHitRegions)
{
    public PixelRect CellPixelBounds(GridCoord coord)
    {
        var localX = coord.X - _viewportOrigin.X;
        var localY = coord.Y - _viewportOrigin.Y;
        return new PixelRect(
            GridOriginPixels.X + localX * (Profile.CellPixelSize + Profile.CellGapPixels),
            GridOriginPixels.Y + localY * (Profile.CellPixelSize + Profile.CellGapPixels),
            Profile.CellPixelSize,
            Profile.CellPixelSize);
    }

    public PixelPoint CellCenter(GridCoord coord) => CellPixelBounds(coord).Center;

    public PixelPoint? EntityCenter(EntityId entityId) =>
        EntityHitRegions.FirstOrDefault(region => region.EntityId == entityId)?.Bounds.Center;

    public InventorySpaceHitTestResult? HitTest(int pixelX, int pixelY)
    {
        foreach (var entity in EntityHitRegions)
        {
            if (entity.Bounds.Contains(pixelX, pixelY))
            {
                return new InventorySpaceHitTestResult(ComponentId, entity.Coord, entity.EntityId, entity.DisplayName, entity.Bounds);
            }
        }

        foreach (var coord in _visibleCoords)
        {
            var bounds = CellPixelBounds(coord);
            if (bounds.Contains(pixelX, pixelY))
            {
                return new InventorySpaceHitTestResult(ComponentId, coord, EntityId: null, DisplayName: null, bounds);
            }
        }

        return null;
    }

    private readonly GridCoord _viewportOrigin = default;
    private readonly IReadOnlyList<GridCoord> _visibleCoords = [];

    private InventorySpacePresentationGeometry(
        string componentId,
        InventorySpaceDisplayProfile profile,
        SadConsoleRect rootCellBounds,
        PixelRect spacePixelBounds,
        PixelPoint gridOriginPixels,
        int rootCellWidthPixels,
        int rootCellHeightPixels,
        IReadOnlyList<InventorySpaceEntityHitRegion> entityHitRegions,
        GridCoord viewportOrigin,
        IReadOnlyList<GridCoord> visibleCoords)
        : this(componentId, profile, rootCellBounds, spacePixelBounds, gridOriginPixels, rootCellWidthPixels, rootCellHeightPixels, entityHitRegions)
    {
        _viewportOrigin = viewportOrigin;
        _visibleCoords = visibleCoords;
    }

    public static InventorySpacePresentationGeometry FromComponent(
        InventorySpaceComponent component,
        int rootCellWidthPixels,
        int rootCellHeightPixels)
    {
        var profile = component.DisplayProfile ?? InventorySpaceDisplayProfile.ForRelationshipTier(InventorySpaceRelationshipTier.ImmediateParent);
        var gridOriginRootCells = GridOriginRootCells(component);
        var gridOriginPixels = new PixelPoint(
            gridOriginRootCells.Left * rootCellWidthPixels,
            gridOriginRootCells.Top * rootCellHeightPixels);
        var visibleCoords = component.View.VisibleCoords();
        var spacePixelBounds = new PixelRect(
            component.Bounds.Left * rootCellWidthPixels,
            component.Bounds.Top * rootCellHeightPixels,
            component.Bounds.Width * rootCellWidthPixels,
            component.Bounds.Height * rootCellHeightPixels);

        PixelRect CellBounds(GridCoord coord)
        {
            var localX = coord.X - component.View.Viewport.Origin.X;
            var localY = coord.Y - component.View.Viewport.Origin.Y;
            return new PixelRect(
                gridOriginPixels.X + localX * (profile.CellPixelSize + profile.CellGapPixels),
                gridOriginPixels.Y + localY * (profile.CellPixelSize + profile.CellGapPixels),
                profile.CellPixelSize,
                profile.CellPixelSize);
        }

        var entityRegions = component.View.Entities
            .Where(entity => component.View.IsVisible(entity.Coord))
            .Select(entity => new InventorySpaceEntityHitRegion(entity.EntityId, entity.Coord, CellBounds(entity.Coord), entity.DisplayName))
            .ToList();

        return new InventorySpacePresentationGeometry(
            component.Id,
            profile,
            component.Bounds,
            spacePixelBounds,
            gridOriginPixels,
            rootCellWidthPixels,
            rootCellHeightPixels,
            entityRegions,
            component.View.Viewport.Origin,
            visibleCoords);
    }

    private static SadConsoleRect GridOriginRootCells(InventorySpaceComponent component)
    {
        var frameLeft = component.Options.ShowFrame ? 1 : 0;
        var frameTop = component.Options.ShowFrame ? 1 : 0;
        var titleRows = component.Options.ShowTitle && !component.Options.ShowFrame ? 1 : 0;
        var debugRows = component.Options.ShowDebugRows ? component.BodyRows.Count + 1 : 0;
        var columnRows = component.Options.ShowColumnLabels ? 1 : 0;
        var rowColumns = component.Options.ShowRowLabels ? 4 : 0;
        return SadConsoleRect.FromSize(
            component.Bounds.Left + frameLeft + rowColumns,
            component.Bounds.Top + frameTop + titleRows + debugRows + columnRows,
            0,
            0);
    }
}

internal sealed class InventorySpacePresentationGeometryRegistry
{
    private readonly Dictionary<string, InventorySpacePresentationGeometry> _byComponentId = [];

    public IReadOnlyCollection<InventorySpacePresentationGeometry> Geometries => _byComponentId.Values;

    public void Clear() => _byComponentId.Clear();

    public InventorySpacePresentationGeometry Register(InventorySpaceComponent component, int rootCellWidthPixels, int rootCellHeightPixels)
    {
        var geometry = InventorySpacePresentationGeometry.FromComponent(component, rootCellWidthPixels, rootCellHeightPixels);
        _byComponentId[component.Id] = geometry;
        return geometry;
    }

    public InventorySpacePresentationGeometry? ForComponent(string componentId) =>
        _byComponentId.GetValueOrDefault(componentId);

    public InventorySpaceHitTestResult? HitTest(int pixelX, int pixelY) =>
        _byComponentId.Values
            .Select(geometry => geometry.HitTest(pixelX, pixelY))
            .FirstOrDefault(hit => hit is not null);
}
