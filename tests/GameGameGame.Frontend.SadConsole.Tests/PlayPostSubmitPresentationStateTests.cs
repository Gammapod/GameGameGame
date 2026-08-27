using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class PlayPostSubmitPresentationStateTests
{
    [Fact]
    public void CloseActionPickersAfterWorldMutationReportsFalseForIdleStateAndClearsMovementPreview()
    {
        var (_, actionSession) = CreateDebugRoomActionSession();
        var selectionStack = new PlaySelectionStack();
        var workflow = new PlayActionWorkflowController(actionSession);
        var movementPreview = new MovementPreviewState();
        movementPreview.Set(Direction.East);

        var closed = PlayPostSubmitPresentationState.CloseActionPickersAfterWorldMutation(selectionStack, workflow, movementPreview);

        Assert.False(closed);
        Assert.Equal(PlaySelectionFrameKind.AdjacentSelection, selectionStack.TopKind);
        Assert.False(workflow.IsActive);
        Assert.False(movementPreview.HasPreview);
    }

    [Fact]
    public void CloseActionPickersAfterWorldMutationClosesActionSelectionStack()
    {
        var (_, actionSession) = CreateDebugRoomActionSession();
        var selectionStack = new PlaySelectionStack();
        var workflow = new PlayActionWorkflowController(actionSession);
        var movementPreview = new MovementPreviewState();
        selectionStack.EnterActionSelection(new GridCoord(2, 3));

        var closed = PlayPostSubmitPresentationState.CloseActionPickersAfterWorldMutation(selectionStack, workflow, movementPreview);

        Assert.True(closed);
        Assert.Equal(PlaySelectionFrameKind.AdjacentSelection, selectionStack.TopKind);
        Assert.Null(selectionStack.LockedAdjacentCoord);
        Assert.False(workflow.IsActive);
    }

    [Fact]
    public void CloseActionPickersAfterWorldMutationClosesActiveWorkflowAndCellSelection()
    {
        var (_, actionSession) = CreateDebugRoomActionSession();
        var selectionStack = new PlaySelectionStack();
        var workflow = new PlayActionWorkflowController(actionSession);
        var movementPreview = new MovementPreviewState();
        Assert.True(workflow.TryBeginPickup(new EntityId("debugScrap3")));
        selectionStack.EnterCellSelection();
        movementPreview.Set(Direction.North);

        var closed = PlayPostSubmitPresentationState.CloseActionPickersAfterWorldMutation(selectionStack, workflow, movementPreview);

        Assert.True(closed);
        Assert.Equal(PlaySelectionFrameKind.AdjacentSelection, selectionStack.TopKind);
        Assert.False(workflow.IsActive);
        Assert.False(movementPreview.HasPreview);
        Assert.Null(workflow.InventoryHighlight());
    }

    private static (PlayableScenarioSession Session, PlayActionSessionController ActionSession) CreateDebugRoomActionSession()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        return (session, new PlayActionSessionController(session));
    }
}
