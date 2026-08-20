namespace GameGameGame.Core;

public enum TopologyEdgeKind
{
    DefaultGrid,
    EntityTopologyPolicy,
    MergedInventoryLayer,
    SourceCellLink
}

public sealed record TopologyNeighbor(
    PlaneCoord Destination,
    Direction Direction,
    TopologyEdgeKind Kind,
    bool IsBlocked,
    FailureReason? FailureReason,
    string? FailureDetail);

public readonly record struct TopologyNodeId(string Value)
{
    public override string ToString() => Value;
}

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

public sealed record TopologyNode(
    TopologyNodeId Id,
    PlaneCoord SourceCoord,
    TopologyLayoutCoord LayoutCoord,
    TopologyDisplayCoord DisplayCoord);

public sealed record TopologyGraphEdge(
    TopologyNodeId SourceNodeId,
    Direction Direction,
    TopologyNodeId DestinationNodeId,
    TopologyEdgeKind Kind,
    bool IsBlocked,
    FailureReason? FailureReason,
    string? FailureDetail);

public sealed class TopologyGraph
{
    private readonly Dictionary<TopologyCellRef, TopologyNode> nodesBySource;
    private readonly Dictionary<TopologyNodeId, TopologyNode> nodesById;
    private readonly Dictionary<(TopologyNodeId Source, Direction Direction), List<TopologyGraphEdge>> edgesBySourceDirection;

    public TopologyGraph(IEnumerable<TopologyNode> nodes, IEnumerable<TopologyGraphEdge> edges)
    {
        Nodes = nodes.ToList();
        Edges = edges.ToList();
        nodesBySource = Nodes.ToDictionary(node => new TopologyCellRef(node.SourceCoord));
        nodesById = Nodes.ToDictionary(node => node.Id);
        edgesBySourceDirection = Edges
            .GroupBy(edge => (edge.SourceNodeId, edge.Direction))
            .ToDictionary(group => group.Key, group => group.ToList());
    }

    public IReadOnlyList<TopologyNode> Nodes { get; }

    public IReadOnlyList<TopologyGraphEdge> Edges { get; }

    public bool TryGetNode(TopologyCellRef source, out TopologyNode node) =>
        nodesBySource.TryGetValue(source, out node!);

    public bool TryGetNode(TopologyNodeId nodeId, out TopologyNode node) =>
        nodesById.TryGetValue(nodeId, out node!);

    public IReadOnlyList<TopologyGraphEdge> GetOutgoingEdges(TopologyNodeId sourceNodeId, Direction direction) =>
        edgesBySourceDirection.TryGetValue((sourceNodeId, direction), out var edges)
            ? edges
            : [];

    public bool TryGetNeighbor(TopologyCellRef source, Direction direction, out TopologyNeighbor neighbor)
    {
        if (!nodesBySource.TryGetValue(source, out var sourceNode))
        {
            var missingSourceDestination = new PlaneCoord(source.SourceCoord.PlaneId, source.SourceCoord.Coord.Offset(direction));
            neighbor = new TopologyNeighbor(
                missingSourceDestination,
                direction,
                TopologyEdgeKind.DefaultGrid,
                IsBlocked: true,
                FailureReason.MoveOutOfBounds,
                $"source topology node {source} does not exist");
            return false;
        }

        if (GetOutgoingEdges(sourceNode.Id, direction)
            .OrderBy(edge => edge.Kind == TopologyEdgeKind.DefaultGrid ? 1 : 0)
            .FirstOrDefault() is { } edge)
        {
            var destination = nodesById[edge.DestinationNodeId].SourceCoord;
            neighbor = new TopologyNeighbor(
                destination,
                edge.Direction,
                edge.Kind,
                edge.IsBlocked,
                edge.FailureReason,
                edge.FailureDetail);
            return !neighbor.IsBlocked;
        }

        var outOfBoundsDestination = new PlaneCoord(source.SourceCoord.PlaneId, source.SourceCoord.Coord.Offset(direction));
        neighbor = new TopologyNeighbor(
            outOfBoundsDestination,
            direction,
            TopologyEdgeKind.DefaultGrid,
            IsBlocked: true,
            FailureReason.MoveOutOfBounds,
            $"neighbor {outOfBoundsDestination} is outside the origin plane");
        return false;
    }

    public IReadOnlyList<TopologyDirectedEdgeFact> ToDirectedEdgeFacts() =>
        Edges
            .Where(edge => !edge.IsBlocked)
            .Select(edge => new TopologyDirectedEdgeFact(
                new TopologyCellRef(nodesById[edge.SourceNodeId].SourceCoord),
                edge.Direction,
                new TopologyCellRef(nodesById[edge.DestinationNodeId].SourceCoord)))
            .ToList();
}

public sealed record TopologyGraphFloodStep(
    TopologyNodeId NodeId,
    PlaneCoord SourceCoord,
    TopologyLayoutCoord LayoutCoord,
    int Distance,
    TopologyNodeId? FromNodeId,
    Direction? Direction,
    TopologyEdgeKind? Kind);

public sealed record TopologyGraphPathStep(
    TopologyNodeId NodeId,
    PlaneCoord SourceCoord,
    TopologyLayoutCoord LayoutCoord,
    int HalfStepDistance,
    TopologyNodeId FromNodeId,
    Direction Direction,
    TopologyEdgeKind Kind);

public static class TopologyGraphTraversalService
{
    public static IReadOnlyList<TopologyGraphFloodStep> Flood(TopologyGraph graph, TopologyNodeId originNodeId, int maxDepth)
    {
        if (maxDepth < 0 || !graph.TryGetNode(originNodeId, out var originNode))
        {
            return [];
        }

        var visited = new HashSet<TopologyNodeId> { originNodeId };
        var result = new List<TopologyGraphFloodStep>
        {
            new(originNodeId, originNode.SourceCoord, originNode.LayoutCoord, Distance: 0, FromNodeId: null, Direction: null, Kind: null)
        };
        var queue = new Queue<TopologyGraphFloodStep>();
        queue.Enqueue(result[0]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.Distance >= maxDepth)
            {
                continue;
            }

            foreach (var direction in DirectionMath.AllDirections)
            {
                foreach (var edge in graph.GetOutgoingEdges(current.NodeId, direction).Where(edge => !edge.IsBlocked))
                {
                    if (!visited.Add(edge.DestinationNodeId) || !graph.TryGetNode(edge.DestinationNodeId, out var destinationNode))
                    {
                        continue;
                    }

                    var step = new TopologyGraphFloodStep(
                        edge.DestinationNodeId,
                        destinationNode.SourceCoord,
                        destinationNode.LayoutCoord,
                        current.Distance + 1,
                        current.NodeId,
                        edge.Direction,
                        edge.Kind);
                    result.Add(step);
                    queue.Enqueue(step);
                }
            }
        }

        return result;
    }

    public static int? HalfStepDistanceToAny(
        TopologyGraph graph,
        TopologyNodeId originNodeId,
        IReadOnlySet<TopologyNodeId> goalNodeIds,
        Func<TopologyGraphEdge, bool>? canTraverse = null,
        Func<Direction, int>? halfStepCost = null)
    {
        var path = ShortestPathToAny(graph, originNodeId, goalNodeIds, canTraverse, halfStepCost);
        return path is null
            ? null
            : path.Count == 0
                ? 0
                : path[^1].HalfStepDistance;
    }

    public static IReadOnlyList<TopologyGraphPathStep>? ShortestPathToAny(
        TopologyGraph graph,
        TopologyNodeId originNodeId,
        IReadOnlySet<TopologyNodeId> goalNodeIds,
        Func<TopologyGraphEdge, bool>? canTraverse = null,
        Func<Direction, int>? halfStepCost = null)
    {
        if (!graph.TryGetNode(originNodeId, out _))
        {
            return null;
        }

        if (goalNodeIds.Contains(originNodeId))
        {
            return [];
        }

        halfStepCost ??= DefaultHalfStepCost;
        canTraverse ??= static edge => !edge.IsBlocked;
        var bestDistances = new Dictionary<TopologyNodeId, int> { [originNodeId] = 0 };
        var previous = new Dictionary<TopologyNodeId, (TopologyNodeId From, Direction Direction, TopologyEdgeKind Kind)>();
        var queue = new PriorityQueue<TopologyNodeId, int>();
        queue.Enqueue(originNodeId, 0);

        while (queue.TryDequeue(out var currentNodeId, out var currentDistance))
        {
            if (bestDistances[currentNodeId] != currentDistance)
            {
                continue;
            }

            if (goalNodeIds.Contains(currentNodeId))
            {
                return ReconstructPath(graph, originNodeId, currentNodeId, bestDistances, previous);
            }

            foreach (var direction in DirectionMath.AllDirections)
            {
                foreach (var edge in graph.GetOutgoingEdges(currentNodeId, direction).Where(canTraverse))
                {
                    if (!graph.TryGetNode(edge.DestinationNodeId, out _))
                    {
                        continue;
                    }

                    var nextDistance = currentDistance + halfStepCost(edge.Direction);
                    if (bestDistances.TryGetValue(edge.DestinationNodeId, out var knownDistance) && knownDistance <= nextDistance)
                    {
                        continue;
                    }

                    bestDistances[edge.DestinationNodeId] = nextDistance;
                    previous[edge.DestinationNodeId] = (currentNodeId, edge.Direction, edge.Kind);
                    queue.Enqueue(edge.DestinationNodeId, nextDistance);
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<TopologyGraphPathStep> ReconstructPath(
        TopologyGraph graph,
        TopologyNodeId originNodeId,
        TopologyNodeId goalNodeId,
        IReadOnlyDictionary<TopologyNodeId, int> distances,
        IReadOnlyDictionary<TopologyNodeId, (TopologyNodeId From, Direction Direction, TopologyEdgeKind Kind)> previous)
    {
        var path = new List<TopologyGraphPathStep>();
        var current = goalNodeId;
        while (current != originNodeId)
        {
            var edge = previous[current];
            var node = graph.TryGetNode(current, out var resolvedNode)
                ? resolvedNode
                : throw new InvalidOperationException($"Topology graph path references missing node {current}");
            path.Add(new TopologyGraphPathStep(
                current,
                node.SourceCoord,
                node.LayoutCoord,
                distances[current],
                edge.From,
                edge.Direction,
                edge.Kind));
            current = edge.From;
        }

        path.Reverse();
        return path;
    }

    private static int DefaultHalfStepCost(Direction direction) => DirectionMath.OrthogonalCorners(direction) is null ? 2 : 3;
}

public static class TopologyGraphMaterializer
{
    private static readonly AsyncLocal<CacheScope?> ActiveCacheScope = new();

    public static IDisposable BeginCacheScope()
    {
        if (ActiveCacheScope.Value is not null)
        {
            return NoopDisposable.Instance;
        }

        var scope = new CacheScope(ActiveCacheScope.Value);
        ActiveCacheScope.Value = scope;
        return scope;
    }

    public static void Invalidate(WorldState world)
    {
        for (var scope = ActiveCacheScope.Value; scope is not null; scope = scope.Parent)
        {
            scope.Graphs.Remove(world);
        }
    }

    public static TopologyGraph Materialize(WorldState world)
    {
        if (ActiveCacheScope.Value is { } cacheScope)
        {
            if (!cacheScope.Graphs.TryGetValue(world, out var cached))
            {
                cached = MaterializeUncached(world);
                cacheScope.Graphs[world] = cached;
            }

            return cached;
        }

        return MaterializeUncached(world);
    }

    private static TopologyGraph MaterializeUncached(WorldState world)
    {
        var nodes = world.Nodes.Values
            .Select(node => CreateNode(world, node))
            .ToList();
        var nodeIdsBySource = nodes.ToDictionary(node => node.SourceCoord, node => node.Id);
        var edges = new List<TopologyGraphEdge>();

        MaterializeDefaultGridEdges(world, nodeIdsBySource, edges);
        MaterializeEntityTopologyPolicyEdges(world, nodeIdsBySource, edges);
        MaterializeMergedInventoryLayerEdges(world, nodeIdsBySource, edges);
        MaterializeSourceCellLinkEdges(world, nodeIdsBySource, edges);

        return new TopologyGraph(nodes, edges);
    }

    private sealed class CacheScope(CacheScope? parent) : IDisposable
    {
        private bool _disposed;

        public CacheScope? Parent { get; } = parent;

        public Dictionary<WorldState, TopologyGraph> Graphs { get; } = new(ReferenceEqualityComparer.Instance);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (ActiveCacheScope.Value == this)
            {
                ActiveCacheScope.Value = Parent;
            }

            _disposed = true;
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        private NoopDisposable()
        {
        }

        public void Dispose()
        {
        }
    }

    private static TopologyNode CreateNode(WorldState world, Node node)
    {
        var sourceCoord = new PlaneCoord(node.PlaneId, node.Coord);
        var layoutCoord = MergedInventoryLayerResolver.TryResolveCell(world, sourceCoord, out var mergedCell)
            ? mergedCell.LayerCoord
            : node.Coord;

        return new TopologyNode(
            new TopologyNodeId(node.Id.Value),
            sourceCoord,
            new TopologyLayoutCoord(layoutCoord),
            new TopologyDisplayCoord(layoutCoord));
    }

    private static void MaterializeDefaultGridEdges(
        WorldState world,
        IReadOnlyDictionary<PlaneCoord, TopologyNodeId> nodeIdsBySource,
        List<TopologyGraphEdge> edges)
    {
        foreach (var source in nodeIdsBySource.Keys)
        {
            foreach (var direction in DirectionMath.AllDirections)
            {
                var destination = new PlaneCoord(source.PlaneId, source.Coord.Offset(direction));
                if (!nodeIdsBySource.TryGetValue(destination, out var destinationNodeId))
                {
                    continue;
                }

                var isBlocked = DirectionMath.OrthogonalCorners(direction) is { } corners &&
                    world.GetOccupant(new PlaneCoord(source.PlaneId, source.Coord.Offset(corners.First))) is not null &&
                    world.GetOccupant(new PlaneCoord(source.PlaneId, source.Coord.Offset(corners.Second))) is not null;

                edges.Add(new TopologyGraphEdge(
                    nodeIdsBySource[source],
                    direction,
                    destinationNodeId,
                    TopologyEdgeKind.DefaultGrid,
                    isBlocked,
                    isBlocked ? FailureReason.MoveBlocked : null,
                    isBlocked ? $"intercardinal adjacency {direction} is blocked by both orthogonal corners" : null));
            }
        }
    }

    private static void MaterializeSourceCellLinkEdges(
        WorldState world,
        IReadOnlyDictionary<PlaneCoord, TopologyNodeId> nodeIdsBySource,
        List<TopologyGraphEdge> edges)
    {
        foreach (var link in world.SourceCellLinks)
        {
            TryAddSourceCellLinkEdge(world, nodeIdsBySource, edges, link.FirstSource, link.FirstDirection, link.SecondSource);
            TryAddSourceCellLinkEdge(world, nodeIdsBySource, edges, link.SecondSource, link.SecondDirection, link.FirstSource);
        }
    }

    private static void MaterializeEntityTopologyPolicyEdges(
        WorldState world,
        IReadOnlyDictionary<PlaneCoord, TopologyNodeId> nodeIdsBySource,
        List<TopologyGraphEdge> edges)
    {
        foreach (var source in nodeIdsBySource.Keys)
        {
            foreach (var direction in DirectionMath.AllDirections)
            {
                if (!TryResolveEntityTopologyDestination(world, source, direction, out var destination) ||
                    !nodeIdsBySource.TryGetValue(destination, out var destinationNodeId))
                {
                    continue;
                }

                edges.Add(new TopologyGraphEdge(
                    nodeIdsBySource[source],
                    direction,
                    destinationNodeId,
                    TopologyEdgeKind.EntityTopologyPolicy,
                    IsBlocked: false,
                    FailureReason: null,
                    FailureDetail: null));
            }
        }
    }

    private static bool TryResolveEntityTopologyDestination(
        WorldState world,
        PlaneCoord origin,
        Direction direction,
        out PlaneCoord destination)
    {
        if (TryResolveOutwardEntityTopologyDestination(world, origin, direction, out destination) ||
            TryResolveInwardEntityTopologyDestination(world, origin, direction, out destination))
        {
            return true;
        }

        destination = default;
        return false;
    }

    private static bool TryResolveOutwardEntityTopologyDestination(
        WorldState world,
        PlaneCoord origin,
        Direction direction,
        out PlaneCoord destination)
    {
        if (!InventoryPlaneOwnership.TryFindOwner(world, origin.PlaneId, out var ownerId) ||
            !world.Entities.TryGetValue(ownerId, out var owner) ||
            !ConnectsOutward(owner.TopologyPolicy) ||
            !IsOnBoundary(origin.Coord, owner.InventoryWidth, owner.InventoryHeight, direction))
        {
            destination = default;
            return false;
        }

        var ownerLocation = world.GetEntityLocation(ownerId);
        destination = new PlaneCoord(ownerLocation.PlaneId, ownerLocation.Coord.Offset(direction));
        return true;
    }

    private static bool TryResolveInwardEntityTopologyDestination(
        WorldState world,
        PlaneCoord origin,
        Direction direction,
        out PlaneCoord destination)
    {
        if (world.GetOccupant(new PlaneCoord(origin.PlaneId, origin.Coord.Offset(direction))) is not { } ownerId ||
            !world.Entities.TryGetValue(ownerId, out var owner) ||
            !ConnectsInward(owner.TopologyPolicy) ||
            world.GetInventoryPlaneId(ownerId) is not { } inventoryPlaneId)
        {
            destination = default;
            return false;
        }

        var boundaryDirection = DirectionMath.Reverse(direction);
        destination = new PlaneCoord(inventoryPlaneId, PreferredBoundaryCoord(owner.InventoryWidth, owner.InventoryHeight, boundaryDirection));
        return true;
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

    private static void MaterializeMergedInventoryLayerEdges(
        WorldState world,
        IReadOnlyDictionary<PlaneCoord, TopologyNodeId> nodeIdsBySource,
        List<TopologyGraphEdge> edges)
    {
        foreach (var layer in world.MergedInventoryLayers)
        {
            var cellsByLayerCoord = ResolveMergedLayerCells(world, layer)
                .GroupBy(cell => cell.LayerCoord)
                .ToDictionary(group => group.Key, group => group.ToList());
            foreach (var originCell in cellsByLayerCoord.Values.SelectMany(cells => cells))
            {
                if (!nodeIdsBySource.TryGetValue(originCell.SourceCoord, out var sourceNodeId))
                {
                    continue;
                }

                foreach (var direction in DirectionMath.AllDirections)
                {
                    var destinationLayerCoord = originCell.LayerCoord.Offset(direction);
                    if (!cellsByLayerCoord.TryGetValue(destinationLayerCoord, out var destinationCells))
                    {
                        continue;
                    }

                    var isBlocked = DirectionMath.OrthogonalCorners(direction) is { } corners &&
                        IsOccupiedMergedLayerCoord(world, cellsByLayerCoord, originCell.LayerCoord.Offset(corners.First)) &&
                        IsOccupiedMergedLayerCoord(world, cellsByLayerCoord, originCell.LayerCoord.Offset(corners.Second));
                    foreach (var destinationCell in destinationCells)
                    {
                        if (!nodeIdsBySource.TryGetValue(destinationCell.SourceCoord, out var destinationNodeId))
                        {
                            continue;
                        }

                        edges.Add(new TopologyGraphEdge(
                            sourceNodeId,
                            direction,
                            destinationNodeId,
                            TopologyEdgeKind.MergedInventoryLayer,
                            isBlocked,
                            isBlocked ? FailureReason.MoveBlocked : null,
                            isBlocked ? $"merged inventory layer intercardinal adjacency {direction} is blocked by both orthogonal corners" : null));
                    }
                }
            }
        }
    }

    private static IReadOnlyList<MergedInventoryLayerCell> ResolveMergedLayerCells(WorldState world, MergedInventoryLayer layer)
    {
        var cells = new List<MergedInventoryLayerCell>();
        foreach (var space in layer.Spaces)
        {
            if (!world.Entities.TryGetValue(space.OwnerId, out var owner) ||
                world.GetRegisteredInventoryPlaneId(space.OwnerId) is not { } inventoryPlaneId)
            {
                continue;
            }

            for (var y = 0; y < owner.InventoryHeight; y++)
            {
                for (var x = 0; x < owner.InventoryWidth; x++)
                {
                    var sourceCoord = new GridCoord(x, y);
                    var planeCoord = new PlaneCoord(inventoryPlaneId, sourceCoord);
                    if (!world.Planes.TryGetValue(inventoryPlaneId, out var plane) ||
                        !plane.Contains(sourceCoord) ||
                        !world.TryGetNodeId(planeCoord, out _))
                    {
                        continue;
                    }

                    cells.Add(new MergedInventoryLayerCell(
                        layer,
                        space,
                        planeCoord,
                        new GridCoord(space.Origin.X + x, space.Origin.Y + y)));
                }
            }
        }

        return cells;
    }

    private static bool IsOccupiedMergedLayerCoord(
        WorldState world,
        IReadOnlyDictionary<GridCoord, List<MergedInventoryLayerCell>> cellsByLayerCoord,
        GridCoord layerCoord) =>
        cellsByLayerCoord.TryGetValue(layerCoord, out var cells) &&
        cells.Any(cell => world.GetOccupant(cell.SourceCoord) is not null);

    private static void TryAddSourceCellLinkEdge(
        WorldState world,
        IReadOnlyDictionary<PlaneCoord, TopologyNodeId> nodeIdsBySource,
        List<TopologyGraphEdge> edges,
        PlaneCoord source,
        Direction direction,
        PlaneCoord destination)
    {
        if (!nodeIdsBySource.TryGetValue(source, out var sourceNodeId) ||
            !nodeIdsBySource.TryGetValue(destination, out var destinationNodeId))
        {
            return;
        }

        var isValidDestination = world.Planes.TryGetValue(destination.PlaneId, out var plane) &&
            plane.Contains(destination.Coord) &&
            world.TryGetNodeId(destination, out _);

        edges.Add(new TopologyGraphEdge(
            sourceNodeId,
            direction,
            destinationNodeId,
            TopologyEdgeKind.SourceCellLink,
            IsBlocked: !isValidDestination,
            isValidDestination ? null : FailureReason.MoveOutOfBounds,
            isValidDestination ? null : $"source-cell link destination {destination} is not a valid node"));
    }
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

public sealed class TopologyTraversalService
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
        var graph = TopologyGraphMaterializer.Materialize(world);
        if (!graph.TryGetNode(new TopologyCellRef(origin), out var currentNode))
        {
            return [];
        }

        for (var index = 0; index < maxSteps; index++)
        {
            var edge = graph.GetOutgoingEdges(currentNode.Id, direction)
                .Where(edge => !edge.IsBlocked)
                .OrderBy(edge => edge.Kind == TopologyEdgeKind.DefaultGrid ? 1 : 0)
                .FirstOrDefault();
            if (edge is null || !graph.TryGetNode(edge.DestinationNodeId, out var destinationNode))
            {
                break;
            }

            steps.Add(new TopologyRayStep(index, currentNode.SourceCoord, destinationNode.SourceCoord, direction, edge.Kind));
            currentNode = destinationNode;
        }

        return steps;
    }

    public IReadOnlyList<TopologyFloodStep> Flood(WorldState world, PlaneCoord origin, int maxDepth)
    {
        if (maxDepth < 0)
        {
            return [];
        }

        var graph = TopologyGraphMaterializer.Materialize(world);
        if (!graph.TryGetNode(new TopologyCellRef(origin), out var originNode))
        {
            return [];
        }

        return TopologyGraphTraversalService.Flood(graph, originNode.Id, maxDepth)
            .Select(step => new TopologyFloodStep(
                step.SourceCoord,
                step.Distance,
                step.FromNodeId is null || !graph.TryGetNode(step.FromNodeId.Value, out var fromNode) ? null : fromNode.SourceCoord,
                step.Direction,
                step.Kind))
            .ToList();
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

