namespace GameGameGame.Core;

public sealed class TurnService(MovementService movement, IReadOnlyDictionary<EntityId, IEntityBehavior> behaviors)
{
    public bool TakePlayerTurn(WorldState world, PlannedActionPlan playerPlan)
    {
        var root = new TraceNode($"Turn {world.TurnNumber + 1}", TraceStatus.Info);
        var (acted, playerTrace) = ResolvePlanTrace(world, WorldBuilder.PlayerId, playerPlan);
        root.Add(playerTrace);

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

        foreach (var (entityId, behavior) in behaviors)
        {
            if (world.Entities.ContainsKey(entityId))
            {
                var (_, trace) = ResolvePlanTrace(world, entityId, behavior.PlanTurn(world, entityId, movement));
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
            var evaluation = option.Evaluate(world, actorId, movement);
            root.Add(evaluation.Trace);

            if (evaluation.CanExecute)
            {
                option.Execute(world, actorId, movement);
                root.Status = TraceStatus.Success;
                root.Detail = $"executed {option.GetType().Name}";
                return (true, root);
            }
        }

        root.Status = TraceStatus.Failure;
        root.Detail = "no planned action could execute";
        return (false, root);
    }
}
