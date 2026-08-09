namespace GameGameGame.Core;

public sealed record ControlledActorAffordances(
    EntityId ActorId,
    IReadOnlyList<ControlledActorDirectionAffordance> MovementDirections,
    IReadOnlyList<ControlledActorEntityAffordance> PickupSources,
    IReadOnlyDictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>> PickupDestinationsByTargetId,
    IReadOnlyList<ControlledActorEntityAffordance> DropSources,
    IReadOnlyDictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>> DropDestinationsByTargetId,
    IReadOnlyList<ControlledActorEntityAffordance> EnterTargets,
    IReadOnlyList<ControlledActorDirectionAffordance> ExitDirections)
{
    public IReadOnlyList<ControlledActorDestinationAffordance> PickupDestinations(EntityId targetId) =>
        PickupDestinationsByTargetId.TryGetValue(targetId, out var destinations) ? destinations : [];

    public IReadOnlyList<ControlledActorDestinationAffordance> DropDestinations(EntityId targetId) =>
        DropDestinationsByTargetId.TryGetValue(targetId, out var destinations) ? destinations : [];
}

public sealed record ControlledActorDirectionAffordance(
    Direction Direction,
    PlaneCoord? Destination,
    bool CanExecute,
    FailureReason? FailureReason,
    string? FailureDetail,
    EntityId? BlockingEntityId = null,
    TopologyNodeId? DestinationNodeId = null,
    TopologyEdgeKind? EdgeKind = null);

public sealed record ControlledActorEntityAffordance(
    EntityId TargetId,
    PlaneCoord? Source,
    bool CanExecute,
    FailureReason? FailureReason,
    string? FailureDetail,
    TopologyNodeId? SourceNodeId = null,
    TopologyEdgeKind? EdgeKind = null);

public sealed record ControlledActorDestinationAffordance(
    EntityId TargetId,
    PlaneCoord Destination,
    bool CanExecute,
    FailureReason? FailureReason,
    string? FailureDetail,
    EntityId? BlockingEntityId = null,
    TopologyNodeId? DestinationNodeId = null,
    TopologyEdgeKind? EdgeKind = null);

public sealed class ControlledActorAffordanceService(MovementService movement)
{
    private static readonly IReadOnlyList<Direction> Directions = DirectionMath.AllDirections;

    public ControlledActorAffordances Query(WorldState world, EntityId actorId)
    {
        var movementDirections = QueryMovementDirections(world, actorId);
        var pickupDestinationsByTargetId = new Dictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>>();
        var pickupSources = QueryPickupSources(world, actorId, pickupDestinationsByTargetId);
        var dropDestinationsByTargetId = new Dictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>>();
        var dropSources = QueryDropSources(world, actorId, dropDestinationsByTargetId);
        var enterTargets = QueryEnterTargets(world, actorId);
        var exitDirections = QueryExitDirections(world, actorId);

        return new ControlledActorAffordances(
            actorId,
            movementDirections,
            pickupSources,
            pickupDestinationsByTargetId,
            dropSources,
            dropDestinationsByTargetId,
            enterTargets,
            exitDirections);
    }

    private IReadOnlyList<ControlledActorDirectionAffordance> QueryMovementDirections(WorldState world, EntityId actorId) =>
        Directions.Select(direction =>
        {
            var evaluation = new MoveAction(direction).Evaluate(world, actorId, movement);
            var movementEdge = movement.TryGetMovementEdge(world, actorId, direction, out var resolvedMovementEdge)
                ? resolvedMovementEdge
                : null;
            var destination = movementEdge is { IsBlocked: false }
                ? movementEdge.Destination
                : (PlaneCoord?)null;
            var destinationNodeId = movementEdge is { IsBlocked: false }
                ? movementEdge.DestinationNodeId
                : (TopologyNodeId?)null;
            var failure = evaluation.CanExecute ? null : FindFailure(evaluation.Trace) ?? evaluation.Trace;
            var blockingEntity = destination is { } concreteDestination ? world.GetOccupant(concreteDestination) : null;
            return new ControlledActorDirectionAffordance(
                direction,
                destination,
                evaluation.CanExecute,
                ToFailureReason(failure),
                failure?.Detail,
                blockingEntity,
                destinationNodeId,
                movementEdge?.Kind);
        }).ToList();

    private IReadOnlyList<ControlledActorEntityAffordance> QueryPickupSources(
        WorldState world,
        EntityId actorId,
        Dictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>> destinationsByTargetId)
    {
        if (!world.Entities.ContainsKey(actorId))
        {
            return [];
        }

        var adjacentTargets = world.Occupancy.Values
            .Where(targetId => targetId != actorId)
            .Select(targetId => (TargetId: targetId, Adjacency: movement.EvaluateAdjacency(world, actorId, targetId)))
            .Where(candidate => candidate.Adjacency.AreAdjacent)
            .OrderBy(candidate => world.GetEntityLocation(candidate.TargetId).Coord.Y)
            .ThenBy(candidate => world.GetEntityLocation(candidate.TargetId).Coord.X)
            .ThenBy(candidate => candidate.TargetId.Value)
            .ToList();

        var result = new List<ControlledActorEntityAffordance>();
        foreach (var (targetId, adjacency) in adjacentTargets)
        {
            var destinations = QueryPickupDestinations(world, actorId, targetId);
            destinationsByTargetId[targetId] = destinations;
            var firstFailure = destinations.FirstOrDefault(destination => !destination.CanExecute);
            result.Add(new ControlledActorEntityAffordance(
                targetId,
                world.GetEntityLocation(targetId),
                destinations.Any(destination => destination.CanExecute),
                firstFailure?.FailureReason,
                firstFailure?.FailureDetail,
                adjacency.DestinationNodeId,
                adjacency.EdgeKind));
        }

        return result;
    }

    private IReadOnlyList<ControlledActorDestinationAffordance> QueryPickupDestinations(WorldState world, EntityId actorId, EntityId targetId)
    {
        if (world.GetRegisteredInventoryPlaneId(actorId) is not { } inventoryPlaneId || !world.Planes.TryGetValue(inventoryPlaneId, out var inventoryPlane))
        {
            return [];
        }

        return PlaneCoords(inventoryPlane).Select(destination => EvaluateDestination(targetId, destination, new PickupAction(targetId, destination), world, actorId)).ToList();
    }

    private IReadOnlyList<ControlledActorEntityAffordance> QueryDropSources(
        WorldState world,
        EntityId actorId,
        Dictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>> destinationsByTargetId)
    {
        if (world.GetRegisteredInventoryPlaneId(actorId) is not { } inventoryPlaneId)
        {
            return [];
        }

        var carried = world.Occupancy
            .Where(entry => world.Nodes.TryGetValue(entry.Key, out var node) && node.PlaneId == inventoryPlaneId)
            .Select(entry => entry.Value)
            .OrderBy(targetId => world.GetEntityLocation(targetId).Coord.Y)
            .ThenBy(targetId => world.GetEntityLocation(targetId).Coord.X)
            .ThenBy(targetId => targetId.Value)
            .ToList();

        var result = new List<ControlledActorEntityAffordance>();
        foreach (var targetId in carried)
        {
            var destinations = QueryDropDestinations(world, actorId, targetId);
            destinationsByTargetId[targetId] = destinations;
            var firstFailure = destinations.FirstOrDefault(destination => !destination.CanExecute);
            result.Add(new ControlledActorEntityAffordance(
                targetId,
                world.GetEntityLocation(targetId),
                destinations.Any(destination => destination.CanExecute),
                firstFailure?.FailureReason,
                firstFailure?.FailureDetail));
        }

        return result;
    }

    private IReadOnlyList<ControlledActorDestinationAffordance> QueryDropDestinations(WorldState world, EntityId actorId, EntityId targetId)
    {
        var actorLocation = world.GetEntityLocation(actorId);
        if (!world.Planes.TryGetValue(actorLocation.PlaneId, out var actorPlane))
        {
            return [];
        }

        return PlaneCoords(actorPlane).Select(destination => EvaluateDestination(targetId, destination, new DropAction(targetId, destination), world, actorId)).ToList();
    }

    private IReadOnlyList<ControlledActorEntityAffordance> QueryEnterTargets(WorldState world, EntityId actorId)
    {
        if (!world.Entities.ContainsKey(actorId))
        {
            return [];
        }

        return world.Occupancy.Values
            .Where(targetId => targetId != actorId)
            .Select(targetId => (TargetId: targetId, Adjacency: movement.EvaluateAdjacency(world, actorId, targetId)))
            .Where(candidate => candidate.Adjacency.AreAdjacent)
            .OrderBy(candidate => world.GetEntityLocation(candidate.TargetId).Coord.Y)
            .ThenBy(candidate => world.GetEntityLocation(candidate.TargetId).Coord.X)
            .ThenBy(candidate => candidate.TargetId.Value)
            .Select(candidate =>
            {
                var targetId = candidate.TargetId;
                var evaluation = new EnterAction(targetId).Evaluate(world, actorId, movement);
                var failure = evaluation.CanExecute ? null : FindFailure(evaluation.Trace) ?? evaluation.Trace;
                return new ControlledActorEntityAffordance(
                    targetId,
                    world.GetEntityLocation(targetId),
                    evaluation.CanExecute,
                    ToFailureReason(failure),
                    failure?.Detail,
                    candidate.Adjacency.DestinationNodeId,
                    candidate.Adjacency.EdgeKind);
            })
            .ToList();
    }

    private IReadOnlyList<ControlledActorDirectionAffordance> QueryExitDirections(WorldState world, EntityId actorId) =>
        Directions.Select(direction =>
        {
            var actionEvaluation = new ExitAction(direction).Evaluate(world, actorId, movement);
            var constrained = TryEvaluateExitDestination(world, actorId, direction, out var destination, out var blockingEntity);
            var destinationNodeId = destination is { } concreteDestination && world.TryGetNodeId(concreteDestination, out var nodeId)
                ? new TopologyNodeId(nodeId.Value)
                : (TopologyNodeId?)null;
            var failure = actionEvaluation.CanExecute ? null : FindFailure(actionEvaluation.Trace) ?? actionEvaluation.Trace;
            return new ControlledActorDirectionAffordance(
                direction,
                destination,
                actionEvaluation.CanExecute,
                ToFailureReason(failure),
                failure?.Detail,
                blockingEntity ?? (constrained?.CanRelocate == false && destination is { } concreteDestinationForOccupant ? world.GetOccupant(concreteDestinationForOccupant) : null),
                destinationNodeId);
        }).ToList();

    private ConstrainedRelocationEvaluation? TryEvaluateExitDestination(WorldState world, EntityId actorId, Direction direction, out PlaneCoord? destination, out EntityId? blockingEntity)
    {
        destination = null;
        blockingEntity = null;
        if (!world.Entities.ContainsKey(actorId))
        {
            return null;
        }

        var actorLocation = world.GetEntityLocation(actorId);
        if (!InventoryPlaneOwnership.TryFindOwner(world, actorLocation.PlaneId, out var containerId))
        {
            return null;
        }

        movement.TryGetMoveDestination(world, containerId, direction, out var topologyDestination);
        destination = topologyDestination;
        blockingEntity = world.GetOccupant(destination.Value);
        return new ConstrainedInventoryRelocationService(movement, ignoredPolicyOwnerId: actorId)
            .Evaluate(world, actorId, MovementDestination.AdjacentTo(containerId, direction));
    }

    private ControlledActorDestinationAffordance EvaluateDestination(EntityId targetId, PlaneCoord destination, IActionIntent action, WorldState world, EntityId actorId)
    {
        var evaluation = action.Evaluate(world, actorId, movement);
        var failure = evaluation.CanExecute ? null : FindFailure(evaluation.Trace) ?? evaluation.Trace;
        var destinationNodeId = world.TryGetNodeId(destination, out var nodeId) ? new TopologyNodeId(nodeId.Value) : (TopologyNodeId?)null;
        return new ControlledActorDestinationAffordance(
            targetId,
            destination,
            evaluation.CanExecute,
            ToFailureReason(failure),
            failure?.Detail,
            world.GetOccupant(destination),
            destinationNodeId);
    }

    private static IEnumerable<PlaneCoord> PlaneCoords(Plane plane)
    {
        for (var y = 0; y < plane.Height; y++)
        {
            for (var x = 0; x < plane.Width; x++)
            {
                yield return new PlaneCoord(plane.Id, new GridCoord(x, y));
            }
        }
    }

    private static FailureReason? ToFailureReason(TraceNode? failure) =>
        failure is null || failure.Reason == FailureReason.None ? null : failure.Reason;

    private static TraceNode? FindFailure(TraceNode trace)
    {
        if (trace.Status == TraceStatus.Failure)
        {
            return trace;
        }

        foreach (var child in trace.Children)
        {
            var failure = FindFailure(child);
            if (failure is not null)
            {
                return failure;
            }
        }

        return null;
    }
}
