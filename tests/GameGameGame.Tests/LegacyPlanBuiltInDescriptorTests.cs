using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class LegacyPlanBuiltInDescriptorTests
{
    [Fact]
    public void ActionPlanDescriptorMaterializesCanonicalBuiltInsWithoutVariableNames()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.South));
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("canonical-descriptor-move"),
            [
                new ActionPlanStepDescriptor(
                    "move facing",
                    [PlanCheckDescriptor.CanMove()],
                    PlanEffectDescriptor.Move(),
                    OnFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            context);

        Assert.True(result.Succeeded);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "Read slot Facing"));
        Assert.True(TraceContains(result.Trace, "Relocate player -> AdjacentMovementDestination { AnchorId = player, Direction = South }"));
    }

    [Fact]
    public void PickupEffectUsesRelocationAfterPickupValidation()
    {
        var world = TestWorld.CreateWorld();
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Bulk = 4 };
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Target, new EntityPlanValue(TestWorld.RockId));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("pickup-relocation"),
            [
                new ActionPlanStep(
                    "pickup",
                    [new CanPickupCheck(new GridCoord(0, 0))],
                    new PickupEffect(new GridCoord(0, 0)),
                    onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.SlimeId,
            plan,
            context);

        Assert.True(result.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
        Assert.True(TraceContains(result.Trace, "Relocate rock -> PlaneMovementDestination { Coord = slime(0,0) }"));
    }

    [Fact]
    public void ActionPlanDescriptorMaterializesTeleportEffectToExplicitDestination()
    {
        var world = TestWorld.CreateWorld();
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("teleport-rock"),
            [
                new ActionPlanStepDescriptor(
                    "teleport rock",
                    [],
                    PlanEffectDescriptor.Teleport(
                        MovementTargetDescriptor.Entity(TestWorld.RockId),
                        MovementDestinationDescriptor.Plane(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0)))),
                    OnFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
        Assert.True(TraceContains(result.Trace, "Teleport Entity"));
    }

    [Fact]
    public void TeleportEffectCanTargetCanonicalTargetAndInventoryDestination()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Target, new EntityPlanValue(TestWorld.RockId));
        var effect = new TeleportEffect(
            MovementTargetDescriptor.CanonicalTarget(),
            MovementDestinationDescriptor.InventorySlot(TestWorld.PlayerId, new GridCoord(0, 0)));

        var result = effect.Apply(world, TestWorld.PlayerId, context, new MovementService());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
        Assert.True(TraceContains(result.Trace, "Read slot Target"));
    }

    [Fact]
    public void TeleportEffectCanTargetCarriedInventoryCoord()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        var effect = new TeleportEffect(
            MovementTargetDescriptor.CarriedInventoryCoord(new GridCoord(0, 0)),
            MovementDestinationDescriptor.Plane(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0))));

        var result = effect.Apply(world, TestWorld.PlayerId, new ActionPlanContext(), movement);

        Assert.True(result.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
    }

    [Fact]
    public void ActionPlanDescriptorMaterializesDropEffectFromCarriedInventoryCoord()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("drop-rock"),
            [
                new ActionPlanStepDescriptor(
                    "drop rock",
                    [],
                    PlanEffectDescriptor.Drop(
                        MovementTargetDescriptor.CarriedInventoryCoord(new GridCoord(0, 0)),
                        MovementDestinationDescriptor.AdjacentToSelf(Direction.West)),
                    OnFailure: null)
            ]);

        var result = new ActionPlanInterpreter(movement).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 2)), world.GetEntityLocation(TestWorld.RockId));
        Assert.True(TraceContains(result.Trace, "Drop CarriedInventoryCoord"));
        Assert.True(TraceContains(result.Trace, "Relocate rock -> PlaneMovementDestination { Coord = world(0,2) }"));
    }

    [Fact]
    public void DropEffectFailsWhenTargetIsNotCarriedByActor()
    {
        var world = TestWorld.CreateWorld();
        var effect = new DropEffect(
            MovementTargetDescriptor.Entity(TestWorld.RockId),
            MovementDestinationDescriptor.AdjacentToSelf(Direction.West));

        var result = effect.Apply(world, TestWorld.PlayerId, new ActionPlanContext(), new MovementService());

        Assert.False(result.Succeeded);
        Assert.Equal(FailureReason.TargetNotInInventory, result.Trace.Reason);
    }

    [Fact]
    public void PlanPrimitiveCatalogCreatesDefaultCanonicalDescriptors()
    {
        var canMove = PlanPrimitiveCatalog.CreateDefaultCheck(PlanCheckKind.CanMove);
        var blockingEntity = PlanPrimitiveCatalog.CreateDefaultCheck(PlanCheckKind.BlockingEntity);
        var move = PlanPrimitiveCatalog.CreateDefaultEffect(PlanEffectKind.Move);
        var reverse = PlanPrimitiveCatalog.CreateDefaultEffect(PlanEffectKind.ReverseDirection);

        Assert.Equal(PlanCheckKind.CanMove, canMove.Kind);
        Assert.Null(canMove.DirectionVariable);
        Assert.Equal(PlanCheckKind.BlockingEntity, blockingEntity.Kind);
        Assert.Null(blockingEntity.DirectionVariable);
        Assert.Null(blockingEntity.TargetVariable);
        Assert.Equal(PlanEffectKind.Move, move.Kind);
        Assert.Null(move.DirectionVariable);
        Assert.Equal(PlanEffectKind.ReverseDirection, reverse.Kind);
        Assert.Null(reverse.DirectionVariable);
    }

    [Fact]
    public void ActionPlanDescriptorMaterializesExecutableBuiltIns()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.South));
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("descriptor-move"),
            [
                new ActionPlanStepDescriptor(
                    "move facing",
                    [PlanCheckDescriptor.CanMove("facing")],
                    PlanEffectDescriptor.Move("facing"),
                    OnFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "Can move facing"));
        Assert.True(TraceContains(result.Trace, "Move facing"));
    }

    [Fact]
    public void BuiltInPlanPartsExposeStructuredInputs()
    {
        var facing = new PlanVariableRef<DirectionPlanValue>("facing");
        var target = new PlanVariableRef<EntityPlanValue>("target");
        var destination = new LiteralCoordValueSource(new GridCoord(0, 0));

        var canMove = new CanMoveCheck(facing);
        var blocking = new BlockingEntityCheck(facing, target);
        var pickup = new PickupEffect(target, destination);
        var reverse = new ReverseDirectionEffect(facing, consumesTurn: false, continuePlan: true);

        Assert.Equal(facing, canMove.Direction);
        Assert.Equal(facing, blocking.Direction);
        Assert.Equal(target, blocking.Target);
        Assert.Equal(target, pickup.Target);
        Assert.Equal(destination, pickup.InventoryCoord);
        Assert.Equal(facing, reverse.Direction);
    }

    private static bool TraceContains(TraceNode trace, string label)
    {
        return trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
    }
}
