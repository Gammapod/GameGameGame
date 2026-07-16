namespace GameGameGame.Core;

public enum ActionChoiceKind
{
    Move
}

public sealed record ActionChoiceDirectionOption(
    Direction Direction,
    PlaneCoord? Destination,
    bool CanExecute,
    FailureReason? FailureReason,
    string? FailureDetail,
    EntityId? BlockingEntityId = null);

public sealed record ActionChoice(
    ActionChoiceKind Kind,
    int StepIndex,
    IReadOnlyList<ActionChoiceDirectionOption> DirectionOptions);

public sealed record ActionChoiceRequest(
    EntityId ActorId,
    IReadOnlyList<ActionChoice> Choices);

public sealed class ActionChoiceService(MovementService movement)
{
    public ActionChoiceRequest? CreateRequest(WorldState world, EntityId actorId, ActionPlanDescriptor descriptor)
    {
        if (world.GetActionControlSource(actorId) != EntityControlSource.PlayerChoice)
        {
            return null;
        }

        if (descriptor.Behavior is not { Steps.Count: > 0 } behavior)
        {
            return null;
        }

        for (var index = 0; index < behavior.Steps.Count; index++)
        {
            var step = behavior.Steps[index];
            if (step.Kind != ActionPlanBehaviorStepKind.Move)
            {
                continue;
            }

            return new ActionChoiceRequest(
                actorId,
                [new ActionChoice(ActionChoiceKind.Move, index, QueryMoveDirections(world, actorId))]);
        }

        return null;
    }

    public ControlledActorCommandResult SubmitMoveChoice(
        WorldState world,
        ActionChoiceRequest request,
        Direction direction,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        Action<WorldState, EntityId>? beforePlan = null)
    {
        if (!request.Choices.Any(choice => choice.Kind == ActionChoiceKind.Move))
        {
            throw new InvalidOperationException("Action choice request does not contain a Move choice.");
        }

        var commands = new ControlledActorCommandService(movement, actionPlans, beforePlan);
        return commands.Execute(world, request.ActorId, ControlledActorCommand.Move(direction));
    }

    private IReadOnlyList<ActionChoiceDirectionOption> QueryMoveDirections(WorldState world, EntityId actorId) =>
        DirectionMath.AllDirections.Select(direction =>
        {
            var evaluation = new MoveAction(direction).Evaluate(world, actorId, movement);
            var destination = movement.TryGetMoveDestination(world, actorId, direction, out var resolvedDestination)
                ? resolvedDestination
                : (PlaneCoord?)null;
            var failure = evaluation.CanExecute ? null : FindFailure(evaluation.Trace) ?? evaluation.Trace;
            var blockingEntity = destination is { } concreteDestination ? world.GetOccupant(concreteDestination) : null;
            return new ActionChoiceDirectionOption(
                direction,
                destination,
                evaluation.CanExecute,
                failure?.Reason == FailureReason.None ? null : failure?.Reason,
                failure?.Detail,
                blockingEntity);
        }).ToList();

    private static TraceNode? FindFailure(TraceNode trace)
    {
        if (trace.Status == TraceStatus.Failure)
        {
            return trace;
        }

        foreach (var child in trace.Children)
        {
            var failure = FindFailure(child);
            if (failure is not null)
            {
                return failure;
            }
        }

        return null;
    }
}
