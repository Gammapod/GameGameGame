using GameGameGame.Core;

namespace GameGameGame.Frontend.SadConsole;

internal sealed record PlayMovementResult(
    ControlledActorCommandResult CommandResult,
    GridCoord BeforeCoord,
    GridCoord AfterCoord,
    bool UsedCoreActionChoice = false)
{
    public bool MovedOneCell => CommandResult.Succeeded
        && BeforeCoord != AfterCoord
        && Math.Abs(AfterCoord.X - BeforeCoord.X) <= 1
        && Math.Abs(AfterCoord.Y - BeforeCoord.Y) <= 1;
}

internal sealed class PlayMovementController(PlayActionSessionController actionSession)
{
    public PlayMovementResult Move(Direction direction) => actionSession.SubmitMove(direction);

    public PlayMovementResult MoveAndDeferRefresh(Direction direction) => actionSession.SubmitMoveAndDeferRefresh(direction);

    public void CompletePendingPostSubmitRefresh() => actionSession.CompletePendingPostSubmitRefresh();
}
