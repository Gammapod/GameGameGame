using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class GameplaySessionController
{
    private readonly MovementService _movement = new();
    private readonly ControlledActorCommandService _commands;
    private readonly ActionChoiceService _actionChoices;
    private readonly InitiativePlayerChoiceStepper _initiativeStepper;
    private readonly DynamicScenarioActionPlanSynchronizer _actionPlanSynchronizer = new();
    private readonly IReadOnlyDictionary<EntityId, IEntityActionPlan> _emptyActionPlans = new Dictionary<EntityId, IEntityActionPlan>();
    private readonly Dictionary<EntityId, IEntityActionPlan> _runtimeActionPlans;
    private IReadOnlyList<EntityId> _actorOrder = [];
    private IReadOnlyDictionary<EntityId, IEntityActionPlan> _automaticActionPlans = new Dictionary<EntityId, IEntityActionPlan>();
    private SimulationHistorySession _history;
    private int _initiativeCursor;
    private int _frameIndex;
    private EntityId _activeControlledActorId;

    public GameplaySessionController(PlayableScenarioSession session)
    {
        Session = session;
        _runtimeActionPlans = new Dictionary<EntityId, IEntityActionPlan>(session.ActionPlans);
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
        RefreshRuntimeActorFacts();
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
    public IReadOnlyDictionary<EntityId, IEntityActionPlan> ProjectionActionPlans => _runtimeActionPlans;

    public IReadOnlyList<ActionPlanBehaviorStepDescriptor> AvailablePlayerActionSteps() =>
        GetActionPlanDescriptorForEntity(_activeControlledActorId)?.Behavior?.Steps ?? [];

    public ActionPlanDescriptor? GetActionPlanDescriptorForEntity(EntityId entityId)
    {
        if (!Session.Registry.TryGetTemplateIdForEntity(World, entityId, out var templateId))
        {
            return null;
        }

        var template = Session.Registry.GetEntityTemplate(templateId);
        var defaultPlanId = World.GetDefaultActionPlanId(entityId) is { } runtimePlanId
            ? new ActionPlanTemplateId(runtimePlanId.Value)
            : template.DefaultActionPlanId;
        return defaultPlanId is { } planId
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
        if (CurrentActionChoiceRequest is null && !World.Entities.ContainsKey(_activeControlledActorId))
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

    public bool UndoPreviousFrame()
    {
        var targetFrameIndex = _history.Intervals
            .Where(interval => interval.ControlledResult is not null && interval.ToFrameIndex <= _history.CurrentFrameIndex)
            .Select(interval => (int?)interval.FromFrameIndex)
            .LastOrDefault();
        if (targetFrameIndex is null)
        {
            return false;
        }

        _history.RollbackToFrame(targetFrameIndex.Value);
        _activeControlledActorId = _history.CurrentFrame.ControlledEntityId;
        _frameIndex = _history.CurrentFrameIndex;
        RefreshRuntimeActorFacts();
        var actorIndex = _actorOrder.ToList().IndexOf(_activeControlledActorId);
        if (actorIndex >= 0)
        {
            _initiativeCursor = actorIndex;
        }

        RefreshAfterRuntimeSubmission();
        return true;
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

    public GameplayRuntimeSubmission SubmitTransferActionChoice(EntityId counterpartyId, EntityId movingEntityId)
    {
        if (CurrentActionChoiceRequest is not { } request)
        {
            return new GameplayRuntimeSubmission(false, "No Core Action Choice request is active.", UsedCoreActionChoice: true);
        }

        var result = _history.SubmitTransferActionChoice(
            _actionChoices,
            request,
            counterpartyId,
            movingEntityId,
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
        RefreshRuntimeActorFacts();
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

        RefreshRuntimeActorFacts();
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
        RefreshRuntimeActorFacts();
        ActionLog = ActionLogProjection.FromHistory(_history);
        RefreshDisplayTargets();
        RefreshActionChoiceRequest();
    }

    private void RefreshRuntimeActorFacts()
    {
        _actionPlanSynchronizer.SynchronizeInPlace(World, Session.Registry, _runtimeActionPlans);
        var refreshedActorOrder = ScenarioInitiativeOrderService
            .GetScenarioActorsInInitiativeOrder(World, _runtimeActionPlans, Session.ActiveContainerEntityId, Session.ActivePlaneId)
            .Select(actor => actor.EntityId)
            .ToList();
        if (Session.ActorOrder.Count > 0)
        {
            var ordered = Session.ActorOrder
                .Select(actor => actor.EntityId)
                .Where(entityId => World.Entities.ContainsKey(entityId))
                .ToList();
            ordered.AddRange(refreshedActorOrder.Where(entityId => !ordered.Contains(entityId)));
            _actorOrder = ordered;
        }
        else
        {
            _actorOrder = refreshedActorOrder;
        }

        if (_actorOrder.Count == 0)
        {
            _actorOrder = _runtimeActionPlans.Keys.Append(Session.PlayerEntityId).Distinct().ToList();
        }

        _automaticActionPlans = _runtimeActionPlans
            .Where(entry => World.GetActionControlSource(entry.Key) != EntityControlSource.PlayerChoice)
            .ToDictionary(entry => entry.Key, entry => entry.Value);
        if (_actorOrder.Count > 0)
        {
            _initiativeCursor = ((_initiativeCursor % _actorOrder.Count) + _actorOrder.Count) % _actorOrder.Count;
        }
    }

    private void RefreshDisplayTargets()
    {
        foreach (var entityId in _runtimeActionPlans.Keys)
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
