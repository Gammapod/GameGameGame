namespace GameGameGame.Core;

public sealed class ConstrainedInventoryRelocationService(
    MovementService movement,
    InventoryTransitionService? transitions = null,
    EntityId? ignoredPolicyOwnerId = null)
{
    private readonly InventoryTransitionService _transitions = transitions ?? new InventoryTransitionService();
    private readonly InventoryBoundaryPolicyService _policies = new();

    public ConstrainedRelocationEvaluation Evaluate(WorldState world, EntityId movingEntityId, MovementDestination destination)
    {
        var trace = new TraceNode($"Constrained inventory relocate {movingEntityId} -> {destination}", TraceStatus.Info);
        var relocation = movement.EvaluateRelocation(world, movingEntityId, destination);
        trace.Add(relocation.Trace);

        if (!relocation.CanRelocate || relocation.Destination is not { } resolvedDestination)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = relocation.Trace.Reason;
            trace.Detail = relocation.Trace.Detail;
            return new ConstrainedRelocationEvaluation(false, null, trace);
        }

        var transition = _transitions.Evaluate(world, movingEntityId, resolvedDestination);
        trace.Add(transition.Trace);

        if (!transition.CanTransition)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = transition.Trace.Reason;
            trace.Detail = transition.Trace.Detail;
            return new ConstrainedRelocationEvaluation(false, resolvedDestination, trace);
        }

        var exitPolicy = _policies.EvaluateExitPolicy(world, movingEntityId, resolvedDestination, ignoredPolicyOwnerId);
        trace.Add(exitPolicy.Trace);
        if (!exitPolicy.CanPass)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = exitPolicy.Trace.Reason;
            trace.Detail = exitPolicy.Trace.Detail;
            return new ConstrainedRelocationEvaluation(false, resolvedDestination, trace);
        }

        trace.Status = TraceStatus.Success;
        trace.Detail = resolvedDestination.ToString();
        return new ConstrainedRelocationEvaluation(true, resolvedDestination, trace);
    }

    public bool TryRelocate(WorldState world, EntityId movingEntityId, MovementDestination destination)
    {
        var evaluation = Evaluate(world, movingEntityId, destination);
        return evaluation is { CanRelocate: true, Destination: { } resolvedDestination }
            && movement.TryPlace(world, movingEntityId, resolvedDestination);
    }
}

public sealed record ConstrainedRelocationEvaluation(bool CanRelocate, PlaneCoord? Destination, TraceNode Trace);
