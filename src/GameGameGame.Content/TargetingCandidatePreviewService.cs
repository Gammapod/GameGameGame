using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed record TargetingCandidatePreview(
    EntityId ActorId,
    IReadOnlyList<TargetingRuleCandidatePreview> Rules);

public sealed record TargetingRuleCandidatePreview(
    int Slot,
    string? Label,
    IReadOnlyList<TargetingCandidatePreviewEntry> Candidates);

public sealed record TargetingCandidatePreviewEntry(
    EntityId EntityId,
    TargetingLocalityOrigin Origin,
    EntityId? ReferenceEntityId,
    int Distance);

public static class TargetingCandidatePreviewService
{
    private static readonly MovementService Movement = new();
    private static readonly EntityInteractionAffordanceService Affordances = new(Movement);
    private static readonly TargetingLocalityCandidateService LocalityCandidates = new();
    private static readonly TargetingDistanceService Distances = new();

    public static TargetingCandidatePreview Preview(WorldState world, PrototypeContentRegistry registry, EntityId actorId)
    {
        if (!world.Entities.ContainsKey(actorId)
            || !registry.TryGetTemplateIdForEntity(actorId, out var actorTemplateId))
        {
            return new TargetingCandidatePreview(actorId, []);
        }

        var template = registry.GetEntityTemplate(actorTemplateId);
        var actorLocation = world.GetEntityLocation(actorId);
        var rules = GetRules(template);
        return new TargetingCandidatePreview(
            actorId,
            rules.Select(rule => new TargetingRuleCandidatePreview(
                    rule.Rule.Slot,
                    rule.Rule.Label,
                    LocalityCandidates.Query(world, actorId, rule.Locality)
                        .Where(candidate => MatchesTargetTemplate(registry, candidate.EntityId, rule.Rule.TargetTemplateId))
                        .Where(candidate => MatchesTargetCapabilities(world, actorId, candidate.EntityId, rule.Rule.TargetCapabilities))
                        .Select(candidate => new TargetingCandidatePreviewEntry(
                            candidate.EntityId,
                            candidate.Origin,
                            candidate.ReferenceEntityId,
                            GetDistance(world, candidate.DistanceOriginLocation ?? actorLocation, candidate.DistanceReferenceLocation, rule.Range)))
                        .Where(candidate => candidate.Distance <= rule.Range)
                        .OrderBy(candidate => candidate.Distance)
                        .ThenBy(candidate => candidate.EntityId.Value, StringComparer.Ordinal)
                        .ToList()))
                .ToList());
    }

    private static int GetDistance(WorldState world, PlaneCoord origin, PlaneCoord destination, int range)
    {
        var distances = Distances.GetOctagonalDistances(world, origin, range);
        return distances.TryGetValue(destination, out var distance) ? distance : int.MaxValue;
    }

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

    private static bool MatchesTargetTemplate(PrototypeContentRegistry registry, EntityId entityId, EntityTemplateId? targetTemplateId) =>
        targetTemplateId is null
        || (registry.TryGetTemplateIdForEntity(entityId, out var templateId) && templateId == targetTemplateId);

    private static bool MatchesTargetCapabilities(
        WorldState world,
        EntityId actorId,
        EntityId candidateId,
        IReadOnlyList<ActionPlanBehaviorStepKind> targetCapabilities) =>
        targetCapabilities.Count == 0
        || targetCapabilities.All(capability => Affordances.QueryTargetCapability(world, actorId, candidateId, capability).CanTarget);
}
