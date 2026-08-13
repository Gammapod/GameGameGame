using SadRogue.Primitives;

namespace GameGameGame.Frontend.SadConsole;

internal sealed record OverlayPanelGeometry(
    FrontendRect CellBounds,
    int PixelX,
    int PixelY,
    int PixelOffsetX,
    int PixelOffsetY)
{
    public static OverlayPanelGeometry HalfTileOffset(FrontendRect cellBounds, SadConsoleDisplaySettings displaySettings) => new(
        cellBounds,
        cellBounds.X * displaySettings.ScaledTileWidth + displaySettings.ScaledTileWidth / 2,
        cellBounds.Y * displaySettings.ScaledTileHeight + displaySettings.ScaledTileHeight / 2,
        displaySettings.ScaledTileWidth / 2,
        displaySettings.ScaledTileHeight / 2);
}

internal sealed record OverlayPanelModel(
    OverlayPanelGeometry Geometry,
    IReadOnlyList<string> Rows,
    Color BorderColor,
    Color Foreground,
    Color Background);

internal sealed class OverlayPanelConsole : global::SadConsole.Console
{
    private readonly OverlayPanelModel _model;
    private readonly TilesetProfile _tilesetProfile;

    public OverlayPanelConsole(OverlayPanelModel model, TilesetProfile tilesetProfile)
        : base(model.Geometry.CellBounds.Width, model.Geometry.CellBounds.Height)
    {
        _model = model;
        _tilesetProfile = tilesetProfile;
        UsePixelPositioning = true;
        Position = new Point(model.Geometry.PixelX, model.Geometry.PixelY);
        UseKeyboard = false;
        UseMouse = false;
        Redraw();
    }

    private void Redraw()
    {
        FillInterior();
        PanelRenderer.DrawPanel(this, new FrontendRect(0, 0, Width, Height), _tilesetProfile.Roles.PanelBorder, _model.BorderColor, _model.Background);

        var width = Math.Max(0, Width - 2);
        for (var index = 0; index < _model.Rows.Count && index + 1 < Height - 1; index++)
        {
            var row = _model.Rows[index];
            var color = row.Contains('>') ? Color.Cyan : _model.Foreground;
            PrintClipped(1, index + 1, width, row.PadRight(width), color, _model.Background);
        }

        Surface.IsDirty = true;
    }

    private void FillInterior()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                SetGlyph(x, y, _tilesetProfile.Blank, _model.Foreground, _model.Background);
            }
        }
    }

    private void PrintClipped(int x, int y, int width, string text, Color foreground, Color background)
    {
        var clipped = text.Length <= width ? text : text[..width];
        for (var index = 0; index < clipped.Length && x + index < Width; index++)
        {
            SetGlyph(x + index, y, _tilesetProfile.ResolveTextGlyph(clipped[index]), foreground, background);
        }
    }

    private void SetGlyph(int x, int y, int glyph, Color foreground, Color background)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return;
        }

        Surface[x, y].Glyph = glyph;
        Surface[x, y].Foreground = foreground;
        Surface[x, y].Background = background;
    }
}
