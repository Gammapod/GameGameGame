namespace GameGameGame.Core;

public sealed class TurnService(MovementService movement, IReadOnlyDictionary<EntityId, IEntityBehavior> behaviors)
{
    public bool TakePlayerTurn(WorldState world, PlannedActionPlan playerPlan)
    {
        var acted = ResolvePlan(world, WorldBuilder.PlayerId, playerPlan);

        AdvanceAfterPlayerTurn(world);

        return acted;
    }

    public void AdvanceAfterPlayerTurn(WorldState world)
    {
        world.AdvanceTurn();

        foreach (var (entityId, behavior) in behaviors)
        {
            if (world.Entities.ContainsKey(entityId))
            {
                ResolvePlan(world, entityId, behavior.PlanTurn(world, entityId));
            }
        }
    }

    public bool ResolvePlan(WorldState world, EntityId actorId, PlannedActionPlan plan)
    {
        foreach (var option in plan.Options)
        {
            if (option.CanExecute(world, actorId, movement))
            {
                option.Execute(world, actorId, movement);
                return true;
            }
        }

        return false;
    }
}
