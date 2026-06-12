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
