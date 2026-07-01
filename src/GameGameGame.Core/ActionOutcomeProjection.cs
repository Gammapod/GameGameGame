namespace GameGameGame.Core;

public sealed record ActionOutcome(
    int? TurnNumber,
    EntityId ActorId,
    string ActorName,
    string ActionKind,
    bool Succeeded,
    EntityId? TargetId,
    string? TargetName,
    PlaneCoord? Source,
    PlaneCoord? Destination,
    Direction? Direction,
    FailureReason? FailureReason,
    string? FailureDetail,
    string Sentence,
    IReadOnlySet<EntityId> AnchorEntityIds,
    IReadOnlySet<PlaneId> AnchorPlaneIds,
    TraceNode Trace);

public static class ActionOutcomeProjection
{
    public static ActionOutcome FromCommandResult(WorldState world, ControlledActorCommandResult result)
    {
        var actorName = EntityName(world, result.ActorId);
        var targetName = result.TargetId is { } targetId ? EntityName(world, targetId) : null;
        var actionKind = ToActionKind(result.Kind);
        var sentence = result.Succeeded
            ? SuccessSentence(actorName, actionKind, targetName, result.Direction)
            : FailureSentence(actorName, actionKind, targetName, result.Direction, result.FailureReason, result.FailureDetail);
        var anchorEntityIds = new HashSet<EntityId> { result.ActorId };
        if (result.TargetId is { } concreteTargetId)
        {
            anchorEntityIds.Add(concreteTargetId);
        }

        var anchorPlaneIds = new HashSet<PlaneId>();
        AddPlane(anchorPlaneIds, result.Source);
        AddPlane(anchorPlaneIds, result.Destination);
        if (world.Entities.ContainsKey(result.ActorId))
        {
            anchorPlaneIds.Add(world.GetEntityLocation(result.ActorId).PlaneId);
        }

        if (result.TargetId is { } target && world.Entities.ContainsKey(target))
        {
            anchorPlaneIds.Add(world.GetEntityLocation(target).PlaneId);
        }

        return new ActionOutcome(
            result.TurnReport?.TurnNumber,
            result.ActorId,
            actorName,
            actionKind,
            result.Succeeded,
            result.TargetId,
            targetName,
            result.Source,
            result.Destination,
            result.Direction,
            result.FailureReason,
            result.FailureDetail,
            sentence,
            anchorEntityIds,
            anchorPlaneIds,
            result.Trace);
    }

    private static string ToActionKind(ControlledActorCommandKind kind) => kind switch
    {
        ControlledActorCommandKind.Move => "move",
        ControlledActorCommandKind.Pickup => "pickup",
        ControlledActorCommandKind.Drop => "drop",
        ControlledActorCommandKind.Enter => "enter",
        ControlledActorCommandKind.Exit => "exit",
        _ => kind.ToString().ToLowerInvariant()
    };

    private static string SuccessSentence(string actorName, string actionKind, string? targetName, Direction? direction) => actionKind switch
    {
        "move" => direction is { } moveDirection ? $"{actorName} moved {moveDirection}" : $"{actorName} moved",
        "pickup" => targetName is { } pickupTarget ? $"{actorName} picked up {pickupTarget}" : $"{actorName} picked up target",
        "drop" => targetName is { } dropTarget ? $"{actorName} dropped {dropTarget}" : $"{actorName} dropped target",
        "enter" => targetName is { } enterTarget ? $"{actorName} entered {enterTarget}" : $"{actorName} entered target",
        "exit" => direction is { } exitDirection ? $"{actorName} exited {exitDirection}" : $"{actorName} exited",
        _ => $"{actorName} {actionKind}ed"
    };

    private static string FailureSentence(
        string actorName,
        string actionKind,
        string? targetName,
        Direction? direction,
        FailureReason? reason,
        string? detail)
    {
        var target = actionKind switch
        {
            "move" or "exit" when direction is { } concreteDirection => concreteDirection.ToString(),
            _ when targetName is { } concreteTargetName => concreteTargetName,
            _ => null
        };
        var targetPhrase = target is null ? string.Empty : $" {target}";
        var reasonText = !string.IsNullOrWhiteSpace(detail)
            ? detail
            : reason?.ToString() ?? "it failed";
        return $"{actorName} tried to {actionKind}{targetPhrase}, but {reasonText}";
    }

    private static string EntityName(WorldState world, EntityId entityId) =>
        world.Entities.TryGetValue(entityId, out var entity) ? entity.Name : entityId.ToString();

    private static void AddPlane(HashSet<PlaneId> planes, PlaneCoord? coord)
    {
        if (coord is { } concrete)
        {
            planes.Add(concrete.PlaneId);
        }
    }
}

public sealed record ActionLogProjection(IReadOnlyList<ActionOutcome> Chronological)
{
    public static ActionLogProjection FromOutcomes(IReadOnlyList<ActionOutcome> outcomes) => new(outcomes);

    public IReadOnlyList<ActionOutcome> ForEntity(EntityId entityId) =>
        Chronological.Where(outcome => outcome.AnchorEntityIds.Contains(entityId)).ToList();

    public IReadOnlyList<ActionOutcome> ForPlane(PlaneId planeId) =>
        Chronological.Where(outcome => outcome.AnchorPlaneIds.Contains(planeId)).ToList();
}
