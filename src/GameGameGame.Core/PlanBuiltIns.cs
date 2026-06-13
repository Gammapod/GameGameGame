namespace GameGameGame.Core;

public sealed class CanMoveCheck : IPlanCheck
{
    public CanMoveCheck()
    {
        DirectionSlot = ActionPlanSlot.Facing;
        Direction = new PlanVariableRef<DirectionPlanValue>("facing");
    }

    public CanMoveCheck(string directionVariableName)
        : this(new PlanVariableRef<DirectionPlanValue>(directionVariableName))
    {
    }

    public CanMoveCheck(PlanVariableRef<DirectionPlanValue> direction)
    {
        Direction = direction;
    }

    public PlanVariableRef<DirectionPlanValue> Direction { get; }

    public ActionPlanSlot? DirectionSlot { get; }

    public PlanCheckResult Evaluate(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
    {
        var label = DirectionSlot?.ToString() ?? Direction.Name;
        var trace = new TraceNode($"Can move {label}", TraceStatus.Info);

        if (!TryReadDirection(context, DirectionSlot, Direction, trace, out var direction))
        {
            return new PlanCheckResult(false, new Dictionary<string, PlanValue>(), trace);
        }

        var actionEvaluation = new MoveAction(direction).Evaluate(world, actorId, movement);
        trace.Add(actionEvaluation.Trace);
        trace.Status = actionEvaluation.CanExecute ? TraceStatus.Success : TraceStatus.Failure;
        trace.Reason = actionEvaluation.Trace.Reason;
        trace.Detail = actionEvaluation.Trace.Detail;

        return new PlanCheckResult(actionEvaluation.CanExecute, new Dictionary<string, PlanValue>(), trace);
    }

    internal static bool TryReadDirection(
        ActionPlanContext context,
        PlanVariableRef<DirectionPlanValue> variable,
        TraceNode trace,
        out Direction direction)
    {
        return TryReadDirection(context, directionSlot: null, variable, trace, out direction);
    }

    internal static bool TryReadDirection(
        ActionPlanContext context,
        ActionPlanSlot? directionSlot,
        PlanVariableRef<DirectionPlanValue>? variable,
        TraceNode trace,
        out Direction direction)
    {
        if (directionSlot is { } slot)
        {
            if (!context.TryRead<DirectionPlanValue>(slot, out var slotValue, out var readTrace))
            {
                direction = default;
                trace.Add(readTrace);
                trace.Status = TraceStatus.Failure;
                trace.Detail = readTrace.Detail;
                return false;
            }

            direction = slotValue.Value;
            trace.Add(readTrace);
            return true;
        }

        if (variable is null)
        {
            direction = default;
            trace.Status = TraceStatus.Failure;
            trace.Detail = "missing direction source";
            return false;
        }

        if (!variable.TryRead(context, out var value))
        {
            direction = default;
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"missing direction variable {variable.Name}";
            return false;
        }

        direction = value.Value;
        trace.Add(TraceNode.Success($"Read variable {variable.Name}", direction.ToString()));
        return true;
    }
}

public sealed class BlockingEntityCheck : IPlanCheck
{
    public BlockingEntityCheck()
    {
        DirectionSlot = ActionPlanSlot.Facing;
        TargetSlot = ActionPlanSlot.Target;
        Direction = new PlanVariableRef<DirectionPlanValue>("facing");
        Target = new PlanVariableRef<EntityPlanValue>("target");
    }

    public BlockingEntityCheck(string directionVariableName, string targetVariableName)
        : this(new PlanVariableRef<DirectionPlanValue>(directionVariableName), new PlanVariableRef<EntityPlanValue>(targetVariableName))
    {
    }

    public BlockingEntityCheck(PlanVariableRef<DirectionPlanValue> direction, PlanVariableRef<EntityPlanValue> target)
    {
        Direction = direction;
        Target = target;
    }

    public PlanVariableRef<DirectionPlanValue> Direction { get; }

    public PlanVariableRef<EntityPlanValue> Target { get; }

    public ActionPlanSlot? DirectionSlot { get; }

    public ActionPlanSlot? TargetSlot { get; }

    public PlanCheckResult Evaluate(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
    {
        var directionLabel = DirectionSlot?.ToString() ?? Direction.Name;
        var trace = new TraceNode($"Blocking entity {directionLabel}", TraceStatus.Info);

        if (!CanMoveCheck.TryReadDirection(context, DirectionSlot, Direction, trace, out var direction))
        {
            return new PlanCheckResult(false, new Dictionary<string, PlanValue>(), trace);
        }

        var blocker = movement.GetBlockingEntity(world, actorId, direction);

        if (blocker is not { } targetId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"no entity blocks {direction}";
            return new PlanCheckResult(false, new Dictionary<string, PlanValue>(), trace);
        }

        trace.Status = TraceStatus.Success;
        if (TargetSlot is { } slot)
        {
            trace.Detail = $"{slot}={targetId}";
            return new PlanCheckResult(
                true,
                new Dictionary<string, PlanValue>(),
                trace,
                new Dictionary<ActionPlanSlot, PlanValue>
                {
                    [slot] = new EntityPlanValue(targetId)
                });
        }

        trace.Detail = $"{Target.Name}={targetId}";
        return new PlanCheckResult(
            true,
            new Dictionary<string, PlanValue>
            {
                [Target.Name] = new EntityPlanValue(targetId)
            },
            trace);
    }
}

public sealed class CanPickupCheck : IPlanCheck
{
    public CanPickupCheck(GridCoord inventoryCoord)
        : this(new LiteralCoordValueSource(inventoryCoord))
    {
    }

    public CanPickupCheck(LiteralCoordValueSource inventoryCoord)
    {
        TargetSlot = ActionPlanSlot.Target;
        Target = new PlanVariableRef<EntityPlanValue>("target");
        InventoryCoord = inventoryCoord;
    }

    public CanPickupCheck(string targetVariableName, GridCoord inventoryCoord)
        : this(new PlanVariableRef<EntityPlanValue>(targetVariableName), new LiteralCoordValueSource(inventoryCoord))
    {
    }

    public CanPickupCheck(PlanVariableRef<EntityPlanValue> target, LiteralCoordValueSource inventoryCoord)
    {
        Target = target;
        InventoryCoord = inventoryCoord;
    }

    public PlanVariableRef<EntityPlanValue> Target { get; }

    public ActionPlanSlot? TargetSlot { get; }

    public LiteralCoordValueSource InventoryCoord { get; }

    public PlanCheckResult Evaluate(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
    {
        var label = TargetSlot?.ToString() ?? Target.Name;
        var trace = new TraceNode($"Can pickup {label}", TraceStatus.Info);

        if (!TryBuildPickupAction(world, actorId, context, TargetSlot, Target, InventoryCoord, trace, out var action))
        {
            return new PlanCheckResult(false, new Dictionary<string, PlanValue>(), trace);
        }

        var evaluation = action.Evaluate(world, actorId, movement);
        trace.Add(evaluation.Trace);
        trace.Status = evaluation.CanExecute ? TraceStatus.Success : TraceStatus.Failure;
        trace.Reason = evaluation.Trace.Reason;
        trace.Detail = evaluation.Trace.Detail;

        return new PlanCheckResult(evaluation.CanExecute, new Dictionary<string, PlanValue>(), trace);
    }

    internal static bool TryBuildPickupAction(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        PlanVariableRef<EntityPlanValue> targetVariable,
        LiteralCoordValueSource inventoryCoord,
        TraceNode trace,
        out PickupAction action)
    {
        return TryBuildPickupAction(world, actorId, context, targetSlot: null, targetVariable, inventoryCoord, trace, out action);
    }

    internal static bool TryBuildPickupAction(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        ActionPlanSlot? targetSlot,
        PlanVariableRef<EntityPlanValue>? targetVariable,
        LiteralCoordValueSource inventoryCoord,
        TraceNode trace,
        out PickupAction action)
    {
        action = default!;

        EntityPlanValue target;

        if (targetSlot is { } slot)
        {
            if (!context.TryRead<EntityPlanValue>(slot, out target, out var readTrace))
            {
                trace.Add(readTrace);
                trace.Status = TraceStatus.Failure;
                trace.Detail = readTrace.Detail;
                return false;
            }

            trace.Add(readTrace);
        }
        else
        {
            if (targetVariable is null)
            {
                trace.Status = TraceStatus.Failure;
                trace.Detail = "missing target source";
                return false;
            }

            if (!targetVariable.TryRead(context, out target))
            {
                trace.Status = TraceStatus.Failure;
                trace.Detail = $"missing entity variable {targetVariable.Name}";
                return false;
            }

            trace.Add(TraceNode.Success($"Read variable {targetVariable.Name}", target.Value.ToString()));
        }

        if (world.GetInventoryPlaneId(actorId) is not { } inventoryPlaneId)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ActorHasNoInventory;
            trace.Detail = $"actor {actorId} has no inventory plane";
            return false;
        }

        trace.Add(TraceNode.Success("Resolve inventory destination", new PlaneCoord(inventoryPlaneId, inventoryCoord.Value).ToString()));
        action = new PickupAction(target.Value, new PlaneCoord(inventoryPlaneId, inventoryCoord.Value));
        return true;
    }
}

public sealed class MoveEffect : IPlanEffect
{
    public MoveEffect()
    {
        DirectionSlot = ActionPlanSlot.Facing;
        Direction = new PlanVariableRef<DirectionPlanValue>("facing");
    }

    public MoveEffect(string directionVariableName)
        : this(new PlanVariableRef<DirectionPlanValue>(directionVariableName))
    {
    }

    public MoveEffect(PlanVariableRef<DirectionPlanValue> direction)
    {
        Direction = direction;
    }

    public PlanVariableRef<DirectionPlanValue> Direction { get; }

    public ActionPlanSlot? DirectionSlot { get; }

    public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
    {
        var label = DirectionSlot?.ToString() ?? Direction.Name;
        var trace = new TraceNode($"Move {label}", TraceStatus.Info);

        if (!CanMoveCheck.TryReadDirection(context, DirectionSlot, Direction, trace, out var direction))
        {
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        IActionIntent action = new MoveAction(direction);
        var resolution = action.Resolve(world, actorId, movement);
        trace.Add(resolution.Trace);
        trace.Status = resolution.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
        trace.Reason = resolution.Trace.Reason;
        trace.Detail = resolution.Trace.Detail;

        return new PlanEffectResult(resolution.Succeeded, resolution.ConsumesTurn, resolution.ContinuePlan, trace);
    }
}

public sealed class PickupEffect : IPlanEffect
{
    public PickupEffect(GridCoord inventoryCoord)
        : this(new LiteralCoordValueSource(inventoryCoord))
    {
    }

    public PickupEffect(LiteralCoordValueSource inventoryCoord)
    {
        TargetSlot = ActionPlanSlot.Target;
        Target = new PlanVariableRef<EntityPlanValue>("target");
        InventoryCoord = inventoryCoord;
    }

    public PickupEffect(string targetVariableName, GridCoord inventoryCoord)
        : this(new PlanVariableRef<EntityPlanValue>(targetVariableName), new LiteralCoordValueSource(inventoryCoord))
    {
    }

    public PickupEffect(PlanVariableRef<EntityPlanValue> target, LiteralCoordValueSource inventoryCoord)
    {
        Target = target;
        InventoryCoord = inventoryCoord;
    }

    public PlanVariableRef<EntityPlanValue> Target { get; }

    public ActionPlanSlot? TargetSlot { get; }

    public LiteralCoordValueSource InventoryCoord { get; }

    public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
    {
        var label = TargetSlot?.ToString() ?? Target.Name;
        var trace = new TraceNode($"Pickup {label}", TraceStatus.Info);

        if (!CanPickupCheck.TryBuildPickupAction(world, actorId, context, TargetSlot, Target, InventoryCoord, trace, out var action))
        {
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        IActionIntent intent = action;
        var resolution = intent.Resolve(world, actorId, movement);
        trace.Add(resolution.Trace);
        trace.Status = resolution.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
        trace.Reason = resolution.Trace.Reason;
        trace.Detail = resolution.Trace.Detail;

        return new PlanEffectResult(resolution.Succeeded, resolution.ConsumesTurn, resolution.ContinuePlan, trace);
    }
}

public sealed class TeleportEffect(
    MovementTargetDescriptor target,
    MovementDestinationDescriptor destination) : IPlanEffect
{
    public MovementTargetDescriptor Target { get; } = target;

    public MovementDestinationDescriptor Destination { get; } = destination;

    public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
    {
        var trace = new TraceNode($"Teleport {Target.Kind}", TraceStatus.Info, detail: $"destination={Destination.Kind}");

        if (!TryResolveTarget(world, actorId, context, Target, trace, out var targetId) ||
            !TryResolveDestination(world, actorId, context, Destination, trace, out var movementDestination))
        {
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var relocation = movement.EvaluateRelocation(world, targetId, movementDestination);
        trace.Add(relocation.Trace);

        if (!relocation.CanRelocate || relocation.Destination is not { } resolvedDestination)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = relocation.Trace.Reason;
            trace.Detail = relocation.Trace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        movement.TryPlace(world, targetId, resolvedDestination);
        trace.Status = TraceStatus.Success;
        return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
    }

    internal static bool TryResolveTarget(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        MovementTargetDescriptor target,
        TraceNode trace,
        out EntityId targetId)
    {
        switch (target.Kind)
        {
            case MovementTargetKind.Self:
                targetId = actorId;
                trace.Add(TraceNode.Success("Resolve target self", actorId.ToString()));
                return true;

            case MovementTargetKind.CanonicalTarget:
                if (context.TryRead<EntityPlanValue>(ActionPlanSlot.Target, out var value, out var readTrace))
                {
                    trace.Add(readTrace);
                    targetId = value.Value;
                    return true;
                }

                trace.Add(readTrace);
                targetId = default;
                trace.Status = TraceStatus.Failure;
                trace.Detail = readTrace.Detail;
                return false;

            case MovementTargetKind.Entity:
                if (target.EntityId is { } explicitEntityId)
                {
                    targetId = explicitEntityId;
                    trace.Add(TraceNode.Success("Resolve target entity", explicitEntityId.ToString()));
                    return true;
                }

                targetId = default;
                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.TargetMissing;
                trace.Detail = "movement target entity id is required";
                return false;

            case MovementTargetKind.CarriedInventoryCoord:
                if (target.InventoryCoord is not { } inventoryCoord)
                {
                    targetId = default;
                    trace.Status = TraceStatus.Failure;
                    trace.Reason = FailureReason.InvalidInventoryDestination;
                    trace.Detail = "movement target inventory coordinate is required";
                    return false;
                }

                if (world.GetInventoryPlaneId(actorId) is not { } inventoryPlaneId)
                {
                    targetId = default;
                    trace.Status = TraceStatus.Failure;
                    trace.Reason = FailureReason.ActorHasNoInventory;
                    trace.Detail = $"{actorId} has no usable inventory plane";
                    return false;
                }

                var source = new PlaneCoord(inventoryPlaneId, inventoryCoord);
                if (world.GetOccupant(source) is { } carriedEntityId)
                {
                    targetId = carriedEntityId;
                    trace.Add(TraceNode.Success("Resolve carried target", carriedEntityId.ToString()));
                    return true;
                }

                targetId = default;
                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.TargetNotInInventory;
                trace.Detail = $"no carried entity at {source}";
                return false;

            default:
                targetId = default;
                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.TargetMissing;
                trace.Detail = $"unsupported movement target {target.Kind}";
                return false;
        }
    }

    internal static bool TryResolveDestination(
        WorldState world,
        EntityId actorId,
        ActionPlanContext context,
        MovementDestinationDescriptor destination,
        TraceNode trace,
        out MovementDestination movementDestination)
    {
        switch (destination.Kind)
        {
            case MovementDestinationKind.PlaneCoord:
                if (destination.PlaneCoord is { } planeCoord)
                {
                    movementDestination = MovementDestination.Plane(planeCoord);
                    return true;
                }

                movementDestination = default!;
                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.InvalidPlacement;
                trace.Detail = "plane destination coordinate is required";
                return false;

            case MovementDestinationKind.InventorySlot:
                if (destination.OwnerId is { } ownerId && destination.InventoryCoord is { } inventoryCoord)
                {
                    movementDestination = MovementDestination.InventorySlot(ownerId, inventoryCoord);
                    return true;
                }

                movementDestination = default!;
                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.InvalidInventoryDestination;
                trace.Detail = "inventory destination owner and coordinate are required";
                return false;

            case MovementDestinationKind.AdjacentToSelf:
                if (destination.Direction is { } selfDirection)
                {
                    movementDestination = MovementDestination.AdjacentTo(actorId, selfDirection);
                    return true;
                }

                movementDestination = default!;
                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.MoveOutOfBounds;
                trace.Detail = "adjacent destination direction is required";
                return false;

            case MovementDestinationKind.AdjacentToEntity:
                if (destination.AnchorEntityId is { } anchorEntityId && destination.Direction is { } entityDirection)
                {
                    movementDestination = MovementDestination.AdjacentTo(anchorEntityId, entityDirection);
                    return true;
                }

                movementDestination = default!;
                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.TargetMissing;
                trace.Detail = "adjacent entity destination anchor and direction are required";
                return false;

            case MovementDestinationKind.AdjacentToCanonicalTarget:
                if (context.TryRead<EntityPlanValue>(ActionPlanSlot.Target, out var value, out var readTrace) &&
                    destination.Direction is { } targetDirection)
                {
                    trace.Add(readTrace);
                    movementDestination = MovementDestination.AdjacentTo(value.Value, targetDirection);
                    return true;
                }

                trace.Add(readTrace);
                movementDestination = default!;
                trace.Status = TraceStatus.Failure;
                trace.Detail = readTrace.Detail;
                return false;

            default:
                movementDestination = default!;
                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.InvalidPlacement;
                trace.Detail = $"unsupported movement destination {destination.Kind}";
                return false;
        }
    }
}

public sealed class DropEffect(
    MovementTargetDescriptor target,
    MovementDestinationDescriptor destination) : IPlanEffect
{
    public MovementTargetDescriptor Target { get; } = target;

    public MovementDestinationDescriptor Destination { get; } = destination;

    public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
    {
        var trace = new TraceNode($"Drop {Target.Kind}", TraceStatus.Info, detail: $"destination={Destination.Kind}");

        if (!TeleportEffect.TryResolveTarget(world, actorId, context, Target, trace, out var targetId) ||
            !TeleportEffect.TryResolveDestination(world, actorId, context, Destination, trace, out var movementDestination))
        {
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var relocation = movement.EvaluateRelocation(world, targetId, movementDestination);
        trace.Add(relocation.Trace);
        if (!relocation.CanRelocate || relocation.Destination is not { } resolvedDestination)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = relocation.Trace.Reason;
            trace.Detail = relocation.Trace.Detail;
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        IActionIntent action = new DropAction(targetId, resolvedDestination);
        var resolution = action.Resolve(world, actorId, movement);
        trace.Add(resolution.Trace);
        trace.Status = resolution.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
        trace.Reason = resolution.Trace.Reason;
        trace.Detail = resolution.Trace.Detail;

        return new PlanEffectResult(resolution.Succeeded, resolution.ConsumesTurn, resolution.ContinuePlan, trace);
    }
}

public sealed class ReverseDirectionEffect : IPlanEffect
{
    public ReverseDirectionEffect(bool consumesTurn, bool continuePlan)
    {
        DirectionSlot = ActionPlanSlot.Facing;
        Direction = new PlanVariableRef<DirectionPlanValue>("facing");
        _consumesTurn = consumesTurn;
        _continuePlan = continuePlan;
    }

    public ReverseDirectionEffect(string directionVariableName, bool consumesTurn, bool continuePlan)
        : this(new PlanVariableRef<DirectionPlanValue>(directionVariableName), consumesTurn, continuePlan)
    {
    }

    public ReverseDirectionEffect(PlanVariableRef<DirectionPlanValue> direction, bool consumesTurn, bool continuePlan)
    {
        Direction = direction;
        _consumesTurn = consumesTurn;
        _continuePlan = continuePlan;
    }

    private readonly bool _consumesTurn;

    private readonly bool _continuePlan;

    public PlanVariableRef<DirectionPlanValue> Direction { get; }

    public ActionPlanSlot? DirectionSlot { get; }

    public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
    {
        var label = DirectionSlot?.ToString() ?? Direction.Name;
        var trace = new TraceNode($"Reverse direction {label}", TraceStatus.Info);

        if (!CanMoveCheck.TryReadDirection(context, DirectionSlot, Direction, trace, out var direction))
        {
            return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
        }

        var reversed = direction switch
        {
            GameGameGame.Core.Direction.North => GameGameGame.Core.Direction.South,
            GameGameGame.Core.Direction.South => GameGameGame.Core.Direction.North,
            GameGameGame.Core.Direction.East => GameGameGame.Core.Direction.West,
            GameGameGame.Core.Direction.West => GameGameGame.Core.Direction.East,
            _ => direction
        };

        trace.Add(DirectionSlot is { } slot
            ? context.Set(slot, new DirectionPlanValue(reversed))
            : context.Set(Direction.Name, new DirectionPlanValue(reversed)));
        trace.Status = TraceStatus.Success;
        return new PlanEffectResult(true, _consumesTurn, _continuePlan, trace);
    }
}

public sealed class WaitEffect : IPlanEffect
{
    public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
    {
        IActionIntent action = new WaitAction();
        var resolution = action.Resolve(world, actorId, movement);

        return new PlanEffectResult(
            resolution.Succeeded,
            resolution.ConsumesTurn,
            resolution.ContinuePlan,
            resolution.Trace);
    }
}

public sealed class SetVariableEffect(
    string name,
    PlanValue value,
    bool consumesTurn,
    bool continuePlan) : IPlanEffect
{
    public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
    {
        var trace = new TraceNode($"Set variable effect {name}", TraceStatus.Success);
        trace.Add(context.Set(name, value));

        return new PlanEffectResult(true, consumesTurn, continuePlan, trace);
    }
}

public sealed record CallPlanEffect(ActionPlanId PlanId) : IPlanEffect
{
    public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
    {
        return new PlanEffectResult(
            false,
            ConsumesTurn: false,
            ContinuePlan: false,
            TraceNode.Failure($"Call plan {PlanId}", FailureReason.None, "call plan effects must be resolved by ActionPlanInterpreter"));
    }
}
