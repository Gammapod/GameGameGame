namespace GameGameGame.Core;

public static class ActorTurnResolver
{
    public static ActionResolution ResolvePlan(
        WorldState world,
        EntityId actorId,
        PlannedActionPlan plan,
        MovementService movement)
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
                return new ActionResolution(
                    resolution.Succeeded,
                    resolution.ConsumesTurn,
                    resolution.ContinuePlan,
                    root,
                    resolution.ActorMovementDirection);
            }

            if (!resolution.ContinuePlan)
            {
                root.Status = resolution.Succeeded ? TraceStatus.Success : TraceStatus.Failure;
                root.Detail = $"stopped at {option.GetType().Name}";
                return new ActionResolution(
                    resolution.Succeeded,
                    resolution.ConsumesTurn,
                    resolution.ContinuePlan,
                    root,
                    resolution.ActorMovementDirection);
            }
        }

        root.Status = TraceStatus.Failure;
        root.Detail = "no planned action could execute";
        return new ActionResolution(false, ConsumesTurn: false, ContinuePlan: false, root);
    }
}
