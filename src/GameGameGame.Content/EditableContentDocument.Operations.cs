using GameGameGame.Core;
using System.Text;

namespace GameGameGame.Content;

public sealed partial class EditableContentDocument
{
    public EntityTemplateId AddEntityTemplate(string name, EntityTemplate template, EntityPresentation presentation)
    {
        var id = GenerateEntityTemplateId(name);
        EntityTemplates[id.Value] = EntityTemplateDto.From(template);
        Presentations[id.Value] = EntityPresentationDto.From(presentation);

        return id;
    }

    public void UpsertScenario(ScenarioDefinition scenario) =>
        Scenarios[scenario.ScenarioId] = ScenarioDefinitionDto.From(scenario);

    public ScenarioDefinition GetScenario(string scenarioId) =>
        Scenarios.TryGetValue(scenarioId, out var scenario)
            ? scenario.ToDefinition(scenarioId)
            : throw new KeyNotFoundException($"Scenario {scenarioId} does not exist.");

    private EntityTemplateId GenerateEntityTemplateId(string name)
    {
        var baseId = ToCamelCaseId(name);
        var candidate = baseId;
        var suffix = 2;

        while (EntityTemplates.ContainsKey(candidate) || Presentations.ContainsKey(candidate))
        {
            candidate = $"{baseId}{suffix}";
            suffix++;
        }

        return new EntityTemplateId(candidate);
    }

    private static string ToCamelCaseId(string name)
    {
        var builder = new StringBuilder();
        var capitalizeNext = false;

        foreach (var character in name)
        {
            if (!char.IsLetterOrDigit(character))
            {
                capitalizeNext = builder.Length > 0;
                continue;
            }

            if (builder.Length == 0)
            {
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            builder.Append(capitalizeNext ? char.ToUpperInvariant(character) : character);
            capitalizeNext = false;
        }

        return builder.Length == 0 ? "entity" : builder.ToString();
    }
}
