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

    [Fact]
    public void ControlledActorCommandGiveOverwriteAssignsCarriedProviderToAdjacentTarget()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var inventoryDestination = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0));
        Assert.True(movement.TryRelocate(world, TestWorld.RockId, MovementDestination.Plane(inventoryDestination)));
        var service = new ControlledActorCommandService(movement, new Dictionary<EntityId, IEntityActionPlan>());

        var result = service.Execute(world, TestWorld.PlayerId, ControlledActorCommand.GiveOverwrite(TestWorld.RockId, TestWorld.SlimeId));

        Assert.True(result.Succeeded);
        Assert.Equal(ControlledActorCommandKind.GiveOverwrite, result.Kind);
        Assert.Equal(TestWorld.RockId, result.TargetId);
        Assert.Equal(TestWorld.SlimeId, result.SecondaryTargetId);
        Assert.Equal(TestWorld.RockId, world.GetBehaviorProvider(TestWorld.SlimeId));
        Assert.True(result.AdvancedTurn);
    }

    [Fact]
    public void ControlledActorCommandTakeOverwriteClearsProviderAndKeepsOrReturnsItToInventory()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var inventoryDestination = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0));
        Assert.True(movement.TryRelocate(world, TestWorld.RockId, MovementDestination.Plane(inventoryDestination)));
        world.SetBehaviorProvider(TestWorld.SlimeId, TestWorld.RockId);
        var service = new ControlledActorCommandService(movement, new Dictionary<EntityId, IEntityActionPlan>());

        var result = service.Execute(world, TestWorld.PlayerId, ControlledActorCommand.TakeOverwrite(TestWorld.SlimeId, inventoryDestination));

        Assert.True(result.Succeeded);
        Assert.Equal(ControlledActorCommandKind.TakeOverwrite, result.Kind);
        Assert.Equal(TestWorld.SlimeId, result.TargetId);
        Assert.Equal(TestWorld.RockId, result.SecondaryTargetId);
        Assert.Null(world.GetBehaviorProvider(TestWorld.SlimeId));
        Assert.Equal(inventoryDestination, world.GetEntityLocation(TestWorld.RockId));
        Assert.True(result.AdvancedTurn);
    }

    [Fact]
    public void ControlledActorCommandGiveOverwriteFailsWhenProviderIsNotCarried()
    {
        var world = TestWorld.CreateWorld();
        var service = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        var result = service.Execute(world, TestWorld.PlayerId, ControlledActorCommand.GiveOverwrite(TestWorld.RockId, TestWorld.SlimeId));

        Assert.False(result.Succeeded);
        Assert.False(result.AdvancedTurn);
        Assert.Null(world.GetBehaviorProvider(TestWorld.SlimeId));
    }
}
