namespace GameGameGame.Core;

public interface IEntityActionPlan
{
    PlannedActionPlan PlanTurn(WorldState world, EntityId entityId, MovementService movement);
}
