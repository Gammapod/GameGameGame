using GameGameGame.Core;

namespace GameGameGame.Content;

internal sealed class TargetingDistanceService
{
    public IReadOnlyDictionary<PlaneCoord, int> GetOctagonalDistances(WorldState world, PlaneCoord origin, int maxDistance)
    {
        if (maxDistance < 0)
        {
            return new Dictionary<PlaneCoord, int>();
        }

        var graph = TopologyGraphMaterializer.Materialize(world);
        if (!graph.TryGetNode(new TopologyCellRef(origin), out var originNode))
        {
            return new Dictionary<PlaneCoord, int>();
        }

        return TopologyGraphTraversalService.OctagonalDistanceFlood(graph, originNode.Id, maxDistance)
            .GroupBy(step => step.SourceCoord)
            .ToDictionary(group => group.Key, group => group.Min(step => step.Distance));
    }
}
