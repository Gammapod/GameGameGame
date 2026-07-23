namespace GameGameGame.Core;

public enum ControlledActorCommandKind
{
    Move,
    Pickup,
    Drop,
    Enter,
    Exit,
    Transfer,
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
    SimulationTurnReport? TurnReport)
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
        var evaluation = action.Evaluate(world, actorId, movement);

        if (!evaluation.CanExecute)
        {
            world.RecordTrace(evaluation.Trace);
            var failure = FindFailure(evaluation.Trace) ?? evaluation.Trace;
            return new ControlledActorCommandResult(
                actorId,
                command.Kind,
                command.Direction,
                command.TargetId,
                command.Source,
                command.Destination,
                Succeeded: false,
                failure.Reason == FailureReason.None ? null : failure.Reason,
                failure.Detail,
                ConsumedTurn: false,
                AdvancedTurn: false,
                evaluation.Trace,
                TurnReport: null);
        }

        var turns = new TurnService(movement, actionPlans, beforePlan);
        var succeeded = turns.TakeActorTurnThenAdvance(world, actorId, PlannedActionPlan.Single(action));
        return new ControlledActorCommandResult(
            actorId,
            command.Kind,
            command.Direction,
            command.TargetId,
            command.Source,
            command.Destination,
            succeeded,
            FailureReason: null,
            FailureDetail: null,
            ConsumedTurn: succeeded,
            AdvancedTurn: true,
            world.LastTrace ?? evaluation.Trace,
            world.LastTurnReport);
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
            ControlledActorCommandKind.Wait => new WaitAction(),
            _ => throw new InvalidOperationException($"Controlled command {command.Kind} is missing required command data.")
        };

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
