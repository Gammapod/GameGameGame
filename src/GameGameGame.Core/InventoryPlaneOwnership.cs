namespace GameGameGame.Core;

public static class InventoryPlaneOwnership
{
    public static bool TryFindOwner(WorldState world, PlaneId inventoryPlaneId, out EntityId ownerId)
    {
        foreach (var (entityId, planeId) in world.InventoryPlanes)
        {
            if (planeId == inventoryPlaneId && world.Entities.ContainsKey(entityId))
            {
                ownerId = entityId;
                return true;
            }
        }

        ownerId = default;
        return false;
    }
}
