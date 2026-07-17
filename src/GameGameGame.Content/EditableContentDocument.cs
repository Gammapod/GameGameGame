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

    public Dictionary<string, ScenarioDefinitionDto> Scenarios { get; set; } = [];

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
            AddActionPlanShapeDiagnostics(diagnostics, planId, plan);
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

        foreach (var (scenarioId, scenario) in Scenarios)
        {
            AddScenarioDiagnostics(diagnostics, scenarioId, scenario);
        }

        return new ContentValidationResult(diagnostics);
    }

    private void AddScenarioDiagnostics(
        List<ContentDiagnostic> diagnostics,
        string scenarioId,
        ScenarioDefinitionDto scenario)
    {
        if (string.IsNullOrWhiteSpace(scenario.ScenarioRootEntityTemplateId) || !EntityTemplates.ContainsKey(scenario.ScenarioRootEntityTemplateId))
        {
            diagnostics.Add(ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidScenarioDefinition,
                $"Scenario {scenarioId} references missing scenario root template {scenario.ScenarioRootEntityTemplateId}.",
                entityTemplateId: string.IsNullOrWhiteSpace(scenario.ScenarioRootEntityTemplateId) ? null : new EntityTemplateId(scenario.ScenarioRootEntityTemplateId)));
        }

        if (string.IsNullOrWhiteSpace(scenario.PlayerEntityTemplateId) || !EntityTemplates.ContainsKey(scenario.PlayerEntityTemplateId))
        {
            diagnostics.Add(ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidScenarioDefinition,
                $"Scenario {scenarioId} references missing player template {scenario.PlayerEntityTemplateId}.",
                entityTemplateId: string.IsNullOrWhiteSpace(scenario.PlayerEntityTemplateId) ? null : new EntityTemplateId(scenario.PlayerEntityTemplateId)));
        }

        if (scenario.ScenarioRootEntityTemplateId is not null && EntityTemplates.TryGetValue(scenario.ScenarioRootEntityTemplateId, out var root))
        {
            var start = ToCoord(scenario.PlayerStart);
            if (root.InventoryWidth <= 0 || root.InventoryHeight <= 0)
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidScenarioDefinition,
                    $"Scenario {scenarioId} root template {scenario.ScenarioRootEntityTemplateId} has no usable inventory/play plane.",
                    entityTemplateId: new EntityTemplateId(scenario.ScenarioRootEntityTemplateId)));
            }
            else if (start.X < 0 || start.Y < 0 || start.X >= root.InventoryWidth || start.Y >= root.InventoryHeight)
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidScenarioDefinition,
                    $"Scenario {scenarioId} player start {start.X},{start.Y} is outside scenario root bounds {root.InventoryWidth}x{root.InventoryHeight}.",
                    entityTemplateId: new EntityTemplateId(scenario.ScenarioRootEntityTemplateId),
                    coord: start));
            }
            else if ((root.CarriedEntities ?? []).FirstOrDefault(carried => carried.Coord?.X == start.X && carried.Coord.Y == start.Y) is { } occupant)
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidScenarioDefinition,
                    $"Scenario {scenarioId} player start {start.X},{start.Y} is occupied by carried entity {occupant.EntityId}.",
                    entityTemplateId: new EntityTemplateId(scenario.ScenarioRootEntityTemplateId),
                    carriedEntityId: string.IsNullOrWhiteSpace(occupant.EntityId) ? null : new EntityId(occupant.EntityId),
                    coord: start));
            }

            if (!string.IsNullOrWhiteSpace(scenario.PlayerEntityId)
                && (root.CarriedEntities ?? []).Any(carried => carried.EntityId == scenario.PlayerEntityId))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidScenarioDefinition,
                    $"Scenario {scenarioId} player entity ID {scenario.PlayerEntityId} conflicts with an entity already carried by scenario root {scenario.ScenarioRootEntityTemplateId}.",
                    entityTemplateId: new EntityTemplateId(scenario.ScenarioRootEntityTemplateId),
                    relatedEntityId: new EntityId(scenario.PlayerEntityId)));
            }

            AddPlayerControlDiagnostics(diagnostics, scenarioId, scenario, root);
        }
    }

    private void AddPlayerControlDiagnostics(
        List<ContentDiagnostic> diagnostics,
        string scenarioId,
        ScenarioDefinitionDto scenario,
        EntityTemplateDto root)
    {
        if (scenario.PlayerControls is null || scenario.PlayerControls.Count == 0)
        {
            return;
        }

        var materializedEntityIds = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(scenario.PlayerEntityId))
        {
            materializedEntityIds.Add(scenario.PlayerEntityId);
        }

        AddCarriedEntityIds(root, materializedEntityIds, new HashSet<string>(StringComparer.Ordinal));
        var assignedEntities = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (playerId, controlledEntityIds) in scenario.PlayerControls)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidScenarioDefinition,
                    $"Scenario {scenarioId} declares a player control binding with an empty player ID."));
                continue;
            }

            if (controlledEntityIds is null || controlledEntityIds.Count == 0)
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.InvalidScenarioDefinition,
                    $"Scenario {scenarioId} player control {playerId} has no controlled entities."));
                continue;
            }

            var seenForPlayer = new HashSet<string>(StringComparer.Ordinal);

            foreach (var controlledEntityId in controlledEntityIds ?? [])
            {
                if (string.IsNullOrWhiteSpace(controlledEntityId))
                {
                    diagnostics.Add(ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidScenarioDefinition,
                        $"Scenario {scenarioId} player control {playerId} references an empty entity ID."));
                    continue;
                }

                if (!seenForPlayer.Add(controlledEntityId))
                {
                    diagnostics.Add(ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidScenarioDefinition,
                        $"Scenario {scenarioId} player control {playerId} lists entity {controlledEntityId} more than once.",
                        relatedEntityId: new EntityId(controlledEntityId)));
                    continue;
                }

                if (!materializedEntityIds.Contains(controlledEntityId))
                {
                    diagnostics.Add(ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidScenarioDefinition,
                        $"Scenario {scenarioId} player control {playerId} references missing entity {controlledEntityId}.",
                        relatedEntityId: new EntityId(controlledEntityId)));
                    continue;
                }

                if (assignedEntities.TryGetValue(controlledEntityId, out var previousPlayerId) && previousPlayerId != playerId)
                {
                    diagnostics.Add(ContentDiagnostic.Error(
                        ContentDiagnosticCode.InvalidScenarioDefinition,
                        $"Scenario {scenarioId} controlled entity {controlledEntityId} is assigned to both {previousPlayerId} and {playerId}.",
                        relatedEntityId: new EntityId(controlledEntityId)));
                    continue;
                }

                assignedEntities[controlledEntityId] = playerId;
            }
        }
    }

    private void AddCarriedEntityIds(EntityTemplateDto template, HashSet<string> entityIds, HashSet<string> visitedTemplateIds)
    {
        foreach (var carried in template.CarriedEntities ?? [])
        {
            if (!string.IsNullOrWhiteSpace(carried.EntityId))
            {
                entityIds.Add(carried.EntityId);
            }

            if (!string.IsNullOrWhiteSpace(carried.TemplateId)
                && EntityTemplates.TryGetValue(carried.TemplateId, out var carriedTemplate)
                && visitedTemplateIds.Add(carried.TemplateId))
            {
                AddCarriedEntityIds(carriedTemplate, entityIds, visitedTemplateIds);
            }
        }
    }

    private static void AddActionPlanShapeDiagnostics(
        List<ContentDiagnostic> diagnostics,
        string planId,
        ActionPlanDescriptorDto plan)
    {
        var descriptor = plan.ToDescriptor(planId);
        var shape = ActionPlanShapeClassifier.Classify(descriptor);
        if (shape == ActionPlanShape.InvalidMixedShape)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidActionPlanShape,
                $"Action plan {planId} declares multiple behavior shapes. Use only one of behavior, primitive, or low-level steps.",
                actionPlanTemplateId: new ActionPlanTemplateId(planId),
                actionPlanId: new ActionPlanId(planId)));
        }

        if (shape == ActionPlanShape.InvalidEmptyBehaviorChain)
        {
            diagnostics.Add(ContentDiagnostic.Error(
                ContentDiagnosticCode.InvalidActionPlanShape,
                $"Action plan {planId} declares an empty behavior chain. Omit behavior or add at least one Action Step.",
                actionPlanTemplateId: new ActionPlanTemplateId(planId),
                actionPlanId: new ActionPlanId(planId)));
        }
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

    public void UpsertScenario(ScenarioDefinition scenario) =>
        Scenarios[scenario.ScenarioId] = ScenarioDefinitionDto.From(scenario);

    public ScenarioDefinition GetScenario(string scenarioId) =>
        Scenarios.TryGetValue(scenarioId, out var scenario)
            ? scenario.ToDefinition(scenarioId)
            : throw new KeyNotFoundException($"Scenario {scenarioId} does not exist.");

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

        public int? Bulk { get; set; }

        public int? Aperture { get; set; }

        public string? DefaultActionPlanId { get; set; }

        public Dictionary<string, PlanValueDescriptorDto>? DefaultPlanVariables { get; set; }

        public ActorActionStateDefaultsDto? ActionStateDefaults { get; set; }

        public List<EntityTargetingRuleDto>? TargetingRules { get; set; }

        public List<CarriedEntityTemplateDto>? CarriedEntities { get; set; }

        public static EntityTemplateDto From(EntityTemplate template) => new()
        {
            Name = template.Name,
            InventoryWidth = template.InventoryWidth,
            InventoryHeight = template.InventoryHeight,
            Bulk = template.Bulk,
            Aperture = template.Aperture,
            DefaultActionPlanId = template.DefaultActionPlanId?.Value,
            DefaultPlanVariables = template.DefaultPlanVariables?.ToDictionary(entry => entry.Key, entry => PlanValueDescriptorDto.From(entry.Value)),
            ActionStateDefaults = template.ActionStateDefaults is null ? null : ActorActionStateDefaultsDto.From(template.ActionStateDefaults),
            TargetingRules = template.TargetingRules?.Select(EntityTargetingRuleDto.From).ToList(),
            CarriedEntities = template.CarriedEntities?.Select(CarriedEntityTemplateDto.From).ToList()
        };
    }

    public sealed class EntityTargetingRuleDto
    {
        public int Slot { get; set; }

        public string? Hint { get; set; }

        public string? Label { get; set; }

        public string? TargetTemplateId { get; set; }

        public List<ActionPlanBehaviorStepKind>? TargetCapabilities { get; set; }

        public int Range { get; set; }

        public static EntityTargetingRuleDto From(EntityTargetingRule rule) => new()
        {
            Slot = rule.Slot,
            Hint = rule.Hint,
            Label = rule.Label,
            TargetTemplateId = rule.TargetTemplateId?.Value,
            TargetCapabilities = rule.TargetCapabilities.Count == 0 ? null : rule.TargetCapabilities.ToList(),
            Range = rule.Range
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

        public ActionPlanPrimitiveDescriptorDto? Primitive { get; set; }

        public ActionPlanBehaviorDescriptorDto? Behavior { get; set; }

        public List<ActionPlanStepDescriptorDto>? Steps { get; set; }

        public static ActionPlanDescriptorDto From(ActionPlanDescriptor descriptor) => new()
        {
            Id = descriptor.Id.Value,
            Primitive = descriptor.Primitive is null ? null : ActionPlanPrimitiveDescriptorDto.From(descriptor.Primitive),
            Behavior = descriptor.Behavior is null ? null : ActionPlanBehaviorDescriptorDto.From(descriptor.Behavior),
            Steps = descriptor.Steps.Select(ActionPlanStepDescriptorDto.From).ToList()
        };

        public ActionPlanDescriptor ToDescriptor(string fallbackId) =>
            new(
                new ActionPlanId(Id ?? fallbackId),
                (Steps ?? []).Select(step => step.ToDescriptor()).ToList(),
                Primitive?.ToDescriptor(),
                Behavior?.ToDescriptor());
    }

    public sealed class ScenarioDefinitionDto
    {
        public string? Name { get; set; }

        public string? ScenarioRootEntityTemplateId { get; set; }

        public string? PlayerEntityTemplateId { get; set; }

        public string? PlayerEntityId { get; set; }

        public GridCoordDto? PlayerStart { get; set; }

        public Dictionary<string, List<string>>? PlayerControls { get; set; }

        public static ScenarioDefinitionDto From(ScenarioDefinition scenario) => new()
        {
            Name = scenario.Name,
            ScenarioRootEntityTemplateId = scenario.ScenarioRootEntityTemplateId.Value,
            PlayerEntityTemplateId = scenario.PlayerEntityTemplateId.Value,
            PlayerEntityId = scenario.PlayerEntityId.Value,
            PlayerStart = GridCoordDto.From(scenario.PlayerStart),
            PlayerControls = scenario.PlayerControls.Count == 0
                ? null
                : scenario.PlayerControls.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value.Select(entityId => entityId.Value).ToList(),
                    StringComparer.Ordinal)
        };

        public ScenarioDefinition ToDefinition(string scenarioId) =>
            new(
                scenarioId,
                Name ?? scenarioId,
                new EntityTemplateId(ScenarioRootEntityTemplateId ?? string.Empty),
                new EntityTemplateId(PlayerEntityTemplateId ?? string.Empty),
                new EntityId(PlayerEntityId ?? string.Empty),
                ToCoord(PlayerStart),
                (PlayerControls ?? new Dictionary<string, List<string>>())
                    .ToDictionary(
                        entry => entry.Key,
                        entry => (IReadOnlyList<EntityId>)entry.Value.Select(entityId => new EntityId(entityId)).ToList(),
                        StringComparer.Ordinal));
    }

    public sealed class ActionPlanBehaviorDescriptorDto
    {
        public List<ActionPlanBehaviorStepDescriptorDto>? Steps { get; set; }

        public static ActionPlanBehaviorDescriptorDto From(ActionPlanBehaviorDescriptor descriptor) => new()
        {
            Steps = descriptor.Steps.Select(ActionPlanBehaviorStepDescriptorDto.From).ToList()
        };

        public ActionPlanBehaviorDescriptor ToDescriptor() =>
            new((Steps ?? []).Select(step => step.ToDescriptor()).ToList());
    }

    public sealed class ActionPlanBehaviorStepDescriptorDto
    {
        public ActionPlanBehaviorStepKind Kind { get; set; }

        public int? TargetSlot { get; set; }

        public string? TargetLabel { get; set; }

        public string? PlanId { get; set; }

        public string? DirectionMode { get; set; }

        public static ActionPlanBehaviorStepDescriptorDto From(ActionPlanBehaviorStepDescriptor descriptor) => new()
        {
            Kind = descriptor.Kind,
            TargetSlot = descriptor.TargetSlot,
            TargetLabel = descriptor.TargetLabel,
            PlanId = descriptor.PlanId?.Value,
            DirectionMode = descriptor.DirectionMode?.ToString()
        };

        public ActionPlanBehaviorStepDescriptor ToDescriptor() =>
            new(
                Kind,
                TargetSlot,
                TargetLabel,
                PlanId is null ? null : new ActionPlanId(PlanId),
                DirectionMode is { } mode ? Enum.Parse<ActionPlanMoveDirectionMode>(mode, ignoreCase: true) : null);
    }

    public sealed class ActionPlanPrimitiveDescriptorDto
    {
        public ActionPlanPrimitiveKind Kind { get; set; }

        public string? FallbackPlanId { get; set; }

        public static ActionPlanPrimitiveDescriptorDto From(ActionPlanPrimitiveDescriptor descriptor) => new()
        {
            Kind = descriptor.Kind,
            FallbackPlanId = descriptor.FallbackPlanId?.Value
        };

        public ActionPlanPrimitiveDescriptor ToDescriptor() =>
            new(Kind, FallbackPlanId is null ? null : new ActionPlanId(FallbackPlanId));
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

        public MovementTargetDescriptorDto? MovementTarget { get; set; }

        public MovementDestinationDescriptorDto? MovementDestination { get; set; }

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
            MovementTarget = descriptor.MovementTarget is null ? null : MovementTargetDescriptorDto.From(descriptor.MovementTarget),
            MovementDestination = descriptor.MovementDestination is null ? null : MovementDestinationDescriptorDto.From(descriptor.MovementDestination),
            ConsumesTurn = descriptor.ConsumesTurn,
            ContinuePlan = descriptor.ContinuePlan
        };

        public PlanEffectDescriptor ToDescriptor() =>
            Kind switch
            {
                PlanEffectKind.Teleport => PlanEffectDescriptor.Teleport(
                    MovementTarget?.ToDescriptor() ?? MovementTargetDescriptor.Entity(new EntityId(string.Empty)),
                    MovementDestination?.ToDescriptor() ?? MovementDestinationDescriptor.Plane(new PlaneCoord(new PlaneId(string.Empty), new GridCoord(0, 0)))),
                PlanEffectKind.Move => DirectionVariable is null
                    ? PlanEffectDescriptor.Move()
                    : PlanEffectDescriptor.Move(DirectionVariable),
                PlanEffectKind.Pickup => TargetVariable is null
                    ? PlanEffectDescriptor.Pickup(ToCoord(InventoryCoord))
                    : PlanEffectDescriptor.Pickup(TargetVariable, ToCoord(InventoryCoord)),
                PlanEffectKind.Drop => PlanEffectDescriptor.Drop(
                    MovementTarget?.ToDescriptor() ?? MovementTargetDescriptor.CarriedInventoryCoord(new GridCoord(0, 0)),
                    MovementDestination?.ToDescriptor() ?? MovementDestinationDescriptor.AdjacentToSelf(Direction.South)),
                PlanEffectKind.ReverseDirection => DirectionVariable is null
                    ? PlanEffectDescriptor.ReverseDirection(ConsumesTurn, ContinuePlan)
                    : PlanEffectDescriptor.ReverseDirection(DirectionVariable, ConsumesTurn, ContinuePlan),
                PlanEffectKind.Wait => PlanEffectDescriptor.Wait(),
                PlanEffectKind.SetVariable => PlanEffectDescriptor.SetVariable(VariableName ?? string.Empty, Value?.ToDescriptor().Materialize() ?? new DirectionPlanValue(Direction.West), ConsumesTurn, ContinuePlan),
                PlanEffectKind.CallPlan => PlanEffectDescriptor.CallPlan(new ActionPlanId(PlanId ?? string.Empty)),
                _ => throw new InvalidOperationException($"Unsupported plan effect kind {Kind}.")
            };
    }

    public sealed class MovementTargetDescriptorDto
    {
        public MovementTargetKind Kind { get; set; }

        public string? EntityId { get; set; }

        public GridCoordDto? InventoryCoord { get; set; }

        public static MovementTargetDescriptorDto From(MovementTargetDescriptor descriptor) => new()
        {
            Kind = descriptor.Kind,
            EntityId = descriptor.EntityId?.Value,
            InventoryCoord = descriptor.InventoryCoord is { } coord ? GridCoordDto.From(coord) : null
        };

        public MovementTargetDescriptor ToDescriptor() =>
            Kind switch
            {
                MovementTargetKind.Self => MovementTargetDescriptor.Self(),
                MovementTargetKind.CanonicalTarget => MovementTargetDescriptor.CanonicalTarget(),
                MovementTargetKind.Entity => MovementTargetDescriptor.Entity(new EntityId(EntityId ?? string.Empty)),
                MovementTargetKind.CarriedInventoryCoord => MovementTargetDescriptor.CarriedInventoryCoord(ToCoord(InventoryCoord)),
                _ => throw new InvalidOperationException($"Unsupported movement target kind {Kind}.")
            };
    }

    public sealed class MovementDestinationDescriptorDto
    {
        public MovementDestinationKind Kind { get; set; }

        public PlaneCoordDto? PlaneCoord { get; set; }

        public string? OwnerId { get; set; }

        public GridCoordDto? InventoryCoord { get; set; }

        public string? AnchorEntityId { get; set; }

        public Direction? Direction { get; set; }

        public static MovementDestinationDescriptorDto From(MovementDestinationDescriptor descriptor) => new()
        {
            Kind = descriptor.Kind,
            PlaneCoord = descriptor.PlaneCoord is { } coord ? PlaneCoordDto.From(coord) : null,
            OwnerId = descriptor.OwnerId?.Value,
            InventoryCoord = descriptor.InventoryCoord is { } inventoryCoord ? GridCoordDto.From(inventoryCoord) : null,
            AnchorEntityId = descriptor.AnchorEntityId?.Value,
            Direction = descriptor.Direction
        };

        public MovementDestinationDescriptor ToDescriptor() =>
            Kind switch
            {
                MovementDestinationKind.PlaneCoord => MovementDestinationDescriptor.Plane(ToPlaneCoord(PlaneCoord)),
                MovementDestinationKind.InventorySlot => MovementDestinationDescriptor.InventorySlot(new EntityId(OwnerId ?? string.Empty), ToCoord(InventoryCoord)),
                MovementDestinationKind.AdjacentToSelf => MovementDestinationDescriptor.AdjacentToSelf(Direction ?? global::GameGameGame.Core.Direction.South),
                MovementDestinationKind.AdjacentToEntity => MovementDestinationDescriptor.AdjacentToEntity(new EntityId(AnchorEntityId ?? string.Empty), Direction ?? global::GameGameGame.Core.Direction.South),
                MovementDestinationKind.AdjacentToCanonicalTarget => MovementDestinationDescriptor.AdjacentToCanonicalTarget(Direction ?? global::GameGameGame.Core.Direction.South),
                _ => throw new InvalidOperationException($"Unsupported movement destination kind {Kind}.")
            };
    }

    public sealed class PlaneCoordDto
    {
        public string? PlaneId { get; set; }

        public GridCoordDto? Coord { get; set; }

        public static PlaneCoordDto From(PlaneCoord coord) => new()
        {
            PlaneId = coord.PlaneId.Value,
            Coord = GridCoordDto.From(coord.Coord)
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

    private static PlaneCoord ToPlaneCoord(PlaneCoordDto? coord) =>
        coord is null ? new PlaneCoord(new PlaneId(string.Empty), new GridCoord(0, 0)) : new PlaneCoord(new PlaneId(coord.PlaneId ?? string.Empty), ToCoord(coord.Coord));
}
