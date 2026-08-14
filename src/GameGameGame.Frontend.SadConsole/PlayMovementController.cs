using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Frontend.SadConsole;

internal sealed record PlayMovementResult(
    ControlledActorCommandResult CommandResult,
    GridCoord BeforeCoord,
    GridCoord AfterCoord)
{
    public bool MovedOneCell => CommandResult.Succeeded
        && BeforeCoord != AfterCoord
        && Math.Abs(AfterCoord.X - BeforeCoord.X) <= 1
        && Math.Abs(AfterCoord.Y - BeforeCoord.Y) <= 1;
}

internal sealed class PlayMovementController(PlayableScenarioSession session)
{
    public PlayMovementResult Move(Direction direction)
    {
        var before = session.World.GetEntityLocation(session.PlayerEntityId).Coord;
        var automaticActionPlans = session.ActionPlans
            .Where(pair => pair.Key != session.PlayerEntityId)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var commands = new ControlledActorCommandService(new MovementService(), automaticActionPlans);
        var result = commands.Execute(session.World, session.PlayerEntityId, ControlledActorCommand.Move(direction));
        var after = session.World.Entities.ContainsKey(session.PlayerEntityId)
            ? session.World.GetEntityLocation(session.PlayerEntityId).Coord
            : before;

        return new PlayMovementResult(result, before, after);
    }
}
