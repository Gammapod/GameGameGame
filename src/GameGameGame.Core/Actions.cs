namespace GameGameGame.Core;

public interface IActionIntent
{
    ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement);

    bool CanExecute(WorldState world, EntityId actorId, MovementService movement) =>
        Evaluate(world, actorId, movement).CanExecute;

    void Execute(WorldState world, EntityId actorId, MovementService movement);
}

public sealed record PlannedActionPlan(IReadOnlyList<IActionIntent> Options)
{
    public static PlannedActionPlan Single(IActionIntent action) => new([action]);
}

public sealed record MoveAction(Direction Direction) : IActionIntent
{
    public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement)
    {
        var trace = new TraceNode($"Move {Direction}", TraceStatus.Info);

        if (!movement.TryGetMoveDestination(world, actorId, Direction, out var destination))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.MoveOutOfBounds;
            trace.Detail = "destination is outside the current plane";
            return new ActionEvaluation(false, trace);
        }

        trace.Add(TraceNode.Info("Destination", destination.ToString()));

        if (world.GetOccupant(destination) is { } occupantId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.MoveBlocked;
            trace.Detail = $"blocked by {world.FormatEntityAddress(occupantId)}";
            return new ActionEvaluation(false, trace);
        }

        trace.Status = TraceStatus.Success;
        return new ActionEvaluation(true, trace);
    }

    public void Execute(WorldState world, EntityId actorId, MovementService movement) =>
        movement.TryMove(world, actorId, Direction);
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

        if (actor.InventoryPlaneId is not { } inventoryPlaneId)
        {
            return ActionTrace.Fail(trace, FailureReason.ActorHasNoInventory, $"{actor.Name} has no inventory plane");
        }

        trace.Add(TraceNode.Success("Actor has inventory", inventoryPlaneId.ToString()));

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

        if (!movement.CanPlace(world, Destination))
        {
            return ActionTrace.Fail(trace, FailureReason.InvalidPlacement, $"cannot place into {Destination}");
        }

        trace.Add(TraceNode.Success("Destination can accept entity", Destination.ToString()));

        var weight = new WeightService();
        var carriedWeight = weight.GetCarriedWeight(world, actorId);
        var targetWeight = weight.GetTotalWeight(world, TargetId);
        var capacityTrace = new TraceNode("Check carrying capacity", TraceStatus.Info, detail: $"carried={carriedWeight}, target={targetWeight}, capacity={actor.CarryingCapacity}");
        capacityTrace.Add(weight.TraceTotalWeight(world, TargetId));

        if (carriedWeight + targetWeight > actor.CarryingCapacity)
        {
            capacityTrace.Status = TraceStatus.Failure;
            capacityTrace.Reason = FailureReason.CapacityExceeded;
            trace.Add(capacityTrace);
            return ActionTrace.Fail(trace, FailureReason.CapacityExceeded, $"{actor.Name} would carry {carriedWeight + targetWeight}/{actor.CarryingCapacity}");
        }

        capacityTrace.Status = TraceStatus.Success;
        trace.Add(capacityTrace);
        trace.Status = TraceStatus.Success;
        return new ActionEvaluation(true, trace);
    }

    public void Execute(WorldState world, EntityId actorId, MovementService movement) =>
        movement.TryPlace(world, TargetId, Destination);
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

        if (actor.InventoryPlaneId is not { } inventoryPlaneId)
        {
            return ActionTrace.Fail(trace, FailureReason.ActorHasNoInventory, $"{actor.Name} has no inventory plane");
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

        if (!movement.CanPlace(world, Destination))
        {
            return ActionTrace.Fail(trace, FailureReason.InvalidPlacement, $"cannot place into {Destination}");
        }

        trace.Add(TraceNode.Success("Destination can accept entity", Destination.ToString()));
        trace.Status = TraceStatus.Success;
        return new ActionEvaluation(true, trace);
    }

    public void Execute(WorldState world, EntityId actorId, MovementService movement) =>
        movement.TryPlace(world, TargetId, Destination);

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
