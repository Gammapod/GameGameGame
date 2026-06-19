namespace GameGameGame.Core;

public enum ActionStepAuthoringTier
{
    Stable,
    Advanced,
    Legacy
}

public sealed record ActionStepDescriptor(
    ActionPlanBehaviorStepKind Kind,
    string DisplayName,
    string Description,
    IReadOnlyList<PlanPrimitiveSlotDescriptor>? RequiredState = null,
    IReadOnlyList<PlanPrimitiveSlotDescriptor>? DefaultableState = null,
    IReadOnlyList<PlanPrimitiveSlotDescriptor>? StateWrites = null,
    ActionStepAuthoringTier Tier = ActionStepAuthoringTier.Stable)
{
    public IReadOnlyList<PlanPrimitiveSlotDescriptor> RequiredState { get; } = RequiredState ?? [];

    public IReadOnlyList<PlanPrimitiveSlotDescriptor> DefaultableState { get; } = DefaultableState ?? [];

    public IReadOnlyList<PlanPrimitiveSlotDescriptor> StateWrites { get; } = StateWrites ?? [];
}

public static class ActionStepCatalog
{
    public static IReadOnlyList<ActionStepDescriptor> Steps { get; } =
    [
        new(
            ActionPlanBehaviorStepKind.MoveFacing,
            "Move Facing",
            "Attempts to move the actor one tile in its persistent Facing direction; when blocked by an entity, writes that entity to Target and falls through to the next Action Step.",
            RequiredState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            DefaultableState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            StateWrites: [State(ActionPlanSlot.Target, PlanValueKind.Entity)]),
        new(
            ActionPlanBehaviorStepKind.PickupTarget,
            "Pickup Target",
            "Attempts to pick up the persistent Target into the first canonical inventory coordinate; when pickup fails, falls through to the next Action Step.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)])
    ];

    public static ActionStepDescriptor Get(ActionPlanBehaviorStepKind kind) =>
        Steps.Single(step => step.Kind == kind);

    private static PlanPrimitiveSlotDescriptor State(ActionPlanSlot slot, PlanValueKind valueKind) =>
        new(slot, valueKind);
}
