using GameGameGame.Core;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal enum ActionChoicePromptMode
{
    Closed,
    ActionList,
    PickupTarget,
    PickupDestination,
    DropSource,
    DropDestination,
    EnterTarget,
    ExitFacing,
    TransferCounterparty,
    TransferItem
}

internal sealed class ActionChoicePromptController
{
    public ActionChoicePromptMode Mode { get; private set; }
    public int SelectedActionStepIndex { get; private set; }
    public ActionChoice? SelectedEntityActionChoice { get; private set; }
    public EntityId? SelectedEntityActionTargetId { get; private set; }
    public int SelectedTargetIndex { get; private set; }
    public int SelectedDestinationIndex { get; private set; }
    public int SelectedDirectionIndex { get; private set; }

    public bool IsOpen => Mode != ActionChoicePromptMode.Closed;

    public string OpenActionStepMenu(IReadOnlyList<ActionPlanBehaviorStepDescriptor> steps)
    {
        if (steps.Count == 0)
        {
            return "No authored action steps are available for the controlled entity.";
        }

        SelectedActionStepIndex = Math.Clamp(SelectedActionStepIndex, 0, steps.Count - 1);
        Mode = ActionChoicePromptMode.ActionList;
        SelectedEntityActionChoice = null;
        SelectedEntityActionTargetId = null;
        SelectedTargetIndex = 0;
        SelectedDestinationIndex = 0;
        return $"Opened action selector 0.2.1. Selected action {SelectedActionStepIndex + 1}/{steps.Count}: {steps[SelectedActionStepIndex].Kind}.";
    }

    public ActionChoicePromptCancelResult Cancel()
    {
        if (Mode == ActionChoicePromptMode.Closed)
        {
            return new ActionChoicePromptCancelResult("No action menu is open.");
        }

        switch (Mode)
        {
            case ActionChoicePromptMode.PickupTarget:
            case ActionChoicePromptMode.DropSource:
            case ActionChoicePromptMode.EnterTarget:
            case ActionChoicePromptMode.ExitFacing:
            case ActionChoicePromptMode.TransferCounterparty:
                Mode = ActionChoicePromptMode.ActionList;
                SelectedEntityActionChoice = null;
                SelectedEntityActionTargetId = null;
                SelectedTargetIndex = 0;
                SelectedDestinationIndex = 0;
                SelectedDirectionIndex = 0;
                return new ActionChoicePromptCancelResult("Returned to action selector.");
            case ActionChoicePromptMode.PickupDestination:
                Mode = ActionChoicePromptMode.PickupTarget;
                SelectedEntityActionTargetId = null;
                SelectedDestinationIndex = 0;
                return new ActionChoicePromptCancelResult("Returned to pickup target selection.");
            case ActionChoicePromptMode.DropDestination:
                Mode = ActionChoicePromptMode.DropSource;
                SelectedEntityActionTargetId = null;
                SelectedDestinationIndex = 0;
                return new ActionChoicePromptCancelResult("Returned to inventory item selection.", InspectPlayer: true);
            case ActionChoicePromptMode.TransferItem:
                Mode = ActionChoicePromptMode.TransferCounterparty;
                SelectedEntityActionTargetId = null;
                SelectedDestinationIndex = 0;
                return new ActionChoicePromptCancelResult("Returned to transfer counterparty selection.");
            default:
                Reset();
                return new ActionChoicePromptCancelResult("Closed action selector.");
        }
    }

    public ActionChoicePromptActionResult ConfirmSelectedActionStep(
        IReadOnlyList<ActionPlanBehaviorStepDescriptor> steps,
        ActionChoiceRequest? request,
        Func<EntityId, string> formatEntityName)
    {
        if (steps.Count == 0)
        {
            Reset();
            return ActionChoicePromptActionResult.Info("No authored action steps are available for the controlled entity.");
        }

        SelectedActionStepIndex = Math.Clamp(SelectedActionStepIndex, 0, steps.Count - 1);
        var step = steps[SelectedActionStepIndex];
        if (TryFindActionChoiceForStep(step, request, out var selectedChoice) && selectedChoice.Kind == ActionChoiceKind.Transfer)
        {
            var validCounterparties = ValidTransferCounterparties(selectedChoice).ToList();
            if (validCounterparties.Count == 0)
            {
                return ActionChoicePromptActionResult.Info($"Selected action {step.Kind}, but Core Action Choice reports no valid transfer counterparties.");
            }

            SelectedEntityActionChoice = selectedChoice;
            SelectedTargetIndex = 0;
            SelectedDestinationIndex = 0;
            SelectedDirectionIndex = 0;
            SelectedEntityActionTargetId = null;
            Mode = ActionChoicePromptMode.TransferCounterparty;
            return ActionChoicePromptActionResult.ChoosingTarget(
                $"Selected action {step.Kind}. Choose transfer entity 1/{validCounterparties.Count}: {formatEntityName(validCounterparties[0].CounterpartyId)}.",
                inspectPlayer: false);
        }

        if (TryFindActionChoiceForStep(step, request, out selectedChoice) && selectedChoice.Kind is ActionChoiceKind.Pickup or ActionChoiceKind.Drop or ActionChoiceKind.Enter)
        {
            var validTargets = ValidTargets(selectedChoice).ToList();
            if (validTargets.Count == 0)
            {
                return ActionChoicePromptActionResult.Info($"Selected action {step.Kind}, but Core Action Choice reports no valid targets.");
            }

            SelectedEntityActionChoice = selectedChoice;
            SelectedTargetIndex = 0;
            SelectedDestinationIndex = 0;
            SelectedDirectionIndex = 0;
            SelectedEntityActionTargetId = null;
            Mode = selectedChoice.Kind switch
            {
                ActionChoiceKind.Pickup => ActionChoicePromptMode.PickupTarget,
                ActionChoiceKind.Drop => ActionChoicePromptMode.DropSource,
                _ => ActionChoicePromptMode.EnterTarget
            };

            var noun = selectedChoice.Kind == ActionChoiceKind.Drop ? "inventory item" : "target";
            return ActionChoicePromptActionResult.ChoosingTarget(
                $"Selected action {step.Kind}. Choose {noun} 1/{validTargets.Count}: {formatEntityName(validTargets[0].TargetId)}.",
                selectedChoice.Kind == ActionChoiceKind.Drop);
        }

        if (TryFindActionChoiceForStep(step, request, out selectedChoice) && selectedChoice.Kind == ActionChoiceKind.Exit)
        {
            var validDirections = ValidDirections(selectedChoice).ToList();
            if (validDirections.Count == 0)
            {
                return ActionChoicePromptActionResult.Info($"Selected action {step.Kind}, but Core Action Choice reports no valid exit directions.");
            }

            SelectedEntityActionChoice = selectedChoice;
            SelectedTargetIndex = 0;
            SelectedDestinationIndex = 0;
            SelectedDirectionIndex = 0;
            SelectedEntityActionTargetId = null;
            Mode = ActionChoicePromptMode.ExitFacing;
            return ActionChoicePromptActionResult.ChoosingTarget(
                $"Selected action {step.Kind}. Choose exit direction 1/{validDirections.Count}: {validDirections[0].Direction}.",
                inspectPlayer: false);
        }

        return ActionChoicePromptActionResult.SubmitAuthoredStep(SelectedActionStepIndex, step);
    }

    public ActionChoicePromptTargetResult ConfirmSelectedTarget(
        Func<EntityId, string> formatEntityName,
        Func<PlaneCoord, string> formatDestination)
    {
        if (SelectedEntityActionChoice is not { } choice)
        {
            Reset();
            return ActionChoicePromptTargetResult.Info("No Core Action Choice target list is active.");
        }

        if (choice.Kind == ActionChoiceKind.Transfer)
        {
            var counterparty = ValidTransferCounterparties(choice).ElementAtOrDefault(SelectedTargetIndex);
            if (counterparty is null)
            {
                return ActionChoicePromptTargetResult.Info("No valid transfer counterparty is selected.");
            }

            var items = ValidTransferItems(choice, counterparty.CounterpartyId).ToList();
            if (items.Count == 0)
            {
                return ActionChoicePromptTargetResult.Info($"Selected {formatEntityName(counterparty.CounterpartyId)}, but Core Action Choice reports no transferable items.");
            }

            SelectedEntityActionTargetId = counterparty.CounterpartyId;
            SelectedDestinationIndex = 0;
            Mode = ActionChoicePromptMode.TransferItem;
            return ActionChoicePromptTargetResult.ChoosingDestination(
                $"Selected {formatEntityName(counterparty.CounterpartyId)}. Choose transfer item 1/{items.Count}: {formatEntityName(items[0].MovingEntityId)}.",
                inspectPlayer: false);
        }

        var targets = ValidTargets(choice).ToList();
        if (targets.Count == 0)
        {
            return ActionChoicePromptTargetResult.Info($"No valid {choice.Kind} targets are available from Core ActionChoiceService.");
        }

        SelectedTargetIndex = Math.Clamp(SelectedTargetIndex, 0, targets.Count - 1);
        var target = targets[SelectedTargetIndex];
        if (choice.Kind == ActionChoiceKind.Enter)
        {
            return ActionChoicePromptTargetResult.SubmitEnter(target.TargetId);
        }

        var destinations = ValidDestinations(choice, target.TargetId).ToList();
        if (destinations.Count == 0)
        {
            return ActionChoicePromptTargetResult.Info($"Selected {formatEntityName(target.TargetId)}, but Core Action Choice reports no valid destinations.");
        }

        SelectedEntityActionTargetId = target.TargetId;
        SelectedDestinationIndex = 0;
        Mode = choice.Kind == ActionChoiceKind.Pickup ? ActionChoicePromptMode.PickupDestination : ActionChoicePromptMode.DropDestination;
        var place = choice.Kind == ActionChoiceKind.Pickup ? "inventory location" : "drop destination";
        return ActionChoicePromptTargetResult.ChoosingDestination(
            $"Selected {formatEntityName(target.TargetId)}. Choose {place} 1/{destinations.Count}: {formatDestination(destinations[0].Destination)}.",
            choice.Kind == ActionChoiceKind.Pickup);
    }

    public ActionChoicePromptDirectionResult ConfirmSelectedDirection()
    {
        if (SelectedEntityActionChoice is not { } choice)
        {
            Reset();
            return ActionChoicePromptDirectionResult.Info("No Core Action Choice direction list is active.");
        }

        var directions = ValidDirections(choice).ToList();
        if (directions.Count == 0)
        {
            return ActionChoicePromptDirectionResult.Info($"No valid {choice.Kind} directions are available from Core ActionChoiceService.");
        }

        SelectedDirectionIndex = Math.Clamp(SelectedDirectionIndex, 0, directions.Count - 1);
        return ActionChoicePromptDirectionResult.Submit(choice, directions[SelectedDirectionIndex].Direction);
    }

    public ActionChoicePromptDestinationResult ConfirmSelectedDestination()
    {
        if (SelectedEntityActionChoice is not { } choice || SelectedEntityActionTargetId is not { } targetId)
        {
            Reset();
            return ActionChoicePromptDestinationResult.Info("No Core Action Choice destination list is active.");
        }

        var destinations = ValidDestinations(choice, targetId).ToList();
        if (destinations.Count == 0)
        {
            return ActionChoicePromptDestinationResult.Info($"No valid {choice.Kind} destinations are available for the selected target from CoreActionChoiceService.");
        }

        SelectedDestinationIndex = Math.Clamp(SelectedDestinationIndex, 0, destinations.Count - 1);
        return ActionChoicePromptDestinationResult.Submit(choice, targetId, destinations[SelectedDestinationIndex].Destination);
    }

    public string SelectMenuItem(
        int delta,
        IReadOnlyList<ActionPlanBehaviorStepDescriptor> steps,
        Func<EntityId, string> formatEntityName,
        Func<PlaneCoord, string> formatDestination)
    {
        return Mode switch
        {
            ActionChoicePromptMode.ActionList => SelectActionStep(delta, steps),
            ActionChoicePromptMode.PickupTarget or ActionChoicePromptMode.DropSource or ActionChoicePromptMode.EnterTarget => SelectTarget(delta, formatEntityName),
            ActionChoicePromptMode.TransferCounterparty => SelectTransferCounterparty(delta, formatEntityName),
            ActionChoicePromptMode.TransferItem => SelectTransferItem(delta, formatEntityName),
            ActionChoicePromptMode.PickupDestination or ActionChoicePromptMode.DropDestination => SelectDestination(delta, formatDestination),
            ActionChoicePromptMode.ExitFacing => SelectDirection(delta),
            _ => SelectActionStep(delta, steps)
        };
    }

    public IReadOnlyList<ControlledActorEntityAffordance> ValidSelectedTargets() =>
        SelectedEntityActionChoice is { } choice ? ValidTargets(choice).ToList() : [];

    public IReadOnlyList<ControlledActorDestinationAffordance> ValidSelectedDestinations() =>
        SelectedEntityActionChoice is { } choice && SelectedEntityActionTargetId is { } targetId
            ? ValidDestinations(choice, targetId).ToList()
            : [];

    public IReadOnlyList<ActionChoiceTransferItemOption> ValidSelectedTransferItems() =>
        SelectedEntityActionChoice is { } choice && SelectedEntityActionTargetId is { } counterpartyId
            ? ValidTransferItems(choice, counterpartyId).ToList()
            : [];

    public ActionChoiceTransferCounterpartyOption? SelectedTransferCounterparty()
    {
        if (SelectedEntityActionChoice is not { } choice)
        {
            return null;
        }

        var counterparties = ValidTransferCounterparties(choice).ToList();
        return counterparties.Count == 0 ? null : counterparties[Math.Clamp(SelectedTargetIndex, 0, counterparties.Count - 1)];
    }

    public ActionChoiceTransferItemOption? SelectedTransferItem()
    {
        var items = ValidSelectedTransferItems();
        return items.Count == 0 ? null : items[Math.Clamp(SelectedDestinationIndex, 0, items.Count - 1)];
    }

    public ActionChoicePromptTransferItemResult ConfirmSelectedTransferItem()
    {
        if (SelectedEntityActionChoice is not { } choice || SelectedEntityActionTargetId is not { } counterpartyId)
        {
            return ActionChoicePromptTransferItemResult.Info("No Core Action Choice transfer item list is active.");
        }

        var items = ValidTransferItems(choice, counterpartyId).ToList();
        if (items.Count == 0)
        {
            return ActionChoicePromptTransferItemResult.Info("No valid transfer items are available.");
        }

        SelectedDestinationIndex = Math.Clamp(SelectedDestinationIndex, 0, items.Count - 1);
        return ActionChoicePromptTransferItemResult.Submit(counterpartyId, items[SelectedDestinationIndex].MovingEntityId);
    }

    public void Reset()
    {
        Mode = ActionChoicePromptMode.Closed;
        SelectedEntityActionChoice = null;
        SelectedEntityActionTargetId = null;
        SelectedTargetIndex = 0;
        SelectedDestinationIndex = 0;
        SelectedDirectionIndex = 0;
    }

    private string SelectActionStep(int delta, IReadOnlyList<ActionPlanBehaviorStepDescriptor> steps)
    {
        if (steps.Count == 0)
        {
            SelectedActionStepIndex = 0;
            return "No authored action steps are available for the controlled entity.";
        }

        SelectedActionStepIndex = (SelectedActionStepIndex + delta + steps.Count) % steps.Count;
        return $"Selected action step {SelectedActionStepIndex + 1}/{steps.Count}: {steps[SelectedActionStepIndex].Kind}.";
    }

    private string SelectTarget(int delta, Func<EntityId, string> formatEntityName)
    {
        if (SelectedEntityActionChoice is not { } choice)
        {
            return "No Core Action Choice target list is active.";
        }

        var targets = ValidTargets(choice).ToList();
        if (targets.Count == 0)
        {
            SelectedTargetIndex = 0;
            return $"No valid {choice.Kind} targets are available.";
        }

        SelectedTargetIndex = (SelectedTargetIndex + delta + targets.Count) % targets.Count;
        return $"Selected target {SelectedTargetIndex + 1}/{targets.Count}: {formatEntityName(targets[SelectedTargetIndex].TargetId)}.";
    }

    private string SelectDestination(int delta, Func<PlaneCoord, string> formatDestination)
    {
        if (SelectedEntityActionChoice is not { } choice || SelectedEntityActionTargetId is not { } targetId)
        {
            return "No Core Action Choice destination list is active.";
        }

        var destinations = ValidDestinations(choice, targetId).ToList();
        if (destinations.Count == 0)
        {
            SelectedDestinationIndex = 0;
            return $"No valid {choice.Kind} destinations are available.";
        }

        SelectedDestinationIndex = (SelectedDestinationIndex + delta + destinations.Count) % destinations.Count;
        return $"Selected destination {SelectedDestinationIndex + 1}/{destinations.Count}: {formatDestination(destinations[SelectedDestinationIndex].Destination)}.";
    }

    private string SelectDirection(int delta)
    {
        if (SelectedEntityActionChoice is not { } choice)
        {
            return "No Core Action Choice direction list is active.";
        }

        var directions = ValidDirections(choice).ToList();
        if (directions.Count == 0)
        {
            SelectedDirectionIndex = 0;
            return $"No valid {choice.Kind} directions are available.";
        }

        SelectedDirectionIndex = (SelectedDirectionIndex + delta + directions.Count) % directions.Count;
        return $"Selected direction {SelectedDirectionIndex + 1}/{directions.Count}: {directions[SelectedDirectionIndex].Direction}.";
    }

    private string SelectTransferCounterparty(int delta, Func<EntityId, string> formatEntityName)
    {
        if (SelectedEntityActionChoice is not { } choice)
        {
            return "No Core Action Choice transfer counterparty list is active.";
        }

        var counterparties = ValidTransferCounterparties(choice).ToList();
        if (counterparties.Count == 0)
        {
            SelectedTargetIndex = 0;
            return "No valid Transfer counterparties are available.";
        }

        SelectedTargetIndex = (SelectedTargetIndex + delta + counterparties.Count) % counterparties.Count;
        return $"Selected transfer entity {SelectedTargetIndex + 1}/{counterparties.Count}: {formatEntityName(counterparties[SelectedTargetIndex].CounterpartyId)}.";
    }

    private string SelectTransferItem(int delta, Func<EntityId, string> formatEntityName)
    {
        if (SelectedEntityActionChoice is not { } choice || SelectedEntityActionTargetId is not { } counterpartyId)
        {
            return "No Core Action Choice transfer item list is active.";
        }

        var items = ValidTransferItems(choice, counterpartyId).ToList();
        if (items.Count == 0)
        {
            SelectedDestinationIndex = 0;
            return "No valid Transfer items are available.";
        }

        SelectedDestinationIndex = (SelectedDestinationIndex + delta + items.Count) % items.Count;
        return $"Selected transfer item {SelectedDestinationIndex + 1}/{items.Count}: {formatEntityName(items[SelectedDestinationIndex].MovingEntityId)}.";
    }

    private static bool TryFindActionChoiceForStep(ActionPlanBehaviorStepDescriptor step, ActionChoiceRequest? request, out ActionChoice choice)
    {
        var kind = step.Kind switch
        {
            ActionPlanBehaviorStepKind.Move => ActionChoiceKind.Move,
            ActionPlanBehaviorStepKind.PickupTarget => ActionChoiceKind.Pickup,
            ActionPlanBehaviorStepKind.TransformAdjacentToInventory => ActionChoiceKind.Pickup,
            ActionPlanBehaviorStepKind.DropFacing => ActionChoiceKind.Drop,
            ActionPlanBehaviorStepKind.TransformInventoryToAdjacent => ActionChoiceKind.Drop,
            ActionPlanBehaviorStepKind.EnterTarget => ActionChoiceKind.Enter,
            ActionPlanBehaviorStepKind.ExitFacing => ActionChoiceKind.Exit,
            ActionPlanBehaviorStepKind.Transfer => ActionChoiceKind.Transfer,
            _ => (ActionChoiceKind?)null
        };

        if (kind is { } actionChoiceKind && request?.Choices.FirstOrDefault(choice => choice.Kind == actionChoiceKind) is { } match)
        {
            choice = match;
            return true;
        }

        choice = null!;
        return false;
    }

    private static IEnumerable<ControlledActorEntityAffordance> ValidTargets(ActionChoice choice) =>
        choice.EntityOptions.Where(option => option.CanExecute);

    private static IEnumerable<ControlledActorDestinationAffordance> ValidDestinations(ActionChoice choice, EntityId targetId) =>
        choice.Destinations(targetId).Where(destination => destination.CanExecute);

    private static IEnumerable<ActionChoiceDirectionOption> ValidDirections(ActionChoice choice) =>
        choice.DirectionOptions.Where(direction => direction.CanExecute);

    private static IEnumerable<ActionChoiceTransferCounterpartyOption> ValidTransferCounterparties(ActionChoice choice) =>
        choice.TransferCounterparties.Where(counterparty => counterparty.CanExecute);

    private static IEnumerable<ActionChoiceTransferItemOption> ValidTransferItems(ActionChoice choice, EntityId counterpartyId) =>
        choice.TransferItems(counterpartyId).Where(item => item.CanExecute);
}

internal sealed record ActionChoicePromptCancelResult(string Message, bool InspectPlayer = false);

internal sealed record ActionChoicePromptActionResult(
    ActionChoicePromptActionResultKind Kind,
    string Message,
    int StepIndex = 0,
    ActionPlanBehaviorStepDescriptor? Step = null,
    bool InspectPlayer = false)
{
    public static ActionChoicePromptActionResult Info(string message) => new(ActionChoicePromptActionResultKind.Message, message);

    public static ActionChoicePromptActionResult ChoosingTarget(string message, bool inspectPlayer) =>
        new(ActionChoicePromptActionResultKind.ChoosingTarget, message, InspectPlayer: inspectPlayer);

    public static ActionChoicePromptActionResult SubmitAuthoredStep(int stepIndex, ActionPlanBehaviorStepDescriptor step) =>
        new(ActionChoicePromptActionResultKind.SubmitAuthoredStep, string.Empty, stepIndex, step);
}

internal enum ActionChoicePromptActionResultKind
{
    Message,
    ChoosingTarget,
    SubmitAuthoredStep
}

internal sealed record ActionChoicePromptTargetResult(ActionChoicePromptTargetResultKind Kind, string Message, bool InspectPlayer = false, EntityId? TargetId = null)
{
    public static ActionChoicePromptTargetResult Info(string message) => new(ActionChoicePromptTargetResultKind.Message, message);

    public static ActionChoicePromptTargetResult ChoosingDestination(string message, bool inspectPlayer) =>
        new(ActionChoicePromptTargetResultKind.ChoosingDestination, message, inspectPlayer);

    public static ActionChoicePromptTargetResult SubmitEnter(EntityId targetId) =>
        new(ActionChoicePromptTargetResultKind.SubmitEnter, string.Empty, TargetId: targetId);
}

internal enum ActionChoicePromptTargetResultKind
{
    Message,
    ChoosingDestination,
    SubmitEnter
}

internal sealed record ActionChoicePromptDestinationResult(
    ActionChoicePromptDestinationResultKind Kind,
    string Message,
    ActionChoice? Choice = null,
    EntityId? TargetId = null,
    PlaneCoord? Destination = null)
{
    public static ActionChoicePromptDestinationResult Info(string message) => new(ActionChoicePromptDestinationResultKind.Message, message);

    public static ActionChoicePromptDestinationResult Submit(ActionChoice choice, EntityId targetId, PlaneCoord destination) =>
        new(ActionChoicePromptDestinationResultKind.Submit, string.Empty, choice, targetId, destination);
}

internal enum ActionChoicePromptDestinationResultKind
{
    Message,
    Submit
}

internal sealed record ActionChoicePromptDirectionResult(ActionChoicePromptDirectionResultKind Kind, string Message, ActionChoice? Choice = null, Direction? Direction = null)
{
    public static ActionChoicePromptDirectionResult Info(string message) => new(ActionChoicePromptDirectionResultKind.Message, message);

    public static ActionChoicePromptDirectionResult Submit(ActionChoice choice, Direction direction) =>
        new(ActionChoicePromptDirectionResultKind.Submit, string.Empty, choice, direction);
}

internal enum ActionChoicePromptDirectionResultKind
{
    Message,
    Submit
}

internal sealed record ActionChoicePromptTransferItemResult(ActionChoicePromptTransferItemResultKind Kind, string Message, EntityId? CounterpartyId = null, EntityId? MovingEntityId = null)
{
    public static ActionChoicePromptTransferItemResult Info(string message) => new(ActionChoicePromptTransferItemResultKind.Message, message);

    public static ActionChoicePromptTransferItemResult Submit(EntityId counterpartyId, EntityId movingEntityId) =>
        new(ActionChoicePromptTransferItemResultKind.Submit, string.Empty, counterpartyId, movingEntityId);
}

internal enum ActionChoicePromptTransferItemResultKind
{
    Message,
    Submit
}
