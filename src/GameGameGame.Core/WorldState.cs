namespace GameGameGame.Core;

public sealed class WorldState
{
    private readonly Dictionary<PlaneCoord, NodeId> _nodeByCoord = [];

    public int TurnNumber { get; private set; }

    public TraceNode? LastTrace { get; private set; }

    public SimulationTurnReport? LastTurnReport { get; private set; }

    public Dictionary<EntityId, Entity> Entities { get; } = [];

    public Dictionary<PlaneId, Plane> Planes { get; } = [];

    public Dictionary<NodeId, Node> Nodes { get; } = [];

    public Dictionary<NodeId, EntityId> Occupancy { get; } = [];

    public Dictionary<EntityId, PlaneId> InventoryPlanes { get; } = [];

    public Dictionary<EntityId, EntityActionState> ActionStates { get; } = [];

    public WorldState Clone()
    {
        var clone = new WorldState();
        clone.CopyFrom(this);
        return clone;
    }

    public void RestoreFrom(WorldState snapshot) => CopyFrom(snapshot);

    private void CopyFrom(WorldState source)
    {
        TurnNumber = source.TurnNumber;
        LastTrace = CloneTrace(source.LastTrace);
        LastTurnReport = CloneTurnReport(source.LastTurnReport);

        Entities.Clear();
        foreach (var (id, entity) in source.Entities)
        {
            Entities[id] = entity;
        }

        Planes.Clear();
        foreach (var (id, plane) in source.Planes)
        {
            Planes[id] = plane;
        }

        Nodes.Clear();
        _nodeByCoord.Clear();
        foreach (var (id, node) in source.Nodes)
        {
            Nodes[id] = node;
            _nodeByCoord[new PlaneCoord(node.PlaneId, node.Coord)] = id;
        }

        Occupancy.Clear();
        foreach (var (nodeId, entityId) in source.Occupancy)
        {
            Occupancy[nodeId] = entityId;
        }

        InventoryPlanes.Clear();
        foreach (var (entityId, planeId) in source.InventoryPlanes)
        {
            InventoryPlanes[entityId] = planeId;
        }

        ActionStates.Clear();
        foreach (var (entityId, state) in source.ActionStates)
        {
            ActionStates[entityId] = CloneActionState(state);
        }
    }

    private static EntityActionState CloneActionState(EntityActionState source)
    {
        var clone = new EntityActionState
        {
            Facing = source.Facing,
            ControlSource = source.ControlSource,
            Target = source.Target
        };

        foreach (var (slot, targetId) in source.Targets)
        {
            clone.Targets[slot] = targetId;
        }

        foreach (var (label, targetId) in source.LabeledTargets)
        {
            clone.LabeledTargets[label] = targetId;
        }

        foreach (var (slot, plan) in source.ActionPlanOverrides)
        {
            clone.ActionPlanOverrides[slot] = plan;
        }

        return clone;
    }

    private static TraceNode? CloneTrace(TraceNode? source)
    {
        if (source is null)
        {
            return null;
        }

        var clone = new TraceNode(source.Label, source.Status, source.Reason, source.Detail);
        foreach (var child in source.Children)
        {
            clone.Add(CloneTrace(child)!);
        }

        return clone;
    }

    private static SimulationTurnReport? CloneTurnReport(SimulationTurnReport? source)
    {
        if (source is null)
        {
            return null;
        }

        return new SimulationTurnReport(
            source.TurnNumber,
            source.Actions
                .Select(action => new TurnActionReport(
                    action.ActorId,
                    action.ActorName,
                    action.Succeeded,
                    action.ConsumedTurn,
                    action.Summary,
                    CloneTrace(action.Trace)!))
                .ToList());
    }

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

    public void SetActionControlSource(EntityId entityId, EntityControlSource controlSource) =>
        GetOrCreateActionState(entityId).ControlSource = controlSource;

    public EntityControlSource GetActionControlSource(EntityId entityId) =>
        ActionStates.TryGetValue(entityId, out var state) ? state.ControlSource : EntityControlSource.Automatic;

    public void SetActionTarget(EntityId entityId, EntityId targetId) =>
        GetOrCreateActionState(entityId).Target = targetId;

    public EntityId? GetActionTarget(EntityId entityId) =>
        ActionStates.TryGetValue(entityId, out var state) ? state.Target : null;

    public void SetActionTarget(EntityId entityId, int slot, EntityId targetId)
    {
        var state = GetOrCreateActionState(entityId);
        state.Targets[slot] = targetId;

        if (slot == 1)
        {
            state.Target = targetId;
        }
    }

    public void SetActionTarget(EntityId entityId, string label, EntityId targetId)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return;
        }

        GetOrCreateActionState(entityId).LabeledTargets[label] = targetId;
    }

    public void ClearActionTarget(EntityId entityId, int slot)
    {
        if (!ActionStates.TryGetValue(entityId, out var state))
        {
            return;
        }

        state.Targets.Remove(slot);

        if (slot == 1)
        {
            state.Target = null;
        }
    }

    public void ClearActionTarget(EntityId entityId, string label)
    {
        if (string.IsNullOrWhiteSpace(label) || !ActionStates.TryGetValue(entityId, out var state))
        {
            return;
        }

        state.LabeledTargets.Remove(label);
    }

    public EntityId? GetActionTarget(EntityId entityId, int slot)
    {
        if (!ActionStates.TryGetValue(entityId, out var state))
        {
            return null;
        }

        if (state.Targets.TryGetValue(slot, out var targetId))
        {
            return targetId;
        }

        return slot == 1 ? state.Target : null;
    }

    public EntityId? GetActionTarget(EntityId entityId, string label)
    {
        if (string.IsNullOrWhiteSpace(label) || !ActionStates.TryGetValue(entityId, out var state))
        {
            return null;
        }

        return state.LabeledTargets.TryGetValue(label, out var targetId) ? targetId : null;
    }

    public void SetActionPlanOverride(EntityId entityId, ActionPlanOverrideSlot slot, PlannedActionPlan plan) =>
        GetOrCreateActionState(entityId).ActionPlanOverrides[slot] = plan;

    public PlannedActionPlan? GetActionPlanOverride(EntityId entityId, ActionPlanOverrideSlot slot) =>
        ActionStates.TryGetValue(entityId, out var state) && state.ActionPlanOverrides.TryGetValue(slot, out var plan)
            ? plan
            : null;

    public IReadOnlyDictionary<ActionPlanOverrideSlot, PlannedActionPlan> SnapshotActionPlanOverrides(EntityId entityId)
    {
        if (!ActionStates.TryGetValue(entityId, out var state))
        {
            return new Dictionary<ActionPlanOverrideSlot, PlannedActionPlan>();
        }

        return new Dictionary<ActionPlanOverrideSlot, PlannedActionPlan>(state.ActionPlanOverrides);
    }

    public void ClearActionPlanOverride(EntityId entityId, ActionPlanOverrideSlot slot)
    {
        if (ActionStates.TryGetValue(entityId, out var state))
        {
            state.ActionPlanOverrides.Remove(slot);
        }
    }

    public void ClearMatchingActionPlanOverrides(
        EntityId entityId,
        IReadOnlyDictionary<ActionPlanOverrideSlot, PlannedActionPlan> overrides)
    {
        if (!ActionStates.TryGetValue(entityId, out var state))
        {
            return;
        }

        foreach (var (slot, plan) in overrides)
        {
            if (state.ActionPlanOverrides.TryGetValue(slot, out var current) && ReferenceEquals(current, plan))
            {
                state.ActionPlanOverrides.Remove(slot);
            }
        }
    }

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

    public void RecordTurnReport(SimulationTurnReport report) => LastTurnReport = report;

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
