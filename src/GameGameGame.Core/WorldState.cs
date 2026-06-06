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
}
