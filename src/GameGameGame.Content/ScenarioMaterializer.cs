using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed record ScenarioMaterializationResult(
    string ScenarioId,
    string Name,
    EntityTemplateId ScenarioRootEntityTemplateId,
    EntityId ScenarioRootEntityId,
    EntityTemplateId PlayerEntityTemplateId,
    EntityId PlayerEntityId,
    PlaneId? ScenarioPlaneId,
    PlaneCoord? PlayerLocation,
    WorldState World,
    IReadOnlyDictionary<EntityId, IEntityActionPlan> ActionPlans,
    PrototypeContentRegistry Registry,
    IReadOnlyList<string> SetupLines,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps)
{
    public bool CanPlay => ValidationDiagnostics.Count == 0 && RuntimeFailures.Count == 0 && ScenarioPlaneId is not null && PlayerLocation is not null;
}

public static class ScenarioMaterializer
{
    public static readonly EntityId DefaultScenarioRootEntityId = new("scenarioRoot");
    public static readonly PlaneId DefaultScenarioHostPlaneId = new("scenarioHost");
    public static readonly PlaneId DefaultScenarioPlaneId = new("scenarioRoot");

    public static ScenarioMaterializationResult Materialize(EditableContentDocument document, string scenarioId) =>
        Materialize(document, document.GetScenario(scenarioId));

    public static ScenarioMaterializationResult Materialize(EditableContentDocument document, ScenarioDefinition scenario)
    {
        var validationDiagnostics = document.ValidateCanonicalAuthoring().Errors.ToList();
        var runtimeFailures = new List<string>();
        var capabilityGaps = new List<string>();
        var setupLines = new List<string>();
        var world = new WorldState();
        var registry = document.ToRegistry();
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>();

        if (!registry.EntityTemplates.ContainsKey(scenario.ScenarioRootEntityTemplateId))
        {
            validationDiagnostics.Add($"missing scenario root template {scenario.ScenarioRootEntityTemplateId}.");
        }

        if (!registry.EntityTemplates.ContainsKey(scenario.PlayerEntityTemplateId))
        {
            validationDiagnostics.Add($"missing player template {scenario.PlayerEntityTemplateId}.");
        }

        if (validationDiagnostics.Count > 0)
        {
            return CreateResult(scenarioPlaneId: null, playerLocation: null);
        }

        try
        {
            AddRectangularPlane(world, new Plane(DefaultScenarioHostPlaneId, "Scenario Host", 1, 1));
            var rootSpawn = registry.SpawnEntity(
                world,
                scenario.ScenarioRootEntityTemplateId,
                new EntitySpawnOptions(
                    DefaultScenarioRootEntityId,
                    new PlaneCoord(DefaultScenarioHostPlaneId, new GridCoord(0, 0)),
                    InventoryPlaneId: DefaultScenarioPlaneId,
                    InventoryPlaneName: "Scenario Space"));
            AddActionPlans(actionPlans, rootSpawn.ActionPlans);
        }
        catch (Exception ex)
        {
            validationDiagnostics.Add($"scenario root {scenario.ScenarioRootEntityTemplateId} could not be spawned: {ex.Message}");
            return CreateResult(DefaultScenarioPlaneId, playerLocation: null);
        }

        if (world.GetInventoryPlaneId(DefaultScenarioRootEntityId) is not { } activeScenarioPlaneId)
        {
            validationDiagnostics.Add($"scenario root {scenario.ScenarioRootEntityTemplateId} has no usable inventory space.");
            return CreateResult(DefaultScenarioPlaneId, playerLocation: null);
        }

        var requestedPlayerLocation = new PlaneCoord(activeScenarioPlaneId, scenario.PlayerStart);
        if (!world.Planes.TryGetValue(activeScenarioPlaneId, out var activePlane) || !activePlane.Contains(scenario.PlayerStart) || !world.TryGetNodeId(requestedPlayerLocation, out _))
        {
            validationDiagnostics.Add($"player start {requestedPlayerLocation} is outside scenario plane {activeScenarioPlaneId}.");
            return CreateResult(activeScenarioPlaneId, playerLocation: null);
        }

        if (world.Entities.ContainsKey(scenario.PlayerEntityId))
        {
            validationDiagnostics.Add($"player entity ID {scenario.PlayerEntityId} is already present in the materialized scenario.");
            return CreateResult(activeScenarioPlaneId, playerLocation: null);
        }

        if (world.GetOccupant(requestedPlayerLocation) is { } occupant)
        {
            validationDiagnostics.Add($"player start {requestedPlayerLocation} is occupied by {occupant}.");
            return CreateResult(activeScenarioPlaneId, playerLocation: null);
        }

        try
        {
            var playerSpawn = registry.SpawnEntity(
                world,
                scenario.PlayerEntityTemplateId,
                new EntitySpawnOptions(scenario.PlayerEntityId, requestedPlayerLocation));
            AddActionPlans(actionPlans, playerSpawn.ActionPlans);
        }
        catch (Exception ex)
        {
            validationDiagnostics.Add($"player {scenario.PlayerEntityId} could not be inserted: {ex.Message}");
            return CreateResult(activeScenarioPlaneId, playerLocation: null);
        }

        setupLines.Add($"Scenario: {scenario.ScenarioId} ({scenario.Name})");
        setupLines.Add($"Scenario root: {scenario.ScenarioRootEntityTemplateId} {DefaultScenarioRootEntityId} using plane {activeScenarioPlaneId}");
        setupLines.Add($"Player: {world.Entities[scenario.PlayerEntityId].Name} {scenario.PlayerEntityId} at {requestedPlayerLocation}, {FormatActionState(world, scenario.PlayerEntityId)}");

        return CreateResult(activeScenarioPlaneId, requestedPlayerLocation);

        ScenarioMaterializationResult CreateResult(PlaneId? scenarioPlaneId, PlaneCoord? playerLocation) =>
            new(
                scenario.ScenarioId,
                scenario.Name,
                scenario.ScenarioRootEntityTemplateId,
                DefaultScenarioRootEntityId,
                scenario.PlayerEntityTemplateId,
                scenario.PlayerEntityId,
                scenarioPlaneId,
                playerLocation,
                world,
                actionPlans,
                registry,
                setupLines,
                validationDiagnostics,
                runtimeFailures,
                capabilityGaps);
    }

    private static void AddActionPlans(Dictionary<EntityId, IEntityActionPlan> actionPlans, IReadOnlyDictionary<EntityId, IEntityActionPlan> additions)
    {
        foreach (var (entityId, actionPlan) in additions)
        {
            actionPlans[entityId] = actionPlan;
        }
    }

    private static string FormatActionState(WorldState world, EntityId entityId)
    {
        var facing = world.GetActionFacing(entityId)?.ToString() ?? "none";
        var target = world.GetActionTarget(entityId)?.ToString() ?? "none";
        return $"facing {facing}, target {target}";
    }

    private static void AddRectangularPlane(WorldState world, Plane plane)
    {
        world.Planes.Add(plane.Id, plane);

        for (var y = 0; y < plane.Height; y++)
        {
            for (var x = 0; x < plane.Width; x++)
            {
                world.AddNode(plane.Id, new GridCoord(x, y));
            }
        }
    }
}
