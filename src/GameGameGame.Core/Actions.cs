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

        if (!movement.AreAdjacent(world, actorId, TargetId))
        {
            return ActionTrace.Fail(trace, FailureReason.TargetNotAdjacent, $"{world.FormatEntityAddress(TargetId)} is not adjacent to {world.FormatEntityAddress(actorId)}");
        }

        trace.Add(TraceNode.Success("Target is adjacent"));

        var constrainedRelocation = new ConstrainedInventoryRelocationService(movement);
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
        new ConstrainedInventoryRelocationService(movement).TryRelocate(world, TargetId, MovementDestination.Plane(Destination));
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

        var constrainedRelocation = new ConstrainedInventoryRelocationService(movement);
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
        new ConstrainedInventoryRelocationService(movement).TryRelocate(world, TargetId, MovementDestination.Plane(Destination));

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

        if (!movement.AreAdjacent(world, actorId, TargetId))
        {
            return ActionTrace.Fail(trace, FailureReason.TargetNotAdjacent, $"{world.FormatEntityAddress(TargetId)} is not adjacent to {world.FormatEntityAddress(actorId)}");
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

        if (!world.Planes.TryGetValue(inventoryPlaneId, out var inventoryPlane))
        {
            return ActionTrace.Fail(trace, FailureReason.InvalidInventoryDestination, $"inventory plane {inventoryPlaneId} does not exist");
        }

        var constrainedRelocation = new ConstrainedInventoryRelocationService(movement);
        ActionResolution? lastFailure = null;
        for (var y = 0; y < inventoryPlane.Height; y++)
        {
            for (var x = 0; x < inventoryPlane.Width; x++)
            {
                var destination = new PlaneCoord(inventoryPlaneId, new GridCoord(x, y));
                var evaluation = constrainedRelocation.Evaluate(world, actorId, MovementDestination.Plane(destination));
                trace.Add(evaluation.Trace);

                if (evaluation.CanRelocate)
                {
                    trace.Status = TraceStatus.Success;
                    trace.Detail = $"entered {actorId} ({actor.Name}) into {TargetId} ({target.Name}) at {destination.Coord}";
                    return new ActionEvaluation(true, trace);
                }

                lastFailure = new ActionResolution(false, false, true, evaluation.Trace);
            }
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = lastFailure?.Trace.Reason ?? FailureReason.InvalidPlacement;
        trace.Detail = $"no inventory coordinate can accept entering {actorId}";
        if (!string.IsNullOrWhiteSpace(lastFailure?.Trace.Detail))
        {
            trace.Detail += $"; last failure: {lastFailure.Trace.Detail}";
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

        var constrainedRelocation = new ConstrainedInventoryRelocationService(movement);
        for (var y = 0; y < inventoryPlane.Height; y++)
        {
            for (var x = 0; x < inventoryPlane.Width; x++)
            {
                if (constrainedRelocation.TryRelocate(world, actorId, MovementDestination.Plane(new PlaneCoord(inventoryPlaneId, new GridCoord(x, y)))))
                {
                    return;
                }
            }
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

        var constrainedRelocation = new ConstrainedInventoryRelocationService(movement);
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
            new ConstrainedInventoryRelocationService(movement).TryRelocate(world, actorId, MovementDestination.AdjacentTo(containerId, Direction));
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

public sealed record GiveOverwriteAction(EntityId ProviderId, EntityId TargetActorId) : IActionIntent
{
    public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement)
    {
        var trace = new TraceNode($"GiveOverwrite {ProviderId} -> {TargetActorId}", TraceStatus.Info);

        if (!world.Entities.ContainsKey(actorId))
        {
            return ActionTrace.Fail(trace, FailureReason.ActorMissing, $"actor {actorId} does not exist");
        }

        if (!world.Entities.ContainsKey(ProviderId) || !world.Entities.ContainsKey(TargetActorId))
        {
            return ActionTrace.Fail(trace, FailureReason.TargetMissing, "provider or target actor does not exist");
        }

        if (ProviderId == actorId || TargetActorId == actorId || ProviderId == TargetActorId)
        {
            return ActionTrace.Fail(trace, FailureReason.TargetIsActor, "overwrite provider, target actor, and controlled actor must be distinct");
        }

        if (world.GetRegisteredInventoryPlaneId(actorId) is not { } inventoryPlaneId || world.GetEntityLocation(ProviderId).PlaneId != inventoryPlaneId)
        {
            return ActionTrace.Fail(trace, FailureReason.TargetNotInInventory, $"provider {ProviderId} is not carried by {actorId}");
        }

        if (!movement.AreAdjacent(world, actorId, TargetActorId))
        {
            return ActionTrace.Fail(trace, FailureReason.TargetNotAdjacent, $"target actor {TargetActorId} is not adjacent to {actorId}");
        }

        trace.Status = TraceStatus.Success;
        trace.Detail = $"{ProviderId} will override {TargetActorId}'s action plan";
        return new ActionEvaluation(true, trace);
    }

    public void Execute(WorldState world, EntityId actorId, MovementService movement) =>
        world.SetBehaviorProvider(TargetActorId, ProviderId);
}

public sealed record TakeOverwriteAction(EntityId TargetActorId, PlaneCoord Destination) : IActionIntent
{
    public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement)
    {
        var trace = new TraceNode($"TakeOverwrite {TargetActorId} -> {Destination}", TraceStatus.Info);

        if (!world.Entities.ContainsKey(actorId))
        {
            return ActionTrace.Fail(trace, FailureReason.ActorMissing, $"actor {actorId} does not exist");
        }

        if (!world.Entities.ContainsKey(TargetActorId))
        {
            return ActionTrace.Fail(trace, FailureReason.TargetMissing, $"target actor {TargetActorId} does not exist");
        }

        if (world.GetBehaviorProvider(TargetActorId) is not { } providerId || !world.Entities.ContainsKey(providerId))
        {
            return ActionTrace.Fail(trace, FailureReason.TargetMissing, $"target actor {TargetActorId} has no behavior provider");
        }

        if (world.GetRegisteredInventoryPlaneId(actorId) is not { } inventoryPlaneId || Destination.PlaneId != inventoryPlaneId)
        {
            return ActionTrace.Fail(trace, FailureReason.InvalidInventoryDestination, $"destination must be inside {actorId}'s inventory");
        }

        if (!movement.AreAdjacent(world, actorId, TargetActorId))
        {
            return ActionTrace.Fail(trace, FailureReason.TargetNotAdjacent, $"target actor {TargetActorId} is not adjacent to {actorId}");
        }

        var providerLocation = world.GetEntityLocation(providerId);
        if (providerLocation != Destination)
        {
            var relocation = new ConstrainedInventoryRelocationService(movement).Evaluate(world, providerId, MovementDestination.Plane(Destination));
            trace.Add(relocation.Trace);
            if (!relocation.CanRelocate)
            {
                return ActionTrace.Fail(trace, relocation.Trace.Reason, relocation.Trace.Detail ?? $"cannot return provider {providerId} to {Destination}");
            }
        }

        trace.Status = TraceStatus.Success;
        trace.Detail = $"{providerId} will stop overriding {TargetActorId}'s action plan";
        return new ActionEvaluation(true, trace);
    }

    public void Execute(WorldState world, EntityId actorId, MovementService movement)
    {
        if (world.GetBehaviorProvider(TargetActorId) is not { } providerId)
        {
            return;
        }

        if (world.GetEntityLocation(providerId) != Destination)
        {
            new ConstrainedInventoryRelocationService(movement).TryRelocate(world, providerId, MovementDestination.Plane(Destination));
        }

        world.ClearBehaviorProvider(TargetActorId);
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
