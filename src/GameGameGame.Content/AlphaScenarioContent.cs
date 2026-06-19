namespace GameGameGame.Content;

public static class AlphaScenarioContent
{
    public const string DefaultScenarioId = "alpha-smoke";

    private const string ResourceName = "GameGameGame.Content.AlphaScenarioContent.yaml";

    public static EditableContentDocument LoadDocument()
    {
        using var stream = typeof(AlphaScenarioContent).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded YAML content resource {ResourceName} was not found.");
        using var reader = new StreamReader(stream);

        return EditableContentDocument.LoadYaml(reader.ReadToEnd());
    }

    public static Stream OpenStream() =>
        typeof(AlphaScenarioContent).Assembly.GetManifestResourceStream(ResourceName)
        ?? throw new InvalidOperationException($"Embedded YAML content resource {ResourceName} was not found.");
}
