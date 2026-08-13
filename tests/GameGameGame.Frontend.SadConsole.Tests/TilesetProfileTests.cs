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

    [Fact]
    public void CandiiTilesetUsesGridDottedAsDefaultBackdrop()
    {
        var profile = TilesetProfileLoader.LoadCandii();

        Assert.Equal(223, profile.Roles.GridDotted);
        Assert.Equal(223, profile.Roles.DefaultBackdrop);
    }

    [Fact]
    public void CandiiTilesetLoadsManifestBackedPanelBorderRoles()
    {
        var profile = TilesetProfileLoader.LoadCandii();

        Assert.Equal(180, profile.Roles.PanelBorder.TopLeft);
        Assert.Equal(153, profile.Roles.PanelBorder.TopRight);
        Assert.Equal(154, profile.Roles.PanelBorder.BottomLeft);
        Assert.Equal(179, profile.Roles.PanelBorder.BottomRight);
        Assert.Equal(158, profile.Roles.PanelBorder.Horizontal);
        Assert.Equal(141, profile.Roles.PanelBorder.Vertical);
    }
}
