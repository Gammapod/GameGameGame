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
    EntityTargetingProfile? Targeting = null,
    IReadOnlyList<EntityTargetingRule>? TargetingRules = null,
    EntityEnterPolicy? EnterPolicy = null,
    EntityExitPolicy? ExitPolicy = null,
    EntityTopologyPolicy TopologyPolicy = EntityTopologyPolicy.None,
    EntityMaterial? Material = null)
{
    public EntityEnterPolicy EffectiveEnterPolicy => EnterPolicy ?? EntityEnterPolicy.FirstUnoccupiedRowMajor;

    public EntityExitPolicy EffectiveExitPolicy => ExitPolicy ?? EntityExitPolicy.AnyCell;
}

public sealed record EntityTargetingRule(
    int Slot,
    EntityTemplateId? TargetTemplateId,
    int Range = 0,
    string? Hint = null,
    string? Label = null,
    IReadOnlyList<ActionPlanBehaviorStepKind>? TargetCapabilities = null,
    TargetingLocalityQuery? Locality = null)
{
    public IReadOnlyList<ActionPlanBehaviorStepKind> TargetCapabilities { get; } = TargetCapabilities ?? [];
}

public sealed record EntityTargetingProfile(
    int Range,
    TargetingLocalityQuery? DefaultLocality = null,
    IReadOnlyList<EntityTargetingRule>? Rules = null)
{
    public IReadOnlyList<EntityTargetingRule> Rules { get; } = Rules ?? [];
}

public sealed record ActorActionStateDefaults(
    Direction? Facing = null,
    EntityId? Target = null);

public readonly record struct PresentationId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct PaletteId(string Value)
{
    public override string ToString() => Value;
}

public sealed record EntityPresentation(PresentationId PresentationId, PaletteId PaletteId, char Glyph, PresentationColor Color)
{
    public EntityPresentation(char Glyph, PresentationColor Color)
        : this(LegacyPresentationId(Glyph), LegacyPaletteId(Color), Glyph: Glyph, Color: Color)
    {
    }

    public EntityInspectionAppearance ToInspectionAppearance() => new(Glyph, Color, PresentationId, PaletteId);

    public static PresentationId LegacyPresentationId(char glyph) => new($"legacy.glyph.{glyph}");

    public static PaletteId LegacyPaletteId(PresentationColor color) => new($"legacy.color.{color}");
}

public sealed record PresentationDefinition(PresentationId Id, string Name, string FallbackText, IReadOnlyList<string>? Tags = null)
{
    public IReadOnlyList<string> Tags { get; } = Tags ?? [];
}

public sealed record PaletteDefinition(PaletteId Id, string Name, IReadOnlyDictionary<string, PresentationColor>? Roles = null)
{
    public IReadOnlyDictionary<string, PresentationColor> Roles { get; } = Roles ?? new Dictionary<string, PresentationColor>();
}

public static class BuiltInPresentationCatalog
{
    public static IReadOnlyDictionary<PresentationId, PresentationDefinition> Presentations { get; } = new Dictionary<PresentationId, PresentationDefinition>
    {
        [new PresentationId("creature.spider")] = new(new PresentationId("creature.spider"), "Spider", "s", ["creature"]),
        [new PresentationId("creature.rat")] = new(new PresentationId("creature.rat"), "Rat", "r", ["creature"]),
        [new PresentationId("creature.slime")] = new(new PresentationId("creature.slime"), "Slime", "s", ["creature"]),
        [new PresentationId("item.coin")] = new(new PresentationId("item.coin"), "Coin", "c", ["item"]),
        [new PresentationId("item.mana")] = new(new PresentationId("item.mana"), "Mana", "m", ["item"]),
        [new PresentationId("item.heart")] = new(new PresentationId("item.heart"), "Heart", "h", ["item"]),
        [new PresentationId("item.bag")] = new(new PresentationId("item.bag"), "Bag", "b", ["item"]),
        [new PresentationId("item.crystal")] = new(new PresentationId("item.crystal"), "Crystal", "c", ["item"]),
        [new PresentationId("item.box")] = new(new PresentationId("item.box"), "Box", "x", ["item"]),
        [new PresentationId("item.potion")] = new(new PresentationId("item.potion"), "Potion", "!", ["item"]),
        [new PresentationId("object.pushBlock")] = new(new PresentationId("object.pushBlock"), "Block", "[", ["object"]),
        [new PresentationId("actor.player")] = new(new PresentationId("actor.player"), "Player", "@", ["actor"]),
        [new PresentationId("face.smile")] = new(new PresentationId("face.smile"), "Smile Face", ":", ["face"]),
        [new PresentationId("face.happy")] = new(new PresentationId("face.happy"), "Happy Face", ":", ["face"]),
        [new PresentationId("face.sad")] = new(new PresentationId("face.sad"), "Sad Face", ":", ["face"]),
        [new PresentationId("face.closed")] = new(new PresentationId("face.closed"), "Closed Face", ":", ["face"]),
        [new PresentationId("face.neutral")] = new(new PresentationId("face.neutral"), "Neutral Face", ":", ["face"])
    };

    public static IReadOnlyDictionary<PaletteId, PaletteDefinition> Palettes { get; } = Presentations.Keys
        .ToDictionary(
            id => new PaletteId($"{id.Value}.default"),
            id => new PaletteDefinition(new PaletteId($"{id.Value}.default"), $"{Presentations[id].Name} Default"));
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

public enum MergedInventoryJoinAlignment
{
    Center
}

public sealed record MergedInventoryJoinEndpoint(EntityId OwnerId, Direction Edge);

public sealed record MergedInventoryAlignedJoin(
    MergedInventoryJoinEndpoint From,
    MergedInventoryJoinEndpoint To,
    MergedInventoryJoinAlignment Align);

public sealed record MergedInventoryLayerDefinition(
    MergedInventoryLayerId Id,
    IReadOnlyList<MergedInventorySpaceContribution> Spaces,
    IReadOnlyList<MergedInventoryAlignedJoin>? Joins = null);

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
