using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class ControlledActorCommandServiceTests
{
    [Fact]
    public void ControlledActorCommandMoveReturnsStructuredSuccessAndAdvancesTurn()
    {
        var world = TestWorld.CreateWorld();
        var service = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        var result = service.Execute(world, TestWorld.PlayerId, ControlledActorCommand.Move(Direction.East));

        Assert.Equal(TestWorld.PlayerId, result.ActorId);
        Assert.Equal(ControlledActorCommandKind.Move, result.Kind);
        Assert.Equal(Direction.East, result.Direction);
        Assert.True(result.Succeeded);
        Assert.True(result.ConsumedTurn);
        Assert.True(result.AdvancedTurn);
        Assert.Null(result.FailureReason);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.NotNull(result.Trace);
        Assert.NotNull(result.TurnReport);
        Assert.Equal(world.LastTurnReport, result.TurnReport);
    }

    [Fact]
    public void ControlledActorCommandFailedMoveRecordsFailureWithoutAdvancingTurn()
    {
        var world = TestWorld.CreateWorld();
        var service = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());
        var startTurn = world.TurnNumber;
        var startLocation = world.GetEntityLocation(TestWorld.PlayerId);

        var result = service.Execute(world, TestWorld.PlayerId, ControlledActorCommand.Move(Direction.North));

        Assert.False(result.Succeeded);
        Assert.False(result.ConsumedTurn);
        Assert.False(result.AdvancedTurn);
        Assert.Equal(FailureReason.InvalidPlacement, result.FailureReason);
        Assert.Contains("cannot place", result.FailureDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(startTurn, world.TurnNumber);
        Assert.Equal(startLocation, world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Equal(result.Trace, world.LastTrace);
        Assert.Null(result.TurnReport);
    }

    [Fact]
    public void ControlledActorCommandWaitReturnsStructuredSuccessAndAdvancesTurnWithoutMovement()
    {
        var world = TestWorld.CreateWorld();
        var service = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());
        var startTurn = world.TurnNumber;
        var startLocation = world.GetEntityLocation(TestWorld.PlayerId);

        var result = service.Execute(world, TestWorld.PlayerId, ControlledActorCommand.Wait());

        Assert.Equal(TestWorld.PlayerId, result.ActorId);
        Assert.Equal(ControlledActorCommandKind.Wait, result.Kind);
        Assert.Null(result.Direction);
        Assert.True(result.Succeeded);
        Assert.True(result.ConsumedTurn);
        Assert.True(result.AdvancedTurn);
        Assert.Null(result.FailureReason);
        Assert.Null(result.FailureDetail);
        Assert.Equal(startLocation, world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Equal(startTurn + 1, world.TurnNumber);
        Assert.NotNull(result.Trace);
        Assert.True(TraceContains(result.Trace, "Wait"));
        Assert.NotNull(result.TurnReport);
        Assert.Equal(world.LastTurnReport, result.TurnReport);
        var report = Assert.Single(result.TurnReport!.Actions);
        Assert.Equal(TestWorld.PlayerId, report.ActorId);
        Assert.Equal("Waited", report.Summary);
    }

    [Fact]
    public void ControlledActorCommandPickupReportsTargetAndDestinationAnchors()
    {
        var world = TestWorld.CreateWorld();
        var service = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());
        var destination = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0));

        var result = service.Execute(world, TestWorld.PlayerId, ControlledActorCommand.Pickup(TestWorld.SlimeId, destination));

        Assert.True(result.Succeeded);
        Assert.Equal(ControlledActorCommandKind.Pickup, result.Kind);
        Assert.Equal(TestWorld.SlimeId, result.TargetId);
        Assert.Equal(destination, result.Destination);
        Assert.Equal(destination, world.GetEntityLocation(TestWorld.SlimeId));
        Assert.True(result.AdvancedTurn);
    }

    private static bool TraceContains(TraceNode trace, string label) =>
        trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
}
