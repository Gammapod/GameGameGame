namespace GameGameGame.Core;

internal static class PickupDropPortabilityRule
{
    public static bool Evaluate(WorldState world, EntityId actorId, EntityId targetId, out TraceNode trace)
    {
        trace = new TraceNode($"Check pickup/drop portability for {targetId}", TraceStatus.Info);

        if (!world.Entities.TryGetValue(actorId, out var actor))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ActorMissing;
            trace.Detail = $"actor {actorId} does not exist";
            return false;
        }

        if (!world.Entities.TryGetValue(targetId, out var target))
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.TargetMissing;
            trace.Detail = $"target {targetId} does not exist";
            return false;
        }

        trace.Detail = $"actorBulk={actor.Bulk}, targetAperture={target.Aperture}";

        if (actor.Bulk <= target.Aperture)
        {
            trace.Status = TraceStatus.Failure;
            trace.Reason = FailureReason.ApertureBlocked;
            trace.Detail = $"{actor.Name} bulk {actor.Bulk} does not exceed {target.Name} aperture {target.Aperture}";
            return false;
        }

        trace.Status = TraceStatus.Success;
        return true;
    }
}
