namespace GameGameGame.Core;

public enum TopologyEdgeKind
{
    DefaultGrid
}

public sealed record TopologyNeighbor(
    PlaneCoord Destination,
    Direction Direction,
    TopologyEdgeKind Kind,
    bool IsBlocked,
    FailureReason? FailureReason,
    string? FailureDetail);

public interface ITopologyService
{
    bool TryGetNeighbor(WorldState world, PlaneCoord origin, Direction direction, out TopologyNeighbor neighbor);

    IReadOnlyList<TopologyNeighbor> GetNeighbors(WorldState world, PlaneCoord origin);

    AdjacencyEvaluation EvaluateAdjacency(WorldState world, PlaneCoord first, PlaneCoord second);
}

public sealed class DefaultTopologyService : ITopologyService
{
    public bool TryGetNeighbor(WorldState world, PlaneCoord origin, Direction direction, out TopologyNeighbor neighbor)
    {
        var destination = new PlaneCoord(origin.PlaneId, origin.Coord.Offset(direction));
        if (!world.Planes.TryGetValue(origin.PlaneId, out var plane) ||
            !plane.Contains(destination.Coord) ||
            !world.TryGetNodeId(destination, out _))
        {
            neighbor = new TopologyNeighbor(
                destination,
                direction,
                TopologyEdgeKind.DefaultGrid,
                IsBlocked: true,
                FailureReason.MoveOutOfBounds,
                $"neighbor {destination} is outside the origin plane");
            return false;
        }

        var corners = DirectionMath.OrthogonalCorners(direction);
        if (corners is { } intercardinalCorners)
        {
            var firstCorner = new PlaneCoord(origin.PlaneId, origin.Coord.Offset(intercardinalCorners.First));
            var secondCorner = new PlaneCoord(origin.PlaneId, origin.Coord.Offset(intercardinalCorners.Second));
            if (world.GetOccupant(firstCorner) is not null && world.GetOccupant(secondCorner) is not null)
            {
                neighbor = new TopologyNeighbor(
                    destination,
                    direction,
                    TopologyEdgeKind.DefaultGrid,
                    IsBlocked: true,
                    FailureReason.MoveBlocked,
                    $"intercardinal adjacency {direction} is blocked by both orthogonal corners");
                return false;
            }
        }

        neighbor = new TopologyNeighbor(
            destination,
            direction,
            TopologyEdgeKind.DefaultGrid,
            IsBlocked: false,
            FailureReason: null,
            FailureDetail: null);
        return true;
    }

    public IReadOnlyList<TopologyNeighbor> GetNeighbors(WorldState world, PlaneCoord origin) =>
        DirectionMath.AllDirections.Select(direction =>
        {
            TryGetNeighbor(world, origin, direction, out var neighbor);
            return neighbor;
        }).ToList();

    public AdjacencyEvaluation EvaluateAdjacency(WorldState world, PlaneCoord first, PlaneCoord second)
    {
        if (first.PlaneId != second.PlaneId)
        {
            return new(false, null, false, FailureReason.TargetNotAdjacent, $"{second} is not on the same plane as {first}");
        }

        var deltaX = second.Coord.X - first.Coord.X;
        var deltaY = second.Coord.Y - first.Coord.Y;
        if (Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)) != 1)
        {
            return new(false, null, false, FailureReason.TargetNotAdjacent, $"{second} is not adjacent to {first}");
        }

        var direction = DirectionFromDelta(deltaX, deltaY);
        if (direction is null)
        {
            return new(false, null, false, FailureReason.TargetNotAdjacent, $"{second} is not adjacent to {first}");
        }

        var corners = DirectionMath.OrthogonalCorners(direction.Value);
        var isIntercardinal = corners is not null;
        if (corners is not null && !TryGetNeighbor(world, first, direction.Value, out var blockedNeighbor))
        {
            return new(false, direction, isIntercardinal, blockedNeighbor.FailureReason, blockedNeighbor.FailureDetail);
        }

        return new(true, direction, isIntercardinal, null, null);
    }

    private static Direction? DirectionFromDelta(int deltaX, int deltaY) => (deltaX, deltaY) switch
    {
        (0, -1) => Direction.North,
        (1, -1) => Direction.NorthEast,
        (1, 0) => Direction.East,
        (1, 1) => Direction.SouthEast,
        (0, 1) => Direction.South,
        (-1, 1) => Direction.SouthWest,
        (-1, 0) => Direction.West,
        (-1, -1) => Direction.NorthWest,
        _ => null
    };
}
