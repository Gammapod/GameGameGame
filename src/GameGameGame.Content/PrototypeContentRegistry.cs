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
        foreach (var (layerId, layer) in MergedInventoryLayers)
        {
            if (layer.Spaces.Count < 2)
            {
                errors.Add($"Merged inventory layer {layerId} must declare at least 2 spaces; found {layer.Spaces.Count}.");
            }

            var occupiedLayerCells = new Dictionary<GridCoord, EntityId>();
            var hasInvalidSpace = false;
            foreach (var space in layer.Spaces)
            {
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

                for (var y = 0; y < template.InventoryHeight; y++)
                {
                    for (var x = 0; x < template.InventoryWidth; x++)
                    {
                        var layerCoord = new GridCoord(space.Origin.X + x, space.Origin.Y + y);
                        if (occupiedLayerCells.TryGetValue(layerCoord, out var previousOwner))
                        {
                            errors.Add($"Merged inventory layer {layerId} has overlap at {layerCoord} between {previousOwner} and {space.OwnerId}.");
                        }
                        else
                        {
                            occupiedLayerCells[layerCoord] = space.OwnerId;
                        }
                    }
                }
            }

            if (!hasInvalidSpace && occupiedLayerCells.Count > 0 && !IsConnected(occupiedLayerCells.Keys.ToHashSet()))
            {
                errors.Add($"Merged inventory layer {layerId} is disconnected; MVP placements must form one connected layer.");
            }
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

    private static bool IsConnected(HashSet<GridCoord> cells)
    {
        var visited = new HashSet<GridCoord>();
        var queue = new Queue<GridCoord>();
        var start = cells.First();
        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var direction in DirectionMath.AllDirections)
            {
                var next = current.Offset(direction);
                if (cells.Contains(next) && visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return visited.Count == cells.Count;
    }
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
