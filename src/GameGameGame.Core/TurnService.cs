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
        var actorResult = ActorTurnResolver.ResolvePlan(world, actorId, actorPlan, movement);
        PostActionStateUpdater.ApplyFacingFromMovement(world, actorId, actorResult.ActorMovementDirection);
        root.Add(actorResult.Trace);
        actions.Add(CreateActionReport(world, actorId, actorResult));

        AdvanceAfterPlayerTurn(world, root, actions);
        root.Status = root.Children.Any(child => child.Status == TraceStatus.Failure)
            ? TraceStatus.Failure
            : TraceStatus.Success;
        world.RecordTrace(root);
        world.RecordTurnReport(new SimulationTurnReport(world.TurnNumber, actions));

        return actorResult.Succeeded;
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

        foreach (var entityId in GetScheduledActorIds(world))
        {
            if (world.Entities.ContainsKey(entityId) && TryGetEffectiveActionPlan(world, entityId, out var actionPlan))
            {
                beforePlan?.Invoke(world, entityId);
                var result = ActorTurnResolver.ResolvePlan(world, entityId, actionPlan.PlanTurn(world, entityId, movement), movement);
                PostActionStateUpdater.ApplyFacingFromMovement(world, entityId, result.ActorMovementDirection);
                root.Add(result.Trace);
                actions.Add(CreateActionReport(world, entityId, result));
            }
        }
    }

    private IEnumerable<EntityId> GetScheduledActorIds(WorldState world)
    {
        var seen = new HashSet<EntityId>();

        foreach (var entityId in actionPlans.Keys.Concat(world.BehaviorProviders.Keys))
        {
            if (!seen.Add(entityId))
            {
                continue;
            }

            if (world.IsAssignedBehaviorProvider(entityId))
            {
                continue;
            }

            yield return entityId;
        }
    }

    private bool TryGetEffectiveActionPlan(WorldState world, EntityId actorId, out IEntityActionPlan actionPlan)
    {
        if (world.GetBehaviorProvider(actorId) is { } providerId && actionPlans.TryGetValue(providerId, out var providerPlan))
        {
            actionPlan = providerPlan;
            return true;
        }

        return actionPlans.TryGetValue(actorId, out actionPlan!);
    }

    public bool ResolvePlan(WorldState world, EntityId actorId, PlannedActionPlan plan)
    {
        beforePlan?.Invoke(world, actorId);
        var result = ActorTurnResolver.ResolvePlan(world, actorId, plan, movement);
        PostActionStateUpdater.ApplyFacingFromMovement(world, actorId, result.ActorMovementDirection);
        world.RecordTrace(result.Trace);

        return result.Succeeded;
    }

    private static TurnActionReport CreateActionReport(WorldState world, EntityId actorId, ActionResolution resolution)
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

}
