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
            MaterializePresentations(document.Presentations));
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
                template.Weight,
                template.CarryingCapacity,
                CarriedEntities: MaterializeCarriedEntities(template.CarriedEntities),
                DefaultActionPlanId: template.DefaultActionPlanId is null ? null : new ActionPlanTemplateId(template.DefaultActionPlanId),
                DefaultPlanVariables: MaterializePlanVariables(template.DefaultPlanVariables));
        }

        return result;
    }

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
                MaterializeCoord(carried.Coord)))
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
            result[new EntityTemplateId(id)] = new EntityPresentation(
                Required(presentation.Glyph, nameof(presentation.Glyph))[0],
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
                (plan.Steps ?? []).Select(MaterializeStep).ToList());
        }

        return result;
    }

    private static ActionPlanStepDescriptor MaterializeStep(ActionPlanStepDescriptorDto step) =>
        new(
            Required(step.Label, nameof(step.Label)),
            (step.Checks ?? []).Select(MaterializeCheck).ToList(),
            step.OnSuccess is null ? null : MaterializeEffect(step.OnSuccess),
            step.OnFailure is null ? null : MaterializeEffect(step.OnFailure));

    private static PlanCheckDescriptor MaterializeCheck(PlanCheckDescriptorDto check) =>
        check.Kind switch
        {
            PlanCheckKind.CanMove => PlanCheckDescriptor.CanMove(Required(check.DirectionVariable, nameof(check.DirectionVariable))),
            PlanCheckKind.BlockingEntity => PlanCheckDescriptor.BlockingEntity(
                Required(check.DirectionVariable, nameof(check.DirectionVariable)),
                Required(check.TargetVariable, nameof(check.TargetVariable))),
            PlanCheckKind.CanPickup => PlanCheckDescriptor.CanPickup(
                Required(check.TargetVariable, nameof(check.TargetVariable)),
                MaterializeCoord(check.InventoryCoord)),
            _ => throw new InvalidOperationException($"Unsupported plan check kind {check.Kind}.")
        };

    private static PlanEffectDescriptor MaterializeEffect(PlanEffectDescriptorDto effect) =>
        effect.Kind switch
        {
            PlanEffectKind.Move => PlanEffectDescriptor.Move(Required(effect.DirectionVariable, nameof(effect.DirectionVariable))),
            PlanEffectKind.Pickup => PlanEffectDescriptor.Pickup(
                Required(effect.TargetVariable, nameof(effect.TargetVariable)),
                MaterializeCoord(effect.InventoryCoord)),
            PlanEffectKind.ReverseDirection => PlanEffectDescriptor.ReverseDirection(
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

    private static GridCoord MaterializeCoord(GridCoordDto? coord) =>
        coord is null ? throw Missing(nameof(coord)) : new GridCoord(coord.X, coord.Y);

    private static string Required(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw Missing(name) : value;

    private static InvalidOperationException Missing(string name) =>
        new($"YAML content field {name} is required.");

    private sealed class ContentDocumentDto
    {
        public Dictionary<string, EntityTemplateDto>? EntityTemplates { get; set; }

        public Dictionary<string, EntityPresentationDto>? Presentations { get; set; }

        public Dictionary<string, ActionPlanDescriptorDto>? ActionPlans { get; set; }
    }

    private sealed class EntityTemplateDto
    {
        public string? Name { get; set; }

        public int InventoryWidth { get; set; }

        public int InventoryHeight { get; set; }

        public int Weight { get; set; }

        public int CarryingCapacity { get; set; }

        public string? DefaultActionPlanId { get; set; }

        public Dictionary<string, PlanValueDescriptorDto>? DefaultPlanVariables { get; set; }

        public List<CarriedEntityTemplateDto>? CarriedEntities { get; set; }
    }

    private sealed class CarriedEntityTemplateDto
    {
        public string? EntityId { get; set; }

        public string? TemplateId { get; set; }

        public GridCoordDto? Coord { get; set; }
    }

    private sealed class EntityPresentationDto
    {
        public string? Glyph { get; set; }

        public PresentationColor Color { get; set; }
    }

    private sealed class ActionPlanDescriptorDto
    {
        public string? Id { get; set; }

        public List<ActionPlanStepDescriptorDto>? Steps { get; set; }
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

    private sealed class GridCoordDto
    {
        public int X { get; set; }

        public int Y { get; set; }
    }
}
