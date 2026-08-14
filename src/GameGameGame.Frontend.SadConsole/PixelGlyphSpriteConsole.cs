using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace GameGameGame.Frontend.SadConsole;

internal sealed class PixelGlyphSpriteConsole : Console
{
    public PixelGlyphSpriteConsole(int glyph, Color foreground, Color background)
        : base(1, 1)
    {
        UsePixelPositioning = true;
        UseKeyboard = false;
        UseMouse = false;
        SetGlyph(glyph, foreground, background);
    }

    public void SetGlyph(int glyph, Color foreground, Color background)
    {
        Surface[0, 0].Glyph = glyph;
        Surface[0, 0].Foreground = foreground;
        Surface[0, 0].Background = background;
        Surface.IsDirty = true;
    }
}
