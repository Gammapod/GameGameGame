using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsoleEditorDefaultActionPlanPickerControllerTests
{
    [Fact]
    public void BeginSelectsCurrentActionPlanOption()
    {
        var picker = new SadConsoleEditorDefaultActionPlanPickerController();
        var options = Options(null, "wander", "wait");

        picker.Begin("wait", options);

        Assert.True(picker.IsActive);
        Assert.Equal(2, picker.SelectedIndex);
    }

    [Fact]
    public void MoveClampsWithinAvailableOptions()
    {
        var picker = new SadConsoleEditorDefaultActionPlanPickerController();
        var options = Options(null, "wander", "wait");
        picker.Begin(null, options);

        picker.Move(99, options.Count);
        var high = picker.SelectedIndex;
        picker.Move(-99, options.Count);

        Assert.Equal(options.Count - 1, high);
        Assert.Equal(0, picker.SelectedIndex);
    }

    [Fact]
    public void ConfirmReturnsSelectionAndClearsPicker()
    {
        var picker = new SadConsoleEditorDefaultActionPlanPickerController();
        var options = Options(null, "wander", "wait");
        picker.Begin(null, options);
        picker.Move(1, options.Count);

        var selection = picker.Confirm(options);

        Assert.Equal("wander", selection?.ActionPlanId);
        Assert.False(picker.IsActive);
        Assert.Equal(0, picker.SelectedIndex);
    }

    [Fact]
    public void ConfirmWhenInactiveReturnsNull()
    {
        var picker = new SadConsoleEditorDefaultActionPlanPickerController();

        var selection = picker.Confirm(Options(null, "wander"));

        Assert.Null(selection);
    }

    private static IReadOnlyList<SadConsoleEditorActionPlanPickerOption> Options(params string?[] ids) =>
        ids.Select(id => new SadConsoleEditorActionPlanPickerOption(id, id ?? "none")).ToList();
}
