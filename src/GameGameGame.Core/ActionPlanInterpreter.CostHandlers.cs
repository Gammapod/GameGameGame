namespace GameGameGame.Core;

public sealed partial class ActionPlanInterpreter
{
    private sealed record BehaviorStepCostEvaluation(
        bool CanExecute,
        TraceNode Trace,
        IReadOnlyDictionary<string, IReadOnlyList<EntityId>> SelectedEntityIds);

    private static BehaviorStepCostEvaluation? EvaluateBehaviorStepCost(
        WorldState world,
        EntityId actorId,
        ActionPlanBehaviorStepDescriptor step)
    {
        if (step.Costs.Count == 0)
        {
            return null;
        }

        var trace = new TraceNode("Action Step Cost", TraceStatus.Info);
        var carriedEntitiesByTemplate = CollectRecursiveActorInventoryEntitiesByTemplate(world, actorId);
        var selected = new Dictionary<string, IReadOnlyList<EntityId>>(StringComparer.OrdinalIgnoreCase);

        foreach (var cost in step.Costs)
        {
            carriedEntitiesByTemplate.TryGetValue(cost.TemplateId, out var availableEntities);
            var available = availableEntities?.Count ?? 0;
            if (available < cost.Quantity)
            {
                trace.Status = TraceStatus.Failure;
                trace.Reason = FailureReason.MissingCost;
                trace.Detail = $"missing cost {cost.TemplateId}: required {cost.Quantity}, available {available}";
                return new BehaviorStepCostEvaluation(false, trace, selected);
            }

            var selectedEntities = availableEntities!
                .Take(cost.Quantity)
                .ToList();
            selected[cost.TemplateId] = selectedEntities;
            trace.Add(TraceNode.Success(
                $"Cost available {cost.TemplateId}",
                $"Cost available {cost.TemplateId}: required {cost.Quantity}, available {available}"));
            trace.Add(TraceNode.Info(
                $"Selected cost {cost.TemplateId}",
                $"Selected cost {cost.TemplateId}: {string.Join(',', selectedEntities.Select(entityId => entityId.Value))}"));
        }

        trace.Status = TraceStatus.Success;
        trace.Detail = "all costs available";
        return new BehaviorStepCostEvaluation(true, trace, selected);
    }

    private static void ConsumeBehaviorStepCost(
        WorldState world,
        BehaviorStepCostEvaluation costEvaluation,
        TraceNode stepTrace)
    {
        foreach (var (templateId, entityIds) in costEvaluation.SelectedEntityIds)
        {
            var destroyed = new List<EntityId>();
            foreach (var entityId in entityIds)
            {
                if (world.Entities.ContainsKey(entityId))
                {
                    destroyed.AddRange(world.DestroyEntityRecursive(entityId));
                }
            }

            stepTrace.Add(TraceNode.Success(
                $"Consumed cost {templateId}",
                $"Consumed cost {templateId}: {string.Join(',', entityIds.Select(entityId => entityId.Value))}"));
        }
    }

    private static Dictionary<string, List<EntityId>> CollectRecursiveActorInventoryEntitiesByTemplate(WorldState world, EntityId actorId)
    {
        var entitiesByTemplate = new Dictionary<string, List<EntityId>>(StringComparer.OrdinalIgnoreCase);
        if (world.GetRegisteredInventoryPlaneId(actorId) is not { } inventoryPlaneId)
        {
            return entitiesByTemplate;
        }

        CollectPlaneTemplateEntities(world, inventoryPlaneId, entitiesByTemplate, visitedEntities: []);
        return entitiesByTemplate;
    }

    private static void CollectPlaneTemplateEntities(
        WorldState world,
        PlaneId planeId,
        Dictionary<string, List<EntityId>> entitiesByTemplate,
        HashSet<EntityId> visitedEntities)
    {
        var contained = world.Occupancy
            .Where(entry => world.Nodes.TryGetValue(entry.Key, out var node) && node.PlaneId == planeId)
            .Select(entry => new { entry.Value, Node = world.Nodes[entry.Key] })
            .OrderBy(entry => entry.Node.Coord.Y)
            .ThenBy(entry => entry.Node.Coord.X)
            .ThenBy(entry => entry.Value.Value, StringComparer.Ordinal)
            .Select(entry => entry.Value)
            .ToList();

        foreach (var entityId in contained)
        {
            CollectEntityTemplateAndContents(world, entityId, entitiesByTemplate, visitedEntities);
        }
    }

    private static void CollectEntityTemplateAndContents(
        WorldState world,
        EntityId entityId,
        Dictionary<string, List<EntityId>> entitiesByTemplate,
        HashSet<EntityId> visitedEntities)
    {
        if (!visitedEntities.Add(entityId) || !world.Entities.TryGetValue(entityId, out var entity))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(entity.TemplateId))
        {
            if (!entitiesByTemplate.TryGetValue(entity.TemplateId, out var entities))
            {
                entities = [];
                entitiesByTemplate[entity.TemplateId] = entities;
            }

            entities.Add(entityId);
        }

        if (world.GetRegisteredInventoryPlaneId(entityId) is { } inventoryPlaneId)
        {
            CollectPlaneTemplateEntities(world, inventoryPlaneId, entitiesByTemplate, visitedEntities);
        }
    }
}
