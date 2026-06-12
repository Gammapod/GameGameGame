namespace GameGameGame.Core;

public readonly record struct ActionPlanId(string Value)
{
    public override string ToString() => Value;
}

public sealed record ActionPlanDefinition(
    ActionPlanId Id,
    IReadOnlyList<ActionPlanStep> Steps);

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
        var root = new TraceNode($"Plan {plan.Id}", TraceStatus.Info);

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
