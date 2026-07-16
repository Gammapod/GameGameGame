using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Navigation;
using GameGameGame.SadConsoleApp.Ui.Styling;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class EntityTemplateEditScreen
{
    private static readonly string[] PresentationFieldIds = ["name", "glyph", "color", "action-plan"];
    private static readonly string[] PresentationFieldLabels = ["name", "glyph", "color", "action plan"];
    private const string ClearActionPlanChoiceId = "__clear_action_plan__";
    private const string EditActionPlanChoiceId = "__edit_action_plan__";
    private const string ClearTargetCapabilitiesChoiceId = "__clear_target_capabilities__";
    private static readonly string[] InventoryMetadataFieldIds = ["inventory-width", "inventory-height", "aperture", "bulk"];
    private static readonly string[] InventoryMetadataFieldLabels = ["inventory width", "inventory height", "aperture", "bulk"];
    private const int InventoryGridEditFieldIndex = 4;

    private readonly FrontendEditorService? _service;
    private readonly Action<FrontendEditorSnapshot>? _snapshotMutated;
    private FrontendEditorEntityTemplateSummary _template;
    private readonly List<FrontendEditorEntityTemplateSummary> _entityTemplates;
    private readonly List<FrontendEditorActionPlanSummary> _actionPlans;
    private FocusRouter _focusRouter;
    private int _selectedPresentationFieldIndex;
    private int _selectedInventoryMetadataFieldIndex;
    private int _selectedTargetingSlotIndex;
    private bool _targetingSlotPanelOpen;
    private int _selectedTargetingDetailFieldIndex;
    private IUiComponent? _activeFieldOverlay;
    private string? _activePresentationFieldId;
    private string? _activeInventoryMetadataFieldId;
    private string? _activeTargetingFieldId;

    private EntityTemplateEditScreen(
        FrontendEditorEntityTemplateSummary template,
        IReadOnlyList<FrontendEditorEntityTemplateSummary> entityTemplates,
        IReadOnlyList<FrontendEditorActionPlanSummary> actionPlans,
        FrontendEditorService? service,
        Action<FrontendEditorSnapshot>? snapshotMutated)
    {
        _template = template;
        _entityTemplates = entityTemplates.ToList();
        _actionPlans = actionPlans.ToList();
        _service = service;
        _snapshotMutated = snapshotMutated;
        _focusRouter = BuildFocusRouter(template);
    }

    public string TemplateId => _template.TemplateId;
    public string Title => $"Edit Entity Template: {_template.Name}";
    public string Purpose => "Review authored presentation, targeting, and inventory information. Edits will be enabled field-by-field after layout review.";
    public string? FocusedComponentId => _focusRouter.FocusedComponentId;
    public string? SelectedComponentId => _focusRouter.SelectedComponentId;
    public int SelectedPresentationFieldIndex => _selectedPresentationFieldIndex;
    public int SelectedInventoryMetadataFieldIndex => _selectedInventoryMetadataFieldIndex;
    public int SelectedTargetingSlotIndex => _selectedTargetingSlotIndex;
    public int SelectedInventoryItemIndex => 0;
    public bool IsTextEntryOverlayActive => _activeFieldOverlay is TextEntryOverlayComponent;

    public InventoryGridEditScreen OpenInventoryGridEditScreen() =>
        InventoryGridEditScreen.FromSnapshot(
            new FrontendEditorSnapshot(
                string.Empty,
                false,
                [],
                _entityTemplates,
                _actionPlans,
                [],
                [],
                string.Empty,
                []),
            _template.TemplateId,
            _service,
            ReplaceAfterMutation);

    public static EntityTemplateEditScreen FromSnapshot(
        FrontendEditorSnapshot snapshot,
        string templateId,
        FrontendEditorService? service = null,
        Action<FrontendEditorSnapshot>? snapshotMutated = null)
    {
        var template = snapshot.EntityTemplates.First(template => template.TemplateId == templateId);
        return new EntityTemplateEditScreen(template, snapshot.EntityTemplates, snapshot.ActionPlans, service, snapshotMutated);
    }

    public IReadOnlyList<IUiComponent> Components() =>
    [
        PresentationFields(),
        TargetingPanel(),
        InventoryPanel()
    ];

    public string FooterText()
    {
        if (FocusedComponentId is null)
        {
            return "No component focused: arrows choose component. Enter focuses. Esc returns to Scenario Edit.";
        }

        if (_activeFieldOverlay is not null)
        {
            return "Field editor open: type/change value. Enter confirms through editor service. Esc cancels.";
        }

        return FocusedComponentId switch
        {
            "presentation" => "Presentation focused: Up/Down chooses field. Enter edits name/glyph/color or chooses action plan. Esc releases focus.",
            "targeting" => _targetingSlotPanelOpen
                ? "Targeting requirement detail focused: Up/Down chooses target template/adjectives/range. Enter edits. Esc closes 3.2.1 detail panel."
                : "Targeting focused: Up/Down chooses action-plan target label. Enter opens 3.2.1 details. Esc releases focus.",
            "inventory" => "Inventory focused: Up/Down chooses metadata field or grid editor. Enter edits/opens. Esc releases focus.",
            _ => "Esc releases focus."
        };
    }

    public EntityTemplateEditResult Handle(UiComponentCommand command)
    {
        if (_activeFieldOverlay is not null)
        {
            return HandleActiveFieldOverlay(command);
        }

        if (_targetingSlotPanelOpen)
        {
            if (command == UiComponentCommand.Cancel)
            {
                _targetingSlotPanelOpen = false;
                return EntityTemplateEditResult.Stay("Closed targeting slot detail panel.");
            }

            if (command is UiComponentCommand.Up or UiComponentCommand.Left)
            {
                _selectedTargetingDetailFieldIndex = Math.Clamp(_selectedTargetingDetailFieldIndex - 1, 0, 2);
                return EntityTemplateEditResult.Stay(TargetingDetailSelectionMessage());
            }

            if (command is UiComponentCommand.Down or UiComponentCommand.Right)
            {
                _selectedTargetingDetailFieldIndex = Math.Clamp(_selectedTargetingDetailFieldIndex + 1, 0, 2);
                return EntityTemplateEditResult.Stay(TargetingDetailSelectionMessage());
            }

            if (command == UiComponentCommand.Select)
            {
                return ActivateSelectedTargetingDetailField();
            }

            return EntityTemplateEditResult.Stay("Use Up/Down to choose target template/adjectives/range. Enter edits. Esc closes detail panel.");
        }

        if (FocusedComponentId is { } focused)
        {
            return HandleFocused(focused, command);
        }

        var result = _focusRouter.Handle(command);
        return result.Kind switch
        {
            FocusRouterResultKind.CancelScreen => EntityTemplateEditResult.ReturnToScenarioEdit("Returned to Scenario Edit."),
            FocusRouterResultKind.SelectedComponent => EntityTemplateEditResult.Stay($"Selected component: {result.ComponentId}."),
            FocusRouterResultKind.FocusedComponent => EntityTemplateEditResult.Stay($"Focused component: {result.ComponentId}."),
            _ => EntityTemplateEditResult.Stay("Use arrows to choose a component, Enter to focus, Esc to return.")
        };
    }

    private EntityTemplateEditResult HandleFocused(string focused, UiComponentCommand command)
    {
        if (command == UiComponentCommand.Cancel)
        {
            _focusRouter.Handle(UiComponentCommand.Cancel);
            return EntityTemplateEditResult.Stay($"Released focus from {focused}.");
        }

        if (command is UiComponentCommand.Up or UiComponentCommand.Left)
        {
            MoveFocusedSelection(focused, -1);
            return EntityTemplateEditResult.Stay(FocusedSelectionMessage(focused));
        }

        if (command is UiComponentCommand.Down or UiComponentCommand.Right)
        {
            MoveFocusedSelection(focused, 1);
            return EntityTemplateEditResult.Stay(FocusedSelectionMessage(focused));
        }

        if (command == UiComponentCommand.Select && focused == "presentation")
        {
            return ActivateSelectedPresentationField();
        }

        if (command == UiComponentCommand.Select && focused == "targeting")
        {
            if (_template.TargetingRequirements.Count == 0)
            {
                return EntityTemplateEditResult.Stay("Choose an Action Plan to define targeting labels before editing targeting rules.");
            }

            _targetingSlotPanelOpen = true;
            _selectedTargetingDetailFieldIndex = 0;
            return EntityTemplateEditResult.Stay($"Opened 3.2.1 details for target label {SelectedTargetingRequirement().Label}.");
        }

        if (command == UiComponentCommand.Select && focused == "inventory")
        {
            return _selectedInventoryMetadataFieldIndex == InventoryGridEditFieldIndex
                ? EntityTemplateEditResult.OpenInventoryGrid("Inventory grid editor next.")
                : ActivateSelectedInventoryMetadataField();
        }

        return EntityTemplateEditResult.Stay("This component is read-only in the first template-edit shell pass.");
    }

    private EntityTemplateEditResult JumpToActionPlan()
    {
        if (string.IsNullOrWhiteSpace(_template.DefaultActionPlanId))
        {
            return EntityTemplateEditResult.Stay("Template has no default action plan to jump to.");
        }

        var plan = _actionPlans.FirstOrDefault(plan => plan.ActionPlanId == _template.DefaultActionPlanId);
        return plan is null
            ? EntityTemplateEditResult.Stay($"Default action plan '{_template.DefaultActionPlanId}' was not found.")
            : EntityTemplateEditResult.OpenActionPlan(plan.ActionPlanId, $"Action Plan screen next: {plan.ActionPlanId}.");
    }

    private void MoveFocusedSelection(string focused, int delta)
    {
        if (focused == "presentation")
        {
            _selectedPresentationFieldIndex = Math.Clamp(_selectedPresentationFieldIndex + delta, 0, PresentationFieldIds.Length - 1);
        }
        else if (focused == "targeting" && _template.TargetingRequirements.Count > 0)
        {
            _selectedTargetingSlotIndex = Math.Clamp(_selectedTargetingSlotIndex + delta, 0, _template.TargetingRequirements.Count - 1);
        }
        else if (focused == "inventory" && _template.CarriedEntities.Count > 0)
        {
            _selectedInventoryMetadataFieldIndex = Math.Clamp(_selectedInventoryMetadataFieldIndex + delta, 0, InventoryGridEditFieldIndex);
        }
        else if (focused == "inventory")
        {
            _selectedInventoryMetadataFieldIndex = Math.Clamp(_selectedInventoryMetadataFieldIndex + delta, 0, InventoryGridEditFieldIndex);
        }
    }

    private string FocusedSelectionMessage(string focused) => focused switch
    {
        "presentation" => $"Selected presentation field: {PresentationFieldLabels[_selectedPresentationFieldIndex]}.",
        "targeting" => _template.TargetingRequirements.Count == 0 ? "No action-plan targeting labels defined." : $"Selected target label: {SelectedTargetingRequirement().Label}.",
        "inventory" => _selectedInventoryMetadataFieldIndex == InventoryGridEditFieldIndex
            ? "Selected inventory grid editor."
            : $"Selected inventory metadata field: {InventoryMetadataFieldLabels[_selectedInventoryMetadataFieldIndex]}.",
        _ => "Selection unchanged."
    };

    private FieldGroupComponent PresentationFields()
    {
        var focused = FocusedComponentId == "presentation";
        return new FieldGroupComponent(
            "presentation",
            "3.1 Presentation information",
            new SadConsoleRect(1, 4, 44, 16),
            [
                PresentationField(0, "name", "name", _template.Name, EditableFieldMode.Editable, focused),
                PresentationField(1, "glyph", "glyph", _template.Glyph.ToString(), EditableFieldMode.Editable, focused),
                PresentationField(2, "color", "color", _template.Color.ToString(), EditableFieldMode.Editable, focused),
                PresentationField(3, "action-plan", "action plan", _template.DefaultActionPlanId ?? "(none)", EditableFieldMode.Editable, focused)
            ],
            _focusRouter.StateFor("presentation"));
    }

    private EditableFieldComponent PresentationField(int index, string id, string label, string value, EditableFieldMode mode, bool focused)
    {
        var isSelected = focused && index == _selectedPresentationFieldIndex;
        var editMode = _activePresentationFieldId == id ? EditableFieldMode.Editing : mode;
        return new EditableFieldComponent(id, isSelected ? $"> {label}" : label, value, editMode);
    }

    public EntityTemplateEditResult InsertText(string text)
    {
        if (_activeFieldOverlay is not TextEntryOverlayComponent textEntry)
        {
            return EntityTemplateEditResult.Stay("No text field editor is open.");
        }

        textEntry.InsertText(text);
        return EntityTemplateEditResult.Stay("Typing text field value.");
    }

    public EntityTemplateEditResult Backspace()
    {
        if (_activeFieldOverlay is not TextEntryOverlayComponent textEntry)
        {
            return EntityTemplateEditResult.Stay("No text field editor is open.");
        }

        textEntry.Backspace();
        return EntityTemplateEditResult.Stay("Typing text field value.");
    }

    private EntityTemplateEditResult ActivateSelectedPresentationField()
    {
        var fieldId = PresentationFieldIds[_selectedPresentationFieldIndex];
        _activePresentationFieldId = fieldId;
        _activeFieldOverlay = fieldId switch
        {
            "name" => new TextEntryOverlayComponent("presentation-name-editor", "Edit presentation name", "name", _template.Name, SadConsoleRect.FromSize(34, 8, 52, 7), maxLength: 80, allowEmpty: false),
            "glyph" => new TextEntryOverlayComponent("presentation-glyph-editor", "Edit presentation glyph", "glyph", _template.Glyph.ToString(), SadConsoleRect.FromSize(34, 8, 52, 7), maxLength: 1, allowEmpty: false),
            "color" => new ChoicePickerOverlayComponent("presentation-color-editor", "Edit presentation color", "color", PresentationColorChoices(), SadConsoleRect.FromSize(34, 8, 52, 12), SelectedColorIndex()),
            "action-plan" => new ChoicePickerOverlayComponent("presentation-action-plan-editor", "Choose default action plan", "action plan", ActionPlanChoices(), SadConsoleRect.FromSize(34, 8, 62, 14), SelectedActionPlanChoiceIndex()),
            _ => null
        };

        return EntityTemplateEditResult.Stay($"Opened editor for presentation {PresentationFieldLabels[_selectedPresentationFieldIndex]}.");
    }

    private EntityTemplateEditResult HandleActiveFieldOverlay(UiComponentCommand command)
    {
        if (_activeFieldOverlay is TextEntryOverlayComponent textEntry)
        {
            var result = textEntry.Handle(command);
            if (result.Kind == FieldEditorOverlayResultKind.Confirmed)
            {
                return ConfirmPresentationEdit(result.Value);
            }

            if (result.Kind == FieldEditorOverlayResultKind.Cancelled)
            {
                ClearFieldOverlay();
            }

            return EntityTemplateEditResult.Stay(result.Message);
        }

        if (_activeFieldOverlay is ChoicePickerOverlayComponent picker)
        {
            var result = picker.Handle(command);
            if (result.Kind == FieldEditorOverlayResultKind.Confirmed && result.Value is { } choice)
            {
                return _activeTargetingFieldId == "target-template"
                    ? ConfirmTargetingTemplateEdit(choice.Id)
                    : _activeTargetingFieldId == "target-adjectives"
                        ? ConfirmTargetingAdjectiveToggle(choice.Id)
                    : _activePresentationFieldId == "action-plan"
                        ? ConfirmActionPlanChoice(choice.Id)
                    : ConfirmPresentationEdit(choice.Id);
            }

            if (result.Kind == FieldEditorOverlayResultKind.Cancelled)
            {
                ClearFieldOverlay();
            }

            return EntityTemplateEditResult.Stay(result.Message);
        }

        if (_activeFieldOverlay is IntSetterOverlayComponent intSetter)
        {
            var result = intSetter.Handle(command);
            if (result.Kind == FieldEditorOverlayResultKind.Confirmed)
            {
                return _activeTargetingFieldId == "target-range"
                    ? ConfirmTargetingRangeEdit(result.Value)
                    : ConfirmInventoryMetadataEdit(result.Value);
            }

            if (result.Kind == FieldEditorOverlayResultKind.Cancelled)
            {
                ClearFieldOverlay();
            }

            return EntityTemplateEditResult.Stay(result.Message);
        }

        return EntityTemplateEditResult.Stay("Field editor is not available.");
    }

    private EntityTemplateEditResult ActivateSelectedInventoryMetadataField()
    {
        var fieldId = InventoryMetadataFieldIds[_selectedInventoryMetadataFieldIndex];
        _activeInventoryMetadataFieldId = fieldId;
        _activeFieldOverlay = new IntSetterOverlayComponent(
            $"{fieldId}-editor",
            $"Edit {InventoryMetadataFieldLabels[_selectedInventoryMetadataFieldIndex]}",
            InventoryMetadataFieldLabels[_selectedInventoryMetadataFieldIndex],
            InventoryMetadataValue(fieldId),
            min: 0,
            max: 99,
            step: 1,
            bounds: SadConsoleRect.FromSize(34, 20, 52, 7));

        return EntityTemplateEditResult.Stay($"Opened editor for inventory {InventoryMetadataFieldLabels[_selectedInventoryMetadataFieldIndex]}.");
    }

    private EntityTemplateEditResult ConfirmInventoryMetadataEdit(int value)
    {
        if (_activeInventoryMetadataFieldId is null)
        {
            ClearFieldOverlay();
            return EntityTemplateEditResult.Stay("No inventory metadata field is active.");
        }

        if (_service is null)
        {
            ClearFieldOverlay();
            return EntityTemplateEditResult.Stay("Inventory metadata edits require a service-backed editor screen.");
        }

        var update = _activeInventoryMetadataFieldId switch
        {
            "inventory-width" => new FrontendEditorTemplateMetadataUpdate(value, _template.InventoryHeight, _template.Bulk, _template.Aperture),
            "inventory-height" => new FrontendEditorTemplateMetadataUpdate(_template.InventoryWidth, value, _template.Bulk, _template.Aperture),
            "bulk" => new FrontendEditorTemplateMetadataUpdate(_template.InventoryWidth, _template.InventoryHeight, value, _template.Aperture),
            "aperture" => new FrontendEditorTemplateMetadataUpdate(_template.InventoryWidth, _template.InventoryHeight, _template.Bulk, value),
            _ => null
        };

        if (update is null)
        {
            ClearFieldOverlay();
            return EntityTemplateEditResult.Stay("Unsupported inventory metadata field edit.");
        }

        var result = _service.UpdateTemplateMetadata(_template.TemplateId, update);
        ReplaceAfterMutation(result.Snapshot);
        ClearFieldOverlay();
        return EntityTemplateEditResult.Stay(result.StatusMessage);
    }

    private int InventoryMetadataValue(string fieldId) => fieldId switch
    {
        "inventory-width" => _template.InventoryWidth,
        "inventory-height" => _template.InventoryHeight,
        "aperture" => _template.Aperture,
        "bulk" => _template.Bulk,
        _ => 0
    };

    private EntityTemplateEditResult ActivateSelectedTargetingDetailField()
    {
        var requirement = SelectedTargetingRequirement();
        if (_selectedTargetingDetailFieldIndex == 0)
        {
            if (_entityTemplates.Count == 0)
            {
                return EntityTemplateEditResult.Stay("No entity templates are available as targeting choices.");
            }

            _activeTargetingFieldId = "target-template";
            _activeFieldOverlay = new ChoicePickerOverlayComponent(
                "target-template-editor",
                $"Choose target template for {requirement.Label}",
                "target template",
                TargetTemplateChoices(requirement),
                SadConsoleRect.FromSize(42, 10, 58, 14),
                SelectedTargetTemplateIndex(requirement));
            return EntityTemplateEditResult.Stay($"Opened target-template picker for {requirement.Label}.");
        }

        if (_selectedTargetingDetailFieldIndex == 1)
        {
            var choices = TargetCapabilityChoices(requirement).ToList();
            if (choices.Count == 0)
            {
                return EntityTemplateEditResult.Stay($"Target label {requirement.Label} has no action-step adjectives available from the current action plan.");
            }

            _activeTargetingFieldId = "target-adjectives";
            _activeFieldOverlay = new ChoicePickerOverlayComponent(
                "target-adjectives-editor",
                $"Toggle target adjectives for {requirement.Label}",
                "target adjectives",
                choices,
                SadConsoleRect.FromSize(42, 10, 62, 14));
            return EntityTemplateEditResult.Stay($"Opened target-adjectives picker for {requirement.Label}.");
        }

        _activeTargetingFieldId = "target-range";
        _activeFieldOverlay = new IntSetterOverlayComponent(
            "target-range-editor",
            $"Set target range for {requirement.Label}",
            "target range",
            requirement.Rule?.Range ?? 0,
            min: 0,
            max: 10,
            step: 1,
            SadConsoleRect.FromSize(42, 10, 58, 7));
        return EntityTemplateEditResult.Stay($"Opened target-range editor for {requirement.Label}.");
    }

    private EntityTemplateEditResult ConfirmTargetingTemplateEdit(string targetTemplateChoiceId)
    {
        var requirement = SelectedTargetingRequirement();
        var targetTemplateId = targetTemplateChoiceId == NullTargetTemplateChoiceId ? null : targetTemplateChoiceId;
        return ConfirmTargetingRuleEdit(targetTemplateId, requirement.Rule?.Range ?? 0);
    }

    private EntityTemplateEditResult ConfirmTargetingRangeEdit(int range)
    {
        var requirement = SelectedTargetingRequirement();
        var targetTemplateId = requirement.Rule?.TargetTemplateId
            ?? (requirement.Rule?.TargetCapabilities.Count > 0 ? null : _entityTemplates.FirstOrDefault()?.TemplateId);
        if (string.IsNullOrWhiteSpace(targetTemplateId) && requirement.Rule?.TargetCapabilities.Count is not > 0)
        {
            ClearFieldOverlay();
            return EntityTemplateEditResult.Stay("Choose a target template or at least one adjective before setting target range.");
        }

        return ConfirmTargetingRuleEdit(targetTemplateId, range);
    }

    private EntityTemplateEditResult ConfirmTargetingAdjectiveToggle(string choiceId)
    {
        var requirement = SelectedTargetingRequirement();
        var current = requirement.Rule?.TargetCapabilities.ToList() ?? [];
        List<ActionPlanBehaviorStepKind> next;
        if (choiceId == ClearTargetCapabilitiesChoiceId)
        {
            next = [];
        }
        else if (!Enum.TryParse<ActionPlanBehaviorStepKind>(choiceId, out var capability))
        {
            ClearFieldOverlay();
            return EntityTemplateEditResult.Stay($"Unknown target adjective {choiceId}.");
        }
        else if (current.Contains(capability))
        {
            next = current.Where(item => item != capability).ToList();
        }
        else
        {
            next = [.. current, capability];
        }

        var targetTemplateId = requirement.Rule?.TargetTemplateId;
        if (string.IsNullOrWhiteSpace(targetTemplateId) && next.Count == 0)
        {
            ClearFieldOverlay();
            return EntityTemplateEditResult.Stay("Choose a target template before clearing the last target adjective.");
        }

        return ConfirmTargetingRuleEdit(targetTemplateId, requirement.Rule?.Range ?? 0, next);
    }

    private EntityTemplateEditResult ConfirmTargetingRuleEdit(string? targetTemplateId, int range, IReadOnlyList<ActionPlanBehaviorStepKind>? targetCapabilities = null)
    {
        if (_service is null)
        {
            ClearFieldOverlay();
            return EntityTemplateEditResult.Stay("Targeting edits require a service-backed editor screen.");
        }

        var requirement = SelectedTargetingRequirement();
        var slot = requirement.Rule?.Slot ?? _selectedTargetingSlotIndex + 1;
        var result = _service.SetTemplateTargetingRule(
            _template.TemplateId,
            new FrontendEditorTargetingRuleUpdate(slot, requirement.Label, targetTemplateId, range, targetCapabilities ?? requirement.Rule?.TargetCapabilities));
        ReplaceAfterMutation(result.Snapshot);
        _selectedTargetingSlotIndex = Math.Clamp(_selectedTargetingSlotIndex, 0, Math.Max(0, _template.TargetingRequirements.Count - 1));
        _targetingSlotPanelOpen = true;
        ClearFieldOverlay();
        return EntityTemplateEditResult.Stay(result.StatusMessage);
    }

    private int SelectedTargetTemplateIndex(FrontendEditorTargetingRequirementSummary requirement)
    {
        if (requirement.Rule is null) return 0;
        if (string.IsNullOrWhiteSpace(requirement.Rule.TargetTemplateId)) return 0;
        var offset = AllowsNullTargetTemplate(requirement) ? 1 : 0;
        var index = _entityTemplates.FindIndex(template => template.TemplateId == requirement.Rule.TargetTemplateId);
        return index < 0 ? 0 : index + offset;
    }

    private string TargetingDetailSelectionMessage() => _selectedTargetingDetailFieldIndex == 0
        ? $"Selected target template for {SelectedTargetingRequirement().Label}."
        : _selectedTargetingDetailFieldIndex == 1
            ? $"Selected target adjectives for {SelectedTargetingRequirement().Label}."
            : $"Selected target range for {SelectedTargetingRequirement().Label}.";

    private EntityTemplateEditResult ConfirmPresentationEdit(string value)
    {
        if (_activePresentationFieldId is null)
        {
            ClearFieldOverlay();
            return EntityTemplateEditResult.Stay("No presentation field is active.");
        }

        if (_service is null)
        {
            ClearFieldOverlay();
            return EntityTemplateEditResult.Stay("Presentation edits require a service-backed editor screen.");
        }

        var result = _activePresentationFieldId switch
        {
            "name" => _service.UpdateTemplatePresentation(_template.TemplateId, new FrontendEditorTemplatePresentationUpdate(value, _template.Glyph.ToString(), _template.Color)),
            "glyph" => _service.UpdateTemplatePresentation(_template.TemplateId, new FrontendEditorTemplatePresentationUpdate(_template.Name, value, _template.Color)),
            "color" when Enum.TryParse<PresentationColor>(value, out var color) => _service.UpdateTemplatePresentation(_template.TemplateId, new FrontendEditorTemplatePresentationUpdate(_template.Name, _template.Glyph.ToString(), color)),
            _ => FrontendEditorMutationResult.Failure("Unsupported presentation field edit.", _service.GetSnapshot())
        };

        ReplaceAfterMutation(result.Snapshot);
        ClearFieldOverlay();
        return EntityTemplateEditResult.Stay(result.StatusMessage);
    }

    private void ReplaceAfterMutation(FrontendEditorSnapshot snapshot)
    {
        _template = snapshot.EntityTemplates.First(template => template.TemplateId == _template.TemplateId);
        _entityTemplates.Clear();
        _entityTemplates.AddRange(snapshot.EntityTemplates);
        _actionPlans.Clear();
        _actionPlans.AddRange(snapshot.ActionPlans);
        _focusRouter = BuildFocusRouter(_template);
        _snapshotMutated?.Invoke(snapshot);
    }

    private void ClearFieldOverlay()
    {
        _activeFieldOverlay = null;
        _activePresentationFieldId = null;
        _activeInventoryMetadataFieldId = null;
        _activeTargetingFieldId = null;
    }

    private IEnumerable<SelectableListItem> PresentationColorChoices() =>
        Enum.GetValues<PresentationColor>()
            .Select(color => new SelectableListItem(color.ToString(), color.ToString(), SampleColorToken: color.ToString()));

    private int SelectedColorIndex() => Array.IndexOf(Enum.GetValues<PresentationColor>(), _template.Color);

    private IEnumerable<SelectableListItem> ActionPlanChoices()
    {
        yield return new SelectableListItem(ClearActionPlanChoiceId, "(none)", "clear default action plan");

        foreach (var plan in _actionPlans)
        {
            yield return new SelectableListItem(plan.ActionPlanId, plan.ActionPlanId, plan.Shape);
        }

        yield return new SelectableListItem(EditActionPlanChoiceId, "Edit current action plan", _template.DefaultActionPlanId ?? "no current action plan");
    }

    private int SelectedActionPlanChoiceIndex()
    {
        if (string.IsNullOrWhiteSpace(_template.DefaultActionPlanId)) return 0;

        var actionPlanIndex = _actionPlans.FindIndex(plan => plan.ActionPlanId == _template.DefaultActionPlanId);
        return actionPlanIndex < 0 ? 0 : actionPlanIndex + 1;
    }

    private EntityTemplateEditResult ConfirmActionPlanChoice(string choiceId)
    {
        if (choiceId == EditActionPlanChoiceId)
        {
            ClearFieldOverlay();
            return JumpToActionPlan();
        }

        if (_service is null)
        {
            ClearFieldOverlay();
            return EntityTemplateEditResult.Stay("Action plan selection requires a service-backed editor screen.");
        }

        var result = choiceId == ClearActionPlanChoiceId
            ? _service.ClearTemplateDefaultActionPlan(_template.TemplateId)
            : _service.SetTemplateDefaultActionPlan(_template.TemplateId, choiceId);

        ReplaceAfterMutation(result.Snapshot);
        ClearFieldOverlay();
        return EntityTemplateEditResult.Stay(result.StatusMessage);
    }

    private PanelComponent TargetingPanel()
    {
        var rows = new List<string>();
        if (_template.TargetingRequirements.Count == 0)
        {
            rows.Add(string.IsNullOrWhiteSpace(_template.DefaultActionPlanId)
                ? "Choose an Action Plan to define targeting labels."
                : "Selected Action Plan has no target-label requirements.");
        }
        else
        {
            rows.AddRange(_template.TargetingRequirements.Select((requirement, index) =>
                $"{(index == _selectedTargetingSlotIndex ? ">" : " ")} {requirement.Label}: {FormatTargetingRequirementSummary(requirement)}"));
        }

        if (_template.OrphanedTargetingRules.Count > 0)
        {
            rows.Add("unused authored targeting rules:");
            rows.AddRange(_template.OrphanedTargetingRules.Select(rule =>
                $"  {rule.Label ?? $"slot {DisplaySlotNumber(rule)}"}: {FormatTargetingRuleCriteria(rule)} range {rule.Range}"));
        }

        return new PanelComponent(
            "targeting",
            "3.2 Targeting information",
            new SadConsoleRect(48, 4, 69, 16),
            rows,
            _focusRouter.StateFor("targeting"));
    }

    public IUiComponent? OverlayComponent() => _activeFieldOverlay ?? (_targetingSlotPanelOpen ? TargetingSlotDetailPanel() : null);

    private FieldGroupComponent TargetingSlotDetailPanel()
    {
        var requirement = SelectedTargetingRequirement();
        var rule = requirement.Rule;
        return new FieldGroupComponent(
            "targeting-slot-detail",
            $"3.2.1 Target label {requirement.Label}",
            SadConsoleRect.FromSize(60, 8, 45, 11),
            [
                new EditableFieldComponent("target-label", "target label", requirement.Label, EditableFieldMode.ReadOnly),
                TargetingDetailField(0, "target-template", "target template", FormatTargetTemplate(rule), EditableFieldMode.Editable),
                TargetingDetailField(1, "target-adjectives", "target adjectives", FormatTargetCapabilities(rule), EditableFieldMode.Editable),
                TargetingDetailField(2, "target-range", "target range", rule?.Range.ToString() ?? "0", EditableFieldMode.Editable)
            ],
            UiComponentState.Focused);
    }

    private EditableFieldComponent TargetingDetailField(int index, string id, string label, string value, EditableFieldMode mode)
    {
        var selected = index == _selectedTargetingDetailFieldIndex;
        var editMode = _activeTargetingFieldId == id ? EditableFieldMode.Editing : mode;
        return new EditableFieldComponent(id, selected ? $"> {label}" : label, value, editMode);
    }

    private FrontendEditorTargetingRequirementSummary SelectedTargetingRequirement() =>
        _template.TargetingRequirements[Math.Clamp(_selectedTargetingSlotIndex, 0, _template.TargetingRequirements.Count - 1)];

    private static string FormatTargetingRequirementSummary(FrontendEditorTargetingRequirementSummary requirement)
    {
        if (requirement.Rule is not { } rule)
        {
            return "(unset) range 0";
        }

        return $"{FormatTargetingRuleCriteria(rule)} range {rule.Range}";
    }

    private const string NullTargetTemplateChoiceId = "__no_target_template__";

    private static FocusRouter BuildFocusRouter(FrontendEditorEntityTemplateSummary template) => new([
        new FocusTarget("presentation"),
        new FocusTarget("targeting", template.TargetingRequirements.Count > 0),
        new FocusTarget("inventory")
    ]);

    private IEnumerable<SelectableListItem> TargetTemplateChoices(FrontendEditorTargetingRequirementSummary requirement)
    {
        if (AllowsNullTargetTemplate(requirement))
        {
            yield return new SelectableListItem(NullTargetTemplateChoiceId, "(none / adjective-only)", "keep only target-capability adjectives");
        }

        foreach (var template in _entityTemplates)
        {
            yield return new SelectableListItem(template.TemplateId, template.Name, template.TemplateId);
        }
    }

    private IEnumerable<SelectableListItem> TargetCapabilityChoices(FrontendEditorTargetingRequirementSummary requirement)
    {
        var current = requirement.Rule?.TargetCapabilities ?? [];
        foreach (var capability in requirement.StepKinds.Where(IsSupportedTargetCapability).Distinct())
        {
            var enabled = current.Contains(capability);
            yield return new SelectableListItem(capability.ToString(), enabled ? $"[x] {capability}" : $"[ ] {capability}", enabled ? "currently selected" : "available from current action plan");
        }

        if (current.Count > 0)
        {
            yield return new SelectableListItem(ClearTargetCapabilitiesChoiceId, "(none)", "clear all target adjectives");
        }
    }

    private static bool IsSupportedTargetCapability(ActionPlanBehaviorStepKind kind) => kind is
        ActionPlanBehaviorStepKind.PickupTarget or
        ActionPlanBehaviorStepKind.TransformAdjacentToInventory or
        ActionPlanBehaviorStepKind.EnterTarget or
        ActionPlanBehaviorStepKind.GiveTarget or
        ActionPlanBehaviorStepKind.TakeTarget or
        ActionPlanBehaviorStepKind.DestroyTarget or
        ActionPlanBehaviorStepKind.PushFacing;

    private static bool AllowsNullTargetTemplate(FrontendEditorTargetingRequirementSummary requirement) =>
        requirement.Rule?.TargetCapabilities.Count > 0;

    private static string FormatTargetTemplate(FrontendEditorTargetingRuleSummary? rule) =>
        rule is null ? "(unset)" : rule.TargetTemplateName ?? rule.TargetTemplateId ?? "(none)";

    private static string FormatTargetCapabilities(FrontendEditorTargetingRuleSummary? rule) =>
        rule is null || rule.TargetCapabilities.Count == 0 ? "(none)" : string.Join(", ", rule.TargetCapabilities);

    private static string FormatTargetingRuleCriteria(FrontendEditorTargetingRuleSummary rule)
    {
        var target = rule.TargetTemplateName ?? rule.TargetTemplateId ?? "any entity";
        var capabilities = rule.TargetCapabilities.Count == 0 ? string.Empty : $" [{string.Join(", ", rule.TargetCapabilities)}]";
        return $"{target}{capabilities}";
    }

    private static int DisplaySlotNumber(FrontendEditorTargetingRuleSummary slot) => slot.Slot + 1;

    private IUiComponent InventoryPanel()
    {
        var rows = new List<string>
        {
            InventoryMetadataRow(0, $"inventory width: {_template.InventoryWidth}"),
            InventoryMetadataRow(1, $"inventory height: {_template.InventoryHeight}"),
            InventoryMetadataRow(2, $"aperture: {_template.Aperture}"),
            InventoryMetadataRow(3, $"bulk: {_template.Bulk}"),
            InventoryMetadataRow(InventoryGridEditFieldIndex, "3.3.2 inventory grid editor")
        };

        return new InventorySummaryComponent(
            "inventory",
            "3.3 Inventory information",
            new SadConsoleRect(1, 18, 116, 36),
            rows,
            _focusRouter.StateFor("inventory"),
            _template.InventoryWidth,
            _template.InventoryHeight,
            _template.CarriedEntities.Select(item => new InventoryGridCell(item.Coord, item.Glyph ?? '?', item.Color)).ToList());
    }

    private string InventoryMetadataRow(int index, string text)
    {
        var marker = FocusedComponentId == "inventory" && index == _selectedInventoryMetadataFieldIndex ? ">" : " ";
        return $"{marker} {text}";
    }
}

internal sealed class InventorySummaryComponent : IUiComponent
{
    public InventorySummaryComponent(
        string id,
        string title,
        SadConsoleRect bounds,
        IReadOnlyList<string> rows,
        UiComponentState state,
        int gridWidth,
        int gridHeight,
        IReadOnlyList<InventoryGridCell> cells)
    {
        Id = id;
        Title = title;
        Bounds = bounds;
        Rows = rows;
        State = state;
        GridWidth = gridWidth;
        GridHeight = gridHeight;
        Cells = cells;
    }

    public string Id { get; }
    public string Title { get; }
    public SadConsoleRect Bounds { get; }
    public UiComponentState State { get; }
    public IReadOnlyList<string> Rows { get; }
    public int GridWidth { get; }
    public int GridHeight { get; }
    public IReadOnlyList<InventoryGridCell> Cells { get; }

    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme)
    {
        var rows = new List<string> { $"[{State.BorderColor(theme)}] {Title}" };
        rows.AddRange(Rows);
        rows.Add("inventory grid preview rendered by SadConsole renderer");
        return rows;
    }
}

internal sealed record EntityTemplateEditResult(EntityTemplateEditResultKind Kind, string Message, string? ActionPlanId = null)
{
    public static EntityTemplateEditResult Stay(string message) => new(EntityTemplateEditResultKind.Stay, message);
    public static EntityTemplateEditResult ReturnToScenarioEdit(string message) => new(EntityTemplateEditResultKind.ReturnToScenarioEdit, message);
    public static EntityTemplateEditResult OpenActionPlan(string actionPlanId, string message) => new(EntityTemplateEditResultKind.OpenActionPlan, message, actionPlanId);
    public static EntityTemplateEditResult OpenInventoryGrid(string message) => new(EntityTemplateEditResultKind.OpenInventoryGrid, message);
}

internal enum EntityTemplateEditResultKind
{
    Stay,
    ReturnToScenarioEdit,
    OpenActionPlan,
    OpenInventoryGrid
}
