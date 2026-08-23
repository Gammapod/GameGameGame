using System.Text.RegularExpressions;

namespace GameGameGame.Documentation;

public static partial class PlanningFreshnessAnalyzer
{
    public static IReadOnlyList<DocumentationDiagnostic> Analyze(DocumentationGraph graph)
    {
        var diagnostics = new List<DocumentationDiagnostic>();

        foreach (var document in graph.Documents.Where(IsActivePlanningDocument))
        {
            AnalyzeDocument(document, diagnostics);
        }

        return diagnostics;
    }

    private static void AnalyzeDocument(MarkdownDocument document, List<DocumentationDiagnostic> diagnostics)
    {
        var section = string.Empty;
        string? currentNowItem = null;
        var currentNowItemHasUpdateMarker = false;

        foreach (var rawLine in document.Body.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.TrimEnd();

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                FlushCurrentItem(document, diagnostics, currentNowItem, currentNowItemHasUpdateMarker);
                currentNowItem = null;
                currentNowItemHasUpdateMarker = false;
                section = line[3..].Trim();
                continue;
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                FlushCurrentItem(document, diagnostics, currentNowItem, currentNowItemHasUpdateMarker);
                currentNowItem = IsNowSection(section) ? line[4..].Trim() : null;
                currentNowItemHasUpdateMarker = false;
                continue;
            }

            if (currentNowItem is not null && LastUpdatedRegex().IsMatch(line))
            {
                currentNowItemHasUpdateMarker = true;
            }
        }

        FlushCurrentItem(document, diagnostics, currentNowItem, currentNowItemHasUpdateMarker);
    }

    private static void FlushCurrentItem(MarkdownDocument document, List<DocumentationDiagnostic> diagnostics, string? currentNowItem, bool hasUpdateMarker)
    {
        if (currentNowItem is null || hasUpdateMarker)
        {
            return;
        }

        diagnostics.Add(new DocumentationDiagnostic(
            DocumentationDiagnosticCodes.PlanningItemNeedsUpdateMarker,
            document.Path,
            $"Active Now planning item '{currentNowItem}' should include '**Last updated:** YYYY-MM-DD' so work sessions can update or deliberately leave planning state unchanged."));
    }

    private static bool IsActivePlanningDocument(MarkdownDocument document)
    {
        return string.Equals(document.Metadata.Status, "active", StringComparison.OrdinalIgnoreCase) &&
               (string.Equals(document.Metadata.Kind, "plan", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(document.Metadata.Kind, "rolling-board", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsNowSection(string section) => string.Equals(section, "Now", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"^\*\*Last updated:\*\*\s+\d{4}-\d{2}-\d{2}\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex LastUpdatedRegex();
}
