using SadRogue.Primitives;

namespace GameGameGame.Frontend.SadConsole;

internal enum CellHighlightKind
{
    MovePreview,
    EntityTarget
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

    public static CellHighlightPresentation EntityTarget(TilesetProfile tilesetProfile) => new(
        CellHighlightKind.EntityTarget,
        tilesetProfile.Roles.EntityHighlight,
        new Color((byte)180, (byte)80, (byte)255, (byte)180));
}
