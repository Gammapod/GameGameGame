using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Components;

namespace GameGameGame.SadConsole.Tests;

public sealed class ConnectorLineViewModelTests
{
    [Fact]
    public void ConnectorLineViewModelPreservesPresentationEndpointsWithoutSimulationData()
    {
        var start = new ConnectorLineEndpoint("parent-cell", 2, 3);
        var end = new ConnectorLineEndpoint("child-node", 9, 6);
        var segment = new ConnectorLineSegment("link", start, end, PresentationColor.Cyan, Layer: 4);
        var view = new ConnectorLineViewModel("view", "Linked spaces", [segment], ConnectorLineFallbackGlyphs.Ascii);

        Assert.Equal("parent-cell", view.Segments.Single().Start.Id);
        Assert.Equal(2, view.Segments.Single().Start.CellX);
        Assert.Equal(3, view.Segments.Single().Start.CellY);
        Assert.Equal("child-node", view.Segments.Single().End.Id);
        Assert.Equal(0.5f, view.Segments.Single().Start.AnchorX);
        Assert.Equal(0.5f, view.Segments.Single().End.AnchorY);
        Assert.Equal(4, view.Segments.Single().Layer);
    }

    [Fact]
    public void ConnectorLineSegmentBuildsDeterministicHorizontalThenVerticalTileFallback()
    {
        var segment = new ConnectorLineSegment(
            "link",
            new ConnectorLineEndpoint("start", 1, 2),
            new ConnectorLineEndpoint("end", 4, 5),
            PresentationColor.Yellow,
            Layer: 2);

        var fallback = segment.FallbackTileSegments(ConnectorLineFallbackGlyphs.Ascii);

        Assert.Collection(
            fallback,
            horizontal =>
            {
                Assert.Equal((1, 2, 4, 2, (int)'-'), (horizontal.StartCellX, horizontal.StartCellY, horizontal.EndCellX, horizontal.EndCellY, horizontal.Glyph));
                Assert.Equal(PresentationColor.Yellow, horizontal.Color);
                Assert.Equal(2, horizontal.Layer);
            },
            vertical => Assert.Equal((4, 2, 4, 5, (int)'|'), (vertical.StartCellX, vertical.StartCellY, vertical.EndCellX, vertical.EndCellY, vertical.Glyph)),
            junction => Assert.Equal((4, 2, 4, 2, (int)'+'), (junction.StartCellX, junction.StartCellY, junction.EndCellX, junction.EndCellY, junction.Glyph)));
    }

    [Fact]
    public void ConnectorLineSegmentUsesSingleFallbackSegmentForStraightOrZeroLengthLines()
    {
        var straight = new ConnectorLineSegment(
            "vertical",
            new ConnectorLineEndpoint("start", 3, 1),
            new ConnectorLineEndpoint("end", 3, 4),
            PresentationColor.Cyan);
        var zero = new ConnectorLineSegment(
            "zero",
            new ConnectorLineEndpoint("same", 7, 7),
            new ConnectorLineEndpoint("same", 7, 7),
            PresentationColor.White);

        var straightFallback = straight.FallbackTileSegments(ConnectorLineFallbackGlyphs.Ascii);
        var zeroFallback = zero.FallbackTileSegments(ConnectorLineFallbackGlyphs.Ascii);

        Assert.Single(straightFallback);
        Assert.Equal((3, 1, 3, 4, (int)'|'), (straightFallback[0].StartCellX, straightFallback[0].StartCellY, straightFallback[0].EndCellX, straightFallback[0].EndCellY, straightFallback[0].Glyph));
        Assert.Single(zeroFallback);
        Assert.Equal((7, 7, 7, 7, (int)'+'), (zeroFallback[0].StartCellX, zeroFallback[0].StartCellY, zeroFallback[0].EndCellX, zeroFallback[0].EndCellY, zeroFallback[0].Glyph));
    }
}
