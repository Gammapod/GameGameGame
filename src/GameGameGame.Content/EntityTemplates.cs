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
    IReadOnlyList<EntityTargetingRule>? TargetingRules = null);

public sealed record EntityTargetingRule(
    int Slot,
    EntityTemplateId TargetTemplateId,
    int Range,
    string? Hint = null,
    string? Label = null);

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
    EntityTemplateId PlayerEntityTemplateId,
    EntityId PlayerEntityId,
    GridCoord PlayerStart);

public sealed record CarriedEntityTemplate
{
    public CarriedEntityTemplate(
        EntityId EntityId,
        EntityTemplate Template,
        GridCoord Coord)
    {
        this.EntityId = EntityId;
        this.Template = Template;
        this.Coord = Coord;
    }

    public CarriedEntityTemplate(
        EntityId EntityId,
        EntityTemplateId TemplateId,
        GridCoord Coord)
    {
        this.EntityId = EntityId;
        this.TemplateId = TemplateId;
        this.Coord = Coord;
    }

    public EntityId EntityId { get; }

    public EntityTemplate? Template { get; }

    public EntityTemplateId? TemplateId { get; }

    public GridCoord Coord { get; }
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
