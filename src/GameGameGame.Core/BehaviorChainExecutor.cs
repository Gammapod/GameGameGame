namespace GameGameGame.Core;

public sealed partial class ActionPlanInterpreter
{
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
            if (step.TargetSelf)
            {
                context.UseSelfTarget();
            }
            else if (!string.IsNullOrWhiteSpace(step.TargetLabel))
            {
                context.UseTargetLabel(step.TargetLabel);
            }
            else
            {
                context.UseTargetSlot(step.TargetSlot ?? 1);
            }

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
                    return new PlanExecutionResult(true, stepResult.ConsumesTurn, stepResult.ContinuePlan, root, stepResult.ActorMovementDirection);
                }

                continue;
            }
        }

        root.Status = TraceStatus.Failure;
        root.Detail = "behavior chain exhausted without a successful action step";
        return new PlanExecutionResult(false, ConsumesTurn: true, ContinuePlan: false, root);
    }

}
