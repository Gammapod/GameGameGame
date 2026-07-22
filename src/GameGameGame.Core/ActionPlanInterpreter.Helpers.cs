namespace GameGameGame.Core;

public sealed partial class ActionPlanInterpreter
{
    private static Direction TurnLeft(Direction direction) =>
        DirectionMath.Rotate(direction, -2);

    private static Direction TurnRight(Direction direction) =>
        DirectionMath.Rotate(direction, 2);

    private static Direction Reverse(Direction direction) =>
        DirectionMath.Reverse(direction);

    private static Direction Clockwise(Direction direction) =>
        DirectionMath.Rotate(direction, 2);

    private static Direction Anticlockwise(Direction direction) =>
        DirectionMath.Rotate(direction, -2);

    private static bool TryResolveMoveDirection(
        ActionPlanMoveDirectionMode mode,
        ActionPlanContext context,
        out Direction direction,
        out TraceNode? readTrace,
        out string? failureDetail)
    {
        readTrace = null;
        failureDetail = null;
        if (TryAbsoluteMoveDirection(mode, out direction))
        {
            return true;
        }

        if (!context.TryRead<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing, out var facingReadTrace))
        {
            readTrace = facingReadTrace;
            failureDetail = facingReadTrace.Detail;
            return false;
        }

        readTrace = facingReadTrace;
        direction = DirectionMath.Rotate(facing.Value, RelativeEighthTurns(mode));
        return true;
    }

    private static bool TryAbsoluteMoveDirection(ActionPlanMoveDirectionMode mode, out Direction direction)
    {
        switch (mode)
        {
            case ActionPlanMoveDirectionMode.North:
                direction = Direction.North;
                return true;
            case ActionPlanMoveDirectionMode.NorthEast:
                direction = Direction.NorthEast;
                return true;
            case ActionPlanMoveDirectionMode.East:
                direction = Direction.East;
                return true;
            case ActionPlanMoveDirectionMode.SouthEast:
                direction = Direction.SouthEast;
                return true;
            case ActionPlanMoveDirectionMode.South:
                direction = Direction.South;
                return true;
            case ActionPlanMoveDirectionMode.SouthWest:
                direction = Direction.SouthWest;
                return true;
            case ActionPlanMoveDirectionMode.West:
                direction = Direction.West;
                return true;
            case ActionPlanMoveDirectionMode.NorthWest:
                direction = Direction.NorthWest;
                return true;
            default:
                direction = default;
                return false;
        }
    }

    private static int RelativeEighthTurns(ActionPlanMoveDirectionMode mode) => mode switch
    {
        ActionPlanMoveDirectionMode.Forward => 0,
        ActionPlanMoveDirectionMode.ForwardRight => 1,
        ActionPlanMoveDirectionMode.Right => 2,
        ActionPlanMoveDirectionMode.BackRight => 3,
        ActionPlanMoveDirectionMode.Back => 4,
        ActionPlanMoveDirectionMode.BackLeft => -3,
        ActionPlanMoveDirectionMode.Left => -2,
        ActionPlanMoveDirectionMode.ForwardLeft => -1,
        _ => 0
    };

    private static int ManhattanDistance(GridCoord first, GridCoord second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);

    private static int ChebyshevDistance(GridCoord first, GridCoord second) =>
        Math.Max(Math.Abs(first.X - second.X), Math.Abs(first.Y - second.Y));

    private static IReadOnlyList<Direction> SeekDirections() =>
        [Direction.North, Direction.South, Direction.West, Direction.East];

    private static EntityId? FindFirstCarriedEntity(WorldState world, EntityId actorId)
    {
        var inventoryPlaneId = world.GetInventoryPlaneId(actorId);
        if (inventoryPlaneId is null)
        {
            return null;
        }

        return world.Occupancy
            .Where(entry => world.Nodes.TryGetValue(entry.Key, out var node) && node.PlaneId == inventoryPlaneId)
            .OrderBy(entry => world.Nodes[entry.Key].Coord.Y)
            .ThenBy(entry => world.Nodes[entry.Key].Coord.X)
            .Select(entry => (EntityId?)entry.Value)
            .FirstOrDefault();
    }

    private bool TryReadTransferTarget(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        TraceNode trace,
        string stepName,
        out EntityId targetId)
    {
        targetId = default;
        if (!context.TryRead<EntityPlanValue>(ActionPlanSlot.Target, out var target, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return false;
        }

        trace.Add(readTrace);
        targetId = target.Value;

        if (targetId == actorId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetIsActor;
            trace.Detail = $"{stepName} cannot transfer with self";
            return false;
        }

        if (!world.Entities.ContainsKey(targetId))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"target {targetId} does not exist";
            return false;
        }

        return true;
    }

    private PlanEffectResult TransferToFirstOpenInventory(
        WorldState world,
        EntityId actorId,
        EntityId carriedId,
        EntityId destinationOwnerId,
        TraceNode trace,
        string verb)
    {
        var carriedLocation = world.GetEntityLocation(carriedId);
        var carried = world.Entities[carriedId];
        if (!world.Entities.TryGetValue(destinationOwnerId, out var destinationOwner))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"destination owner {destinationOwnerId} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (world.GetRegisteredInventoryPlaneId(destinationOwnerId) is not { } inventoryPlaneId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ActorHasNoInventory;
            trace.Detail = $"{destinationOwner.Name} has no inventory plane";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!destinationOwner.HasUsableInventory)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ActorInventoryUnusable;
            trace.Detail = $"{destinationOwner.Name} inventory dimensions are {destinationOwner.InventoryWidth}x{destinationOwner.InventoryHeight}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!world.Planes.TryGetValue(inventoryPlaneId, out _))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidInventoryDestination;
            trace.Detail = $"inventory plane {inventoryPlaneId} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var constrainedRelocation = new ConstrainedInventoryRelocationService(_movement, ignoredPolicyOwnerId: actorId);
        var placement = new InventoryBoundaryPolicyService().EvaluatePolicyAwarePlacement(
            world,
            carriedId,
            destinationOwnerId,
            constrainedRelocation,
            actorId);
        trace.Add(placement.Trace);
        if (placement is { CanRelocate: true, Destination: { } resolvedDestination })
        {
            _movement.TryPlace(world, carriedId, resolvedDestination);
            trace.Status = TraceStatus.Success;
            trace.Detail = $"{verb} {carriedId} ({carried.Name}) from {carriedLocation.Coord} to {resolvedDestination.Coord}";
            return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = placement.Trace.Reason == FailureReason.None ? FailureReason.InvalidPlacement : placement.Trace.Reason;
        trace.Detail = $"no inventory coordinate in {inventoryPlaneId} can accept {carriedId}";
        if (!string.IsNullOrWhiteSpace(placement.Trace.Detail))
        {
            trace.Detail += $"; last failure: {placement.Trace.Detail}";
        }

        return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
    }

    private static EntityId GeneratePlaceholderEntityId(WorldState world)
    {
        var candidate = new EntityId("placeholderRock");
        var suffix = 2;
        while (world.Entities.ContainsKey(candidate))
        {
            candidate = new EntityId($"placeholderRock{suffix}");
            suffix++;
        }

        return candidate;
    }
}
