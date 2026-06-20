namespace GameGameGame.Core;

public enum LocalTurnParticipation
{
    Player,
    Actor,
    Inert
}

public sealed record TurnActionReport(
    EntityId ActorId,
    string ActorName,
    bool Succeeded,
    bool ConsumedTurn,
    string Summary,
    TraceNode Trace);

public sealed record SimulationTurnReport(
    int TurnNumber,
    IReadOnlyList<TurnActionReport> Actions);

public sealed record LocalTurnOrderRow(
    int Order,
    EntityId EntityId,
    string EntityName,
    char Glyph,
    PlaneCoord Location,
    LocalTurnParticipation Participation,
    string PreviousAction);

public sealed record LocalTurnOrderReport(
    PlaneId PlaneId,
    IReadOnlyList<LocalTurnOrderRow> Rows)
{
    public static LocalTurnOrderReport Create(
        WorldState world,
        PlaneId planeId,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        EntityId? playerId = null,
        Func<EntityId, char>? getGlyph = null)
    {
        var previousActions = world.LastTurnReport?.Actions.ToDictionary(action => action.ActorId) ?? [];
        var occupants = world.Occupancy
            .Where(entry => world.Nodes.TryGetValue(entry.Key, out var node) && node.PlaneId == planeId)
            .Select(entry => (EntityId: entry.Value, Location: world.GetEntityLocation(entry.Value)))
            .OrderBy(entry => entry.Location.Coord.Y)
            .ThenBy(entry => entry.Location.Coord.X)
            .ThenBy(entry => entry.EntityId.Value, StringComparer.Ordinal)
            .ToList();

        var rows = new List<LocalTurnOrderRow>();
        var orderedActors = new List<(EntityId EntityId, PlaneCoord Location, LocalTurnParticipation Participation)>();

        if (playerId is { } player && occupants.Any(entry => entry.EntityId == player))
        {
            orderedActors.Add((player, world.GetEntityLocation(player), LocalTurnParticipation.Player));
        }

        orderedActors.AddRange(occupants
            .Where(entry => actionPlans.ContainsKey(entry.EntityId) && entry.EntityId != playerId)
            .Select(entry => (entry.EntityId, entry.Location, LocalTurnParticipation.Actor)));

        for (var index = 0; index < orderedActors.Count; index++)
        {
            var actor = orderedActors[index];
            rows.Add(CreateRow(world, actor.EntityId, actor.Location, actor.Participation, index, previousActions, getGlyph));
        }

        foreach (var inert in occupants.Where(entry => !orderedActors.Any(actor => actor.EntityId == entry.EntityId)))
        {
            rows.Add(CreateRow(world, inert.EntityId, inert.Location, LocalTurnParticipation.Inert, -1, previousActions, getGlyph));
        }

        return new LocalTurnOrderReport(planeId, rows);
    }

    private static LocalTurnOrderRow CreateRow(
        WorldState world,
        EntityId entityId,
        PlaneCoord location,
        LocalTurnParticipation participation,
        int order,
        IReadOnlyDictionary<EntityId, TurnActionReport> previousActions,
        Func<EntityId, char>? getGlyph)
    {
        var previousAction = participation == LocalTurnParticipation.Inert
            ? "----"
            : previousActions.TryGetValue(entityId, out var action) ? action.Summary : "None";

        return new LocalTurnOrderRow(
            order,
            entityId,
            world.Entities[entityId].Name,
            getGlyph?.Invoke(entityId) ?? '?',
            location,
            participation,
            previousAction);
    }
}

public static class LocalTurnOrderReportFormatter
{
    public static IReadOnlyList<string> Format(LocalTurnOrderReport report)
    {
        var lines = new List<string> { "Order# | Entity | Prev. Action" };
        lines.AddRange(report.Rows.Select(row => $"{FormatOrder(row.Order)} | {row.Glyph} {row.EntityName} | {row.PreviousAction}"));
        return lines;
    }

    private static string FormatOrder(int order) => order < 0 ? "--" : order.ToString();
}

public static class TurnActionSummaryFormatter
{
    public static string Format(ActionResolution resolution) => FormatTrace(resolution.Trace, resolution.Succeeded);

    public static string FormatTrace(TraceNode trace, bool succeeded)
    {
        var terminal = FindTerminalAction(trace, succeeded) ?? trace;
        return FormatLabel(terminal.Label, succeeded);
    }

    private static TraceNode? FindTerminalAction(TraceNode trace, bool succeeded)
    {
        var nodes = DescendantsAndSelf(trace).Where(node => IsActionLabel(node.Label)).ToList();
        return nodes.LastOrDefault(node => node.Status == TraceStatus.Success)
            ?? nodes.LastOrDefault(node => node.Status == TraceStatus.Failure)
            ?? nodes.LastOrDefault();
    }

    private static bool IsActionLabel(string label) =>
        label == "Wait"
        || label.StartsWith("Primitive ", StringComparison.Ordinal)
        || label.StartsWith("Move ", StringComparison.Ordinal)
        || label.StartsWith("Pickup ", StringComparison.Ordinal)
        || label.StartsWith("Drop ", StringComparison.Ordinal)
        || label.StartsWith("Teleport ", StringComparison.Ordinal)
        || label.StartsWith("Reverse direction ", StringComparison.Ordinal)
        || label.StartsWith("Action Step ", StringComparison.Ordinal);

    private static string FormatLabel(string label, bool succeeded)
    {
        if (!succeeded)
        {
            return $"{Normalize(label)} failed";
        }

        if (label == "Wait")
        {
            return "Waited";
        }

        if (label.StartsWith("Move ", StringComparison.Ordinal))
        {
            return $"Moved {label[5..]}";
        }

        if (label.StartsWith("Pickup ", StringComparison.Ordinal))
        {
            return $"Picked up {label[7..].Split(" -> ", StringSplitOptions.None)[0]}";
        }

        if (label.StartsWith("Drop ", StringComparison.Ordinal))
        {
            return $"Dropped {label[5..].Split(" -> ", StringSplitOptions.None)[0]}";
        }

        if (label.StartsWith("Primitive ", StringComparison.Ordinal))
        {
            return label[10..];
        }

        if (label.StartsWith("Teleport ", StringComparison.Ordinal))
        {
            return $"Teleported {label[9..]}";
        }

        if (label.StartsWith("Reverse direction ", StringComparison.Ordinal))
        {
            return $"Reversed direction {label[18..]}";
        }

        return Normalize(label);
    }

    private static string Normalize(string label) =>
        label.StartsWith("Action Step ", StringComparison.Ordinal) ? label[12..] :
        label.StartsWith("Primitive ", StringComparison.Ordinal) ? label[10..] :
        label;

    private static IEnumerable<TraceNode> DescendantsAndSelf(TraceNode node)
    {
        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var descendant in DescendantsAndSelf(child))
            {
                yield return descendant;
            }
        }
    }
}
