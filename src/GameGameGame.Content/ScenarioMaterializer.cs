using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed record ScenarioMaterializationResult(
    string ScenarioId,
    string Name,
    EntityTemplateId ScenarioRootEntityTemplateId,
    EntityId ScenarioRootEntityId,
    EntityTemplateId? PlayerEntityTemplateId,
    EntityId? PlayerEntityId,
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
        => Materialize(
            document,
            scenario.ScenarioId,
            scenario.Name,
            scenario.ScenarioRootEntityTemplateId,
            DefaultScenarioRootEntityId,
            DefaultScenarioPlaneId,
            scenario.PlayerEntityTemplateId,
            scenario.PlayerEntityId,
            scenario.PlayerStart);

    public static ScenarioMaterializationResult MaterializeRootOnly(
        EditableContentDocument document,
        string scenarioId,
        string name,
        EntityTemplateId scenarioRootEntityTemplateId,
        EntityId scenarioRootEntityId,
        PlaneId scenarioPlaneId)
        => Materialize(
            document,
            scenarioId,
            name,
            scenarioRootEntityTemplateId,
            scenarioRootEntityId,
            scenarioPlaneId,
            playerEntityTemplateId: null,
            playerEntityId: null,
            playerStart: null);

    private static ScenarioMaterializationResult Materialize(
        EditableContentDocument document,
        string scenarioId,
        string name,
        EntityTemplateId scenarioRootEntityTemplateId,
        EntityId scenarioRootEntityId,
        PlaneId scenarioPlaneId,
        EntityTemplateId? playerEntityTemplateId,
        EntityId? playerEntityId,
        GridCoord? playerStart)
    {
        var validationDiagnostics = document.ValidateCanonicalAuthoring().Errors.ToList();
        var runtimeFailures = new List<string>();
        var capabilityGaps = new List<string>();
        var setupLines = new List<string>();
        var world = new WorldState();
        var registry = new PrototypeContentRegistry(
            new Dictionary<EntityTemplateId, EntityTemplate>(),
            new Dictionary<ActionPlanTemplateId, ActionPlanDescriptor>(),
            new Dictionary<EntityTemplateId, EntityPresentation>());
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>();

        try
        {
            registry = document.ToRegistry();
            validationDiagnostics.AddRange(registry.Validate().Errors);
            validationDiagnostics = validationDiagnostics.Distinct().ToList();
        }
        catch (Exception ex)
        {
            validationDiagnostics.Add($"content registry could not be materialized: {ex.Message}");
            return CreateResult(resultScenarioPlaneId: null, playerLocation: null);
        }

        if (!registry.EntityTemplates.ContainsKey(scenarioRootEntityTemplateId))
        {
            validationDiagnostics.Add($"missing scenario root template {scenarioRootEntityTemplateId}.");
        }

        if (playerEntityTemplateId is { } requiredPlayerTemplateId && !registry.EntityTemplates.ContainsKey(requiredPlayerTemplateId))
        {
            validationDiagnostics.Add($"missing player template {requiredPlayerTemplateId}.");
        }

        if (validationDiagnostics.Count > 0)
        {
            return CreateResult(resultScenarioPlaneId: null, playerLocation: null);
        }

        try
        {
            AddRectangularPlane(world, new Plane(DefaultScenarioHostPlaneId, "Scenario Host", 1, 1));
            var rootSpawn = registry.SpawnEntity(
                world,
                scenarioRootEntityTemplateId,
                new EntitySpawnOptions(
                    scenarioRootEntityId,
                    new PlaneCoord(DefaultScenarioHostPlaneId, new GridCoord(0, 0)),
                    InventoryPlaneId: scenarioPlaneId,
                    InventoryPlaneName: "Scenario Space"));
            AddActionPlans(actionPlans, rootSpawn.ActionPlans);
        }
        catch (Exception ex)
        {
            validationDiagnostics.Add($"scenario root {scenarioRootEntityTemplateId} could not be spawned: {ex.Message}");
            return CreateResult(scenarioPlaneId, playerLocation: null);
        }

        if (world.GetInventoryPlaneId(scenarioRootEntityId) is not { } activeScenarioPlaneId)
        {
            validationDiagnostics.Add($"scenario root {scenarioRootEntityTemplateId} has no usable inventory space.");
            return CreateResult(scenarioPlaneId, playerLocation: null);
        }

        PlaneCoord? insertedPlayerLocation = null;
        if (playerEntityTemplateId is { } concretePlayerTemplateId && playerEntityId is { } concretePlayerEntityId && playerStart is { } concretePlayerStart)
        {
            var requestedPlayerLocation = new PlaneCoord(activeScenarioPlaneId, concretePlayerStart);
            if (!world.Planes.TryGetValue(activeScenarioPlaneId, out var activePlane) || !activePlane.Contains(concretePlayerStart) || !world.TryGetNodeId(requestedPlayerLocation, out _))
            {
                validationDiagnostics.Add($"player start {requestedPlayerLocation} is outside scenario plane {activeScenarioPlaneId}.");
                return CreateResult(activeScenarioPlaneId, playerLocation: null);
            }

            if (world.Entities.ContainsKey(concretePlayerEntityId))
            {
                validationDiagnostics.Add($"player entity ID {concretePlayerEntityId} is already present in the materialized scenario.");
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
                    concretePlayerTemplateId,
                    new EntitySpawnOptions(concretePlayerEntityId, requestedPlayerLocation));
                AddActionPlans(actionPlans, playerSpawn.ActionPlans);
                insertedPlayerLocation = requestedPlayerLocation;
            }
            catch (Exception ex)
            {
                validationDiagnostics.Add($"player {concretePlayerEntityId} could not be inserted: {ex.Message}");
                return CreateResult(activeScenarioPlaneId, playerLocation: null);
            }
        }

        setupLines.Add($"Scenario: {scenarioId} ({name})");
        setupLines.Add($"Scenario root: {scenarioRootEntityTemplateId} {scenarioRootEntityId} using plane {activeScenarioPlaneId}");
        if (insertedPlayerLocation is { } playerLocation && playerEntityId is { } insertedPlayerEntityId)
        {
            var playerName = world.Entities.TryGetValue(insertedPlayerEntityId, out var player)
                ? player.Name
                : playerEntityTemplateId?.ToString() ?? insertedPlayerEntityId.ToString();
            setupLines.Add($"Player: {playerName} {insertedPlayerEntityId} at {playerLocation}, {FormatActionState(world, insertedPlayerEntityId)}");
        }

        return CreateResult(activeScenarioPlaneId, insertedPlayerLocation);

        ScenarioMaterializationResult CreateResult(PlaneId? resultScenarioPlaneId, PlaneCoord? playerLocation) =>
            new(
                scenarioId,
                name,
                scenarioRootEntityTemplateId,
                scenarioRootEntityId,
                playerEntityTemplateId,
                playerEntityId,
                resultScenarioPlaneId,
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
