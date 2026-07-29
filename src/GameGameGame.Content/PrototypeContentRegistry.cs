using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed class PrototypeContentRegistry(
    IReadOnlyDictionary<EntityTemplateId, EntityTemplate> entityTemplates,
    IReadOnlyDictionary<ActionPlanTemplateId, ActionPlanDescriptor> actionPlanTemplates,
    IReadOnlyDictionary<EntityTemplateId, EntityPresentation> presentations)
{
    private readonly Dictionary<EntityId, EntityTemplateId> _entityTemplateAssignments = [];

    public IReadOnlyDictionary<EntityTemplateId, EntityTemplate> EntityTemplates => entityTemplates;

    public IReadOnlyDictionary<ActionPlanTemplateId, ActionPlanDescriptor> ActionPlanDescriptors => actionPlanTemplates;

    public IReadOnlyDictionary<EntityTemplateId, EntityPresentation> Presentations => presentations;

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

        return new PrototypeContentRegistry(templates, actionPlanTemplates, presentations);
    }

    public PrototypeContentRegistry WithPresentation(EntityTemplateId id, EntityPresentation presentation)
    {
        var updated = new Dictionary<EntityTemplateId, EntityPresentation>(presentations)
        {
            [id] = presentation
        };

        return new PrototypeContentRegistry(entityTemplates, actionPlanTemplates, updated);
    }

    public PrototypeContentRegistry WithActionPlanDescriptor(ActionPlanTemplateId id, ActionPlanDescriptor descriptor)
    {
        var updated = new Dictionary<ActionPlanTemplateId, ActionPlanDescriptor>(actionPlanTemplates)
        {
            [id] = descriptor
        };

        return new PrototypeContentRegistry(entityTemplates, updated, presentations);
    }

    public ContentValidationResult Validate()
    {
        var errors = new List<string>();
        var diagnostics = new List<ContentDiagnostic>();
        ValidateEntityTemplates(errors, diagnostics);
        ValidateActionPlans(errors, diagnostics);

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
