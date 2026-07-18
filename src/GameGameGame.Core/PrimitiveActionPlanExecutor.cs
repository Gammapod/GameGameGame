namespace GameGameGame.Core;

public sealed partial class ActionPlanInterpreter
{
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
            return new PlanExecutionResult(true, primitiveResult.ConsumesTurn, primitiveResult.ContinuePlan, root, primitiveResult.ActorMovementDirection);
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
            root,
            fallbackResult.ActorMovementDirection);
    }

}
