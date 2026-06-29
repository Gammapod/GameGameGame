using GameGameGame.Core;

namespace GameGameGame.ConsoleApp;

public enum ConsolePlayerCommand
{
    None,
    Enter,
    Exit
}

public static class ConsolePlayerControls
{
    public static ConsolePlayerCommand GetCommand(ConsoleKey key) => key switch
    {
        ConsoleKey.E => ConsolePlayerCommand.Enter,
        ConsoleKey.X => ConsolePlayerCommand.Exit,
        _ => ConsolePlayerCommand.None
    };

    public static IActionIntent CreateEnterAction(EntityId targetId) => new EnterAction(targetId);

    public static IActionIntent CreateExitAction(Direction direction) => new ExitAction(direction);
}
