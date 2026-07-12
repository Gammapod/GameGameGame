namespace GameGameGame.Content;

internal static class ContentEditorIdHelpers
{
    public static string ToCamelCaseId(string name)
    {
        var result = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select((part, index) => index == 0
                ? char.ToLowerInvariant(part[0]) + part[1..]
                : char.ToUpperInvariant(part[0]) + part[1..]));

        return string.IsNullOrWhiteSpace(result) ? "entity" : result;
    }

    public static string UppercaseFirst(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
