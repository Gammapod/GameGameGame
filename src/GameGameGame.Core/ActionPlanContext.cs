namespace GameGameGame.Core;

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

    public TraceNode Set(string name, PlanValue value)
    {
        Variables[name] = value;

        return TraceNode.Success($"Set variable {name}", value.ToString());
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
}
