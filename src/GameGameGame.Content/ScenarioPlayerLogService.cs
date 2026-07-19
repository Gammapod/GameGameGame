using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed record ScenarioPlayerLogRequest(string ScenarioId, int TurnCount, EntityId? ObserverEntityId = null);

public static class ScenarioPlayerLogService
{
    private const string ProjectionKind = "player narrative projection";

    public static AgentScenarioPlayerLogReport Run(ContentEditorSession session, ScenarioPlayerLogRequest request)
    {
        var documentValidation = session.Editor.Validate();
        var canonicalValidation = session.Document.ValidateCanonicalAuthoring();
        var scenario = session.Editor.GetScenario(request.ScenarioId);
        var materialization = AlphaScenarioMaterializer.Materialize(session, new AgentAlphaScenarioDefinition(
            scenario.ScenarioId,
            scenario.Name,
            scenario.ScenarioRootEntityTemplateId,
            scenario.PlayerEntityTemplateId,
            scenario.PlayerEntityId,
            scenario.PlayerStart,
            scenario.PlayerControls)).ToAgentReport();
        var observerEntityId = request.ObserverEntityId ?? scenario.PlayerEntityId;

        var run = ScenarioRunService.RunPersistedWithHistory(
            session.Document,
            new PersistedScenarioRunRequest(request.ScenarioId, request.TurnCount));

        var rows = run.History is null || observerEntityId is null
            ? []
            : ProjectRows(run.History, observerEntityId.Value);
        var turns = rows
            .GroupBy(row => row.TurnNumber)
            .OrderBy(group => group.Key)
            .Select(group => new AgentScenarioPlayerLogTurn(
                group.Key,
                $"Turn {group.Key}",
                group.OrderBy(row => row.OrderIndex).Select(row => row.MessageId).ToList()))
            .ToList();

        return new AgentScenarioPlayerLogReport(
            request.ScenarioId,
            scenario.Name,
            session.FilePath,
            observerEntityId,
            request.TurnCount,
            ProjectionKind,
            documentValidation,
            canonicalValidation,
            materialization,
            run.Report.ValidationDiagnostics,
            run.Report.RuntimeFailures,
            run.Report.CapabilityGaps,
            turns,
            rows,
            BuildFollowUps(rows));
    }

    private static IReadOnlyList<AgentScenarioPlayerLogRow> ProjectRows(SimulationHistorySession history, EntityId observerEntityId)
    {
        var rows = new List<AgentScenarioPlayerLogRow>();
        var orderIndex = 0;
        foreach (var interval in history.Intervals.OrderBy(interval => interval.ToFrameIndex))
        {
            var world = history.Frames[interval.ToFrameIndex].Snapshot;
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
                    var messageId = BuildMessageId(stepKind, succeeded);
                    var messageArgs = BuildMessageArgs(outcome, attempt);
                    rows.Add(new AgentScenarioPlayerLogRow(
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
                        MessageId: messageId,
                        Variant: null,
                        Text: null,
                        TargetEntityId: null,
                        TargetDisplayName: null,
                        MessageArgs: messageArgs,
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

    private static IReadOnlyList<string> BuildFollowUps(IReadOnlyList<AgentScenarioPlayerLogRow> rows)
    {
        var followUps = new List<string>
        {
            "True line-of-sight/audibility filtering is not implemented; this report is labeled as a player narrative projection.",
            "ActionPlanId and target entity/name fields are null until autonomous actor outcome projection carries those structured anchors."
        };
        if (rows.Any(row => row.MessageArgs.ContainsKey("successRatio")))
        {
            followUps.Add("Aperture success ratios are exposed in messageArgs; shared ratio bucket names/thresholds are not yet available, so large/barely variants are not selected here.");
        }

        return followUps;
    }
}
