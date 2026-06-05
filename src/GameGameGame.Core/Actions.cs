namespace GameGameGame.Core;

public interface IActionIntent
{
    bool CanExecute(WorldState world, EntityId actorId, MovementService movement);

    void Execute(WorldState world, EntityId actorId, MovementService movement);
}

public sealed record PlannedActionPlan(IReadOnlyList<IActionIntent> Options)
{
    public static PlannedActionPlan Single(IActionIntent action) => new([action]);
}

public sealed record MoveAction(Direction Direction) : IActionIntent
{
    public bool CanExecute(WorldState world, EntityId actorId, MovementService movement) =>
        movement.CanMove(world, actorId, Direction);

    public void Execute(WorldState world, EntityId actorId, MovementService movement) =>
        movement.TryMove(world, actorId, Direction);
}

public sealed record WaitAction : IActionIntent
{
    public bool CanExecute(WorldState world, EntityId actorId, MovementService movement) => true;

    public void Execute(WorldState world, EntityId actorId, MovementService movement)
    {
    }
}
