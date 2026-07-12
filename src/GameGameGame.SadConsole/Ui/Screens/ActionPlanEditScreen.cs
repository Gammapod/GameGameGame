using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Navigation;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class ActionPlanEditScreen
{
    private readonly FrontendEditorService? _service;
    private readonly Action<FrontendEditorSnapshot>? _snapshotMutated;
    private readonly FocusRouter _focusRouter;
    private FrontendEditorActionPlanSummary _actionPlan;
    private readonly List<FrontendEditorAvailableActionStepSummary> _availableSteps;
    private int _selectedStepIndex;
    private int _selectedStepDetailFieldIndex;
    private IUiComponent? _overlay;
    private ActionPlanStepPickerMode? _pickerMode;
    private bool _insertBelow;
    private bool _moveMode;
    private bool _stepDetailOpen;

    private ActionPlanEditScreen(
        FrontendEditorActionPlanSummary actionPlan,
        IReadOnlyList<FrontendEditorAvailableActionStepSummary> availableSteps,
        ActionPlanEditReturnDestination returnDestination,
        FrontendEditorService? service,
        Action<FrontendEditorSnapshot>? snapshotMutated)
    {
        _actionPlan = actionPlan;
        _availableSteps = availableSteps.ToList();
        ReturnDestination = returnDestination;
        _service = service;
        _snapshotMutated = snapshotMutated;
        _focusRouter = new FocusRouter([new FocusTarget("action-plan-steps")], focusFirstEnabled: true);
    }

    public string ActionPlanId => _actionPlan.ActionPlanId;
    public ActionPlanEditReturnDestination ReturnDestination { get; }
    public string Title => $"Edit Action Plan: {_actionPlan.ActionPlanId}";
    public string Purpose => "Edit authored canonical behavior steps. Step parameters remain a later slice.";
    public string? FocusedComponentId => _focusRouter.FocusedComponentId;
    public int SelectedStepIndex => _selectedStepIndex;
    public int SelectedStepFieldIndex => _selectedStepDetailFieldIndex;
    public bool IsMoveMode => _moveMode;
    public bool IsTextEntryOverlayActive => _overlay is TextEntryOverlayComponent;

    public static ActionPlanEditScreen FromSnapshot(
        FrontendEditorSnapshot snapshot,
        string actionPlanId,
        ActionPlanEditReturnDestination returnDestination,
        FrontendEditorService? service = null,
        Action<FrontendEditorSnapshot>? snapshotMutated = null)
    {
        var actionPlan = snapshot.ActionPlans.First(plan => plan.ActionPlanId == actionPlanId);
        return new ActionPlanEditScreen(actionPlan, snapshot.AvailableActionSteps, returnDestination, service, snapshotMutated);
    }

    public IReadOnlyList<IUiComponent> Components() => [ActionStepList(), HighlightedStepPanel()];

    public IUiComponent? OverlayComponent() => _overlay ?? StepDetailComponent();

    public string FooterText()
    {
        if (_overlay is not null)
        {
            return _overlay.Id == "action-step-insert-position"
                ? "Insert position: Up/Down chooses above/below. Enter confirms. Esc cancels."
                : _overlay.Id == "action-step-label-picker"
                    ? "Target label entry: type lowercase letters/digits or leave blank. Enter confirms. Esc cancels."
                        : "Action primitive picker: Up/Down chooses. Enter confirms. Esc cancels.";
        }

        if (_moveMode)
        {
            return "Move mode: Up/Down swaps selected step. Enter/Space confirms. Esc cancels move mode.";
        }

        if (FocusedComponentId is null)
        {
            return ReturnDestination == ActionPlanEditReturnDestination.EntityTemplateEdit
                ? "No component focused: Enter focuses steps. Esc returns to Entity Template Edit."
                : "No component focused: Enter focuses steps. Esc returns to Scenario Edit.";
        }

        if (_stepDetailOpen)
        {
            return "4.1.2 step detail: Up/Down chooses Action Step or Label. Enter edits field. Esc closes popup.";
        }

        return "Action steps: Up/Down chooses step. Enter opens 4.1.2 details. I inserts. Delete removes. Space moves. Esc releases focus.";
    }

    public ActionPlanEditResult Handle(UiComponentCommand command)
    {
        if (_overlay is ChoicePickerOverlayComponent picker)
        {
            return HandleChoiceOverlay(picker, command);
        }

        if (_overlay is TextEntryOverlayComponent textEntry)
        {
            return HandleTextEntryOverlay(textEntry, command);
        }

        if (_moveMode)
        {
            return HandleMoveMode(command);
        }

        if (_stepDetailOpen)
        {
            return HandleStepDetail(command);
        }

        if (FocusedComponentId is { } focused)
        {
            return HandleFocused(focused, command);
        }

        var focusResult = _focusRouter.Handle(command);
        return focusResult.Kind switch
        {
            FocusRouterResultKind.CancelScreen => ActionPlanEditResult.Return(ReturnDestination, ReturnMessage()),
            FocusRouterResultKind.FocusedComponent => ActionPlanEditResult.Stay("Focused action steps."),
            _ => ActionPlanEditResult.Stay("Use Enter to focus action steps, Esc to return.")
        };
    }

    public ActionPlanEditResult Handle(ActionPlanEditCommand command) => command switch
    {
        ActionPlanEditCommand.Insert => OpenInsertPositionPicker(),
        ActionPlanEditCommand.Delete => DeleteSelectedStep(),
        ActionPlanEditCommand.ToggleMoveMode => ToggleMoveMode(),
        _ => ActionPlanEditResult.Stay("Use action-plan edit controls.")
    };

    private ActionPlanEditResult HandleChoiceOverlay(ChoicePickerOverlayComponent picker, UiComponentCommand command)
    {
        var result = picker.Handle(command);
        if (result.Kind == FieldEditorOverlayResultKind.Cancelled)
        {
            ClearOverlay();
            return ActionPlanEditResult.Stay(result.Message);
        }

        if (result.Kind != FieldEditorOverlayResultKind.Confirmed || result.Value is not { } choice)
        {
            return ActionPlanEditResult.Stay(result.Message);
        }

        if (picker.Id == "action-step-insert-position")
        {
            _insertBelow = choice.Id == "below";
            return OpenStepPrimitivePicker(ActionPlanStepPickerMode.Insert);
        }

        return _pickerMode switch
        {
            ActionPlanStepPickerMode.Replace => ReplaceSelectedStep(choice.Id),
            ActionPlanStepPickerMode.Insert => InsertSelectedStep(choice.Id),
            _ => ActionPlanEditResult.Stay("No action-step edit mode is active.")
        };
    }

    private ActionPlanEditResult HandleTextEntryOverlay(TextEntryOverlayComponent textEntry, UiComponentCommand command)
    {
        var result = textEntry.Handle(command);
        if (result.Kind == FieldEditorOverlayResultKind.Cancelled)
        {
            ClearOverlay();
            return ActionPlanEditResult.Stay(result.Message);
        }

        if (result.Kind != FieldEditorOverlayResultKind.Confirmed)
        {
            return ActionPlanEditResult.Stay(result.Message);
        }

        return SetSelectedStepLabel(string.IsNullOrWhiteSpace(result.Value) ? null : result.Value);
    }

    private ActionPlanEditResult HandleMoveMode(UiComponentCommand command)
    {
        if (command == UiComponentCommand.Cancel)
        {
            _moveMode = false;
            return ActionPlanEditResult.Stay("Cancelled action-step move mode.");
        }

        if (command == UiComponentCommand.Select)
        {
            _moveMode = false;
            return ActionPlanEditResult.Stay($"Placed action step at position {_selectedStepIndex + 1}.");
        }

        if (command is UiComponentCommand.Up or UiComponentCommand.Left)
        {
            return SwapSelectedStep(-1);
        }

        if (command is UiComponentCommand.Down or UiComponentCommand.Right)
        {
            return SwapSelectedStep(1);
        }

        return ActionPlanEditResult.Stay("Move mode: Up/Down swaps selected step. Enter/Space confirms. Esc cancels.");
    }

    private ActionPlanEditResult HandleFocused(string focused, UiComponentCommand command)
    {
        if (command == UiComponentCommand.Cancel)
        {
            _focusRouter.Handle(UiComponentCommand.Cancel);
            return ActionPlanEditResult.Stay($"Released focus from {focused}.");
        }

        if (command is UiComponentCommand.Up or UiComponentCommand.Left)
        {
            if (command == UiComponentCommand.Left)
            {
                MoveStepFieldSelection(-1);
                return ActionPlanEditResult.Stay(StepFieldSelectionMessage());
            }

            MoveStepSelection(-1);
            return ActionPlanEditResult.Stay(StepSelectionMessage());
        }

        if (command is UiComponentCommand.Down or UiComponentCommand.Right)
        {
            if (command == UiComponentCommand.Right)
            {
                MoveStepFieldSelection(1);
                return ActionPlanEditResult.Stay(StepFieldSelectionMessage());
            }

            MoveStepSelection(1);
            return ActionPlanEditResult.Stay(StepSelectionMessage());
        }

        if (command == UiComponentCommand.Select)
        {
            return OpenStepDetail();
        }

        return ActionPlanEditResult.Stay("Use Up/Down to choose a step. Enter opens 4.1.2 details.");
    }

    private ActionPlanEditResult HandleStepDetail(UiComponentCommand command)
    {
        if (command == UiComponentCommand.Cancel)
        {
            _stepDetailOpen = false;
            return ActionPlanEditResult.Stay("Closed 4.1.2 step detail popup.");
        }

        if (command is UiComponentCommand.Up or UiComponentCommand.Left)
        {
            MoveStepFieldSelection(-1);
            return ActionPlanEditResult.Stay(StepFieldSelectionMessage());
        }

        if (command is UiComponentCommand.Down or UiComponentCommand.Right)
        {
            MoveStepFieldSelection(1);
            return ActionPlanEditResult.Stay(StepFieldSelectionMessage());
        }

        if (command == UiComponentCommand.Select)
        {
            return _selectedStepDetailFieldIndex == 0
                ? OpenStepPrimitivePicker(ActionPlanStepPickerMode.Replace)
                : OpenLabelTextEntry();
        }

        return ActionPlanEditResult.Stay("4.1.2 step detail: Up/Down chooses field. Enter edits. Esc closes popup.");
    }

    private void MoveStepSelection(int delta)
    {
        if (_actionPlan.ActionSteps.Count == 0) return;
        _selectedStepIndex = Math.Clamp(_selectedStepIndex + delta, 0, _actionPlan.ActionSteps.Count - 1);
    }

    private string StepSelectionMessage() => _actionPlan.ActionSteps.Count == 0
        ? "No action steps defined."
        : $"Highlighted step {_selectedStepIndex + 1}: {_actionPlan.ActionSteps[_selectedStepIndex].DisplayName}.";

    private void MoveStepFieldSelection(int delta) =>
        _selectedStepDetailFieldIndex = Math.Clamp(_selectedStepDetailFieldIndex + delta, 0, 1);

    private string StepFieldSelectionMessage() => _selectedStepDetailFieldIndex == 0
        ? "Selected Action Step field. Enter opens 4.1.1 primitive picker."
        : "Selected Label field. Enter opens target-label text input.";

    private ActionPlanEditResult OpenStepDetail()
    {
        if (_actionPlan.ActionSteps.Count == 0)
        {
            return ActionPlanEditResult.Stay("No action step is available to inspect.");
        }

        _stepDetailOpen = true;
        _selectedStepDetailFieldIndex = 0;
        return ActionPlanEditResult.Stay($"Opened 4.1.2 details for step {_selectedStepIndex + 1}.");
    }

    private ActionPlanEditResult OpenStepPrimitivePicker(ActionPlanStepPickerMode mode)
    {
        if (_availableSteps.Count == 0)
        {
            return ActionPlanEditResult.Stay("No engine-defined action steps are available for action-plan editing.");
        }

        _pickerMode = mode;
        _overlay = new ChoicePickerOverlayComponent(
            "action-step-primitive-picker",
            mode == ActionPlanStepPickerMode.Replace ? "4.1.1 Replace action step" : "4.1.1 Insert action step",
            "action step primitive",
            _availableSteps.Select(step => new SelectableListItem(step.Kind.ToString(), step.DisplayName, step.Hint)),
            SadConsoleRect.FromSize(34, 8, 70, 18),
            SelectedPrimitiveIndex());
        return ActionPlanEditResult.Stay(mode == ActionPlanStepPickerMode.Replace
            ? $"Opened replacement picker for step {_selectedStepIndex + 1}."
            : "Opened insert action-step picker.");
    }

    private ActionPlanEditResult OpenInsertPositionPicker()
    {
        if (_actionPlan.ActionSteps.Count == 0)
        {
            _insertBelow = false;
            return OpenStepPrimitivePicker(ActionPlanStepPickerMode.Insert);
        }

        _overlay = new ChoicePickerOverlayComponent(
            "action-step-insert-position",
            "4.1.2 Insert position",
            "insert position",
            [
                new SelectableListItem("above", "insert above", $"before step {_selectedStepIndex + 1}"),
                new SelectableListItem("below", "insert below", $"after step {_selectedStepIndex + 1}")
            ],
            SadConsoleRect.FromSize(42, 10, 42, 8),
            _insertBelow ? 1 : 0);
        return ActionPlanEditResult.Stay($"Opened insert-position picker for step {_selectedStepIndex + 1}.");
    }

    private ActionPlanEditResult OpenLabelTextEntry()
    {
        if (_actionPlan.ActionSteps.Count == 0)
        {
            return ActionPlanEditResult.Stay("No action step is available for label editing.");
        }

        var step = _actionPlan.ActionSteps[_selectedStepIndex];
        if (!step.ConsumesTargetReference)
        {
            return ActionPlanEditResult.Stay($"Step {_selectedStepIndex + 1} ({step.DisplayName}) does not consume a target reference; label editing is disabled.");
        }

        _overlay = new TextEntryOverlayComponent(
            "action-step-label-editor",
            "4.1.2 Edit action label",
            "target label",
            step.TargetLabel ?? string.Empty,
            SadConsoleRect.FromSize(36, 10, 56, 7),
            maxLength: 24,
            allowEmpty: true,
            validate: ValidateTargetLabel);
        return ActionPlanEditResult.Stay($"Opened target-label text input for step {_selectedStepIndex + 1}.");
    }

    private ActionPlanEditResult ReplaceSelectedStep(string kindId)
    {
        if (_service is null) return CloseOverlayWith("Action-plan edits require a service-backed editor screen.");
        if (_actionPlan.ActionSteps.Count == 0) return CloseOverlayWith("No action step is available to replace.");
        if (!TryParseKind(kindId, out var kind)) return CloseOverlayWith($"Unknown action step kind {kindId}.");

        var result = _service.ReplaceActionPlanStep(_actionPlan.ActionPlanId, _selectedStepIndex, kind);
        ReplaceAfterMutation(result.Snapshot, _selectedStepIndex);
        ClearOverlay();
        return ActionPlanEditResult.Stay(result.StatusMessage);
    }

    private ActionPlanEditResult InsertSelectedStep(string kindId)
    {
        if (_service is null) return CloseOverlayWith("Action-plan edits require a service-backed editor screen.");
        if (!TryParseKind(kindId, out var kind)) return CloseOverlayWith($"Unknown action step kind {kindId}.");

        var insertIndex = _actionPlan.ActionSteps.Count == 0 ? 0 : _selectedStepIndex + (_insertBelow ? 1 : 0);
        var result = _service.InsertActionPlanStep(_actionPlan.ActionPlanId, insertIndex, kind);
        ReplaceAfterMutation(result.Snapshot, insertIndex);
        ClearOverlay();
        return ActionPlanEditResult.Stay(result.StatusMessage);
    }

    private ActionPlanEditResult DeleteSelectedStep()
    {
        if (_service is null) return ActionPlanEditResult.Stay("Action-plan edits require a service-backed editor screen.");
        if (_actionPlan.ActionSteps.Count == 0) return ActionPlanEditResult.Stay("No action step is available to delete.");

        var result = _service.RemoveActionPlanStep(_actionPlan.ActionPlanId, _selectedStepIndex);
        ReplaceAfterMutation(result.Snapshot, Math.Max(0, _selectedStepIndex - 1));
        return ActionPlanEditResult.Stay(result.StatusMessage);
    }

    private ActionPlanEditResult SetSelectedStepLabel(string? label)
    {
        if (_service is null) return CloseOverlayWith("Action-plan label edits require a service-backed editor screen.");
        if (_actionPlan.ActionSteps.Count == 0) return CloseOverlayWith("No action step is available for label editing.");

        var result = _service.SetActionPlanStepTargetLabel(_actionPlan.ActionPlanId, _selectedStepIndex, label);
        ReplaceAfterMutation(result.Snapshot, _selectedStepIndex);
        ClearOverlay();
        return ActionPlanEditResult.Stay(result.StatusMessage);
    }

    private ActionPlanEditResult ToggleMoveMode()
    {
        if (_actionPlan.ActionSteps.Count == 0)
        {
            return ActionPlanEditResult.Stay("No action step is available to move.");
        }

        if (_moveMode)
        {
            _moveMode = false;
            return ActionPlanEditResult.Stay($"Placed action step at position {_selectedStepIndex + 1}.");
        }

        _moveMode = true;
        return ActionPlanEditResult.Stay($"Move mode: picked up step {_selectedStepIndex + 1}. Up/Down swaps; Enter/Space places.");
    }

    private ActionPlanEditResult SwapSelectedStep(int delta)
    {
        if (_service is null) return ActionPlanEditResult.Stay("Action-plan edits require a service-backed editor screen.");
        var toIndex = _selectedStepIndex + delta;
        if (toIndex < 0 || toIndex >= _actionPlan.ActionSteps.Count)
        {
            return ActionPlanEditResult.Stay("Selected step cannot move farther in that direction.");
        }

        var result = _service.MoveActionPlanStep(_actionPlan.ActionPlanId, _selectedStepIndex, toIndex);
        ReplaceAfterMutation(result.Snapshot, toIndex);
        return ActionPlanEditResult.Stay(result.StatusMessage);
    }

    private void ReplaceAfterMutation(FrontendEditorSnapshot snapshot, int preferredIndex)
    {
        _actionPlan = snapshot.ActionPlans.First(plan => plan.ActionPlanId == _actionPlan.ActionPlanId);
        _availableSteps.Clear();
        _availableSteps.AddRange(snapshot.AvailableActionSteps);
        _selectedStepIndex = _actionPlan.ActionSteps.Count == 0 ? 0 : Math.Clamp(preferredIndex, 0, _actionPlan.ActionSteps.Count - 1);
        _snapshotMutated?.Invoke(snapshot);
    }

    private ActionPlanEditResult CloseOverlayWith(string message)
    {
        ClearOverlay();
        return ActionPlanEditResult.Stay(message);
    }

    private void ClearOverlay()
    {
        _overlay = null;
        _pickerMode = null;
    }

    public ActionPlanEditResult InsertText(string text)
    {
        if (_overlay is not TextEntryOverlayComponent textEntry)
        {
            return ActionPlanEditResult.Stay("No text field editor is open.");
        }

        textEntry.InsertText(text);
        return ActionPlanEditResult.Stay("Typing action-step label.");
    }

    public ActionPlanEditResult Backspace()
    {
        if (_overlay is not TextEntryOverlayComponent textEntry)
        {
            return ActionPlanEditResult.Stay("No text field editor is open.");
        }

        textEntry.Backspace();
        return ActionPlanEditResult.Stay("Typing action-step label.");
    }

    private int SelectedPrimitiveIndex()
    {
        if (_actionPlan.ActionSteps.Count == 0) return 0;
        var kind = _actionPlan.ActionSteps[_selectedStepIndex].Kind;
        var index = _availableSteps.FindIndex(step => step.Kind == kind);
        return index < 0 ? 0 : index;
    }

    private static bool TryParseKind(string kindId, out ActionPlanBehaviorStepKind kind) =>
        Enum.TryParse(kindId, out kind);

    private string ReturnMessage() => ReturnDestination == ActionPlanEditReturnDestination.EntityTemplateEdit
        ? "Returned to Entity Template Edit."
        : "Returned to Scenario Edit.";

    private PanelComponent ActionStepList()
    {
        var rows = new List<string>();
        if (_actionPlan.ActionSteps.Count == 0)
        {
            rows.Add("No action steps defined. Press I to insert.");
        }
        else
        {
            foreach (var step in _actionPlan.ActionSteps)
            {
                var selected = step.Index == _selectedStepIndex;
                rows.Add($"{(selected ? ">" : " ")} Step {step.Index + 1}: {step.DisplayName} {FormatTargetLabelInline(step)}");
            }
        }

        return new PanelComponent(
            "action-plan-steps",
            "4.1 Action steps",
            new SadConsoleRect(1, 4, 58, 36),
            rows,
            _focusRouter.StateFor("action-plan-steps"));
    }

    private PanelComponent HighlightedStepPanel()
    {
        var rows = new List<string>();
        if (_overlay is ChoicePickerOverlayComponent picker && picker.Id == "action-step-primitive-picker" && HighlightedAvailableStep(picker) is { } primitive)
        {
            rows.Add("highlighting primitive:");
            rows.Add($"kind: {primitive.Kind}");
            rows.Add($"name: {primitive.DisplayName}");
            rows.Add($"hint: {primitive.Hint}");
        }
        else if (_actionPlan.ActionSteps.Count == 0)
        {
            rows.Add("No action steps defined.");
            rows.Add("Press I to insert the first step.");
        }
        else
        {
            var step = _actionPlan.ActionSteps[_selectedStepIndex];
            rows.Add("highlighting plan step:");
            rows.Add($"step: {_selectedStepIndex + 1}");
            rows.Add($"kind: {step.Kind}");
            rows.Add($"name: {step.DisplayName}");
            rows.Add($"target label: {FormatTargetLabel(step)}");
            rows.Add($"target-consuming: {(step.ConsumesTargetReference ? "yes" : "no")}");
            rows.Add(_moveMode ? "move mode active" : "Enter opens 4.1.2 details. I inserts. Delete removes. Space moves.");
        }

        if (_actionPlan.TargetLabelRequirements.Count > 0)
        {
            rows.Add("target labels required:");
            rows.AddRange(_actionPlan.TargetLabelRequirements.Select(requirement =>
                $"{requirement.Label}: steps {string.Join(",", requirement.StepIndexes.Select(index => index + 1))}"));
        }
        else
        {
            rows.Add("target labels required: none");
        }

        return new PanelComponent(
            "highlighted-action-step",
            "4.2 Highlighted step details",
            new SadConsoleRect(62, 4, 55, 20),
            rows,
            UiComponentState.Selected);
    }

    private FrontendEditorAvailableActionStepSummary? HighlightedAvailableStep(ChoicePickerOverlayComponent picker)
    {
        var kindId = picker.SelectedChoice?.Id;
        return kindId is null ? null : _availableSteps.FirstOrDefault(step => step.Kind.ToString() == kindId);
    }

    private static string FormatTargetLabel(FrontendEditorActionPlanStepSummary step) =>
        string.IsNullOrWhiteSpace(step.TargetLabel)
            ? "(none)"
            : step.TargetLabel;

    private static string FormatTargetLabelInline(FrontendEditorActionPlanStepSummary step) =>
        string.IsNullOrWhiteSpace(step.TargetLabel) ? "(none)" : step.TargetLabel;

    private IUiComponent? StepDetailComponent()
    {
        if (!_stepDetailOpen || _actionPlan.ActionSteps.Count == 0)
        {
            return null;
        }

        var step = _actionPlan.ActionSteps[_selectedStepIndex];
        return new FieldGroupComponent(
            "action-step-detail",
            $"4.1.2 Step {step.Index + 1} details",
            SadConsoleRect.FromSize(36, 8, 56, 10),
            [
                new EditableFieldComponent(
                    "action-step-kind",
                    _selectedStepDetailFieldIndex == 0 ? "> action step name" : "action step name",
                    step.DisplayName,
                    EditableFieldMode.Editable),
                new EditableFieldComponent(
                    "action-step-label",
                    _selectedStepDetailFieldIndex == 1 ? "> label" : "label",
                    FormatTargetLabel(step),
                    step.ConsumesTargetReference ? EditableFieldMode.Editable : EditableFieldMode.ReadOnly,
                    ValidationMessage: step.ConsumesTargetReference ? null : "step does not consume target")
            ],
            UiComponentState.Focused);
    }

    private static string? ValidateTargetLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.All(ch => char.IsAsciiLetterLower(ch) || char.IsDigit(ch))
            ? null
            : "Target label must be lowercase alphanumeric with no spaces.";
    }
}

internal enum ActionPlanEditCommand
{
    Insert,
    Delete,
    ToggleMoveMode
}

internal enum ActionPlanStepPickerMode
{
    Replace,
    Insert
}

internal enum ActionPlanEditReturnDestination
{
    ScenarioEdit,
    EntityTemplateEdit
}

internal sealed record ActionPlanEditResult(ActionPlanEditResultKind Kind, string Message, ActionPlanEditReturnDestination? ReturnDestination = null)
{
    public static ActionPlanEditResult Stay(string message) => new(ActionPlanEditResultKind.Stay, message);
    public static ActionPlanEditResult Return(ActionPlanEditReturnDestination destination, string message) => new(ActionPlanEditResultKind.Return, message, destination);
}

internal enum ActionPlanEditResultKind
{
    Stay,
    Return
}
