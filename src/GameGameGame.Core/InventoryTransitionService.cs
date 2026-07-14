namespace GameGameGame.Core;

public sealed class InventoryTransitionService
{
    public InventoryTransitionEvaluation Evaluate(WorldState world, EntityId movingEntityId, PlaneCoord destination)
    {
        var trace = new TraceNode($"Check inventory aperture transition for {movingEntityId}", TraceStatus.Info, detail: $"destination={destination}");

        if (!world.Entities.TryGetValue(movingEntityId, out var movingEntity))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"moving entity {movingEntityId} does not exist";
            return new InventoryTransitionEvaluation(false, trace);
        }

        var source = world.GetEntityLocation(movingEntityId);
        trace.Add(TraceNode.Info("Source", source.ToString()));

        if (source.PlaneId == destination.PlaneId)
        {
            trace.Status = TraceStatus.Success;
            trace.Detail = "same plane; no inventory aperture crossed";
            return new InventoryTransitionEvaluation(true, trace);
        }

        foreach (var ownerId in GetCrossedInventoryOwners(world, source.PlaneId, destination.PlaneId))
        {
            var owner = world.Entities[ownerId];
            var comparison = new TraceNode(
                $"Compare {movingEntity.Name} bulk to {owner.Name} aperture",
                TraceStatus.Info,
                detail: $"bulk={movingEntity.Bulk}, aperture={owner.Aperture}");
            comparison.SuccessCriteria.Add(new ActionSuccessCriterion(
                ActionSuccessCriterionKind.Aperture,
                Satisfied: movingEntity.Bulk <= owner.Aperture,
                SuccessRatio: movingEntity.Bulk == 0 ? null : decimal.Divide(owner.Aperture, movingEntity.Bulk),
                RequiredValue: movingEntity.Bulk,
                AvailableValue: owner.Aperture,
                SubjectEntityId: movingEntityId,
                LimitEntityId: ownerId,
                Detail: $"{movingEntity.Name} bulk {movingEntity.Bulk} vs {owner.Name} aperture {owner.Aperture}"));

            if (movingEntity.Bulk > owner.Aperture)
            {
                comparison.Status = TraceStatus.Failure;
                comparison.Reason = FailureReason.ApertureBlocked;
                trace.Add(comparison);
                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.ApertureBlocked;
                trace.Detail = $"{movingEntity.Name} bulk {movingEntity.Bulk} exceeds {owner.Name} aperture {owner.Aperture}";
                return new InventoryTransitionEvaluation(false, trace);
            }

            comparison.Status = TraceStatus.Success;
            trace.Add(comparison);
        }

        trace.Status = TraceStatus.Success;
        trace.Detail = "inventory aperture transition allowed";
        return new InventoryTransitionEvaluation(true, trace);
    }

    private static IReadOnlyList<EntityId> GetCrossedInventoryOwners(WorldState world, PlaneId sourcePlaneId, PlaneId destinationPlaneId)
    {
        var owners = new List<EntityId>();
        if (TryFindInventoryOwner(world, sourcePlaneId, out var sourceOwner))
        {
            owners.Add(sourceOwner);
        }

        if (TryFindInventoryOwner(world, destinationPlaneId, out var destinationOwner) && !owners.Contains(destinationOwner))
        {
            owners.Add(destinationOwner);
        }

        return owners;
    }

    private static bool TryFindInventoryOwner(WorldState world, PlaneId planeId, out EntityId ownerId)
    {
        return InventoryPlaneOwnership.TryFindOwner(world, planeId, out ownerId);
    }
}

public sealed record InventoryTransitionEvaluation(bool CanTransition, TraceNode Trace);
