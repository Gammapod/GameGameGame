using GameGameGame.Core;

namespace GameGameGame.Tests;

public sealed class FirstSliceTests
{
    [Fact]
    public void FirstSliceWorldPlacesPlayerInGameInventoryCenter()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();

        Assert.Equal("Player@gameInventory(2,2)", world.FormatEntityAddress(WorldBuilder.PlayerId));
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
}
