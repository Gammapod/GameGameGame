using GameGameGame.Content;

namespace GameGameGame.Frontend.SadConsole;

internal enum ScenarioBrowserCommand
{
    Up,
    Down,
    Select,
    Cancel
}

internal enum ScenarioBrowserResultKind
{
    Stay,
    LaunchRequested,
    ExitRequested
}

internal sealed record ScenarioBrowserResult(
    ScenarioBrowserResultKind Kind,
    string Message,
    WorkspaceScenarioCatalogEntry? Entry = null)
{
    public static ScenarioBrowserResult Stay(string message) => new(ScenarioBrowserResultKind.Stay, message);
    public static ScenarioBrowserResult Launch(WorkspaceScenarioCatalogEntry entry) => new(ScenarioBrowserResultKind.LaunchRequested, $"Launch selected: {entry.Name}.", entry);
    public static ScenarioBrowserResult Exit() => new(ScenarioBrowserResultKind.ExitRequested, "Scenario browser cancelled; exiting.");
}

internal sealed class ScenarioBrowserScreenModel
{
    private readonly List<WorkspaceScenarioCatalogEntry> _entries;
    private int _selectedIndex;

    public ScenarioBrowserScreenModel(WorkspaceScenarioCatalogResult catalog, int selectedIndex = 0)
    {
        _entries = catalog.Entries.ToList();
        Diagnostics = catalog.Diagnostics;
        _selectedIndex = _entries.Count == 0 ? 0 : Math.Clamp(selectedIndex, 0, _entries.Count - 1);
    }

    public string Title => "GameGameGame - Scenario Browser";
    public IReadOnlyList<WorkspaceScenarioCatalogEntry> Entries => _entries;
    public IReadOnlyList<string> Diagnostics { get; }
    public int SelectedIndex => _selectedIndex;
    public WorkspaceScenarioCatalogEntry? SelectedEntry => _entries.Count == 0 ? null : _entries[_selectedIndex];
    public string Footer => "Up/Down: Move  Enter: Select  Esc: Exit  F11: Fullscreen  F12: Layout debug  Input: Keyboard";

    public ScenarioBrowserResult Handle(ScenarioBrowserCommand command)
    {
        switch (command)
        {
            case ScenarioBrowserCommand.Up:
                Move(-1);
                return ScenarioBrowserResult.Stay($"Selected scenario: {SelectedEntry?.Name ?? "none"}.");
            case ScenarioBrowserCommand.Down:
                Move(1);
                return ScenarioBrowserResult.Stay($"Selected scenario: {SelectedEntry?.Name ?? "none"}.");
            case ScenarioBrowserCommand.Select:
                return SelectedEntry is { } entry
                    ? ScenarioBrowserResult.Launch(entry)
                    : ScenarioBrowserResult.Stay("No scenario is available to launch.");
            case ScenarioBrowserCommand.Cancel:
                return ScenarioBrowserResult.Exit();
            default:
                return ScenarioBrowserResult.Stay("Use Up/Down to choose a scenario, Enter to select, Esc to exit.");
        }
    }

    private void Move(int delta)
    {
        if (_entries.Count == 0)
        {
            return;
        }

        _selectedIndex = Math.Clamp(_selectedIndex + delta, 0, _entries.Count - 1);
    }
}
