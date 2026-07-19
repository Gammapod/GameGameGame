using System.Text;

namespace GameGameGame.Documentation;

public static class MermaidGraphRenderer
{
    public static string Render(DocumentationGraph graph)
    {
        return Render(graph, highlightedTraversal: null);
    }

    public static string Render(DocumentationGraph graph, TraversalProfile? highlightedTraversal)
    {
        var builder = new StringBuilder();
        builder.AppendLine("flowchart TD");

        foreach (var document in graph.Documents.OrderBy(d => d.Metadata.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (document.Metadata.Id is null)
            {
                continue;
            }

            builder.AppendLine($"  {NodeId(document.Metadata.Id)}[\"{EscapeLabel(document.Metadata.Title ?? document.Metadata.Id)}<br/><small>{EscapeLabel(document.Metadata.Id)}</small>\"]");
        }

        foreach (var edge in graph.GetDiscoveryEdges())
        {
            builder.AppendLine($"  {NodeId(edge.From.Metadata.Id!)} -->|{EscapeEdgeLabel(edge.Label)}| {NodeId(edge.To.Metadata.Id!)}");
        }

        if (highlightedTraversal is not null)
        {
            var highlightedNodeIds = highlightedTraversal.Documents
                .Select(document => document.Metadata.Id)
                .Where(id => id is not null)
                .Select(id => NodeId(id!))
                .ToArray();

            if (highlightedNodeIds.Length > 0)
            {
                builder.AppendLine("  classDef traversal fill:#ffe8a3,stroke:#d18b00,stroke-width:3px;");
                builder.AppendLine($"  class {string.Join(',', highlightedNodeIds)} traversal");
            }
        }

        return builder.ToString();
    }

    private static string NodeId(string id)
    {
        var builder = new StringBuilder(id.Length);
        foreach (var c in id)
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        return builder.ToString();
    }

    private static string EscapeLabel(string label)
    {
        return label.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("[", "&#91;", StringComparison.Ordinal)
            .Replace("]", "&#93;", StringComparison.Ordinal);
    }

    private static string EscapeEdgeLabel(string label)
    {
        return label.Replace("|", "/", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
    }
}
