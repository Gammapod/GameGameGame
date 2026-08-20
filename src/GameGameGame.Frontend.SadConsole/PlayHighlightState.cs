using GameGameGame.Core;

namespace GameGameGame.Frontend.SadConsole;

internal sealed record PlayHighlightState(GridCoord Coord, CellHighlightKind Kind)
{
    public CellHighlightPresentation Presentation(TilesetProfile tilesetProfile) => Kind switch
    {
        CellHighlightKind.MovePreview => CellHighlightPresentation.MovePreview(tilesetProfile),
        CellHighlightKind.EntityTarget => CellHighlightPresentation.EntityTarget(tilesetProfile),
        CellHighlightKind.Pickup => CellHighlightPresentation.Pickup(tilesetProfile),
        CellHighlightKind.Drop => CellHighlightPresentation.Drop(tilesetProfile),
        CellHighlightKind.Enter => CellHighlightPresentation.Enter(tilesetProfile),
        CellHighlightKind.Exit => CellHighlightPresentation.Exit(tilesetProfile),
        CellHighlightKind.Transfer => CellHighlightPresentation.Transfer(tilesetProfile),
        CellHighlightKind.NoAction => CellHighlightPresentation.NoAction(tilesetProfile),
        _ => CellHighlightPresentation.MovePreview(tilesetProfile)
    };
}
