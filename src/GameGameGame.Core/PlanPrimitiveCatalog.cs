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

public sealed record PlanPrimitiveSlotDescriptor(
    ActionPlanSlot Slot,
    PlanValueKind ValueKind);

public sealed record PlanCheckPrimitiveDescriptor(
    PlanCheckKind Kind,
    string DisplayName,
    IReadOnlyList<PlanPrimitiveFieldDescriptor> Fields,
    IReadOnlyList<PlanPrimitiveSlotDescriptor>? SlotReads = null,
    IReadOnlyList<PlanPrimitiveSlotDescriptor>? SlotWrites = null)
{
    public IReadOnlyList<PlanPrimitiveSlotDescriptor> SlotReads { get; } = SlotReads ?? [];

    public IReadOnlyList<PlanPrimitiveSlotDescriptor> SlotWrites { get; } = SlotWrites ?? [];
}

public sealed record PlanEffectPrimitiveDescriptor(
    PlanEffectKind Kind,
    string DisplayName,
    IReadOnlyList<PlanPrimitiveFieldDescriptor> Fields,
    IReadOnlyList<PlanPrimitiveSlotDescriptor>? SlotReads = null,
    IReadOnlyList<PlanPrimitiveSlotDescriptor>? SlotWrites = null)
{
    public IReadOnlyList<PlanPrimitiveSlotDescriptor> SlotReads { get; } = SlotReads ?? [];

    public IReadOnlyList<PlanPrimitiveSlotDescriptor> SlotWrites { get; } = SlotWrites ?? [];
}

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
            ],
            SlotReads: [SlotRead(ActionPlanSlot.Facing, PlanValueKind.Direction)]),
        new(
            PlanCheckKind.BlockingEntity,
            "Blocking Entity",
            [
                VariableRead("directionVariable", PlanValueKind.Direction),
                VariableWrite("targetVariable", PlanValueKind.Entity)
            ],
            SlotReads: [SlotRead(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            SlotWrites: [SlotWrite(ActionPlanSlot.Target, PlanValueKind.Entity)]),
        new(
            PlanCheckKind.CanPickup,
            "Can Pickup",
            [
                VariableRead("targetVariable", PlanValueKind.Entity),
                CoordLiteral("inventoryCoord")
            ],
            SlotReads: [SlotRead(ActionPlanSlot.Target, PlanValueKind.Entity)])
    ];

    public static IReadOnlyList<PlanEffectPrimitiveDescriptor> Effects { get; } =
    [
        new(
            PlanEffectKind.Move,
            "Move",
            [
                VariableRead("directionVariable", PlanValueKind.Direction)
            ],
            SlotReads: [SlotRead(ActionPlanSlot.Facing, PlanValueKind.Direction)]),
        new(
            PlanEffectKind.Pickup,
            "Pickup",
            [
                VariableRead("targetVariable", PlanValueKind.Entity),
                CoordLiteral("inventoryCoord")
            ],
            SlotReads: [SlotRead(ActionPlanSlot.Target, PlanValueKind.Entity)]),
        new(
            PlanEffectKind.ReverseDirection,
            "Reverse Direction",
            [
                VariableRead("directionVariable", PlanValueKind.Direction),
                VariableWrite("directionVariable", PlanValueKind.Direction),
                BoolLiteral("consumesTurn"),
                BoolLiteral("continuePlan")
            ],
            SlotReads: [SlotRead(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            SlotWrites: [SlotWrite(ActionPlanSlot.Facing, PlanValueKind.Direction)]),
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

    public static PlanCheckDescriptor CreateDefaultCheck(PlanCheckKind kind) =>
        kind switch
        {
            PlanCheckKind.CanMove => PlanCheckDescriptor.CanMove(),
            PlanCheckKind.BlockingEntity => PlanCheckDescriptor.BlockingEntity(),
            PlanCheckKind.CanPickup => PlanCheckDescriptor.CanPickup(new GridCoord(0, 0)),
            _ => throw new InvalidOperationException($"Unsupported plan check kind {kind}.")
        };

    public static PlanEffectDescriptor CreateDefaultEffect(PlanEffectKind kind) =>
        kind switch
        {
            PlanEffectKind.Move => PlanEffectDescriptor.Move(),
            PlanEffectKind.Pickup => PlanEffectDescriptor.Pickup(new GridCoord(0, 0)),
            PlanEffectKind.ReverseDirection => PlanEffectDescriptor.ReverseDirection(consumesTurn: false, continuePlan: false),
            PlanEffectKind.Wait => PlanEffectDescriptor.Wait(),
            PlanEffectKind.SetVariable => PlanEffectDescriptor.SetVariable("facing", new DirectionPlanValue(Direction.West), consumesTurn: false, continuePlan: false),
            PlanEffectKind.CallPlan => PlanEffectDescriptor.CallPlan(new ActionPlanId("wait")),
            _ => throw new InvalidOperationException($"Unsupported plan effect kind {kind}.")
        };

    private static PlanPrimitiveFieldDescriptor VariableRead(string name, PlanValueKind? valueKind) =>
        new(name, PlanPrimitiveFieldKind.VariableRead, valueKind);

    private static PlanPrimitiveFieldDescriptor VariableWrite(string name, PlanValueKind? valueKind) =>
        new(name, PlanPrimitiveFieldKind.VariableWrite, valueKind);

    private static PlanPrimitiveFieldDescriptor CoordLiteral(string name) =>
        new(name, PlanPrimitiveFieldKind.CoordLiteral);

    private static PlanPrimitiveFieldDescriptor BoolLiteral(string name) =>
        new(name, PlanPrimitiveFieldKind.BoolLiteral, IsRequired: false);

    private static PlanPrimitiveSlotDescriptor SlotRead(ActionPlanSlot slot, PlanValueKind valueKind) =>
        new(slot, valueKind);

    private static PlanPrimitiveSlotDescriptor SlotWrite(ActionPlanSlot slot, PlanValueKind valueKind) =>
        new(slot, valueKind);
}
