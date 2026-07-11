using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Navigation;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class ScenarioEditScreen
{
    private readonly FrontendEditorSnapshot? _snapshot;
    private readonly FrontendEditorScenarioSummary? _scenario;
    private readonly List<FrontendEditorEntityTemplateSummary> _entities;
    private readonly List<FrontendEditorActionPlanSummary> _actionPlans;
    private readonly List<string> _diagnostics;
    private readonly FocusRouter _focusRouter;
    private int _selectedPreviewIndex;
    private int _selectedEntityIndex;
    private int _selectedActionPlanIndex;

    private ScenarioEditScreen(
        ScenarioCatalogEntry catalogEntry,
        FrontendEditorSnapshot? snapshot,
        FrontendEditorScenarioSummary? scenario,
        IReadOnlyList<string> diagnostics)
    {
        CatalogEntry = catalogEntry;
        _snapshot = snapshot;
        _scenario = scenario;
        _entities = snapshot?.EntityTemplates.ToList() ?? [];
        _actionPlans = snapshot?.ActionPlans.ToList() ?? [];
        _diagnostics = diagnostics.ToList();
        _focusRouter = new FocusRouter([
            new FocusTarget("scenario-preview"),
            new FocusTarget("player-start"),
            new FocusTarget("entity-list"),
            new FocusTarget("action-plan-list")
        ]);
    }

    public ScenarioCatalogEntry CatalogEntry { get; }
    public string Title => $"Scenario Edit: {CatalogEntry.Name}";
    public string Purpose => "Review authored scenario fields, defined entities, defined action plans, and turn-0 preview facts.";
    public string? SelectedComponentId => _focusRouter.SelectedComponentId;
    public string? FocusedComponentId => _focusRouter.FocusedComponentId;
    public int SelectedEntityIndex => _selectedEntityIndex;
    public int SelectedActionPlanIndex => _selectedActionPlanIndex;

    public static ScenarioEditOpenResult Open(ScenarioCatalogEntry entry)
    {
        try
        {
            var open = FrontendEditorService.OpenFile(entry.ContentPath);
            if (!open.IsSuccess || open.Service is null)
            {
                return ScenarioEditOpenResult.Success(new ScenarioEditScreen(entry, null, null, [open.ErrorMessage ?? $"Could not open {entry.ContentPath}."]));
            }

            var snapshot = open.Service.GetSnapshot();
            var scenario = snapshot.Scenarios.FirstOrDefault(item => item.ScenarioId == entry.ScenarioId);
            var diagnostics = scenario is null
                ? [$"Scenario '{entry.ScenarioId}' was not found in {entry.ContentPath}."]
                : snapshot.ValidationDiagnostics.Select(diagnostic => $"{diagnostic.Severity}: {diagnostic.Message}").ToList();
            return ScenarioEditOpenResult.Success(new ScenarioEditScreen(entry, snapshot, scenario, diagnostics));
        }
        catch (Exception ex)
        {
            return ScenarioEditOpenResult.Success(new ScenarioEditScreen(entry, null, null, [ex.Message]));
        }
    }

    public static ScenarioEditScreen FromSnapshot(ScenarioCatalogEntry entry, FrontendEditorSnapshot snapshot)
    {
        var scenario = snapshot.Scenarios.FirstOrDefault(item => item.ScenarioId == entry.ScenarioId);
        return new ScenarioEditScreen(entry, snapshot, scenario, []);
    }

    public EntityTemplateEditScreen? OpenEntityTemplateEditScreen(string templateId)
    {
        if (_snapshot is null || !_snapshot.EntityTemplates.Any(template => template.TemplateId == templateId))
        {
            return null;
        }

        return EntityTemplateEditScreen.FromSnapshot(_snapshot, templateId);
    }

    public ActionPlanEditScreen? OpenActionPlanEditScreen(string actionPlanId, ActionPlanEditReturnDestination returnDestination)
    {
        if (_snapshot is null || !_snapshot.ActionPlans.Any(plan => plan.ActionPlanId == actionPlanId))
        {
            return null;
        }

        return ActionPlanEditScreen.FromSnapshot(_snapshot, actionPlanId, returnDestination);
    }

    public IReadOnlyList<IUiComponent> Components()
    {
        var components = new List<IUiComponent>
        {
            ScenarioPreview(),
            PlayerStartFields(),
            EntityList(),
            ActionPlanList()
        };

        if (_diagnostics.Count > 0)
        {
            components.Add(new PanelComponent(
                "scenario-edit-diagnostics",
                "Diagnostics",
                new SadConsoleRect(1, 34, 116, 40),
                _diagnostics.Take(4).ToList(),
                UiComponentState.Error));
        }

        return components;
    }

    public string FooterText()
    {
        if (FocusedComponentId is null)
        {
            return "No component focused: arrows choose component. Enter focuses. Esc returns to Scenario Selection.";
        }

        return FocusedComponentId switch
        {
            "scenario-preview" => "Preview focused: Up/Down chooses preview entity row. Enter opens Entity Template screen. Esc releases focus.",
            "player-start" => "Player start focused: fields are review-only in this demo. Esc releases focus.",
            "entity-list" => "Entity list focused: Up/Down chooses entity. Enter opens Entity Template screen. Esc releases focus.",
            "action-plan-list" => "Action plan list focused: Up/Down chooses plan. Enter opens Action Plan screen. Esc releases focus.",
            _ => "Esc releases focus."
        };
    }

    public ScenarioEditResult Handle(UiComponentCommand command)
    {
        if (FocusedComponentId is { } focused)
        {
            return HandleFocused(focused, command);
        }

        var result = _focusRouter.Handle(command);
        return result.Kind switch
        {
            FocusRouterResultKind.CancelScreen => ScenarioEditResult.ReturnToScenarioSelection("Returned to Scenario Selection."),
            FocusRouterResultKind.SelectedComponent => ScenarioEditResult.Stay($"Selected component: {result.ComponentId}."),
            FocusRouterResultKind.FocusedComponent => ScenarioEditResult.Stay($"Focused component: {result.ComponentId}."),
            _ => ScenarioEditResult.Stay("Use arrows to choose a component, Enter to focus, Esc to return.")
        };
    }

    private ScenarioEditResult HandleFocused(string focused, UiComponentCommand command)
    {
        if (command == UiComponentCommand.Cancel)
        {
            _focusRouter.Handle(UiComponentCommand.Cancel);
            return ScenarioEditResult.Stay($"Released focus from {focused}.");
        }

        if (command is UiComponentCommand.Up or UiComponentCommand.Left)
        {
            MoveFocusedSelection(focused, -1);
            return ScenarioEditResult.Stay(FocusedSelectionMessage(focused));
        }

        if (command is UiComponentCommand.Down or UiComponentCommand.Right)
        {
            MoveFocusedSelection(focused, 1);
            return ScenarioEditResult.Stay(FocusedSelectionMessage(focused));
        }

        if (command == UiComponentCommand.Select)
        {
            if (focused is "scenario-preview" or "entity-list")
            {
                var entity = SelectedEntity();
                return entity is null
                    ? ScenarioEditResult.Stay("No entity template is available to open.")
                    : ScenarioEditResult.OpenEntity(entity.TemplateId, $"Entity Template screen next: {entity.Name} ({entity.TemplateId}).");
            }

            if (focused == "action-plan-list")
            {
                var plan = SelectedActionPlan();
                return plan is null
                    ? ScenarioEditResult.Stay("No action plan is available to open.")
                    : ScenarioEditResult.OpenActionPlan(plan.ActionPlanId, $"Action Plan screen next: {plan.ActionPlanId}.");
            }
        }

        return ScenarioEditResult.Stay("No action for current field yet.");
    }

    private void MoveFocusedSelection(string focused, int delta)
    {
        if (focused is "scenario-preview" or "entity-list" && _entities.Count > 0)
        {
            _selectedEntityIndex = Math.Clamp(_selectedEntityIndex + delta, 0, _entities.Count - 1);
            _selectedPreviewIndex = _selectedEntityIndex;
        }
        else if (focused == "action-plan-list" && _actionPlans.Count > 0)
        {
            _selectedActionPlanIndex = Math.Clamp(_selectedActionPlanIndex + delta, 0, _actionPlans.Count - 1);
        }
    }

    private string FocusedSelectionMessage(string focused) => focused switch
    {
        "scenario-preview" or "entity-list" => SelectedEntity() is { } entity ? $"Selected entity: {entity.Name} ({entity.TemplateId})." : "No entity selected.",
        "action-plan-list" => SelectedActionPlan() is { } plan ? $"Selected action plan: {plan.ActionPlanId}." : "No action plan selected.",
        _ => "Field selection unchanged."
    };

    private IUiComponent ScenarioPreview()
    {
        var rows = _scenario is null
            ? new List<string> { "No scenario preview available." }
            : new List<string>
            {
                $"Turn-0 preview for: {_scenario.Name}",
                $"Root template: {_scenario.ScenarioRootEntityTemplateId}",
                $"Player template: {_scenario.PlayerEntityTemplateId}",
                "Derived runtime preview tree placeholder:"
            };
        rows.AddRange(_entities.Select((entity, index) => $"{(index == _selectedPreviewIndex ? ">" : " ")} {entity.Glyph} {entity.Name} ({entity.TemplateId})"));

        return new PanelComponent(
            "scenario-preview",
            "2.1 Scenario preview",
            new SadConsoleRect(1, 4, 57, 20),
            rows,
            _focusRouter.StateFor("scenario-preview"));
    }

    private IUiComponent PlayerStartFields()
    {
        return new FieldGroupComponent(
            "player-start",
            "2.2 Player starting position",
            new SadConsoleRect(61, 4, 56, 13),
            [
                new EditableFieldComponent("scenario-root", "scenario root", _scenario?.ScenarioRootEntityTemplateId ?? "(missing)", EditableFieldMode.ReadOnly),
                new EditableFieldComponent("player-x", "player X position", _scenario?.PlayerStart.X.ToString() ?? "?", EditableFieldMode.ReadOnly),
                new EditableFieldComponent("player-y", "player Y position", _scenario?.PlayerStart.Y.ToString() ?? "?", EditableFieldMode.ReadOnly)
            ],
            _focusRouter.StateFor("player-start"));
    }

    private SelectableListComponent EntityList()
    {
        var list = new SelectableListComponent(
            "entity-list",
            "2.3 Defined entities",
            new SadConsoleRect(1, 22, 57, 33),
            _entities.Select(entity => new SelectableListItem(entity.TemplateId, $"{entity.Glyph} {entity.Name}", entity.TemplateId)),
            _focusRouter.StateFor("entity-list"),
            visibleRowCount: 9);
        for (var index = 0; index < _selectedEntityIndex; index++) list.MoveSelection(1);
        return list;
    }

    private SelectableListComponent ActionPlanList()
    {
        var list = new SelectableListComponent(
            "action-plan-list",
            "2.4 Defined action plans",
            new SadConsoleRect(61, 15, 56, 33),
            _actionPlans.Select(plan => new SelectableListItem(plan.ActionPlanId, plan.ActionPlanId, $"{plan.ActionSteps.Count} steps | {plan.Shape}")),
            _focusRouter.StateFor("action-plan-list"),
            visibleRowCount: 16);
        for (var index = 0; index < _selectedActionPlanIndex; index++) list.MoveSelection(1);
        return list;
    }

    private FrontendEditorEntityTemplateSummary? SelectedEntity() => _entities.Count == 0 ? null : _entities[_selectedEntityIndex];
    private FrontendEditorActionPlanSummary? SelectedActionPlan() => _actionPlans.Count == 0 ? null : _actionPlans[_selectedActionPlanIndex];
}

internal sealed record ScenarioEditOpenResult(ScenarioEditScreen Screen)
{
    public static ScenarioEditOpenResult Success(ScenarioEditScreen screen) => new(screen);
}

internal sealed record ScenarioEditResult(ScenarioEditResultKind Kind, string Message, string? EntityTemplateId = null, string? ActionPlanId = null)
{
    public static ScenarioEditResult Stay(string message) => new(ScenarioEditResultKind.Stay, message);
    public static ScenarioEditResult ReturnToScenarioSelection(string message) => new(ScenarioEditResultKind.ReturnToScenarioSelection, message);
    public static ScenarioEditResult OpenEntity(string entityTemplateId, string message) => new(ScenarioEditResultKind.OpenEntityTemplate, message, EntityTemplateId: entityTemplateId);
    public static ScenarioEditResult OpenActionPlan(string actionPlanId, string message) => new(ScenarioEditResultKind.OpenActionPlan, message, ActionPlanId: actionPlanId);
}

internal enum ScenarioEditResultKind
{
    Stay,
    ReturnToScenarioSelection,
    OpenEntityTemplate,
    OpenActionPlan
}
