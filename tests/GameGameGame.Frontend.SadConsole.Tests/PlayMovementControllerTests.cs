using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class PlayMovementControllerTests
{
    [Fact]
    public void PlayMovementControllerMovesDebugRoomPlayerThroughSharedCommandService()
    {
        var catalog = TestRepository.BuildDebugRoomCatalog();
        var entry = Assert.Single(catalog.Entries, entry => entry.ScenarioId == "debug-room");
        var session = WorkspaceScenarioCatalogService.Launch(catalog, entry.EntryId);
        var controller = new PlayMovementController(session);

        var result = controller.Move(Direction.East);

        Assert.True(result.CommandResult.Succeeded);
        Assert.True(result.CommandResult.AdvancedTurn);
        Assert.Equal(new GridCoord(4, 3), result.BeforeCoord);
        Assert.Equal(new GridCoord(5, 3), result.AfterCoord);
        Assert.True(result.MovedOneCell);
        Assert.Equal(new GridCoord(5, 3), session.World.GetEntityLocation(session.PlayerEntityId).Coord);
    }

}
