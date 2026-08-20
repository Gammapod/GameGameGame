using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class PlayMovementControllerTests
{
    [Fact]
    public void PlayMovementControllerSubmitsDebugRoomMovementThroughPlayActionSessionController()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var actionSession = new PlayActionSessionController(session);
        var controller = new PlayMovementController(actionSession);

        var result = controller.Move(Direction.East);

        Assert.False(result.CommandResult.Succeeded);
        Assert.False(result.CommandResult.AdvancedTurn);
        Assert.True(result.UsedCoreActionChoice);
        Assert.Equal(new GridCoord(4, 3), result.BeforeCoord);
        Assert.Equal(new GridCoord(4, 3), result.AfterCoord);
        Assert.False(result.MovedOneCell);
        Assert.Equal(new GridCoord(4, 3), session.World.GetEntityLocation(session.PlayerEntityId).Coord);
        Assert.NotNull(actionSession.CurrentActionChoiceRequest);
    }

    [Fact]
    public void DeferredMoveAppliesPlayerMoveBeforeCompletingPostSubmitRefresh()
    {
        PlayableScenarioSession? session = null;
        PlayActionSessionController? actionSession = null;
        PlayMovementController? controller = null;
        PlayMovementResult? result = null;
        foreach (var direction in Enum.GetValues<Direction>())
        {
            session = PlayableScenarioLauncher.CreatePrototype();
            actionSession = new PlayActionSessionController(session);
            controller = new PlayMovementController(actionSession);
            result = controller.MoveAndDeferRefresh(direction);
            if (result.CommandResult.Succeeded && result.MovedOneCell)
            {
                break;
            }
        }

        Assert.NotNull(session);
        Assert.NotNull(actionSession);
        Assert.NotNull(controller);
        Assert.NotNull(result);
        Assert.True(result!.CommandResult.Succeeded, result.CommandResult.FailureDetail ?? result.CommandResult.FailureReason?.ToString() ?? "unknown");
        Assert.True(result.MovedOneCell);
        Assert.Equal(result.AfterCoord, session!.World.GetEntityLocation(session.PlayerEntityId).Coord);
        Assert.True(actionSession!.IsPostSubmitRefreshPending);

        controller!.CompletePendingPostSubmitRefresh();

        Assert.False(actionSession.IsPostSubmitRefreshPending);
    }

}
