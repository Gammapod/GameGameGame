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
    ActionStepAuthoringTier Tier = ActionStepAuthoringTier.Stable,
    ActionPlanBehaviorStepKind? TargetCapability = null)
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
            "Attempts to pick up the persistent Target into the first available actor inventory coordinate in deterministic row-major order; when pickup fails, falls through to the next Action Step.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            TargetCapability: ActionPlanBehaviorStepKind.PickupTarget),
        new(
            ActionPlanBehaviorStepKind.DropFacing,
            "Drop Facing",
            "Drops the first carried entity from actor inventory onto the floor in the actor's persistent Facing direction.",
            RequiredState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            DefaultableState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)]),
        new(
            ActionPlanBehaviorStepKind.PushFacing,
            "Push Facing",
            "Attempts to push the blocking entity in the actor's persistent Facing direction, then moves the actor into the blocker original location; a successful push consumes the turn.",
            RequiredState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            DefaultableState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            TargetCapability: ActionPlanBehaviorStepKind.PushFacing),
        new(
            ActionPlanBehaviorStepKind.DestroyTarget,
            "Destroy Target",
            "Destroys the persistent Target entity recursively, including its inventory space and contained entities.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            TargetCapability: ActionPlanBehaviorStepKind.DestroyTarget),
        new(
            ActionPlanBehaviorStepKind.CreateFacing,
            "Create Facing",
            "Creates a placeholder rock-like entity on the floor in the actor's persistent Facing direction as a prototype for future spawning Action Steps.",
            RequiredState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            DefaultableState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)]),
        new(
            ActionPlanBehaviorStepKind.TurnLeft,
            "Turn Left",
            "Turns the actor's persistent Facing direction 90 degrees counter-clockwise without moving any entity.",
            RequiredState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            DefaultableState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            StateWrites: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            Tier: ActionStepAuthoringTier.Legacy),
        new(
            ActionPlanBehaviorStepKind.TurnRight,
            "Turn Right",
            "Turns the actor's persistent Facing direction 90 degrees clockwise without moving any entity.",
            RequiredState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            DefaultableState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            StateWrites: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            Tier: ActionStepAuthoringTier.Legacy),
        new(
            ActionPlanBehaviorStepKind.ReverseFacing,
            "Reverse Facing",
            "Turns the actor's persistent Facing direction to the opposite direction without moving any entity.",
            RequiredState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            DefaultableState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            StateWrites: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            Tier: ActionStepAuthoringTier.Legacy),
        new(
            ActionPlanBehaviorStepKind.Backstep,
            "Backstep",
            "Attempts to move the actor one tile opposite its persistent Facing direction while preserving Facing; when blocked by an entity, writes that entity to Target and falls through to the next Action Step.",
            RequiredState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            DefaultableState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            StateWrites: [State(ActionPlanSlot.Target, PlanValueKind.Entity)]),
        new(
            ActionPlanBehaviorStepKind.AcquireNearestTarget,
            "Acquire Nearest Target",
            "Selects the nearest same-plane entity other than self by Manhattan distance, breaking ties by row-major coordinate order, writes it to Target, and continues to the next Action Step.",
            StateWrites: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            Tier: ActionStepAuthoringTier.Legacy),
        new(
            ActionPlanBehaviorStepKind.SeekTarget,
            "Seek Target",
            "Reads the persistent Target and greedily moves one cardinal step that reduces Manhattan distance, breaking ties North, South, West, East; preserves Target on failure/contact.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)]),
        new(
            ActionPlanBehaviorStepKind.FleeTarget,
            "Flee Target",
            "Reads the persistent Target and greedily moves one cardinal step that increases Manhattan distance, breaking ties North, South, West, East; preserves Target on success/failure.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)]),
        new(
            ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo,
            "Maintain Chebyshev Distance Two",
            "Reads the persistent Target and moves one cardinal step toward Chebyshev distance 2, backing away when too close and closing when too far; falls through at exact distance 2 and preserves Target.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)]),
        new(
            ActionPlanBehaviorStepKind.StrafeClockwise,
            "Strafe Clockwise",
            "Reads the persistent Target, selects the same primary seek direction as SeekTarget, then attempts the clockwise perpendicular cardinal move; preserves Target on success/failure.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)]),
        new(
            ActionPlanBehaviorStepKind.StrafeAnticlockwise,
            "Strafe Anticlockwise",
            "Reads the persistent Target, selects the same primary seek direction as SeekTarget, then attempts the anticlockwise perpendicular cardinal move; preserves Target on success/failure.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)]),
        new(
            ActionPlanBehaviorStepKind.GiveTarget,
            "Give Target",
            "Transfers the first carried entity from actor inventory into the persistent Target inventory using deterministic row-major source and destination order; falls through when transfer cannot be completed.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            TargetCapability: ActionPlanBehaviorStepKind.GiveTarget),
        new(
            ActionPlanBehaviorStepKind.TakeTarget,
            "Take Target",
            "Transfers the first carried entity from the persistent Target inventory into actor inventory using deterministic row-major source and destination order; falls through when transfer cannot be completed.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            TargetCapability: ActionPlanBehaviorStepKind.TakeTarget),
        new(
            ActionPlanBehaviorStepKind.EnterTarget,
            "Enter Target",
            "Moves the actor into the persistent Target inventory using deterministic row-major destination order; falls through when target adjacency, inventory space, or aperture checks fail.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            TargetCapability: ActionPlanBehaviorStepKind.EnterTarget),
        new(
            ActionPlanBehaviorStepKind.ExitFacing,
            "Exit Facing",
            "Moves the actor out of its containing entity inventory to the cell adjacent to the container in the actor's persistent Facing direction; falls through when no containing inventory, destination, or aperture check allows exit.",
            RequiredState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            DefaultableState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)]),
        new(
            ActionPlanBehaviorStepKind.ApplyPrePlan,
            "Apply Pre-Plan",
            "Reads the persistent Target and applies the referenced Action Plan as that target's one-turn Pre override, replacing any existing Pre override on the target.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)]),
        new(
            ActionPlanBehaviorStepKind.ApplyMainPlan,
            "Apply Main Plan",
            "Reads the persistent Target and applies the referenced Action Plan as that target's one-turn Main override, replacing the target's default main plan for its next turn.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)]),
        new(
            ActionPlanBehaviorStepKind.ApplyPostPlan,
            "Apply Post-Plan",
            "Reads the persistent Target and applies the referenced Action Plan as that target's one-turn Post override, replacing any existing Post override on the target.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)])
    ];

    public static ActionStepDescriptor Get(ActionPlanBehaviorStepKind kind) =>
        Steps.Single(step => step.Kind == kind);

    public static bool IsStableAuthoringStep(ActionPlanBehaviorStepKind kind) =>
        Get(kind).Tier == ActionStepAuthoringTier.Stable;

    private static PlanPrimitiveSlotDescriptor State(ActionPlanSlot slot, PlanValueKind valueKind) =>
        new(slot, valueKind);
}
