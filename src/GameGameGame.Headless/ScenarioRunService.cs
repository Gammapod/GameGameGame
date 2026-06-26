using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Headless;

public sealed record ScenarioRunRequest(
    EntityTemplateId ScenarioRootEntityTemplateId,
    int TurnCount);

public sealed record PersistedScenarioRunRequest(
    string ScenarioId,
    int TurnCount);

public sealed record ScenarioActorSummary(
    EntityId EntityId,
    string Name,
    PlaneCoord Location);

public sealed record ScenarioTurnReport(
    int TurnNumber,
    int InitiativeIndex,
    EntityId ActorId,
    string ActorName,
    IReadOnlyList<string> TraceLines);

public sealed record ScenarioRunReport(
    EntityTemplateId ScenarioRootEntityTemplateId,
    EntityId ScenarioRootEntityId,
    PlaneId ScenarioPlaneId,
    IReadOnlyList<ScenarioActorSummary> ActorOrder,
    IReadOnlyList<ScenarioTurnReport> Turns,
    IReadOnlyList<string> SetupLines,
    IReadOnlyList<string> FinalStateLines,
    IReadOnlyList<string> InventorySummaryLines,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeObservations,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps);

public static class ScenarioRunService
{
    private static readonly EntityId ScenarioRootEntityId = ScenarioMaterializer.DefaultScenarioRootEntityId;
    private static readonly PlaneId ScenarioPlaneId = ScenarioMaterializer.DefaultScenarioPlaneId;

    public static ScenarioRunReport Run(EditableContentDocument document, ScenarioRunRequest request)
    {
        if (request.TurnCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Scenario turn count must be non-negative.");
        }

        var materialization = ScenarioMaterializer.MaterializeRootOnly(
            document,
            "legacy-run",
            "Legacy RunScenario",
            request.ScenarioRootEntityTemplateId,
            ScenarioRootEntityId,
            ScenarioPlaneId);

        return RunMaterialized(
            request,
            materialization,
            request.TurnCount,
            "Root-only compatibility simulation");
    }

    public static ScenarioRunReport Run(EditableContentDocument document, PersistedScenarioRunRequest request)
    {
        if (request.TurnCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Scenario turn count must be non-negative.");
        }

        var materialization = ScenarioMaterializer.Materialize(document, request.ScenarioId);

        return RunMaterialized(
            new ScenarioRunRequest(materialization.ScenarioRootEntityTemplateId, request.TurnCount),
            materialization,
            request.TurnCount,
            "Persisted scenario simulation");
    }

    private static ScenarioRunReport RunMaterialized(
        ScenarioRunRequest reportRequest,
        ScenarioMaterializationResult materialization,
        int turnCount,
        string runMode)
    {
        var validationDiagnostics = materialization.ValidationDiagnostics.ToList();
        var world = materialization.World;

        if (!materialization.CanPlay && materialization.ScenarioPlaneId is null)
        {
            return CreateReport(
                reportRequest,
                scenarioPlaneId: ScenarioPlaneId,
                world,
                [],
                [],
                CreateSetupLines(materialization, ScenarioPlaneId, [], runMode),
                validationDiagnostics,
                [],
                [],
                materialization.CapabilityGaps);
        }

        var scenarioPlaneId = materialization.ScenarioPlaneId ?? ScenarioPlaneId;
        var actorOrder = GetScenarioActorsInInitiativeOrder(world, materialization.ActionPlans, scenarioPlaneId);
        var setupLines = CreateSetupLines(materialization, scenarioPlaneId, actorOrder, runMode);
        if (validationDiagnostics.Count > 0 || materialization.RuntimeFailures.Count > 0)
        {
            return CreateReport(
                reportRequest,
                scenarioPlaneId,
                world,
                actorOrder,
                [],
                setupLines,
                validationDiagnostics,
                [],
                materialization.RuntimeFailures,
                materialization.CapabilityGaps);
        }

        var turns = new List<ScenarioTurnReport>();
        var runtimeObservations = new List<string>();
        var runtimeFailures = new List<string>();
        var movement = new MovementService();

        for (var turn = 1; turn <= turnCount; turn++)
        {
            for (var initiative = 0; initiative < actorOrder.Count; initiative++)
            {
                var actor = actorOrder[initiative];
                if (!world.Entities.TryGetValue(actor.EntityId, out var entity) || !materialization.ActionPlans.TryGetValue(actor.EntityId, out var actionPlan))
                {
                    continue;
                }

                var resolution = ResolvePlan(world, actor.EntityId, actionPlan.PlanTurn(world, actor.EntityId, movement), movement);
                world.RecordTrace(resolution.Trace);

                if (resolution.ConsumesTurn)
                {
                    world.AdvanceTurn();
                }

                var formattedTrace = FormatScenarioTrace(resolution);
                turns.Add(new ScenarioTurnReport(
                    turn,
                    initiative + 1,
                    actor.EntityId,
                    entity.Name,
                    formattedTrace));

                if (!resolution.Succeeded)
                {
                    runtimeObservations.Add($"Turn {turn}, initiative {initiative + 1}: {entity.Name} could not act ({FindFailureDetail(resolution.Trace)}).");
                }
            }
        }

        return CreateReport(
            reportRequest,
            scenarioPlaneId,
            world,
            actorOrder,
            turns,
            setupLines,
            validationDiagnostics,
            runtimeObservations,
            runtimeFailures,
            materialization.CapabilityGaps);
    }

    private static IReadOnlyList<string> FormatScenarioTrace(ActionResolution resolution)
    {
        var planTrace = resolution.Trace.Children.Count == 1
            && resolution.Trace.Children[0].Label.StartsWith("Plan ", StringComparison.Ordinal)
                ? resolution.Trace.Children[0]
                : resolution.Trace;

        return BehaviorChainTraceFormatter.Format(new PlanExecutionResult(
            resolution.Succeeded,
            resolution.ConsumesTurn,
            resolution.ContinuePlan,
            planTrace));
    }

    private static ActionResolution ResolvePlan(WorldState world, EntityId actorId, PlannedActionPlan plan, MovementService movement)
    {
        var actorName = world.Entities.TryGetValue(actorId, out var actor) ? actor.Name : actorId.ToString();
        var root = new TraceNode($"Resolve plan for {actorName}", TraceStatus.Info);

        foreach (var option in plan.Options)
        {
            var resolution = option.Resolve(world, actorId, movement);
            root.Add(resolution.Trace);

            if (resolution.ConsumesTurn)
            {
                root.Status = resolution.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
                root.Detail = $"resolved {option.GetType().Name}";
                return new ActionResolution(resolution.Succeeded, resolution.ConsumesTurn, resolution.ContinuePlan, root);
            }

            if (!resolution.ContinuePlan)
            {
                root.Status = resolution.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
                root.Detail = $"stopped at {option.GetType().Name}";
                return new ActionResolution(resolution.Succeeded, resolution.ConsumesTurn, resolution.ContinuePlan, root);
            }
        }

        root.Status = TraceStatus.Failure;
        root.Detail = "no planned action could execute";
        return new ActionResolution(false, ConsumesTurn: false, ContinuePlan: false, root);
    }

    private static ScenarioRunReport CreateReport(
        ScenarioRunRequest request,
        PlaneId scenarioPlaneId,
        WorldState world,
        IReadOnlyList<ScenarioActorSummary> actorOrder,
        IReadOnlyList<ScenarioTurnReport> turns,
        IReadOnlyList<string> setupLines,
        IReadOnlyList<string> validationDiagnostics,
        IReadOnlyList<string> runtimeObservations,
        IReadOnlyList<string> runtimeFailures,
        IReadOnlyList<string> capabilityGaps) =>
        new(
            request.ScenarioRootEntityTemplateId,
            ScenarioRootEntityId,
            scenarioPlaneId,
            actorOrder,
            turns,
            setupLines,
            SummarizePlaneEntities(world, scenarioPlaneId),
            ScenarioInventorySummaryFormatter.SummarizeScenarioInventories(world, ScenarioRootEntityId),
            validationDiagnostics,
            runtimeObservations,
            runtimeFailures,
            capabilityGaps);

    private static IReadOnlyList<ScenarioActorSummary> GetScenarioActorsInInitiativeOrder(
        WorldState world,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        PlaneId scenarioPlaneId) =>
        actionPlans.Keys
            .Where(world.Entities.ContainsKey)
            .Select(entityId => (EntityId: entityId, Location: world.GetEntityLocation(entityId)))
            .Where(entry => entry.Location.PlaneId == scenarioPlaneId)
            .OrderBy(entry => entry.Location.Coord.Y)
            .ThenBy(entry => entry.Location.Coord.X)
            .ThenBy(entry => entry.EntityId.Value, StringComparer.Ordinal)
            .Select(entry => new ScenarioActorSummary(
                entry.EntityId,
                world.Entities[entry.EntityId].Name,
                entry.Location))
            .ToList();

    private static IReadOnlyList<string> CreateSetupLines(
        ScenarioMaterializationResult materialization,
        PlaneId scenarioPlaneId,
        IReadOnlyList<ScenarioActorSummary> actorOrder,
        string runMode)
    {
        var lines = new List<string>
        {
            $"Run mode: {runMode}"
        };
        lines.AddRange(materialization.SetupLines);
        lines.AddRange([
            $"Scenario plane: {scenarioPlaneId}",
            "Actor initiative order:"
        ]);
        lines.AddRange(actorOrder.Select((actor, index) => $"  - {index + 1}. {actor.Name}: {actor.Location}, {FormatActionState(materialization.World, actor.EntityId)}"));
        return lines;
    }

    private static IReadOnlyList<string> SummarizePlaneEntities(WorldState world, PlaneId planeId) =>
        world.Occupancy
            .Where(entry => world.Nodes.TryGetValue(entry.Key, out var node) && node.PlaneId == planeId)
            .Select(entry => (EntityId: entry.Value, Node: world.Nodes[entry.Key]))
            .OrderBy(entry => entry.Node.Coord.Y)
            .ThenBy(entry => entry.Node.Coord.X)
            .ThenBy(entry => entry.EntityId.Value, StringComparer.Ordinal)
            .Select(entry => $"{world.Entities[entry.EntityId].Name}: {world.GetEntityLocation(entry.EntityId)}, {FormatActionState(world, entry.EntityId)}")
            .ToList();

    private static string FormatActionState(WorldState world, EntityId entityId)
    {
        var facing = world.GetActionFacing(entityId)?.ToString() ?? "none";
        var target = world.GetActionTarget(entityId)?.ToString() ?? "none";
        return $"facing {facing}, target {target}";
    }

    private static string FindFailureDetail(TraceNode trace) =>
        DescendantsAndSelf(trace)
            .Where(node => node.Status == TraceStatus.Failure && !string.IsNullOrWhiteSpace(node.Detail))
            .Select(node => node.Detail!)
            .LastOrDefault()
        ?? trace.Detail
        ?? "no detail";

    private static IEnumerable<TraceNode> DescendantsAndSelf(TraceNode node)
    {
        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var descendant in DescendantsAndSelf(child))
            {
                yield return descendant;
            }
        }
    }
}

public static class ScenarioInventorySummaryFormatter
{
    public static IReadOnlyList<string> SummarizeScenarioInventories(WorldState world, EntityId scenarioRootEntityId)
    {
        var containedByNonRootInventory = world.InventoryPlanes
            .Where(entry => entry.Key != scenarioRootEntityId)
            .SelectMany(entry => GetContainedEntities(world, entry.Value).Select(contained => contained.EntityId))
            .ToHashSet();

        var topLevelInventoryOwners = world.InventoryPlanes
            .Where(entry => entry.Key != scenarioRootEntityId)
            .Where(entry => world.Entities.ContainsKey(entry.Key))
            .Where(entry => !containedByNonRootInventory.Contains(entry.Key))
            .Where(entry => GetContainedEntities(world, entry.Value).Count > 0)
            .Select(entry => entry.Key)
            .OrderBy(entityId => world.GetEntityLocation(entityId).PlaneId.Value, StringComparer.Ordinal)
            .ThenBy(entityId => world.GetEntityLocation(entityId).Coord.Y)
            .ThenBy(entityId => world.GetEntityLocation(entityId).Coord.X)
            .ThenBy(entityId => entityId.Value, StringComparer.Ordinal)
            .ToList();

        var lines = new List<string>();
        foreach (var ownerId in topLevelInventoryOwners)
        {
            AddInventorySummary(lines, world, ownerId, indent: string.Empty, visited: [ownerId]);
        }

        return lines;
    }

    public static IReadOnlyList<string> SummarizeEntityInventory(WorldState world, EntityId ownerId)
    {
        var lines = new List<string>();
        AddInventorySummary(lines, world, ownerId, indent: string.Empty, visited: [ownerId]);
        return lines;
    }

    private static void AddInventorySummary(
        List<string> lines,
        WorldState world,
        EntityId ownerId,
        string indent,
        HashSet<EntityId> visited)
    {
        if (!world.Entities.TryGetValue(ownerId, out var owner) || world.GetInventoryPlaneId(ownerId) is not { } inventoryPlaneId)
        {
            return;
        }

        var containedEntities = GetContainedEntities(world, inventoryPlaneId);
        if (containedEntities.Count == 0)
        {
            return;
        }

        lines.Add($"{indent}{owner.Name} inventory:");
        foreach (var contained in containedEntities)
        {
            var containedName = world.Entities.TryGetValue(contained.EntityId, out var entity)
                ? entity.Name
                : contained.EntityId.ToString();
            lines.Add($"{indent}  - {containedName} {contained.EntityId} at {contained.Coord}");

            if (world.GetInventoryPlaneId(contained.EntityId) is not null)
            {
                if (!visited.Add(contained.EntityId))
                {
                    lines.Add($"{indent}    - cycle detected for {contained.EntityId}; nested contents omitted");
                    continue;
                }

                AddInventorySummary(lines, world, contained.EntityId, indent + "    ", visited);
                visited.Remove(contained.EntityId);
            }
        }
    }

    private static IReadOnlyList<(EntityId EntityId, GridCoord Coord)> GetContainedEntities(WorldState world, PlaneId inventoryPlaneId) =>
        world.Occupancy
            .Where(entry => world.Nodes.TryGetValue(entry.Key, out var node) && node.PlaneId == inventoryPlaneId)
            .Select(entry => (EntityId: entry.Value, Coord: world.Nodes[entry.Key].Coord))
            .OrderBy(entry => entry.Coord.Y)
            .ThenBy(entry => entry.Coord.X)
            .ThenBy(entry => entry.EntityId.Value, StringComparer.Ordinal)
            .ToList();
}
