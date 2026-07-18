namespace GameGameGame.SadConsoleApp;

internal sealed class SadConsoleEditorCommandMenuController
{
    public bool IsOpen { get; private set; }
    public int SelectedIndex { get; private set; }

    public SadConsoleEditorMutationUiResult Open(bool inputSubmodeActive, int entryCount)
    {
        if (inputSubmodeActive)
        {
            return SadConsoleEditorMutationUiResult.Failure("Finish or cancel the active editor submode before opening the command menu.");
        }

        IsOpen = true;
        SelectedIndex = ClampIndex(SelectedIndex, entryCount);
        return SadConsoleEditorMutationUiResult.Success("Editor command menu opened. Up/Down chooses a command; Enter/Select activates; Esc cancels.");
    }

    public SadConsoleEditorMutationUiResult Cancel()
    {
        if (!IsOpen)
        {
            return SadConsoleEditorMutationUiResult.Success("Editor command menu is not open.");
        }

        IsOpen = false;
        return SadConsoleEditorMutationUiResult.Success("Editor command menu cancelled; no command was invoked.");
    }

    public void MoveSelection(int delta, int entryCount)
    {
        if (!IsOpen)
        {
            return;
        }

        SelectedIndex = ClampIndex(SelectedIndex + delta, entryCount);
    }

    public SadConsoleEditorCommandMenuSelectionResult Select(IReadOnlyList<SadConsoleEditorCommandMenuEntry> entries)
    {
        if (!IsOpen)
        {
            return SadConsoleEditorCommandMenuSelectionResult.None("Editor command menu is not open.");
        }

        if (entries.Count == 0)
        {
            IsOpen = false;
            SelectedIndex = 0;
            return SadConsoleEditorCommandMenuSelectionResult.None("No editor commands are available for the current context.");
        }

        var selected = entries[Math.Clamp(SelectedIndex, 0, entries.Count - 1)];
        IsOpen = false;
        SelectedIndex = 0;
        return SadConsoleEditorCommandMenuSelectionResult.Selected(selected);
    }

    private static int ClampIndex(int index, int count) =>
        count <= 0 ? 0 : Math.Clamp(index, 0, count - 1);
}

internal sealed record SadConsoleEditorCommandMenuSelectionResult(SadConsoleEditorCommandMenuEntry? Entry, string? Message)
{
    public static SadConsoleEditorCommandMenuSelectionResult Selected(SadConsoleEditorCommandMenuEntry entry) => new(entry, null);

    public static SadConsoleEditorCommandMenuSelectionResult None(string message) => new(null, message);
}
