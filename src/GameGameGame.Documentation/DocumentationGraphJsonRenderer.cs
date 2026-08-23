using System.Text.Json;

namespace GameGameGame.Documentation;

public static class DocumentationGraphJsonRenderer
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string Render(DocumentationGraph graph)
    {
        var payload = new
        {
            documents = graph.Documents
                .OrderBy(document => document.Metadata.Id, StringComparer.OrdinalIgnoreCase)
                .Select(document => new
                {
                    id = document.Metadata.Id,
                    title = document.Metadata.Title,
                    purpose = document.Metadata.Purpose,
                    summary = document.Metadata.Summary,
                    path = document.Path,
                    kind = document.Metadata.Kind,
                    status = document.Metadata.Status,
                    owners = document.Metadata.Owners,
                    audience = document.Metadata.Audience,
                    lane = document.Metadata.Lane,
                    read_when = document.Metadata.ReadWhen,
                    do_not_read_when = document.Metadata.DoNotReadWhen,
                    truth_rank = document.Metadata.TruthRank,
                    truth_domains = document.Metadata.TruthDomains
                }),
            edges = graph.GetDiscoveryEdges()
                .Select(edge => new
                {
                    from = edge.From.Metadata.Id,
                    to = edge.To.Metadata.Id,
                    kind = edge.Kind,
                    label = edge.Label
                })
        };

        return JsonSerializer.Serialize(payload, Options);
    }
}
