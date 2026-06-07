using GameGameGame.Core;
using GameGameGame.Content;
using WorldBuilder = GameGameGame.Content.PrototypeContent;

namespace GameGameGame.Tests;

public sealed class FirstSliceTests
{
    [Fact]
    public void FirstSliceWorldPlacesPlayerInGameInventoryCenter()
    {
        var world = WorldBuilder.CreateFirstSlice().World;

        Assert.Equal("Player@world(1,2)", world.FormatEntityAddress(WorldBuilder.PlayerId));
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Giant Slime@world(3,3)", world.FormatEntityAddress(WorldBuilder.GiantSlimeId));
        Assert.Equal("Rock@world(2,1)", world.FormatEntityAddress(WorldBuilder.RockId));
    }

    [Fact]
    public void FirstSliceWorldDefinesInventoryDimensionsOnEntities()
    {
        var world = WorldBuilder.CreateFirstSlice().World;

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
        var world = WorldBuilder.CreateFirstSlice().World;
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
        var world = WorldBuilder.CreateFirstSlice().World;
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
    public void GiantSlimeCanUseContentDefinedWanderingPlan()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.GiantSlimeId] = CreateWanderingActionPlan()
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Giant Slime@world(2,3)", world.FormatEntityAddress(WorldBuilder.GiantSlimeId));
    }

    [Fact]
    public void PrototypeActionPlansUseInterpretedPlansForSlimes()
    {
        var plans = CreatePrototypeActionPlans();

        Assert.IsType<InterpretedEntityActionPlan>(plans[WorldBuilder.SlimeId]);
        Assert.IsType<InterpretedEntityActionPlan>(plans[WorldBuilder.GiantSlimeId]);
    }

    [Fact]
    public void PrototypeInterpretedPlansPreserveSlimeAndGiantSlimeMovement()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var turns = new TurnService(movement, CreatePrototypeActionPlans());

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Slime@world(0,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Giant Slime@world(2,3)", world.FormatEntityAddress(WorldBuilder.GiantSlimeId));
        Assert.True(TraceContains(world.LastTrace!, "Plan wandering"));
    }

    [Fact]
    public void CreateFirstSliceReturnsWorldAndPrototypeActionPlansTogether()
    {
        var slice = WorldBuilder.CreateFirstSlice();
        var turns = new TurnService(new MovementService(), slice.ActionPlans);

        turns.AdvanceAfterPlayerTurn(slice.World);

        Assert.Equal("Slime@world(0,1)", slice.World.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Giant Slime@world(2,3)", slice.World.FormatEntityAddress(WorldBuilder.GiantSlimeId));
        Assert.IsType<InterpretedEntityActionPlan>(slice.ActionPlans[WorldBuilder.SlimeId]);
        Assert.IsType<InterpretedEntityActionPlan>(slice.ActionPlans[WorldBuilder.GiantSlimeId]);
    }

    [Fact]
    public void PrototypeInterpretedSlimePlanPicksUpBlockingCarryableTarget()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var turns = new TurnService(movement, CreatePrototypeActionPlans());

        movement.TryPlace(world, WorldBuilder.RockId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 1)));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(WorldBuilder.RockId));
        Assert.True(TraceContains(world.LastTrace!, "Call plan handleBlocker"));
    }

    [Fact]
    public void SpawnEntityFromRockTemplateCreatesRockInstanceAtRequestedLocation()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var registry = WorldBuilder.CreateRegistry();
        var spawnId = new EntityId("spawnedRock");

        var result = registry.SpawnEntity(
            world,
            WorldBuilder.RockTemplateId,
            new EntitySpawnOptions(spawnId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 0))));

        Assert.Equal(spawnId, result.EntityId);
        Assert.Null(result.ActionPlan);
        Assert.Equal("Rock@world(0,0)", world.FormatEntityAddress(spawnId));
        Assert.Equal(0, world.Entities[spawnId].InventoryWidth);
        Assert.Equal(0, world.Entities[spawnId].InventoryHeight);
        Assert.Equal(3, world.Entities[spawnId].Weight);
        Assert.Equal(3, world.Entities[spawnId].CarryingCapacity);
    }

    [Fact]
    public void PrototypeContentExposesTemplatesForFirstSliceEntityTypes()
    {
        Assert.Equal("Game", WorldBuilder.CreateGameTemplate().Name);
        Assert.Equal("Player", WorldBuilder.CreatePlayerTemplate().Name);
        Assert.Equal("Slime", WorldBuilder.CreateSlimeTemplate().Name);
        Assert.Equal("Giant Slime", WorldBuilder.CreateGiantSlimeTemplate().Name);
        Assert.Equal("Rock", WorldBuilder.CreateRockTemplate().Name);

        Assert.Equal(5, WorldBuilder.CreateGameTemplate().InventoryWidth);
        Assert.Equal(3, WorldBuilder.CreatePlayerTemplate().InventoryWidth);
        Assert.Equal(1, WorldBuilder.CreateSlimeTemplate().InventoryWidth);
        Assert.Equal(3, WorldBuilder.CreateGiantSlimeTemplate().InventoryWidth);
        Assert.Equal(0, WorldBuilder.CreateRockTemplate().InventoryWidth);
    }

    [Fact]
    public void SpawnEntityCanOverrideTemplateProperties()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var registry = WorldBuilder.CreateRegistry();
        var spawnId = new EntityId("heavyRock");

        registry.SpawnEntity(
            world,
            WorldBuilder.RockTemplateId,
            new EntitySpawnOptions(
                spawnId,
                new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 0)),
                ModifyTemplate: template => template with
                {
                    Name = "Heavy Rock",
                    Weight = 9,
                    CarryingCapacity = 0
                }));

        Assert.Equal("Heavy Rock@world(0,0)", world.FormatEntityAddress(spawnId));
        Assert.Equal(9, world.Entities[spawnId].Weight);
        Assert.Equal(0, world.Entities[spawnId].CarryingCapacity);
    }

    [Fact]
    public void SpawnEntityCanOverrideInventoryPlaneIdentity()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var registry = WorldBuilder.CreateRegistry();
        var spawnId = new EntityId("customInventoryCarrier");
        var inventoryPlaneId = new PlaneId("customInventory");

        registry.SpawnEntity(
            world,
            WorldBuilder.SlimeTemplateId,
            new EntitySpawnOptions(
                spawnId,
                new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 0)),
                InventoryPlaneId: inventoryPlaneId,
                InventoryPlaneName: "Custom Inventory"));

        Assert.Equal(inventoryPlaneId, world.GetInventoryPlaneId(spawnId));
        Assert.Equal("Custom Inventory", world.Planes[inventoryPlaneId].Name);
    }

    [Fact]
    public void SpawnEntityCanOverrideRockTemplateWithWanderingActionPlan()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var registry = WorldBuilder.CreateRegistry();
        var movement = new MovementService();
        var spawnId = new EntityId("wanderingRock");
        var result = registry.SpawnEntity(
            world,
            WorldBuilder.RockTemplateId,
            new EntitySpawnOptions(
                spawnId,
                new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(4, 4)),
                ActionPlanOverrideId: WorldBuilder.WanderingActionPlanTemplateId,
                PlanVariableOverrides: new Dictionary<string, PlanValueDescriptor>
                {
                    ["facing"] = PlanValueDescriptor.Direction(Direction.West)
                }));
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [spawnId] = result.ActionPlan!
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.NotNull(result.ActionPlan);
        Assert.Equal("Rock@world(3,4)", world.FormatEntityAddress(spawnId));
        Assert.True(TraceContains(world.LastTrace!, "Plan wandering"));
    }

    [Fact]
    public void SpawnEntityFromInventoryTemplateMaterializesOwnedInventoryPlane()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var registry = WorldBuilder.CreateRegistry();
        var bagId = new EntityId("bag");
        var template = new EntityTemplate(
            "Bag",
            InventoryWidth: 2,
            InventoryHeight: 1,
            Weight: 1,
            CarryingCapacity: 10);
        var templateId = new EntityTemplateId("bag");
        registry = registry.WithEntityTemplate(templateId, template);

        registry.SpawnEntity(
            world,
            templateId,
            new EntitySpawnOptions(bagId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 0))));

        var inventoryPlaneId = world.GetInventoryPlaneId(bagId);

        Assert.NotNull(inventoryPlaneId);
        Assert.Equal(new PlaneId("bag"), inventoryPlaneId);
        Assert.Equal(2, world.Planes[inventoryPlaneId.Value].Width);
        Assert.Equal(1, world.Planes[inventoryPlaneId.Value].Height);
    }

    [Fact]
    public void SpawnEntityPlacesTemplateCarriedEntitiesIntoInventoryLayout()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var registry = WorldBuilder.CreateRegistry();
        var bagId = new EntityId("loadedBag");
        var carriedRockId = new EntityId("carriedRock");
        var template = new EntityTemplate(
            "Loaded Bag",
            InventoryWidth: 2,
            InventoryHeight: 1,
            Weight: 1,
            CarryingCapacity: 10,
            CarriedEntities:
            [
                new CarriedEntityTemplate(carriedRockId, WorldBuilder.RockTemplateId, new GridCoord(1, 0))
            ]);
        var templateId = new EntityTemplateId("loadedBag");
        registry = registry.WithEntityTemplate(templateId, template);

        registry.SpawnEntity(
            world,
            templateId,
            new EntitySpawnOptions(bagId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 0))));

        Assert.Equal("Loaded Bag@world(0,0)", world.FormatEntityAddress(bagId));
        Assert.Equal("Rock@loadedBag(1,0)", world.FormatEntityAddress(carriedRockId));
    }

    [Fact]
    public void InspectionPanelShowsInventoryOccupants()
    {
        var slice = WorldBuilder.CreateFirstSlice();
        var world = slice.World;
        var movement = new MovementService();
        var registry = slice.Registry;
        var inspector = new EntityInspectionService(entityId => registry.GetPresentationForEntity(entityId).ToInspectionAppearance());

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
        var world = WorldBuilder.CreateFirstSlice().World;
        var inspector = new EntityInspectionService();

        var containerId = inspector.FindEntityContainingPlane(world, WorldBuilder.GameInventoryPlaneId);

        Assert.Equal(WorldBuilder.GameId, containerId);
    }

    [Fact]
    public void PlayerCanMoveCardinallyInsideGameInventory()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();

        var moved = movement.TryMove(world, WorldBuilder.PlayerId, Direction.South);

        Assert.True(moved);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(WorldBuilder.PlayerId));
    }

    [Fact]
    public void PlayerCannotMoveOutsideGameInventory()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();

        movement.TryMove(world, WorldBuilder.PlayerId, Direction.West);
        var moved = movement.TryMove(world, WorldBuilder.PlayerId, Direction.West);

        Assert.False(moved);
        Assert.Equal("Player@world(0,2)", world.FormatEntityAddress(WorldBuilder.PlayerId));
    }

    [Fact]
    public void SlimePicksUpBlockingRockThenContinuesMoving()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = CreateWanderingActionPlan()
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
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = CreateWanderingActionPlan()
            });

        movement.TryPlace(world, WorldBuilder.RockId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 1)));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(WorldBuilder.RockId));
    }

    [Fact]
    public void WanderingSlimeBumpsUncarryableBlockerAndSetsFacingRight()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = CreateWanderingActionPlan()
            });

        movement.TryPlace(world, WorldBuilder.RockId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(4, 4)));
        movement.TryPlace(world, WorldBuilder.PlayerId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 1)));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.True(TraceContains(world.LastTrace!, "Set variable facing"));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Slime@world(2,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }

    [Fact]
    public void TurnServiceResolvesPlayerPlannedActionBeforeSlimeAction()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = CreateWanderingActionPlan()
            });

        turns.TakeActorTurnThenAdvance(world, WorldBuilder.PlayerId, PlannedActionPlan.Single(new MoveAction(Direction.South)));

        Assert.Equal(1, world.TurnNumber);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(WorldBuilder.PlayerId));

        Assert.Equal("Slime@world(0,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Rock@world(2,1)", world.FormatEntityAddress(WorldBuilder.RockId));
    }

    [Fact]
    public void PlannedActionUsesFirstExecutableOption()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
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
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var turns = new TurnService(movement, new Dictionary<EntityId, IEntityActionPlan>());
        var destination = new PlaneCoord(WorldBuilder.PlayerInventoryPlaneId, new GridCoord(0, 0));

        turns.ResolvePlan(world, WorldBuilder.PlayerId, PlannedActionPlan.Single(new PickupAction(WorldBuilder.SlimeId, destination)));

        Assert.Equal("Slime@player(0,0)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }

    [Fact]
    public void SlimeContinuesMovingInsidePlayerInventoryAfterPickup()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = CreateWanderingActionPlan(Direction.East)
            });
        var destination = new PlaneCoord(WorldBuilder.PlayerInventoryPlaneId, new GridCoord(0, 0));

        turns.TakeActorTurnThenAdvance(world, WorldBuilder.PlayerId, PlannedActionPlan.Single(new PickupAction(WorldBuilder.SlimeId, destination)));

        Assert.Equal(1, world.TurnNumber);
        Assert.Equal("Slime@player(1,0)", world.FormatEntityAddress(WorldBuilder.SlimeId));
    }

    [Fact]
    public void PlayerCanDropSlimeFromInventoryOntoWorldPlane()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
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
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = CreateWanderingActionPlan(Direction.East)
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
        var world = WorldBuilder.CreateFirstSlice().World;
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
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = CreateWanderingActionPlan(Direction.East)
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
        var world = WorldBuilder.CreateFirstSlice().World;
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
        var world = WorldBuilder.CreateFirstSlice().World;
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
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var rock = world.Entities[WorldBuilder.RockId];
        var rockInventoryPlaneId = new PlaneId("rock");
        world.RegisterInventoryPlane(WorldBuilder.RockId, rockInventoryPlaneId);
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
        var world = WorldBuilder.CreateFirstSlice().World;
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
        var world = WorldBuilder.CreateFirstSlice().World;
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

    private static IReadOnlyDictionary<EntityId, IEntityActionPlan> CreatePrototypeActionPlans() =>
        new Dictionary<EntityId, IEntityActionPlan>
        {
            [WorldBuilder.SlimeId] = CreateWanderingActionPlan(),
            [WorldBuilder.GiantSlimeId] = CreateWanderingActionPlan()
        };

    private static IEntityActionPlan CreateWanderingActionPlan(Direction initialFacing = Direction.West) =>
        WorldBuilder.CreateRegistry().CreateActionPlan(
            WorldBuilder.WanderingActionPlanTemplateId,
            new Dictionary<string, PlanValueDescriptor>
            {
                ["facing"] = PlanValueDescriptor.Direction(initialFacing)
            });
}
