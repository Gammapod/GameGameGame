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
                root.Status = TraceStatus.Success;
                return new PlanExecutionResult(true, stepResult.ConsumesTurn, stepResult.ContinuePlan, root);
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
            ActionPlanPrimitiveKind.PickupTarget => ApplyPickupTargetPrimitive(world, actorId, context),
            ActionPlanPrimitiveKind.DropFacing => ApplyDropFacingPrimitive(world, actorId, context),
            ActionPlanPrimitiveKind.PushFacing => ApplyPushFacingPrimitive(world, actorId, context),
            ActionPlanPrimitiveKind.DestroyTarget => ApplyDestroyTargetPrimitive(world, actorId, context),
            ActionPlanPrimitiveKind.CreateFacing => ApplyCreateFacingPrimitive(world, actorId, context),
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
            ActionPlanBehaviorStepKind.PickupTarget => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PickupTarget),
            ActionPlanBehaviorStepKind.DropFacing => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.DropFacing),
            ActionPlanBehaviorStepKind.PushFacing => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PushFacing),
            ActionPlanBehaviorStepKind.DestroyTarget => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.DestroyTarget),
            ActionPlanBehaviorStepKind.CreateFacing => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.CreateFacing),
            _ => throw new InvalidOperationException($"Unsupported behavior action step kind {step.Kind}.")
        };

        return ApplyPrimitive(world, actorId, context, primitive);
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

        var effect = new PickupEffect(new GridCoord(0, 0));
        var result = effect.Apply(world, actorId, context, _movement);
        trace.Add(result.Trace);
        trace.Status = result.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
        trace.Reason = result.Trace.Reason;
        trace.Detail = result.Trace.Detail;

        return new PlanEffectResult(result.Succeeded, result.ConsumesTurn, result.ContinuePlan, trace);
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
