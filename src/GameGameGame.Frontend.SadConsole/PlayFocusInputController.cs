using GameGameGame.Core;
using SadConsole.Input;

namespace GameGameGame.Frontend.SadConsole;

internal enum PlayInspectionInputIntentKind
{
    Consume,
    ReturnToGrid,
    PreviousAction,
    NextAction,
    ConfirmAction
}

internal sealed record PlayInspectionInputIntent(PlayInspectionInputIntentKind Kind)
{
    public static PlayInspectionInputIntent Consume { get; } = new(PlayInspectionInputIntentKind.Consume);
    public static PlayInspectionInputIntent ReturnToGrid { get; } = new(PlayInspectionInputIntentKind.ReturnToGrid);
    public static PlayInspectionInputIntent PreviousAction { get; } = new(PlayInspectionInputIntentKind.PreviousAction);
    public static PlayInspectionInputIntent NextAction { get; } = new(PlayInspectionInputIntentKind.NextAction);
    public static PlayInspectionInputIntent ConfirmAction { get; } = new(PlayInspectionInputIntentKind.ConfirmAction);
}

internal static class PlayInspectionInputController
{
    public static PlayInspectionInputIntent Read(Keyboard keyboard) => ReadKeys(keyboard.KeysReleased.Select(key => key.Key));

    public static PlayInspectionInputIntent ReadKeys(IEnumerable<Keys> releasedKeys)
    {
        var keys = releasedKeys.ToHashSet();
        if (keys.Contains(Keys.Escape)) return PlayInspectionInputIntent.ReturnToGrid;
        if (keys.Contains(Keys.Up)) return PlayInspectionInputIntent.PreviousAction;
        if (keys.Contains(Keys.Down)) return PlayInspectionInputIntent.NextAction;
        if (keys.Contains(Keys.Enter) || keys.Contains(Keys.Space)) return PlayInspectionInputIntent.ConfirmAction;
        return PlayInspectionInputIntent.Consume;
    }
}

internal static class PlayInventorySelectionInputController
{
    public static Direction? ReadDirection(Keyboard keyboard) => MovementPreviewKeyboardReader.ReadHeldDirection(keyboard.KeysReleased.Select(key => key.Key));
}
