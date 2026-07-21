using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class GameplaySessionController
{
    private readonly MovementService _movement = new();
    private readonly ControlledActorCommandService _commands;
    private readonly ActionChoiceService _actionChoices;
    private readonly InitiativePlayerChoiceStepper _initiativeStepper;
    private readonly IReadOnlyList<EntityId> _actorOrder;
    private readonly IReadOnlyDictionary<EntityId, IEntityActionPlan> _automaticActionPlans;
    private readonly IReadOnlyDictionary<EntityId, IEntityActionPlan> _emptyActionPlans = new Dictionary<EntityId, IEntityActionPlan>();
    private SimulationHistorySession _history;
    private int _initiativeCursor;
    private int _frameIndex;
    private EntityId _activeControlledActorId;

    public GameplaySessionController(PlayableScenarioSession session)
    {
        Session = session;
        _actorOrder = session.ActorOrder.Count > 0
            ? session.ActorOrder.Select(actor => actor.EntityId).ToList()
            : session.ActionPlans.Keys.Append(session.PlayerEntityId).Distinct().ToList();
        _automaticActionPlans = session.ActionPlans
            .Where(entry => session.World.GetActionControlSource(entry.Key) != EntityControlSource.PlayerChoice)
            .ToDictionary(entry => entry.Key, entry => entry.Value);
        _activeControlledActorId = session.PlayerEntityId;
        _commands = new ControlledActorCommandService(
            _movement,
            _emptyActionPlans,
            (world, entityId) => TargetingService.RefreshTargets(world, session.Registry, entityId));
        _actionChoices = new ActionChoiceService(_movement);
        _initiativeStepper = new InitiativePlayerChoiceStepper(_movement, _actionChoices);
        _history = SimulationHistorySession.Start(
            session.World,
            _activeControlledActorId,
            session.ActivePlaneId,
            session.ActiveContainerEntityId);
        AdvanceUntilPlayerChoice(0);
        RefreshDisplayTargets();
        RefreshActionChoiceRequest();
        ActionLog = ActionLogProjection.FromHistory(_history);
    }

    public PlayableScenarioSession Session { get; }
    public WorldState World => Session.World;
    public EntityId PlayerEntityId => _activeControlledActorId;
    public int FrameIndex => _frameIndex;
    public ActionChoiceRequest? CurrentActionChoiceRequest { get; private set; }
    public ActionLogProjection? ActionLog { get; private set; }
    public IReadOnlyDictionary<EntityId, IEntityActionPlan> ProjectionActionPlans => Session.ActionPlans;

    public IReadOnlyList<ActionPlanBehaviorStepDescriptor> AvailablePlayerActionSteps() =>
        GetActionPlanDescriptorForEntity(_activeControlledActorId)?.Behavior?.Steps ?? [];

    public ActionPlanDescriptor? GetActionPlanDescriptorForEntity(EntityId entityId)
    {
        if (!Session.Registry.TryGetTemplateIdForEntity(entityId, out var templateId))
        {
            return null;
        }

        var template = Session.Registry.GetEntityTemplate(templateId);
        return template.DefaultActionPlanId is { } planId
            && Session.Registry.ActionPlanDescriptors.TryGetValue(planId, out var descriptor)
                ? descriptor
                : null;
    }

    public void RefreshForFrameBuilding()
    {
        RefreshDisplayTargets();
        RefreshActionChoiceRequest();
    }

    public GameplayRuntimeSubmission SubmitWait()
    {
        if (CurrentActionChoiceRequest is null)
        {
            AdvanceUntilPlayerChoice(_initiativeCursor);
            _frameIndex++;
            RefreshAfterRuntimeSubmission();
            return new GameplayRuntimeSubmission(true, null, UsedCoreActionChoice: false);
        }

        EnsureHistoryControlledActor(_activeControlledActorId);
        var result = _history.SubmitControlledCommand(_commands, ControlledActorCommand.Wait());
        RefreshAfterControlledSubmission(result.Succeeded);
        return new GameplayRuntimeSubmission(result.Succeeded, FailureText(result), UsedCoreActionChoice: false);
    }

    public GameplayRuntimeSubmission SubmitMove(Direction direction)
    {
        RefreshActionChoiceRequest();
        var usedCoreChoice = CurrentActionChoiceRequest is { } request
            && request.Choices.Any(choice => choice.Kind == ActionChoiceKind.Move);
        EnsureHistoryControlledActor(_activeControlledActorId);
        var result = usedCoreChoice
            ? _history.SubmitActionChoice(
                _actionChoices,
                CurrentActionChoiceRequest!,
                direction,
                _emptyActionPlans,
                RefreshTargets)
            : _history.SubmitControlledCommand(_commands, ControlledActorCommand.Move(direction));

        RefreshAfterControlledSubmission(result.Succeeded);
        return new GameplayRuntimeSubmission(result.Succeeded, FailureText(result), usedCoreChoice);
    }

    public GameplayRuntimeSubmission SubmitPickupActionChoice(EntityId targetId, PlaneCoord destination)
    {
        if (CurrentActionChoiceRequest is not { } request)
        {
            return new GameplayRuntimeSubmission(false, "No Core Action Choice request is active.", UsedCoreActionChoice: true);
        }

        var result = _history.SubmitPickupActionChoice(
            _actionChoices,
            request,
            targetId,
            destination,
            _emptyActionPlans,
            RefreshTargets);

        RefreshAfterControlledSubmission(result.Succeeded);
        return new GameplayRuntimeSubmission(result.Succeeded, FailureText(result), UsedCoreActionChoice: true);
    }

    public GameplayRuntimeSubmission SubmitDropActionChoice(EntityId targetId, PlaneCoord destination)
    {
        if (CurrentActionChoiceRequest is not { } request)
        {
            return new GameplayRuntimeSubmission(false, "No Core Action Choice request is active.", UsedCoreActionChoice: true);
        }

        var result = _history.SubmitDropActionChoice(
            _actionChoices,
            request,
            targetId,
            destination,
            _emptyActionPlans,
            RefreshTargets);

        RefreshAfterControlledSubmission(result.Succeeded);
        return new GameplayRuntimeSubmission(result.Succeeded, FailureText(result), UsedCoreActionChoice: true);
    }

    public GameplayRuntimeSubmission SubmitEnterActionChoice(EntityId targetId)
    {
        if (CurrentActionChoiceRequest is not { } request)
        {
            return new GameplayRuntimeSubmission(false, "No Core Action Choice request is active.", UsedCoreActionChoice: true);
        }

        var result = _history.SubmitEnterActionChoice(
            _actionChoices,
            request,
            targetId,
            _emptyActionPlans,
            RefreshTargets);

        RefreshAfterControlledSubmission(result.Succeeded);
        return new GameplayRuntimeSubmission(result.Succeeded, FailureText(result), UsedCoreActionChoice: true);
    }

    public GameplayRuntimeSubmission SubmitExitActionChoice(Direction direction)
    {
        if (CurrentActionChoiceRequest is not { } request)
        {
            return new GameplayRuntimeSubmission(false, "No Core Action Choice request is active.", UsedCoreActionChoice: true);
        }

        var result = _history.SubmitExitActionChoice(
            _actionChoices,
            request,
            direction,
            _emptyActionPlans,
            RefreshTargets);

        RefreshAfterControlledSubmission(result.Succeeded);
        return new GameplayRuntimeSubmission(result.Succeeded, FailureText(result), UsedCoreActionChoice: true);
    }

    public GameplayRuntimeSubmission SubmitAuthoredActionStepChoice(int stepIndex, ActionPlanBehaviorStepDescriptor step)
    {
        if (CurrentActionChoiceRequest is not { } request)
        {
            return new GameplayRuntimeSubmission(false, "No Core Action Choice request is active.", UsedCoreActionChoice: true);
        }

        var result = _history.SubmitAuthoredActionStepChoice(
            _actionChoices,
            request,
            stepIndex,
            step);

        RefreshAfterControlledSubmission(result.Succeeded);
        return new GameplayRuntimeSubmission(result.Succeeded, FailureText(result), UsedCoreActionChoice: true);
    }

    private void RefreshActionChoiceRequest()
    {
        CurrentActionChoiceRequest = GetActionPlanDescriptorForEntity(_activeControlledActorId) is { } descriptor
            ? _actionChoices.CreateRequest(World, _activeControlledActorId, descriptor)
            : null;
    }

    private void RefreshAfterControlledSubmission(bool succeeded)
    {
        if (succeeded)
        {
            _frameIndex++;
            AdvanceUntilPlayerChoice(_initiativeCursor + 1);
        }

        RefreshAfterRuntimeSubmission();
    }

    private void AdvanceUntilPlayerChoice(int startIndex)
    {
        if (_actorOrder.Count == 0)
        {
            CurrentActionChoiceRequest = null;
            return;
        }

        var result = _initiativeStepper.AdvanceUntilPlayerChoice(
            World,
            _actorOrder,
            _automaticActionPlans,
            GetActionPlanDescriptorForEntity,
            startIndex,
            (world, entityId) => TargetingService.RefreshTargets(world, Session.Registry, entityId));

        // TODO(frontend): surface initiative stepper diagnostics in play-mode status once
        // the gameplay screen has a durable non-debug status/log presentation for them.
        if (result.Diagnostics.Count > 0)
        {
        }

        var historyContextActorId = result.Request?.ActorId ?? _activeControlledActorId;
        var (activePlaneId, activeContainerId) = ResolveActiveHistoryContext(historyContextActorId);
        if (result.ActorLogs.Count > 0)
        {
            _history.RecordActorInterval(result.ActorLogs, activePlaneId, activeContainerId);
        }

        _initiativeCursor = result.NextActorIndex;
        if (result.Request is { } request)
        {
            _activeControlledActorId = request.ActorId;
            CurrentActionChoiceRequest = request;
            EnsureHistoryControlledActor(_activeControlledActorId, activePlaneId, activeContainerId);
        }
        else
        {
            CurrentActionChoiceRequest = null;
        }
    }

    private void EnsureHistoryControlledActor(EntityId actorId)
    {
        var (activePlaneId, activeContainerId) = ResolveActiveHistoryContext(actorId);
        EnsureHistoryControlledActor(actorId, activePlaneId, activeContainerId);
    }

    private void EnsureHistoryControlledActor(EntityId actorId, PlaneId activePlaneId, EntityId? activeContainerId)
    {
        if (_history.CurrentFrame.ControlledEntityId == actorId)
        {
            return;
        }

        _history.SetCurrentControlledEntity(actorId, activePlaneId, activeContainerId);
    }

    private (PlaneId ActivePlaneId, EntityId? ActiveContainerId) ResolveActiveHistoryContext(EntityId actorId)
    {
        if (!World.Entities.ContainsKey(actorId))
        {
            return (Session.ActivePlaneId, Session.ActiveContainerEntityId);
        }

        var actorPlaneId = World.GetEntityLocation(actorId).PlaneId;
        var activeContainerId = FindContainerForPlane(actorPlaneId)
            ?? (actorPlaneId == Session.ActivePlaneId ? Session.ActiveContainerEntityId : null);
        return (actorPlaneId, activeContainerId);
    }

    private EntityId? FindContainerForPlane(PlaneId planeId)
    {
        foreach (var (entityId, inventoryPlaneId) in World.InventoryPlanes)
        {
            if (inventoryPlaneId == planeId)
            {
                return entityId;
            }
        }

        return null;
    }

    private void RefreshAfterRuntimeSubmission()
    {
        ActionLog = ActionLogProjection.FromHistory(_history);
        RefreshDisplayTargets();
        RefreshActionChoiceRequest();
    }

    private void RefreshDisplayTargets()
    {
        foreach (var entityId in Session.ActionPlans.Keys)
        {
            TargetingService.RefreshTargets(World, Session.Registry, entityId);
        }
    }

    private void RefreshTargets(WorldState world, EntityId entityId) =>
        TargetingService.RefreshTargets(world, Session.Registry, entityId);

    private static string? FailureText(ControlledActorCommandResult result) =>
        result.FailureDetail ?? result.FailureReason?.ToString();

    private static string? FailureText(PlanExecutionResult result) => null;
}

internal sealed record GameplayRuntimeSubmission(bool Succeeded, string? FailureText, bool UsedCoreActionChoice);
