using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Editor;

public sealed class AgentContentEditorApi(ContentEditorSession session)
{
    private static readonly HashSet<PlanEffectKind> SupportedAuthoringEffects =
    [
        PlanEffectKind.Teleport,
        PlanEffectKind.Move,
        PlanEffectKind.Pickup,
        PlanEffectKind.Drop,
        PlanEffectKind.ReverseDirection,
        PlanEffectKind.Wait,
        PlanEffectKind.CallPlan
    ];

    public ContentEditorSession Session { get; } = session;

    public static AgentContentEditorApi CreateNew() => new(ContentEditorSession.CreateNew());

    public static AgentApiResult<AgentContentEditorApi> OpenFile(string path)
    {
        var result = ContentEditorSession.OpenFile(path);

        return result.IsSuccess
            ? AgentApiResult<AgentContentEditorApi>.Success(new AgentContentEditorApi(result.Session!))
            : AgentApiResult<AgentContentEditorApi>.Failure(AgentApiError.FromMessage("OpenFileFailed", result.ErrorMessage));
    }

    public AgentApiResult Save() => FromFileOperation("SaveFailed", Session.Save());

    public AgentApiResult SaveAs(string path) => FromFileOperation("SaveAsFailed", Session.SaveAs(path));

    public AgentApiResult Reload() => FromFileOperation("ReloadFailed", Session.Reload());

    public AgentDocumentSnapshot GetDocumentSnapshot() =>
        new(
            Session.FilePath,
            Session.IsDirty,
            Session.GetYamlPreview(),
            Session.GetYamlDiff().Lines,
            Session.Editor.Validate(),
            Session.Document.ValidateCanonicalAuthoring());

    public AgentApiResult<ContentValidationResult> Validate() =>
        AgentApiResult<ContentValidationResult>.Success(Session.Editor.Validate());

    public AgentApiResult<ContentValidationResult> ValidateCanonicalAuthoring() =>
        AgentApiResult<ContentValidationResult>.Success(Session.Document.ValidateCanonicalAuthoring());

    public AgentApiResult<AgentScenarioRunReport> RunScenario(AgentScenarioRunRequest request) =>
        Try("RunScenarioFailed", () => AgentScenarioRunner.Run(Session, request));

    public AgentApiResult<EntityTemplateId> CreateEntityTemplate(string name) =>
        Try("CreateEntityTemplateFailed", () => Session.Editor.CreateEntityPreset(name));

    public AgentApiResult UpdateEntityTemplate(EntityTemplateId id, AgentEntityTemplateUpdate update) =>
        Try("UpdateEntityTemplateFailed", () =>
        {
            var preset = Session.Editor.GetEntityPreset(id);
            var template = preset.Template with
            {
                Name = update.Name ?? preset.Template.Name,
                InventoryWidth = update.InventoryWidth ?? preset.Template.InventoryWidth,
                InventoryHeight = update.InventoryHeight ?? preset.Template.InventoryHeight,
                Weight = update.Weight ?? preset.Template.Weight,
                CarryingCapacity = update.CarryingCapacity ?? preset.Template.CarryingCapacity
            };
            var presentation = preset.Presentation with
            {
                Glyph = update.Glyph ?? preset.Presentation.Glyph,
                Color = update.Color ?? preset.Presentation.Color
            };

            Session.Editor.UpdateEntityPreset(id, template, presentation);
        });

    public AgentApiResult SetDefaultActionPlan(EntityTemplateId entityTemplateId, ActionPlanTemplateId actionPlanId) =>
        Try("SetDefaultActionPlanFailed", () => Session.Editor.SetDefaultActionPlan(entityTemplateId, actionPlanId));

    public AgentApiResult ClearDefaultActionPlan(EntityTemplateId entityTemplateId) =>
        Try("ClearDefaultActionPlanFailed", () => Session.Editor.ClearDefaultActionPlan(entityTemplateId));

    public AgentApiResult SetInitialFacing(EntityTemplateId entityTemplateId, Direction facing) =>
        Try("SetInitialFacingFailed", () => Session.Editor.SetInitialFacing(entityTemplateId, facing));

    public AgentApiResult ClearInitialFacing(EntityTemplateId entityTemplateId) =>
        Try("ClearInitialFacingFailed", () => Session.Editor.ClearInitialFacing(entityTemplateId));

    public AgentApiResult<EntityId> PlaceCarriedEntity(EntityTemplateId parentTemplateId, EntityTemplateId carriedTemplateId, GridCoord coord) =>
        Try("PlaceCarriedEntityFailed", () => Session.Editor.PlaceCarriedEntity(parentTemplateId, carriedTemplateId, coord));

    public AgentApiResult<EntityId> PlaceCarriedEntity(EntityTemplateId parentTemplateId, EntityId entityId, EntityTemplateId carriedTemplateId, GridCoord coord) =>
        Try("PlaceCarriedEntityFailed", () =>
        {
            Session.Editor.PlaceCarriedEntity(parentTemplateId, entityId, carriedTemplateId, coord);
            return entityId;
        });

    public AgentApiResult<ActionPlanTemplateId> CreateActionPlan(string name) =>
        Try("CreateActionPlanFailed", () => Session.Editor.CreateActionPlan(name));

    public AgentApiResult<IReadOnlyList<ActionStepDescriptor>> ListActionSteps() =>
        AgentApiResult<IReadOnlyList<ActionStepDescriptor>>.Success(Session.Editor.ListActionSteps());

    public AgentApiResult<ActionPlanPreview> PreviewActionPlan(ActionPlanTemplateId planId, EntityTemplateId? entityTemplateId = null) =>
        Try("PreviewActionPlanFailed", () => Session.Editor.PreviewActionPlan(planId, entityTemplateId));

    public AgentApiResult SetActionPlanPrimitive(ActionPlanTemplateId planId, ActionPlanPrimitiveKind kind, ActionPlanId? fallbackPlanId = null) =>
        Try("SetActionPlanPrimitiveFailed", () => Session.Editor.SetActionPlanPrimitive(planId, kind, fallbackPlanId));

    public AgentApiResult ClearActionPlanPrimitive(ActionPlanTemplateId planId) =>
        Try("ClearActionPlanPrimitiveFailed", () => Session.Editor.ClearActionPlanPrimitive(planId));

    public AgentApiResult<PrimitiveActionPlanChain> CreateMoveFacingPickupTargetChain(string moveFacingPlanName, string pickupTargetPlanName) =>
        Try("CreateMoveFacingPickupTargetChainFailed", () => Session.Editor.CreateMoveFacingPickupTargetChain(moveFacingPlanName, pickupTargetPlanName));

    public AgentApiResult<ActionPlanTemplateId> CreateMoveFacingPickupTargetBehavior(string behaviorPlanName) =>
        Try("CreateMoveFacingPickupTargetBehaviorFailed", () => Session.Editor.CreateMoveFacingPickupTargetBehavior(behaviorPlanName));

    public AgentApiResult SetActionPlanBehavior(ActionPlanTemplateId planId, IReadOnlyList<ActionPlanBehaviorStepKind> steps) =>
        Try("SetActionPlanBehaviorFailed", () => Session.Editor.SetActionPlanBehavior(planId, steps));

    public AgentApiResult ClearActionPlanBehavior(ActionPlanTemplateId planId) =>
        Try("ClearActionPlanBehaviorFailed", () => Session.Editor.ClearActionPlanBehavior(planId));

    public AgentApiResult AddActionPlanBehaviorStep(ActionPlanTemplateId planId, ActionPlanBehaviorStepKind kind) =>
        Try("AddActionPlanBehaviorStepFailed", () => Session.Editor.AddActionPlanBehaviorStep(planId, kind));

    public AgentApiResult MoveActionPlanBehaviorStep(ActionPlanTemplateId planId, int fromIndex, int toIndex) =>
        Try("MoveActionPlanBehaviorStepFailed", () => Session.Editor.MoveActionPlanBehaviorStep(planId, fromIndex, toIndex));

    public AgentApiResult RemoveActionPlanBehaviorStep(ActionPlanTemplateId planId, int stepIndex) =>
        Try("RemoveActionPlanBehaviorStepFailed", () => Session.Editor.RemoveActionPlanBehaviorStep(planId, stepIndex));

    public AgentApiResult AddActionPlanStep(ActionPlanTemplateId planId, AgentActionPlanStepRequest step) =>
        Try("AddActionPlanStepFailed", () => Session.Editor.AddActionPlanStep(planId, step.ToDescriptor()));

    public AgentApiResult UpdateActionPlanStep(ActionPlanTemplateId planId, int stepIndex, AgentActionPlanStepRequest step) =>
        Try("UpdateActionPlanStepFailed", () => Session.Editor.UpdateActionPlanStep(planId, stepIndex, step.ToDescriptor()));

    public AgentApiResult MoveActionPlanStep(ActionPlanTemplateId planId, int fromIndex, int toIndex) =>
        Try("MoveActionPlanStepFailed", () => Session.Editor.MoveActionPlanStep(planId, fromIndex, toIndex));

    public AgentApiResult RemoveActionPlanStep(ActionPlanTemplateId planId, int stepIndex) =>
        Try("RemoveActionPlanStepFailed", () => Session.Editor.RemoveActionPlanStep(planId, stepIndex));

    public AgentApiResult AddActionPlanCheck(ActionPlanTemplateId planId, int stepIndex, PlanCheckKind kind) =>
        Try("AddActionPlanCheckFailed", () => Session.Editor.AddActionPlanCheck(planId, stepIndex, kind));

    public AgentApiResult UpdateActionPlanCheck(ActionPlanTemplateId planId, int stepIndex, int checkIndex, PlanCheckKind kind) =>
        Try("UpdateActionPlanCheckFailed", () => Session.Editor.UpdateActionPlanCheck(planId, stepIndex, checkIndex, kind));

    public AgentApiResult SetActionPlanStepSuccessEffect(ActionPlanTemplateId planId, int stepIndex, PlanEffectDescriptor effect) =>
        SetActionPlanStepEffect(planId, stepIndex, effect, updateSuccess: true);

    public AgentApiResult SetActionPlanStepFailureEffect(ActionPlanTemplateId planId, int stepIndex, PlanEffectDescriptor effect) =>
        SetActionPlanStepEffect(planId, stepIndex, effect, updateSuccess: false);

    private AgentApiResult SetActionPlanStepEffect(ActionPlanTemplateId planId, int stepIndex, PlanEffectDescriptor effect, bool updateSuccess)
    {
        if (!SupportedAuthoringEffects.Contains(effect.Kind))
        {
            return AgentApiResult.Failure(new AgentApiError(
                "UnsupportedEffectForAuthoring",
                $"Effect {effect.Kind} is not supported by canonical agent authoring.",
                Recoverable: true));
        }

        return Try(updateSuccess ? "SetSuccessEffectFailed" : "SetFailureEffectFailed", () =>
        {
            if (updateSuccess)
            {
                Session.Editor.SetActionPlanStepSuccessEffect(planId, stepIndex, effect);
            }
            else
            {
                Session.Editor.SetActionPlanStepFailureEffect(planId, stepIndex, effect);
            }
        });
    }

    private static AgentApiResult FromFileOperation(string code, ContentEditorFileOperationResult result) =>
        result.IsSuccess
            ? AgentApiResult.Success()
            : AgentApiResult.Failure(AgentApiError.FromMessage(code, result.ErrorMessage));

    private static AgentApiResult Try(string code, Action operation)
    {
        try
        {
            operation();
            return AgentApiResult.Success();
        }
        catch (Exception ex)
        {
            return AgentApiResult.Failure(AgentApiError.FromException(code, ex));
        }
    }

    private static AgentApiResult<T> Try<T>(string code, Func<T> operation)
    {
        try
        {
            return AgentApiResult<T>.Success(operation());
        }
        catch (Exception ex)
        {
            return AgentApiResult<T>.Failure(AgentApiError.FromException(code, ex));
        }
    }
}

public sealed record AgentDocumentSnapshot(
    string? FilePath,
    bool IsDirty,
    string YamlPreview,
    IReadOnlyList<string> YamlDiffLines,
    ContentValidationResult Validation,
    ContentValidationResult CanonicalValidation);

public sealed record AgentEntityTemplateUpdate(
    string? Name = null,
    int? InventoryWidth = null,
    int? InventoryHeight = null,
    int? Weight = null,
    int? CarryingCapacity = null,
    char? Glyph = null,
    PresentationColor? Color = null);

public sealed record AgentActionPlanStepRequest(
    string Label,
    IReadOnlyList<PlanCheckDescriptor>? Checks = null,
    PlanEffectDescriptor? OnSuccess = null,
    PlanEffectDescriptor? OnFailure = null)
{
    public ActionPlanStepDescriptor ToDescriptor() =>
        new(Label, Checks ?? [], OnSuccess, OnFailure);
}

public sealed record AgentScenarioRunRequest(
    EntityTemplateId ScenarioRootEntityTemplateId,
    int TurnCount);

public sealed record AgentScenarioActorSummary(
    EntityId EntityId,
    string Name,
    PlaneCoord Location);

public sealed record AgentScenarioTurnReport(
    int TurnNumber,
    int InitiativeIndex,
    EntityId ActorId,
    string ActorName,
    IReadOnlyList<string> TraceLines);

public sealed record AgentScenarioRunReport(
    EntityTemplateId ScenarioRootEntityTemplateId,
    EntityId ScenarioRootEntityId,
    PlaneId ScenarioPlaneId,
    IReadOnlyList<AgentScenarioActorSummary> ActorOrder,
    IReadOnlyList<AgentScenarioTurnReport> Turns,
    IReadOnlyList<string> SetupLines,
    IReadOnlyList<string> FinalStateLines,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeObservations,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps);

internal static class AgentScenarioRunner
{
    private static readonly EntityId ScenarioRootEntityId = new("scenarioRoot");
    private static readonly PlaneId ScenarioHostPlaneId = new("scenarioHost");
    private static readonly PlaneId ScenarioPlaneId = new("scenarioRoot");

    public static AgentScenarioRunReport Run(ContentEditorSession session, AgentScenarioRunRequest request)
    {
        if (request.TurnCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Scenario turn count must be non-negative.");
        }

        var validationDiagnostics = session.Editor.Validate().Errors
            .Concat(session.Document.ValidateCanonicalAuthoring().Errors)
            .Distinct()
            .ToList();
        var registry = session.Document.ToRegistry();
        var world = new WorldState();
        AddRectangularPlane(world, new Plane(ScenarioHostPlaneId, "Scenario Host", 1, 1));
        var spawn = registry.SpawnEntity(
            world,
            request.ScenarioRootEntityTemplateId,
            new EntitySpawnOptions(
                ScenarioRootEntityId,
                new PlaneCoord(ScenarioHostPlaneId, new GridCoord(0, 0)),
                InventoryPlaneId: ScenarioPlaneId,
                InventoryPlaneName: "Scenario Space"));

        if (world.GetInventoryPlaneId(ScenarioRootEntityId) is not { } scenarioPlaneId)
        {
            validationDiagnostics.Add($"scenario root {request.ScenarioRootEntityTemplateId} has no usable inventory space.");
            return CreateReport(request, scenarioPlaneId: ScenarioPlaneId, world, [], [], [], validationDiagnostics, [], [], []);
        }

        var actorOrder = GetScenarioActorsInInitiativeOrder(world, spawn.ActionPlans, scenarioPlaneId);
        var setupLines = CreateSetupLines(world, scenarioPlaneId, actorOrder);
        var turns = new List<AgentScenarioTurnReport>();
        var runtimeObservations = new List<string>();
        var runtimeFailures = new List<string>();
        var movement = new MovementService();

        for (var turn = 1; turn <= request.TurnCount; turn++)
        {
            for (var initiative = 0; initiative < actorOrder.Count; initiative++)
            {
                var actor = actorOrder[initiative];
                if (!world.Entities.TryGetValue(actor.EntityId, out var entity) || !spawn.ActionPlans.TryGetValue(actor.EntityId, out var actionPlan))
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
                turns.Add(new AgentScenarioTurnReport(
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
            request,
            scenarioPlaneId,
            world,
            actorOrder,
            turns,
            setupLines,
            validationDiagnostics,
            runtimeObservations,
            runtimeFailures,
            []);
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

    private static AgentScenarioRunReport CreateReport(
        AgentScenarioRunRequest request,
        PlaneId scenarioPlaneId,
        WorldState world,
        IReadOnlyList<AgentScenarioActorSummary> actorOrder,
        IReadOnlyList<AgentScenarioTurnReport> turns,
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
            validationDiagnostics,
            runtimeObservations,
            runtimeFailures,
            capabilityGaps);

    private static IReadOnlyList<AgentScenarioActorSummary> GetScenarioActorsInInitiativeOrder(
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
            .Select(entry => new AgentScenarioActorSummary(
                entry.EntityId,
                world.Entities[entry.EntityId].Name,
                entry.Location))
            .ToList();

    private static IReadOnlyList<string> CreateSetupLines(
        WorldState world,
        PlaneId scenarioPlaneId,
        IReadOnlyList<AgentScenarioActorSummary> actorOrder)
    {
        var lines = new List<string>
        {
            $"Scenario plane: {scenarioPlaneId}",
            "Actor initiative order:"
        };
        lines.AddRange(actorOrder.Select((actor, index) => $"  - {index + 1}. {actor.Name}: {actor.Location}, {FormatActionState(world, actor.EntityId)}"));
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

public sealed record AgentApiError(
    string Code,
    string Message,
    bool Recoverable = true,
    IReadOnlyList<string>? SuggestedActions = null)
{
    public static AgentApiError FromMessage(string code, string? message) =>
        new(code, string.IsNullOrWhiteSpace(message) ? "Operation failed." : message);

    public static AgentApiError FromException(string code, Exception exception) =>
        new(code, exception.Message);
}

public record AgentApiResult(AgentApiError? Error)
{
    public bool IsSuccess => Error is null;

    public static AgentApiResult Success() => new(Error: null);

    public static AgentApiResult Failure(AgentApiError error) => new(error);
}

public sealed record AgentApiResult<T>(T? Value, AgentApiError? Error) : AgentApiResult(Error)
{
    public static AgentApiResult<T> Success(T value) => new(value, Error: null);

    public new static AgentApiResult<T> Failure(AgentApiError error) => new(default, error);
}
