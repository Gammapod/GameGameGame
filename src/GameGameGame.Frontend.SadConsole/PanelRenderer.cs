using SadRogue.Primitives;

namespace GameGameGame.Frontend.SadConsole;

internal static class PanelRenderer
{
    public static void DrawPanel(
        global::SadConsole.Console target,
        FrontendRect bounds,
        TileBorderGlyphSet border,
        Color foreground,
        Color background)
    {
        if (bounds.Width < 2 || bounds.Height < 2)
        {
            return;
        }

        for (var x = bounds.X; x <= bounds.Right; x++)
        {
            SetGlyph(target, x, bounds.Y, x == bounds.X ? border.TopLeft : x == bounds.Right ? border.TopRight : border.Horizontal, foreground, background);
            SetGlyph(target, x, bounds.Bottom, x == bounds.X ? border.BottomLeft : x == bounds.Right ? border.BottomRight : border.Horizontal, foreground, background);
        }

        for (var y = bounds.Y + 1; y < bounds.Bottom; y++)
        {
            SetGlyph(target, bounds.X, y, border.Vertical, foreground, background);
            SetGlyph(target, bounds.Right, y, border.Vertical, foreground, background);
        }
    }

    private static void SetGlyph(global::SadConsole.Console target, int x, int y, int glyph, Color foreground, Color background)
    {
        if (x < 0 || y < 0 || x >= target.Width || y >= target.Height)
        {
            return;
        }

        target.Surface[x, y].Glyph = glyph;
        target.Surface[x, y].Foreground = foreground;
        target.Surface[x, y].Background = background;
    }
}
