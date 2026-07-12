using GameGameGame.Content;

namespace GameGameGame.SadConsoleApp;

internal enum SadConsoleExplorationScreenKind
{
    ScenarioSelection,
    ScenarioEdit,
    EntityTemplateEdit,
    ActionPlanEdit,
    SimulationPlay,
    Exit
}

internal enum SadConsoleExplorationComponentState
{
    Unselected,
    Selected,
    Focused
}

internal enum SadConsoleExplorationComponentKind
{
    ScenarioList,
    ScenarioCommandList,
    ScenarioPreviewList,
    PlayerStartFields,
    EntityTemplateList,
    ActionPlanList,
    PresentationFields,
    TargetingFields,
    TargetingSlotFields,
    InventoryFields,
    InventoryItemFields,
    InventoryDrawingPanel,
    ActionPlanStepList
}

internal enum SadConsoleExplorationCommand
{
    MoveNextComponent,
    MovePreviousComponent,
    FocusSelectedComponent,
    ReleaseFocusedComponent,
    Activate,
    Cancel
}

internal sealed record SadConsoleExplorationBorderPalette(
    string UnselectedHighlight,
    string SelectedHighlight,
    string FocusedHighlight)
{
    public static SadConsoleExplorationBorderPalette Default { get; } = new("MutedBlue", "Gold", "HotPink");

    public string For(SadConsoleExplorationComponentState state) => state switch
    {
        SadConsoleExplorationComponentState.Unselected => UnselectedHighlight,
        SadConsoleExplorationComponentState.Selected => SelectedHighlight,
        SadConsoleExplorationComponentState.Focused => FocusedHighlight,
        _ => UnselectedHighlight
    };
}

internal sealed record SadConsoleExplorationComponent(
    string Id,
    SadConsoleExplorationComponentKind Kind,
    string Title,
    IReadOnlyList<string> Rows,
    SadConsoleRect Bounds,
    SadConsoleExplorationComponentState State)
{
    public string BorderColor(SadConsoleExplorationBorderPalette palette) => palette.For(State);
}

internal sealed record SadConsoleExplorationScreen(
    SadConsoleExplorationScreenKind Kind,
    string Title,
    string ContextLabel,
    IReadOnlyList<SadConsoleExplorationComponent> Components,
    string Footer)
{
    public SadConsoleExplorationComponent? SelectedComponent => Components.FirstOrDefault(component => component.State != SadConsoleExplorationComponentState.Unselected);
}

internal sealed record SadConsoleScenarioSelectionItem(string ContentPath, string ScenarioId, string Name, string Description);

internal sealed record SadConsoleScenarioEditItem(
    string ContentPath,
    string ScenarioId,
    string Name,
    string ScenarioRootEntityTemplateId,
    string PlayerEntityTemplateId,
    int PlayerX,
    int PlayerY,
    IReadOnlyList<SadConsoleEntityTemplateEditItem> EntityTemplates,
    IReadOnlyList<SadConsoleActionPlanEditItem> ActionPlans,
    IReadOnlyList<string> PreviewRows);

internal sealed record SadConsoleEntityTemplateEditItem(
    string TemplateId,
    string Name,
    char Glyph,
    string Color,
    string? ActionPlanId,
    IReadOnlyList<SadConsoleTargetingSlotEditItem> TargetingSlots,
    IReadOnlyList<SadConsoleInventoryItemEditItem> InventoryItems,
    int InventoryWidth,
    int InventoryHeight,
    int Aperture,
    int Bulk);

internal sealed record SadConsoleTargetingSlotEditItem(int Slot, string TargetLabel, string TargetTemplateOrCriteria, int TargetRange);

internal sealed record SadConsoleInventoryItemEditItem(
    string EntityId,
    int InventorySpaceX,
    int InventorySpaceY,
    string BrushSelection,
    string Aperture,
    string Bulk);

internal sealed record SadConsoleActionPlanEditItem(string ActionPlanId, IReadOnlyList<string> Steps);

internal sealed class SadConsoleScenarioSelectionModel
{
    private readonly IReadOnlyList<SadConsoleScenarioSelectionItem> _scenarios;
    private int _selectedScenarioIndex;
    private bool _commandChoiceOpen;
    private int _selectedCommandIndex;

    public SadConsoleScenarioSelectionModel(IReadOnlyList<SadConsoleScenarioSelectionItem> scenarios)
    {
        _scenarios = scenarios;
    }

    public int SelectedScenarioIndex => _selectedScenarioIndex;
    public bool CommandChoiceOpen => _commandChoiceOpen;
    public int SelectedCommandIndex => _selectedCommandIndex;
    public SadConsoleScenarioSelectionItem? SelectedScenario => _scenarios.Count == 0 ? null : _scenarios[_selectedScenarioIndex];

    public static SadConsoleScenarioSelectionModel FromCatalog(ScenarioCatalogResult? catalog) => new((catalog?.Entries ?? [])
        .Select(entry => new SadConsoleScenarioSelectionItem(
            entry.ContentPath,
            entry.ScenarioId,
            entry.Name,
            entry.Description ?? string.Empty))
        .ToList());

    public SadConsoleExplorationScreen BuildScreen()
    {
        var components = new List<SadConsoleExplorationComponent>
        {
            new(
                "scenario-list",
                SadConsoleExplorationComponentKind.ScenarioList,
                "1.1 Scenarios",
                _scenarios.Count == 0
                    ? ["No scenarios found."]
                    : _scenarios.Select((scenario, index) => FormatScenarioRow(scenario, index == _selectedScenarioIndex)).ToList(),
                new SadConsoleRect(1, 3, 58, 34),
                _commandChoiceOpen ? SadConsoleExplorationComponentState.Unselected : SadConsoleExplorationComponentState.Focused)
        };

        if (_commandChoiceOpen)
        {
            components.Add(new SadConsoleExplorationComponent(
                "scenario-command-list",
                SadConsoleExplorationComponentKind.ScenarioCommandList,
                "1.1.1 Scenario action",
                [FormatCommandRow("Play", 0), FormatCommandRow("Edit", 1), FormatCommandRow("Cancel", 2)],
                new SadConsoleRect(62, 3, 32, 10),
                SadConsoleExplorationComponentState.Focused));
        }

        return new SadConsoleExplorationScreen(
            SadConsoleExplorationScreenKind.ScenarioSelection,
            "Scenario selection",
            "Choose a scenario, then choose Play or Edit.",
            components,
            _commandChoiceOpen ? "Up/Down changes action. Select activates. Cancel closes action list." : "Up/Down changes scenario. Select opens Play/Edit. Cancel exits application.");
    }

    public SadConsoleScenarioSelectionResult Handle(SadConsoleExplorationCommand command)
    {
        if (_scenarios.Count == 0 && command != SadConsoleExplorationCommand.Cancel)
        {
            return SadConsoleScenarioSelectionResult.Stay;
        }

        switch (command)
        {
            case SadConsoleExplorationCommand.MoveNextComponent:
                if (_commandChoiceOpen)
                {
                    _selectedCommandIndex = Math.Min(_selectedCommandIndex + 1, 2);
                }
                else
                {
                    _selectedScenarioIndex = Math.Min(_selectedScenarioIndex + 1, _scenarios.Count - 1);
                }

                return SadConsoleScenarioSelectionResult.Stay;
            case SadConsoleExplorationCommand.MovePreviousComponent:
                if (_commandChoiceOpen)
                {
                    _selectedCommandIndex = Math.Max(_selectedCommandIndex - 1, 0);
                }
                else
                {
                    _selectedScenarioIndex = Math.Max(_selectedScenarioIndex - 1, 0);
                }

                return SadConsoleScenarioSelectionResult.Stay;
            case SadConsoleExplorationCommand.FocusSelectedComponent:
            case SadConsoleExplorationCommand.Activate:
                if (!_commandChoiceOpen)
                {
                    _commandChoiceOpen = true;
                    _selectedCommandIndex = 0;
                    return SadConsoleScenarioSelectionResult.Stay;
                }

                return _selectedCommandIndex switch
                {
                    0 => SadConsoleScenarioSelectionResult.Play(SelectedScenario!),
                    1 => SadConsoleScenarioSelectionResult.Edit(SelectedScenario!),
                    _ => SadConsoleScenarioSelectionResult.Exit
                };
            case SadConsoleExplorationCommand.ReleaseFocusedComponent:
                _commandChoiceOpen = false;
                return SadConsoleScenarioSelectionResult.Stay;
            case SadConsoleExplorationCommand.Cancel:
                if (_commandChoiceOpen)
                {
                    _commandChoiceOpen = false;
                    return SadConsoleScenarioSelectionResult.Stay;
                }

                return SadConsoleScenarioSelectionResult.Exit;
            default:
                return SadConsoleScenarioSelectionResult.Stay;
        }
    }

    private static string FormatScenarioRow(SadConsoleScenarioSelectionItem scenario, bool selected)
    {
        var marker = selected ? ">" : " ";
        var description = string.IsNullOrWhiteSpace(scenario.Description) ? string.Empty : $" - {scenario.Description}";
        return $"{marker} {scenario.Name} ({scenario.ScenarioId}){description}";
    }

    private string FormatCommandRow(string label, int index) => $"{(index == _selectedCommandIndex ? ">" : " ")} {label}";
}

internal sealed record SadConsoleScenarioSelectionResult(
    SadConsoleExplorationScreenKind NextScreen,
    SadConsoleScenarioSelectionItem? Scenario)
{
    public static SadConsoleScenarioSelectionResult Stay { get; } = new(SadConsoleExplorationScreenKind.ScenarioSelection, null);
    public static SadConsoleScenarioSelectionResult Exit { get; } = new(SadConsoleExplorationScreenKind.Exit, null);
    public static SadConsoleScenarioSelectionResult Play(SadConsoleScenarioSelectionItem scenario) => new(SadConsoleExplorationScreenKind.SimulationPlay, scenario);
    public static SadConsoleScenarioSelectionResult Edit(SadConsoleScenarioSelectionItem scenario) => new(SadConsoleExplorationScreenKind.ScenarioEdit, scenario);
}

internal sealed class SadConsoleScenarioEditScreenModel
{
    private readonly SadConsoleScenarioEditItem _scenario;
    private int _selectedComponentIndex;
    private bool _focused;
    private int _previewIndex;
    private int _entityIndex;
    private int _actionPlanIndex;

    public SadConsoleScenarioEditScreenModel(SadConsoleScenarioEditItem scenario)
    {
        _scenario = scenario;
    }

    public int SelectedComponentIndex => _selectedComponentIndex;
    public bool IsFocused => _focused;

    public static SadConsoleScenarioEditItem FromSnapshot(string contentPath, FrontendEditorSnapshot snapshot, string scenarioId)
    {
        var scenario = snapshot.Scenarios.First(item => item.ScenarioId == scenarioId);
        var entityTemplates = snapshot.EntityTemplates.Select(template => new SadConsoleEntityTemplateEditItem(
            template.TemplateId,
            template.Name,
            template.Glyph,
            template.Color.ToString(),
            template.DefaultActionPlanId,
            template.TargetingRules.Select(rule => new SadConsoleTargetingSlotEditItem(
                rule.Slot,
                rule.Label ?? string.Empty,
                FormatTargetingCriteria(rule),
                rule.Range)).ToList(),
            template.CarriedEntities.Select(item => new SadConsoleInventoryItemEditItem(
                item.EntityId,
                item.Coord.X,
                item.Coord.Y,
                item.TemplateName ?? item.TemplateId ?? "unbound",
                template.Aperture.ToString(),
                template.Bulk.ToString())).ToList(),
            template.InventoryWidth,
            template.InventoryHeight,
            template.Aperture,
            template.Bulk)).ToList();

        var actionPlans = snapshot.ActionPlans.Select(plan => new SadConsoleActionPlanEditItem(
            plan.ActionPlanId,
            plan.ActionSteps.Count == 0 ? ["No steps defined."] : plan.ActionSteps.Select(step => $"{step.Index}: {step.DisplayName}").ToList())).ToList();

        return new SadConsoleScenarioEditItem(
            contentPath,
            scenario.ScenarioId,
            scenario.Name,
            scenario.ScenarioRootEntityTemplateId,
            scenario.PlayerEntityTemplateId,
            scenario.PlayerStart.X,
            scenario.PlayerStart.Y,
            entityTemplates,
            actionPlans,
            [
                $"Preview: turn-0 runtime state for {scenario.Name} is derived from authored content.",
                $"Root template: {scenario.ScenarioRootEntityTemplateId}",
                $"Player template: {scenario.PlayerEntityTemplateId} at ({scenario.PlayerStart.X},{scenario.PlayerStart.Y})"
            ]);
    }

    private static string FormatTargetingCriteria(FrontendEditorTargetingRuleSummary rule)
    {
        var target = rule.TargetTemplateName ?? rule.TargetTemplateId ?? "any entity";
        var capabilities = rule.TargetCapabilities.Count == 0 ? string.Empty : $" [{string.Join(", ", rule.TargetCapabilities)}]";
        return $"{target}{capabilities}";
    }

    public SadConsoleExplorationScreen BuildScreen()
    {
        var specs = new[]
        {
            BuildComponent("scenario-preview", SadConsoleExplorationComponentKind.ScenarioPreviewList, "2.1 Scenario preview", _scenario.PreviewRows, new SadConsoleRect(1, 3, 56, 16)),
            BuildComponent("player-start", SadConsoleExplorationComponentKind.PlayerStartFields, "2.2 Player start", [
                $"scenario root: {_scenario.ScenarioRootEntityTemplateId}",
                $"player X position: {_scenario.PlayerX}",
                $"player Y position: {_scenario.PlayerY}"
            ], new SadConsoleRect(60, 3, 36, 10)),
            BuildComponent("entity-list", SadConsoleExplorationComponentKind.EntityTemplateList, "2.3 Defined entities", Rows(_scenario.EntityTemplates, _entityIndex, entity => $"{entity.Name} ({entity.TemplateId})"), new SadConsoleRect(1, 18, 56, 36)),
            BuildComponent("action-plan-list", SadConsoleExplorationComponentKind.ActionPlanList, "2.4 Defined action plans", Rows(_scenario.ActionPlans, _actionPlanIndex, plan => $"{plan.ActionPlanId} [{plan.Steps.Count} steps]"), new SadConsoleRect(60, 12, 56, 36))
        };

        return new SadConsoleExplorationScreen(
            SadConsoleExplorationScreenKind.ScenarioEdit,
            $"Scenario Edit: {_scenario.Name}",
            "Authored scenario fields plus derived turn-0 preview.",
            specs,
            _focused ? "Focused component handles controls. Select activates row/field. Cancel releases focus." : "Directional controls choose a component. Select focuses it. Cancel returns to scenario list.");
    }

    public SadConsoleScenarioEditResult Handle(SadConsoleExplorationCommand command)
    {
        if (!_focused)
        {
            switch (command)
            {
                case SadConsoleExplorationCommand.MoveNextComponent:
                    _selectedComponentIndex = Math.Min(_selectedComponentIndex + 1, 3);
                    return SadConsoleScenarioEditResult.Stay;
                case SadConsoleExplorationCommand.MovePreviousComponent:
                    _selectedComponentIndex = Math.Max(_selectedComponentIndex - 1, 0);
                    return SadConsoleScenarioEditResult.Stay;
                case SadConsoleExplorationCommand.FocusSelectedComponent:
                case SadConsoleExplorationCommand.Activate:
                    _focused = true;
                    return SadConsoleScenarioEditResult.Stay;
                case SadConsoleExplorationCommand.Cancel:
                    return SadConsoleScenarioEditResult.ReturnToScenarioSelection;
                default:
                    return SadConsoleScenarioEditResult.Stay;
            }
        }

        switch (command)
        {
            case SadConsoleExplorationCommand.ReleaseFocusedComponent:
            case SadConsoleExplorationCommand.Cancel:
                _focused = false;
                return SadConsoleScenarioEditResult.Stay;
            case SadConsoleExplorationCommand.MoveNextComponent:
                MoveFocusedList(1);
                return SadConsoleScenarioEditResult.Stay;
            case SadConsoleExplorationCommand.MovePreviousComponent:
                MoveFocusedList(-1);
                return SadConsoleScenarioEditResult.Stay;
            case SadConsoleExplorationCommand.Activate:
            case SadConsoleExplorationCommand.FocusSelectedComponent:
                if (_selectedComponentIndex is 0 or 2 && _scenario.EntityTemplates.Count > 0)
                {
                    return SadConsoleScenarioEditResult.EditEntity(_scenario.EntityTemplates[_entityIndex]);
                }

                if (_selectedComponentIndex == 3 && _scenario.ActionPlans.Count > 0)
                {
                    return SadConsoleScenarioEditResult.EditActionPlan(_scenario.ActionPlans[_actionPlanIndex]);
                }

                return SadConsoleScenarioEditResult.Stay;
            default:
                return SadConsoleScenarioEditResult.Stay;
        }
    }

    private SadConsoleExplorationComponent BuildComponent(string id, SadConsoleExplorationComponentKind kind, string title, IReadOnlyList<string> rows, SadConsoleRect bounds)
    {
        var state = _selectedComponentIndex == ComponentIndex(kind)
            ? _focused ? SadConsoleExplorationComponentState.Focused : SadConsoleExplorationComponentState.Selected
            : SadConsoleExplorationComponentState.Unselected;
        return new SadConsoleExplorationComponent(id, kind, title, rows, bounds, state);
    }

    private static int ComponentIndex(SadConsoleExplorationComponentKind kind) => kind switch
    {
        SadConsoleExplorationComponentKind.ScenarioPreviewList => 0,
        SadConsoleExplorationComponentKind.PlayerStartFields => 1,
        SadConsoleExplorationComponentKind.EntityTemplateList => 2,
        SadConsoleExplorationComponentKind.ActionPlanList => 3,
        _ => 0
    };

    private void MoveFocusedList(int delta)
    {
        if (_selectedComponentIndex is 0 or 2 && _scenario.EntityTemplates.Count > 0)
        {
            _entityIndex = Math.Clamp(_entityIndex + delta, 0, _scenario.EntityTemplates.Count - 1);
            _previewIndex = _entityIndex;
        }
        else if (_selectedComponentIndex == 3 && _scenario.ActionPlans.Count > 0)
        {
            _actionPlanIndex = Math.Clamp(_actionPlanIndex + delta, 0, _scenario.ActionPlans.Count - 1);
        }
        else if (_selectedComponentIndex == 0 && _scenario.PreviewRows.Count > 0)
        {
            _previewIndex = Math.Clamp(_previewIndex + delta, 0, _scenario.PreviewRows.Count - 1);
        }
    }

    private static IReadOnlyList<string> Rows<T>(IReadOnlyList<T> items, int selectedIndex, Func<T, string> format)
    {
        if (items.Count == 0)
        {
            return ["(none defined)"];
        }

        return items.Select((item, index) => $"{(index == selectedIndex ? ">" : " ")} {format(item)}").ToList();
    }
}

internal sealed record SadConsoleScenarioEditResult(
    SadConsoleExplorationScreenKind NextScreen,
    SadConsoleEntityTemplateEditItem? EntityTemplate,
    SadConsoleActionPlanEditItem? ActionPlan)
{
    public static SadConsoleScenarioEditResult Stay { get; } = new(SadConsoleExplorationScreenKind.ScenarioEdit, null, null);
    public static SadConsoleScenarioEditResult ReturnToScenarioSelection { get; } = new(SadConsoleExplorationScreenKind.ScenarioSelection, null, null);
    public static SadConsoleScenarioEditResult EditEntity(SadConsoleEntityTemplateEditItem entityTemplate) => new(SadConsoleExplorationScreenKind.EntityTemplateEdit, entityTemplate, null);
    public static SadConsoleScenarioEditResult EditActionPlan(SadConsoleActionPlanEditItem actionPlan) => new(SadConsoleExplorationScreenKind.ActionPlanEdit, null, actionPlan);
}

internal sealed class SadConsoleEntityTemplateEditScreenModel
{
    private readonly SadConsoleEntityTemplateEditItem _entity;
    private int _selectedComponentIndex;
    private bool _focused;

    public SadConsoleEntityTemplateEditScreenModel(SadConsoleEntityTemplateEditItem entity)
    {
        _entity = entity;
    }

    public SadConsoleExplorationScreen BuildScreen()
    {
        var components = new[]
        {
            Component(0, "presentation", SadConsoleExplorationComponentKind.PresentationFields, "3.1 Presentation", [
                $"name: {_entity.Name}",
                $"glyph: {_entity.Glyph}",
                $"color: {_entity.Color}",
                $"action plan: {_entity.ActionPlanId ?? "(none)"}"
            ], new SadConsoleRect(1, 3, 38, 13)),
            Component(1, "targeting", SadConsoleExplorationComponentKind.TargetingFields, "3.2 Targeting", _entity.TargetingSlots.Count == 0 ? ["No targeting slots."] : _entity.TargetingSlots.Select(slot => $"slot {slot.Slot}: {slot.TargetLabel} -> {slot.TargetTemplateOrCriteria}, range {slot.TargetRange}").ToList(), new SadConsoleRect(42, 3, 74, 16)),
            Component(2, "inventory", SadConsoleExplorationComponentKind.InventoryFields, "3.3 Inventory", [
                $"inventory space X: {_entity.InventoryWidth}",
                $"inventory space Y: {_entity.InventoryHeight}",
                $"Aperture: {_entity.Aperture}",
                $"Bulk: {_entity.Bulk}",
                $"brush selection: {(_entity.InventoryItems.FirstOrDefault()?.BrushSelection ?? "(none)")}",
                "3.3.2 inventory drawing panel: deferred component surface"
            ], new SadConsoleRect(1, 18, 115, 36))
        };

        return new SadConsoleExplorationScreen(
            SadConsoleExplorationScreenKind.EntityTemplateEdit,
            $"Edit Entity Template: {_entity.Name}",
            $"Authored entity template {_entity.TemplateId}.",
            components,
            _focused ? "Focused component handles editable fields. Cancel releases focus." : "Select component to edit. Jump to action plan opens the referenced plan. Cancel returns to scenario edit.");
    }

    public SadConsoleEntityTemplateEditResult Handle(SadConsoleExplorationCommand command)
    {
        if (!_focused)
        {
            switch (command)
            {
                case SadConsoleExplorationCommand.MoveNextComponent:
                    _selectedComponentIndex = Math.Min(_selectedComponentIndex + 1, 2);
                    return SadConsoleEntityTemplateEditResult.Stay;
                case SadConsoleExplorationCommand.MovePreviousComponent:
                    _selectedComponentIndex = Math.Max(_selectedComponentIndex - 1, 0);
                    return SadConsoleEntityTemplateEditResult.Stay;
                case SadConsoleExplorationCommand.FocusSelectedComponent:
                case SadConsoleExplorationCommand.Activate:
                    _focused = true;
                    return SadConsoleEntityTemplateEditResult.Stay;
                case SadConsoleExplorationCommand.Cancel:
                    return SadConsoleEntityTemplateEditResult.ReturnToScenarioEdit;
                default:
                    return SadConsoleEntityTemplateEditResult.Stay;
            }
        }

        if (command is SadConsoleExplorationCommand.Cancel or SadConsoleExplorationCommand.ReleaseFocusedComponent)
        {
            _focused = false;
        }

        return SadConsoleEntityTemplateEditResult.Stay;
    }

    public SadConsoleEntityTemplateEditResult JumpToActionPlan(IReadOnlyList<SadConsoleActionPlanEditItem> actionPlans)
    {
        var actionPlan = actionPlans.FirstOrDefault(plan => plan.ActionPlanId == _entity.ActionPlanId);
        return actionPlan is null
            ? SadConsoleEntityTemplateEditResult.Stay
            : SadConsoleEntityTemplateEditResult.EditActionPlan(actionPlan);
    }

    private SadConsoleExplorationComponent Component(int index, string id, SadConsoleExplorationComponentKind kind, string title, IReadOnlyList<string> rows, SadConsoleRect bounds)
    {
        var state = _selectedComponentIndex == index
            ? _focused ? SadConsoleExplorationComponentState.Focused : SadConsoleExplorationComponentState.Selected
            : SadConsoleExplorationComponentState.Unselected;
        return new SadConsoleExplorationComponent(id, kind, title, rows, bounds, state);
    }
}

internal sealed record SadConsoleEntityTemplateEditResult(
    SadConsoleExplorationScreenKind NextScreen,
    SadConsoleActionPlanEditItem? ActionPlan)
{
    public static SadConsoleEntityTemplateEditResult Stay { get; } = new(SadConsoleExplorationScreenKind.EntityTemplateEdit, null);
    public static SadConsoleEntityTemplateEditResult ReturnToScenarioEdit { get; } = new(SadConsoleExplorationScreenKind.ScenarioEdit, null);
    public static SadConsoleEntityTemplateEditResult EditActionPlan(SadConsoleActionPlanEditItem actionPlan) => new(SadConsoleExplorationScreenKind.ActionPlanEdit, actionPlan);
}

internal sealed class SadConsoleActionPlanEditScreenModel
{
    private readonly SadConsoleActionPlanEditItem _actionPlan;
    private readonly SadConsoleExplorationScreenKind _cancelDestination;
    private bool _focused;

    public SadConsoleActionPlanEditScreenModel(SadConsoleActionPlanEditItem actionPlan, SadConsoleExplorationScreenKind cancelDestination)
    {
        _actionPlan = actionPlan;
        _cancelDestination = cancelDestination;
    }

    public SadConsoleExplorationScreen BuildScreen() => new(
        SadConsoleExplorationScreenKind.ActionPlanEdit,
        $"Edit Action Plan: {_actionPlan.ActionPlanId}",
        "Action step editing component is intentionally not fully designed yet.",
        [new SadConsoleExplorationComponent(
            "action-plan-steps",
            SadConsoleExplorationComponentKind.ActionPlanStepList,
            "4.1 Action steps",
            _actionPlan.Steps,
            new SadConsoleRect(1, 3, 115, 36),
            _focused ? SadConsoleExplorationComponentState.Focused : SadConsoleExplorationComponentState.Selected)],
        _focused ? "Future controls: insert, replace, delete, rearrange. Cancel releases focus." : "Select focuses action steps. Cancel returns to prior screen.");

    public SadConsoleActionPlanEditResult Handle(SadConsoleExplorationCommand command)
    {
        if (command is SadConsoleExplorationCommand.FocusSelectedComponent or SadConsoleExplorationCommand.Activate)
        {
            _focused = true;
            return SadConsoleActionPlanEditResult.Stay;
        }

        if (command is SadConsoleExplorationCommand.ReleaseFocusedComponent)
        {
            _focused = false;
            return SadConsoleActionPlanEditResult.Stay;
        }

        if (command == SadConsoleExplorationCommand.Cancel)
        {
            if (_focused)
            {
                _focused = false;
                return SadConsoleActionPlanEditResult.Stay;
            }

            return new SadConsoleActionPlanEditResult(_cancelDestination);
        }

        return SadConsoleActionPlanEditResult.Stay;
    }
}

internal sealed record SadConsoleActionPlanEditResult(SadConsoleExplorationScreenKind NextScreen)
{
    public static SadConsoleActionPlanEditResult Stay { get; } = new(SadConsoleExplorationScreenKind.ActionPlanEdit);
}
