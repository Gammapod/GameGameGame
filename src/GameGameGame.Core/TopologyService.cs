namespace GameGameGame.Core;

public enum TopologyEdgeKind
{
    DefaultGrid,
    DirectedOverlay,
    EntityTopologyPolicy,
    MergedInventoryLayer
}

public sealed record TopologyNeighbor(
    PlaneCoord Destination,
    Direction Direction,
    TopologyEdgeKind Kind,
    bool IsBlocked,
    FailureReason? FailureReason,
    string? FailureDetail);

public readonly record struct TopologyCellRef(PlaneCoord SourceCoord)
{
    public override string ToString() => SourceCoord.ToString();
}

public readonly record struct TopologyLayoutCoord(GridCoord Coord)
{
    public override string ToString() => $"layout{Coord}";
}

public readonly record struct TopologyDisplayCoord(GridCoord Coord)
{
    public override string ToString() => $"display{Coord}";
}

public sealed record TopologyEdgeFact(
    TopologyCellRef Source,
    Direction Direction,
    TopologyCellRef Destination,
    TopologyEdgeKind Kind,
    bool IsBlocked,
    FailureReason? FailureReason,
    string? FailureDetail)
{
    public static TopologyEdgeFact FromNeighbor(PlaneCoord source, TopologyNeighbor neighbor) =>
        new(
            new TopologyCellRef(source),
            neighbor.Direction,
            new TopologyCellRef(neighbor.Destination),
            neighbor.Kind,
            neighbor.IsBlocked,
            neighbor.FailureReason,
            neighbor.FailureDetail);

    public TopologyNeighbor ToNeighbor() =>
        new(Destination.SourceCoord, Direction, Kind, IsBlocked, FailureReason, FailureDetail);
}

public sealed record TopologyDirectedEdgeFact(
    TopologyCellRef Source,
    Direction Direction,
    TopologyCellRef Destination);

public sealed record TopologyDirectionalUniquenessConflict(
    TopologyCellRef Source,
    Direction Direction,
    TopologyCellRef FirstDestination,
    TopologyCellRef ConflictingDestination);

public sealed record TopologyDirectionalUniquenessResult(IReadOnlyList<TopologyDirectionalUniquenessConflict> Conflicts)
{
    public bool IsValid => Conflicts.Count == 0;
}

public static class TopologyDirectionalUniqueness
{
    public static TopologyDirectionalUniquenessResult Validate(IEnumerable<TopologyDirectedEdgeFact> edges)
    {
        var destinationsBySourceDirection = new Dictionary<(TopologyCellRef Source, Direction Direction), TopologyCellRef>();
        var conflicts = new List<TopologyDirectionalUniquenessConflict>();
        foreach (var edge in edges)
        {
            var key = (edge.Source, edge.Direction);
            if (!destinationsBySourceDirection.TryGetValue(key, out var existingDestination))
            {
                destinationsBySourceDirection[key] = edge.Destination;
                continue;
            }

            if (existingDestination != edge.Destination)
            {
                conflicts.Add(new TopologyDirectionalUniquenessConflict(
                    edge.Source,
                    edge.Direction,
                    existingDestination,
                    edge.Destination));
            }
        }

        return new TopologyDirectionalUniquenessResult(conflicts);
    }
}

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

public sealed record MergedInventoryLayerCell(
    MergedInventoryLayer Layer,
    MergedInventorySpaceContribution Space,
    PlaneCoord SourceCoord,
    GridCoord LayerCoord);

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

public sealed class EntityTopologyService(ITopologyService inner) : ITopologyService
{
    public bool TryGetNeighbor(WorldState world, PlaneCoord origin, Direction direction, out TopologyNeighbor neighbor)
    {
        if (TryGetOutwardNeighbor(world, origin, direction, out neighbor))
        {
            return !neighbor.IsBlocked;
        }

        if (TryGetInwardNeighbor(world, origin, direction, out neighbor))
        {
            return !neighbor.IsBlocked;
        }

        return inner.TryGetNeighbor(world, origin, direction, out neighbor);
    }

    public IReadOnlyList<TopologyNeighbor> GetNeighbors(WorldState world, PlaneCoord origin) =>
        DirectionMath.AllDirections.Select(direction =>
        {
            TryGetNeighbor(world, origin, direction, out var neighbor);
            return neighbor;
        }).ToList();

    public AdjacencyEvaluation EvaluateAdjacency(WorldState world, PlaneCoord first, PlaneCoord second)
    {
        foreach (var direction in DirectionMath.AllDirections)
        {
            TryGetNeighbor(world, first, direction, out var neighbor);
            if (neighbor.Destination == second)
            {
                return neighbor.IsBlocked
                    ? new AdjacencyEvaluation(
                        AreAdjacent: false,
                        Direction: direction,
                        IsIntercardinal: DirectionMath.OrthogonalCorners(direction) is not null,
                        FailureReason: neighbor.FailureReason,
                        FailureDetail: neighbor.FailureDetail)
                    : new AdjacencyEvaluation(
                        AreAdjacent: true,
                        Direction: direction,
                        IsIntercardinal: DirectionMath.OrthogonalCorners(direction) is not null,
                        FailureReason: null,
                        FailureDetail: null);
            }
        }

        if (TryCoordinateDirection(first, second, out var coordinateDirection) &&
            TryGetNeighbor(world, first, coordinateDirection, out var coordinateNeighbor) &&
            coordinateNeighbor.Kind == TopologyEdgeKind.EntityTopologyPolicy &&
            coordinateNeighbor.Destination != second)
        {
            return new AdjacencyEvaluation(
                AreAdjacent: false,
                Direction: coordinateDirection,
                IsIntercardinal: DirectionMath.OrthogonalCorners(coordinateDirection) is not null,
                FailureReason.TargetNotAdjacent,
                $"entity topology direction {coordinateDirection} from {first} resolves to {coordinateNeighbor.Destination}, not {second}");
        }

        return inner.EvaluateAdjacency(world, first, second);
    }

    private static bool TryGetOutwardNeighbor(WorldState world, PlaneCoord origin, Direction direction, out TopologyNeighbor neighbor)
    {
        neighbor = default!;
        if (!InventoryPlaneOwnership.TryFindOwner(world, origin.PlaneId, out var ownerId) ||
            !world.Entities.TryGetValue(ownerId, out var owner) ||
            !ConnectsOutward(owner.TopologyPolicy) ||
            !IsOnBoundary(origin.Coord, owner.InventoryWidth, owner.InventoryHeight, direction))
        {
            return false;
        }

        var ownerLocation = world.GetEntityLocation(ownerId);
        var destination = new PlaneCoord(ownerLocation.PlaneId, ownerLocation.Coord.Offset(direction));
        neighbor = CreateEntityTopologyNeighbor(world, destination, direction);
        return true;
    }

    private static bool TryGetInwardNeighbor(WorldState world, PlaneCoord origin, Direction direction, out TopologyNeighbor neighbor)
    {
        neighbor = default!;
        if (world.GetOccupant(new PlaneCoord(origin.PlaneId, origin.Coord.Offset(direction))) is not { } ownerId ||
            !world.Entities.TryGetValue(ownerId, out var owner) ||
            !ConnectsInward(owner.TopologyPolicy) ||
            world.GetInventoryPlaneId(ownerId) is not { } inventoryPlaneId)
        {
            return false;
        }

        var boundaryDirection = DirectionMath.Reverse(direction);
        var destination = new PlaneCoord(inventoryPlaneId, PreferredBoundaryCoord(owner.InventoryWidth, owner.InventoryHeight, boundaryDirection));
        neighbor = CreateEntityTopologyNeighbor(world, destination, direction);
        return true;
    }

    private static TopologyNeighbor CreateEntityTopologyNeighbor(WorldState world, PlaneCoord destination, Direction direction)
    {
        if (!world.Planes.TryGetValue(destination.PlaneId, out var plane) ||
            !plane.Contains(destination.Coord) ||
            !world.TryGetNodeId(destination, out _))
        {
            return new TopologyNeighbor(
                destination,
                direction,
                TopologyEdgeKind.EntityTopologyPolicy,
                IsBlocked: true,
                FailureReason.MoveOutOfBounds,
                $"entity topology destination {destination} is not a valid node");
        }

        return new TopologyNeighbor(
            destination,
            direction,
            TopologyEdgeKind.EntityTopologyPolicy,
            IsBlocked: false,
            FailureReason: null,
            FailureDetail: null);
    }

    private static bool ConnectsInward(EntityTopologyPolicy policy) =>
        policy is EntityTopologyPolicy.ConnectsInward or EntityTopologyPolicy.ConnectsInwardAndOutward;

    private static bool ConnectsOutward(EntityTopologyPolicy policy) =>
        policy is EntityTopologyPolicy.ConnectsOutward or EntityTopologyPolicy.ConnectsInwardAndOutward;

    private static bool IsOnBoundary(GridCoord coord, int width, int height, Direction direction) => direction switch
    {
        Direction.North => coord.Y == 0,
        Direction.NorthEast => coord.Y == 0 && coord.X == width - 1,
        Direction.East => coord.X == width - 1,
        Direction.SouthEast => coord.Y == height - 1 && coord.X == width - 1,
        Direction.South => coord.Y == height - 1,
        Direction.SouthWest => coord.Y == height - 1 && coord.X == 0,
        Direction.West => coord.X == 0,
        Direction.NorthWest => coord.Y == 0 && coord.X == 0,
        _ => false
    };

    private static GridCoord PreferredBoundaryCoord(int width, int height, Direction direction) => direction switch
    {
        Direction.North => new GridCoord(SecondFromLeft(width), 0),
        Direction.NorthEast => new GridCoord(width - 1, 0),
        Direction.East => new GridCoord(width - 1, SecondFromTop(height)),
        Direction.SouthEast => new GridCoord(width - 1, height - 1),
        Direction.South => new GridCoord(SecondFromLeft(width), height - 1),
        Direction.SouthWest => new GridCoord(0, height - 1),
        Direction.West => new GridCoord(0, SecondFromTop(height)),
        Direction.NorthWest => new GridCoord(0, 0),
        _ => new GridCoord(0, 0)
    };

    private static int SecondFromLeft(int width) => Math.Min(1, Math.Max(0, width - 1));

    private static int SecondFromTop(int height) => Math.Min(1, Math.Max(0, height - 1));

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

public static class MergedInventoryLayerResolver
{
    public static bool TryFindLocalOwner(WorldState world, PlaneCoord sourceCoord, out EntityId ownerId)
    {
        if (TryResolveCell(world, sourceCoord, out var cell))
        {
            ownerId = cell.Space.OwnerId;
            return true;
        }

        return InventoryPlaneOwnership.TryFindOwner(world, sourceCoord.PlaneId, out ownerId);
    }

    public static bool TryResolveCell(WorldState world, PlaneCoord sourceCoord, out MergedInventoryLayerCell cell)
    {
        foreach (var layer in world.MergedInventoryLayers)
        {
            foreach (var space in layer.Spaces)
            {
                if (world.GetRegisteredInventoryPlaneId(space.OwnerId) != sourceCoord.PlaneId ||
                    !world.Entities.TryGetValue(space.OwnerId, out var owner) ||
                    !IsWithinInventory(sourceCoord.Coord, owner))
                {
                    continue;
                }

                cell = new MergedInventoryLayerCell(
                    layer,
                    space,
                    sourceCoord,
                    new GridCoord(space.Origin.X + sourceCoord.Coord.X, space.Origin.Y + sourceCoord.Coord.Y));
                return true;
            }
        }

        cell = default!;
        return false;
    }

    public static bool TryResolveLayerCoord(
        WorldState world,
        MergedInventoryLayer layer,
        GridCoord layerCoord,
        out MergedInventoryLayerCell cell)
    {
        foreach (var space in layer.Spaces)
        {
            if (!world.Entities.TryGetValue(space.OwnerId, out var owner) ||
                world.GetRegisteredInventoryPlaneId(space.OwnerId) is not { } inventoryPlaneId)
            {
                continue;
            }

            var sourceCoord = new GridCoord(layerCoord.X - space.Origin.X, layerCoord.Y - space.Origin.Y);
            if (!IsWithinInventory(sourceCoord, owner))
            {
                continue;
            }

            var planeCoord = new PlaneCoord(inventoryPlaneId, sourceCoord);
            if (!world.Planes.TryGetValue(inventoryPlaneId, out var plane) ||
                !plane.Contains(sourceCoord) ||
                !world.TryGetNodeId(planeCoord, out _))
            {
                continue;
            }

            cell = new MergedInventoryLayerCell(layer, space, planeCoord, layerCoord);
            return true;
        }

        cell = default!;
        return false;
    }

    private static bool IsWithinInventory(GridCoord coord, Entity owner) =>
        coord.X >= 0 && coord.Y >= 0 && coord.X < owner.InventoryWidth && coord.Y < owner.InventoryHeight;
}

public sealed class MergedInventoryLayerTopologyService(ITopologyService inner) : ITopologyService
{
    public bool TryGetNeighbor(WorldState world, PlaneCoord origin, Direction direction, out TopologyNeighbor neighbor)
    {
        if (!MergedInventoryLayerResolver.TryResolveCell(world, origin, out var originCell))
        {
            return inner.TryGetNeighbor(world, origin, direction, out neighbor);
        }

        var destinationLayerCoord = originCell.LayerCoord.Offset(direction);
        if (!MergedInventoryLayerResolver.TryResolveLayerCoord(world, originCell.Layer, destinationLayerCoord, out var destinationCell))
        {
            neighbor = new TopologyNeighbor(
                new PlaneCoord(origin.PlaneId, origin.Coord.Offset(direction)),
                direction,
                TopologyEdgeKind.MergedInventoryLayer,
                IsBlocked: true,
                FailureReason.MoveOutOfBounds,
                $"merged inventory layer {originCell.Layer.Id} has no cell at {destinationLayerCoord}");
            return false;
        }

        if (DirectionMath.OrthogonalCorners(direction) is { } corners &&
            IsOccupiedLayerCoord(world, originCell.Layer, originCell.LayerCoord.Offset(corners.First)) &&
            IsOccupiedLayerCoord(world, originCell.Layer, originCell.LayerCoord.Offset(corners.Second)))
        {
            neighbor = new TopologyNeighbor(
                destinationCell.SourceCoord,
                direction,
                TopologyEdgeKind.MergedInventoryLayer,
                IsBlocked: true,
                FailureReason.MoveBlocked,
                $"merged inventory layer intercardinal adjacency {direction} is blocked by both orthogonal corners");
            return false;
        }

        neighbor = new TopologyNeighbor(
            destinationCell.SourceCoord,
            direction,
            TopologyEdgeKind.MergedInventoryLayer,
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
        if (!MergedInventoryLayerResolver.TryResolveCell(world, first, out var firstCell) ||
            !MergedInventoryLayerResolver.TryResolveCell(world, second, out var secondCell) ||
            firstCell.Layer.Id != secondCell.Layer.Id)
        {
            return inner.EvaluateAdjacency(world, first, second);
        }

        var deltaX = secondCell.LayerCoord.X - firstCell.LayerCoord.X;
        var deltaY = secondCell.LayerCoord.Y - firstCell.LayerCoord.Y;
        if (Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)) != 1 ||
            DirectionFromDelta(deltaX, deltaY) is not { } direction)
        {
            return new(false, null, false, FailureReason.TargetNotAdjacent, $"{second} is not adjacent to {first} in merged inventory layer {firstCell.Layer.Id}");
        }

        var found = TryGetNeighbor(world, first, direction, out var neighbor);
        return found && neighbor.Destination == second
            ? new AdjacencyEvaluation(true, direction, DirectionMath.OrthogonalCorners(direction) is not null, null, null)
            : new AdjacencyEvaluation(false, direction, DirectionMath.OrthogonalCorners(direction) is not null, neighbor.FailureReason, neighbor.FailureDetail);
    }

    private static bool IsOccupiedLayerCoord(WorldState world, MergedInventoryLayer layer, GridCoord layerCoord) =>
        MergedInventoryLayerResolver.TryResolveLayerCoord(world, layer, layerCoord, out var cell) && world.GetOccupant(cell.SourceCoord) is not null;

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
