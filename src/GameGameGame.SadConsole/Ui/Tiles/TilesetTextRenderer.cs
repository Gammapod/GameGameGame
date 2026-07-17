using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace GameGameGame.SadConsoleApp.Ui.Tiles;

internal sealed class TilesetTextRenderer(TilesetProfile profile)
{
    public IReadOnlyList<int> ToGlyphs(string text) => text.Select(profile.ResolveTextGlyph).ToArray();

    public void Clear(Console target, Color foreground, Color background)
    {
        for (var y = 0; y < target.Height; y++)
        {
            for (var x = 0; x < target.Width; x++)
            {
                SetCell(target, x, y, profile.Blank, foreground, background);
            }
        }
    }

    public void Print(Console target, int x, int y, string text, Color foreground, Color background)
    {
        if (y < 0 || y >= target.Height || x >= target.Width) return;
        var glyphs = ToGlyphs(text);
        for (var index = 0; index < glyphs.Count && x + index < target.Width; index++)
        {
            SetCell(target, x + index, y, glyphs[index], foreground, background);
        }
    }

    public void PrintClipped(Console target, int x, int y, int width, string text, Color foreground, Color background)
    {
        if (width <= 0) return;
        var clipped = text.Length <= width ? text : text[..Math.Max(0, width - 1)];
        Print(target, x, y, clipped.PadRight(Math.Max(0, width)), foreground, background);
    }

    public void DrawBox(Console target, Color color, Color background, TileBorderGlyphSet glyphs)
    {
        var right = target.Width - 1;
        var bottom = target.Height - 1;
        for (var x = 0; x <= right; x++)
        {
            SetCell(target, x, 0, x == 0 ? glyphs.TopLeft : x == right ? glyphs.TopRight : glyphs.Horizontal, color, background);
            SetCell(target, x, bottom, x == 0 ? glyphs.BottomLeft : x == right ? glyphs.BottomRight : glyphs.Horizontal, color, background);
        }

        for (var y = 1; y < bottom; y++)
        {
            SetCell(target, 0, y, glyphs.Vertical, color, background);
            SetCell(target, right, y, glyphs.Vertical, color, background);
        }
    }

    public static void SetCell(Console target, int x, int y, int glyph, Color foreground, Color background)
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
