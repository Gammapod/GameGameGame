using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed class FrontendEditorService(ContentEditorSession session)
{
    public ContentEditorSession Session { get; } = session;

    public static FrontendEditorOpenResult OpenFile(string path)
    {
        var result = ContentEditorSession.OpenFile(path);
        return result.IsSuccess
            ? FrontendEditorOpenResult.Success(new FrontendEditorService(result.Session!))
            : FrontendEditorOpenResult.Failure(result.ErrorMessage ?? $"Could not open content file {path}.");
    }

    public static FrontendEditorService CreateNew() => new(ContentEditorSession.CreateNew());

    public FrontendEditorSnapshot GetSnapshot()
    {
        var validation = Session.Editor.Validate();
        var canonicalValidation = Session.Document.ValidateCanonicalAuthoring();
        var diagnostics = validation.Diagnostics
            .Concat(canonicalValidation.Diagnostics)
            .Select(FrontendEditorDiagnostic.From)
            .ToList();

        return new FrontendEditorSnapshot(
            Session.FilePath,
            Session.IsDirty,
            ListScenarios(),
            ListEntityTemplates(),
            ListActionPlans(),
            diagnostics,
            Session.GetYamlPreview(),
            Session.GetYamlDiff().Lines);
    }

    public FrontendEditorScenarioPreview PreviewScenario(string scenarioId)
    {
        var session = PlayableScenarioLauncher.CreateFromDocument(Session.Document, scenarioId);

        return new FrontendEditorScenarioPreview(
            session.ScenarioId,
            session.Name,
            IsDerivedRuntimeState: true,
            session.CanPlay,
            session,
            session.ValidationDiagnostics,
            session.RuntimeFailures,
            session.CapabilityGaps);
    }

    private IReadOnlyList<FrontendEditorScenarioSummary> ListScenarios() =>
        Session.Document.Scenarios
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry =>
            {
                var scenario = entry.Value.ToDefinition(entry.Key);
                return new FrontendEditorScenarioSummary(
                    scenario.ScenarioId,
                    scenario.Name,
                    scenario.ScenarioRootEntityTemplateId.Value,
                    scenario.PlayerEntityTemplateId.Value,
                    scenario.PlayerEntityId.Value,
                    scenario.PlayerStart);
            })
            .ToList();

    private IReadOnlyList<FrontendEditorEntityTemplateSummary> ListEntityTemplates() =>
        Session.Editor.ListEntityPresets()
            .Select(model => new FrontendEditorEntityTemplateSummary(
                model.Id.Value,
                model.Template.Name,
                model.Presentation.Glyph,
                model.Presentation.Color,
                model.Template.InventoryWidth,
                model.Template.InventoryHeight,
                model.Template.Bulk,
                model.Template.Aperture,
                model.Template.DefaultActionPlanId?.Value,
                (model.Template.CarriedEntities ?? [])
                    .OrderBy(carried => carried.Coord.Y)
                    .ThenBy(carried => carried.Coord.X)
                    .ThenBy(carried => carried.EntityId.Value, StringComparer.Ordinal)
                    .Select(carried => new FrontendEditorCarriedEntitySummary(
                        carried.EntityId.Value,
                        carried.TemplateId?.Value,
                        carried.Coord))
                    .ToList()))
            .ToList();

    private IReadOnlyList<FrontendEditorActionPlanSummary> ListActionPlans() =>
        Session.Editor.ListActionPlans()
            .Select(model => new FrontendEditorActionPlanSummary(
                model.TemplateId.Value,
                ContentEditorService.FormatActionPlanShape(ActionPlanShapeClassifier.Classify(model.Descriptor)),
                GetActionStepNames(model.Descriptor)))
            .ToList();

    private static IReadOnlyList<string> GetActionStepNames(ActionPlanDescriptor descriptor)
    {
        if (descriptor.Behavior?.Steps.Count > 0)
        {
            return descriptor.Behavior.Steps
                .Select(step => ActionStepCatalog.Get(step.Kind).DisplayName)
                .ToList();
        }

        if (descriptor.Primitive is { } primitive)
        {
            return [primitive.Kind.ToString()];
        }

        if (descriptor.Steps.Count > 0)
        {
        return descriptor.Steps.Select(step => step.Label).ToList();
        }

        return [];
    }
}

public sealed record FrontendEditorOpenResult(FrontendEditorService? Service, string? ErrorMessage)
{
    public bool IsSuccess => Service is not null;

    public static FrontendEditorOpenResult Success(FrontendEditorService service) => new(service, ErrorMessage: null);

    public static FrontendEditorOpenResult Failure(string errorMessage) => new(Service: null, errorMessage);
}

public sealed record FrontendEditorSnapshot(
    string? FilePath,
    bool IsDirty,
    IReadOnlyList<FrontendEditorScenarioSummary> Scenarios,
    IReadOnlyList<FrontendEditorEntityTemplateSummary> EntityTemplates,
    IReadOnlyList<FrontendEditorActionPlanSummary> ActionPlans,
    IReadOnlyList<FrontendEditorDiagnostic> ValidationDiagnostics,
    string YamlPreview,
    IReadOnlyList<string> YamlDiffLines);

public sealed record FrontendEditorScenarioSummary(
    string ScenarioId,
    string Name,
    string ScenarioRootEntityTemplateId,
    string PlayerEntityTemplateId,
    string PlayerEntityId,
    GridCoord PlayerStart);

public sealed record FrontendEditorEntityTemplateSummary(
    string TemplateId,
    string Name,
    char Glyph,
    PresentationColor Color,
    int InventoryWidth,
    int InventoryHeight,
    int Bulk,
    int Aperture,
    string? DefaultActionPlanId,
    IReadOnlyList<FrontendEditorCarriedEntitySummary> CarriedEntities);

public sealed record FrontendEditorCarriedEntitySummary(
    string EntityId,
    string? TemplateId,
    GridCoord Coord);

public sealed record FrontendEditorActionPlanSummary(
    string ActionPlanId,
    string Shape,
    IReadOnlyList<string> ActionStepNames);

public sealed record FrontendEditorDiagnostic(
    ContentDiagnosticSeverity Severity,
    ContentDiagnosticCode Code,
    string Message,
    string? EntityTemplateId,
    string? ActionPlanId,
    int? StepIndex,
    string? CarriedEntityId,
    GridCoord? Coord)
{
    public static FrontendEditorDiagnostic From(ContentDiagnostic diagnostic) =>
        new(
            diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.EntityTemplateId?.Value,
            diagnostic.ActionPlanTemplateId?.Value,
            diagnostic.StepIndex,
            diagnostic.CarriedEntityId?.Value,
            diagnostic.Coord);
}

public sealed record FrontendEditorScenarioPreview(
    string ScenarioId,
    string Name,
    bool IsDerivedRuntimeState,
    bool CanPlay,
    PlayableScenarioSession Session,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps);
