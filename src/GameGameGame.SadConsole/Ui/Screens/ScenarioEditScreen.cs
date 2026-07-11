using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Navigation;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class ScenarioEditScreen
{
    private FrontendEditorSnapshot? _snapshot;
    private readonly FrontendEditorService? _service;
    private readonly FrontendEditorScenarioSummary? _scenario;
    private readonly List<FrontendEditorEntityTemplateSummary> _entities;
    private readonly List<FrontendEditorActionPlanSummary> _actionPlans;
    private readonly List<string> _diagnostics;
    private readonly FocusRouter _focusRouter;
    private IUiComponent? _overlay;
    private int _selectedPreviewIndex;
    private int _selectedEntityIndex;
    private int _selectedActionPlanIndex;
    private const string BackToEditingChoiceId = "back-to-editing";
    private const string SaveAndExitChoiceId = "save-and-exit";
    private const string ExitWithoutSavingChoiceId = "exit-without-saving";

    private ScenarioEditScreen(
        ScenarioCatalogEntry catalogEntry,
        FrontendEditorService? service,
        FrontendEditorSnapshot? snapshot,
        FrontendEditorScenarioSummary? scenario,
        IReadOnlyList<string> diagnostics)
    {
        CatalogEntry = catalogEntry;
        _service = service;
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
    public bool IsDirty => _snapshot?.IsDirty == true;

    public static ScenarioEditOpenResult Open(ScenarioCatalogEntry entry)
    {
        try
        {
            var open = FrontendEditorService.OpenFile(entry.ContentPath);
            if (!open.IsSuccess || open.Service is null)
            {
                return ScenarioEditOpenResult.Success(new ScenarioEditScreen(entry, null, null, null, [open.ErrorMessage ?? $"Could not open {entry.ContentPath}."]));
            }

            var snapshot = open.Service.GetSnapshot();
            var scenario = snapshot.Scenarios.FirstOrDefault(item => item.ScenarioId == entry.ScenarioId);
            var diagnostics = scenario is null
                ? [$"Scenario '{entry.ScenarioId}' was not found in {entry.ContentPath}."]
                : snapshot.ValidationDiagnostics.Select(diagnostic => $"{diagnostic.Severity}: {diagnostic.Message}").ToList();
            return ScenarioEditOpenResult.Success(new ScenarioEditScreen(entry, open.Service, snapshot, scenario, diagnostics));
        }
        catch (Exception ex)
        {
            return ScenarioEditOpenResult.Success(new ScenarioEditScreen(entry, null, null, null, [ex.Message]));
        }
    }

    public static ScenarioEditScreen FromSnapshot(ScenarioCatalogEntry entry, FrontendEditorSnapshot snapshot, FrontendEditorService? service = null)
    {
        var scenario = snapshot.Scenarios.FirstOrDefault(item => item.ScenarioId == entry.ScenarioId);
        return new ScenarioEditScreen(entry, service, snapshot, scenario, []);
    }

    public EntityTemplateEditScreen? OpenEntityTemplateEditScreen(string templateId)
    {
        if (_snapshot is null || !_snapshot.EntityTemplates.Any(template => template.TemplateId == templateId))
        {
            return null;
        }

        return EntityTemplateEditScreen.FromSnapshot(_snapshot, templateId, _service, ReplaceSnapshotAfterChildMutation);
    }

    private void ReplaceSnapshotAfterChildMutation(FrontendEditorSnapshot snapshot)
    {
        _snapshot = snapshot;
        _entities.Clear();
        _entities.AddRange(snapshot.EntityTemplates);
        _actionPlans.Clear();
        _actionPlans.AddRange(snapshot.ActionPlans);
    }

    public ActionPlanEditScreen? OpenActionPlanEditScreen(string actionPlanId, ActionPlanEditReturnDestination returnDestination)
    {
        if (_snapshot is null || !_snapshot.ActionPlans.Any(plan => plan.ActionPlanId == actionPlanId))
        {
            return null;
        }

        return ActionPlanEditScreen.FromSnapshot(_snapshot, actionPlanId, returnDestination, _service, ReplaceSnapshotAfterChildMutation);
    }

    public IReadOnlyList<IUiComponent> Components()
    {
        var components = new List<IUiComponent>
        {
            SaveStatusPanel(),
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

    public IUiComponent? OverlayComponent() => _overlay;

    public string FooterText()
    {
        if (FocusedComponentId is null)
        {
            return IsDirty
                ? "No component focused: arrows choose component. Enter focuses. S saves. Esc opens unsaved-exit options."
                : "No component focused: arrows choose component. Enter focuses. S saves. Esc returns to Scenario Selection.";
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
        if (_overlay is ChoicePickerOverlayComponent picker)
        {
            return HandleOverlay(picker, command);
        }

        if (FocusedComponentId is { } focused)
        {
            return HandleFocused(focused, command);
        }

        var result = _focusRouter.Handle(command);
        return result.Kind switch
        {
            FocusRouterResultKind.CancelScreen => IsDirty ? OpenUnsavedExitModal() : ScenarioEditResult.ReturnToScenarioSelection("Returned to Scenario Selection."),
            FocusRouterResultKind.SelectedComponent => ScenarioEditResult.Stay($"Selected component: {result.ComponentId}."),
            FocusRouterResultKind.FocusedComponent => ScenarioEditResult.Stay($"Focused component: {result.ComponentId}."),
            _ => ScenarioEditResult.Stay("Use arrows to choose a component, Enter to focus, Esc to return.")
        };
    }

    public ScenarioEditResult Save()
    {
        if (_service is null)
        {
            return ScenarioEditResult.Stay("Save requires a service-backed editor screen.");
        }

        var result = _service.Save();
        ReplaceSnapshotAfterChildMutation(result.Snapshot);
        return ScenarioEditResult.Stay(result.StatusMessage);
    }

    private ScenarioEditResult OpenUnsavedExitModal()
    {
        _overlay = new ChoicePickerOverlayComponent(
            "unsaved-exit-confirmation",
            "2.5 Unsaved changes",
            "You have pending changes. What would you like to do?",
            [
                new SelectableListItem(BackToEditingChoiceId, "Back to Editing", "return without saving"),
                new SelectableListItem(SaveAndExitChoiceId, "Save & Exit", "save changes and return to Scenario Selection"),
                new SelectableListItem(ExitWithoutSavingChoiceId, "Exit without Saving", "discard pending editor-session changes")
            ],
            SadConsoleRect.FromSize(32, 10, 62, 10),
            0);
        return ScenarioEditResult.Stay("Unsaved changes: choose Back to Editing, Save & Exit, or Exit without Saving.");
    }

    private ScenarioEditResult HandleOverlay(ChoicePickerOverlayComponent picker, UiComponentCommand command)
    {
        var result = picker.Handle(command);
        if (result.Kind == FieldEditorOverlayResultKind.Cancelled)
        {
            _overlay = null;
            return ScenarioEditResult.Stay("Back to editing.");
        }

        if (result.Kind != FieldEditorOverlayResultKind.Confirmed || result.Value is not { } choice)
        {
            return ScenarioEditResult.Stay(result.Message);
        }

        return choice.Id switch
        {
            BackToEditingChoiceId => BackToEditing(),
            SaveAndExitChoiceId => SaveAndExit(),
            ExitWithoutSavingChoiceId => ScenarioEditResult.ReturnToScenarioSelection("Exited without saving pending changes."),
            _ => ScenarioEditResult.Stay("Unknown unsaved-exit choice.")
        };
    }

    private ScenarioEditResult BackToEditing()
    {
        _overlay = null;
        return ScenarioEditResult.Stay("Back to editing.");
    }

    private ScenarioEditResult SaveAndExit()
    {
        var save = Save();
        if (IsDirty)
        {
            _overlay = null;
            return save;
        }

        return ScenarioEditResult.ReturnToScenarioSelection(save.Message);
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

    private PanelComponent SaveStatusPanel() => new(
        "save-status",
        IsDirty ? "Unsaved changes" : "Saved",
        new SadConsoleRect(98, 3, 19, 8),
        IsDirty
            ? ["status: dirty", "S: save"]
            : ["status: saved", "S: save"],
        IsDirty ? UiComponentState.Dirty : UiComponentState.Saved);

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
