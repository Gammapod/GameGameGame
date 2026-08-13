using GameGameGame.Content;

namespace GameGameGame.Frontend.SadConsole;

internal enum ScenarioBrowserCommand
{
    Up,
    Down,
    Select,
    Cancel
}

internal enum ScenarioBrowserActionOption
{
    Play,
    Edit
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

internal sealed record ScenarioBrowserViewport(
    int StartIndex,
    IReadOnlyList<WorkspaceScenarioCatalogEntry> Entries,
    int SelectedVisibleIndex,
    bool HasItemsAbove,
    bool HasItemsBelow)
{
    public int EndIndexExclusive => StartIndex + Entries.Count;
    public string PositionSummary(int selectedIndex, int totalCount) => totalCount == 0
        ? "0/0"
        : $"{selectedIndex + 1}/{totalCount}";
}

internal sealed class ScenarioBrowserScreenModel
{
    private readonly List<WorkspaceScenarioCatalogEntry> _entries;
    private int _selectedIndex;
    private int? _hoveredIndex;
    private ScenarioBrowserActionOption _selectedActionOption = ScenarioBrowserActionOption.Play;

    public ScenarioBrowserScreenModel(WorkspaceScenarioCatalogResult catalog, int selectedIndex = 0, FrontendInputMode inputMode = FrontendInputMode.Keyboard)
    {
        _entries = catalog.Entries.ToList();
        Diagnostics = catalog.Diagnostics;
        _selectedIndex = _entries.Count == 0 ? 0 : Math.Clamp(selectedIndex, 0, _entries.Count - 1);
        ActiveInputMode = inputMode;
    }

    public string Title => "GameGameGame - Scenario Browser";
    public IReadOnlyList<WorkspaceScenarioCatalogEntry> Entries => _entries;
    public IReadOnlyList<string> Diagnostics { get; }
    public int SelectedIndex => _selectedIndex;
    public int? HoveredIndex => _hoveredIndex;
    public FrontendInputMode ActiveInputMode { get; private set; }
    public bool ActionSelectorOpen { get; private set; }
    public ScenarioBrowserActionOption SelectedActionOption => _selectedActionOption;
    public WorkspaceScenarioCatalogEntry? SelectedEntry => _entries.Count == 0 ? null : _entries[_selectedIndex];
    public WorkspaceScenarioCatalogEntry? HoveredEntry => _hoveredIndex is { } index && index >= 0 && index < _entries.Count ? _entries[index] : null;
    public string Footer => ActionSelectorOpen
        ? $"Up/Down: Play/Edit  Enter/Click: Activate  Esc: Back  F12: Layout debug  Input: {ActiveInputMode}"
        : $"Up/Down: Move  Enter/Click: Select  Esc: Exit  F11: Fullscreen  F12: Layout debug  Input: {ActiveInputMode}";

    public ScenarioBrowserViewport Viewport(int visibleRows)
    {
        visibleRows = Math.Max(0, visibleRows);
        if (_entries.Count == 0 || visibleRows == 0)
        {
            return new ScenarioBrowserViewport(0, [], 0, false, false);
        }

        var start = Math.Clamp(
            _selectedIndex - visibleRows / 2,
            0,
            Math.Max(0, _entries.Count - visibleRows));
        var visible = _entries.Skip(start).Take(visibleRows).ToList();
        return new ScenarioBrowserViewport(
            start,
            visible,
            _selectedIndex - start,
            HasItemsAbove: start > 0,
            HasItemsBelow: start + visible.Count < _entries.Count);
    }

    public ScenarioBrowserResult Handle(ScenarioBrowserCommand command)
    {
        return Handle(command, FrontendInputMode.Keyboard);
    }

    public ScenarioBrowserResult HandleGamepad(ScenarioBrowserCommand command) =>
        Handle(command, FrontendInputMode.Gamepad);

    private ScenarioBrowserResult Handle(ScenarioBrowserCommand command, FrontendInputMode inputMode)
    {
        ActiveInputMode = inputMode;
        if (ActionSelectorOpen)
        {
            return HandleActionSelector(command);
        }

        switch (command)
        {
            case ScenarioBrowserCommand.Up:
                Move(-1);
                return ScenarioBrowserResult.Stay($"Selected scenario: {SelectedEntry?.Name ?? "none"}.");
            case ScenarioBrowserCommand.Down:
                Move(1);
                return ScenarioBrowserResult.Stay($"Selected scenario: {SelectedEntry?.Name ?? "none"}.");
            case ScenarioBrowserCommand.Select:
                if (SelectedEntry is null)
                {
                    return ScenarioBrowserResult.Stay("No scenario is available to select.");
                }

                ActionSelectorOpen = true;
                _selectedActionOption = ScenarioBrowserActionOption.Play;
                ClearHover();
                return ScenarioBrowserResult.Stay($"Selected {SelectedEntry.Name}. Choose Play or Edit.");
            case ScenarioBrowserCommand.Cancel:
                return ScenarioBrowserResult.Exit();
            default:
                return ScenarioBrowserResult.Stay("Use Up/Down to choose a scenario, Enter to select, Esc to exit.");
        }
    }

    private ScenarioBrowserResult HandleActionSelector(ScenarioBrowserCommand command)
    {
        switch (command)
        {
            case ScenarioBrowserCommand.Up:
            case ScenarioBrowserCommand.Down:
                _selectedActionOption = _selectedActionOption == ScenarioBrowserActionOption.Play
                    ? ScenarioBrowserActionOption.Edit
                    : ScenarioBrowserActionOption.Play;
                return ScenarioBrowserResult.Stay($"Selected option: {_selectedActionOption}.");
            case ScenarioBrowserCommand.Select:
                if (_selectedActionOption == ScenarioBrowserActionOption.Play && SelectedEntry is { } entry)
                {
                    ActionSelectorOpen = false;
                    return ScenarioBrowserResult.Launch(entry);
                }

                return ScenarioBrowserResult.Stay("Edit mode placeholder selected; editor surface is not implemented yet.");
            case ScenarioBrowserCommand.Cancel:
                ActionSelectorOpen = false;
                _selectedActionOption = ScenarioBrowserActionOption.Play;
                return ScenarioBrowserResult.Stay("Scenario details closed; scenario list focused.");
            default:
                return ScenarioBrowserResult.Stay("Choose Play or Edit, Enter to activate, Esc to close.");
        }
    }

    public ScenarioBrowserResult Scroll(int deltaRows)
    {
        ActiveInputMode = FrontendInputMode.Mouse;
        if (ActionSelectorOpen)
        {
            return ScenarioBrowserResult.Stay("Scenario details focused; close them before scrolling the scenario list.");
        }

        Move(deltaRows);
        return ScenarioBrowserResult.Stay($"Selected scenario: {SelectedEntry?.Name ?? "none"}.");
    }

    public ScenarioBrowserResult SelectVisibleRow(ScenarioBrowserViewport viewport, int visibleRowIndex, bool launch)
    {
        ActiveInputMode = FrontendInputMode.Mouse;
        if (ActionSelectorOpen)
        {
            return ScenarioBrowserResult.Stay("Scenario details focused; close them before selecting another scenario.");
        }

        if (visibleRowIndex < 0 || visibleRowIndex >= viewport.Entries.Count)
        {
            return ScenarioBrowserResult.Stay("No scenario row is under the mouse.");
        }

        _selectedIndex = Math.Clamp(viewport.StartIndex + visibleRowIndex, 0, Math.Max(0, _entries.Count - 1));
        if (launch && SelectedEntry is not null)
        {
            ActionSelectorOpen = true;
            _selectedActionOption = ScenarioBrowserActionOption.Play;
            ClearHover();
            return ScenarioBrowserResult.Stay($"Selected {SelectedEntry.Name}. Choose Play or Edit.");
        }

        return ScenarioBrowserResult.Stay($"Selected scenario: {SelectedEntry?.Name ?? "none"}.");
    }

    public ScenarioBrowserResult HoverVisibleRow(ScenarioBrowserViewport viewport, int visibleRowIndex)
    {
        ActiveInputMode = FrontendInputMode.Mouse;
        if (ActionSelectorOpen)
        {
            return ScenarioBrowserResult.Stay("Scenario details focused; scenario list hover is inactive.");
        }

        if (visibleRowIndex < 0 || visibleRowIndex >= viewport.Entries.Count)
        {
            ClearHover();
            return ScenarioBrowserResult.Stay("No scenario row is under the mouse.");
        }

        _hoveredIndex = viewport.StartIndex + visibleRowIndex;
        return ScenarioBrowserResult.Stay($"Hover scenario: {HoveredEntry?.Name ?? "none"}. Click to select.");
    }

    public void ClearHover() => _hoveredIndex = null;

    private void Move(int delta)
    {
        if (_entries.Count == 0)
        {
            return;
        }

        _selectedIndex = Math.Clamp(_selectedIndex + delta, 0, _entries.Count - 1);
        ClearHover();
    }
}
