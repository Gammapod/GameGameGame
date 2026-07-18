using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsoleEditorCommandMenuControllerTests
{
    [Fact]
    public void OpenIsBlockedWhenEditorSubmodeIsActive()
    {
        var controller = new SadConsoleEditorCommandMenuController();

        var result = controller.Open(inputSubmodeActive: true, entryCount: 3);

        Assert.False(result.Succeeded);
        Assert.False(controller.IsOpen);
        Assert.Contains("active editor submode", result.Message);
    }

    [Fact]
    public void DirectionalMoveClampsSelectionToAvailableCommands()
    {
        var controller = new SadConsoleEditorCommandMenuController();
        controller.Open(inputSubmodeActive: false, entryCount: 3);

        controller.MoveSelection(10, entryCount: 3);
        var high = controller.SelectedIndex;
        controller.MoveSelection(-10, entryCount: 3);

        Assert.Equal(2, high);
        Assert.Equal(0, controller.SelectedIndex);
    }

    [Fact]
    public void SelectClosesMenuAndReturnsSelectedEntry()
    {
        var controller = new SadConsoleEditorCommandMenuController();
        var entries = new[]
        {
            new SadConsoleEditorCommandMenuEntry(SadConsoleEditorCommandId.Save, "Save", "save"),
            new SadConsoleEditorCommandMenuEntry(SadConsoleEditorCommandId.Refresh, "Refresh", "refresh")
        };
        controller.Open(inputSubmodeActive: false, entries.Length);
        controller.MoveSelection(1, entries.Length);

        var result = controller.Select(entries);

        Assert.Equal(SadConsoleEditorCommandId.Refresh, result.Entry?.CommandId);
        Assert.False(controller.IsOpen);
        Assert.Equal(0, controller.SelectedIndex);
    }

    [Fact]
    public void CancelClosesMenuWithoutSelectingCommand()
    {
        var controller = new SadConsoleEditorCommandMenuController();
        controller.Open(inputSubmodeActive: false, entryCount: 1);

        var result = controller.Cancel();

        Assert.True(result.Succeeded, result.Message);
        Assert.False(controller.IsOpen);
        Assert.Contains("no command was invoked", result.Message);
    }
}
