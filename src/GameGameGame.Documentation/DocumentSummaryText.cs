namespace GameGameGame.Documentation;

public static class DocumentSummaryText
{
    public static string? PurposeFor(MarkdownDocument document)
    {
        return string.IsNullOrWhiteSpace(document.Metadata.Purpose)
            ? document.Metadata.Title
            : document.Metadata.Purpose;
    }

    public static string? SummaryFor(MarkdownDocument document)
    {
        if (!string.IsNullOrWhiteSpace(document.Metadata.Summary))
        {
            return document.Metadata.Summary;
        }

        foreach (var rawLine in document.Body.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith('-'))
            {
                continue;
            }

            return line.Length <= 180 ? line : $"{line[..177]}...";
        }

        return null;
    }
}
