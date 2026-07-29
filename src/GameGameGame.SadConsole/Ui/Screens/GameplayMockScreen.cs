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
    IReadOnlyList<string> Diagnostics,
    IReadOnlyList<GameplayMockRegion> Regions);

internal sealed class GameplayMockScreen
{
    private readonly PlayableScenarioSession _session;
    private readonly GameplaySessionController _sessionController;
    private readonly ActionChoicePromptController _prompt = new();
    private readonly EntityPanelProjectionService _panelProjection;
    private EntityId? _inspectedEntityId;
    private bool _layoutDebugVisible;
    private (int X, int Y)? _layoutDebugMouseCell;
    private GameplayMockManualLayoutRecalculation? _manualLayoutRecalculation;

    public GameplayMockScreen(PlayableScenarioSession session)
    {
        _session = session;
        _sessionController = new GameplaySessionController(session);
        _panelProjection = new EntityPanelProjectionService(
            ResolveInspectionAppearance,
            _sessionController.GetActionPlanDescriptorForEntity);
    }

    public EntityId PlayerEntityId => _sessionController.PlayerEntityId;
    public EntityId? InspectedEntityId => _inspectedEntityId;
    public int FrameIndex => _sessionController.FrameIndex;
    public int SelectedActionStepIndex => _prompt.SelectedActionStepIndex;
    public ActionChoiceRequest? CurrentActionChoiceRequest => _sessionController.CurrentActionChoiceRequest;
    public string ActionMenuState => _prompt.Mode.ToString();
    public bool IsActionMenuOpen => _prompt.IsOpen;
    public bool UsesCoreActionChoiceMovement => CurrentActionChoiceRequest?.Choices.Any(choice => choice.Kind == ActionChoiceKind.Move) == true;
    public bool UsesCoreActionChoicePickup => CurrentActionChoiceRequest?.Choices.Any(choice => choice.Kind == ActionChoiceKind.Pickup) == true;
    public bool UsesCoreActionChoiceDrop => CurrentActionChoiceRequest?.Choices.Any(choice => choice.Kind == ActionChoiceKind.Drop) == true;
    public bool UsesCoreActionChoiceEnter => CurrentActionChoiceRequest?.Choices.Any(choice => choice.Kind == ActionChoiceKind.Enter) == true;
    public bool UsesCoreActionChoiceExit => CurrentActionChoiceRequest?.Choices.Any(choice => choice.Kind == ActionChoiceKind.Exit) == true;
    public bool IsLayoutDebugVisible => _layoutDebugVisible;
    private WorldState World => _session.World;
    private IReadOnlyDictionary<EntityId, IEntityActionPlan> ProjectionActionPlans => _sessionController.ProjectionActionPlans;

    private EntityInspectionAppearance ResolveInspectionAppearance(EntityId entityId)
    {
        if (_session.Registry.TryGetTemplateIdForEntity(World, entityId, out var templateId)
            && _session.Registry.Presentations.TryGetValue(templateId, out var presentation))
        {
            return presentation.ToInspectionAppearance();
        }

        return new EntityInspectionAppearance('?', PresentationColor.Gray);
    }

    public GameplayMockFrame BuildFrame(int width, int height)
    {
        var layout = GameplayMockLayout.Resolve(width, height);
        _sessionController.RefreshForFrameBuilding();
        var projectionActionPlans = ProjectionActionPlans;
        var playerProjection = _panelProjection.Project(World, PlayerEntityId, projectionActionPlans, PlayerEntityId, _sessionController.ActionLog);
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
            ? _panelProjection.Project(World, currentPlace.EntityId, projectionActionPlans, PlayerEntityId, _sessionController.ActionLog)
            : null;

        var hudBounds = layout.HudBounds;
        var currentPlaceBounds = layout.CurrentPlaceBounds;
        var inspectionBounds = layout.InspectionBounds;

        var components = new List<IUiComponent>();
        components.Add(BuildCurrentPlaceComponent(currentPlaceProjection, currentPlaceBounds));
        EntityPanelProjection? inspectedProjection = null;
        if (_inspectedEntityId is { } inspectedEntityId && World.Entities.ContainsKey(inspectedEntityId))
        {
            inspectedProjection = _panelProjection.Project(World, inspectedEntityId, projectionActionPlans, PlayerEntityId, _sessionController.ActionLog);
            components.Add(BuildInspectionComponent(inspectedProjection, inspectionBounds));
        }

        if (_prompt.Mode == ActionChoicePromptMode.ActionList)
        {
            var actionSteps = AvailablePlayerActionSteps();
            components.Add(BuildActionSelectorComponent(GameplayMockLayout.ResolveActionSelectorBounds(layout, actionSteps.Count), actionSteps, _prompt.SelectedActionStepIndex));
        }

        if (_prompt.Mode == ActionChoicePromptMode.TransferItem
            && _prompt.SelectedTransferItem() is { } selectedTransferItem
            && World.Entities.ContainsKey(selectedTransferItem.CounterpartyId))
        {
            var counterpartyProjection = _panelProjection.Project(World, selectedTransferItem.CounterpartyId, projectionActionPlans, PlayerEntityId, _sessionController.ActionLog);
            components.Add(BuildTransferInventoryComparisonComponent(playerProjection, counterpartyProjection, inspectionBounds, _prompt.ValidSelectedTransferItems(), selectedTransferItem));
        }

        if (diagnostics.Count > 0)
        {
            components.Add(new PanelComponent(
                "play-mock-diagnostics",
                "POV / setup diagnostics",
                layout.DiagnosticsBounds,
                diagnostics.Take(4).ToList(),
                UiComponentState.Error));
        }

        if (_layoutDebugVisible)
        {
            components.Add(new PanelComponent(
                "0.layout-debug",
                "Layout regions (F12)",
                layout.DiagnosticsBounds,
                BuildLayoutDebugRows(layout),
                UiComponentState.Focused));
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
            BuildHudRows(playerProjection, currentPlaceProjection, AvailablePlayerActionSteps(), _prompt.SelectedActionStepIndex, DescribeMovementControlMode(), BuildActionChoiceRows(), BuildActionMenuRows()),
            BuildActionChoiceRows(),
            BuildCurrentPlacePlayerLogRows(currentPlaceProjection),
            CurrentPlaceValidSelectionCoords(),
            CurrentPlaceSelectedCoord(),
            InspectionValidSelectionCoords(),
            InspectionSelectedCoord(),
            diagnostics,
            layout.Regions);
    }

    public string InspectNextEntity()
    {
        var frame = BuildFrame(SadConsoleScreenMetrics.ScreenWidth, SadConsoleScreenMetrics.ScreenHeight);
        var candidates = frame.CurrentPlaceProjection?.InventoryGrid?.Cells
            .Select(cell => cell.EntityId)
            .Where(entityId => entityId is not null && entityId != PlayerEntityId)
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

    public string ToggleLayoutDebug()
    {
        _layoutDebugVisible = !_layoutDebugVisible;
        return _layoutDebugVisible ? "Layout debug visible. F12 hides region overlay." : "Layout debug hidden.";
    }

    public string RecalculateLayout(int width, int height)
    {
        var layout = GameplayMockLayout.Resolve(width, height);
        _manualLayoutRecalculation = new GameplayMockManualLayoutRecalculation(layout.Width, layout.Height, layout.Regions.Count);
        return $"Recalculated layout from logical console {width}x{height}; resolved {layout.Width}x{layout.Height} with {layout.Regions.Count} regions. Window pixel resize does not change cells yet.";
    }

    public bool SetLayoutDebugMouseCell(int x, int y)
    {
        var next = (x, y);
        if (_layoutDebugMouseCell == next)
        {
            return false;
        }

        _layoutDebugMouseCell = next;
        return true;
    }

    public string DebugAdvanceOneControlledTurn()
    {
        var result = _sessionController.SubmitWait();
        return result.Succeeded
            ? $"Debug wait advanced to frame {FrameIndex}; world turn {World.TurnNumber}."
            : $"Debug wait failed: {result.FailureText ?? "unknown"}.";
    }

    public string ExecuteControlledMove(Direction direction)
    {
        var result = _sessionController.SubmitMove(direction);

        return result.Succeeded
            ? $"Moved {direction} via {(result.UsedCoreActionChoice ? "Core Action Choice" : "direct compatibility controls")}; frame {FrameIndex}, world turn {World.TurnNumber}."
            : $"Move {direction} failed via {(result.UsedCoreActionChoice ? "Core Action Choice" : "direct compatibility controls")}: {result.FailureText ?? "unknown"}; frame {FrameIndex}, world turn {World.TurnNumber}.";
    }

    public string SelectPreviousActionStep() => SelectMenuItem(-1);

    public string SelectNextActionStep() => SelectMenuItem(1);

    public string ExecuteSelectedActionStep()
    {
        return _prompt.Mode switch
        {
            ActionChoicePromptMode.Closed => OpenActionStepMenu(),
            ActionChoicePromptMode.ActionList => ConfirmSelectedActionStep(),
            ActionChoicePromptMode.PickupTarget or ActionChoicePromptMode.DropSource or ActionChoicePromptMode.EnterTarget => ConfirmSelectedTarget(),
            ActionChoicePromptMode.TransferCounterparty => ConfirmSelectedTarget(),
            ActionChoicePromptMode.TransferItem => ConfirmSelectedTransferItem(),
            ActionChoicePromptMode.PickupDestination or ActionChoicePromptMode.DropDestination => ConfirmSelectedDestination(),
            ActionChoicePromptMode.ExitFacing => ConfirmSelectedDirection(),
            _ => OpenActionStepMenu()
        };
    }

    public string CancelActionMenu()
    {
        var result = _prompt.Cancel();
        if (result.InspectPlayer)
        {
            _inspectedEntityId = PlayerEntityId;
        }

        UpdateTransferInspectionFocus();

        return result.Message;
    }

    private string OpenActionStepMenu()
    {
        _sessionController.RefreshForFrameBuilding();
        return _prompt.OpenActionStepMenu(AvailablePlayerActionSteps());
    }

    private string ConfirmSelectedActionStep()
    {
        var promptResult = _prompt.ConfirmSelectedActionStep(AvailablePlayerActionSteps(), CurrentActionChoiceRequest, FormatEntityName);
        if (promptResult.Kind == ActionChoicePromptActionResultKind.Message)
        {
            return promptResult.Message;
        }

        if (promptResult.Kind == ActionChoicePromptActionResultKind.ChoosingTarget)
        {
            if (promptResult.InspectPlayer)
            {
                _inspectedEntityId = PlayerEntityId;
            }

            UpdateTransferInspectionFocus();

            return promptResult.Message;
        }

        var step = promptResult.Step!;
        var result = _sessionController.SubmitAuthoredActionStepChoice(promptResult.StepIndex, step);
        _prompt.Reset();

        var status = result.Succeeded ? "succeeded" : "failed";
        return $"Selected action {step.Kind} {status}; frame {FrameIndex}, world turn {World.TurnNumber}.";
    }

    private string ConfirmSelectedTarget()
    {
        var result = _prompt.ConfirmSelectedTarget(FormatEntityName, FormatDestination);
        if (result.Kind == ActionChoicePromptTargetResultKind.SubmitEnter)
        {
            var targetId = result.TargetId!.Value;
            var submission = _sessionController.SubmitEnterActionChoice(targetId);
            _prompt.Reset();
            return submission.Succeeded
                ? $"Enter {FormatEntityName(targetId)} via Core Action Choice; frame {FrameIndex}, world turn {World.TurnNumber}."
                : $"Enter {FormatEntityName(targetId)} failed via Core Action Choice: {submission.FailureText ?? "unknown"}; frame {FrameIndex}, world turn {World.TurnNumber}.";
        }

        if (result.InspectPlayer)
        {
            _inspectedEntityId = PlayerEntityId;
        }

        UpdateTransferInspectionFocus();

        return result.Message;
    }

    private string ConfirmSelectedDirection()
    {
        var submit = _prompt.ConfirmSelectedDirection();
        if (submit.Kind == ActionChoicePromptDirectionResultKind.Message)
        {
            return submit.Message;
        }

        var direction = submit.Direction!.Value;
        var result = _sessionController.SubmitExitActionChoice(direction);
        _prompt.Reset();
        return result.Succeeded
            ? $"Exit {direction} via Core Action Choice; frame {FrameIndex}, world turn {World.TurnNumber}."
            : $"Exit {direction} failed via Core Action Choice: {result.FailureText ?? "unknown"}; frame {FrameIndex}, world turn {World.TurnNumber}.";
    }

    private string ConfirmSelectedTransferItem()
    {
        var submit = _prompt.ConfirmSelectedTransferItem();
        if (submit.Kind == ActionChoicePromptTransferItemResultKind.Message)
        {
            return submit.Message;
        }

        var counterpartyId = submit.CounterpartyId!.Value;
        var movingEntityId = submit.MovingEntityId!.Value;
        var result = _sessionController.SubmitTransferActionChoice(counterpartyId, movingEntityId);
        _prompt.Reset();
        return result.Succeeded
            ? $"Transfer {FormatEntityName(movingEntityId)} with {FormatEntityName(counterpartyId)} via Core Action Choice; frame {FrameIndex}, world turn {World.TurnNumber}."
            : $"Transfer {FormatEntityName(movingEntityId)} with {FormatEntityName(counterpartyId)} failed via Core Action Choice: {result.FailureText ?? "unknown"}; frame {FrameIndex}, world turn {World.TurnNumber}.";
    }

    private string ConfirmSelectedDestination()
    {
        if (CurrentActionChoiceRequest is null)
        {
            _prompt.Reset();
            return "No Core Action Choice destination list is active.";
        }

        var submit = _prompt.ConfirmSelectedDestination();
        if (submit.Kind == ActionChoicePromptDestinationResultKind.Message)
        {
            return submit.Message;
        }

        var choice = submit.Choice!;
        var targetId = submit.TargetId!.Value;
        var destination = submit.Destination!.Value;
        var result = choice.Kind == ActionChoiceKind.Pickup
            ? _sessionController.SubmitPickupActionChoice(targetId, destination)
            : _sessionController.SubmitDropActionChoice(targetId, destination);
        _prompt.Reset();

        var verb = choice.Kind.ToString();
        return result.Succeeded
            ? $"{verb} {FormatEntityName(targetId)} via Core Action Choice; frame {FrameIndex}, world turn {World.TurnNumber}."
            : $"{verb} {FormatEntityName(targetId)} failed via Core Action Choice: {result.FailureText ?? "unknown"}; frame {FrameIndex}, world turn {World.TurnNumber}.";
    }

    private string SelectMenuItem(int delta)
    {
        var message = _prompt.SelectMenuItem(delta, AvailablePlayerActionSteps(), FormatEntityName, FormatDestination);
        UpdateTransferInspectionFocus();
        return message;
    }

    private void UpdateTransferInspectionFocus()
    {
        if (_prompt.Mode == ActionChoicePromptMode.TransferItem && _prompt.SelectedTransferItem() is { } item)
        {
            _inspectedEntityId = item.OwnerEntityId;
            return;
        }

        if (_prompt.Mode == ActionChoicePromptMode.TransferCounterparty && _prompt.SelectedTransferCounterparty() is { } counterparty)
        {
            _inspectedEntityId = counterparty.CounterpartyId;
        }
    }

    private IReadOnlyList<ActionPlanBehaviorStepDescriptor> AvailablePlayerActionSteps() =>
        _sessionController.AvailablePlayerActionSteps();

    private string DescribeMovementControlMode()
    {
        var moveChoice = CurrentActionChoiceRequest?.Choices.FirstOrDefault(choice => choice.Kind == ActionChoiceKind.Move);
        return moveChoice is null
            ? "Move: direct compatibility controls"
            : $"Move: Core Action Choice ({moveChoice.DirectionOptions.Count}-way)";
    }

    private IReadOnlyList<string> BuildActionChoiceRows()
    {
        if (CurrentActionChoiceRequest is null)
        {
            return ["Choices: none from Core ActionChoiceService"];
        }

        return CurrentActionChoiceRequest.Choices
            .OrderBy(choice => choice.StepIndex)
            .Select(choice => $"Choice: step {choice.StepIndex + 1} {DescribeActionChoice(choice)}")
            .ToList();
    }

    private IUiComponent BuildActionSelectorComponent(SadConsoleRect bounds, IReadOnlyList<ActionPlanBehaviorStepDescriptor> steps, int selectedIndex)
    {
        var component = new SelectableListComponent(
            "0.2.1",
            "0.2.1 Action selector",
            bounds,
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
        ActionPlanBehaviorStepKind.EnterTarget => "choose enter target",
        ActionPlanBehaviorStepKind.ExitFacing => "choose exit direction",
        ActionPlanBehaviorStepKind.Transfer => "choose entity, then item from either inventory",
        ActionPlanBehaviorStepKind.Move => "movement also has direct controls",
        _ => "select authored action"
    };

    private IReadOnlyList<string> BuildLayoutDebugRows(GameplayMockLayoutFrame layout)
    {
        var rows = new List<string>();
        if (_layoutDebugMouseCell is { } cell)
        {
            rows.Add(GameplayMockLayout.HitTest(layout, cell.X, cell.Y)?.Format() ?? $"hit: none at {cell.X},{cell.Y}");
        }
        else
        {
            rows.Add("hit: move mouse over play surface");
        }

        rows.Add(_manualLayoutRecalculation is { } recalculation
            ? $"manual: logical {recalculation.Width}x{recalculation.Height} regions {recalculation.RegionCount}; window pixels do not change cells yet"
            : "manual: F11 recalculates current logical console layout");

        rows.AddRange(layout.Regions
            .OrderBy(region => region.Id, StringComparer.Ordinal)
            .Select(region => $"{region.Id} {region.Title} L{region.Bounds.Left} T{region.Bounds.Top} W{region.Bounds.Width} H{region.Bounds.Height} Z{region.Layer}"));
        return rows;
    }

    private IReadOnlySet<GridCoord> CurrentPlaceValidSelectionCoords() => _prompt.Mode switch
    {
        ActionChoicePromptMode.PickupTarget or ActionChoicePromptMode.EnterTarget => _prompt.ValidSelectedTargets()
            .Where(target => target.Source?.PlaneId == _session.ActivePlaneId)
            .Select(target => target.Source!.Value.Coord)
            .ToHashSet(),
        ActionChoicePromptMode.TransferCounterparty => _prompt.SelectedEntityActionChoice?.TransferCounterparties
            .Where(counterparty => counterparty.CanExecute && counterparty.Source.PlaneId == _session.ActivePlaneId)
            .Select(counterparty => counterparty.Source.Coord)
            .ToHashSet() ?? new HashSet<GridCoord>(),
        ActionChoicePromptMode.DropDestination => _prompt.ValidSelectedDestinations()
            .Where(destination => destination.Destination.PlaneId == _session.ActivePlaneId)
            .Select(destination => destination.Destination.Coord)
            .ToHashSet(),
        _ => new HashSet<GridCoord>()
    };

    private GridCoord? CurrentPlaceSelectedCoord()
    {
        if (_prompt.Mode is ActionChoicePromptMode.PickupTarget or ActionChoicePromptMode.EnterTarget)
        {
            var targets = _prompt.ValidSelectedTargets();
            return targets.Count == 0 ? null : targets[Math.Clamp(_prompt.SelectedTargetIndex, 0, targets.Count - 1)].Source?.Coord;
        }

        if (_prompt.Mode == ActionChoicePromptMode.TransferCounterparty && _prompt.SelectedEntityActionChoice is { } transferChoice)
        {
            var counterparties = transferChoice.TransferCounterparties.Where(counterparty => counterparty.CanExecute).ToList();
            return counterparties.Count == 0 ? null : counterparties[Math.Clamp(_prompt.SelectedTargetIndex, 0, counterparties.Count - 1)].Source.Coord;
        }

        if (_prompt.Mode == ActionChoicePromptMode.DropDestination)
        {
            var destinations = _prompt.ValidSelectedDestinations();
            return destinations.Count == 0 ? null : destinations[Math.Clamp(_prompt.SelectedDestinationIndex, 0, destinations.Count - 1)].Destination.Coord;
        }

        return null;
    }

    private IReadOnlySet<GridCoord> InspectionValidSelectionCoords()
    {
        if (_session.World.GetInventoryPlaneId(PlayerEntityId) is not { } inventoryPlaneId)
        {
            return new HashSet<GridCoord>();
        }

        return _prompt.Mode switch
        {
            ActionChoicePromptMode.DropSource => _prompt.ValidSelectedTargets()
                .Where(target => target.Source?.PlaneId == inventoryPlaneId)
                .Select(target => target.Source!.Value.Coord)
                .ToHashSet(),
            ActionChoicePromptMode.PickupDestination => _prompt.ValidSelectedDestinations()
                .Where(destination => destination.Destination.PlaneId == inventoryPlaneId)
                .Select(destination => destination.Destination.Coord)
                .ToHashSet(),
            ActionChoicePromptMode.TransferItem => _prompt.ValidSelectedTransferItems()
                .Where(item => item.OwnerEntityId == _inspectedEntityId && item.Source.PlaneId == inventoryPlaneId)
                .Select(item => item.Source.Coord)
                .ToHashSet(),
            _ => new HashSet<GridCoord>()
        };
    }

    private GridCoord? InspectionSelectedCoord()
    {
        if (_session.World.GetInventoryPlaneId(PlayerEntityId) is not { } inventoryPlaneId)
        {
            return null;
        }

        if (_prompt.Mode == ActionChoicePromptMode.DropSource)
        {
            var targets = _prompt.ValidSelectedTargets();
            if (targets.Count == 0) return null;
            var source = targets[Math.Clamp(_prompt.SelectedTargetIndex, 0, targets.Count - 1)].Source;
            return source?.PlaneId == inventoryPlaneId ? source.Value.Coord : null;
        }

        if (_prompt.Mode == ActionChoicePromptMode.PickupDestination)
        {
            var destinations = _prompt.ValidSelectedDestinations();
            if (destinations.Count == 0) return null;
            var destination = destinations[Math.Clamp(_prompt.SelectedDestinationIndex, 0, destinations.Count - 1)].Destination;
            return destination.PlaneId == inventoryPlaneId ? destination.Coord : null;
        }

        if (_prompt.Mode == ActionChoicePromptMode.TransferItem && _prompt.SelectedTransferItem() is { } item)
        {
            return item.OwnerEntityId == _inspectedEntityId && item.Source.PlaneId == inventoryPlaneId ? item.Source.Coord : null;
        }

        return null;
    }

    private IReadOnlyList<string> BuildActionMenuRows()
    {
        return _prompt.Mode switch
        {
            ActionChoicePromptMode.Closed => ["Menu: Enter opens authored action steps"],
            ActionChoicePromptMode.ActionList => ["Menu: action selector shown in component 0.2.1"],
            ActionChoicePromptMode.PickupTarget or ActionChoicePromptMode.DropSource or ActionChoicePromptMode.EnterTarget => BuildTargetListRows(),
            ActionChoicePromptMode.TransferCounterparty => BuildTransferCounterpartyRows(),
            ActionChoicePromptMode.TransferItem => BuildTransferItemRows(),
            ActionChoicePromptMode.PickupDestination or ActionChoicePromptMode.DropDestination => BuildDestinationListRows(),
            ActionChoicePromptMode.ExitFacing => BuildDirectionListRows(),
            _ => []
        };
    }

    private IReadOnlyList<string> BuildActionListRows()
    {
        var steps = AvailablePlayerActionSteps();
        return ["Menu: choose authored action step", .. steps.Select((step, index) => $"{(index == _prompt.SelectedActionStepIndex ? ">" : " ")} {index + 1}. {step.Kind}")];
    }

    private IReadOnlyList<string> BuildTargetListRows()
    {
        if (_prompt.SelectedEntityActionChoice is not { } choice)
        {
            return ["Menu: target list unavailable"];
        }

        var targets = _prompt.ValidSelectedTargets();
        var label = choice.Kind == ActionChoiceKind.Drop ? "inventory item" : "target";
        return [$"Menu: choose {choice.Kind} {label}", .. targets.Select((target, index) => $"{(index == _prompt.SelectedTargetIndex ? ">" : " ")} {FormatEntityName(target.TargetId)} from {FormatNullableSource(target.Source)}")];
    }

    private IReadOnlyList<string> BuildDirectionListRows()
    {
        if (_prompt.SelectedEntityActionChoice is not { } choice)
        {
            return ["Menu: direction list unavailable"];
        }

        var directions = choice.DirectionOptions.Where(option => option.CanExecute).ToList();
        return [$"Menu: choose {choice.Kind} direction", .. directions.Select((direction, index) => $"{(index == _prompt.SelectedDirectionIndex ? ">" : " ")} {direction.Direction} to {FormatNullableSource(direction.Destination)}")];
    }

    private IReadOnlyList<string> BuildDestinationListRows()
    {
        if (_prompt.SelectedEntityActionChoice is not { } choice || _prompt.SelectedEntityActionTargetId is not { } targetId)
        {
            return ["Menu: destination list unavailable"];
        }

        var destinations = _prompt.ValidSelectedDestinations();
        var label = choice.Kind == ActionChoiceKind.Pickup ? "inventory location" : "drop destination";
        return [$"Menu: choose {choice.Kind} {label} for {FormatEntityName(targetId)}", .. destinations.Select((destination, index) => $"{(index == _prompt.SelectedDestinationIndex ? ">" : " ")} {FormatDestination(destination.Destination)}")];
    }

    private IReadOnlyList<string> BuildTransferCounterpartyRows()
    {
        if (_prompt.SelectedEntityActionChoice is not { } choice)
        {
            return ["Menu: transfer entity list unavailable"];
        }

        var counterparties = choice.TransferCounterparties.Where(counterparty => counterparty.CanExecute).ToList();
        return ["Menu: choose Transfer entity", .. counterparties.Select((counterparty, index) => $"{(index == _prompt.SelectedTargetIndex ? ">" : " ")} {FormatEntityName(counterparty.CounterpartyId)} {counterparty.Direction}")];
    }

    private IReadOnlyList<string> BuildTransferItemRows()
    {
        if (_prompt.SelectedEntityActionChoice is not { } choice || _prompt.SelectedEntityActionTargetId is not { } counterpartyId)
        {
            return ["Menu: transfer item list unavailable"];
        }

        var items = choice.TransferItems(counterpartyId).Where(item => item.CanExecute).ToList();
        return [$"Menu: choose Transfer item with {FormatEntityName(counterpartyId)}", .. items.Select((item, index) => $"{(index == _prompt.SelectedDestinationIndex ? ">" : " ")} {FormatTransferDirection(item)} {FormatEntityName(item.MovingEntityId)} from {FormatEntityName(item.OwnerEntityId)}")];
    }

    private string FormatTransferDirection(ActionChoiceTransferItemOption item) => item.TransferDirection switch
    {
        TransferDirection.ActorToTarget => $"Give to {FormatEntityName(item.CounterpartyId)}",
        TransferDirection.TargetToActor => $"Take from {FormatEntityName(item.CounterpartyId)}",
        _ => item.TransferDirection.ToString()
    };

    private string DescribeActionChoice(ActionChoice choice)
    {
        return choice.Kind switch
        {
            ActionChoiceKind.Move => $"Move {choice.DirectionOptions.Count(option => option.CanExecute)}/{choice.DirectionOptions.Count} executable directions",
            ActionChoiceKind.Pickup => DescribeEntityDestinationChoice("Pickup", choice),
            ActionChoiceKind.Drop => DescribeEntityDestinationChoice("Drop", choice),
            ActionChoiceKind.Enter => DescribeEntityChoice("Enter", choice),
            ActionChoiceKind.Exit => $"Exit {choice.DirectionOptions.Count(option => option.CanExecute)}/{choice.DirectionOptions.Count} executable directions",
            ActionChoiceKind.Transfer => $"Transfer {choice.TransferCounterparties.Count(option => option.CanExecute)}/{choice.TransferCounterparties.Count} counterparties",
            _ => choice.Kind.ToString()
        };
    }

    private string DescribeEntityChoice(string label, ActionChoice choice)
    {
        var executableTargets = choice.EntityOptions.Count(option => option.CanExecute);
        var firstExecutableTarget = choice.EntityOptions.FirstOrDefault(option => option.CanExecute)?.TargetId;
        var targetLabel = firstExecutableTarget is { } targetId
            ? $"; first {FormatEntityName(targetId)}"
            : string.Empty;
        return $"{label} {executableTargets}/{choice.EntityOptions.Count} targets{targetLabel}";
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
        if (!_session.Registry.TryGetTemplateIdForEntity(World, entityId, out var templateId))
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
        if (!_session.Registry.TryGetTemplateIdForEntity(World, entityId, out var templateId))
        {
            return ["action plan: template unavailable"];
        }

        var template = _session.Registry.GetEntityTemplate(templateId);
        var defaultPlanId = World.GetDefaultActionPlanId(entityId) is { } runtimePlanId
            ? new ActionPlanTemplateId(runtimePlanId.Value)
            : template.DefaultActionPlanId;
        if (defaultPlanId is not { } planId || !_session.Registry.ActionPlanDescriptors.TryGetValue(planId, out var plan))
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

    private IUiComponent BuildTransferInventoryComparisonComponent(
        EntityPanelProjection actorProjection,
        EntityPanelProjection counterpartyProjection,
        SadConsoleRect bounds,
        IReadOnlyList<ActionChoiceTransferItemOption> validItems,
        ActionChoiceTransferItemOption selectedItem)
    {
        var actorGrid = actorProjection.InventoryGrid;
        var counterpartyGrid = counterpartyProjection.InventoryGrid;
        var actorPlaneId = actorGrid?.PlaneId;
        var counterpartyPlaneId = counterpartyGrid?.PlaneId;
        return new TransferInventoryComparisonComponent(
            "0.3.1",
            "0.3.1 Transfer inventories",
            bounds,
            UiComponentState.Focused,
            BuildTransferInventorySide(actorProjection, actorGrid, validItems, selectedItem, actorPlaneId),
            BuildTransferInventorySide(counterpartyProjection, counterpartyGrid, validItems, selectedItem, counterpartyPlaneId),
            $"Selected: {FormatTransferDirection(selectedItem)} {FormatEntityName(selectedItem.MovingEntityId)}",
            "Controls: Up/Down choose valid item | Enter transfer | Esc back");
    }

    private static TransferInventorySideComponent BuildTransferInventorySide(
        EntityPanelProjection projection,
        InventoryInspectionGrid? grid,
        IReadOnlyList<ActionChoiceTransferItemOption> validItems,
        ActionChoiceTransferItemOption selectedItem,
        PlaneId? planeId)
    {
        if (grid is null || planeId is null)
        {
            return new TransferInventorySideComponent(
                $"{projection.Glyph} {projection.Name}",
                ["inventory unavailable"],
                0,
                0,
                [],
                new HashSet<GridCoord>(),
                null);
        }

        var validCoords = validItems
            .Where(item => item.Source.PlaneId == planeId.Value)
            .Select(item => item.Source.Coord)
            .ToHashSet();
        var selectedCoord = selectedItem.Source.PlaneId == planeId.Value ? selectedItem.Source.Coord : (GridCoord?)null;
        return new TransferInventorySideComponent(
            $"{projection.Glyph} {projection.Name}",
            [$"inventory: {grid.Width}x{grid.Height} {grid.PlaneId}", $"valid items: {validCoords.Count}"],
            grid.Width,
            grid.Height,
            grid.Cells.Select(cell => new InventoryGridCell(cell.Coord, cell.Glyph, cell.Color)).ToList(),
            validCoords,
            selectedCoord);
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

internal sealed record TransferInventoryComparisonComponent(
    string Id,
    string Title,
    SadConsoleRect Bounds,
    UiComponentState State,
    TransferInventorySideComponent ActorSide,
    TransferInventorySideComponent CounterpartySide,
    string SelectedSummary,
    string Controls) : IUiComponent
{
    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme) =>
        [$"[{State.BorderColor(theme)}] {Title}", ActorSide.Title, CounterpartySide.Title, SelectedSummary, Controls];
}

internal sealed record TransferInventorySideComponent(
    string Title,
    IReadOnlyList<string> Rows,
    int GridWidth,
    int GridHeight,
    IReadOnlyList<InventoryGridCell> Cells,
    IReadOnlySet<GridCoord> ValidSelectionCoords,
    GridCoord? SelectedCoord);

internal sealed record GameplayMockManualLayoutRecalculation(int Width, int Height, int RegionCount);
