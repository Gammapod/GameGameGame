namespace GameGameGame.Core;

public sealed class MovementService
{
    public RelocationEvaluation EvaluateRelocation(WorldState world, EntityId entityId, MovementDestination destination)
    {
        var trace = new TraceNode($"Relocate {entityId} -> {destination}", TraceStatus.Info);

        if (!world.Entities.ContainsKey(entityId))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"target {entityId} does not exist";
            return new RelocationEvaluation(false, Destination: null, trace);
        }

        if (!TryResolveDestination(world, destination, trace, out var resolvedDestination))
        {
            return new RelocationEvaluation(false, Destination: null, trace);
        }

        trace.Add(TraceNode.Info("Resolved destination", resolvedDestination.ToString()));

        if (!CanPlace(world, resolvedDestination))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = $"cannot place into {resolvedDestination}";
            return new RelocationEvaluation(false, resolvedDestination, trace);
        }

        trace.Status = TraceStatus.Success;
        return new RelocationEvaluation(true, resolvedDestination, trace);
    }

    public bool TryRelocate(WorldState world, EntityId entityId, MovementDestination destination)
    {
        var evaluation = EvaluateRelocation(world, entityId, destination);
        return evaluation is { CanRelocate: true, Destination: { } resolvedDestination }
            && TryPlace(world, entityId, resolvedDestination);
    }

    public bool AreAdjacent(WorldState world, EntityId firstEntityId, EntityId secondEntityId)
    {
        var first = world.GetEntityLocation(firstEntityId);
        var second = world.GetEntityLocation(secondEntityId);

        return first.PlaneId == second.PlaneId
            && Math.Abs(first.Coord.X - second.Coord.X) + Math.Abs(first.Coord.Y - second.Coord.Y) == 1;
    }

    public bool CanPlace(WorldState world, PlaneCoord destination)
    {
        return world.Planes.TryGetValue(destination.PlaneId, out var plane)
            && plane.Contains(destination.Coord)
            && world.TryGetNodeId(destination, out var nodeId)
            && !world.Occupancy.ContainsKey(nodeId);
    }

    public bool TryPlace(WorldState world, EntityId entityId, PlaneCoord destination)
    {
        if (!CanPlace(world, destination))
        {
            return false;
        }

        var entity = world.Entities[entityId];
        var destinationNodeId = world.GetNodeId(destination);

        world.Occupancy.Remove(entity.OccupiedNodeId);
        world.Occupancy[destinationNodeId] = entityId;
        world.Entities[entityId] = entity with { OccupiedNodeId = destinationNodeId };

        return true;
    }

    public bool CanMove(WorldState world, EntityId entityId, Direction direction)
    {
        return TryGetMoveDestination(world, entityId, direction, out var destination)
            && CanPlace(world, destination);
    }

    public bool TryMove(WorldState world, EntityId entityId, Direction direction)
    {
        var entity = world.Entities[entityId];
        var currentNode = world.Nodes[entity.OccupiedNodeId];
        var destinationCoord = currentNode.Coord.Offset(direction);

        if (!CanMove(world, entityId, direction))
        {
            return false;
        }

        return TryPlace(world, entityId, new PlaneCoord(currentNode.PlaneId, destinationCoord));
    }

    public bool TryGetMoveDestination(
        WorldState world,
        EntityId entityId,
        Direction direction,
        out PlaneCoord destination)
    {
        var entity = world.Entities[entityId];
        var currentNode = world.Nodes[entity.OccupiedNodeId];
        var destinationCoord = currentNode.Coord.Offset(direction);
        destination = new PlaneCoord(currentNode.PlaneId, destinationCoord);

        return world.Planes[currentNode.PlaneId].Contains(destinationCoord)
            && world.TryGetNodeId(destination, out _);
    }

    public EntityId? GetBlockingEntity(WorldState world, EntityId entityId, Direction direction)
    {
        if (!TryGetMoveDestination(world, entityId, direction, out var destination))
        {
            return null;
        }

        return world.GetOccupant(destination);
    }

    private static bool TryResolveDestination(
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

                var anchorLocation = world.GetEntityLocation(adjacentDestination.AnchorId);
                var adjacentCoord = anchorLocation.Coord.Offset(adjacentDestination.Direction);
                resolvedDestination = new PlaneCoord(anchorLocation.PlaneId, adjacentCoord);
                if (world.Planes[anchorLocation.PlaneId].Contains(adjacentCoord) &&
                    world.TryGetNodeId(resolvedDestination, out _))
                {
                    return true;
                }

                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.MoveOutOfBounds;
                trace.Detail = $"adjacent destination {resolvedDestination} is outside the anchor plane";
                return false;

            default:
                resolvedDestination = default;
                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.InvalidPlacement;
                trace.Detail = $"unsupported movement destination {destination}";
                return false;
        }
    }
}
