namespace GameGameGame.Core;

public enum ActionChoiceKind
{
    Move,
    Pickup,
    Drop,
    Enter,
    Exit,
    Transfer,
    Push,
    AuthoredStep
}

public sealed record ActionChoicePushDirectionOption(
    EntityId TargetId,
    Direction Direction,
    PlaneCoord? Destination,
    bool CanExecute,
    FailureReason? FailureReason,
    string? FailureDetail,
    EntityId? BlockingEntityId = null,
    TopologyNodeId? DestinationNodeId = null,
    TopologyEdgeKind? EdgeKind = null);

public sealed record ActionChoiceTransferCounterpartyOption(
    EntityId CounterpartyId,
    Direction Direction,
    PlaneCoord Source,
    bool CanExecute,
    FailureReason? FailureReason,
    string? FailureDetail,
    TopologyNodeId? SourceNodeId = null,
    TopologyEdgeKind? EdgeKind = null);

public sealed record ActionChoiceTransferItemOption(
    EntityId CounterpartyId,
    EntityId MovingEntityId,
    EntityId OwnerEntityId,
    PlaneCoord Source,
    TransferDirection TransferDirection,
    bool CanExecute,
    FailureReason? FailureReason,
    string? FailureDetail,
    PlaneCoord? Destination = null);

public sealed record ActionChoiceDirectionOption(
    Direction Direction,
    PlaneCoord? Destination,
    bool CanExecute,
    FailureReason? FailureReason,
    string? FailureDetail,
    EntityId? BlockingEntityId = null,
    TopologyNodeId? DestinationNodeId = null,
    TopologyEdgeKind? EdgeKind = null);

public sealed record ActionChoice(
    ActionChoiceKind Kind,
    int StepIndex,
    IReadOnlyList<ActionChoiceDirectionOption> DirectionOptions,
    IReadOnlyList<ControlledActorEntityAffordance> EntityOptions,
    IReadOnlyDictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>> DestinationsByTargetId,
    IReadOnlyList<ActionChoiceTransferCounterpartyOption> TransferCounterparties = null!,
    IReadOnlyDictionary<EntityId, IReadOnlyList<ActionChoiceTransferItemOption>>? TransferItemsByCounterpartyId = null,
    IReadOnlyDictionary<EntityId, IReadOnlyList<ActionChoicePushDirectionOption>>? PushDirectionsByTargetId = null)
{
    public IReadOnlyList<ControlledActorDestinationAffordance> Destinations(EntityId targetId) =>
        DestinationsByTargetId.TryGetValue(targetId, out var destinations) ? destinations : [];

    public IReadOnlyList<ActionChoiceTransferItemOption> TransferItems(EntityId counterpartyId) =>
        TransferItemsByCounterpartyId is not null && TransferItemsByCounterpartyId.TryGetValue(counterpartyId, out var items) ? items : [];

    public IReadOnlyList<ActionChoicePushDirectionOption> PushDirections(EntityId targetId) =>
        PushDirectionsByTargetId is not null && PushDirectionsByTargetId.TryGetValue(targetId, out var directions) ? directions : [];
}

public sealed record ActionChoiceRequest(
    EntityId ActorId,
    IReadOnlyList<ActionChoice> Choices);

public sealed class ActionChoiceService(MovementService movement)
{
    public ActionChoiceRequest? CreateRequest(WorldState world, EntityId actorId, ActionPlanDescriptor descriptor)
    {
        using var topologyScope = TopologyGraphMaterializer.BeginCacheScope();

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
        ControlledActorAffordances? affordances = null;
        ControlledActorAffordances GetAffordances() =>
            affordances ??= new ControlledActorAffordanceService(movement).Query(world, actorId);

        for (var index = 0; index < behavior.Steps.Count; index++)
        {
            var step = behavior.Steps[index];
            var actorAffordances = step.Kind is ActionPlanBehaviorStepKind.PickupTarget
                    or ActionPlanBehaviorStepKind.TransformAdjacentToInventory
                    or ActionPlanBehaviorStepKind.DropFacing
                    or ActionPlanBehaviorStepKind.TransformInventoryToAdjacent
                    or ActionPlanBehaviorStepKind.EnterTarget
                    or ActionPlanBehaviorStepKind.ExitFacing
                ? GetAffordances()
                : null;
            switch (step.Kind)
            {
                case ActionPlanBehaviorStepKind.Move when !hasMoveChoice:
                    choices.Add(new ActionChoice(ActionChoiceKind.Move, index, QueryMoveDirections(world, actorId), [], new Dictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>>()));
                    hasMoveChoice = true;
                    break;
                case ActionPlanBehaviorStepKind.Move:
                    break;
                case ActionPlanBehaviorStepKind.PickupTarget:
                case ActionPlanBehaviorStepKind.TransformAdjacentToInventory:
                    choices.Add(new ActionChoice(ActionChoiceKind.Pickup, index, [], actorAffordances!.PickupSources, actorAffordances.PickupDestinationsByTargetId));
                    break;
                case ActionPlanBehaviorStepKind.DropFacing:
                case ActionPlanBehaviorStepKind.TransformInventoryToAdjacent:
                    choices.Add(new ActionChoice(ActionChoiceKind.Drop, index, [], actorAffordances!.DropSources, QueryAdjacentDropDestinations(world, actorId, actorAffordances.DropSources)));
                    break;
                case ActionPlanBehaviorStepKind.EnterTarget:
                    choices.Add(new ActionChoice(ActionChoiceKind.Enter, index, [], actorAffordances!.EnterTargets, new Dictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>>()));
                    break;
                case ActionPlanBehaviorStepKind.ExitFacing:
                    choices.Add(new ActionChoice(ActionChoiceKind.Exit, index, actorAffordances!.ExitDirections.Select(ToDirectionOption).ToList(), [], new Dictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>>()));
                    break;
                case ActionPlanBehaviorStepKind.Transfer:
                    var transferCounterparties = QueryTransferCounterparties(world, actorId);
                    choices.Add(new ActionChoice(
                        ActionChoiceKind.Transfer,
                        index,
                        [],
                        [],
                        new Dictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>>(),
                        transferCounterparties,
                        transferCounterparties.ToDictionary(counterparty => counterparty.CounterpartyId, counterparty => QueryTransferItems(world, actorId, counterparty))));
                    break;
                case ActionPlanBehaviorStepKind.Push:
                    var pushDirectionsByTarget = new Dictionary<EntityId, IReadOnlyList<ActionChoicePushDirectionOption>>();
                    var pushTargets = QueryPushTargets(world, actorId, pushDirectionsByTarget);
                    choices.Add(new ActionChoice(
                        ActionChoiceKind.Push,
                        index,
                        [],
                        pushTargets,
                        new Dictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>>(),
                        PushDirectionsByTargetId: pushDirectionsByTarget));
                    break;
                default:
                    choices.Add(new ActionChoice(ActionChoiceKind.AuthoredStep, index, [], [], new Dictionary<EntityId, IReadOnlyList<ControlledActorDestinationAffordance>>()));
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

    public ControlledActorCommandResult SubmitEnterChoice(
        WorldState world,
        ActionChoiceRequest request,
        EntityId targetId,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        Action<WorldState, EntityId>? beforePlan = null)
    {
        if (!request.Choices.Any(choice => choice.Kind == ActionChoiceKind.Enter))
        {
            throw new InvalidOperationException("Action choice request does not contain an Enter choice.");
        }

        var commands = new ControlledActorCommandService(movement, actionPlans, beforePlan);
        return commands.Execute(world, request.ActorId, ControlledActorCommand.Enter(targetId));
    }

    public ControlledActorCommandResult SubmitExitChoice(
        WorldState world,
        ActionChoiceRequest request,
        Direction direction,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        Action<WorldState, EntityId>? beforePlan = null)
    {
        if (!request.Choices.Any(choice => choice.Kind == ActionChoiceKind.Exit))
        {
            throw new InvalidOperationException("Action choice request does not contain an Exit choice.");
        }

        var commands = new ControlledActorCommandService(movement, actionPlans, beforePlan);
        return commands.Execute(world, request.ActorId, ControlledActorCommand.Exit(direction));
    }

    public PlanExecutionResult SubmitAuthoredStepChoice(
        WorldState world,
        ActionChoiceRequest request,
        int stepIndex,
        ActionPlanBehaviorStepDescriptor step,
        IReadOnlyDictionary<ActionPlanId, ActionPlanDefinition>? planRegistry = null)
    {
        if (!request.Choices.Any(choice => choice.StepIndex == stepIndex))
        {
            throw new InvalidOperationException($"Action choice request does not contain step {stepIndex}.");
        }

        var plan = new ActionPlanDefinition(
            new ActionPlanId($"choice-step-{request.ActorId.Value}-{stepIndex}-{step.Kind}"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([step]));
        var result = new ActionPlanInterpreter(movement, planRegistry ?? new Dictionary<ActionPlanId, ActionPlanDefinition>())
            .Execute(world, request.ActorId, plan, new ActionPlanContext());
        PostActionStateUpdater.ApplyFacingFromMovement(world, request.ActorId, result.ActorMovementDirection);
        world.RecordTrace(result.Trace);
        if (result.ConsumesTurn)
        {
            world.AdvanceTurn();
        }

        return result;
    }

    public ControlledActorCommandResult SubmitTransferChoice(
        WorldState world,
        ActionChoiceRequest request,
        EntityId counterpartyId,
        EntityId movingEntityId,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        Action<WorldState, EntityId>? beforePlan = null)
    {
        if (!request.Choices.Any(choice => choice.Kind == ActionChoiceKind.Transfer))
        {
            throw new InvalidOperationException("Action choice request does not contain a Transfer choice.");
        }

        var command = ControlledActorCommand.Transfer(movingEntityId, counterpartyId);
        if (!TryDeriveTransfer(world, request.ActorId, counterpartyId, movingEntityId, out var transferDirection, out var counterpartyDirection, out var source, out var failure))
        {
            var trace = TraceNode.Failure("Transfer choice", failure.Reason, failure.Detail);
            world.RecordTrace(trace);
            return new ControlledActorCommandResult(request.ActorId, ControlledActorCommandKind.Transfer, null, movingEntityId, source, null, false, failure.Reason, failure.Detail, false, false, trace, null);
        }

        var action = new TransferAction(transferDirection, movingEntityId, counterpartyDirection);
        var evaluation = action.Evaluate(world, request.ActorId, movement);
        if (!evaluation.CanExecute)
        {
            world.RecordTrace(evaluation.Trace);
            var failureTrace = FindFailure(evaluation.Trace) ?? evaluation.Trace;
            return new ControlledActorCommandResult(
                request.ActorId,
                ControlledActorCommandKind.Transfer,
                counterpartyDirection,
                movingEntityId,
                source,
                null,
                false,
                failureTrace.Reason == FailureReason.None ? null : failureTrace.Reason,
                failureTrace.Detail,
                false,
                false,
                evaluation.Trace,
                null)
            { CounterpartyId = counterpartyId };
        }

        var turns = new TurnService(movement, actionPlans, beforePlan);
        var succeeded = turns.TakeActorTurnThenAdvance(world, request.ActorId, PlannedActionPlan.Single(action));
        return new ControlledActorCommandResult(
            request.ActorId,
            ControlledActorCommandKind.Transfer,
            counterpartyDirection,
            movingEntityId,
            source,
            null,
            succeeded,
            null,
            null,
            succeeded,
            true,
            world.LastTrace ?? evaluation.Trace,
            world.LastTurnReport)
        { CounterpartyId = counterpartyId };
    }

    public ControlledActorCommandResult SubmitPushChoice(
        WorldState world,
        ActionChoiceRequest request,
        EntityId targetId,
        Direction direction,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        Action<WorldState, EntityId>? beforePlan = null)
    {
        if (!request.Choices.Any(choice => choice.Kind == ActionChoiceKind.Push))
        {
            throw new InvalidOperationException("Action choice request does not contain a Push choice.");
        }

        var commands = new ControlledActorCommandService(movement, actionPlans, beforePlan);
        return commands.Execute(world, request.ActorId, ControlledActorCommand.Push(targetId, direction));
    }

    private IReadOnlyList<ActionChoiceDirectionOption> QueryMoveDirections(WorldState world, EntityId actorId) =>
        DirectionMath.AllDirections.Select(direction =>
        {
            var evaluation = new MoveAction(direction).Evaluate(world, actorId, movement);
            var movementEdge = movement.TryGetMovementEdge(world, actorId, direction, out var resolvedMovementEdge)
                ? resolvedMovementEdge
                : null;
            var destination = movementEdge is { IsBlocked: false }
                ? movementEdge.Destination
                : (PlaneCoord?)null;
            var destinationNodeId = movementEdge is { IsBlocked: false }
                ? movementEdge.DestinationNodeId
                : (TopologyNodeId?)null;
            var failure = evaluation.CanExecute ? null : FindFailure(evaluation.Trace) ?? evaluation.Trace;
            var blockingEntity = destination is { } concreteDestination ? world.GetOccupant(concreteDestination) : null;
            return new ActionChoiceDirectionOption(
                direction,
                destination,
                evaluation.CanExecute,
                failure?.Reason == FailureReason.None ? null : failure?.Reason,
                failure?.Detail,
                blockingEntity,
                destinationNodeId,
                movementEdge?.Kind);
        }).ToList();

    private static ActionChoiceDirectionOption ToDirectionOption(ControlledActorDirectionAffordance affordance) =>
        new(
            affordance.Direction,
            affordance.Destination,
            affordance.CanExecute,
            affordance.FailureReason,
            affordance.FailureDetail,
            affordance.BlockingEntityId,
            affordance.DestinationNodeId,
            affordance.EdgeKind);

    private IReadOnlyList<ControlledActorEntityAffordance> QueryPushTargets(
        WorldState world,
        EntityId actorId,
        Dictionary<EntityId, IReadOnlyList<ActionChoicePushDirectionOption>> directionsByTargetId)
    {
        if (!world.Entities.ContainsKey(actorId))
        {
            return [];
        }

        var adjacentTargets = world.Occupancy.Values
            .Where(targetId => targetId != actorId)
            .Select(targetId => (TargetId: targetId, Adjacency: movement.EvaluateAdjacency(world, actorId, targetId)))
            .Where(candidate => candidate.Adjacency.AreAdjacent)
            .OrderBy(candidate => world.GetEntityLocation(candidate.TargetId).Coord.Y)
            .ThenBy(candidate => world.GetEntityLocation(candidate.TargetId).Coord.X)
            .ThenBy(candidate => candidate.TargetId.Value)
            .ToList();

        var result = new List<ControlledActorEntityAffordance>();
        foreach (var (targetId, adjacency) in adjacentTargets)
        {
            var directions = QueryPushDirections(world, actorId, targetId);
            directionsByTargetId[targetId] = directions;
            var firstFailure = directions.FirstOrDefault(direction => !direction.CanExecute);
            result.Add(new ControlledActorEntityAffordance(
                targetId,
                world.GetEntityLocation(targetId),
                directions.Any(direction => direction.CanExecute),
                firstFailure?.FailureReason,
                firstFailure?.FailureDetail,
                adjacency.DestinationNodeId,
                adjacency.EdgeKind));
        }

        return result;
    }

    private IReadOnlyList<ActionChoicePushDirectionOption> QueryPushDirections(WorldState world, EntityId actorId, EntityId targetId) =>
        DirectionMath.AllDirections.Select(direction =>
        {
            var movementEdge = world.Entities.ContainsKey(targetId) && movement.TryGetMovementEdge(world, targetId, direction, out var resolvedMovementEdge)
                ? resolvedMovementEdge
                : null;
            var destination = movementEdge is { IsBlocked: false }
                ? movementEdge.Destination
                : (PlaneCoord?)null;
            var destinationNodeId = movementEdge is { IsBlocked: false }
                ? movementEdge.DestinationNodeId
                : (TopologyNodeId?)null;
            var evaluation = new PushAction(targetId, direction).Evaluate(world, actorId, movement);
            var failure = evaluation.CanExecute ? null : FindFailure(evaluation.Trace) ?? evaluation.Trace;
            var blockingEntity = destination is { } concreteDestination ? world.GetOccupant(concreteDestination) : null;
            return new ActionChoicePushDirectionOption(
                targetId,
                direction,
                destination,
                evaluation.CanExecute,
                failure?.Reason == FailureReason.None ? null : failure?.Reason,
                failure?.Detail,
                blockingEntity,
                destinationNodeId,
                movementEdge?.Kind);
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

        return dropSources.ToDictionary(source => source.TargetId, source => QueryAdjacentDropDestinations(world, actorId, source.TargetId));
    }

    private IReadOnlyList<ControlledActorDestinationAffordance> QueryAdjacentDropDestinations(WorldState world, EntityId actorId, EntityId targetId) =>
        DirectionMath.AllDirections.Select(direction =>
        {
            var movementEdge = movement.TryGetMovementEdge(world, actorId, direction, out var resolvedMovementEdge)
                ? resolvedMovementEdge
                : null;
            var destination = movementEdge?.Destination ?? ResolveProjectedMoveDestination(world, actorId, direction);
            var destinationNodeId = movementEdge is { IsBlocked: false }
                ? movementEdge.DestinationNodeId
                : (TopologyNodeId?)null;
            var evaluation = new DropAction(targetId, destination).Evaluate(world, actorId, movement);
            var failure = evaluation.CanExecute ? null : FindFailure(evaluation.Trace) ?? evaluation.Trace;
            return new ControlledActorDestinationAffordance(
                targetId,
                destination,
                evaluation.CanExecute,
                failure?.Reason == FailureReason.None ? null : failure?.Reason,
                failure?.Detail,
                world.GetOccupant(destination),
                destinationNodeId,
                movementEdge?.Kind);
        }).ToList();

    private IReadOnlyList<ActionChoiceTransferCounterpartyOption> QueryTransferCounterparties(WorldState world, EntityId actorId)
    {
        if (!world.Entities.ContainsKey(actorId))
        {
            return [];
        }

        return DirectionMath.AllDirections.Select(direction =>
            {
                var movementEdge = movement.TryGetMovementEdge(world, actorId, direction, out var resolvedMovementEdge)
                    ? resolvedMovementEdge
                    : null;
                var coord = movementEdge?.Destination ?? ResolveProjectedMoveDestination(world, actorId, direction);
                var occupant = world.GetOccupant(coord);
                if (occupant is null || !world.Entities.TryGetValue(occupant.Value, out var entity))
                {
                    return null;
                }

                var canExecute = entity.HasUsableInventory && world.GetRegisteredInventoryPlaneId(occupant.Value) is not null;
                var sourceNodeId = movementEdge?.DestinationNodeId ?? (world.TryGetNodeId(coord, out var nodeId) ? new TopologyNodeId(nodeId.Value) : (TopologyNodeId?)null);
                return new ActionChoiceTransferCounterpartyOption(
                    occupant.Value,
                    direction,
                    coord,
                    canExecute,
                    canExecute ? null : FailureReason.TargetHasNoInventory,
                    canExecute ? null : $"{entity.Name} has no usable inventory",
                    sourceNodeId,
                    movementEdge?.Kind);
            })
            .Where(option => option is not null)
            .Cast<ActionChoiceTransferCounterpartyOption>()
            .ToList();
    }

    private PlaneCoord ResolveProjectedMoveDestination(WorldState world, EntityId actorId, Direction direction)
    {
        movement.TryGetMoveDestination(world, actorId, direction, out var destination);
        return destination;
    }

    private IReadOnlyList<ActionChoiceTransferItemOption> QueryTransferItems(WorldState world, EntityId actorId, ActionChoiceTransferCounterpartyOption counterparty)
    {
        var items = new List<ActionChoiceTransferItemOption>();
        items.AddRange(QueryTransferItemsFromOwner(world, actorId, actorId, counterparty.CounterpartyId, counterparty.Direction, TransferDirection.ActorToTarget));
        items.AddRange(QueryTransferItemsFromOwner(world, actorId, counterparty.CounterpartyId, counterparty.CounterpartyId, counterparty.Direction, TransferDirection.TargetToActor));
        return items;
    }

    private IReadOnlyList<ActionChoiceTransferItemOption> QueryTransferItemsFromOwner(WorldState world, EntityId actorId, EntityId ownerId, EntityId counterpartyId, Direction counterpartyDirection, TransferDirection transferDirection)
    {
        if (world.GetRegisteredInventoryPlaneId(ownerId) is not { } inventoryPlaneId)
        {
            return [];
        }

        return world.Occupancy
            .Where(entry => world.Nodes.TryGetValue(entry.Key, out var node) && node.PlaneId == inventoryPlaneId)
            .OrderBy(entry => world.Nodes[entry.Key].Coord.Y)
            .ThenBy(entry => world.Nodes[entry.Key].Coord.X)
            .Select(entry =>
            {
                var source = world.GetEntityLocation(entry.Value);
                var evaluation = new TransferAction(transferDirection, entry.Value, counterpartyDirection).Evaluate(world, actorId, movement);
                var failure = evaluation.CanExecute ? null : FindFailure(evaluation.Trace) ?? evaluation.Trace;
                return new ActionChoiceTransferItemOption(
                    counterpartyId,
                    entry.Value,
                    ownerId,
                    source,
                    transferDirection,
                    evaluation.CanExecute,
                    failure?.Reason == FailureReason.None ? null : failure?.Reason,
                    failure?.Detail);
            })
            .ToList();
    }

    private bool TryDeriveTransfer(WorldState world, EntityId actorId, EntityId counterpartyId, EntityId movingEntityId, out TransferDirection transferDirection, out Direction counterpartyDirection, out PlaneCoord? source, out (FailureReason Reason, string Detail) failure)
    {
        transferDirection = default;
        counterpartyDirection = default;
        source = null;
        failure = (FailureReason.None, string.Empty);
        if (!world.Entities.ContainsKey(actorId) || !world.Entities.ContainsKey(counterpartyId) || !world.Entities.ContainsKey(movingEntityId))
        {
            failure = (FailureReason.TargetMissing, "actor, counterparty, or moving entity is missing");
            return false;
        }

        var adjacency = movement.EvaluateAdjacency(world, actorId, counterpartyId);
        if (!adjacency.AreAdjacent || adjacency.Direction is null)
        {
            failure = (adjacency.FailureReason ?? FailureReason.TargetNotAdjacent, adjacency.FailureDetail ?? "counterparty is not adjacent");
            return false;
        }

        counterpartyDirection = adjacency.Direction.Value;
        source = world.GetEntityLocation(movingEntityId);
        var actorInventory = world.GetRegisteredInventoryPlaneId(actorId);
        var counterpartyInventory = world.GetRegisteredInventoryPlaneId(counterpartyId);
        if (source.Value.PlaneId == actorInventory)
        {
            transferDirection = TransferDirection.ActorToTarget;
            return true;
        }

        if (source.Value.PlaneId == counterpartyInventory)
        {
            transferDirection = TransferDirection.TargetToActor;
            return true;
        }

        failure = (FailureReason.TargetNotInInventory, $"{movingEntityId} is not contained by actor {actorId} or counterparty {counterpartyId}");
        return false;
    }

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
