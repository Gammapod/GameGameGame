namespace GameGameGame.Core;

public enum TopologyEdgeKind
{
    DefaultGrid,
    DirectedOverlay
}

public sealed record TopologyNeighbor(
    PlaneCoord Destination,
    Direction Direction,
    TopologyEdgeKind Kind,
    bool IsBlocked,
    FailureReason? FailureReason,
    string? FailureDetail);

public sealed record TopologyRayStep(
    int StepIndex,
    PlaneCoord Origin,
    PlaneCoord Destination,
    Direction Direction,
    TopologyEdgeKind Kind);

public sealed record TopologyFloodStep(
    PlaneCoord Coord,
    int Distance,
    PlaneCoord? From,
    Direction? Direction,
    TopologyEdgeKind? Kind);

public interface ITopologyService
{
    bool TryGetNeighbor(WorldState world, PlaneCoord origin, Direction direction, out TopologyNeighbor neighbor);

    IReadOnlyList<TopologyNeighbor> GetNeighbors(WorldState world, PlaneCoord origin);

    AdjacencyEvaluation EvaluateAdjacency(WorldState world, PlaneCoord first, PlaneCoord second);
}

public sealed class TopologyTraversalService(ITopologyService topology)
{
    public IReadOnlyList<TopologyRayStep> CastDirectionalRay(
        WorldState world,
        PlaneCoord origin,
        Direction direction,
        int maxSteps)
    {
        if (maxSteps <= 0)
        {
            return [];
        }

        var steps = new List<TopologyRayStep>();
        var current = origin;
        for (var index = 0; index < maxSteps; index++)
        {
            if (!topology.TryGetNeighbor(world, current, direction, out var neighbor))
            {
                break;
            }

            steps.Add(new TopologyRayStep(index, current, neighbor.Destination, direction, neighbor.Kind));
            current = neighbor.Destination;
        }

        return steps;
    }

    public IReadOnlyList<TopologyFloodStep> Flood(WorldState world, PlaneCoord origin, int maxDepth)
    {
        if (maxDepth < 0)
        {
            return [];
        }

        var visited = new HashSet<PlaneCoord> { origin };
        var result = new List<TopologyFloodStep>
        {
            new(origin, Distance: 0, From: null, Direction: null, Kind: null)
        };
        var queue = new Queue<TopologyFloodStep>();
        queue.Enqueue(result[0]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.Distance >= maxDepth)
            {
                continue;
            }

            foreach (var neighbor in topology.GetNeighbors(world, current.Coord).Where(neighbor => !neighbor.IsBlocked))
            {
                if (!visited.Add(neighbor.Destination))
                {
                    continue;
                }

                var step = new TopologyFloodStep(
                    neighbor.Destination,
                    current.Distance + 1,
                    current.Coord,
                    neighbor.Direction,
                    neighbor.Kind);
                result.Add(step);
                queue.Enqueue(step);
            }
        }

        return result;
    }
}

public sealed record DirectedTopologyEdge(PlaneCoord Origin, Direction Direction, PlaneCoord Destination);

public sealed class DirectedOverlayTopologyService : ITopologyService
{
    private readonly ITopologyService inner;
    private readonly Dictionary<(PlaneCoord Origin, Direction Direction), DirectedTopologyEdge> edgesByOriginAndDirection = [];

    public DirectedOverlayTopologyService(ITopologyService inner, IEnumerable<DirectedTopologyEdge> edges)
    {
        this.inner = inner;
        foreach (var edge in edges)
        {
            if (!edgesByOriginAndDirection.TryAdd((edge.Origin, edge.Direction), edge))
            {
                throw new ArgumentException($"Duplicate topology overlay edge from {edge.Origin} toward {edge.Direction}.", nameof(edges));
            }
        }
    }

    public bool TryGetNeighbor(WorldState world, PlaneCoord origin, Direction direction, out TopologyNeighbor neighbor)
    {
        if (!edgesByOriginAndDirection.TryGetValue((origin, direction), out var edge))
        {
            return inner.TryGetNeighbor(world, origin, direction, out neighbor);
        }

        if (edge.Origin.PlaneId != edge.Destination.PlaneId)
        {
            neighbor = new TopologyNeighbor(
                edge.Destination,
                edge.Direction,
                TopologyEdgeKind.DirectedOverlay,
                IsBlocked: true,
                FailureReason.TargetNotAdjacent,
                $"cross-plane overlay edge from {edge.Origin} to {edge.Destination} is not supported by the first directed-overlay slice");
            return false;
        }

        if (!world.Planes.TryGetValue(edge.Destination.PlaneId, out var plane) ||
            !plane.Contains(edge.Destination.Coord) ||
            !world.TryGetNodeId(edge.Destination, out _))
        {
            neighbor = new TopologyNeighbor(
                edge.Destination,
                edge.Direction,
                TopologyEdgeKind.DirectedOverlay,
                IsBlocked: true,
                FailureReason.MoveOutOfBounds,
                $"overlay neighbor {edge.Destination} is not a valid node");
            return false;
        }

        neighbor = new TopologyNeighbor(
            edge.Destination,
            edge.Direction,
            TopologyEdgeKind.DirectedOverlay,
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
        foreach (var edge in edgesByOriginAndDirection.Values.Where(edge => edge.Origin == first))
        {
            if (edge.Destination == second)
            {
                return TryGetNeighbor(world, first, edge.Direction, out var neighbor)
                    ? new AdjacencyEvaluation(
                        AreAdjacent: true,
                        Direction: neighbor.Direction,
                        IsIntercardinal: DirectionMath.OrthogonalCorners(neighbor.Direction) is not null,
                        FailureReason: null,
                        FailureDetail: null)
                    : new AdjacencyEvaluation(
                        AreAdjacent: false,
                        Direction: edge.Direction,
                        IsIntercardinal: DirectionMath.OrthogonalCorners(edge.Direction) is not null,
                        FailureReason: neighbor.FailureReason,
                        FailureDetail: neighbor.FailureDetail);
            }
        }

        if (TryCoordinateDirection(first, second, out var coordinateDirection) &&
            edgesByOriginAndDirection.TryGetValue((first, coordinateDirection), out var overrideEdge) &&
            overrideEdge.Destination != second)
        {
            return new AdjacencyEvaluation(
                AreAdjacent: false,
                Direction: coordinateDirection,
                IsIntercardinal: DirectionMath.OrthogonalCorners(coordinateDirection) is not null,
                FailureReason.TargetNotAdjacent,
                $"topology direction {coordinateDirection} from {first} resolves to {overrideEdge.Destination}, not {second}");
        }

        return inner.EvaluateAdjacency(world, first, second);
    }

    private static bool TryCoordinateDirection(PlaneCoord first, PlaneCoord second, out Direction direction)
    {
        direction = default;
        if (first.PlaneId != second.PlaneId)
        {
            return false;
        }

        var deltaX = second.Coord.X - first.Coord.X;
        var deltaY = second.Coord.Y - first.Coord.Y;
        var resolved = (deltaX, deltaY) switch
        {
            (0, -1) => (Direction?)Direction.North,
            (1, -1) => Direction.NorthEast,
            (1, 0) => Direction.East,
            (1, 1) => Direction.SouthEast,
            (0, 1) => Direction.South,
            (-1, 1) => Direction.SouthWest,
            (-1, 0) => Direction.West,
            (-1, -1) => Direction.NorthWest,
            _ => null
        };
        if (resolved is null)
        {
            return false;
        }

        direction = resolved.Value;
        return true;
    }
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
