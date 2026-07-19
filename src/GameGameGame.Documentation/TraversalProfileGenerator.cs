namespace GameGameGame.Documentation;

public static class TraversalProfileGenerator
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Profiles = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["core-stable-behavior-change"] = ["navigation", "invariant-trace", "testing", "capability-matrix", "vertical-slice"],
        ["capability-support-change"] = ["navigation", "current-goals", "capability-matrix", "invariant-trace", "content-authoring", "vertical-slice"],
        ["content-authoring"] = ["navigation", "content-authoring"],
        ["content-gap-review"] = ["navigation", "content-authoring"],
        ["frontend-ux-change"] = ["navigation", "frontend-ux-invariants", "frontend-ux-standards", "frontend-ux-decisions"],
        ["canonical-action-slice"] = ["navigation", "current-goals", "invariant-trace", "testing", "capability-matrix", "content-authoring", "action-logic", "frontend-game-text", "vertical-slice"],
        ["sprint-wrapup"] = ["navigation", "current-goals"]
    };

    public static IReadOnlyList<string> ProfileIds => Profiles.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();

    public static TraversalProfile Generate(DocumentationGraph graph, string profileId)
    {
        if (!Profiles.TryGetValue(profileId, out var lanes))
        {
            return new TraversalProfile(profileId, [], []);
        }

        var documents = new List<MarkdownDocument>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var lane in lanes)
        {
            foreach (var document in graph.DocumentsForLane(lane))
            {
                if (document.Metadata.Id is null || !seen.Add(document.Metadata.Id))
                {
                    continue;
                }

                if (string.Equals(document.Metadata.Kind, "archived", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(document.Metadata.Status, "archived", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                documents.Add(document);
            }
        }

        return new TraversalProfile(profileId, lanes, documents);
    }

    public static IReadOnlyList<TraversalCoverageMetric> GenerateCoverageMetrics(DocumentationGraph graph)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var profileId in ProfileIds)
        {
            foreach (var document in Generate(graph, profileId).Documents)
            {
                if (document.Metadata.Id is null)
                {
                    continue;
                }

                counts[document.Metadata.Id] = counts.GetValueOrDefault(document.Metadata.Id) + 1;
            }
        }

        return graph.Documents
            .Where(document => document.Metadata.Id is not null)
            .Select(document => new TraversalCoverageMetric(document.Metadata.Id!, document.Path, counts.GetValueOrDefault(document.Metadata.Id!)))
            .OrderByDescending(metric => metric.ProfileCount)
            .ThenBy(metric => metric.DocumentId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
