namespace GameGameGame.Core;

public sealed record AdjacencyEvaluation(
    bool AreAdjacent,
    Direction? Direction,
    bool IsIntercardinal,
    FailureReason? FailureReason,
    string? FailureDetail,
    TopologyNodeId? SourceNodeId = null,
    TopologyNodeId? DestinationNodeId = null,
    TopologyEdgeKind? EdgeKind = null);

public sealed record MovementEdgeResult(
    EntityId EntityId,
    TopologyNodeId SourceNodeId,
    PlaneCoord Source,
    TopologyLayoutCoord SourceLayoutCoord,
    TopologyDisplayCoord SourceDisplayCoord,
    Direction Direction,
    TopologyNodeId DestinationNodeId,
    PlaneCoord Destination,
    TopologyLayoutCoord DestinationLayoutCoord,
    TopologyDisplayCoord DestinationDisplayCoord,
    TopologyEdgeKind Kind,
    bool IsBlocked,
    FailureReason? FailureReason,
    string? FailureDetail);

public sealed class MovementService
{
    public RelocationEvaluation EvaluateRelocation(WorldState world, EntityId entityId, MovementDestination destination)
    {
        var trace = new TraceNode($"Relocate {entityId} -> {destination}", TraceStatus.Info);
        var movementEdge = TryResolveMovementEdge(world, destination);

        if (!world.Entities.ContainsKey(entityId))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"target {entityId} does not exist";
            return new RelocationEvaluation(false, Destination: null, trace, movementEdge?.DestinationNodeId, movementEdge?.Kind);
        }

        if (!TryResolveDestination(world, destination, trace, out var resolvedDestination))
        {
            return new RelocationEvaluation(false, Destination: null, trace, movementEdge?.DestinationNodeId, movementEdge?.Kind);
        }

        var destinationNodeId = movementEdge?.DestinationNodeId ??
            (world.TryGetNodeId(resolvedDestination, out var nodeId) ? new TopologyNodeId(nodeId.Value) : (TopologyNodeId?)null);

        trace.Add(TraceNode.Info("Resolved destination", resolvedDestination.ToString()));

        if (!CanPlace(world, resolvedDestination))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = $"cannot place into {resolvedDestination}";
            return new RelocationEvaluation(false, resolvedDestination, trace, destinationNodeId, movementEdge?.Kind);
        }

        if (destination is MovementDestination.AdjacentMovementDestination adjacentDestination
            && EvaluateAdjacency(world, world.GetEntityLocation(adjacentDestination.AnchorId), resolvedDestination).FailureReason == FailureReason.MoveBlocked)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.MoveBlocked;
            trace.Detail = $"diagonal movement {adjacentDestination.Direction} is blocked by both orthogonal corners";
            return new RelocationEvaluation(false, resolvedDestination, trace, destinationNodeId, movementEdge?.Kind);
        }

        trace.Status = TraceStatus.Success;
        return new RelocationEvaluation(true, resolvedDestination, trace, destinationNodeId, movementEdge?.Kind);
    }

    public bool TryRelocate(WorldState world, EntityId entityId, MovementDestination destination)
    {
        var evaluation = EvaluateRelocation(world, entityId, destination);
        return evaluation is { CanRelocate: true, Destination: { } resolvedDestination }
            && TryPlace(world, entityId, resolvedDestination);
    }

    public bool AreAdjacent(WorldState world, EntityId firstEntityId, EntityId secondEntityId)
    {
        return EvaluateAdjacency(world, firstEntityId, secondEntityId).AreAdjacent;
    }

    // Compatibility adapter: adjacency is resolved through the materialized topology graph and
    // returns source/destination node facts; coordinate inputs are projections for older callers.
    public AdjacencyEvaluation EvaluateAdjacency(WorldState world, EntityId firstEntityId, EntityId secondEntityId)
    {
        if (!world.Entities.ContainsKey(firstEntityId))
        {
            return new(false, null, false, FailureReason.ActorMissing, $"first entity {firstEntityId} does not exist");
        }

        if (!world.Entities.ContainsKey(secondEntityId))
        {
            return new(false, null, false, FailureReason.TargetMissing, $"second entity {secondEntityId} does not exist");
        }

        var first = world.GetEntityLocation(firstEntityId);
        var second = world.GetEntityLocation(secondEntityId);

        return EvaluateAdjacency(world, first, second, firstEntityId);
    }

    public AdjacencyEvaluation EvaluateAdjacency(WorldState world, PlaneCoord first, PlaneCoord second, EntityId? cornerAnchorId = null)
    {
        var graph = TopologyGraphMaterializer.Materialize(world);
        var hasSourceNode = graph.TryGetNode(new TopologyCellRef(first), out var sourceNode);
        foreach (var direction in DirectionMath.AllDirections)
        {
            var edge = hasSourceNode
                ? graph.GetOutgoingEdges(sourceNode.Id, direction)
                    .OrderBy(candidate => candidate.Kind == TopologyEdgeKind.DefaultGrid ? 1 : 0)
                    .FirstOrDefault()
                : null;
            TopologyNode? destinationNode = null;
            var hasDestinationNode = edge is not null && graph.TryGetNode(edge.DestinationNodeId, out destinationNode);
            if (edge is not null && hasDestinationNode && destinationNode!.SourceCoord == second && !edge.IsBlocked)
            {
                return new AdjacencyEvaluation(
                    AreAdjacent: true,
                    Direction: direction,
                    IsIntercardinal: DirectionMath.OrthogonalCorners(direction) is not null,
                    FailureReason: null,
                    FailureDetail: null,
                    sourceNode.Id,
                    destinationNode!.Id,
                    edge.Kind);
            }

            graph.TryGetNeighbor(new TopologyCellRef(first), direction, out var neighbor);

            if (TryCoordinateDirection(first, second, out var coordinateDirection) &&
                coordinateDirection == direction &&
                neighbor.Kind != TopologyEdgeKind.DefaultGrid &&
                neighbor.Destination != second)
            {
                return new AdjacencyEvaluation(
                    AreAdjacent: false,
                    Direction: direction,
                    IsIntercardinal: DirectionMath.OrthogonalCorners(direction) is not null,
                    FailureReason.TargetNotAdjacent,
                    $"topology direction {direction} from {first} resolves to {neighbor.Destination}, not {second}",
                    hasSourceNode ? sourceNode.Id : null,
                    hasDestinationNode ? destinationNode!.Id : null,
                    edge?.Kind ?? neighbor.Kind);
            }

            if (neighbor.Destination == second && neighbor.IsBlocked)
            {
                return new AdjacencyEvaluation(
                    AreAdjacent: false,
                    Direction: direction,
                    IsIntercardinal: DirectionMath.OrthogonalCorners(direction) is not null,
                    FailureReason: neighbor.FailureReason,
                    FailureDetail: neighbor.FailureDetail,
                    hasSourceNode ? sourceNode.Id : null,
                    hasDestinationNode ? destinationNode!.Id : null,
                    edge?.Kind ?? neighbor.Kind);
            }
        }

        return EvaluateDefaultCoordinateAdjacency(first, second);
    }

    public bool CanPlace(WorldState world, PlaneCoord destination)
    {
        return world.Planes.TryGetValue(destination.PlaneId, out var plane)
            && plane.Contains(destination.Coord)
            && world.TryGetNodeId(destination, out var nodeId)
            && !world.Occupancy.ContainsKey(nodeId);
    }

    public bool CanPlace(WorldState world, TopologyNodeId destinationNodeId)
    {
        return TryResolveTopologyNode(world, destinationNodeId, out var nodeId, out _)
            && !world.Occupancy.ContainsKey(nodeId);
    }

    public bool TryPlace(WorldState world, EntityId entityId, PlaneCoord destination)
    {
        if (!world.TryGetNodeId(destination, out var destinationNodeId))
        {
            return false;
        }

        return TryPlace(world, entityId, new TopologyNodeId(destinationNodeId.Value));
    }

    public bool TryPlace(WorldState world, EntityId entityId, TopologyNodeId destinationNodeId)
    {
        if (!CanPlace(world, destinationNodeId))
        {
            return false;
        }

        var entity = world.Entities[entityId];
        var nodeId = new NodeId(destinationNodeId.Value);

        world.Occupancy.Remove(entity.OccupiedNodeId);
        world.Occupancy[nodeId] = entityId;
        world.Entities[entityId] = entity with { OccupiedNodeId = nodeId };

        return true;
    }

    public bool CanMove(WorldState world, EntityId entityId, Direction direction)
    {
        return TryGetMovementEdge(world, entityId, direction, out var edge)
            && !edge.IsBlocked
            && CanPlace(world, edge.DestinationNodeId);
    }

    public bool TryMove(WorldState world, EntityId entityId, Direction direction)
    {
        return TryGetMoveDestinationNode(world, entityId, direction, out var destinationNodeId)
            && TryMove(world, entityId, destinationNodeId);
    }

    public bool TryGetMovementEdge(
        WorldState world,
        EntityId entityId,
        Direction direction,
        out MovementEdgeResult edgeResult)
    {
        edgeResult = default!;
        if (!world.Entities.TryGetValue(entityId, out var entity) ||
            !world.Nodes.TryGetValue(entity.OccupiedNodeId, out var currentNode))
        {
            return false;
        }

        var origin = new PlaneCoord(currentNode.PlaneId, currentNode.Coord);
        var graph = TopologyGraphMaterializer.Materialize(world);
        if (!graph.TryGetNode(new TopologyCellRef(origin), out var sourceNode))
        {
            return false;
        }

        var edge = graph.GetOutgoingEdges(sourceNode.Id, direction)
            .OrderBy(candidate => candidate.Kind == TopologyEdgeKind.DefaultGrid ? 1 : 0)
            .FirstOrDefault();
        if (edge is null || !graph.TryGetNode(edge.DestinationNodeId, out var destinationNode))
        {
            return false;
        }

        edgeResult = new MovementEdgeResult(
            entityId,
            sourceNode.Id,
            sourceNode.SourceCoord,
            sourceNode.LayoutCoord,
            sourceNode.DisplayCoord,
            edge.Direction,
            destinationNode.Id,
            destinationNode.SourceCoord,
            destinationNode.LayoutCoord,
            destinationNode.DisplayCoord,
            edge.Kind,
            edge.IsBlocked,
            edge.FailureReason,
            edge.FailureDetail);
        return true;
    }

    public bool TryMove(WorldState world, EntityId entityId, TopologyNodeId destinationNodeId)
    {
        if (!CanPlace(world, destinationNodeId))
        {
            return false;
        }

        return TryPlace(world, entityId, destinationNodeId);
    }

    // Compatibility adapter: callers that still need PlaneCoord receive the graph edge destination projection.
    public bool TryGetMoveDestination(
        WorldState world,
        EntityId entityId,
        Direction direction,
        out PlaneCoord destination)
    {
        if (TryGetMovementEdge(world, entityId, direction, out var edge))
        {
            destination = edge.Destination;
            return !edge.IsBlocked;
        }

        if (!world.Entities.TryGetValue(entityId, out var entity) ||
            !world.Nodes.TryGetValue(entity.OccupiedNodeId, out var currentNode))
        {
            destination = default;
            return false;
        }

        var origin = new PlaneCoord(currentNode.PlaneId, currentNode.Coord);
        var found = TopologyGraphMaterializer.Materialize(world)
            .TryGetNeighbor(new TopologyCellRef(origin), direction, out var neighbor);
        destination = neighbor.Destination;
        return found;
    }

    public bool TryGetMoveDestinationNode(
        WorldState world,
        EntityId entityId,
        Direction direction,
        out TopologyNodeId destinationNodeId)
    {
        if (!TryGetMovementEdge(world, entityId, direction, out var edge) || edge.IsBlocked)
        {
            destinationNodeId = default;
            return false;
        }

        destinationNodeId = edge.DestinationNodeId;
        return true;
    }

    public EntityId? GetBlockingEntity(WorldState world, EntityId entityId, Direction direction)
    {
        if (!TryGetMovementEdge(world, entityId, direction, out var edge) || edge.IsBlocked)
        {
            return null;
        }

        return world.Occupancy.TryGetValue(new NodeId(edge.DestinationNodeId.Value), out var occupant)
            ? occupant
            : null;
    }

    public IReadOnlyList<TopologyNeighbor> GetLegalMovementNeighbors(WorldState world, EntityId movingEntityId, PlaneCoord origin)
    {
        var graph = TopologyGraphMaterializer.Materialize(world);
        return DirectionMath.AllDirections.Select(direction =>
            {
                graph.TryGetNeighbor(new TopologyCellRef(origin), direction, out var neighbor);
                return neighbor;
            })
            .Where(neighbor => !neighbor.IsBlocked && CanOccupyForPath(world, movingEntityId, neighbor.Destination))
            .ToList();
    }

    public bool CanOccupyForPath(WorldState world, EntityId movingEntityId, PlaneCoord destination)
    {
        return world.Planes.TryGetValue(destination.PlaneId, out var plane)
            && plane.Contains(destination.Coord)
            && world.TryGetNodeId(destination, out var nodeId)
            && (!world.Occupancy.TryGetValue(nodeId, out var occupant) || occupant == movingEntityId);
    }

    private static bool TryResolveTopologyNode(
        WorldState world,
        TopologyNodeId topologyNodeId,
        out NodeId nodeId,
        out Node node)
    {
        nodeId = new NodeId(topologyNodeId.Value);
        return world.Nodes.TryGetValue(nodeId, out node!);
    }

    private bool TryResolveDestination(
        WorldState world,
        MovementDestination destination,
        TraceNode trace,
        out PlaneCoord resolvedDestination)
    {
        switch (destination)
        {
            case MovementDestination.PlaneMovementDestination planeDestination:
                resolvedDestination = planeDestination.Coord;
                if (world.Planes.TryGetValue(resolvedDestination.PlaneId, out var plane) &&
                    plane.Contains(resolvedDestination.Coord) &&
                    world.TryGetNodeId(resolvedDestination, out _))
                {
                    return true;
                }

                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.InvalidPlacement;
                trace.Detail = $"destination {resolvedDestination} is not a valid plane coordinate";
                return false;

            case MovementDestination.InventorySlotMovementDestination inventoryDestination:
                if (world.GetInventoryPlaneId(inventoryDestination.OwnerId) is not { } inventoryPlaneId)
                {
                    resolvedDestination = default;
                    trace.Status = TraceStatus.Failure;
                    trace.Reason = FailureReason.ActorHasNoInventory;
                    trace.Detail = $"{inventoryDestination.OwnerId} has no usable inventory plane";
                    return false;
                }

                resolvedDestination = new PlaneCoord(inventoryPlaneId, inventoryDestination.Coord);
                if (world.Planes.TryGetValue(resolvedDestination.PlaneId, out var inventoryPlane) &&
                    inventoryPlane.Contains(resolvedDestination.Coord) &&
                    world.TryGetNodeId(resolvedDestination, out _))
                {
                    return true;
                }

                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.InvalidInventoryDestination;
                trace.Detail = $"inventory destination {resolvedDestination} is outside {inventoryPlaneId}";
                return false;

            case MovementDestination.AdjacentMovementDestination adjacentDestination:
                if (!world.Entities.ContainsKey(adjacentDestination.AnchorId))
                {
                    resolvedDestination = default;
                    trace.Status = TraceStatus.Failure;
                    trace.Reason = FailureReason.TargetMissing;
                    trace.Detail = $"anchor {adjacentDestination.AnchorId} does not exist";
                    return false;
                }

                if (TryGetMovementEdge(world, adjacentDestination.AnchorId, adjacentDestination.Direction, out var edge))
                {
                    resolvedDestination = edge.Destination;
                    if (!edge.IsBlocked)
                    {
                        return true;
                    }

                    trace.Status = TraceStatus.Failure;
                    trace.Reason = edge.FailureReason ?? FailureReason.MoveOutOfBounds;
                    trace.Detail = edge.FailureDetail ?? $"adjacent destination {resolvedDestination} is outside the anchor plane";
                    return false;
                }

                var anchorLocation = world.GetEntityLocation(adjacentDestination.AnchorId);
                TopologyGraphMaterializer.Materialize(world)
                    .TryGetNeighbor(new TopologyCellRef(anchorLocation), adjacentDestination.Direction, out var neighbor);

                resolvedDestination = neighbor.Destination;

                trace.Status = TraceStatus.Failure;
                trace.Reason = neighbor.FailureReason ?? FailureReason.MoveOutOfBounds;
                trace.Detail = neighbor.FailureDetail ?? $"adjacent destination {resolvedDestination} is outside the anchor plane";
                return false;

            default:
                resolvedDestination = default;
                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.InvalidPlacement;
                trace.Detail = $"unsupported movement destination {destination}";
                return false;
        }
    }

    private MovementEdgeResult? TryResolveMovementEdge(WorldState world, MovementDestination destination)
    {
        return destination is MovementDestination.AdjacentMovementDestination adjacentDestination &&
            TryGetMovementEdge(world, adjacentDestination.AnchorId, adjacentDestination.Direction, out var edge)
            ? edge
            : null;
    }

    private static AdjacencyEvaluation EvaluateDefaultCoordinateAdjacency(PlaneCoord first, PlaneCoord second)
    {
        if (first.PlaneId != second.PlaneId)
        {
            return new(false, null, false, FailureReason.TargetNotAdjacent, $"{second} is not on the same plane as {first}");
        }

        var deltaX = second.Coord.X - first.Coord.X;
        var deltaY = second.Coord.Y - first.Coord.Y;
        if (Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)) != 1 || DirectionFromDelta(deltaX, deltaY) is not { } direction)
        {
            return new(false, null, false, FailureReason.TargetNotAdjacent, $"{second} is not adjacent to {first}");
        }

        return new(true, direction, DirectionMath.OrthogonalCorners(direction) is not null, null, null);
    }

    private static bool TryCoordinateDirection(PlaneCoord first, PlaneCoord second, out Direction direction)
    {
        direction = default;
        if (first.PlaneId != second.PlaneId)
        {
            return false;
        }

        if (DirectionFromDelta(second.Coord.X - first.Coord.X, second.Coord.Y - first.Coord.Y) is not { } resolved)
        {
            return false;
        }

        direction = resolved;
        return true;
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
