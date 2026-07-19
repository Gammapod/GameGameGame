namespace GameGameGame.Documentation;

public static class RoleReadPathGenerator
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RoleLanes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["core-owner"] = ["navigation", "invariant-trace", "testing", "capability-matrix", "vertical-slice"],
        ["content-editor"] = ["navigation", "content-authoring"],
        ["frontend-owner"] = ["navigation", "frontend-ux-invariants", "frontend-ux-standards", "frontend-ux-decisions"]
    };

    public static IReadOnlyList<MarkdownDocument> Generate(DocumentationGraph graph, string role)
    {
        if (!RoleLanes.TryGetValue(role, out var lanes))
        {
            return [];
        }

        var result = new List<MarkdownDocument>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var lane in lanes)
        {
            foreach (var document in graph.DocumentsForLane(lane))
            {
                if (document.Metadata.Id is null || !seen.Add(document.Metadata.Id))
                {
                    continue;
                }

                if (string.Equals(document.Metadata.Status, "archived", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(document.Metadata.Kind, "archived", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(document);
            }
        }

        return result;
    }
}
