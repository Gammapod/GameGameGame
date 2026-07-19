using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class GameplaySessionController
{
    private readonly MovementService _movement = new();
    private readonly ControlledActorCommandService _commands;
    private readonly ActionChoiceService _actionChoices;
    private readonly IReadOnlyDictionary<EntityId, IEntityActionPlan> _controlledCommandActionPlans;
    private readonly SimulationHistorySession _history;

    public GameplaySessionController(PlayableScenarioSession session)
    {
        Session = session;
        // Temporary debug wait and direct/action-choice movement use controlled-command compatibility:
        // the controlled actor's authored plan must not also resolve autonomously while it is acting as
        // the player-controlled entity.
        var playerControlledEntityIds = session.PlayerControls.Values
            .SelectMany(entityIds => entityIds)
            .Append(session.PlayerEntityId)
            .ToHashSet();
        _controlledCommandActionPlans = session.ActionPlans
            .Where(entry => !playerControlledEntityIds.Contains(entry.Key))
            .ToDictionary(entry => entry.Key, entry => entry.Value);
        _commands = new ControlledActorCommandService(
            _movement,
            _controlledCommandActionPlans,
            (world, entityId) => TargetingService.RefreshTargets(world, session.Registry, entityId));
        _actionChoices = new ActionChoiceService(_movement);
        _history = SimulationHistorySession.Start(
            session.World,
            session.PlayerEntityId,
            session.ActivePlaneId,
            session.ActiveContainerEntityId);
        RefreshDisplayTargets();
        RefreshActionChoiceRequest();
        ActionLog = ActionLogProjection.FromHistory(_history);
    }

    public PlayableScenarioSession Session { get; }
    public WorldState World => Session.World;
    public EntityId PlayerEntityId => Session.PlayerEntityId;
    public int FrameIndex => _history.CurrentFrame.FrameIndex;
    public ActionChoiceRequest? CurrentActionChoiceRequest { get; private set; }
    public ActionLogProjection? ActionLog { get; private set; }
    public IReadOnlyDictionary<EntityId, IEntityActionPlan> ProjectionActionPlans => Session.ActionPlans;

    public IReadOnlyList<ActionPlanBehaviorStepDescriptor> AvailablePlayerActionSteps() =>
        GetActionPlanDescriptorForEntity(Session.PlayerEntityId)?.Behavior?.Steps ?? [];

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
        var result = _history.SubmitControlledCommand(_commands, ControlledActorCommand.Wait());
        RefreshAfterRuntimeSubmission();
        return new GameplayRuntimeSubmission(result.Succeeded, FailureText(result), UsedCoreActionChoice: false);
    }

    public GameplayRuntimeSubmission SubmitMove(Direction direction)
    {
        RefreshActionChoiceRequest();
        var usedCoreChoice = CurrentActionChoiceRequest is { } request
            && request.Choices.Any(choice => choice.Kind == ActionChoiceKind.Move);
        var result = usedCoreChoice
            ? _history.SubmitActionChoice(
                _actionChoices,
                CurrentActionChoiceRequest!,
                direction,
                _controlledCommandActionPlans,
                RefreshTargets)
            : _history.SubmitControlledCommand(_commands, ControlledActorCommand.Move(direction));

        RefreshAfterRuntimeSubmission();
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
            _controlledCommandActionPlans,
            RefreshTargets);

        RefreshAfterRuntimeSubmission();
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
            _controlledCommandActionPlans,
            RefreshTargets);

        RefreshAfterRuntimeSubmission();
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
            _controlledCommandActionPlans,
            RefreshTargets);

        RefreshAfterRuntimeSubmission();
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
            _controlledCommandActionPlans,
            RefreshTargets);

        RefreshAfterRuntimeSubmission();
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

        RefreshAfterRuntimeSubmission();
        return new GameplayRuntimeSubmission(result.Succeeded, FailureText(result), UsedCoreActionChoice: true);
    }

    private void RefreshActionChoiceRequest()
    {
        CurrentActionChoiceRequest = GetActionPlanDescriptorForEntity(Session.PlayerEntityId) is { } descriptor
            ? _actionChoices.CreateRequest(World, Session.PlayerEntityId, descriptor)
            : null;
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
