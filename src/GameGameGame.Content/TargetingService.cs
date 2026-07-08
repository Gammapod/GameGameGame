using GameGameGame.Core;

namespace GameGameGame.Content;

public static class TargetingService
{
    public static void RefreshTargets(WorldState world, PrototypeContentRegistry registry, EntityId actorId)
    {
        if (!world.Entities.ContainsKey(actorId)
            || !registry.TryGetTemplateIdForEntity(actorId, out var actorTemplateId))
        {
            return;
        }

        var template = registry.GetEntityTemplate(actorTemplateId);
        if (template.TargetingRules is null || template.TargetingRules.Count == 0)
        {
            return;
        }

        var actorLocation = world.GetEntityLocation(actorId);
        foreach (var rule in template.TargetingRules)
        {
            var selected = FindNearestMatchingTarget(world, registry, actorId, actorLocation, rule);
            if (selected is { } targetId)
            {
                world.SetActionTarget(actorId, rule.Slot, targetId);
                if (!string.IsNullOrWhiteSpace(rule.Label))
                {
                    world.SetActionTarget(actorId, rule.Label, targetId);
                }
            }
            else
            {
                world.ClearActionTarget(actorId, rule.Slot);
                if (!string.IsNullOrWhiteSpace(rule.Label))
                {
                    world.ClearActionTarget(actorId, rule.Label);
                }
            }
        }
    }

    private static EntityId? FindNearestMatchingTarget(
        WorldState world,
        PrototypeContentRegistry registry,
        EntityId actorId,
        PlaneCoord actorLocation,
        EntityTargetingRule rule) =>
        world.Occupancy
            .Select(entry => (EntityId: entry.Value, Node: world.Nodes[entry.Key]))
            .Where(entry => entry.EntityId != actorId)
            .Where(entry => entry.Node.PlaneId == actorLocation.PlaneId)
            .Where(entry => registry.TryGetTemplateIdForEntity(entry.EntityId, out var templateId) && templateId == rule.TargetTemplateId)
            .Select(entry => new
            {
                entry.EntityId,
                entry.Node.Coord,
                Distance = ManhattanDistance(actorLocation.Coord, entry.Node.Coord)
            })
            .Where(entry => entry.Distance <= rule.Range)
            .OrderBy(entry => entry.Distance)
            .ThenBy(entry => entry.Coord.Y)
            .ThenBy(entry => entry.Coord.X)
            .ThenBy(entry => entry.EntityId.Value, StringComparer.Ordinal)
            .Select(entry => (EntityId?)entry.EntityId)
            .FirstOrDefault();

    private static int ManhattanDistance(GridCoord first, GridCoord second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);
}
