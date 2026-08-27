using System.Collections;
using System.Reflection;
using YamlDotNet.RepresentationModel;

namespace GameGameGame.Content;

internal static class StrictYamlPropertyValidator
{
    public static IReadOnlyList<ContentDiagnostic> ValidateContentDocument(string yaml, string? sourcePath = null) =>
        Validate(yaml, typeof(EditableContentDocument), sourcePath)
            .Select(result => ContentDiagnostic.Error(
                ContentDiagnosticCode.UnknownYamlProperty,
                result.Message,
                sourcePath: sourcePath))
            .ToList();

    public static IReadOnlyList<string> ValidateManifest<TManifestDto>(string yaml, string manifestPath) =>
        Validate(yaml, typeof(TManifestDto), manifestPath)
            .Select(result => $"{manifestPath}: {result.Message}")
            .ToList();

    private static IReadOnlyList<UnknownYamlProperty> Validate(string yaml, Type rootType, string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return [];
        }

        var stream = new YamlStream();
        using var reader = new StringReader(yaml);
        stream.Load(reader);
        if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is not YamlMappingNode mapping)
        {
            return [];
        }

        var results = new List<UnknownYamlProperty>();
        ValidateMapping(mapping, rootType, string.Empty, results);
        return results;
    }

    private static void ValidateNode(YamlNode node, Type expectedType, string path, List<UnknownYamlProperty> results)
    {
        expectedType = Nullable.GetUnderlyingType(expectedType) ?? expectedType;

        if (IsScalarLike(expectedType))
        {
            return;
        }

        if (TryGetDictionaryValueType(expectedType, out var dictionaryValueType))
        {
            if (node is not YamlMappingNode dictionary)
            {
                return;
            }

            foreach (var entry in dictionary.Children)
            {
                var key = ScalarKey(entry.Key);
                ValidateNode(entry.Value, dictionaryValueType, AppendPath(path, key), results);
            }

            return;
        }

        if (TryGetEnumerableElementType(expectedType, out var elementType))
        {
            if (node is not YamlSequenceNode sequence)
            {
                return;
            }

            for (var index = 0; index < sequence.Children.Count; index++)
            {
                ValidateNode(sequence.Children[index], elementType, $"{path}[{index}]", results);
            }

            return;
        }

        if (node is YamlMappingNode mapping)
        {
            ValidateMapping(mapping, expectedType, path, results);
        }
    }

    private static void ValidateMapping(YamlMappingNode mapping, Type dtoType, string path, List<UnknownYamlProperty> results)
    {
        var properties = GetYamlProperties(dtoType);
        foreach (var entry in mapping.Children)
        {
            var propertyName = ScalarKey(entry.Key);
            if (!properties.TryGetValue(propertyName, out var property))
            {
                var propertyPath = AppendPath(path, propertyName);
                results.Add(new UnknownYamlProperty(propertyPath, propertyName, Suggest(propertyName, properties.Keys)));
                continue;
            }

            ValidateNode(entry.Value, property.PropertyType, AppendPath(path, propertyName), results);
        }
    }

    private static Dictionary<string, PropertyInfo> GetYamlProperties(Type type) =>
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetMethod is not null && property.SetMethod is not null)
            .ToDictionary(property => ToCamelCase(property.Name), StringComparer.Ordinal);

    private static bool TryGetDictionaryValueType(Type type, out Type valueType)
    {
        var dictionaryType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)
            ? type
            : type.GetInterfaces().FirstOrDefault(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        if (dictionaryType is null)
        {
            valueType = typeof(object);
            return false;
        }

        valueType = dictionaryType.GetGenericArguments()[1];
        return true;
    }

    private static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        if (type == typeof(string))
        {
            elementType = typeof(object);
            return false;
        }

        if (type.IsArray)
        {
            elementType = type.GetElementType() ?? typeof(object);
            return true;
        }

        var enumerableType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumerableType is null || !typeof(IEnumerable).IsAssignableFrom(type))
        {
            elementType = typeof(object);
            return false;
        }

        elementType = enumerableType.GetGenericArguments()[0];
        return true;
    }

    private static bool IsScalarLike(Type type) =>
        type.IsPrimitive
        || type.IsEnum
        || type == typeof(string)
        || type == typeof(decimal)
        || type == typeof(Guid);

    private static string ScalarKey(YamlNode node) =>
        node is YamlScalarNode scalar ? scalar.Value ?? string.Empty : node.ToString() ?? string.Empty;

    private static string AppendPath(string path, string property) =>
        string.IsNullOrEmpty(path) ? property : $"{path}.{property}";

    private static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value) || char.IsLower(value[0])
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];

    private static string? Suggest(string unknown, IEnumerable<string> candidates) =>
        candidates
            .Select(candidate => new { Candidate = candidate, Distance = Levenshtein(unknown, candidate) })
            .Where(item => item.Distance <= Math.Max(2, unknown.Length / 3))
            .OrderBy(item => item.Distance)
            .ThenBy(item => item.Candidate, StringComparer.Ordinal)
            .FirstOrDefault()
            ?.Candidate;

    private static int Levenshtein(string left, string right)
    {
        var distances = new int[left.Length + 1, right.Length + 1];
        for (var i = 0; i <= left.Length; i++)
        {
            distances[i, 0] = i;
        }

        for (var j = 0; j <= right.Length; j++)
        {
            distances[0, j] = j;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }

        return distances[left.Length, right.Length];
    }

    private sealed record UnknownYamlProperty(string Path, string PropertyName, string? Suggestion)
    {
        public string Message => Suggestion is null
            ? $"Unknown YAML property `{PropertyName}` at `{Path}`."
            : $"Unknown YAML property `{PropertyName}` at `{Path}`. Did you mean `{Suggestion}`?";
    }
}
