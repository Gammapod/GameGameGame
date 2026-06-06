namespace GameGameGame.Core;

public sealed class TurnService(MovementService movement, IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans)
{
    public bool TakeActorTurnThenAdvance(WorldState world, EntityId actorId, PlannedActionPlan actorPlan)
    {
        var root = new TraceNode($"Turn {world.TurnNumber + 1}", TraceStatus.Info);
        var (acted, actorTrace) = ResolvePlanTrace(world, actorId, actorPlan);
        root.Add(actorTrace);

        AdvanceAfterPlayerTurn(world, root);
        root.Status = root.Children.Any(child => child.Status == TraceStatus.Failure)
            ? TraceStatus.Failure
            : TraceStatus.Success;
        world.RecordTrace(root);

        return acted;
    }

    public void AdvanceAfterPlayerTurn(WorldState world)
    {
        var root = new TraceNode($"Turn {world.TurnNumber + 1}", TraceStatus.Info);
        AdvanceAfterPlayerTurn(world, root);
        root.Status = root.Children.Any(child => child.Status == TraceStatus.Failure)
            ? TraceStatus.Failure
            : TraceStatus.Success;
        world.RecordTrace(root);
    }

    private void AdvanceAfterPlayerTurn(WorldState world, TraceNode root)
    {
        world.AdvanceTurn();

        foreach (var (entityId, actionPlan) in actionPlans)
        {
            if (world.Entities.ContainsKey(entityId))
            {
                var (_, trace) = ResolvePlanTrace(world, entityId, actionPlan.PlanTurn(world, entityId, movement));
                root.Add(trace);
            }
        }
    }

    public bool ResolvePlan(WorldState world, EntityId actorId, PlannedActionPlan plan)
    {
        var (acted, trace) = ResolvePlanTrace(world, actorId, plan);
        world.RecordTrace(trace);

        return acted;
    }

    private (bool Acted, TraceNode Trace) ResolvePlanTrace(WorldState world, EntityId actorId, PlannedActionPlan plan)
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
                return (resolution.Succeeded, root);
            }

            if (!resolution.ContinuePlan)
            {
                root.Status = resolution.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
                root.Detail = $"stopped at {option.GetType().Name}";
                return (resolution.Succeeded, root);
            }
        }

        root.Status = TraceStatus.Failure;
        root.Detail = "no planned action could execute";
        return (false, root);
    }
}
