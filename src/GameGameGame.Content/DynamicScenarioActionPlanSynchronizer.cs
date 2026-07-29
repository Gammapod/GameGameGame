using GameGameGame.Core;

namespace GameGameGame.Content;

/// <summary>
/// Refreshes the runtime action-plan map for scenario entities whose template-backed default plans
/// are created, changed, cleared, or removed during simulation.
/// </summary>
public sealed class DynamicScenarioActionPlanSynchronizer
{
    private readonly Dictionary<EntityId, ActionPlanId> _synchronizedDefaultPlanIds = [];

    public IReadOnlyDictionary<EntityId, IEntityActionPlan> Synchronize(
        WorldState world,
        PrototypeContentRegistry registry,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans)
    {
        var synchronized = new Dictionary<EntityId, IEntityActionPlan>(actionPlans);
        SynchronizeInPlace(world, registry, synchronized);
        return synchronized;
    }

    public void SynchronizeInPlace(
        WorldState world,
        PrototypeContentRegistry registry,
        IDictionary<EntityId, IEntityActionPlan> actionPlans)
    {
        foreach (var entityId in world.Entities.Keys)
        {
            if (world.GetDefaultActionPlanId(entityId) is not { } planId)
            {
                actionPlans.Remove(entityId);
                _synchronizedDefaultPlanIds.Remove(entityId);
                continue;
            }

            if (actionPlans.ContainsKey(entityId)
                && _synchronizedDefaultPlanIds.TryGetValue(entityId, out var existingPlanId)
                && existingPlanId == planId)
            {
                continue;
            }

            var templatePlanId = new ActionPlanTemplateId(planId.Value);
            if (!registry.ActionPlanDescriptors.ContainsKey(templatePlanId))
            {
                continue;
            }

            actionPlans[entityId] = registry.CreateActionPlan(templatePlanId);
            _synchronizedDefaultPlanIds[entityId] = planId;
        }

        foreach (var entityId in actionPlans.Keys.ToList())
        {
            if (!world.Entities.ContainsKey(entityId))
            {
                actionPlans.Remove(entityId);
                _synchronizedDefaultPlanIds.Remove(entityId);
            }
        }
    }
}
