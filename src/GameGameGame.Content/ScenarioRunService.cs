using GameGameGame.Core;

namespace GameGameGame.Content;

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

internal sealed record ScenarioRunHistoryResult(
    ScenarioRunReport Report,
    ScenarioMaterializationResult Materialization,
    SimulationHistorySession? History);

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

        return RunPersistedWithHistory(document, request).Report;
    }

    internal static ScenarioRunHistoryResult RunPersistedWithHistory(EditableContentDocument document, PersistedScenarioRunRequest request)
    {
        if (request.TurnCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Scenario turn count must be non-negative.");
        }

        var materialization = ScenarioMaterializer.Materialize(document, request.ScenarioId);

        return RunMaterializedWithHistory(
            new ScenarioRunRequest(materialization.ScenarioRootEntityTemplateId, request.TurnCount),
            materialization,
            request.TurnCount,
            "Persisted scenario simulation");
    }

    private static ScenarioRunReport RunMaterialized(
        ScenarioRunRequest reportRequest,
        ScenarioMaterializationResult materialization,
        int turnCount,
        string runMode) =>
        RunMaterializedWithHistory(reportRequest, materialization, turnCount, runMode).Report;

    private static ScenarioRunHistoryResult RunMaterializedWithHistory(
        ScenarioRunRequest reportRequest,
        ScenarioMaterializationResult materialization,
        int turnCount,
        string runMode)
    {
        var validationDiagnostics = materialization.ValidationDiagnostics.ToList();
        var world = materialization.World;

        if (!materialization.CanPlay && materialization.ScenarioPlaneId is null)
        {
            var report = CreateReport(
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
            return new ScenarioRunHistoryResult(report, materialization, History: null);
        }

        var scenarioPlaneId = materialization.ScenarioPlaneId ?? ScenarioPlaneId;
        var actorOrder = GetScenarioActorsInInitiativeOrder(world, materialization.ActionPlans, scenarioPlaneId);
        var setupLines = CreateSetupLines(materialization, scenarioPlaneId, actorOrder, runMode);
        if (validationDiagnostics.Count > 0 || materialization.RuntimeFailures.Count > 0)
        {
            var report = CreateReport(
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
            return new ScenarioRunHistoryResult(report, materialization, History: null);
        }

        var runtimeObservations = new List<string>();
        var runtimeFailures = new List<string>();
        var movement = new MovementService();
        var history = SimulationHistorySession.Start(world, ScenarioRootEntityId, scenarioPlaneId, ScenarioRootEntityId);

        for (var turn = 1; turn <= turnCount; turn++)
        {
            var actorLogs = new List<SimulationHistoryActorLog>();
            for (var initiative = 0; initiative < actorOrder.Count; initiative++)
            {
                var actor = actorOrder[initiative];
                if (!world.Entities.TryGetValue(actor.EntityId, out var entity) || !materialization.ActionPlans.TryGetValue(actor.EntityId, out var actionPlan))
                {
                    continue;
                }

                TargetingService.RefreshTargets(world, materialization.Registry, actor.EntityId);
                var resolution = ActorTurnResolver.ResolvePlan(world, actor.EntityId, actionPlan.PlanTurn(world, actor.EntityId, movement), movement);
                PostActionStateUpdater.ApplyFacingFromMovement(world, actor.EntityId, resolution.ActorMovementDirection);
                world.RecordTrace(resolution.Trace);

                if (resolution.ConsumesTurn)
                {
                    world.AdvanceTurn();
                }

                actorLogs.Add(new SimulationHistoryActorLog(
                    initiative,
                    actor.EntityId,
                    entity.Name,
                    resolution.Succeeded,
                    resolution.ConsumesTurn,
                    resolution.ContinuePlan,
                    TurnActionSummaryFormatter.FormatTrace(resolution.Trace, resolution.Succeeded),
                    resolution.Trace));

                if (!resolution.Succeeded)
                {
                    runtimeObservations.Add($"Turn {turn}, initiative {initiative + 1}: {entity.Name} could not act ({FindFailureDetail(resolution.Trace)}).");
                }
            }

            if (actorLogs.Count > 0)
            {
                history.RecordActorInterval(actorLogs, scenarioPlaneId, ScenarioRootEntityId);
            }
        }

        var completedReport = CreateReport(
            reportRequest,
            scenarioPlaneId,
            world,
            actorOrder,
            CreateTurnReports(history),
            setupLines,
            validationDiagnostics,
            runtimeObservations,
            runtimeFailures,
            materialization.CapabilityGaps);
        return new ScenarioRunHistoryResult(completedReport, materialization, history);
    }

    private static IReadOnlyList<ScenarioTurnReport> CreateTurnReports(SimulationHistorySession history) =>
        history.Intervals
            .SelectMany(interval => interval.ActorLogs.Select(log => new ScenarioTurnReport(
                interval.ToFrameIndex,
                log.Order + 1,
                log.ActorId,
                log.ActorName,
                FormatScenarioTrace(log))))
            .ToList();

    private static IReadOnlyList<string> FormatScenarioTrace(SimulationHistoryActorLog log)
    {
        var planTrace = log.Trace.Children.Count == 1
            && log.Trace.Children[0].Label.StartsWith("Plan ", StringComparison.Ordinal)
                ? log.Trace.Children[0]
                : log.Trace;

        return BehaviorChainTraceFormatter.Format(new PlanExecutionResult(
            log.Succeeded,
            log.ConsumedTurn,
            log.ContinuePlan,
            planTrace));
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
        PlaneId scenarioPlaneId)
    {
        var containmentPaths = new EntityContainmentPathService();

        return actionPlans.Keys
            .Where(world.Entities.ContainsKey)
            .Where(entityId => entityId != ScenarioRootEntityId)
            .Select(entityId => (
                EntityId: entityId,
                Location: world.GetEntityLocation(entityId),
                Path: containmentPaths.GetPathFromRoot(world, ScenarioRootEntityId, entityId)))
            .Where(entry => IsScheduledScenarioActor(entry.Path, scenarioPlaneId))
            .OrderBy(entry => FormatScenarioInitiativePath(entry.Path), StringComparer.Ordinal)
            .ThenBy(entry => entry.EntityId.Value, StringComparer.Ordinal)
            .Select(entry => new ScenarioActorSummary(
                entry.EntityId,
                world.Entities[entry.EntityId].Name,
                entry.Location))
            .ToList();
    }

    private static bool IsScheduledScenarioActor(EntityContainmentPath path, PlaneId scenarioPlaneId) =>
        path.Status == EntityContainmentPathStatus.Complete
        && path.Segments.Count > 1
        && path.Segments[0].EntityId == ScenarioRootEntityId
        && path.Segments[1].ContainingPlaneId == scenarioPlaneId;

    private static string FormatScenarioInitiativePath(EntityContainmentPath path) =>
        string.Join(
            "/",
            path.Segments
                .Skip(1)
                .Select(segment =>
                {
                    var coord = segment.CoordinateInContainingPlane ?? new GridCoord(0, 0);
                    return $"{coord.Y:D6},{coord.X:D6},{segment.EntityId.Value}";
                }));

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
