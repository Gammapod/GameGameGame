using SadRogue.Primitives;

namespace GameGameGame.SadConsoleApp.Ui.Tiles;

internal readonly record struct TileLayoutRect(int Left, int Top, int Width, int Height)
{
    public int RightExclusive => Left + Width;
    public int BottomExclusive => Top + Height;

    public static TileLayoutRect FromParentCells(
        int left,
        int top,
        int width,
        int height,
        Point parentCellSize,
        Point tileSize)
    {
        if (parentCellSize.X <= 0 || parentCellSize.Y <= 0) throw new ArgumentOutOfRangeException(nameof(parentCellSize), "Parent cell size must be positive.");
        if (tileSize.X <= 0 || tileSize.Y <= 0) throw new ArgumentOutOfRangeException(nameof(tileSize), "Tile size must be positive.");

        return new TileLayoutRect(
            left,
            top,
            Math.Max(1, width * parentCellSize.X / tileSize.X),
            Math.Max(1, height * parentCellSize.Y / tileSize.Y));
    }
}

internal readonly record struct TilesetRenderMetrics(Point ParentCellSize, Point TileSize)
{
    public static TilesetRenderMetrics FromSurfaces(SadConsole.Console parent, SadConsole.IFont font)
    {
        var parentCellSize = new Point(
            Math.Max(1, parent.WidthPixels / Math.Max(1, parent.Width)),
            Math.Max(1, parent.HeightPixels / Math.Max(1, parent.Height)));
        var tileSize = new Point(Math.Max(1, font.GlyphWidth), Math.Max(1, font.GlyphHeight));
        return new TilesetRenderMetrics(parentCellSize, tileSize);
    }

    public Point ParentCellToPixelPosition(int left, int top) => new(left * ParentCellSize.X, top * ParentCellSize.Y);

    public TileLayoutRect ParentCellRectToTileRect(GameGameGame.SadConsoleApp.SadConsoleRect bounds) =>
        TileLayoutRect.FromParentCells(bounds.Left, bounds.Top, bounds.Width, bounds.Height, ParentCellSize, TileSize);
}
