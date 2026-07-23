namespace GameGameGame.Core;

public sealed partial class ActionPlanInterpreter
{
    private PlanEffectResult ApplyPickupTargetPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive PickupTarget", TraceStatus.Info);
        if (!context.TryRead<EntityPlanValue>(ActionPlanSlot.Target, out _, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Add(context.Set(ActionPlanSlot.Target, new EntityPlanValue(actorId)));
        }

        if (!context.TryRead<EntityPlanValue>(ActionPlanSlot.Target, out var target, out readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);

        if (!world.Entities.TryGetValue(actorId, out var actor))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ActorMissing;
            trace.Detail = $"actor {actorId} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: true, trace);
        }

        if (world.GetRegisteredInventoryPlaneId(actorId) is not { } inventoryPlaneId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ActorHasNoInventory;
            trace.Detail = $"{actor.Name} has no inventory plane";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: true, trace);
        }

        if (!actor.HasUsableInventory)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ActorInventoryUnusable;
            trace.Detail = $"{actor.Name} inventory dimensions are {actor.InventoryWidth}x{actor.InventoryHeight}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: true, trace);
        }

        if (!world.Planes.TryGetValue(inventoryPlaneId, out var inventoryPlane))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidInventoryDestination;
            trace.Detail = $"inventory plane {inventoryPlaneId} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: true, trace);
        }

        var adjacency = _movement.EvaluateAdjacency(world, actorId, target.Value);
        if (!adjacency.AreAdjacent)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetNotAdjacent;
            trace.Detail = adjacency.FailureDetail is { Length: > 0 }
                ? $"{world.FormatEntityAddress(target.Value)} is not adjacent to {world.FormatEntityAddress(actorId)}: {adjacency.FailureDetail}"
                : $"{world.FormatEntityAddress(target.Value)} is not adjacent to {world.FormatEntityAddress(actorId)}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: true, trace);
        }

        trace.Add(TraceNode.Success("Target is adjacent"));

        var policyService = new InventoryBoundaryPolicyService();
        foreach (var candidate in policyService.OrderedEnterPolicyDestinations(world, actorId, actorId))
        {
            trace.Add(TraceNode.Info($"Pickup {target.Value} -> {candidate}"));
        }

        var placement = policyService
            .EvaluatePolicyAwarePlacement(
                world,
                target.Value,
                actorId,
                new ConstrainedInventoryRelocationService(_movement, ignoredPolicyOwnerId: actorId),
                actorId);
        trace.Add(placement.Trace);
        if (placement is { CanRelocate: true, Destination: { } resolvedPickupDestination })
        {
            _movement.TryPlace(world, target.Value, resolvedPickupDestination);
            trace.Status = TraceStatus.Success;
            trace.Detail = $"picked up {target.Value} into first available inventory coordinate {resolvedPickupDestination.Coord}";
            return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = placement.Trace.Reason == FailureReason.None ? FailureReason.InvalidPlacement : placement.Trace.Reason;
        trace.Detail = $"no inventory coordinate can accept {target.Value}";
        if (!string.IsNullOrWhiteSpace(placement.Trace.Detail))
        {
            trace.Detail += $"; last failure: {placement.Trace.Detail}";
        }

        return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: true, trace);
    }

    private PlanEffectResult ApplyDropFacingPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive DropFacing", TraceStatus.Info);
        if (!context.TryRead<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        var carried = FindFirstCarriedEntity(world, actorId);
        if (carried is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"{actorId} carries no entity to drop";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var destination = new MovementDestination.AdjacentMovementDestination(actorId, facing.Value);
        var constrainedRelocation = new ConstrainedInventoryRelocationService(_movement, ignoredPolicyOwnerId: actorId);
        var evaluation = constrainedRelocation.Evaluate(world, carried.Value, destination);
        trace.Add(evaluation.Trace);
        if (!evaluation.CanRelocate || evaluation.Destination is not { } resolvedDestination)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = evaluation.Trace.Reason;
            trace.Detail = evaluation.Trace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        _movement.TryPlace(world, carried.Value, resolvedDestination);
        trace.Status = TraceStatus.Success;
        trace.Detail = $"dropped {carried.Value} to {resolvedDestination}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
    }

    private PlanEffectResult ApplyGiveTargetPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive GiveTarget", TraceStatus.Info);
        if (!TryReadTransferTarget(world, actorId, context, trace, "GiveTarget", out var targetId))
        {
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var carried = FindFirstCarriedEntity(world, actorId);
        if (carried is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"{actorId} carries no entity to give";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        return TransferToFirstOpenInventory(world, actorId, carried.Value, targetId, trace, "gave");
    }

    private PlanEffectResult ApplyTakeTargetPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive TakeTarget", TraceStatus.Info);
        if (!TryReadTransferTarget(world, actorId, context, trace, "TakeTarget", out var targetId))
        {
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!world.Entities[targetId].HasUsableInventory || world.GetRegisteredInventoryPlaneId(targetId) is not { } targetInventoryPlaneId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ActorHasNoInventory;
            trace.Detail = $"{targetId} has no usable inventory to take from";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!world.Planes.ContainsKey(targetInventoryPlaneId))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidInventoryDestination;
            trace.Detail = $"inventory plane {targetInventoryPlaneId} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var carried = FindFirstCarriedEntity(world, targetId);
        if (carried is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"{targetId} carries no entity to take";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        return TransferToFirstOpenInventory(world, actorId, carried.Value, actorId, trace, "took");
    }

    private PlanEffectResult ApplyEnterTargetPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive EnterTarget", TraceStatus.Info);
        if (!TryReadTransferTarget(world, actorId, context, trace, "EnterTarget", out var targetId))
        {
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var resolution = ((IActionIntent)new EnterAction(targetId)).Resolve(world, actorId, _movement);
        trace.Add(resolution.Trace);

        if (resolution.Succeeded)
        {
            trace.Status = TraceStatus.Success;
            trace.Detail = resolution.Trace.Detail;
            return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = resolution.Trace.Reason;
        trace.Detail = resolution.Trace.Detail;
        return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
    }

    private PlanEffectResult ApplyExitFacingPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive ExitFacing", TraceStatus.Info);
        if (!context.TryRead<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        var resolution = ((IActionIntent)new ExitAction(facing.Value)).Resolve(world, actorId, _movement);
        trace.Add(resolution.Trace);

        if (resolution.Succeeded)
        {
            trace.Status = TraceStatus.Success;
            trace.Detail = resolution.Trace.Detail;
            return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace, resolution.ActorMovementDirection);
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = resolution.Trace.Reason;
        trace.Detail = resolution.Trace.Detail;
        return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
    }

    private static PlanEffectResult ApplyDestroyTargetPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive DestroyTarget", TraceStatus.Info);
        if (!context.TryRead<EntityPlanValue>(ActionPlanSlot.Target, out var target, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        if (target.Value == actorId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = "DestroyTarget cannot destroy self";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!world.Entities.ContainsKey(target.Value))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"target {target.Value} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var destroyed = world.DestroyEntityRecursive(target.Value);
        trace.Status = TraceStatus.Success;
        trace.Detail = $"destroyed {string.Join(", ", destroyed)}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
    }

    private PlanEffectResult ApplyCanonicalTransfer(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        ActionPlanBehaviorStepDescriptor step)
    {
        var trace = new TraceNode("Primitive Transfer", TraceStatus.Info);
        if (step.TransferDirection is not { } transferDirection)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = "Transfer requires transferDirection";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (step.DirectionMode is not { } mode)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = "Transfer requires directionMode";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!TryResolveMoveDirection(mode, context, out var counterpartyDirection, out var directionReadTrace, out var directionFailureDetail))
        {
            if (directionReadTrace is not null)
            {
                trace.Add(directionReadTrace);
            }

            trace.Status = TraceStatus.Failure;
            trace.Detail = directionFailureDetail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (directionReadTrace is not null)
        {
            trace.Add(directionReadTrace);
        }

        if (!context.TryRead<EntityPlanValue>(ActionPlanSlot.Target, out var target, out var targetReadTrace))
        {
            trace.Add(targetReadTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = targetReadTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(targetReadTrace);
        var resolution = ((IActionIntent)new TransferAction(transferDirection, target.Value, counterpartyDirection))
            .Resolve(world, actorId, _movement);
        trace.Add(resolution.Trace);
        if (resolution.Succeeded)
        {
            trace.Status = TraceStatus.Success;
            trace.Detail = resolution.Trace.Detail;
            return new PlanEffectResult(true, resolution.ConsumesTurn, resolution.ContinuePlan, trace);
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = resolution.Trace.Reason;
        trace.Detail = resolution.Trace.Detail;
        return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
    }
}
