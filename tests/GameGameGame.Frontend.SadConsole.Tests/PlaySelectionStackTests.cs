using GameGameGame.Core;
using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class PlaySelectionStackTests
{
    [Fact]
    public void StartsInAdjacentSelection()
    {
        var stack = new PlaySelectionStack();

        Assert.Equal(PlaySelectionFrameKind.AdjacentSelection, stack.TopKind);
        Assert.True(stack.IsAdjacentSelection);
        Assert.Null(stack.LockedAdjacentCoord);
    }

    [Fact]
    public void ActionSelectionLocksAdjacentCoord()
    {
        var stack = new PlaySelectionStack();
        var coord = new GridCoord(2, 1);

        stack.EnterActionSelection(coord);

        Assert.Equal(PlaySelectionFrameKind.ActionSelection, stack.TopKind);
        Assert.Equal(coord, stack.LockedAdjacentCoord);
    }

    [Fact]
    public void CellSelectionPreservesLockedAdjacentCoordAndCanPopBackToActionSelection()
    {
        var stack = new PlaySelectionStack();
        var coord = new GridCoord(2, 1);

        stack.EnterActionSelection(coord);
        stack.EnterCellSelection();
        stack.PopToActionOrAdjacent();

        Assert.Equal(PlaySelectionFrameKind.ActionSelection, stack.TopKind);
        Assert.Equal(coord, stack.LockedAdjacentCoord);
    }

    [Fact]
    public void ClearToAdjacentSelectionClearsLockedCoord()
    {
        var stack = new PlaySelectionStack();

        stack.EnterActionSelection(new GridCoord(2, 1));
        stack.EnterCellSelection();
        stack.ClearToAdjacentSelection();

        Assert.Equal(PlaySelectionFrameKind.AdjacentSelection, stack.TopKind);
        Assert.Null(stack.LockedAdjacentCoord);
    }
}
