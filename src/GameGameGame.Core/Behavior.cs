namespace GameGameGame.Core;

public interface IEntityBehavior
{
    PlannedActionPlan PlanTurn(WorldState world, EntityId entityId);
}

public sealed class AlternatingHorizontalBehavior : IEntityBehavior
{
    public PlannedActionPlan PlanTurn(WorldState world, EntityId entityId)
    {
        var direction = world.TurnNumber % 2 == 1
            ? Direction.East
            : Direction.West;

        return PlannedActionPlan.Single(new MoveAction(direction));
    }
}
