using GameGameGame.Core;

namespace GameGameGame.SadConsoleApp;

internal sealed class SadConsoleEditorInitialFacingPickerController
{
    public bool IsActive { get; private set; }
    public int SelectedIndex { get; private set; }

    public IReadOnlyList<SadConsoleEditorInitialFacingPickerOption> Options =>
    [
        new SadConsoleEditorInitialFacingPickerOption(null, "none"),
        new SadConsoleEditorInitialFacingPickerOption(Direction.North, "North"),
        new SadConsoleEditorInitialFacingPickerOption(Direction.South, "South"),
        new SadConsoleEditorInitialFacingPickerOption(Direction.West, "West"),
        new SadConsoleEditorInitialFacingPickerOption(Direction.East, "East")
    ];

    public void Begin(Direction? currentFacing)
    {
        var currentIndex = Options.ToList().FindIndex(option => option.Facing == currentFacing);
        SelectedIndex = currentIndex >= 0 ? currentIndex : 0;
        IsActive = true;
    }

    public SadConsoleEditorInitialFacingPickerSelection? Confirm()
    {
        if (!IsActive)
        {
            return null;
        }

        var option = Options[Math.Clamp(SelectedIndex, 0, Options.Count - 1)];
        Clear();
        return new SadConsoleEditorInitialFacingPickerSelection(option.Facing);
    }

    public void Move(int delta)
    {
        if (!IsActive)
        {
            return;
        }

        SelectedIndex = Options.Count <= 0 ? 0 : Math.Clamp(SelectedIndex + delta, 0, Options.Count - 1);
    }

    public void Clear()
    {
        IsActive = false;
        SelectedIndex = 0;
    }
}

internal sealed record SadConsoleEditorInitialFacingPickerSelection(Direction? Facing);
