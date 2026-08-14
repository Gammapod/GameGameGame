using SadRogue.Primitives;

namespace GameGameGame.Frontend.SadConsole;

internal enum CellHighlightKind
{
    MovePreview
}

internal sealed record CellHighlightPresentation(
    CellHighlightKind Kind,
    int Glyph,
    Color Foreground,
    global::SadConsole.Mirror Mirror = global::SadConsole.Mirror.None)
{
    public static CellHighlightPresentation MovePreview(TilesetProfile tilesetProfile) => new(
        CellHighlightKind.MovePreview,
        tilesetProfile.Roles.MoveHighlight,
        new Color((byte)0, (byte)255, (byte)255, (byte)160));
}
