namespace GameGameGame.Core;

public interface IActionIntent
{
    ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement);

    bool CanExecute(WorldState world, EntityId actorId, MovementService movement) =>
        Evaluate(world, actorId, movement).CanExecute;

    void Execute(WorldState world, EntityId actorId, MovementService movement);

    ActionResolution Resolve(WorldState world, EntityId actorId, MovementService movement)
    {
        var evaluation = Evaluate(world, actorId, movement);

        if (!evaluation.CanExecute)
        {
            return new ActionResolution(false, ConsumesTurn: false, ContinuePlan: true, evaluation.Trace);
        }

        Execute(world, actorId, movement);
        return new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, evaluation.Trace);
    }
}

public sealed record ActionResolution(
    bool Succeeded,
    bool ConsumesTurn,
    bool ContinuePlan,
    TraceNode Trace,
    Direction? ActorMovementDirection = null);

public sealed record PlannedActionPlan(IReadOnlyList<IActionIntent> Options)
{
    public static PlannedActionPlan Single(IActionIntent action) => new([action]);
}

public sealed record MoveAction(Direction Direction) : IActionIntent
{
    public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement)
    {
        var trace = new TraceNode($"Move {Direction}", TraceStatus.Info);

        var relocation = movement.EvaluateRelocation(world, actorId, MovementDestination.AdjacentTo(actorId, Direction));
        trace.Add(relocation.Trace);

        if (!relocation.CanRelocate)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = relocation.Trace.Reason;
            trace.Detail = relocation.Trace.Detail;
            return new ActionEvaluation(false, trace);
        }

        trace.Status = TraceStatus.Success;
        return new ActionEvaluation(true, trace);
    }

    public void Execute(WorldState world, EntityId actorId, MovementService movement) =>
        movement.TryRelocate(world, actorId, MovementDestination.AdjacentTo(actorId, Direction));

    public ActionResolution Resolve(WorldState world, EntityId actorId, MovementService movement)
    {
        var evaluation = Evaluate(world, actorId, movement);

        if (!evaluation.CanExecute)
        {
            return new ActionResolution(false, ConsumesTurn: false, ContinuePlan: true, evaluation.Trace);
        }

        Execute(world, actorId, movement);
        return new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, evaluation.Trace, Direction);
    }
}

public sealed record WaitAction : IActionIntent
{
    public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement) =>
        new(true, TraceNode.Success("Wait"));

    public void Execute(WorldState world, EntityId actorId, MovementService movement)
    {
    }
}

public sealed record PickupAction(EntityId TargetId, PlaneCoord Destination) : IActionIntent
{
    public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement)
    {
        var trace = new TraceNode($"Pickup {TargetId} -> {Destination}", TraceStatus.Info);

        if (!world.Entities.TryGetValue(actorId, out var actor))
        {
            return ActionTrace.Fail(trace, FailureReason.ActorMissing, $"actor {actorId} does not exist");
        }

        trace.Add(TraceNode.Success("Actor exists", world.FormatEntityAddress(actorId)));

        if (!world.Entities.ContainsKey(TargetId))
        {
            return ActionTrace.Fail(trace, FailureReason.TargetMissing, $"target {TargetId} does not exist");
        }

        trace.Add(TraceNode.Success("Target exists", world.FormatEntityAddress(TargetId)));

        if (TargetId == actorId)
        {
            return ActionTrace.Fail(trace, FailureReason.TargetIsActor, "actor cannot pick up itself");
        }

        if (world.GetRegisteredInventoryPlaneId(actorId) is not { } inventoryPlaneId)
        {
            return ActionTrace.Fail(trace, FailureReason.ActorHasNoInventory, $"{actor.Name} has no inventory plane");
        }

        if (!actor.HasUsableInventory)
        {
            return ActionTrace.Fail(trace, FailureReason.ActorInventoryUnusable, $"{actor.Name} inventory dimensions are {actor.InventoryWidth}x{actor.InventoryHeight}");
        }

        trace.Add(TraceNode.Success("Actor has inventory", inventoryPlaneId.ToString()));
        trace.Add(TraceNode.Success("Actor inventory is usable", $"{actor.InventoryWidth}x{actor.InventoryHeight}"));

        if (Destination.PlaneId != inventoryPlaneId)
        {
            return ActionTrace.Fail(trace, FailureReason.InvalidInventoryDestination, $"destination must be inside {inventoryPlaneId}");
        }

        trace.Add(TraceNode.Success("Destination is inside actor inventory", Destination.ToString()));

        var adjacency = movement.EvaluateAdjacency(world, actorId, TargetId);
        if (!adjacency.AreAdjacent)
        {
            var detail = adjacency.FailureDetail is { Length: > 0 }
                ? $"{world.FormatEntityAddress(TargetId)} is not adjacent to {world.FormatEntityAddress(actorId)}: {adjacency.FailureDetail}"
                : $"{world.FormatEntityAddress(TargetId)} is not adjacent to {world.FormatEntityAddress(actorId)}";
            return ActionTrace.Fail(trace, FailureReason.TargetNotAdjacent, detail);
        }

        trace.Add(TraceNode.Success("Target is adjacent"));

        var constrainedRelocation = new ConstrainedInventoryRelocationService(movement, ignoredPolicyOwnerId: actorId);
        var relocation = constrainedRelocation.Evaluate(world, TargetId, MovementDestination.Plane(Destination));
        trace.Add(relocation.Trace);

        if (!relocation.CanRelocate)
        {
            return ActionTrace.Fail(trace, relocation.Trace.Reason, relocation.Trace.Detail ?? $"cannot place into {Destination}");
        }

        trace.Add(TraceNode.Success("Destination can accept entity", Destination.ToString()));

        trace.Status = TraceStatus.Success;
        return new ActionEvaluation(true, trace);
    }

    public void Execute(WorldState world, EntityId actorId, MovementService movement) =>
        new ConstrainedInventoryRelocationService(movement, ignoredPolicyOwnerId: actorId)
            .TryRelocate(world, TargetId, MovementDestination.Plane(Destination));
}

public sealed record DropAction(EntityId TargetId, PlaneCoord Destination) : IActionIntent
{
    public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement)
    {
        var trace = new TraceNode($"Drop {TargetId} -> {Destination}", TraceStatus.Info);

        if (!world.Entities.TryGetValue(actorId, out var actor))
        {
            return ActionTrace.Fail(trace, FailureReason.ActorMissing, $"actor {actorId} does not exist");
        }

        if (!world.Entities.ContainsKey(TargetId))
        {
            return ActionTrace.Fail(trace, FailureReason.TargetMissing, $"target {TargetId} does not exist");
        }

        if (world.GetRegisteredInventoryPlaneId(actorId) is not { } inventoryPlaneId)
        {
            return ActionTrace.Fail(trace, FailureReason.ActorHasNoInventory, $"{actor.Name} has no inventory plane");
        }

        if (!actor.HasUsableInventory)
        {
            return ActionTrace.Fail(trace, FailureReason.ActorInventoryUnusable, $"{actor.Name} inventory dimensions are {actor.InventoryWidth}x{actor.InventoryHeight}");
        }

        var actorLocation = world.GetEntityLocation(actorId);
        var targetLocation = world.GetEntityLocation(TargetId);

        if (targetLocation.PlaneId != inventoryPlaneId)
        {
            return ActionTrace.Fail(trace, FailureReason.TargetNotInInventory, $"{world.FormatEntityAddress(TargetId)} is not inside {inventoryPlaneId}");
        }

        trace.Add(TraceNode.Success("Target is in actor inventory", targetLocation.ToString()));

        if (Destination.PlaneId != actorLocation.PlaneId)
        {
            return ActionTrace.Fail(trace, FailureReason.InvalidDropDestination, $"destination must be on actor plane {actorLocation.PlaneId}");
        }

        trace.Add(TraceNode.Success("Destination is on actor plane", Destination.ToString()));

        var adjacency = movement.EvaluateAdjacency(world, actorLocation, Destination);
        if (!adjacency.AreAdjacent)
        {
            var detail = adjacency.FailureDetail is { Length: > 0 }
                ? $"destination {Destination} is not adjacent to {world.FormatEntityAddress(actorId)}: {adjacency.FailureDetail}"
                : $"destination {Destination} is not adjacent to {world.FormatEntityAddress(actorId)}";
            return ActionTrace.Fail(trace, adjacency.FailureReason ?? FailureReason.InvalidDropDestination, detail);
        }

        trace.Add(TraceNode.Success("Destination is adjacent", Destination.ToString()));

        var constrainedRelocation = new ConstrainedInventoryRelocationService(movement, ignoredPolicyOwnerId: actorId);
        var relocation = constrainedRelocation.Evaluate(world, TargetId, MovementDestination.Plane(Destination));
        trace.Add(relocation.Trace);

        if (!relocation.CanRelocate)
        {
            return ActionTrace.Fail(trace, relocation.Trace.Reason, relocation.Trace.Detail ?? $"cannot place into {Destination}");
        }

        trace.Add(TraceNode.Success("Destination can accept entity", Destination.ToString()));
        trace.Status = TraceStatus.Success;
        return new ActionEvaluation(true, trace);
    }

    public void Execute(WorldState world, EntityId actorId, MovementService movement) =>
        new ConstrainedInventoryRelocationService(movement, ignoredPolicyOwnerId: actorId)
            .TryRelocate(world, TargetId, MovementDestination.Plane(Destination));

}

public enum TransferDirection
{
    ActorToTarget,
    TargetToActor
}

public sealed record TransferAction(TransferDirection TransferDirection, EntityId MovingEntityId, Direction CounterpartyDirection) : IActionIntent
{
    public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement)
    {
        var trace = new TraceNode($"Transfer {TransferDirection} {MovingEntityId} {CounterpartyDirection}", TraceStatus.Info);
        if (!TryResolve(world, actorId, movement, trace, out var destination))
        {
            return new ActionEvaluation(false, trace);
        }

        trace.Status = TraceStatus.Success;
        if (world.Entities.TryGetValue(MovingEntityId, out var movingEntity) &&
            InventoryPlaneOwnership.TryFindOwner(world, destination.PlaneId, out var destinationOwnerId) &&
            world.Entities.TryGetValue(destinationOwnerId, out var destinationOwner))
        {
            var verb = TransferDirection == TransferDirection.ActorToTarget ? "gave" : "took";
            trace.Detail = $"{verb} {MovingEntityId} ({movingEntity.Name}) to {destinationOwnerId} ({destinationOwner.Name}) slot {destination.Coord}";
        }

        return new ActionEvaluation(true, trace);
    }

    public void Execute(WorldState world, EntityId actorId, MovementService movement)
    {
        var trace = new TraceNode($"Transfer {TransferDirection} {MovingEntityId} {CounterpartyDirection}", TraceStatus.Info);
        if (TryResolve(world, actorId, movement, trace, out var destination))
        {
            movement.TryPlace(world, MovingEntityId, destination);
        }
    }

    private bool TryResolve(WorldState world, EntityId actorId, MovementService movement, TraceNode trace, out PlaneCoord destination)
    {
        destination = default;
        if (!world.Entities.TryGetValue(actorId, out var actor))
        {
            ActionTrace.Fail(trace, FailureReason.ActorMissing, $"actor {actorId} does not exist");
            return false;
        }

        if (!world.Entities.TryGetValue(MovingEntityId, out var movingEntity))
        {
            ActionTrace.Fail(trace, FailureReason.TargetMissing, $"moving entity {MovingEntityId} does not exist");
            return false;
        }

        if (!TryResolveCounterparty(world, actorId, movement, trace, out var counterpartyId))
        {
            return false;
        }

        return TransferDirection switch
        {
            TransferDirection.ActorToTarget => TryResolveActorToTarget(world, actorId, actor, counterpartyId, movement, trace, out destination),
            TransferDirection.TargetToActor => TryResolveTargetToActor(world, actorId, actor, counterpartyId, movement, trace, out destination),
            _ => throw new InvalidOperationException($"Unsupported transfer direction {TransferDirection}.")
        };
    }

    private bool TryResolveCounterparty(WorldState world, EntityId actorId, MovementService movement, TraceNode trace, out EntityId counterpartyId)
    {
        counterpartyId = default;
        var actorLocation = world.GetEntityLocation(actorId);
        var counterpartyCoord = new PlaneCoord(actorLocation.PlaneId, actorLocation.Coord.Offset(CounterpartyDirection));
        var adjacency = movement.EvaluateAdjacency(world, actorLocation, counterpartyCoord, actorId);
        if (!adjacency.AreAdjacent)
        {
            ActionTrace.Fail(trace, adjacency.FailureReason ?? FailureReason.TargetNotAdjacent, adjacency.FailureDetail ?? $"counterparty direction {CounterpartyDirection} is not adjacent");
            return false;
        }

        if (!world.Planes.TryGetValue(counterpartyCoord.PlaneId, out var plane) ||
            !plane.Contains(counterpartyCoord.Coord) ||
            world.TryGetNodeId(counterpartyCoord, out _) == false)
        {
            ActionTrace.Fail(trace, FailureReason.TargetNotAdjacent, $"counterparty destination {counterpartyCoord} is outside the actor plane");
            return false;
        }

        if (world.GetOccupant(counterpartyCoord) is not { } occupant || !world.Entities.ContainsKey(occupant))
        {
            ActionTrace.Fail(trace, FailureReason.TargetMissing, $"no counterparty entity at {counterpartyCoord}");
            return false;
        }

        counterpartyId = occupant;
        return true;
    }

    private bool TryResolveActorToTarget(WorldState world, EntityId actorId, Entity actor, EntityId counterpartyId, MovementService movement, TraceNode trace, out PlaneCoord destination)
    {
        destination = default;
        if (world.GetRegisteredInventoryPlaneId(actorId) is not { } actorInventoryPlaneId)
        {
            ActionTrace.Fail(trace, FailureReason.ActorHasNoInventory, $"{actor.Name} has no inventory plane");
            return false;
        }

        var movingLocation = world.GetEntityLocation(MovingEntityId);
        if (movingLocation.PlaneId != actorInventoryPlaneId)
        {
            ActionTrace.Fail(trace, FailureReason.TargetNotInInventory, $"{MovingEntityId} is not contained by actor {actorId}");
            return false;
        }

        if (!world.Entities.TryGetValue(counterpartyId, out var counterparty))
        {
            ActionTrace.Fail(trace, FailureReason.TargetMissing, $"counterparty {counterpartyId} does not exist");
            return false;
        }

        if (world.GetRegisteredInventoryPlaneId(counterpartyId) is not { } counterpartyInventoryPlaneId)
        {
            ActionTrace.Fail(trace, FailureReason.TargetHasNoInventory, $"{counterparty.Name} has no inventory plane");
            return false;
        }

        if (!counterparty.HasUsableInventory)
        {
            ActionTrace.Fail(trace, FailureReason.TargetInventoryUnusable, $"{counterparty.Name} inventory dimensions are {counterparty.InventoryWidth}x{counterparty.InventoryHeight}");
            return false;
        }

        if (!world.Planes.ContainsKey(counterpartyInventoryPlaneId))
        {
            ActionTrace.Fail(trace, FailureReason.InvalidInventoryDestination, $"inventory plane {counterpartyInventoryPlaneId} does not exist");
            return false;
        }

        var constrainedRelocation = new ConstrainedInventoryRelocationService(movement, ignoredPolicyOwnerId: actorId);
        var placement = new InventoryBoundaryPolicyService().EvaluatePolicyAwarePlacement(world, MovingEntityId, counterpartyId, constrainedRelocation, actorId);
        trace.Add(placement.Trace);
        if (placement is { CanRelocate: true, Destination: { } resolvedDestination })
        {
            destination = resolvedDestination;
            return true;
        }

        ActionTrace.Fail(trace, placement.Trace.Reason == FailureReason.None ? FailureReason.InvalidPlacement : placement.Trace.Reason, placement.Trace.Detail ?? $"no inventory coordinate in {counterpartyInventoryPlaneId} can accept {MovingEntityId}");
        return false;
    }

    private bool TryResolveTargetToActor(WorldState world, EntityId actorId, Entity actor, EntityId counterpartyId, MovementService movement, TraceNode trace, out PlaneCoord destination)
    {
        destination = default;
        if (world.GetRegisteredInventoryPlaneId(counterpartyId) is not { } counterpartyInventoryPlaneId || !world.Entities.TryGetValue(counterpartyId, out var counterparty))
        {
            ActionTrace.Fail(trace, FailureReason.TargetHasNoInventory, $"{counterpartyId} has no inventory plane");
            return false;
        }

        var movingLocation = world.GetEntityLocation(MovingEntityId);
        if (movingLocation.PlaneId != counterpartyInventoryPlaneId)
        {
            ActionTrace.Fail(trace, FailureReason.TargetNotInInventory, $"{MovingEntityId} is not contained by counterparty {counterpartyId}");
            return false;
        }

        if (!counterparty.HasUsableInventory)
        {
            ActionTrace.Fail(trace, FailureReason.TargetInventoryUnusable, $"{counterparty.Name} inventory dimensions are {counterparty.InventoryWidth}x{counterparty.InventoryHeight}");
            return false;
        }

        if (world.GetRegisteredInventoryPlaneId(actorId) is not { } actorInventoryPlaneId)
        {
            ActionTrace.Fail(trace, FailureReason.ActorHasNoInventory, $"{actor.Name} has no inventory plane");
            return false;
        }

        if (!actor.HasUsableInventory)
        {
            ActionTrace.Fail(trace, FailureReason.ActorInventoryUnusable, $"{actor.Name} inventory dimensions are {actor.InventoryWidth}x{actor.InventoryHeight}");
            return false;
        }

        if (!world.Planes.ContainsKey(actorInventoryPlaneId))
        {
            ActionTrace.Fail(trace, FailureReason.InvalidInventoryDestination, $"inventory plane {actorInventoryPlaneId} does not exist");
            return false;
        }

        var policies = new InventoryBoundaryPolicyService();
        var transitions = new InventoryTransitionService();
        InventoryTransitionEvaluation? lastTransitionFailure = null;
        InventoryBoundaryPolicyEvaluation? lastPolicyFailure = null;
        foreach (var candidate in policies.OrderedEnterPolicyDestinations(world, actorId, actorId))
        {
            if (!movement.CanPlace(world, candidate))
            {
                continue;
            }

            var transition = transitions.Evaluate(world, MovingEntityId, candidate);
            trace.Add(transition.Trace);
            if (!transition.CanTransition)
            {
                lastTransitionFailure = transition;
                continue;
            }

            var actorLocation = world.GetEntityLocation(actorId);
            var exitPolicy = policies.EvaluateExitPolicy(world, MovingEntityId, actorLocation, actorId);
            trace.Add(exitPolicy.Trace);
            if (!exitPolicy.CanPass)
            {
                lastPolicyFailure = exitPolicy;
                continue;
            }

            destination = candidate;
            return true;
        }

        if (lastPolicyFailure is { CanPass: false })
        {
            ActionTrace.Fail(trace, lastPolicyFailure.Trace.Reason, lastPolicyFailure.Trace.Detail ?? "source exit policy blocks selected item");
            return false;
        }

        if (lastTransitionFailure is { CanTransition: false })
        {
            ActionTrace.Fail(trace, lastTransitionFailure.Trace.Reason, lastTransitionFailure.Trace.Detail ?? "inventory transition blocks selected item");
            return false;
        }

        ActionTrace.Fail(trace, FailureReason.InvalidPlacement, $"no inventory coordinate in {actorInventoryPlaneId} can accept {MovingEntityId}");
        return false;
    }
}

public sealed record EnterAction(EntityId TargetId) : IActionIntent
{
    public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement)
    {
        var trace = new TraceNode($"Enter {TargetId}", TraceStatus.Info);

        if (!world.Entities.TryGetValue(actorId, out var actor))
        {
            return ActionTrace.Fail(trace, FailureReason.ActorMissing, $"actor {actorId} does not exist");
        }

        if (!world.Entities.TryGetValue(TargetId, out var target))
        {
            return ActionTrace.Fail(trace, FailureReason.TargetMissing, $"target {TargetId} does not exist");
        }

        if (TargetId == actorId)
        {
            return ActionTrace.Fail(trace, FailureReason.TargetIsActor, "actor cannot enter itself");
        }

        if (InventoryBoundaryPolicyService.WouldCreateContainmentCycle(world, actorId, TargetId))
        {
            return ActionTrace.Fail(trace, FailureReason.InventoryPolicyBlocked, $"actor {actorId} cannot enter contained descendant {TargetId}");
        }

        var adjacency = movement.EvaluateAdjacency(world, actorId, TargetId);
        if (!adjacency.AreAdjacent)
        {
            var detail = adjacency.FailureDetail is { Length: > 0 }
                ? $"{world.FormatEntityAddress(TargetId)} is not adjacent to {world.FormatEntityAddress(actorId)}: {adjacency.FailureDetail}"
                : $"{world.FormatEntityAddress(TargetId)} is not adjacent to {world.FormatEntityAddress(actorId)}";
            return ActionTrace.Fail(trace, FailureReason.TargetNotAdjacent, detail);
        }

        trace.Add(TraceNode.Success("Target is adjacent"));

        if (world.GetRegisteredInventoryPlaneId(TargetId) is not { } inventoryPlaneId)
        {
            return ActionTrace.Fail(trace, FailureReason.TargetHasNoInventory, $"target {TargetId} ({target.Name}) has no inventory plane");
        }

        if (!target.HasUsableInventory)
        {
            return ActionTrace.Fail(trace, FailureReason.TargetInventoryUnusable, $"target {TargetId} ({target.Name}) inventory dimensions are {target.InventoryWidth}x{target.InventoryHeight}");
        }

        if (!world.Planes.TryGetValue(inventoryPlaneId, out _))
        {
            return ActionTrace.Fail(trace, FailureReason.InvalidInventoryDestination, $"inventory plane {inventoryPlaneId} does not exist");
        }

        var constrainedRelocation = new ConstrainedInventoryRelocationService(movement, ignoredPolicyOwnerId: actorId);
        var placement = new InventoryBoundaryPolicyService().EvaluatePolicyAwarePlacement(world, actorId, TargetId, constrainedRelocation, actorId);
        trace.Add(placement.Trace);
        if (placement is { CanRelocate: true, Destination: { } resolvedDestination })
        {
            trace.Status = TraceStatus.Success;
            trace.Detail = $"entered {actorId} ({actor.Name}) into {TargetId} ({target.Name}) at {resolvedDestination.Coord}";
            return new ActionEvaluation(true, trace);
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = placement.Trace.Reason == FailureReason.None ? FailureReason.InvalidPlacement : placement.Trace.Reason;
        trace.Detail = $"no inventory coordinate can accept entering {actorId}";
        if (!string.IsNullOrWhiteSpace(placement.Trace.Detail))
        {
            trace.Detail += $"; last failure: {placement.Trace.Detail}";
        }

        return new ActionEvaluation(false, trace);
    }

    public void Execute(WorldState world, EntityId actorId, MovementService movement)
    {
        if (world.GetRegisteredInventoryPlaneId(TargetId) is not { } inventoryPlaneId ||
            !world.Planes.TryGetValue(inventoryPlaneId, out var inventoryPlane))
        {
            return;
        }

        var constrainedRelocation = new ConstrainedInventoryRelocationService(movement, ignoredPolicyOwnerId: actorId);
        var placement = new InventoryBoundaryPolicyService().EvaluatePolicyAwarePlacement(world, actorId, TargetId, constrainedRelocation, actorId);
        if (placement is { CanRelocate: true, Destination: { } resolvedDestination })
        {
            movement.TryPlace(world, actorId, resolvedDestination);
        }
    }
}

public sealed record ExitAction(Direction Direction) : IActionIntent
{
    public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement)
    {
        var trace = new TraceNode($"Exit {Direction}", TraceStatus.Info);

        if (!world.Entities.TryGetValue(actorId, out var actor))
        {
            return ActionTrace.Fail(trace, FailureReason.ActorMissing, $"actor {actorId} does not exist");
        }

        var actorLocation = world.GetEntityLocation(actorId);
        if (!InventoryPlaneOwnership.TryFindOwner(world, actorLocation.PlaneId, out var containerId) ||
            !world.Entities.TryGetValue(containerId, out var container))
        {
            return ActionTrace.Fail(trace, FailureReason.TargetNotInInventory, $"{actor.Name} is not inside an entity inventory plane");
        }

        var constrainedRelocation = new ConstrainedInventoryRelocationService(movement, ignoredPolicyOwnerId: actorId);
        var evaluation = constrainedRelocation.Evaluate(world, actorId, MovementDestination.AdjacentTo(containerId, Direction));
        trace.Add(evaluation.Trace);

        if (!evaluation.CanRelocate || evaluation.Destination is not { } resolvedDestination)
        {
            return ActionTrace.Fail(trace, evaluation.Trace.Reason, evaluation.Trace.Detail ?? $"cannot exit {containerId} toward {Direction}");
        }

        trace.Status = TraceStatus.Success;
        trace.Detail = $"exited {actorId} ({actor.Name}) from {containerId} ({container.Name}) to {resolvedDestination.Coord}";
        return new ActionEvaluation(true, trace);
    }

    public void Execute(WorldState world, EntityId actorId, MovementService movement)
    {
        var actorLocation = world.GetEntityLocation(actorId);
        if (InventoryPlaneOwnership.TryFindOwner(world, actorLocation.PlaneId, out var containerId))
        {
            new ConstrainedInventoryRelocationService(movement, ignoredPolicyOwnerId: actorId)
                .TryRelocate(world, actorId, MovementDestination.AdjacentTo(containerId, Direction));
        }
    }

    public ActionResolution Resolve(WorldState world, EntityId actorId, MovementService movement)
    {
        var evaluation = Evaluate(world, actorId, movement);

        if (!evaluation.CanExecute)
        {
            return new ActionResolution(false, ConsumesTurn: false, ContinuePlan: true, evaluation.Trace);
        }

        Execute(world, actorId, movement);
        return new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, evaluation.Trace, Direction);
    }
}

internal static class ActionTrace
{
    public static ActionEvaluation Fail(TraceNode trace, FailureReason reason, string detail)
    {
        trace.Status = TraceStatus.Failure;
        trace.Reason = reason;
        trace.Detail = detail;

        return new ActionEvaluation(false, trace);
    }
}
