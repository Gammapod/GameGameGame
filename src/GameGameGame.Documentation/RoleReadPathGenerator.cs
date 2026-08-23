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
        return GenerateDefault(graph, role);
    }

    public static RoleReadPath Generate(DocumentationGraph graph, string role, string? topic)
    {
        var defaultPath = GenerateDefault(graph, role);
        if (string.IsNullOrWhiteSpace(topic))
        {
            return new RoleReadPath(defaultPath, role, null, UsedFallback: false, Matches: []);
        }

        var matches = defaultPath
            .Select(document => Score(document, topic))
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => IndexOf(defaultPath, match.Document))
            .ToArray();

        if (matches.Length == 0)
        {
            return new RoleReadPath(defaultPath, role, topic, UsedFallback: true, Matches: []);
        }

        var filtered = new List<MarkdownDocument>();
        var navigation = defaultPath.FirstOrDefault(document => string.Equals(document.Metadata.Lane, "navigation", StringComparison.OrdinalIgnoreCase));
        if (navigation is not null)
        {
            filtered.Add(navigation);
        }

        foreach (var match in matches)
        {
            if (!filtered.Contains(match.Document))
            {
                filtered.Add(match.Document);
            }
        }

        return new RoleReadPath(filtered, role, topic, UsedFallback: false, Matches: matches);
    }

    private static IReadOnlyList<MarkdownDocument> GenerateDefault(DocumentationGraph graph, string role)
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

    private static int IndexOf(IReadOnlyList<MarkdownDocument> documents, MarkdownDocument document)
    {
        for (var i = 0; i < documents.Count; i++)
        {
            if (ReferenceEquals(documents[i], document) || Equals(documents[i], document))
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    private static RoleReadPathMatch Score(MarkdownDocument document, string topic)
    {
        var score = 0;
        var reasons = new List<string>();

        AddScore(document.Metadata.Id, 6, "id");
        AddScore(document.Metadata.Title, 6, "title");
        AddScore(document.Metadata.Lane, 5, "lane");
        AddScore(document.Metadata.Purpose, 4, "purpose");
        AddScore(document.Metadata.Summary, 4, "summary");
        AddListScore(document.Metadata.ReadWhen, 3, "read_when");
        AddListScore(document.Metadata.TruthDomains, 3, "truth_domains");
        AddListScore(document.Metadata.Related, 2, "related");
        AddScore(document.Body, 1, "body");

        var reason = reasons.Count == 0
            ? string.Empty
            : $"matched topic '{topic}' in {string.Join(", ", reasons.Distinct(StringComparer.OrdinalIgnoreCase))}";

        return new RoleReadPathMatch(document, score, reason);

        void AddScore(string? value, int weight, string field)
        {
            if (value is not null && value.Contains(topic, StringComparison.OrdinalIgnoreCase))
            {
                score += weight;
                reasons.Add(field);
            }
        }

        void AddListScore(IReadOnlyList<string> values, int weight, string field)
        {
            if (values.Any(value => value.Contains(topic, StringComparison.OrdinalIgnoreCase)))
            {
                score += weight;
                reasons.Add(field);
            }
        }
    }
}
