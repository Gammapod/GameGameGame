using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Styling;

namespace GameGameGame.SadConsoleApp.Ui.Components;

internal sealed record ConnectorLineViewModel(
    string Id,
    string Title,
    IReadOnlyList<ConnectorLineSegment> Segments,
    ConnectorLineFallbackGlyphs FallbackGlyphs)
{
    public IReadOnlyList<ConnectorLineTileSegment> FallbackTileSegments() =>
        Segments
            .SelectMany(segment => segment.FallbackTileSegments(FallbackGlyphs))
            .ToList();
}

internal sealed record ConnectorLineEndpoint(string Id, int CellX, int CellY, float AnchorX = 0.5f, float AnchorY = 0.5f);

internal sealed record ConnectorLineSegment(
    string Id,
    ConnectorLineEndpoint Start,
    ConnectorLineEndpoint End,
    PresentationColor Color,
    int Layer = 1)
{
    public IReadOnlyList<ConnectorLineTileSegment> FallbackTileSegments(ConnectorLineFallbackGlyphs glyphs)
    {
        if (Start.CellX == End.CellX && Start.CellY == End.CellY)
        {
            return [new ConnectorLineTileSegment(Id, Start.CellX, Start.CellY, Start.CellX, Start.CellY, glyphs.Junction, Color, Layer)];
        }

        var segments = new List<ConnectorLineTileSegment>();
        var cornerX = End.CellX;
        var cornerY = Start.CellY;

        if (Start.CellX != cornerX)
        {
            segments.Add(new ConnectorLineTileSegment(Id, Start.CellX, Start.CellY, cornerX, cornerY, glyphs.Horizontal, Color, Layer));
        }

        if (Start.CellY != End.CellY)
        {
            segments.Add(new ConnectorLineTileSegment(Id, cornerX, cornerY, End.CellX, End.CellY, glyphs.Vertical, Color, Layer));
        }

        if (segments.Count == 2)
        {
            segments.Add(new ConnectorLineTileSegment(Id, cornerX, cornerY, cornerX, cornerY, glyphs.Junction, Color, Layer));
        }

        return segments;
    }
}

internal sealed record ConnectorLineFallbackGlyphs(int Horizontal, int Vertical, int Junction)
{
    public static ConnectorLineFallbackGlyphs Ascii { get; } = new('-', '|', '+');
}

internal sealed record ConnectorLineTileSegment(
    string SegmentId,
    int StartCellX,
    int StartCellY,
    int EndCellX,
    int EndCellY,
    int Glyph,
    PresentationColor Color,
    int Layer);

internal sealed class ConnectorLineComponent : IUiComponent
{
    public ConnectorLineComponent(
        string id,
        string title,
        SadConsoleRect bounds,
        ConnectorLineViewModel view,
        UiComponentState state = UiComponentState.Unselected)
    {
        Id = id;
        Title = title;
        Bounds = bounds;
        View = view;
        State = state;
    }

    public string Id { get; }
    public string Title { get; }
    public SadConsoleRect Bounds { get; }
    public UiComponentState State { get; }
    public ConnectorLineViewModel View { get; }

    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme)
    {
        var fallback = View.FallbackTileSegments();
        return
        [
            $"[{State.BorderColor(theme)}] {Title}",
            "Accepted connector-line pattern.",
            "Endpoints derive from resolved cell geometry.",
            $"segments: {View.Segments.Count} smooth; {fallback.Count} tile fallback",
            $"fallback glyphs: {(char)View.FallbackGlyphs.Horizontal} {(char)View.FallbackGlyphs.Vertical} {(char)View.FallbackGlyphs.Junction}",
            "Layer: linked-space canvas, below prompts/debug."
        ];
    }
}
