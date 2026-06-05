namespace GameGameGame.Core;

public sealed class MovementService
{
    public bool CanMove(WorldState world, EntityId entityId, Direction direction)
    {
        var entity = world.Entities[entityId];
        var currentNode = world.Nodes[entity.OccupiedNodeId];
        var plane = world.Planes[currentNode.PlaneId];
        var destinationCoord = currentNode.Coord.Offset(direction);

        if (!plane.Contains(destinationCoord))
        {
            return false;
        }

        var destinationNodeId = world.GetNodeId(new PlaneCoord(currentNode.PlaneId, destinationCoord));

        return !world.Occupancy.ContainsKey(destinationNodeId);
    }

    public bool TryMove(WorldState world, EntityId entityId, Direction direction)
    {
        var entity = world.Entities[entityId];
        var currentNode = world.Nodes[entity.OccupiedNodeId];
        var destinationCoord = currentNode.Coord.Offset(direction);

        if (!CanMove(world, entityId, direction))
        {
            return false;
        }

        var destinationNodeId = world.GetNodeId(new PlaneCoord(currentNode.PlaneId, destinationCoord));

        world.Occupancy.Remove(entity.OccupiedNodeId);
        world.Occupancy[destinationNodeId] = entityId;
        world.Entities[entityId] = entity with { OccupiedNodeId = destinationNodeId };

        return true;
    }
}
