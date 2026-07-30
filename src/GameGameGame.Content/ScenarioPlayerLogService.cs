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

        var projectedRows = run.History is null || observerEntityId is null
            ? []
            : PlayerNarrativeLogProjection.Project(new PlayerNarrativeLogProjectionRequest(run.History, observerEntityId.Value));
        var rows = projectedRows.Select(ToAgentRow).ToList();
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
            BuildFollowUps(projectedRows));
    }

    private static AgentScenarioPlayerLogRow ToAgentRow(PlayerNarrativeLogRow row) => new(
        row.TurnNumber,
        row.InitiativeIndex,
        row.OrderIndex,
        row.ActorEntityId,
        row.ActorDisplayName,
        row.ActionPlanId,
        row.ActionStepKind,
        row.ActionStepIndex,
        row.Succeeded,
        row.Result,
        row.MessageId,
        row.Variant,
        row.Text,
        row.TargetEntityId,
        row.TargetDisplayName,
        row.MessageArgs,
        row.IsPlayerVisible);

    private static IReadOnlyList<string> BuildFollowUps(IReadOnlyList<PlayerNarrativeLogRow> rows)
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
