using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed record PlayableScenarioSession(
    string ScenarioId,
    string Name,
    WorldState World,
    PrototypeContentRegistry Registry,
    IReadOnlyDictionary<EntityId, IEntityActionPlan> ActionPlans,
    EntityId PlayerEntityId,
    PlaneId ActivePlaneId,
    EntityId ActiveContainerEntityId,
    bool CanPlay,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps,
    IReadOnlyDictionary<string, IReadOnlyList<EntityId>>? PlayerControls = null,
    IReadOnlyList<ScenarioActorSummary>? ActorOrder = null)
{
    public IReadOnlyDictionary<string, IReadOnlyList<EntityId>> PlayerControls { get; } = PlayerControls ?? new Dictionary<string, IReadOnlyList<EntityId>>();

    public IReadOnlyList<ScenarioActorSummary> ActorOrder { get; } = ActorOrder ?? [];
}

public static class PlayableScenarioLauncher
{
    public static PlayableScenarioSession CreatePrototype()
    {
        var slice = PrototypeContent.CreateFirstSlice();
        return new PlayableScenarioSession(
            "prototype",
            "Prototype",
            slice.World,
            slice.Registry,
            slice.ActionPlans,
            PrototypeContent.PlayerId,
            PrototypeContent.GameInventoryPlaneId,
            PrototypeContent.GameId,
            CanPlay: true,
            [],
            [],
            []);
    }

    public static PlayableScenarioSession CreateFromFile(string path, string scenarioId)
    {
        var document = EditableContentDocument.LoadYaml(File.ReadAllText(path));
        return CreateFromDocument(document, scenarioId);
    }

    public static PlayableScenarioSession CreateFromCatalogEntry(ScenarioCatalogEntry entry) =>
        CreateFromFile(entry.ContentPath, entry.ScenarioId);

    public static PlayableScenarioSession CreateFromDocument(EditableContentDocument document, string scenarioId) =>
        CreateFromMaterialization(ScenarioMaterializer.Materialize(document, scenarioId));

    public static PlayableScenarioSession CreateFromMaterialization(ScenarioMaterializationResult result)
    {
        var activePlaneId = result.ScenarioPlaneId ?? ScenarioMaterializer.DefaultScenarioPlaneId;
        var firstControlledEntityId = result.PlayerControls.Values
            .SelectMany(entityIds => entityIds)
            .Select(entityId => (EntityId?)entityId)
            .FirstOrDefault();
        var materializedScenarioPlayerId = result.PlayerEntityId is { } scenarioPlayerId
            && result.World.Entities.ContainsKey(scenarioPlayerId)
            ? scenarioPlayerId
            : (EntityId?)null;
        var fallbackFocusEntityId = FindFirstEntityOnPlane(result.World, activePlaneId) ?? result.ScenarioRootEntityId;
        var playerEntityId = materializedScenarioPlayerId ?? firstControlledEntityId ?? fallbackFocusEntityId;

        var actorOrder = ScenarioInitiativeOrderService.GetScenarioActorsInInitiativeOrder(
            result.World,
            result.ActionPlans,
            result.ScenarioRootEntityId,
            activePlaneId);

        return new PlayableScenarioSession(
            result.ScenarioId,
            result.Name,
            result.World,
            result.Registry,
            result.ActionPlans,
            playerEntityId,
            activePlaneId,
            result.ScenarioRootEntityId,
            result.CanPlay,
            result.ValidationDiagnostics,
            result.RuntimeFailures,
            result.CapabilityGaps,
            result.PlayerControls,
            actorOrder);
    }

    private static EntityId? FindFirstEntityOnPlane(WorldState world, PlaneId planeId)
    {
        if (!world.Planes.TryGetValue(planeId, out var plane))
        {
            return null;
        }

        for (var y = 0; y < plane.Height; y++)
        {
            for (var x = 0; x < plane.Width; x++)
            {
                if (world.GetOccupant(new PlaneCoord(planeId, new GridCoord(x, y))) is { } occupant)
                {
                    return occupant;
                }
            }
        }

        return null;
    }
}
