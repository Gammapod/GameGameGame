namespace GameGameGame.Core;

public interface IEntityBehavior
{
    PlannedActionPlan PlanTurn(WorldState world, EntityId entityId, MovementService movement);
}

public sealed class AlternatingHorizontalBehavior : IEntityBehavior
{
    public PlannedActionPlan PlanTurn(WorldState world, EntityId entityId, MovementService movement)
    {
        var direction = world.TurnNumber % 2 == 1
            ? Direction.East
            : Direction.West;

        if (movement.CanMove(world, entityId, direction))
        {
            return PlannedActionPlan.Single(new MoveAction(direction));
        }

        var entity = world.Entities[entityId];

        if (entity.InventoryPlaneId is { } inventoryPlaneId
            && movement.GetBlockingEntity(world, entityId, direction) is { } blockingEntityId)
        {
            return PlannedActionPlan.Single(new PickupAction(
                blockingEntityId,
                new PlaneCoord(inventoryPlaneId, new GridCoord(0, 0))));
        }

        return PlannedActionPlan.Single(new WaitAction());
    }
}
