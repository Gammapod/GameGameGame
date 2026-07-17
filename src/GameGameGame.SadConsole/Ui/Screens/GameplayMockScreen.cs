using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Styling;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed record GameplayMockFrame(
    string Title,
    EntityPanelProjection PlayerProjection,
    EntityPanelProjection? CurrentPlaceProjection,
    EntityPanelProjection? InspectedProjection,
    string CurrentRoomSizeLabel,
    IReadOnlyList<string> CurrentPlaceEntityRows,
    IReadOnlyList<string> InspectedTargetingRows,
    IReadOnlyList<string> InspectedActionPlanRows,
    SadConsoleRect CurrentPlaceBounds,
    SadConsoleRect HudBounds,
    SadConsoleRect InspectionBounds,
    IReadOnlyList<IUiComponent> Components,
    IReadOnlyList<string> HudRows,
    IReadOnlyList<string> ActionChoiceRows,
    IReadOnlyList<string> CurrentPlacePlayerLogRows,
    IReadOnlySet<GridCoord> CurrentPlaceValidSelectionCoords,
    GridCoord? CurrentPlaceSelectedCoord,
    IReadOnlySet<GridCoord> InspectionValidSelectionCoords,
    GridCoord? InspectionSelectedCoord,
    IReadOnlyList<string> Diagnostics);

internal sealed class GameplayMockScreen
{
    private enum ActionMenuMode
    {
        Closed,
        ActionList,
        PickupTarget,
        PickupDestination,
        DropSource,
        DropDestination
    }

    private readonly PlayableScenarioSession _session;
    private readonly EntityPanelProjectionService _panelProjection;
    private readonly ControlledActorCommandService _commands;
    private readonly ActionChoiceService _actionChoices;
    private readonly SimulationHistorySession _history;
    private readonly IReadOnlyDictionary<EntityId, IEntityActionPlan> _controlledCommandActionPlans;
    private readonly MovementService _movement = new();
    private ActionLogProjection? _actionLog;
    private ActionChoiceRequest? _currentActionChoiceRequest;
    private EntityId? _inspectedEntityId;
    private int _selectedActionStepIndex;
    private ActionMenuMode _actionMenuMode;
    private ActionChoice? _selectedEntityActionChoice;
    private EntityId? _selectedEntityActionTargetId;
    private int _selectedTargetIndex;
    private int _selectedDestinationIndex;

    public GameplayMockScreen(PlayableScenarioSession session)
    {
        _session = session;
        _panelProjection = new EntityPanelProjectionService(
            entityId => session.Registry.GetPresentationForEntity(entityId).ToInspectionAppearance(),
            GetActionPlanDescriptorForEntity);
        // Temporary debug wait and direct/action-choice movement use controlled-command compatibility:
        // the controlled actor's authored plan must not also resolve autonomously while it is acting as
        // the player-controlled entity.
        _controlledCommandActionPlans = session.ActionPlans
            .Where(entry => entry.Key != session.PlayerEntityId)
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
        RefreshActionChoiceRequest();
        _actionLog = ActionLogProjection.FromHistory(_history);
    }

    public EntityId PlayerEntityId => _session.PlayerEntityId;
    public EntityId? InspectedEntityId => _inspectedEntityId;
    public int FrameIndex => _history.CurrentFrame.FrameIndex;
    public int SelectedActionStepIndex => _selectedActionStepIndex;
    public ActionChoiceRequest? CurrentActionChoiceRequest => _currentActionChoiceRequest;
    public string ActionMenuState => _actionMenuMode.ToString();
    public bool IsActionMenuOpen => _actionMenuMode != ActionMenuMode.Closed;
    public bool UsesCoreActionChoiceMovement => _currentActionChoiceRequest?.Choices.Any(choice => choice.Kind == ActionChoiceKind.Move) == true;
    public bool UsesCoreActionChoicePickup => _currentActionChoiceRequest?.Choices.Any(choice => choice.Kind == ActionChoiceKind.Pickup) == true;
    public bool UsesCoreActionChoiceDrop => _currentActionChoiceRequest?.Choices.Any(choice => choice.Kind == ActionChoiceKind.Drop) == true;
    private WorldState World => _session.World;
    private IReadOnlyDictionary<EntityId, IEntityActionPlan> ProjectionActionPlans => _session.ActionPlans;

    public GameplayMockFrame BuildFrame(int width, int height)
    {
        var safeWidth = Math.Max(40, width);
        var safeHeight = Math.Max(18, height);
        RefreshDisplayTargets();
        RefreshActionChoiceRequest();
        var projectionActionPlans = ProjectionActionPlans;
        var playerProjection = _panelProjection.Project(World, _session.PlayerEntityId, projectionActionPlans, _session.PlayerEntityId, _actionLog);
        var diagnostics = new List<string>();
        diagnostics.AddRange(_session.ValidationDiagnostics);
        diagnostics.AddRange(_session.RuntimeFailures);
        diagnostics.AddRange(_session.CapabilityGaps);

        if (playerProjection.PointOfView is null)
        {
            diagnostics.Add("Player POV projection is unavailable.");
        }
        else
        {
            diagnostics.AddRange(playerProjection.PointOfView.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
        }

        var currentPlaceProjection = playerProjection.PointOfView?.CurrentPlace is { } currentPlace
            ? _panelProjection.Project(World, currentPlace.EntityId, projectionActionPlans, _session.PlayerEntityId, _actionLog)
            : null;

        var hudWidth = Math.Clamp(safeWidth / 5, 20, Math.Max(20, safeWidth - 42));
        var hudBounds = SadConsoleRect.FromSize(0, 0, hudWidth, safeHeight);
        var contentLeft = hudBounds.Left + hudBounds.Width + 1;
        var contentWidth = Math.Max(20, safeWidth - contentLeft - 1);
        var inspectionHeight = Math.Max(8, safeHeight / 3);
        var inspectionTop = Math.Max(8, safeHeight - inspectionHeight);
        var currentPlaceBounds = SadConsoleRect.FromSize(contentLeft, 0, contentWidth, inspectionTop);
        var inspectionBounds = SadConsoleRect.FromSize(
            contentLeft,
            inspectionTop,
            contentWidth,
            Math.Max(0, safeHeight - inspectionTop));

        var components = new List<IUiComponent>();
        components.Add(BuildCurrentPlaceComponent(currentPlaceProjection, currentPlaceBounds));
        EntityPanelProjection? inspectedProjection = null;
        if (_inspectedEntityId is { } inspectedEntityId && World.Entities.ContainsKey(inspectedEntityId))
        {
            inspectedProjection = _panelProjection.Project(World, inspectedEntityId, projectionActionPlans, _session.PlayerEntityId, _actionLog);
            components.Add(BuildInspectionComponent(inspectedProjection, inspectionBounds));
        }

        if (_actionMenuMode == ActionMenuMode.ActionList)
        {
            components.Add(BuildActionSelectorComponent(currentPlaceBounds, AvailablePlayerActionSteps(), _selectedActionStepIndex));
        }

        if (diagnostics.Count > 0)
        {
            components.Add(new PanelComponent(
                "play-mock-diagnostics",
                "POV / setup diagnostics",
                SadConsoleRect.FromSize(contentLeft, Math.Max(1, currentPlaceBounds.Bottom - 7), Math.Min(60, contentWidth), Math.Min(6, currentPlaceBounds.Height - 2)),
                diagnostics.Take(4).ToList(),
                UiComponentState.Error));
        }

        return new GameplayMockFrame(
            $"Play UX Mock | {_session.Name} | frame {FrameIndex} | world turn {World.TurnNumber}",
            playerProjection,
            currentPlaceProjection,
            inspectedProjection,
            DescribeCurrentRoomSize(playerProjection.PointOfView?.CurrentPlace),
            BuildCurrentPlaceEntityRows(
                currentPlaceProjection,
                playerProjection.PointOfView?.TargetAdjectives ?? [],
                playerProjection.PointOfView?.ReciprocalAdjectives ?? []),
            inspectedProjection is null ? [] : BuildTargetingRuleRows(inspectedProjection.EntityId),
            inspectedProjection is null ? [] : BuildActionPlanRows(inspectedProjection.EntityId),
            currentPlaceBounds,
            hudBounds,
            inspectionBounds,
            components,
            BuildHudRows(playerProjection, currentPlaceProjection, AvailablePlayerActionSteps(), _selectedActionStepIndex, DescribeMovementControlMode(), BuildActionChoiceRows(), BuildActionMenuRows()),
            BuildActionChoiceRows(),
            BuildCurrentPlacePlayerLogRows(currentPlaceProjection),
            CurrentPlaceValidSelectionCoords(),
            CurrentPlaceSelectedCoord(),
            InspectionValidSelectionCoords(),
            InspectionSelectedCoord(),
            diagnostics);
    }

    public string InspectNextEntity()
    {
        var frame = BuildFrame(SadConsoleScreenMetrics.ScreenWidth, SadConsoleScreenMetrics.ScreenHeight);
        var candidates = frame.CurrentPlaceProjection?.InventoryGrid?.Cells
            .Select(cell => cell.EntityId)
            .Where(entityId => entityId is not null && entityId != _session.PlayerEntityId)
            .Select(entityId => entityId!.Value)
            .Distinct()
            .OrderBy(entityId => entityId.Value, StringComparer.Ordinal)
            .ToList() ?? [];

        if (candidates.Count == 0)
        {
            _inspectedEntityId = null;
            return "No non-player entity is visible in the current POV place.";
        }

        var nextIndex = _inspectedEntityId is null ? 0 : (candidates.IndexOf(_inspectedEntityId.Value) + 1) % candidates.Count;
        _inspectedEntityId = candidates[Math.Max(0, nextIndex)];
        return $"Inspecting {World.Entities[_inspectedEntityId.Value].Name}.";
    }

    public void ClearInspection() => _inspectedEntityId = null;

    public string DebugAdvanceOneControlledTurn()
    {
        var result = _history.SubmitControlledCommand(_commands, ControlledActorCommand.Wait());
        _actionLog = ActionLogProjection.FromHistory(_history);
        RefreshDisplayTargets();
        return result.Succeeded
            ? $"Debug wait advanced to frame {FrameIndex}; world turn {World.TurnNumber}."
            : $"Debug wait failed: {result.FailureReason?.ToString() ?? "unknown"}.";
    }

    public string ExecuteControlledMove(Direction direction)
    {
        RefreshActionChoiceRequest();
        var usedCoreChoice = _currentActionChoiceRequest is { } request
            && request.Choices.Any(choice => choice.Kind == ActionChoiceKind.Move);
        var result = usedCoreChoice
            ? _history.SubmitActionChoice(
                _actionChoices,
                _currentActionChoiceRequest!,
                direction,
                _controlledCommandActionPlans,
                (world, entityId) => TargetingService.RefreshTargets(world, _session.Registry, entityId))
            : _history.SubmitControlledCommand(_commands, ControlledActorCommand.Move(direction));
        _actionLog = ActionLogProjection.FromHistory(_history);
        RefreshDisplayTargets();
        RefreshActionChoiceRequest();

        return result.Succeeded
            ? $"Moved {direction} via {(usedCoreChoice ? "Core Action Choice" : "direct compatibility controls")}; frame {FrameIndex}, world turn {World.TurnNumber}."
            : $"Move {direction} failed via {(usedCoreChoice ? "Core Action Choice" : "direct compatibility controls")}: {result.FailureDetail ?? result.FailureReason?.ToString() ?? "unknown"}; frame {FrameIndex}, world turn {World.TurnNumber}.";
    }

    public string SelectPreviousActionStep() => SelectMenuItem(-1);

    public string SelectNextActionStep() => SelectMenuItem(1);

    public string ExecuteSelectedActionStep()
    {
        return _actionMenuMode switch
        {
            ActionMenuMode.Closed => OpenActionStepMenu(),
            ActionMenuMode.ActionList => ConfirmSelectedActionStep(),
            ActionMenuMode.PickupTarget or ActionMenuMode.DropSource => ConfirmSelectedTarget(),
            ActionMenuMode.PickupDestination or ActionMenuMode.DropDestination => ConfirmSelectedDestination(),
            _ => OpenActionStepMenu()
        };
    }

    public string CancelActionMenu()
    {
        if (_actionMenuMode == ActionMenuMode.Closed)
        {
            return "No action menu is open.";
        }

        switch (_actionMenuMode)
        {
            case ActionMenuMode.PickupTarget:
            case ActionMenuMode.DropSource:
                _actionMenuMode = ActionMenuMode.ActionList;
                _selectedEntityActionChoice = null;
                _selectedEntityActionTargetId = null;
                _selectedTargetIndex = 0;
                _selectedDestinationIndex = 0;
                return "Returned to action selector.";
            case ActionMenuMode.PickupDestination:
                _actionMenuMode = ActionMenuMode.PickupTarget;
                _selectedEntityActionTargetId = null;
                _selectedDestinationIndex = 0;
                return "Returned to pickup target selection.";
            case ActionMenuMode.DropDestination:
                _actionMenuMode = ActionMenuMode.DropSource;
                _selectedEntityActionTargetId = null;
                _selectedDestinationIndex = 0;
                _inspectedEntityId = _session.PlayerEntityId;
                return "Returned to inventory item selection.";
            default:
                ResetActionMenu();
                return "Closed action selector.";
        }
    }

    private string OpenActionStepMenu()
    {
        RefreshActionChoiceRequest();
        var steps = AvailablePlayerActionSteps();
        if (steps.Count == 0)
        {
            return "No authored action steps are available for the controlled entity.";
        }

        _selectedActionStepIndex = Math.Clamp(_selectedActionStepIndex, 0, steps.Count - 1);
        _actionMenuMode = ActionMenuMode.ActionList;
        _selectedEntityActionChoice = null;
        _selectedEntityActionTargetId = null;
        _selectedTargetIndex = 0;
        _selectedDestinationIndex = 0;
        return $"Opened action selector 0.2.1. Selected action {_selectedActionStepIndex + 1}/{steps.Count}: {steps[_selectedActionStepIndex].Kind}.";
    }

    private string ConfirmSelectedActionStep()
    {
        var steps = AvailablePlayerActionSteps();
        if (steps.Count == 0)
        {
            ResetActionMenu();
            return "No authored action steps are available for the controlled entity.";
        }

        _selectedActionStepIndex = Math.Clamp(_selectedActionStepIndex, 0, steps.Count - 1);
        var step = steps[_selectedActionStepIndex];
        if (TryFindActionChoiceForStep(step, out var selectedChoice) && selectedChoice.Kind is ActionChoiceKind.Pickup or ActionChoiceKind.Drop)
        {
            var validTargets = ValidTargets(selectedChoice).ToList();
            if (validTargets.Count == 0)
            {
                return $"Selected action {step.Kind}, but Core Action Choice reports no valid targets. {DescribeActionChoice(selectedChoice)}";
            }

            _selectedEntityActionChoice = selectedChoice;
            _selectedTargetIndex = 0;
            _selectedDestinationIndex = 0;
            _selectedEntityActionTargetId = null;
            _actionMenuMode = selectedChoice.Kind == ActionChoiceKind.Pickup ? ActionMenuMode.PickupTarget : ActionMenuMode.DropSource;
            if (selectedChoice.Kind == ActionChoiceKind.Drop)
            {
                _inspectedEntityId = _session.PlayerEntityId;
            }

            var noun = selectedChoice.Kind == ActionChoiceKind.Pickup ? "target" : "inventory item";
            return $"Selected action {step.Kind}. Choose {noun} 1/{validTargets.Count}: {FormatEntityName(validTargets[0].TargetId)}.";
        }

        var plan = new ActionPlanDefinition(
            new ActionPlanId($"play-choice-{step.Kind}"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([step]));
        var result = new ActionPlanInterpreter(_movement).Execute(World, _session.PlayerEntityId, plan, new ActionPlanContext());
        PostActionStateUpdater.ApplyFacingFromMovement(World, _session.PlayerEntityId, result.ActorMovementDirection);
        World.RecordTrace(result.Trace);
        if (result.ConsumesTurn)
        {
            World.AdvanceTurn();
        }

        _history.RecordActorInterval([
            new SimulationHistoryActorLog(
                0,
                _session.PlayerEntityId,
                World.Entities[_session.PlayerEntityId].Name,
                result.Succeeded,
                result.ConsumesTurn,
                result.ContinuePlan,
                step.Kind.ToString(),
                result.Trace)
        ], _session.ActivePlaneId, _session.ActiveContainerEntityId);
        _actionLog = ActionLogProjection.FromHistory(_history);
        RefreshDisplayTargets();
        RefreshActionChoiceRequest();
        ResetActionMenu();

        var status = result.Succeeded ? "succeeded" : "failed";
        return $"Selected action {step.Kind} {status}; frame {FrameIndex}, world turn {World.TurnNumber}.";
    }

    private string ConfirmSelectedTarget()
    {
        if (_selectedEntityActionChoice is not { } choice)
        {
            ResetActionMenu();
            return "No Core Action Choice target list is active.";
        }

        var targets = ValidTargets(choice).ToList();
        if (targets.Count == 0)
        {
            return $"No valid {choice.Kind} targets are available from Core ActionChoiceService.";
        }

        _selectedTargetIndex = Math.Clamp(_selectedTargetIndex, 0, targets.Count - 1);
        var target = targets[_selectedTargetIndex];
        var destinations = ValidDestinations(choice, target.TargetId).ToList();
        if (destinations.Count == 0)
        {
            return $"Selected {FormatEntityName(target.TargetId)}, but Core Action Choice reports no valid destinations.";
        }

        _selectedEntityActionTargetId = target.TargetId;
        _selectedDestinationIndex = 0;
        _actionMenuMode = choice.Kind == ActionChoiceKind.Pickup ? ActionMenuMode.PickupDestination : ActionMenuMode.DropDestination;
        if (choice.Kind == ActionChoiceKind.Pickup)
        {
            _inspectedEntityId = _session.PlayerEntityId;
        }

        var place = choice.Kind == ActionChoiceKind.Pickup ? "inventory location" : "drop destination";
        return $"Selected {FormatEntityName(target.TargetId)}. Choose {place} 1/{destinations.Count}: {FormatDestination(destinations[0].Destination)}.";
    }

    private string ConfirmSelectedDestination()
    {
        if (_selectedEntityActionChoice is not { } choice || _selectedEntityActionTargetId is not { } targetId || _currentActionChoiceRequest is not { } request)
        {
            ResetActionMenu();
            return "No Core Action Choice destination list is active.";
        }

        var destinations = ValidDestinations(choice, targetId).ToList();
        if (destinations.Count == 0)
        {
            return $"No valid {choice.Kind} destinations are available for {FormatEntityName(targetId)} from Core ActionChoiceService.";
        }

        _selectedDestinationIndex = Math.Clamp(_selectedDestinationIndex, 0, destinations.Count - 1);
        var destination = destinations[_selectedDestinationIndex].Destination;
        var result = choice.Kind == ActionChoiceKind.Pickup
            ? _history.SubmitPickupActionChoice(
                _actionChoices,
                request,
                targetId,
                destination,
                _controlledCommandActionPlans,
                (world, entityId) => TargetingService.RefreshTargets(world, _session.Registry, entityId))
            : _history.SubmitDropActionChoice(
                _actionChoices,
                request,
                targetId,
                destination,
                _controlledCommandActionPlans,
                (world, entityId) => TargetingService.RefreshTargets(world, _session.Registry, entityId));

        _actionLog = ActionLogProjection.FromHistory(_history);
        RefreshDisplayTargets();
        RefreshActionChoiceRequest();
        ResetActionMenu();

        var verb = choice.Kind.ToString();
        return result.Succeeded
            ? $"{verb} {FormatEntityName(targetId)} via Core Action Choice; frame {FrameIndex}, world turn {World.TurnNumber}."
            : $"{verb} {FormatEntityName(targetId)} failed via Core Action Choice: {result.FailureDetail ?? result.FailureReason?.ToString() ?? "unknown"}; frame {FrameIndex}, world turn {World.TurnNumber}.";
    }

    private string SelectActionStep(int delta)
    {
        var steps = AvailablePlayerActionSteps();
        if (steps.Count == 0)
        {
            _selectedActionStepIndex = 0;
            return "No authored action steps are available for the controlled entity.";
        }

        _selectedActionStepIndex = (_selectedActionStepIndex + delta + steps.Count) % steps.Count;
        return $"Selected action step {_selectedActionStepIndex + 1}/{steps.Count}: {steps[_selectedActionStepIndex].Kind}.";
    }

    private string SelectMenuItem(int delta)
    {
        return _actionMenuMode switch
        {
            ActionMenuMode.ActionList => SelectActionStep(delta),
            ActionMenuMode.PickupTarget or ActionMenuMode.DropSource => SelectTarget(delta),
            ActionMenuMode.PickupDestination or ActionMenuMode.DropDestination => SelectDestination(delta),
            _ => SelectActionStep(delta)
        };
    }

    private string SelectTarget(int delta)
    {
        if (_selectedEntityActionChoice is not { } choice)
        {
            return "No target list is open.";
        }

        var targets = ValidTargets(choice).ToList();
        if (targets.Count == 0)
        {
            _selectedTargetIndex = 0;
            return $"No valid {choice.Kind} targets are available.";
        }

        _selectedTargetIndex = (_selectedTargetIndex + delta + targets.Count) % targets.Count;
        return $"Selected target {_selectedTargetIndex + 1}/{targets.Count}: {FormatEntityName(targets[_selectedTargetIndex].TargetId)}.";
    }

    private string SelectDestination(int delta)
    {
        if (_selectedEntityActionChoice is not { } choice || _selectedEntityActionTargetId is not { } targetId)
        {
            return "No destination list is open.";
        }

        var destinations = ValidDestinations(choice, targetId).ToList();
        if (destinations.Count == 0)
        {
            _selectedDestinationIndex = 0;
            return $"No valid {choice.Kind} destinations are available for {FormatEntityName(targetId)}.";
        }

        _selectedDestinationIndex = (_selectedDestinationIndex + delta + destinations.Count) % destinations.Count;
        return $"Selected destination {_selectedDestinationIndex + 1}/{destinations.Count}: {FormatDestination(destinations[_selectedDestinationIndex].Destination)}.";
    }

    private IEnumerable<ControlledActorEntityAffordance> ValidTargets(ActionChoice choice) =>
        choice.EntityOptions.Where(option => option.CanExecute);

    private IEnumerable<ControlledActorDestinationAffordance> ValidDestinations(ActionChoice choice, EntityId targetId) =>
        choice.Destinations(targetId).Where(destination => destination.CanExecute);

    private void ResetActionMenu()
    {
        _actionMenuMode = ActionMenuMode.Closed;
        _selectedEntityActionChoice = null;
        _selectedEntityActionTargetId = null;
        _selectedTargetIndex = 0;
        _selectedDestinationIndex = 0;
    }

    private IReadOnlyList<ActionPlanBehaviorStepDescriptor> AvailablePlayerActionSteps() =>
        GetActionPlanDescriptorForEntity(_session.PlayerEntityId)?.Behavior?.Steps ?? [];

    private void RefreshActionChoiceRequest()
    {
        _currentActionChoiceRequest = GetActionPlanDescriptorForEntity(_session.PlayerEntityId) is { } descriptor
            ? _actionChoices.CreateRequest(World, _session.PlayerEntityId, descriptor)
            : null;
    }

    private bool TryFindActionChoiceForStep(ActionPlanBehaviorStepDescriptor step, out ActionChoice choice)
    {
        var kind = step.Kind switch
        {
            ActionPlanBehaviorStepKind.Move => ActionChoiceKind.Move,
            ActionPlanBehaviorStepKind.PickupTarget => ActionChoiceKind.Pickup,
            ActionPlanBehaviorStepKind.TransformAdjacentToInventory => ActionChoiceKind.Pickup,
            ActionPlanBehaviorStepKind.DropFacing => ActionChoiceKind.Drop,
            ActionPlanBehaviorStepKind.TransformInventoryToAdjacent => ActionChoiceKind.Drop,
            _ => (ActionChoiceKind?)null
        };

        if (kind is { } actionChoiceKind && _currentActionChoiceRequest?.Choices.FirstOrDefault(choice => choice.Kind == actionChoiceKind) is { } match)
        {
            choice = match;
            return true;
        }

        choice = null!;
        return false;
    }

    private string DescribeMovementControlMode()
    {
        var moveChoice = _currentActionChoiceRequest?.Choices.FirstOrDefault(choice => choice.Kind == ActionChoiceKind.Move);
        return moveChoice is null
            ? "Move: direct compatibility controls"
            : $"Move: Core Action Choice ({moveChoice.DirectionOptions.Count}-way)";
    }

    private IReadOnlyList<string> BuildActionChoiceRows()
    {
        if (_currentActionChoiceRequest is null)
        {
            return ["Choices: none from Core ActionChoiceService"];
        }

        return _currentActionChoiceRequest.Choices
            .OrderBy(choice => choice.StepIndex)
            .Select(choice => $"Choice: step {choice.StepIndex + 1} {DescribeActionChoice(choice)}")
            .ToList();
    }

    private IUiComponent BuildActionSelectorComponent(SadConsoleRect currentPlaceBounds, IReadOnlyList<ActionPlanBehaviorStepDescriptor> steps, int selectedIndex)
    {
        var component = new SelectableListComponent(
            "0.2.1",
            "0.2.1 Action selector",
            SadConsoleRect.FromSize(currentPlaceBounds.Left + 2, currentPlaceBounds.Top + 2, Math.Min(38, currentPlaceBounds.Width - 4), Math.Min(10, Math.Max(6, steps.Count + 3))),
            steps.Select((step, index) => new SelectableListItem($"step-{index}", step.Kind.ToString(), ActionSelectorDetail(step))),
            UiComponentState.Focused,
            visibleRowCount: 7);
        component.MoveSelection(Math.Clamp(selectedIndex, 0, Math.Max(0, steps.Count - 1)));
        return component;
    }

    private static string ActionSelectorDetail(ActionPlanBehaviorStepDescriptor step) => step.Kind switch
    {
        ActionPlanBehaviorStepKind.PickupTarget or ActionPlanBehaviorStepKind.TransformAdjacentToInventory => "choose target, then inventory location",
        ActionPlanBehaviorStepKind.DropFacing or ActionPlanBehaviorStepKind.TransformInventoryToAdjacent => "choose carried item, then drop destination",
        ActionPlanBehaviorStepKind.Move => "movement also has direct controls",
        _ => "select authored action"
    };

    private IReadOnlySet<GridCoord> CurrentPlaceValidSelectionCoords() => _actionMenuMode switch
    {
        ActionMenuMode.PickupTarget when _selectedEntityActionChoice is { } choice => ValidTargets(choice)
            .Where(target => target.Source?.PlaneId == _session.ActivePlaneId)
            .Select(target => target.Source!.Value.Coord)
            .ToHashSet(),
        ActionMenuMode.DropDestination when _selectedEntityActionChoice is { } choice && _selectedEntityActionTargetId is { } targetId => ValidDestinations(choice, targetId)
            .Where(destination => destination.Destination.PlaneId == _session.ActivePlaneId)
            .Select(destination => destination.Destination.Coord)
            .ToHashSet(),
        _ => new HashSet<GridCoord>()
    };

    private GridCoord? CurrentPlaceSelectedCoord()
    {
        if (_actionMenuMode == ActionMenuMode.PickupTarget && _selectedEntityActionChoice is { } pickupChoice)
        {
            var targets = ValidTargets(pickupChoice).ToList();
            return targets.Count == 0 ? null : targets[Math.Clamp(_selectedTargetIndex, 0, targets.Count - 1)].Source?.Coord;
        }

        if (_actionMenuMode == ActionMenuMode.DropDestination && _selectedEntityActionChoice is { } dropChoice && _selectedEntityActionTargetId is { } targetId)
        {
            var destinations = ValidDestinations(dropChoice, targetId).ToList();
            return destinations.Count == 0 ? null : destinations[Math.Clamp(_selectedDestinationIndex, 0, destinations.Count - 1)].Destination.Coord;
        }

        return null;
    }

    private IReadOnlySet<GridCoord> InspectionValidSelectionCoords()
    {
        if (_session.World.GetInventoryPlaneId(_session.PlayerEntityId) is not { } inventoryPlaneId)
        {
            return new HashSet<GridCoord>();
        }

        return _actionMenuMode switch
        {
            ActionMenuMode.DropSource when _selectedEntityActionChoice is { } choice => ValidTargets(choice)
                .Where(target => target.Source?.PlaneId == inventoryPlaneId)
                .Select(target => target.Source!.Value.Coord)
                .ToHashSet(),
            ActionMenuMode.PickupDestination when _selectedEntityActionChoice is { } choice && _selectedEntityActionTargetId is { } targetId => ValidDestinations(choice, targetId)
                .Where(destination => destination.Destination.PlaneId == inventoryPlaneId)
                .Select(destination => destination.Destination.Coord)
                .ToHashSet(),
            _ => new HashSet<GridCoord>()
        };
    }

    private GridCoord? InspectionSelectedCoord()
    {
        if (_session.World.GetInventoryPlaneId(_session.PlayerEntityId) is not { } inventoryPlaneId)
        {
            return null;
        }

        if (_actionMenuMode == ActionMenuMode.DropSource && _selectedEntityActionChoice is { } dropChoice)
        {
            var targets = ValidTargets(dropChoice).ToList();
            if (targets.Count == 0) return null;
            var source = targets[Math.Clamp(_selectedTargetIndex, 0, targets.Count - 1)].Source;
            return source?.PlaneId == inventoryPlaneId ? source.Value.Coord : null;
        }

        if (_actionMenuMode == ActionMenuMode.PickupDestination && _selectedEntityActionChoice is { } pickupChoice && _selectedEntityActionTargetId is { } targetId)
        {
            var destinations = ValidDestinations(pickupChoice, targetId).ToList();
            if (destinations.Count == 0) return null;
            var destination = destinations[Math.Clamp(_selectedDestinationIndex, 0, destinations.Count - 1)].Destination;
            return destination.PlaneId == inventoryPlaneId ? destination.Coord : null;
        }

        return null;
    }

    private IReadOnlyList<string> BuildActionMenuRows()
    {
        return _actionMenuMode switch
        {
            ActionMenuMode.Closed => ["Menu: Enter opens authored action steps"],
            ActionMenuMode.ActionList => ["Menu: action selector shown in component 0.2.1"],
            ActionMenuMode.PickupTarget or ActionMenuMode.DropSource => BuildTargetListRows(),
            ActionMenuMode.PickupDestination or ActionMenuMode.DropDestination => BuildDestinationListRows(),
            _ => []
        };
    }

    private IReadOnlyList<string> BuildActionListRows()
    {
        var steps = AvailablePlayerActionSteps();
        return ["Menu: choose authored action step", .. steps.Select((step, index) => $"{(index == _selectedActionStepIndex ? ">" : " ")} {index + 1}. {step.Kind}")];
    }

    private IReadOnlyList<string> BuildTargetListRows()
    {
        if (_selectedEntityActionChoice is not { } choice)
        {
            return ["Menu: target list unavailable"];
        }

        var targets = ValidTargets(choice).ToList();
        var label = choice.Kind == ActionChoiceKind.Drop ? "inventory item" : "target";
        return [$"Menu: choose {choice.Kind} {label}", .. targets.Select((target, index) => $"{(index == _selectedTargetIndex ? ">" : " ")} {FormatEntityName(target.TargetId)} from {FormatNullableSource(target.Source)}")];
    }

    private IReadOnlyList<string> BuildDestinationListRows()
    {
        if (_selectedEntityActionChoice is not { } choice || _selectedEntityActionTargetId is not { } targetId)
        {
            return ["Menu: destination list unavailable"];
        }

        var destinations = ValidDestinations(choice, targetId).ToList();
        var label = choice.Kind == ActionChoiceKind.Pickup ? "inventory location" : "drop destination";
        return [$"Menu: choose {choice.Kind} {label} for {FormatEntityName(targetId)}", .. destinations.Select((destination, index) => $"{(index == _selectedDestinationIndex ? ">" : " ")} {FormatDestination(destination.Destination)}")];
    }

    private string DescribeActionChoice(ActionChoice choice)
    {
        return choice.Kind switch
        {
            ActionChoiceKind.Move => $"Move {choice.DirectionOptions.Count(option => option.CanExecute)}/{choice.DirectionOptions.Count} executable directions",
            ActionChoiceKind.Pickup => DescribeEntityDestinationChoice("Pickup", choice),
            ActionChoiceKind.Drop => DescribeEntityDestinationChoice("Drop", choice),
            _ => choice.Kind.ToString()
        };
    }

    private string DescribeEntityDestinationChoice(string label, ActionChoice choice)
    {
        var executableTargets = choice.EntityOptions.Count(option => option.CanExecute);
        var executableDestinations = choice.EntityOptions
            .SelectMany(option => choice.Destinations(option.TargetId))
            .Count(destination => destination.CanExecute);
        var firstExecutableTarget = choice.EntityOptions.FirstOrDefault(option => option.CanExecute)?.TargetId;
        var targetLabel = firstExecutableTarget is { } targetId
            ? $"; first {FormatEntityName(targetId)}"
            : string.Empty;
        return $"{label} {executableTargets}/{choice.EntityOptions.Count} targets, {executableDestinations} executable destinations{targetLabel}";
    }

    private void RefreshDisplayTargets()
    {
        foreach (var entityId in _session.ActionPlans.Keys)
        {
            TargetingService.RefreshTargets(World, _session.Registry, entityId);
        }
    }

    private ActionPlanDescriptor? GetActionPlanDescriptorForEntity(EntityId entityId)
    {
        if (!_session.Registry.TryGetTemplateIdForEntity(entityId, out var templateId))
        {
            return null;
        }

        var template = _session.Registry.GetEntityTemplate(templateId);
        return template.DefaultActionPlanId is { } planId
            && _session.Registry.ActionPlanDescriptors.TryGetValue(planId, out var descriptor)
                ? descriptor
                : null;
    }

    private IReadOnlyList<string> BuildCurrentPlaceEntityRows(
        EntityPanelProjection? currentPlaceProjection,
        IReadOnlyList<EntityPointOfViewTargetAdjectiveProjection> targetAdjectives,
        IReadOnlyList<EntityPointOfViewTargetAdjectiveProjection> reciprocalAdjectives)
    {
        var entityIds = currentPlaceProjection?.InventoryGrid?.Cells
            .Select(cell => cell.EntityId)
            .Where(entityId => entityId is not null)
            .Select(entityId => entityId!.Value)
            .Distinct()
            .OrderBy(entityId => World.GetEntityLocation(entityId).Coord.Y)
            .ThenBy(entityId => World.GetEntityLocation(entityId).Coord.X)
            .ThenBy(entityId => entityId.Value, StringComparer.Ordinal)
            .ToList() ?? [];

        var adjectivesByEntity = GroupAdjectivesByEntity(targetAdjectives);
        var reciprocalAdjectivesByEntity = GroupAdjectivesByEntity(reciprocalAdjectives);

        return entityIds.Select(entityId => BuildDisplayedEntityRow(entityId, adjectivesByEntity, reciprocalAdjectivesByEntity)).ToList();
    }

    private static IReadOnlyDictionary<EntityId, IReadOnlyList<string>> GroupAdjectivesByEntity(
        IReadOnlyList<EntityPointOfViewTargetAdjectiveProjection> adjectives) =>
        adjectives
            .GroupBy(adjective => adjective.EntityId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(adjective => adjective.Adjective)
                    .Where(adjective => !string.IsNullOrWhiteSpace(adjective))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList());

    private string BuildDisplayedEntityRow(
        EntityId entityId,
        IReadOnlyDictionary<EntityId, IReadOnlyList<string>> adjectivesByEntity,
        IReadOnlyDictionary<EntityId, IReadOnlyList<string>> reciprocalAdjectivesByEntity)
    {
        var entity = World.Entities[entityId];
        var facing = World.GetActionFacing(entityId)?.ToString() ?? "none";
        var target = FormatCurrentTarget(entityId);
        var adjectives = adjectivesByEntity.TryGetValue(entityId, out var labels) && labels.Count > 0
            ? $"; adjectives {string.Join(", ", labels)}"
            : string.Empty;
        var reciprocalAdjectives = reciprocalAdjectivesByEntity.TryGetValue(entityId, out var reciprocalLabels) && reciprocalLabels.Count > 0
            ? $"; reciprocal {string.Join(", ", reciprocalLabels)}"
            : string.Empty;
        return $"{entity.Name}: facing {facing}; target {target}{adjectives}{reciprocalAdjectives}";
    }

    private string FormatCurrentTarget(EntityId entityId)
    {
        if (!World.ActionStates.TryGetValue(entityId, out var state))
        {
            return "none";
        }

        var labeledTarget = state.LabeledTargets
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(labeledTarget.Key))
        {
            return $"{labeledTarget.Key} -> {FormatEntityName(labeledTarget.Value)}";
        }

        if (state.Target is { } target)
        {
            return $"target -> {FormatEntityName(target)}";
        }

        var slottedTarget = state.Targets.OrderBy(entry => entry.Key).FirstOrDefault();
        return slottedTarget.Value.Value is null
            ? "none"
            : $"slot {slottedTarget.Key} -> {FormatEntityName(slottedTarget.Value)}";
    }

    private IReadOnlyList<string> BuildTargetingRuleRows(EntityId entityId)
    {
        if (!_session.Registry.TryGetTemplateIdForEntity(entityId, out var templateId))
        {
            return ["targeting rules: template unavailable"];
        }

        var rules = _session.Registry.GetEntityTemplate(templateId).TargetingRules ?? [];
        if (rules.Count == 0)
        {
            return ["targeting rules: none"];
        }

        return rules.Select(rule =>
        {
            var label = string.IsNullOrWhiteSpace(rule.Label) ? $"slot {rule.Slot}" : rule.Label;
            var target = World.GetActionTarget(entityId, rule.Slot) is { } targetId
                ? FormatEntityName(targetId)
                : "none";
            var template = rule.TargetTemplateId?.Value ?? "any";
            var capabilities = rule.TargetCapabilities.Count == 0 ? "" : $" [{string.Join(",", rule.TargetCapabilities)}]";
            return $"rule {label}: {template} r{rule.Range}{capabilities} -> {target}";
        }).ToList();
    }

    private IReadOnlyList<string> BuildActionPlanRows(EntityId entityId)
    {
        if (!_session.Registry.TryGetTemplateIdForEntity(entityId, out var templateId))
        {
            return ["action plan: template unavailable"];
        }

        var template = _session.Registry.GetEntityTemplate(templateId);
        if (template.DefaultActionPlanId is not { } planId || !_session.Registry.ActionPlanDescriptors.TryGetValue(planId, out var plan))
        {
            return ["action plan: none"];
        }

        var rows = new List<string> { $"action plan: {planId}" };
        if (plan.Behavior?.Steps.Count > 0)
        {
            rows.AddRange(plan.Behavior.Steps.Select((step, index) => $"{index + 1}. {FormatBehaviorStep(step)}"));
        }
        else if (plan.Steps.Count > 0)
        {
            rows.AddRange(plan.Steps.Select((step, index) => $"{index + 1}. {step.Label}"));
        }
        else if (plan.Primitive is { } primitive)
        {
            rows.Add($"1. {primitive.Kind}");
        }
        else
        {
            rows.Add("steps: none");
        }

        return rows;
    }

    private string FormatEntityName(EntityId entityId) =>
        World.Entities.TryGetValue(entityId, out var entity) ? entity.Name : entityId.Value;

    private static string FormatNullableSource(PlaneCoord? source) => source is { } value ? FormatDestination(value) : "unknown";

    private static string FormatDestination(PlaneCoord destination) => $"{destination.PlaneId.Value}@({destination.Coord.X},{destination.Coord.Y})";

    private static string FormatBehaviorStep(ActionPlanBehaviorStepDescriptor step)
    {
        var target = step.TargetLabel is { Length: > 0 }
            ? $" {step.TargetLabel}"
            : step.TargetSlot is { } slot
                ? $" slot {slot}"
                : string.Empty;
        var plan = step.PlanId is { } planId ? $" -> {planId}" : string.Empty;
        return $"{step.Kind}{target}{plan}";
    }

    private static IUiComponent BuildCurrentPlaceComponent(EntityPanelProjection? currentPlaceProjection, SadConsoleRect bounds)
    {
        if (currentPlaceProjection?.InventoryGrid is not { } grid)
        {
            return new PanelComponent(
                "0.2",
                "0.2 Current place",
                bounds,
                ["Player POV did not resolve to a usable current-place inventory."],
                UiComponentState.Error);
        }

        return new InventoryGridComponent(
            "0.2",
            $"0.2 Current place: {currentPlaceProjection.Name}",
            bounds,
            [$"plane: {grid.PlaneId}", $"inventory: {grid.Width}x{grid.Height}", "centered from player POV"],
            UiComponentState.Selected,
            grid.Width,
            grid.Height,
            new GridCoord(0, 0),
            grid.Cells.Select(cell => new InventoryGridCell(cell.Coord, cell.Glyph, cell.Color)).ToList());
    }

    private static IUiComponent BuildInspectionComponent(EntityPanelProjection projection, SadConsoleRect bounds)
    {
        var rows = new List<string>
        {
            $"{projection.Glyph} {projection.Name}",
            $"id: {projection.EntityId}",
            $"path: {FormatBreadcrumbFromCurrentPlace(projection)}"
        };
        rows.AddRange(projection.Properties.Take(4).Select(property => $"{property.Name}: {property.Value}"));
        if (projection.InventoryGrid is { } grid)
        {
            rows.Add($"inventory: {grid.Width}x{grid.Height} {grid.PlaneId}");
        }

        return new PanelComponent("0.3", "0.3 Inspection panel", bounds, rows, UiComponentState.Focused);
    }

    private static IReadOnlyList<string> BuildHudRows(
        EntityPanelProjection playerProjection,
        EntityPanelProjection? currentPlaceProjection,
        IReadOnlyList<ActionPlanBehaviorStepDescriptor> actionSteps,
        int selectedActionStepIndex,
        string movementControlMode,
        IReadOnlyList<string> actionChoiceRows,
        IReadOnlyList<string> actionMenuRows)
    {
        var rows = new List<string>
        {
            "Component 0.1 HUD",
            $"Player: {playerProjection.Glyph} {playerProjection.Name} ({playerProjection.EntityId})",
            $"Facing: {playerProjection.ActionState.Facing?.ToString() ?? "none"} | Target: {playerProjection.ActionState.Target?.ToString() ?? "none"}",
            $"Current place: {currentPlaceProjection?.Name ?? "none"}"
        };

        if (playerProjection.PointOfView?.CurrentPlace is { } place)
        {
            rows.Add($"Bulk/aperture: {place.ObserverBulk}/{place.PlaceAperture} ratio {place.BulkToApertureRatio?.ToString() ?? "n/a"}");
            rows.Add($"POV rule: {place.SelectionRule}");
        }
        else
        {
            rows.Add("POV rule: unresolved");
        }

        if (actionSteps.Count == 0)
        {
            rows.Add("Actions: none authored");
        }
        else
        {
            var clampedIndex = Math.Clamp(selectedActionStepIndex, 0, actionSteps.Count - 1);
            rows.Add($"Action: {clampedIndex + 1}/{actionSteps.Count} {actionSteps[clampedIndex].Kind}");
        }

        rows.Add(movementControlMode);
        rows.AddRange(actionMenuRows.Take(6));
        rows.AddRange(actionChoiceRows.Take(4));
        rows.Add("Controls: arrows/Home/PgUp/PgDn/End/numpad move 8-way | Enter menu/confirm | Up/Down pick while menu open | Space wait | I inspect | Esc");
        return rows;
    }

    private static IReadOnlyList<string> BuildCurrentPlacePlayerLogRows(EntityPanelProjection? currentPlaceProjection)
    {
        if (currentPlaceProjection is null)
        {
            return ["player-log: current place unavailable"];
        }

        if (currentPlaceProjection.LocalLog.Count == 0)
        {
            return ["player-log: no local player-facing ids yet"];
        }

        return currentPlaceProjection.LocalLog
            .SelectMany(ProjectPlayerFacingIds)
            .Take(5)
            .ToList();
    }

    private static IReadOnlyList<string> ProjectPlayerFacingIds(ActionOutcome outcome)
    {
        if (outcome.ActionStepAttempts.Count == 0)
        {
            return [FormatPlayerFacingLogRow(outcome, outcome.ActionKind)];
        }

        return outcome.ActionStepAttempts
            .Select(attempt => FormatPlayerFacingLogRow(outcome, attempt.StepKind))
            .ToList();
    }

    private static string FormatPlayerFacingLogRow(ActionOutcome outcome, string? actionKind)
    {
        var result = outcome.Succeeded ? "success" : "failure";
        var messageId = $"action.{ToSnakeCase(string.IsNullOrWhiteSpace(actionKind) ? "turn" : actionKind!)}.{result}";
        var fields = new List<string>
        {
            $"actor={outcome.ActorName}",
            $"result={result}"
        };

        if (outcome.Direction is { } direction)
        {
            fields.Add($"direction={direction}");
        }

        var reason = outcome.FailureDetail ?? outcome.FailureReason?.ToString();
        if (!string.IsNullOrWhiteSpace(reason))
        {
            fields.Add($"reason={reason}");
        }

        fields.Add($"consumedTurn={outcome.ConsumedTurn}");
        return $"player-log: {messageId} {string.Join(" ", fields)}";
    }

    private static string ToSnakeCase(string value)
    {
        var chars = new List<char>(value.Length + 4);
        for (var index = 0; index < value.Length; index++)
        {
            var c = value[index];
            if (char.IsUpper(c) && index > 0 && value[index - 1] != '_')
            {
                chars.Add('_');
            }

            chars.Add(char.IsWhiteSpace(c) || c == '-' ? '_' : char.ToLowerInvariant(c));
        }

        return new string(chars.ToArray());
    }

    private static string DescribeCurrentRoomSize(EntityPointOfViewCurrentPlaceProjection? currentPlace)
    {
        if (currentPlace?.BulkToApertureRatio is not { } ratio)
        {
            return "Unknown";
        }

        if (ratio >= 0.9m)
        {
            return "Small";
        }

        return ratio < 0.1m ? "Large" : "Medium";
    }

    private static string FormatBreadcrumbFromCurrentPlace(EntityPanelProjection projection) =>
        string.Join(" > ", projection.Breadcrumb.Segments.Select(segment => segment.EntityId.Value));
}
