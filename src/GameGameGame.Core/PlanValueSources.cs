namespace GameGameGame.Core;

public sealed record PlanVariableRef<TValue>(string Name)
    where TValue : PlanValue
{
    public bool TryRead(ActionPlanContext context, out TValue value) =>
        context.TryGet(Name, out value);

    public override string ToString() => Name;
}

public sealed record LiteralCoordValueSource(GridCoord Value)
{
    public override string ToString() => Value.ToString();
}
