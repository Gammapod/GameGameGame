namespace GameGameGame.Core;

public sealed partial class ActionPlanInterpreter
{
    private PlanEffectResult ApplyTargetPathMove(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        ActionPlanBehaviorStepDescriptor step)
    {
        var trace = new TraceNode("Primitive TargetPathMove", TraceStatus.Info);
        if (step.PathMode is not { } mode)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = "TargetPathMove requires pathMode";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!TryReadTargetPathEndpoints(world, actorId, context, trace, out var targetId, out var actorLocation, out var targetLocation))
        {
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var adjacency = GetLegalTargetAdjacency(world, actorId, targetLocation).ToHashSet();
        if (adjacency.Count == 0)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = $"no reachable target-adjacent spaces around {targetId}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        return mode switch
        {
            ActionPlanTargetPathMode.SeekAdjacency => ApplyTargetPathSeek(world, actorId, targetId, actorLocation, adjacency, trace),
            ActionPlanTargetPathMode.FleeAdjacency => ApplyTargetPathFlee(world, actorId, targetId, actorLocation, adjacency, trace),
            ActionPlanTargetPathMode.MaintainDistance => ApplyTargetPathMaintainDistance(world, actorId, targetId, actorLocation, adjacency, step, trace),
            ActionPlanTargetPathMode.Orbit => ApplyTargetPathOrbit(world, actorId, targetId, actorLocation, targetLocation, adjacency, step, trace),
            _ => UnsupportedTargetPathMode(mode, trace)
        };
    }

    private PlanEffectResult ApplyTargetPathSeek(
        WorldState world,
        EntityId actorId,
        EntityId targetId,
        PlaneCoord actorLocation,
        HashSet<PlaneCoord> adjacency,
        TraceNode trace)
    {
        if (adjacency.Contains(actorLocation))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetNotAdjacent;
            trace.Detail = $"already at target adjacency for {targetId}; preserving Target for followup";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var path = FindShortestPathToAny(world, actorId, actorLocation, adjacency);
        if (path is null || path.Count == 0)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = $"no reachable target-adjacent path to {targetId}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var next = path[0];
        trace.Add(TraceNode.Success("Select target path seek step", $"{next.Direction} to {next.Destination}; pathDistance={path.Count}; target={targetId}"));
        _movement.TryPlace(world, actorId, next.Destination);
        world.SetActionFacing(actorId, next.Direction);
        trace.Add(TraceNode.Success("Set Facing", next.Direction.ToString()));
        trace.Status = TraceStatus.Success;
        trace.Detail = $"moved {next.Direction} toward target adjacency for {targetId}; pathDistance={path.Count}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace, next.Direction);
    }

    private PlanEffectResult ApplyTargetPathFlee(
        WorldState world,
        EntityId actorId,
        EntityId targetId,
        PlaneCoord actorLocation,
        HashSet<PlaneCoord> adjacency,
        TraceNode trace)
    {
        var currentDistance = DistanceToAny(world, actorId, actorLocation, adjacency);
        if (currentDistance is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = $"no reachable target-adjacent spaces from actor to {targetId}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var best = _movement.GetLegalMovementNeighbors(world, actorId, actorLocation)
            .Select(neighbor => new TargetPathCandidate(
                neighbor.Destination,
                neighbor.Direction,
                DistanceToAny(world, actorId, neighbor.Destination, adjacency)))
            .Where(candidate => candidate.Distance is not null && candidate.Distance > currentDistance)
            .OrderByDescending(candidate => candidate.Distance!.Value)
            .ThenBy(candidate => DirectionMath.AllDirections.IndexOf(candidate.Direction))
            .FirstOrDefault();

        if (best is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = $"no valid distance-increasing flee step from target adjacency for {targetId}; distance={currentDistance}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(TraceNode.Success("Select target path flee step", $"{best.Direction} to {best.Destination}; distance {currentDistance}->{best.Distance}; target={targetId}"));
        _movement.TryPlace(world, actorId, best.Destination);
        world.SetActionFacing(actorId, best.Direction);
        trace.Add(TraceNode.Success("Set Facing", best.Direction.ToString()));
        trace.Status = TraceStatus.Success;
        trace.Detail = $"moved {best.Direction} away from target adjacency for {targetId}; distance {currentDistance}->{best.Distance}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace, best.Direction);
    }

    private PlanEffectResult ApplyTargetPathMaintainDistance(
        WorldState world,
        EntityId actorId,
        EntityId targetId,
        PlaneCoord actorLocation,
        HashSet<PlaneCoord> adjacency,
        ActionPlanBehaviorStepDescriptor step,
        TraceNode trace)
    {
        if (step.DesiredDistance is not { } desiredDistance || desiredDistance < 0)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = "TargetPathMove MaintainDistance requires non-negative desiredDistance";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        return ApplyTargetPathDistanceCorrection(world, actorId, targetId, actorLocation, adjacency, desiredDistance, trace, orbitCorrection: false);
    }

    private PlanEffectResult ApplyTargetPathOrbit(
        WorldState world,
        EntityId actorId,
        EntityId targetId,
        PlaneCoord actorLocation,
        PlaneCoord targetLocation,
        HashSet<PlaneCoord> adjacency,
        ActionPlanBehaviorStepDescriptor step,
        TraceNode trace)
    {
        if (step.DesiredDistance is not { } desiredDistance || desiredDistance < 0)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = "TargetPathMove Orbit requires non-negative desiredDistance";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (step.OrbitDirection is not { } orbitDirection)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = "TargetPathMove Orbit requires orbitDirection";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var currentDistance = DistanceToAny(world, actorId, actorLocation, adjacency);
        if (currentDistance is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = $"no reachable target-adjacent spaces from actor to {targetId}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (currentDistance.Value != desiredDistance)
        {
            return ApplyTargetPathDistanceCorrection(world, actorId, targetId, actorLocation, adjacency, desiredDistance, trace, orbitCorrection: true);
        }

        var stepCandidate = SelectOrbitStep(world, actorId, actorLocation, targetLocation, adjacency, desiredDistance, orbitDirection);
        if (stepCandidate is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = $"no {orbitDirection} orbit step on distance {desiredDistance} ring around {targetId}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var evaluation = _movement.EvaluateRelocation(world, actorId, MovementDestination.Plane(stepCandidate.Destination));
        trace.Add(evaluation.Trace);
        if (!evaluation.CanRelocate || evaluation.Destination is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = evaluation.Trace.Reason;
            trace.Detail = $"orbit {orbitDirection} step {stepCandidate.Direction} blocked: {evaluation.Trace.Detail}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(TraceNode.Success("Select target path orbit step", $"{stepCandidate.Direction} to {stepCandidate.Destination}; orbit {orbitDirection}; desiredDistance={desiredDistance}; target={targetId}"));
        _movement.TryPlace(world, actorId, stepCandidate.Destination);
        world.SetActionFacing(actorId, stepCandidate.Direction);
        trace.Add(TraceNode.Success("Set Facing", stepCandidate.Direction.ToString()));
        trace.Status = TraceStatus.Success;
        trace.Detail = $"moved {stepCandidate.Direction} for orbit {orbitDirection} around {targetId}; desiredDistance={desiredDistance}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace, stepCandidate.Direction);
    }

    private PlanEffectResult ApplyTargetPathDistanceCorrection(
        WorldState world,
        EntityId actorId,
        EntityId targetId,
        PlaneCoord actorLocation,
        HashSet<PlaneCoord> adjacency,
        int desiredDistance,
        TraceNode trace,
        bool orbitCorrection)
    {
        var currentDistance = DistanceToAny(world, actorId, actorLocation, adjacency);
        if (currentDistance is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = $"no reachable target-adjacent spaces from actor to {targetId}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (currentDistance.Value == desiredDistance)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = $"already at desired distance {desiredDistance} from target adjacency for {targetId}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var correction = _movement.GetLegalMovementNeighbors(world, actorId, actorLocation)
            .Select(neighbor => new TargetPathCandidate(
                neighbor.Destination,
                neighbor.Direction,
                DistanceToAny(world, actorId, neighbor.Destination, adjacency)))
            .Where(candidate => candidate.Distance is not null)
            .Where(candidate => currentDistance.Value > desiredDistance
                ? candidate.Distance!.Value < currentDistance.Value
                : candidate.Distance!.Value > currentDistance.Value)
            .OrderBy(candidate => Math.Abs(candidate.Distance!.Value - desiredDistance))
            .ThenBy(candidate => DirectionMath.AllDirections.IndexOf(candidate.Direction))
            .FirstOrDefault();

        if (correction is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = $"no valid step toward desired distance {desiredDistance} from target adjacency for {targetId}; distance={currentDistance}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(TraceNode.Success("Select target path distance-correction step", $"{correction.Direction} to {correction.Destination}; distance {currentDistance}->{correction.Distance}; desiredDistance={desiredDistance}; target={targetId}"));
        _movement.TryPlace(world, actorId, correction.Destination);
        world.SetActionFacing(actorId, correction.Direction);
        trace.Add(TraceNode.Success("Set Facing", correction.Direction.ToString()));
        trace.Status = TraceStatus.Success;
        trace.Detail = orbitCorrection
            ? $"moved {correction.Direction}; corrected toward desired distance {desiredDistance} from target adjacency for {targetId}; distance {currentDistance}->{correction.Distance}"
            : $"moved {correction.Direction} toward desired distance {desiredDistance} from target adjacency for {targetId}; distance {currentDistance}->{correction.Distance}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace, correction.Direction);
    }

    private bool TryReadTargetPathEndpoints(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        TraceNode trace,
        out EntityId targetId,
        out PlaneCoord actorLocation,
        out PlaneCoord targetLocation)
    {
        targetId = default;
        actorLocation = default;
        targetLocation = default;
        if (!context.TryRead<EntityPlanValue>(ActionPlanSlot.Target, out var target, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return false;
        }

        trace.Add(readTrace);
        targetId = target.Value;
        if (targetId == actorId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetIsActor;
            trace.Detail = "TargetPathMove cannot target self";
            return false;
        }

        if (!world.Entities.ContainsKey(targetId))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"target {targetId} does not exist";
            return false;
        }

        if (!world.Entities.ContainsKey(actorId))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ActorMissing;
            trace.Detail = $"actor {actorId} does not exist";
            return false;
        }

        actorLocation = world.GetEntityLocation(actorId);
        targetLocation = world.GetEntityLocation(targetId);
        if (actorLocation.PlaneId != targetLocation.PlaneId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"target {targetId} is off-plane at {targetLocation}; actor is on {actorLocation.PlaneId}";
            return false;
        }

        return true;
    }

    private IEnumerable<PlaneCoord> GetLegalTargetAdjacency(WorldState world, EntityId movingEntityId, PlaneCoord targetLocation)
    {
        return _movement.GetLegalMovementNeighbors(world, movingEntityId, targetLocation)
            .Select(neighbor => neighbor.Destination);
    }

    private int? DistanceToAny(WorldState world, EntityId actorId, PlaneCoord start, HashSet<PlaneCoord> goals)
    {
        var halfStepDistance = HalfStepDistanceToAny(world, actorId, start, goals);
        return halfStepDistance is null ? null : halfStepDistance.Value / 2;
    }

    private int? HalfStepDistanceToAny(WorldState world, EntityId actorId, PlaneCoord start, HashSet<PlaneCoord> goals)
    {
        var graph = TopologyGraphMaterializer.Materialize(world);
        if (!TryGetTopologyNodeId(world, start, out var startNodeId) || !TryGetTopologyNodeIds(world, goals, out var goalNodeIds))
        {
            return null;
        }

        return TopologyGraphTraversalService.HalfStepDistanceToAny(
            graph,
            startNodeId,
            goalNodeIds,
            edge => CanTraverseForTargetPath(world, graph, actorId, edge),
            HalfStepCost);
    }

    private static int HalfStepCost(Direction direction) => DirectionMath.OrthogonalCorners(direction) is null ? 2 : 3;

    private TargetPathStep? SelectOrbitStep(
        WorldState world,
        EntityId actorId,
        PlaneCoord actorLocation,
        PlaneCoord targetLocation,
        HashSet<PlaneCoord> adjacency,
        int desiredDistance,
        ActionPlanOrbitDirection orbitDirection)
    {
        return DirectionMath.AllDirections
            .Select(direction => TryGetOrbitCandidate(world, actorId, actorLocation, targetLocation, adjacency, desiredDistance, orbitDirection, direction))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .OrderBy(candidate => candidate.AngleDelta)
            .ThenBy(candidate => DirectionMath.AllDirections.IndexOf(candidate.Step.Direction))
            .Select(candidate => candidate.Step)
            .FirstOrDefault();
    }

    private TargetPathOrbitCandidate? TryGetOrbitCandidate(
        WorldState world,
        EntityId actorId,
        PlaneCoord actorLocation,
        PlaneCoord targetLocation,
        HashSet<PlaneCoord> adjacency,
        int desiredDistance,
        ActionPlanOrbitDirection orbitDirection,
        Direction direction)
    {
        if (!_movement.TryGetMoveDestination(world, actorId, direction, out var destination))
        {
            return null;
        }

        if (DistanceToAny(world, actorId, destination, adjacency) != desiredDistance)
        {
            return null;
        }

        var currentAngle = ClockAngle(actorLocation.Coord, targetLocation.Coord);
        var candidateAngle = ClockAngle(destination.Coord, targetLocation.Coord);
        var delta = orbitDirection == ActionPlanOrbitDirection.Clockwise
            ? PositiveAngleDelta(currentAngle, candidateAngle)
            : PositiveAngleDelta(candidateAngle, currentAngle);
        if (delta <= 0.000001 || delta >= Math.Tau - 0.000001)
        {
            return null;
        }

        return new TargetPathOrbitCandidate(new TargetPathStep(destination, direction), delta);
    }

    private List<TargetPathStep>? FindShortestPathToAny(WorldState world, EntityId actorId, PlaneCoord start, HashSet<PlaneCoord> goals)
    {
        var graph = TopologyGraphMaterializer.Materialize(world);
        if (!TryGetTopologyNodeId(world, start, out var startNodeId) || !TryGetTopologyNodeIds(world, goals, out var goalNodeIds))
        {
            return null;
        }

        var path = TopologyGraphTraversalService.ShortestPathToAny(
            graph,
            startNodeId,
            goalNodeIds,
            edge => CanTraverseForTargetPath(world, graph, actorId, edge));

        return path?.Select(step => new TargetPathStep(step.SourceCoord, step.Direction)).ToList();
    }

    private static bool TryGetTopologyNodeId(WorldState world, PlaneCoord coord, out TopologyNodeId topologyNodeId)
    {
        if (world.TryGetNodeId(coord, out var nodeId))
        {
            topologyNodeId = new TopologyNodeId(nodeId.Value);
            return true;
        }

        topologyNodeId = default;
        return false;
    }

    private static bool TryGetTopologyNodeIds(WorldState world, IEnumerable<PlaneCoord> coords, out HashSet<TopologyNodeId> topologyNodeIds)
    {
        topologyNodeIds = [];
        foreach (var coord in coords)
        {
            if (!TryGetTopologyNodeId(world, coord, out var topologyNodeId))
            {
                return false;
            }

            topologyNodeIds.Add(topologyNodeId);
        }

        return topologyNodeIds.Count > 0;
    }

    private bool CanTraverseForTargetPath(WorldState world, TopologyGraph graph, EntityId actorId, TopologyGraphEdge edge) =>
        !edge.IsBlocked &&
        graph.TryGetNode(edge.DestinationNodeId, out var destinationNode) &&
        _movement.CanOccupyForPath(world, actorId, destinationNode.SourceCoord);

    private static PlanEffectResult UnsupportedTargetPathMode(ActionPlanTargetPathMode mode, TraceNode trace)
    {
        trace.Status = TraceStatus.Failure;
        trace.Detail = $"TargetPathMove pathMode {mode} is not implemented in this slice";
        return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
    }

    private sealed record TargetPathStep(PlaneCoord Destination, Direction Direction);

    private sealed record TargetPathCandidate(PlaneCoord Destination, Direction Direction, int? Distance);

    private sealed record TargetPathOrbitCandidate(TargetPathStep Step, double AngleDelta);

    private static double ClockAngle(GridCoord coord, GridCoord center)
    {
        var dx = coord.X - center.X;
        var dy = coord.Y - center.Y;
        var angle = Math.Atan2(dx, -dy);
        return angle < 0 ? angle + Math.Tau : angle;
    }

    private static double PositiveAngleDelta(double from, double to)
    {
        var delta = to - from;
        return delta < 0 ? delta + Math.Tau : delta;
    }
}
