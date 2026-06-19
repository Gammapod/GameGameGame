namespace GameGameGame.Core;

public sealed class WorldState
{
    private readonly Dictionary<PlaneCoord, NodeId> _nodeByCoord = [];

    public int TurnNumber { get; private set; }

    public TraceNode? LastTrace { get; private set; }

    public Dictionary<EntityId, Entity> Entities { get; } = [];

    public Dictionary<PlaneId, Plane> Planes { get; } = [];

    public Dictionary<NodeId, Node> Nodes { get; } = [];

    public Dictionary<NodeId, EntityId> Occupancy { get; } = [];

    public Dictionary<EntityId, PlaneId> InventoryPlanes { get; } = [];

    public Dictionary<EntityId, EntityActionState> ActionStates { get; } = [];

    public EntityActionState GetOrCreateActionState(EntityId entityId)
    {
        if (!ActionStates.TryGetValue(entityId, out var state))
        {
            state = new EntityActionState();
            ActionStates[entityId] = state;
        }

        return state;
    }

    public void SetActionFacing(EntityId entityId, Direction facing) =>
        GetOrCreateActionState(entityId).Facing = facing;

    public Direction? GetActionFacing(EntityId entityId) =>
        ActionStates.TryGetValue(entityId, out var state) ? state.Facing : null;

    public void SetActionTarget(EntityId entityId, EntityId targetId) =>
        GetOrCreateActionState(entityId).Target = targetId;

    public EntityId? GetActionTarget(EntityId entityId) =>
        ActionStates.TryGetValue(entityId, out var state) ? state.Target : null;

    public NodeId AddNode(PlaneId planeId, GridCoord coord)
    {
        var nodeId = new NodeId($"{planeId}:{coord.X},{coord.Y}");
        var node = new Node(nodeId, planeId, coord);

        Nodes.Add(nodeId, node);
        _nodeByCoord.Add(new PlaneCoord(planeId, coord), nodeId);

        return nodeId;
    }

    public NodeId GetNodeId(PlaneCoord coord) => _nodeByCoord[coord];

    public bool TryGetNodeId(PlaneCoord coord, out NodeId nodeId) => _nodeByCoord.TryGetValue(coord, out nodeId);

    public PlaneCoord GetEntityLocation(EntityId entityId)
    {
        var entity = Entities[entityId];
        var node = Nodes[entity.OccupiedNodeId];

        return new PlaneCoord(node.PlaneId, node.Coord);
    }

    public string FormatEntityAddress(EntityId entityId)
    {
        var entity = Entities[entityId];
        var location = GetEntityLocation(entityId);

        return $"{entity.Name}@{location}";
    }

    public EntityId? GetOccupant(PlaneCoord coord)
    {
        return TryGetNodeId(coord, out var nodeId) && Occupancy.TryGetValue(nodeId, out var entityId)
            ? entityId
            : null;
    }

    public void AdvanceTurn() => TurnNumber++;

    public void RecordTrace(TraceNode trace) => LastTrace = trace;

    public void RegisterInventoryPlane(EntityId entityId, PlaneId planeId) => InventoryPlanes[entityId] = planeId;

    public PlaneId? GetRegisteredInventoryPlaneId(EntityId entityId) =>
        InventoryPlanes.TryGetValue(entityId, out var planeId) ? planeId : null;

    public PlaneId? GetInventoryPlaneId(EntityId entityId)
    {
        if (!Entities.TryGetValue(entityId, out var entity) || !entity.HasUsableInventory)
        {
            return null;
        }

        return InventoryPlanes.TryGetValue(entityId, out var planeId) ? planeId : null;
    }

    public IReadOnlyList<EntityId> DestroyEntityRecursive(EntityId entityId)
    {
        var destroyed = new List<EntityId>();
        DestroyEntityRecursive(entityId, destroyed, []);
        return destroyed;
    }

    private void DestroyEntityRecursive(EntityId entityId, List<EntityId> destroyed, HashSet<EntityId> visited)
    {
        if (!visited.Add(entityId) || !Entities.TryGetValue(entityId, out var entity))
        {
            return;
        }

        if (InventoryPlanes.TryGetValue(entityId, out var inventoryPlaneId))
        {
            var contained = Occupancy
                .Where(entry => Nodes.TryGetValue(entry.Key, out var node) && node.PlaneId == inventoryPlaneId)
                .Select(entry => entry.Value)
                .ToList();

            foreach (var containedEntityId in contained)
            {
                DestroyEntityRecursive(containedEntityId, destroyed, visited);
            }

            RemovePlane(inventoryPlaneId);
            InventoryPlanes.Remove(entityId);
        }

        Occupancy.Remove(entity.OccupiedNodeId);
        Entities.Remove(entityId);
        ActionStates.Remove(entityId);
        destroyed.Add(entityId);
    }

    private void RemovePlane(PlaneId planeId)
    {
        var nodes = Nodes.Values
            .Where(node => node.PlaneId == planeId)
            .ToList();

        foreach (var node in nodes)
        {
            Occupancy.Remove(node.Id);
            Nodes.Remove(node.Id);
            _nodeByCoord.Remove(new PlaneCoord(node.PlaneId, node.Coord));
        }

        Planes.Remove(planeId);
    }
}
