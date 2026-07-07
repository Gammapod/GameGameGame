namespace GameGameGame.Core;

public enum ActionPlanOverrideSlot
{
    Pre,
    Main,
    Post
}

public interface IEntityActionPlan
{
    PlannedActionPlan PlanTurn(WorldState world, EntityId entityId, MovementService movement);
}
