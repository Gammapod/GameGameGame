using GameGameGame.Core;

namespace GameGameGame.Content;

public static class ScenarioInitiativeOrderService
{
    public static IReadOnlyList<ScenarioActorSummary> GetScenarioActorsInInitiativeOrder(
        WorldState world,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        EntityId scenarioRootEntityId,
        PlaneId scenarioPlaneId)
    {
        var containmentPaths = new EntityContainmentPathService();

        return actionPlans.Keys
            .Where(world.Entities.ContainsKey)
            .Where(entityId => entityId != scenarioRootEntityId)
            .Select(entityId => (
                EntityId: entityId,
                Location: world.GetEntityLocation(entityId),
                Path: containmentPaths.GetPathFromRoot(world, scenarioRootEntityId, entityId)))
            .Where(entry => IsScheduledScenarioActor(entry.Path, scenarioRootEntityId, scenarioPlaneId))
            .OrderBy(entry => FormatScenarioInitiativePath(entry.Path), StringComparer.Ordinal)
            .ThenBy(entry => entry.EntityId.Value, StringComparer.Ordinal)
            .Select(entry => new ScenarioActorSummary(
                entry.EntityId,
                world.Entities[entry.EntityId].Name,
                entry.Location))
            .ToList();
    }

    private static bool IsScheduledScenarioActor(EntityContainmentPath path, EntityId scenarioRootEntityId, PlaneId scenarioPlaneId) =>
        path.Status == EntityContainmentPathStatus.Complete
        && path.Segments.Count > 1
        && path.Segments[0].EntityId == scenarioRootEntityId
        && path.Segments[1].ContainingPlaneId == scenarioPlaneId;

    private static string FormatScenarioInitiativePath(EntityContainmentPath path) =>
        string.Join(
            "/",
            path.Segments
                .Skip(1)
                .Select(segment =>
                {
                    var coord = segment.CoordinateInContainingPlane ?? new GridCoord(0, 0);
                    return $"{coord.Y:D6},{coord.X:D6},{segment.EntityId.Value}";
                }));
}
