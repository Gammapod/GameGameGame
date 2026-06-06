namespace GameGameGame.Core;

public interface IEntityActionPlan
{
    PlannedActionPlan PlanTurn(WorldState world, EntityId entityId, MovementService movement);
}

public sealed class WanderingSlimeActionPlan(Direction initialFacing = Direction.West) : IEntityActionPlan
{
    public Direction Facing { get; private set; } = initialFacing;

    public PlannedActionPlan PlanTurn(WorldState world, EntityId entityId, MovementService movement)
    {
        var actions = new List<IActionIntent>
        {
            new MoveAction(Facing)
        };

        var blockingEntity = movement.GetBlockingEntity(world, entityId, Facing);

        if (blockingEntity is { } targetId && world.GetInventoryPlaneId(entityId) is { } inventoryPlaneId)
        {
            actions.Add(new PickupAction(targetId, new PlaneCoord(inventoryPlaneId, new GridCoord(0, 0))));
            actions.Add(new BumpAndSetFacingAction(targetId, Facing, Reverse(Facing), SetFacing));
        }
        else
        {
            actions.Add(new SetFacingAction(Reverse(Facing), SetFacing));
            actions.Add(new WaitAction());
        }

        return new PlannedActionPlan(actions);
    }

    private void SetFacing(Direction direction) => Facing = direction;

    private static Direction Reverse(Direction direction) => direction switch
    {
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.East => Direction.West,
        Direction.West => Direction.East,
        _ => direction
    };
}

public sealed record SetFacingAction(Direction Direction, Action<Direction> SetFacing) : IActionIntent
{
    public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement) =>
        new(true, TraceNode.Success($"Set facing {Direction}"));

    public void Execute(WorldState world, EntityId actorId, MovementService movement) => SetFacing(Direction);

    public ActionResolution Resolve(WorldState world, EntityId actorId, MovementService movement)
    {
        var evaluation = Evaluate(world, actorId, movement);
        Execute(world, actorId, movement);

        return new ActionResolution(true, ConsumesTurn: false, ContinuePlan: true, evaluation.Trace);
    }
}

public sealed record BumpAndSetFacingAction(
    EntityId TargetId,
    Direction BumpDirection,
    Direction NewFacing,
    Action<Direction> SetFacing) : IActionIntent
{
    public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement)
    {
        var trace = new TraceNode($"Bump {BumpDirection}", TraceStatus.Info);

        if (!world.Entities.ContainsKey(TargetId))
        {
            return ActionTrace.Fail(trace, FailureReason.TargetMissing, $"target {TargetId} does not exist");
        }

        if (!movement.AreAdjacent(world, actorId, TargetId))
        {
            return ActionTrace.Fail(trace, FailureReason.TargetNotAdjacent, $"{world.FormatEntityAddress(TargetId)} is not adjacent to {world.FormatEntityAddress(actorId)}");
        }

        if (movement.GetBlockingEntity(world, actorId, BumpDirection) != TargetId)
        {
            return ActionTrace.Fail(trace, FailureReason.TargetNotAdjacent, $"{TargetId} is not blocking {BumpDirection}");
        }

        trace.Add(TraceNode.Success("Bump target", world.FormatEntityAddress(TargetId)));
        trace.Add(TraceNode.Success("New facing", NewFacing.ToString()));
        trace.Status = TraceStatus.Success;
        return new ActionEvaluation(true, trace);
    }

    public void Execute(WorldState world, EntityId actorId, MovementService movement) => SetFacing(NewFacing);
}
