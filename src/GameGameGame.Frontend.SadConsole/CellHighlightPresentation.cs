using SadRogue.Primitives;

namespace GameGameGame.Frontend.SadConsole;

internal enum CellHighlightKind
{
    MovePreview,
    EntityTarget,
    Pickup,
    Drop,
    Enter,
    Exit,
    Transfer,
    NoAction
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

    public static CellHighlightPresentation Pickup(TilesetProfile tilesetProfile) => new(
        CellHighlightKind.Pickup,
        tilesetProfile.Roles.PickupHighlight,
        new Color((byte)80, (byte)255, (byte)120, (byte)180));

    public static CellHighlightPresentation Drop(TilesetProfile tilesetProfile) => new(
        CellHighlightKind.Drop,
        tilesetProfile.Roles.DropHighlight,
        new Color((byte)255, (byte)220, (byte)80, (byte)180));

    public static CellHighlightPresentation Enter(TilesetProfile tilesetProfile) => new(
        CellHighlightKind.Enter,
        tilesetProfile.Roles.EnterHighlight,
        new Color((byte)80, (byte)180, (byte)255, (byte)180));

    public static CellHighlightPresentation Exit(TilesetProfile tilesetProfile) => new(
        CellHighlightKind.Exit,
        tilesetProfile.Roles.ExitHighlight,
        new Color((byte)80, (byte)180, (byte)255, (byte)180));

    public static CellHighlightPresentation Transfer(TilesetProfile tilesetProfile) => new(
        CellHighlightKind.Transfer,
        tilesetProfile.Roles.TransferHighlight,
        new Color((byte)255, (byte)160, (byte)80, (byte)180));

    public static CellHighlightPresentation NoAction(TilesetProfile tilesetProfile) => new(
        CellHighlightKind.NoAction,
        tilesetProfile.Roles.NoActionHighlight,
        new Color((byte)160, (byte)160, (byte)160, (byte)180));
}
