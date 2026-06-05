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
        Assert.Equal("Giant Slime@world(3,3)", world.FormatEntityAddress(WorldBuilder.GiantSlimeId));
        Assert.Equal("Rock@world(2,1)", world.FormatEntityAddress(WorldBuilder.RockId));
    }

    [Fact]
    public void FirstSliceWorldDefinesInventoryDimensionsOnEntities()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();

        Assert.Equal(3, world.Entities[WorldBuilder.PlayerId].InventoryWidth);
        Assert.Equal(2, world.Entities[WorldBuilder.PlayerId].InventoryHeight);
        Assert.True(world.Entities[WorldBuilder.PlayerId].HasUsableInventory);

        Assert.Equal(1, world.Entities[WorldBuilder.SlimeId].InventoryWidth);
        Assert.Equal(1, world.Entities[WorldBuilder.SlimeId].InventoryHeight);
        Assert.True(world.Entities[WorldBuilder.SlimeId].HasUsableInventory);

        Assert.Equal(3, world.Entities[WorldBuilder.GiantSlimeId].InventoryWidth);
        Assert.Equal(3, world.Entities[WorldBuilder.GiantSlimeId].InventoryHeight);
        Assert.Equal(20, world.Entities[WorldBuilder.GiantSlimeId].Weight);
        Assert.Equal(20, world.Entities[WorldBuilder.GiantSlimeId].CarryingCapacity);
        Assert.True(world.Entities[WorldBuilder.GiantSlimeId].HasUsableInventory);

        Assert.Equal(0, world.Entities[WorldBuilder.RockId].InventoryWidth);
        Assert.Equal(0, world.Entities[WorldBuilder.RockId].InventoryHeight);
        Assert.False(world.Entities[WorldBuilder.RockId].HasUsableInventory);
    }

    [Fact]
    public void InspectionPanelIncludesEntityPropertiesAndInventoryGrid()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var inspector = new EntityInspectionService();

        var panel = inspector.Inspect(world, WorldBuilder.PlayerId);

        Assert.Equal(WorldBuilder.PlayerId, panel.EntityId);
        Assert.Equal("Player", panel.Name);
        Assert.Contains(panel.Properties, property => property.Name == "Weight" && property.Value == "10");
        Assert.Contains(panel.Properties, property => property.Name == "Inventory Dimensions" && property.Value == "3x2");
        Assert.NotNull(panel.InventoryGrid);
        Assert.Equal(WorldBuilder.PlayerInventoryPlaneId, panel.InventoryGrid.PlaneId);
        Assert.Equal(6, panel.InventoryGrid.Cells.Count);
    }

    [Fact]
    public void GiantSlimeInspectionShowsLargeInventoryAndProperties()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var inspector = new EntityInspectionService();

        var panel = inspector.Inspect(world, WorldBuilder.GiantSlimeId);

        Assert.Equal(WorldBuilder.GiantSlimeId, panel.EntityId);
        Assert.Equal("Giant Slime", panel.Name);
        Assert.Contains(panel.Properties, property => property.Name == "Weight" && property.Value == "20");
        Assert.Contains(panel.Properties, property => property.Name == "Carrying Capacity" && property.Value == "20");
        Assert.Contains(panel.Properties, property => property.Name == "Inventory Dimensions" && property.Value == "3x3");
        Assert.NotNull(panel.InventoryGrid);
        Assert.Equal(WorldBuilder.GiantSlimeInventoryPlaneId, panel.InventoryGrid.PlaneId);
        Assert.Equal(9, panel.InventoryGrid.Cells.Count);
    }

    [Fact]
    public void GiantSlimeCanUseWanderingSlimeActionPlan()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.GiantSlimeId] = new WanderingSlimeActionPlan()
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Giant Slime@world(2,3)", world.FormatEntityAddress(WorldBuilder.GiantSlimeId));
    }

    [Fact]
    public void InspectionPanelShowsInventoryOccupants()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var inspector = new EntityInspectionService();

        movement.TryPlace(world, WorldBuilder.RockId, new PlaneCoord(WorldBuilder.PlayerInventoryPlaneId, new GridCoord(0, 0)));

        var panel = inspector.Inspect(world, WorldBuilder.PlayerId);

        Assert.NotNull(panel.InventoryGrid);
        Assert.Contains(panel.InventoryGrid.Cells, cell =>
            cell.Coord == new GridCoord(0, 0)
            && cell.EntityId == WorldBuilder.RockId
            && cell.Glyph == '*');
    }

    [Fact]
    public void InspectionServiceFindsEntityContainingPlane()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var inspector = new EntityInspectionService();

        var containerId = inspector.FindEntityContainingPlane(world, WorldBuilder.GameInventoryPlaneId);

        Assert.Equal(WorldBuilder.GameId, containerId);
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
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = new WanderingSlimeActionPlan()
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal(1, world.TurnNumber);
        Assert.Equal("Slime@world(0,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Rock@world(2,1)", world.FormatEntityAddress(WorldBuilder.RockId));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal(2, world.TurnNumber);
        Assert.Equal("Slime@world(0,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }

    [Fact]
    public void WanderingSlimePicksUpCarryableObjectBlockingFacingDirection()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = new WanderingSlimeActionPlan()
            });

        movement.TryPlace(world, WorldBuilder.RockId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 1)));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(WorldBuilder.RockId));
    }

    [Fact]
    public void WanderingSlimeBumpsUncarryableBlockerAndSetsFacingRight()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var plan = new WanderingSlimeActionPlan();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = plan
            });

        movement.TryPlace(world, WorldBuilder.RockId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(4, 4)));
        movement.TryPlace(world, WorldBuilder.PlayerId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 1)));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal(Direction.East, plan.Facing);

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Slime@world(2,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }

    [Fact]
    public void TurnServiceResolvesPlayerPlannedActionBeforeSlimeAction()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = new WanderingSlimeActionPlan()
            });

        turns.TakePlayerTurn(world, PlannedActionPlan.Single(new MoveAction(Direction.South)));

        Assert.Equal(1, world.TurnNumber);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(WorldBuilder.PlayerId));

        Assert.Equal("Slime@world(0,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Rock@world(2,1)", world.FormatEntityAddress(WorldBuilder.RockId));
    }

    [Fact]
    public void PlannedActionUsesFirstExecutableOption()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var turns = new TurnService(movement, new Dictionary<EntityId, IEntityActionPlan>());
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
        var turns = new TurnService(movement, new Dictionary<EntityId, IEntityActionPlan>());
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
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = new WanderingSlimeActionPlan(Direction.East)
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
        var turns = new TurnService(movement, new Dictionary<EntityId, IEntityActionPlan>());
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
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = new WanderingSlimeActionPlan(Direction.East)
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
        var turns = new TurnService(movement, new Dictionary<EntityId, IEntityActionPlan>());
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
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = new WanderingSlimeActionPlan(Direction.East)
            });
        var destination = new PlaneCoord(WorldBuilder.PlayerInventoryPlaneId, new GridCoord(0, 0));

        turns.AdvanceAfterPlayerTurn(world);
        var acted = turns.ResolvePlan(world, WorldBuilder.PlayerId, PlannedActionPlan.Single(new PickupAction(WorldBuilder.SlimeId, destination)));

        Assert.False(acted);
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(WorldBuilder.RockId));
    }

    [Fact]
    public void PickupEvaluationExplainsCapacityFailureWithNestedWeightTrace()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var action = new PickupAction(
            WorldBuilder.SlimeId,
            new PlaneCoord(WorldBuilder.PlayerInventoryPlaneId, new GridCoord(0, 0)));

        movement.TryPlace(world, WorldBuilder.RockId, new PlaneCoord(WorldBuilder.SlimeInventoryPlaneId, new GridCoord(0, 0)));

        var evaluation = action.Evaluate(world, WorldBuilder.PlayerId, movement);

        Assert.False(evaluation.CanExecute);
        Assert.Equal(FailureReason.CapacityExceeded, evaluation.Trace.Reason);
        Assert.Contains("6/5", evaluation.Trace.Detail);
        Assert.Contains(evaluation.Trace.Children, child => child.Label == "Check carrying capacity");
        Assert.True(TraceContains(evaluation.Trace, "Total weight of Slime"));
        Assert.True(TraceContains(evaluation.Trace, "Total weight of Rock"));
    }

    [Fact]
    public void PickupEvaluationExplainsInvalidPlacementSeparatelyFromWeight()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var action = new PickupAction(
            WorldBuilder.SlimeId,
            new PlaneCoord(WorldBuilder.PlayerInventoryPlaneId, new GridCoord(0, 0)));

        movement.TryPlace(world, WorldBuilder.RockId, new PlaneCoord(WorldBuilder.PlayerInventoryPlaneId, new GridCoord(0, 0)));

        var evaluation = action.Evaluate(world, WorldBuilder.PlayerId, movement);

        Assert.False(evaluation.CanExecute);
        Assert.Equal(FailureReason.InvalidPlacement, evaluation.Trace.Reason);
    }

    [Fact]
    public void PickupEvaluationFailsWhenActorInventoryDimensionsAreUnusable()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var rock = world.Entities[WorldBuilder.RockId];
        var rockInventoryPlaneId = new PlaneId("rock");
        world.Entities[WorldBuilder.RockId] = rock with { InventoryPlaneId = rockInventoryPlaneId };
        var action = new PickupAction(
            WorldBuilder.SlimeId,
            new PlaneCoord(rockInventoryPlaneId, new GridCoord(0, 0)));

        var evaluation = action.Evaluate(world, WorldBuilder.RockId, movement);

        Assert.False(evaluation.CanExecute);
        Assert.Equal(FailureReason.ActorInventoryUnusable, evaluation.Trace.Reason);
    }

    [Fact]
    public void ResolvePlanRecordsFailedOptionThenSuccessfulFallback()
    {
        var world = WorldBuilder.CreateFirstSliceWorld();
        var movement = new MovementService();
        var turns = new TurnService(movement, new Dictionary<EntityId, IEntityActionPlan>());
        var plan = new PlannedActionPlan([
            new MoveAction(Direction.East),
            new MoveAction(Direction.West)
        ]);

        var acted = turns.ResolvePlan(world, WorldBuilder.SlimeId, plan);

        Assert.True(acted);
        Assert.NotNull(world.LastTrace);
        Assert.Equal(TraceStatus.Success, world.LastTrace.Status);
        Assert.Contains(world.LastTrace.Children, child => child.Reason == FailureReason.MoveBlocked);
        Assert.Contains(world.LastTrace.Children, child => child.Status == TraceStatus.Success);
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

    private static bool TraceContains(TraceNode trace, string label)
    {
        return trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
    }
}
