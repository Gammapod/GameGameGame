namespace GameGameGame.Core;

public sealed class MovementService
{
    public bool AreAdjacent(WorldState world, EntityId firstEntityId, EntityId secondEntityId)
    {
        var first = world.GetEntityLocation(firstEntityId);
        var second = world.GetEntityLocation(secondEntityId);

        return first.PlaneId == second.PlaneId
            && Math.Abs(first.Coord.X - second.Coord.X) + Math.Abs(first.Coord.Y - second.Coord.Y) == 1;
    }

    public bool CanPlace(WorldState world, PlaneCoord destination)
    {
        return world.Planes.TryGetValue(destination.PlaneId, out var plane)
            && plane.Contains(destination.Coord)
            && world.TryGetNodeId(destination, out var nodeId)
            && !world.Occupancy.ContainsKey(nodeId);
    }

    public bool TryPlace(WorldState world, EntityId entityId, PlaneCoord destination)
    {
        if (!CanPlace(world, destination))
        {
            return false;
        }

        var entity = world.Entities[entityId];
        var destinationNodeId = world.GetNodeId(destination);

        world.Occupancy.Remove(entity.OccupiedNodeId);
        world.Occupancy[destinationNodeId] = entityId;
        world.Entities[entityId] = entity with { OccupiedNodeId = destinationNodeId };

        return true;
    }

    public bool CanMove(WorldState world, EntityId entityId, Direction direction)
    {
        var entity = world.Entities[entityId];
        var currentNode = world.Nodes[entity.OccupiedNodeId];
        var plane = world.Planes[currentNode.PlaneId];
        var destinationCoord = currentNode.Coord.Offset(direction);

        return CanPlace(world, new PlaneCoord(currentNode.PlaneId, destinationCoord));
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

        return TryPlace(world, entityId, new PlaneCoord(currentNode.PlaneId, destinationCoord));
    }
}
