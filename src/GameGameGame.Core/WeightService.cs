namespace GameGameGame.Core;

public sealed class WeightService
{
    public int GetTotalWeight(WorldState world, EntityId entityId) =>
        GetTotalWeight(world, entityId, []);

    public int GetCarriedWeight(WorldState world, EntityId entityId) =>
        GetCarriedWeight(world, entityId, []);

    public bool CanCarry(WorldState world, EntityId actorId, EntityId targetId) =>
        GetCarriedWeight(world, actorId) + GetTotalWeight(world, targetId) <= world.Entities[actorId].CarryingCapacity;

    private int GetTotalWeight(WorldState world, EntityId entityId, HashSet<EntityId> visited)
    {
        if (!visited.Add(entityId))
        {
            return 0;
        }

        return world.Entities[entityId].Weight + GetCarriedWeight(world, entityId, visited);
    }

    private int GetCarriedWeight(WorldState world, EntityId entityId, HashSet<EntityId> visited)
    {
        var entity = world.Entities[entityId];

        if (entity.InventoryPlaneId is not { } inventoryPlaneId)
        {
            return 0;
        }

        var total = 0;

        foreach (var (nodeId, occupantId) in world.Occupancy)
        {
            if (world.Nodes[nodeId].PlaneId == inventoryPlaneId)
            {
                total += GetTotalWeight(world, occupantId, visited);
            }
        }

        return total;
    }
}
