using GameGameGame.Core;

namespace GameGameGame.Content;

internal sealed class PrototypeEntitySpawner(
    IReadOnlyDictionary<EntityTemplateId, EntityTemplate> entityTemplates,
    Func<ActionPlanTemplateId, IReadOnlyDictionary<string, PlanValueDescriptor>, ActorActionStateDefaults?, IEntityActionPlan> createActionPlan,
    Action<EntityId, EntityTemplateId> registerTemplateAssignment)
{
    public EntitySpawnResult SpawnEntity(WorldState world, EntityTemplateId templateId, EntitySpawnOptions options)
    {
        var template = entityTemplates[templateId];

        var result = SpawnEntity(world, template, options);
        registerTemplateAssignment(result.EntityId, templateId);

        return result;
    }

    private EntitySpawnResult SpawnEntity(WorldState world, EntityTemplate template, EntitySpawnOptions options)
    {
        template = options.ModifyTemplate?.Invoke(template) ?? template;

        var defaultActionPlanId = options.ActionPlanOverrideId ?? template.DefaultActionPlanId;

        var variables = MergePlanVariables(template.DefaultPlanVariables, options.PlanVariableOverrides);
        var actionStateDefaults = MergeActionStateDefaults(template.ActionStateDefaults, options.ActionStateOverrides);

        var carriedEntities = template.CarriedEntities;
        var parentResult = PrototypeContent.SpawnEntity(
            world,
            template with { CarriedEntities = null },
            options with { ModifyTemplate = null });
        ApplyActionStateDefaults(world, parentResult.EntityId, actionStateDefaults);
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>(parentResult.ActionPlans);
        IEntityActionPlan? actionPlan = null;

        if (defaultActionPlanId is { } actionPlanTemplateId)
        {
            actionPlan = createActionPlan(actionPlanTemplateId, variables, actionStateDefaults);
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
            var carriedResult = carried.TemplateId is { } carriedTemplateId
                ? SpawnEntity(world, carriedTemplateId, carriedOptions)
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

    private static ActorActionStateDefaults? MergeActionStateDefaults(
        ActorActionStateDefaults? defaults,
        ActorActionStateDefaults? overrides)
    {
        if (defaults is null)
        {
            return overrides;
        }

        if (overrides is null)
        {
            return defaults;
        }

        return new ActorActionStateDefaults(
            overrides.Facing ?? defaults.Facing,
            overrides.Target ?? defaults.Target);
    }

    private static void ApplyActionStateDefaults(WorldState world, EntityId entityId, ActorActionStateDefaults? defaults)
    {
        if (defaults?.Facing is { } facing)
        {
            world.SetActionFacing(entityId, facing);
        }

        if (defaults?.Target is { } target)
        {
            world.SetActionTarget(entityId, target);
        }
    }
}
