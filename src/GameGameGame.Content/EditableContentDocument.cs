using GameGameGame.Core;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GameGameGame.Content;

public sealed class EditableContentDocument
{
    public Dictionary<string, EntityTemplateDto> EntityTemplates { get; set; } = [];

    public Dictionary<string, EntityPresentationDto> Presentations { get; set; } = [];

    public Dictionary<string, ActionPlanDescriptorDto> ActionPlans { get; set; } = [];

    public static EditableContentDocument LoadYaml(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<EditableContentDocument>(yaml) ?? new EditableContentDocument();
    }

    public string SaveYaml()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        return serializer.Serialize(this);
    }

    public PrototypeContentRegistry ToRegistry() => YamlContentLoader.LoadRegistry(SaveYaml());

    public EntityTemplateId AddEntityTemplate(string name, EntityTemplate template, EntityPresentation presentation)
    {
        var id = GenerateEntityTemplateId(name);
        EntityTemplates[id.Value] = EntityTemplateDto.From(template);
        Presentations[id.Value] = EntityPresentationDto.From(presentation);

        return id;
    }

    private EntityTemplateId GenerateEntityTemplateId(string name)
    {
        var baseId = ToCamelCaseId(name);
        var candidate = baseId;
        var suffix = 2;

        while (EntityTemplates.ContainsKey(candidate) || Presentations.ContainsKey(candidate))
        {
            candidate = $"{baseId}{suffix}";
            suffix++;
        }

        return new EntityTemplateId(candidate);
    }

    private static string ToCamelCaseId(string name)
    {
        var builder = new StringBuilder();
        var capitalizeNext = false;

        foreach (var character in name)
        {
            if (!char.IsLetterOrDigit(character))
            {
                capitalizeNext = builder.Length > 0;
                continue;
            }

            if (builder.Length == 0)
            {
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            builder.Append(capitalizeNext ? char.ToUpperInvariant(character) : character);
            capitalizeNext = false;
        }

        return builder.Length == 0 ? "entity" : builder.ToString();
    }

    public sealed class EntityTemplateDto
    {
        public string? Name { get; set; }

        public int InventoryWidth { get; set; }

        public int InventoryHeight { get; set; }

        public int Weight { get; set; }

        public int CarryingCapacity { get; set; }

        public string? DefaultActionPlanId { get; set; }

        public Dictionary<string, PlanValueDescriptorDto>? DefaultPlanVariables { get; set; }

        public List<CarriedEntityTemplateDto>? CarriedEntities { get; set; }

        public static EntityTemplateDto From(EntityTemplate template) => new()
        {
            Name = template.Name,
            InventoryWidth = template.InventoryWidth,
            InventoryHeight = template.InventoryHeight,
            Weight = template.Weight,
            CarryingCapacity = template.CarryingCapacity,
            DefaultActionPlanId = template.DefaultActionPlanId?.Value,
            DefaultPlanVariables = template.DefaultPlanVariables?.ToDictionary(entry => entry.Key, entry => PlanValueDescriptorDto.From(entry.Value)),
            CarriedEntities = template.CarriedEntities?.Select(CarriedEntityTemplateDto.From).ToList()
        };
    }

    public sealed class CarriedEntityTemplateDto
    {
        public string? EntityId { get; set; }

        public string? TemplateId { get; set; }

        public GridCoordDto? Coord { get; set; }

        public static CarriedEntityTemplateDto From(CarriedEntityTemplate carried) => new()
        {
            EntityId = carried.EntityId.Value,
            TemplateId = carried.TemplateId?.Value,
            Coord = GridCoordDto.From(carried.Coord)
        };
    }

    public sealed class EntityPresentationDto
    {
        public string? Glyph { get; set; }

        public PresentationColor Color { get; set; }

        public static EntityPresentationDto From(EntityPresentation presentation) => new()
        {
            Glyph = presentation.Glyph.ToString(),
            Color = presentation.Color
        };
    }

    public sealed class ActionPlanDescriptorDto
    {
        public string? Id { get; set; }

        public List<ActionPlanStepDescriptorDto>? Steps { get; set; }

        public static ActionPlanDescriptorDto From(ActionPlanDescriptor descriptor) => new()
        {
            Id = descriptor.Id.Value,
            Steps = descriptor.Steps.Select(ActionPlanStepDescriptorDto.From).ToList()
        };
    }

    public sealed class ActionPlanStepDescriptorDto
    {
        public string? Label { get; set; }

        public List<PlanCheckDescriptorDto>? Checks { get; set; }

        public PlanEffectDescriptorDto? OnSuccess { get; set; }

        public PlanEffectDescriptorDto? OnFailure { get; set; }

        public static ActionPlanStepDescriptorDto From(ActionPlanStepDescriptor descriptor) => new()
        {
            Label = descriptor.Label,
            Checks = descriptor.Checks.Select(PlanCheckDescriptorDto.From).ToList(),
            OnSuccess = descriptor.OnSuccess is null ? null : PlanEffectDescriptorDto.From(descriptor.OnSuccess),
            OnFailure = descriptor.OnFailure is null ? null : PlanEffectDescriptorDto.From(descriptor.OnFailure)
        };
    }

    public sealed class PlanCheckDescriptorDto
    {
        public PlanCheckKind Kind { get; set; }

        public string? DirectionVariable { get; set; }

        public string? TargetVariable { get; set; }

        public GridCoordDto? InventoryCoord { get; set; }

        public static PlanCheckDescriptorDto From(PlanCheckDescriptor descriptor) => new()
        {
            Kind = descriptor.Kind,
            DirectionVariable = descriptor.DirectionVariable,
            TargetVariable = descriptor.TargetVariable,
            InventoryCoord = descriptor.InventoryCoord is { } coord ? GridCoordDto.From(coord) : null
        };
    }

    public sealed class PlanEffectDescriptorDto
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

        public static PlanEffectDescriptorDto From(PlanEffectDescriptor descriptor) => new()
        {
            Kind = descriptor.Kind,
            DirectionVariable = descriptor.DirectionVariable,
            TargetVariable = descriptor.TargetVariable,
            InventoryCoord = descriptor.InventoryCoord is { } coord ? GridCoordDto.From(coord) : null,
            PlanId = descriptor.PlanId?.Value,
            VariableName = descriptor.VariableName,
            Value = descriptor.Value is null ? null : PlanValueDescriptorDto.From(descriptor.Value),
            ConsumesTurn = descriptor.ConsumesTurn,
            ContinuePlan = descriptor.ContinuePlan
        };
    }

    public sealed class PlanValueDescriptorDto
    {
        public PlanValueKind Kind { get; set; }

        public Direction? DirectionValue { get; set; }

        public string? EntityValue { get; set; }

        public GridCoordDto? CoordValue { get; set; }

        public int? IntValue { get; set; }

        public static PlanValueDescriptorDto From(PlanValueDescriptor value) => new()
        {
            Kind = value.Kind,
            DirectionValue = value.DirectionValue,
            EntityValue = value.EntityValue?.Value,
            CoordValue = value.CoordValue is { } coord ? GridCoordDto.From(coord) : null,
            IntValue = value.IntValue
        };

        public static PlanValueDescriptorDto From(PlanValue value) =>
            value switch
            {
                DirectionPlanValue direction => new PlanValueDescriptorDto { Kind = PlanValueKind.Direction, DirectionValue = direction.Value },
                EntityPlanValue entity => new PlanValueDescriptorDto { Kind = PlanValueKind.Entity, EntityValue = entity.Value.Value },
                CoordPlanValue coord => new PlanValueDescriptorDto { Kind = PlanValueKind.Coord, CoordValue = GridCoordDto.From(coord.Value) },
                IntPlanValue integer => new PlanValueDescriptorDto { Kind = PlanValueKind.Int, IntValue = integer.Value },
                _ => throw new InvalidOperationException($"Unsupported plan value type {value.GetType().Name}.")
            };
    }

    public sealed class GridCoordDto
    {
        public int X { get; set; }

        public int Y { get; set; }

        public static GridCoordDto From(GridCoord coord) => new() { X = coord.X, Y = coord.Y };
    }
}
