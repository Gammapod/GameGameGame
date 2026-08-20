using GameGameGame.Core;

namespace GameGameGame.Frontend.SadConsole;

internal enum PlaySelectionFrameKind
{
    AdjacentSelection,
    ActionSelection,
    CellSelection
}

internal sealed record PlaySelectionFrame(PlaySelectionFrameKind Kind, GridCoord? AdjacentCoord = null)
{
    public static PlaySelectionFrame Adjacent { get; } = new(PlaySelectionFrameKind.AdjacentSelection);
    public static PlaySelectionFrame Action(GridCoord adjacentCoord) => new(PlaySelectionFrameKind.ActionSelection, adjacentCoord);
    public static PlaySelectionFrame Cell(GridCoord adjacentCoord) => new(PlaySelectionFrameKind.CellSelection, adjacentCoord);
}

internal sealed class PlaySelectionStack
{
    private readonly Stack<PlaySelectionFrame> _frames = new([PlaySelectionFrame.Adjacent]);

    public PlaySelectionFrame Top => _frames.Peek();
    public PlaySelectionFrameKind TopKind => Top.Kind;
    public bool IsAdjacentSelection => TopKind == PlaySelectionFrameKind.AdjacentSelection;
    public GridCoord? LockedAdjacentCoord => _frames.Select(frame => frame.AdjacentCoord).FirstOrDefault(coord => coord is not null);

    public void EnterActionSelection(GridCoord adjacentCoord)
    {
        ClearToAdjacentSelection();
        _frames.Push(PlaySelectionFrame.Action(adjacentCoord));
    }

    public void EnterCellSelection()
    {
        var adjacentCoord = LockedAdjacentCoord ?? throw new InvalidOperationException("Cell selection requires an existing adjacent selection context.");
        if (TopKind != PlaySelectionFrameKind.CellSelection)
        {
            _frames.Push(PlaySelectionFrame.Cell(adjacentCoord));
        }
    }

    public void PopToActionOrAdjacent()
    {
        if (_frames.Count > 1)
        {
            _frames.Pop();
        }
    }

    public void ClearToAdjacentSelection()
    {
        _frames.Clear();
        _frames.Push(PlaySelectionFrame.Adjacent);
    }
}
