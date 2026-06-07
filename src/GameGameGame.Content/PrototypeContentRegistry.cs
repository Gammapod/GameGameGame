using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed class PrototypeContentRegistry(
    IReadOnlyDictionary<EntityTemplateId, EntityTemplate> entityTemplates,
    IReadOnlyDictionary<ActionPlanTemplateId, ActionPlanDescriptor> actionPlanTemplates,
    IReadOnlyDictionary<EntityTemplateId, EntityPresentation> presentations)
{
    private readonly Dictionary<EntityId, EntityTemplateId> _entityTemplateAssignments = [];

    public EntityTemplate GetEntityTemplate(EntityTemplateId id) => entityTemplates[id];

    public EntityPresentation GetPresentation(EntityTemplateId id) => presentations[id];

    public EntityPresentation GetPresentationForEntity(EntityId entityId) =>
        presentations[GetTemplateIdForEntity(entityId)];

    public EntityTemplateId GetTemplateIdForEntity(EntityId entityId) =>
        _entityTemplateAssignments.TryGetValue(entityId, out var templateId)
            ? templateId
            : throw new InvalidOperationException($"No template assignment is registered for entity {entityId}.");

    public ActionPlanDescriptor GetActionPlanDescriptor(ActionPlanTemplateId id) => actionPlanTemplates[id];

    public IEntityActionPlan CreateActionPlan(ActionPlanTemplateId id) =>
        CreateActionPlan(id, new Dictionary<string, PlanValueDescriptor>());

    public IEntityActionPlan CreateActionPlan(ActionPlanTemplateId id, IReadOnlyDictionary<string, PlanValueDescriptor> variables)
    {
        var context = new ActionPlanContext();

        foreach (var (name, value) in variables)
        {
            context.Set(name, value.Materialize());
        }

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
        ValidateEntityTemplates(errors);
        ValidateActionPlans(errors);

        return new ContentValidationResult(errors);
    }

    public EntitySpawnResult SpawnEntity(WorldState world, EntityTemplateId templateId, EntitySpawnOptions options)
    {
        var template = GetEntityTemplate(templateId);

        var result = SpawnEntity(world, template, options);
        RegisterTemplateAssignment(result.EntityId, templateId);

        return result;
    }

    private EntitySpawnResult SpawnEntity(WorldState world, EntityTemplate template, EntitySpawnOptions options)
    {
        template = options.ModifyTemplate?.Invoke(template) ?? template;

        var defaultActionPlanId = options.ActionPlanOverrideId ?? template.DefaultActionPlanId;

        var variables = MergePlanVariables(template.DefaultPlanVariables, options.PlanVariableOverrides);

        var carriedEntities = template.CarriedEntities;
        var parentResult = PrototypeContent.SpawnEntity(
            world,
            template with { CarriedEntities = null },
            options with { ModifyTemplate = null });
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>(parentResult.ActionPlans);
        IEntityActionPlan? actionPlan = null;

        if (defaultActionPlanId is { } actionPlanTemplateId)
        {
            actionPlan = CreateActionPlan(actionPlanTemplateId, variables);
            actionPlans[parentResult.EntityId] = actionPlan;
        }

        if (carriedEntities is null || carriedEntities.Count == 0)
        {
            return new EntitySpawnResult(parentResult.EntityId, actionPlan, actionPlans);
        }

        if (world.GetInventoryPlaneId(options.EntityId) is not { } inventoryPlaneId)
        {
            throw new InvalidOperationException($"Cannot place carried entities for {options.EntityId}: template has no usable inventory.");
        }

        foreach (var carried in carriedEntities)
        {
            var carriedOptions = new EntitySpawnOptions(
                carried.EntityId,
                new PlaneCoord(inventoryPlaneId, carried.Coord));
            var carriedResult = carried.TemplateId is { } templateId
                ? SpawnEntity(world, templateId, carriedOptions)
                : carried.Template is { } carriedTemplate
                    ? SpawnEntity(world, carriedTemplate, carriedOptions)
                    : throw new InvalidOperationException($"Carried entity {carried.EntityId} has no template or template ID.");

            foreach (var (entityId, carriedActionPlan) in carriedResult.ActionPlans)
            {
                actionPlans[entityId] = carriedActionPlan;
            }
        }

        return new EntitySpawnResult(parentResult.EntityId, actionPlan, actionPlans);
    }

    private void RegisterTemplateAssignment(EntityId entityId, EntityTemplateId templateId)
    {
        _entityTemplateAssignments[entityId] = templateId;
    }

    private IReadOnlyDictionary<ActionPlanId, ActionPlanDefinition> BuildPlanRegistry() =>
        actionPlanTemplates.Values.ToDictionary(plan => plan.Id, plan => plan.Materialize());

    private void ValidateEntityTemplates(List<string> errors)
    {
        foreach (var (templateId, template) in entityTemplates)
        {
            if (!presentations.ContainsKey(templateId))
            {
                errors.Add($"Entity template {templateId} ({template.Name}) has no presentation.");
            }

            ValidateActionPlanTemplateReference(errors, templateId, template, template.DefaultActionPlanId, nameof(template.DefaultActionPlanId));

            if (template.DefaultPlanVariables is not null)
            {
                foreach (var (name, value) in template.DefaultPlanVariables)
                {
                    TryValidate(errors, $"Entity template {templateId} ({template.Name}) default variable {name}", () => value.Materialize());
                }
            }

            if (template.CarriedEntities is null)
            {
                continue;
            }

            foreach (var carried in template.CarriedEntities)
            {
                if (carried.TemplateId is { } carriedTemplateId && !entityTemplates.ContainsKey(carriedTemplateId))
                {
                    errors.Add($"Entity template {templateId} ({template.Name}) carries {carried.EntityId} with missing template {carriedTemplateId}.");
                }
            }
        }
    }

    private void ValidateActionPlanTemplateReference(
        List<string> errors,
        EntityTemplateId templateId,
        EntityTemplate template,
        ActionPlanTemplateId? actionPlanTemplateId,
        string fieldName)
    {
        if (actionPlanTemplateId is { } id && !actionPlanTemplates.ContainsKey(id))
        {
            errors.Add($"Entity template {templateId} ({template.Name}) references missing {fieldName} {id}.");
        }
    }

    private void ValidateActionPlans(List<string> errors)
    {
        var planIds = actionPlanTemplates.Values.Select(plan => plan.Id).ToHashSet();

        foreach (var (templateId, descriptor) in actionPlanTemplates)
        {
            TryValidate(errors, $"Action plan template {templateId} ({descriptor.Id})", () => descriptor.Materialize());

            foreach (var step in descriptor.Steps)
            {
                ValidateCalledPlan(errors, descriptor, step, step.OnSuccess);
                ValidateCalledPlan(errors, descriptor, step, step.OnFailure);
            }
        }

        void ValidateCalledPlan(
            List<string> validationErrors,
            ActionPlanDescriptor descriptor,
            ActionPlanStepDescriptor step,
            PlanEffectDescriptor? effect)
        {
            if (effect?.Kind == PlanEffectKind.CallPlan && effect.PlanId is { } planId && !planIds.Contains(planId))
            {
                validationErrors.Add($"Action plan {descriptor.Id} step {step.Label} calls missing plan {planId}.");
            }
        }
    }

    private static void TryValidate(List<string> errors, string subject, Action materialize)
    {
        try
        {
            materialize();
        }
        catch (Exception ex)
        {
            errors.Add($"{subject} is invalid: {ex.Message}");
        }
    }

    private static IReadOnlyDictionary<string, PlanValueDescriptor> MergePlanVariables(
        IReadOnlyDictionary<string, PlanValueDescriptor>? defaults,
        IReadOnlyDictionary<string, PlanValueDescriptor>? overrides)
    {
        var merged = defaults is null
            ? new Dictionary<string, PlanValueDescriptor>()
            : new Dictionary<string, PlanValueDescriptor>(defaults);

        if (overrides is not null)
        {
            foreach (var (name, value) in overrides)
            {
                merged[name] = value;
            }
        }

        return merged;
    }
}
