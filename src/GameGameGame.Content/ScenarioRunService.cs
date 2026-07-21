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
        var actorOrder = ScenarioInitiativeOrderService.GetScenarioActorsInInitiativeOrder(world, materialization.ActionPlans, materialization.ScenarioRootEntityId, scenarioPlaneId);
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
        var stepper = new InitiativePlayerChoiceStepper(movement, new ActionChoiceService(movement));
        var actorOrderIds = actorOrder.Select(actor => actor.EntityId).ToList();

        for (var turn = 1; turn <= turnCount; turn++)
        {
            var step = stepper.AdvanceUntilPlayerChoice(
                world,
                actorOrderIds,
                materialization.ActionPlans,
                actorId => GetActionPlanDescriptor(materialization.Registry, actorId),
                startIndex: 0,
                (stepWorld, entityId) => TargetingService.RefreshTargets(stepWorld, materialization.Registry, entityId));

            if (step.ActorLogs.Count > 0)
            {
                history.RecordActorInterval(step.ActorLogs, scenarioPlaneId, ScenarioRootEntityId);

                foreach (var log in step.ActorLogs.Where(log => !log.Succeeded))
                {
                    runtimeObservations.Add($"Turn {turn}, initiative {log.Order + 1}: {log.ActorName} could not act ({FindFailureDetail(log.Trace)}).");
                }
            }

            if (step.Request is { } request)
            {
                var actorName = world.Entities.TryGetValue(request.ActorId, out var entity)
                    ? entity.Name
                    : request.ActorId.ToString();
                runtimeObservations.Add($"Turn {turn}, initiative {step.NextActorIndex + 1}: {actorName} is awaiting PlayerChoice input; headless run stopped before resolving player input.");
                break;
            }

            if (step.Diagnostics.Count > 0)
            {
                runtimeObservations.AddRange(step.Diagnostics.Select(diagnostic => $"Turn {turn}: {diagnostic}"));
                break;
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

    private static ActionPlanDescriptor? GetActionPlanDescriptor(PrototypeContentRegistry registry, EntityId entityId)
    {
        if (!registry.TryGetTemplateIdForEntity(entityId, out var templateId))
        {
            return null;
        }

        var template = registry.GetEntityTemplate(templateId);
        return template.DefaultActionPlanId is { } planId
            && registry.ActionPlanDescriptors.TryGetValue(planId, out var descriptor)
                ? descriptor
                : null;
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
