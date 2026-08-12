using GameGameGame.Core;

namespace GameGameGame.Content;

public static class ContentCompiler
{
    public static ContentCompileResult Compile(EditableContentDocument document, ContentCompileOptions? options = null)
    {
        var workspaceDocument = new ContentWorkspaceDocument(
            document,
            options?.DocumentId,
            options?.SourcePath,
            SourceKind: ContentWorkspaceSourceKind.Compatibility);

        return Compile(new ContentWorkspace([workspaceDocument]));
    }

    public static ContentCompileResult Compile(ContentWorkspace workspace, ContentCompileOptions? options = null)
    {
        if (workspace.Documents.Count == 1)
        {
            var document = workspace.Documents[0];
            var documentOptions = new ContentCompileOptions(
                options?.DocumentId ?? document.DocumentId,
                options?.SourcePath ?? document.SourcePath);
            var result = CompileSingleDocument(document.Document, documentOptions);

            return result with
            {
                WorkspaceDocuments = Summaries(workspace)
            };
        }

        var results = workspace.Documents
            .Select(document =>
            {
                var documentOptions = new ContentCompileOptions(document.DocumentId, document.SourcePath);
                return CompileSingleDocument(document.Document, documentOptions);
            })
            .ToList();

        var symbols = results.SelectMany(result => result.Symbols).ToList();
        var references = ResolveWorkspaceReferences(results.SelectMany(result => result.References).ToList(), symbols);
        var diagnostics = results.SelectMany(result => result.Diagnostics)
            .Where(diagnostic => !IsLocallyMissingReferenceResolvedByWorkspace(diagnostic, references))
            .Concat(DuplicateSymbolDiagnostics(symbols))
            .Concat(AmbiguousReferenceDiagnostics(references))
            .ToList();

        PrototypeContentRegistry? registry = null;
        if (diagnostics.All(diagnostic => diagnostic.Severity != ContentDiagnosticSeverity.Error))
        {
            try
            {
                registry = ComposeDocument(workspace).ToRegistry();
                diagnostics.AddRange(registry.Validate().Diagnostics);
                diagnostics = diagnostics.Distinct().ToList();
            }
            catch (Exception ex)
            {
                diagnostics.Add(ContentDiagnostic.Error(
                    ContentDiagnosticCode.General,
                    $"Content workspace could not be composed: {ex.Message}"));
            }
        }

        return new ContentCompileResult(
            Registry: registry,
            Validation: new ContentValidationResult(diagnostics),
            Symbols: symbols,
            References: references,
            WorkspaceDocuments: Summaries(workspace));
    }

    private static ContentCompileResult CompileSingleDocument(EditableContentDocument document, ContentCompileOptions? options = null)
    {
        try
        {
            var registry = document.ToRegistry();
            var diagnostics = registry.Validate().Diagnostics
                .Concat(document.ValidateCanonicalAuthoring().Diagnostics)
                .Distinct()
                .Select(diagnostic => ApplyAttribution(diagnostic, options))
                .ToList();

            var index = ContentReferenceIndex.Build(document, options);

            return new ContentCompileResult(
                registry,
                new ContentValidationResult(diagnostics),
                index.Symbols,
                index.References);
        }
        catch (Exception ex)
        {
            return new ContentCompileResult(
                Registry: null,
                Validation: new ContentValidationResult([
                    ApplyAttribution(ContentDiagnostic.Error(
                        ContentDiagnosticCode.General,
                        $"Content document could not be compiled: {ex.Message}"), options)
                ]),
                Symbols: [],
                References: []);
        }
    }

    private static IReadOnlyList<ContentWorkspaceDocumentSummary> Summaries(ContentWorkspace workspace) =>
        workspace.Documents
            .Select((document, index) => new ContentWorkspaceDocumentSummary(
                document.DocumentId,
                document.SourcePath,
                document.SourceKind,
                document.IsReadOnly,
                document.IsDirty,
                LoadOrder: index,
                document.HasProtectedMutation))
            .ToList();

    private static IEnumerable<ContentDiagnostic> DuplicateSymbolDiagnostics(IReadOnlyList<ContentSymbol> symbols) =>
        symbols
            .GroupBy(symbol => new { symbol.Kind, symbol.Id })
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Select(symbol => ContentDiagnostic.Error(
                ContentDiagnosticCode.DuplicateSymbolDeclaration,
                $"Duplicate {symbol.Kind} symbol '{symbol.Id}' is declared in multiple workspace documents.",
                documentId: symbol.DocumentId,
                sourcePath: symbol.SourcePath,
                symbolKind: symbol.Kind.ToString(),
                symbolId: symbol.Id)));

    internal static EditableContentDocument ComposeDocument(ContentWorkspace workspace)
    {
        var document = new EditableContentDocument();
        foreach (var workspaceDocument in workspace.Documents)
        {
            foreach (var (id, value) in workspaceDocument.Document.EntityTemplates)
            {
                document.EntityTemplates[id] = value;
            }

            foreach (var (id, value) in workspaceDocument.Document.Presentations)
            {
                document.Presentations[id] = value;
            }

            foreach (var (id, value) in workspaceDocument.Document.PresentationCatalog ?? [])
            {
                document.PresentationCatalog ??= [];
                document.PresentationCatalog[id] = value;
            }

            foreach (var (id, value) in workspaceDocument.Document.Palettes ?? [])
            {
                document.Palettes ??= [];
                document.Palettes[id] = value;
            }

            foreach (var (id, value) in workspaceDocument.Document.ActionPlans)
            {
                document.ActionPlans[id] = value;
            }

            foreach (var (id, value) in workspaceDocument.Document.Scenarios)
            {
                document.Scenarios[id] = value;
            }

            foreach (var (id, value) in workspaceDocument.Document.MergedLayers)
            {
                document.MergedLayers[id] = value;
            }
        }

        return document;
    }

    private static IReadOnlyList<ContentReference> ResolveWorkspaceReferences(
        IReadOnlyList<ContentReference> references,
        IReadOnlyList<ContentSymbol> symbols) =>
        references
            .Select(reference => reference with
            {
                Resolution = ResolveWorkspaceReference(reference, symbols)
            })
            .ToList();

    private static ContentReferenceResolution ResolveWorkspaceReference(
        ContentReference reference,
        IReadOnlyList<ContentSymbol> symbols)
    {
        var matches = symbols
            .Where(symbol => symbol.Kind == reference.TargetKind && symbol.Id == reference.TargetId)
            .Take(2)
            .Count();

        return matches switch
        {
            0 => reference.Resolution,
            1 => ContentReferenceResolution.Resolved,
            _ => ContentReferenceResolution.Ambiguous
        };
    }

    private static IEnumerable<ContentDiagnostic> AmbiguousReferenceDiagnostics(IReadOnlyList<ContentReference> references) =>
        references
            .Where(reference => reference.Resolution == ContentReferenceResolution.Ambiguous)
            .Select(reference => ContentDiagnostic.Error(
                ContentDiagnosticCode.AmbiguousSymbolReference,
                $"{reference.SourceKind} symbol '{reference.SourceId}' references ambiguous {reference.TargetKind} symbol '{reference.TargetId}'.",
                documentId: reference.DocumentId,
                sourcePath: reference.SourcePath,
                symbolKind: reference.TargetKind.ToString(),
                symbolId: reference.TargetId));

    private static bool IsLocallyMissingReferenceResolvedByWorkspace(
        ContentDiagnostic diagnostic,
        IReadOnlyList<ContentReference> references) =>
        diagnostic.Code switch
        {
            ContentDiagnosticCode.MissingActionPlanReference => IsActionPlanReferenceResolved(diagnostic, references),
            ContentDiagnosticCode.MissingTargetTemplateReference => IsEntityTemplateReferenceResolved(diagnostic, references),
            ContentDiagnosticCode.MissingCarriedEntityTemplateReference => IsEntityTemplateReferenceResolved(diagnostic, references),
            ContentDiagnosticCode.UnknownPresentationId => IsPresentationCatalogReferenceResolved(diagnostic, references, ContentReferenceKind.PresentationId),
            ContentDiagnosticCode.UnknownPaletteId => IsPresentationCatalogReferenceResolved(diagnostic, references, ContentReferenceKind.PaletteId),
            ContentDiagnosticCode.InvalidScenarioDefinition when diagnostic.Message.Contains("references missing", StringComparison.OrdinalIgnoreCase) => IsScenarioReferenceResolved(diagnostic, references),
            _ => false
        };

    private static bool IsActionPlanReferenceResolved(ContentDiagnostic diagnostic, IReadOnlyList<ContentReference> references)
    {
        var targetId = diagnostic.ActionPlanTemplateId?.Value ?? diagnostic.ReferencedActionPlanId?.Value;
        return targetId is not null && HasResolvedReference(diagnostic.DocumentId, references, ContentSymbolKind.ActionPlan, targetId);
    }

    private static bool IsEntityTemplateReferenceResolved(ContentDiagnostic diagnostic, IReadOnlyList<ContentReference> references)
    {
        var targetId = diagnostic.ReferencedEntityTemplateId?.Value ?? diagnostic.EntityTemplateId?.Value;
        return targetId is not null && HasResolvedReference(diagnostic.DocumentId, references, ContentSymbolKind.EntityTemplate, targetId);
    }

    private static bool IsScenarioReferenceResolved(ContentDiagnostic diagnostic, IReadOnlyList<ContentReference> references)
    {
        var targetId = diagnostic.EntityTemplateId?.Value;
        return targetId is not null && HasResolvedReference(diagnostic.DocumentId, references, ContentSymbolKind.EntityTemplate, targetId);
    }

    private static bool IsPresentationCatalogReferenceResolved(
        ContentDiagnostic diagnostic,
        IReadOnlyList<ContentReference> references,
        ContentReferenceKind kind)
    {
        var sourceId = diagnostic.EntityTemplateId?.Value;
        return sourceId is not null
            && references.Any(reference =>
                reference.DocumentId == diagnostic.DocumentId
                && reference.Kind == kind
                && reference.SourceId == sourceId
                && reference.Resolution == ContentReferenceResolution.Resolved);
    }

    private static bool HasResolvedReference(
        string? documentId,
        IReadOnlyList<ContentReference> references,
        ContentSymbolKind targetKind,
        string targetId) =>
        references.Any(reference =>
            reference.DocumentId == documentId
            && reference.TargetKind == targetKind
            && reference.TargetId == targetId
            && reference.Resolution == ContentReferenceResolution.Resolved);

    private static ContentDiagnostic ApplyAttribution(ContentDiagnostic diagnostic, ContentCompileOptions? options)
    {
        if (options is null)
        {
            return diagnostic;
        }

        return diagnostic with
        {
            DocumentId = options.DocumentId,
            SourcePath = options.SourcePath
        };
    }
}
