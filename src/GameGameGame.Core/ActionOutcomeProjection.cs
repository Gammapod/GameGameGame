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
    TraceNode Trace)
{
    public bool ConsumedTurn { get; init; }

    public IReadOnlyList<ActionStepAttempt> ActionStepAttempts { get; init; } = [];
}

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
            result.Trace)
        {
            ConsumedTurn = result.ConsumedTurn,
            ActionStepAttempts = ExtractActionStepAttempts(result.Trace)
        };
    }

    public static ActionOutcome FromActorLog(WorldState world, int? turnNumber, SimulationHistoryActorLog log)
    {
        var attempts = ExtractActionStepAttempts(log.Trace);
        var anchorEntityIds = new HashSet<EntityId> { log.ActorId };
        var anchorPlaneIds = new HashSet<PlaneId>();
        if (world.Entities.ContainsKey(log.ActorId))
        {
            anchorPlaneIds.Add(world.GetEntityLocation(log.ActorId).PlaneId);
        }

        return new ActionOutcome(
            turnNumber,
            log.ActorId,
            log.ActorName,
            attempts.Count > 0 ? ToActionKind(attempts[0].StepKind) : "turn",
            log.Succeeded,
            TargetId: null,
            TargetName: null,
            Source: null,
            Destination: null,
            Direction: null,
            FailureReason: FindFailureReason(log.Trace),
            FailureDetail: log.Succeeded ? null : FindFailureDetail(log.Trace),
            $"{log.ActorName}: {log.Summary}",
            anchorEntityIds,
            anchorPlaneIds,
            log.Trace)
        {
            ConsumedTurn = log.ConsumedTurn,
            ActionStepAttempts = attempts
        };
    }

    private static string ToActionKind(ControlledActorCommandKind kind) => kind switch
    {
        ControlledActorCommandKind.Move => "move",
        ControlledActorCommandKind.Pickup => "pickup",
        ControlledActorCommandKind.Drop => "drop",
        ControlledActorCommandKind.Enter => "enter",
        ControlledActorCommandKind.Exit => "exit",
        ControlledActorCommandKind.Wait => "wait",
        _ => kind.ToString().ToLowerInvariant()
    };

    private static string ToActionKind(string stepKind) => stepKind.Length == 0
        ? "turn"
        : char.ToLowerInvariant(stepKind[0]) + stepKind[1..];

    private static string SuccessSentence(string actorName, string actionKind, string? targetName, Direction? direction) => actionKind switch
    {
        "move" => direction is { } moveDirection ? $"{actorName} moved {moveDirection}" : $"{actorName} moved",
        "pickup" => targetName is { } pickupTarget ? $"{actorName} picked up {pickupTarget}" : $"{actorName} picked up target",
        "drop" => targetName is { } dropTarget ? $"{actorName} dropped {dropTarget}" : $"{actorName} dropped target",
        "enter" => targetName is { } enterTarget ? $"{actorName} entered {enterTarget}" : $"{actorName} entered target",
        "exit" => direction is { } exitDirection ? $"{actorName} exited {exitDirection}" : $"{actorName} exited",
        "wait" => $"{actorName} waited",
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

    private static IReadOnlyList<ActionStepAttempt> ExtractActionStepAttempts(TraceNode trace)
    {
        if (trace.Children.Any(child => child.Label.StartsWith("Action Step ", StringComparison.Ordinal)))
        {
            return ActionStepAttemptProjection.Project(trace);
        }

        var planTrace = trace.Children.FirstOrDefault(child => child.Label.StartsWith("Plan ", StringComparison.Ordinal));
        return planTrace is null ? [] : ActionStepAttemptProjection.Project(planTrace);
    }

    private static FailureReason? FindFailureReason(TraceNode trace)
    {
        var failure = DescendantsAndSelf(trace).FirstOrDefault(node => node.Status == TraceStatus.Failure && node.Reason != FailureReason.None);
        return failure?.Reason;
    }

    private static string? FindFailureDetail(TraceNode trace) =>
        DescendantsAndSelf(trace).FirstOrDefault(node => node.Status == TraceStatus.Failure && !string.IsNullOrWhiteSpace(node.Detail))?.Detail;

    private static IEnumerable<TraceNode> DescendantsAndSelf(TraceNode trace)
    {
        yield return trace;
        foreach (var child in trace.Children)
        {
            foreach (var descendant in DescendantsAndSelf(child))
            {
                yield return descendant;
            }
        }
    }
}

public sealed record ActionLogProjection(IReadOnlyList<ActionOutcome> Chronological)
{
    public static ActionLogProjection FromOutcomes(IReadOnlyList<ActionOutcome> outcomes) => new(outcomes);

    public static ActionLogProjection FromHistory(SimulationHistorySession history)
    {
        var outcomes = new List<ActionOutcome>();

        foreach (var frame in history.Frames)
        {
            outcomes.AddRange(history
                .GetFrameLogEntries(frame.FrameIndex)
                .Select(entry => ActionOutcomeProjection.FromCommandResult(frame.Snapshot, entry.ControlledResult)));

            outcomes.AddRange(history.Intervals
                .Where(interval => interval.FromFrameIndex == frame.FrameIndex)
                .SelectMany(interval =>
                {
                    var intervalSnapshot = history.Frames[interval.ToFrameIndex].Snapshot;
                    var turnNumber = interval.ControlledResult?.TurnReport?.TurnNumber ?? history.Frames[interval.ToFrameIndex].WorldTurnNumber;
                    var rows = new List<ActionOutcome>();
                    if (interval.ControlledResult is { } controlled)
                    {
                        rows.Add(ActionOutcomeProjection.FromCommandResult(intervalSnapshot, controlled));
                    }

                    rows.AddRange(interval.ActorLogs
                        .Where(log => interval.ControlledResult?.ActorId != log.ActorId)
                        .Select(log => ActionOutcomeProjection.FromActorLog(intervalSnapshot, turnNumber, log)));
                    return rows;
                }));
        }

        return new ActionLogProjection(outcomes);
    }

    public IReadOnlyList<ActionOutcome> ForEntity(EntityId entityId) =>
        Chronological.Where(outcome => outcome.AnchorEntityIds.Contains(entityId)).ToList();

    public IReadOnlyList<ActionOutcome> ForPlane(PlaneId planeId) =>
        Chronological.Where(outcome => outcome.AnchorPlaneIds.Contains(planeId)).ToList();
}
