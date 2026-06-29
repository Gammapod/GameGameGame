using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class CoreBulkApertureTests
{
    [Fact]
    public void PickupFailsWhenTargetBulkExceedsCarrierAperture()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 2 };
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Bulk = 3 };
        var action = new PickupAction(TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));

        var evaluation = action.Evaluate(world, TestWorld.PlayerId, movement);

        Assert.False(evaluation.CanExecute);
        Assert.Equal(FailureReason.ApertureBlocked, evaluation.Trace.Reason);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 1)), world.GetEntityLocation(TestWorld.SlimeId));
    }

    [Fact]
    public void PickupIgnoresRecursiveContentsWhenMovingEntityBulkFitsAperture()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 2 };
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Bulk = 2 };
        world.Entities[TestWorld.RockId] = world.Entities[TestWorld.RockId] with { Bulk = 99 };
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        var action = new PickupAction(TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));

        var resolution = ((IActionIntent)action).Resolve(world, TestWorld.PlayerId, movement);

        Assert.True(resolution.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.SlimeId));
    }

    [Fact]
    public void DropFailsWhenTargetBulkExceedsSourceCarrierAperture()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 1 };
        world.Entities[TestWorld.RockId] = world.Entities[TestWorld.RockId] with { Bulk = 2 };
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        var action = new DropAction(TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)));

        var evaluation = action.Evaluate(world, TestWorld.PlayerId, movement);

        Assert.False(evaluation.CanExecute);
        Assert.Equal(FailureReason.ApertureBlocked, evaluation.Trace.Reason);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void DropFacingUsesApertureTransitionRules()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.SetActionFacing(TestWorld.PlayerId, Direction.South);
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 1 };
        world.Entities[TestWorld.RockId] = world.Entities[TestWorld.RockId] with { Bulk = 2 };
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        var plan = CreateBehaviorPlan("drop-facing", ActionPlanBehaviorStepKind.DropFacing);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(TraceHasReason(result.Trace, FailureReason.ApertureBlocked));
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void EnterTargetFailsWhenActorBulkExceedsTargetAperture()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Bulk = 10 };
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Aperture = 9 };
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, CreateBehaviorPlan("enter", ActionPlanBehaviorStepKind.EnterTarget), new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(TraceHasReason(result.Trace, FailureReason.ApertureBlocked));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), world.GetEntityLocation(TestWorld.PlayerId));
    }

    [Fact]
    public void ExitFacingFailsWhenActorBulkExceedsContainerAperture()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Bulk = 10 };
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Aperture = 9 };
        world.SetActionFacing(TestWorld.PlayerId, Direction.South);
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, CreateBehaviorPlan("exit", ActionPlanBehaviorStepKind.ExitFacing), new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(TraceHasReason(result.Trace, FailureReason.ApertureBlocked));
        Assert.Equal(new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.PlayerId));
    }

    [Fact]
    public void GiveTargetFailsWhenTransferBulkExceedsSourceAperture()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 1 };
        world.Entities[TestWorld.RockId] = world.Entities[TestWorld.RockId] with { Bulk = 2 };
        var chestId = AddEntityWithInventory(world, "chest", "Chest", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)), inventoryWidth: 1, inventoryHeight: 1, aperture: 10);
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, chestId);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, CreateBehaviorPlan("give", ActionPlanBehaviorStepKind.GiveTarget), new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(TraceHasReason(result.Trace, FailureReason.ApertureBlocked));
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void GiveTargetFailsWhenTransferBulkExceedsDestinationAperture()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 10 };
        world.Entities[TestWorld.RockId] = world.Entities[TestWorld.RockId] with { Bulk = 2 };
        var chestId = AddEntityWithInventory(world, "chest", "Chest", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)), inventoryWidth: 1, inventoryHeight: 1, aperture: 1);
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, chestId);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, CreateBehaviorPlan("give", ActionPlanBehaviorStepKind.GiveTarget), new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(TraceHasReason(result.Trace, FailureReason.ApertureBlocked));
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void TakeTargetFailsWhenTransferBulkExceedsSourceAperture()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 10 };
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Aperture = 1 };
        world.Entities[TestWorld.RockId] = world.Entities[TestWorld.RockId] with { Bulk = 2 };
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, CreateBehaviorPlan("take", ActionPlanBehaviorStepKind.TakeTarget), new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(TraceHasReason(result.Trace, FailureReason.ApertureBlocked));
        Assert.Equal(new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void TakeTargetFailsWhenTransferBulkExceedsDestinationAperture()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 1 };
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Aperture = 10 };
        world.Entities[TestWorld.RockId] = world.Entities[TestWorld.RockId] with { Bulk = 2 };
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, CreateBehaviorPlan("take", ActionPlanBehaviorStepKind.TakeTarget), new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(TraceHasReason(result.Trace, FailureReason.ApertureBlocked));
        Assert.Equal(new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void TeleportBypassesApertureTransitionRules()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 1 };
        world.Entities[TestWorld.RockId] = world.Entities[TestWorld.RockId] with { Bulk = 99 };
        var effect = new TeleportEffect(
            MovementTargetDescriptor.Entity(TestWorld.RockId),
            MovementDestinationDescriptor.InventorySlot(TestWorld.PlayerId, new GridCoord(0, 0)));

        var result = effect.Apply(world, TestWorld.PlayerId, new ActionPlanContext(), movement);

        Assert.True(result.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
    }

    private static ActionPlanDefinition CreateBehaviorPlan(string id, ActionPlanBehaviorStepKind stepKind) =>
        new(
            new ActionPlanId(id),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(stepKind)]));

    private static bool TraceHasReason(TraceNode trace, FailureReason reason) =>
        trace.Reason == reason || trace.Children.Any(child => TraceHasReason(child, reason));

    private static EntityId AddEntityWithInventory(
        WorldState world,
        string id,
        string name,
        PlaneCoord location,
        int inventoryWidth,
        int inventoryHeight,
        int aperture)
    {
        var entityId = new EntityId(id);
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, inventoryWidth, inventoryHeight, Bulk: 1, Aperture: aperture));
        world.Occupancy.Add(nodeId, entityId);
        var inventoryPlaneId = new PlaneId($"{id}Inventory");
        world.Planes.Add(inventoryPlaneId, new Plane(inventoryPlaneId, inventoryPlaneId.Value, inventoryWidth, inventoryHeight));
        for (var y = 0; y < inventoryHeight; y++)
        {
            for (var x = 0; x < inventoryWidth; x++)
            {
                world.AddNode(inventoryPlaneId, new GridCoord(x, y));
            }
        }

        world.RegisterInventoryPlane(entityId, inventoryPlaneId);
        return entityId;
    }
}
