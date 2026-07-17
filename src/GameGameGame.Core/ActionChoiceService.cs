namespace GameGameGame.Core;

public enum ActionChoiceKind
{
    Move,
    Pickup,
    Drop
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
    IReadOnlyList<ActionChoiceDirectionOption> DirectionOptions,
    IReadOnlyList<ControlledActorEntityAffordance> EntityOptions,
    IReadOnlyDictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>> DestinationsByTargetId)
{
    public IReadOnlyList<ControlledActorDestinationAffordance> Destinations(EntityId targetId) =>
        DestinationsByTargetId.TryGetValue(targetId, out var destinations) ? destinations : [];
}

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

        var choices = new List<ActionChoice>();
        var hasMoveChoice = false;
        var affordanceService = new ControlledActorAffordanceService(movement);
        var affordances = affordanceService.Query(world, actorId);

        for (var index = 0; index < behavior.Steps.Count; index++)
        {
            var step = behavior.Steps[index];
            switch (step.Kind)
            {
                case ActionPlanBehaviorStepKind.Move when !hasMoveChoice:
                    choices.Add(new ActionChoice(ActionChoiceKind.Move, index, QueryMoveDirections(world, actorId), [], new Dictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>>()));
                    hasMoveChoice = true;
                    break;
                case ActionPlanBehaviorStepKind.PickupTarget:
                case ActionPlanBehaviorStepKind.TransformAdjacentToInventory:
                    choices.Add(new ActionChoice(ActionChoiceKind.Pickup, index, [], affordances.PickupSources, affordances.PickupDestinationsByTargetId));
                    break;
                case ActionPlanBehaviorStepKind.DropFacing:
                case ActionPlanBehaviorStepKind.TransformInventoryToAdjacent:
                    choices.Add(new ActionChoice(ActionChoiceKind.Drop, index, [], affordances.DropSources, QueryAdjacentDropDestinations(world, actorId, affordances.DropSources)));
                    break;
            }
        }

        return choices.Count == 0 ? null : new ActionChoiceRequest(actorId, choices);
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

    public ControlledActorCommandResult SubmitPickupChoice(
        WorldState world,
        ActionChoiceRequest request,
        EntityId targetId,
        PlaneCoord destination,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        Action<WorldState, EntityId>? beforePlan = null)
    {
        if (!request.Choices.Any(choice => choice.Kind == ActionChoiceKind.Pickup))
        {
            throw new InvalidOperationException("Action choice request does not contain a Pickup choice.");
        }

        var commands = new ControlledActorCommandService(movement, actionPlans, beforePlan);
        return commands.Execute(world, request.ActorId, ControlledActorCommand.Pickup(targetId, destination));
    }

    public ControlledActorCommandResult SubmitDropChoice(
        WorldState world,
        ActionChoiceRequest request,
        EntityId targetId,
        PlaneCoord destination,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        Action<WorldState, EntityId>? beforePlan = null)
    {
        if (!request.Choices.Any(choice => choice.Kind == ActionChoiceKind.Drop))
        {
            throw new InvalidOperationException("Action choice request does not contain a Drop choice.");
        }

        var commands = new ControlledActorCommandService(movement, actionPlans, beforePlan);
        return commands.Execute(world, request.ActorId, ControlledActorCommand.Drop(targetId, destination));
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

    private IReadOnlyDictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>> QueryAdjacentDropDestinations(
        WorldState world,
        EntityId actorId,
        IReadOnlyList<ControlledActorEntityAffordance> dropSources)
    {
        if (!world.Entities.ContainsKey(actorId))
        {
            return new Dictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>>();
        }

        var actorLocation = world.GetEntityLocation(actorId);
        return dropSources.ToDictionary(source => source.TargetId, source => QueryAdjacentDropDestinations(world, actorId, source.TargetId, actorLocation));
    }

    private IReadOnlyList<ControlledActorDestinationAffordance> QueryAdjacentDropDestinations(WorldState world, EntityId actorId, EntityId targetId, PlaneCoord actorLocation) =>
        DirectionMath.AllDirections.Select(direction =>
        {
            var destination = new PlaneCoord(actorLocation.PlaneId, actorLocation.Coord.Offset(direction));
            var evaluation = new DropAction(targetId, destination).Evaluate(world, actorId, movement);
            var failure = evaluation.CanExecute ? null : FindFailure(evaluation.Trace) ?? evaluation.Trace;
            return new ControlledActorDestinationAffordance(
                targetId,
                destination,
                evaluation.CanExecute,
                failure?.Reason == FailureReason.None ? null : failure?.Reason,
                failure?.Detail,
                world.GetOccupant(destination));
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
