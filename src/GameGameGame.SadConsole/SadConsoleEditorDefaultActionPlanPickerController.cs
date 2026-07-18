namespace GameGameGame.SadConsoleApp;

internal sealed class SadConsoleEditorDefaultActionPlanPickerController
{
    public bool IsActive { get; private set; }
    public int SelectedIndex { get; private set; }

    public void Begin(string? currentActionPlanId, IReadOnlyList<SadConsoleEditorActionPlanPickerOption> options)
    {
        var currentIndex = options.ToList().FindIndex(option => string.Equals(option.ActionPlanId, currentActionPlanId, StringComparison.Ordinal));
        SelectedIndex = currentIndex >= 0 ? currentIndex : 0;
        IsActive = true;
    }

    public SadConsoleEditorDefaultActionPlanPickerSelection? Confirm(IReadOnlyList<SadConsoleEditorActionPlanPickerOption> options)
    {
        if (!IsActive)
        {
            return null;
        }

        var option = options.Count == 0
            ? new SadConsoleEditorActionPlanPickerOption(null, "none")
            : options[Math.Clamp(SelectedIndex, 0, options.Count - 1)];
        Clear();
        return new SadConsoleEditorDefaultActionPlanPickerSelection(option.ActionPlanId);
    }

    public void Move(int delta, int optionCount)
    {
        if (!IsActive)
        {
            return;
        }

        SelectedIndex = optionCount <= 0 ? 0 : Math.Clamp(SelectedIndex + delta, 0, optionCount - 1);
    }

    public void Clear()
    {
        IsActive = false;
        SelectedIndex = 0;
    }
}

internal sealed record SadConsoleEditorDefaultActionPlanPickerSelection(string? ActionPlanId);
