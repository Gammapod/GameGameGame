using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class CorePushActionTests
{
    [Fact]
    public void CanonicalPushDescriptorMaterializesExecutablePush()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Target, new EntityPlanValue(TestWorld.SlimeId));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("canonical-push"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(
                    ActionPlanBehaviorStepKind.Push,
                    DirectionMode: ActionPlanMoveDirectionMode.West)
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "Action Step Push"));
    }

    [Fact]
    public void CanonicalPushMovesTargetInSelectedTargetRelativeDirection()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var action = new PushAction(TestWorld.SlimeId, Direction.West);

        var resolution = ((IActionIntent)action).Resolve(world, TestWorld.PlayerId, movement);

        Assert.True(resolution.Succeeded);
        Assert.True(resolution.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1)), world.GetEntityLocation(TestWorld.SlimeId));
    }

    [Fact]
    public void CanonicalPushDoesNotMoveActor()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var actorStart = world.GetEntityLocation(TestWorld.PlayerId);
        var action = new PushAction(TestWorld.SlimeId, Direction.West);

        var resolution = ((IActionIntent)action).Resolve(world, TestWorld.PlayerId, movement);

        Assert.True(resolution.Succeeded);
        Assert.Equal(actorStart, world.GetEntityLocation(TestWorld.PlayerId));
    }

    [Fact]
    public void CanonicalPushFailsWhenTargetBulkExceedsActorAperture()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 5 };
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Bulk = 6 };
        var action = new PushAction(TestWorld.SlimeId, Direction.West);

        var evaluation = action.Evaluate(world, TestWorld.PlayerId, movement);

        Assert.False(evaluation.CanExecute);
        Assert.Equal(FailureReason.ApertureBlocked, evaluation.Trace.Reason);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 1)), world.GetEntityLocation(TestWorld.SlimeId));
    }

    [Fact]
    public void CanonicalPushFailsWhenDestinationIsOccupied()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var action = new PushAction(TestWorld.SlimeId, Direction.East);

        var evaluation = action.Evaluate(world, TestWorld.PlayerId, movement);

        Assert.False(evaluation.CanExecute);
        Assert.Equal(FailureReason.InvalidPlacement, evaluation.Trace.Reason);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 1)), world.GetEntityLocation(TestWorld.SlimeId));
    }

    [Fact]
    public void CanonicalPushFailsWhenTargetIsNotAdjacent()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4))));
        var action = new PushAction(TestWorld.SlimeId, Direction.West);

        var evaluation = action.Evaluate(world, TestWorld.PlayerId, movement);

        Assert.False(evaluation.CanExecute);
        Assert.Equal(FailureReason.TargetNotAdjacent, evaluation.Trace.Reason);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)), world.GetEntityLocation(TestWorld.SlimeId));
    }

    private static bool TraceContains(TraceNode trace, string label) =>
        trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
}
