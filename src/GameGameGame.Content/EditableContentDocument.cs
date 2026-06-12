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
        var canonical = LoadYaml(SerializeYaml());
        canonical.CanonicalizeLegacyActionPlanVariableFields();

        return canonical.SerializeYaml();
    }

    private string SerializeYaml()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        return serializer.Serialize(this);
    }

    private void CanonicalizeLegacyActionPlanVariableFields()
    {
        CanonicalizeLegacyActionStateDefaults();

        foreach (var plan in ActionPlans.Values)
        {
            foreach (var step in plan.Steps ?? [])
            {
                foreach (var check in step.Checks ?? [])
                {
                    CanonicalizeLegacyCheckVariableFields(check);
                }

                if (step.OnSuccess is not null)
                {
                    CanonicalizeLegacyEffectVariableFields(step.OnSuccess);
                }

                if (step.OnFailure is not null)
                {
                    CanonicalizeLegacyEffectVariableFields(step.OnFailure);
                }
            }
        }
    }

    private void CanonicalizeLegacyActionStateDefaults()
    {
        foreach (var template in EntityTemplates.Values)
        {
            if (template.DefaultPlanVariables is null)
            {
                continue;
            }

            if (template.DefaultPlanVariables.TryGetValue("facing", out var facing)
                && facing.Kind == PlanValueKind.Direction
                && facing.DirectionValue is { } direction)
            {
                template.ActionStateDefaults ??= new ActorActionStateDefaultsDto();
                template.ActionStateDefaults.Facing ??= direction;
                template.DefaultPlanVariables.Remove("facing");
            }

            if (template.DefaultPlanVariables.Count == 0)
            {
                template.DefaultPlanVariables = null;
            }
        }
    }

    private static void CanonicalizeLegacyCheckVariableFields(PlanCheckDescriptorDto check)
    {
        switch (check.Kind)
        {
            case PlanCheckKind.CanMove:
                check.DirectionVariable = ClearIfCanonicalFacing(check.DirectionVariable);
                break;
            case PlanCheckKind.BlockingEntity:
                if (IsCanonicalFacing(check.DirectionVariable) && IsCanonicalTarget(check.TargetVariable))
                {
                    check.DirectionVariable = null;
                    check.TargetVariable = null;
                }
                break;
            case PlanCheckKind.CanPickup:
                check.TargetVariable = ClearIfCanonicalTarget(check.TargetVariable);
                break;
        }
    }

    private static void CanonicalizeLegacyEffectVariableFields(PlanEffectDescriptorDto effect)
    {
        switch (effect.Kind)
        {
            case PlanEffectKind.Move:
                effect.DirectionVariable = ClearIfCanonicalFacing(effect.DirectionVariable);
                break;
            case PlanEffectKind.Pickup:
                effect.TargetVariable = ClearIfCanonicalTarget(effect.TargetVariable);
                break;
            case PlanEffectKind.ReverseDirection:
                effect.DirectionVariable = ClearIfCanonicalFacing(effect.DirectionVariable);
                break;
        }
    }

    private static string? ClearIfCanonicalFacing(string? value) =>
        IsCanonicalFacing(value) ? null : value;

    private static string? ClearIfCanonicalTarget(string? value) =>
        IsCanonicalTarget(value) ? null : value;

    private static bool IsCanonicalFacing(string? value) =>
        string.Equals(value, "facing", StringComparison.Ordinal);

    private static bool IsCanonicalTarget(string? value) =>
        string.Equals(value, "target", StringComparison.Ordinal);

    public PrototypeContentRegistry ToRegistry() => YamlContentLoader.LoadRegistry(SerializeYaml());

    public ContentValidationResult ValidateCanonicalAuthoring()
    {
        var diagnostics = new List<ContentDiagnostic>();

        foreach (var (templateId, template) in EntityTemplates)
        {
            if (template.DefaultPlanVariables is null)
            {
                continue;
            }

            foreach (var variableName in template.DefaultPlanVariables.Keys)
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.ArbitraryPlanVariableField,
                    $"Entity template {templateId} declares arbitrary default plan variable {variableName}.",
                    entityTemplateId: new EntityTemplateId(templateId),
                    variableName: variableName));
            }
        }

        foreach (var (planId, plan) in ActionPlans)
        {
            var steps = plan.Steps ?? [];
            for (var stepIndex = 0; stepIndex < steps.Count; stepIndex++)
            {
                var step = steps[stepIndex];
                foreach (var check in step.Checks ?? [])
                {
                    AddVariableFieldDiagnostics(diagnostics, planId, stepIndex, check.DirectionVariable, "directionVariable");
                    AddVariableFieldDiagnostics(diagnostics, planId, stepIndex, check.TargetVariable, "targetVariable");
                }

                if (step.OnSuccess is not null)
                {
                    AddEffectVariableFieldDiagnostics(diagnostics, planId, stepIndex, step.OnSuccess);
                }

                if (step.OnFailure is not null)
                {
                    AddEffectVariableFieldDiagnostics(diagnostics, planId, stepIndex, step.OnFailure);
                }
            }
        }

        return new ContentValidationResult(diagnostics);
    }

    private static void AddEffectVariableFieldDiagnostics(
        List<ContentDiagnostic> diagnostics,
        string planId,
        int stepIndex,
        PlanEffectDescriptorDto effect)
    {
        AddVariableFieldDiagnostics(diagnostics, planId, stepIndex, effect.DirectionVariable, "directionVariable");
        AddVariableFieldDiagnostics(diagnostics, planId, stepIndex, effect.TargetVariable, "targetVariable");
        AddVariableFieldDiagnostics(diagnostics, planId, stepIndex, effect.VariableName, "variableName");
    }

    private static void AddVariableFieldDiagnostics(
        List<ContentDiagnostic> diagnostics,
        string planId,
        int stepIndex,
        string? variableName,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(variableName))
        {
            return;
        }

        diagnostics.Add(ContentDiagnostic.Error(
            ContentDiagnosticCode.ArbitraryPlanVariableField,
            $"Action plan {planId} step {stepIndex} declares arbitrary {fieldName} {variableName}.",
            actionPlanTemplateId: new ActionPlanTemplateId(planId),
            actionPlanId: new ActionPlanId(planId),
            stepIndex: stepIndex,
            variableName: variableName));
    }

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

        public ActorActionStateDefaultsDto? ActionStateDefaults { get; set; }

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
            ActionStateDefaults = template.ActionStateDefaults is null ? null : ActorActionStateDefaultsDto.From(template.ActionStateDefaults),
            CarriedEntities = template.CarriedEntities?.Select(CarriedEntityTemplateDto.From).ToList()
        };
    }

    public sealed class ActorActionStateDefaultsDto
    {
        public Direction? Facing { get; set; }

        public string? Target { get; set; }

        public static ActorActionStateDefaultsDto From(ActorActionStateDefaults defaults) => new()
        {
            Facing = defaults.Facing,
            Target = defaults.Target?.Value
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

        public ActionPlanStepDescriptor ToDescriptor() =>
            new(
                Label ?? string.Empty,
                (Checks ?? []).Select(check => check.ToDescriptor()).ToList(),
                OnSuccess?.ToDescriptor(),
                OnFailure?.ToDescriptor());
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

        public PlanCheckDescriptor ToDescriptor() =>
            Kind switch
            {
                PlanCheckKind.CanMove => DirectionVariable is null
                    ? PlanCheckDescriptor.CanMove()
                    : PlanCheckDescriptor.CanMove(DirectionVariable),
                PlanCheckKind.BlockingEntity => DirectionVariable is null && TargetVariable is null
                    ? PlanCheckDescriptor.BlockingEntity()
                    : PlanCheckDescriptor.BlockingEntity(DirectionVariable ?? string.Empty, TargetVariable ?? string.Empty),
                PlanCheckKind.CanPickup => TargetVariable is null
                    ? PlanCheckDescriptor.CanPickup(ToCoord(InventoryCoord))
                    : PlanCheckDescriptor.CanPickup(TargetVariable, ToCoord(InventoryCoord)),
                _ => throw new InvalidOperationException($"Unsupported plan check kind {Kind}.")
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

        public PlanEffectDescriptor ToDescriptor() =>
            Kind switch
            {
                PlanEffectKind.Move => DirectionVariable is null
                    ? PlanEffectDescriptor.Move()
                    : PlanEffectDescriptor.Move(DirectionVariable),
                PlanEffectKind.Pickup => TargetVariable is null
                    ? PlanEffectDescriptor.Pickup(ToCoord(InventoryCoord))
                    : PlanEffectDescriptor.Pickup(TargetVariable, ToCoord(InventoryCoord)),
                PlanEffectKind.ReverseDirection => DirectionVariable is null
                    ? PlanEffectDescriptor.ReverseDirection(ConsumesTurn, ContinuePlan)
                    : PlanEffectDescriptor.ReverseDirection(DirectionVariable, ConsumesTurn, ContinuePlan),
                PlanEffectKind.Wait => PlanEffectDescriptor.Wait(),
                PlanEffectKind.SetVariable => PlanEffectDescriptor.SetVariable(VariableName ?? string.Empty, Value?.ToDescriptor().Materialize() ?? new DirectionPlanValue(Direction.West), ConsumesTurn, ContinuePlan),
                PlanEffectKind.CallPlan => PlanEffectDescriptor.CallPlan(new ActionPlanId(PlanId ?? string.Empty)),
                _ => throw new InvalidOperationException($"Unsupported plan effect kind {Kind}.")
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

        public PlanValueDescriptor ToDescriptor() =>
            Kind switch
            {
                PlanValueKind.Direction => PlanValueDescriptor.Direction(DirectionValue ?? Direction.West),
                PlanValueKind.Entity => PlanValueDescriptor.Entity(new EntityId(EntityValue ?? string.Empty)),
                PlanValueKind.Coord => PlanValueDescriptor.Coord(ToCoord(CoordValue)),
                PlanValueKind.Int => PlanValueDescriptor.Int(IntValue ?? 0),
                _ => throw new InvalidOperationException($"Unsupported plan value kind {Kind}.")
            };
    }

    public sealed class GridCoordDto
    {
        public int X { get; set; }

        public int Y { get; set; }

        public static GridCoordDto From(GridCoord coord) => new() { X = coord.X, Y = coord.Y };
    }

    private static GridCoord ToCoord(GridCoordDto? coord) =>
        coord is null ? new GridCoord(0, 0) : new GridCoord(coord.X, coord.Y);
}
