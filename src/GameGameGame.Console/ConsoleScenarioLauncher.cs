using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.ConsoleApp;

public sealed record ConsoleGameSession(
    string ScenarioId,
    WorldState World,
    PrototypeContentRegistry Registry,
    IReadOnlyDictionary<EntityId, IEntityActionPlan> ActionPlans,
    EntityId PlayerEntityId,
    PlaneId ActivePlaneId,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps);

public static class ConsoleScenarioLauncher
{
    public static ConsoleGameSession CreatePrototype()
    {
        var slice = PrototypeContent.CreateFirstSlice();
        return new ConsoleGameSession(
            "prototype",
            slice.World,
            slice.Registry,
            slice.ActionPlans,
            PrototypeContent.PlayerId,
            PrototypeContent.GameInventoryPlaneId,
            [],
            [],
            []);
    }

    public static ConsoleGameSession CreateFromFile(string path, string scenarioId)
    {
        var document = EditableContentDocument.LoadYaml(File.ReadAllText(path));
        return CreateFromDocument(document, scenarioId);
    }

    public static ConsoleGameSession CreateFromDocument(EditableContentDocument document, string scenarioId)
    {
        var result = ScenarioMaterializer.Materialize(document, scenarioId);
        if (!result.CanPlay || result.ScenarioPlaneId is not { } activePlaneId)
        {
            return new ConsoleGameSession(
                result.ScenarioId,
                result.World,
                result.Registry,
                result.ActionPlans,
                result.PlayerEntityId,
                result.ScenarioPlaneId ?? ScenarioMaterializer.DefaultScenarioPlaneId,
                result.ValidationDiagnostics,
                result.RuntimeFailures,
                result.CapabilityGaps);
        }

        return new ConsoleGameSession(
            result.ScenarioId,
            result.World,
            result.Registry,
            result.ActionPlans,
            result.PlayerEntityId,
            activePlaneId,
            result.ValidationDiagnostics,
            result.RuntimeFailures,
            result.CapabilityGaps);
    }
}
