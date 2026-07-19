using GameGameGame.Core;

namespace GameGameGame.Content;

public readonly record struct EntityTemplateId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct ActionPlanTemplateId(string Value)
{
    public override string ToString() => Value;
}

public enum EntityController
{
    Computer,
    Player
}

public sealed record EntityTemplate(
    string Name,
    int InventoryWidth,
    int InventoryHeight,
    int Bulk,
    int Aperture,
    IReadOnlyList<CarriedEntityTemplate>? CarriedEntities = null,
    ActionPlanTemplateId? DefaultActionPlanId = null,
    IReadOnlyDictionary<string, PlanValueDescriptor>? DefaultPlanVariables = null,
    ActorActionStateDefaults? ActionStateDefaults = null,
    IReadOnlyList<EntityTargetingRule>? TargetingRules = null,
    EntityEnterPolicy? EnterPolicy = null,
    EntityExitPolicy? ExitPolicy = null)
{
    public EntityEnterPolicy EffectiveEnterPolicy => EnterPolicy ?? EntityEnterPolicy.FirstUnoccupiedRowMajor;

    public EntityExitPolicy EffectiveExitPolicy => ExitPolicy ?? EntityExitPolicy.AnyCell;
}

public sealed record EntityTargetingRule(
    int Slot,
    EntityTemplateId? TargetTemplateId,
    int Range,
    string? Hint = null,
    string? Label = null,
    IReadOnlyList<ActionPlanBehaviorStepKind>? TargetCapabilities = null)
{
    public IReadOnlyList<ActionPlanBehaviorStepKind> TargetCapabilities { get; } = TargetCapabilities ?? [];
}

public sealed record ActorActionStateDefaults(
    Direction? Facing = null,
    EntityId? Target = null);

public sealed record EntityPresentation(char Glyph, PresentationColor Color)
{
    public EntityInspectionAppearance ToInspectionAppearance() => new(Glyph, Color);
}

public sealed record ScenarioDefinition(
    string ScenarioId,
    string Name,
    EntityTemplateId ScenarioRootEntityTemplateId,
    EntityTemplateId? PlayerEntityTemplateId,
    EntityId? PlayerEntityId,
    GridCoord? PlayerStart,
    IReadOnlyDictionary<string, IReadOnlyList<EntityId>>? PlayerControls = null)
{
    public IReadOnlyDictionary<string, IReadOnlyList<EntityId>> PlayerControls { get; } = PlayerControls ?? new Dictionary<string, IReadOnlyList<EntityId>>();
}

public sealed record CarriedEntityTemplate
{
    public CarriedEntityTemplate(
        EntityId EntityId,
        EntityTemplate Template,
        GridCoord Coord,
        EntityController? Controller = null)
    {
        this.EntityId = EntityId;
        this.Template = Template;
        this.Coord = Coord;
        this.Controller = Controller;
    }

    public CarriedEntityTemplate(
        EntityId EntityId,
        EntityTemplateId TemplateId,
        GridCoord Coord,
        EntityController? Controller = null)
    {
        this.EntityId = EntityId;
        this.TemplateId = TemplateId;
        this.Coord = Coord;
        this.Controller = Controller;
    }

    public EntityId EntityId { get; }

    public EntityTemplate? Template { get; }

    public EntityTemplateId? TemplateId { get; }

    public GridCoord Coord { get; }

    public EntityController? Controller { get; }
}

public sealed record EntitySpawnOptions(
    EntityId EntityId,
    PlaneCoord Location,
    Func<EntityTemplate, EntityTemplate>? ModifyTemplate = null,
    PlaneId? InventoryPlaneId = null,
    string? InventoryPlaneName = null,
    ActionPlanTemplateId? ActionPlanOverrideId = null,
    IReadOnlyDictionary<string, PlanValueDescriptor>? PlanVariableOverrides = null,
    ActorActionStateDefaults? ActionStateOverrides = null);

public sealed record EntitySpawnResult(
    EntityId EntityId,
    IEntityActionPlan? ActionPlan,
    IReadOnlyDictionary<EntityId, IEntityActionPlan> ActionPlans);

public sealed record FirstSliceBuildResult(
    WorldState World,
    IReadOnlyDictionary<EntityId, IEntityActionPlan> ActionPlans,
    PrototypeContentRegistry Registry);
