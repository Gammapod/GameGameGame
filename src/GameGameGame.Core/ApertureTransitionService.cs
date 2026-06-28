namespace GameGameGame.Core;

public sealed class ApertureTransitionService
{
    public ApertureTransitionEvaluation EvaluateTransition(
        WorldState world,
        EntityId movingEntityId,
        PlaneCoord destination)
    {
        var trace = new TraceNode($"Check aperture transition for {movingEntityId}", TraceStatus.Info, detail: $"destination={destination}");

        if (!world.Entities.TryGetValue(movingEntityId, out var movingEntity))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"moving entity {movingEntityId} does not exist";
            return new ApertureTransitionEvaluation(false, trace);
        }

        var source = world.GetEntityLocation(movingEntityId);
        trace.Add(TraceNode.Info("Source", source.ToString()));

        if (source.PlaneId == destination.PlaneId)
        {
            trace.Status = TraceStatus.Success;
            trace.Detail = "same plane; aperture not crossed";
            return new ApertureTransitionEvaluation(true, trace);
        }

        foreach (var ownerId in GetCrossedInventoryOwners(world, source.PlaneId, destination.PlaneId))
        {
            var owner = world.Entities[ownerId];
            var comparison = TraceNode.Info(
                $"Compare {movingEntity.Name} bulk with {owner.Name} aperture",
                $"bulk={movingEntity.Bulk}, aperture={owner.Aperture}");

            if (movingEntity.Bulk > owner.Aperture)
            {
                comparison.Status = TraceStatus.Failure;
                comparison.Reason = FailureReason.ApertureBlocked;
                trace.Add(comparison);
                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.ApertureBlocked;
                trace.Detail = $"{movingEntity.Name} bulk {movingEntity.Bulk} exceeds {owner.Name} aperture {owner.Aperture}";
                return new ApertureTransitionEvaluation(false, trace);
            }

            comparison.Status = TraceStatus.Success;
            trace.Add(comparison);
        }

        trace.Status = TraceStatus.Success;
        trace.Detail = "aperture transition allowed";
        return new ApertureTransitionEvaluation(true, trace);
    }

    private static IReadOnlyList<EntityId> GetCrossedInventoryOwners(WorldState world, PlaneId sourcePlaneId, PlaneId destinationPlaneId)
    {
        var owners = new List<EntityId>();
        if (TryFindInventoryOwner(world, sourcePlaneId, out var sourceOwner))
        {
            owners.Add(sourceOwner);
        }

        if (destinationPlaneId != sourcePlaneId &&
            TryFindInventoryOwner(world, destinationPlaneId, out var destinationOwner) &&
            !owners.Contains(destinationOwner))
        {
            owners.Add(destinationOwner);
        }

        return owners;
    }

    private static bool TryFindInventoryOwner(WorldState world, PlaneId planeId, out EntityId ownerId)
    {
        foreach (var (entityId, inventoryPlaneId) in world.InventoryPlanes)
        {
            if (inventoryPlaneId == planeId && world.Entities.ContainsKey(entityId))
            {
                ownerId = entityId;
                return true;
            }
        }

        ownerId = default;
        return false;
    }
}

public sealed record ApertureTransitionEvaluation(bool CanTransition, TraceNode Trace);
