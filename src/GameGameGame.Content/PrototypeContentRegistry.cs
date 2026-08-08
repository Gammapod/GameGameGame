using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed class PrototypeContentRegistry(
    IReadOnlyDictionary<EntityTemplateId, EntityTemplate> entityTemplates,
    IReadOnlyDictionary<ActionPlanTemplateId, ActionPlanDescriptor> actionPlanTemplates,
    IReadOnlyDictionary<EntityTemplateId, EntityPresentation> presentations,
    IReadOnlyDictionary<PresentationId, PresentationDefinition>? presentationCatalog = null,
    IReadOnlyDictionary<PaletteId, PaletteDefinition>? paletteCatalog = null,
    IReadOnlyDictionary<MergedInventoryLayerId, MergedInventoryLayerDefinition>? mergedInventoryLayers = null)
{
    private readonly Dictionary<EntityId, EntityTemplateId> _entityTemplateAssignments = [];

    public IReadOnlyDictionary<EntityTemplateId, EntityTemplate> EntityTemplates => entityTemplates;

    public IReadOnlyDictionary<ActionPlanTemplateId, ActionPlanDescriptor> ActionPlanDescriptors => actionPlanTemplates;

    public IReadOnlyDictionary<EntityTemplateId, EntityPresentation> Presentations => presentations;

    public IReadOnlyDictionary<PresentationId, PresentationDefinition> PresentationCatalog { get; } = presentationCatalog ?? BuiltInPresentationCatalog.Presentations;

    public IReadOnlyDictionary<PaletteId, PaletteDefinition> PaletteCatalog { get; } = paletteCatalog ?? BuiltInPresentationCatalog.Palettes;

    public IReadOnlyDictionary<MergedInventoryLayerId, MergedInventoryLayerDefinition> MergedInventoryLayers { get; } = mergedInventoryLayers ?? new Dictionary<MergedInventoryLayerId, MergedInventoryLayerDefinition>();

    public EntityTemplate GetEntityTemplate(EntityTemplateId id) => entityTemplates[id];

    public EntityPresentation GetPresentation(EntityTemplateId id) => presentations[id];

    public EntityPresentation GetPresentationForEntity(EntityId entityId) =>
        presentations[GetTemplateIdForEntity(entityId)];

    public EntityPresentation GetPresentationForEntity(WorldState world, EntityId entityId) =>
        presentations[GetTemplateIdForEntity(world, entityId)];

    public EntityTemplateId GetTemplateIdForEntity(EntityId entityId) =>
        _entityTemplateAssignments.TryGetValue(entityId, out var templateId)
            ? templateId
            : throw new InvalidOperationException($"No template assignment is registered for entity {entityId}.");

    public bool TryGetTemplateIdForEntity(EntityId entityId, out EntityTemplateId templateId) =>
        _entityTemplateAssignments.TryGetValue(entityId, out templateId);

    public EntityTemplateId GetTemplateIdForEntity(WorldState world, EntityId entityId) =>
        TryGetTemplateIdForEntity(world, entityId, out var templateId)
            ? templateId
            : throw new InvalidOperationException($"No template assignment is registered for entity {entityId}.");

    public bool TryGetTemplateIdForEntity(WorldState world, EntityId entityId, out EntityTemplateId templateId)
    {
        if (world.Entities.TryGetValue(entityId, out var entity) && !string.IsNullOrWhiteSpace(entity.TemplateId))
        {
            templateId = new EntityTemplateId(entity.TemplateId);
            if (entityTemplates.ContainsKey(templateId))
            {
                return true;
            }
        }

        if (_entityTemplateAssignments.TryGetValue(entityId, out templateId))
        {
            return true;
        }

        templateId = default;
        return false;
    }

    public ActionPlanDescriptor GetActionPlanDescriptor(ActionPlanTemplateId id) => actionPlanTemplates[id];

    public IEntityActionPlan CreateActionPlan(ActionPlanTemplateId id) =>
        CreateActionPlan(id, new Dictionary<string, PlanValueDescriptor>(), actionStateDefaults: null);

    public IEntityActionPlan CreateActionPlan(ActionPlanTemplateId id, IReadOnlyDictionary<string, PlanValueDescriptor> variables)
        => CreateActionPlan(id, variables, actionStateDefaults: null);

    public IEntityActionPlan CreateActionPlan(
        ActionPlanTemplateId id,
        IReadOnlyDictionary<string, PlanValueDescriptor> variables,
        ActorActionStateDefaults? actionStateDefaults)
    {
        var context = new ActionPlanContext();

        foreach (var (name, value) in variables)
        {
            context.Set(name, value.Materialize());
        }

        ApplyActionStateDefaults(context, actionStateDefaults);

        return new InterpretedEntityActionPlan(
            GetActionPlanDescriptor(id).Materialize(),
            context,
            BuildPlanRegistry());
    }

    public PrototypeContentRegistry WithEntityTemplate(EntityTemplateId id, EntityTemplate template)
    {
        var templates = new Dictionary<EntityTemplateId, EntityTemplate>(entityTemplates)
        {
            [id] = template
        };

        return new PrototypeContentRegistry(templates, actionPlanTemplates, presentations, PresentationCatalog, PaletteCatalog, MergedInventoryLayers);
    }

    public PrototypeContentRegistry WithPresentation(EntityTemplateId id, EntityPresentation presentation)
    {
        var updated = new Dictionary<EntityTemplateId, EntityPresentation>(presentations)
        {
            [id] = presentation
        };

        return new PrototypeContentRegistry(entityTemplates, actionPlanTemplates, updated, PresentationCatalog, PaletteCatalog, MergedInventoryLayers);
    }

    public PrototypeContentRegistry WithActionPlanDescriptor(ActionPlanTemplateId id, ActionPlanDescriptor descriptor)
    {
        var updated = new Dictionary<ActionPlanTemplateId, ActionPlanDescriptor>(actionPlanTemplates)
        {
            [id] = descriptor
        };

        return new PrototypeContentRegistry(entityTemplates, updated, presentations, PresentationCatalog, PaletteCatalog, MergedInventoryLayers);
    }

    public ContentValidationResult Validate()
    {
        var errors = new List<string>();
        var diagnostics = new List<ContentDiagnostic>();
        ValidateEntityTemplates(errors, diagnostics);
        ValidatePresentations(diagnostics);
        ValidateActionPlans(errors, diagnostics);
        ValidateMergedInventoryLayers(errors);

        diagnostics.AddRange(errors.Select(error => ContentDiagnostic.Error(ContentDiagnosticCode.General, error)));
        return new ContentValidationResult(diagnostics);
    }

    public EntitySpawnResult SpawnEntity(WorldState world, EntityTemplateId templateId, EntitySpawnOptions options)
        => new PrototypeEntitySpawner(
                entityTemplates,
                this.CreateActionPlan,
                RegisterTemplateAssignment)
            .SpawnEntity(world, templateId, options);

    private void RegisterTemplateAssignment(EntityId entityId, EntityTemplateId templateId)
    {
        _entityTemplateAssignments[entityId] = templateId;
    }

    private IReadOnlyDictionary<ActionPlanId, ActionPlanDefinition> BuildPlanRegistry() =>
        actionPlanTemplates.Values.ToDictionary(plan => plan.Id, plan => plan.Materialize());

    private void ValidateEntityTemplates(List<string> errors, List<ContentDiagnostic> diagnostics)
        => EntityTemplateValidator.Validate(entityTemplates, actionPlanTemplates, presentations, errors, diagnostics);

    private void ValidatePresentations(List<ContentDiagnostic> diagnostics)
    {
        foreach (var (templateId, presentation) in presentations)
        {
            if (!presentation.PresentationId.Value.StartsWith("legacy.glyph.", StringComparison.Ordinal)
                && !PresentationCatalog.ContainsKey(presentation.PresentationId))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.UnknownPresentationId,
                    $"Entity template {templateId} references unknown presentationId {presentation.PresentationId}.",
                    entityTemplateId: templateId));
            }

            if (!presentation.PaletteId.Value.StartsWith("legacy.color.", StringComparison.Ordinal)
                && !PaletteCatalog.ContainsKey(presentation.PaletteId))
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.UnknownPaletteId,
                    $"Entity template {templateId} references unknown paletteId {presentation.PaletteId}.",
                    entityTemplateId: templateId));
            }
        }
    }

    private void ValidateActionPlans(List<string> errors, List<ContentDiagnostic> diagnostics)
    {
        ActionPlanValidator.Validate(entityTemplates, actionPlanTemplates, errors, diagnostics);
        ValidateTemplateActionPlanVariables(errors, diagnostics);
    }

    private void ValidateTemplateActionPlanVariables(List<string> errors, List<ContentDiagnostic> diagnostics)
    {
        var plansById = actionPlanTemplates.Values.ToDictionary(plan => plan.Id);

        foreach (var (templateId, template) in entityTemplates)
        {
            if (template.DefaultActionPlanId is not { } actionPlanTemplateId
                || !actionPlanTemplates.TryGetValue(actionPlanTemplateId, out var plan))
            {
                continue;
            }

            var variables = template.DefaultPlanVariables is null
                ? new Dictionary<string, PlanValueKind>()
                : template.DefaultPlanVariables.ToDictionary(entry => entry.Key, entry => entry.Value.Kind);

            LegacyPlanVariableValidator.ValidatePlanVariables(
                errors,
                diagnostics,
                $"Entity template {templateId} ({template.Name}) action plan {plan.Id}",
                templateId,
                actionPlanTemplateId,
                plan,
                variables,
                plansById,
                []);

            ActionStateContractValidator.ValidateTemplatePlanSlots(
                diagnostics,
                templateId,
                template,
                actionPlanTemplateId,
                plan,
                plansById);
        }
    }

    private void ValidateMergedInventoryLayers(List<string> errors)
    {
        var entityTemplatesByAuthoredEntityId = CollectAuthoredEntityTemplates();
        var layerIdsByOwner = new Dictionary<EntityId, HashSet<MergedInventoryLayerId>>();
        foreach (var (layerId, layer) in MergedInventoryLayers)
        {
            if (layer.Spaces.Count < 1 || (layer.Spaces.Count < 2 && layer.Seams.Count == 0))
            {
                errors.Add($"Merged inventory layer {layerId} must declare at least 2 spaces, or one space with seams for self-connected topology; found {layer.Spaces.Count}.");
            }

            var ownersInLayer = new HashSet<EntityId>();
            var occupiedLayerCells = new Dictionary<GridCoord, LayerCell>();
            var templatesByOwner = new Dictionary<EntityId, EntityTemplate>();
            var hasInvalidSpace = false;
            foreach (var space in layer.Spaces)
            {
                if (!ownersInLayer.Add(space.OwnerId))
                {
                    errors.Add($"Merged inventory layer {layerId} references owner entity {space.OwnerId} more than once; duplicate source-space participation would make adjacency ambiguous.");
                    hasInvalidSpace = true;
                }

                if (!layerIdsByOwner.TryGetValue(space.OwnerId, out var ownerLayers))
                {
                    ownerLayers = [];
                    layerIdsByOwner[space.OwnerId] = ownerLayers;
                }

                ownerLayers.Add(layerId);

                if (!entityTemplatesByAuthoredEntityId.TryGetValue(space.OwnerId, out var templateId) || !entityTemplates.TryGetValue(templateId, out var template))
                {
                    errors.Add($"Merged inventory layer {layerId} references unknown owner entity {space.OwnerId}.");
                    hasInvalidSpace = true;
                    continue;
                }

                if (!template.HasUsableInventory())
                {
                    errors.Add($"Merged inventory layer {layerId} owner {space.OwnerId} template {templateId} has no usable inventory space.");
                    hasInvalidSpace = true;
                    continue;
                }

                templatesByOwner[space.OwnerId] = template;

                for (var y = 0; y < template.InventoryHeight; y++)
                {
                    for (var x = 0; x < template.InventoryWidth; x++)
                    {
                        var layerCoord = new GridCoord(space.Origin.X + x, space.Origin.Y + y);
                        if (occupiedLayerCells.TryGetValue(layerCoord, out var previousOwner))
                        {
                            errors.Add($"Merged inventory layer {layerId} has overlap at {layerCoord} between {previousOwner.OwnerId} and {space.OwnerId}.");
                        }
                        else
                        {
                            occupiedLayerCells[layerCoord] = new LayerCell(space.OwnerId, new GridCoord(x, y));
                        }
                    }
                }
            }

            IReadOnlyList<(LayerCell From, LayerCell To)> seamEdges = hasInvalidSpace
                ? []
                : ValidateMergedInventoryLayerSeams(errors, layerId, layer, ownersInLayer, templatesByOwner, occupiedLayerCells);
            if (!hasInvalidSpace && occupiedLayerCells.Count > 0 && !IsConnected(occupiedLayerCells, seamEdges))
            {
                errors.Add($"Merged inventory layer {layerId} is disconnected; placements and seams must form one connected layer.");
            }
        }

        foreach (var (ownerId, layerIds) in layerIdsByOwner.Where(entry => entry.Value.Count > 1))
        {
            errors.Add($"Merged inventory layer owner {ownerId} participates in more than one merged inventory layer ({string.Join(", ", layerIds.OrderBy(id => id.Value))}); prototype layer authoring requires one source inventory in at most one layer.");
        }
    }

    private static IReadOnlyList<(LayerCell From, LayerCell To)> ValidateMergedInventoryLayerSeams(
        List<string> errors,
        MergedInventoryLayerId layerId,
        MergedInventoryLayerDefinition layer,
        HashSet<EntityId> ownersInLayer,
        Dictionary<EntityId, EntityTemplate> templatesByOwner,
        Dictionary<GridCoord, LayerCell> occupiedLayerCells)
    {
        var result = new List<(LayerCell From, LayerCell To)>();
        var directionalNeighbors = new Dictionary<(LayerCell Cell, Direction Direction), LayerCell>();
        var layerCoordsByCell = occupiedLayerCells.ToDictionary(entry => entry.Value, entry => entry.Key);
        foreach (var seam in layer.Seams)
        {
            ValidateEndpoint(seam.First);
            ValidateEndpoint(seam.Second);
            if (!TryGetEdgeLength(seam.First, templatesByOwner, out var firstLength) ||
                !TryGetEdgeLength(seam.Second, templatesByOwner, out var secondLength))
            {
                continue;
            }

            if (firstLength != secondLength)
            {
                errors.Add($"Merged inventory layer {layerId} seam {Format(seam)} has edge length mismatch: {seam.First.OwnerId}.{seam.First.Edge} length {firstLength}, {seam.Second.OwnerId}.{seam.Second.Edge} length {secondLength}.");
                continue;
            }

            for (var index = 0; index < firstLength; index++)
            {
                var firstCell = new LayerCell(seam.First.OwnerId, EdgeCoord(index, templatesByOwner[seam.First.OwnerId], seam.First.Edge));
                var secondCell = new LayerCell(seam.Second.OwnerId, EdgeCoord(index, templatesByOwner[seam.Second.OwnerId], seam.Second.Edge));
                AddDirectional(firstCell, seam.First.Edge, secondCell, seam);
                AddDirectional(secondCell, seam.Second.Edge, firstCell, seam);
                result.Add((firstCell, secondCell));
                result.Add((secondCell, firstCell));
            }
        }

        return result;

        void ValidateEndpoint(MergedInventoryLayerEdge edge)
        {
            if (!IsCardinal(edge.Edge))
            {
                errors.Add($"Merged inventory layer {layerId} seam endpoint {edge.OwnerId}.{edge.Edge} is invalid; seams currently support cardinal edges only.");
            }

            if (!ownersInLayer.Contains(edge.OwnerId))
            {
                errors.Add($"Merged inventory layer {layerId} seam endpoint references owner {edge.OwnerId}, but that owner does not contribute to the layer.");
            }
        }

        void AddDirectional(LayerCell from, Direction direction, LayerCell to, MergedInventoryLayerSeam seam)
        {
            var key = (from, direction);
            if (layerCoordsByCell.TryGetValue(from, out var fromLayerCoord) &&
                occupiedLayerCells.TryGetValue(fromLayerCoord.Offset(direction), out var euclideanNeighbor) &&
                euclideanNeighbor != to)
            {
                errors.Add($"Merged inventory layer {layerId} has directional conflict at {from.OwnerId}{from.Coord}.{direction}: Euclidean placement neighbor {euclideanNeighbor.OwnerId}{euclideanNeighbor.Coord} conflicts with seam neighbor {to.OwnerId}{to.Coord}. Conflicting seam: {Format(seam)}.");
                return;
            }

            if (directionalNeighbors.TryGetValue(key, out var existing) && existing != to)
            {
                errors.Add($"Merged inventory layer {layerId} has directional conflict at {from.OwnerId}{from.Coord}.{direction}: both {existing.OwnerId}{existing.Coord} and {to.OwnerId}{to.Coord} are seam neighbors. Conflicting seam: {Format(seam)}.");
                return;
            }

            directionalNeighbors[key] = to;
        }
    }

    private Dictionary<EntityId, EntityTemplateId> CollectAuthoredEntityTemplates()
    {
        var result = new Dictionary<EntityId, EntityTemplateId>();
        var visited = new HashSet<EntityTemplateId>();
        foreach (var templateId in entityTemplates.Keys)
        {
            Collect(templateId, visited);
        }

        return result;

        void Collect(EntityTemplateId templateId, HashSet<EntityTemplateId> ancestry)
        {
            if (!entityTemplates.TryGetValue(templateId, out var template) || template.CarriedEntities is null || !ancestry.Add(templateId))
            {
                return;
            }

            foreach (var carried in template.CarriedEntities)
            {
                if (carried.TemplateId is { } carriedTemplateId)
                {
                    result[carried.EntityId] = carriedTemplateId;
                    Collect(carriedTemplateId, ancestry);
                }
            }

            ancestry.Remove(templateId);
        }
    }

    private static bool IsConnected(Dictionary<GridCoord, LayerCell> cellsByLayerCoord, IReadOnlyList<(LayerCell From, LayerCell To)> seamEdges)
    {
        var layerCoordsByCell = cellsByLayerCoord.ToDictionary(entry => entry.Value, entry => entry.Key);
        var cells = layerCoordsByCell.Keys.ToHashSet();
        var seamNeighbors = seamEdges
            .GroupBy(edge => edge.From)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.To).Where(cells.Contains).ToList());
        var visited = new HashSet<LayerCell>();
        var queue = new Queue<LayerCell>();
        var start = cells.First();
        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentLayerCoord = layerCoordsByCell[current];
            foreach (var direction in DirectionMath.AllDirections)
            {
                var nextLayerCoord = currentLayerCoord.Offset(direction);
                if (cellsByLayerCoord.TryGetValue(nextLayerCoord, out var next) && visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }

            foreach (var next in seamNeighbors.GetValueOrDefault(current) ?? [])
            {
                if (visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return visited.Count == cells.Count;
    }

    private static bool TryGetEdgeLength(MergedInventoryLayerEdge edge, Dictionary<EntityId, EntityTemplate> templatesByOwner, out int length)
    {
        length = 0;
        if (!templatesByOwner.TryGetValue(edge.OwnerId, out var template) || !IsCardinal(edge.Edge))
        {
            return false;
        }

        length = edge.Edge is Direction.North or Direction.South ? template.InventoryWidth : template.InventoryHeight;
        return true;
    }

    private static GridCoord EdgeCoord(int index, EntityTemplate template, Direction edge) => edge switch
    {
        Direction.North => new GridCoord(index, 0),
        Direction.East => new GridCoord(template.InventoryWidth - 1, index),
        Direction.South => new GridCoord(index, template.InventoryHeight - 1),
        Direction.West => new GridCoord(0, index),
        _ => new GridCoord(0, 0)
    };

    private static bool IsCardinal(Direction direction) =>
        direction is Direction.North or Direction.East or Direction.South or Direction.West;

    private static string Format(MergedInventoryLayerSeam seam) =>
        $"{seam.First.OwnerId}.{seam.First.Edge}<->{seam.Second.OwnerId}.{seam.Second.Edge}";

    private sealed record LayerCell(EntityId OwnerId, GridCoord Coord);
    private static void ApplyActionStateDefaults(ActionPlanContext context, ActorActionStateDefaults? defaults)
    {
        if (defaults?.Facing is { } facing)
        {
            context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(facing));
        }

        if (defaults?.Target is { } target)
        {
            context.Set(ActionPlanSlot.Target, new EntityPlanValue(target));
        }
    }

}

internal static class EntityTemplateExtensions
{
    public static bool HasUsableInventory(this EntityTemplate template) => template.InventoryWidth > 0 && template.InventoryHeight > 0;
}
