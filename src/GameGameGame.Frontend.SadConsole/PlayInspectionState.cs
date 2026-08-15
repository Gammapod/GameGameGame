using GameGameGame.Core;

namespace GameGameGame.Frontend.SadConsole;

internal sealed class PlayInspectionState
{
    public EntityId? LastInspectedEntityId { get; private set; }

    public PlayCellVisual? ResolveInspectedCell(PlayGridViewModel grid, GridCoord? previewCoord)
    {
        var adjacent = AdjacentOccupiedCells(grid).ToList();
        if (adjacent.Count == 0)
        {
            LastInspectedEntityId = null;
            return null;
        }

        var inspected = TryOccupiedCell(grid, previewCoord)
            ?? TryLastInspectedCell(adjacent)
            ?? adjacent[0];
        if (inspected?.EntityId is { } entityId)
        {
            LastInspectedEntityId = entityId;
        }

        return inspected;
    }

    private static PlayCellVisual? TryOccupiedCell(PlayGridViewModel grid, GridCoord? coord)
    {
        if (coord is not { } target)
        {
            return null;
        }

        var cell = grid.TryCellAt(target.X, target.Y);
        return cell?.EntityId is { } entityId && entityId != grid.ControlledEntityId ? cell : null;
    }

    private PlayCellVisual? TryLastInspectedCell(IEnumerable<PlayCellVisual> adjacent) => LastInspectedEntityId is { } last
        ? adjacent.FirstOrDefault(cell => cell.EntityId == last)
        : null;

    private static IEnumerable<PlayCellVisual> AdjacentOccupiedCells(PlayGridViewModel grid)
    {
        if (grid.ControlledEntityCoord is not { } origin)
        {
            yield break;
        }

        foreach (var direction in DirectionMath.AllDirections)
        {
            var coord = origin.Offset(direction);
            var cell = grid.TryCellAt(coord.X, coord.Y);
            if (cell?.EntityId is { } entityId && entityId != grid.ControlledEntityId)
            {
                yield return cell;
            }
        }
    }
}
