using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Navigation;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class ScenarioSelectionScreen
{
    private readonly List<ScenarioCatalogEntry> _scenarios;
    private readonly List<ScenarioCatalogSection> _sections;
    private readonly FocusRouter _focusRouter;
    private readonly List<SelectableListItem> _commandItems =
    [
        new("play", "Play", "goto simulation/play mode"),
        new("edit", "Edit", "goto Scenario Edit screen")
    ];

    private int _selectedScenarioIndex;
    private int _selectedSectionIndex;
    private int _selectedCommandIndex;
    private bool _commandPanelOpen;

    private ScenarioSelectionScreen(
        IEnumerable<ScenarioCatalogEntry> scenarios,
        IReadOnlyList<string> diagnostics,
        IEnumerable<ScenarioCatalogSection>? sections = null)
    {
        _scenarios = scenarios.ToList();
        _sections = (sections ?? []).ToList();
        Diagnostics = diagnostics;
        _focusRouter = new FocusRouter([
            new FocusTarget("scenario-list"),
            new FocusTarget("scenario-command-panel")
        ]);
    }

    public string Title => "Scenario Selection";
    public string Purpose => "Choose a scenario, then choose Play or Edit.";
    public IReadOnlyList<string> Diagnostics { get; }
    public bool CommandPanelOpen => _commandPanelOpen;
    public int SelectedScenarioIndex => _selectedScenarioIndex;
    public int SelectedSectionIndex => _selectedSectionIndex;
    public int SelectedCommandIndex => _selectedCommandIndex;
    public ScenarioCatalogSection? SelectedSection => _sections.Count == 0 ? null : _sections[_selectedSectionIndex];
    public IReadOnlyList<ScenarioCatalogEntry> VisibleScenarios => ActiveScenarios();
    public ScenarioCatalogEntry? SelectedScenario
    {
        get
        {
            var scenarios = ActiveScenarios();
            return scenarios.Count == 0 ? null : scenarios[_selectedScenarioIndex];
        }
    }

    public static ScenarioSelectionScreen FromCatalog(ScenarioCatalogResult? catalog) => new(
        catalog?.Entries ?? [],
        catalog?.Diagnostics ?? [],
        catalog?.Sections);

    public IReadOnlyList<IUiComponent> Components()
    {
        var components = new List<IUiComponent>
        {
            ScenarioList()
        };

        if (_commandPanelOpen)
        {
            components.Add(CommandPanel());
        }

        if (Diagnostics.Count > 0)
        {
            components.Add(new PanelComponent(
                "catalog-diagnostics",
                "Catalog diagnostics",
                new SadConsoleRect(1, 34, 116, 40),
                Diagnostics.Take(4).ToList(),
                UiComponentState.Error));
        }

        return components;
    }

    public IUiComponent ScenarioListComponent() => ScenarioList();

    public IUiComponent? OverlayComponent() => _commandPanelOpen ? CommandPanel() : null;

    public string FooterText()
    {
        if (_commandPanelOpen)
        {
            return "Command panel focused: Up/Down chooses Play/Edit. Enter activates. Esc closes command panel.";
        }

        return _sections.Count > 0
            ? "Scenario list focused: Left/Right changes curated section. Up/Down chooses scenario. Enter opens Play/Edit. Esc exits application."
            : "Scenario list focused: Up/Down chooses scenario. Enter opens Play/Edit. Esc exits application.";
    }

    public ScenarioSelectionResult Handle(UiComponentCommand command)
    {
        if (_commandPanelOpen)
        {
            return HandleCommandPanel(command);
        }

        return HandleScenarioList(command);
    }

    private ScenarioSelectionResult HandleScenarioList(UiComponentCommand command)
    {
        switch (command)
        {
            case UiComponentCommand.Up:
                MoveScenario(-1);
                return ScenarioSelectionResult.Stay($"Selected scenario: {SelectedScenario?.Name ?? "none"}.");
            case UiComponentCommand.Down:
                MoveScenario(1);
                return ScenarioSelectionResult.Stay($"Selected scenario: {SelectedScenario?.Name ?? "none"}.");
            case UiComponentCommand.Left:
                return MoveSection(-1);
            case UiComponentCommand.Right:
                return MoveSection(1);
            case UiComponentCommand.Select:
                if (SelectedScenario is null)
                {
                    return ScenarioSelectionResult.Stay("No scenario is available to select.");
                }

                _commandPanelOpen = true;
                _selectedCommandIndex = 0;
                _focusRouter.Handle(UiComponentCommand.Right);
                _focusRouter.Handle(UiComponentCommand.Select);
                return ScenarioSelectionResult.Stay($"Choose Play/Edit for {SelectedScenario.Name}.");
            case UiComponentCommand.Cancel:
                return ScenarioSelectionResult.Exit("Scenario selection cancelled; exiting application.");
            default:
                return ScenarioSelectionResult.Stay(_sections.Count > 0
                    ? "Use Left/Right to change section, Up/Down to choose a scenario, Enter to select, Esc to exit."
                    : "Use Up/Down to choose a scenario, Enter to select, Esc to exit.");
        }
    }

    private ScenarioSelectionResult HandleCommandPanel(UiComponentCommand command)
    {
        switch (command)
        {
            case UiComponentCommand.Up:
                _selectedCommandIndex = Math.Max(0, _selectedCommandIndex - 1);
                return ScenarioSelectionResult.Stay($"Selected command: {_commandItems[_selectedCommandIndex].Label}.");
            case UiComponentCommand.Down:
                _selectedCommandIndex = Math.Min(_commandItems.Count - 1, _selectedCommandIndex + 1);
                return ScenarioSelectionResult.Stay($"Selected command: {_commandItems[_selectedCommandIndex].Label}.");
            case UiComponentCommand.Cancel:
                CloseCommandPanel();
                return ScenarioSelectionResult.Stay("Command panel closed; scenario list focused.");
            case UiComponentCommand.Select:
                return ActivateCommand();
            default:
                return ScenarioSelectionResult.Stay("Use Up/Down to choose Play/Edit, Enter to activate, Esc to close.");
        }
    }

    private ScenarioSelectionResult ActivateCommand()
    {
        var scenario = SelectedScenario;
        if (scenario is null)
        {
            CloseCommandPanel();
            return ScenarioSelectionResult.Stay("No scenario is available to activate.");
        }

        var commandId = _commandItems[_selectedCommandIndex].Id;
        CloseCommandPanel();

        return commandId switch
        {
            "play" => ScenarioSelectionResult.Play(scenario, $"Play selected: {scenario.Name}. Opening Play UX mock."),
            "edit" => ScenarioSelectionResult.Edit(scenario, $"Edit selected: {scenario.Name}. Scenario Edit screen is next screen work."),
            _ => ScenarioSelectionResult.Stay("Unknown scenario command.")
        };
    }

    private void CloseCommandPanel()
    {
        _commandPanelOpen = false;
        _selectedCommandIndex = 0;
        if (_focusRouter.FocusedComponentId is not null)
        {
            _focusRouter.Handle(UiComponentCommand.Cancel);
        }
        if (_focusRouter.SelectedComponentId != "scenario-list")
        {
            _focusRouter.Handle(UiComponentCommand.Left);
        }
    }

    private void MoveScenario(int delta)
    {
        var scenarios = ActiveScenarios();
        if (scenarios.Count == 0)
        {
            return;
        }

        _selectedScenarioIndex = Math.Clamp(_selectedScenarioIndex + delta, 0, scenarios.Count - 1);
    }

    private ScenarioSelectionResult MoveSection(int delta)
    {
        if (_sections.Count == 0)
        {
            return ScenarioSelectionResult.Stay("This catalog has no curated sections; showing all scenarios.");
        }

        _selectedSectionIndex = Math.Clamp(_selectedSectionIndex + delta, 0, _sections.Count - 1);
        _selectedScenarioIndex = 0;
        var section = SelectedSection!;
        return ScenarioSelectionResult.Stay($"Section: {section.Name} ({section.Entries.Count} scenario{(section.Entries.Count == 1 ? string.Empty : "s")}).");
    }

    private SelectableListComponent ScenarioList()
    {
        var scenarios = ActiveScenarios();
        var items = scenarios.Select(entry => new SelectableListItem(
            entry.ScenarioId,
            entry.Name,
            FormatScenarioDescription(entry),
            DetailOnNextLine: true)).ToList();
        var list = new SelectableListComponent(
            "scenario-list",
            ScenarioListTitle(),
            new SadConsoleRect(1, 4, 116, 32),
            items,
            _commandPanelOpen ? UiComponentState.Unselected : UiComponentState.Focused,
            visibleRowCount: 12);

        for (var index = 0; index < _selectedScenarioIndex; index++)
        {
            list.MoveSelection(1);
        }

        return list;
    }

    private IReadOnlyList<ScenarioCatalogEntry> ActiveScenarios()
    {
        if (_sections.Count == 0)
        {
            return _scenarios;
        }

        return SelectedSection?.Entries ?? [];
    }

    private string ScenarioListTitle()
    {
        if (SelectedSection is not { } section)
        {
            return "1.1 Scenarios";
        }

        var metadata = string.IsNullOrWhiteSpace(section.Status) ? string.Empty : $" [{section.Status}]";
        return $"1.1 Scenarios: {section.Name}{metadata} ({_selectedSectionIndex + 1}/{_sections.Count})";
    }

    private static string FormatScenarioDescription(ScenarioCatalogEntry entry) =>
        string.IsNullOrWhiteSpace(entry.Description) ? entry.ContentPath : entry.Description;

    private SelectableListComponent CommandPanel()
    {
        var list = new SelectableListComponent(
            "scenario-command-panel",
            "1.1.1 Scenario action",
            new SadConsoleRect(76, 4, 41, 13),
            _commandItems,
            UiComponentState.Focused,
            visibleRowCount: 2);

        for (var index = 0; index < _selectedCommandIndex; index++)
        {
            list.MoveSelection(1);
        }

        return list;
    }
}

internal sealed record ScenarioSelectionResult(
    ScenarioSelectionResultKind Kind,
    ScenarioCatalogEntry? Scenario,
    string Message)
{
    public static ScenarioSelectionResult Stay(string message) => new(ScenarioSelectionResultKind.Stay, null, message);
    public static ScenarioSelectionResult Play(ScenarioCatalogEntry scenario, string message) => new(ScenarioSelectionResultKind.Play, scenario, message);
    public static ScenarioSelectionResult Edit(ScenarioCatalogEntry scenario, string message) => new(ScenarioSelectionResultKind.Edit, scenario, message);
    public static ScenarioSelectionResult Exit(string message) => new(ScenarioSelectionResultKind.Exit, null, message);
}

internal enum ScenarioSelectionResultKind
{
    Stay,
    Play,
    Edit,
    Exit
}
