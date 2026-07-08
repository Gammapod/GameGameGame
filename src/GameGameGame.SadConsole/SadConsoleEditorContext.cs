using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.SadConsoleApp;

internal sealed class SadConsoleEditorContext
{
    private readonly FrontendEditorService _service;
    private FrontendEditorSnapshot _snapshot;
    private FrontendEditorScenarioPreview? _cachedPreview;
    private string _previewInvalidationReason = "Preview has not been materialized in this editor context.";
    private SadConsoleEditorTemplateEditMode _templateEditMode = SadConsoleEditorTemplateEditMode.None;
    private string _templateEditBuffer = string.Empty;
    private bool _isPickingTemplateDefaultActionPlan;
    private int _templateDefaultActionPlanPickerIndex;
    private SadConsoleEditorActionStepEditState? _actionStepEdit;
    private SadConsoleEditorTargetingRuleEditState? _targetingRuleEdit;
    private SadConsoleEditorInventoryBrushState? _inventoryBrush;
    private bool _isCommandMenuOpen;
    private int _commandMenuSelectedIndex;

    private SadConsoleEditorContext(FrontendEditorService service, string contentPath, FrontendEditorSnapshot snapshot, int selectedScenarioIndex)
    {
        _service = service;
        _snapshot = snapshot;
        ContentPath = contentPath;
        SelectedScenarioIndex = selectedScenarioIndex;
    }

    public string ContentPath { get; }
    public SadConsoleEditorSection Section { get; private set; } = SadConsoleEditorSection.Scenarios;
    public int SelectedScenarioIndex { get; private set; }
    public int SelectedTemplateIndex { get; private set; }
    public int SelectedActionPlanIndex { get; private set; }
    public int SelectedDiagnosticIndex { get; private set; }
    public int SelectedPreviewEntityIndex { get; private set; }
    public int YamlScrollOffset { get; private set; }
    public SadConsoleEditorTextSurface TextSurface { get; private set; } = SadConsoleEditorTextSurface.YamlPreview;
    public FrontendEditorScenarioPreview? CachedPreview => _cachedPreview;
    public string PreviewInvalidationReason => _previewInvalidationReason;
    public SadConsoleEditorTemplateEditMode TemplateEditMode => _templateEditMode;
    public string TemplateEditBuffer => _templateEditBuffer;
    public bool IsEditingTemplatePresentation => _templateEditMode != SadConsoleEditorTemplateEditMode.None;
    public bool IsPickingTemplateDefaultActionPlan => _isPickingTemplateDefaultActionPlan;
    public int TemplateDefaultActionPlanPickerIndex => _templateDefaultActionPlanPickerIndex;
    public bool IsEditingActionPlanSteps => _actionStepEdit is not null;
    public SadConsoleEditorActionStepEditState? ActionStepEdit => _actionStepEdit;
    public bool IsEditingTemplateTargetingRule => _targetingRuleEdit is not null;
    public SadConsoleEditorTargetingRuleEditState? TargetingRuleEdit => _targetingRuleEdit;
    public bool IsEditingTargetingRuleLabel => _targetingRuleEdit?.IsEditingLabel == true;
    public bool IsTemplateInventoryBrushActive => _inventoryBrush is not null;
    public SadConsoleEditorInventoryBrushState? InventoryBrush => _inventoryBrush;
    public bool IsTemplateEditInputActive => IsEditingTemplatePresentation || IsPickingTemplateDefaultActionPlan || IsEditingActionPlanSteps || IsEditingTemplateTargetingRule || IsTemplateInventoryBrushActive;
    public bool IsCommandMenuOpen => _isCommandMenuOpen;
    public int CommandMenuSelectedIndex => _commandMenuSelectedIndex;

    public static SadConsoleEditorOpenResult Open(string contentPath, string? selectedScenarioId = null)
    {
        var result = FrontendEditorService.OpenFile(contentPath);
        if (!result.IsSuccess || result.Service is null)
        {
            return SadConsoleEditorOpenResult.Failure(result.ErrorMessage ?? $"Could not open content file {contentPath}.");
        }

        var context = new SadConsoleEditorContext(result.Service, contentPath, result.Service.GetSnapshot(), 0);
        if (!string.IsNullOrWhiteSpace(selectedScenarioId))
        {
            context.TrySelectScenario(selectedScenarioId);
        }

        return SadConsoleEditorOpenResult.Success(context);
    }

    public FrontendEditorSnapshot Snapshot() => _snapshot;

    public SadConsoleEditorMutationUiResult OpenCommandMenu()
    {
        if (IsTemplateEditInputActive)
        {
            return SadConsoleEditorMutationUiResult.Failure("Finish or cancel the active editor submode before opening the command menu.");
        }

        _isCommandMenuOpen = true;
        _commandMenuSelectedIndex = ClampIndex(_commandMenuSelectedIndex, CommandMenuEntries().Count);
        return SadConsoleEditorMutationUiResult.Success("Editor command menu opened. Up/Down chooses a command; Enter/Select activates; Esc cancels.");
    }

    public SadConsoleEditorMutationUiResult CancelCommandMenu()
    {
        if (!_isCommandMenuOpen)
        {
            return SadConsoleEditorMutationUiResult.Success("Editor command menu is not open.");
        }

        _isCommandMenuOpen = false;
        return SadConsoleEditorMutationUiResult.Success("Editor command menu cancelled; no command was invoked.");
    }

    public void MoveCommandMenuSelection(int delta)
    {
        if (!_isCommandMenuOpen)
        {
            return;
        }

        _commandMenuSelectedIndex = ClampIndex(_commandMenuSelectedIndex + delta, CommandMenuEntries().Count);
    }

    public SadConsoleEditorCommandMenuActivationResult ActivateSelectedCommand()
    {
        if (!_isCommandMenuOpen)
        {
            return SadConsoleEditorCommandMenuActivationResult.None("Editor command menu is not open.");
        }

        var entries = CommandMenuEntries();
        if (entries.Count == 0)
        {
            _isCommandMenuOpen = false;
            _commandMenuSelectedIndex = 0;
            return SadConsoleEditorCommandMenuActivationResult.None("No editor commands are available for the current context.");
        }

        var selected = entries[Math.Clamp(_commandMenuSelectedIndex, 0, entries.Count - 1)];
        _isCommandMenuOpen = false;
        _commandMenuSelectedIndex = 0;

        switch (selected.CommandId)
        {
            case SadConsoleEditorCommandId.Save:
                return SadConsoleEditorCommandMenuActivationResult.Mutation(selected, Save());
            case SadConsoleEditorCommandId.Refresh:
            {
                var result = RefreshSnapshot();
                return SadConsoleEditorCommandMenuActivationResult.Completed(selected, result.Message);
            }
            case SadConsoleEditorCommandId.LaunchSimulation:
                return SadConsoleEditorCommandMenuActivationResult.LaunchSimulation(selected, SelectedScenario() is null ? "No authored scenario is selected." : "Launching derived Simulation for selected authored scenario.");
            case SadConsoleEditorCommandId.RematerializePreview:
            {
                SelectSection(SadConsoleEditorSection.Preview);
                var preview = RefreshSelectedScenarioPreview();
                return SadConsoleEditorCommandMenuActivationResult.Completed(selected, preview is null
                    ? "No authored scenario is selected for preview."
                    : $"Refreshed turn-0 derived runtime preview for {preview.ScenarioId}. Preview is not authored source.");
            }
            case SadConsoleEditorCommandId.ToggleYamlDiff:
                ToggleTextSurface();
                SelectSection(SadConsoleEditorSection.YamlAndDiff);
                return SadConsoleEditorCommandMenuActivationResult.Completed(selected, "Toggled read-only YAML/diff inspection surface.");
            case SadConsoleEditorCommandId.EditTemplateName:
                return SadConsoleEditorCommandMenuActivationResult.Mutation(selected, BeginTemplateNameEdit());
            case SadConsoleEditorCommandId.EditTemplateGlyph:
                return SadConsoleEditorCommandMenuActivationResult.Mutation(selected, BeginTemplateGlyphEdit());
            case SadConsoleEditorCommandId.CycleTemplateColor:
                return SadConsoleEditorCommandMenuActivationResult.Mutation(selected, CycleSelectedTemplateColor());
            case SadConsoleEditorCommandId.SetTemplateDefaultActionPlan:
                return SadConsoleEditorCommandMenuActivationResult.Mutation(selected, BeginTemplateDefaultActionPlanPicker());
            case SadConsoleEditorCommandId.EditTemplateTargetingRules:
                return SadConsoleEditorCommandMenuActivationResult.Mutation(selected, BeginTemplateTargetingRuleEditor());
            case SadConsoleEditorCommandId.InventoryBrushMode:
                return SadConsoleEditorCommandMenuActivationResult.Mutation(selected, ToggleTemplateInventoryBrush());
            case SadConsoleEditorCommandId.OpenActionStepEditor:
                return SadConsoleEditorCommandMenuActivationResult.Mutation(selected, BeginActionPlanStepEditor());
            case SadConsoleEditorCommandId.JumpPreviewRowToSourceTemplate:
            {
                var result = JumpSelectedPreviewEntityToSourceTemplate();
                return SadConsoleEditorCommandMenuActivationResult.Completed(selected, result.Message, result.Succeeded);
            }
            default:
                return SadConsoleEditorCommandMenuActivationResult.None($"Editor command {selected.CommandId} is not implemented.");
        }
    }

    public IReadOnlyList<SadConsoleEditorCommandMenuEntry> CommandMenuEntries()
    {
        var entries = new List<SadConsoleEditorCommandMenuEntry>
        {
            new(SadConsoleEditorCommandId.Save, "Save authored content", "Writes dirty editor-service snapshot to the opened content file."),
            new(SadConsoleEditorCommandId.Refresh, "Refresh/revalidate snapshot", "Reloads editor-service snapshot and marks Preview stale."),
        };

        if (SelectedScenario() is not null)
        {
            entries.Add(new(SadConsoleEditorCommandId.LaunchSimulation, "Launch Simulation", "Materializes selected authored scenario into a runtime Simulation session."));
        }

        switch (Section)
        {
            case SadConsoleEditorSection.Scenarios:
                if (SelectedScenario() is not null)
                {
                    entries.Add(new(SadConsoleEditorCommandId.RematerializePreview, "Rematerialize Preview", "Refreshes the turn-0 derived runtime preview for the selected scenario."));
                }
                break;
            case SadConsoleEditorSection.Templates:
                entries.AddRange([
                    new SadConsoleEditorCommandMenuEntry(SadConsoleEditorCommandId.EditTemplateName, "Edit template name", "Enter explicit typing submode for selected authored template name."),
                    new SadConsoleEditorCommandMenuEntry(SadConsoleEditorCommandId.EditTemplateGlyph, "Edit template glyph", "Enter explicit typing submode for selected authored template glyph."),
                    new SadConsoleEditorCommandMenuEntry(SadConsoleEditorCommandId.CycleTemplateColor, "Cycle template color", "Applies next presentation color through editor services."),
                    new SadConsoleEditorCommandMenuEntry(SadConsoleEditorCommandId.SetTemplateDefaultActionPlan, "Set default action plan", "Opens picker over existing authored action plans plus none."),
                    new SadConsoleEditorCommandMenuEntry(SadConsoleEditorCommandId.EditTemplateTargetingRules, "Edit targeting rules", "Opens targeting-rule slot editor for the selected template."),
                    new SadConsoleEditorCommandMenuEntry(SadConsoleEditorCommandId.InventoryBrushMode, "Inventory brush mode", "Opens place-only authored inventory brush mode.")]);
                break;
            case SadConsoleEditorSection.ActionPlans:
                entries.Add(new(SadConsoleEditorCommandId.OpenActionStepEditor, "Open action-step editor", "Edits selected action-plan step kinds through editor services."));
                break;
            case SadConsoleEditorSection.YamlAndDiff:
                entries.Add(new(SadConsoleEditorCommandId.ToggleYamlDiff, "Toggle YAML/Diff", "Switches the read-only authored YAML inspection surface."));
                break;
            case SadConsoleEditorSection.Preview:
                entries.Add(new(SadConsoleEditorCommandId.RematerializePreview, "Rematerialize Preview", "Refreshes the turn-0 derived runtime preview for the selected scenario."));
                entries.Add(new(SadConsoleEditorCommandId.JumpPreviewRowToSourceTemplate, "Jump preview row to source template", "Uses runtime-to-template provenance when available."));
                break;
        }

        return entries;
    }

    public SadConsoleEditorMutationUiResult BeginTemplateNameEdit()
    {
        if (SelectedTemplate() is not { } template)
        {
            return SadConsoleEditorMutationUiResult.Failure("No authored template is selected for name edit.");
        }

        Section = SadConsoleEditorSection.Templates;
        ClearTemplateDefaultActionPlanPicker();
        ClearActionPlanStepEditor();
        ClearTemplateTargetingRuleEditor();
        ClearTemplateInventoryBrush();
        _templateEditMode = SadConsoleEditorTemplateEditMode.Name;
        _templateEditBuffer = template.Name;
        return SadConsoleEditorMutationUiResult.Success($"Editing template name for {template.TemplateId}. Enter applies; Esc cancels.");
    }

    public SadConsoleEditorMutationUiResult BeginTemplateGlyphEdit()
    {
        if (SelectedTemplate() is not { } template)
        {
            return SadConsoleEditorMutationUiResult.Failure("No authored template is selected for glyph edit.");
        }

        Section = SadConsoleEditorSection.Templates;
        ClearTemplateDefaultActionPlanPicker();
        ClearActionPlanStepEditor();
        ClearTemplateTargetingRuleEditor();
        ClearTemplateInventoryBrush();
        _templateEditMode = SadConsoleEditorTemplateEditMode.Glyph;
        _templateEditBuffer = template.Glyph.ToString();
        return SadConsoleEditorMutationUiResult.Success($"Editing template glyph for {template.TemplateId}. Type a symbol, Enter applies; Esc cancels.");
    }

    public SadConsoleEditorMutationUiResult TypeEditText(string text)
    {
        if (!IsEditingTemplatePresentation)
        {
            return SadConsoleEditorMutationUiResult.Failure("No template presentation edit is active.");
        }

        if (string.IsNullOrEmpty(text))
        {
            return SadConsoleEditorMutationUiResult.Success(EditStatusMessage());
        }

        var printable = new string(text.Where(ch => !char.IsControl(ch)).ToArray());
        if (printable.Length == 0)
        {
            return SadConsoleEditorMutationUiResult.Success(EditStatusMessage());
        }

        if (_templateEditMode == SadConsoleEditorTemplateEditMode.Glyph)
        {
            _templateEditBuffer = printable[0].ToString();
        }
        else
        {
            _templateEditBuffer += printable;
        }

        return SadConsoleEditorMutationUiResult.Success(EditStatusMessage());
    }

    public SadConsoleEditorMutationUiResult BackspaceEditText()
    {
        if (!IsEditingTemplatePresentation)
        {
            return SadConsoleEditorMutationUiResult.Failure("No template presentation edit is active.");
        }

        if (_templateEditBuffer.Length > 0)
        {
            _templateEditBuffer = _templateEditBuffer[..^1];
        }

        return SadConsoleEditorMutationUiResult.Success(EditStatusMessage());
    }

    public SadConsoleEditorMutationUiResult ConfirmEdit()
    {
        if (_templateEditMode == SadConsoleEditorTemplateEditMode.None)
        {
            return SadConsoleEditorMutationUiResult.Failure("No template presentation edit is active.");
        }

        if (SelectedTemplate() is not { } template)
        {
            CancelEdit();
            return SadConsoleEditorMutationUiResult.Failure("No authored template is selected; edit cancelled.");
        }

        var update = _templateEditMode == SadConsoleEditorTemplateEditMode.Name
            ? new FrontendEditorTemplatePresentationUpdate(_templateEditBuffer, template.Glyph.ToString(), template.Color)
            : new FrontendEditorTemplatePresentationUpdate(template.Name, FirstGlyphText(_templateEditBuffer), template.Color);

        ClearTemplateEdit();
        return ApplyTemplatePresentationUpdate(template.TemplateId, update);
    }

    public SadConsoleEditorMutationUiResult CancelEdit()
    {
        if (_isPickingTemplateDefaultActionPlan)
        {
            ClearTemplateDefaultActionPlanPicker();
            return SadConsoleEditorMutationUiResult.Success("Default action plan picker cancelled; authored content was not mutated.");
        }

        if (_targetingRuleEdit is { IsEditingLabel: true } edit)
        {
            _targetingRuleEdit = edit with { IsEditingLabel = false, LabelBuffer = edit.Label };
            return SadConsoleEditorMutationUiResult.Success("Targeting rule label edit cancelled; pending rule unchanged.");
        }

        if (_targetingRuleEdit is not null)
        {
            ClearTemplateTargetingRuleEditor();
            return SadConsoleEditorMutationUiResult.Success("Targeting rule editor closed; unapplied pending changes were discarded.");
        }

        if (_inventoryBrush is not null)
        {
            ClearTemplateInventoryBrush();
            return SadConsoleEditorMutationUiResult.Success("Template inventory brush mode exited; authored content was not mutated by exit.");
        }

        if (_actionStepEdit is not null)
        {
            ClearActionPlanStepEditor();
            return SadConsoleEditorMutationUiResult.Success("Action-plan step editor closed; authored content was not mutated by exit.");
        }

        if (_templateEditMode == SadConsoleEditorTemplateEditMode.None)
        {
            return SadConsoleEditorMutationUiResult.Success("No template presentation edit is active.");
        }

        ClearTemplateEdit();
        return SadConsoleEditorMutationUiResult.Success("Template presentation edit cancelled; authored content was not mutated.");
    }

    public SadConsoleEditorMutationUiResult CycleSelectedTemplateColor()
    {
        if (SelectedTemplate() is not { } template)
        {
            return SadConsoleEditorMutationUiResult.Failure("No authored template is selected for color edit.");
        }

        ClearTemplateEdit();
        ClearTemplateDefaultActionPlanPicker();
        ClearActionPlanStepEditor();
        ClearTemplateTargetingRuleEditor();
        ClearTemplateInventoryBrush();
        var values = Enum.GetValues<PresentationColor>();
        var index = Array.IndexOf(values, template.Color);
        var next = values[(index + 1 + values.Length) % values.Length];
        return ApplyTemplatePresentationUpdate(
            template.TemplateId,
            new FrontendEditorTemplatePresentationUpdate(template.Name, template.Glyph.ToString(), next));
    }

    public SadConsoleEditorMutationUiResult Save()
    {
        ClearTemplateEdit();
        ClearTemplateDefaultActionPlanPicker();
        ClearActionPlanStepEditor();
        ClearTemplateTargetingRuleEditor();
        ClearTemplateInventoryBrush();
        var selectedTemplateId = SelectedTemplate()?.TemplateId;
        var result = _service.Save();
        ReplaceSnapshotAfterMutation(result.Snapshot, selectedTemplateId, markPreviewStale: false);
        return new SadConsoleEditorMutationUiResult(result.IsSuccess, result.StatusMessage);
    }

    public SadConsoleEditorRefreshResult RefreshSnapshot()
    {
        ClearTemplateEdit();
        ClearTemplateDefaultActionPlanPicker();
        ClearActionPlanStepEditor();
        ClearTemplateTargetingRuleEditor();
        ClearTemplateInventoryBrush();
        var section = Section;
        var scenarioId = SelectedScenario()?.ScenarioId;
        var templateId = SelectedTemplateIndex >= 0 && SelectedTemplateIndex < _snapshot.EntityTemplates.Count
            ? _snapshot.EntityTemplates[SelectedTemplateIndex].TemplateId
            : null;
        var actionPlanId = SelectedActionPlanIndex >= 0 && SelectedActionPlanIndex < _snapshot.ActionPlans.Count
            ? _snapshot.ActionPlans[SelectedActionPlanIndex].ActionPlanId
            : null;
        var diagnosticIndex = SelectedDiagnosticIndex;

        _snapshot = _service.GetSnapshot();
        Section = section;

        var messages = new List<string>();
        var scenarioPreserved = TryRestoreScenarioSelection(scenarioId);
        if (!scenarioPreserved && scenarioId is not null)
        {
            messages.Add($"selected scenario '{scenarioId}' no longer exists; selection clamped");
        }

        var templatePreserved = TryRestoreTemplateSelection(templateId);
        if (!templatePreserved && templateId is not null)
        {
            messages.Add($"selected template '{templateId}' no longer exists; selection clamped");
        }

        var actionPlanPreserved = TryRestoreActionPlanSelection(actionPlanId);
        if (!actionPlanPreserved && actionPlanId is not null)
        {
            messages.Add($"selected action plan '{actionPlanId}' no longer exists; selection clamped");
        }

        SelectedDiagnosticIndex = ClampIndex(diagnosticIndex, _snapshot.ValidationDiagnostics.Count);
        var previewWasCleared = _cachedPreview is not null;
        _cachedPreview = null;
        SelectedPreviewEntityIndex = 0;
        _previewInvalidationReason = previewWasCleared
            ? "Preview marked stale and cleared because the authored snapshot was refreshed. Press P to rematerialize."
            : "Preview is not materialized for this cached authored snapshot. Press P to materialize.";

        ClampAllSelections();

        var message = messages.Count == 0
            ? "Refreshed/revalidated cached authored snapshot; selections preserved where possible. Preview is stale until P rematerializes."
            : $"Refreshed/revalidated cached authored snapshot; {string.Join("; ", messages)}. Preview is stale until P rematerializes.";
        return new SadConsoleEditorRefreshResult(message, previewWasCleared, scenarioPreserved, templatePreserved, actionPlanPreserved);
    }

    public FrontendEditorScenarioSummary? SelectedScenario()
    {
        var scenarios = _snapshot.Scenarios;
        return scenarios.Count == 0 ? null : scenarios[Math.Clamp(SelectedScenarioIndex, 0, scenarios.Count - 1)];
    }

    private FrontendEditorEntityTemplateSummary? SelectedTemplate()
    {
        var templates = _snapshot.EntityTemplates;
        return templates.Count == 0 ? null : templates[Math.Clamp(SelectedTemplateIndex, 0, templates.Count - 1)];
    }

    public void MoveSelection(int delta)
    {
        if (_isPickingTemplateDefaultActionPlan)
        {
            MoveTemplateDefaultActionPlanPicker(delta);
            return;
        }

        if (_targetingRuleEdit is not null)
        {
            MoveTemplateTargetingRuleSlot(delta);
            return;
        }

        if (_actionStepEdit is not null)
        {
            MoveActionPlanStepEditorPosition(delta);
            return;
        }

        if (_inventoryBrush is not null)
        {
            MoveTemplateInventoryBrushCursor(0, delta);
            return;
        }

        switch (Section)
        {
            case SadConsoleEditorSection.Scenarios:
                MoveScenarioSelection(delta);
                break;
            case SadConsoleEditorSection.Templates:
                SelectedTemplateIndex = ClampIndex(SelectedTemplateIndex + delta, _snapshot.EntityTemplates.Count);
                break;
            case SadConsoleEditorSection.ActionPlans:
                SelectedActionPlanIndex = ClampIndex(SelectedActionPlanIndex + delta, _snapshot.ActionPlans.Count);
                break;
            case SadConsoleEditorSection.Diagnostics:
                SelectedDiagnosticIndex = ClampIndex(SelectedDiagnosticIndex + delta, _snapshot.ValidationDiagnostics.Count);
                break;
            case SadConsoleEditorSection.YamlAndDiff:
                YamlScrollOffset = Math.Max(0, YamlScrollOffset + delta);
                break;
            case SadConsoleEditorSection.Preview:
                if (HasCurrentScenarioPreview())
                {
                    MovePreviewEntitySelection(delta);
                }
                else
                {
                    MoveScenarioSelection(delta);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void MoveSection(int delta)
    {
        ClearTemplateEdit();
        ClearTemplateDefaultActionPlanPicker();
        ClearActionPlanStepEditor();
        ClearTemplateTargetingRuleEditor();
        ClearTemplateInventoryBrush();
        var values = Enum.GetValues<SadConsoleEditorSection>();
        var index = Array.IndexOf(values, Section);
        Section = values[Math.Clamp(index + delta, 0, values.Length - 1)];
        ClampAllSelections();
    }

    public void SelectSection(SadConsoleEditorSection section)
    {
        if (Section != section)
        {
            ClearTemplateEdit();
            ClearTemplateDefaultActionPlanPicker();
            ClearActionPlanStepEditor();
            ClearTemplateTargetingRuleEditor();
            ClearTemplateInventoryBrush();
        }

        Section = section;
        ClampAllSelections();
    }

    public void ToggleTextSurface()
    {
        TextSurface = TextSurface == SadConsoleEditorTextSurface.YamlPreview
            ? SadConsoleEditorTextSurface.Diff
            : SadConsoleEditorTextSurface.YamlPreview;
        YamlScrollOffset = 0;
    }

    public bool TrySelectScenario(string scenarioId)
    {
        ClearTemplateEdit();
        ClearTemplateDefaultActionPlanPicker();
        ClearActionPlanStepEditor();
        ClearTemplateTargetingRuleEditor();
        ClearTemplateInventoryBrush();
        var scenarios = _snapshot.Scenarios;
        var index = scenarios.ToList().FindIndex(scenario => string.Equals(scenario.ScenarioId, scenarioId, StringComparison.Ordinal));
        if (index < 0)
        {
            return false;
        }

        SelectedScenarioIndex = index;
        ClearPreviewIfScenarioChanged();
        return true;
    }

    public FrontendEditorScenarioPreview? RefreshSelectedScenarioPreview()
    {
        var selected = SelectedScenario();
        if (selected is null)
        {
            _cachedPreview = null;
            return null;
        }

        _cachedPreview = _service.PreviewScenario(selected.ScenarioId);
        _previewInvalidationReason = "Preview is current for the selected cached authored scenario.";
        SelectedPreviewEntityIndex = ClampIndex(SelectedPreviewEntityIndex, BuildEntityLocationTreeNodes(_cachedPreview.Session).Count);
        return _cachedPreview;
    }

    public FrontendEditorScenarioPreview? GetOrRefreshSelectedScenarioPreview()
    {
        var selected = SelectedScenario();
        if (selected is null)
        {
            _cachedPreview = null;
            return null;
        }

        return _cachedPreview?.ScenarioId == selected.ScenarioId
            ? _cachedPreview
            : RefreshSelectedScenarioPreview();
    }

    public FrontendEditorScenarioPreview? MaterializeSelectedScenarioForSimulation()
    {
        var selected = SelectedScenario();
        return selected is null ? null : _service.PreviewScenario(selected.ScenarioId);
    }

    private void MoveScenarioSelection(int delta)
    {
        var before = SelectedScenario()?.ScenarioId;
        SelectedScenarioIndex = ClampIndex(SelectedScenarioIndex + delta, _snapshot.Scenarios.Count);
        var after = SelectedScenario()?.ScenarioId;
        if (!string.Equals(before, after, StringComparison.Ordinal))
        {
            _cachedPreview = null;
            SelectedPreviewEntityIndex = 0;
            _previewInvalidationReason = "Preview cleared because the selected authored scenario changed. Press P to rematerialize.";
        }
    }

    private void MovePreviewEntitySelection(int delta)
    {
        if (_cachedPreview is null)
        {
            SelectedPreviewEntityIndex = 0;
            return;
        }

        SelectedPreviewEntityIndex = ClampIndex(SelectedPreviewEntityIndex + delta, BuildEntityLocationTreeNodes(_cachedPreview.Session).Count);
    }

    private void ClearPreviewIfScenarioChanged()
    {
        if (_cachedPreview is not null && !string.Equals(_cachedPreview.ScenarioId, SelectedScenario()?.ScenarioId, StringComparison.Ordinal))
        {
            _cachedPreview = null;
            SelectedPreviewEntityIndex = 0;
            _previewInvalidationReason = "Preview cleared because the selected authored scenario changed. Press P to rematerialize.";
        }
    }

    private bool TryRestoreScenarioSelection(string? scenarioId)
    {
        if (scenarioId is not null)
        {
            var index = _snapshot.Scenarios.ToList().FindIndex(scenario => string.Equals(scenario.ScenarioId, scenarioId, StringComparison.Ordinal));
            if (index >= 0)
            {
                SelectedScenarioIndex = index;
                return true;
            }
        }

        SelectedScenarioIndex = ClampIndex(SelectedScenarioIndex, _snapshot.Scenarios.Count);
        return scenarioId is null || _snapshot.Scenarios.Count == 0;
    }

    private bool TryRestoreTemplateSelection(string? templateId)
    {
        if (templateId is not null)
        {
            var index = _snapshot.EntityTemplates.ToList().FindIndex(template => string.Equals(template.TemplateId, templateId, StringComparison.Ordinal));
            if (index >= 0)
            {
                SelectedTemplateIndex = index;
                return true;
            }
        }

        SelectedTemplateIndex = ClampIndex(SelectedTemplateIndex, _snapshot.EntityTemplates.Count);
        return templateId is null || _snapshot.EntityTemplates.Count == 0;
    }

    private bool TryRestoreActionPlanSelection(string? actionPlanId)
    {
        if (actionPlanId is not null)
        {
            var index = _snapshot.ActionPlans.ToList().FindIndex(plan => string.Equals(plan.ActionPlanId, actionPlanId, StringComparison.Ordinal));
            if (index >= 0)
            {
                SelectedActionPlanIndex = index;
                return true;
            }
        }

        SelectedActionPlanIndex = ClampIndex(SelectedActionPlanIndex, _snapshot.ActionPlans.Count);
        return actionPlanId is null || _snapshot.ActionPlans.Count == 0;
    }

    private void ClampAllSelections()
    {
        SelectedScenarioIndex = ClampIndex(SelectedScenarioIndex, _snapshot.Scenarios.Count);
        SelectedTemplateIndex = ClampIndex(SelectedTemplateIndex, _snapshot.EntityTemplates.Count);
        SelectedActionPlanIndex = ClampIndex(SelectedActionPlanIndex, _snapshot.ActionPlans.Count);
        SelectedDiagnosticIndex = ClampIndex(SelectedDiagnosticIndex, _snapshot.ValidationDiagnostics.Count);
        SelectedPreviewEntityIndex = _cachedPreview is null
            ? 0
            : ClampIndex(SelectedPreviewEntityIndex, BuildEntityLocationTreeNodes(_cachedPreview.Session).Count);
        YamlScrollOffset = Math.Max(0, YamlScrollOffset);
        ClampActionStepEditorState();
    }

    private static int ClampIndex(int index, int count) => count == 0 ? 0 : Math.Clamp(index, 0, count - 1);

    public FrontendEditorScenarioPreview? PreviewSelectedScenario() => RefreshSelectedScenarioPreview();

    public SadConsoleEditorSourceJumpResult JumpSelectedPreviewEntityToSourceTemplate()
    {
        ClearTemplateEdit();
        ClearTemplateDefaultActionPlanPicker();
        ClearActionPlanStepEditor();
        ClearTemplateTargetingRuleEditor();
        ClearTemplateInventoryBrush();

        if (!HasCurrentScenarioPreview() || _cachedPreview is null)
        {
            return SadConsoleEditorSourceJumpResult.Failure("Source unknown for selected runtime entity");
        }

        var nodes = BuildEntityLocationTreeNodes(_cachedPreview.Session);
        if (nodes.Count == 0)
        {
            return SadConsoleEditorSourceJumpResult.Failure("Source unknown for selected runtime entity");
        }

        var entityId = nodes[Math.Clamp(SelectedPreviewEntityIndex, 0, nodes.Count - 1)].EntityId;
        if (!_cachedPreview.Session.Registry.TryGetTemplateIdForEntity(entityId, out var templateId))
        {
            return SadConsoleEditorSourceJumpResult.Failure("Source unknown for selected runtime entity");
        }

        var templateIndex = _snapshot.EntityTemplates.ToList().FindIndex(template => string.Equals(template.TemplateId, templateId.Value, StringComparison.Ordinal));
        if (templateIndex < 0)
        {
            return SadConsoleEditorSourceJumpResult.Failure("Source unknown for selected runtime entity");
        }

        SelectedTemplateIndex = templateIndex;
        Section = SadConsoleEditorSection.Templates;
        return SadConsoleEditorSourceJumpResult.Success($"Jumped to source template {templateId.Value}");
    }

    public SadConsoleEditorMutationUiResult BeginTemplateDefaultActionPlanPicker()
    {
        if (SelectedTemplate() is not { } template)
        {
            return SadConsoleEditorMutationUiResult.Failure("No authored template is selected for default action plan edit.");
        }

        ClearTemplateEdit();
        ClearActionPlanStepEditor();
        ClearTemplateTargetingRuleEditor();
        ClearTemplateInventoryBrush();
        Section = SadConsoleEditorSection.Templates;
        var options = TemplateDefaultActionPlanPickerOptions();
        var currentIndex = options.ToList().FindIndex(option => string.Equals(option.ActionPlanId, template.DefaultActionPlanId, StringComparison.Ordinal));
        _templateDefaultActionPlanPickerIndex = currentIndex >= 0 ? currentIndex : 0;
        _isPickingTemplateDefaultActionPlan = true;
        return SadConsoleEditorMutationUiResult.Success($"Choosing default action plan for {template.TemplateId}. Up/Down selects; Enter applies; Esc cancels.");
    }

    public IReadOnlyList<SadConsoleEditorActionPlanPickerOption> TemplateDefaultActionPlanPickerOptions() =>
        [new SadConsoleEditorActionPlanPickerOption(null, "none"), .. _snapshot.ActionPlans.Select(plan => new SadConsoleEditorActionPlanPickerOption(plan.ActionPlanId, plan.ActionPlanId))];

    public SadConsoleEditorMutationUiResult ConfirmTemplateDefaultActionPlanPicker()
    {
        if (!_isPickingTemplateDefaultActionPlan)
        {
            return SadConsoleEditorMutationUiResult.Failure("No default action plan picker is active.");
        }

        if (SelectedTemplate() is not { } template)
        {
            ClearTemplateDefaultActionPlanPicker();
            return SadConsoleEditorMutationUiResult.Failure("No authored template is selected; default action plan edit cancelled.");
        }

        var options = TemplateDefaultActionPlanPickerOptions();
        var option = options[Math.Clamp(_templateDefaultActionPlanPickerIndex, 0, options.Count - 1)];
        ClearTemplateDefaultActionPlanPicker();
        var result = option.ActionPlanId is null
            ? _service.ClearTemplateDefaultActionPlan(template.TemplateId)
            : _service.SetTemplateDefaultActionPlan(template.TemplateId, option.ActionPlanId);
        ReplaceSnapshotAfterMutation(
            result.Snapshot,
            template.TemplateId,
            markPreviewStale: result.IsSuccess,
            previewStaleReason: "Preview marked stale because authored template default action plan changed. Press P to rematerialize.");
        return new SadConsoleEditorMutationUiResult(result.IsSuccess, result.StatusMessage);
    }

    private void MoveTemplateDefaultActionPlanPicker(int delta)
    {
        _templateDefaultActionPlanPickerIndex = ClampIndex(_templateDefaultActionPlanPickerIndex + delta, TemplateDefaultActionPlanPickerOptions().Count);
    }

    public SadConsoleEditorMutationUiResult BeginActionPlanStepEditor()
    {
        if (SelectedActionPlan() is not { } plan)
        {
            return SadConsoleEditorMutationUiResult.Failure("No authored action plan is selected for step edit.");
        }

        if (_snapshot.AvailableActionSteps.Count == 0)
        {
            return SadConsoleEditorMutationUiResult.Failure("No engine-defined action steps are available for action-plan editing.");
        }

        ClearTemplateEdit();
        ClearTemplateDefaultActionPlanPicker();
        ClearTemplateTargetingRuleEditor();
        ClearTemplateInventoryBrush();
        Section = SadConsoleEditorSection.ActionPlans;
        _actionStepEdit = new SadConsoleEditorActionStepEditState(StepOrInsertIndex: 0, AvailableActionStepIndex: 0);
        ClampActionStepEditorState();
        return SadConsoleEditorMutationUiResult.Success($"Editing steps for action plan {plan.ActionPlanId}. Up/Down selects step or append slot; Tab/Left/Right cycles engine-defined step; R replaces; I inserts; Esc exits.");
    }

    public IReadOnlyList<FrontendEditorAvailableActionStepSummary> AvailableActionStepOptions() => _snapshot.AvailableActionSteps;

    public SadConsoleEditorMutationUiResult CycleActionStepEditorAvailable(int delta = 1)
    {
        if (_actionStepEdit is not { } edit)
        {
            return SadConsoleEditorMutationUiResult.Failure("No action-plan step editor is active.");
        }

        var options = AvailableActionStepOptions();
        if (options.Count == 0)
        {
            return SadConsoleEditorMutationUiResult.Failure("No engine-defined action steps are available for action-plan editing.");
        }

        var next = (edit.AvailableActionStepIndex + delta) % options.Count;
        if (next < 0)
        {
            next += options.Count;
        }

        _actionStepEdit = edit with { AvailableActionStepIndex = next };
        var option = options[next];
        return SadConsoleEditorMutationUiResult.Success($"Selected action step kind {option.DisplayName} ({option.Kind}) for replace/insert.");
    }

    public SadConsoleEditorMutationUiResult ReplaceSelectedActionPlanStep()
    {
        if (_actionStepEdit is not { } edit)
        {
            return SadConsoleEditorMutationUiResult.Failure("No action-plan step editor is active.");
        }

        if (SelectedActionPlan() is not { } plan)
        {
            ClearActionPlanStepEditor();
            return SadConsoleEditorMutationUiResult.Failure("No authored action plan is selected; action-plan step edit cancelled.");
        }

        if (edit.StepOrInsertIndex < 0 || edit.StepOrInsertIndex >= plan.ActionSteps.Count)
        {
            return SadConsoleEditorMutationUiResult.Failure($"Cannot replace at insertion position {edit.StepOrInsertIndex}; select an existing step row. Insert is still available.");
        }

        if (SelectedAvailableActionStep() is not { } option)
        {
            return SadConsoleEditorMutationUiResult.Failure("No engine-defined action step is selected for replace.");
        }

        var result = _service.ReplaceActionPlanStep(plan.ActionPlanId, edit.StepOrInsertIndex, option.Kind);
        ReplaceSnapshotAfterActionPlanMutation(
            result.Snapshot,
            plan.ActionPlanId,
            edit.StepOrInsertIndex,
            markPreviewStale: result.IsSuccess,
            previewStaleReason: "Preview marked stale because authored action plan steps changed. Press P to rematerialize.");
        return new SadConsoleEditorMutationUiResult(result.IsSuccess, result.StatusMessage);
    }

    public SadConsoleEditorMutationUiResult InsertSelectedActionPlanStep()
    {
        if (_actionStepEdit is not { } edit)
        {
            return SadConsoleEditorMutationUiResult.Failure("No action-plan step editor is active.");
        }

        if (SelectedActionPlan() is not { } plan)
        {
            ClearActionPlanStepEditor();
            return SadConsoleEditorMutationUiResult.Failure("No authored action plan is selected; action-plan step edit cancelled.");
        }

        if (SelectedAvailableActionStep() is not { } option)
        {
            return SadConsoleEditorMutationUiResult.Failure("No engine-defined action step is selected for insert.");
        }

        var insertIndex = Math.Clamp(edit.StepOrInsertIndex, 0, plan.ActionSteps.Count);
        var result = _service.InsertActionPlanStep(plan.ActionPlanId, insertIndex, option.Kind);
        ReplaceSnapshotAfterActionPlanMutation(
            result.Snapshot,
            plan.ActionPlanId,
            insertIndex,
            markPreviewStale: result.IsSuccess,
            previewStaleReason: "Preview marked stale because authored action plan steps changed. Press P to rematerialize.");
        return new SadConsoleEditorMutationUiResult(result.IsSuccess, result.StatusMessage);
    }

    private FrontendEditorAvailableActionStepSummary? SelectedAvailableActionStep()
    {
        if (_actionStepEdit is not { } edit || _snapshot.AvailableActionSteps.Count == 0)
        {
            return null;
        }

        return _snapshot.AvailableActionSteps[Math.Clamp(edit.AvailableActionStepIndex, 0, _snapshot.AvailableActionSteps.Count - 1)];
    }

    private FrontendEditorActionPlanSummary? SelectedActionPlan()
    {
        var plans = _snapshot.ActionPlans;
        return plans.Count == 0 ? null : plans[Math.Clamp(SelectedActionPlanIndex, 0, plans.Count - 1)];
    }

    private void MoveActionPlanStepEditorPosition(int delta)
    {
        if (_actionStepEdit is not { } edit)
        {
            return;
        }

        var stepCount = SelectedActionPlan()?.ActionSteps.Count ?? 0;
        _actionStepEdit = edit with { StepOrInsertIndex = Math.Clamp(edit.StepOrInsertIndex + delta, 0, stepCount) };
    }

    public SadConsoleEditorMutationUiResult BeginTemplateTargetingRuleEditor()
    {
        if (SelectedTemplate() is not { } template)
        {
            return SadConsoleEditorMutationUiResult.Failure("No authored template is selected for targeting rule edit.");
        }

        ClearTemplateEdit();
        ClearTemplateDefaultActionPlanPicker();
        ClearTemplateInventoryBrush();
        Section = SadConsoleEditorSection.Templates;
        var slot = template.TargetingRules.OrderBy(rule => rule.Slot).FirstOrDefault()?.Slot ?? 1;
        LoadTargetingRuleSlot(slot);
        return SadConsoleEditorMutationUiResult.Success($"Editing targeting rules for {template.TemplateId}. Up/Down selects slot; L edits label; E cycles target; +/- range; Enter applies; X/Delete clears; Esc exits.");
    }

    public IReadOnlyList<SadConsoleEditorTargetTemplatePickerOption> TargetTemplatePickerOptions() =>
        _snapshot.EntityTemplates.Select(template => new SadConsoleEditorTargetTemplatePickerOption(template.TemplateId, $"{template.Name} ({template.TemplateId})")).ToList();

    public SadConsoleEditorMutationUiResult BeginTargetingRuleLabelEdit()
    {
        if (_targetingRuleEdit is not { } edit)
        {
            return SadConsoleEditorMutationUiResult.Failure("No targeting rule editor is active.");
        }

        _targetingRuleEdit = edit with { IsEditingLabel = true, LabelBuffer = edit.Label };
        return SadConsoleEditorMutationUiResult.Success($"Editing targeting rule slot {edit.Slot} label. Lowercase letters/digits only; Enter accepts pending label; Esc cancels label edit.");
    }

    public SadConsoleEditorMutationUiResult TypeTargetingRuleLabelText(string text)
    {
        if (_targetingRuleEdit is not { IsEditingLabel: true } edit)
        {
            return SadConsoleEditorMutationUiResult.Failure("No targeting rule label edit is active.");
        }

        var printable = new string((text ?? string.Empty).Where(ch => !char.IsControl(ch)).ToArray());
        _targetingRuleEdit = edit with { LabelBuffer = edit.LabelBuffer + printable };
        return SadConsoleEditorMutationUiResult.Success(TargetingRuleStatusMessage());
    }

    public SadConsoleEditorMutationUiResult BackspaceTargetingRuleLabelText()
    {
        if (_targetingRuleEdit is not { IsEditingLabel: true } edit)
        {
            return SadConsoleEditorMutationUiResult.Failure("No targeting rule label edit is active.");
        }

        _targetingRuleEdit = edit with { LabelBuffer = edit.LabelBuffer.Length == 0 ? string.Empty : edit.LabelBuffer[..^1] };
        return SadConsoleEditorMutationUiResult.Success(TargetingRuleStatusMessage());
    }

    public SadConsoleEditorMutationUiResult ConfirmTargetingRuleLabelEdit()
    {
        if (_targetingRuleEdit is not { IsEditingLabel: true } edit)
        {
            return SadConsoleEditorMutationUiResult.Failure("No targeting rule label edit is active.");
        }

        _targetingRuleEdit = edit with { Label = edit.LabelBuffer, IsEditingLabel = false };
        return SadConsoleEditorMutationUiResult.Success(TargetingRuleStatusMessage());
    }

    public SadConsoleEditorMutationUiResult CycleTargetingRuleTarget(int delta = 1)
    {
        if (_targetingRuleEdit is not { } edit)
        {
            return SadConsoleEditorMutationUiResult.Failure("No targeting rule editor is active.");
        }

        var options = TargetTemplatePickerOptions();
        if (options.Count == 0)
        {
            return SadConsoleEditorMutationUiResult.Failure("No target templates are available.");
        }

        var nextIndex = ClampIndex(edit.TargetTemplateIndex + delta, options.Count);
        _targetingRuleEdit = edit with { TargetTemplateIndex = nextIndex, TargetTemplateId = options[nextIndex].TemplateId };
        return SadConsoleEditorMutationUiResult.Success(TargetingRuleStatusMessage());
    }

    public SadConsoleEditorMutationUiResult AdjustTargetingRuleRange(int delta)
    {
        if (_targetingRuleEdit is not { } edit)
        {
            return SadConsoleEditorMutationUiResult.Failure("No targeting rule editor is active.");
        }

        _targetingRuleEdit = edit with { Range = Math.Clamp(edit.Range + delta, 0, 10) };
        return SadConsoleEditorMutationUiResult.Success(TargetingRuleStatusMessage());
    }

    public SadConsoleEditorMutationUiResult ConfirmTemplateTargetingRuleEditor()
    {
        if (_targetingRuleEdit is not { } edit)
        {
            return SadConsoleEditorMutationUiResult.Failure("No targeting rule editor is active.");
        }

        if (SelectedTemplate() is not { } template)
        {
            ClearTemplateTargetingRuleEditor();
            return SadConsoleEditorMutationUiResult.Failure("No authored template is selected; targeting rule edit cancelled.");
        }

        var result = _service.SetTemplateTargetingRule(template.TemplateId, new FrontendEditorTargetingRuleUpdate(edit.Slot, edit.Label, edit.TargetTemplateId, edit.Range));
        ReplaceSnapshotAfterMutation(
            result.Snapshot,
            template.TemplateId,
            markPreviewStale: result.IsSuccess,
            previewStaleReason: "Preview marked stale because authored template targeting rules changed. Press P to rematerialize.");
        if (result.IsSuccess)
        {
            LoadTargetingRuleSlot(edit.Slot);
        }

        return new SadConsoleEditorMutationUiResult(result.IsSuccess, result.StatusMessage);
    }

    public SadConsoleEditorMutationUiResult ClearTemplateTargetingRuleSlot()
    {
        if (_targetingRuleEdit is not { } edit)
        {
            return SadConsoleEditorMutationUiResult.Failure("No targeting rule editor is active.");
        }

        if (SelectedTemplate() is not { } template)
        {
            ClearTemplateTargetingRuleEditor();
            return SadConsoleEditorMutationUiResult.Failure("No authored template is selected; targeting rule edit cancelled.");
        }

        if (!edit.ExistingRulePresent)
        {
            return SadConsoleEditorMutationUiResult.Success($"Targeting rule slot {edit.Slot} is already empty; authored content was not mutated.");
        }

        var result = _service.ClearTemplateTargetingRule(template.TemplateId, edit.Slot);
        ReplaceSnapshotAfterMutation(
            result.Snapshot,
            template.TemplateId,
            markPreviewStale: result.IsSuccess,
            previewStaleReason: "Preview marked stale because authored template targeting rules changed. Press P to rematerialize.");
        if (result.IsSuccess)
        {
            LoadTargetingRuleSlot(edit.Slot);
        }

        return new SadConsoleEditorMutationUiResult(result.IsSuccess, result.StatusMessage);
    }

    private void MoveTemplateTargetingRuleSlot(int delta)
    {
        if (_targetingRuleEdit is not { } edit || edit.IsEditingLabel)
        {
            return;
        }

        LoadTargetingRuleSlot(Math.Clamp(edit.Slot + delta, 1, 4));
    }

    public SadConsoleEditorMutationUiResult ToggleTemplateInventoryBrush()
    {
        if (_inventoryBrush is not null)
        {
            ClearTemplateInventoryBrush();
            return SadConsoleEditorMutationUiResult.Success("Template inventory brush mode exited.");
        }

        return BeginTemplateInventoryBrush();
    }

    public SadConsoleEditorMutationUiResult BeginTemplateInventoryBrush()
    {
        if (SelectedTemplate() is not { } template)
        {
            return SadConsoleEditorMutationUiResult.Failure("No authored template is selected for inventory brush placement.");
        }

        ClearTemplateEdit();
        ClearTemplateDefaultActionPlanPicker();
        ClearTemplateTargetingRuleEditor();
        Section = SadConsoleEditorSection.Templates;
        _inventoryBrush = new SadConsoleEditorInventoryBrushState(new GridCoord(0, 0), 0);
        ClampTemplateInventoryBrushState();

        if (!HasUsableInventory(template))
        {
            return SadConsoleEditorMutationUiResult.Success($"Template inventory brush mode active for {template.TemplateId}, but this template has no usable inventory grid; placement is disabled.");
        }

        if (TemplateInventoryBrushOptions().Count == 0)
        {
            return SadConsoleEditorMutationUiResult.Success($"Template inventory brush mode active for {template.TemplateId}, but no other entity templates are available as brushes.");
        }

        return SadConsoleEditorMutationUiResult.Success($"Template inventory brush mode active for {template.TemplateId}. Tab/E cycles brush, arrows move cursor, Enter places, Esc exits.");
    }

    public IReadOnlyList<SadConsoleEditorTemplateBrushOption> TemplateInventoryBrushOptions()
    {
        var selectedId = SelectedTemplate()?.TemplateId;
        return _snapshot.EntityTemplates
            .Where(template => !string.Equals(template.TemplateId, selectedId, StringComparison.Ordinal))
            .Select(template => new SadConsoleEditorTemplateBrushOption(template.TemplateId, template.Name, template.Glyph, template.Color))
            .ToList();
    }

    public SadConsoleEditorMutationUiResult CycleTemplateInventoryBrush(int delta = 1)
    {
        if (_inventoryBrush is not { } brush)
        {
            return SadConsoleEditorMutationUiResult.Failure("No template inventory brush mode is active.");
        }

        var options = TemplateInventoryBrushOptions();
        if (options.Count == 0)
        {
            _inventoryBrush = brush with { BrushTemplateIndex = 0 };
            return SadConsoleEditorMutationUiResult.Success("No other entity templates are available as inventory brushes.");
        }

        var nextIndex = (brush.BrushTemplateIndex + delta) % options.Count;
        if (nextIndex < 0)
        {
            nextIndex += options.Count;
        }

        _inventoryBrush = brush with { BrushTemplateIndex = nextIndex };
        return SadConsoleEditorMutationUiResult.Success(TemplateInventoryBrushStatusMessage());
    }

    public SadConsoleEditorMutationUiResult MoveTemplateInventoryBrushCursor(int dx, int dy)
    {
        if (_inventoryBrush is not { } brush)
        {
            return SadConsoleEditorMutationUiResult.Failure("No template inventory brush mode is active.");
        }

        if (SelectedTemplate() is not { } template || !HasUsableInventory(template))
        {
            _inventoryBrush = brush with { Cursor = new GridCoord(0, 0) };
            return SadConsoleEditorMutationUiResult.Success("Selected template has no usable inventory grid; brush cursor cannot move and placement is disabled.");
        }

        var next = new GridCoord(
            Math.Clamp(brush.Cursor.X + dx, 0, template.InventoryWidth - 1),
            Math.Clamp(brush.Cursor.Y + dy, 0, template.InventoryHeight - 1));
        _inventoryBrush = brush with { Cursor = next };
        return SadConsoleEditorMutationUiResult.Success(TemplateInventoryBrushStatusMessage());
    }

    public SadConsoleEditorMutationUiResult PlaceTemplateInventoryBrush()
    {
        if (_inventoryBrush is not { } brush)
        {
            return SadConsoleEditorMutationUiResult.Failure("No template inventory brush mode is active.");
        }

        if (SelectedTemplate() is not { } template)
        {
            ClearTemplateInventoryBrush();
            return SadConsoleEditorMutationUiResult.Failure("No authored template is selected; inventory brush mode cancelled.");
        }

        if (!HasUsableInventory(template))
        {
            return SadConsoleEditorMutationUiResult.Failure($"Template {template.TemplateId} has no usable inventory grid; placement is disabled.");
        }

        var options = TemplateInventoryBrushOptions();
        if (options.Count == 0)
        {
            return SadConsoleEditorMutationUiResult.Failure("No other entity templates are available as inventory brushes.");
        }

        var option = options[Math.Clamp(brush.BrushTemplateIndex, 0, options.Count - 1)];
        var result = _service.PlaceTemplateInInventory(template.TemplateId, option.TemplateId, brush.Cursor);
        ReplaceSnapshotAfterMutation(
            result.Snapshot,
            template.TemplateId,
            markPreviewStale: result.IsSuccess,
            previewStaleReason: "Preview marked stale because authored template carried inventory changed. Press P to rematerialize.");
        ClampTemplateInventoryBrushState();
        return new SadConsoleEditorMutationUiResult(result.IsSuccess, result.StatusMessage);
    }

    private void LoadTargetingRuleSlot(int slot)
    {
        if (SelectedTemplate() is not { } template)
        {
            ClearTemplateTargetingRuleEditor();
            return;
        }

        slot = Math.Clamp(slot, 1, 4);
        var existing = template.TargetingRules.FirstOrDefault(rule => rule.Slot == slot);
        var options = TargetTemplatePickerOptions();
        var defaultTarget = existing?.TargetTemplateId ?? template.TemplateId;
        var targetIndex = options.ToList().FindIndex(option => string.Equals(option.TemplateId, defaultTarget, StringComparison.Ordinal));
        if (targetIndex < 0)
        {
            targetIndex = options.ToList().FindIndex(option => string.Equals(option.TemplateId, template.TemplateId, StringComparison.Ordinal));
        }

        targetIndex = ClampIndex(targetIndex, options.Count);
        var targetId = options.Count == 0 ? string.Empty : options[targetIndex].TemplateId;
        _targetingRuleEdit = new SadConsoleEditorTargetingRuleEditState(
            slot,
            existing?.Label ?? string.Empty,
            targetId,
            targetIndex,
            existing?.Range ?? 0,
            IsEditingLabel: false,
            LabelBuffer: existing?.Label ?? string.Empty,
            ExistingRulePresent: existing is not null);
    }

    private SadConsoleEditorMutationUiResult ApplyTemplatePresentationUpdate(string templateId, FrontendEditorTemplatePresentationUpdate update)
    {
        var result = _service.UpdateTemplatePresentation(templateId, update);
        ReplaceSnapshotAfterMutation(
            result.Snapshot,
            templateId,
            markPreviewStale: result.IsSuccess,
            previewStaleReason: "Preview marked stale because authored template presentation changed. Press P to rematerialize.");
        return new SadConsoleEditorMutationUiResult(result.IsSuccess, result.StatusMessage);
    }

    private void ReplaceSnapshotAfterMutation(FrontendEditorSnapshot snapshot, string? selectedTemplateId, bool markPreviewStale, string? previewStaleReason = null)
    {
        _snapshot = snapshot;
        TryRestoreTemplateSelection(selectedTemplateId);
        ClampAllSelections();

        if (!markPreviewStale)
        {
            return;
        }

        _cachedPreview = null;
        SelectedPreviewEntityIndex = 0;
        _previewInvalidationReason = previewStaleReason ?? "Preview marked stale because authored content changed. Press P to rematerialize.";
    }

    private void ReplaceSnapshotAfterActionPlanMutation(FrontendEditorSnapshot snapshot, string actionPlanId, int preferredStepOrInsertIndex, bool markPreviewStale, string? previewStaleReason = null)
    {
        var availableKind = SelectedAvailableActionStep()?.Kind;
        _snapshot = snapshot;
        TryRestoreActionPlanSelection(actionPlanId);
        if (_actionStepEdit is not null)
        {
            var availableIndex = availableKind is null
                ? _actionStepEdit.AvailableActionStepIndex
                : _snapshot.AvailableActionSteps.ToList().FindIndex(option => option.Kind == availableKind.Value);
            _actionStepEdit = _actionStepEdit with
            {
                StepOrInsertIndex = preferredStepOrInsertIndex,
                AvailableActionStepIndex = availableIndex >= 0 ? availableIndex : _actionStepEdit.AvailableActionStepIndex
            };
        }

        ClampAllSelections();
        ClampActionStepEditorState();

        if (!markPreviewStale)
        {
            return;
        }

        _cachedPreview = null;
        SelectedPreviewEntityIndex = 0;
        _previewInvalidationReason = previewStaleReason ?? "Preview marked stale because authored action plan steps changed. Press P to rematerialize.";
    }

    private string EditStatusMessage() => _templateEditMode switch
    {
        SadConsoleEditorTemplateEditMode.Name => $"Editing template name: {_templateEditBuffer}",
        SadConsoleEditorTemplateEditMode.Glyph => $"Editing template glyph: {(_templateEditBuffer.Length == 0 ? "<empty>" : _templateEditBuffer[0])}",
        _ => "No template presentation edit is active."
    };

    private void ClearTemplateEdit()
    {
        _templateEditMode = SadConsoleEditorTemplateEditMode.None;
        _templateEditBuffer = string.Empty;
    }

    private void ClearTemplateDefaultActionPlanPicker()
    {
        _isPickingTemplateDefaultActionPlan = false;
        _templateDefaultActionPlanPickerIndex = 0;
    }

    private void ClearActionPlanStepEditor()
    {
        _actionStepEdit = null;
    }

    private void ClearTemplateTargetingRuleEditor()
    {
        _targetingRuleEdit = null;
    }

    private void ClearTemplateInventoryBrush()
    {
        _inventoryBrush = null;
    }

    private void ClampActionStepEditorState()
    {
        if (_actionStepEdit is not { } edit)
        {
            return;
        }

        var stepCount = SelectedActionPlan()?.ActionSteps.Count ?? 0;
        _actionStepEdit = edit with
        {
            StepOrInsertIndex = Math.Clamp(edit.StepOrInsertIndex, 0, stepCount),
            AvailableActionStepIndex = ClampIndex(edit.AvailableActionStepIndex, _snapshot.AvailableActionSteps.Count)
        };
    }

    private void ClampTemplateInventoryBrushState()
    {
        if (_inventoryBrush is not { } brush)
        {
            return;
        }

        var options = TemplateInventoryBrushOptions();
        var brushIndex = ClampIndex(brush.BrushTemplateIndex, options.Count);
        var template = SelectedTemplate();
        var cursor = template is not null && HasUsableInventory(template)
            ? new GridCoord(
                Math.Clamp(brush.Cursor.X, 0, template.InventoryWidth - 1),
                Math.Clamp(brush.Cursor.Y, 0, template.InventoryHeight - 1))
            : new GridCoord(0, 0);
        _inventoryBrush = brush with { Cursor = cursor, BrushTemplateIndex = brushIndex };
    }

    private string TemplateInventoryBrushStatusMessage()
    {
        if (_inventoryBrush is not { } brush)
        {
            return "No template inventory brush mode is active.";
        }

        var template = SelectedTemplate();
        if (template is null)
        {
            return "No authored template is selected for inventory brush placement.";
        }

        if (!HasUsableInventory(template))
        {
            return $"Template {template.TemplateId} has no usable inventory grid; placement is disabled.";
        }

        var options = TemplateInventoryBrushOptions();
        var brushText = options.Count == 0
            ? "<no brush templates available>"
            : FormatBrushOption(options[Math.Clamp(brush.BrushTemplateIndex, 0, options.Count - 1)]);
        return $"Inventory brush for {template.TemplateId}: {brushText} at {brush.Cursor}. Place-only; no overwrite/delete/move.";
    }

    private static string FormatBrushOption(SadConsoleEditorTemplateBrushOption option) =>
        $"{option.Glyph} {option.Name} ({option.TemplateId}) color {option.Color}";

    private static bool HasUsableInventory(FrontendEditorEntityTemplateSummary template) =>
        template.InventoryWidth > 0 && template.InventoryHeight > 0;

    private string TargetingRuleStatusMessage()
    {
        if (_targetingRuleEdit is not { } edit)
        {
            return "No targeting rule editor is active.";
        }

        var label = edit.IsEditingLabel ? edit.LabelBuffer : edit.Label;
        return $"Targeting slot {edit.Slot}: label '{(string.IsNullOrEmpty(label) ? "<blank>" : label)}' target {edit.TargetTemplateId} range {edit.Range}.";
    }

    private static string FirstGlyphText(string text) =>
        string.IsNullOrEmpty(text) ? string.Empty : text[0].ToString();

    private bool HasCurrentScenarioPreview() =>
        _cachedPreview is not null && string.Equals(_cachedPreview.ScenarioId, SelectedScenario()?.ScenarioId, StringComparison.Ordinal);

    internal static IReadOnlyList<SadConsoleEditorPreviewTreeNode> BuildEntityLocationTreeNodes(PlayableScenarioSession session)
    {
        var rootId = session.ActiveContainerEntityId;
        if (!session.World.Entities.ContainsKey(rootId))
        {
            return [];
        }

        var rows = new List<SadConsoleEditorPreviewTreeNode>();
        AddEntityLocationTreeNodes(session, rootId, depth: 0, visited: [], rows);
        return rows;
    }

    private static void AddEntityLocationTreeNodes(
        PlayableScenarioSession session,
        EntityId entityId,
        int depth,
        HashSet<EntityId> visited,
        List<SadConsoleEditorPreviewTreeNode> rows)
    {
        if (!session.World.Entities.TryGetValue(entityId, out var entity))
        {
            return;
        }

        rows.Add(new SadConsoleEditorPreviewTreeNode(entityId, entity.Name, depth));

        if (!visited.Add(entityId))
        {
            return;
        }

        foreach (var childId in GetContainedEntityIds(session.World, entityId))
        {
            AddEntityLocationTreeNodes(session, childId, depth + 1, visited, rows);
        }

        visited.Remove(entityId);
    }

    internal static IReadOnlyList<EntityId> GetContainedEntityIds(WorldState world, EntityId ownerId)
    {
        var inventoryPlaneId = world.GetInventoryPlaneId(ownerId);
        if (inventoryPlaneId is null)
        {
            return [];
        }

        return world.Occupancy
            .Where(entry => world.Nodes.TryGetValue(entry.Key, out var node) && node.PlaneId == inventoryPlaneId)
            .Select(entry => (EntityId: entry.Value, Coord: world.Nodes[entry.Key].Coord))
            .OrderBy(entry => entry.Coord.Y)
            .ThenBy(entry => entry.Coord.X)
            .ThenBy(entry => entry.EntityId.Value, StringComparer.Ordinal)
            .Select(entry => entry.EntityId)
            .ToList();
    }
}

internal sealed record SadConsoleEditorPreviewTreeNode(EntityId EntityId, string Name, int Depth);

internal sealed record SadConsoleEditorCommandMenuEntry(SadConsoleEditorCommandId CommandId, string Label, string HelpText);

internal sealed record SadConsoleEditorCommandMenuActivationResult(
    SadConsoleEditorCommandMenuEntry? Entry,
    string Message,
    bool Succeeded,
    bool RequestsSimulationLaunch)
{
    public static SadConsoleEditorCommandMenuActivationResult None(string message) => new(null, message, false, RequestsSimulationLaunch: false);
    public static SadConsoleEditorCommandMenuActivationResult Completed(SadConsoleEditorCommandMenuEntry entry, string message, bool succeeded = true) => new(entry, message, succeeded, RequestsSimulationLaunch: false);
    public static SadConsoleEditorCommandMenuActivationResult Mutation(SadConsoleEditorCommandMenuEntry entry, SadConsoleEditorMutationUiResult result) => new(entry, result.Message, result.Succeeded, RequestsSimulationLaunch: false);
    public static SadConsoleEditorCommandMenuActivationResult LaunchSimulation(SadConsoleEditorCommandMenuEntry entry, string message) => new(entry, message, true, RequestsSimulationLaunch: true);
}

internal enum SadConsoleEditorCommandId
{
    Save,
    Refresh,
    LaunchSimulation,
    RematerializePreview,
    ToggleYamlDiff,
    EditTemplateName,
    EditTemplateGlyph,
    CycleTemplateColor,
    SetTemplateDefaultActionPlan,
    EditTemplateTargetingRules,
    InventoryBrushMode,
    OpenActionStepEditor,
    JumpPreviewRowToSourceTemplate
}

internal sealed record SadConsoleEditorActionPlanPickerOption(string? ActionPlanId, string Label);

internal sealed record SadConsoleEditorActionStepEditState(int StepOrInsertIndex, int AvailableActionStepIndex);

internal sealed record SadConsoleEditorTargetTemplatePickerOption(string TemplateId, string Label);

internal sealed record SadConsoleEditorTemplateBrushOption(string TemplateId, string Name, char Glyph, PresentationColor Color);

internal sealed record SadConsoleEditorInventoryBrushState(GridCoord Cursor, int BrushTemplateIndex);

internal sealed record SadConsoleEditorTargetingRuleEditState(
    int Slot,
    string Label,
    string TargetTemplateId,
    int TargetTemplateIndex,
    int Range,
    bool IsEditingLabel,
    string LabelBuffer,
    bool ExistingRulePresent);

internal sealed record SadConsoleEditorRefreshResult(
    string Message,
    bool PreviewWasCleared,
    bool ScenarioSelectionPreserved,
    bool TemplateSelectionPreserved,
    bool ActionPlanSelectionPreserved);

internal sealed record SadConsoleEditorSourceJumpResult(bool Succeeded, string Message)
{
    public static SadConsoleEditorSourceJumpResult Success(string message) => new(true, message);
    public static SadConsoleEditorSourceJumpResult Failure(string message) => new(false, message);
}

internal sealed record SadConsoleEditorMutationUiResult(bool Succeeded, string Message)
{
    public static SadConsoleEditorMutationUiResult Success(string message) => new(true, message);
    public static SadConsoleEditorMutationUiResult Failure(string message) => new(false, message);
}

internal enum SadConsoleEditorTemplateEditMode
{
    None,
    Name,
    Glyph
}

internal enum SadConsoleEditorSection
{
    Scenarios,
    Templates,
    ActionPlans,
    Diagnostics,
    YamlAndDiff,
    Preview
}

internal enum SadConsoleEditorTextSurface
{
    YamlPreview,
    Diff
}

internal sealed record SadConsoleEditorOpenResult(SadConsoleEditorContext? Context, string? ErrorMessage)
{
    public bool IsSuccess => Context is not null;

    public static SadConsoleEditorOpenResult Success(SadConsoleEditorContext context) => new(context, ErrorMessage: null);
    public static SadConsoleEditorOpenResult Failure(string errorMessage) => new(Context: null, errorMessage);
}

internal sealed record SadConsoleEditorView(
    string Header,
    string Message,
    string FileLine,
    string DirtyLine,
    string CountLine,
    string SelectedScenarioLine,
    string PromptHint,
    string SectionLine,
    string DetailHeader,
    IReadOnlyList<string> ScenarioRows,
    IReadOnlyList<string> DetailRows,
    IReadOnlyList<string> DiagnosticRows,
    IReadOnlyList<string> PreviewRows);

internal static class SadConsoleEditorViewBuilder
{
    public static SadConsoleEditorView Build(SadConsoleEditorContext context, string message)
    {
        var snapshot = context.Snapshot();
        var selected = context.SelectedScenario();
        var scenarios = snapshot.Scenarios;
        var rows = BuildScenarioRows(context, scenarios, 14);

        var diagnostics = BuildDiagnosticRows(snapshot.ValidationDiagnostics, 8);
        var detail = BuildDetailRows(context, snapshot, 18);

        return new SadConsoleEditorView(
            "GameGameGame SadConsole | Editor mode (authored content)",
            message,
            $"Authored content file: {snapshot.FilePath ?? context.ContentPath}",
            $"Cached snapshot dirty state: {(snapshot.IsDirty ? "dirty" : "clean")} | Template presentation/default-plan/targeting/inventory-brush and action-plan step edits enabled through editor services.",
            $"Authored counts: scenarios {snapshot.Scenarios.Count} | templates {snapshot.EntityTemplates.Count} | action plans {snapshot.ActionPlans.Count} | diagnostics {snapshot.ValidationDiagnostics.Count}",
            selected is null ? "Selected authored scenario: none" : $"Selected authored scenario: {selected.Name} ({selected.ScenarioId}) root {selected.ScenarioRootEntityTemplateId} player {selected.PlayerEntityTemplateId} start {selected.PlayerStart}",
            BuildPromptHint(context),
            BuildSectionLine(context.Section),
            BuildDetailHeader(context, snapshot),
            rows,
            detail,
            diagnostics,
            BuildPreviewRows(context, selected, 10));
    }

    private static IReadOnlyList<string> BuildScenarioRows(SadConsoleEditorContext context, IReadOnlyList<FrontendEditorScenarioSummary> scenarios, int count)
    {
        var first = Math.Max(0, Math.Min(context.SelectedScenarioIndex - count / 2, Math.Max(0, scenarios.Count - count)));
        var rows = scenarios
            .Skip(first)
            .Take(count)
            .Select((scenario, offset) =>
            {
                var index = first + offset;
                var marker = context.Section == SadConsoleEditorSection.Scenarios && index == context.SelectedScenarioIndex ? '>' : ' ';
                return $"{marker} {scenario.Name} ({scenario.ScenarioId}) root:{scenario.ScenarioRootEntityTemplateId} player:{scenario.PlayerEntityTemplateId}";
            })
            .ToList();

        if (rows.Count == 0)
        {
            rows.Add("No authored scenarios in this content snapshot.");
        }

        return rows;
    }

    private static IReadOnlyList<string> BuildDiagnosticRows(IReadOnlyList<FrontendEditorDiagnostic> validationDiagnostics, int count)
    {
        var diagnostics = validationDiagnostics
            .Take(count)
            .Select(FormatDiagnostic)
            .ToList();

        if (diagnostics.Count == 0)
        {
            diagnostics.Add("No validation diagnostics from editor services.");
        }

        return diagnostics;
    }

    private static IReadOnlyList<string> BuildDetailRows(SadConsoleEditorContext context, FrontendEditorSnapshot snapshot, int count)
    {
        if (context.IsCommandMenuOpen)
        {
            return BuildCommandMenuRows(context, count);
        }

        return context.Section switch
        {
            SadConsoleEditorSection.Scenarios => BuildScenarioDetailRows(context, snapshot, count),
            SadConsoleEditorSection.Templates => BuildTemplateBrowserRows(context, snapshot, count),
            SadConsoleEditorSection.ActionPlans => BuildActionPlanRows(context, snapshot, count),
            SadConsoleEditorSection.Diagnostics => BuildGroupedDiagnosticRows(context, snapshot, count),
            SadConsoleEditorSection.YamlAndDiff => BuildTextRows(context, snapshot, count),
            SadConsoleEditorSection.Preview => BuildPreviewRows(context, context.SelectedScenario(), count),
            _ => []
        };
    }

    private static string BuildPromptHint(SadConsoleEditorContext context)
    {
        if (context.IsCommandMenuOpen)
        {
            return "Editor command menu: Up/Down command, Enter/Select activates, Esc cancels. Direct hotkeys remain temporary shortcuts.";
        }

        var contextual = context.Section switch
        {
            SadConsoleEditorSection.Scenarios => "Scenarios: Up/Down selects; M launches Simulation; P/command menu rematerializes Preview.",
            SadConsoleEditorSection.Templates => "Templates: command menu offers name/glyph/color/default plan/targeting/inventory brush. Temporary hotkeys: N/G/C/A/Y/B.",
            SadConsoleEditorSection.ActionPlans => "Action Plans: command menu or A opens step editor; step submode uses Up/Down, Left/Right/Tab, R/I, Esc.",
            SadConsoleEditorSection.Diagnostics => "Diagnostics: Up/Down reviews validation rows.",
            SadConsoleEditorSection.YamlAndDiff => "YAML/Diff: Up/Down scrolls; command menu or T toggles read-only YAML/Diff.",
            SadConsoleEditorSection.Preview => "Preview: command menu offers Rematerialize Preview and Jump row to source; temporary hotkeys P/J.",
            _ => "Editor browser"
        };

        return $"Mode: Editor/authored content. Enter opens command menu. M launches selected scenario. Left/Right sections. Up/Down selection/scroll. Esc/Cancel returns. S saves, R refreshes/revalidates cached snapshot, P Preview, T YAML/Diff, J jumps Preview row to source template remain shortcuts. {contextual}";
    }

    private static IReadOnlyList<string> BuildCommandMenuRows(SadConsoleEditorContext context, int count)
    {
        var entries = context.CommandMenuEntries();
        var rows = new List<string>
        {
            $"Command menu for {SectionName(context.Section)} (Editor/authored context): Up/Down choose, Enter/Select activate, Esc cancel."
        };

        if (entries.Count == 0)
        {
            rows.Add("No commands available for current focus.");
            return rows;
        }

        var first = WindowFirst(context.CommandMenuSelectedIndex, entries.Count, Math.Max(1, count - 1));
        rows.AddRange(entries.Skip(first).Take(Math.Max(1, count - 1)).Select((entry, offset) =>
        {
            var index = first + offset;
            var marker = index == context.CommandMenuSelectedIndex ? '>' : ' ';
            return $"{marker} {entry.Label} - {entry.HelpText}";
        }));

        return rows.Take(count).ToList();
    }

    private static IReadOnlyList<string> BuildScenarioDetailRows(SadConsoleEditorContext context, FrontendEditorSnapshot snapshot, int count)
    {
        if (snapshot.Scenarios.Count == 0)
        {
            return ["No scenarios available."];
        }

        var scenario = snapshot.Scenarios[Math.Clamp(context.SelectedScenarioIndex, 0, snapshot.Scenarios.Count - 1)];
        return [
            $"Authored scenario: {scenario.Name} ({scenario.ScenarioId})",
            $"Root template: {scenario.ScenarioRootEntityTemplateId}",
            $"Player template/entity: {scenario.PlayerEntityTemplateId} / {scenario.PlayerEntityId}",
            $"Player start: {scenario.PlayerStart}",
            "M launches a derived runtime Simulation. Runtime state is not written back to authored content.",
            "Template presentation edits use shared editor services; runtime Simulation state is never written back."
        ];
    }

    private static IReadOnlyList<string> BuildTemplateBrowserRows(SadConsoleEditorContext context, FrontendEditorSnapshot snapshot, int count)
    {
        if (snapshot.EntityTemplates.Count == 0)
        {
            return ["No entity templates available."];
        }

        var selected = snapshot.EntityTemplates[Math.Clamp(context.SelectedTemplateIndex, 0, snapshot.EntityTemplates.Count - 1)];
        var rows = BuildSelectedTemplateDetailRows(context, snapshot, selected).Take(count).ToList();
        if (rows.Count >= count)
        {
            return rows;
        }

        rows.Add("Template list (authored; Up/Down selects):");
        if (rows.Count >= count)
        {
            return rows;
        }

        rows.AddRange(BuildTemplateListRows(context, snapshot, count - rows.Count));
        return rows.Take(count).ToList();
    }

    private static IReadOnlyList<string> BuildTemplateListRows(SadConsoleEditorContext context, FrontendEditorSnapshot snapshot, int count)
    {
        var first = WindowFirst(context.SelectedTemplateIndex, snapshot.EntityTemplates.Count, count);
        return snapshot.EntityTemplates.Skip(first).Take(count).Select((template, offset) =>
        {
            var index = first + offset;
            var marker = index == context.SelectedTemplateIndex ? '>' : ' ';
            var carried = template.CarriedEntities.Count == 0
                ? "carried:none"
                : $"carried:{template.CarriedEntities.Count} [{string.Join(", ", template.CarriedEntities.Take(3).Select(FormatCarried))}{(template.CarriedEntities.Count > 3 ? ", ..." : string.Empty)}]";
            return $"{marker} {template.Glyph} {template.Name} ({template.TemplateId}) inv:{template.InventoryWidth}x{template.InventoryHeight} bulk:{template.Bulk} aperture:{template.Aperture} plan:{template.DefaultActionPlanId ?? "none"} {carried}";
        }).ToList();
    }

    private static IReadOnlyList<string> BuildSelectedTemplateDetailRows(SadConsoleEditorContext context, FrontendEditorSnapshot snapshot, FrontendEditorEntityTemplateSummary template)
    {
        var rows = new List<string>
        {
            $"Authored entity template panel (presentation editable): {template.Glyph} {template.Name} ({template.TemplateId})",
            $"Identity/presentation: template id {template.TemplateId} | glyph '{template.Glyph}' | color {template.Color}",
            TemplateEditStatusLine(context),
            $"Template metadata: inventory {template.InventoryWidth}x{template.InventoryHeight} | bulk {template.Bulk} | aperture {template.Aperture}",
            $"Default action plan id: {template.DefaultActionPlanId ?? "none"}",
            FormatAssignedActionPlanSummary(snapshot, template.DefaultActionPlanId),
            $"Action-state defaults: facing {template.ActionStateDefaults.Facing?.ToString() ?? "none"} | target entity id {template.ActionStateDefaults.TargetEntityId ?? "none"}"
        };

        if (context.IsPickingTemplateDefaultActionPlan)
        {
            rows.AddRange(BuildTemplateDefaultActionPlanPickerRows(context, snapshot));
        }

        if (context.IsEditingTemplateTargetingRule)
        {
            rows.AddRange(BuildTemplateTargetingRuleEditorRows(context, snapshot, template));
        }

        if (context.IsTemplateInventoryBrushActive)
        {
            rows.AddRange(BuildTemplateInventoryBrushRows(context, template));
        }

        rows.Add(template.CarriedEntities.Count == 0
            ? "Authored starting inventory/carried layout: none"
            : $"Authored starting inventory/carried layout ({template.CarriedEntities.Count}): {string.Join("; ", template.CarriedEntities.Take(4).Select(FormatCarriedDetail))}{(template.CarriedEntities.Count > 4 ? "; ..." : string.Empty)}");

        rows.Add(template.TargetingRules.Count == 0
            ? "Targeting rules: none"
            : $"Targeting rules ({template.TargetingRules.Count}): {string.Join("; ", template.TargetingRules.Take(3).Select(FormatTargetingRule))}{(template.TargetingRules.Count > 3 ? "; ..." : string.Empty)}");

        if (template.Diagnostics.Count == 0)
        {
            rows.Add("Template diagnostics: none");
        }
        else
        {
            rows.Add($"Template diagnostics: {string.Join(" | ", template.Diagnostics.Take(2).Select(FormatDiagnostic))}{(template.Diagnostics.Count > 2 ? " | ..." : string.Empty)}");
        }

        var carriedDiagnostics = template.CarriedEntities
            .Where(carried => carried.Diagnostics.Count > 0)
            .Select(carried => $"{carried.EntityId}: {string.Join(" / ", carried.Diagnostics.Take(2).Select(FormatDiagnostic))}{(carried.Diagnostics.Count > 2 ? " / ..." : string.Empty)}")
            .ToList();
        rows.Add(carriedDiagnostics.Count == 0
            ? "Carried diagnostics: none"
            : $"Carried diagnostics: {string.Join(" | ", carriedDiagnostics.Take(2))}{(carriedDiagnostics.Count > 2 ? " | ..." : string.Empty)}");

        rows.Add("Authored/template facts only: not runtime location, initiative, command history, or Simulation log data.");
        return rows;
    }

    private static string TemplateEditStatusLine(SadConsoleEditorContext context)
    {
        if (context.IsPickingTemplateDefaultActionPlan)
        {
            return "Default action plan picker active: Up/Down selects existing plan or none, Enter applies through editor service, Esc cancels.";
        }

        if (context.IsTemplateInventoryBrushActive)
        {
            return "Inventory brush active: Tab/E cycles brush, arrows move grid cursor, Enter places through editor service, Esc exits. Place-only; no overwrite/delete/move.";
        }

        return context.TemplateEditMode == SadConsoleEditorTemplateEditMode.None
            ? "Edit controls: N name, G glyph, C cycle color, A default plan, Y targeting rules, B inventory brush, S save. Enter opens the command menu; M launches Simulation."
            : $"Editing {context.TemplateEditMode}: '{context.TemplateEditBuffer}' (Enter applies, Esc cancels).";
    }

    private static IReadOnlyList<string> BuildTemplateInventoryBrushRows(SadConsoleEditorContext context, FrontendEditorEntityTemplateSummary template)
    {
        var brush = context.InventoryBrush;
        if (brush is null)
        {
            return [];
        }

        var rows = new List<string>
        {
            "Inventory brush mode: authored template inventory placement (place-only; no overwrite/delete/move yet)."
        };

        if (!HasUsableInventory(template))
        {
            rows.Add($"No usable inventory: selected template inventory is {template.InventoryWidth}x{template.InventoryHeight}; placement disabled.");
            return rows;
        }

        var options = context.TemplateInventoryBrushOptions();
        if (options.Count == 0)
        {
            rows.Add("Brush template: none available (current template is excluded; no other entity templates exist).");
        }
        else
        {
            var option = options[Math.Clamp(brush.BrushTemplateIndex, 0, options.Count - 1)];
            rows.Add($"Brush template: {FormatBrushOption(option)} ({brush.BrushTemplateIndex + 1}/{options.Count}); current template excluded.");
        }

        rows.Add($"Cursor: {brush.Cursor}; Enter places only into empty cells via FrontendEditorService.PlaceTemplateInInventory.");
        rows.Add($"Authored inventory grid {template.InventoryWidth}x{template.InventoryHeight}:");
        rows.AddRange(BuildTemplateInventoryGridRows(template, brush.Cursor));
        return rows;
    }

    private static IEnumerable<string> BuildTemplateInventoryGridRows(FrontendEditorEntityTemplateSummary template, GridCoord cursor)
    {
        for (var y = 0; y < template.InventoryHeight; y++)
        {
            var cells = new List<string>();
            for (var x = 0; x < template.InventoryWidth; x++)
            {
                var coord = new GridCoord(x, y);
                var carried = template.CarriedEntities.FirstOrDefault(entity => entity.Coord == coord);
                var glyph = carried?.Glyph ?? (carried is null ? '.' : '?');
                cells.Add(coord == cursor ? $"[{glyph}]" : $" {glyph} ");
            }

            yield return string.Concat(cells) + $"  y={y}";
        }
    }

    private static IReadOnlyList<string> BuildTemplateTargetingRuleEditorRows(SadConsoleEditorContext context, FrontendEditorSnapshot snapshot, FrontendEditorEntityTemplateSummary template)
    {
        if (context.TargetingRuleEdit is not { } edit)
        {
            return [];
        }

        var rows = new List<string>
        {
            "Targeting rule editor active: Up/Down slot, L label, E cycle target template, +/- range, Enter apply, X/Delete clear, Esc exit.",
            edit.IsEditingLabel
                ? $"Editing slot {edit.Slot} label buffer: '{edit.LabelBuffer}' (lowercase alphanumeric required by service; Enter accepts pending label; Esc cancels label edit)."
                : $"Pending slot {edit.Slot}: label '{(string.IsNullOrWhiteSpace(edit.Label) ? "<blank>" : edit.Label)}' | target {FormatTargetTemplate(snapshot, edit.TargetTemplateId)} | range {edit.Range} | {(edit.ExistingRulePresent ? "existing rule" : "empty slot/new rule")}",
            "Slots 1-4:"
        };

        for (var slot = 1; slot <= 4; slot++)
        {
            var marker = slot == edit.Slot ? '>' : ' ';
            var rule = template.TargetingRules.FirstOrDefault(candidate => candidate.Slot == slot);
            rows.Add(rule is null
                ? $"{marker} slot {slot}: <empty>"
                : $"{marker} {FormatTargetingRule(rule)}");
        }

        var options = context.TargetTemplatePickerOptions();
        if (options.Count > 0)
        {
            var option = options[Math.Clamp(edit.TargetTemplateIndex, 0, options.Count - 1)];
            rows.Add($"Target template picker: {option.Label} ({edit.TargetTemplateIndex + 1}/{options.Count}); self/current template is allowed.");
        }

        return rows;
    }

    private static string FormatTargetTemplate(FrontendEditorSnapshot snapshot, string templateId)
    {
        var template = snapshot.EntityTemplates.FirstOrDefault(candidate => string.Equals(candidate.TemplateId, templateId, StringComparison.Ordinal));
        return template is null ? templateId : $"{template.Name} ({template.TemplateId})";
    }

    private static string FormatAssignedActionPlanSummary(FrontendEditorSnapshot snapshot, string? actionPlanId)
    {
        if (string.IsNullOrWhiteSpace(actionPlanId))
        {
            return "Assigned action plan summary (read-only): none";
        }

        var plan = snapshot.ActionPlans.FirstOrDefault(candidate => string.Equals(candidate.ActionPlanId, actionPlanId, StringComparison.Ordinal));
        if (plan is null)
        {
            return $"Assigned action plan summary (read-only): missing plan '{actionPlanId}'";
        }

        var steps = plan.ActionStepNames.Count == 0 ? "steps:none" : $"steps:{string.Join(" -> ", plan.ActionStepNames)}";
        return $"Assigned action plan summary (read-only): {plan.ActionPlanId} shape:{plan.Shape} {steps}";
    }

    private static IReadOnlyList<string> BuildTemplateDefaultActionPlanPickerRows(SadConsoleEditorContext context, FrontendEditorSnapshot snapshot)
    {
        var options = context.TemplateDefaultActionPlanPickerOptions();
        var first = WindowFirst(context.TemplateDefaultActionPlanPickerIndex, options.Count, Math.Min(5, options.Count));
        var rows = new List<string> { "Default action plan picker options:" };
        rows.AddRange(options.Skip(first).Take(5).Select((option, offset) =>
        {
            var index = first + offset;
            var marker = index == context.TemplateDefaultActionPlanPickerIndex ? '>' : ' ';
            if (option.ActionPlanId is null)
            {
                return $"{marker} none (clear default action plan)";
            }

            var plan = snapshot.ActionPlans.FirstOrDefault(candidate => string.Equals(candidate.ActionPlanId, option.ActionPlanId, StringComparison.Ordinal));
            var steps = plan is null || plan.ActionStepNames.Count == 0 ? "steps:none" : $"steps:{string.Join(" -> ", plan.ActionStepNames)}";
            return $"{marker} {option.Label} {steps}";
        }));
        return rows;
    }

    private static IReadOnlyList<string> BuildActionPlanRows(SadConsoleEditorContext context, FrontendEditorSnapshot snapshot, int count)
    {
        if (snapshot.ActionPlans.Count == 0)
        {
            return ["No action plans available."];
        }

        var selected = snapshot.ActionPlans[Math.Clamp(context.SelectedActionPlanIndex, 0, snapshot.ActionPlans.Count - 1)];
        var rows = BuildSelectedActionPlanDetailRows(context, snapshot, selected).Take(count).ToList();
        if (rows.Count >= count)
        {
            return rows;
        }

        rows.Add("Action plan list (authored; Up/Down selects when step editor is closed):");
        if (rows.Count >= count)
        {
            return rows;
        }

        var first = WindowFirst(context.SelectedActionPlanIndex, snapshot.ActionPlans.Count, count - rows.Count);
        rows.AddRange(snapshot.ActionPlans.Skip(first).Take(count - rows.Count).Select((plan, offset) =>
        {
            var index = first + offset;
            var marker = index == context.SelectedActionPlanIndex ? '>' : ' ';
            var steps = FormatActionPlanStepChain(plan);
            return $"{marker} {plan.ActionPlanId} shape:{plan.Shape} {steps}";
        }));
        return rows.Take(count).ToList();
    }

    private static IReadOnlyList<string> BuildSelectedActionPlanDetailRows(SadConsoleEditorContext context, FrontendEditorSnapshot snapshot, FrontendEditorActionPlanSummary plan)
    {
        var rows = new List<string>
        {
            $"Authored action plan panel (step kind editable): {plan.ActionPlanId}",
            $"Shape: {plan.Shape} | {FormatActionPlanStepChain(plan)}",
            context.IsEditingActionPlanSteps
                ? "Action-step editor active: Up/Down selects existing step or append slot; Tab/Left/Right cycles engine-defined step; R replaces existing; I inserts; Esc exits."
                : "Edit controls: A opens action-step editor. Step target labels/plan ids remain read-only in this slice."
        };

        if (context.IsEditingActionPlanSteps)
        {
            rows.AddRange(BuildActionStepEditorRows(context, snapshot, plan));
        }
        else
        {
            rows.Add("Steps (read-only until A opens editor):");
            rows.AddRange(plan.ActionSteps.Count == 0
                ? ["  <empty passive plan>"]
                : plan.ActionSteps.Select(step => $"  {FormatActionPlanStep(step)}"));
        }

        return rows;
    }

    private static IReadOnlyList<string> BuildActionStepEditorRows(SadConsoleEditorContext context, FrontendEditorSnapshot snapshot, FrontendEditorActionPlanSummary plan)
    {
        if (context.ActionStepEdit is not { } edit)
        {
            return [];
        }

        var options = context.AvailableActionStepOptions();
        var option = options.Count == 0 ? null : options[Math.Clamp(edit.AvailableActionStepIndex, 0, options.Count - 1)];
        var selectedIndex = Math.Clamp(edit.StepOrInsertIndex, 0, plan.ActionSteps.Count);
        var replacePossible = selectedIndex < plan.ActionSteps.Count;
        var rows = new List<string>
        {
            option is null
                ? "Selected engine-defined action step: <none available>"
                : $"Selected engine-defined action step: {option.DisplayName} ({option.Kind}) [{edit.AvailableActionStepIndex + 1}/{options.Count}] - {option.Hint}",
            $"Selected position: {selectedIndex} | Replace possible: {(replacePossible ? "yes" : "no (append slot/empty plan)")} | Insert possible: yes (0..{plan.ActionSteps.Count})",
            "Editable step rows / insertion positions:"
        };

        for (var index = 0; index <= plan.ActionSteps.Count; index++)
        {
            var marker = index == selectedIndex ? '>' : ' ';
            rows.Add($"{marker} insert at {index}");
            if (index < plan.ActionSteps.Count)
            {
                var stepMarker = index == selectedIndex ? '>' : ' ';
                rows.Add($"{stepMarker} {FormatActionPlanStep(plan.ActionSteps[index])}");
            }
        }

        if (snapshot.AvailableActionSteps.Count > 0)
        {
            rows.Add($"Available action steps: {string.Join(", ", snapshot.AvailableActionSteps.Select(step => step.DisplayName))}");
        }

        return rows;
    }

    private static string FormatActionPlanStepChain(FrontendEditorActionPlanSummary plan) =>
        plan.ActionSteps.Count == 0 ? "steps:none" : $"steps:{string.Join(" -> ", plan.ActionSteps.Select(step => step.DisplayName))}";

    private static string FormatActionPlanStep(FrontendEditorActionPlanStepSummary step) =>
        $"step {step.Index}: {step.DisplayName} ({step.Kind})";

    private static IReadOnlyList<string> BuildGroupedDiagnosticRows(SadConsoleEditorContext context, FrontendEditorSnapshot snapshot, int count)
    {
        if (snapshot.ValidationDiagnostics.Count == 0)
        {
            return ["No validation diagnostics from editor services."];
        }

        var first = WindowFirst(context.SelectedDiagnosticIndex, snapshot.ValidationDiagnostics.Count, count);
        return snapshot.ValidationDiagnostics.Skip(first).Take(count).Select((diagnostic, offset) =>
        {
            var marker = first + offset == context.SelectedDiagnosticIndex ? '>' : ' ';
            return $"{marker} {FormatDiagnostic(diagnostic)}";
        }).ToList();
    }

    private static IReadOnlyList<string> BuildTextRows(SadConsoleEditorContext context, FrontendEditorSnapshot snapshot, int count)
    {
        var lines = context.TextSurface == SadConsoleEditorTextSurface.YamlPreview
            ? SplitLines(snapshot.YamlPreview)
            : (snapshot.YamlDiffLines.Count == 0 ? ["No diff lines reported by editor services."] : snapshot.YamlDiffLines);
        var maxOffset = Math.Max(0, lines.Count - count);
        var offset = Math.Clamp(context.YamlScrollOffset, 0, maxOffset);
        return lines.Skip(offset).Take(count).Select((line, i) => $"{offset + i + 1,4}: {line}").ToList();
    }

    private static string BuildSectionLine(SadConsoleEditorSection active) =>
        string.Join("  ", Enum.GetValues<SadConsoleEditorSection>().Select(section => section == active ? $"[{SectionName(section)}]" : SectionName(section)));

    private static string BuildDetailHeader(SadConsoleEditorContext context, FrontendEditorSnapshot snapshot) => context.IsCommandMenuOpen
        ? "Editor command menu (contextual controls)"
        : context.Section switch
    {
        SadConsoleEditorSection.Scenarios => "Scenario browser (authored definitions)",
        SadConsoleEditorSection.Templates => "Template browser (authored entity templates and carried layout summaries)",
        SadConsoleEditorSection.ActionPlans => "Action plan browser (authored plans; canonical step names where available)",
        SadConsoleEditorSection.Diagnostics => "Validation diagnostics grouped by authored object hints",
        SadConsoleEditorSection.YamlAndDiff => context.TextSurface == SadConsoleEditorTextSurface.YamlPreview
            ? $"YAML preview (read-only, {SplitLines(snapshot.YamlPreview).Count} lines)"
            : $"Diff surface (read-only, {snapshot.YamlDiffLines.Count} lines)",
        SadConsoleEditorSection.Preview => "Scenario preview (manual turn-0 derived runtime materialization)",
        _ => "Editor browser"
    };

    private static string SectionName(SadConsoleEditorSection section) => section switch
    {
        SadConsoleEditorSection.Scenarios => "Scenarios",
        SadConsoleEditorSection.Templates => "Templates",
        SadConsoleEditorSection.ActionPlans => "Action Plans",
        SadConsoleEditorSection.Diagnostics => "Diagnostics",
        SadConsoleEditorSection.YamlAndDiff => "YAML/Diff",
        SadConsoleEditorSection.Preview => "Preview",
        _ => section.ToString()
    };

    private static IReadOnlyList<string> BuildPreviewRows(SadConsoleEditorContext context, FrontendEditorScenarioSummary? selected, int count)
    {
        if (selected is null)
        {
            return ["No authored scenario is selected for preview."];
        }

        var preview = context.CachedPreview;
        if (preview is null || !string.Equals(preview.ScenarioId, selected.ScenarioId, StringComparison.Ordinal))
        {
            return [
                $"Selected authored scenario: {selected.Name} ({selected.ScenarioId})",
                $"Preview status: not materialized/stale. {context.PreviewInvalidationReason}",
                "Press P to materialize/refresh a turn-0 derived runtime preview. No auto-refresh occurs during redraw.",
                "Preview state will be runtime-derived facts, not authored source."
            ];
        }

        var session = preview.Session;
        var hasPlayerLocation = session.World.Entities.ContainsKey(session.PlayerEntityId)
                                && session.World.Occupancy.ContainsValue(session.PlayerEntityId);
        var playerLocation = hasPlayerLocation ? session.World.GetEntityLocation(session.PlayerEntityId).ToString() : "unavailable (materialization incomplete)";
        var playerName = session.World.Entities.TryGetValue(session.PlayerEntityId, out var player)
            ? player.Name
            : "unavailable";
        var activePlane = session.World.Planes.TryGetValue(session.ActivePlaneId, out var plane)
            ? $"{session.ActivePlaneId} ({plane.Width}x{plane.Height})"
            : session.ActivePlaneId.ToString();
        var rows = new List<string>
        {
            $"Runtime preview for authored scenario: {preview.Name} ({preview.ScenarioId})",
            $"Derived runtime state: {(preview.IsDerivedRuntimeState ? "yes - not authored source" : "no/unknown")} | Can play: {preview.CanPlay}",
            $"Turn: {session.World.TurnNumber} | Runtime entities: {session.World.Entities.Count} | Active plane: {activePlane}",
            $"Player runtime entity: {playerName} ({session.PlayerEntityId}) at {playerLocation}",
            $"Active container: {session.ActiveContainerEntityId}",
            FormatPreviewDiagnostics("Validation/materialization", preview.ValidationDiagnostics),
            FormatPreviewDiagnostics("Runtime failures", preview.RuntimeFailures),
            FormatPreviewDiagnostics("Capability gaps", preview.CapabilityGaps)
        };

        rows.Add("Entity location tree (derived runtime containment; names only; J jumps selected row to source template):");
        rows.AddRange(BuildEntityLocationTree(session, Math.Max(0, count - rows.Count), context.SelectedPreviewEntityIndex));

        if (session.World.Planes.ContainsKey(session.ActivePlaneId))
        {
            rows.AddRange(BuildGridSummary(session, session.ActivePlaneId, Math.Max(0, count - rows.Count)));
        }

        return rows.Take(count).ToList();
    }

    private static IEnumerable<string> BuildEntityLocationTree(PlayableScenarioSession session, int remainingRows, int selectedIndex)
    {
        if (remainingRows <= 0)
        {
            yield break;
        }

        var rootId = session.ActiveContainerEntityId;
        if (!session.World.Entities.ContainsKey(rootId))
        {
            yield return $"- missing root runtime entity {rootId}";
            yield break;
        }

        var emitted = 0;
        var index = 0;
        foreach (var row in BuildEntityLocationTreeRows(session, rootId, depth: 0, visited: []))
        {
            if (emitted >= remainingRows)
            {
                yield return "... tree truncated by preview panel height";
                yield break;
            }

            emitted++;
            yield return index++ == selectedIndex ? $"> {row}" : row;
        }
    }

    private static IEnumerable<string> BuildEntityLocationTreeRows(
        PlayableScenarioSession session,
        EntityId entityId,
        int depth,
        HashSet<EntityId> visited)
    {
        var indent = new string(' ', depth * 2);
        if (!session.World.Entities.TryGetValue(entityId, out var entity))
        {
            yield return $"{indent}- missing runtime entity {entityId}";
            yield break;
        }

        yield return $"{indent}- {entity.Name}";

        if (!visited.Add(entityId))
        {
            yield return $"{indent}  - cycle detected; nested contents omitted";
            yield break;
        }

        foreach (var childId in GetContainedEntityIds(session.World, entityId))
        {
            foreach (var row in BuildEntityLocationTreeRows(session, childId, depth + 1, visited))
            {
                yield return row;
            }
        }

        visited.Remove(entityId);
    }

    private static IReadOnlyList<EntityId> GetContainedEntityIds(WorldState world, EntityId ownerId)
    {
        var inventoryPlaneId = world.GetInventoryPlaneId(ownerId);
        if (inventoryPlaneId is null)
        {
            return [];
        }

        return world.Occupancy
            .Where(entry => world.Nodes.TryGetValue(entry.Key, out var node) && node.PlaneId == inventoryPlaneId)
            .Select(entry => (EntityId: entry.Value, Coord: world.Nodes[entry.Key].Coord))
            .OrderBy(entry => entry.Coord.Y)
            .ThenBy(entry => entry.Coord.X)
            .ThenBy(entry => entry.EntityId.Value, StringComparer.Ordinal)
            .Select(entry => entry.EntityId)
            .ToList();
    }

    private static string FormatPreviewDiagnostics(string label, IReadOnlyList<string> diagnostics) =>
        diagnostics.Count == 0 ? $"{label}: none reported." : $"{label}: {string.Join(" | ", diagnostics.Take(3))}{(diagnostics.Count > 3 ? " | ..." : string.Empty)}";

    private static IEnumerable<string> BuildGridSummary(PlayableScenarioSession session, PlaneId planeId, int remainingRows)
    {
        if (remainingRows <= 0 || !session.World.Planes.TryGetValue(planeId, out var plane))
        {
            yield break;
        }

        yield return $"Initial grid summary for {planeId}:";
        for (var y = 0; y < plane.Height && y < remainingRows - 1; y++)
        {
            var chars = new char[Math.Min(plane.Width, 40)];
            for (var x = 0; x < chars.Length; x++)
            {
                var coord = new PlaneCoord(planeId, new GridCoord(x, y));
                chars[x] = session.World.GetOccupant(coord) is { } occupant
                    ? session.Registry.GetPresentationForEntity(occupant).Glyph
                    : '.';
            }

            yield return new string(chars);
        }
    }

    private static string FormatDiagnostic(FrontendEditorDiagnostic diagnostic) =>
        $"{diagnostic.Severity}: {diagnostic.Code} [{DiagnosticObjectLabel(diagnostic)}] {diagnostic.Message}";

    private static string DiagnosticObjectLabel(FrontendEditorDiagnostic diagnostic)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(diagnostic.EntityTemplateId)) parts.Add($"template:{diagnostic.EntityTemplateId}");
        if (!string.IsNullOrWhiteSpace(diagnostic.ActionPlanId)) parts.Add($"plan:{diagnostic.ActionPlanId}");
        if (diagnostic.StepIndex is { } step) parts.Add($"step:{step}");
        if (!string.IsNullOrWhiteSpace(diagnostic.CarriedEntityId)) parts.Add($"carried:{diagnostic.CarriedEntityId}");
        if (diagnostic.Coord is { } coord) parts.Add($"coord:{coord}");
        return parts.Count == 0 ? "document" : string.Join(" ", parts);
    }

    private static string FormatCarried(FrontendEditorCarriedEntitySummary carried) =>
        $"{carried.EntityId}:{carried.TemplateId ?? "inline"}@{carried.Coord}";

    private static string FormatBrushOption(SadConsoleEditorTemplateBrushOption option) =>
        $"{option.Glyph} {option.Name} ({option.TemplateId}) color {option.Color}";

    private static bool HasUsableInventory(FrontendEditorEntityTemplateSummary template) =>
        template.InventoryWidth > 0 && template.InventoryHeight > 0;

    private static string FormatCarriedDetail(FrontendEditorCarriedEntitySummary carried)
    {
        var name = !string.IsNullOrWhiteSpace(carried.TemplateName)
            ? carried.TemplateName
            : carried.TemplateId ?? carried.EntityId;
        var presentation = carried.Glyph is { } glyph
            ? $" glyph '{glyph}' color {carried.Color?.ToString() ?? "unknown"}"
            : " glyph/color unavailable";
        return $"{name} [{carried.EntityId}] template:{carried.TemplateId ?? "inline/unknown"} at {carried.Coord}{presentation}";
    }

    private static string FormatTargetingRule(FrontendEditorTargetingRuleSummary rule)
    {
        var label = string.IsNullOrWhiteSpace(rule.Label) ? "unlabeled" : rule.Label;
        var hint = string.IsNullOrWhiteSpace(rule.Hint) ? "no hint" : rule.Hint;
        var target = !string.IsNullOrWhiteSpace(rule.TargetTemplateName)
            ? $"{rule.TargetTemplateName} ({rule.TargetTemplateId})"
            : rule.TargetTemplateId;
        return $"slot {rule.Slot} {label}; hint:{hint}; target:{target}; range:{rule.Range}";
    }

    private static int WindowFirst(int selected, int total, int count) =>
        Math.Max(0, Math.Min(selected - count / 2, Math.Max(0, total - count)));

    private static IReadOnlyList<string> SplitLines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
}
