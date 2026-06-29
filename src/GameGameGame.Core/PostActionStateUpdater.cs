namespace GameGameGame.Core;

public static class PostActionStateUpdater
{
    public static void ApplyFacingFromMovement(WorldState world, EntityId actorId, Direction? actorMovementDirection)
    {
        if (actorMovementDirection is { } direction && world.Entities.ContainsKey(actorId))
        {
            world.SetActionFacing(actorId, direction);
        }
    }
}
