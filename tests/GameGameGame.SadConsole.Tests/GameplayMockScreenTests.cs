using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Screens;
using System.Reflection;

namespace GameGameGame.SadConsole.Tests;

public sealed class GameplayMockScreenTests
{
    [Fact]
    public void StartupRecognizesPlayMockModeWithContentAndScenario()
    {
        var startup = SadConsoleStartup.FromArgs(["--play-mock", "content.yaml", "demo-scenario"]);

        Assert.True(startup.LaunchPlayMock);
        Assert.Equal("content.yaml", startup.DirectContentPath);
        Assert.Equal("demo-scenario", startup.DirectScenarioId);
        Assert.False(startup.LaunchGallery);
        Assert.Null(startup.Catalog);
    }

    [Fact]
    public void FrameUsesPlayerPointOfViewCurrentPlaceAsCenteredViewport()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.Equal(session.PlayerEntityId, frame.PlayerProjection.EntityId);
        Assert.NotNull(frame.PlayerProjection.PointOfView);
        Assert.NotNull(frame.PlayerProjection.PointOfView.CurrentPlace);
        Assert.Equal(session.ActiveContainerEntityId, frame.PlayerProjection.PointOfView.CurrentPlace.EntityId);
        Assert.Equal(session.ActiveContainerEntityId, frame.CurrentPlaceProjection?.EntityId);
        Assert.NotNull(frame.CurrentPlaceProjection?.InventoryGrid);
        Assert.Equal("0.2", frame.Components[0].Id);
        Assert.Equal(0, frame.HudBounds.Left);
        Assert.InRange(frame.HudBounds.Width, 20, 28);
        Assert.True(frame.CurrentPlaceBounds.Left > frame.HudBounds.Left + frame.HudBounds.Width);
        Assert.Equal(0, frame.CurrentPlaceBounds.Top);
        Assert.True(frame.CurrentPlaceBounds.Bottom <= frame.InspectionBounds.Top);
    }

    [Fact]
    public void FrameUsesWorldTemplateWhenCurrentPlaceLacksRegistryTemplateAssignment()
    {
        var session = CreateGameplayMockSession();
        RemoveTemplateAssignment(session.Registry, session.ActiveContainerEntityId);
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.Equal(session.ActiveContainerEntityId, frame.PlayerProjection.PointOfView?.CurrentPlace?.EntityId);
        Assert.Equal(session.ActiveContainerEntityId, frame.CurrentPlaceProjection?.EntityId);
        Assert.Equal('#', frame.PlayerProjection.PointOfView?.CurrentPlace?.Glyph);
        Assert.Equal(PresentationColor.Gray, frame.PlayerProjection.PointOfView?.CurrentPlace?.Color);
        Assert.Equal('#', frame.CurrentPlaceProjection?.Glyph);
        Assert.Equal(PresentationColor.Gray, frame.CurrentPlaceProjection?.Color);
    }

    [Fact]
    public void FrameDrawsPersistentHudRowsAboveMainViewport()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.Contains(frame.HudRows, row => row.Contains("Player:"));
        Assert.Contains(frame.HudRows, row => row.Contains("Current place: Mock Room"));
        Assert.Contains(frame.HudRows, row => row.Contains("Action: 1/2 PickupTarget"));
        Assert.Contains(frame.HudRows, row => row.Contains("Move: direct compatibility controls"));
        Assert.Contains(frame.HudRows, row => row.Contains("move 8-way"));
        Assert.Equal(0, frame.HudBounds.Top);
        Assert.Equal(42, frame.HudBounds.Bottom);
        Assert.True(frame.InspectionBounds.Top >= 28);
    }

    [Fact]
    public void FrameHidesLayoutDebugPanelByDefault()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.False(screen.IsLayoutDebugVisible);
        Assert.DoesNotContain(frame.Components, component => component.Id == "0.layout-debug");
    }

    [Fact]
    public void ToggleLayoutDebugShowsResolvedRegionRows()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        var message = screen.ToggleLayoutDebug();
        var frame = screen.BuildFrame(120, 42);

        Assert.Contains("Layout debug visible", message);
        Assert.True(screen.IsLayoutDebugVisible);
        var panel = Assert.IsType<PanelComponent>(Assert.Single(frame.Components, component => component.Id == "0.layout-debug"));
        Assert.Equal(frame.Regions.Count + 2, panel.BodyRows.Count);
        Assert.Equal("hit: move mouse over play surface", panel.BodyRows[0]);
        Assert.Equal("manual: F11 recalculates current logical console layout", panel.BodyRows[1]);
        Assert.Contains(panel.BodyRows, row => row == "0.1 HUD/status L0 T0 W24 H42 Z2");
        Assert.Contains(panel.BodyRows, row => row == "0.2.1 Action selector L27 T2 W38 H10 Z10");
    }

    [Fact]
    public void LayoutDebugRowsShowMouseHitTestResult()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        screen.ToggleLayoutDebug();
        Assert.True(screen.SetLayoutDebugMouseCell(27, 22));
        var frame = screen.BuildFrame(120, 42);

        var panel = Assert.IsType<PanelComponent>(Assert.Single(frame.Components, component => component.Id == "0.layout-debug"));
        Assert.Equal("hit: 0.diagnostics Diagnostics local 2,1 Z20", panel.BodyRows[0]);
    }

    [Fact]
    public void RecalculateLayoutRecordsManualDebugStatus()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        screen.ToggleLayoutDebug();
        var message = screen.RecalculateLayout(160, 60);
        var frame = screen.BuildFrame(160, 60);

        Assert.Equal("Recalculated layout from logical console 160x60; resolved 160x60 with 6 regions. Window pixel resize does not change cells yet.", message);
        var panel = Assert.IsType<PanelComponent>(Assert.Single(frame.Components, component => component.Id == "0.layout-debug"));
        Assert.Equal("manual: logical 160x60 regions 6; window pixels do not change cells yet", panel.BodyRows[1]);
    }

    [Fact]
    public void DebugAdvanceOneControlledTurnMovesMockForwardThroughHistory()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        var message = screen.DebugAdvanceOneControlledTurn();
        var frame = screen.BuildFrame(120, 42);

        Assert.Contains("Debug wait advanced", message);
        Assert.Equal(1, screen.FrameIndex);
        Assert.Contains("frame 1", frame.Title);
        Assert.Contains("world turn 2", frame.Title);
    }

    [Fact]
    public void GameplaySessionControllerSubmitsWaitAndRefreshesStructuredLog()
    {
        var session = CreateGameplayMockSession();
        var controller = new GameplaySessionController(session);

        var result = controller.SubmitWait();

        Assert.True(result.Succeeded, result.FailureText);
        Assert.Equal(1, controller.FrameIndex);
        Assert.Equal(2, controller.World.TurnNumber);
        Assert.NotNull(controller.ActionLog);
        Assert.NotEmpty(controller.ActionLog!.Chronological);
    }

    [Fact]
    public void GameplaySessionControllerSubmitsMoveThroughCoreActionChoiceWhenAvailable()
    {
        var session = CreateGameplayMockSession(includeCanonicalMove: true, bindPlayerChoiceControl: true);
        var controller = new GameplaySessionController(session);

        var result = controller.SubmitMove(Direction.West);

        Assert.True(result.UsedCoreActionChoice);
        Assert.True(result.Succeeded, result.FailureText);
        Assert.Equal(1, controller.FrameIndex);
        Assert.Equal(2, controller.World.TurnNumber);
    }

    [Fact]
    public void GameplaySessionControllerPromptsLaterPlayerChoiceAfterEarlierAutomaticActorRuns()
    {
        var session = WithActorOrder(CreateGameplayMockSession(bindPlayerChoiceControl: true), [new EntityId("mockCrate"), new EntityId("mockPlayer")]);

        var controller = new GameplaySessionController(session);

        Assert.Equal(new EntityId("mockPlayer"), controller.PlayerEntityId);
        Assert.NotNull(controller.CurrentActionChoiceRequest);
        Assert.Equal(new EntityId("mockPlayer"), controller.CurrentActionChoiceRequest!.ActorId);
        Assert.Equal(1, session.World.TurnNumber);
    }

    [Fact]
    public void GameplaySessionControllerKeepsHistoryLogsWhenPromptActorChanges()
    {
        var automaticActorId = new EntityId("mockCrate");
        var promptActorId = new EntityId("mockPlayer");
        var session = WithActorOrder(CreateGameplayMockSession(bindPlayerChoiceControl: true), [automaticActorId, promptActorId]);

        var controller = new GameplaySessionController(session);

        Assert.Equal(promptActorId, controller.PlayerEntityId);
        Assert.NotNull(controller.ActionLog);
        Assert.Contains(controller.ActionLog!.Chronological, outcome => outcome.ActorId == automaticActorId);
    }

    [Fact]
    public void GameplaySessionControllerPromptsMultiplePlayerChoiceActorsInInitiativeOrder()
    {
        var session = WithActorOrder(
            CreateGameplayMockSession(bindPlayerChoiceControl: true, includeCratePlayerControl: true, includePlayerReciprocalAdjectives: true),
            [new EntityId("mockPlayer"), new EntityId("mockCrate")]);
        var controller = new GameplaySessionController(session);

        var result = controller.SubmitWait();

        Assert.True(result.Succeeded, result.FailureText);
        Assert.Equal(new EntityId("mockCrate"), controller.PlayerEntityId);
        Assert.NotNull(controller.CurrentActionChoiceRequest);
        Assert.Equal(new EntityId("mockCrate"), controller.CurrentActionChoiceRequest!.ActorId);
    }

    [Fact]
    public void GameplaySessionControllerNoPlayerScenarioCanAdvanceAutomaticActors()
    {
        var session = CreateGameplayMockSession();
        session.World.SetActionControlSource(session.PlayerEntityId, EntityControlSource.Automatic);
        var controller = new GameplaySessionController(session);
        var turnAfterStartupCycle = session.World.TurnNumber;

        var result = controller.SubmitWait();

        Assert.True(result.Succeeded, result.FailureText);
        Assert.Null(controller.CurrentActionChoiceRequest);
        Assert.True(session.World.TurnNumber > turnAfterStartupCycle);
    }

    [Fact]
    public void GameplaySessionControllerExcludesSecondaryPlayerControlsFromAutonomousPlans()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true, includeCratePlayerControl: true);
        var controller = new GameplaySessionController(session);
        var controlledCrateId = new EntityId("mockCrate");
        var initialCrateLocation = session.World.GetEntityLocation(controlledCrateId);

        var result = controller.SubmitWait();

        Assert.True(result.Succeeded, result.FailureText);
        Assert.Equal(1, controller.FrameIndex);
        Assert.Equal(initialCrateLocation, session.World.GetEntityLocation(controlledCrateId));
    }

    [Fact]
    public void GameplaySessionControllerSynchronizesCreatedActorActionPlanAfterAutomaticCreate()
    {
        var session = LifecycleFlagshipSession();

        var controller = new GameplaySessionController(session);

        var createdRatId = new EntityId("lifecyclerat-1");
        Assert.True(controller.World.Entities.ContainsKey(createdRatId));
        Assert.True(controller.ProjectionActionPlans.ContainsKey(createdRatId));
    }

    [Fact]
    public void GameplaySessionControllerUsesPolymorphedActionPlanOnNextAutomaticCycle()
    {
        var session = LifecycleFlagshipSession();
        var lifecycleId = new EntityId("lifecycleEgg");
        var controller = new GameplaySessionController(session);
        Assert.Equal("lifecycleCaterpillar", controller.World.Entities[lifecycleId].TemplateId);

        var result = controller.SubmitWait();

        Assert.True(result.Succeeded, result.FailureText);
        Assert.Equal("lifecycleCocoon", controller.World.Entities[lifecycleId].TemplateId);
    }

    [Fact]
    public void ActionChoicePromptControllerEnterOpensActionList()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true);
        var runtime = new GameplaySessionController(session);
        var prompt = new ActionChoicePromptController();

        var message = prompt.OpenActionStepMenu(runtime.AvailablePlayerActionSteps());

        Assert.Equal(ActionChoicePromptMode.ActionList, prompt.Mode);
        Assert.Equal(0, prompt.SelectedActionStepIndex);
        Assert.Contains("Opened action selector", message);
    }

    [Fact]
    public void ActionChoicePromptControllerPickupAdvancesTargetThenDestinationAndCancelUnwinds()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true);
        var runtime = new GameplaySessionController(session);
        var prompt = new ActionChoicePromptController();
        prompt.OpenActionStepMenu(runtime.AvailablePlayerActionSteps());

        var target = prompt.ConfirmSelectedActionStep(runtime.AvailablePlayerActionSteps(), runtime.CurrentActionChoiceRequest, FormatEntityId);
        var destination = prompt.ConfirmSelectedTarget(FormatEntityId, FormatPlaneCoord);
        var destinationMode = prompt.Mode;
        var cancelDestination = prompt.Cancel();
        var cancelTarget = prompt.Cancel();

        Assert.Equal(ActionChoicePromptActionResultKind.ChoosingTarget, target.Kind);
        Assert.Equal(ActionChoicePromptTargetResultKind.ChoosingDestination, destination.Kind);
        Assert.Equal(ActionChoicePromptMode.PickupDestination, destinationMode);
        Assert.Equal("Returned to pickup target selection.", cancelDestination.Message);
        Assert.Equal(ActionChoicePromptMode.ActionList, prompt.Mode);
        Assert.Equal("Returned to action selector.", cancelTarget.Message);
    }

    [Fact]
    public void ActionChoicePromptControllerDropAdvancesSourceThenDestinationAndRequestsInventoryInspection()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true, includeDropStep: true, startWithCarriedItem: true);
        var runtime = new GameplaySessionController(session);
        var prompt = new ActionChoicePromptController();
        var steps = runtime.AvailablePlayerActionSteps();
        prompt.OpenActionStepMenu(steps);
        prompt.SelectMenuItem(1, steps, FormatEntityId, FormatPlaneCoord);

        var source = prompt.ConfirmSelectedActionStep(steps, runtime.CurrentActionChoiceRequest, FormatEntityId);
        var destination = prompt.ConfirmSelectedTarget(FormatEntityId, FormatPlaneCoord);

        Assert.Equal(ActionChoicePromptActionResultKind.ChoosingTarget, source.Kind);
        Assert.True(source.InspectPlayer);
        Assert.Equal(ActionChoicePromptMode.DropDestination, prompt.Mode);
        Assert.Equal(ActionChoicePromptTargetResultKind.ChoosingDestination, destination.Kind);
        Assert.False(destination.InspectPlayer);
    }

    [Fact]
    public void TransferChoiceUsesCounterpartyThenItemPromptStack()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true, startWithCarriedItem: true, includeTransferStep: true);
        var runtime = new GameplaySessionController(session);
        var request = runtime.CurrentActionChoiceRequest!;
        var steps = new[] { new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Transfer) };
        var prompt = new ActionChoicePromptController();
        prompt.OpenActionStepMenu(steps);

        var counterparty = prompt.ConfirmSelectedActionStep(steps, request, FormatEntityId);
        var counterpartyMode = prompt.Mode;
        var item = prompt.ConfirmSelectedTarget(FormatEntityId, FormatPlaneCoord);
        var itemMode = prompt.Mode;
        var submit = prompt.ConfirmSelectedTransferItem();
        var cancelItem = prompt.Cancel();
        var cancelCounterparty = prompt.Cancel();

        Assert.Equal(ActionChoicePromptActionResultKind.ChoosingTarget, counterparty.Kind);
        Assert.Equal(ActionChoicePromptMode.TransferCounterparty, counterpartyMode);
        Assert.Equal(ActionChoicePromptTargetResultKind.ChoosingDestination, item.Kind);
        Assert.Equal(ActionChoicePromptMode.TransferItem, itemMode);
        Assert.Equal(ActionChoicePromptTransferItemResultKind.Submit, submit.Kind);
        Assert.Equal(new EntityId("mockCrate"), submit.CounterpartyId);
        Assert.Equal(new EntityId("mockToken"), submit.MovingEntityId);
        Assert.Equal("Returned to transfer counterparty selection.", cancelItem.Message);
        Assert.Equal("Returned to action selector.", cancelCounterparty.Message);
    }

    [Fact]
    public void TransferUxFocusesCounterpartyThenItemOwnerInventory()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true, startWithCarriedItem: true, includeTransferStep: true);
        var screen = new GameplayMockScreen(session);

        screen.ExecuteSelectedActionStep();
        screen.SelectNextActionStep();
        screen.ExecuteSelectedActionStep();
        var counterpartyMode = screen.ActionMenuState;
        var counterpartyFrame = screen.BuildFrame(120, 42);
        screen.ExecuteSelectedActionStep();
        var itemMode = screen.ActionMenuState;
        var itemFrame = screen.BuildFrame(120, 42);

        Assert.Equal(ActionChoicePromptMode.TransferCounterparty.ToString(), counterpartyMode);
        Assert.Equal(new EntityId("mockCrate"), counterpartyFrame.InspectedProjection?.EntityId);
        Assert.Equal(new GridCoord(2, 1), counterpartyFrame.CurrentPlaceSelectedCoord);
        Assert.Contains(counterpartyFrame.HudRows, row => row.Contains("Menu: choose Transfer entity", StringComparison.Ordinal));

        Assert.Equal(ActionChoicePromptMode.TransferItem.ToString(), itemMode);
        Assert.Equal(new EntityId("mockPlayer"), itemFrame.InspectedProjection?.EntityId);
        Assert.Equal(new GridCoord(0, 0), itemFrame.InspectionSelectedCoord);
        Assert.Contains(new GridCoord(0, 0), itemFrame.InspectionValidSelectionCoords);
        Assert.Contains(itemFrame.HudRows, row => row.Contains("Give to Mock Crate Mock Token from Mock Player", StringComparison.Ordinal));

        var transferComponent = Assert.IsType<TransferInventoryComparisonComponent>(Assert.Single(itemFrame.Components, component => component.Id == "0.3.1"));
        Assert.Equal("0.3.1 Transfer inventories", transferComponent.Title);
        Assert.Contains("Mock Player", transferComponent.ActorSide.Title, StringComparison.Ordinal);
        Assert.Contains("Mock Crate", transferComponent.CounterpartySide.Title, StringComparison.Ordinal);
        Assert.Contains(new GridCoord(0, 0), transferComponent.ActorSide.ValidSelectionCoords);
        Assert.Equal(new GridCoord(0, 0), transferComponent.ActorSide.SelectedCoord);
        Assert.Empty(transferComponent.CounterpartySide.ValidSelectionCoords);
        Assert.Contains("Give to Mock Crate Mock Token", transferComponent.SelectedSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void ActionChoicePromptControllerEmptyTargetListExplainsWithoutEnteringDeadEndMode()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true);
        var runtime = new GameplaySessionController(session);
        var prompt = new ActionChoicePromptController();
        var steps = runtime.AvailablePlayerActionSteps();
        prompt.OpenActionStepMenu(steps);
        runtime.SubmitPickupActionChoice(new EntityId("mockCrate"), new PlaneCoord(session.World.GetInventoryPlaneId(session.PlayerEntityId)!.Value, new GridCoord(0, 0)));

        var result = prompt.ConfirmSelectedActionStep(steps, runtime.CurrentActionChoiceRequest, FormatEntityId);

        Assert.Equal(ActionChoicePromptActionResultKind.Message, result.Kind);
        Assert.Equal(ActionChoicePromptMode.ActionList, prompt.Mode);
        Assert.Contains("no valid targets", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DebugAdvanceExposesCurrentPlacePlayerFacingLogIds()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        screen.DebugAdvanceOneControlledTurn();
        var frame = screen.BuildFrame(120, 42);

        Assert.Contains(frame.CurrentPlacePlayerLogRows, row => row.Contains("player-log: action.wait.success"));
        Assert.All(frame.CurrentPlacePlayerLogRows, row => Assert.DoesNotContain("Trace", row, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExecuteControlledMoveNorthEastChangesLocationAndFacingAdvancesFrameAndLogsDirection()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        var message = screen.ExecuteControlledMove(Direction.NorthEast);
        var frame = screen.BuildFrame(120, 42);

        Assert.Contains("Moved NorthEast", message);
        Assert.Equal(new GridCoord(2, 0), session.World.GetEntityLocation(session.PlayerEntityId).Coord);
        Assert.Equal(Direction.NorthEast, session.World.GetActionFacing(session.PlayerEntityId));
        Assert.Equal(1, screen.FrameIndex);
        Assert.Contains("frame 1", frame.Title);
        Assert.Contains(frame.CurrentPlacePlayerLogRows, row =>
            row.Contains("player-log: action.move.success") && row.Contains("direction=NorthEast"));
    }

    [Fact]
    public void PlayerChoiceCanonicalMoveUsesCoreActionChoiceMovementInHud()
    {
        var session = CreateGameplayMockSession(includeCanonicalMove: true, bindPlayerChoiceControl: true);
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.Equal(EntityControlSource.PlayerChoice, session.World.GetActionControlSource(session.PlayerEntityId));
        Assert.True(screen.UsesCoreActionChoiceMovement);
        Assert.NotNull(screen.CurrentActionChoiceRequest);
        Assert.Contains(frame.HudRows, row => row.Contains("Move: Core Action Choice (8-way)"));
    }

    [Fact]
    public void ExecuteControlledMoveNorthEastWithPlayerChoiceCanonicalMoveUsesActionChoicePath()
    {
        var session = CreateGameplayMockSession(includeCanonicalMove: true, bindPlayerChoiceControl: true);
        var screen = new GameplayMockScreen(session);

        var message = screen.ExecuteControlledMove(Direction.NorthEast);
        var frame = screen.BuildFrame(120, 42);

        Assert.Contains("via Core Action Choice", message);
        Assert.Equal(new GridCoord(2, 0), session.World.GetEntityLocation(session.PlayerEntityId).Coord);
        Assert.Equal(Direction.NorthEast, session.World.GetActionFacing(session.PlayerEntityId));
        Assert.Equal(1, screen.FrameIndex);
        Assert.Contains("frame 1", frame.Title);
        Assert.Contains(frame.CurrentPlacePlayerLogRows, row =>
            row.Contains("player-log: action.move.success") && row.Contains("direction=NorthEast"));
    }

    [Fact]
    public void PlayerChoicePickupTargetSurfacesCoreActionChoiceInHud()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true);
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.True(screen.UsesCoreActionChoicePickup);
        Assert.NotNull(screen.CurrentActionChoiceRequest);
        Assert.Contains(frame.ActionChoiceRows, row => row.Contains("Pickup") && row.Contains("Mock Crate"));
        Assert.Contains(frame.HudRows, row => row.Contains("Choice: step 1 Pickup 1/1 targets"));
    }

    [Fact]
    public void PlayerChoiceDropFacingSurfacesCoreActionChoiceInHud()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true, includeDropStep: true, startWithCarriedItem: true);
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.True(screen.UsesCoreActionChoiceDrop);
        Assert.NotNull(screen.CurrentActionChoiceRequest);
        Assert.Contains(frame.ActionChoiceRows, row => row.Contains("Drop") && row.Contains("Mock Token"));
        Assert.Contains(frame.HudRows, row => row.Contains("Choice: step 2 Drop"));
    }

    [Fact]
    public void EnterOpensActionStepFirstMenuBeforeChoosingPickupTargetAndDestination()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true);
        var screen = new GameplayMockScreen(session);

        var opened = screen.ExecuteSelectedActionStep();
        var actionSelectorFrame = screen.BuildFrame(120, 42);
        var targetPrompt = screen.ExecuteSelectedActionStep();
        var destinationPrompt = screen.ExecuteSelectedActionStep();
        var message = screen.ExecuteSelectedActionStep();

        Assert.Contains("Opened action selector 0.2.1", opened);
        Assert.Contains(actionSelectorFrame.Components, component => component.Id == "0.2.1");
        Assert.Contains("Choose target", targetPrompt);
        Assert.Contains("Mock Crate", targetPrompt);
        Assert.Contains("Choose inventory location", destinationPrompt);
        Assert.Contains("Pickup Mock Crate via Core Action Choice", message);
        Assert.Equal(1, screen.FrameIndex);
        Assert.True(session.World.GetEntityLocation(new EntityId("mockCrate")).PlaneId.Value.Contains("mockPlayer", StringComparison.Ordinal));
        Assert.Equal("Closed", screen.ActionMenuState);
    }

    [Fact]
    public void PickupSelectionHighlightsCurrentPlaceTargetThenPlayerInventoryDestination()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true);
        var screen = new GameplayMockScreen(session);

        screen.ExecuteSelectedActionStep();
        screen.ExecuteSelectedActionStep();
        var targetFrame = screen.BuildFrame(120, 42);
        Assert.Equal("PickupTarget", screen.ActionMenuState);
        screen.ExecuteSelectedActionStep();
        var inventoryFrame = screen.BuildFrame(120, 42);

        Assert.Equal("PickupDestination", screen.ActionMenuState);
        Assert.Contains(new GridCoord(2, 1), targetFrame.CurrentPlaceValidSelectionCoords);
        Assert.Equal(new GridCoord(2, 1), targetFrame.CurrentPlaceSelectedCoord);
        Assert.Equal(session.PlayerEntityId, screen.InspectedEntityId);
        Assert.Contains(new GridCoord(0, 0), inventoryFrame.InspectionValidSelectionCoords);
        Assert.Equal(new GridCoord(0, 0), inventoryFrame.InspectionSelectedCoord);
    }

    [Fact]
    public void CancelReturnsThroughPickupPromptStackWithoutSubmitting()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true);
        var screen = new GameplayMockScreen(session);

        screen.ExecuteSelectedActionStep();
        screen.ExecuteSelectedActionStep();
        screen.ExecuteSelectedActionStep();
        var backToTarget = screen.CancelActionMenu();
        Assert.Contains("Returned to pickup target selection", backToTarget);
        Assert.Equal("PickupTarget", screen.ActionMenuState);

        var backToSelector = screen.CancelActionMenu();
        Assert.Contains("Returned to action selector", backToSelector);
        Assert.Equal("ActionList", screen.ActionMenuState);

        var closed = screen.CancelActionMenu();
        Assert.Contains("Closed action selector", closed);
        Assert.Equal("Closed", screen.ActionMenuState);
        Assert.Equal(0, screen.FrameIndex);
    }

    [Fact]
    public void DropChoiceUsesTargetThenDestinationListsAndHistorySubmission()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true, includeDropStep: true, startWithCarriedItem: true);
        var screen = new GameplayMockScreen(session);

        screen.ExecuteSelectedActionStep();
        screen.SelectNextActionStep();
        var targetPrompt = screen.ExecuteSelectedActionStep();
        var destinationPrompt = screen.ExecuteSelectedActionStep();
        var message = screen.ExecuteSelectedActionStep();

        Assert.Contains("DropFacing", targetPrompt);
        Assert.Contains("Mock Token", targetPrompt);
        Assert.Contains("Choose drop destination", destinationPrompt);
        Assert.Contains("Drop Mock Token via Core Action Choice", message);
        Assert.Equal(1, screen.FrameIndex);
        Assert.Equal(session.ActivePlaneId, session.World.GetEntityLocation(new EntityId("mockToken")).PlaneId);
        Assert.Equal("Closed", screen.ActionMenuState);
    }

    [Fact]
    public void PlayerWithoutPlayerChoiceFallsBackToDirectCompatibilityMovement()
    {
        var session = CreateGameplayMockSession(includeCanonicalMove: true, bindPlayerChoiceControl: false);
        session.World.SetActionControlSource(session.PlayerEntityId, EntityControlSource.Automatic);
        var screen = new GameplayMockScreen(session);

        var message = screen.ExecuteControlledMove(Direction.NorthEast);
        var frame = screen.BuildFrame(120, 42);

        Assert.Contains("via direct compatibility controls", message);
        Assert.False(screen.UsesCoreActionChoiceMovement);
        Assert.Equal(new GridCoord(2, 0), session.World.GetEntityLocation(session.PlayerEntityId).Coord);
        Assert.Contains(frame.HudRows, row => row.Contains("Move: direct compatibility controls"));
    }

    [Fact]
    public void PlayerChoiceWithoutCanonicalMoveFallsBackToDirectCompatibilityMovement()
    {
        var session = CreateGameplayMockSession(includeCanonicalMove: false, bindPlayerChoiceControl: true);
        var screen = new GameplayMockScreen(session);

        var message = screen.ExecuteControlledMove(Direction.NorthEast);
        var frame = screen.BuildFrame(120, 42);

        Assert.Contains("via direct compatibility controls", message);
        Assert.False(screen.UsesCoreActionChoiceMovement);
        Assert.Equal(new GridCoord(2, 0), session.World.GetEntityLocation(session.PlayerEntityId).Coord);
        Assert.Contains(frame.HudRows, row => row.Contains("Move: direct compatibility controls"));
    }

    [Fact]
    public void ExecuteControlledMoveBlockedEastDoesNotAdvanceAndLogsDirectionAndReason()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        var message = screen.ExecuteControlledMove(Direction.East);
        var frame = screen.BuildFrame(120, 42);

        Assert.Contains("Move East failed", message);
        Assert.Equal(new GridCoord(1, 1), session.World.GetEntityLocation(session.PlayerEntityId).Coord);
        Assert.Equal(0, screen.FrameIndex);
        Assert.Contains(frame.CurrentPlacePlayerLogRows, row =>
            row.Contains("player-log: action.move.failure")
            && row.Contains("direction=East")
            && row.Contains("reason=", StringComparison.Ordinal));
    }

    [Fact]
    public void ExecuteSelectedActionStepRunsAuthoredStepThroughHistory()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        screen.SelectNextActionStep();
        screen.ExecuteSelectedActionStep();
        var message = screen.ExecuteSelectedActionStep();
        var frame = screen.BuildFrame(120, 42);

        Assert.Contains("Choose target", message);
        Assert.Contains("Mock Crate", message);
        Assert.Equal(0, screen.FrameIndex);
        Assert.Contains("frame 0", frame.Title);
        Assert.Equal("EnterTarget", screen.ActionMenuState);
    }

    [Fact]
    public void EnterChoiceUsesTypedTargetPromptAndHistorySubmission()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true);
        var screen = new GameplayMockScreen(session);

        screen.ExecuteSelectedActionStep();
        screen.SelectNextActionStep();
        var targetPrompt = screen.ExecuteSelectedActionStep();
        var message = screen.ExecuteSelectedActionStep();

        Assert.True(screen.UsesCoreActionChoiceEnter);
        Assert.Contains("EnterTarget", targetPrompt);
        Assert.Contains("Mock Crate", targetPrompt);
        Assert.Contains("Enter Mock Crate via Core Action Choice", message);
        Assert.Equal(1, screen.FrameIndex);
        Assert.Contains("mockCrate", session.World.GetEntityLocation(session.PlayerEntityId).PlaneId.Value, StringComparison.Ordinal);
        Assert.Equal("Closed", screen.ActionMenuState);
    }

    [Fact]
    public void ExitChoiceUsesTypedDirectionPromptAndHistorySubmission()
    {
        var session = CreateGameplayMockSession(bindPlayerChoiceControl: true, includeExitStep: true, startPlayerInsideCrate: true);
        var screen = new GameplayMockScreen(session);

        screen.ExecuteSelectedActionStep();
        screen.SelectNextActionStep();
        screen.SelectNextActionStep();
        var directionPrompt = screen.ExecuteSelectedActionStep();
        var message = screen.ExecuteSelectedActionStep();

        Assert.True(screen.UsesCoreActionChoiceExit);
        Assert.Contains("ExitFacing", directionPrompt);
        Assert.Contains("Choose exit direction", directionPrompt);
        Assert.Contains("Exit", message);
        Assert.Contains("via Core Action Choice", message);
        Assert.Equal(1, screen.FrameIndex);
        Assert.Equal(session.ActivePlaneId, session.World.GetEntityLocation(session.PlayerEntityId).PlaneId);
        Assert.Equal("Closed", screen.ActionMenuState);
    }

    [Fact]
    public void InspectionCyclesVisibleNonPlayerEntitiesWithoutAdvancingTurn()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);
        var turn = session.World.TurnNumber;

        var message = screen.InspectNextEntity();
        var frame = screen.BuildFrame(120, 42);

        Assert.Contains("Inspecting Mock Crate", message);
        Assert.Equal(turn, session.World.TurnNumber);
        Assert.Equal(new EntityId("mockCrate"), screen.InspectedEntityId);
        Assert.NotNull(frame.InspectedProjection?.InventoryGrid);
        Assert.Contains(frame.Components, component => component.Id == "0.3");
    }

    [Fact]
    public void CurrentPlaceEntityRowsShowFacingAndLabeledTarget()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.Contains(frame.CurrentPlaceEntityRows, row => row.Contains("Mock Crate") && row.Contains("facing East") && row.Contains("loves -> Mock Player"));
    }

    [Fact]
    public void CurrentPlaceEntityRowsShowPlayerPointOfViewTargetAdjectivesWithoutAdvancingTurn()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);
        var turn = session.World.TurnNumber;

        var frame = screen.BuildFrame(120, 42);

        Assert.Equal(turn, session.World.TurnNumber);
        Assert.Contains(frame.CurrentPlaceEntityRows, row => row.Contains("Mock Crate") && row.Contains("adjectives portable, enterable"));
    }

    [Fact]
    public void CurrentPlaceEntityRowsShowPlayerPointOfViewReciprocalAdjectivesWithoutAdvancingTurn()
    {
        var session = CreateGameplayMockSession(includePlayerReciprocalAdjectives: true);
        var screen = new GameplayMockScreen(session);
        var turn = session.World.TurnNumber;

        var frame = screen.BuildFrame(120, 42);

        Assert.Equal(turn, session.World.TurnNumber);
        Assert.Contains(frame.CurrentPlaceEntityRows, row => row.Contains("Mock Crate") && row.Contains("reciprocal portable"));
    }

    [Fact]
    public void CurrentPlaceEntityRowsOmitReciprocalAdjectivesWhenProjectionDoesNotExposeThem()
    {
        var session = CreateGameplayMockSession(includePlayerReciprocalAdjectives: false);
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        var crateRow = Assert.Single(frame.CurrentPlaceEntityRows, row => row.Contains("Mock Crate"));
        Assert.DoesNotContain("reciprocal", crateRow);
    }

    [Fact]
    public void CurrentPlaceEntityRowsOmitTargetAdjectivesWhenProjectionDoesNotExposeThem()
    {
        var session = CreateGameplayMockSession(includePlayerTargetAdjectives: false);
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        var crateRow = Assert.Single(frame.CurrentPlaceEntityRows, row => row.Contains("Mock Crate"));
        Assert.DoesNotContain("portable", crateRow);
        Assert.DoesNotContain("enterable", crateRow);
    }

    [Fact]
    public void InspectedPanelRowsShowTargetingRulesAndActionPlanSteps()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        screen.InspectNextEntity();
        var frame = screen.BuildFrame(120, 42);

        Assert.Contains(frame.InspectedTargetingRows, row => row.Contains("rule loves") && row.Contains("Mock Player"));
        Assert.Contains(frame.InspectedActionPlanRows, row => row.Contains("action plan: mockCratePlan"));
        Assert.Contains(frame.InspectedActionPlanRows, row => row.Contains("SeekTarget loves"));
    }

    [Fact]
    public void CurrentRoomSizeIsLargeWhenPlayerBulkIsLessThanTenPercentOfAperture()
    {
        var session = CreateGameplayMockSession(playerBulk: 5, roomAperture: 100);
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.Equal("Large", frame.CurrentRoomSizeLabel);
    }

    [Fact]
    public void CurrentRoomSizeIsSmallWhenPlayerBulkIsWithinTenPercentOfAperture()
    {
        var session = CreateGameplayMockSession(playerBulk: 95, roomAperture: 100);
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.Equal("Small", frame.CurrentRoomSizeLabel);
    }

    private static PlayableScenarioSession CreateGameplayMockSession(
        int playerBulk = 1,
        int roomAperture = 100,
        bool includePlayerTargetAdjectives = true,
        bool includePlayerReciprocalAdjectives = false,
        bool includeCanonicalMove = false,
        bool bindPlayerChoiceControl = false,
        bool includeDropStep = false,
        bool startWithCarriedItem = false,
        bool includeExitStep = false,
        bool startPlayerInsideCrate = false,
        bool includeCratePlayerControl = false,
        bool includeTransferStep = false)
    {
        var document = new EditableContentDocument();
        var playerInteractionPlanId = new ActionPlanTemplateId("mockPlayerInteractionPlan");
        var playerSteps = new List<ActionPlanBehaviorStepDescriptor>();
        if (includeCanonicalMove)
        {
            playerSteps.Add(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Move, DirectionMode: ActionPlanMoveDirectionMode.Forward));
        }

        playerSteps.Add(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget));
        if (includeDropStep)
        {
            playerSteps.Add(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DropFacing));
        }

        if (includeTransferStep)
        {
            playerSteps.Add(new ActionPlanBehaviorStepDescriptor(
                ActionPlanBehaviorStepKind.Transfer,
                TargetSlot: 1,
                DirectionMode: ActionPlanMoveDirectionMode.Forward,
                TransferDirection: TransferDirection.ActorToTarget));
        }

        playerSteps.Add(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.EnterTarget));
        if (includeExitStep)
        {
            playerSteps.Add(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.ExitFacing));
        }
        document.ActionPlans[playerInteractionPlanId.Value] = EditableContentDocument.ActionPlanDescriptorDto.From(new ActionPlanDescriptor(
            new ActionPlanId(playerInteractionPlanId.Value),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(playerSteps)));
        var tokenTemplateId = document.AddEntityTemplate(
            "Mock Token",
            new EntityTemplate(
                "Mock Token",
                InventoryWidth: 1,
                InventoryHeight: 1,
                Bulk: 1,
                Aperture: 1),
            new EntityPresentation('t', PresentationColor.Cyan));
        var playerTemplateId = document.AddEntityTemplate(
            "Mock Player",
            new EntityTemplate(
                "Mock Player",
                InventoryWidth: 1,
                InventoryHeight: 1,
                Bulk: playerBulk,
                Aperture: 5,
                CarriedEntities: startWithCarriedItem ? [new CarriedEntityTemplate(new EntityId("mockToken"), tokenTemplateId, new GridCoord(0, 0))] : null,
                DefaultActionPlanId: includePlayerTargetAdjectives ? playerInteractionPlanId : null),
            new EntityPresentation('@', PresentationColor.Yellow));
        var cratePlanId = new ActionPlanTemplateId("mockCratePlan");
        var crateSteps = new List<ActionPlanBehaviorStepDescriptor>
        {
            new(ActionPlanBehaviorStepKind.SeekTarget, TargetLabel: "loves"),
            new(ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo, TargetLabel: "loves")
        };
        if (includePlayerReciprocalAdjectives)
        {
            crateSteps.Add(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget, TargetLabel: "loves"));
        }

        document.ActionPlans[cratePlanId.Value] = EditableContentDocument.ActionPlanDescriptorDto.From(new ActionPlanDescriptor(
            new ActionPlanId(cratePlanId.Value),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(crateSteps)));
        var crateTemplateId = document.AddEntityTemplate(
            "Mock Crate",
            new EntityTemplate(
                "Mock Crate",
                InventoryWidth: 2,
                InventoryHeight: 1,
                Bulk: 2,
                Aperture: 2,
                DefaultActionPlanId: cratePlanId,
                ActionStateDefaults: new ActorActionStateDefaults(Direction.East),
                TargetingRules: [new EntityTargetingRule(1, playerTemplateId, Range: 10, Label: "loves")]),
            new EntityPresentation('c', PresentationColor.Earth));
        var roomTemplateId = document.AddEntityTemplate(
            "Mock Room",
            new EntityTemplate(
                "Mock Room",
                InventoryWidth: 6,
                InventoryHeight: 4,
                Bulk: 100,
                Aperture: roomAperture,
                CarriedEntities: [new CarriedEntityTemplate(new EntityId("mockCrate"), crateTemplateId, new GridCoord(2, 1))]),
            new EntityPresentation('#', PresentationColor.Gray));
        document.UpsertScenario(new ScenarioDefinition(
            "play-mock-scenario",
            "Play Mock Scenario",
            roomTemplateId,
            playerTemplateId,
            new EntityId("mockPlayer"),
            new GridCoord(1, 1),
            bindPlayerChoiceControl || includeCratePlayerControl
                ? new Dictionary<string, IReadOnlyList<EntityId>>
                {
                    ["local-player"] = includeCratePlayerControl
                        ? [new EntityId("mockPlayer"), new EntityId("mockCrate")]
                        : [new EntityId("mockPlayer")]
                }
                : null));

        var session = PlayableScenarioLauncher.CreateFromDocument(document, "play-mock-scenario");
        if (startPlayerInsideCrate)
        {
            var movement = new MovementService();
            var crateInventoryPlane = session.World.GetInventoryPlaneId(new EntityId("mockCrate"))!.Value;
            Assert.True(movement.TryPlace(session.World, session.PlayerEntityId, new PlaneCoord(crateInventoryPlane, new GridCoord(0, 0))));
        }

        return session;
    }

    private static PlayableScenarioSession WithActorOrder(PlayableScenarioSession session, IReadOnlyList<EntityId> actorOrder) =>
        new(
            session.ScenarioId,
            session.Name,
            session.World,
            session.Registry,
            session.ActionPlans,
            session.PlayerEntityId,
            session.ActivePlaneId,
            session.ActiveContainerEntityId,
            session.CanPlay,
            session.ValidationDiagnostics,
            session.RuntimeFailures,
            session.CapabilityGaps,
            session.PlayerControls,
            actorOrder.Select(entityId => new ScenarioActorSummary(entityId, session.World.Entities[entityId].Name, session.World.GetEntityLocation(entityId))).ToList());

    private static void RemoveTemplateAssignment(PrototypeContentRegistry registry, EntityId entityId)
    {
        var field = typeof(PrototypeContentRegistry).GetField("_entityTemplateAssignments", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var assignments = Assert.IsType<Dictionary<EntityId, EntityTemplateId>>(field.GetValue(registry));
        Assert.True(assignments.Remove(entityId));
    }

    private static PlayableScenarioSession LifecycleFlagshipSession()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Content",
            "Beta",
            "EntityLifecycle",
            "CreateDestroyPolymorphShowcase.yaml");
        return PlayableScenarioLauncher.CreateFromFile(path, "delta-create-destroy-polymorph-flagship-room");
    }

    private static string FormatEntityId(EntityId entityId) => entityId.Value;

    private static string FormatPlaneCoord(PlaneCoord coord) => $"{coord.PlaneId.Value}:{coord.Coord.X},{coord.Coord.Y}";
}
