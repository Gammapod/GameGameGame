using GameGameGame.Core;

namespace GameGameGame.Content;

public static class ContentCompiler
{
    public static ContentCompileResult Compile(EditableContentDocument document, ContentCompileOptions? options = null)
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
