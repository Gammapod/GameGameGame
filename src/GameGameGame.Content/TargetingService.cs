using GameGameGame.Core;

namespace GameGameGame.Content;

public static class TargetingService
{
    private static readonly MovementService Movement = new();
    private static readonly EntityInteractionAffordanceService Affordances = new(Movement);
    private static readonly TargetingLocalityCandidateService LocalityCandidates = new();

    public static void RefreshTargets(WorldState world, PrototypeContentRegistry registry, EntityId actorId)
    {
        if (!world.Entities.ContainsKey(actorId)
            || !registry.TryGetTemplateIdForEntity(world, actorId, out var actorTemplateId))
        {
            return;
        }

        var template = registry.GetEntityTemplate(actorTemplateId);
        var rules = GetRules(template);
        if (rules.Count == 0)
        {
            return;
        }

        var actorLocation = world.GetEntityLocation(actorId);
        foreach (var (rule, range, locality) in rules)
        {
            var selected = FindNearestMatchingTarget(world, registry, actorId, actorLocation, rule, range, locality);
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
        EntityTargetingRule rule,
        int range,
        TargetingLocalityQuery locality) =>
        LocalityCandidates.Query(world, actorId, locality)
            .Where(entry => MatchesTargetTemplate(world, registry, entry.EntityId, rule.TargetTemplateId))
            .Where(entry => MatchesTargetCapabilities(world, actorId, entry.EntityId, rule.TargetCapabilities))
            .Select(entry => new
            {
                entry.EntityId,
                entry.DistanceReferenceLocation.Coord,
                Distance = entry.DistanceReferenceLocation.PlaneId == actorLocation.PlaneId
                    ? ManhattanDistance(actorLocation.Coord, entry.DistanceReferenceLocation.Coord)
                    : int.MaxValue
            })
            .Where(entry => entry.Distance <= range)
            .OrderBy(entry => entry.Distance)
            .ThenBy(entry => entry.Coord.Y)
            .ThenBy(entry => entry.Coord.X)
            .ThenBy(entry => entry.EntityId.Value, StringComparer.Ordinal)
            .Select(entry => (EntityId?)entry.EntityId)
            .FirstOrDefault();

    private static IReadOnlyList<(EntityTargetingRule Rule, int Range, TargetingLocalityQuery Locality)> GetRules(EntityTemplate template)
    {
        if (template.Targeting is { } profile && profile.Rules.Count > 0)
        {
            return profile.Rules
                .Select(rule => (rule, profile.Range, rule.Locality ?? profile.DefaultLocality ?? new TargetingLocalityQuery()))
                .ToList();
        }

        return (template.TargetingRules ?? [])
            .Select(rule => (rule, rule.Range, rule.Locality ?? new TargetingLocalityQuery()))
            .ToList();
    }

    private static int ManhattanDistance(GridCoord first, GridCoord second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);

    private static bool MatchesTargetTemplate(WorldState world, PrototypeContentRegistry registry, EntityId entityId, EntityTemplateId? targetTemplateId) =>
        targetTemplateId is null
        || (registry.TryGetTemplateIdForEntity(world, entityId, out var templateId) && templateId == targetTemplateId);

    private static bool MatchesTargetCapabilities(
        WorldState world,
        EntityId actorId,
        EntityId candidateId,
        IReadOnlyList<ActionPlanBehaviorStepKind> targetCapabilities) =>
        targetCapabilities.Count == 0
        || targetCapabilities.All(capability => Affordances.QueryTargetCapability(world, actorId, candidateId, capability).CanTarget);
}
