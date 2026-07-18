using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class ActionPlanContextTests
{
    [Fact]
    public void ActionPlanContextStoresTypedVariables()
    {
        var context = new ActionPlanContext();

        context.Set("facing", new DirectionPlanValue(Direction.West));

        Assert.True(context.TryGet<DirectionPlanValue>("facing", out var facing));
        Assert.Equal(Direction.West, facing.Value);
    }

    [Fact]
    public void ActionPlanContextVariableUpdatesAreTraced()
    {
        var context = new ActionPlanContext();

        var trace = context.Set("facing", new DirectionPlanValue(Direction.East));

        Assert.Equal(TraceStatus.Success, trace.Status);
        Assert.Equal("Set variable facing", trace.Label);
        Assert.Contains("East", trace.Detail);
    }

    [Fact]
    public void ActionPlanContextVariablesPersistAcrossUpdates()
    {
        var context = new ActionPlanContext();

        context.Set("facing", new DirectionPlanValue(Direction.West));
        context.Set("facing", new DirectionPlanValue(Direction.East));

        Assert.True(context.TryGet<DirectionPlanValue>("facing", out var facing));
        Assert.Equal(Direction.East, facing.Value);
    }

    [Fact]
    public void ActionPlanContextStoresTypedCanonicalSlots()
    {
        var context = new ActionPlanContext();

        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        context.Set(ActionPlanSlot.Target, new EntityPlanValue(TestWorld.RockId));

        Assert.True(context.TryGet<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing));
        Assert.Equal(Direction.West, facing.Value);
        Assert.True(context.TryGet<EntityPlanValue>(ActionPlanSlot.Target, out var target));
        Assert.Equal(TestWorld.RockId, target.Value);
    }

    [Fact]
    public void ActionPlanContextCanonicalSlotWritesAreTraced()
    {
        var context = new ActionPlanContext();

        var trace = context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.East));

        Assert.Equal(TraceStatus.Success, trace.Status);
        Assert.Equal("Set slot Facing", trace.Label);
        Assert.Contains("East", trace.Detail);
    }

    [Fact]
    public void ActionPlanContextCanonicalSlotReadsTraceMissingAndWrongKind()
    {
        var context = new ActionPlanContext();

        Assert.False(context.TryRead<DirectionPlanValue>(ActionPlanSlot.Facing, out _, out var missingTrace));
        Assert.Equal(TraceStatus.Failure, missingTrace.Status);
        Assert.Equal("Read slot Facing", missingTrace.Label);
        Assert.Contains("missing", missingTrace.Detail);

        context.Set(ActionPlanSlot.Target, new EntityPlanValue(TestWorld.RockId));

        Assert.False(context.TryRead<DirectionPlanValue>(ActionPlanSlot.Target, out _, out var wrongKindTrace));
        Assert.Equal(TraceStatus.Failure, wrongKindTrace.Status);
        Assert.Equal("Read slot Target", wrongKindTrace.Label);
        Assert.Contains("expected Direction", wrongKindTrace.Detail);
        Assert.Contains("actual Entity", wrongKindTrace.Detail);
    }

    [Fact]
    public void ActionPlanContextCanonicalSlotsPersistAcrossPlanExecutions()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        var writer = new ActionPlanDefinition(
            new ActionPlanId("write-facing"),
            [
                new ActionPlanStep(
                    "write canonical facing",
                    [],
                    new SlotWritingEffect(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.East)),
                    onFailure: null)
            ]);
        Direction? readFacing = null;
        var reader = new ActionPlanDefinition(
            new ActionPlanId("read-facing"),
            [
                new ActionPlanStep(
                    "read canonical facing",
                    [],
                    new SlotReadingEffect(ActionPlanSlot.Facing, value => readFacing = value),
                    onFailure: null)
            ]);
        var interpreter = new ActionPlanInterpreter(new MovementService());

        var writeResult = interpreter.Execute(world, TestWorld.PlayerId, writer, context);
        var readResult = interpreter.Execute(world, TestWorld.PlayerId, reader, context);

        Assert.True(writeResult.Succeeded);
        Assert.True(readResult.Succeeded);
        Assert.Equal(Direction.East, readFacing);
    }

    [Fact]
    public void CanonicalFacingPersistsOnActorActionStateAcrossPlanExecutions()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.West);
        var context = new ActionPlanContext();
        var reverse = new ActionPlanDefinition(
            new ActionPlanId("reverse-facing"),
            [
                new ActionPlanStep(
                    "reverse canonical facing",
                    [],
                    new ReverseDirectionEffect(consumesTurn: false, continuePlan: false),
                    onFailure: null)
            ]);
        Direction? readFacing = null;
        var reader = new ActionPlanDefinition(
            new ActionPlanId("read-facing"),
            [
                new ActionPlanStep(
                    "read canonical facing",
                    [],
                    new SlotReadingEffect(ActionPlanSlot.Facing, value => readFacing = value),
                    onFailure: null)
            ]);
        var interpreter = new ActionPlanInterpreter(new MovementService());

        var reverseResult = interpreter.Execute(world, TestWorld.PlayerId, reverse, context);
        var readResult = interpreter.Execute(world, TestWorld.PlayerId, reader, context);

        Assert.True(reverseResult.Succeeded);
        Assert.True(readResult.Succeeded);
        Assert.Equal(Direction.East, world.GetActionFacing(TestWorld.PlayerId));
        Assert.Equal(Direction.East, readFacing);
    }

    [Fact]
    public void CanonicalTargetPersistsOnActorActionStateWhenBlockingEntityIsFound()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("remember-blocker"),
            [
                new ActionPlanStepDescriptor(
                    "find blocker",
                    [PlanCheckDescriptor.BlockingEntity()],
                    PlanEffectDescriptor.Wait(),
                    OnFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "Set slot Target"));
    }

    private static bool TraceContains(TraceNode trace, string label)
    {
        return trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
    }

    private sealed class SlotWritingEffect(ActionPlanSlot slot, PlanValue value) : IPlanEffect
    {
        public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
        {
            var trace = context.Set(slot, value);

            return new PlanEffectResult(true, ConsumesTurn: false, ContinuePlan: false, trace);
        }
    }

    private sealed class SlotReadingEffect(ActionPlanSlot slot, Action<Direction> read) : IPlanEffect
    {
        public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
        {
            if (!context.TryRead<DirectionPlanValue>(slot, out var value, out var trace))
            {
                return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, trace);
            }

            read(value.Value);

            return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, trace);
        }
    }
}
