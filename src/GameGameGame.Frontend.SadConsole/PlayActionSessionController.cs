using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Frontend.SadConsole;

internal sealed class PlayActionSessionController
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

    public PlayActionSessionController(PlayableScenarioSession session)
    {
        Session = session;
        _runtimeActionPlans = new Dictionary<EntityId, IEntityActionPlan>(session.ActionPlans);
        ControlledActorId = session.PlayerEntityId;
        _commands = new ControlledActorCommandService(
            _movement,
            _emptyActionPlans,
            (world, entityId) => TargetingService.RefreshTargets(world, session.Registry, entityId));
        _actionChoices = new ActionChoiceService(_movement);
        _initiativeStepper = new InitiativePlayerChoiceStepper(_movement, _actionChoices);
        _history = SimulationHistorySession.Start(
            session.World,
            ControlledActorId,
            session.ActivePlaneId,
            session.ActiveContainerEntityId);
        RefreshRuntimeActorFacts();
        AdvanceUntilPlayerChoice(0);
        RefreshAfterRuntimeSubmission();
    }

    public PlayableScenarioSession Session { get; }
    public WorldState World => Session.World;
    public EntityId ControlledActorId { get; private set; }
    public int FrameIndex => _history.CurrentFrameIndex;
    public ActionChoiceRequest? CurrentActionChoiceRequest { get; private set; }
    public ActionLogProjection? ActionLog { get; private set; }
    public IReadOnlyDictionary<EntityId, IEntityActionPlan> ProjectionActionPlans => _runtimeActionPlans;

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

    public PlayMovementResult SubmitMove(Direction direction)
    {
        var before = World.Entities.ContainsKey(ControlledActorId)
            ? World.GetEntityLocation(ControlledActorId).Coord
            : new GridCoord(0, 0);
        var usedCoreChoice = CurrentActionChoiceRequest is { } request
            && request.Choices.Any(choice => choice.Kind == ActionChoiceKind.Move);

        EnsureHistoryControlledActor(ControlledActorId);
        var result = usedCoreChoice
            ? _history.SubmitActionChoice(
                _actionChoices,
                CurrentActionChoiceRequest!,
                direction,
                _emptyActionPlans,
                RefreshTargets)
            : _history.SubmitControlledCommand(_commands, ControlledActorCommand.Move(direction));

        RefreshAfterControlledSubmission(result.Succeeded);
        var after = World.Entities.ContainsKey(ControlledActorId)
            ? World.GetEntityLocation(ControlledActorId).Coord
            : before;
        return new PlayMovementResult(result, before, after, usedCoreChoice);
    }

    private void RefreshActionChoiceRequest()
    {
        CurrentActionChoiceRequest = GetActionPlanDescriptorForEntity(ControlledActorId) is { } descriptor
            ? _actionChoices.CreateRequest(World, ControlledActorId, descriptor)
            : null;
    }

    private void RefreshAfterControlledSubmission(bool succeeded)
    {
        if (succeeded)
        {
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

        var historyContextActorId = result.Request?.ActorId ?? ControlledActorId;
        var (activePlaneId, activeContainerId) = ResolveActiveHistoryContext(historyContextActorId);
        if (result.ActorLogs.Count > 0)
        {
            _history.RecordActorInterval(result.ActorLogs, activePlaneId, activeContainerId);
        }

        RefreshRuntimeActorFacts();
        _initiativeCursor = result.NextActorIndex;
        if (result.Request is { } request)
        {
            ControlledActorId = request.ActorId;
            CurrentActionChoiceRequest = request;
            EnsureHistoryControlledActor(ControlledActorId, activePlaneId, activeContainerId);
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
}
