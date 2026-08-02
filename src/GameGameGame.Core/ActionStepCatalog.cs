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
    ActionPlanBehaviorStepKind? TargetCapability = null,
    string CostFieldDescription = "Optional costs paid from actor inventory recursively by runtime template ID; consumed only after this step succeeds, while missing cost causes normal fallthrough.",
    IReadOnlyList<ActionStepFieldDescriptor>? Fields = null)
{
    public IReadOnlyList<PlanPrimitiveSlotDescriptor> RequiredState { get; } = RequiredState ?? [];

    public IReadOnlyList<PlanPrimitiveSlotDescriptor> DefaultableState { get; } = DefaultableState ?? [];

    public IReadOnlyList<PlanPrimitiveSlotDescriptor> StateWrites { get; } = StateWrites ?? [];

    public IReadOnlyList<ActionStepFieldDescriptor> Fields { get; } = Fields ?? [];
}

public sealed record ActionStepFieldDescriptor(
    string Name,
    string Description,
    bool IsRequired = true);

public static class ActionStepCatalog
{
    public static IReadOnlyList<ActionStepDescriptor> Steps { get; } =
    [
        new(
            ActionPlanBehaviorStepKind.Move,
            "Move",
            "Canonical adjacent movement. Resolves directionMode, attempts one 8-way adjacent move, sets Facing to the actual moved direction on success, and does not write Target on failure.",
            RequiredState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            DefaultableState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)]),
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
            "Compatibility name for TransformAdjacentToInventory. Attempts to pick up the persistent Target into the first available actor inventory coordinate in deterministic row-major order; when pickup fails, falls through to the next Action Step.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            TargetCapability: ActionPlanBehaviorStepKind.PickupTarget),
        new(
            ActionPlanBehaviorStepKind.TransformAdjacentToInventory,
            "Transform Adjacent To Inventory",
            "Preferred canonical name for pickup semantics: transforms an adjacent non-actor entity from the actor's current plane into the actor inventory using deterministic row-major destination selection.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            TargetCapability: ActionPlanBehaviorStepKind.TransformAdjacentToInventory),
        new(
            ActionPlanBehaviorStepKind.DropFacing,
            "Drop Facing",
            "Compatibility name for TransformInventoryToAdjacent. Drops the first carried entity from actor inventory onto the floor in the actor's persistent Facing direction.",
            RequiredState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            DefaultableState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)]),
        new(
            ActionPlanBehaviorStepKind.TransformInventoryToAdjacent,
            "Transform Inventory To Adjacent",
            "Preferred canonical name for adjacent drop semantics: transforms the first carried entity from actor inventory to the adjacent map space in the actor's persistent Facing direction.",
            RequiredState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            DefaultableState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)]),
        new(
            ActionPlanBehaviorStepKind.Push,
            "Push",
            "Canonical forced target movement. Reads Target, resolves directionMode as the target-relative move direction, and moves only the target when the target bulk fits actor aperture and the target destination is legal/open.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            TargetCapability: ActionPlanBehaviorStepKind.Push,
            Fields:
            [
                Field("directionMode", "Required target-relative direction for the pushed entity to move.")
            ]),
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
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            Tier: ActionStepAuthoringTier.Legacy),
        new(
            ActionPlanBehaviorStepKind.FleeTarget,
            "Flee Target",
            "Reads the persistent Target and greedily moves one cardinal step that increases Manhattan distance, breaking ties North, South, West, East; preserves Target on success/failure.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            Tier: ActionStepAuthoringTier.Legacy),
        new(
            ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo,
            "Maintain Chebyshev Distance Two",
            "Reads the persistent Target and moves one cardinal step toward Chebyshev distance 2, backing away when too close and closing when too far; falls through at exact distance 2 and preserves Target.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            Tier: ActionStepAuthoringTier.Legacy),
        new(
            ActionPlanBehaviorStepKind.StrafeClockwise,
            "Strafe Clockwise",
            "Reads the persistent Target, selects the same primary seek direction as SeekTarget, then attempts the clockwise perpendicular cardinal move; preserves Target on success/failure.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            Tier: ActionStepAuthoringTier.Legacy),
        new(
            ActionPlanBehaviorStepKind.StrafeAnticlockwise,
            "Strafe Anticlockwise",
            "Reads the persistent Target, selects the same primary seek direction as SeekTarget, then attempts the anticlockwise perpendicular cardinal move; preserves Target on success/failure.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            Tier: ActionStepAuthoringTier.Legacy),
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
            ActionPlanBehaviorStepKind.Transfer,
            "Transfer",
            "Canonical peer inventory transfer. Reads Target as the moving entity, resolves directionMode as the adjacent counterparty direction, and moves the selected entity ActorToTarget or TargetToActor according to transferDirection while respecting the other entity's inventory policy.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity), State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity), State(ActionPlanSlot.Facing, PlanValueKind.Direction)]),
        new(
            ActionPlanBehaviorStepKind.CreateEntity,
            "Create Entity",
            "Creates a new runtime entity from an authored template. Defaults to the first open adjacent cell and can also create in a resolved facing direction when create placement is Facing.",
            RequiredState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)],
            DefaultableState: [State(ActionPlanSlot.Facing, PlanValueKind.Direction)]),
        new(
            ActionPlanBehaviorStepKind.PolymorphTarget,
            "Polymorph Target",
            "Reads the persistent Target and changes that entity to another authored template while preserving runtime identity, facing, inventory dimensions, current inventory, and target state.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)]),
        new(
            ActionPlanBehaviorStepKind.TargetPathMove,
            "Target Path Move",
            "Canonical target-aware path movement. Reads Target and moves one step using pathMode relative to legal adjacent spaces around that target; future runtime support will pathfind for seek, flee, maintain-distance, and orbit modes.",
            RequiredState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            DefaultableState: [State(ActionPlanSlot.Target, PlanValueKind.Entity)],
            Fields:
            [
                Field("pathMode", "Required target-path mode: SeekAdjacency, FleeAdjacency, MaintainDistance, or Orbit."),
                Field("desiredDistance", "Optional non-negative graph distance to target adjacency; required by MaintainDistance and Orbit.", IsRequired: false),
                Field("orbitDirection", "Optional orbit direction; required by Orbit and rejected by non-orbit modes.", IsRequired: false)
            ]),
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
        Get(kind).Tier == ActionStepAuthoringTier.Stable || IsTargetMovementCompatibilityStep(kind);

    private static bool IsTargetMovementCompatibilityStep(ActionPlanBehaviorStepKind kind) =>
        kind is ActionPlanBehaviorStepKind.SeekTarget
            or ActionPlanBehaviorStepKind.FleeTarget
            or ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo
            or ActionPlanBehaviorStepKind.StrafeClockwise
            or ActionPlanBehaviorStepKind.StrafeAnticlockwise;

    private static PlanPrimitiveSlotDescriptor State(ActionPlanSlot slot, PlanValueKind valueKind) =>
        new(slot, valueKind);

    private static ActionStepFieldDescriptor Field(string name, string description, bool IsRequired = true) =>
        new(name, description, IsRequired);
}
