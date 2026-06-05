using GameGameGame.Core;

namespace GameGameGame.Tests;

public sealed class FirstSliceTests
{
    [Fact]
    public void FirstSliceWorldPlacesPlayerInGameInventoryCenter()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();

        Assert.Equal("Player@world(2,2)", world.FormatEntityAddress(WorldBuilder.PlayerId));
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }

    [Fact]
    public void PlayerCanMoveCardinallyInsideGameInventory()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();

        var moved = movement.TryMove(world, WorldBuilder.PlayerId, Direction.North);

        Assert.True(moved);
        Assert.Equal("Player@world(2,1)", world.FormatEntityAddress(WorldBuilder.PlayerId));
    }

    [Fact]
    public void PlayerCannotMoveOutsideGameInventory()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();

        movement.TryMove(world, WorldBuilder.PlayerId, Direction.North);
        movement.TryMove(world, WorldBuilder.PlayerId, Direction.North);
        var moved = movement.TryMove(world, WorldBuilder.PlayerId, Direction.North);

        Assert.False(moved);
        Assert.Equal("Player@world(2,0)", world.FormatEntityAddress(WorldBuilder.PlayerId));
    }

    [Fact]
    public void SlimeMovesRightOnOddTurnsAndLeftOnEvenTurns()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityBehavior>
            {
                [WorldBuilder.SlimeId] = new AlternatingHorizontalBehavior()
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal(1, world.TurnNumber);
        Assert.Equal("Slime@world(2,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal(2, world.TurnNumber);
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }

    [Fact]
    public void TurnServiceResolvesPlayerPlannedActionBeforeSlimeAction()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityBehavior>
            {
                [WorldBuilder.SlimeId] = new AlternatingHorizontalBehavior()
            });

        turns.TakePlayerTurn(world, PlannedActionPlan.Single(new MoveAction(Direction.North)));

        Assert.Equal(1, world.TurnNumber);
        Assert.Equal("Player@world(2,1)", world.FormatEntityAddress(WorldBuilder.PlayerId));

        // The slime wanted to move right into the player's new node, so its planned action failed.
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }

    [Fact]
    public void PlannedActionUsesFirstExecutableOption()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var turns = new TurnService(movement, new Dictionary<EntityId, IEntityBehavior>());
        movement.TryMove(world, WorldBuilder.PlayerId, Direction.North);
        var plan = new PlannedActionPlan([
            new MoveAction(Direction.East),
            new MoveAction(Direction.South)
        ]);

        turns.ResolvePlan(world, WorldBuilder.SlimeId, plan);

        Assert.Equal("Slime@world(1,2)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }

    [Fact]
    public void PlayerCanPickUpSlimeIntoInventory()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var turns = new TurnService(movement, new Dictionary<EntityId, IEntityBehavior>());
        var destination = new PlaneCoord(WorldBuilder.PlayerInventoryPlaneId, new GridCoord(0, 0));

        movement.TryMove(world, WorldBuilder.PlayerId, Direction.North);
        turns.ResolvePlan(world, WorldBuilder.PlayerId, PlannedActionPlan.Single(new PickupAction(WorldBuilder.SlimeId, destination)));

        Assert.Equal("Slime@player(0,0)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }

    [Fact]
    public void SlimeContinuesMovingInsidePlayerInventoryAfterPickup()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityBehavior>
            {
                [WorldBuilder.SlimeId] = new AlternatingHorizontalBehavior()
            });
        var destination = new PlaneCoord(WorldBuilder.PlayerInventoryPlaneId, new GridCoord(0, 0));

        movement.TryMove(world, WorldBuilder.PlayerId, Direction.North);
        turns.TakePlayerTurn(world, PlannedActionPlan.Single(new PickupAction(WorldBuilder.SlimeId, destination)));

        Assert.Equal(1, world.TurnNumber);
        Assert.Equal("Slime@player(1,0)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }

    [Fact]
    public void PlayerCanDropSlimeFromInventoryOntoWorldPlane()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var turns = new TurnService(movement, new Dictionary<EntityId, IEntityBehavior>());
        var inventoryDestination = new PlaneCoord(WorldBuilder.PlayerInventoryPlaneId, new GridCoord(0, 0));
        var worldDestination = new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 0));

        movement.TryMove(world, WorldBuilder.PlayerId, Direction.North);
        turns.ResolvePlan(world, WorldBuilder.PlayerId, PlannedActionPlan.Single(new PickupAction(WorldBuilder.SlimeId, inventoryDestination)));
        turns.ResolvePlan(world, WorldBuilder.PlayerId, PlannedActionPlan.Single(new DropAction(WorldBuilder.SlimeId, worldDestination)));

        Assert.Equal("Slime@world(0,0)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }
}
