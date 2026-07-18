namespace GameGameGame.Core;

public sealed partial class ActionPlanInterpreter
{
    private PlanEffectResult ApplyAcquireNearestTarget(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive AcquireNearestTarget", TraceStatus.Info);
        if (!world.Entities.ContainsKey(actorId))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ActorMissing;
            trace.Detail = $"actor {actorId} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var actorLocation = world.GetEntityLocation(actorId);
        trace.Add(TraceNode.Success("Read actor position", actorLocation.ToString()));

        var selected = world.Occupancy
            .Select(entry => (EntityId: entry.Value, Node: world.Nodes[entry.Key]))
            .Where(entry => entry.EntityId != actorId && entry.Node.PlaneId == actorLocation.PlaneId)
            .Select(entry => new
            {
                entry.EntityId,
                entry.Node.Coord,
                Distance = ManhattanDistance(actorLocation.Coord, entry.Node.Coord)
            })
            .OrderBy(entry => entry.Distance)
            .ThenBy(entry => entry.Coord.Y)
            .ThenBy(entry => entry.Coord.X)
            .ThenBy(entry => entry.EntityId.Value, StringComparer.Ordinal)
            .FirstOrDefault();

        if (selected is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"no same-plane target found on {actorLocation.PlaneId}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(context.Set(ActionPlanSlot.Target, new EntityPlanValue(selected.EntityId)));
        var selectedEntity = world.Entities[selected.EntityId];
        trace.Status = TraceStatus.Success;
        trace.Detail = $"selected {selected.EntityId} ({selectedEntity.Name}) at {actorLocation.PlaneId}{selected.Coord}; distance={selected.Distance}; tieBreak=row-major";
        return new PlanEffectResult(true, ConsumesTurn: false, ContinuePlan: true, trace);
    }

    private PlanEffectResult ApplySeekTarget(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive SeekTarget", TraceStatus.Info);
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
            trace.Reason = FailureReason.TargetIsActor;
            trace.Detail = "SeekTarget cannot seek self";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!world.Entities.ContainsKey(target.Value))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"target {target.Value} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!world.Entities.ContainsKey(actorId))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ActorMissing;
            trace.Detail = $"actor {actorId} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var actorLocation = world.GetEntityLocation(actorId);
        var targetLocation = world.GetEntityLocation(target.Value);
        if (actorLocation.PlaneId != targetLocation.PlaneId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"target {target.Value} is off-plane at {targetLocation}; actor is on {actorLocation.PlaneId}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var currentDistance = ManhattanDistance(actorLocation.Coord, targetLocation.Coord);
        var step = SeekDirections()
            .Select(direction => new
            {
                Direction = direction,
                Destination = new PlaneCoord(actorLocation.PlaneId, actorLocation.Coord.Offset(direction)),
                Distance = ManhattanDistance(actorLocation.Coord.Offset(direction), targetLocation.Coord)
            })
            .Where(candidate => candidate.Distance < currentDistance)
            .FirstOrDefault();

        if (step is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"no cardinal step reduces distance to target {target.Value}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(TraceNode.Success("Select seek step", $"{step.Direction} to {step.Destination}; distance {currentDistance}->{step.Distance}; tieBreak=North,South,West,East"));

        if (step.Destination == targetLocation)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetNotAdjacent;
            trace.Detail = $"target {target.Value} is adjacent at {targetLocation}; preserving Target for followup";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var evaluation = _movement.EvaluateRelocation(world, actorId, MovementDestination.Plane(step.Destination));
        trace.Add(evaluation.Trace);
        if (!evaluation.CanRelocate || evaluation.Destination is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = evaluation.Trace.Reason;
            trace.Detail = $"seek step {step.Direction} blocked: {evaluation.Trace.Detail}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        _movement.TryPlace(world, actorId, step.Destination);
        trace.Status = TraceStatus.Success;
        trace.Detail = $"moved {step.Direction} toward {target.Value}; distance {currentDistance}->{step.Distance}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace, step.Direction);
    }

    private PlanEffectResult ApplyFleeTarget(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive FleeTarget", TraceStatus.Info);
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
            trace.Reason = FailureReason.TargetIsActor;
            trace.Detail = "FleeTarget cannot flee self";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!world.Entities.ContainsKey(target.Value))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"target {target.Value} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!world.Entities.ContainsKey(actorId))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ActorMissing;
            trace.Detail = $"actor {actorId} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var actorLocation = world.GetEntityLocation(actorId);
        var targetLocation = world.GetEntityLocation(target.Value);
        if (actorLocation.PlaneId != targetLocation.PlaneId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"target {target.Value} is off-plane at {targetLocation}; actor is on {actorLocation.PlaneId}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var currentDistance = ManhattanDistance(actorLocation.Coord, targetLocation.Coord);
        var candidates = SeekDirections()
            .Select(direction => new
            {
                Direction = direction,
                Destination = new PlaneCoord(actorLocation.PlaneId, actorLocation.Coord.Offset(direction)),
                Distance = ManhattanDistance(actorLocation.Coord.Offset(direction), targetLocation.Coord)
            })
            .Where(candidate => candidate.Distance > currentDistance)
            .ToList();

        if (candidates.Count == 0)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"no cardinal step increases distance from target {target.Value}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        foreach (var step in candidates)
        {
            var evaluation = _movement.EvaluateRelocation(world, actorId, MovementDestination.Plane(step.Destination));
            trace.Add(evaluation.Trace);
            if (!evaluation.CanRelocate || evaluation.Destination is null)
            {
                trace.Add(TraceNode.Failure("Reject flee step", evaluation.Trace.Reason, $"{step.Direction} blocked: {evaluation.Trace.Detail}; distance {currentDistance}->{step.Distance}"));
                continue;
            }

            trace.Add(TraceNode.Success("Select flee step", $"{step.Direction} to {step.Destination}; distance {currentDistance}->{step.Distance}; tieBreak=North,South,West,East"));
            _movement.TryPlace(world, actorId, step.Destination);
            trace.Status = TraceStatus.Success;
            trace.Detail = $"moved {step.Direction} away from {target.Value}; distance {currentDistance}->{step.Distance}";
            return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace, step.Direction);
        }

        var lastFailure = trace.Children.LastOrDefault(child => child.Label == "Reject flee step");
        trace.Status = TraceStatus.Failure;
        trace.Reason = lastFailure?.Reason ?? FailureReason.InvalidPlacement;
        trace.Detail = $"no valid distance-increasing flee step from target {target.Value}; distance={currentDistance}";
        return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
    }

    private PlanEffectResult ApplyMaintainChebyshevDistanceTwo(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive MaintainChebyshevDistanceTwo", TraceStatus.Info);
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
            trace.Reason = FailureReason.TargetIsActor;
            trace.Detail = "MaintainChebyshevDistanceTwo cannot target self";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!world.Entities.ContainsKey(target.Value))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"target {target.Value} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!world.Entities.ContainsKey(actorId))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ActorMissing;
            trace.Detail = $"actor {actorId} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var actorLocation = world.GetEntityLocation(actorId);
        var targetLocation = world.GetEntityLocation(target.Value);
        if (actorLocation.PlaneId != targetLocation.PlaneId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"target {target.Value} is off-plane at {targetLocation}; actor is on {actorLocation.PlaneId}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        const int idealDistance = 2;
        var currentDistance = ChebyshevDistance(actorLocation.Coord, targetLocation.Coord);
        if (currentDistance == idealDistance)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"mode=ideal-distance fallthrough; target {target.Value}; distance={currentDistance}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var mode = currentDistance < idealDistance ? "flee/back-away" : "seek/close";
        var candidates = SeekDirections()
            .Select(direction => new
            {
                Direction = direction,
                Destination = new PlaneCoord(actorLocation.PlaneId, actorLocation.Coord.Offset(direction)),
                Distance = ChebyshevDistance(actorLocation.Coord.Offset(direction), targetLocation.Coord)
            })
            .Where(candidate => Math.Abs(candidate.Distance - idealDistance) < Math.Abs(currentDistance - idealDistance))
            .ToList();

        if (candidates.Count == 0)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"mode={mode}; no cardinal step improves Chebyshev distance to 2 from target {target.Value}; distance={currentDistance}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        foreach (var step in candidates)
        {
            var evaluation = _movement.EvaluateRelocation(world, actorId, MovementDestination.Plane(step.Destination));
            trace.Add(evaluation.Trace);
            if (!evaluation.CanRelocate || evaluation.Destination is null)
            {
                trace.Add(TraceNode.Failure("Reject distance-two step", evaluation.Trace.Reason, $"mode={mode}; {step.Direction} blocked: {evaluation.Trace.Detail}; distance {currentDistance}->{step.Distance}"));
                continue;
            }

            trace.Add(TraceNode.Success("Select distance-two step", $"mode={mode}; {step.Direction} to {step.Destination}; distance {currentDistance}->{step.Distance}; tieBreak=North,South,West,East"));
            _movement.TryPlace(world, actorId, step.Destination);
            trace.Status = TraceStatus.Success;
            trace.Detail = $"mode={mode}; moved {step.Direction} relative to {target.Value}; Chebyshev distance {currentDistance}->{step.Distance}";
            return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace, step.Direction);
        }

        var lastFailure = trace.Children.LastOrDefault(child => child.Label == "Reject distance-two step");
        trace.Status = TraceStatus.Failure;
        trace.Reason = lastFailure?.Reason ?? FailureReason.InvalidPlacement;
        trace.Detail = $"mode={mode}; no valid Chebyshev distance-2 step from target {target.Value}; distance={currentDistance}";
        return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
    }

    private PlanEffectResult ApplyStrafeTarget(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        bool clockwise)
    {
        var stepName = clockwise ? "StrafeClockwise" : "StrafeAnticlockwise";
        var trace = new TraceNode($"Primitive {stepName}", TraceStatus.Info);
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
            trace.Reason = FailureReason.TargetIsActor;
            trace.Detail = $"{stepName} cannot target self";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!world.Entities.ContainsKey(target.Value))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"target {target.Value} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!world.Entities.ContainsKey(actorId))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ActorMissing;
            trace.Detail = $"actor {actorId} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var actorLocation = world.GetEntityLocation(actorId);
        var targetLocation = world.GetEntityLocation(target.Value);
        if (actorLocation.PlaneId != targetLocation.PlaneId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"target {target.Value} is off-plane at {targetLocation}; actor is on {actorLocation.PlaneId}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var currentDistance = ManhattanDistance(actorLocation.Coord, targetLocation.Coord);
        var primary = SeekDirections()
            .Select(direction => new
            {
                Direction = direction,
                Distance = ManhattanDistance(actorLocation.Coord.Offset(direction), targetLocation.Coord)
            })
            .Where(candidate => candidate.Distance < currentDistance)
            .FirstOrDefault();

        if (primary is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"no primary seek direction reduces distance to target {target.Value}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var strafeDirection = clockwise ? Clockwise(primary.Direction) : Anticlockwise(primary.Direction);
        var strafeDestination = new PlaneCoord(actorLocation.PlaneId, actorLocation.Coord.Offset(strafeDirection));
        trace.Add(TraceNode.Success("Select strafe step", $"primary={primary.Direction}; strafe={strafeDirection} to {strafeDestination}; tieBreak=North,South,West,East"));

        var evaluation = _movement.EvaluateRelocation(world, actorId, MovementDestination.Plane(strafeDestination));
        trace.Add(evaluation.Trace);
        if (!evaluation.CanRelocate || evaluation.Destination is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = evaluation.Trace.Reason;
            trace.Detail = $"primary={primary.Direction}; strafe={strafeDirection} blocked: {evaluation.Trace.Detail}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        _movement.TryPlace(world, actorId, strafeDestination);
        trace.Status = TraceStatus.Success;
        trace.Detail = $"primary={primary.Direction}; moved {strafeDirection} strafing {(clockwise ? "clockwise" : "anticlockwise")} around {target.Value}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace, strafeDirection);
    }
}
