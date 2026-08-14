using GameGameGame.Core;
using SadConsole.Input;

namespace GameGameGame.Frontend.SadConsole;

internal sealed class MovementPreviewState
{
    public Direction? Direction { get; private set; }

    public bool HasPreview => Direction is not null;

    public void Set(Direction direction) => Direction = direction;

    public void Clear() => Direction = null;

    public bool TryDestination(GridCoord origin, out GridCoord destination)
    {
        if (Direction is { } direction)
        {
            destination = origin.Offset(direction);
            return true;
        }

        destination = default;
        return false;
    }
}

internal static class MovementPreviewKeyboardReader
{
    public static Direction? ReadHeldDirection(IEnumerable<Keys> keys)
    {
        var set = keys.ToHashSet();

        if (set.Contains(Keys.NumPad7)) return Direction.NorthWest;
        if (set.Contains(Keys.NumPad8)) return Direction.North;
        if (set.Contains(Keys.NumPad9)) return Direction.NorthEast;
        if (set.Contains(Keys.NumPad4)) return Direction.West;
        if (set.Contains(Keys.NumPad6)) return Direction.East;
        if (set.Contains(Keys.NumPad1)) return Direction.SouthWest;
        if (set.Contains(Keys.NumPad2)) return Direction.South;
        if (set.Contains(Keys.NumPad3)) return Direction.SouthEast;

        var x = (set.Contains(Keys.Right) || set.Contains(Keys.D) ? 1 : 0)
            + (set.Contains(Keys.Left) || set.Contains(Keys.A) ? -1 : 0);
        var y = (set.Contains(Keys.Down) || set.Contains(Keys.S) ? 1 : 0)
            + (set.Contains(Keys.Up) || set.Contains(Keys.W) ? -1 : 0);

        return (x, y) switch
        {
            (0, -1) => Direction.North,
            (1, -1) => Direction.NorthEast,
            (1, 0) => Direction.East,
            (1, 1) => Direction.SouthEast,
            (0, 1) => Direction.South,
            (-1, 1) => Direction.SouthWest,
            (-1, 0) => Direction.West,
            (-1, -1) => Direction.NorthWest,
            _ => null
        };
    }

    public static bool IsConfirmReleased(Keyboard keyboard) =>
        keyboard.IsKeyReleased(Keys.Space) || keyboard.IsKeyReleased(Keys.Enter);

    public static bool IsMovementKey(Keys key) => key is
        Keys.NumPad7 or Keys.NumPad8 or Keys.NumPad9 or
        Keys.NumPad4 or Keys.NumPad6 or
        Keys.NumPad1 or Keys.NumPad2 or Keys.NumPad3 or
        Keys.Up or Keys.Down or Keys.Left or Keys.Right or
        Keys.W or Keys.A or Keys.S or Keys.D;
}

internal static class MovementPreviewConfirmation
{
    public static Direction? ResolveDirection(MovementPreviewState preview, Direction? currentFacing) =>
        preview.Direction ?? currentFacing;

    public static Direction? ResolveAfterApplyingHeldDirection(MovementPreviewState preview, Direction? heldDirection, Direction? currentFacing)
    {
        if (heldDirection is { } direction)
        {
            preview.Set(direction);
        }

        return ResolveDirection(preview, currentFacing);
    }
}
