namespace GameGameGame.Core;

public enum ControlledActorCommandKind
{
    Move,
    Pickup,
    Drop,
    Enter,
    Exit,
    Transfer,
    Push,
    Wait
}

public sealed record ControlledActorCommand(
    ControlledActorCommandKind Kind,
    Direction? Direction = null,
    EntityId? TargetId = null,
    PlaneCoord? Source = null,
    PlaneCoord? Destination = null)
{
    public static ControlledActorCommand Move(Direction direction) =>
        new(ControlledActorCommandKind.Move, Direction: direction);

    public static ControlledActorCommand Pickup(EntityId targetId, PlaneCoord destination) =>
        new(ControlledActorCommandKind.Pickup, TargetId: targetId, Destination: destination);

    public static ControlledActorCommand Drop(EntityId targetId, PlaneCoord destination) =>
        new(ControlledActorCommandKind.Drop, TargetId: targetId, Destination: destination);

    public static ControlledActorCommand Enter(EntityId targetId) =>
        new(ControlledActorCommandKind.Enter, TargetId: targetId);

    public static ControlledActorCommand Exit(Direction direction) =>
        new(ControlledActorCommandKind.Exit, Direction: direction);

    public static ControlledActorCommand Transfer(EntityId movingEntityId, EntityId counterpartyId) =>
        new ControlledActorCommand(ControlledActorCommandKind.Transfer, TargetId: movingEntityId) with { CounterpartyId = counterpartyId };

    public static ControlledActorCommand Push(EntityId targetId, Direction direction) =>
        new(ControlledActorCommandKind.Push, Direction: direction, TargetId: targetId);

    public static ControlledActorCommand Wait() =>
        new(ControlledActorCommandKind.Wait);

    public EntityId? CounterpartyId { get; init; }
}

public sealed record ControlledActorCommandResult(
    EntityId ActorId,
    ControlledActorCommandKind Kind,
    Direction? Direction,
    EntityId? TargetId,
    PlaneCoord? Source,
    PlaneCoord? Destination,
    bool Succeeded,
    FailureReason? FailureReason,
    string? FailureDetail,
    bool ConsumedTurn,
    bool AdvancedTurn,
    TraceNode Trace,
    SimulationTurnReport? TurnReport,
    TopologyNodeId? DestinationNodeId = null,
    TopologyEdgeKind? EdgeKind = null)
{
    public EntityId? CounterpartyId { get; init; }
}

public sealed class ControlledActorCommandService(
    MovementService movement,
    IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
    Action<WorldState, EntityId>? beforePlan = null)
{
    public ControlledActorCommandResult Execute(WorldState world, EntityId actorId, ControlledActorCommand command)
    {
        var action = CreateAction(command);
        var source = command.Source ?? ResolveSource(world, command);
        var evaluation = action.Evaluate(world, actorId, movement);
        var movementEdge = ResolveMovementEdge(world, actorId, command);
        var destination = command.Destination ?? movementEdge?.Destination ?? ResolveDestination(world, actorId, command);
        var destinationNodeId = movementEdge?.DestinationNodeId ?? ResolveDestinationNodeId(world, actorId, command, destination);
        var edgeKind = movementEdge?.Kind;

        if (!evaluation.CanExecute)
        {
            world.RecordTrace(evaluation.Trace);
            var failure = FindFailure(evaluation.Trace) ?? evaluation.Trace;
            return new ControlledActorCommandResult(
                actorId,
                command.Kind,
                command.Direction,
                command.TargetId,
                source,
                destination,
                Succeeded: false,
                failure.Reason == FailureReason.None ? null : failure.Reason,
                failure.Detail,
                ConsumedTurn: false,
                AdvancedTurn: false,
                evaluation.Trace,
                TurnReport: null,
                destinationNodeId,
                edgeKind);
        }

        var turns = new TurnService(movement, actionPlans, beforePlan);
        var succeeded = turns.TakeActorTurnThenAdvance(world, actorId, PlannedActionPlan.Single(action));
        return new ControlledActorCommandResult(
            actorId,
            command.Kind,
            command.Direction,
            command.TargetId,
            source,
            destination,
            succeeded,
            FailureReason: null,
            FailureDetail: null,
            ConsumedTurn: succeeded,
            AdvancedTurn: true,
            world.LastTrace ?? evaluation.Trace,
            world.LastTurnReport,
            destinationNodeId,
            edgeKind);
    }

    private static IActionIntent CreateAction(ControlledActorCommand command) =>
        command.Kind switch
        {
            ControlledActorCommandKind.Move when command.Direction is { } direction => new MoveAction(direction),
            ControlledActorCommandKind.Pickup when command.TargetId is { } targetId && command.Destination is { } destination => new PickupAction(targetId, destination),
            ControlledActorCommandKind.Drop when command.TargetId is { } targetId && command.Destination is { } destination => new DropAction(targetId, destination),
            ControlledActorCommandKind.Enter when command.TargetId is { } targetId => new EnterAction(targetId),
            ControlledActorCommandKind.Exit when command.Direction is { } direction => new ExitAction(direction),
            ControlledActorCommandKind.Transfer when command.TargetId is { } movingEntityId && command.CounterpartyId is { } counterpartyId => CreateTransferAction(command, movingEntityId, counterpartyId),
            ControlledActorCommandKind.Push when command.TargetId is { } targetId && command.Direction is { } direction => new PushAction(targetId, direction),
            ControlledActorCommandKind.Wait => new WaitAction(),
            _ => throw new InvalidOperationException($"Controlled command {command.Kind} is missing required command data.")
        };

    private static PlaneCoord? ResolveSource(WorldState world, ControlledActorCommand command) =>
        command.Kind == ControlledActorCommandKind.Push && command.TargetId is { } targetId && world.Entities.ContainsKey(targetId)
            ? world.GetEntityLocation(targetId)
            : command.Source;

    private PlaneCoord? ResolveDestination(WorldState world, EntityId actorId, ControlledActorCommand command)
    {
        if (command.Kind == ControlledActorCommandKind.Move && command.Direction is { } moveDirection)
        {
            return movement.TryGetMoveDestination(world, actorId, moveDirection, out var moveDestination)
                ? moveDestination
                : null;
        }

        if (command.Kind == ControlledActorCommandKind.Exit && command.Direction is { } exitDirection)
        {
            var actorLocation = world.GetEntityLocation(actorId);
            if (InventoryPlaneOwnership.TryFindOwner(world, actorLocation.PlaneId, out var containerId) &&
                movement.TryGetMoveDestination(world, containerId, exitDirection, out var exitDestination))
            {
                return exitDestination;
            }
        }

        if (command.Kind == ControlledActorCommandKind.Push && command.TargetId is { } targetId && command.Direction is { } direction && world.Entities.ContainsKey(targetId))
        {
            return movement.TryGetMoveDestination(world, targetId, direction, out var destination)
                ? destination
                : null;
        }

        return command.Destination;
    }

    private MovementEdgeResult? ResolveMovementEdge(WorldState world, EntityId actorId, ControlledActorCommand command)
    {
        if (command.Kind == ControlledActorCommandKind.Move && command.Direction is { } moveDirection &&
            movement.TryGetMovementEdge(world, actorId, moveDirection, out var moveEdge))
        {
            return moveEdge;
        }

        if (command.Kind == ControlledActorCommandKind.Exit && command.Direction is { } exitDirection)
        {
            var actorLocation = world.GetEntityLocation(actorId);
            if (InventoryPlaneOwnership.TryFindOwner(world, actorLocation.PlaneId, out var containerId) &&
                movement.TryGetMovementEdge(world, containerId, exitDirection, out var exitEdge))
            {
                return exitEdge;
            }
        }

        if (command.Kind == ControlledActorCommandKind.Push && command.TargetId is { } targetId && command.Direction is { } pushDirection &&
            world.Entities.ContainsKey(targetId) &&
            movement.TryGetMovementEdge(world, targetId, pushDirection, out var pushEdge))
        {
            return pushEdge;
        }

        return null;
    }

    private TopologyNodeId? ResolveDestinationNodeId(WorldState world, EntityId actorId, ControlledActorCommand command, PlaneCoord? destination)
    {
        if (command.Kind == ControlledActorCommandKind.Move && command.Direction is { } moveDirection)
        {
            return movement.TryGetMoveDestinationNode(world, actorId, moveDirection, out var nodeId)
                ? nodeId
                : null;
        }

        return destination is { } concreteDestination && world.TryGetNodeId(concreteDestination, out var destinationNodeId)
            ? new TopologyNodeId(destinationNodeId.Value)
            : null;
    }

    private static IActionIntent CreateTransferAction(ControlledActorCommand command, EntityId movingEntityId, EntityId counterpartyId)
    {
        throw new InvalidOperationException("Transfer commands must be constructed by ActionChoiceService so direction can be derived from the current world state.");
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
