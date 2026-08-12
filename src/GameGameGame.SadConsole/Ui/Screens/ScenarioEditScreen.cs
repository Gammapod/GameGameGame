using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Navigation;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class ScenarioEditScreen
{
    private FrontendEditorSnapshot? _snapshot;
    private readonly FrontendEditorService? _service;
    private readonly FrontendEditorScenarioSummary? _scenario;
    private ContentScenarioSurface? _scenarioSurface;
    private readonly List<FrontendEditorEntityTemplateSummary> _entities;
    private readonly List<FrontendEditorActionPlanSummary> _actionPlans;
    private readonly List<string> _diagnostics;
    private readonly FocusRouter _focusRouter;
    private IUiComponent? _overlay;
    private ScenarioEditOverlayMode? _overlayMode;
    private int _selectedPreviewIndex;
    private int _selectedEntityIndex;
    private int _selectedActionPlanIndex;
    private const string BackToEditingChoiceId = "back-to-editing";
    private const string SaveAndExitChoiceId = "save-and-exit";
    private const string ExitWithoutSavingChoiceId = "exit-without-saving";
    private const string CreateEntityChoiceId = "create-template";
    private const string EditEntityChoiceId = "edit-template";
    private const string DuplicateEntityChoiceId = "duplicate-template";
    private const string DeleteEntityChoiceId = "delete-template";
    private const string CreateActionPlanChoiceId = "create-action-plan";
    private const string EditActionPlanChoiceId = "edit-action-plan";
    private const string DuplicateActionPlanChoiceId = "duplicate-action-plan";
    private const string DeleteActionPlanChoiceId = "delete-action-plan";

    private ScenarioEditScreen(
        ScenarioCatalogEntry catalogEntry,
        FrontendEditorService? service,
        FrontendEditorSnapshot? snapshot,
        FrontendEditorScenarioSummary? scenario,
        ContentScenarioSurface? scenarioSurface,
        IReadOnlyList<string> diagnostics)
    {
        CatalogEntry = catalogEntry;
        _service = service;
        _snapshot = snapshot;
        _scenario = scenario;
        _scenarioSurface = scenarioSurface;
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
    public bool IsTextEntryOverlayActive => _overlay is TextEntryOverlayComponent;

    public static ScenarioEditOpenResult Open(ScenarioCatalogEntry entry)
    {
        try
        {
            var open = FrontendEditorService.OpenFile(entry.ContentPath);
            if (!open.IsSuccess || open.Service is null)
            {
                return ScenarioEditOpenResult.Success(new ScenarioEditScreen(entry, null, null, null, null, [open.ErrorMessage ?? $"Could not open {entry.ContentPath}."]));
            }

            var snapshot = open.Service.GetSnapshot();
            var scenario = snapshot.Scenarios.FirstOrDefault(item => item.ScenarioId == entry.ScenarioId);
            var scenarioSurface = scenario is null
                ? null
                : open.Service.BuildScenarioSurface(entry.ScenarioId, new ContentCompileOptions(SourcePath: entry.ContentPath));
            var diagnostics = scenario is null
                ? [$"Scenario '{entry.ScenarioId}' was not found in {entry.ContentPath}."]
                : BuildDiagnostics(snapshot, scenarioSurface);
            return ScenarioEditOpenResult.Success(new ScenarioEditScreen(entry, open.Service, snapshot, scenario, scenarioSurface, diagnostics));
        }
        catch (Exception ex)
        {
            return ScenarioEditOpenResult.Success(new ScenarioEditScreen(entry, null, null, null, null, [ex.Message]));
        }
    }

    public static ScenarioEditScreen FromSnapshot(ScenarioCatalogEntry entry, FrontendEditorSnapshot snapshot, FrontendEditorService? service = null)
    {
        var scenario = snapshot.Scenarios.FirstOrDefault(item => item.ScenarioId == entry.ScenarioId);
        var scenarioSurface = service is null || scenario is null
            ? null
            : service.BuildScenarioSurface(entry.ScenarioId, new ContentCompileOptions(SourcePath: entry.ContentPath));
        return new ScenarioEditScreen(entry, service, snapshot, scenario, scenarioSurface, []);
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
        _scenarioSurface = _service is null || _scenario is null
            ? null
            : _service.BuildScenarioSurface(_scenario.ScenarioId, new ContentCompileOptions(SourcePath: CatalogEntry.ContentPath));
    }

    private static List<string> BuildDiagnostics(FrontendEditorSnapshot snapshot, ContentScenarioSurface? scenarioSurface)
    {
        if (scenarioSurface is null)
        {
            return snapshot.ValidationDiagnostics.Select(diagnostic => $"{diagnostic.Severity}: {diagnostic.Message}").ToList();
        }

        var rows = scenarioSurface.SelectedScenarioDiagnostics
            .Select(diagnostic => $"{diagnostic.Severity}: {diagnostic.Message}")
            .ToList();
        rows.AddRange(scenarioSurface.SelectedScenarioReferences
            .Where(reference => reference.Resolution == ContentReferenceResolution.Missing)
            .Select(reference => $"Missing {reference.Kind}: {reference.SourceId} -> {reference.TargetId}"));
        rows.AddRange(scenarioSurface.GlobalDiagnostics
            .Take(Math.Max(0, 4 - rows.Count))
            .Select(diagnostic => $"{diagnostic.Severity}: {diagnostic.Message}"));
        return rows;
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

    public ScenarioEditResult InsertText(string text)
    {
        if (_overlay is not TextEntryOverlayComponent textEntry) return ScenarioEditResult.Stay("No text field editor is open.");
        textEntry.InsertText(text);
        return ScenarioEditResult.Stay("Typing text field value.");
    }

    public ScenarioEditResult Backspace()
    {
        if (_overlay is not TextEntryOverlayComponent textEntry) return ScenarioEditResult.Stay("No text field editor is open.");
        textEntry.Backspace();
        return ScenarioEditResult.Stay("Typing text field value.");
    }

    public string FooterText()
    {
        if (FocusedComponentId is null)
        {
            return IsDirty
                ? "No component focused: arrows choose component. Enter focuses. P plays selected scenario. S saves. Esc opens unsaved-exit options."
                : "No component focused: arrows choose component. Enter focuses. P plays selected scenario. S saves. Esc returns to Scenario Selection.";
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
        if (_overlay is TextEntryOverlayComponent textEntry)
        {
            return HandleTextOverlay(textEntry, command);
        }

        if (_overlay is ConfirmOverlayComponent confirm)
        {
            return HandleConfirmOverlay(confirm, command);
        }

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
        if (result.IsSuccess && _scenario is not null)
        {
            _ = _service.PreviewScenario(_scenario.ScenarioId);
            return ScenarioEditResult.Stay($"{result.StatusMessage} Preview refreshed.");
        }

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
            ClearOverlay();
            return ScenarioEditResult.Stay("Back to editing.");
        }

        if (result.Kind != FieldEditorOverlayResultKind.Confirmed || result.Value is not { } choice)
        {
            return ScenarioEditResult.Stay(result.Message);
        }

        if (picker.Id == "entity-template-actions") return HandleEntityActionChoice(choice.Id);
        if (picker.Id == "action-plan-actions") return HandleActionPlanActionChoice(choice.Id);

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
        ClearOverlay();
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

    private ScenarioEditResult HandleTextOverlay(TextEntryOverlayComponent textEntry, UiComponentCommand command)
    {
        var result = textEntry.Handle(command);
        if (result.Kind == FieldEditorOverlayResultKind.Cancelled)
        {
            ClearOverlay();
            return ScenarioEditResult.Stay(result.Message);
        }

        if (result.Kind != FieldEditorOverlayResultKind.Confirmed)
        {
            return ScenarioEditResult.Stay(result.Message);
        }

        return _overlayMode switch
        {
            ScenarioEditOverlayMode.CreateEntityTemplateName => CreateEntityTemplate(result.Value),
            ScenarioEditOverlayMode.DuplicateEntityTemplateName => DuplicateSelectedEntityTemplate(result.Value),
            ScenarioEditOverlayMode.CreateActionPlanName => CreateActionPlan(result.Value),
            ScenarioEditOverlayMode.DuplicateActionPlanName => DuplicateSelectedActionPlan(result.Value),
            _ => ScenarioEditResult.Stay("No text-entry operation is active.")
        };
    }

    private ScenarioEditResult HandleConfirmOverlay(ConfirmOverlayComponent confirm, UiComponentCommand command)
    {
        var result = confirm.Handle(command);
        if (result.Kind == FieldEditorOverlayResultKind.Cancelled)
        {
            ClearOverlay();
            return ScenarioEditResult.Stay(result.Message);
        }

        if (result.Kind != FieldEditorOverlayResultKind.Confirmed)
        {
            return ScenarioEditResult.Stay(result.Message);
        }

        return _overlayMode switch
        {
            ScenarioEditOverlayMode.DeleteEntityTemplateConfirm => DeleteSelectedEntityTemplate(),
            ScenarioEditOverlayMode.DeleteActionPlanConfirm => DeleteSelectedActionPlan(),
            _ => ScenarioEditResult.Stay("No delete confirmation is active.")
        };
    }

    private void ClearOverlay()
    {
        _overlay = null;
        _overlayMode = null;
    }

    private ScenarioEditResult HandleEntityActionChoice(string choiceId)
    {
        return choiceId switch
        {
            EditEntityChoiceId => SelectedEntity() is { } entity ? ScenarioEditResult.OpenEntity(entity.TemplateId, $"Entity Template screen next: {entity.Name} ({entity.TemplateId}).") : ScenarioEditResult.Stay("No entity template is selected."),
            DuplicateEntityChoiceId => OpenEntityNameEntry(ScenarioEditOverlayMode.DuplicateEntityTemplateName, $"Duplicate {SelectedEntity()?.Name ?? "template"}", $"{SelectedEntity()?.Name ?? "Template"} Copy"),
            DeleteEntityChoiceId => OpenDeleteEntityConfirmation(),
            _ => ScenarioEditResult.Stay("Unknown entity-template action.")
        };
    }

    private ScenarioEditResult HandleActionPlanActionChoice(string choiceId)
    {
        return choiceId switch
        {
            EditActionPlanChoiceId => SelectedActionPlan() is { } plan ? ScenarioEditResult.OpenActionPlan(plan.ActionPlanId, $"Action Plan screen next: {plan.ActionPlanId}.") : ScenarioEditResult.Stay("No action plan is selected."),
            DuplicateActionPlanChoiceId => OpenActionPlanNameEntry(ScenarioEditOverlayMode.DuplicateActionPlanName, $"Duplicate {SelectedActionPlan()?.ActionPlanId ?? "action plan"}", $"{SelectedActionPlan()?.ActionPlanId ?? "Action Plan"} Copy"),
            DeleteActionPlanChoiceId => OpenDeleteActionPlanConfirmation(),
            _ => ScenarioEditResult.Stay("Unknown action-plan action.")
        };
    }

    private ScenarioEditResult OpenEntityNameEntry(ScenarioEditOverlayMode mode, string title, string initialValue)
    {
        _overlayMode = mode;
        _overlay = new TextEntryOverlayComponent("entity-template-name-entry", title, "template name", initialValue, SadConsoleRect.FromSize(34, 8, 58, 7), maxLength: 80, allowEmpty: false);
        return ScenarioEditResult.Stay($"Opened template name entry for {title}.");
    }

    private ScenarioEditResult OpenActionPlanNameEntry(ScenarioEditOverlayMode mode, string title, string initialValue)
    {
        _overlayMode = mode;
        _overlay = new TextEntryOverlayComponent("action-plan-name-entry", title, "action plan name", initialValue, SadConsoleRect.FromSize(34, 8, 58, 7), maxLength: 80, allowEmpty: false);
        return ScenarioEditResult.Stay($"Opened action-plan name entry for {title}.");
    }

    private ScenarioEditResult CreateEntityTemplate(string name)
    {
        if (_service is null) return CloseOverlayWith("Template creation requires a service-backed editor screen.");
        var before = _entities.Select(entity => entity.TemplateId).ToHashSet(StringComparer.Ordinal);
        var result = _service.CreateEntityTemplate(name);
        ReplaceSnapshotAfterChildMutation(result.Snapshot);
        ClearOverlay();
        var createdId = _entities.FirstOrDefault(entity => !before.Contains(entity.TemplateId))?.TemplateId;
        return result.IsSuccess && createdId is not null ? ScenarioEditResult.OpenEntity(createdId, result.StatusMessage) : ScenarioEditResult.Stay(result.StatusMessage);
    }

    private ScenarioEditResult DuplicateSelectedEntityTemplate(string name)
    {
        if (_service is null) return CloseOverlayWith("Template duplication requires a service-backed editor screen.");
        if (SelectedEntity() is not { } source) return CloseOverlayWith("No entity template is selected to duplicate.");
        var before = _entities.Select(entity => entity.TemplateId).ToHashSet(StringComparer.Ordinal);
        var result = _service.DuplicateEntityTemplate(source.TemplateId, name);
        ReplaceSnapshotAfterChildMutation(result.Snapshot);
        ClearOverlay();
        var createdId = _entities.FirstOrDefault(entity => !before.Contains(entity.TemplateId))?.TemplateId;
        return result.IsSuccess && createdId is not null ? ScenarioEditResult.OpenEntity(createdId, result.StatusMessage) : ScenarioEditResult.Stay(result.StatusMessage);
    }

    private ScenarioEditResult DeleteSelectedEntityTemplate()
    {
        if (_service is null) return CloseOverlayWith("Template deletion requires a service-backed editor screen.");
        if (SelectedEntity() is not { } entity) return CloseOverlayWith("No entity template is selected to delete.");
        var result = _service.DeleteEntityTemplate(entity.TemplateId);
        ReplaceSnapshotAfterChildMutation(result.Snapshot);
        _selectedEntityIndex = Math.Clamp(_selectedEntityIndex, 0, _entities.Count);
        _selectedPreviewIndex = Math.Clamp(_selectedPreviewIndex, 0, Math.Max(0, _entities.Count - 1));
        ClearOverlay();
        return ScenarioEditResult.Stay(result.StatusMessage);
    }

    private ScenarioEditResult CreateActionPlan(string name)
    {
        if (_service is null) return CloseOverlayWith("Action-plan creation requires a service-backed editor screen.");
        var before = _actionPlans.Select(plan => plan.ActionPlanId).ToHashSet(StringComparer.Ordinal);
        var result = _service.CreateActionPlan(name);
        ReplaceSnapshotAfterChildMutation(result.Snapshot);
        ClearOverlay();
        var createdId = _actionPlans.FirstOrDefault(plan => !before.Contains(plan.ActionPlanId))?.ActionPlanId;
        return result.IsSuccess && createdId is not null ? ScenarioEditResult.OpenActionPlan(createdId, result.StatusMessage) : ScenarioEditResult.Stay(result.StatusMessage);
    }

    private ScenarioEditResult DuplicateSelectedActionPlan(string name)
    {
        if (_service is null) return CloseOverlayWith("Action-plan duplication requires a service-backed editor screen.");
        if (SelectedActionPlan() is not { } source) return CloseOverlayWith("No action plan is selected to duplicate.");
        var before = _actionPlans.Select(plan => plan.ActionPlanId).ToHashSet(StringComparer.Ordinal);
        var result = _service.DuplicateActionPlan(source.ActionPlanId, name);
        ReplaceSnapshotAfterChildMutation(result.Snapshot);
        ClearOverlay();
        var createdId = _actionPlans.FirstOrDefault(plan => !before.Contains(plan.ActionPlanId))?.ActionPlanId;
        return result.IsSuccess && createdId is not null ? ScenarioEditResult.OpenActionPlan(createdId, result.StatusMessage) : ScenarioEditResult.Stay(result.StatusMessage);
    }

    private ScenarioEditResult DeleteSelectedActionPlan()
    {
        if (_service is null) return CloseOverlayWith("Action-plan deletion requires a service-backed editor screen.");
        if (SelectedActionPlan() is not { } plan) return CloseOverlayWith("No action plan is selected to delete.");
        var result = _service.DeleteActionPlan(plan.ActionPlanId);
        ReplaceSnapshotAfterChildMutation(result.Snapshot);
        _selectedActionPlanIndex = Math.Clamp(_selectedActionPlanIndex, 0, _actionPlans.Count);
        ClearOverlay();
        return ScenarioEditResult.Stay(result.StatusMessage);
    }

    private ScenarioEditResult CloseOverlayWith(string message)
    {
        ClearOverlay();
        return ScenarioEditResult.Stay(message);
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
                if (focused == "entity-list" && _selectedEntityIndex == 0)
                {
                    return OpenEntityNameEntry(ScenarioEditOverlayMode.CreateEntityTemplateName, "Create new template", "New Template");
                }

                return OpenEntityActionModal();
            }

            if (focused == "action-plan-list")
            {
                if (_selectedActionPlanIndex == 0)
                {
                    return OpenActionPlanNameEntry(ScenarioEditOverlayMode.CreateActionPlanName, "Create new action plan", "New Action Plan");
                }

                return OpenActionPlanActionModal();
            }
        }

        return ScenarioEditResult.Stay("No action for current field yet.");
    }

    private void MoveFocusedSelection(string focused, int delta)
    {
        if (focused is "scenario-preview" or "entity-list" && _entities.Count > 0)
        {
            if (focused == "entity-list")
            {
                _selectedEntityIndex = Math.Clamp(_selectedEntityIndex + delta, 0, _entities.Count);
                _selectedPreviewIndex = Math.Clamp(_selectedEntityIndex - 1, 0, _entities.Count - 1);
            }
            else
            {
                _selectedPreviewIndex = Math.Clamp(_selectedPreviewIndex + delta, 0, _entities.Count - 1);
                _selectedEntityIndex = _selectedPreviewIndex + 1;
            }
        }
        else if (focused == "action-plan-list" && _actionPlans.Count > 0)
        {
            _selectedActionPlanIndex = Math.Clamp(_selectedActionPlanIndex + delta, 0, _actionPlans.Count);
        }
    }

    private string FocusedSelectionMessage(string focused) => focused switch
    {
        "entity-list" when _selectedEntityIndex == 0 => "Selected Create New Template.",
        "scenario-preview" or "entity-list" => SelectedEntity() is { } entity ? $"Selected entity: {entity.Name} ({entity.TemplateId})." : "No entity selected.",
        "action-plan-list" when _selectedActionPlanIndex == 0 => "Selected Create New Action Plan.",
        "action-plan-list" => SelectedActionPlan() is { } plan ? $"Selected action plan: {plan.ActionPlanId}." : "No action plan selected.",
        _ => "Field selection unchanged."
    };

    private ScenarioEditResult OpenEntityActionModal()
    {
        if (SelectedEntity() is not { } entity) return ScenarioEditResult.Stay("No entity template is selected.");
        _overlay = new ChoicePickerOverlayComponent(
            "entity-template-actions",
            $"2.3.1 Entity Template: {entity.Name}",
            "entity template action",
            [
                new SelectableListItem(EditEntityChoiceId, "Edit Template", entity.TemplateId),
                new SelectableListItem(DuplicateEntityChoiceId, "Duplicate Template", entity.TemplateId),
                new SelectableListItem(DeleteEntityChoiceId, "Delete Template", entity.TemplateId)
            ],
            SadConsoleRect.FromSize(34, 9, 54, 10),
            0);
        return ScenarioEditResult.Stay($"Opened 2.3.1 actions for {entity.Name}.");
    }

    private ScenarioEditResult OpenActionPlanActionModal()
    {
        if (SelectedActionPlan() is not { } plan) return ScenarioEditResult.Stay("No action plan is selected.");
        _overlay = new ChoicePickerOverlayComponent(
            "action-plan-actions",
            $"2.4.1 Action Plan: {plan.ActionPlanId}",
            "action plan action",
            [
                new SelectableListItem(EditActionPlanChoiceId, "Edit Action Plan", plan.ActionPlanId),
                new SelectableListItem(DuplicateActionPlanChoiceId, "Duplicate Action Plan", plan.ActionPlanId),
                new SelectableListItem(DeleteActionPlanChoiceId, "Delete Action Plan", plan.ActionPlanId)
            ],
            SadConsoleRect.FromSize(34, 9, 54, 10),
            0);
        return ScenarioEditResult.Stay($"Opened 2.4.1 actions for {plan.ActionPlanId}.");
    }

    private ScenarioEditResult OpenDeleteEntityConfirmation()
    {
        if (SelectedEntity() is not { } entity) return ScenarioEditResult.Stay("No entity template is selected.");
        _overlayMode = ScenarioEditOverlayMode.DeleteEntityTemplateConfirm;
        _overlay = new ConfirmOverlayComponent("delete-template-confirm", "Delete template", $"Delete template {entity.Name} ({entity.TemplateId})?", SadConsoleRect.FromSize(34, 10, 58, 8), "Delete Template", "Back");
        return ScenarioEditResult.Stay($"Confirm delete for template {entity.TemplateId}.");
    }

    private ScenarioEditResult OpenDeleteActionPlanConfirmation()
    {
        if (SelectedActionPlan() is not { } plan) return ScenarioEditResult.Stay("No action plan is selected.");
        _overlayMode = ScenarioEditOverlayMode.DeleteActionPlanConfirm;
        _overlay = new ConfirmOverlayComponent("delete-action-plan-confirm", "Delete action plan", $"Delete action plan {plan.ActionPlanId}?", SadConsoleRect.FromSize(34, 10, 58, 8), "Delete Action Plan", "Back");
        return ScenarioEditResult.Stay($"Confirm delete for action plan {plan.ActionPlanId}.");
    }

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
                $"Root template: {_scenarioSurface?.RootTemplateId ?? _scenario.ScenarioRootEntityTemplateId}",
                $"Player template: {_scenarioSurface?.PlayerTemplateId ?? _scenario.PlayerEntityTemplateId}",
                _scenarioSurface is null
                    ? "Type-first surface: not available"
                    : $"Type-first surface: {_scenarioSurface.Workspace.Scenarios.Count} scenarios, {_scenarioSurface.Workspace.EntityTemplates.Count} templates, {_scenarioSurface.Workspace.ActionPlans.Count} action plans",
                _scenarioSurface is null
                    ? "Scenario refs: not available"
                    : $"Scenario refs: {_scenarioSurface.SelectedScenarioReferences.Count} | dependencies: {_scenarioSurface.DependencySymbols.Count}",
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
                new EditableFieldComponent("player-x", "player X position", (_scenarioSurface?.PlayerStart?.X ?? _scenario?.PlayerStart.X)?.ToString() ?? "?", EditableFieldMode.ReadOnly),
                new EditableFieldComponent("player-y", "player Y position", (_scenarioSurface?.PlayerStart?.Y ?? _scenario?.PlayerStart.Y)?.ToString() ?? "?", EditableFieldMode.ReadOnly)
            ],
            _focusRouter.StateFor("player-start"));
    }

    private SelectableListComponent EntityList()
    {
        var items = new List<SelectableListItem>
        {
            new(CreateEntityChoiceId, "Create New Template", "initialized template")
        };
        items.AddRange(_entities.Select(entity => new SelectableListItem(entity.TemplateId, $"{entity.Glyph} {entity.Name}", entity.TemplateId)));
        var list = new SelectableListComponent(
            "entity-list",
            "2.3 Defined entities",
            new SadConsoleRect(1, 22, 57, 33),
            items,
            _focusRouter.StateFor("entity-list"),
            visibleRowCount: 9);
        for (var index = 0; index < _selectedEntityIndex; index++) list.MoveSelection(1);
        return list;
    }

    private SelectableListComponent ActionPlanList()
    {
        var items = new List<SelectableListItem>
        {
            new(CreateActionPlanChoiceId, "Create New Action Plan", "empty action plan")
        };
        items.AddRange(_actionPlans.Select(plan => new SelectableListItem(plan.ActionPlanId, plan.ActionPlanId, $"{plan.ActionSteps.Count} steps | {plan.Shape}")));
        var list = new SelectableListComponent(
            "action-plan-list",
            "2.4 Defined action plans",
            new SadConsoleRect(61, 15, 56, 33),
            items,
            _focusRouter.StateFor("action-plan-list"),
            visibleRowCount: 16);
        for (var index = 0; index < _selectedActionPlanIndex; index++) list.MoveSelection(1);
        return list;
    }

    private FrontendEditorEntityTemplateSummary? SelectedEntity()
    {
        if (_entities.Count == 0) return null;
        var index = FocusedComponentId == "scenario-preview" ? _selectedPreviewIndex : _selectedEntityIndex - 1;
        return index < 0 || index >= _entities.Count ? null : _entities[index];
    }

    private FrontendEditorActionPlanSummary? SelectedActionPlan()
    {
        if (_actionPlans.Count == 0) return null;
        var index = _selectedActionPlanIndex - 1;
        return index < 0 || index >= _actionPlans.Count ? null : _actionPlans[index];
    }
}

internal enum ScenarioEditOverlayMode
{
    CreateEntityTemplateName,
    DuplicateEntityTemplateName,
    DeleteEntityTemplateConfirm,
    CreateActionPlanName,
    DuplicateActionPlanName,
    DeleteActionPlanConfirm
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
