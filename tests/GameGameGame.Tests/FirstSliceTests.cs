using GameGameGame.Core;

namespace GameGameGame.Tests;

public sealed class FirstSliceTests
{
    [Fact]
    public void FirstSliceWorldPlacesPlayerInGameInventoryCenter()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();

        Assert.Equal("Player@gameInventory(2,2)", world.FormatEntityAddress(WorldBuilder.PlayerId));
        Assert.Equal("Slime@gameInventory(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }

    [Fact]
    public void PlayerCanMoveCardinallyInsideGameInventory()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();

        var moved = movement.TryMove(world, WorldBuilder.PlayerId, Direction.North);

        Assert.True(moved);
        Assert.Equal("Player@gameInventory(2,1)", world.FormatEntityAddress(WorldBuilder.PlayerId));
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
        Assert.Equal("Player@gameInventory(2,0)", world.FormatEntityAddress(WorldBuilder.PlayerId));
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
        Assert.Equal("Slime@gameInventory(2,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal(2, world.TurnNumber);
        Assert.Equal("Slime@gameInventory(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
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
        Assert.Equal("Player@gameInventory(2,1)", world.FormatEntityAddress(WorldBuilder.PlayerId));

        // The slime wanted to move right into the player's new node, so its planned action failed.
        Assert.Equal("Slime@gameInventory(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
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

        Assert.Equal("Slime@gameInventory(1,2)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }
}
