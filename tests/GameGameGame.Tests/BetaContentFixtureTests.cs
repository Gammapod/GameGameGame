using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Editor;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class BetaContentFixtureTests
{
    [Fact]
    public void PushFacingShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("CurrentTools", "PushFacingShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-push-showcase"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(7, 2)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new AgentScenarioRunRequest(new EntityTemplateId("betaPushRoom"), TurnCount: 1)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("successPusher"), new EntityId("blockedPusher")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. PushFacing: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("1. PushFacing: Failure", StringComparison.Ordinal));
        Assert.Contains("Successful Pusher: scenarioRoot(2,1), facing East, target none", report.FinalStateLines);
        Assert.Contains("Success Block: scenarioRoot(3,1), facing none, target none", report.FinalStateLines);
        Assert.Contains("Blocked Pusher: scenarioRoot(1,3), facing East, target none", report.FinalStateLines);
        Assert.Contains("Front Block: scenarioRoot(2,3), facing none, target none", report.FinalStateLines);
        Assert.Contains("Rear Block: scenarioRoot(3,3), facing none, target none", report.FinalStateLines);
        Assert.Contains(report.RuntimeObservations, observation => observation.Contains("Blocked Pusher could not act", StringComparison.Ordinal));
    }

    [Fact]
    public void DestroyTargetShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("CurrentTools", "DestroyTargetShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-destroy-showcase"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(7, 2)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new AgentScenarioRunRequest(new EntityTemplateId("betaDestroyRoom"), TurnCount: 1)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("clearingDestroyer"), new EntityId("selfDestroyAttempt")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. MoveFacing: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("writes: Target=fragileWall", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. DestroyTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("1. DestroyTarget: Failure", StringComparison.Ordinal));
        Assert.Contains("Clearing Destroyer: scenarioRoot(1,1), facing East, target fragileWall", report.FinalStateLines);
        Assert.DoesNotContain(report.FinalStateLines, line => line.StartsWith("Fragile Wall:", StringComparison.Ordinal));
        Assert.Contains("Self Destroy Attempt: scenarioRoot(1,3), facing none, target none", report.FinalStateLines);
        Assert.Contains(report.RuntimeObservations, observation => observation.Contains("Self Destroy Attempt could not act", StringComparison.Ordinal));
    }

    private static AgentContentEditorApi OpenBetaContent(string group, string fileName)
    {
        var path = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Beta", group, fileName));
        return AssertSuccess(AgentContentEditorApi.OpenFile(path));
    }

    private static string FindRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.GetFullPath(relativePath);
    }

    private static T AssertSuccess<T>(AgentApiResult<T> result)
    {
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value!;
    }
}
