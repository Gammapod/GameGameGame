namespace GameGameGame.Core;

public abstract record MovementDestination
{
    private MovementDestination()
    {
    }

    public static MovementDestination Plane(PlaneCoord coord) => new PlaneMovementDestination(coord);

    public static MovementDestination InventorySlot(EntityId ownerId, GridCoord coord) => new InventorySlotMovementDestination(ownerId, coord);

    public static MovementDestination AdjacentTo(EntityId anchorId, Direction direction) => new AdjacentMovementDestination(anchorId, direction);

    public sealed record PlaneMovementDestination(PlaneCoord Coord) : MovementDestination;

    public sealed record InventorySlotMovementDestination(EntityId OwnerId, GridCoord Coord) : MovementDestination;

    public sealed record AdjacentMovementDestination(EntityId AnchorId, Direction Direction) : MovementDestination;
}

public sealed record RelocationEvaluation(
    bool CanRelocate,
    PlaneCoord? Destination,
    TraceNode Trace,
    TopologyNodeId? DestinationNodeId = null,
    TopologyEdgeKind? EdgeKind = null);
