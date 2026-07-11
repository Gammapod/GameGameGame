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
    private IUiComponent? _overlay;
    private ActionPlanStepPickerMode? _pickerMode;
    private bool _insertBelow;
    private bool _moveMode;

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
        _focusRouter = new FocusRouter([new FocusTarget("action-plan-steps")]);
    }

    public string ActionPlanId => _actionPlan.ActionPlanId;
    public ActionPlanEditReturnDestination ReturnDestination { get; }
    public string Title => $"Edit Action Plan: {_actionPlan.ActionPlanId}";
    public string Purpose => "Edit authored canonical behavior steps. Step parameters remain a later slice.";
    public string? FocusedComponentId => _focusRouter.FocusedComponentId;
    public int SelectedStepIndex => _selectedStepIndex;
    public bool IsMoveMode => _moveMode;

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

    public IUiComponent? OverlayComponent() => _overlay;

    public string FooterText()
    {
        if (_overlay is not null)
        {
            return _overlay.Id == "action-step-insert-position"
                ? "Insert position: Up/Down chooses above/below. Enter confirms. Esc cancels."
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

        return "Action steps: Up/Down highlights step. Enter replaces. I inserts. Delete removes. Space moves. Esc releases focus.";
    }

    public ActionPlanEditResult Handle(UiComponentCommand command)
    {
        if (_overlay is ChoicePickerOverlayComponent picker)
        {
            return HandleChoiceOverlay(picker, command);
        }

        if (_moveMode)
        {
            return HandleMoveMode(command);
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
            MoveStepSelection(-1);
            return ActionPlanEditResult.Stay(StepSelectionMessage());
        }

        if (command is UiComponentCommand.Down or UiComponentCommand.Right)
        {
            MoveStepSelection(1);
            return ActionPlanEditResult.Stay(StepSelectionMessage());
        }

        if (command == UiComponentCommand.Select)
        {
            return OpenStepPrimitivePicker(ActionPlanStepPickerMode.Replace);
        }

        return ActionPlanEditResult.Stay("Use Up/Down to highlight a step. Enter replaces. I inserts. Delete removes. Space moves.");
    }

    private void MoveStepSelection(int delta)
    {
        if (_actionPlan.ActionSteps.Count == 0) return;
        _selectedStepIndex = Math.Clamp(_selectedStepIndex + delta, 0, _actionPlan.ActionSteps.Count - 1);
    }

    private string StepSelectionMessage() => _actionPlan.ActionSteps.Count == 0
        ? "No action steps defined."
        : $"Highlighted step {_selectedStepIndex + 1}: {_actionPlan.ActionSteps[_selectedStepIndex].DisplayName}.";

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

    private SelectableListComponent ActionStepList()
    {
        var items = _actionPlan.ActionSteps.Select(step => new SelectableListItem(
            step.Index.ToString(),
            $"{step.Index + 1}. {step.DisplayName}",
            step.Kind.ToString())).ToList();
        if (items.Count == 0)
        {
            items.Add(new SelectableListItem("empty", "No action steps defined. Press I to insert.", string.Empty, IsEnabled: false));
        }

        var list = new SelectableListComponent(
            "action-plan-steps",
            "4.1 Action steps",
            new SadConsoleRect(1, 4, 58, 36),
            items,
            _focusRouter.StateFor("action-plan-steps"),
            visibleRowCount: 30);
        for (var index = 0; index < _selectedStepIndex && _actionPlan.ActionSteps.Count > 0; index++) list.MoveSelection(1);
        return list;
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
            rows.Add(_moveMode ? "move mode active" : "Enter replaces. I inserts. Delete removes. Space moves.");
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
