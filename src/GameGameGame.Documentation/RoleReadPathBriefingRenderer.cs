using System.Text;

namespace GameGameGame.Documentation;

public static class RoleReadPathBriefingRenderer
{
    public static string Render(DocumentationGraph graph, string role)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Read path briefing for {role}:");

        var index = 1;
        foreach (var document in RoleReadPathGenerator.Generate(graph, role))
        {
            builder.AppendLine($"{index++}. {document.Metadata.Id} - {document.Path}");

            var purpose = DocumentSummaryText.PurposeFor(document);
            if (!string.IsNullOrWhiteSpace(purpose))
            {
                builder.AppendLine($"   Purpose: {purpose}");
            }

            if (document.Metadata.ReadWhen.Count > 0)
            {
                builder.AppendLine($"   Read when: {string.Join("; ", document.Metadata.ReadWhen)}");
            }

            if (document.Metadata.DoNotReadWhen.Count > 0)
            {
                builder.AppendLine($"   Skip when: {string.Join("; ", document.Metadata.DoNotReadWhen)}");
            }
        }

        return builder.ToString();
    }
}
