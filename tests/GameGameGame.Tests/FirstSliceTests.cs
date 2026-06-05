using GameGameGame.Core;

namespace GameGameGame.Tests;

public sealed class FirstSliceTests
{
    [Fact]
    public void FirstSliceWorldPlacesPlayerInGameInventoryCenter()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();

        Assert.Equal("Player@world(1,2)", world.FormatEntityAddress(WorldBuilder.PlayerId));
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Rock@world(2,1)", world.FormatEntityAddress(WorldBuilder.RockId));
    }

    [Fact]
    public void PlayerCanMoveCardinallyInsideGameInventory()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();

        var moved = movement.TryMove(world, WorldBuilder.PlayerId, Direction.South);

        Assert.True(moved);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(WorldBuilder.PlayerId));
    }

    [Fact]
    public void PlayerCannotMoveOutsideGameInventory()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();

        movement.TryMove(world, WorldBuilder.PlayerId, Direction.West);
        var moved = movement.TryMove(world, WorldBuilder.PlayerId, Direction.West);

        Assert.False(moved);
        Assert.Equal("Player@world(0,2)", world.FormatEntityAddress(WorldBuilder.PlayerId));
    }

    [Fact]
    public void SlimePicksUpBlockingRockThenContinuesMoving()
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
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(WorldBuilder.RockId));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal(2, world.TurnNumber);
        Assert.Equal("Slime@world(0,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
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

        turns.TakePlayerTurn(world, PlannedActionPlan.Single(new MoveAction(Direction.South)));

        Assert.Equal(1, world.TurnNumber);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(WorldBuilder.PlayerId));

        // The slime wanted to move right into the rock, so it picked the rock up instead.
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(WorldBuilder.RockId));
    }

    [Fact]
    public void PlannedActionUsesFirstExecutableOption()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var turns = new TurnService(movement, new Dictionary<EntityId, IEntityBehavior>());
        movement.TryMove(world, WorldBuilder.PlayerId, Direction.South);
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

        turns.ResolvePlan(world, WorldBuilder.PlayerId, PlannedActionPlan.Single(new PickupAction(WorldBuilder.SlimeId, inventoryDestination)));
        turns.ResolvePlan(world, WorldBuilder.PlayerId, PlannedActionPlan.Single(new DropAction(WorldBuilder.SlimeId, worldDestination)));

        Assert.Equal("Slime@world(0,0)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }

    [Fact]
    public void SlimeCannotPickUpPlayerBecausePlayerIsTooHeavy()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityBehavior>
            {
                [WorldBuilder.SlimeId] = new AlternatingHorizontalBehavior()
            });

        movement.TryPlace(world, WorldBuilder.RockId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, WorldBuilder.PlayerId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(2, 1)));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Player@world(2,1)", world.FormatEntityAddress(WorldBuilder.PlayerId));
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }

    [Fact]
    public void PlayerCanPickUpAndDropRock()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var turns = new TurnService(movement, new Dictionary<EntityId, IEntityBehavior>());
        var inventoryDestination = new PlaneCoord(WorldBuilder.PlayerInventoryPlaneId, new GridCoord(0, 0));
        var worldDestination = new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 0));

        movement.TryMove(world, WorldBuilder.PlayerId, Direction.East);
        turns.ResolvePlan(world, WorldBuilder.PlayerId, PlannedActionPlan.Single(new PickupAction(WorldBuilder.RockId, inventoryDestination)));
        Assert.Equal("Rock@player(0,0)", world.FormatEntityAddress(WorldBuilder.RockId));

        turns.ResolvePlan(world, WorldBuilder.PlayerId, PlannedActionPlan.Single(new DropAction(WorldBuilder.RockId, worldDestination)));
        Assert.Equal("Rock@world(0,0)", world.FormatEntityAddress(WorldBuilder.RockId));
    }

    [Fact]
    public void PlayerCannotPickUpSlimeWhileSlimeCarriesRock()
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

        turns.AdvanceAfterPlayerTurn(world);
        var acted = turns.ResolvePlan(world, WorldBuilder.PlayerId, PlannedActionPlan.Single(new PickupAction(WorldBuilder.SlimeId, destination)));

        Assert.False(acted);
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(WorldBuilder.RockId));
    }

    [Fact]
    public void RecursiveWeightCountsCarriedInventory()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var weight = new WeightService();

        movement.TryPlace(world, WorldBuilder.RockId, new PlaneCoord(WorldBuilder.SlimeInventoryPlaneId, new GridCoord(0, 0)));

        Assert.Equal(6, weight.GetTotalWeight(world, WorldBuilder.SlimeId));
        Assert.Equal(3, weight.GetCarriedWeight(world, WorldBuilder.SlimeId));
        Assert.False(weight.CanCarry(world, WorldBuilder.PlayerId, WorldBuilder.SlimeId));
    }
}
