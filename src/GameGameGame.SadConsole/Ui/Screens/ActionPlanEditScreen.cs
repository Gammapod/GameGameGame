using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Navigation;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed class ActionPlanEditScreen
{
    private readonly FrontendEditorActionPlanSummary _actionPlan;
    private readonly FocusRouter _focusRouter;
    private int _selectedStepIndex;

    private ActionPlanEditScreen(FrontendEditorActionPlanSummary actionPlan, ActionPlanEditReturnDestination returnDestination)
    {
        _actionPlan = actionPlan;
        ReturnDestination = returnDestination;
        _focusRouter = new FocusRouter([new FocusTarget("action-plan-steps")]);
    }

    public string ActionPlanId => _actionPlan.ActionPlanId;
    public ActionPlanEditReturnDestination ReturnDestination { get; }
    public string Title => $"Edit Action Plan: {_actionPlan.ActionPlanId}";
    public string Purpose => "Review authored action-plan steps. Mutation controls will be added after the full UX skeleton is accepted.";
    public string? FocusedComponentId => _focusRouter.FocusedComponentId;
    public int SelectedStepIndex => _selectedStepIndex;

    public static ActionPlanEditScreen FromSnapshot(FrontendEditorSnapshot snapshot, string actionPlanId, ActionPlanEditReturnDestination returnDestination)
    {
        var actionPlan = snapshot.ActionPlans.First(plan => plan.ActionPlanId == actionPlanId);
        return new ActionPlanEditScreen(actionPlan, returnDestination);
    }

    public IReadOnlyList<IUiComponent> Components() => [ActionStepList()];

    public string FooterText()
    {
        if (FocusedComponentId is null)
        {
            return ReturnDestination == ActionPlanEditReturnDestination.EntityTemplateEdit
                ? "No component focused: Enter focuses steps. Esc returns to Entity Template Edit."
                : "No component focused: Enter focuses steps. Esc returns to Scenario Edit.";
        }

        return "Action steps focused: Up/Down chooses step. Enter reports edit placeholder. Esc releases focus.";
    }

    public ActionPlanEditResult Handle(UiComponentCommand command)
    {
        if (FocusedComponentId is { } focused)
        {
            return HandleFocused(focused, command);
        }

        var result = _focusRouter.Handle(command);
        return result.Kind switch
        {
            FocusRouterResultKind.CancelScreen => ActionPlanEditResult.Return(ReturnDestination, ReturnMessage()),
            FocusRouterResultKind.FocusedComponent => ActionPlanEditResult.Stay("Focused action steps."),
            _ => ActionPlanEditResult.Stay("Use Enter to focus action steps, Esc to return.")
        };
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
            return ActionPlanEditResult.Stay(_actionPlan.ActionSteps.Count == 0
                ? "No action step is available to edit."
                : $"Action-step edit placeholder: step {_selectedStepIndex + 1} ({_actionPlan.ActionSteps[_selectedStepIndex].DisplayName}).");
        }

        return ActionPlanEditResult.Stay("Action Plan editor shell is read-only for now.");
    }

    private void MoveStepSelection(int delta)
    {
        if (_actionPlan.ActionSteps.Count == 0)
        {
            return;
        }

        _selectedStepIndex = Math.Clamp(_selectedStepIndex + delta, 0, _actionPlan.ActionSteps.Count - 1);
    }

    private string StepSelectionMessage() => _actionPlan.ActionSteps.Count == 0
        ? "No action steps defined."
        : $"Selected step {_selectedStepIndex + 1}: {_actionPlan.ActionSteps[_selectedStepIndex].DisplayName}.";

    private string ReturnMessage() => ReturnDestination == ActionPlanEditReturnDestination.EntityTemplateEdit
        ? "Returned to Entity Template Edit."
        : "Returned to Scenario Edit.";

    private SelectableListComponent ActionStepList()
    {
        var items = _actionPlan.ActionSteps.Select(step => new SelectableListItem(
            step.Index.ToString(),
            $"step {step.Index + 1}: {step.DisplayName}",
            step.Kind.ToString())).ToList();
        if (items.Count == 0)
        {
            items.Add(new SelectableListItem("empty", "No action steps defined.", "Insert/replace/delete/reorder will be designed next.", IsEnabled: false));
        }

        var list = new SelectableListComponent(
            "action-plan-steps",
            "4.1 Action steps",
            new SadConsoleRect(1, 4, 116, 36),
            items,
            _focusRouter.StateFor("action-plan-steps"),
            visibleRowCount: 30);
        for (var index = 0; index < _selectedStepIndex; index++) list.MoveSelection(1);
        return list;
    }
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
