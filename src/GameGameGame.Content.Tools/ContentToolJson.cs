using System.Text.Json;
using System.Text.Json.Serialization;
using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Content.Tools;

public static class ContentToolJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions(writeIndented: false);

    public static JsonSerializerOptions CreateOptions(bool writeIndented)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = writeIndented
        };
        options.Converters.Add(new EntityTemplateIdJsonConverter());
        options.Converters.Add(new ActionPlanTemplateIdJsonConverter());
        options.Converters.Add(new ActionPlanIdJsonConverter());
        options.Converters.Add(new EntityIdJsonConverter());
        options.Converters.Add(new PlaneIdJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

internal sealed class EntityTemplateIdJsonConverter : StringValueJsonConverter<EntityTemplateId>
{
    protected override EntityTemplateId Create(string value) => new(value);
    protected override string GetValue(EntityTemplateId value) => value.Value;
}

internal sealed class ActionPlanTemplateIdJsonConverter : StringValueJsonConverter<ActionPlanTemplateId>
{
    protected override ActionPlanTemplateId Create(string value) => new(value);
    protected override string GetValue(ActionPlanTemplateId value) => value.Value;
}

internal sealed class ActionPlanIdJsonConverter : StringValueJsonConverter<ActionPlanId>
{
    protected override ActionPlanId Create(string value) => new(value);
    protected override string GetValue(ActionPlanId value) => value.Value;
}

internal sealed class EntityIdJsonConverter : StringValueJsonConverter<EntityId>
{
    protected override EntityId Create(string value) => new(value);
    protected override string GetValue(EntityId value) => value.Value;
}

internal sealed class PlaneIdJsonConverter : StringValueJsonConverter<PlaneId>
{
    protected override PlaneId Create(string value) => new(value);
    protected override string GetValue(PlaneId value) => value.Value;
}

internal abstract class StringValueJsonConverter<T> : JsonConverter<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return Create(reader.GetString() ?? string.Empty);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            if (document.RootElement.TryGetProperty("value", out var value) || document.RootElement.TryGetProperty("Value", out value))
            {
                return Create(value.GetString() ?? string.Empty);
            }
        }

        throw new JsonException($"Expected a string or {{ value }} object for {typeof(T).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(GetValue(value));

    protected abstract T Create(string value);

    protected abstract string GetValue(T value);
}
