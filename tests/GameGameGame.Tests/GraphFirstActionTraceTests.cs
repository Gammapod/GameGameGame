using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class GraphFirstActionTraceTests
{
    [Fact]
    public void PickupActionTraceReportsGraphAdjacencyFacts()
    {
        var world = CreateWorldWithRemoteRockLinkedFromPlayer();
        var action = new PickupAction(TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));

        var result = action.Evaluate(world, TestWorld.PlayerId, new MovementService());

        Assert.True(result.CanExecute);
        Assert.True(TraceDetailContains(result.Trace, "sourceNode=world:1,2"));
        Assert.True(TraceDetailContains(result.Trace, "destinationNode=world:3,3"));
        Assert.True(TraceDetailContains(result.Trace, "edge=SourceCellLink"));
    }

    [Fact]
    public void EnterActionTraceReportsGraphAdjacencyFacts()
    {
        var world = TestWorld.CreateWorld();
        Relocate(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 3)));
        world.SourceCellLinks.Add(new SourceCellLink(
            world.GetEntityLocation(TestWorld.PlayerId),
            Direction.East,
            world.GetEntityLocation(TestWorld.SlimeId),
            Direction.West));
        var action = new EnterAction(TestWorld.SlimeId);

        var result = action.Evaluate(world, TestWorld.PlayerId, new MovementService());

        Assert.True(result.CanExecute);
        Assert.True(TraceDetailContains(result.Trace, "sourceNode=world:1,2"));
        Assert.True(TraceDetailContains(result.Trace, "destinationNode=world:3,3"));
        Assert.True(TraceDetailContains(result.Trace, "edge=SourceCellLink"));
    }

    [Fact]
    public void TransferActionCounterpartyTraceReportsGraphMovementEdgeFacts()
    {
        var world = TestWorld.CreateWorld();
        Relocate(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));
        Relocate(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 3)));
        world.SourceCellLinks.Add(new SourceCellLink(
            world.GetEntityLocation(TestWorld.PlayerId),
            Direction.East,
            world.GetEntityLocation(TestWorld.SlimeId),
            Direction.West));
        var action = new TransferAction(TransferDirection.ActorToTarget, TestWorld.RockId, Direction.East);

        var result = action.Evaluate(world, TestWorld.PlayerId, new MovementService());

        Assert.True(result.CanExecute);
        Assert.True(TraceDetailContains(result.Trace, "sourceNode=world:1,2"));
        Assert.True(TraceDetailContains(result.Trace, "destinationNode=world:3,3"));
        Assert.True(TraceDetailContains(result.Trace, "edge=SourceCellLink"));
    }

    [Fact]
    public void PrimitivePickupTargetTraceReportsGraphAdjacencyFacts()
    {
        var world = CreateWorldWithRemoteRockLinkedFromPlayer();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Target, new EntityPlanValue(TestWorld.RockId));
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("pickup-target"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PickupTarget));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            context);

        Assert.True(result.Succeeded);
        Assert.True(TraceDetailContains(result.Trace, "sourceNode=world:1,2"));
        Assert.True(TraceDetailContains(result.Trace, "destinationNode=world:3,3"));
        Assert.True(TraceDetailContains(result.Trace, "edge=SourceCellLink"));
    }

    private static WorldState CreateWorldWithRemoteRockLinkedFromPlayer()
    {
        var world = TestWorld.CreateWorld();
        Relocate(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 3)));
        world.SourceCellLinks.Add(new SourceCellLink(
            world.GetEntityLocation(TestWorld.PlayerId),
            Direction.East,
            world.GetEntityLocation(TestWorld.RockId),
            Direction.West));
        return world;
    }

    private static void Relocate(WorldState world, EntityId entityId, PlaneCoord destination)
    {
        Assert.True(new MovementService().TryPlace(world, entityId, destination));
    }

    private static bool TraceDetailContains(TraceNode trace, string text)
    {
        return (trace.Detail?.Contains(text, StringComparison.Ordinal) ?? false) ||
            trace.Children.Any(child => TraceDetailContains(child, text));
    }
}
