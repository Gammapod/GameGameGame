namespace GameGameGame.Core;

public enum ActionPlanSlot
{
    Facing,
    Target
}

public abstract record PlanValue;

public sealed record DirectionPlanValue(Direction Value) : PlanValue
{
    public override string ToString() => Value.ToString();
}

public sealed record EntityPlanValue(EntityId Value) : PlanValue
{
    public override string ToString() => Value.ToString();
}

public sealed record CoordPlanValue(GridCoord Value) : PlanValue
{
    public override string ToString() => Value.ToString();
}

public sealed record IntPlanValue(int Value) : PlanValue
{
    public override string ToString() => Value.ToString();
}

public sealed class ActionPlanContext
{
    public Dictionary<string, PlanValue> Variables { get; } = [];

    private readonly Dictionary<ActionPlanSlot, PlanValue> _slots = [];

    private WorldState? _attachedWorld;

    private EntityId? _attachedActorId;

    private int _activeTargetSlot = 1;

    private string? _activeTargetLabel;

    private bool _activeTargetSelf;

    public void UseTargetSlot(int targetSlot)
    {
        _activeTargetSlot = targetSlot <= 0 ? 1 : targetSlot;
        _activeTargetLabel = null;
        _activeTargetSelf = false;
    }

    public void UseTargetLabel(string targetLabel)
    {
        _activeTargetLabel = string.IsNullOrWhiteSpace(targetLabel) ? null : targetLabel;
        _activeTargetSelf = false;
    }

    public void UseSelfTarget()
    {
        _activeTargetSelf = true;
        _activeTargetLabel = null;
    }

    public void AttachEntityActionState(WorldState world, EntityId actorId)
    {
        _attachedWorld = world;
        _attachedActorId = actorId;
    }

    public TraceNode Set(string name, PlanValue value)
    {
        Variables[name] = value;

        return TraceNode.Success($"Set variable {name}", value.ToString());
    }

    public TraceNode Set(ActionPlanSlot slot, PlanValue value)
    {
        _slots[slot] = value;

        if (_attachedWorld is not null && _attachedActorId is { } actorId)
        {
            switch (slot, value)
            {
                case (ActionPlanSlot.Facing, DirectionPlanValue direction):
                    _attachedWorld.SetActionFacing(actorId, direction.Value);
                    break;
                case (ActionPlanSlot.Target, EntityPlanValue entity):
                    if (_activeTargetLabel is { } label)
                    {
                        _attachedWorld.SetActionTarget(actorId, label, entity.Value);
                    }
                    else
                    {
                        _attachedWorld.SetActionTarget(actorId, _activeTargetSlot, entity.Value);
                    }
                    break;
            }
        }

        return TraceNode.Success($"Set slot {slot}", value.ToString());
    }

    public bool TryGet<TValue>(string name, out TValue value)
        where TValue : PlanValue
    {
        if (Variables.TryGetValue(name, out var stored) && stored is TValue typed)
        {
            value = typed;
            return true;
        }

        value = null!;
        return false;
    }

    public bool TryGet<TValue>(ActionPlanSlot slot, out TValue value)
        where TValue : PlanValue
    {
        if (TryGetAttachedSlot(slot, out var attached) && attached is TValue attachedTyped)
        {
            value = attachedTyped;
            return true;
        }

        if (_slots.TryGetValue(slot, out var stored) && stored is TValue typed)
        {
            value = typed;
            return true;
        }

        value = null!;
        return false;
    }

    public bool TryRead<TValue>(ActionPlanSlot slot, out TValue value, out TraceNode trace)
        where TValue : PlanValue
    {
        trace = new TraceNode($"Read slot {slot}", TraceStatus.Info);

        if (!TryGetAttachedSlot(slot, out var stored) && !_slots.TryGetValue(slot, out stored))
        {
            value = null!;
            trace.Status = TraceStatus.Failure;
            trace.Detail = slot == ActionPlanSlot.Target && _activeTargetLabel is { } label
                ? $"missing target label {label}"
                : $"missing {slot} slot";
            return false;
        }

        if (stored is not TValue typed)
        {
            value = null!;
            trace.Status = TraceStatus.Failure;
            trace.Detail = $"expected {FormatValueKind<TValue>()}, actual {FormatValueKind(stored)}";
            return false;
        }

        value = typed;
        trace.Status = TraceStatus.Success;
        trace.Detail = typed.ToString();
        return true;
    }

    private bool TryGetAttachedSlot(ActionPlanSlot slot, out PlanValue value)
    {
        if (_attachedWorld is not null && _attachedActorId is { } actorId)
        {
            switch (slot)
            {
                case ActionPlanSlot.Facing when _attachedWorld.GetActionFacing(actorId) is { } facing:
                    value = new DirectionPlanValue(facing);
                    return true;
                case ActionPlanSlot.Target when _activeTargetLabel is { } label && _attachedWorld.GetActionTarget(actorId, label) is { } labeledTarget:
                    value = new EntityPlanValue(labeledTarget);
                    return true;
                case ActionPlanSlot.Target when _activeTargetSelf:
                    value = new EntityPlanValue(actorId);
                    return true;
                case ActionPlanSlot.Target when _activeTargetLabel is null && _attachedWorld.GetActionTarget(actorId, _activeTargetSlot) is { } target:
                    value = new EntityPlanValue(target);
                    return true;
            }
        }

        value = null!;
        return false;
    }

    private static string FormatValueKind<TValue>()
        where TValue : PlanValue =>
        typeof(TValue) == typeof(DirectionPlanValue) ? PlanValueKind.Direction.ToString() :
        typeof(TValue) == typeof(EntityPlanValue) ? PlanValueKind.Entity.ToString() :
        typeof(TValue) == typeof(CoordPlanValue) ? PlanValueKind.Coord.ToString() :
        typeof(TValue) == typeof(IntPlanValue) ? PlanValueKind.Int.ToString() :
        typeof(TValue).Name;

    private static string FormatValueKind(PlanValue value) =>
        value switch
        {
            DirectionPlanValue => PlanValueKind.Direction.ToString(),
            EntityPlanValue => PlanValueKind.Entity.ToString(),
            CoordPlanValue => PlanValueKind.Coord.ToString(),
            IntPlanValue => PlanValueKind.Int.ToString(),
            _ => value.GetType().Name
        };
}
