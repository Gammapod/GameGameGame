using System.Text.RegularExpressions;

namespace GameGameGame.Documentation;

public static partial class DocumentationLinter
{
    private static readonly HashSet<string> KnownRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "core-owner",
        "content-editor",
        "frontend-owner"
    };

    private static readonly HashSet<string> KnownKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "source-of-truth",
        "plan",
        "roadmap",
        "backlog-reference",
        "gap-log",
        "retrospective",
        "archived",
        "generated"
    };

    private static readonly HashSet<string> KnownLanes = new(StringComparer.OrdinalIgnoreCase)
    {
        "navigation",
        "invariant-trace",
        "testing",
        "capability-matrix",
        "content-authoring",
        "action-logic",
        "frontend-game-text",
        "frontend-ux-invariants",
        "frontend-ux-standards",
        "frontend-ux-decisions",
        "ux-spec",
        "vertical-slice",
        "current-goals",
        "glossary",
        "design-notes"
    };

    private static readonly HashSet<string> KnownTruthDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "runtime-behavior",
        "stable-contract",
        "test-trace",
        "capability-support",
        "parity-tier",
        "authorability",
        "content-workflow",
        "gap-workflow",
        "action-logic",
        "frontend-boundary",
        "frontend-presentation",
        "frontend-rationale",
        "planning-priority",
        "implementation-navigation",
        "testing-policy",
        "process",
        "navigation"
    };

    public static DocumentationLintResult Lint(DocumentationGraph graph, string repositoryRoot)
    {
        var diagnostics = new List<DocumentationDiagnostic>();

        foreach (var document in graph.Documents)
        {
            LintRequiredMetadata(document, diagnostics);
            LintRoles(document, diagnostics);
            LintKindAndLane(document, diagnostics);
            LintTruthMetadata(document, diagnostics);
            LintDocumentReferences(graph, document, diagnostics);
            LintPathRules(document, diagnostics);
            LintMarkdownLinks(repositoryRoot, document, diagnostics);
            LintPathReferences(repositoryRoot, document, diagnostics);
        }

        LintDuplicateIds(graph, diagnostics);

        return new DocumentationLintResult(diagnostics);
    }

    private static void LintRequiredMetadata(MarkdownDocument document, List<DocumentationDiagnostic> diagnostics)
    {
        Require(document, diagnostics, document.Metadata.Id, "id");
        Require(document, diagnostics, document.Metadata.Title, "title");
        Require(document, diagnostics, document.Metadata.Kind, "kind");
        Require(document, diagnostics, document.Metadata.Status, "status");

        if (document.Metadata.Owners.Count == 0)
        {
            diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.MissingRequiredMetadata, document.Path, "Missing required metadata field 'owners'."));
        }

        if (document.Metadata.Audience.Count == 0)
        {
            diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.MissingRequiredMetadata, document.Path, "Missing required metadata field 'audience'."));
        }
    }

    private static void Require(MarkdownDocument document, List<DocumentationDiagnostic> diagnostics, string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.MissingRequiredMetadata, document.Path, $"Missing required metadata field '{field}'."));
        }
    }

    private static void LintRoles(MarkdownDocument document, List<DocumentationDiagnostic> diagnostics)
    {
        foreach (var owner in document.Metadata.Owners.Where(role => !KnownRoles.Contains(role)))
        {
            diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.UnknownRole, document.Path, $"Unknown owner role '{owner}'."));
        }

        foreach (var audience in document.Metadata.Audience.Where(role => !KnownRoles.Contains(role)))
        {
            diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.UnknownRole, document.Path, $"Unknown audience role '{audience}'."));
        }
    }

    private static void LintKindAndLane(MarkdownDocument document, List<DocumentationDiagnostic> diagnostics)
    {
        if (document.Metadata.Kind is { } kind && !KnownKinds.Contains(kind))
        {
            diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.UnknownKind, document.Path, $"Unknown document kind '{kind}'."));
        }

        if (!string.Equals(document.Metadata.Kind, "source-of-truth", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(document.Metadata.Lane))
        {
            diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.MissingSourceOfTruthMetadata, document.Path, "Source-of-truth document is missing 'lane'."));
        }
        else if (!KnownLanes.Contains(document.Metadata.Lane))
        {
            diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.UnknownLane, document.Path, $"Unknown source-of-truth lane '{document.Metadata.Lane}'."));
        }

        if (document.Metadata.ReadWhen.Count == 0)
        {
            diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.MissingSourceOfTruthMetadata, document.Path, "Source-of-truth document is missing non-empty 'read_when'."));
        }
    }

    private static void LintDocumentReferences(DocumentationGraph graph, MarkdownDocument document, List<DocumentationDiagnostic> diagnostics)
    {
        foreach (var reference in document.Metadata.Related.Concat(document.Metadata.Supersedes).Concat(document.Metadata.SupersededBy is null ? [] : [document.Metadata.SupersededBy]))
        {
            if (!graph.ContainsId(reference))
            {
                diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.UnresolvedDocumentReference, document.Path, $"Document reference '{reference}' does not resolve to a compiled document ID."));
            }
        }
    }

    private static void LintTruthMetadata(MarkdownDocument document, List<DocumentationDiagnostic> diagnostics)
    {
        if (document.Metadata.TruthRankRaw is not null && document.Metadata.TruthRank is null)
        {
            diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.InvalidTruthRank, document.Path, $"Truth rank '{document.Metadata.TruthRankRaw}' is not an integer."));
        }

        foreach (var domain in document.Metadata.TruthDomains.Where(domain => !KnownTruthDomains.Contains(domain)))
        {
            diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.UnknownTruthDomain, document.Path, $"Unknown truth domain '{domain}'."));
        }
    }

    private static void LintDuplicateIds(DocumentationGraph graph, List<DocumentationDiagnostic> diagnostics)
    {
        foreach (var group in graph.Documents.Where(d => !string.IsNullOrWhiteSpace(d.Metadata.Id)).GroupBy(d => d.Metadata.Id!, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Count() <= 1)
            {
                continue;
            }

            foreach (var document in group)
            {
                diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.DuplicateDocumentId, document.Path, $"Duplicate document ID '{group.Key}'."));
            }
        }
    }

    private static void LintPathRules(MarkdownDocument document, List<DocumentationDiagnostic> diagnostics)
    {
        var normalized = document.Path.Replace('\\', '/');
        if (normalized.StartsWith("docs/Source of Truth/", StringComparison.OrdinalIgnoreCase) && !string.Equals(document.Metadata.Kind, "source-of-truth", StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.SourceOfTruthPathKindMismatch, document.Path, "Document under docs/Source of Truth should use kind 'source-of-truth'."));
        }

        if (normalized.StartsWith("docs/Archived/", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(document.Metadata.Kind, "archived", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(document.Metadata.Status, "archived", StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.ArchivedPathStatusMismatch, document.Path, "Document under docs/Archived should use kind 'archived' or status 'archived'."));
        }

        if (normalized.StartsWith("docs/Archived/", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(document.Metadata.Kind, "plan", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(document.Metadata.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.ActivePlanInArchive, document.Path, "Active implementation plan should not live under docs/Archived."));
        }
    }

    private static void LintMarkdownLinks(string repositoryRoot, MarkdownDocument document, List<DocumentationDiagnostic> diagnostics)
    {
        foreach (Match match in MarkdownLinkRegex().Matches(document.Body))
        {
            var target = match.Groups[1].Value;
            if (ShouldSkipLink(target))
            {
                continue;
            }

            var withoutAnchor = target.Split('#')[0];
            if (string.IsNullOrWhiteSpace(withoutAnchor))
            {
                continue;
            }

            var documentDirectory = Path.GetDirectoryName(document.Path) ?? string.Empty;
            var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, documentDirectory, withoutAnchor));
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.UnresolvedMarkdownLink, document.Path, $"Markdown link '{target}' does not resolve."));
            }
        }
    }

    private static void LintPathReferences(string repositoryRoot, MarkdownDocument document, List<DocumentationDiagnostic> diagnostics)
    {
        foreach (var reference in document.Metadata.CodeRefs.Concat(document.Metadata.TestRefs.Where(IsPathLikeReference)))
        {
            var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, reference));
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                diagnostics.Add(new DocumentationDiagnostic(DocumentationDiagnosticCodes.UnresolvedPathReference, document.Path, $"Path reference '{reference}' does not resolve."));
            }
        }
    }

    private static bool IsPathLikeReference(string reference) => reference.Contains('/') || reference.Contains('\\');

    private static bool ShouldSkipLink(string target)
    {
        return target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               target.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"\[[^\]]+\]\(([^)]+)\)")]
    private static partial Regex MarkdownLinkRegex();
}
