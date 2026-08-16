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
    public void CandiiTilesetLoadsFacingRoleGlyphsAndMirrorsDirections()
    {
        var profile = TilesetProfileLoader.LoadCandii();

        Assert.Equal(251, profile.Roles.FacingDiag);
        Assert.Equal(252, profile.Roles.FacingNS);
        Assert.Equal(253, profile.Roles.FacingWE);
        Assert.Equal(218, profile.Roles.MoveHighlight);
        Assert.Equal((252, global::SadConsole.Mirror.None), profile.Roles.FacingGlyph(GameGameGame.Core.Direction.North));
        Assert.Equal((252, global::SadConsole.Mirror.Vertical), profile.Roles.FacingGlyph(GameGameGame.Core.Direction.South));
        Assert.Equal((253, global::SadConsole.Mirror.None), profile.Roles.FacingGlyph(GameGameGame.Core.Direction.East));
        Assert.Equal((253, global::SadConsole.Mirror.Horizontal), profile.Roles.FacingGlyph(GameGameGame.Core.Direction.West));
        Assert.Equal((251, global::SadConsole.Mirror.None), profile.Roles.FacingGlyph(GameGameGame.Core.Direction.NorthWest));
        Assert.Equal((251, global::SadConsole.Mirror.Horizontal), profile.Roles.FacingGlyph(GameGameGame.Core.Direction.NorthEast));
        Assert.Equal((251, global::SadConsole.Mirror.Vertical), profile.Roles.FacingGlyph(GameGameGame.Core.Direction.SouthWest));
        Assert.Equal((251, global::SadConsole.Mirror.Horizontal | global::SadConsole.Mirror.Vertical), profile.Roles.FacingGlyph(GameGameGame.Core.Direction.SouthEast));
    }

    [Fact]
    public void MovePreviewHighlightUsesMoveHighlightGlyphWithSemiTransparentForeground()
    {
        var profile = TilesetProfileLoader.LoadCandii();

        var highlight = CellHighlightPresentation.MovePreview(profile);

        Assert.Equal(CellHighlightKind.MovePreview, highlight.Kind);
        Assert.Equal(218, highlight.Glyph);
        Assert.Equal(global::SadConsole.Mirror.None, highlight.Mirror);
        Assert.Equal((byte)160, highlight.Foreground.A);
        Assert.Equal((byte)0, highlight.Foreground.R);
        Assert.Equal((byte)255, highlight.Foreground.G);
        Assert.Equal((byte)255, highlight.Foreground.B);
    }

    [Fact]
    public void EntityTargetHighlightUsesEntityHighlightGlyphWithPurpleForeground()
    {
        var profile = TilesetProfileLoader.LoadCandii();

        var highlight = CellHighlightPresentation.EntityTarget(profile);

        Assert.Equal(CellHighlightKind.EntityTarget, highlight.Kind);
        Assert.Equal(217, highlight.Glyph);
        Assert.Equal((byte)180, highlight.Foreground.R);
        Assert.Equal((byte)80, highlight.Foreground.G);
        Assert.Equal((byte)255, highlight.Foreground.B);
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
        Assert.Equal(156, profile.Roles.PanelBorder.HorizontalWithSouthVertical);
        Assert.Equal(155, profile.Roles.PanelBorder.HorizontalWithNorthVertical);
        Assert.Equal(157, profile.Roles.PanelBorder.VerticalWithEastHorizontal);
        Assert.Equal(159, profile.Roles.PanelBorder.VerticalWithWestHorizontal);
    }
}
