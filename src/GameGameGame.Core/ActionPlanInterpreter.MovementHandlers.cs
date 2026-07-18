namespace GameGameGame.Core;

public sealed partial class ActionPlanInterpreter
{
    private PlanEffectResult ApplyMoveFacingPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive MoveFacing", TraceStatus.Info);

        if (!context.TryRead<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        IActionIntent action = new MoveAction(facing.Value);
        var resolution = action.Resolve(world, actorId, _movement);
        trace.Add(resolution.Trace);

        if (resolution.Succeeded)
        {
            trace.Status = TraceStatus.Success;
            trace.Detail = resolution.Trace.Detail;
            return new PlanEffectResult(true, resolution.ConsumesTurn, resolution.ContinuePlan, trace, resolution.ActorMovementDirection);
        }

        if (_movement.GetBlockingEntity(world, actorId, facing.Value) is { } blocker)
        {
            trace.Add(context.Set(ActionPlanSlot.Target, new EntityPlanValue(blocker)));
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = resolution.Trace.Reason;
        trace.Detail = resolution.Trace.Detail;
        return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
    }

    private PlanEffectResult ApplyBackstepPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive Backstep", TraceStatus.Info);

        if (!context.TryRead<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        var movementDirection = Reverse(facing.Value);
        IActionIntent action = new MoveAction(movementDirection);
        var resolution = action.Resolve(world, actorId, _movement);
        trace.Add(resolution.Trace);

        if (resolution.Succeeded)
        {
            trace.Add(TraceNode.Success("Preserve Facing", facing.Value.ToString()));
            trace.Status = TraceStatus.Success;
            trace.Detail = $"moved {movementDirection}; preserved Facing={facing.Value}";
            return new PlanEffectResult(true, resolution.ConsumesTurn, resolution.ContinuePlan, trace, resolution.ActorMovementDirection);
        }

        if (_movement.GetBlockingEntity(world, actorId, movementDirection) is { } blocker)
        {
            trace.Add(context.Set(ActionPlanSlot.Target, new EntityPlanValue(blocker)));
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = resolution.Trace.Reason;
        trace.Detail = resolution.Trace.Detail;
        return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
    }

    private PlanEffectResult ApplyCanonicalMove(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        ActionPlanBehaviorStepDescriptor step)
    {
        var trace = new TraceNode("Primitive Move", TraceStatus.Info);
        if (step.DirectionMode is not { } mode)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = "Move requires directionMode";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (!TryResolveMoveDirection(mode, context, out var direction, out var readTrace, out var failureDetail))
        {
            if (readTrace is not null)
            {
                trace.Add(readTrace);
            }

            trace.Status = TraceStatus.Failure;
            trace.Detail = failureDetail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        if (readTrace is not null)
        {
            trace.Add(readTrace);
        }

        trace.Add(TraceNode.Success("Resolve direction", $"mode={mode}; direction={direction}"));
        IActionIntent action = new MoveAction(direction);
        var resolution = action.Resolve(world, actorId, _movement);
        trace.Add(resolution.Trace);

        if (resolution.Succeeded)
        {
            world.SetActionFacing(actorId, direction);
            trace.Add(TraceNode.Success("Set Facing", direction.ToString()));
            trace.Status = TraceStatus.Success;
            trace.Detail = $"moved {direction}; Facing={direction}";
            return new PlanEffectResult(true, resolution.ConsumesTurn, resolution.ContinuePlan, trace, direction);
        }

        trace.Status = TraceStatus.Failure;
        trace.Reason = resolution.Trace.Reason;
        trace.Detail = resolution.Trace.Detail;
        return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
    }

    private PlanEffectResult ApplyPushFacingPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive PushFacing", TraceStatus.Info);
        if (!context.TryRead<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        var blocker = _movement.GetBlockingEntity(world, actorId, facing.Value);
        if (blocker is null)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"no blocking entity in {facing.Value}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var pushDestination = new MovementDestination.AdjacentMovementDestination(blocker.Value, facing.Value);
        var pushEvaluation = _movement.EvaluateRelocation(world, blocker.Value, pushDestination);
        trace.Add(pushEvaluation.Trace);
        if (!pushEvaluation.CanRelocate || pushEvaluation.Destination is not { } resolvedPushDestination)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = pushEvaluation.Trace.Reason;
            trace.Detail = pushEvaluation.Trace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        _movement.TryPlace(world, blocker.Value, resolvedPushDestination);
        _movement.TryMove(world, actorId, facing.Value);
        trace.Status = TraceStatus.Success;
        trace.Detail = $"pushed {blocker.Value} {facing.Value}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace, facing.Value);
    }

    private PlanEffectResult ApplyCreateFacingPrimitive(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context)
    {
        var trace = new TraceNode("Primitive CreateFacing", TraceStatus.Info);
        if (!context.TryRead<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        if (!_movement.TryGetMoveDestination(world, actorId, facing.Value, out var destination) || !_movement.CanPlace(world, destination))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.InvalidPlacement;
            trace.Detail = $"cannot create placeholder entity at {destination}";
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var createdId = GeneratePlaceholderEntityId(world);
        var nodeId = world.GetNodeId(destination);
        world.Entities.Add(createdId, new Entity(createdId, "Placeholder Rock", nodeId, InventoryWidth: 0, InventoryHeight: 0, Bulk: 3, Aperture: 3));
        world.Occupancy.Add(nodeId, createdId);
        trace.Status = TraceStatus.Success;
        trace.Detail = $"created {createdId} at {destination}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
    }

    private static PlanEffectResult ApplyTurnFacingPrimitive(
        ActionPlanContext context,
        ActionPlanPrimitiveKind kind)
    {
        var trace = new TraceNode($"Primitive {kind}", TraceStatus.Info);
        if (!context.TryRead<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing, out var readTrace))
        {
            trace.Add(readTrace);
            trace.Status = TraceStatus.Failure;
            trace.Detail = readTrace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        trace.Add(readTrace);
        var turned = kind switch
        {
            ActionPlanPrimitiveKind.TurnLeft => TurnLeft(facing.Value),
            ActionPlanPrimitiveKind.TurnRight => TurnRight(facing.Value),
            ActionPlanPrimitiveKind.ReverseFacing => Reverse(facing.Value),
            _ => facing.Value
        };

        trace.Add(context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(turned)));
        trace.Status = TraceStatus.Success;
        trace.Detail = $"Facing {facing.Value} -> {turned}";
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
    }
}
