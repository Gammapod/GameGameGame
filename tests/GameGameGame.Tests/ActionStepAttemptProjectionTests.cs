using GameGameGame.Core;

namespace GameGameGame.Tests;

public sealed class ActionStepAttemptProjectionTests
{
    [Fact]
    public void ProjectExtractsFailedContinuedAndSuccessfulStoppedActionStepAttempts()
    {
        var trace = new TraceNode("Plan test", TraceStatus.Success);
        var failed = new TraceNode("Action Step Backstep", TraceStatus.Failure, FailureReason.MoveBlocked, "blocked by slime");
        failed.Add(TraceNode.Success("Read slot Facing", "East"));
        failed.Add(TraceNode.Success("Set slot Target", "slime"));
        failed.Add(TraceNode.Failure("Primitive Backstep", FailureReason.MoveBlocked, "blocked by slime"));
        var succeeded = new TraceNode("Action Step PickupTarget", TraceStatus.Success);
        succeeded.Add(TraceNode.Success("Read slot Target", "slime"));
        succeeded.Add(TraceNode.Success("Primitive PickupTarget", "picked up slime"));
        trace.Add(failed);
        trace.Add(succeeded);

        var attempts = ActionStepAttemptProjection.Project(trace);

        Assert.Collection(
            attempts,
            attempt =>
            {
                Assert.Equal(1, attempt.Order);
                Assert.Equal("Backstep", attempt.StepKind);
                Assert.Equal(TraceStatus.Failure, attempt.Status);
                Assert.Equal(FailureReason.MoveBlocked, attempt.FailureReason);
                Assert.Equal("blocked by slime", attempt.Detail);
                Assert.True(attempt.Continued);
                Assert.False(attempt.Stopped);
                Assert.Equal(["Facing=East"], attempt.StateReads);
                Assert.Equal(["Target=slime"], attempt.StateWrites);
                Assert.Equal(["blocked by slime"], attempt.Results);
                Assert.Same(failed, attempt.Trace);
            },
            attempt =>
            {
                Assert.Equal(2, attempt.Order);
                Assert.Equal("PickupTarget", attempt.StepKind);
                Assert.Equal(TraceStatus.Success, attempt.Status);
                Assert.Null(attempt.FailureReason);
                Assert.False(attempt.Continued);
                Assert.True(attempt.Stopped);
                Assert.Equal(["Target=slime"], attempt.StateReads);
                Assert.Empty(attempt.StateWrites);
                Assert.Equal(["picked up slime"], attempt.Results);
                Assert.Same(succeeded, attempt.Trace);
            });
    }

    [Fact]
    public void ProjectIgnoresNonActionStepTraceChildren()
    {
        var trace = new TraceNode("Plan test", TraceStatus.Success);
        trace.Add(TraceNode.Info("Read slot Facing", "East"));
        trace.Add(new TraceNode("Action Step Wait", TraceStatus.Success));

        var attempt = Assert.Single(ActionStepAttemptProjection.Project(trace));

        Assert.Equal("Wait", attempt.StepKind);
    }

    [Fact]
    public void ProjectIncludesCanonicalTransferResultDetails()
    {
        var trace = new TraceNode("Plan test", TraceStatus.Success);
        var transfer = new TraceNode("Action Step Transfer", TraceStatus.Failure, FailureReason.TargetMissing, "missing target label carriedScrap");
        transfer.Add(TraceNode.Failure("Primitive Transfer", FailureReason.TargetMissing, "missing target label carriedScrap"));
        trace.Add(transfer);

        var attempt = Assert.Single(ActionStepAttemptProjection.Project(trace));

        Assert.Equal("Transfer", attempt.StepKind);
        Assert.Equal(FailureReason.TargetMissing, attempt.FailureReason);
        Assert.Equal(["missing target label carriedScrap"], attempt.Results);
    }
}
