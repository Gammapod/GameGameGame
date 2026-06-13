namespace GameGameGame.Core;

public enum MovementTargetKind
{
    Self,
    CanonicalTarget,
    Entity,
    CarriedInventoryCoord
}

public sealed record MovementTargetDescriptor(
    MovementTargetKind Kind,
    EntityId? EntityId = null,
    GridCoord? InventoryCoord = null)
{
    public static MovementTargetDescriptor Self() => new(MovementTargetKind.Self);

    public static MovementTargetDescriptor CanonicalTarget() => new(MovementTargetKind.CanonicalTarget);

    public static MovementTargetDescriptor Entity(EntityId entityId) => new(MovementTargetKind.Entity, EntityId: entityId);

    public static MovementTargetDescriptor CarriedInventoryCoord(GridCoord inventoryCoord) =>
        new(MovementTargetKind.CarriedInventoryCoord, InventoryCoord: inventoryCoord);
}

public enum MovementDestinationKind
{
    PlaneCoord,
    InventorySlot,
    AdjacentToSelf,
    AdjacentToEntity,
    AdjacentToCanonicalTarget
}

public sealed record MovementDestinationDescriptor(
    MovementDestinationKind Kind,
    PlaneCoord? PlaneCoord = null,
    EntityId? OwnerId = null,
    GridCoord? InventoryCoord = null,
    EntityId? AnchorEntityId = null,
    Direction? Direction = null)
{
    public static MovementDestinationDescriptor Plane(PlaneCoord coord) =>
        new(MovementDestinationKind.PlaneCoord, PlaneCoord: coord);

    public static MovementDestinationDescriptor InventorySlot(EntityId ownerId, GridCoord inventoryCoord) =>
        new(MovementDestinationKind.InventorySlot, OwnerId: ownerId, InventoryCoord: inventoryCoord);

    public static MovementDestinationDescriptor AdjacentToSelf(Direction direction) =>
        new(MovementDestinationKind.AdjacentToSelf, Direction: direction);

    public static MovementDestinationDescriptor AdjacentToEntity(EntityId anchorEntityId, Direction direction) =>
        new(MovementDestinationKind.AdjacentToEntity, AnchorEntityId: anchorEntityId, Direction: direction);

    public static MovementDestinationDescriptor AdjacentToCanonicalTarget(Direction direction) =>
        new(MovementDestinationKind.AdjacentToCanonicalTarget, Direction: direction);
}
