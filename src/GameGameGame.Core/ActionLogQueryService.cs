namespace GameGameGame.Core;

public enum ActionLogOrder
{
    Chronological,
    NewestFirst
}

public sealed record ActionLogQuery(
    IReadOnlySet<EntityId>? EntityAnchors = null,
    IReadOnlySet<PlaneId>? PlaneAnchors = null,
    bool? Succeeded = null,
    ActionLogOrder Order = ActionLogOrder.Chronological,
    int? MaxRows = null);

public static class ActionLogQueryService
{
    public static IReadOnlyList<ActionOutcome> Select(ActionLogProjection? actionLog, ActionLogQuery query)
    {
        if (actionLog is null)
        {
            return [];
        }

        IEnumerable<ActionOutcome> rows = actionLog.Chronological;

        if (query.EntityAnchors is { Count: > 0 } entityAnchors || query.PlaneAnchors is { Count: > 0 } planeAnchors)
        {
            var entities = query.EntityAnchors ?? new HashSet<EntityId>();
            var planes = query.PlaneAnchors ?? new HashSet<PlaneId>();
            rows = rows.Where(outcome =>
                outcome.AnchorEntityIds.Any(entities.Contains)
                || outcome.AnchorPlaneIds.Any(planes.Contains));
        }

        if (query.Succeeded is { } succeeded)
        {
            rows = rows.Where(outcome => outcome.Succeeded == succeeded);
        }

        if (query.Order == ActionLogOrder.NewestFirst)
        {
            rows = rows.Reverse();
        }

        if (query.MaxRows is { } maxRows)
        {
            rows = maxRows <= 0 ? [] : rows.Take(maxRows);
        }

        return rows.ToList();
    }
}
