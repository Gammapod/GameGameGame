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
    public void ActionHighlightResolverUsesNoActionForGreyedOutRows()
    {
        var row = new EntityInspectionActionRow(FrontendTextMessage.Create(FrontendTextIds.InspectionActionNoValidActions), Selectable: false);

        var kind = PlayActionHighlightResolver.ForInspectionAction(row);

        Assert.Equal(CellHighlightKind.NoAction, kind);
    }

    [Fact]
    public void InventorySelectionConfirmsPickupIntoSelectedPlayerInventoryCell()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var actionSession = new PlayActionSessionController(session);
        var selection = new PlayInventorySelectionController(actionSession);
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
}
