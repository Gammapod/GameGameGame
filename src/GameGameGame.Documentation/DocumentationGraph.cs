namespace GameGameGame.Documentation;

public sealed class DocumentationGraph
{
    private readonly Dictionary<string, MarkdownDocument> _documentsById;

    private DocumentationGraph(IReadOnlyList<MarkdownDocument> documents)
    {
        Documents = documents;
        _documentsById = documents
            .Where(d => !string.IsNullOrWhiteSpace(d.Metadata.Id))
            .GroupBy(d => d.Metadata.Id!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<MarkdownDocument> Documents { get; }

    public static DocumentationGraph Build(IEnumerable<MarkdownDocument> documents) => new(documents.ToArray());

    public bool TryGetDocument(string id, out MarkdownDocument document) => _documentsById.TryGetValue(id, out document!);

    public bool ContainsId(string id) => _documentsById.ContainsKey(id);

    public IReadOnlyList<MarkdownDocument> DocumentsForLane(string lane)
    {
        return Documents
            .Where(d => string.Equals(d.Metadata.Lane, lane, StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => d.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<DocumentationGraphEdge> GetDiscoveryEdges()
    {
        var edges = new List<DocumentationGraphEdge>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var document in Documents.OrderBy(d => d.Metadata.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (document.Metadata.Id is null)
            {
                continue;
            }

            foreach (var related in document.Metadata.Related)
            {
                if (!TryGetDocument(related, out var relatedDocument) || relatedDocument.Metadata.Id is null)
                {
                    continue;
                }

                var (from, to) = NormalizeDiscoveryDirection(document, relatedDocument);
                var key = $"{from.Metadata.Id}->{to.Metadata.Id}:related";
                if (seen.Add(key))
                {
                    edges.Add(new DocumentationGraphEdge(from, to, "related", DeriveDiscoveryLabel(to)));
                }
            }
        }

        return edges;
    }

    private static (MarkdownDocument From, MarkdownDocument To) NormalizeDiscoveryDirection(MarkdownDocument origin, MarkdownDocument target)
    {
        var originRank = origin.Metadata.TruthRank ?? int.MaxValue;
        var targetRank = target.Metadata.TruthRank ?? int.MaxValue;

        if (originRank < targetRank)
        {
            return (origin, target);
        }

        if (targetRank < originRank)
        {
            return (target, origin);
        }

        return (origin, target);
    }

    private static string DeriveDiscoveryLabel(MarkdownDocument target)
    {
        if (string.Equals(target.Metadata.Kind, "archived", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(target.Metadata.Status, "archived", StringComparison.OrdinalIgnoreCase))
        {
            return "historical context";
        }

        var domains = target.Metadata.TruthDomains;
        if (HasAny(domains, "runtime-behavior", "stable-contract", "test-trace"))
        {
            return "behavior/test trace";
        }

        if (HasAny(domains, "capability-support", "parity-tier"))
        {
            return "capability support";
        }

        if (HasAny(domains, "authorability", "content-workflow"))
        {
            return "authoring guidance";
        }

        if (HasAny(domains, "gap-workflow"))
        {
            return "gap workflow";
        }

        if (HasAny(domains, "action-logic"))
        {
            return "action outcome logic";
        }

        if (HasAny(domains, "frontend-boundary"))
        {
            return "frontend boundary";
        }

        if (HasAny(domains, "frontend-presentation"))
        {
            return "frontend presentation";
        }

        if (HasAny(domains, "frontend-rationale"))
        {
            return "decision rationale";
        }

        if (HasAny(domains, "implementation-navigation"))
        {
            return "implementation path";
        }

        if (HasAny(domains, "planning-priority"))
        {
            return "planning context";
        }

        return "related";
    }

    private static bool HasAny(IReadOnlyList<string> domains, params string[] expected)
    {
        return domains.Any(domain => expected.Contains(domain, StringComparer.OrdinalIgnoreCase));
    }
}
