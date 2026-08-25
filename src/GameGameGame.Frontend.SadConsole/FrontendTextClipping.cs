namespace GameGameGame.Frontend.SadConsole;

internal static class FrontendTextClipping
{
    public static IReadOnlyList<int> ToClippedGlyphs(string text, int width, TilesetProfile tilesetProfile)
    {
        if (width <= 0 || string.IsNullOrEmpty(text)) return [];
        if (text.Length <= width)
        {
            return text.Select(tilesetProfile.ResolveTextGlyph).ToArray();
        }

        if (width == 1)
        {
            return [tilesetProfile.Roles.Ellipsis];
        }

        var glyphs = text[..(width - 1)].Select(tilesetProfile.ResolveTextGlyph).ToList();
        glyphs.Add(tilesetProfile.Roles.Ellipsis);
        return glyphs;
    }
}
