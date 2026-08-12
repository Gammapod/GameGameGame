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
    IReadOnlyList<string> CapabilityGaps,
    IReadOnlyDictionary<string, IReadOnlyList<EntityId>>? PlayerControls = null,
    bool AllowPlayerlessPlay = false)
{
    public IReadOnlyDictionary<string, IReadOnlyList<EntityId>> PlayerControls { get; } = PlayerControls ?? new Dictionary<string, IReadOnlyList<EntityId>>();

    public bool CanPlay => ValidationDiagnostics.Count == 0
        && RuntimeFailures.Count == 0
        && ScenarioPlaneId is not null
        && (PlayerLocation is not null || PlayerControls.Count > 0 || AllowPlayerlessPlay);
}

public static class ScenarioMaterializer
{
    public static readonly EntityId DefaultScenarioRootEntityId = new("scenarioRoot");
    public static readonly PlaneId DefaultScenarioHostPlaneId = new("scenarioHost");
    public static readonly PlaneId DefaultScenarioPlaneId = new("scenarioRoot");

    public static ScenarioMaterializationResult Materialize(EditableContentDocument document, string scenarioId) =>
        Materialize(document, document.GetScenario(scenarioId));

    public static ScenarioMaterializationResult Materialize(ContentWorkspace workspace, string scenarioId)
    {
        var compile = ContentCompiler.Compile(workspace);
        var scenarioSymbols = compile.Symbols
            .Where(symbol => symbol.Kind == ContentSymbolKind.Scenario && symbol.Id == scenarioId)
            .ToList();
        var fallbackScenario = new ScenarioDefinition(
            scenarioId,
            scenarioId,
            new EntityTemplateId(string.Empty),
            PlayerEntityTemplateId: null,
            PlayerEntityId: null,
            PlayerStart: null);

        if (scenarioSymbols.Count != 1)
        {
            var diagnostics = compile.Validation.Errors.ToList();
            diagnostics.Add(scenarioSymbols.Count == 0
                ? $"workspace scenario {scenarioId} was not found."
                : $"workspace scenario {scenarioId} is ambiguous across workspace documents.");

            return CreateInvalidWorkspaceResult(scenarioId, scenarioId, fallbackScenario, compile.Registry, diagnostics);
        }

        var composedDocument = ContentCompiler.ComposeDocument(workspace);
        if (!composedDocument.Scenarios.TryGetValue(scenarioId, out var scenarioDto))
        {
            var diagnostics = compile.Validation.Errors.ToList();
            diagnostics.Add($"workspace scenario {scenarioId} was not found in the composed document.");
            return CreateInvalidWorkspaceResult(scenarioId, scenarioId, fallbackScenario, compile.Registry, diagnostics);
        }

        var scenario = scenarioDto.ToDefinition(scenarioId);
        if (compile.Registry is null || !compile.Validation.IsValid)
        {
            return CreateInvalidWorkspaceResult(
                scenarioId,
                scenario.Name,
                scenario,
                compile.Registry,
                FormatWorkspaceDiagnostics(compile.Diagnostics));
        }

        return Materialize(composedDocument, scenario);
    }

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
            scenario.PlayerStart,
            scenario.PlayerControls,
            allowPlayerlessPlay: true);

    private static ScenarioMaterializationResult CreateInvalidWorkspaceResult(
        string scenarioId,
        string name,
        ScenarioDefinition scenario,
        PrototypeContentRegistry? registry,
        IReadOnlyList<string> validationDiagnostics) =>
        new(
            scenarioId,
            name,
            scenario.ScenarioRootEntityTemplateId,
            DefaultScenarioRootEntityId,
            scenario.PlayerEntityTemplateId,
            scenario.PlayerEntityId,
            ScenarioPlaneId: null,
            PlayerLocation: null,
            new WorldState(),
            new Dictionary<EntityId, IEntityActionPlan>(),
            registry ?? new PrototypeContentRegistry(
                new Dictionary<EntityTemplateId, EntityTemplate>(),
                new Dictionary<ActionPlanTemplateId, ActionPlanDescriptor>(),
                new Dictionary<EntityTemplateId, EntityPresentation>()),
            SetupLines: [],
            validationDiagnostics,
            RuntimeFailures: [],
            CapabilityGaps: [],
            PlayerControls: null,
            AllowPlayerlessPlay: true);

    private static IReadOnlyList<string> FormatWorkspaceDiagnostics(IReadOnlyList<ContentDiagnostic> diagnostics) =>
        diagnostics
            .Where(diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error)
            .Select(diagnostic =>
            {
                var source = diagnostic.DocumentId is null && diagnostic.SourcePath is null
                    ? null
                    : $" [{diagnostic.DocumentId ?? "unknown-document"} {diagnostic.SourcePath ?? "unknown-source"}]";
                return $"{diagnostic.Message}{source}";
            })
            .ToList();

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
            playerStart: null,
            playerControls: null,
            allowPlayerlessPlay: false);

    private static ScenarioMaterializationResult Materialize(
        EditableContentDocument document,
        string scenarioId,
        string name,
        EntityTemplateId scenarioRootEntityTemplateId,
        EntityId scenarioRootEntityId,
        PlaneId scenarioPlaneId,
        EntityTemplateId? playerEntityTemplateId,
        EntityId? playerEntityId,
        GridCoord? playerStart,
        IReadOnlyDictionary<string, IReadOnlyList<EntityId>>? playerControls,
        bool allowPlayerlessPlay)
    {
        var validationDiagnostics = document.ValidateCanonicalAuthoring().Errors.ToList();
        var runtimeFailures = new List<string>();
        var capabilityGaps = new List<string>();
        var setupLines = new List<string>();
        var resolvedPlayerControls = new Dictionary<string, IReadOnlyList<EntityId>>(StringComparer.Ordinal);
        var world = new WorldState();
        var registry = new PrototypeContentRegistry(
            new Dictionary<EntityTemplateId, EntityTemplate>(),
            new Dictionary<ActionPlanTemplateId, ActionPlanDescriptor>(),
            new Dictionary<EntityTemplateId, EntityPresentation>());
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>();

        var compile = ContentCompiler.Compile(document);
        validationDiagnostics.AddRange(compile.Validation.Errors);
        validationDiagnostics = validationDiagnostics.Distinct().ToList();
        if (compile.Registry is null)
        {
            return CreateResult(resultScenarioPlaneId: null, playerLocation: null);
        }

        registry = compile.Registry;
        PopulateRuntimeTemplates(world, registry);

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
            PopulateMergedInventoryLayers(world, registry);
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
        var authoredPlayerControllerEntityIds = CollectAuthoredPlayerControllerEntityIds(registry, scenarioRootEntityTemplateId).ToList();
        if (authoredPlayerControllerEntityIds.Count == 0
            && playerEntityTemplateId is { } concretePlayerTemplateId
            && playerEntityId is { } concretePlayerEntityId
            && playerStart is { } concretePlayerStart)
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

        var controlBindings = authoredPlayerControllerEntityIds.Count > 0
            ? new Dictionary<string, IReadOnlyList<EntityId>>(StringComparer.Ordinal)
            {
                ["player-1"] = authoredPlayerControllerEntityIds
            }
            : ResolvePlayerControls(playerControls, insertedPlayerLocation is null ? null : playerEntityId);

        var alreadyAssigned = new Dictionary<EntityId, string>();
        foreach (var (playerId, controlledEntityIds) in controlBindings)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                validationDiagnostics.Add("player control binding declares an empty player ID.");
                continue;
            }

            if (controlledEntityIds.Count == 0)
            {
                validationDiagnostics.Add($"player control {playerId} has no controlled entities.");
                continue;
            }

            var seenForPlayer = new HashSet<EntityId>();
            var resolvedEntityIds = new List<EntityId>();
            foreach (var controlledEntityId in controlledEntityIds)
            {
                if (!seenForPlayer.Add(controlledEntityId))
                {
                    validationDiagnostics.Add($"player control {playerId} lists entity {controlledEntityId} more than once.");
                    continue;
                }

                if (!world.Entities.ContainsKey(controlledEntityId))
                {
                    validationDiagnostics.Add($"player control {playerId} references missing materialized entity {controlledEntityId}.");
                    continue;
                }

                if (alreadyAssigned.TryGetValue(controlledEntityId, out var previousPlayerId) && previousPlayerId != playerId)
                {
                    validationDiagnostics.Add($"controlled entity {controlledEntityId} is assigned to both {previousPlayerId} and {playerId}.");
                    continue;
                }

                alreadyAssigned[controlledEntityId] = playerId;
                resolvedEntityIds.Add(controlledEntityId);
                world.SetActionControlSource(controlledEntityId, EntityControlSource.PlayerChoice);
            }

            if (resolvedEntityIds.Count == 0)
            {
                continue;
            }

            resolvedPlayerControls[playerId] = resolvedEntityIds;
            setupLines.Add($"Control: {playerId} -> {string.Join(", ", resolvedEntityIds)}");
        }

        if (validationDiagnostics.Count > 0)
        {
            return CreateResult(activeScenarioPlaneId, insertedPlayerLocation);
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
                capabilityGaps,
                resolvedPlayerControls,
                allowPlayerlessPlay);
    }

    private static IEnumerable<EntityId> CollectAuthoredPlayerControllerEntityIds(
        PrototypeContentRegistry registry,
        EntityTemplateId scenarioRootEntityTemplateId)
    {
        var visitedTemplateIds = new HashSet<EntityTemplateId>();
        return Collect(scenarioRootEntityTemplateId, visitedTemplateIds);

        IEnumerable<EntityId> Collect(EntityTemplateId templateId, HashSet<EntityTemplateId> ancestry)
        {
            if (!registry.EntityTemplates.TryGetValue(templateId, out var template)
                || template.CarriedEntities is null
                || template.CarriedEntities.Count == 0
                || !ancestry.Add(templateId))
            {
                yield break;
            }

            foreach (var carried in template.CarriedEntities)
            {
                if (carried.Controller == EntityController.Player)
                {
                    yield return carried.EntityId;
                }

                if (carried.TemplateId is { } carriedTemplateId)
                {
                    foreach (var nested in Collect(carriedTemplateId, ancestry))
                    {
                        yield return nested;
                    }
                }
            }

            ancestry.Remove(templateId);
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<EntityId>> ResolvePlayerControls(
        IReadOnlyDictionary<string, IReadOnlyList<EntityId>>? authoredPlayerControls,
        EntityId? legacyPlayerEntityId)
    {
        if (authoredPlayerControls is { Count: > 0 })
        {
            return authoredPlayerControls;
        }

        return legacyPlayerEntityId is { } playerEntityId
            ? new Dictionary<string, IReadOnlyList<EntityId>>(StringComparer.Ordinal)
            {
                ["player-1"] = [playerEntityId]
            }
            : new Dictionary<string, IReadOnlyList<EntityId>>(StringComparer.Ordinal);
    }

    private static void AddActionPlans(Dictionary<EntityId, IEntityActionPlan> actionPlans, IReadOnlyDictionary<EntityId, IEntityActionPlan> additions)
    {
        foreach (var (entityId, actionPlan) in additions)
        {
            actionPlans[entityId] = actionPlan;
        }
    }

    private static void PopulateRuntimeTemplates(WorldState world, PrototypeContentRegistry registry)
    {
        foreach (var (templateId, template) in registry.EntityTemplates)
        {
            world.RuntimeEntityTemplates[templateId.Value] = new RuntimeEntityTemplate(
                templateId.Value,
                template.Name,
                template.InventoryWidth,
                template.InventoryHeight,
                template.Bulk,
                template.Aperture,
                template.DefaultActionPlanId is { } planId ? new ActionPlanId(planId.Value) : null,
                template.ActionStateDefaults?.Facing,
                template.EnterPolicy,
                template.ExitPolicy,
                template.TopologyPolicy);
        }
    }

    private static void PopulateMergedInventoryLayers(WorldState world, PrototypeContentRegistry registry)
    {
        foreach (var layer in registry.MergedInventoryLayers.Values)
        {
            world.MergedInventoryLayers.Add(new MergedInventoryLayer(layer.Id, layer.Spaces));

            var templatesByOwnerId = CollectAuthoredEntityTemplates(registry);
            foreach (var link in MergedInventoryAlignedJoinResolver.Resolve(layer, templatesByOwnerId))
            {
                if (world.GetRegisteredInventoryPlaneId(link.First.OwnerId) is not { } firstPlaneId ||
                    world.GetRegisteredInventoryPlaneId(link.Second.OwnerId) is not { } secondPlaneId)
                {
                    continue;
                }

                world.SourceCellLinks.Add(new SourceCellLink(
                    new PlaneCoord(firstPlaneId, link.First.SourceCoord),
                    link.First.Direction,
                    new PlaneCoord(secondPlaneId, link.Second.SourceCoord),
                    link.Second.Direction));
            }
        }
    }

    private static Dictionary<EntityId, EntityTemplate> CollectAuthoredEntityTemplates(PrototypeContentRegistry registry)
    {
        var result = new Dictionary<EntityId, EntityTemplate>();
        var visited = new HashSet<EntityTemplateId>();
        foreach (var templateId in registry.EntityTemplates.Keys)
        {
            Collect(templateId, visited);
        }

        return result;

        void Collect(EntityTemplateId templateId, HashSet<EntityTemplateId> ancestry)
        {
            if (!registry.EntityTemplates.TryGetValue(templateId, out var template) ||
                template.CarriedEntities is null ||
                !ancestry.Add(templateId))
            {
                return;
            }

            foreach (var carried in template.CarriedEntities)
            {
                if (carried.TemplateId is { } carriedTemplateId && registry.EntityTemplates.TryGetValue(carriedTemplateId, out var carriedTemplate))
                {
                    result[carried.EntityId] = carriedTemplate;
                    Collect(carriedTemplateId, ancestry);
                }
            }

            ancestry.Remove(templateId);
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
