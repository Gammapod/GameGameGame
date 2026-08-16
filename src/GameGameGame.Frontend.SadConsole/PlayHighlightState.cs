using GameGameGame.Core;

namespace GameGameGame.Frontend.SadConsole;

internal sealed record PlayHighlightState(GridCoord Coord, CellHighlightKind Kind)
{
    public CellHighlightPresentation Presentation(TilesetProfile tilesetProfile) => Kind == CellHighlightKind.EntityTarget
        ? CellHighlightPresentation.EntityTarget(tilesetProfile)
        : CellHighlightPresentation.MovePreview(tilesetProfile);
}
