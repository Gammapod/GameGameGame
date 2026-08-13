using GameGameGame.Content;

namespace GameGameGame.Frontend.SadConsole;

internal sealed record ScenarioLaunchFailurePresentation(string Summary, IReadOnlyList<string> Details);

internal static class ScenarioLaunchFailurePresenter
{
    public static ScenarioLaunchFailurePresentation FromSession(PlayableScenarioSession session)
    {
        var details = session.ValidationDiagnostics
            .Select(diagnostic => $"Validation: {diagnostic}")
            .Concat(session.RuntimeFailures.Select(failure => $"Runtime: {failure}"))
            .Concat(session.CapabilityGaps.Select(gap => $"Capability gap: {gap}"))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (details.Count == 0)
        {
            details.Add("The shared launcher returned CanPlay=false without diagnostics.");
        }

        return new ScenarioLaunchFailurePresentation(
            $"Cannot play {session.Name}: {details[0]}",
            details);
    }

    public static ScenarioLaunchFailurePresentation FromException(WorkspaceScenarioCatalogEntry entry, Exception exception) => new(
        $"Launch failed for {entry.Name}: {exception.Message}",
        [$"Exception: {exception.Message}"]);
}
