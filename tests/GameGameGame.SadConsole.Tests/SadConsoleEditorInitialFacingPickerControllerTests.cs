using GameGameGame.Core;
using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsoleEditorInitialFacingPickerControllerTests
{
    [Fact]
    public void BeginSelectsCurrentFacingOption()
    {
        var picker = new SadConsoleEditorInitialFacingPickerController();

        picker.Begin(Direction.South);

        Assert.True(picker.IsActive);
        Assert.Equal(Direction.South, picker.Options[picker.SelectedIndex].Facing);
    }

    [Fact]
    public void MoveClampsWithinAvailableFacingOptions()
    {
        var picker = new SadConsoleEditorInitialFacingPickerController();
        picker.Begin(null);

        picker.Move(99);
        var high = picker.SelectedIndex;
        picker.Move(-99);

        Assert.Equal(picker.Options.Count - 1, high);
        Assert.Equal(0, picker.SelectedIndex);
    }

    [Fact]
    public void ConfirmReturnsSelectionAndClearsPicker()
    {
        var picker = new SadConsoleEditorInitialFacingPickerController();
        picker.Begin(Direction.North);
        picker.Move(1);

        var selection = picker.Confirm();

        Assert.Equal(Direction.South, selection?.Facing);
        Assert.False(picker.IsActive);
        Assert.Equal(0, picker.SelectedIndex);
    }

    [Fact]
    public void ConfirmWhenInactiveReturnsNull()
    {
        var picker = new SadConsoleEditorInitialFacingPickerController();

        var selection = picker.Confirm();

        Assert.Null(selection);
    }
}
