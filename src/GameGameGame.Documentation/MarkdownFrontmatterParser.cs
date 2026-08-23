namespace GameGameGame.Documentation;

public static class MarkdownFrontmatterParser
{
    public static MarkdownDocument Parse(string path, string markdown)
    {
        var normalized = markdown.Replace("\r\n", "\n");
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
        {
            return new MarkdownDocument(path, CreateMetadata(new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)), markdown);
        }

        var end = normalized.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (end < 0)
        {
            return new MarkdownDocument(path, CreateMetadata(new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)), markdown);
        }

        var frontmatter = normalized[4..end];
        var body = normalized[(end + "\n---\n".Length)..];
        return new MarkdownDocument(path, CreateMetadata(ParseFields(frontmatter)), body);
    }

    private static Dictionary<string, List<string>> ParseFields(string frontmatter)
    {
        var fields = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        string? currentListKey = null;

        foreach (var rawLine in frontmatter.Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal) && currentListKey is not null)
            {
                fields[currentListKey].Add(Unquote(trimmed[2..].Trim()));
                continue;
            }

            currentListKey = null;
            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var key = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            fields[key] = ParseValue(value);

            if (value.Length == 0)
            {
                currentListKey = key;
            }
        }

        return fields;
    }

    private static List<string> ParseValue(string value)
    {
        if (value.Length == 0)
        {
            return [];
        }

        if (value == "[]")
        {
            return [];
        }

        if (value.StartsWith('[') && value.EndsWith(']'))
        {
            var inner = value[1..^1].Trim();
            if (inner.Length == 0)
            {
                return [];
            }

            return inner.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(Unquote)
                .ToList();
        }

        return [Unquote(value)];
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }

    private static DocumentMetadata CreateMetadata(Dictionary<string, List<string>> fields)
    {
        return new DocumentMetadata(
            Scalar(fields, "id"),
            Scalar(fields, "title"),
            Scalar(fields, "purpose"),
            Scalar(fields, "kind"),
            Scalar(fields, "subkind"),
            Scalar(fields, "status"),
            List(fields, "owners"),
            List(fields, "audience"),
            Scalar(fields, "lane"),
            List(fields, "read_when"),
            List(fields, "do_not_read_when"),
            List(fields, "related"),
            List(fields, "supersedes"),
            Scalar(fields, "superseded_by"),
            List(fields, "code_refs"),
            List(fields, "test_refs"),
            Scalar(fields, "truth_rank"),
            int.TryParse(Scalar(fields, "truth_rank"), out var truthRank) ? truthRank : null,
            List(fields, "truth_domains"),
            fields.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value, StringComparer.OrdinalIgnoreCase));
    }

    private static string? Scalar(Dictionary<string, List<string>> fields, string key)
    {
        return fields.TryGetValue(key, out var values) && values.Count > 0 && !string.IsNullOrWhiteSpace(values[0])
            ? values[0]
            : null;
    }

    private static IReadOnlyList<string> List(Dictionary<string, List<string>> fields, string key)
    {
        return fields.TryGetValue(key, out var values)
            ? values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray()
            : [];
    }
}
