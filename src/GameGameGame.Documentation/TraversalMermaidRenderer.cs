using System.Text;

namespace GameGameGame.Documentation;

public static class TraversalMermaidRenderer
{
    public static string Render(TraversalProfile profile)
    {
        var builder = new StringBuilder();
        builder.AppendLine("flowchart LR");

        var taskNode = NodeId("task." + profile.Id);
        builder.AppendLine($"  {taskNode}(({profile.Id}))");

        MarkdownDocument? previous = null;
        foreach (var document in profile.Documents)
        {
            if (document.Metadata.Id is null)
            {
                continue;
            }

            var current = NodeId(document.Metadata.Id);
            builder.AppendLine($"  {current}[\"{document.Metadata.Title ?? document.Metadata.Id}\"]");
            builder.AppendLine(previous is null
                ? $"  {taskNode} --> {current}"
                : $"  {NodeId(previous.Metadata.Id!)} --> {current}");
            previous = document;
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
}
