using GameGameGame.Core;

namespace GameGameGame.Tests;

public sealed class CoreActorTurnResolverTests
{
    [Fact]
    public void ResolvePlanReportsConsumingSuccessWithCanonicalTraceShape()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();

        var resolution = ActorTurnResolver.ResolvePlan(
            world,
            TestWorld.PlayerId,
            PlannedActionPlan.Single(new MoveAction(Direction.East)),
            movement);

        Assert.True(resolution.Succeeded);
        Assert.True(resolution.ConsumesTurn);
        Assert.False(resolution.ContinuePlan);
        Assert.Equal(Direction.East, resolution.ActorMovementDirection);
        Assert.Equal("Resolve plan for Player", resolution.Trace.Label);
        Assert.Equal(TraceStatus.Success, resolution.Trace.Status);
        Assert.Equal("resolved MoveAction", resolution.Trace.Detail);
        Assert.Equal("Move East", Assert.Single(resolution.Trace.Children).Label);
    }

    [Fact]
    public void ResolvePlanContinuesAfterFallthroughAndStopsAtTerminalFailure()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var plan = new PlannedActionPlan([
            new FixedIntent(new ActionResolution(false, ConsumesTurn: false, ContinuePlan: true, TraceNode.Failure("fallthrough", FailureReason.None))),
            new FixedIntent(new ActionResolution(false, ConsumesTurn: false, ContinuePlan: false, TraceNode.Failure("terminal", FailureReason.None)))
        ]);

        var resolution = ActorTurnResolver.ResolvePlan(world, TestWorld.PlayerId, plan, movement);

        Assert.False(resolution.Succeeded);
        Assert.False(resolution.ConsumesTurn);
        Assert.False(resolution.ContinuePlan);
        Assert.Equal(TraceStatus.Failure, resolution.Trace.Status);
        Assert.Equal("stopped at FixedIntent", resolution.Trace.Detail);
        Assert.Equal(["fallthrough", "terminal"], resolution.Trace.Children.Select(child => child.Label));
    }

    [Fact]
    public void ResolvePlanReportsFailureWhenNoStepConsumesOrStops()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var plan = new PlannedActionPlan([
            new FixedIntent(new ActionResolution(false, ConsumesTurn: false, ContinuePlan: true, TraceNode.Failure("first", FailureReason.None))),
            new FixedIntent(new ActionResolution(false, ConsumesTurn: false, ContinuePlan: true, TraceNode.Failure("second", FailureReason.None)))
        ]);

        var resolution = ActorTurnResolver.ResolvePlan(world, TestWorld.PlayerId, plan, movement);

        Assert.False(resolution.Succeeded);
        Assert.False(resolution.ConsumesTurn);
        Assert.False(resolution.ContinuePlan);
        Assert.Equal(TraceStatus.Failure, resolution.Trace.Status);
        Assert.Equal("no planned action could execute", resolution.Trace.Detail);
        Assert.Equal(["first", "second"], resolution.Trace.Children.Select(child => child.Label));
    }

    private sealed class FixedIntent(ActionResolution resolution) : IActionIntent
    {
        public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement) =>
            new(true, TraceNode.Success("unused"));

        public void Execute(WorldState world, EntityId actorId, MovementService movement)
        {
        }

        public ActionResolution Resolve(WorldState world, EntityId actorId, MovementService movement) => resolution;
    }
}
