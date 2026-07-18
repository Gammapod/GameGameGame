namespace GameGameGame.Core;

public sealed partial class ActionPlanInterpreter
{
    private PlanEffectResult ApplyPlanOverride(
        WorldState world,
        ActionPlanContext context,
        ActionPlanBehaviorStepDescriptor step,
        ActionPlanOverrideSlot slot)
    {
        var trace = new TraceNode($"Primitive {step.Kind}", TraceStatus.Info);
        if (!context.TryRead<EntityPlanValue>(ActionPlanSlot.Target, out var target, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        if (!world.Entities.ContainsKey(target.Value))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"target {target.Value} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (step.PlanId is not { } planId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"{step.Kind} requires planId";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!_planRegistry.TryGetValue(planId, out var plan))
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"plan {planId} does not exist";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        world.SetActionPlanOverride(
            target.Value,
            slot,
            PlannedActionPlan.Single(new InterpretedPlanIntent(plan, new ActionPlanContext(), _planRegistry)));
        trace.Status = TraceStatus.Success;
        trace.Detail = $"applied {planId} as one-turn {slot} override for {target.Value}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
    }
}
