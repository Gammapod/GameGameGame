namespace GameGameGame.Core;

public enum EntityContainmentPathStatus
{
    Complete,
    RequestedEntityNotFound,
    RequestedEntityUnlocated,
    CycleDetected,
    Incomplete,
    Truncated,
    NotUnderRoot,
    NoSharedRoot
}

public sealed record EntityContainmentPath(
    EntityId RequestedEntityId,
    EntityContainmentPathStatus Status,
    IReadOnlyList<EntityContainmentPathSegment> Segments,
    IReadOnlyList<EntityContainmentPathCycle> Cycles,
    IReadOnlyList<string> Diagnostics);

public sealed record EntityContainmentPathSegment(
    EntityId EntityId,
    PlaneId? ContainingPlaneId,
    GridCoord? CoordinateInContainingPlane,
    EntityId? ContainerEntityId);

public sealed record EntityContainmentPathCycle(
    IReadOnlyList<EntityContainmentPathCycleEdge> Edges);

public sealed record EntityContainmentPathCycleEdge(
    EntityId FromEntityId,
    EntityId ToEntityId,
    PlaneId ViaInventoryPlaneId);

public sealed record EntityContainmentSharedPath(
    EntityId FirstEntityId,
    EntityId SecondEntityId,
    EntityId? SharedRootEntityId,
    IReadOnlyList<EntityContainmentPathSegment> SharedRootToFirst,
    IReadOnlyList<EntityContainmentPathSegment> SharedRootToSecond,
    EntityContainmentPathStatus Status,
    IReadOnlyList<EntityContainmentPathCycle> Cycles,
    IReadOnlyList<string> Diagnostics);

public sealed class EntityContainmentPathService
{
    public EntityContainmentSharedPath GetSharedRootPath(WorldState world, EntityId firstEntityId, EntityId secondEntityId, int? maxDepth = null)
    {
        var firstPath = GetUpwardPath(world, firstEntityId, maxDepth);
        var secondPath = GetUpwardPath(world, secondEntityId, maxDepth);
        var cycles = firstPath.Cycles.Concat(secondPath.Cycles).ToList();
        var diagnostics = firstPath.Diagnostics.Concat(secondPath.Diagnostics).ToList();

        if (firstPath.Status != EntityContainmentPathStatus.Complete || secondPath.Status != EntityContainmentPathStatus.Complete)
        {
            return new EntityContainmentSharedPath(
                firstEntityId,
                secondEntityId,
                null,
                [],
                [],
                ChooseSharedPathStatus(firstPath.Status, secondPath.Status),
                cycles,
                diagnostics);
        }

        var sharedRootIndex = FindNearestSharedRootIndex(firstPath.Segments, secondPath.Segments);
        if (sharedRootIndex is null)
        {
            diagnostics.Add($"No shared containment root found for {firstEntityId} and {secondEntityId}.");
            return new EntityContainmentSharedPath(
                firstEntityId,
                secondEntityId,
                null,
                [],
                [],
                EntityContainmentPathStatus.NoSharedRoot,
                cycles,
                diagnostics);
        }

        var index = sharedRootIndex.Value;
        var sharedRootEntityId = firstPath.Segments[index].EntityId;
        return new EntityContainmentSharedPath(
            firstEntityId,
            secondEntityId,
            sharedRootEntityId,
            firstPath.Segments.Skip(index).ToList(),
            secondPath.Segments.Skip(index).ToList(),
            EntityContainmentPathStatus.Complete,
            cycles,
            diagnostics);
    }

    public EntityContainmentPath GetPathFromRoot(WorldState world, EntityId rootEntityId, EntityId entityId, int? maxDepth = null)
    {
        var upwardPath = GetUpwardPath(world, entityId, maxDepth);
        if (upwardPath.Status != EntityContainmentPathStatus.Complete)
        {
            return upwardPath;
        }

        if (upwardPath.Segments.Any(segment => segment.EntityId == rootEntityId))
        {
            var rootIndex = upwardPath.Segments
                .Select((segment, index) => (segment, index))
                .First(item => item.segment.EntityId == rootEntityId)
                .index;

            return upwardPath with
            {
                Segments = upwardPath.Segments.Skip(rootIndex).ToList()
            };
        }

        return upwardPath with
        {
            Status = EntityContainmentPathStatus.NotUnderRoot,
            Diagnostics = upwardPath.Diagnostics
                .Concat([$"Requested entity {entityId} is not contained by root {rootEntityId}."])
                .ToList()
        };
    }

    public EntityContainmentPath GetUpwardPath(WorldState world, EntityId entityId, int? maxDepth = null)
    {
        if (!world.Entities.ContainsKey(entityId))
        {
            return new EntityContainmentPath(
                entityId,
                EntityContainmentPathStatus.RequestedEntityNotFound,
                [],
                [],
                [$"Requested entity {entityId} was not found."]);
        }

        var segmentsLeafToRoot = new List<EntityContainmentPathSegment>();
        var diagnostics = new List<string>();
        var visited = new Dictionary<EntityId, int>();
        var currentEntityId = entityId;
        var truncated = false;

        while (true)
        {
            if (visited.TryGetValue(currentEntityId, out var cycleStartIndex))
            {
                var cycleSegments = segmentsLeafToRoot.Skip(cycleStartIndex).ToList();
                var cycle = BuildCycle(cycleSegments);
                diagnostics.Add($"Cycle detected while resolving containment path for {entityId} at {currentEntityId}.");

                return new EntityContainmentPath(
                    entityId,
                    EntityContainmentPathStatus.CycleDetected,
                    segmentsLeafToRoot.AsEnumerable().Reverse().ToList(),
                    [cycle],
                    diagnostics);
            }

            if (maxDepth is { } depth && segmentsLeafToRoot.Count >= depth)
            {
                truncated = true;
                diagnostics.Add($"Max depth {depth} reached while resolving containment path for {entityId}.");
                break;
            }

            visited.Add(currentEntityId, segmentsLeafToRoot.Count);

            if (!TryCreateSegment(world, currentEntityId, out var segment, out var unlocatedDiagnostic))
            {
                return new EntityContainmentPath(
                    entityId,
                    EntityContainmentPathStatus.RequestedEntityUnlocated,
                    segmentsLeafToRoot.AsEnumerable().Reverse().ToList(),
                    [],
                    [unlocatedDiagnostic ?? $"Requested entity {currentEntityId} is unlocated."]);
            }

            segmentsLeafToRoot.Add(segment);

            if (segment.ContainerEntityId is not { } containerEntityId)
            {
                break;
            }

            currentEntityId = containerEntityId;
        }

        return new EntityContainmentPath(
            entityId,
            truncated ? EntityContainmentPathStatus.Truncated : EntityContainmentPathStatus.Complete,
            segmentsLeafToRoot.AsEnumerable().Reverse().ToList(),
            [],
            diagnostics);
    }

    private static bool TryCreateSegment(
        WorldState world,
        EntityId entityId,
        out EntityContainmentPathSegment segment,
        out string? diagnostic)
    {
        segment = default!;
        diagnostic = null;

        if (!world.Entities.TryGetValue(entityId, out var entity))
        {
            diagnostic = $"Entity {entityId} was not found while resolving containment path.";
            return false;
        }

        if (!world.Nodes.TryGetValue(entity.OccupiedNodeId, out var node))
        {
            diagnostic = $"Entity {entityId} occupies missing node {entity.OccupiedNodeId}.";
            return false;
        }

        var containerEntityId = FindEntityOwningInventoryPlane(world, node.PlaneId);
        segment = new EntityContainmentPathSegment(
            entityId,
            containerEntityId is null ? null : node.PlaneId,
            containerEntityId is null ? null : node.Coord,
            containerEntityId);
        return true;
    }

    private static EntityId? FindEntityOwningInventoryPlane(WorldState world, PlaneId planeId)
    {
        foreach (var (entityId, inventoryPlaneId) in world.InventoryPlanes)
        {
            if (inventoryPlaneId == planeId)
            {
                return entityId;
            }
        }

        return null;
    }

    private static EntityContainmentPathCycle BuildCycle(IReadOnlyList<EntityContainmentPathSegment> cycleSegments)
    {
        var edges = cycleSegments
            .Where(segment => segment.ContainerEntityId is not null && segment.ContainingPlaneId is not null)
            .Select(segment => new EntityContainmentPathCycleEdge(
                segment.ContainerEntityId!.Value,
                segment.EntityId,
                segment.ContainingPlaneId!.Value))
            .ToList();

        return new EntityContainmentPathCycle(edges);
    }

    private static EntityContainmentPathStatus ChooseSharedPathStatus(
        EntityContainmentPathStatus firstStatus,
        EntityContainmentPathStatus secondStatus)
    {
        if (firstStatus == EntityContainmentPathStatus.CycleDetected || secondStatus == EntityContainmentPathStatus.CycleDetected)
        {
            return EntityContainmentPathStatus.CycleDetected;
        }

        return firstStatus != EntityContainmentPathStatus.Complete ? firstStatus : secondStatus;
    }

    private static int? FindNearestSharedRootIndex(
        IReadOnlyList<EntityContainmentPathSegment> firstSegments,
        IReadOnlyList<EntityContainmentPathSegment> secondSegments)
    {
        int? sharedRootIndex = null;
        var sharedLength = Math.Min(firstSegments.Count, secondSegments.Count);
        for (var index = 0; index < sharedLength; index++)
        {
            if (firstSegments[index].EntityId != secondSegments[index].EntityId)
            {
                break;
            }

            sharedRootIndex = index;
        }

        return sharedRootIndex;
    }
}
