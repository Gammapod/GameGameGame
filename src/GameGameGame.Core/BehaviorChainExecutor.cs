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
            if (!string.IsNullOrWhiteSpace(step.TargetLabel))
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
