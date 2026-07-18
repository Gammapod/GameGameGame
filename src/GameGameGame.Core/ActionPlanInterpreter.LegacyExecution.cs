namespace GameGameGame.Core;

public sealed partial class ActionPlanInterpreter
{
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
                    root,
                    effectResult.ActorMovementDirection);
            }
        }

        root.Status = TraceStatus.Failure;
        root.Detail = "no step consumed or stopped the plan";
        return new PlanExecutionResult(false, ConsumesTurn: false, ContinuePlan: false, root);
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
            trace,
            nestedResult.ActorMovementDirection);
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
