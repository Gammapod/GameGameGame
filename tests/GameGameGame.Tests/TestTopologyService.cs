using GameGameGame.Core;

namespace GameGameGame.Tests;

internal sealed class OverrideNeighborTopologyService(PlaneCoord origin, Direction direction, PlaneCoord destination) : ITopologyService
{
    private readonly DefaultTopologyService inner = new();

    public bool TryGetNeighbor(WorldState world, PlaneCoord candidateOrigin, Direction candidateDirection, out TopologyNeighbor neighbor)
    {
        if (candidateOrigin == origin && candidateDirection == direction)
        {
            neighbor = new TopologyNeighbor(destination, direction, TopologyEdgeKind.DefaultGrid, false, null, null);
            return true;
        }

        return inner.TryGetNeighbor(world, candidateOrigin, candidateDirection, out neighbor);
    }

    public IReadOnlyList<TopologyNeighbor> GetNeighbors(WorldState world, PlaneCoord candidateOrigin) =>
        DirectionMath.AllDirections.Select(candidateDirection =>
        {
            TryGetNeighbor(world, candidateOrigin, candidateDirection, out var neighbor);
            return neighbor;
        }).ToList();

    public AdjacencyEvaluation EvaluateAdjacency(WorldState world, PlaneCoord first, PlaneCoord second)
    {
        if (first == origin && second == destination)
        {
            return new AdjacencyEvaluation(
                AreAdjacent: true,
                Direction: direction,
                IsIntercardinal: DirectionMath.OrthogonalCorners(direction) is not null,
                FailureReason: null,
                FailureDetail: null);
        }

        return inner.EvaluateAdjacency(world, first, second);
    }
}
