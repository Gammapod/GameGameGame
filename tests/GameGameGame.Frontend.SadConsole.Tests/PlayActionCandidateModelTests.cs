using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class PlayActionCandidateModelTests
{
    [Fact]
    public void ResolverReportsFollowUpNeededForIncompleteValidCandidate()
    {
        var targetId = new EntityId("target");
        var candidate = new PlayActionCandidate(
            PlayActionCandidateSource.InspectionEntity(targetId),
            ActionChoiceKind.Pickup,
            PlayActionCandidateProjector.ActionText(ActionChoiceKind.Pickup, targetId),
            IsValid: true,
            IsComplete: false,
            Prompt: new PlayActionPromptLayer(
                FrontendTextMessage.Create(FrontendTextIds.PlayActionPromptPickupDestination, ("targetName", targetId.Value)),
                [new PlayActionPromptChoice(FrontendTextMessage.Create(FrontendTextIds.PlayActionPromptDestination, ("coord", "1,2")), true, FocusCoord: new GridCoord(1, 2))]));

        var outcome = PlayActionCandidateResolver.ResolveSelection(candidate);

        Assert.Equal(PlayActionCandidateOutcomeKind.FollowUpNeeded, outcome.Kind);
        Assert.Same(candidate.Prompt, outcome.Prompt);
        Assert.True(candidate.OpensPrompt);
    }

    [Fact]
    public void ResolverMarksCompleteCandidateReadyToSubmitWithoutChoosingUiWorkflow()
    {
        var targetId = new EntityId("target");
        var candidate = new PlayActionCandidate(
            PlayActionCandidateSource.InspectionEntity(targetId),
            ActionChoiceKind.Enter,
            PlayActionCandidateProjector.ActionText(ActionChoiceKind.Enter, targetId),
            IsValid: true,
            IsComplete: true);

        var outcome = PlayActionCandidateResolver.ResolveSelection(candidate);

        Assert.Equal(PlayActionCandidateOutcomeKind.ReadyToSubmit, outcome.Kind);
        Assert.Same(candidate, outcome.Candidate);
    }

    [Fact]
    public void InspectionProjectionRowsCarryActionCandidatesFromSharedRequest()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var actionSession = new PlayActionSessionController(session);
        var targetId = new EntityId("debugPushBlock");

        var rows = InspectionActionChoiceProjector.Project(actionSession.CurrentActionChoiceRequest, targetId);

        Assert.Contains(rows, row => row.Candidate is { Source.Kind: PlayActionCandidateSourceKind.InspectionEntity, Source.EntityId: { } entityId } && entityId == targetId);
        Assert.DoesNotContain(rows, row => row.Selectable && row.Candidate is null);
    }

    [Fact]
    public void ActionHighlightResolverUsesGenericEntityTargetForSelectableRows()
    {
        var row = new EntityInspectionActionRow(FrontendTextMessage.Create(FrontendTextIds.InspectionActionPickup, ("targetName", "target")), Selectable: true);

        var kind = PlayActionHighlightResolver.ForInspectionAction(row);

        Assert.Equal(CellHighlightKind.EntityTarget, kind);
    }

    [Fact]
    public void ActionHighlightResolverUsesPickupForSelectablePickupRows()
    {
        var targetId = new EntityId("target");
        var candidate = new PlayActionCandidate(
            PlayActionCandidateSource.InspectionEntity(targetId),
            ActionChoiceKind.Pickup,
            PlayActionCandidateProjector.ActionText(ActionChoiceKind.Pickup, targetId),
            IsValid: true,
            IsComplete: true);
        var row = new EntityInspectionActionRow(candidate.Text, Selectable: true, Candidate: candidate);

        var kind = PlayActionHighlightResolver.ForInspectionAction(row);

        Assert.Equal(CellHighlightKind.Pickup, kind);
    }

    [Fact]
    public void ActionHighlightResolverUsesEnterForSelectableEnterRows()
    {
        var targetId = new EntityId("target");
        var candidate = new PlayActionCandidate(
            PlayActionCandidateSource.InspectionEntity(targetId),
            ActionChoiceKind.Enter,
            PlayActionCandidateProjector.ActionText(ActionChoiceKind.Enter, targetId),
            IsValid: true,
            IsComplete: true);
        var row = new EntityInspectionActionRow(candidate.Text, Selectable: true, Candidate: candidate);

        var kind = PlayActionHighlightResolver.ForInspectionAction(row);

        Assert.Equal(CellHighlightKind.Enter, kind);
    }

    [Fact]
    public void ActionHighlightResolverUsesPushForSelectablePushRows()
    {
        var targetId = new EntityId("target");
        var candidate = new PlayActionCandidate(
            PlayActionCandidateSource.InspectionEntity(targetId),
            ActionChoiceKind.Push,
            PlayActionCandidateProjector.ActionText(ActionChoiceKind.Push, targetId),
            IsValid: true,
            IsComplete: true);
        var row = new EntityInspectionActionRow(candidate.Text, Selectable: true, Candidate: candidate);

        var kind = PlayActionHighlightResolver.ForInspectionAction(row);

        Assert.Equal(CellHighlightKind.Push, kind);
    }

    [Fact]
    public void ActionHighlightResolverUsesNoActionForGreyedOutRows()
    {
        var row = new EntityInspectionActionRow(FrontendTextMessage.Create(FrontendTextIds.InspectionActionNoValidActions), Selectable: false);

        var kind = PlayActionHighlightResolver.ForInspectionAction(row);

        Assert.Equal(CellHighlightKind.NoAction, kind);
    }

    [Fact]
    public void ActionWorkflowConfirmsPickupIntoSelectedPlayerInventoryCell()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var actionSession = new PlayActionSessionController(session);
        var selection = new PlayActionWorkflowController(actionSession);
        var targetId = new EntityId("debugScrap3");

        Assert.True(selection.TryBeginPickup(targetId));
        Assert.Equal(CellHighlightKind.Pickup, selection.InventoryHighlight()?.Kind);
        var result = selection.ConfirmPickup();

        Assert.NotNull(result);
        Assert.True(result!.Succeeded, result.FailureDetail ?? result.FailureReason?.ToString() ?? "unknown");
        Assert.False(selection.IsActive);
        var inventoryPlane = session.World.GetRegisteredInventoryPlaneId(session.PlayerEntityId);
        Assert.NotNull(inventoryPlane);
        Assert.Equal(new PlaneCoord(inventoryPlane.Value, new GridCoord(0, 0)), session.World.GetEntityLocation(targetId));
    }

    [Fact]
    public void PlayerInventoryProjectionShowsDropFromActorChoiceRequest()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var actionSession = new PlayActionSessionController(session);

        var rows = InspectionActionChoiceProjector.ProjectPlayerInventory(actionSession.CurrentActionChoiceRequest);

        Assert.Contains(rows, row => row.Candidate is { Source.Kind: PlayActionCandidateSourceKind.PlayerInventory, Kind: ActionChoiceKind.Drop });
    }

    [Fact]
    public void ActionWorkflowConfirmsDropFromPlayerInventoryToAdjacentCell()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var actionSession = new PlayActionSessionController(session);
        var selection = new PlayActionWorkflowController(actionSession);
        var targetId = new EntityId("debugScrap3");

        Assert.True(selection.TryBeginPickup(targetId));
        var pickup = selection.ConfirmPickup();
        Assert.NotNull(pickup);
        Assert.True(pickup!.Succeeded, pickup.FailureDetail ?? pickup.FailureReason?.ToString() ?? "unknown");

        Assert.True(selection.TryBeginDropSource());
        Assert.Equal(CellHighlightKind.Drop, selection.InventoryHighlight()?.Kind);
        Assert.True(selection.ConfirmDropSource());
        Assert.Equal(CellHighlightKind.Drop, selection.GridHighlight()?.Kind);
        Assert.True(selection.CancelDropDestinationToSource());
        Assert.True(selection.IsDropSourceSelection);
        Assert.True(selection.ConfirmDropSource());
        var drop = selection.ConfirmDrop();

        Assert.NotNull(drop);
        Assert.True(drop!.Succeeded, drop.FailureDetail ?? drop.FailureReason?.ToString() ?? "unknown");
        Assert.False(selection.IsActive);
        var droppedLocation = session.World.GetEntityLocation(targetId);
        Assert.Equal(session.World.GetEntityLocation(session.PlayerEntityId).PlaneId, droppedLocation.PlaneId);
    }

    [Fact]
    public void DropDestinationSelectionHighlightsPressedAdjacentDirection()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var actionSession = new PlayActionSessionController(session);
        var selection = new PlayActionWorkflowController(actionSession);
        var targetId = new EntityId("debugScrap3");

        Assert.True(selection.TryBeginPickup(targetId));
        var pickup = selection.ConfirmPickup();
        Assert.NotNull(pickup);
        Assert.True(pickup!.Succeeded, pickup.FailureDetail ?? pickup.FailureReason?.ToString() ?? "unknown");
        Assert.True(selection.TryBeginDropSource());
        Assert.True(selection.ConfirmDropSource());

        var actorCoord = session.World.GetEntityLocation(session.PlayerEntityId).Coord;
        Assert.True(selection.Move(Direction.North));

        Assert.Equal(actorCoord.Offset(Direction.North), selection.GridHighlight()?.Coord);
    }

    [Fact]
    public void EnterChoiceSubmitsTargetWithoutFrontendDestinationSelection()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var actionSession = new PlayActionSessionController(session);
        MoveNextToDebugChest(actionSession);
        var targetId = new EntityId("debugChest");

        var rows = InspectionActionChoiceProjector.Project(actionSession.CurrentActionChoiceRequest, targetId);
        Assert.Contains(rows, row => row.Selectable && row.Candidate is { Kind: ActionChoiceKind.Enter });
        var result = actionSession.SubmitEnter(targetId);

        Assert.True(result.Succeeded, result.FailureDetail ?? result.FailureReason?.ToString() ?? "unknown");
        Assert.Equal(session.World.GetRegisteredInventoryPlaneId(targetId), session.World.GetEntityLocation(session.PlayerEntityId).PlaneId);
    }

    [Fact]
    public void PushSelectionChoosesDirectionAndSubmitsThroughCore()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var actionSession = new PlayActionSessionController(session);
        var selection = new PlayActionWorkflowController(actionSession);
        var targetId = new EntityId("debugPushBlock");

        var rows = InspectionActionChoiceProjector.Project(actionSession.CurrentActionChoiceRequest, targetId);
        Assert.Contains(rows, row => row.Selectable && row.Candidate is { Kind: ActionChoiceKind.Push });
        Assert.True(selection.TryBeginPushDirection(targetId));
        Assert.True(selection.SelectDirection(Direction.South));
        Assert.Equal(CellHighlightKind.Push, selection.GridHighlight()?.Kind);
        var result = selection.ConfirmCurrentSubmission();

        Assert.NotNull(result);
        Assert.True(result!.Succeeded, result.FailureDetail ?? result.FailureReason?.ToString() ?? "unknown");
        Assert.False(selection.IsActive);
        Assert.Equal(new GridCoord(4, 5), session.World.GetEntityLocation(targetId).Coord);
    }

    [Fact]
    public void ExitSelectionUsesCoreDirectionDestinationAndSubmitsDirection()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var actionSession = new PlayActionSessionController(session);
        MoveNextToDebugChest(actionSession);
        var enter = actionSession.SubmitEnter(new EntityId("debugChest"));
        Assert.True(enter.Succeeded, enter.FailureDetail ?? enter.FailureReason?.ToString() ?? "unknown");
        var selection = new PlayActionWorkflowController(actionSession);

        var rows = InspectionActionChoiceProjector.ProjectPlayerInventory(actionSession.CurrentActionChoiceRequest);
        Assert.Contains(rows, row => row.Selectable && row.Candidate is { Source.Kind: PlayActionCandidateSourceKind.PlayerInventory, Kind: ActionChoiceKind.Exit });
        Assert.True(selection.TryBeginExitDestination());
        Assert.NotNull(selection.ExitDestinationPlaneId());
        Assert.True(selection.Move(Direction.East));
        var highlight = selection.GridHighlight();

        Assert.NotNull(highlight);
        Assert.Equal(CellHighlightKind.Exit, highlight!.Kind);
        var exit = selection.ConfirmExit();
        Assert.NotNull(exit);
        Assert.True(exit!.Succeeded, exit.FailureDetail ?? exit.FailureReason?.ToString() ?? "unknown");
        Assert.False(selection.IsActive);
        Assert.NotEqual(session.World.GetRegisteredInventoryPlaneId(new EntityId("debugChest")), session.World.GetEntityLocation(session.PlayerEntityId).PlaneId);
    }

    [Fact]
    public void TransferSelectionListsCoreItemsAndSubmitsSelectedItem()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var actionSession = new PlayActionSessionController(session);
        MoveNextToDebugChest(actionSession);
        var selection = new PlayActionWorkflowController(actionSession);
        var counterpartyId = new EntityId("debugChest");

        var rows = InspectionActionChoiceProjector.Project(actionSession.CurrentActionChoiceRequest, counterpartyId);
        Assert.Single(rows, row => row.Candidate is { Kind: ActionChoiceKind.Transfer });
        Assert.Contains(rows, row => row.Selectable && row.Candidate is { Kind: ActionChoiceKind.Transfer });
        Assert.True(selection.TryBeginTransferItems(counterpartyId));
        var transferRows = selection.TransferSelectionRows();
        var transferOptions = selection.TransferSelectionOptions();
        Assert.NotEmpty(transferRows);
        Assert.NotEmpty(transferOptions);
        Assert.Single(transferRows, row => row.IsSelected);
        Assert.Single(transferOptions, option => option.IsSelected && option.Highlight?.Kind == CellHighlightKind.Transfer);
        Assert.Contains(transferRows, row => row.EntityName == "debugScrap1" || row.EntityName == "debugScrap2");
        Assert.Null(selection.InventoryHighlight());
        var firstHighlight = selection.TransferInventoryHighlightFor(actionSession.ControlledActorId);
        Assert.Equal(CellHighlightKind.Transfer, firstHighlight?.Kind);
        Assert.Contains("Give", selection.TransferItemSummary(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("debugScrap", selection.TransferItemSummary(), StringComparison.OrdinalIgnoreCase);
        Assert.True(selection.MoveTransferItem(1));
        var secondHighlight = selection.TransferInventoryHighlightFor(actionSession.ControlledActorId);
        Assert.Equal(CellHighlightKind.Transfer, secondHighlight?.Kind);
        Assert.NotEqual(firstHighlight?.Coord, secondHighlight?.Coord);
        var transfer = selection.ConfirmTransfer();

        Assert.NotNull(transfer);
        Assert.True(transfer!.Succeeded, transfer.FailureDetail ?? transfer.FailureReason?.ToString() ?? "unknown");
        Assert.False(selection.IsActive);
        var chestPlaneId = session.World.GetRegisteredInventoryPlaneId(counterpartyId);
        Assert.NotNull(chestPlaneId);
        var chestPlane = session.World.Planes[chestPlaneId.Value];
        var chestOccupants = Enumerable.Range(0, chestPlane.Width)
            .SelectMany(x => Enumerable.Range(0, chestPlane.Height).Select(y => session.World.GetOccupant(new PlaneCoord(chestPlaneId.Value, new GridCoord(x, y)))))
            .ToList();
        Assert.Contains(chestOccupants, occupant => occupant is not null);
    }

    private static void MoveNextToDebugChest(PlayActionSessionController actionSession)
    {
        var inventoryPlane = actionSession.World.GetRegisteredInventoryPlaneId(actionSession.ControlledActorId);
        Assert.NotNull(inventoryPlane);

        var pickupScrap2 = actionSession.SubmitPickup(new EntityId("debugScrap2"), new PlaneCoord(inventoryPlane.Value, new GridCoord(0, 0)));
        Assert.True(pickupScrap2.Succeeded, pickupScrap2.FailureDetail ?? pickupScrap2.FailureReason?.ToString() ?? "unknown");
        var west = actionSession.SubmitMove(Direction.West);
        Assert.True(west.CommandResult.Succeeded, west.CommandResult.FailureDetail ?? west.CommandResult.FailureReason?.ToString() ?? "unknown");
        var pickupScrap1 = actionSession.SubmitPickup(new EntityId("debugScrap1"), new PlaneCoord(inventoryPlane.Value, new GridCoord(1, 0)));
        Assert.True(pickupScrap1.Succeeded, pickupScrap1.FailureDetail ?? pickupScrap1.FailureReason?.ToString() ?? "unknown");
        var westAgain = actionSession.SubmitMove(Direction.West);
        Assert.True(westAgain.CommandResult.Succeeded, westAgain.CommandResult.FailureDetail ?? westAgain.CommandResult.FailureReason?.ToString() ?? "unknown");
        var north = actionSession.SubmitMove(Direction.North);
        Assert.True(north.CommandResult.Succeeded, north.CommandResult.FailureDetail ?? north.CommandResult.FailureReason?.ToString() ?? "unknown");
    }
}
