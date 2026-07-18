namespace GameGameGame.Core;

public sealed partial class ActionPlanInterpreter
{
    private PlanEffectResult ApplyBehaviorStep(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        ActionPlanBehaviorStepDescriptor step)
    {
        var primitive = step.Kind switch
        {
            ActionPlanBehaviorStepKind.Move => null,
            ActionPlanBehaviorStepKind.MoveFacing => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing),
            ActionPlanBehaviorStepKind.Backstep => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.Backstep),
            ActionPlanBehaviorStepKind.PickupTarget => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PickupTarget),
            ActionPlanBehaviorStepKind.TransformAdjacentToInventory => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PickupTarget),
            ActionPlanBehaviorStepKind.DropFacing => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.DropFacing),
            ActionPlanBehaviorStepKind.TransformInventoryToAdjacent => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.DropFacing),
            ActionPlanBehaviorStepKind.PushFacing => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PushFacing),
            ActionPlanBehaviorStepKind.DestroyTarget => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.DestroyTarget),
            ActionPlanBehaviorStepKind.CreateFacing => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.CreateFacing),
            ActionPlanBehaviorStepKind.TurnLeft => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.TurnLeft),
            ActionPlanBehaviorStepKind.TurnRight => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.TurnRight),
            ActionPlanBehaviorStepKind.ReverseFacing => new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.ReverseFacing),
            ActionPlanBehaviorStepKind.AcquireNearestTarget => null,
            ActionPlanBehaviorStepKind.SeekTarget => null,
            ActionPlanBehaviorStepKind.FleeTarget => null,
            ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo => null,
            ActionPlanBehaviorStepKind.StrafeClockwise => null,
            ActionPlanBehaviorStepKind.StrafeAnticlockwise => null,
            ActionPlanBehaviorStepKind.GiveTarget => null,
            ActionPlanBehaviorStepKind.TakeTarget => null,
            ActionPlanBehaviorStepKind.EnterTarget => null,
            ActionPlanBehaviorStepKind.ExitFacing => null,
            ActionPlanBehaviorStepKind.ApplyPrePlan => null,
            ActionPlanBehaviorStepKind.ApplyMainPlan => null,
            ActionPlanBehaviorStepKind.ApplyPostPlan => null,
            _ => throw new InvalidOperationException($"Unsupported behavior action step kind {step.Kind}.")
        };

        return step.Kind switch
        {
            ActionPlanBehaviorStepKind.Move => ApplyCanonicalMove(world, actorId, context, step),
            ActionPlanBehaviorStepKind.AcquireNearestTarget => ApplyAcquireNearestTarget(world, actorId, context),
            ActionPlanBehaviorStepKind.SeekTarget => ApplySeekTarget(world, actorId, context),
            ActionPlanBehaviorStepKind.FleeTarget => ApplyFleeTarget(world, actorId, context),
            ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo => ApplyMaintainChebyshevDistanceTwo(world, actorId, context),
            ActionPlanBehaviorStepKind.StrafeClockwise => ApplyStrafeTarget(world, actorId, context, clockwise: true),
            ActionPlanBehaviorStepKind.StrafeAnticlockwise => ApplyStrafeTarget(world, actorId, context, clockwise: false),
            ActionPlanBehaviorStepKind.GiveTarget => ApplyGiveTargetPrimitive(world, actorId, context),
            ActionPlanBehaviorStepKind.TakeTarget => ApplyTakeTargetPrimitive(world, actorId, context),
            ActionPlanBehaviorStepKind.EnterTarget => ApplyEnterTargetPrimitive(world, actorId, context),
            ActionPlanBehaviorStepKind.ExitFacing => ApplyExitFacingPrimitive(world, actorId, context),
            ActionPlanBehaviorStepKind.ApplyPrePlan => ApplyPlanOverride(world, context, step, ActionPlanOverrideSlot.Pre),
            ActionPlanBehaviorStepKind.ApplyMainPlan => ApplyPlanOverride(world, context, step, ActionPlanOverrideSlot.Main),
            ActionPlanBehaviorStepKind.ApplyPostPlan => ApplyPlanOverride(world, context, step, ActionPlanOverrideSlot.Post),
            _ => ApplyPrimitive(world, actorId, context, primitive!)
        };
    }
}
