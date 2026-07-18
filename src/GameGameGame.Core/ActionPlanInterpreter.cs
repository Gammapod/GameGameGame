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
    TraceNode Trace,
    Direction? ActorMovementDirection = null);

public sealed record PlanExecutionResult(
    bool Succeeded,
    bool ConsumesTurn,
    bool ContinuePlan,
    TraceNode Trace,
    Direction? ActorMovementDirection = null);

public sealed partial class ActionPlanInterpreter
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
}
