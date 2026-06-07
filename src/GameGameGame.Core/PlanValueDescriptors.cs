namespace GameGameGame.Core;

public enum PlanValueKind
{
    Direction,
    Entity,
    Coord,
    Int
}

public sealed record PlanValueDescriptor(
    PlanValueKind Kind,
    Direction? DirectionValue = null,
    EntityId? EntityValue = null,
    GridCoord? CoordValue = null,
    int? IntValue = null)
{
    public static PlanValueDescriptor Direction(Direction value) =>
        new(PlanValueKind.Direction, DirectionValue: value);

    public static PlanValueDescriptor Entity(EntityId value) =>
        new(PlanValueKind.Entity, EntityValue: value);

    public static PlanValueDescriptor Coord(GridCoord value) =>
        new(PlanValueKind.Coord, CoordValue: value);

    public static PlanValueDescriptor Int(int value) =>
        new(PlanValueKind.Int, IntValue: value);

    public PlanValue Materialize() =>
        Kind switch
        {
            PlanValueKind.Direction => new DirectionPlanValue(DirectionValue ?? throw Missing(nameof(DirectionValue))),
            PlanValueKind.Entity => new EntityPlanValue(EntityValue ?? throw Missing(nameof(EntityValue))),
            PlanValueKind.Coord => new CoordPlanValue(CoordValue ?? throw Missing(nameof(CoordValue))),
            PlanValueKind.Int => new IntPlanValue(IntValue ?? throw Missing(nameof(IntValue))),
            _ => throw new InvalidOperationException($"Unsupported plan value kind {Kind}.")
        };

    private static InvalidOperationException Missing(string name) =>
        new($"Plan value {name} is required.");
}
