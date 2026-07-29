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
        Direction.NorthEast => new GridCoord(X + 1, Y - 1),
        Direction.SouthEast => new GridCoord(X + 1, Y + 1),
        Direction.South => this with { Y = Y + 1 },
        Direction.SouthWest => new GridCoord(X - 1, Y + 1),
        Direction.West => this with { X = X - 1 },
        Direction.NorthWest => new GridCoord(X - 1, Y - 1),
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
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest
}

public enum EntityControlSource
{
    Automatic,
    PlayerChoice
}

public enum EntityEnterPolicy
{
    FirstUnoccupiedRowMajor,
    FarthestFromOccupied
}

public enum EntityExitPolicy
{
    AnyCell,
    EdgeAlignedWithExitDirection
}

public enum EntityTopologyPolicy
{
    None,
    ConnectsInward,
    ConnectsOutward,
    ConnectsInwardAndOutward
}

public sealed record RuntimeEntityTemplate(
    string TemplateId,
    string Name,
    int InventoryWidth,
    int InventoryHeight,
    int Bulk,
    int Aperture,
    ActionPlanId? DefaultActionPlanId = null,
    Direction? InitialFacing = null,
    EntityEnterPolicy? EnterPolicy = null,
    EntityExitPolicy? ExitPolicy = null,
    EntityTopologyPolicy TopologyPolicy = EntityTopologyPolicy.None);

public static class DirectionMath
{
    public static Direction[] AllDirections { get; } =
    [
        Direction.North,
        Direction.NorthEast,
        Direction.East,
        Direction.SouthEast,
        Direction.South,
        Direction.SouthWest,
        Direction.West,
        Direction.NorthWest
    ];

    public static Direction Rotate(Direction direction, int eighthTurns)
    {
        var index = AllDirections.IndexOf(direction);
        if (index < 0)
        {
            return direction;
        }

        var next = ((index + eighthTurns) % AllDirections.Length + AllDirections.Length) % AllDirections.Length;
        return AllDirections[next];
    }

    public static Direction Reverse(Direction direction) => Rotate(direction, 4);

    public static (Direction First, Direction Second)? OrthogonalCorners(Direction diagonal) => diagonal switch
    {
        Direction.NorthEast => (Direction.North, Direction.East),
        Direction.SouthEast => (Direction.South, Direction.East),
        Direction.SouthWest => (Direction.South, Direction.West),
        Direction.NorthWest => (Direction.North, Direction.West),
        _ => null
    };
}

public sealed record Entity(
    EntityId Id,
    string Name,
    NodeId OccupiedNodeId,
    int InventoryWidth,
    int InventoryHeight,
    int Bulk,
    int Aperture,
    EntityEnterPolicy? EnterPolicy = null,
    EntityExitPolicy? ExitPolicy = null,
    EntityTopologyPolicy TopologyPolicy = EntityTopologyPolicy.None,
    string? TemplateId = null)
{
    public bool HasUsableInventory => InventoryWidth > 0 && InventoryHeight > 0;

    public EntityEnterPolicy EffectiveEnterPolicy => EnterPolicy ?? EntityEnterPolicy.FirstUnoccupiedRowMajor;

    public EntityExitPolicy EffectiveExitPolicy => ExitPolicy ?? EntityExitPolicy.AnyCell;
}

public sealed class EntityActionState
{
    public Direction? Facing { get; set; }

    public EntityControlSource ControlSource { get; set; } = EntityControlSource.Automatic;

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
