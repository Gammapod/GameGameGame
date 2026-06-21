using GameGameGame.Content;
using GameGameGame.Core;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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

    public AgentApiResult<AgentScenarioRecordingReport> RecordScenario(AgentScenarioRecordingRequest request) =>
        Try("RecordScenarioFailed", () => AgentScenarioRecorder.Record(Session, request));

    public AgentApiResult<AgentScenarioMaterializationReport> MaterializeScenario(AgentAlphaScenarioDefinition definition) =>
        Try("MaterializeScenarioFailed", () => AlphaScenarioMaterializer.Materialize(Session, definition).ToAgentReport());

    public AgentApiResult<AgentScenarioMaterializationReport> MaterializeScenario(string scenarioId) =>
        Try("MaterializeScenarioFailed", () => AlphaScenarioMaterializer.Materialize(Session, ToAgentDefinition(Session.Editor.GetScenario(scenarioId))).ToAgentReport());

    public AgentApiResult UpsertScenario(AgentAlphaScenarioDefinition definition) =>
        Try("UpsertScenarioFailed", () => Session.Editor.UpsertScenario(ToContentDefinition(definition)));

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

    private static ScenarioDefinition ToContentDefinition(AgentAlphaScenarioDefinition definition) =>
        new(
            definition.ScenarioId,
            definition.Name,
            definition.ScenarioRootEntityTemplateId,
            definition.PlayerEntityTemplateId,
            definition.PlayerEntityId,
            definition.PlayerStart);

    private static AgentAlphaScenarioDefinition ToAgentDefinition(ScenarioDefinition definition) =>
        new(
            definition.ScenarioId,
            definition.Name,
            definition.ScenarioRootEntityTemplateId,
            definition.PlayerEntityTemplateId,
            definition.PlayerEntityId,
            definition.PlayerStart);

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

public sealed record AgentScenarioRecordingRequest(
    string ScenarioId,
    int TurnCount,
    string OutputDirectory);

public sealed record AgentScenarioRecordingFrame(
    int FrameIndex,
    int TurnNumber,
    string PngPath);

public sealed record AgentScenarioRecordingReport(
    string ScenarioId,
    string Name,
    PlaneId ScenarioPlaneId,
    EntityId? PlayerEntityId,
    IReadOnlyList<AgentScenarioRecordingFrame> Frames,
    string? GifPath,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeObservations,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps);

public sealed record AgentAlphaScenarioDefinition(
    string ScenarioId,
    string Name,
    EntityTemplateId ScenarioRootEntityTemplateId,
    EntityTemplateId PlayerEntityTemplateId,
    EntityId PlayerEntityId,
    GridCoord PlayerStart);

public sealed record AgentScenarioMaterializationReport(
    string ScenarioId,
    string Name,
    EntityTemplateId ScenarioRootEntityTemplateId,
    EntityId ScenarioRootEntityId,
    EntityTemplateId? PlayerEntityTemplateId,
    EntityId? PlayerEntityId,
    PlaneId ScenarioPlaneId,
    PlaneCoord? PlayerLocation,
    IReadOnlyList<string> SetupLines,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps);

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
    private static readonly EntityId ScenarioRootEntityId = AlphaScenarioMaterializer.DefaultScenarioRootEntityId;
    private static readonly PlaneId ScenarioPlaneId = AlphaScenarioMaterializer.DefaultScenarioPlaneId;

    public static AgentScenarioRunReport Run(ContentEditorSession session, AgentScenarioRunRequest request)
    {
        if (request.TurnCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Scenario turn count must be non-negative.");
        }

        var materialization = AlphaScenarioMaterializer.MaterializeRootOnly(
            session,
            "legacy-run",
            "Legacy RunScenario",
            request.ScenarioRootEntityTemplateId,
            ScenarioRootEntityId,
            ScenarioPlaneId);
        var validationDiagnostics = materialization.ValidationDiagnostics.ToList();
        var world = materialization.World;

        if (!materialization.CanSimulate || materialization.ScenarioPlaneId is not { } scenarioPlaneId)
        {
            return CreateReport(request, scenarioPlaneId: ScenarioPlaneId, world, [], [], [], validationDiagnostics, [], [], []);
        }

        var actorOrder = GetScenarioActorsInInitiativeOrder(world, materialization.ActionPlans, scenarioPlaneId);
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

}

internal static class AgentScenarioRecorder
{
    public static AgentScenarioRecordingReport Record(ContentEditorSession session, AgentScenarioRecordingRequest request)
    {
        if (request.TurnCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Scenario recording turn count must be non-negative.");
        }

        var validationDiagnostics = new List<string>();
        if (string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            validationDiagnostics.Add("recording output directory is required.");
        }
        else if (!Directory.Exists(request.OutputDirectory))
        {
            validationDiagnostics.Add($"recording output directory does not exist: {request.OutputDirectory}.");
        }

        AgentAlphaScenarioDefinition definition;
        try
        {
            definition = ToAgentDefinition(session.Editor.GetScenario(request.ScenarioId));
        }
        catch (KeyNotFoundException ex)
        {
            validationDiagnostics.Add(ex.Message);
            return CreateReport(
                request,
                name: request.ScenarioId,
                scenarioPlaneId: AlphaScenarioMaterializer.DefaultScenarioPlaneId,
                playerEntityId: null,
                frames: [],
                gifPath: null,
                validationDiagnostics,
                runtimeObservations: [],
                runtimeFailures: [],
                capabilityGaps: []);
        }

        if (validationDiagnostics.Count > 0)
        {
            return CreateReport(
                request,
                definition.Name,
                AlphaScenarioMaterializer.DefaultScenarioPlaneId,
                definition.PlayerEntityId,
                frames: [],
                gifPath: null,
                validationDiagnostics,
                runtimeObservations: [],
                runtimeFailures: [],
                capabilityGaps: []);
        }

        var materialization = AlphaScenarioMaterializer.Materialize(session, definition);
        validationDiagnostics.AddRange(materialization.ValidationDiagnostics);

        if (!materialization.CanSimulate || materialization.ScenarioPlaneId is not { } scenarioPlaneId)
        {
            return CreateReport(
                request,
                definition.Name,
                materialization.ScenarioPlaneId ?? AlphaScenarioMaterializer.DefaultScenarioPlaneId,
                definition.PlayerEntityId,
                frames: [],
                gifPath: null,
                validationDiagnostics,
                runtimeObservations: [],
                materialization.RuntimeFailures,
                materialization.CapabilityGaps);
        }

        var outputDirectory = Path.GetFullPath(request.OutputDirectory);
        var baseName = SanitizeFileName(request.ScenarioId);
        var frames = new List<AgentScenarioRecordingFrame>();
        var runtimeObservations = new List<string>();

        var renderer = new DebugScenarioFrameRenderer();

        AddFrame(frames, outputDirectory, baseName, frameIndex: 0, turnNumber: 0, path =>
            renderer.RenderPng(materialization.World, materialization.Registry!, materialization.ActionPlans, scenarioPlaneId, definition.PlayerEntityId, frameIndex: 0, turnNumber: 0, definition.ScenarioId, definition.Name, path));
        RunFullScenarioTurns(materialization.World, materialization.ActionPlans, scenarioPlaneId, request.TurnCount, runtimeObservations, turnNumber =>
            AddFrame(frames, outputDirectory, baseName, frameIndex: turnNumber, turnNumber, path =>
                renderer.RenderPng(materialization.World, materialization.Registry!, materialization.ActionPlans, scenarioPlaneId, definition.PlayerEntityId, turnNumber, turnNumber, definition.ScenarioId, definition.Name, path)));

        var gifPath = Path.Combine(outputDirectory, $"{baseName}.gif");
        WriteGif(frames, gifPath);
        return CreateReport(
            request,
            definition.Name,
            scenarioPlaneId,
            definition.PlayerEntityId,
            frames,
            gifPath,
            validationDiagnostics,
            runtimeObservations,
            materialization.RuntimeFailures,
            materialization.CapabilityGaps);
    }

    private static void RunFullScenarioTurns(
        WorldState world,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        PlaneId scenarioPlaneId,
        int turnCount,
        List<string> runtimeObservations,
        Action<int> afterTurn)
    {
        var actorOrder = GetScenarioActorsInInitiativeOrder(world, actionPlans, scenarioPlaneId);
        var movement = new MovementService();

        for (var turn = 1; turn <= turnCount; turn++)
        {
            for (var initiative = 0; initiative < actorOrder.Count; initiative++)
            {
                var actorId = actorOrder[initiative];
                if (!world.Entities.TryGetValue(actorId, out var entity) || !actionPlans.TryGetValue(actorId, out var actionPlan))
                {
                    continue;
                }

                var resolution = ResolvePlan(world, actorId, actionPlan.PlanTurn(world, actorId, movement), movement);
                world.RecordTrace(resolution.Trace);

                if (resolution.ConsumesTurn)
                {
                    world.AdvanceTurn();
                }

                if (!resolution.Succeeded)
                {
                    runtimeObservations.Add($"Turn {turn}, initiative {initiative + 1}: {entity.Name} could not act.");
                }
            }

            afterTurn(turn);
        }
    }

    private static IReadOnlyList<EntityId> GetScenarioActorsInInitiativeOrder(
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
            .Select(entry => entry.EntityId)
            .ToList();

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

    private static void AddFrame(List<AgentScenarioRecordingFrame> frames, string outputDirectory, string baseName, int frameIndex, int turnNumber, Action<string> writeFrame)
    {
        var path = Path.Combine(outputDirectory, $"{baseName}_frame_{frameIndex:000}.png");
        writeFrame(path);
        frames.Add(new AgentScenarioRecordingFrame(
            frameIndex,
            turnNumber,
            path));
    }

    private static void WriteGif(IReadOnlyList<AgentScenarioRecordingFrame> frames, string gifPath)
    {
        if (frames.Count == 0)
        {
            return;
        }

        using var gif = Image.Load<Rgba32>(frames[0].PngPath);
        gif.Metadata.GetGifMetadata().RepeatCount = 0;
        gif.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = 75;

        foreach (var frame in frames.Skip(1))
        {
            using var next = Image.Load<Rgba32>(frame.PngPath);
            next.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = 75;
            gif.Frames.AddFrame(next.Frames.RootFrame);
        }

        gif.SaveAsGif(gifPath);
    }

    private static AgentScenarioRecordingReport CreateReport(
        AgentScenarioRecordingRequest request,
        string name,
        PlaneId scenarioPlaneId,
        EntityId? playerEntityId,
        IReadOnlyList<AgentScenarioRecordingFrame> frames,
        string? gifPath,
        IReadOnlyList<string> validationDiagnostics,
        IReadOnlyList<string> runtimeObservations,
        IReadOnlyList<string> runtimeFailures,
        IReadOnlyList<string> capabilityGaps) =>
        new(
            request.ScenarioId,
            name,
            scenarioPlaneId,
            playerEntityId,
            frames,
            gifPath,
            validationDiagnostics,
            runtimeObservations,
            runtimeFailures,
            capabilityGaps);

    private static AgentAlphaScenarioDefinition ToAgentDefinition(ScenarioDefinition definition) =>
        new(
            definition.ScenarioId,
            definition.Name,
            definition.ScenarioRootEntityTemplateId,
            definition.PlayerEntityTemplateId,
            definition.PlayerEntityId,
            definition.PlayerStart);

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var characters = value.Select(character => invalid.Contains(character) ? '_' : character).ToArray();
        var sanitized = new string(characters).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "scenario-recording" : sanitized;
    }
}

internal sealed class DebugScenarioFrameRenderer
{
    private const int CellWidth = 12;
    private const int CellHeight = 18;
    private const int Margin = 12;
    private const int HeaderHeight = 72;
    private const int PaneGap = 12;
    private const int ImageWidth = 960;
    private const int ImageHeight = 640;

    private readonly Font font = CreateFont(size: 14);
    private readonly Font smallFont = CreateFont(size: 11);

    public void RenderPng(
        WorldState world,
        PrototypeContentRegistry registry,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        PlaneId scenarioPlaneId,
        EntityId playerEntityId,
        int frameIndex,
        int turnNumber,
        string scenarioId,
        string scenarioName,
        string path)
    {
        using var image = new Image<Rgba32>(ImageWidth, ImageHeight, ToColor(PresentationColor.Default));
        var inspector = new EntityInspectionService(entityId => registry.GetPresentationForEntity(entityId).ToInspectionAppearance());
        var playerPlaneId = world.GetEntityLocation(playerEntityId).PlaneId;
        var containerId = inspector.FindEntityContainingPlane(world, playerPlaneId) ?? AlphaScenarioMaterializer.DefaultScenarioRootEntityId;
        if (!world.Entities.ContainsKey(containerId))
        {
            containerId = playerEntityId;
        }

        var left = new Rectangle(Margin, HeaderHeight, 616, ImageHeight - HeaderHeight - Margin);
        var right = new Rectangle(left.Right + PaneGap, HeaderHeight, ImageWidth - left.Right - PaneGap - Margin, ImageHeight - HeaderHeight - Margin);
        var visibleCenters = new Dictionary<EntityId, PointF>();

        image.Mutate(context =>
        {
            DrawHeader(context, scenarioId, scenarioName, frameIndex, turnNumber, world.TurnNumber);
            DrawPane(context, world, inspector, registry, actionPlans, containerId, left, "Current Container", visibleCenters);
            DrawPane(context, world, inspector, registry, actionPlans, playerEntityId, right, "Player", visibleCenters);
            DrawTargetArrows(context, world, visibleCenters);
        });

        image.SaveAsPng(path);
    }

    private void DrawHeader(IImageProcessingContext context, string scenarioId, string scenarioName, int frameIndex, int turnNumber, int worldTurnNumber)
    {
        context.DrawText($"Scenario recording: {scenarioId} ({scenarioName})", font, Color.White, new PointF(Margin, 10));
        context.DrawText($"Frame {frameIndex} | Simulated turn {turnNumber} | World turn counter {worldTurnNumber}", smallFont, Color.LightGray, new PointF(Margin, 34));
        context.DrawText("Debug view: metadata above, inventory plane center, carried initiative/info below", smallFont, Color.Gray, new PointF(Margin, 52));
    }

    private void DrawPane(
        IImageProcessingContext context,
        WorldState world,
        EntityInspectionService inspector,
        PrototypeContentRegistry registry,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        EntityId entityId,
        Rectangle bounds,
        string title,
        Dictionary<EntityId, PointF> visibleCenters)
    {
        context.Draw(Color.DarkSlateGray, 1, bounds);
        var panel = inspector.Inspect(world, entityId);
        var titleColor = ToColor(panel.Color);
        context.DrawText($"{title}: {panel.Name}", font, titleColor, new PointF(bounds.X + 8, bounds.Y + 8));
        context.DrawText($"{panel.Glyph} {panel.EntityId} @ {panel.Address}", smallFont, Color.LightGray, new PointF(bounds.X + 8, bounds.Y + 30));

        var propertyY = bounds.Y + 50;
        foreach (var property in panel.Properties.Take(6))
        {
            context.DrawText($"{property.Name}: {property.Value}", smallFont, Color.Gray, new PointF(bounds.X + 8, propertyY));
            propertyY += 15;
        }

        if (panel.InventoryGrid is not { } grid)
        {
            context.DrawText("Inventory: none", smallFont, Color.Gray, new PointF(bounds.X + 8, bounds.Y + 150));
            return;
        }

        var gridPixelWidth = grid.Width * CellWidth;
        var gridPixelHeight = grid.Height * CellHeight;
        var gridLeft = bounds.X + Math.Max(8, (bounds.Width - gridPixelWidth) / 2);
        var gridTop = bounds.Y + 156;
        context.DrawText($"Inventory: {grid.PlaneId} ({grid.Width}x{grid.Height})", smallFont, Color.LightGray, new PointF(bounds.X + 8, gridTop - 20));

        foreach (var cell in grid.Cells)
        {
            var cellBounds = new Rectangle(gridLeft + cell.Coord.X * CellWidth, gridTop + cell.Coord.Y * CellHeight, CellWidth, CellHeight);
            context.Fill(Color.Black, cellBounds);
            context.Draw(Color.DimGray, 1, cellBounds);
            var glyph = cell.EntityId is null ? '.' : cell.Glyph;
            context.DrawText(glyph.ToString(), smallFont, ToColor(cell.Color), new PointF(cellBounds.X + 2, cellBounds.Y + 1));

            if (cell.EntityId is { } occupantId)
            {
                visibleCenters[occupantId] = new PointF(cellBounds.X + cellBounds.Width / 2f, cellBounds.Y + cellBounds.Height / 2f);
                DrawFacingMarker(context, world.GetActionFacing(occupantId), cellBounds);
                DrawTargetMarker(context, world, occupantId, cellBounds);
            }
        }

        DrawCarriedInfo(context, world, registry, actionPlans, grid.PlaneId, bounds, gridTop + gridPixelHeight + 20);
    }

    private void DrawCarriedInfo(IImageProcessingContext context, WorldState world, PrototypeContentRegistry registry, IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans, PlaneId planeId, Rectangle bounds, int top)
    {
        context.DrawText("Order | Entity | State", smallFont, Color.White, new PointF(bounds.X + 8, top));
        var rows = LocalTurnOrderReport.Create(world, planeId, actionPlans, getGlyph: entityId => registry.GetPresentationForEntity(entityId).Glyph).Rows;
        var y = top + 16;
        foreach (var row in rows.Take(12))
        {
            var facing = world.GetActionFacing(row.EntityId)?.ToString() ?? "none";
            var target = world.GetActionTarget(row.EntityId)?.ToString() ?? "none";
            context.DrawText($"{(row.Order < 0 ? "--" : row.Order)} | {row.Glyph} {row.EntityName} | F={facing} T={target}", smallFont, Color.LightGray, new PointF(bounds.X + 8, y));
            y += 15;
        }
    }

    private static void DrawFacingMarker(IImageProcessingContext context, Direction? direction, Rectangle cellBounds)
    {
        if (direction is null)
        {
            return;
        }

        var color = Color.Yellow;
        switch (direction.Value)
        {
            case Direction.North:
                context.DrawLine(color, 1, new PointF(cellBounds.Left, cellBounds.Top), new PointF(cellBounds.Right, cellBounds.Top));
                break;
            case Direction.South:
                context.DrawLine(color, 1, new PointF(cellBounds.Left, cellBounds.Bottom - 1), new PointF(cellBounds.Right, cellBounds.Bottom - 1));
                break;
            case Direction.East:
                context.DrawLine(color, 1, new PointF(cellBounds.Right - 1, cellBounds.Top), new PointF(cellBounds.Right - 1, cellBounds.Bottom));
                break;
            case Direction.West:
                context.DrawLine(color, 1, new PointF(cellBounds.Left, cellBounds.Top), new PointF(cellBounds.Left, cellBounds.Bottom));
                break;
        }
    }

    private static void DrawTargetMarker(IImageProcessingContext context, WorldState world, EntityId entityId, Rectangle cellBounds)
    {
        var target = world.GetActionTarget(entityId);
        if (target is null || target.Value == entityId)
        {
            return;
        }

        context.Draw(Color.White, 1, new Rectangle(cellBounds.X - 3, cellBounds.Y - 3, 7, 7));
    }

    private static void DrawTargetArrows(IImageProcessingContext context, WorldState world, IReadOnlyDictionary<EntityId, PointF> visibleCenters)
    {
        foreach (var (entityId, start) in visibleCenters)
        {
            var target = world.GetActionTarget(entityId);
            if (target is null || target.Value == entityId || !visibleCenters.TryGetValue(target.Value, out var end))
            {
                continue;
            }

            context.DrawLine(Color.White, 1, start, end);
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var length = MathF.Max(1, MathF.Sqrt(dx * dx + dy * dy));
            var ux = dx / length;
            var uy = dy / length;
            var left = new PointF(end.X - ux * 6 - uy * 3, end.Y - uy * 6 + ux * 3);
            var right = new PointF(end.X - ux * 6 + uy * 3, end.Y - uy * 6 - ux * 3);
            context.DrawLine(Color.White, 1, end, left);
            context.DrawLine(Color.White, 1, end, right);
        }
    }

    private static Color ToColor(PresentationColor color) => color switch
    {
        PresentationColor.White => Color.White,
        PresentationColor.Yellow => Color.Yellow,
        PresentationColor.Cyan => Color.Cyan,
        PresentationColor.Green => Color.LimeGreen,
        PresentationColor.DarkGreen => Color.DarkGreen,
        PresentationColor.Earth => Color.SandyBrown,
        PresentationColor.Gray => Color.Gray,
        _ => Color.Black
    };

    private static Font CreateFont(float size)
    {
        if (SystemFonts.TryGet("Segoe UI", out var family)
            || SystemFonts.TryGet("Arial", out family)
            || SystemFonts.TryGet("Consolas", out family))
        {
            return family.CreateFont(size);
        }

        return SystemFonts.Families.First().CreateFont(size);
    }
}

public sealed record AlphaScenarioMaterializationResult(
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
    PrototypeContentRegistry? Registry,
    IReadOnlyList<string> SetupLines,
    IReadOnlyList<string> ValidationDiagnostics,
    IReadOnlyList<string> RuntimeFailures,
    IReadOnlyList<string> CapabilityGaps)
{
    public bool CanSimulate => ValidationDiagnostics.Count == 0 && RuntimeFailures.Count == 0 && ScenarioPlaneId is not null;

    public AgentScenarioMaterializationReport ToAgentReport() =>
        new(
            ScenarioId,
            Name,
            ScenarioRootEntityTemplateId,
            ScenarioRootEntityId,
            PlayerEntityTemplateId,
            PlayerEntityId,
            ScenarioPlaneId ?? AlphaScenarioMaterializer.DefaultScenarioPlaneId,
            PlayerLocation,
            SetupLines,
            ValidationDiagnostics,
            RuntimeFailures,
            CapabilityGaps);
}

public static class AlphaScenarioMaterializer
{
    public static readonly EntityId DefaultScenarioRootEntityId = new("scenarioRoot");
    public static readonly PlaneId DefaultScenarioHostPlaneId = new("scenarioHost");
    public static readonly PlaneId DefaultScenarioPlaneId = new("scenarioRoot");

    public static AlphaScenarioMaterializationResult Materialize(ContentEditorSession session, AgentAlphaScenarioDefinition definition) =>
        Materialize(
            session,
            definition.ScenarioId,
            definition.Name,
            definition.ScenarioRootEntityTemplateId,
            DefaultScenarioRootEntityId,
            DefaultScenarioPlaneId,
            definition.PlayerEntityTemplateId,
            definition.PlayerEntityId,
            definition.PlayerStart);

    internal static AlphaScenarioMaterializationResult MaterializeRootOnly(
        ContentEditorSession session,
        string scenarioId,
        string name,
        EntityTemplateId scenarioRootEntityTemplateId,
        EntityId scenarioRootEntityId,
        PlaneId scenarioPlaneId) =>
        Materialize(
            session,
            scenarioId,
            name,
            scenarioRootEntityTemplateId,
            scenarioRootEntityId,
            scenarioPlaneId,
            playerEntityTemplateId: null,
            playerEntityId: null,
            playerStart: null);

    private static AlphaScenarioMaterializationResult Materialize(
        ContentEditorSession session,
        string scenarioId,
        string name,
        EntityTemplateId scenarioRootEntityTemplateId,
        EntityId scenarioRootEntityId,
        PlaneId scenarioPlaneId,
        EntityTemplateId? playerEntityTemplateId,
        EntityId? playerEntityId,
        GridCoord? playerStart)
    {
        var validationDiagnostics = session.Editor.Validate().Errors
            .Concat(session.Document.ValidateCanonicalAuthoring().Errors)
            .Distinct()
            .ToList();
        var runtimeFailures = new List<string>();
        var capabilityGaps = new List<string>();
        var setupLines = new List<string>();
        var world = new WorldState();
        PrototypeContentRegistry? registry = null;
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>();

        try
        {
            registry = session.Document.ToRegistry();
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
            var playerName = world.Entities.TryGetValue(insertedPlayerEntityId, out var player) ? player.Name : playerEntityTemplateId?.ToString() ?? insertedPlayerEntityId.ToString();
            setupLines.Add($"Player: {playerName} {insertedPlayerEntityId} at {playerLocation}, {FormatActionState(world, insertedPlayerEntityId)}");
        }

        return CreateResult(activeScenarioPlaneId, insertedPlayerLocation);

        AlphaScenarioMaterializationResult CreateResult(PlaneId? resultScenarioPlaneId, PlaneCoord? playerLocation) =>
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
