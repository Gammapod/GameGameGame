using System.Text;

namespace GameGameGame.Documentation;

public static class MermaidClassDiagramRenderer
{
    public static string Render(DocumentationGraph graph)
    {
        var builder = new StringBuilder();
        builder.AppendLine("classDiagram");

        foreach (var document in graph.Documents.OrderBy(d => d.Metadata.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (document.Metadata.Id is null)
            {
                continue;
            }

            var nodeId = NodeId(document.Metadata.Id);
            builder.AppendLine($"  class {nodeId} {{");
            builder.AppendLine($"    id {Sanitize(document.Metadata.Id)}");
            builder.AppendLine($"    title {Sanitize(document.Metadata.Title ?? string.Empty)}");
            builder.AppendLine($"    kind {Sanitize(document.Metadata.Kind ?? string.Empty)}");
            builder.AppendLine($"    status {Sanitize(document.Metadata.Status ?? string.Empty)}");
            if (!string.IsNullOrWhiteSpace(document.Metadata.Lane))
            {
                builder.AppendLine($"    lane {Sanitize(document.Metadata.Lane)}");
            }

            builder.AppendLine($"    owners {Sanitize(string.Join(", ", document.Metadata.Owners))}");
            builder.AppendLine($"    audience {Sanitize(string.Join(", ", document.Metadata.Audience))}");
            if (document.Metadata.TruthRank is not null)
            {
                builder.AppendLine($"    truth_rank {document.Metadata.TruthRank}");
            }

            if (document.Metadata.TruthDomains.Count > 0)
            {
                builder.AppendLine($"    domains {Sanitize(string.Join(", ", document.Metadata.TruthDomains))}");
            }

            builder.AppendLine("  }");
        }

        foreach (var edge in graph.GetDiscoveryEdges())
        {
            builder.AppendLine($"  {NodeId(edge.From.Metadata.Id!)} --> {NodeId(edge.To.Metadata.Id!)} : {edge.Label}");
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

    private static string Sanitize(string value)
    {
        return value.Replace("{", "(", StringComparison.Ordinal)
            .Replace("}", ")", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
    }
}
