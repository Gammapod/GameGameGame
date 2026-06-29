using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Headless;
using HeadlessScenarioRunReport = GameGameGame.Headless.ScenarioRunReport;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class BetaContentFixtureTests
{
    [Fact]
    public void TurnShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("DirectionTransforms", "TurnShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-turn-showcase"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(7, 2)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaTurnRoom"), TurnCount: 1)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("leftTurner"), new EntityId("rightTurner"), new EntityId("reverser")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. TurnLeft: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("reads: Facing=North", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("writes: Facing=West", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("1. TurnRight: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.Contains("reads: Facing=North", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.Contains("writes: Facing=East", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("1. ReverseFacing: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.Contains("reads: Facing=East", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.Contains("writes: Facing=West", StringComparison.Ordinal));
        Assert.Contains("Left Turner: scenarioRoot(1,1), facing West, target none", report.FinalStateLines);
        Assert.Contains("Right Turner: scenarioRoot(3,1), facing East, target none", report.FinalStateLines);
        Assert.Contains("Reverser: scenarioRoot(5,1), facing West, target none", report.FinalStateLines);
        Assert.Empty(report.RuntimeObservations);
    }

    [Fact]
    public void BackstepShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("DirectionTransforms", "BackstepShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-backstep-showcase"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(7, 2)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaBackstepRoom"), TurnCount: 1)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("successBackstepper"), new EntityId("blockedBackstepper"), new EntityId("edgeBackstepper")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. Backstep: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("reads: Facing=North", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("moved South; preserved Facing=North", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("1. Backstep: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.Contains("writes: Target=backstepBlocker", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("1. Backstep: Failure", StringComparison.Ordinal));
        Assert.DoesNotContain(report.Turns[2].TraceLines, line => line.Contains("writes: Target=", StringComparison.Ordinal));
        Assert.Contains("Successful Backstepper: scenarioRoot(1,2), facing North, target none", report.FinalStateLines);
        Assert.Contains("Blocked Backstepper: scenarioRoot(3,1), facing North, target backstepBlocker", report.FinalStateLines);
        Assert.Contains("Backstep Blocker: scenarioRoot(3,2), facing none, target none", report.FinalStateLines);
        Assert.Contains("Edge Backstepper: scenarioRoot(5,4), facing North, target none", report.FinalStateLines);
        Assert.Contains(report.RuntimeObservations, observation => observation.Contains("Blocked Backstepper could not act", StringComparison.Ordinal));
        Assert.Contains(report.RuntimeObservations, observation => observation.Contains("Edge Backstepper could not act", StringComparison.Ordinal));
    }

    [Fact]
    public void WallBounceShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("DirectionTransforms", "WallBounceShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-wall-bounce"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(7, 2)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaWallBounceRoom"), TurnCount: 3)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("wallBouncer")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. MoveFacing: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("reads: Facing=East", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("1. MoveFacing: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.Contains("writes: Target=bounceWall", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("2. ReverseFacing: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.Contains("reads: Facing=East", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.Contains("writes: Facing=West", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("1. MoveFacing: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.Contains("reads: Facing=West", StringComparison.Ordinal));
        Assert.Contains("Wall Bouncer: scenarioRoot(2,2), facing West, target bounceWall", report.FinalStateLines);
        Assert.Contains("Bounce Wall: scenarioRoot(4,2), facing none, target none", report.FinalStateLines);
        Assert.Empty(report.RuntimeObservations);
    }

    [Fact]
    public void PatrolTurnShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("DirectionTransforms", "PatrolTurnShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-patrol-turn"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(8, 5)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaPatrolTurnRoom"), TurnCount: 3)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("rightPatroller"), new EntityId("leftPatroller")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. MoveFacing: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("reads: Facing=East", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("1. MoveFacing: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.Contains("reads: Facing=East", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("1. MoveFacing: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.Contains("writes: Target=rightPatrolWall", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("2. TurnRight: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.Contains("writes: Facing=South", StringComparison.Ordinal));
        Assert.Contains(report.Turns[3].TraceLines, line => line.StartsWith("1. MoveFacing: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[3].TraceLines, line => line.Contains("writes: Target=leftPatrolWall", StringComparison.Ordinal));
        Assert.Contains(report.Turns[3].TraceLines, line => line.StartsWith("2. TurnLeft: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[3].TraceLines, line => line.Contains("writes: Facing=North", StringComparison.Ordinal));
        Assert.Contains(report.Turns[4].TraceLines, line => line.StartsWith("1. MoveFacing: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[4].TraceLines, line => line.Contains("reads: Facing=South", StringComparison.Ordinal));
        Assert.Contains(report.Turns[5].TraceLines, line => line.StartsWith("1. MoveFacing: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[5].TraceLines, line => line.Contains("reads: Facing=North", StringComparison.Ordinal));
        Assert.Contains("Right-Turn Patroller: scenarioRoot(2,2), facing South, target rightPatrolWall", report.FinalStateLines);
        Assert.Contains("Left-Turn Patroller: scenarioRoot(6,2), facing North, target leftPatrolWall", report.FinalStateLines);
        Assert.Contains("Right Patrol Wall: scenarioRoot(3,1), facing none, target none", report.FinalStateLines);
        Assert.Contains("Left Patrol Wall: scenarioRoot(7,3), facing none, target none", report.FinalStateLines);
        Assert.Empty(report.RuntimeObservations);
    }

    [Fact]
    public void AcquireTargetShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("Targeting", "AcquireTargetShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-acquire-target-showcase"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(8, 5)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaAcquireTargetRoom"), TurnCount: 1)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("targetAcquirer")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. AcquireNearestTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("writes: Target=westTieTarget", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("distance=2", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("tieBreak=row-major", StringComparison.Ordinal));
        Assert.Contains("Target Acquirer: scenarioRoot(4,2), facing West, target westTieTarget", report.FinalStateLines);
        Assert.Contains("Target Beacon: scenarioRoot(3,1), facing none, target none", report.FinalStateLines);
        Assert.Contains("Target Beacon: scenarioRoot(5,1), facing none, target none", report.FinalStateLines);
        Assert.Contains("Target Beacon: scenarioRoot(7,3), facing none, target none", report.FinalStateLines);
    }

    [Fact]
    public void DirectChaseShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("Targeting", "DirectChaseShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-direct-chase"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(8, 4)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaDirectChaseRoom"), TurnCount: 3)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("directChaser")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. AcquireNearestTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("writes: Target=directChaseTarget", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. SeekTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("moved East toward directChaseTarget", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("1. AcquireNearestTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("2. SeekTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("1. AcquireNearestTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("2. SeekTarget: Success", StringComparison.Ordinal));
        Assert.Contains("Direct Chaser: scenarioRoot(4,2), facing East, target directChaseTarget", report.FinalStateLines);
        Assert.Contains("Direct Chase Target: scenarioRoot(6,2), facing none, target none", report.FinalStateLines);
        Assert.Empty(report.RuntimeObservations);
    }

    [Fact]
    public void TargetedDestroyerShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("Targeting", "TargetedDestroyerShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-targeted-destroyer"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(8, 4)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaTargetedDestroyerRoom"), TurnCount: 3)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("targetedDestroyer")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. AcquireNearestTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("writes: Target=destructibleTarget", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. SeekTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("1. AcquireNearestTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("2. SeekTarget: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("3. DestroyTarget: Success", StringComparison.Ordinal));
        Assert.Contains("Targeted Destroyer: scenarioRoot(3,2), facing East, target destructibleTarget", report.FinalStateLines);
        Assert.DoesNotContain(report.FinalStateLines, line => line.StartsWith("Destructible Target:", StringComparison.Ordinal));
        Assert.Empty(report.RuntimeObservations);
    }

    [Fact]
    public void CollectorShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("Targeting", "CollectorShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-collector"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(6, 2)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaCollectorRoom"), TurnCount: 3)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("collector")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. AcquireNearestTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("writes: Target=collectibleGem", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. SeekTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("1. AcquireNearestTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("2. SeekTarget: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("3. PickupTarget: Success", StringComparison.Ordinal));
        Assert.Contains("Collector: scenarioRoot(3,2), facing East, target collectibleGem", report.FinalStateLines);
        Assert.DoesNotContain(report.FinalStateLines, line => line.StartsWith("Collectible Gem:", StringComparison.Ordinal));
        Assert.Empty(report.RuntimeObservations);

        var playableMaterialization = ScenarioMaterializer.Materialize(api.Document, "beta-collector");
        Assert.True(playableMaterialization.CanPlay, string.Join(Environment.NewLine, playableMaterialization.ValidationDiagnostics.Concat(playableMaterialization.RuntimeFailures)));

        var turnService = new TurnService(new MovementService(), playableMaterialization.ActionPlans);
        for (var i = 0; i < 6; i++)
        {
            turnService.AdvanceAfterPlayerTurn(playableMaterialization.World);
        }

        Assert.Equal(new PlaneCoord(new PlaneId("collector"), new GridCoord(0, 0)), playableMaterialization.World.GetEntityLocation(new EntityId("collectibleGem")));
        Assert.Equal(new PlaneCoord(new PlaneId("collector"), new GridCoord(1, 0)), playableMaterialization.World.GetEntityLocation(new EntityId("betaPlayer")));
        Assert.Equal(new EntityId("betaPlayer"), playableMaterialization.World.GetActionTarget(new EntityId("collector")));
    }

    [Fact]
    public void FleeTargetShowcaseValidatesMaterializesRunsAndRecords()
    {
        var api = OpenBetaContent("DistanceMovement", "FleeTargetShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-flee-target"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(8, 4)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaFleeTargetRoom"), TurnCount: 3)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("fleeingPrey")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. AcquireNearestTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("writes: Target=fleeBeacon", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. FleeTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("moved North away from fleeBeacon", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("2. FleeTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("2. FleeTarget: Success", StringComparison.Ordinal));
        Assert.Contains("Fleeing Prey: scenarioRoot(6,0), facing East, target fleeBeacon", report.FinalStateLines);
        Assert.Contains("Flee Beacon: scenarioRoot(4,2), facing none, target none", report.FinalStateLines);
        Assert.Empty(report.RuntimeObservations);
    }

    [Fact]
    public void DistanceTwoShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("DistanceMovement", "DistanceTwoShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-distance-two"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(18, 6)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaDistanceTwoRoom"), TurnCount: 1)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("tooCloseMaintainer"), new EntityId("tooFarMaintainer"), new EntityId("idealMaintainer")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());

        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. AcquireNearestTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("writes: Target=tooCloseBeacon", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. MaintainChebyshevDistanceTwo: Success", StringComparison.Ordinal));

        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("1. AcquireNearestTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.Contains("writes: Target=tooFarBeacon", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("2. MaintainChebyshevDistanceTwo: Success", StringComparison.Ordinal));

        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("1. AcquireNearestTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.Contains("writes: Target=idealBeacon", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("2. MaintainChebyshevDistanceTwo: Failure", StringComparison.Ordinal));

        Assert.Contains("Distance-Two Maintainer: scenarioRoot(2,1), facing East, target tooCloseBeacon", report.FinalStateLines);
        Assert.Contains("Distance-Two Maintainer: scenarioRoot(8,3), facing East, target tooFarBeacon", report.FinalStateLines);
        Assert.Contains("Distance-Two Maintainer: scenarioRoot(14,2), facing East, target idealBeacon", report.FinalStateLines);
        Assert.Contains("Distance-Two Beacon: scenarioRoot(2,3), facing none, target none", report.FinalStateLines);
        Assert.Contains("Distance-Two Beacon: scenarioRoot(8,6), facing none, target none", report.FinalStateLines);
        Assert.Contains("Distance-Two Beacon: scenarioRoot(16,2), facing none, target none", report.FinalStateLines);
        Assert.Contains(report.RuntimeObservations, observation => observation.Contains("Distance-Two Maintainer could not act", StringComparison.Ordinal));
    }

    [Fact]
    public void StrafeClockwiseShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("DistanceMovement", "StrafeClockwiseShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-strafe-clockwise"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(8, 6)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaStrafeClockwiseRoom"), TurnCount: 3)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("clockwiseStrafer")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. AcquireNearestTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("writes: Target=clockwiseStrafeBeacon", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. StrafeClockwise: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("primary=West", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("moved North strafing clockwise", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("2. StrafeClockwise: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("2. StrafeClockwise: Success", StringComparison.Ordinal));
        Assert.Contains("Clockwise Strafer: scenarioRoot(3,2), facing West, target clockwiseStrafeBeacon", report.FinalStateLines);
        Assert.Contains("Strafe Beacon: scenarioRoot(3,3), facing none, target none", report.FinalStateLines);
        Assert.Empty(report.RuntimeObservations);
    }

    [Fact]
    public void StrafeAnticlockwiseShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("DistanceMovement", "StrafeAnticlockwiseShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-strafe-anticlockwise"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(8, 6)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaStrafeAnticlockwiseRoom"), TurnCount: 3)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("anticlockwiseStrafer")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. AcquireNearestTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("writes: Target=anticlockwiseStrafeBeacon", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. StrafeAnticlockwise: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("primary=West", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("moved South strafing anticlockwise", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("2. StrafeAnticlockwise: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("2. StrafeAnticlockwise: Success", StringComparison.Ordinal));
        Assert.Contains("Anticlockwise Strafer: scenarioRoot(3,4), facing West, target anticlockwiseStrafeBeacon", report.FinalStateLines);
        Assert.Contains("Strafe Beacon: scenarioRoot(3,3), facing none, target none", report.FinalStateLines);
        Assert.Empty(report.RuntimeObservations);
    }

    [Fact]
    public void KitingOrbiterShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("DistanceMovement", "KitingOrbiterShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-kiting-orbiter"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(23, 6)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaKitingOrbiterRoom"), TurnCount: 1)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("anticlockwiseFallbackOrbiter"), new EntityId("closeOrbiter"), new EntityId("clockwiseOrbiter")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());

        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("writes: Target=anticlockwiseFallbackTarget", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. MaintainChebyshevDistanceTwo: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("3. StrafeClockwise: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("4. StrafeAnticlockwise: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("moved South strafing anticlockwise", StringComparison.Ordinal));

        Assert.Contains(report.Turns[1].TraceLines, line => line.Contains("writes: Target=closeOrbiterTarget", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("2. MaintainChebyshevDistanceTwo: Success", StringComparison.Ordinal));
        Assert.DoesNotContain(report.Turns[1].TraceLines, line => line.StartsWith("3. StrafeClockwise:", StringComparison.Ordinal));

        Assert.Contains(report.Turns[2].TraceLines, line => line.Contains("writes: Target=clockwiseOrbiterTarget", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("2. MaintainChebyshevDistanceTwo: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("3. StrafeClockwise: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.Contains("moved North strafing clockwise", StringComparison.Ordinal));

        Assert.Contains("Kiting Orbiter: scenarioRoot(17,1), facing West, target anticlockwiseFallbackTarget", report.FinalStateLines);
        Assert.Contains("Kiting Orbiter: scenarioRoot(2,1), facing West, target closeOrbiterTarget", report.FinalStateLines);
        Assert.Contains("Kiting Orbiter: scenarioRoot(10,2), facing West, target clockwiseOrbiterTarget", report.FinalStateLines);
        Assert.Empty(report.RuntimeObservations);
    }

    [Fact]
    public void KitingOrbiterFallbackLaneValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("DistanceMovement", "KitingOrbiterShowcase.yaml");

        var materialization = AssertSuccess(api.MaterializeScenario("beta-kiting-orbiter-fallback-lane"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(4, 0)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaKitingOrbiterFallbackLane"), TurnCount: 1)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("fleeFallbackOrbiter"), new EntityId("seekFallbackOrbiter")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());

        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("writes: Target=fleeFallbackTarget", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. MaintainChebyshevDistanceTwo: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("3. StrafeClockwise: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("4. StrafeAnticlockwise: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("5. FleeTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.Contains("moved East away from fleeFallbackTarget", StringComparison.Ordinal));

        Assert.Contains(report.Turns[1].TraceLines, line => line.Contains("writes: Target=seekFallbackTarget", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("2. MaintainChebyshevDistanceTwo: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("3. StrafeClockwise: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("4. StrafeAnticlockwise: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("5. FleeTarget: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("6. SeekTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.Contains("moved West toward seekFallbackTarget", StringComparison.Ordinal));

        Assert.Contains("Kiting Orbiter: scenarioRoot(3,0), facing West, target fleeFallbackTarget", report.FinalStateLines);
        Assert.Contains("Kiting Orbiter: scenarioRoot(6,0), facing West, target seekFallbackTarget", report.FinalStateLines);
        Assert.Empty(report.RuntimeObservations);
    }

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

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaPushRoom"), TurnCount: 1)));

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

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaDestroyRoom"), TurnCount: 1)));

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

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaCreateRoom"), TurnCount: 1)));

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

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaDropRoom"), TurnCount: 1)));

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

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaWeightRoom"), TurnCount: 1)));

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

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaChainRoom"), TurnCount: 1)));

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

    [Fact]
    public void PassiveChestTransferShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("Transfer", "PassiveChestTransferShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-passive-chest-transfer"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(7, 2)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaPassiveChestTransferRoom"), TurnCount: 3)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("chestRunner")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. SeekTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("3. GiveTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("4. TakeTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.Contains("gave offeringGem", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.Contains("took chestCoin", StringComparison.Ordinal));
        Assert.Contains("Chest Runner: scenarioRoot(2,2), facing East, target passiveChest", report.FinalStateLines);
        Assert.Contains("Passive Chest: scenarioRoot(3,2), facing none, target none", report.FinalStateLines);
        Assert.DoesNotContain(report.FinalStateLines, line => line.StartsWith("Chest Coin:", StringComparison.Ordinal));
        Assert.DoesNotContain(report.FinalStateLines, line => line.StartsWith("Offering Gem:", StringComparison.Ordinal));
        Assert.Contains("Chest Runner inventory:", report.InventorySummaryLines);
        Assert.Contains("  - Chest Coin chestCoin at (0,0)", report.InventorySummaryLines);
        Assert.Contains("Passive Chest inventory:", report.InventorySummaryLines);
        Assert.Contains("  - Offering Gem offeringGem at (1,0)", report.InventorySummaryLines);
        Assert.Empty(report.RuntimeObservations);
    }

    [Fact]
    public void StealingActorShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("Transfer", "StealingActorShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-stealing-actor"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(8, 1)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaStealingActorRoom"), TurnCount: 4)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("sneakyThief")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. SeekTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("2. SeekTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("2. SeekTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[3].TraceLines, line => line.StartsWith("2. SeekTarget: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[3].TraceLines, line => line.StartsWith("3. TakeTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[3].TraceLines, line => line.Contains("took stolenRuby", StringComparison.Ordinal));
        Assert.Contains("Sneaky Thief: scenarioRoot(4,1), facing East, target treasureVictim", report.FinalStateLines);
        Assert.Contains("Treasure Victim: scenarioRoot(5,1), facing none, target none", report.FinalStateLines);
        Assert.DoesNotContain(report.FinalStateLines, line => line.StartsWith("Stolen Ruby:", StringComparison.Ordinal));
        Assert.Empty(report.RuntimeObservations);
    }

    [Fact]
    public void FeedingOfferingShowcaseValidatesMaterializesAndRuns()
    {
        var api = OpenBetaContent("Transfer", "FeedingOfferingShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-feeding-offering"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(8, 1)), materialization.PlayerLocation);

        var report = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaFeedingOfferingRoom"), TurnCount: 4)));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Equal([new EntityId("offeringBearer")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("2. SeekTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("2. SeekTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[2].TraceLines, line => line.StartsWith("2. SeekTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[3].TraceLines, line => line.StartsWith("2. SeekTarget: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[3].TraceLines, line => line.StartsWith("3. GiveTarget: Success", StringComparison.Ordinal));
        Assert.Contains(report.Turns[3].TraceLines, line => line.Contains("gave sweetBerry", StringComparison.Ordinal));
        Assert.Contains("Offering Bearer: scenarioRoot(4,1), facing East, target hungryBeast", report.FinalStateLines);
        Assert.Contains("Hungry Beast: scenarioRoot(5,1), facing none, target none", report.FinalStateLines);
        Assert.DoesNotContain(report.FinalStateLines, line => line.StartsWith("Sweet Berry:", StringComparison.Ordinal));
        Assert.Empty(report.RuntimeObservations);
    }

    [Fact]
    public void CollectorTraderHandoffShowcaseValidatesMaterializesAndRunsPersistedScenario()
    {
        var api = OpenBetaContent("Transfer", "CollectorTraderHandoffShowcase.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        var materialization = AssertSuccess(api.MaterializeScenario("beta-collector-trader-handoff"));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(3, 1)), materialization.PlayerLocation);

        var turns = new TurnService(new MovementService(), materialization.ActionPlans);
        for (var turn = 0; turn < 6; turn++)
        {
            turns.AdvanceAfterPlayerTurn(materialization.World);
        }

        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(7, 1)), materialization.World.GetEntityLocation(new EntityId("betaPlayer")));
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(5, 1)), materialization.World.GetEntityLocation(new EntityId("handoffCollector")));
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(6, 1)), materialization.World.GetEntityLocation(new EntityId("handoffTrader")));
        Assert.Equal(new EntityId("handoffTrader"), materialization.World.GetActionTarget(new EntityId("handoffCollector")));
        Assert.Contains(materialization.World.LastTurnReport!.Actions, action => action.ActorId == new EntityId("handoffCollector") && action.Summary.Contains("GiveTarget", StringComparison.Ordinal));
        Assert.Contains(materialization.World.LastTurnReport!.Actions, action => action.ActorId == new EntityId("handoffTrader") && action.Summary.Contains("DropFacing", StringComparison.Ordinal));
    }

    [Fact]
    public void EnterExitShowcasesValidateMaterializeAndRun()
    {
        var api = OpenBetaContent("Containment", "EnterExitShowcases.yaml");

        var snapshot = api.GetDocumentSnapshot();
        Assert.True(snapshot.Validation.IsValid, string.Join(Environment.NewLine, snapshot.Validation.Errors));
        Assert.True(snapshot.CanonicalValidation.IsValid, string.Join(Environment.NewLine, snapshot.CanonicalValidation.Errors));

        foreach (var scenarioId in new[]
        {
            "beta-enter-exit-manual",
            "beta-auto-enter-box",
            "beta-too-bulky-enter",
            "beta-nested-aperture-success",
            "beta-nested-aperture-blocked",
            "beta-exit-practice",
            "beta-mouse-crack"
        })
        {
            var materialization = AssertSuccess(api.MaterializeScenario(scenarioId));
            Assert.Empty(materialization.ValidationDiagnostics);
            Assert.Empty(materialization.RuntimeFailures);
        }

        var enterReport = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaAutoEnterRoom"), TurnCount: 1)));
        Assert.Empty(enterReport.ValidationDiagnostics);
        Assert.Empty(enterReport.RuntimeFailures);
        Assert.Contains(enterReport.Turns[0].TraceLines, line => line.StartsWith("2. EnterTarget: Success", StringComparison.Ordinal));
        Assert.Contains("Open Box inventory:", enterReport.InventorySummaryLines);
        Assert.Contains("  - Enter Crawler autoCrawler at (0,0)", enterReport.InventorySummaryLines);

        var bulkyReport = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaTooBulkyEnterRoom"), TurnCount: 1)));
        Assert.Empty(bulkyReport.ValidationDiagnostics);
        Assert.Empty(bulkyReport.RuntimeFailures);
        Assert.Contains(bulkyReport.Turns[0].TraceLines, line => line.StartsWith("2. EnterTarget: Failure; reason=ApertureBlocked", StringComparison.Ordinal));
        Assert.Contains(bulkyReport.RuntimeObservations, observation => observation.Contains("Bulky Crawler could not act", StringComparison.Ordinal));

        var nestedSuccessReport = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaNestedApertureSuccessRoom"), TurnCount: 1)));
        Assert.Empty(nestedSuccessReport.ValidationDiagnostics);
        Assert.Empty(nestedSuccessReport.RuntimeFailures);
        Assert.Contains(nestedSuccessReport.Turns[0].TraceLines, line => line.StartsWith("2. EnterTarget: Success", StringComparison.Ordinal));
        Assert.Contains("Open Box inventory:", nestedSuccessReport.InventorySummaryLines);
        Assert.Contains("  - Nested Crawler nestedCrawler at (0,0)", nestedSuccessReport.InventorySummaryLines);

        var nestedBlockedReport = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaNestedApertureBlockedRoom"), TurnCount: 1)));
        Assert.Empty(nestedBlockedReport.ValidationDiagnostics);
        Assert.Empty(nestedBlockedReport.RuntimeFailures);
        Assert.Contains(nestedBlockedReport.Turns[0].TraceLines, line => line.StartsWith("2. EnterTarget: Failure; reason=ApertureBlocked", StringComparison.Ordinal));
        Assert.Contains(nestedBlockedReport.RuntimeObservations, observation => observation.Contains("Nested Crawler could not act", StringComparison.Ordinal));

        var mouseReport = AssertSuccess(api.RunScenario(new ScenarioRunRequest(new EntityTemplateId("betaMouseCrackRoom"), TurnCount: 1)));
        Assert.Empty(mouseReport.ValidationDiagnostics);
        Assert.Empty(mouseReport.RuntimeFailures);
        Assert.Contains(mouseReport.Turns[0].TraceLines, line => line.StartsWith("2. EnterTarget: Success", StringComparison.Ordinal));
        Assert.Contains("Mouse Crack inventory:", mouseReport.InventorySummaryLines);
        Assert.Contains("  - Tiny Mouse tinyMouse at (0,0)", mouseReport.InventorySummaryLines);
    }

    private static BetaContentApi OpenBetaContent(string group, string fileName)
    {
        var path = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Beta", group, fileName));
        return new BetaContentApi(EditableContentDocument.LoadYaml(File.ReadAllText(path)));
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

    private static T AssertSuccess<T>(T result) => result;

    private sealed class BetaContentApi(EditableContentDocument document)
    {
        public EditableContentDocument Document { get; } = document;

        public BetaDocumentSnapshot GetDocumentSnapshot() =>
            new(Document.ToRegistry().Validate(), Document.ValidateCanonicalAuthoring());

        public ScenarioMaterializationResult MaterializeScenario(string scenarioId) =>
            ScenarioMaterializer.Materialize(Document, scenarioId);

        public HeadlessScenarioRunReport RunScenario(ScenarioRunRequest request) =>
            ScenarioRunService.Run(Document, request);
    }

    private sealed record BetaDocumentSnapshot(
        ContentValidationResult Validation,
        ContentValidationResult CanonicalValidation);
}
