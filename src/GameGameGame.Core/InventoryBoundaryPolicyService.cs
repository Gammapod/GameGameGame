namespace GameGameGame.Core;

public sealed class InventoryBoundaryPolicyService
{
    public IReadOnlyList<PlaneCoord> OrderedEnterPolicyDestinations(
        WorldState world,
        EntityId destinationOwnerId,
        EntityId? ignoredPolicyOwnerId = null)
    {
        if (!world.Entities.TryGetValue(destinationOwnerId, out var destinationOwner) ||
            world.GetRegisteredInventoryPlaneId(destinationOwnerId) is not { } inventoryPlaneId ||
            !world.Planes.TryGetValue(inventoryPlaneId, out var inventoryPlane))
        {
            return [];
        }

        var candidates = PlaneCoords(inventoryPlane).ToList();

        var effectiveEnterPolicy = destinationOwnerId == ignoredPolicyOwnerId
            ? EntityEnterPolicy.FirstUnoccupiedRowMajor
            : destinationOwner.EffectiveEnterPolicy;

        return effectiveEnterPolicy switch
        {
            EntityEnterPolicy.FarthestFromOccupied => candidates
                .Where(coord => world.GetOccupant(coord) is null)
                .OrderByDescending(candidate => DistanceFromNearestOccupied(world, inventoryPlane, candidate.Coord))
                .ThenBy(candidate => candidate.Coord.Y)
                .ThenBy(candidate => candidate.Coord.X)
                .ToList(),
            _ => candidates
        };
    }

    public ConstrainedRelocationEvaluation EvaluatePolicyAwarePlacement(
        WorldState world,
        EntityId movingEntityId,
        EntityId destinationOwnerId,
        ConstrainedInventoryRelocationService constrainedRelocation,
        EntityId? ignoredEnterPolicyOwnerId = null)
    {
        var trace = new TraceNode($"Apply enter policy for {movingEntityId} -> {destinationOwnerId}", TraceStatus.Info);
        if (!world.Entities.TryGetValue(destinationOwnerId, out var destinationOwner))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"destination owner {destinationOwnerId} does not exist";
            return new ConstrainedRelocationEvaluation(false, null, trace);
        }

        ActionResolution? lastFailure = null;
        var effectiveEnterPolicy = destinationOwnerId == ignoredEnterPolicyOwnerId
            ? EntityEnterPolicy.FirstUnoccupiedRowMajor
            : destinationOwner.EffectiveEnterPolicy;

        foreach (var destination in OrderedEnterPolicyDestinations(world, destinationOwnerId, ignoredEnterPolicyOwnerId))
        {
            var evaluation = constrainedRelocation.Evaluate(world, movingEntityId, MovementDestination.Plane(destination));
            trace.Add(evaluation.Trace);
            if (evaluation is { CanRelocate: true, Destination: { } resolvedDestination })
            {
                trace.Status = TraceStatus.Success;
                trace.Detail = $"{effectiveEnterPolicy} selected {resolvedDestination.Coord}";
                return new ConstrainedRelocationEvaluation(true, resolvedDestination, trace);
            }

            lastFailure = new ActionResolution(false, ConsumesTurn: false, ContinuePlan: false, evaluation.Trace);
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = lastFailure?.Trace.Reason ?? FailureReason.InvalidPlacement;
        trace.Detail = $"enter policy found no valid destination in {destinationOwnerId}";
        if (!string.IsNullOrWhiteSpace(lastFailure?.Trace.Detail))
        {
            trace.Detail += $"; last failure: {lastFailure.Trace.Detail}";
        }

        return new ConstrainedRelocationEvaluation(false, null, trace);
    }

    public InventoryBoundaryPolicyEvaluation EvaluateExitPolicy(
        WorldState world,
        EntityId movingEntityId,
        PlaneCoord destination,
        EntityId? ignoredPolicyOwnerId = null)
    {
        var trace = new TraceNode($"Apply exit policy for {movingEntityId} -> {destination}", TraceStatus.Info);
        if (!world.Entities.ContainsKey(movingEntityId))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"moving entity {movingEntityId} does not exist";
            return new(false, trace);
        }

        var source = world.GetEntityLocation(movingEntityId);
        if (source.PlaneId == destination.PlaneId ||
            !InventoryPlaneOwnership.TryFindOwner(world, source.PlaneId, out var sourceOwnerId) ||
            sourceOwnerId == ignoredPolicyOwnerId ||
            !world.Entities.TryGetValue(sourceOwnerId, out var sourceOwner))
        {
            trace.Status = TraceStatus.Success;
            trace.Detail = "no source inventory exit policy applies";
            return new(true, trace);
        }

        if (sourceOwner.EffectiveExitPolicy == EntityExitPolicy.AnyCell)
        {
            trace.Status = TraceStatus.Success;
            trace.Detail = "AnyCell allows exit";
            return new(true, trace);
        }

        var ownerLocation = world.GetEntityLocation(sourceOwnerId);
        var direction = DirectionFromDelta(destination.Coord.X - ownerLocation.Coord.X, destination.Coord.Y - ownerLocation.Coord.Y);
        if (direction is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InventoryPolicyBlocked;
            trace.Detail = $"{sourceOwner.Name} exit policy requires an adjacent exit direction";
            return new(false, trace);
        }

        if (IsOnExitEdge(source.Coord, sourceOwner.InventoryWidth, sourceOwner.InventoryHeight, direction.Value))
        {
            trace.Status = TraceStatus.Success;
            trace.Detail = $"EdgeAlignedWithExitDirection allows {direction} from {source.Coord}";
            return new(true, trace);
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = FailureReason.InventoryPolicyBlocked;
        trace.Detail = $"{sourceOwner.Name} exit policy blocks {direction} from inventory coordinate {source.Coord}";
        return new(false, trace);
    }

    public static bool WouldCreateContainmentCycle(WorldState world, EntityId movingEntityId, EntityId destinationOwnerId)
    {
        var current = destinationOwnerId;
        var visited = new HashSet<EntityId>();
        while (visited.Add(current) && world.Entities.ContainsKey(current))
        {
            if (current == movingEntityId)
            {
                return true;
            }

            var location = world.GetEntityLocation(current);
            if (!InventoryPlaneOwnership.TryFindOwner(world, location.PlaneId, out current))
            {
                return false;
            }
        }

        return false;
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

    private static int DistanceFromNearestOccupied(WorldState world, Plane plane, GridCoord candidate)
    {
        var occupied = world.Occupancy
            .Where(entry => world.Nodes.TryGetValue(entry.Key, out var node) && node.PlaneId == plane.Id)
            .Select(entry => world.Nodes[entry.Key].Coord)
            .ToList();
        return occupied.Count == 0
            ? int.MaxValue
            : occupied.Min(coord => Math.Max(Math.Abs(candidate.X - coord.X), Math.Abs(candidate.Y - coord.Y)));
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

    private static bool IsOnExitEdge(GridCoord coord, int width, int height, Direction direction) => direction switch
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
}

public sealed record InventoryBoundaryPolicyEvaluation(bool CanPass, TraceNode Trace);
