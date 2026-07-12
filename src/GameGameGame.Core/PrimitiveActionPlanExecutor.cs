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
}
