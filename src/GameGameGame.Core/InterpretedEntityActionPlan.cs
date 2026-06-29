namespace GameGameGame.Core;

public sealed class InterpretedEntityActionPlan(
    ActionPlanDefinition plan,
    ActionPlanContext context,
    IReadOnlyDictionary<ActionPlanId, ActionPlanDefinition> planRegistry) : IEntityActionPlan
{
    public PlannedActionPlan PlanTurn(WorldState world, EntityId entityId, MovementService movement) =>
        PlannedActionPlan.Single(new InterpretedPlanIntent(plan, context, planRegistry));
}

public sealed class InterpretedPlanIntent(
    ActionPlanDefinition plan,
    ActionPlanContext context,
    IReadOnlyDictionary<ActionPlanId, ActionPlanDefinition> planRegistry) : IActionIntent
{
    public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement) =>
        new(true, TraceNode.Success($"Interpreted plan {plan.Id} is available"));

    public void Execute(WorldState world, EntityId actorId, MovementService movement)
    {
    }

    public ActionResolution Resolve(WorldState world, EntityId actorId, MovementService movement)
    {
        var interpreter = new ActionPlanInterpreter(movement, planRegistry);
        var result = interpreter.Execute(world, actorId, plan, context);

        return new ActionResolution(
            result.Succeeded,
            result.ConsumesTurn,
            result.ContinuePlan,
            result.Trace,
            result.ActorMovementDirection);
    }
}
