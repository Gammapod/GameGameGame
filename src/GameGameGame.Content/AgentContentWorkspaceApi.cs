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

    public AgentApiResult<AgentScenarioRunReport> RunWorkspaceScenarioById(string scenarioId, int turnCount) =>
        Try("RunWorkspaceScenarioByIdFailed", () => ToAgentReport(ScenarioRunService.Run(
            Workspace,
            new PersistedScenarioRunRequest(scenarioId, turnCount))));

    private static AgentScenarioRunReport ToAgentReport(ScenarioRunReport report) =>
        new(
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
            report.CapabilityGaps);

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
