namespace GameGameGame.Core;

public static class BehaviorChainTraceFormatter
{
    public static IReadOnlyList<string> Format(PlanExecutionResult result)
    {
        var lines = new List<string>
        {
            $"{FormatPlanLabel(result.Trace.Label)}: {result.Trace.Status}; consumedTurn={result.ConsumesTurn}; continuePlan={result.ContinuePlan}"
        };

        var attempts = ActionStepAttemptProjection.Project(result.Trace);

        foreach (var attempt in attempts)
        {
            var fallback = attempt.Continued ? "continued" : "stopped";
            var reason = attempt.FailureReason is null ? string.Empty : $"; reason={attempt.FailureReason}";
            lines.Add($"{attempt.Order}. {attempt.StepKind}: {attempt.Status}{reason}; fallback={fallback}");

            AddStateLine(lines, "reads", attempt.StateReads);
            AddStateLine(lines, "writes", attempt.StateWrites);
            AddStateLine(lines, "results", attempt.Results);
        }

        lines.Add($"Terminal: {FormatTerminalStatus(result)}; {FormatTurnStatus(result)}");
        return lines;
    }

    private static string FormatPlanLabel(string label) =>
        label.StartsWith("Plan ", StringComparison.Ordinal)
            ? $"Plan {label[5..]}"
            : label;

    private static void AddStateLine(List<string> lines, string label, IReadOnlyList<string> entries)
    {
        if (entries.Count > 0)
        {
            lines.Add($"   {label}: {string.Join(", ", entries)}");
        }
    }

    private static string FormatTerminalStatus(PlanExecutionResult result) =>
        result.Succeeded ? "succeeded" : "failed";

    private static string FormatTurnStatus(PlanExecutionResult result) =>
        result.ConsumesTurn ? "consumed turn" : "no turn consumed";
}
