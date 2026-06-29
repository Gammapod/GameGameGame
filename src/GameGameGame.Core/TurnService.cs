namespace GameGameGame.Core;

public sealed class TurnService(
    MovementService movement,
    IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
    Action<WorldState, EntityId>? beforePlan = null)
{
    public bool TakeActorTurnThenAdvance(WorldState world, EntityId actorId, PlannedActionPlan actorPlan)
    {
        var root = new TraceNode($"Turn {world.TurnNumber + 1}", TraceStatus.Info);
        var actions = new List<TurnActionReport>();
        beforePlan?.Invoke(world, actorId);
        var actorResult = ResolvePlanTrace(world, actorId, actorPlan);
        PostActionStateUpdater.ApplyFacingFromMovement(world, actorId, actorResult.ActorMovementDirection);
        root.Add(actorResult.Trace);
        actions.Add(CreateActionReport(world, actorId, actorResult));

        AdvanceAfterPlayerTurn(world, root, actions);
        root.Status = root.Children.Any(child => child.Status == TraceStatus.Failure)
            ? TraceStatus.Failure
            : TraceStatus.Success;
        world.RecordTrace(root);
        world.RecordTurnReport(new SimulationTurnReport(world.TurnNumber, actions));

        return actorResult.Acted;
    }

    public void AdvanceAfterPlayerTurn(WorldState world)
    {
        var root = new TraceNode($"Turn {world.TurnNumber + 1}", TraceStatus.Info);
        var actions = new List<TurnActionReport>();
        AdvanceAfterPlayerTurn(world, root, actions);
        root.Status = root.Children.Any(child => child.Status == TraceStatus.Failure)
            ? TraceStatus.Failure
            : TraceStatus.Success;
        world.RecordTrace(root);
        world.RecordTurnReport(new SimulationTurnReport(world.TurnNumber, actions));
    }

    private void AdvanceAfterPlayerTurn(WorldState world, TraceNode root, List<TurnActionReport> actions)
    {
        world.AdvanceTurn();

        foreach (var (entityId, actionPlan) in actionPlans)
        {
            if (world.Entities.ContainsKey(entityId))
            {
                beforePlan?.Invoke(world, entityId);
                var result = ResolvePlanTrace(world, entityId, actionPlan.PlanTurn(world, entityId, movement));
                PostActionStateUpdater.ApplyFacingFromMovement(world, entityId, result.ActorMovementDirection);
                root.Add(result.Trace);
                actions.Add(CreateActionReport(world, entityId, result));
            }
        }
    }

    public bool ResolvePlan(WorldState world, EntityId actorId, PlannedActionPlan plan)
    {
        beforePlan?.Invoke(world, actorId);
        var result = ResolvePlanTrace(world, actorId, plan);
        PostActionStateUpdater.ApplyFacingFromMovement(world, actorId, result.ActorMovementDirection);
        world.RecordTrace(result.Trace);

        return result.Acted;
    }

    private TurnResolutionReport ResolvePlanTrace(WorldState world, EntityId actorId, PlannedActionPlan plan)
    {
        var actorName = world.Entities.TryGetValue(actorId, out var actor) ? actor.Name : actorId.ToString();
        var root = new TraceNode($"Resolve plan for {actorName}", TraceStatus.Info);

        foreach (var option in plan.Options)
        {
            var resolution = option.Resolve(world, actorId, movement);
            root.Add(resolution.Trace);

            if (resolution.ConsumesTurn)
            {
                root.Status = resolution.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
                root.Detail = $"resolved {option.GetType().Name}";
                return new TurnResolutionReport(resolution.Succeeded, resolution.Succeeded, resolution.ConsumesTurn, resolution.ContinuePlan, root, resolution.ActorMovementDirection);
            }

            if (!resolution.ContinuePlan)
            {
                root.Status = resolution.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
                root.Detail = $"stopped at {option.GetType().Name}";
                return new TurnResolutionReport(resolution.Succeeded, resolution.Succeeded, resolution.ConsumesTurn, resolution.ContinuePlan, root, resolution.ActorMovementDirection);
            }
        }

        root.Status = TraceStatus.Failure;
        root.Detail = "no planned action could execute";
        return new TurnResolutionReport(false, false, ConsumesTurn: false, ContinuePlan: false, root);
    }

    private static TurnActionReport CreateActionReport(WorldState world, EntityId actorId, TurnResolutionReport resolution)
    {
        var actorName = world.Entities.TryGetValue(actorId, out var actor) ? actor.Name : actorId.ToString();
        return new TurnActionReport(
            actorId,
            actorName,
            resolution.Succeeded,
            resolution.ConsumesTurn,
            TurnActionSummaryFormatter.FormatTrace(resolution.Trace, resolution.Succeeded),
            resolution.Trace);
    }

    private sealed record TurnResolutionReport(bool Acted, bool Succeeded, bool ConsumesTurn, bool ContinuePlan, TraceNode Trace, Direction? ActorMovementDirection = null);
}
