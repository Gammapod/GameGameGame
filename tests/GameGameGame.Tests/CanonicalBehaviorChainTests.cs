using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class CanonicalBehaviorChainTests
{
    [Fact]
    public void BehaviorChainRunsMoveFacingThenPickupTargetWithoutLinkedFallbackPlan()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("behavior-chain"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget)
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(TestWorld.SlimeId));
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(TestWorld.RockId));
        Assert.Equal(TestWorld.RockId, world.GetActionTarget(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Action Step MoveFacing"));
        Assert.True(TraceContains(result.Trace, "Action Step PickupTarget"));
        Assert.False(TraceContains(result.Trace, "Fallback plan"));
    }

    [Fact]
    public void TransformAdjacentToInventoryBehaviorUsesPickupSemantics()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = CreateBehaviorPlan("transform-adjacent-to-inventory", ActionPlanBehaviorStepKind.TransformAdjacentToInventory);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Action Step TransformAdjacentToInventory"));
        Assert.True(TraceContains(result.Trace, "Primitive PickupTarget"));
    }

    [Fact]
    public void BehaviorChainTraceFormatterSummarizesFallbackStateAndTerminalOutcome()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("behavior-chain"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget)
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, context);
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.Collection(
            summary,
            line => Assert.Equal("Plan behavior-chain: Success; consumedTurn=True; continuePlan=False", line),
            line => Assert.Equal("1. MoveFacing: Failure; reason=InvalidPlacement; fallback=continued", line),
            line => Assert.Equal("   reads: Facing=West", line),
            line => Assert.Equal("   writes: Target=rock", line),
            line => Assert.Equal("2. PickupTarget: Success; fallback=stopped", line),
            line => Assert.Equal("   reads: Target=rock", line),
            line => Assert.Equal("   results: picked up rock into first available inventory coordinate (0,0)", line),
            line => Assert.Equal("Terminal: succeeded; consumed turn", line));
    }

    [Fact]
    public void BehaviorChainStopsAfterFirstSuccessfulActionStep()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("behavior-chain"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget)
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Slime@world(0,1)", world.FormatEntityAddress(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Action Step MoveFacing"));
        Assert.False(TraceContains(result.Trace, "Action Step PickupTarget"));
    }

    [Fact]
    public void BehaviorStepWithoutCostPreservesExistingExecutionBehavior()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("no-cost-behavior-chain"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Slime@world(0,1)", world.FormatEntityAddress(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Action Step MoveFacing"));
    }

    [Fact]
    public void CostedActionStepFallsThroughWhenRequiredTemplateIsMissing()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("missing-cost-fallthrough"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Backstep)
                {
                    Costs = [new ActionStepCostDescriptor("Mana", 1)]
                },
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(0,2)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "missing cost Mana: required 1, available 0"));
    }

    [Fact]
    public void CostedActionStepFallsThroughWhenQuantityIsInsufficient()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        AddEntity(world, new EntityId("mana-1"), "Mana", "Mana", new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("insufficient-cost-fallthrough"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Backstep)
                {
                    Costs = [new ActionStepCostDescriptor("Mana", 2)]
                },
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.Equal("Player@world(0,2)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "missing cost Mana: required 2, available 1"));
    }

    [Fact]
    public void CostedActionStepFindsCostRecursivelyInActorInventoryContents()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        var pouchId = new EntityId("pouch");
        var pouchPlaneId = new PlaneId("pouch-inventory");
        AddEntity(world, pouchId, "Pouch", "Pouch", new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), inventoryWidth: 1, inventoryHeight: 1);
        AddInventoryPlane(world, pouchId, pouchPlaneId, width: 1, height: 1);
        AddEntity(world, new EntityId("leaf"), "Leaf", "Leaf", new PlaneCoord(pouchPlaneId, new GridCoord(0, 0)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("nested-cost"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)
                {
                    Costs = [new ActionStepCostDescriptor("Leaf", 1)]
                }
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(0,2)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "Cost available Leaf: required 1, available 1"));
    }

    [Fact]
    public void MissingCostDoesNotExecuteUnderlyingActionStep()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("missing-cost-no-underlying-execute"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Backstep)
                {
                    Costs = [new ActionStepCostDescriptor("Mana", 1)]
                }
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.False(result.Succeeded);
        Assert.Equal("Player@world(1,2)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.False(TraceContains(result.Trace, "Primitive Backstep"));
        Assert.True(TraceContains(result.Trace, "missing cost Mana: required 1, available 0"));
    }

    [Fact]
    public void CostedActionStepConsumesRequiredEntitiesAfterConsumingSuccess()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        AddEntity(world, new EntityId("mana-1"), "Mana", "Mana", new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        AddEntity(world, new EntityId("mana-2"), "Mana", "Mana", new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("consume-cost-success"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)
                {
                    Costs = [new ActionStepCostDescriptor("Mana", 2)]
                }
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.Equal("Player@world(0,2)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.False(world.Entities.ContainsKey(new EntityId("mana-1")));
        Assert.False(world.Entities.ContainsKey(new EntityId("mana-2")));
        Assert.True(TraceContains(result.Trace, "Consumed cost Mana: mana-1,mana-2"));
    }

    [Fact]
    public void CostedActionStepDestroysNestedCostEntityInventoryRecursively()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        var pouchId = new EntityId("pouch");
        var pouchPlaneId = new PlaneId("pouch-inventory");
        AddEntity(world, pouchId, "Pouch", "Pouch", new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), inventoryWidth: 1, inventoryHeight: 1);
        AddInventoryPlane(world, pouchId, pouchPlaneId, width: 1, height: 1);
        AddEntity(world, new EntityId("leaf"), "Leaf", "Leaf", new PlaneCoord(pouchPlaneId, new GridCoord(0, 0)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("consume-nested-cost-success"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)
                {
                    Costs = [new ActionStepCostDescriptor("Pouch", 1)]
                }
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.False(world.Entities.ContainsKey(pouchId));
        Assert.False(world.Entities.ContainsKey(new EntityId("leaf")));
        Assert.False(world.Planes.ContainsKey(pouchPlaneId));
        Assert.True(TraceContains(result.Trace, "Consumed cost Pouch: pouch"));
    }

    [Fact]
    public void CostedActionStepPreservesCostWhenUnderlyingStepFails()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.North));
        AddEntity(world, new EntityId("mana-1"), "Mana", "Mana", new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("preserve-cost-underlying-failure"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)
                {
                    Costs = [new ActionStepCostDescriptor("Mana", 1)]
                }
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.False(result.Succeeded);
        Assert.Equal("Player@world(1,2)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.True(world.Entities.ContainsKey(new EntityId("mana-1")));
        Assert.False(TraceContains(result.Trace, "Consumed cost Mana: mana-1"));
    }

    [Fact]
    public void CostedActionStepConsumesOnlySelectedQuantityWhenMoreCostExists()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        AddEntity(world, new EntityId("mana-1"), "Mana", "Mana", new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        AddEntity(world, new EntityId("mana-2"), "Mana", "Mana", new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0)));
        AddEntity(world, new EntityId("mana-3"), "Mana", "Mana", new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("consume-selected-cost-success"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)
                {
                    Costs = [new ActionStepCostDescriptor("Mana", 2)]
                }
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.False(world.Entities.ContainsKey(new EntityId("mana-1")));
        Assert.False(world.Entities.ContainsKey(new EntityId("mana-2")));
        Assert.True(world.Entities.ContainsKey(new EntityId("mana-3")));
        Assert.True(TraceContains(result.Trace, "Consumed cost Mana: mana-1,mana-2"));
    }

    private static ActionPlanDefinition CreateBehaviorPlan(string id, ActionPlanBehaviorStepKind stepKind) =>
        new(
            new ActionPlanId(id),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(stepKind)]));

    private static bool TraceContains(TraceNode trace, string label)
    {
        return trace.Label == label || trace.Detail == label || trace.Children.Any(child => TraceContains(child, label));
    }

    private static void AddEntity(
        WorldState world,
        EntityId entityId,
        string name,
        string templateId,
        PlaneCoord location,
        int inventoryWidth = 0,
        int inventoryHeight = 0)
    {
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, inventoryWidth, inventoryHeight, Bulk: 1, Aperture: 1, TemplateId: templateId));
        world.Occupancy.Add(nodeId, entityId);
    }

    private static void AddInventoryPlane(WorldState world, EntityId ownerId, PlaneId planeId, int width, int height)
    {
        world.Planes.Add(planeId, new Plane(planeId, $"{ownerId} Inventory", width, height));
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                world.AddNode(planeId, new GridCoord(x, y));
            }
        }

        world.RegisterInventoryPlane(ownerId, planeId);
    }
}
