using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed record PlayerNarrativeLogProjectionRequest(
    SimulationHistorySession History,
    EntityId ObserverEntityId);

public sealed record PlayerNarrativeLogRow(
    int TurnNumber,
    int InitiativeIndex,
    int OrderIndex,
    EntityId ActorEntityId,
    string ActorDisplayName,
    ActionPlanId? ActionPlanId,
    string? ActionStepKind,
    int? ActionStepIndex,
    bool Succeeded,
    string Result,
    string MessageId,
    string? Variant,
    string? Text,
    EntityId? TargetEntityId,
    string? TargetDisplayName,
    IReadOnlyDictionary<string, string> MessageArgs,
    bool? IsPlayerVisible);

public static class PlayerNarrativeLogProjection
{
    public static IReadOnlyList<PlayerNarrativeLogRow> Project(PlayerNarrativeLogProjectionRequest request)
    {
        var rows = new List<PlayerNarrativeLogRow>();
        var orderIndex = 0;
        foreach (var interval in request.History.Intervals.OrderBy(interval => interval.ToFrameIndex))
        {
            var world = request.History.Frames[interval.ToFrameIndex].Snapshot;
            foreach (var log in interval.ActorLogs.OrderBy(log => log.Order))
            {
                var outcome = ActionOutcomeProjection.FromActorLog(world, interval.ToFrameIndex, log);
                var attempts = outcome.ActionStepAttempts.Count == 0
                    ? [(ActionStepAttempt?)null]
                    : outcome.ActionStepAttempts.Select(attempt => (ActionStepAttempt?)attempt).ToList();

                foreach (var attempt in attempts)
                {
                    var stepKind = attempt?.StepKind;
                    var succeeded = attempt?.Status == TraceStatus.Success || (attempt is null && log.Succeeded);
                    rows.Add(new PlayerNarrativeLogRow(
                        TurnNumber: interval.ToFrameIndex,
                        InitiativeIndex: log.Order + 1,
                        OrderIndex: orderIndex++,
                        ActorEntityId: log.ActorId,
                        ActorDisplayName: StableEntityName(world, log.ActorId, log.ActorName),
                        ActionPlanId: null,
                        ActionStepKind: stepKind,
                        ActionStepIndex: attempt?.Order,
                        Succeeded: succeeded,
                        Result: succeeded ? "succeeded" : "failed",
                        MessageId: BuildMessageId(stepKind, succeeded),
                        Variant: null,
                        Text: null,
                        TargetEntityId: null,
                        TargetDisplayName: null,
                        MessageArgs: BuildMessageArgs(outcome, attempt),
                        IsPlayerVisible: null));
                }
            }
        }

        return rows;
    }

    private static IReadOnlyDictionary<string, string> BuildMessageArgs(ActionOutcome outcome, ActionStepAttempt? attempt)
    {
        var args = new Dictionary<string, string>
        {
            ["actor"] = outcome.ActorName
        };
        if (attempt?.FailureReason is { } reason)
        {
            args["failureReason"] = reason.ToString();
        }
        if (!string.IsNullOrWhiteSpace(attempt?.Detail))
        {
            args["detail"] = attempt.Detail!;
        }
        var aperture = outcome.SuccessCriteria.FirstOrDefault(criterion => criterion.Kind == ActionSuccessCriterionKind.Aperture && criterion.SuccessRatio is not null);
        if (aperture?.SuccessRatio is { } ratio)
        {
            args["successRatio"] = ratio.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return args;
    }

    private static string BuildMessageId(string? stepKind, bool succeeded) =>
        $"action.{ToSnakeCase(stepKind ?? "Turn")}.{(succeeded ? "success" : "failure")}";

    private static string ToSnakeCase(string value)
    {
        var chars = new List<char>(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c) && i > 0)
            {
                chars.Add('_');
            }

            chars.Add(char.ToLowerInvariant(c));
        }

        return new string(chars.ToArray());
    }

    private static string StableEntityName(WorldState world, EntityId entityId, string? knownName = null) =>
        !string.IsNullOrWhiteSpace(knownName)
            ? knownName!
            : world.Entities.TryGetValue(entityId, out var entity) && !string.IsNullOrWhiteSpace(entity.Name)
                ? entity.Name
                : entityId.Value;
}
