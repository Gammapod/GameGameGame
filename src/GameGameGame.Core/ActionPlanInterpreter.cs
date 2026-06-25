namespace GameGameGame.Core;

public readonly record struct ActionPlanId(string Value)
{
    public override string ToString() => Value;
}

public sealed record ActionPlanDefinition(
    ActionPlanId Id,
    IReadOnlyList<ActionPlanStep> Steps,
    ActionPlanPrimitiveDescriptor? Primitive = null,
    ActionPlanBehaviorDescriptor? Behavior = null);

public sealed record ActionPlanStep
{
    public ActionPlanStep(
        string Label,
        IReadOnlyList<IPlanCheck> Checks,
        IPlanEffect? onSuccess,
        IPlanEffect? onFailure)
    {
        this.Label = Label;
        this.Checks = Checks;
        OnSuccess = onSuccess;
        OnFailure = onFailure;
    }

    public string Label { get; }

    public IReadOnlyList<IPlanCheck> Checks { get; }

    public IPlanEffect? OnSuccess { get; }

    public IPlanEffect? OnFailure { get; }
}

public interface IPlanCheck
{
    PlanCheckResult Evaluate(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement);
}

public sealed record PlanCheckResult(
    bool Passed,
    IReadOnlyDictionary<string, PlanValue> VariableWrites,
    TraceNode Trace,
    IReadOnlyDictionary<ActionPlanSlot, PlanValue>? SlotWrites = null);

public interface IPlanEffect
{
    PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement);
}

public sealed record PlanEffectResult(
    bool Succeeded,
    bool ConsumesTurn,
    bool ContinuePlan,
    TraceNode Trace);

public sealed record PlanExecutionResult(
    bool Succeeded,
    bool ConsumesTurn,
    bool ContinuePlan,
    TraceNode Trace);

public sealed class ActionPlanInterpreter
{
    private readonly MovementService _movement;
    private readonly IReadOnlyDictionary<ActionPlanId, ActionPlanDefinition> _planRegistry;
    private readonly int _maxCallDepth;

    public ActionPlanInterpreter(MovementService movement)
        : this(movement, new Dictionary<ActionPlanId, ActionPlanDefinition>())
    {
    }

    public ActionPlanInterpreter(
        MovementService movement,
        IReadOnlyDictionary<ActionPlanId, ActionPlanDefinition> planRegistry,
        int maxCallDepth = 16)
    {
        _movement = movement;
        _planRegistry = planRegistry;
        _maxCallDepth = maxCallDepth;
    }

    public PlanExecutionResult Execute(
        WorldState world,
        EntityId actorId,
        ActionPlanDefinition plan,
        ActionPlanContext context) =>
        Execute(world, actorId, plan, context, callDepth: 0);

    private PlanExecutionResult Execute(
        WorldState world,
        EntityId actorId,
        ActionPlanDefinition plan,
        ActionPlanContext context,
        int callDepth)
    {
        context.AttachEntityActionState(world, actorId);
        var root = new TraceNode($"Plan {plan.Id}", TraceStatus.Info);

        if (plan.Behavior is not null)
        {
            return ExecuteBehavior(world, actorId, plan, context, root);
        }

        if (plan.Primitive is not null)
        {
            return ExecutePrimitive(world, actorId, plan, context, callDepth, root);
        }

        foreach (var step in plan.Steps)
        {
            var stepTrace = new TraceNode($"Step {step.Label}", TraceStatus.Info);
            root.Add(stepTrace);

            var passed = EvaluateChecks(world, actorId, context, step, stepTrace);
            var effect = passed ? step.OnSuccess : step.OnFailure;

            if (effect is null)
            {
                stepTrace.Status = passed ? TraceStatus.Success : TraceStatus.Failure;
                continue;
            }

            var effectResult = ApplyEffect(world, actorId, context, effect, callDepth);
            stepTrace.Add(effectResult.Trace);
            stepTrace.Status = effectResult.Succeeded ? TraceStatus.Success : TraceStatus.Failure;

            if (effectResult.ConsumesTurn || !effectResult.ContinuePlan)
            {
                root.Status = effectResult.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
                return new PlanExecutionResult(
                    effectResult.Succeeded,
                    effectResult.ConsumesTurn,
                    effectResult.ContinuePlan,
                    root);
            }
        }

        root.Status = TraceStatus.Failure;
        root.Detail = "no step consumed or stopped the plan";
        return new PlanExecutionResult(false, ConsumesTurn: false, ContinuePlan: false, root);
    }

    private PlanExecutionResult ExecuteBehavior(
        WorldState world,
        EntityId actorId,
        ActionPlanDefinition plan,
        ActionPlanContext context,
        TraceNode root)
    {
        var steps = plan.Behavior!.Steps;

        if (steps.Count == 0)
        {
            root.Status = TraceStatus.Success;
            root.Detail = "behavior chain has no action steps";
            return new PlanExecutionResult(false, ConsumesTurn: false, ContinuePlan: false, root);
        }

        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            var stepTrace = new TraceNode($"Action Step {step.Kind}", TraceStatus.Info);
            root.Add(stepTrace);

            var stepResult = ApplyBehaviorStep(world, actorId, context, step);
            stepTrace.Add(stepResult.Trace);
            stepTrace.Status = stepResult.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
            stepTrace.Reason = stepResult.Trace.Reason;
            stepTrace.Detail = stepResult.Trace.Detail;

            if (stepResult.Succeeded)
            {
                if (stepResult.ConsumesTurn || !stepResult.ContinuePlan || index == steps.Count - 1)
                {
                    root.Status = TraceStatus.Success;
                    return new PlanExecutionResult(true, stepResult.ConsumesTurn, stepResult.ContinuePlan, root);
                }

                continue;
            }
        }

        root.Status = TraceStatus.Failure;
        root.Detail = "behavior chain exhausted without a successful action step";
        return new PlanExecutionResult(false, ConsumesTurn: true, ContinuePlan: false, root);
    }

    private PlanExecutionResult ExecutePrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanDefinition plan,
        ActionPlanContext context,
        int callDepth,
        TraceNode root)
    {
        var primitive = plan.Primitive!;
        var primitiveResult = ApplyPrimitive(world, actorId, context, primitive);
        root.Add(primitiveResult.Trace);

        if (primitiveResult.Succeeded)
        {
            root.Status = TraceStatus.Success;
            return new PlanExecutionResult(true, primitiveResult.ConsumesTurn, primitiveResult.ContinuePlan, root);
        }

        if (primitive.FallbackPlanId is not { } fallbackPlanId)
        {
            root.Status = TraceStatus.Failure;
            root.Detail = $"primitive {primitive.Kind} failed without fallback";
            return new PlanExecutionResult(false, ConsumesTurn: true, ContinuePlan: false, root);
        }

        var fallbackTrace = new TraceNode($"Fallback plan {fallbackPlanId}", TraceStatus.Info);
        root.Add(fallbackTrace);

        var fallbackResult = ApplyCallPlan(
            world,
            actorId,
            context,
            new CallPlanEffect(fallbackPlanId),
            callDepth);
        fallbackTrace.Add(fallbackResult.Trace);
        fallbackTrace.Status = fallbackResult.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
        root.Status = fallbackResult.Succeeded ? TraceStatus.Success : TraceStatus.Failure;

        return new PlanExecutionResult(
            fallbackResult.Succeeded,
            fallbackResult.ConsumesTurn,
            fallbackResult.ContinuePlan,
            root);
    }

    private PlanEffectResult ApplyPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        ActionPlanPrimitiveDescriptor primitive)
    {
        return primitive.Kind switch
        {
            ActionPlanPrimitiveKind.MoveFacing => ApplyMoveFacingPrimitive(world, actorId, context),
            ActionPlanPrimitiveKind.Backstep => ApplyBackstepPrimitive(world, actorId, context),
            ActionPlanPrimitiveKind.PickupTarget => ApplyPickupTargetPrimitive(world, actorId, context),
            ActionPlanPrimitiveKind.DropFacing => ApplyDropFacingPrimitive(world, actorId, context),
            ActionPlanPrimitiveKind.PushFacing => ApplyPushFacingPrimitive(world, actorId, context),
            ActionPlanPrimitiveKind.DestroyTarget => ApplyDestroyTargetPrimitive(world, actorId, context),
            ActionPlanPrimitiveKind.CreateFacing => ApplyCreateFacingPrimitive(world, actorId, context),
            ActionPlanPrimitiveKind.TurnLeft => ApplyTurnFacingPrimitive(context, ActionPlanPrimitiveKind.TurnLeft),
            ActionPlanPrimitiveKind.TurnRight => ApplyTurnFacingPrimitive(context, ActionPlanPrimitiveKind.TurnRight),
            ActionPlanPrimitiveKind.ReverseFacing => ApplyTurnFacingPrimitive(context, ActionPlanPrimitiveKind.ReverseFacing),
            _ => new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, TraceNode.Failure($"Primitive {primitive.Kind}", FailureReason.None, $"unsupported primitive {primitive.Kind}"))
        };
    }

    private PlanEffectResult ApplyBehaviorStep(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        ActionPlanBehaviorStepDescriptor step)
    {
        var primitive = step.Kind switch
        {
            ActionPlanBehaviorStepKind.MoveFacing => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing),
            ActionPlanBehaviorStepKind.Backstep => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.Backstep),
            ActionPlanBehaviorStepKind.PickupTarget => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PickupTarget),
            ActionPlanBehaviorStepKind.DropFacing => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.DropFacing),
            ActionPlanBehaviorStepKind.PushFacing => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PushFacing),
            ActionPlanBehaviorStepKind.DestroyTarget => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.DestroyTarget),
            ActionPlanBehaviorStepKind.CreateFacing => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.CreateFacing),
            ActionPlanBehaviorStepKind.TurnLeft => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.TurnLeft),
            ActionPlanBehaviorStepKind.TurnRight => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.TurnRight),
            ActionPlanBehaviorStepKind.ReverseFacing => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.ReverseFacing),
            ActionPlanBehaviorStepKind.AcquireNearestTarget => null,
            ActionPlanBehaviorStepKind.SeekTarget => null,
            ActionPlanBehaviorStepKind.FleeTarget => null,
            ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo => null,
            ActionPlanBehaviorStepKind.StrafeClockwise => null,
            ActionPlanBehaviorStepKind.StrafeAnticlockwise => null,
            _ => throw new InvalidOperationException($"Unsupported behavior action step kind {step.Kind}.")
        };

        return step.Kind switch
        {
            ActionPlanBehaviorStepKind.AcquireNearestTarget => ApplyAcquireNearestTarget(world, actorId, context),
            ActionPlanBehaviorStepKind.SeekTarget => ApplySeekTarget(world, actorId, context),
            ActionPlanBehaviorStepKind.FleeTarget => ApplyFleeTarget(world, actorId, context),
            ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo => ApplyMaintainChebyshevDistanceTwo(world, actorId, context),
            ActionPlanBehaviorStepKind.StrafeClockwise => ApplyStrafeTarget(world, actorId, context, clockwise: true),
            ActionPlanBehaviorStepKind.StrafeAnticlockwise => ApplyStrafeTarget(world, actorId, context, clockwise: false),
            _ => ApplyPrimitive(world, actorId, context, primitive!)
        };
    }

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
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
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
            return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
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
            return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
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
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
    }

    private PlanEffectResult ApplyMoveFacingPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive MoveFacing", TraceStatus.Info);

        if (!context.TryRead<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        IActionIntent action = new MoveAction(facing.Value);
        var resolution = action.Resolve(world, actorId, _movement);
        trace.Add(resolution.Trace);

        if (resolution.Succeeded)
        {
            trace.Status = TraceStatus.Success;
            trace.Detail = resolution.Trace.Detail;
            return new PlanEffectResult(true, resolution.ConsumesTurn, resolution.ContinuePlan, trace);
        }

        if (_movement.GetBlockingEntity(world, actorId, facing.Value) is { } blocker)
        {
            trace.Add(context.Set(ActionPlanSlot.Target, new EntityPlanValue(blocker)));
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = resolution.Trace.Reason;
        trace.Detail = resolution.Trace.Detail;
        return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
    }

    private PlanEffectResult ApplyBackstepPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive Backstep", TraceStatus.Info);

        if (!context.TryRead<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        var movementDirection = Reverse(facing.Value);
        IActionIntent action = new MoveAction(movementDirection);
        var resolution = action.Resolve(world, actorId, _movement);
        trace.Add(resolution.Trace);

        if (resolution.Succeeded)
        {
            trace.Add(TraceNode.Success("Preserve Facing", facing.Value.ToString()));
            trace.Status = TraceStatus.Success;
            trace.Detail = $"moved {movementDirection}; preserved Facing={facing.Value}";
            return new PlanEffectResult(true, resolution.ConsumesTurn, resolution.ContinuePlan, trace);
        }

        if (_movement.GetBlockingEntity(world, actorId, movementDirection) is { } blocker)
        {
            trace.Add(context.Set(ActionPlanSlot.Target, new EntityPlanValue(blocker)));
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = resolution.Trace.Reason;
        trace.Detail = resolution.Trace.Detail;
        return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
    }

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

        ActionResolution? lastFailure = null;
        for (var y = 0; y < inventoryPlane.Height; y++)
        {
            for (var x = 0; x < inventoryPlane.Width; x++)
            {
                var destination = new PlaneCoord(inventoryPlaneId, new GridCoord(x, y));
                IActionIntent action = new PickupAction(target.Value, destination);
                var result = action.Resolve(world, actorId, _movement);
                trace.Add(result.Trace);

                if (result.Succeeded)
                {
                    trace.Status = TraceStatus.Success;
                    trace.Detail = $"picked up {target.Value} into first available inventory coordinate {destination.Coord}";
                    return new PlanEffectResult(true, result.ConsumesTurn, result.ContinuePlan, trace);
                }

                lastFailure = result;
            }
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = lastFailure?.Trace.Reason ?? FailureReason.InvalidPlacement;
        trace.Detail = $"no inventory coordinate can accept {target.Value}";
        if (!string.IsNullOrWhiteSpace(lastFailure?.Trace.Detail))
        {
            trace.Detail += $"; last failure: {lastFailure.Trace.Detail}";
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
        var evaluation = _movement.EvaluateRelocation(world, carried.Value, destination);
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

    private PlanEffectResult ApplyPushFacingPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive PushFacing", TraceStatus.Info);
        if (!context.TryRead<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        var blocker = _movement.GetBlockingEntity(world, actorId, facing.Value);
        if (blocker is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"no blocking entity in {facing.Value}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var pushDestination = new MovementDestination.AdjacentMovementDestination(blocker.Value, facing.Value);
        var pushEvaluation = _movement.EvaluateRelocation(world, blocker.Value, pushDestination);
        trace.Add(pushEvaluation.Trace);
        if (!pushEvaluation.CanRelocate || pushEvaluation.Destination is not { } resolvedPushDestination)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = pushEvaluation.Trace.Reason;
            trace.Detail = pushEvaluation.Trace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        _movement.TryPlace(world, blocker.Value, resolvedPushDestination);
        _movement.TryMove(world, actorId, facing.Value);
        trace.Status = TraceStatus.Success;
        trace.Detail = $"pushed {blocker.Value} {facing.Value}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
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

    private PlanEffectResult ApplyCreateFacingPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive CreateFacing", TraceStatus.Info);
        if (!context.TryRead<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        if (!_movement.TryGetMoveDestination(world, actorId, facing.Value, out var destination) || !_movement.CanPlace(world, destination))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = $"cannot create placeholder entity at {destination}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var createdId = GeneratePlaceholderEntityId(world);
        var nodeId = world.GetNodeId(destination);
        world.Entities.Add(createdId, new Entity(createdId, "Placeholder Rock", nodeId, InventoryWidth: 0, InventoryHeight: 0, Weight: 3, CarryingCapacity: 3));
        world.Occupancy.Add(nodeId, createdId);
        trace.Status = TraceStatus.Success;
        trace.Detail = $"created {createdId} at {destination}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
    }

    private static PlanEffectResult ApplyTurnFacingPrimitive(
        ActionPlanContext context,
        ActionPlanPrimitiveKind kind)
    {
        var trace = new TraceNode($"Primitive {kind}", TraceStatus.Info);
        if (!context.TryRead<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        var turned = kind switch
        {
            ActionPlanPrimitiveKind.TurnLeft => TurnLeft(facing.Value),
            ActionPlanPrimitiveKind.TurnRight => TurnRight(facing.Value),
            ActionPlanPrimitiveKind.ReverseFacing => Reverse(facing.Value),
            _ => facing.Value
        };

        trace.Add(context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(turned)));
        trace.Status = TraceStatus.Success;
        trace.Detail = $"Facing {facing.Value} -> {turned}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
    }

    private static Direction TurnLeft(Direction direction) =>
        direction switch
        {
            Direction.North => Direction.West,
            Direction.West => Direction.South,
            Direction.South => Direction.East,
            Direction.East => Direction.North,
            _ => direction
        };

    private static Direction TurnRight(Direction direction) =>
        direction switch
        {
            Direction.North => Direction.East,
            Direction.East => Direction.South,
            Direction.South => Direction.West,
            Direction.West => Direction.North,
            _ => direction
        };

    private static Direction Reverse(Direction direction) =>
        direction switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East => Direction.West,
            Direction.West => Direction.East,
            _ => direction
        };

    private static Direction Clockwise(Direction direction) =>
        direction switch
        {
            Direction.North => Direction.East,
            Direction.East => Direction.South,
            Direction.South => Direction.West,
            Direction.West => Direction.North,
            _ => direction
        };

    private static Direction Anticlockwise(Direction direction) =>
        direction switch
        {
            Direction.North => Direction.West,
            Direction.West => Direction.South,
            Direction.South => Direction.East,
            Direction.East => Direction.North,
            _ => direction
        };

    private static int ManhattanDistance(GridCoord first, GridCoord second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);

    private static int ChebyshevDistance(GridCoord first, GridCoord second) =>
        Math.Max(Math.Abs(first.X - second.X), Math.Abs(first.Y - second.Y));

    private static IReadOnlyList<Direction> SeekDirections() =>
        [Direction.North, Direction.South, Direction.West, Direction.East];

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
            .Select(entry => (EntityId?)entry.Value)
            .FirstOrDefault();
    }

    private static EntityId GeneratePlaceholderEntityId(WorldState world)
    {
        var candidate = new EntityId("placeholderRock");
        var suffix = 2;
        while (world.Entities.ContainsKey(candidate))
        {
            candidate = new EntityId($"placeholderRock{suffix}");
            suffix++;
        }

        return candidate;
    }

    private PlanEffectResult ApplyEffect(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        IPlanEffect effect,
        int callDepth)
    {
        if (effect is CallPlanEffect call)
        {
            return ApplyCallPlan(world, actorId, context, call, callDepth);
        }

        return effect.Apply(world, actorId, context, _movement);
    }

    private PlanEffectResult ApplyCallPlan(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        CallPlanEffect call,
        int callDepth)
    {
        var trace = new TraceNode($"Call plan {call.PlanId}", TraceStatus.Info);

        if (callDepth >= _maxCallDepth)
        {
            trace.Status = TraceStatus.Failure;
            trace.Add(TraceNode.Failure("Plan call depth exceeded", FailureReason.None, $"max depth {_maxCallDepth}"));
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!_planRegistry.TryGetValue(call.PlanId, out var nestedPlan))
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"plan {call.PlanId} is not registered";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var nestedResult = Execute(world, actorId, nestedPlan, context, callDepth + 1);
        trace.Add(nestedResult.Trace);
        trace.Status = nestedResult.Succeeded ? TraceStatus.Success : TraceStatus.Failure;

        return new PlanEffectResult(
            nestedResult.Succeeded,
            nestedResult.ConsumesTurn,
            nestedResult.ContinuePlan,
            trace);
    }

    private bool EvaluateChecks(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        ActionPlanStep step,
        TraceNode stepTrace)
    {
        foreach (var check in step.Checks)
        {
            var result = check.Evaluate(world, actorId, context, _movement);
            stepTrace.Add(result.Trace);

            if (!result.Passed)
            {
                return false;
            }

            foreach (var (name, value) in result.VariableWrites)
            {
                stepTrace.Add(context.Set(name, value));
            }

            foreach (var (slot, value) in result.SlotWrites ?? new Dictionary<ActionPlanSlot, PlanValue>())
            {
                stepTrace.Add(context.Set(slot, value));
            }
        }

        return true;
    }
}
