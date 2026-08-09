using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed record TopologyVisibilityProjection(
    EntityId ObserverEntityId,
    TopologyCellRef Origin,
    int MaxDepth,
    IReadOnlyList<TopologyVisibleCellProjection> VisibleCells,
    IReadOnlyList<TopologyVisibilityDiagnostic> Diagnostics);

public sealed record TopologyVisibleCellProjection(
    TopologyCellRef Cell,
    int Distance,
    TopologyCellRef? From,
    Direction? Direction,
    TopologyEdgeKind? Kind,
    TopologyNodeId? NodeId = null,
    TopologyLayoutCoord? LayoutCoord = null);

public enum TopologyVisibilityDiagnosticCode
{
    ObserverNotFound,
    NegativeDepthNotSupported,
    LineOfSightNotImplemented
}

public sealed record TopologyVisibilityDiagnostic(
    TopologyVisibilityDiagnosticCode Code,
    string Message);

public sealed class TopologyVisibilityProjectionService
{
    public TopologyVisibilityProjection Project(WorldState world, EntityId observerEntityId, int maxDepth)
    {
        if (!world.Entities.ContainsKey(observerEntityId))
        {
            return new TopologyVisibilityProjection(
                observerEntityId,
                new TopologyCellRef(new PlaneCoord(new PlaneId(string.Empty), new GridCoord(0, 0))),
                maxDepth,
                [],
                [new TopologyVisibilityDiagnostic(TopologyVisibilityDiagnosticCode.ObserverNotFound, $"Observer entity {observerEntityId} was not found.")]);
        }

        var origin = new TopologyCellRef(world.GetEntityLocation(observerEntityId));
        if (maxDepth < 0)
        {
            return new TopologyVisibilityProjection(
                observerEntityId,
                origin,
                maxDepth,
                [],
                [new TopologyVisibilityDiagnostic(TopologyVisibilityDiagnosticCode.NegativeDepthNotSupported, "Topology visibility max depth must be non-negative.")]);
        }

        var graph = TopologyGraphMaterializer.Materialize(world);
        if (!graph.TryGetNode(origin, out var originNode))
        {
            return new TopologyVisibilityProjection(
                observerEntityId,
                origin,
                maxDepth,
                [],
                [new TopologyVisibilityDiagnostic(TopologyVisibilityDiagnosticCode.ObserverNotFound, $"Observer entity {observerEntityId} has no topology node at {origin}.")]);
        }

        var visibleCells = TopologyGraphTraversalService.Flood(graph, originNode.Id, maxDepth)
            .Select(step => new TopologyVisibleCellProjection(
                new TopologyCellRef(step.SourceCoord),
                step.Distance,
                step.FromNodeId is null || !graph.TryGetNode(step.FromNodeId.Value, out var fromNode) ? null : new TopologyCellRef(fromNode.SourceCoord),
                step.Direction,
                step.Kind,
                step.NodeId,
                step.LayoutCoord))
            .ToList();

        return new TopologyVisibilityProjection(
            observerEntityId,
            origin,
            maxDepth,
            visibleCells,
            [new TopologyVisibilityDiagnostic(TopologyVisibilityDiagnosticCode.LineOfSightNotImplemented, "Topology visibility currently reports depth-limited topology reachability, not line-of-sight or audibility.")]);
    }
}
