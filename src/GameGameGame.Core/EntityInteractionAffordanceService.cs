namespace GameGameGame.Core;

public sealed record EntityInteractionCapabilityResult(
    ActionPlanBehaviorStepKind Capability,
    EntityId ActorId,
    EntityId TargetId,
    bool CanTarget,
    FailureReason? FailureReason = null,
    string? FailureDetail = null,
    IReadOnlyList<ActionSuccessCriterion>? SuccessCriteria = null);

public sealed class EntityInteractionAffordanceService(MovementService movement)
{
    public static bool IsSupportedTargetCapability(ActionPlanBehaviorStepKind capability) =>
        capability is ActionPlanBehaviorStepKind.PickupTarget
            or ActionPlanBehaviorStepKind.EnterTarget
            or ActionPlanBehaviorStepKind.GiveTarget
            or ActionPlanBehaviorStepKind.TakeTarget
            or ActionPlanBehaviorStepKind.DestroyTarget
            or ActionPlanBehaviorStepKind.PushFacing;

    public EntityInteractionCapabilityResult QueryTargetCapability(
        WorldState world,
        EntityId actorId,
        EntityId targetId,
        ActionPlanBehaviorStepKind capability) =>
        capability switch
        {
            ActionPlanBehaviorStepKind.PickupTarget => QueryPickupTarget(world, actorId, targetId),
            ActionPlanBehaviorStepKind.EnterTarget => QueryEnterTarget(world, actorId, targetId),
            ActionPlanBehaviorStepKind.GiveTarget => QueryGiveTarget(world, actorId, targetId),
            ActionPlanBehaviorStepKind.TakeTarget => QueryTakeTarget(world, actorId, targetId),
            ActionPlanBehaviorStepKind.DestroyTarget => QueryDestroyTarget(world, actorId, targetId),
            ActionPlanBehaviorStepKind.PushFacing => QueryPushFacing(world, actorId, targetId),
            _ => Failure(capability, actorId, targetId, FailureReason.None, $"Action Step {capability} does not expose a target capability.")
        };

    private EntityInteractionCapabilityResult QueryPickupTarget(WorldState world, EntityId actorId, EntityId targetId)
    {
        if (!world.Entities.TryGetValue(actorId, out var actor))
        {
            return Failure(ActionPlanBehaviorStepKind.PickupTarget, actorId, targetId, FailureReason.ActorMissing, $"actor {actorId} does not exist");
        }

        if (!world.Entities.ContainsKey(targetId))
        {
            return Failure(ActionPlanBehaviorStepKind.PickupTarget, actorId, targetId, FailureReason.TargetMissing, $"target {targetId} does not exist");
        }

        if (targetId == actorId)
        {
            return Failure(ActionPlanBehaviorStepKind.PickupTarget, actorId, targetId, FailureReason.TargetIsActor, "actor cannot pick up itself");
        }

        if (world.GetRegisteredInventoryPlaneId(actorId) is not { } inventoryPlaneId || !actor.HasUsableInventory)
        {
            return Failure(ActionPlanBehaviorStepKind.PickupTarget, actorId, targetId, FailureReason.ActorHasNoInventory, $"{actor.Name} has no usable inventory");
        }

        return CanRelocateToAnyInventoryCoord(world, targetId, inventoryPlaneId, ActionPlanBehaviorStepKind.PickupTarget, actorId, targetId);
    }

    private EntityInteractionCapabilityResult QueryEnterTarget(WorldState world, EntityId actorId, EntityId targetId)
    {
        if (!world.Entities.ContainsKey(actorId))
        {
            return Failure(ActionPlanBehaviorStepKind.EnterTarget, actorId, targetId, FailureReason.ActorMissing, $"actor {actorId} does not exist");
        }

        if (!world.Entities.TryGetValue(targetId, out var target))
        {
            return Failure(ActionPlanBehaviorStepKind.EnterTarget, actorId, targetId, FailureReason.TargetMissing, $"target {targetId} does not exist");
        }

        if (targetId == actorId)
        {
            return Failure(ActionPlanBehaviorStepKind.EnterTarget, actorId, targetId, FailureReason.TargetIsActor, "actor cannot enter itself");
        }

        if (world.GetRegisteredInventoryPlaneId(targetId) is not { } inventoryPlaneId || !target.HasUsableInventory)
        {
            return Failure(ActionPlanBehaviorStepKind.EnterTarget, actorId, targetId, FailureReason.TargetHasNoInventory, $"target {targetId} has no usable inventory");
        }

        return CanRelocateToAnyInventoryCoord(world, actorId, inventoryPlaneId, ActionPlanBehaviorStepKind.EnterTarget, actorId, targetId);
    }

    private EntityInteractionCapabilityResult QueryGiveTarget(WorldState world, EntityId actorId, EntityId targetId)
    {
        if (targetId == actorId)
        {
            return Failure(ActionPlanBehaviorStepKind.GiveTarget, actorId, targetId, FailureReason.TargetIsActor, "actor cannot give to itself");
        }

        if (!world.Entities.TryGetValue(targetId, out var target))
        {
            return Failure(ActionPlanBehaviorStepKind.GiveTarget, actorId, targetId, FailureReason.TargetMissing, $"target {targetId} does not exist");
        }

        var carried = FindFirstCarriedEntity(world, actorId);
        if (carried is null)
        {
            return Failure(ActionPlanBehaviorStepKind.GiveTarget, actorId, targetId, FailureReason.TargetMissing, $"{actorId} carries no entity to give");
        }

        if (world.GetRegisteredInventoryPlaneId(targetId) is not { } inventoryPlaneId || !target.HasUsableInventory)
        {
            return Failure(ActionPlanBehaviorStepKind.GiveTarget, actorId, targetId, FailureReason.TargetHasNoInventory, $"target {targetId} has no usable inventory");
        }

        return CanRelocateToAnyInventoryCoord(world, carried.Value, inventoryPlaneId, ActionPlanBehaviorStepKind.GiveTarget, actorId, targetId);
    }

    private EntityInteractionCapabilityResult QueryTakeTarget(WorldState world, EntityId actorId, EntityId targetId)
    {
        if (targetId == actorId)
        {
            return Failure(ActionPlanBehaviorStepKind.TakeTarget, actorId, targetId, FailureReason.TargetIsActor, "actor cannot take from itself");
        }

        if (!world.Entities.TryGetValue(actorId, out var actor))
        {
            return Failure(ActionPlanBehaviorStepKind.TakeTarget, actorId, targetId, FailureReason.ActorMissing, $"actor {actorId} does not exist");
        }

        if (!world.Entities.TryGetValue(targetId, out var target))
        {
            return Failure(ActionPlanBehaviorStepKind.TakeTarget, actorId, targetId, FailureReason.TargetMissing, $"target {targetId} does not exist");
        }

        if (world.GetRegisteredInventoryPlaneId(actorId) is not { } actorInventoryPlaneId || !actor.HasUsableInventory)
        {
            return Failure(ActionPlanBehaviorStepKind.TakeTarget, actorId, targetId, FailureReason.ActorHasNoInventory, $"{actor.Name} has no usable inventory");
        }

        if (world.GetRegisteredInventoryPlaneId(targetId) is not { } || !target.HasUsableInventory)
        {
            return Failure(ActionPlanBehaviorStepKind.TakeTarget, actorId, targetId, FailureReason.TargetHasNoInventory, $"target {targetId} has no usable inventory");
        }

        var carried = FindFirstCarriedEntity(world, targetId);
        if (carried is null)
        {
            return Failure(ActionPlanBehaviorStepKind.TakeTarget, actorId, targetId, FailureReason.TargetMissing, $"{targetId} carries no entity to take");
        }

        return CanRelocateToAnyInventoryCoord(world, carried.Value, actorInventoryPlaneId, ActionPlanBehaviorStepKind.TakeTarget, actorId, targetId);
    }

    private static EntityInteractionCapabilityResult QueryDestroyTarget(WorldState world, EntityId actorId, EntityId targetId)
    {
        if (targetId == actorId)
        {
            return Failure(ActionPlanBehaviorStepKind.DestroyTarget, actorId, targetId, FailureReason.TargetIsActor, "DestroyTarget cannot destroy self");
        }

        return world.Entities.ContainsKey(targetId)
            ? Success(ActionPlanBehaviorStepKind.DestroyTarget, actorId, targetId)
            : Failure(ActionPlanBehaviorStepKind.DestroyTarget, actorId, targetId, FailureReason.TargetMissing, $"target {targetId} does not exist");
    }

    private EntityInteractionCapabilityResult QueryPushFacing(WorldState world, EntityId actorId, EntityId targetId)
    {
        if (world.GetActionFacing(actorId) is not { } facing)
        {
            return Failure(ActionPlanBehaviorStepKind.PushFacing, actorId, targetId, FailureReason.None, $"{actorId} has no Facing for PushFacing");
        }

        var blocker = movement.GetBlockingEntity(world, actorId, facing);
        if (blocker != targetId)
        {
            return Failure(ActionPlanBehaviorStepKind.PushFacing, actorId, targetId, FailureReason.TargetMissing, $"{targetId} is not the blocking entity in {facing}");
        }

        var pushEvaluation = movement.EvaluateRelocation(world, targetId, MovementDestination.AdjacentTo(targetId, facing));
        return pushEvaluation.CanRelocate
            ? Success(ActionPlanBehaviorStepKind.PushFacing, actorId, targetId)
            : Failure(ActionPlanBehaviorStepKind.PushFacing, actorId, targetId, pushEvaluation.Trace.Reason, pushEvaluation.Trace.Detail);
    }

    private EntityInteractionCapabilityResult CanRelocateToAnyInventoryCoord(
        WorldState world,
        EntityId movingId,
        PlaneId inventoryPlaneId,
        ActionPlanBehaviorStepKind capability,
        EntityId actorId,
        EntityId targetId)
    {
        if (!world.Planes.TryGetValue(inventoryPlaneId, out var inventoryPlane))
        {
            return Failure(capability, actorId, targetId, FailureReason.InvalidInventoryDestination, $"inventory plane {inventoryPlaneId} does not exist");
        }

        var constrained = new ConstrainedInventoryRelocationService(movement);
        ConstrainedRelocationEvaluation? lastFailure = null;
        for (var y = 0; y < inventoryPlane.Height; y++)
        {
            for (var x = 0; x < inventoryPlane.Width; x++)
            {
                var destination = new PlaneCoord(inventoryPlaneId, new GridCoord(x, y));
                var evaluation = constrained.Evaluate(world, movingId, MovementDestination.Plane(destination));
                if (evaluation.CanRelocate)
                {
                    return Success(capability, actorId, targetId, ExtractSuccessCriteria(evaluation.Trace));
                }

                lastFailure = evaluation;
            }
        }

        return Failure(
            capability,
            actorId,
            targetId,
            lastFailure?.Trace.Reason ?? FailureReason.InvalidPlacement,
            lastFailure?.Trace.Detail ?? $"no inventory coordinate in {inventoryPlaneId} can accept {movingId}",
            lastFailure is null ? [] : ExtractSuccessCriteria(lastFailure.Trace));
    }

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
            .ThenBy(entry => entry.Value.Value, StringComparer.Ordinal)
            .Select(entry => (EntityId?)entry.Value)
            .FirstOrDefault();
    }

    private static EntityInteractionCapabilityResult Success(
        ActionPlanBehaviorStepKind capability,
        EntityId actorId,
        EntityId targetId,
        IReadOnlyList<ActionSuccessCriterion>? successCriteria = null) =>
        new(capability, actorId, targetId, CanTarget: true, SuccessCriteria: successCriteria ?? []);

    private static EntityInteractionCapabilityResult Failure(
        ActionPlanBehaviorStepKind capability,
        EntityId actorId,
        EntityId targetId,
        FailureReason? reason,
        string? detail,
        IReadOnlyList<ActionSuccessCriterion>? successCriteria = null) =>
        new(capability, actorId, targetId, CanTarget: false, reason, detail, successCriteria ?? []);

    private static IReadOnlyList<ActionSuccessCriterion> ExtractSuccessCriteria(TraceNode trace) =>
        DescendantsAndSelf(trace).SelectMany(node => node.SuccessCriteria).ToList();

    private static IEnumerable<TraceNode> DescendantsAndSelf(TraceNode trace)
    {
        yield return trace;
        foreach (var child in trace.Children)
        {
            foreach (var descendant in DescendantsAndSelf(child))
            {
                yield return descendant;
            }
        }
    }
}
