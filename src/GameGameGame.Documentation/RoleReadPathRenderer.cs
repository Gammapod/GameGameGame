using System.Text;

namespace GameGameGame.Documentation;

public static class RoleReadPathRenderer
{
    public static string Render(DocumentationGraph graph, string role)
    {
        return Render(RoleReadPathGenerator.Generate(graph, role, topic: null));
    }

    public static string Render(RoleReadPath path)
    {
        var builder = new StringBuilder();
        builder.AppendLine(path.Topic is null
            ? $"Read path for {path.Role}:"
            : $"Read path for {path.Role} filtered by topic '{path.Topic}':");

        if (path.Topic is null)
        {
            builder.AppendLine("For narrower follow-up discovery, rerun with --topic <topic>.");
        }

        if (path.UsedFallback && path.Topic is not null)
        {
            builder.AppendLine($"No documents in the {path.Role} read path matched topic '{path.Topic}'; showing the default read path.");
        }

        var index = 1;
        foreach (var document in path.Documents)
        {
            builder.AppendLine($"{index++}. {document.Metadata.Id} - {document.Path}");

            var match = path.Matches.FirstOrDefault(candidate => Equals(candidate.Document, document));
            if (!string.IsNullOrWhiteSpace(match?.Reason))
            {
                builder.AppendLine($"   Match: {match.Reason}");
            }

            var purpose = DocumentSummaryText.PurposeFor(document);
            if (!string.IsNullOrWhiteSpace(purpose))
            {
                builder.AppendLine($"   Purpose: {purpose}");
            }

            var summary = DocumentSummaryText.SummaryFor(document);
            if (!string.IsNullOrWhiteSpace(summary))
            {
                builder.AppendLine($"   Summary: {summary}");
            }
        }

        return builder.ToString();
    }
}
