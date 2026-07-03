namespace GameGameGame.Core;

public sealed record ActionStepAttempt(
    int Order,
    string StepKind,
    TraceStatus Status,
    FailureReason? FailureReason,
    string? Detail,
    bool Continued,
    bool Stopped,
    IReadOnlyList<string> StateReads,
    IReadOnlyList<string> StateWrites,
    IReadOnlyList<string> Results,
    TraceNode Trace);

public static class ActionStepAttemptProjection
{
    public static IReadOnlyList<ActionStepAttempt> Project(TraceNode planTrace)
    {
        var actionSteps = planTrace.Children
            .Where(child => child.Label.StartsWith("Action Step ", StringComparison.Ordinal))
            .ToList();

        return actionSteps
            .Select((step, index) =>
            {
                var continued = ShouldContinue(step, index, actionSteps.Count);
                return new ActionStepAttempt(
                    index + 1,
                    FormatActionStepLabel(step.Label),
                    step.Status,
                    step.Reason == FailureReason.None ? null : step.Reason,
                    step.Detail,
                    continued,
                    Stopped: !continued,
                    FindStateReads(step),
                    FindStateWrites(step),
                    FindResults(step),
                    step);
            })
            .ToList();
    }

    private static string FormatActionStepLabel(string label) =>
        label.StartsWith("Action Step ", StringComparison.Ordinal)
            ? label[12..]
            : label;

    private static bool ShouldContinue(TraceNode step, int index, int actionStepCount) =>
        index < actionStepCount - 1
        && (step.Status == TraceStatus.Failure
            || Descendants(step).Any(trace => trace.Label == "Primitive AcquireNearestTarget" && trace.Status == TraceStatus.Success));

    private static IReadOnlyList<string> FindStateReads(TraceNode node) =>
        Descendants(node)
            .Where(trace => trace.Label.StartsWith("Read slot ", StringComparison.Ordinal) && trace.Status == TraceStatus.Success)
            .Select(trace => $"{trace.Label[10..]}={trace.Detail}")
            .Distinct()
            .ToList();

    private static IReadOnlyList<string> FindStateWrites(TraceNode node) =>
        Descendants(node)
            .Where(trace => trace.Label.StartsWith("Set slot ", StringComparison.Ordinal) && trace.Status == TraceStatus.Success)
            .Select(trace => $"{trace.Label[9..]}={trace.Detail}")
            .Distinct()
            .ToList();

    private static IReadOnlyList<string> FindResults(TraceNode node) =>
        Descendants(node)
            .Where(trace => IsProjectedResultPrimitive(trace.Label) && !string.IsNullOrWhiteSpace(trace.Detail))
            .Select(trace => trace.Detail!)
            .Distinct()
            .ToList();

    private static bool IsProjectedResultPrimitive(string label) =>
        label is "Primitive Backstep"
            or "Primitive PickupTarget"
            or "Primitive AcquireNearestTarget"
            or "Primitive SeekTarget"
            or "Primitive FleeTarget"
            or "Primitive MaintainChebyshevDistanceTwo"
            or "Primitive StrafeClockwise"
            or "Primitive StrafeAnticlockwise"
            or "Primitive GiveTarget"
            or "Primitive TakeTarget"
            or "Primitive EnterTarget"
            or "Primitive ExitFacing";

    private static IEnumerable<TraceNode> Descendants(TraceNode node)
    {
        foreach (var child in node.Children)
        {
            yield return child;

            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
