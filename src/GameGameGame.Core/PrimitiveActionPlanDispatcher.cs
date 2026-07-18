namespace GameGameGame.Core;

public sealed partial class ActionPlanInterpreter
{
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
