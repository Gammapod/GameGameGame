namespace GameGameGame.Core;

public enum PlanPrimitiveFieldKind
{
    VariableRead,
    VariableWrite,
    CoordLiteral,
    ActionPlanReference,
    PlanValueLiteral,
    BoolLiteral
}

public sealed record PlanPrimitiveFieldDescriptor(
    string Name,
    PlanPrimitiveFieldKind Kind,
    PlanValueKind? ValueKind = null,
    bool IsRequired = true);

public sealed record PlanCheckPrimitiveDescriptor(
    PlanCheckKind Kind,
    string DisplayName,
    IReadOnlyList<PlanPrimitiveFieldDescriptor> Fields);

public sealed record PlanEffectPrimitiveDescriptor(
    PlanEffectKind Kind,
    string DisplayName,
    IReadOnlyList<PlanPrimitiveFieldDescriptor> Fields);

public sealed record PlanValuePrimitiveDescriptor(
    PlanValueKind Kind,
    string DisplayName);

public static class PlanPrimitiveCatalog
{
    public static IReadOnlyList<PlanCheckPrimitiveDescriptor> Checks { get; } =
    [
        new(
            PlanCheckKind.CanMove,
            "Can Move",
            [
                VariableRead("directionVariable", PlanValueKind.Direction)
            ]),
        new(
            PlanCheckKind.BlockingEntity,
            "Blocking Entity",
            [
                VariableRead("directionVariable", PlanValueKind.Direction),
                VariableWrite("targetVariable", PlanValueKind.Entity)
            ]),
        new(
            PlanCheckKind.CanPickup,
            "Can Pickup",
            [
                VariableRead("targetVariable", PlanValueKind.Entity),
                CoordLiteral("inventoryCoord")
            ])
    ];

    public static IReadOnlyList<PlanEffectPrimitiveDescriptor> Effects { get; } =
    [
        new(
            PlanEffectKind.Move,
            "Move",
            [
                VariableRead("directionVariable", PlanValueKind.Direction)
            ]),
        new(
            PlanEffectKind.Pickup,
            "Pickup",
            [
                VariableRead("targetVariable", PlanValueKind.Entity),
                CoordLiteral("inventoryCoord")
            ]),
        new(
            PlanEffectKind.ReverseDirection,
            "Reverse Direction",
            [
                VariableRead("directionVariable", PlanValueKind.Direction),
                VariableWrite("directionVariable", PlanValueKind.Direction),
                BoolLiteral("consumesTurn"),
                BoolLiteral("continuePlan")
            ]),
        new(
            PlanEffectKind.Wait,
            "Wait",
            []),
        new(
            PlanEffectKind.SetVariable,
            "Set Variable",
            [
                VariableWrite("variableName", valueKind: null),
                new PlanPrimitiveFieldDescriptor("value", PlanPrimitiveFieldKind.PlanValueLiteral),
                BoolLiteral("consumesTurn"),
                BoolLiteral("continuePlan")
            ]),
        new(
            PlanEffectKind.CallPlan,
            "Call Plan",
            [
                new PlanPrimitiveFieldDescriptor("planId", PlanPrimitiveFieldKind.ActionPlanReference)
            ])
    ];

    public static IReadOnlyList<PlanValuePrimitiveDescriptor> ValueKinds { get; } =
    [
        new(PlanValueKind.Direction, "Direction"),
        new(PlanValueKind.Entity, "Entity"),
        new(PlanValueKind.Coord, "Coordinate"),
        new(PlanValueKind.Int, "Integer")
    ];

    public static PlanCheckPrimitiveDescriptor GetCheck(PlanCheckKind kind) =>
        Checks.Single(check => check.Kind == kind);

    public static PlanEffectPrimitiveDescriptor GetEffect(PlanEffectKind kind) =>
        Effects.Single(effect => effect.Kind == kind);

    private static PlanPrimitiveFieldDescriptor VariableRead(string name, PlanValueKind? valueKind) =>
        new(name, PlanPrimitiveFieldKind.VariableRead, valueKind);

    private static PlanPrimitiveFieldDescriptor VariableWrite(string name, PlanValueKind? valueKind) =>
        new(name, PlanPrimitiveFieldKind.VariableWrite, valueKind);

    private static PlanPrimitiveFieldDescriptor CoordLiteral(string name) =>
        new(name, PlanPrimitiveFieldKind.CoordLiteral);

    private static PlanPrimitiveFieldDescriptor BoolLiteral(string name) =>
        new(name, PlanPrimitiveFieldKind.BoolLiteral, IsRequired: false);
}
