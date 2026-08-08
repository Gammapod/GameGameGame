using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed partial class EditableContentDocument
{
    public Dictionary<string, EntityTemplateDto> EntityTemplates { get; set; } = [];

    public Dictionary<string, EntityPresentationDto> Presentations { get; set; } = [];

    public Dictionary<string, PresentationDefinitionDto>? PresentationCatalog { get; set; }

    public Dictionary<string, PaletteDefinitionDto>? Palettes { get; set; }

    public Dictionary<string, ActionPlanDescriptorDto> ActionPlans { get; set; } = [];

    public Dictionary<string, ScenarioDefinitionDto> Scenarios { get; set; } = [];

    public Dictionary<string, MergedInventoryLayerDto> MergedLayers { get; set; } = [];

    public sealed class MergedInventoryLayerDto
    {
        public List<MergedInventorySpaceContributionDto>? Spaces { get; set; }

        public List<MergedInventoryLayerSeamDto>? Seams { get; set; }

        public List<MergedInventoryLayerCellLinkDto>? CellLinks { get; set; }

        public List<MergedInventoryLayerJoinDto>? Joins { get; set; }

        public bool AllowLayoutOverlap { get; set; }
    }

    public sealed class MergedInventorySpaceContributionDto
    {
        public string? Owner { get; set; }

        public GridCoordDto? Origin { get; set; }
    }

    public sealed class MergedInventoryLayerSeamDto
    {
        public MergedInventoryLayerEdgeDto? First { get; set; }

        public MergedInventoryLayerEdgeDto? Second { get; set; }
    }

    public sealed class MergedInventoryLayerEdgeDto
    {
        public string? Owner { get; set; }

        public Direction? Edge { get; set; }
    }

    public sealed class MergedInventoryLayerCellLinkDto
    {
        public MergedInventoryLayerCellEndpointDto? First { get; set; }

        public Direction? FirstDirection { get; set; }

        public MergedInventoryLayerCellEndpointDto? Second { get; set; }

        public Direction? SecondDirection { get; set; }
    }

    public sealed class MergedInventoryLayerCellEndpointDto
    {
        public string? Owner { get; set; }

        public GridCoordDto? Coord { get; set; }
    }

    public sealed class MergedInventoryLayerJoinDto
    {
        public MergedInventoryLayerEdgeDto? From { get; set; }

        public MergedInventoryLayerEdgeDto? To { get; set; }

        public MergedInventoryLayerJoinAlignment? Align { get; set; }

        public int? Offset { get; set; }

        public int? Length { get; set; }
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

        public EntityEnterPolicy? EnterPolicy { get; set; }

        public EntityExitPolicy? ExitPolicy { get; set; }

        public EntityTopologyPolicy? TopologyPolicy { get; set; }

        public string? DefaultActionPlanId { get; set; }

        public Dictionary<string, PlanValueDescriptorDto>? DefaultPlanVariables { get; set; }

        public ActorActionStateDefaultsDto? ActionStateDefaults { get; set; }

        public EntityTargetingProfileDto? Targeting { get; set; }

        public List<EntityTargetingRuleDto>? TargetingRules { get; set; }

        public List<CarriedEntityTemplateDto>? CarriedEntities { get; set; }

        public static EntityTemplateDto From(EntityTemplate template) => new()
        {
            Name = template.Name,
            InventoryWidth = template.InventoryWidth,
            InventoryHeight = template.InventoryHeight,
            Bulk = template.Bulk,
            Aperture = template.Aperture,
            EnterPolicy = template.EnterPolicy,
            ExitPolicy = template.ExitPolicy,
            TopologyPolicy = template.TopologyPolicy == EntityTopologyPolicy.None ? null : template.TopologyPolicy,
            DefaultActionPlanId = template.DefaultActionPlanId?.Value,
            DefaultPlanVariables = template.DefaultPlanVariables?.ToDictionary(entry => entry.Key, entry => PlanValueDescriptorDto.From(entry.Value)),
            ActionStateDefaults = template.ActionStateDefaults is null ? null : ActorActionStateDefaultsDto.From(template.ActionStateDefaults),
            Targeting = template.Targeting is null ? null : EntityTargetingProfileDto.From(template.Targeting),
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

        public TargetingLocalityDto? Locality { get; set; }

        public static EntityTargetingRuleDto From(EntityTargetingRule rule) => new()
        {
            Slot = rule.Slot,
            Hint = rule.Hint,
            Label = rule.Label,
            TargetTemplateId = rule.TargetTemplateId?.Value,
            TargetCapabilities = rule.TargetCapabilities.Count == 0 ? null : rule.TargetCapabilities.ToList(),
            Range = rule.Range,
            Locality = rule.Locality is null ? null : TargetingLocalityDto.From(rule.Locality)
        };
    }

    public sealed class EntityTargetingProfileDto
    {
        public int Range { get; set; }

        public TargetingLocalityDto? DefaultLocality { get; set; }

        public List<EntityTargetingRuleDto>? Rules { get; set; }

        public static EntityTargetingProfileDto From(EntityTargetingProfile profile) => new()
        {
            Range = profile.Range,
            DefaultLocality = profile.DefaultLocality is null ? null : TargetingLocalityDto.From(profile.DefaultLocality),
            Rules = profile.Rules.Count == 0 ? null : profile.Rules.Select(EntityTargetingRuleDto.From).ToList()
        };
    }

    public sealed class TargetingLocalityDto
    {
        public List<TargetingLocalityOrigin>? Origins { get; set; }

        public static TargetingLocalityDto From(TargetingLocalityQuery locality) => new()
        {
            Origins = locality.Origins.ToList()
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

        public EntityController? Controller { get; set; }

        public static CarriedEntityTemplateDto From(CarriedEntityTemplate carried) => new()
        {
            EntityId = carried.EntityId.Value,
            TemplateId = carried.TemplateId?.Value,
            Coord = GridCoordDto.From(carried.Coord),
            Controller = carried.Controller
        };
    }

    public sealed class EntityPresentationDto
    {
        public string? PresentationId { get; set; }

        public string? PaletteId { get; set; }

        public string? Glyph { get; set; }

        public PresentationColor Color { get; set; }

        public static EntityPresentationDto From(EntityPresentation presentation) => new()
        {
            PresentationId = presentation.PresentationId.Value,
            PaletteId = presentation.PaletteId.Value,
            Glyph = presentation.Glyph.ToString(),
            Color = presentation.Color
        };
    }

    public sealed class PresentationDefinitionDto
    {
        public string? Name { get; set; }

        public string? FallbackText { get; set; }

        public List<string>? Tags { get; set; }
    }

    public sealed class PaletteDefinitionDto
    {
        public string? Name { get; set; }

        public Dictionary<string, PresentationColor>? Roles { get; set; }
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
            PlayerEntityTemplateId = scenario.PlayerEntityTemplateId?.Value,
            PlayerEntityId = scenario.PlayerEntityId?.Value,
            PlayerStart = scenario.PlayerStart is { } start ? GridCoordDto.From(start) : null,
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
                string.IsNullOrWhiteSpace(PlayerEntityTemplateId) ? null : new EntityTemplateId(PlayerEntityTemplateId),
                string.IsNullOrWhiteSpace(PlayerEntityId) ? null : new EntityId(PlayerEntityId),
                ToNullableCoord(PlayerStart),
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

        public static ActionPlanBehaviorStepDescriptorDto From(ActionPlanBehaviorStepDescriptor descriptor) => new()
        {
            Kind = descriptor.Kind,
            TargetSlot = descriptor.TargetSlot,
            TargetLabel = descriptor.TargetLabel,
            TargetSelf = descriptor.TargetSelf,
            PlanId = descriptor.PlanId?.Value,
            DirectionMode = descriptor.DirectionMode?.ToString(),
            TransferDirection = descriptor.TransferDirection?.ToString(),
            TemplateId = descriptor.TemplateId,
            CreatePlacement = descriptor.CreatePlacement?.ToString(),
            PathMode = descriptor.PathMode?.ToString(),
            DesiredDistance = descriptor.DesiredDistance,
            OrbitDirection = descriptor.OrbitDirection?.ToString(),
            Costs = descriptor.Costs.Count == 0 ? null : descriptor.Costs.Select(ActionStepCostDescriptorDto.From).ToList()
        };

        public ActionPlanBehaviorStepDescriptor ToDescriptor() =>
            new(
                Kind,
                TargetSlot,
                TargetLabel,
                TargetSelf,
                PlanId is null ? null : new ActionPlanId(PlanId),
                DirectionMode is { } mode ? Enum.Parse<ActionPlanMoveDirectionMode>(mode, ignoreCase: true) : null,
                TransferDirection is { } transferDirection ? Enum.Parse<TransferDirection>(transferDirection, ignoreCase: true) : null,
                TemplateId,
                CreatePlacement is { } placement ? Enum.Parse<CreateEntityPlacement>(placement, ignoreCase: true) : null,
                PathMode is { } pathMode ? Enum.Parse<ActionPlanTargetPathMode>(pathMode, ignoreCase: true) : null,
                DesiredDistance,
                OrbitDirection is { } orbitDirection ? Enum.Parse<ActionPlanOrbitDirection>(orbitDirection, ignoreCase: true) : null)
            {
                Costs = (Costs ?? [])
                    .Select(cost => new ActionStepCostDescriptor(cost.TemplateId ?? string.Empty, cost.Quantity))
                    .ToList()
            };
    }

    public sealed class ActionStepCostDescriptorDto
    {
        public string? TemplateId { get; set; }

        public int Quantity { get; set; }

        public static ActionStepCostDescriptorDto From(ActionStepCostDescriptor descriptor) => new()
        {
            TemplateId = descriptor.TemplateId,
            Quantity = descriptor.Quantity
        };
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

    private static GridCoord? ToNullableCoord(GridCoordDto? coord) =>
        coord is null ? null : new GridCoord(coord.X, coord.Y);

    private static PlaneCoord ToPlaneCoord(PlaneCoordDto? coord) =>
        coord is null ? new PlaneCoord(new PlaneId(string.Empty), new GridCoord(0, 0)) : new PlaneCoord(new PlaneId(coord.PlaneId ?? string.Empty), ToCoord(coord.Coord));
}
