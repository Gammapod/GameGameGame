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

    [Fact]
    public void CreateFacingShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("CurrentTools", "CreateFacingShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-create-showcase"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(7, 2)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new AgentScenarioRunRequest(new EntityTemplateId("betaCreateRoom"), TurnCount: 1)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("openCreator"), new EntityId("blockedCreator")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. CreateFacing: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("reads: Facing=East", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("1. CreateFacing: Failure", StringComparison.Ordinal));
        Assert.Contains("Open Creator: scenarioRoot(1,1), facing East, target none", report.FinalStateLines);
        Assert.Contains("Placeholder Rock: scenarioRoot(2,1), facing none, target none", report.FinalStateLines);
        Assert.Contains("Blocked Creator: scenarioRoot(1,3), facing East, target none", report.FinalStateLines);
        Assert.Contains("Occupied Cell Blocker: scenarioRoot(2,3), facing none, target none", report.FinalStateLines);
        Assert.Contains(report.RuntimeObservations, observation => observation.Contains("Blocked Creator could not act", StringComparison.Ordinal));
    }

    [Fact]
    public void DropFacingShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("CurrentTools", "DropFacingShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-drop-showcase"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(7, 2)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new AgentScenarioRunRequest(new EntityTemplateId("betaDropRoom"), TurnCount: 1)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("successDropper"), new EntityId("emptyDropper"), new EntityId("blockedDropper")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. DropFacing: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("reads: Facing=East", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("1. DropFacing: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("1. DropFacing: Failure", StringComparison.Ordinal));
        Assert.Contains("Successful Dropper: scenarioRoot(1,1), facing East, target none", report.FinalStateLines);
        Assert.Contains("Dropped Pebble: scenarioRoot(2,1), facing none, target none", report.FinalStateLines);
        Assert.Contains("Empty Dropper: scenarioRoot(5,1), facing East, target none", report.FinalStateLines);
        Assert.Contains("Blocked Dropper: scenarioRoot(1,3), facing East, target none", report.FinalStateLines);
        Assert.Contains("Drop Blocker: scenarioRoot(2,3), facing none, target none", report.FinalStateLines);
        Assert.DoesNotContain(report.FinalStateLines, line => line.StartsWith("Carried Pebble:", StringComparison.Ordinal));
        Assert.Contains(report.RuntimeObservations, observation => observation.Contains("Empty Dropper could not act", StringComparison.Ordinal));
        Assert.Contains(report.RuntimeObservations, observation => observation.Contains("Blocked Dropper could not act", StringComparison.Ordinal));
    }

    [Fact]
    public void PickupDropWeightShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("CurrentTools", "PickupDropWeightShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-pickup-drop-weight"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(7, 2)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new AgentScenarioRunRequest(new EntityTemplateId("betaWeightRoom"), TurnCount: 1)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("lightTester"), new EntityId("heavyTester")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. MoveFacing: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("writes: Target=lightPebble", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. PickupTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("1. MoveFacing: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.Contains("writes: Target=heavyBoulder", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("2. PickupTarget: Failure", StringComparison.Ordinal));
        Assert.Contains("Light Pickup Tester: scenarioRoot(1,1), facing East, target lightPebble", report.FinalStateLines);
        Assert.DoesNotContain(report.FinalStateLines, line => line.StartsWith("Light Pebble:", StringComparison.Ordinal));
        Assert.Contains("Heavy Pickup Tester: scenarioRoot(1,3), facing East, target heavyBoulder", report.FinalStateLines);
        Assert.Contains("Heavy Boulder: scenarioRoot(2,3), facing none, target none", report.FinalStateLines);
        Assert.Contains("Manual Pebble: scenarioRoot(6,1), facing none, target none", report.FinalStateLines);
        Assert.Contains("Manual Boulder: scenarioRoot(6,3), facing none, target none", report.FinalStateLines);
        Assert.Contains(report.RuntimeObservations, observation => observation.Contains("Heavy Pickup Tester could not act", StringComparison.Ordinal));
    }

    [Fact]
    public void BehaviorChainCompositionShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("CurrentTools", "BehaviorChainCompositionShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-behavior-chain-composition"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(7, 2)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new AgentScenarioRunRequest(new EntityTemplateId("betaChainRoom"), TurnCount: 1)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("chainPusher"), new EntityId("chainDestroyer"), new EntityId("chainCollector")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. MoveFacing: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. PushFacing: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("2. DestroyTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("2. PickupTarget: Success", StringComparison.Ordinal));
        Assert.Contains("Chain Pusher: scenarioRoot(2,1), facing East, target chainPushBlock", report.FinalStateLines);
        Assert.Contains("Chain Push Block: scenarioRoot(3,1), facing none, target none", report.FinalStateLines);
        Assert.Contains("Chain Destroyer: scenarioRoot(1,2), facing East, target chainFragileWall", report.FinalStateLines);
        Assert.DoesNotContain(report.FinalStateLines, line => line.StartsWith("Chain Fragile Wall:", StringComparison.Ordinal));
        Assert.Contains("Chain Collector: scenarioRoot(1,3), facing East, target chainCollectible", report.FinalStateLines);
        Assert.DoesNotContain(report.FinalStateLines, line => line.StartsWith("Chain Collectible:", StringComparison.Ordinal));
        Assert.Empty(report.RuntimeObservations);
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
