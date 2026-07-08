namespace GameGameGame.Core;

public readonly record struct EntityId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct PlaneId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct NodeId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct GridCoord(int X, int Y)
{
    public GridCoord Offset(Direction direction) => direction switch
    {
        Direction.North => this with { Y = Y - 1 },
        Direction.South => this with { Y = Y + 1 },
        Direction.West => this with { X = X - 1 },
        Direction.East => this with { X = X + 1 },
        _ => this
    };

    public override string ToString() => $"({X},{Y})";
}

public readonly record struct PlaneCoord(PlaneId PlaneId, GridCoord Coord)
{
    public override string ToString() => $"{PlaneId}{Coord}";
}

public enum Direction
{
    North,
    South,
    East,
    West
}

public sealed record Entity(
    EntityId Id,
    string Name,
    NodeId OccupiedNodeId,
    int InventoryWidth,
    int InventoryHeight,
    int Bulk,
    int Aperture)
{
    public bool HasUsableInventory => InventoryWidth > 0 && InventoryHeight > 0;
}

public sealed class EntityActionState
{
    public Direction? Facing { get; set; }

    public EntityId? Target { get; set; }

    public Dictionary<int, EntityId> Targets { get; } = [];

    public Dictionary<string, EntityId> LabeledTargets { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<ActionPlanOverrideSlot, PlannedActionPlan> ActionPlanOverrides { get; } = [];
}

public sealed record Plane(
    PlaneId Id,
    string Name,
    int Width,
    int Height)
{
    public bool Contains(GridCoord coord) =>
        coord.X >= 0 && coord.Y >= 0 && coord.X < Width && coord.Y < Height;
}

public sealed record Node(
    NodeId Id,
    PlaneId PlaneId,
    GridCoord Coord);
