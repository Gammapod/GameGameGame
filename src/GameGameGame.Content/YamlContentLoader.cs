using GameGameGame.Core;
using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GameGameGame.Content;

public static class YamlContentLoader
{
    public static PrototypeContentRegistry LoadRegistryResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded YAML content resource {resourceName} was not found.");
        using var reader = new StreamReader(stream);

        return LoadRegistry(reader.ReadToEnd());
    }

    public static PrototypeContentRegistry LoadRegistryFile(string path) =>
        LoadRegistry(File.ReadAllText(path));

    public static PrototypeContentRegistry LoadRegistry(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        var document = deserializer.Deserialize<ContentDocumentDto>(yaml) ?? new ContentDocumentDto();

        return new PrototypeContentRegistry(
            MaterializeEntityTemplates(document.EntityTemplates),
            MaterializeActionPlans(document.ActionPlans),
            MaterializePresentations(document.Presentations),
            MaterializePresentationCatalog(document.PresentationCatalog),
            MaterializePaletteCatalog(document.Palettes));
    }

    private static IReadOnlyDictionary<PresentationId, PresentationDefinition> MaterializePresentationCatalog(
        Dictionary<string, PresentationDefinitionDto>? catalog)
    {
        var result = new Dictionary<PresentationId, PresentationDefinition>(BuiltInPresentationCatalog.Presentations);
        foreach (var (id, definition) in catalog ?? [])
        {
            var presentationId = new PresentationId(id);
            result[presentationId] = new PresentationDefinition(
                presentationId,
                definition.Name ?? id,
                definition.FallbackText ?? "?",
                definition.Tags);
        }

        return result;
    }

    private static IReadOnlyDictionary<PaletteId, PaletteDefinition> MaterializePaletteCatalog(
        Dictionary<string, PaletteDefinitionDto>? palettes)
    {
        var result = new Dictionary<PaletteId, PaletteDefinition>(BuiltInPresentationCatalog.Palettes);
        foreach (var (id, definition) in palettes ?? [])
        {
            var paletteId = new PaletteId(id);
            result[paletteId] = new PaletteDefinition(
                paletteId,
                definition.Name ?? id,
                definition.Roles ?? new Dictionary<string, PresentationColor>());
        }

        return result;
    }

    private static IReadOnlyDictionary<EntityTemplateId, EntityTemplate> MaterializeEntityTemplates(
        Dictionary<string, EntityTemplateDto>? templates)
    {
        var result = new Dictionary<EntityTemplateId, EntityTemplate>();

        foreach (var (id, template) in templates ?? [])
        {
            result[new EntityTemplateId(id)] = new EntityTemplate(
                template.Name ?? id,
                template.InventoryWidth,
                template.InventoryHeight,
                template.Bulk ?? template.Weight,
                template.Aperture ?? template.CarryingCapacity,
                CarriedEntities: MaterializeCarriedEntities(template.CarriedEntities),
                DefaultActionPlanId: template.DefaultActionPlanId is null ? null : new ActionPlanTemplateId(template.DefaultActionPlanId),
                DefaultPlanVariables: MaterializePlanVariables(template.DefaultPlanVariables),
                ActionStateDefaults: MaterializeActionStateDefaults(template.ActionStateDefaults),
                Targeting: MaterializeTargetingProfile(template.Targeting),
                TargetingRules: MaterializeTargetingRules(template.TargetingRules),
                EnterPolicy: template.EnterPolicy,
                ExitPolicy: template.ExitPolicy,
                TopologyPolicy: template.TopologyPolicy ?? EntityTopologyPolicy.None);
        }

        return result;
    }

    private static IReadOnlyList<EntityTargetingRule>? MaterializeTargetingRules(List<EntityTargetingRuleDto>? rules)
    {
        if (rules is null || rules.Count == 0)
        {
            return null;
        }

        return rules
            .Select(rule => new EntityTargetingRule(
                rule.Slot,
                string.IsNullOrWhiteSpace(rule.TargetTemplateId) ? null : new EntityTemplateId(rule.TargetTemplateId),
                rule.Range,
                rule.Hint,
                rule.Label,
                rule.TargetCapabilities,
                MaterializeLocality(rule.Locality)))
            .ToList();
    }

    private static EntityTargetingProfile? MaterializeTargetingProfile(EntityTargetingProfileDto? targeting)
    {
        if (targeting is null)
        {
            return null;
        }

        return new EntityTargetingProfile(
            targeting.Range,
            MaterializeLocality(targeting.DefaultLocality ?? targeting.Locality),
            MaterializeTargetingRules(targeting.Rules) ?? []);
    }

    private static TargetingLocalityQuery? MaterializeLocality(TargetingLocalityDto? locality) =>
        locality?.Origins is { Count: > 0 } origins
            ? new TargetingLocalityQuery(origins)
            : null;

    private static IReadOnlyList<CarriedEntityTemplate>? MaterializeCarriedEntities(List<CarriedEntityTemplateDto>? carriedEntities)
    {
        if (carriedEntities is null || carriedEntities.Count == 0)
        {
            return null;
        }

        return carriedEntities
            .Select(carried => new CarriedEntityTemplate(
                new EntityId(Required(carried.EntityId, nameof(carried.EntityId))),
                new EntityTemplateId(Required(carried.TemplateId, nameof(carried.TemplateId))),
                MaterializeCoord(carried.Coord),
                carried.Controller))
            .ToList();
    }

    private static IReadOnlyDictionary<string, PlanValueDescriptor>? MaterializePlanVariables(
        Dictionary<string, PlanValueDescriptorDto>? variables)
    {
        if (variables is null || variables.Count == 0)
        {
            return null;
        }

        return variables.ToDictionary(entry => entry.Key, entry => MaterializePlanValue(entry.Value));
    }

    private static ActorActionStateDefaults? MaterializeActionStateDefaults(ActorActionStateDefaultsDto? defaults)
    {
        if (defaults is null)
        {
            return null;
        }

        return new ActorActionStateDefaults(
            defaults.Facing,
            string.IsNullOrWhiteSpace(defaults.Target) ? null : new EntityId(defaults.Target));
    }

    private static PlanValueDescriptor MaterializePlanValue(PlanValueDescriptorDto dto) =>
        dto.Kind switch
        {
            PlanValueKind.Direction => PlanValueDescriptor.Direction(dto.DirectionValue ?? throw Missing(nameof(dto.DirectionValue))),
            PlanValueKind.Entity => PlanValueDescriptor.Entity(new EntityId(Required(dto.EntityValue, nameof(dto.EntityValue)))),
            PlanValueKind.Coord => PlanValueDescriptor.Coord(MaterializeCoord(dto.CoordValue)),
            PlanValueKind.Int => PlanValueDescriptor.Int(dto.IntValue ?? throw Missing(nameof(dto.IntValue))),
            _ => throw new InvalidOperationException($"Unsupported plan value kind {dto.Kind}.")
        };

    private static IReadOnlyDictionary<EntityTemplateId, EntityPresentation> MaterializePresentations(
        Dictionary<string, EntityPresentationDto>? presentations)
    {
        var result = new Dictionary<EntityTemplateId, EntityPresentation>();

        foreach (var (id, presentation) in presentations ?? [])
        {
            var glyph = string.IsNullOrWhiteSpace(presentation.Glyph)
                ? FallbackGlyph(presentation.PresentationId)
                : presentation.Glyph[0];
            result[new EntityTemplateId(id)] = new EntityPresentation(
                string.IsNullOrWhiteSpace(presentation.PresentationId)
                    ? EntityPresentation.LegacyPresentationId(glyph)
                    : new PresentationId(presentation.PresentationId),
                string.IsNullOrWhiteSpace(presentation.PaletteId)
                    ? EntityPresentation.LegacyPaletteId(presentation.Color)
                    : new PaletteId(presentation.PaletteId),
                glyph,
                presentation.Color);
        }

        return result;
    }

    private static IReadOnlyDictionary<ActionPlanTemplateId, ActionPlanDescriptor> MaterializeActionPlans(
        Dictionary<string, ActionPlanDescriptorDto>? actionPlans)
    {
        var result = new Dictionary<ActionPlanTemplateId, ActionPlanDescriptor>();

        foreach (var (templateId, plan) in actionPlans ?? [])
        {
            result[new ActionPlanTemplateId(templateId)] = new ActionPlanDescriptor(
                new ActionPlanId(Required(plan.Id, nameof(plan.Id))),
                (plan.Steps ?? []).Select(MaterializeStep).ToList(),
                plan.Primitive is null ? null : MaterializePrimitive(plan.Primitive),
                plan.Behavior is null ? null : MaterializeBehavior(plan.Behavior));
        }

        return result;
    }

    private static ActionPlanPrimitiveDescriptor MaterializePrimitive(ActionPlanPrimitiveDescriptorDto primitive) =>
        new(
            primitive.Kind,
            primitive.FallbackPlanId is null ? null : new ActionPlanId(primitive.FallbackPlanId));

    private static ActionPlanBehaviorDescriptor MaterializeBehavior(ActionPlanBehaviorDescriptorDto behavior) =>
        new((behavior.Steps ?? []).Select(MaterializeBehaviorStep).ToList());

    private static ActionPlanBehaviorStepDescriptor MaterializeBehaviorStep(ActionPlanBehaviorStepDescriptorDto step) =>
        new(
            step.Kind,
            step.TargetSlot,
            step.TargetLabel,
            step.TargetSelf,
            step.PlanId is null ? null : new ActionPlanId(step.PlanId),
            step.DirectionMode is null ? null : Enum.Parse<ActionPlanMoveDirectionMode>(step.DirectionMode, ignoreCase: true),
            step.TransferDirection is null ? null : Enum.Parse<TransferDirection>(step.TransferDirection, ignoreCase: true),
            step.TemplateId,
            step.CreatePlacement is null ? null : Enum.Parse<CreateEntityPlacement>(step.CreatePlacement, ignoreCase: true),
            step.PathMode is null ? null : Enum.Parse<ActionPlanTargetPathMode>(step.PathMode, ignoreCase: true),
            step.DesiredDistance,
            step.OrbitDirection is null ? null : Enum.Parse<ActionPlanOrbitDirection>(step.OrbitDirection, ignoreCase: true))
        {
            Costs = (step.Costs ?? [])
                .Select(cost => new ActionStepCostDescriptor(cost.TemplateId ?? string.Empty, cost.Quantity))
                .ToList()
        };

    private static ActionPlanStepDescriptor MaterializeStep(ActionPlanStepDescriptorDto step) =>
        new(
            Required(step.Label, nameof(step.Label)),
            (step.Checks ?? []).Select(MaterializeCheck).ToList(),
            step.OnSuccess is null ? null : MaterializeEffect(step.OnSuccess),
            step.OnFailure is null ? null : MaterializeEffect(step.OnFailure));

    private static PlanCheckDescriptor MaterializeCheck(PlanCheckDescriptorDto check) =>
        check.Kind switch
        {
            PlanCheckKind.CanMove => check.DirectionVariable is null
                ? PlanCheckDescriptor.CanMove()
                : PlanCheckDescriptor.CanMove(Required(check.DirectionVariable, nameof(check.DirectionVariable))),
            PlanCheckKind.BlockingEntity => check.DirectionVariable is null && check.TargetVariable is null
                ? PlanCheckDescriptor.BlockingEntity()
                : PlanCheckDescriptor.BlockingEntity(
                    Required(check.DirectionVariable, nameof(check.DirectionVariable)),
                    Required(check.TargetVariable, nameof(check.TargetVariable))),
            PlanCheckKind.CanPickup => check.TargetVariable is null
                ? PlanCheckDescriptor.CanPickup(MaterializeCoord(check.InventoryCoord))
                : PlanCheckDescriptor.CanPickup(
                    Required(check.TargetVariable, nameof(check.TargetVariable)),
                    MaterializeCoord(check.InventoryCoord)),
            _ => throw new InvalidOperationException($"Unsupported plan check kind {check.Kind}.")
        };

    private static PlanEffectDescriptor MaterializeEffect(PlanEffectDescriptorDto effect) =>
        effect.Kind switch
        {
            PlanEffectKind.Teleport => PlanEffectDescriptor.Teleport(
                MaterializeMovementTarget(effect.MovementTarget),
                MaterializeMovementDestination(effect.MovementDestination)),
            PlanEffectKind.Move => effect.DirectionVariable is null
                ? PlanEffectDescriptor.Move()
                : PlanEffectDescriptor.Move(Required(effect.DirectionVariable, nameof(effect.DirectionVariable))),
            PlanEffectKind.Pickup => effect.TargetVariable is null
                ? PlanEffectDescriptor.Pickup(MaterializeCoord(effect.InventoryCoord))
                : PlanEffectDescriptor.Pickup(
                    Required(effect.TargetVariable, nameof(effect.TargetVariable)),
                    MaterializeCoord(effect.InventoryCoord)),
            PlanEffectKind.Drop => PlanEffectDescriptor.Drop(
                MaterializeMovementTarget(effect.MovementTarget),
                MaterializeMovementDestination(effect.MovementDestination)),
            PlanEffectKind.ReverseDirection => effect.DirectionVariable is null
                ? PlanEffectDescriptor.ReverseDirection(effect.ConsumesTurn, effect.ContinuePlan)
                : PlanEffectDescriptor.ReverseDirection(
                    Required(effect.DirectionVariable, nameof(effect.DirectionVariable)),
                    effect.ConsumesTurn,
                    effect.ContinuePlan),
            PlanEffectKind.Wait => PlanEffectDescriptor.Wait(),
            PlanEffectKind.SetVariable => PlanEffectDescriptor.SetVariable(
                Required(effect.VariableName, nameof(effect.VariableName)),
                MaterializePlanValue(effect.Value ?? throw Missing(nameof(effect.Value))).Materialize(),
                effect.ConsumesTurn,
                effect.ContinuePlan),
            PlanEffectKind.CallPlan => PlanEffectDescriptor.CallPlan(new ActionPlanId(Required(effect.PlanId, nameof(effect.PlanId)))),
            _ => throw new InvalidOperationException($"Unsupported plan effect kind {effect.Kind}.")
        };

    private static MovementTargetDescriptor MaterializeMovementTarget(MovementTargetDescriptorDto? target)
    {
        if (target is null)
        {
            throw Missing(nameof(target));
        }

        return target.Kind switch
        {
            MovementTargetKind.Self => MovementTargetDescriptor.Self(),
            MovementTargetKind.CanonicalTarget => MovementTargetDescriptor.CanonicalTarget(),
            MovementTargetKind.Entity => MovementTargetDescriptor.Entity(new EntityId(Required(target.EntityId, nameof(target.EntityId)))),
            MovementTargetKind.CarriedInventoryCoord => MovementTargetDescriptor.CarriedInventoryCoord(MaterializeCoord(target.InventoryCoord)),
            _ => throw new InvalidOperationException($"Unsupported movement target kind {target.Kind}.")
        };
    }

    private static MovementDestinationDescriptor MaterializeMovementDestination(MovementDestinationDescriptorDto? destination)
    {
        if (destination is null)
        {
            throw Missing(nameof(destination));
        }

        return destination.Kind switch
        {
            MovementDestinationKind.PlaneCoord => MovementDestinationDescriptor.Plane(MaterializePlaneCoord(destination.PlaneCoord)),
            MovementDestinationKind.InventorySlot => MovementDestinationDescriptor.InventorySlot(
                new EntityId(Required(destination.OwnerId, nameof(destination.OwnerId))),
                MaterializeCoord(destination.InventoryCoord)),
            MovementDestinationKind.AdjacentToSelf => MovementDestinationDescriptor.AdjacentToSelf(destination.Direction ?? throw Missing(nameof(destination.Direction))),
            MovementDestinationKind.AdjacentToEntity => MovementDestinationDescriptor.AdjacentToEntity(
                new EntityId(Required(destination.AnchorEntityId, nameof(destination.AnchorEntityId))),
                destination.Direction ?? throw Missing(nameof(destination.Direction))),
            MovementDestinationKind.AdjacentToCanonicalTarget => MovementDestinationDescriptor.AdjacentToCanonicalTarget(destination.Direction ?? throw Missing(nameof(destination.Direction))),
            _ => throw new InvalidOperationException($"Unsupported movement destination kind {destination.Kind}.")
        };
    }

    private static PlaneCoord MaterializePlaneCoord(PlaneCoordDto? coord) =>
        coord is null
            ? throw Missing(nameof(coord))
            : new PlaneCoord(new PlaneId(Required(coord.PlaneId, nameof(coord.PlaneId))), MaterializeCoord(coord.Coord));

    private static GridCoord MaterializeCoord(GridCoordDto? coord) =>
        coord is null ? throw Missing(nameof(coord)) : new GridCoord(coord.X, coord.Y);

    private static string Required(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw Missing(name) : value;

    private static char FallbackGlyph(string? presentationId) =>
        string.IsNullOrWhiteSpace(presentationId) ? throw Missing(nameof(EntityPresentationDto.Glyph)) : '?';

    private static InvalidOperationException Missing(string name) =>
        new($"YAML content field {name} is required.");

    private sealed class ContentDocumentDto
    {
        public Dictionary<string, EntityTemplateDto>? EntityTemplates { get; set; }

        public Dictionary<string, EntityPresentationDto>? Presentations { get; set; }

        public Dictionary<string, PresentationDefinitionDto>? PresentationCatalog { get; set; }

        public Dictionary<string, PaletteDefinitionDto>? Palettes { get; set; }

        public Dictionary<string, ActionPlanDescriptorDto>? ActionPlans { get; set; }
    }

    private sealed class PresentationDefinitionDto
    {
        public string? Name { get; set; }

        public string? FallbackText { get; set; }

        public List<string>? Tags { get; set; }
    }

    private sealed class PaletteDefinitionDto
    {
        public string? Name { get; set; }

        public Dictionary<string, PresentationColor>? Roles { get; set; }
    }

    private sealed class EntityTemplateDto
    {
        public string? Name { get; set; }

        public int InventoryWidth { get; set; }

        public int InventoryHeight { get; set; }

        public int Weight { get; set; }

        public int CarryingCapacity { get; set; }

        public int? Bulk { get; set; }

        public int? Aperture { get; set; }

        public EntityEnterPolicy? EnterPolicy { get; set; }

        public EntityExitPolicy? ExitPolicy { get; set; }

        public EntityTopologyPolicy? TopologyPolicy { get; set; }

        public string? DefaultActionPlanId { get; set; }

        public Dictionary<string, PlanValueDescriptorDto>? DefaultPlanVariables { get; set; }

        public ActorActionStateDefaultsDto? ActionStateDefaults { get; set; }

        public EntityTargetingProfileDto? Targeting { get; set; }

        public List<EntityTargetingRuleDto>? TargetingRules { get; set; }

        public List<CarriedEntityTemplateDto>? CarriedEntities { get; set; }
    }

    private sealed class EntityTargetingRuleDto
    {
        public int Slot { get; set; }

        public string? Hint { get; set; }

        public string? Label { get; set; }

        public string? TargetTemplateId { get; set; }

        public List<ActionPlanBehaviorStepKind>? TargetCapabilities { get; set; }

        public int Range { get; set; }

        public TargetingLocalityDto? Locality { get; set; }
    }

    private sealed class EntityTargetingProfileDto
    {
        public int Range { get; set; }

        public TargetingLocalityDto? Locality { get; set; }

        public TargetingLocalityDto? DefaultLocality { get; set; }

        public List<EntityTargetingRuleDto>? Rules { get; set; }
    }

    private sealed class TargetingLocalityDto
    {
        public List<TargetingLocalityOrigin>? Origins { get; set; }
    }

    private sealed class ActorActionStateDefaultsDto
    {
        public Direction? Facing { get; set; }

        public string? Target { get; set; }
    }

    private sealed class CarriedEntityTemplateDto
    {
        public string? EntityId { get; set; }

        public string? TemplateId { get; set; }

        public GridCoordDto? Coord { get; set; }

        public EntityController? Controller { get; set; }
    }

    private sealed class EntityPresentationDto
    {
        public string? PresentationId { get; set; }

        public string? PaletteId { get; set; }

        public string? Glyph { get; set; }

        public PresentationColor Color { get; set; }
    }

    private sealed class ActionPlanDescriptorDto
    {
        public string? Id { get; set; }

        public ActionPlanPrimitiveDescriptorDto? Primitive { get; set; }

        public ActionPlanBehaviorDescriptorDto? Behavior { get; set; }

        public List<ActionPlanStepDescriptorDto>? Steps { get; set; }
    }

    private sealed class ActionPlanBehaviorDescriptorDto
    {
        public List<ActionPlanBehaviorStepDescriptorDto>? Steps { get; set; }
    }

    private sealed class ActionPlanBehaviorStepDescriptorDto
    {
        public ActionPlanBehaviorStepKind Kind { get; set; }

        public int? TargetSlot { get; set; }

        public string? TargetLabel { get; set; }

        public bool TargetSelf { get; set; }

        public string? PlanId { get; set; }

        public string? DirectionMode { get; set; }

        public string? TransferDirection { get; set; }

        public string? TemplateId { get; set; }

        public string? CreatePlacement { get; set; }

        public string? PathMode { get; set; }

        public int? DesiredDistance { get; set; }

        public string? OrbitDirection { get; set; }

        public List<ActionStepCostDescriptorDto>? Costs { get; set; }
    }

    private sealed class ActionStepCostDescriptorDto
    {
        public string? TemplateId { get; set; }

        public int Quantity { get; set; }
    }

    private sealed class ActionPlanPrimitiveDescriptorDto
    {
        public ActionPlanPrimitiveKind Kind { get; set; }

        public string? FallbackPlanId { get; set; }
    }

    private sealed class ActionPlanStepDescriptorDto
    {
        public string? Label { get; set; }

        public List<PlanCheckDescriptorDto>? Checks { get; set; }

        public PlanEffectDescriptorDto? OnSuccess { get; set; }

        public PlanEffectDescriptorDto? OnFailure { get; set; }
    }

    private sealed class PlanCheckDescriptorDto
    {
        public PlanCheckKind Kind { get; set; }

        public string? DirectionVariable { get; set; }

        public string? TargetVariable { get; set; }

        public GridCoordDto? InventoryCoord { get; set; }
    }

    private sealed class PlanEffectDescriptorDto
    {
        public PlanEffectKind Kind { get; set; }

        public string? DirectionVariable { get; set; }

        public string? TargetVariable { get; set; }

        public GridCoordDto? InventoryCoord { get; set; }

        public string? PlanId { get; set; }

        public string? VariableName { get; set; }

        public PlanValueDescriptorDto? Value { get; set; }

        public MovementTargetDescriptorDto? MovementTarget { get; set; }

        public MovementDestinationDescriptorDto? MovementDestination { get; set; }

        public bool ConsumesTurn { get; set; }

        public bool ContinuePlan { get; set; }
    }

    private sealed class PlanValueDescriptorDto
    {
        public PlanValueKind Kind { get; set; }

        public Direction? DirectionValue { get; set; }

        public string? EntityValue { get; set; }

        public GridCoordDto? CoordValue { get; set; }

        public int? IntValue { get; set; }
    }

    private sealed class MovementTargetDescriptorDto
    {
        public MovementTargetKind Kind { get; set; }

        public string? EntityId { get; set; }

        public GridCoordDto? InventoryCoord { get; set; }
    }

    private sealed class MovementDestinationDescriptorDto
    {
        public MovementDestinationKind Kind { get; set; }

        public PlaneCoordDto? PlaneCoord { get; set; }

        public string? OwnerId { get; set; }

        public GridCoordDto? InventoryCoord { get; set; }

        public string? AnchorEntityId { get; set; }

        public Direction? Direction { get; set; }
    }

    private sealed class PlaneCoordDto
    {
        public string? PlaneId { get; set; }

        public GridCoordDto? Coord { get; set; }
    }

    private sealed class GridCoordDto
    {
        public int X { get; set; }

        public int Y { get; set; }
    }
}
