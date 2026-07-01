using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class ActionOutcomeProjectionTests
{
    [Fact]
    public void ActionOutcomeProjectionRendersSuccessfulPickupFromStructuredCommandResult()
    {
        var world = TestWorld.CreateWorld();
        var service = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());
        var destination = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0));

        var commandResult = service.Execute(world, TestWorld.PlayerId, ControlledActorCommand.Pickup(TestWorld.SlimeId, destination));
        var outcome = ActionOutcomeProjection.FromCommandResult(world, commandResult);

        Assert.Equal(TestWorld.PlayerId, outcome.ActorId);
        Assert.Equal(TestWorld.SlimeId, outcome.TargetId);
        Assert.Equal(destination, outcome.Destination);
        Assert.True(outcome.Succeeded);
        Assert.Equal("pickup", outcome.ActionKind);
        Assert.Equal("Player picked up Slime", outcome.Sentence);
        Assert.Contains(TestWorld.PlayerId, outcome.AnchorEntityIds);
        Assert.Contains(TestWorld.SlimeId, outcome.AnchorEntityIds);
        Assert.Equal(commandResult.Trace, outcome.Trace);
    }

    [Fact]
    public void ActionOutcomeProjectionRendersFailedMoveWithoutParsingDisplaySummary()
    {
        var world = TestWorld.CreateWorld();
        var service = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        var commandResult = service.Execute(world, TestWorld.PlayerId, ControlledActorCommand.Move(Direction.North));
        var outcome = ActionOutcomeProjection.FromCommandResult(world, commandResult);

        Assert.False(outcome.Succeeded);
        Assert.Equal("move", outcome.ActionKind);
        Assert.Equal(Direction.North, outcome.Direction);
        Assert.Equal(FailureReason.InvalidPlacement, outcome.FailureReason);
        Assert.StartsWith("Player tried to move North, but", outcome.Sentence, StringComparison.Ordinal);
        Assert.Null(commandResult.TurnReport);
    }

    [Fact]
    public void ActionLogProjectionFiltersOutcomesByEntityAndPlaneAnchors()
    {
        var world = TestWorld.CreateWorld();
        var service = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());
        var destination = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0));
        var outcome = ActionOutcomeProjection.FromCommandResult(
            world,
            service.Execute(world, TestWorld.PlayerId, ControlledActorCommand.Pickup(TestWorld.SlimeId, destination)));

        var log = ActionLogProjection.FromOutcomes([outcome]);

        Assert.Single(log.Chronological);
        Assert.Contains(outcome, log.ForEntity(TestWorld.PlayerId));
        Assert.Contains(outcome, log.ForEntity(TestWorld.SlimeId));
        Assert.Contains(outcome, log.ForPlane(TestWorld.PlayerInventoryPlaneId));
        Assert.Empty(log.ForEntity(TestWorld.RockId));
    }
}
