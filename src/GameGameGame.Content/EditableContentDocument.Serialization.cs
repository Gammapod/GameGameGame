using GameGameGame.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GameGameGame.Content;

public sealed partial class EditableContentDocument
{
    public static EditableContentDocument LoadYaml(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<EditableContentDocument>(yaml) ?? new EditableContentDocument();
    }

    public string SaveYaml()
    {
        var canonical = LoadYaml(SerializeYaml());
        canonical.CanonicalizeLegacyActionPlanVariableFields();

        return canonical.SerializeYaml();
    }

    private string SerializeYaml()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        return serializer.Serialize(this);
    }

    public PrototypeContentRegistry ToRegistry() => YamlContentLoader.LoadRegistry(SerializeYaml());
}
