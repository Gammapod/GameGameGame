namespace GameGameGame.Content;

public static class ContentScenarioSurfaceService
{
    public static ContentScenarioSurface Build(
        EditableContentDocument document,
        string scenarioId,
        ContentCompileOptions? options = null)
    {
        var workspace = ContentWorkspaceSurfaceService.Build(document, options);
        var scenarioSymbol = workspace.Symbols.FirstOrDefault(symbol =>
            symbol.Kind == ContentSymbolKind.Scenario
            && string.Equals(symbol.Id, scenarioId, StringComparison.Ordinal));

        var selectedScenario = scenarioSymbol is null
            ? new ContentWorkspaceSymbolSummary(ContentSymbolKind.Scenario, scenarioId, scenarioId, options?.DocumentId, options?.SourcePath)
            : new ContentWorkspaceSymbolSummary(scenarioSymbol.Kind, scenarioSymbol.Id, scenarioSymbol.DisplayName, scenarioSymbol.DocumentId, scenarioSymbol.SourcePath);

        document.Scenarios.TryGetValue(scenarioId, out var scenario);
        var selectedReferences = workspace.References
            .Where(reference => reference.SourceKind == ContentSymbolKind.Scenario && string.Equals(reference.SourceId, scenarioId, StringComparison.Ordinal))
            .ToList();
        var selectedDiagnostics = workspace.Diagnostics
            .Where(diagnostic => IsSelectedScenarioDiagnostic(diagnostic, scenarioId, scenario, selectedReferences))
            .ToList();
        var selectedSet = selectedDiagnostics.ToHashSet();
        var globalDiagnostics = workspace.Diagnostics
            .Where(diagnostic => !selectedSet.Contains(diagnostic))
            .ToList();

        var dependencies = BuildDependencySymbols(workspace, scenario, selectedReferences);

        return new ContentScenarioSurface(
            workspace,
            selectedScenario,
            scenario?.ScenarioRootEntityTemplateId,
            scenario?.PlayerEntityTemplateId,
            scenario?.PlayerEntityId,
            ToSurfaceCoord(scenario?.PlayerStart),
            selectedDiagnostics,
            globalDiagnostics,
            selectedReferences,
            dependencies);
    }

    private static bool IsSelectedScenarioDiagnostic(
        ContentDiagnostic diagnostic,
        string scenarioId,
        EditableContentDocument.ScenarioDefinitionDto? scenario,
        IReadOnlyList<ContentReference> selectedReferences)
    {
        if (diagnostic.Code != ContentDiagnosticCode.InvalidScenarioDefinition)
        {
            return false;
        }

        if (diagnostic.Message.Contains($"Scenario {scenarioId}", StringComparison.Ordinal))
        {
            return true;
        }

        return diagnostic.EntityTemplateId is { } templateId
            && selectedReferences.Any(reference =>
                reference.TargetKind == ContentSymbolKind.EntityTemplate
                && string.Equals(reference.TargetId, templateId.Value, StringComparison.Ordinal));
    }

    private static IReadOnlyList<ContentSymbol> BuildDependencySymbols(
        ContentWorkspaceSurface workspace,
        EditableContentDocument.ScenarioDefinitionDto? scenario,
        IReadOnlyList<ContentReference> selectedReferences)
    {
        var keys = new HashSet<(ContentSymbolKind Kind, string Id)>();

        foreach (var reference in selectedReferences.Where(reference => reference.Resolution == ContentReferenceResolution.Resolved))
        {
            keys.Add((reference.TargetKind, reference.TargetId));
        }

        AddTemplateDependencies(workspace, scenario?.ScenarioRootEntityTemplateId, keys, new HashSet<string>(StringComparer.Ordinal));
        AddTemplateDependencies(workspace, scenario?.PlayerEntityTemplateId, keys, new HashSet<string>(StringComparer.Ordinal));

        return workspace.Symbols
            .Where(symbol => keys.Contains((symbol.Kind, symbol.Id)))
            .OrderBy(symbol => symbol.Kind)
            .ThenBy(symbol => symbol.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static void AddTemplateDependencies(
        ContentWorkspaceSurface workspace,
        string? templateId,
        HashSet<(ContentSymbolKind Kind, string Id)> keys,
        HashSet<string> visitedTemplates)
    {
        if (string.IsNullOrWhiteSpace(templateId) || !visitedTemplates.Add(templateId))
        {
            return;
        }

        keys.Add((ContentSymbolKind.EntityTemplate, templateId));

        var templateReferences = workspace.References.Where(reference =>
            reference.SourceKind == ContentSymbolKind.EntityTemplate
            && string.Equals(reference.SourceId, templateId, StringComparison.Ordinal)
            && reference.Resolution == ContentReferenceResolution.Resolved);

        foreach (var reference in templateReferences)
        {
            keys.Add((reference.TargetKind, reference.TargetId));
            if (reference.TargetKind == ContentSymbolKind.EntityTemplate)
            {
                AddTemplateDependencies(workspace, reference.TargetId, keys, visitedTemplates);
            }
        }
    }

    private static ContentSurfaceGridCoord? ToSurfaceCoord(EditableContentDocument.GridCoordDto? coord) =>
        coord is null ? null : new ContentSurfaceGridCoord(coord.X, coord.Y);
}

public sealed record ContentScenarioSurface(
    ContentWorkspaceSurface Workspace,
    ContentWorkspaceSymbolSummary SelectedScenario,
    string? RootTemplateId,
    string? PlayerTemplateId,
    string? PlayerEntityId,
    ContentSurfaceGridCoord? PlayerStart,
    IReadOnlyList<ContentDiagnostic> SelectedScenarioDiagnostics,
    IReadOnlyList<ContentDiagnostic> GlobalDiagnostics,
    IReadOnlyList<ContentReference> SelectedScenarioReferences,
    IReadOnlyList<ContentSymbol> DependencySymbols);

public sealed record ContentSurfaceGridCoord(int X, int Y);
