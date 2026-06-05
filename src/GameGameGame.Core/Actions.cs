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

public sealed record PickupAction(EntityId TargetId, PlaneCoord Destination) : IActionIntent
{
    public bool CanExecute(WorldState world, EntityId actorId, MovementService movement)
    {
        if (!world.Entities.TryGetValue(actorId, out var actor)
            || !world.Entities.ContainsKey(TargetId)
            || actor.InventoryPlaneId is not { } inventoryPlaneId
            || Destination.PlaneId != inventoryPlaneId
            || TargetId == actorId)
        {
            return false;
        }

        return movement.AreAdjacent(world, actorId, TargetId)
            && movement.CanPlace(world, Destination);
    }

    public void Execute(WorldState world, EntityId actorId, MovementService movement) =>
        movement.TryPlace(world, TargetId, Destination);
}

public sealed record DropAction(EntityId TargetId, PlaneCoord Destination) : IActionIntent
{
    public bool CanExecute(WorldState world, EntityId actorId, MovementService movement)
    {
        if (!world.Entities.TryGetValue(actorId, out var actor)
            || !world.Entities.ContainsKey(TargetId)
            || actor.InventoryPlaneId is not { } inventoryPlaneId)
        {
            return false;
        }

        var actorLocation = world.GetEntityLocation(actorId);
        var targetLocation = world.GetEntityLocation(TargetId);

        return targetLocation.PlaneId == inventoryPlaneId
            && Destination.PlaneId == actorLocation.PlaneId
            && movement.CanPlace(world, Destination);
    }

    public void Execute(WorldState world, EntityId actorId, MovementService movement) =>
        movement.TryPlace(world, TargetId, Destination);
}
