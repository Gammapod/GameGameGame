using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class InventoryTransferActionStepTests
{
    [Fact]
    public void DropFacingDropsFirstCarriedEntityInFacingDirection()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("drop-facing"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DropFacing)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), world.GetEntityLocation(TestWorld.RockId));
        Assert.True(TraceContains(result.Trace, "Action Step DropFacing"));
    }

    [Fact]
    public void TransformInventoryToAdjacentBehaviorUsesDropSemantics()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        var plan = CreateBehaviorPlan("transform-inventory-to-adjacent", ActionPlanBehaviorStepKind.TransformInventoryToAdjacent);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), world.GetEntityLocation(TestWorld.RockId));
        Assert.True(TraceContains(result.Trace, "Action Step TransformInventoryToAdjacent"));
        Assert.True(TraceContains(result.Trace, "Primitive DropFacing"));
    }

    [Fact]
    public void DropFacingIgnoresActorExitPolicy()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with
        {
            ExitPolicy = EntityExitPolicy.EdgeAlignedWithExitDirection
        };
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        var plan = CreateBehaviorPlan("drop-ignores-actor-exit-policy", ActionPlanBehaviorStepKind.DropFacing);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void PickupTargetIgnoresActorEnterPolicy()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with
        {
            EnterPolicy = EntityEnterPolicy.FarthestFromOccupied
        };
        AddEntity(world, "blocker", "Blocker", new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.RockId);
        var plan = CreateBehaviorPlan("pickup-ignores-actor-enter-policy", ActionPlanBehaviorStepKind.PickupTarget);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void GiveTargetTransfersFirstCarriedEntityToTargetInventoryRowMajor()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 30 };
        var chestId = AddEntityWithInventory(world, "chest", "Chest", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)), inventoryWidth: 2, inventoryHeight: 2, carryingCapacity: 30);
        var blockerId = AddEntity(world, "blocker", "Blocker", new PlaneCoord(new PlaneId("chestInventory"), new GridCoord(0, 0)));
        var gemId = AddEntity(world, "gem", "Gem", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 4)));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0))));
        Assert.True(movement.TryPlace(world, gemId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 1))));
        world.SetActionTarget(TestWorld.PlayerId, chestId);
        var plan = CreateBehaviorPlan("give-target", ActionPlanBehaviorStepKind.GiveTarget);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(new PlaneId("chestInventory"), new GridCoord(1, 0)), world.GetEntityLocation(TestWorld.RockId));
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 1)), world.GetEntityLocation(gemId));
        Assert.Equal(new PlaneCoord(new PlaneId("chestInventory"), new GridCoord(0, 0)), world.GetEntityLocation(blockerId));
        Assert.True(TraceDetailContains(result.Trace, "gave rock (Rock) from (1,0) to (1,0)"));
    }

    [Fact]
    public void EnterTargetMovesActorIntoAdjacentTargetInventoryRowMajor()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var blockerId = AddEntity(world, "blocker", "Blocker", new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        AddPlane(world, new PlaneId("roomInventory"), 2, 2);
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { InventoryWidth = 2, InventoryHeight = 2, Aperture = 20 };
        world.RegisterInventoryPlane(TestWorld.SlimeId, new PlaneId("roomInventory"));
        Assert.True(movement.TryPlace(world, blockerId, new PlaneCoord(new PlaneId("roomInventory"), new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = CreateBehaviorPlan("enter-target", ActionPlanBehaviorStepKind.EnterTarget);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(new PlaneId("roomInventory"), new GridCoord(1, 0)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(new PlaneId("roomInventory"), new GridCoord(0, 0)), world.GetEntityLocation(blockerId));
        Assert.True(TraceDetailContains(result.Trace, "entered player (Player) into slime (Slime) at (1,0)"));
    }

    [Fact]
    public void EnterTargetReportsTargetInventoryMissingWithTargetCentricReason()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var doorwayId = AddEntity(world, "doorway", "Doorway", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)));
        world.SetActionTarget(TestWorld.PlayerId, doorwayId);
        var plan = CreateBehaviorPlan("enter-target", ActionPlanBehaviorStepKind.EnterTarget);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(TraceHasReason(result.Trace, FailureReason.TargetHasNoInventory));
        Assert.True(TraceDetailContains(result.Trace, "target doorway (Doorway) has no inventory plane"));
    }

    [Fact]
    public void EnterTargetReportsTargetInventoryUnusableWithTargetCentricReason()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { InventoryWidth = 0, InventoryHeight = 1 };
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = CreateBehaviorPlan("enter-target", ActionPlanBehaviorStepKind.EnterTarget);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(TraceHasReason(result.Trace, FailureReason.TargetInventoryUnusable));
        Assert.True(TraceDetailContains(result.Trace, "target slime (Slime) inventory dimensions are 0x1"));
    }

    [Fact]
    public void ExitFacingMovesActorOutOfContainingInventoryToAdjacentContainerCell()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.SetActionFacing(TestWorld.PlayerId, Direction.South);
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        var plan = CreateBehaviorPlan("exit-facing", ActionPlanBehaviorStepKind.ExitFacing);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.True(TraceDetailContains(result.Trace, "exited player (Player) from slime (Slime) to (1,2)"));
    }

    [Fact]
    public void ExitFacingFromMergedLayerUsesCurrentContributionOwner()
    {
        var world = TestWorld.CreateWorld();
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("shared-interior"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(3, 0))
            ]));
        world.SourceCellLinks.Add(new SourceCellLink(
            new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0)),
            Direction.East,
            new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)),
            Direction.West));
        var movement = new MovementService();
        world.SetActionFacing(TestWorld.RockId, Direction.East);
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0))));
        Assert.True(movement.TryMove(world, TestWorld.RockId, Direction.East));
        var plan = CreateBehaviorPlan("exit-facing-merged", ActionPlanBehaviorStepKind.ExitFacing);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.RockId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 1)), world.GetEntityLocation(TestWorld.RockId));
        Assert.True(TraceDetailContains(result.Trace, "exited rock (Rock) from slime (Slime) to (2,1)"));
    }

    [Fact]
    public void EnterAndExitActionsAreUsableAsPlayerActionIntents()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Aperture = 20 };
        IActionIntent enter = new EnterAction(TestWorld.SlimeId);

        var enterResolution = enter.Resolve(world, TestWorld.PlayerId, movement);

        Assert.True(enterResolution.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.PlayerId));

        IActionIntent exit = new ExitAction(Direction.South);
        var exitResolution = exit.Resolve(world, TestWorld.PlayerId, movement);

        Assert.True(exitResolution.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), world.GetEntityLocation(TestWorld.PlayerId));
    }

    [Fact]
    public void TakeTargetTransfersFirstTargetInventoryEntityToActorInventoryRowMajor()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 30 };
        var blockerId = AddEntity(world, "blocker", "Blocker", new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = CreateBehaviorPlan("take-target", ActionPlanBehaviorStepKind.TakeTarget);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0)), world.GetEntityLocation(TestWorld.RockId));
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(blockerId));
        Assert.True(TraceDetailContains(result.Trace, "took rock (Rock) from (0,0) to (1,0)"));
    }

    [Fact]
    public void CanonicalTransferActorToTargetUsesSelectedMovingEntityAndFacingCounterparty()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var gemId = AddEntity(world, "gem", "Gem", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 4)));
        Assert.True(movement.TryPlace(world, gemId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 1))));

        var result = ((IActionIntent)new TransferAction(TransferDirection.ActorToTarget, TestWorld.RockId, Direction.North))
            .Resolve(world, TestWorld.PlayerId, movement);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(gemId));
        Assert.True(TraceDetailContains(result.Trace, "gave rock (Rock) to slime (Slime) slot (0,0)"));
    }

    [Fact]
    public void CanonicalTransferActorToTargetDoesNotInvokeActorExitPolicy()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with
        {
            ExitPolicy = EntityExitPolicy.EdgeAlignedWithExitDirection
        };
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 1))));

        var result = ((IActionIntent)new TransferAction(TransferDirection.ActorToTarget, TestWorld.RockId, Direction.North))
            .Resolve(world, TestWorld.PlayerId, movement);

        Assert.True(result.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void CanonicalTransferFailureKeepsSelectedItemInSourceSlotWithoutIntermediateWorldPlacement()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var blockerId = AddEntity(world, "blocker", "Blocker", new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 1))));

        var result = ((IActionIntent)new TransferAction(TransferDirection.ActorToTarget, TestWorld.RockId, Direction.North))
            .Resolve(world, TestWorld.PlayerId, movement);

        Assert.False(result.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 1)), world.GetEntityLocation(TestWorld.RockId));
        Assert.Equal(new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(blockerId));
        Assert.True(TraceHasReason(result.Trace, FailureReason.InvalidPlacement));
    }

    [Fact]
    public void CanonicalTransferTargetToActorUsesSelectedMovingEntityFromAdjacentHolder()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var coinId = AddEntity(world, "coin", "Coin", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 4)));
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with
        {
            InventoryWidth = 2,
            InventoryHeight = 1,
            Aperture = 20
        };
        var slimeInventory = new PlaneId("slimeInventory2");
        AddPlane(world, slimeInventory, 2, 1);
        world.RegisterInventoryPlane(TestWorld.SlimeId, slimeInventory);
        Assert.True(movement.TryPlace(world, coinId, new PlaneCoord(slimeInventory, new GridCoord(0, 0))));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(slimeInventory, new GridCoord(1, 0))));

        var result = ((IActionIntent)new TransferAction(TransferDirection.TargetToActor, TestWorld.RockId, Direction.North))
            .Resolve(world, TestWorld.PlayerId, movement);

        Assert.True(result.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
        Assert.Equal(new PlaneCoord(slimeInventory, new GridCoord(0, 0)), world.GetEntityLocation(coinId));
        Assert.True(TraceDetailContains(result.Trace, "took rock (Rock) to player (Player) slot (0,0)"));
    }

    [Fact]
    public void CanonicalTransferActorToTargetReportsDestinationApertureFailure()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Aperture = 1 };
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));

        var result = ((IActionIntent)new TransferAction(TransferDirection.ActorToTarget, TestWorld.RockId, Direction.North))
            .Resolve(world, TestWorld.PlayerId, movement);

        Assert.False(result.Succeeded);
        Assert.True(TraceHasReason(result.Trace, FailureReason.ApertureBlocked));
        Assert.True(TraceDetailContains(result.Trace, "Rock bulk 3 exceeds Slime aperture 1"));
    }

    [Fact]
    public void CanonicalTransferBehaviorStepReadsTargetDirectionModeAndTransferDirection()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        world.SetActionTarget(TestWorld.PlayerId, "offers", TestWorld.RockId);
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("transfer"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(
                    ActionPlanBehaviorStepKind.Transfer,
                    TargetLabel: "offers",
                    DirectionMode: ActionPlanMoveDirectionMode.Forward,
                    TransferDirection: TransferDirection.ActorToTarget)
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
        Assert.True(TraceContains(result.Trace, "Action Step Transfer"));
    }

    [Fact]
    public void CanonicalTransferTargetToActorReportsActorInventoryFullSeparatelyFromExitPolicyFailure()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                AddEntity(world, $"blocker-{x}-{y}", "Blocker", new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(x, y)));
            }
        }

        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));

        var result = ((IActionIntent)new TransferAction(TransferDirection.TargetToActor, TestWorld.RockId, Direction.North))
            .Resolve(world, TestWorld.PlayerId, movement);

        Assert.False(result.Succeeded);
        Assert.True(TraceHasReason(result.Trace, FailureReason.InvalidPlacement));
        Assert.True(TraceDetailContains(result.Trace, "no inventory coordinate in player can accept rock"));
    }

    [Fact]
    public void CanonicalTransferTargetToActorDoesNotInvokeActorEnterPolicy()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with
        {
            EnterPolicy = EntityEnterPolicy.FarthestFromOccupied
        };
        AddEntity(world, "blocker", "Blocker", new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));

        var result = ((IActionIntent)new TransferAction(TransferDirection.TargetToActor, TestWorld.RockId, Direction.North))
            .Resolve(world, TestWorld.PlayerId, movement);

        Assert.True(result.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void CanonicalTransferTargetToActorFailsWhenSourceExitPolicyRejectsItem()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with
        {
            InventoryWidth = 3,
            InventoryHeight = 3,
            Aperture = 20,
            ExitPolicy = EntityExitPolicy.EdgeAlignedWithExitDirection
        };
        var slimeInventory = new PlaneId("slimeInventory3");
        AddPlane(world, slimeInventory, 3, 3);
        world.RegisterInventoryPlane(TestWorld.SlimeId, slimeInventory);
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(slimeInventory, new GridCoord(1, 1))));

        var result = ((IActionIntent)new TransferAction(TransferDirection.TargetToActor, TestWorld.RockId, Direction.North))
            .Resolve(world, TestWorld.PlayerId, movement);

        Assert.False(result.Succeeded);
        Assert.Equal(new PlaneCoord(slimeInventory, new GridCoord(1, 1)), world.GetEntityLocation(TestWorld.RockId));
        Assert.True(TraceHasReason(result.Trace, FailureReason.InventoryPolicyBlocked));
    }

    [Fact]
    public void FarthestFromOccupiedEnterPolicyChoosesFarthestEmptyCellWithRowMajorTieBreak()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with
        {
            InventoryWidth = 3,
            InventoryHeight = 3,
            Aperture = 20,
            EnterPolicy = EntityEnterPolicy.FarthestFromOccupied
        };
        var roomPlaneId = new PlaneId("roomInventory");
        AddPlane(world, roomPlaneId, 3, 3);
        world.RegisterInventoryPlane(TestWorld.SlimeId, roomPlaneId);
        AddEntity(world, "blocker-a", "Blocker A", new PlaneCoord(roomPlaneId, new GridCoord(0, 0)));
        AddEntity(world, "blocker-b", "Blocker B", new PlaneCoord(roomPlaneId, new GridCoord(2, 2)));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = CreateBehaviorPlan("enter-farthest", ActionPlanBehaviorStepKind.EnterTarget);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.Equal(new PlaneCoord(roomPlaneId, new GridCoord(2, 0)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.True(TraceDetailContains(result.Trace, "at (2,0)"));
    }

    [Fact]
    public void EdgeAlignedExitPolicyRejectsNonMatchingInventoryCoordinate()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with
        {
            InventoryWidth = 3,
            InventoryHeight = 3,
            Aperture = 20,
            ExitPolicy = EntityExitPolicy.EdgeAlignedWithExitDirection
        };
        var roomPlaneId = new PlaneId("roomInventory");
        AddPlane(world, roomPlaneId, 3, 3);
        world.RegisterInventoryPlane(TestWorld.SlimeId, roomPlaneId);
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(roomPlaneId, new GridCoord(1, 1))));
        world.SetActionFacing(TestWorld.PlayerId, Direction.South);
        var plan = CreateBehaviorPlan("exit-edge", ActionPlanBehaviorStepKind.ExitFacing);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.Equal(new PlaneCoord(roomPlaneId, new GridCoord(1, 1)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.True(TraceHasReason(result.Trace, FailureReason.InventoryPolicyBlocked));
    }

    [Fact]
    public void EdgeAlignedExitPolicyAllowsMatchingCardinalEdgeExit()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with
        {
            InventoryWidth = 3,
            InventoryHeight = 3,
            Aperture = 20,
            ExitPolicy = EntityExitPolicy.EdgeAlignedWithExitDirection
        };
        var roomPlaneId = new PlaneId("roomInventory");
        AddPlane(world, roomPlaneId, 3, 3);
        world.RegisterInventoryPlane(TestWorld.SlimeId, roomPlaneId);
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(roomPlaneId, new GridCoord(1, 2))));
        world.SetActionFacing(TestWorld.PlayerId, Direction.South);
        var plan = CreateBehaviorPlan("exit-edge", ActionPlanBehaviorStepKind.ExitFacing);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), world.GetEntityLocation(TestWorld.PlayerId));
    }

    [Fact]
    public void EnterRejectsTargetContainedWithinActor()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = CreateBehaviorPlan("enter-descendant", ActionPlanBehaviorStepKind.EnterTarget);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(TraceHasReason(result.Trace, FailureReason.InventoryPolicyBlocked));
    }

    [Fact]
    public void GiveTargetFailureFallsThroughWithoutConsumingStepTurn()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("give-then-turn"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.GiveTarget),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TurnLeft)
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(Direction.West, world.GetActionFacing(TestWorld.PlayerId));
        Assert.Contains("1. GiveTarget: Failure; fallback=continued", summary);
        Assert.True(TraceDetailContains(result.Trace, "player carries no entity to give"));
    }

    [Fact]
    public void TakeTargetFailureFallsThroughWhenTargetInventoryIsEmpty()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("take-then-turn"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TakeTarget),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TurnLeft)
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(Direction.West, world.GetActionFacing(TestWorld.PlayerId));
        Assert.Contains("1. TakeTarget: Failure; fallback=continued", summary);
        Assert.True(TraceDetailContains(result.Trace, "slime carries no entity to take"));
    }

    [Fact]
    public void GiveTargetCanTransferPlayerEntityWhenInventoryRulesAllowIt()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var chestId = AddEntityWithInventory(world, "chest", "Chest", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)), inventoryWidth: 1, inventoryHeight: 1, carryingCapacity: 30);
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Aperture = 30 };
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.SlimeId, chestId);
        var plan = CreateBehaviorPlan("give-player", ActionPlanBehaviorStepKind.GiveTarget);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(new PlaneId("chestInventory"), new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.True(TraceDetailContains(result.Trace, "gave player (Player)"));
    }

    private static ActionPlanDefinition CreateBehaviorPlan(string id, ActionPlanBehaviorStepKind stepKind) =>
        new(
            new ActionPlanId(id),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(stepKind)]));

    private static EntityId AddEntityWithInventory(WorldState world, string id, string name, PlaneCoord location, int inventoryWidth, int inventoryHeight, int carryingCapacity)
    {
        var entityId = AddEntity(world, id, name, location, inventoryWidth, inventoryHeight, bulk: 1, aperture: carryingCapacity);
        var inventoryPlaneId = new PlaneId($"{id}Inventory");
        AddPlane(world, inventoryPlaneId, inventoryWidth, inventoryHeight);
        world.RegisterInventoryPlane(entityId, inventoryPlaneId);
        return entityId;
    }

    private static EntityId AddEntity(WorldState world, string id, string name, PlaneCoord location, int inventoryWidth = 0, int inventoryHeight = 0, int bulk = 1, int aperture = 1)
    {
        var entityId = new EntityId(id);
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, inventoryWidth, inventoryHeight, bulk, aperture));
        world.Occupancy.Add(nodeId, entityId);
        return entityId;
    }

    private static void AddPlane(WorldState world, PlaneId planeId, int width, int height)
    {
        world.Planes.Add(planeId, new Plane(planeId, planeId.Value, width, height));
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                world.AddNode(planeId, new GridCoord(x, y));
            }
        }
    }

    private static bool TraceContains(TraceNode trace, string label)
    {
        return trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
    }

    private static bool TraceDetailContains(TraceNode trace, string detail)
    {
        return trace.Detail?.Contains(detail, StringComparison.Ordinal) == true
            || trace.Children.Any(child => TraceDetailContains(child, detail));
    }

    private static bool TraceHasReason(TraceNode trace, FailureReason reason)
    {
        return trace.Reason == reason || trace.Children.Any(child => TraceHasReason(child, reason));
    }
}
