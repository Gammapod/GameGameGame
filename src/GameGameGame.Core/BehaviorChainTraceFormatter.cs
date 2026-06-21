namespace GameGameGame.Core;

public static class BehaviorChainTraceFormatter
{
    public static IReadOnlyList<string> Format(PlanExecutionResult result)
    {
        var lines = new List<string>
        {
            $"{FormatPlanLabel(result.Trace.Label)}: {result.Trace.Status}; consumedTurn={result.ConsumesTurn}; continuePlan={result.ContinuePlan}"
        };

        var actionSteps = result.Trace.Children
            .Where(child => child.Label.StartsWith("Action Step ", StringComparison.Ordinal))
            .ToList();

        for (var index = 0; index < actionSteps.Count; index++)
        {
            var step = actionSteps[index];
            var fallback = ShouldContinue(step, index, actionSteps.Count)
                ? "continued"
                : "stopped";
            var reason = step.Reason == FailureReason.None ? string.Empty : $"; reason={step.Reason}";
            lines.Add($"{index + 1}. {FormatActionStepLabel(step.Label)}: {step.Status}{reason}; fallback={fallback}");

            AddStateLine(lines, "reads", FindStateReads(step));
            AddStateLine(lines, "writes", FindStateWrites(step));
            AddStateLine(lines, "results", FindResults(step));
        }

        lines.Add($"Terminal: {FormatTerminalStatus(result)}; {FormatTurnStatus(result)}");
        return lines;
    }

    private static string FormatPlanLabel(string label) =>
        label.StartsWith("Plan ", StringComparison.Ordinal)
            ? $"Plan {label[5..]}"
            : label;

    private static string FormatActionStepLabel(string label) =>
        label.StartsWith("Action Step ", StringComparison.Ordinal)
            ? label[12..]
            : label;

    private static bool ShouldContinue(TraceNode step, int index, int actionStepCount) =>
        index < actionStepCount - 1
        && (step.Status == TraceStatus.Failure
            || Descendants(step).Any(trace => trace.Label == "Primitive AcquireNearestTarget" && trace.Status == TraceStatus.Success));

    private static void AddStateLine(List<string> lines, string label, IReadOnlyList<string> entries)
    {
        if (entries.Count > 0)
        {
            lines.Add($"   {label}: {string.Join(", ", entries)}");
        }
    }

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
            .Where(trace => trace.Label is "Primitive Backstep" or "Primitive PickupTarget" or "Primitive AcquireNearestTarget" or "Primitive SeekTarget" or "Primitive FleeTarget" && !string.IsNullOrWhiteSpace(trace.Detail))
            .Select(trace => trace.Detail!)
            .Distinct()
            .ToList();

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

    private static string FormatTerminalStatus(PlanExecutionResult result) =>
        result.Succeeded ? "succeeded" : "failed";

    private static string FormatTurnStatus(PlanExecutionResult result) =>
        result.ConsumesTurn ? "consumed turn" : "no turn consumed";
}
