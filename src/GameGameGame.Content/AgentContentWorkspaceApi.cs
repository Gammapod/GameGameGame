using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed class AgentContentWorkspaceApi(IReadOnlyList<ContentWorkspaceDocument> documents)
{
    public ContentWorkspace Workspace { get; } = new(documents);

    public AgentApiResult<AgentWorkspaceValidationReport> ValidateWorkspace() =>
        Try("ValidateWorkspaceFailed", () =>
        {
            var compile = ContentCompiler.Compile(Workspace);
            return new AgentWorkspaceValidationReport(
                compile.Validation,
                AgentWorkspaceDocument.FromSummaries(compile.WorkspaceDocuments));
        });

    public AgentApiResult<AgentWorkspaceReport> ListWorkspace() =>
        Try("ListWorkspaceFailed", () =>
        {
            var compile = ContentCompiler.Compile(Workspace);
            return new AgentWorkspaceReport(
                AgentWorkspaceDocument.FromSummaries(compile.WorkspaceDocuments),
                compile.Symbols,
                compile.References,
                compile.Diagnostics);
        });

    public AgentApiResult<AgentScenarioRunReport> RunWorkspaceScenarioById(string scenarioId, int turnCount, AgentScenarioRunOptions? options = null) =>
        Try("RunWorkspaceScenarioByIdFailed", () => ToAgentReport(ScenarioRunService.Run(
            Workspace,
            new PersistedScenarioRunRequest(scenarioId, turnCount, ToScenarioRunOptions(options)))));

    private static AgentScenarioRunReport ToAgentReport(ScenarioRunReport report) =>
        new AgentScenarioRunReport(
            report.ScenarioRootEntityTemplateId,
            report.ScenarioRootEntityId,
            report.ScenarioPlaneId,
            report.ActorOrder
                .Select(actor => new AgentScenarioActorSummary(actor.EntityId, actor.Name, actor.Location))
                .ToList(),
            report.Turns
                .Select(turn => new AgentScenarioTurnReport(turn.TurnNumber, turn.InitiativeIndex, turn.ActorId, turn.ActorName, turn.TraceLines))
                .ToList(),
            report.SetupLines,
            report.FinalStateLines,
            report.InventorySummaryLines,
            report.ValidationDiagnostics,
            report.RuntimeObservations,
            report.RuntimeFailures,
            report.CapabilityGaps)
        {
            DebugReportLines = FormatDebugReport(report)
        };

    private static ScenarioRunOptions ToScenarioRunOptions(AgentScenarioRunOptions? options) =>
        options is null
            ? new ScenarioRunOptions()
            : new ScenarioRunOptions(options.IgnorePlayerChoiceControl, options.TraceActorFilter, options.IncludeAllTraces);

    private static IReadOnlyList<string> FormatDebugReport(ScenarioRunReport report)
    {
        var lines = new List<string>();
        AddSection(lines, "Setup", report.SetupLines);
        AddSection(lines, "Validation diagnostics", report.ValidationDiagnostics);
        AddSection(lines, "Runtime observations", report.RuntimeObservations);
        AddSection(lines, "Runtime failures", report.RuntimeFailures);
        AddSection(lines, "Capability gaps", report.CapabilityGaps);
        lines.Add("Turn-by-turn traces:");
        if (report.Turns.Count == 0)
        {
            lines.Add("  (none)");
        }
        else
        {
            foreach (var turn in report.Turns)
            {
                lines.Add($"  Turn {turn.TurnNumber}, initiative {turn.InitiativeIndex}, {turn.ActorName} {turn.ActorId}:");
                lines.AddRange(turn.TraceLines.Select(line => $"    {line}"));
            }
        }
        AddSection(lines, "Final state", report.FinalStateLines);
        AddSection(lines, "Nested inventory summary", report.InventorySummaryLines);
        return lines;
    }

    private static void AddSection(List<string> lines, string heading, IReadOnlyList<string> sectionLines)
    {
        lines.Add($"{heading}:");
        lines.AddRange(sectionLines.Count == 0 ? ["  (none)"] : sectionLines.Select(line => $"  {line}"));
    }

    private static AgentApiResult<T> Try<T>(string code, Func<T> operation)
    {
        try
        {
            return AgentApiResult<T>.Success(operation());
        }
        catch (Exception ex)
        {
            return AgentApiResult<T>.Failure(AgentApiError.FromException(code, ex));
        }
    }
}

public sealed record AgentWorkspaceValidationReport(
    ContentValidationResult Validation,
    IReadOnlyList<AgentWorkspaceDocument> Documents);

public sealed record AgentWorkspaceReport(
    IReadOnlyList<AgentWorkspaceDocument> Documents,
    IReadOnlyList<ContentSymbol> Symbols,
    IReadOnlyList<ContentReference> References,
    IReadOnlyList<ContentDiagnostic> Diagnostics);

public sealed record AgentWorkspaceDocument(
    string? DocumentId,
    string? SourcePath,
    ContentWorkspaceSourceKind SourceKind,
    bool IsProtected,
    bool IsDirty,
    bool HasProtectedMutation,
    bool CanSaveInPlace,
    int LoadOrder)
{
    public static IReadOnlyList<AgentWorkspaceDocument> FromSummaries(IReadOnlyList<ContentWorkspaceDocumentSummary> summaries) =>
        summaries
            .Select(summary => new AgentWorkspaceDocument(
                summary.DocumentId,
                summary.SourcePath,
                summary.SourceKind,
                summary.IsReadOnly,
                summary.IsDirty,
                summary.HasProtectedMutation,
                CanSaveInPlace: !string.IsNullOrWhiteSpace(summary.SourcePath),
                summary.LoadOrder))
            .ToList();
}
