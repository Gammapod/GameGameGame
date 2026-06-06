namespace GameGameGame.Core;

public sealed class WeightService
{
    public int GetTotalWeight(WorldState world, EntityId entityId) =>
        GetTotalWeight(world, entityId, []);

    public int GetCarriedWeight(WorldState world, EntityId entityId) =>
        GetCarriedWeight(world, entityId, []);

    public bool CanCarry(WorldState world, EntityId actorId, EntityId targetId) =>
        GetCarriedWeight(world, actorId) + GetTotalWeight(world, targetId) <= world.Entities[actorId].CarryingCapacity;

    public TraceNode TraceTotalWeight(WorldState world, EntityId entityId)
    {
        var (weight, trace) = TraceTotalWeight(world, entityId, []);
        trace.Detail = $"total={weight}";

        return trace;
    }

    private (int Weight, TraceNode Trace) TraceTotalWeight(WorldState world, EntityId entityId, HashSet<EntityId> visited)
    {
        if (!visited.Add(entityId))
        {
            return (0, TraceNode.Info($"{world.Entities[entityId].Name} already counted", "cycle guard: contributes 0"));
        }

        var entity = world.Entities[entityId];
        var trace = TraceNode.Info($"Total weight of {entity.Name}");
        trace.Add(TraceNode.Info("Base weight", entity.Weight.ToString()));

        var carried = 0;

        if (world.GetInventoryPlaneId(entityId) is { } inventoryPlaneId)
        {
            var carriedTrace = TraceNode.Info($"Carried entities in {inventoryPlaneId}");

            foreach (var (nodeId, occupantId) in world.Occupancy)
            {
                if (world.Nodes[nodeId].PlaneId == inventoryPlaneId)
                {
                    var (childWeight, childTrace) = TraceTotalWeight(world, occupantId, visited);
                    carried += childWeight;
                    carriedTrace.Add(childTrace);
                }
            }

            carriedTrace.Detail = $"carried={carried}";
            trace.Add(carriedTrace);
        }

        var total = entity.Weight + carried;
        trace.Detail = $"total={total}";

        return (total, trace);
    }

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

        if (world.GetInventoryPlaneId(entityId) is not { } inventoryPlaneId)
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
