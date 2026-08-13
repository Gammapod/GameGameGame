using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class TilesetProfileTests
{
    [Fact]
    public void CandiiTilesetUsesConfiguredBlankGlyphForSpaces()
    {
        var profile = TilesetProfileLoader.LoadCandii();

        Assert.Equal(0, profile.Blank);
        Assert.Equal(profile.Blank, profile.ResolveTextGlyph(' '));
        Assert.Equal((int)'A', profile.ResolveTextGlyph('A'));
    }
}
