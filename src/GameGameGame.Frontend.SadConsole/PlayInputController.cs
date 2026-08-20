using GameGameGame.Core;
using SadConsole.Input;

namespace GameGameGame.Frontend.SadConsole;

internal enum PlayControlIntentKind
{
    None,
    Cancel,
    TogglePlayerPanel,
    AimMove,
    ConfirmMove,
    ClearMoveAim
}

internal sealed record PlayControlIntent(PlayControlIntentKind Kind, Direction? Direction = null)
{
    public static PlayControlIntent None { get; } = new(PlayControlIntentKind.None);
    public static PlayControlIntent Cancel { get; } = new(PlayControlIntentKind.Cancel);
    public static PlayControlIntent TogglePlayerPanel { get; } = new(PlayControlIntentKind.TogglePlayerPanel);
    public static PlayControlIntent ConfirmMove { get; } = new(PlayControlIntentKind.ConfirmMove);
    public static PlayControlIntent ClearMoveAim { get; } = new(PlayControlIntentKind.ClearMoveAim);
    public static PlayControlIntent AimMove(Direction direction) => new(PlayControlIntentKind.AimMove, direction);
    public static PlayControlIntent ConfirmMoveDirection(Direction direction) => new(PlayControlIntentKind.ConfirmMove, direction);
}

internal static class PlayInputController
{
    public static PlayControlIntent Read(Keyboard keyboard, bool hasMovementPreview)
    {
        return ReadKeys(
            keyboard.KeysDown.Select(key => key.Key),
            keyboard.KeysReleased.Select(key => key.Key),
            keyboard.IsKeyReleased(Keys.Escape),
            keyboard.IsKeyReleased(Keys.I),
            MovementPreviewKeyboardReader.IsConfirmReleased(keyboard),
            hasMovementPreview);
    }

    public static PlayControlIntent ReadKeys(
        IEnumerable<Keys> keysDown,
        IEnumerable<Keys> keysReleased,
        bool cancelReleased,
        bool playerPanelToggleReleased,
        bool confirmReleased,
        bool hasMovementPreview)
    {
        if (cancelReleased)
        {
            return PlayControlIntent.Cancel;
        }

        if (playerPanelToggleReleased)
        {
            return PlayControlIntent.TogglePlayerPanel;
        }

        var heldDirection = MovementPreviewKeyboardReader.ReadHeldDirection(keysDown);
        if (confirmReleased)
        {
            return heldDirection is { } direction
                ? PlayControlIntent.ConfirmMoveDirection(direction)
                : PlayControlIntent.ConfirmMove;
        }

        if (heldDirection is { } aimDirection)
        {
            return PlayControlIntent.AimMove(aimDirection);
        }

        return hasMovementPreview && keysReleased.Any(MovementPreviewKeyboardReader.IsMovementKey)
            ? PlayControlIntent.ClearMoveAim
            : PlayControlIntent.None;
    }
}
