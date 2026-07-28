namespace GameGameGame.Core;

public sealed record ActionPlanDescriptor(
    ActionPlanId Id,
    IReadOnlyList<ActionPlanStepDescriptor> Steps,
    ActionPlanPrimitiveDescriptor? Primitive = null,
    ActionPlanBehaviorDescriptor? Behavior = null)
{
    public ActionPlanDefinition Materialize() =>
        new(
            Id,
            Steps.Select(step => step.Materialize()).ToList(),
            Primitive,
            Behavior);
}

public enum ActionPlanShape
{
    EmptyPassive,
    CanonicalBehaviorChain,
    TransitionalPrimitivePlan,
    LegacyLowLevelSteps,
    InvalidMixedShape,
    InvalidEmptyBehaviorChain
}

public static class ActionPlanShapeClassifier
{
    public static ActionPlanShape Classify(ActionPlanDescriptor descriptor)
    {
        var hasBehavior = descriptor.Behavior?.Steps.Count > 0;
        var hasPrimitive = descriptor.Primitive is not null;
        var hasLowLevelSteps = descriptor.Steps.Count > 0;
        var activeShapes = 0;
        activeShapes += hasBehavior ? 1 : 0;
        activeShapes += hasPrimitive ? 1 : 0;
        activeShapes += hasLowLevelSteps ? 1 : 0;

        if (activeShapes > 1)
        {
            return ActionPlanShape.InvalidMixedShape;
        }

        if (descriptor.Behavior is { Steps.Count: 0 })
        {
            return ActionPlanShape.InvalidEmptyBehaviorChain;
        }

        if (hasBehavior)
        {
            return ActionPlanShape.CanonicalBehaviorChain;
        }

        if (hasPrimitive)
        {
            return ActionPlanShape.TransitionalPrimitivePlan;
        }

        return hasLowLevelSteps
            ? ActionPlanShape.LegacyLowLevelSteps
            : ActionPlanShape.EmptyPassive;
    }
}

public sealed record ActionPlanBehaviorDescriptor(
    IReadOnlyList<ActionPlanBehaviorStepDescriptor> Steps);

public enum ActionPlanBehaviorStepKind
{
    Move,
    MoveFacing,
    PickupTarget,
    DropFacing,
    PushFacing,
    DestroyTarget,
    CreateFacing,
    TurnLeft,
    TurnRight,
    ReverseFacing,
    Backstep,
    AcquireNearestTarget,
    SeekTarget,
    FleeTarget,
    MaintainChebyshevDistanceTwo,
    StrafeClockwise,
    StrafeAnticlockwise,
    GiveTarget,
    TakeTarget,
    EnterTarget,
    ExitFacing,
    ApplyPrePlan,
    ApplyMainPlan,
    ApplyPostPlan,
    TransformAdjacentToInventory,
    TransformInventoryToAdjacent,
    Transfer
}

public enum ActionPlanMoveDirectionMode
{
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest,
    Forward,
    ForwardRight,
    Right,
    BackRight,
    Back,
    BackLeft,
    Left,
    ForwardLeft
}

public sealed record ActionPlanBehaviorStepDescriptor(
    ActionPlanBehaviorStepKind Kind,
    int? TargetSlot = null,
    string? TargetLabel = null,
    bool TargetSelf = false,
    ActionPlanId? PlanId = null,
    ActionPlanMoveDirectionMode? DirectionMode = null,
    TransferDirection? TransferDirection = null);

public enum ActionPlanPrimitiveKind
{
    MoveFacing,
    PickupTarget,
    DropFacing,
    PushFacing,
    DestroyTarget,
    CreateFacing,
    TurnLeft,
    TurnRight,
    ReverseFacing,
    Backstep
}

public sealed record ActionPlanPrimitiveDescriptor(
    ActionPlanPrimitiveKind Kind,
    ActionPlanId? FallbackPlanId = null);

public sealed record ActionPlanStepDescriptor(
    string Label,
    IReadOnlyList<PlanCheckDescriptor> Checks,
    PlanEffectDescriptor? OnSuccess,
    PlanEffectDescriptor? OnFailure)
{
    public ActionPlanStep Materialize() =>
        new(
            Label,
            Checks.Select(check => check.Materialize()).ToList(),
            OnSuccess?.Materialize(),
            OnFailure?.Materialize());
}

public enum PlanCheckKind
{
    CanMove,
    BlockingEntity,
    CanPickup
}

public sealed record PlanCheckDescriptor(
    PlanCheckKind Kind,
    string? DirectionVariable = null,
    string? TargetVariable = null,
    GridCoord? InventoryCoord = null)
{
    public static PlanCheckDescriptor CanMove() =>
        new(PlanCheckKind.CanMove);

    public static PlanCheckDescriptor CanMove(string directionVariable) =>
        new(PlanCheckKind.CanMove, DirectionVariable: directionVariable);

    public static PlanCheckDescriptor BlockingEntity() =>
        new(PlanCheckKind.BlockingEntity);

    public static PlanCheckDescriptor BlockingEntity(string directionVariable, string targetVariable) =>
        new(PlanCheckKind.BlockingEntity, DirectionVariable: directionVariable, TargetVariable: targetVariable);

    public static PlanCheckDescriptor CanPickup(GridCoord inventoryCoord) =>
        new(PlanCheckKind.CanPickup, InventoryCoord: inventoryCoord);

    public static PlanCheckDescriptor CanPickup(string targetVariable, GridCoord inventoryCoord) =>
        new(PlanCheckKind.CanPickup, TargetVariable: targetVariable, InventoryCoord: inventoryCoord);

    public IPlanCheck Materialize() =>
        Kind switch
        {
            PlanCheckKind.CanMove => DirectionVariable is null
                ? new CanMoveCheck()
                : new CanMoveCheck(Required(DirectionVariable, nameof(DirectionVariable))),
            PlanCheckKind.BlockingEntity => DirectionVariable is null && TargetVariable is null
                ? new BlockingEntityCheck()
                : new BlockingEntityCheck(
                    Required(DirectionVariable, nameof(DirectionVariable)),
                    Required(TargetVariable, nameof(TargetVariable))),
            PlanCheckKind.CanPickup => TargetVariable is null
                ? new CanPickupCheck(Required(InventoryCoord, nameof(InventoryCoord)))
                : new CanPickupCheck(
                    Required(TargetVariable, nameof(TargetVariable)),
                    Required(InventoryCoord, nameof(InventoryCoord))),
            _ => throw new InvalidOperationException($"Unsupported plan check kind {Kind}.")
        };

    private static string Required(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"Plan check {name} is required.") : value;

    private static GridCoord Required(GridCoord? value, string name) =>
        value ?? throw new InvalidOperationException($"Plan check {name} is required.");
}

public enum PlanEffectKind
{
    Teleport,
    Move,
    Pickup,
    Drop,
    ReverseDirection,
    Wait,
    SetVariable,
    CallPlan
}

public sealed record PlanEffectDescriptor(
    PlanEffectKind Kind,
    string? DirectionVariable = null,
    string? TargetVariable = null,
    GridCoord? InventoryCoord = null,
    ActionPlanId? PlanId = null,
    string? VariableName = null,
    PlanValue? Value = null,
    MovementTargetDescriptor? MovementTarget = null,
    MovementDestinationDescriptor? MovementDestination = null,
    bool ConsumesTurn = false,
    bool ContinuePlan = false)
{
    public static PlanEffectDescriptor Teleport(MovementTargetDescriptor target, MovementDestinationDescriptor destination) =>
        new(PlanEffectKind.Teleport, MovementTarget: target, MovementDestination: destination);

    public static PlanEffectDescriptor Move() =>
        new(PlanEffectKind.Move);

    public static PlanEffectDescriptor Move(string directionVariable) =>
        new(PlanEffectKind.Move, DirectionVariable: directionVariable);

    public static PlanEffectDescriptor Pickup(GridCoord inventoryCoord) =>
        new(PlanEffectKind.Pickup, InventoryCoord: inventoryCoord);

    public static PlanEffectDescriptor Pickup(string targetVariable, GridCoord inventoryCoord) =>
        new(PlanEffectKind.Pickup, TargetVariable: targetVariable, InventoryCoord: inventoryCoord);

    public static PlanEffectDescriptor Drop(MovementTargetDescriptor target, MovementDestinationDescriptor destination) =>
        new(PlanEffectKind.Drop, MovementTarget: target, MovementDestination: destination);

    public static PlanEffectDescriptor ReverseDirection(bool consumesTurn, bool continuePlan) =>
        new(PlanEffectKind.ReverseDirection, ConsumesTurn: consumesTurn, ContinuePlan: continuePlan);

    public static PlanEffectDescriptor ReverseDirection(string directionVariable, bool consumesTurn, bool continuePlan) =>
        new(PlanEffectKind.ReverseDirection, DirectionVariable: directionVariable, ConsumesTurn: consumesTurn, ContinuePlan: continuePlan);

    public static PlanEffectDescriptor Wait() =>
        new(PlanEffectKind.Wait);

    public static PlanEffectDescriptor SetVariable(string variableName, PlanValue value, bool consumesTurn, bool continuePlan) =>
        new(PlanEffectKind.SetVariable, VariableName: variableName, Value: value, ConsumesTurn: consumesTurn, ContinuePlan: continuePlan);

    public static PlanEffectDescriptor CallPlan(ActionPlanId planId) =>
        new(PlanEffectKind.CallPlan, PlanId: planId);

    public IPlanEffect Materialize() =>
        Kind switch
        {
            PlanEffectKind.Teleport => new TeleportEffect(
                MovementTarget ?? throw new InvalidOperationException("Plan effect MovementTarget is required."),
                MovementDestination ?? throw new InvalidOperationException("Plan effect MovementDestination is required.")),
            PlanEffectKind.Move => DirectionVariable is null
                ? new MoveEffect()
                : new MoveEffect(Required(DirectionVariable, nameof(DirectionVariable))),
            PlanEffectKind.Pickup => TargetVariable is null
                ? new PickupEffect(Required(InventoryCoord, nameof(InventoryCoord)))
                : new PickupEffect(
                    Required(TargetVariable, nameof(TargetVariable)),
                    Required(InventoryCoord, nameof(InventoryCoord))),
            PlanEffectKind.Drop => new DropEffect(
                MovementTarget ?? throw new InvalidOperationException("Plan effect MovementTarget is required."),
                MovementDestination ?? throw new InvalidOperationException("Plan effect MovementDestination is required.")),
            PlanEffectKind.ReverseDirection => DirectionVariable is null
                ? new ReverseDirectionEffect(ConsumesTurn, ContinuePlan)
                : new ReverseDirectionEffect(
                    Required(DirectionVariable, nameof(DirectionVariable)),
                    ConsumesTurn,
                    ContinuePlan),
            PlanEffectKind.Wait => new WaitEffect(),
            PlanEffectKind.SetVariable => new SetVariableEffect(
                Required(VariableName, nameof(VariableName)),
                Value ?? throw new InvalidOperationException("Plan effect Value is required."),
                ConsumesTurn,
                ContinuePlan),
            PlanEffectKind.CallPlan => new CallPlanEffect(PlanId ?? throw new InvalidOperationException("Plan effect PlanId is required.")),
            _ => throw new InvalidOperationException($"Unsupported plan effect kind {Kind}.")
        };

    private static string Required(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"Plan effect {name} is required.") : value;

    private static GridCoord Required(GridCoord? value, string name) =>
        value ?? throw new InvalidOperationException($"Plan effect {name} is required.");
}
