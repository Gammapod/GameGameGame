using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class CoreActionPlanTests
{
    [Theory]
    [InlineData(ActionPlanShape.EmptyPassive)]
    [InlineData(ActionPlanShape.CanonicalBehaviorChain)]
    [InlineData(ActionPlanShape.TransitionalPrimitivePlan)]
    [InlineData(ActionPlanShape.LegacyLowLevelSteps)]
    [InlineData(ActionPlanShape.InvalidMixedShape)]
    [InlineData(ActionPlanShape.InvalidEmptyBehaviorChain)]
    public void ActionPlanShapeClassifierIdentifiesPlanShape(ActionPlanShape expectedShape)
    {
        var descriptor = expectedShape switch
        {
            ActionPlanShape.EmptyPassive => new ActionPlanDescriptor(new ActionPlanId("empty"), []),
            ActionPlanShape.CanonicalBehaviorChain => new ActionPlanDescriptor(
                new ActionPlanId("behavior"),
                [],
                Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)])),
            ActionPlanShape.TransitionalPrimitivePlan => new ActionPlanDescriptor(
                new ActionPlanId("primitive"),
                [],
                Primitive: new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing)),
            ActionPlanShape.LegacyLowLevelSteps => new ActionPlanDescriptor(
                new ActionPlanId("legacy"),
                [new ActionPlanStepDescriptor("wait", [], PlanEffectDescriptor.Wait(), OnFailure: null)]),
            ActionPlanShape.InvalidMixedShape => new ActionPlanDescriptor(
                new ActionPlanId("mixed"),
                [new ActionPlanStepDescriptor("wait", [], PlanEffectDescriptor.Wait(), OnFailure: null)],
                Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MoveFacing)])),
            ActionPlanShape.InvalidEmptyBehaviorChain => new ActionPlanDescriptor(
                new ActionPlanId("empty-behavior"),
                [],
                Behavior: new ActionPlanBehaviorDescriptor([])),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedShape))
        };

        Assert.Equal(expectedShape, ActionPlanShapeClassifier.Classify(descriptor));
    }

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

    [Fact]
    public void PlanVariableRefReadsTypedVariableFromContext()
    {
        var context = new ActionPlanContext();
        var facingRef = new PlanVariableRef<DirectionPlanValue>("facing");
        context.Set("facing", new DirectionPlanValue(Direction.West));

        Assert.True(facingRef.TryRead(context, out var value));
        Assert.Equal(Direction.West, value.Value);
        Assert.Equal("facing", facingRef.Name);
    }

    [Fact]
    public void LiteralCoordValueSourceExposesSerializableLiteralValue()
    {
        var source = new LiteralCoordValueSource(new GridCoord(1, 0));

        Assert.Equal(new GridCoord(1, 0), source.Value);
        Assert.Equal("(1,0)", source.ToString());
    }

    [Fact]
    public void PlanValueDescriptorKeepsInitialVariablesAsData()
    {
        var descriptor = PlanValueDescriptor.Direction(Direction.West);

        Assert.Equal(PlanValueKind.Direction, descriptor.Kind);
        Assert.Equal(Direction.West, descriptor.DirectionValue);
        Assert.Equal(new DirectionPlanValue(Direction.West), descriptor.Materialize());
    }

    [Fact]
    public void ActionPlanDescriptorKeepsBuiltInInputsAsData()
    {
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("descriptor-test"),
            [
                new ActionPlanStepDescriptor(
                    "move facing",
                    [PlanCheckDescriptor.CanMove("facing")],
                    PlanEffectDescriptor.Move("facing"),
                    PlanEffectDescriptor.ReverseDirection("facing", consumesTurn: false, continuePlan: true))
            ]);

        var step = Assert.Single(descriptor.Steps);
        var check = Assert.Single(step.Checks);

        Assert.Equal(PlanCheckKind.CanMove, check.Kind);
        Assert.Equal("facing", check.DirectionVariable);
        Assert.Equal(PlanEffectKind.Move, step.OnSuccess!.Kind);
        Assert.Equal("facing", step.OnSuccess.DirectionVariable);
        Assert.Equal(PlanEffectKind.ReverseDirection, step.OnFailure!.Kind);
        Assert.False(step.OnFailure.ConsumesTurn);
        Assert.True(step.OnFailure.ContinuePlan);
    }

    [Fact]
    public void PlanPrimitiveCatalogExposesAllCheckEffectAndValueKinds()
    {
        Assert.Equal(Enum.GetValues<PlanCheckKind>().Order(), PlanPrimitiveCatalog.Checks.Select(check => check.Kind).Order());
        Assert.Equal(Enum.GetValues<PlanEffectKind>().Order(), PlanPrimitiveCatalog.Effects.Select(effect => effect.Kind).Order());
        Assert.Equal(Enum.GetValues<PlanValueKind>().Order(), PlanPrimitiveCatalog.ValueKinds.Select(value => value.Kind).Order());
    }

    [Fact]
    public void PlanPrimitiveCatalogDescribesCheckFieldsAndVariableContracts()
    {
        var canMove = PlanPrimitiveCatalog.GetCheck(PlanCheckKind.CanMove);
        var blockingEntity = PlanPrimitiveCatalog.GetCheck(PlanCheckKind.BlockingEntity);

        var canMoveField = Assert.Single(canMove.Fields);
        Assert.Equal("directionVariable", canMoveField.Name);
        Assert.Equal(PlanPrimitiveFieldKind.VariableRead, canMoveField.Kind);
        Assert.Equal(PlanValueKind.Direction, canMoveField.ValueKind);
        Assert.True(canMoveField.IsRequired);

        Assert.Contains(blockingEntity.Fields, field =>
            field.Name == "directionVariable"
            && field.Kind == PlanPrimitiveFieldKind.VariableRead
            && field.ValueKind == PlanValueKind.Direction);
        Assert.Contains(blockingEntity.Fields, field =>
            field.Name == "targetVariable"
            && field.Kind == PlanPrimitiveFieldKind.VariableWrite
            && field.ValueKind == PlanValueKind.Entity);
    }

    [Fact]
    public void PlanPrimitiveCatalogDescribesEffectFieldsAndReferences()
    {
        var pickup = PlanPrimitiveCatalog.GetEffect(PlanEffectKind.Pickup);
        var callPlan = PlanPrimitiveCatalog.GetEffect(PlanEffectKind.CallPlan);
        var setVariable = PlanPrimitiveCatalog.GetEffect(PlanEffectKind.SetVariable);

        Assert.Contains(pickup.Fields, field =>
            field.Name == "targetVariable"
            && field.Kind == PlanPrimitiveFieldKind.VariableRead
            && field.ValueKind == PlanValueKind.Entity);
        Assert.Contains(pickup.Fields, field =>
            field.Name == "inventoryCoord"
            && field.Kind == PlanPrimitiveFieldKind.CoordLiteral);
        Assert.Contains(callPlan.Fields, field =>
            field.Name == "planId"
            && field.Kind == PlanPrimitiveFieldKind.ActionPlanReference);
        Assert.Contains(setVariable.Fields, field =>
            field.Name == "variableName"
            && field.Kind == PlanPrimitiveFieldKind.VariableWrite);
        Assert.Contains(setVariable.Fields, field =>
            field.Name == "value"
            && field.Kind == PlanPrimitiveFieldKind.PlanValueLiteral);
    }

    [Fact]
    public void PlanPrimitiveCatalogDescribesCanonicalSlotUsage()
    {
        var canMove = PlanPrimitiveCatalog.GetCheck(PlanCheckKind.CanMove);
        var blockingEntity = PlanPrimitiveCatalog.GetCheck(PlanCheckKind.BlockingEntity);
        var pickup = PlanPrimitiveCatalog.GetEffect(PlanEffectKind.Pickup);
        var reverse = PlanPrimitiveCatalog.GetEffect(PlanEffectKind.ReverseDirection);

        Assert.Contains(canMove.SlotReads, slot => slot.Slot == ActionPlanSlot.Facing && slot.ValueKind == PlanValueKind.Direction);
        Assert.Contains(blockingEntity.SlotReads, slot => slot.Slot == ActionPlanSlot.Facing && slot.ValueKind == PlanValueKind.Direction);
        Assert.Contains(blockingEntity.SlotWrites, slot => slot.Slot == ActionPlanSlot.Target && slot.ValueKind == PlanValueKind.Entity);
        Assert.Contains(pickup.SlotReads, slot => slot.Slot == ActionPlanSlot.Target && slot.ValueKind == PlanValueKind.Entity);
        Assert.Contains(reverse.SlotReads, slot => slot.Slot == ActionPlanSlot.Facing && slot.ValueKind == PlanValueKind.Direction);
        Assert.Contains(reverse.SlotWrites, slot => slot.Slot == ActionPlanSlot.Facing && slot.ValueKind == PlanValueKind.Direction);
    }

    [Fact]
    public void ActionStepCatalogExposesAllCanonicalActionStepKinds()
    {
        Assert.Equal(
            Enum.GetValues<ActionPlanBehaviorStepKind>().Order(),
            ActionStepCatalog.Steps.Select(step => step.Kind).Order());
    }

    [Fact]
    public void ActionStepCatalogDescribesMoveFacingMetadata()
    {
        var moveFacing = ActionStepCatalog.Get(ActionPlanBehaviorStepKind.MoveFacing);

        Assert.Equal("Move Facing", moveFacing.DisplayName);
        Assert.Equal(ActionStepAuthoringTier.Stable, moveFacing.Tier);
        Assert.Contains("blocked", moveFacing.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(moveFacing.RequiredState, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);
        Assert.Contains(moveFacing.DefaultableState, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);
        Assert.Contains(moveFacing.StateWrites, state => state.Slot == ActionPlanSlot.Target && state.ValueKind == PlanValueKind.Entity);
    }

    [Fact]
    public void ActionStepCatalogDescribesBackstepMetadata()
    {
        var backstep = ActionStepCatalog.Get(ActionPlanBehaviorStepKind.Backstep);

        Assert.Equal("Backstep", backstep.DisplayName);
        Assert.Equal(ActionStepAuthoringTier.Stable, backstep.Tier);
        Assert.Contains("opposite", backstep.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("preserving", backstep.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(backstep.RequiredState, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);
        Assert.Contains(backstep.DefaultableState, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);
        Assert.Contains(backstep.StateWrites, state => state.Slot == ActionPlanSlot.Target && state.ValueKind == PlanValueKind.Entity);
        Assert.DoesNotContain(backstep.StateWrites, state => state.Slot == ActionPlanSlot.Facing);
    }

    [Fact]
    public void ActionStepCatalogDescribesPickupTargetMetadata()
    {
        var pickupTarget = ActionStepCatalog.Get(ActionPlanBehaviorStepKind.PickupTarget);

        Assert.Equal("Pickup Target", pickupTarget.DisplayName);
        Assert.Equal(ActionStepAuthoringTier.Stable, pickupTarget.Tier);
        Assert.Contains("pick up", pickupTarget.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(pickupTarget.RequiredState, state => state.Slot == ActionPlanSlot.Target && state.ValueKind == PlanValueKind.Entity);
        Assert.Contains(pickupTarget.DefaultableState, state => state.Slot == ActionPlanSlot.Target && state.ValueKind == PlanValueKind.Entity);
        Assert.Empty(pickupTarget.StateWrites);
    }

    [Theory]
    [InlineData(ActionPlanBehaviorStepKind.DropFacing, "Drop Facing")]
    [InlineData(ActionPlanBehaviorStepKind.PushFacing, "Push Facing")]
    [InlineData(ActionPlanBehaviorStepKind.DestroyTarget, "Destroy Target")]
    [InlineData(ActionPlanBehaviorStepKind.CreateFacing, "Create Facing")]
    [InlineData(ActionPlanBehaviorStepKind.TurnLeft, "Turn Left")]
    [InlineData(ActionPlanBehaviorStepKind.TurnRight, "Turn Right")]
    [InlineData(ActionPlanBehaviorStepKind.ReverseFacing, "Reverse Facing")]
    [InlineData(ActionPlanBehaviorStepKind.AcquireNearestTarget, "Acquire Nearest Target")]
    [InlineData(ActionPlanBehaviorStepKind.SeekTarget, "Seek Target")]
    [InlineData(ActionPlanBehaviorStepKind.FleeTarget, "Flee Target")]
    [InlineData(ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo, "Maintain Chebyshev Distance Two")]
    [InlineData(ActionPlanBehaviorStepKind.StrafeClockwise, "Strafe Clockwise")]
    [InlineData(ActionPlanBehaviorStepKind.StrafeAnticlockwise, "Strafe Anticlockwise")]
    [InlineData(ActionPlanBehaviorStepKind.GiveTarget, "Give Target")]
    [InlineData(ActionPlanBehaviorStepKind.TakeTarget, "Take Target")]
    [InlineData(ActionPlanBehaviorStepKind.EnterTarget, "Enter Target")]
    [InlineData(ActionPlanBehaviorStepKind.ExitFacing, "Exit Facing")]
    public void ActionStepCatalogDescribesFirstUtilityBatch(ActionPlanBehaviorStepKind kind, string displayName)
    {
        var step = ActionStepCatalog.Get(kind);

        Assert.Equal(displayName, step.DisplayName);
        Assert.Equal(ActionStepAuthoringTier.Stable, step.Tier);
        Assert.NotEmpty(step.Description);
    }

    [Theory]
    [InlineData(ActionPlanBehaviorStepKind.TurnLeft, Direction.North, Direction.West)]
    [InlineData(ActionPlanBehaviorStepKind.TurnLeft, Direction.West, Direction.South)]
    [InlineData(ActionPlanBehaviorStepKind.TurnRight, Direction.North, Direction.East)]
    [InlineData(ActionPlanBehaviorStepKind.TurnRight, Direction.East, Direction.South)]
    [InlineData(ActionPlanBehaviorStepKind.ReverseFacing, Direction.North, Direction.South)]
    [InlineData(ActionPlanBehaviorStepKind.ReverseFacing, Direction.East, Direction.West)]
    public void ActionStepCatalogDescribesTurnFacingMetadata(ActionPlanBehaviorStepKind kind, Direction from, Direction to)
    {
        var step = ActionStepCatalog.Get(kind);

        Assert.Contains(step.RequiredState, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);
        Assert.Contains(step.DefaultableState, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);
        Assert.Contains(step.StateWrites, state => state.Slot == ActionPlanSlot.Facing && state.ValueKind == PlanValueKind.Direction);

        var world = TestWorld.CreateWorld();
        var start = world.GetEntityLocation(TestWorld.PlayerId);
        world.SetActionFacing(TestWorld.PlayerId, from);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("turn-facing"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(kind)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(to, world.GetActionFacing(TestWorld.PlayerId));
        Assert.Equal(start, world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Null(world.GetActionTarget(TestWorld.PlayerId));
    }

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
    public void PrimitiveBackedPlanWithoutFallbackTerminatesRootTurnWhenPrimitiveFails()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("move-into-wall"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.False(result.ContinuePlan);
        Assert.True(TraceContains(result.Trace, "Primitive MoveFacing"));
    }

    [Fact]
    public void PrimitiveMoveFacingMovesUsingPersistentActorFacing()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.South);
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("move-facing"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "Read slot Facing"));
        Assert.True(TraceContains(result.Trace, "Primitive MoveFacing"));
    }

    [Fact]
    public void PrimitiveMoveFacingStoresBlockingEntityAsPersistentTargetBeforeFallback()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var fallback = new ActionPlanDescriptor(
            new ActionPlanId("wait"),
            [new ActionPlanStepDescriptor("wait", [], PlanEffectDescriptor.Wait(), OnFailure: null)]);
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("move-then-wait"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing, fallback.Id));
        var registry = new Dictionary<ActionPlanId, ActionPlanDefinition>
        {
            [fallback.Id] = fallback.Materialize()
        };

        var result = new ActionPlanInterpreter(new MovementService(), registry).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "Set slot Target"));
        Assert.True(TraceContains(result.Trace, "Wait"));
    }

    [Fact]
    public void PrimitiveBackedPlanUsesExplicitFallbackWhenPrimitiveFails()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var fallback = new ActionPlanDescriptor(
            new ActionPlanId("wait"),
            [
                new ActionPlanStepDescriptor(
                    "wait",
                    [],
                    PlanEffectDescriptor.Wait(),
                    OnFailure: null)
            ]);
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("move-then-wait"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing, fallback.Id));
        var registry = new Dictionary<ActionPlanId, ActionPlanDefinition>
        {
            [fallback.Id] = fallback.Materialize()
        };

        var result = new ActionPlanInterpreter(new MovementService(), registry).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.True(TraceContains(result.Trace, "Call plan wait"));
        Assert.True(TraceContains(result.Trace, "Wait"));
    }

    [Fact]
    public void PrimitiveFallbackCyclesUsePlanCallDepthGuard()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var first = new ActionPlanDescriptor(
            new ActionPlanId("first"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing, new ActionPlanId("second")));
        var second = new ActionPlanDescriptor(
            new ActionPlanId("second"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing, new ActionPlanId("first")));
        var registry = new Dictionary<ActionPlanId, ActionPlanDefinition>
        {
            [first.Id] = first.Materialize(),
            [second.Id] = second.Materialize()
        };

        var result = new ActionPlanInterpreter(new MovementService(), registry, maxCallDepth: 2).Execute(
            world,
            TestWorld.PlayerId,
            first.Materialize(),
            new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.True(TraceContains(result.Trace, "Plan call depth exceeded"));
    }

    [Fact]
    public void PrimitivePickupTargetPicksUpPersistentActorTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("pickup-target"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PickupTarget));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Primitive PickupTarget"));
        Assert.True(TraceContains(result.Trace, "Read slot Target"));
    }

    [Fact]
    public void PrimitivePickupTargetUsesFirstAvailableInventoryCoordinateRowMajor()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 20 };
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2))));
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("pickup-target"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PickupTarget));
        var interpreter = new ActionPlanInterpreter(movement);

        world.SetActionTarget(TestWorld.PlayerId, TestWorld.RockId);
        var first = interpreter.Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var second = interpreter.Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.RockId));
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.True(TraceContains(second.Trace, "Pickup slime -> player(0,0)"));
        Assert.True(TraceDetailContains(second.Trace, "first available inventory coordinate (1,0)"));
    }

    [Fact]
    public void PrimitiveMoveFacingCanFallbackToPickupTargetUsingBlockedEntityTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var pickup = new ActionPlanDescriptor(
            new ActionPlanId("pickupTarget"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PickupTarget));
        var move = new ActionPlanDescriptor(
            new ActionPlanId("moveThenPickup"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.MoveFacing, pickup.Id));
        var registry = new Dictionary<ActionPlanId, ActionPlanDefinition>
        {
            [pickup.Id] = pickup.Materialize()
        };

        var result = new ActionPlanInterpreter(new MovementService(), registry).Execute(
            world,
            TestWorld.PlayerId,
            move.Materialize(),
            new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Set slot Target"));
        Assert.True(TraceContains(result.Trace, "Primitive PickupTarget"));
    }

    [Fact]
    public void PrimitivePickupTargetWithoutFallbackTerminatesRootTurnWhenPickupFails()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.RockId);
        var descriptor = new ActionPlanDescriptor(
            new ActionPlanId("pickup-target"),
            [],
            new ActionPlanPrimitiveDescriptor(ActionPlanPrimitiveKind.PickupTarget));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.PlayerId,
            descriptor.Materialize(),
            new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.False(result.ContinuePlan);
        Assert.True(TraceContains(result.Trace, "Primitive PickupTarget"));
    }

    [Fact]
    public void PickupEffectUsesRelocationAfterPickupValidation()
    {
        var world = TestWorld.CreateWorld();
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

    [Fact]
    public void PlanInterpreterUsesFirstSuccessfulConsumingRankedStep()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        var executed = new List<string>();
        var plan = new ActionPlanDefinition(
            new ActionPlanId("test"),
            [
                new ActionPlanStep(
                    "blocked first step",
                    [new TestPlanCheck("first check", passed: false)],
                    new RecordingPlanEffect("first effect", executed, consumesTurn: true),
                    onFailure: null),
                new ActionPlanStep(
                    "fallback step",
                    [new TestPlanCheck("second check", passed: true)],
                    new RecordingPlanEffect("fallback effect", executed, consumesTurn: true),
                    onFailure: null),
                new ActionPlanStep(
                    "unreached step",
                    [new TestPlanCheck("third check", passed: true)],
                    new RecordingPlanEffect("unreached effect", executed, consumesTurn: true),
                    onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(["fallback effect"], executed);
        Assert.Equal(TraceStatus.Success, result.Trace.Status);
        Assert.Contains(result.Trace.Children, child => child.Label == "Step blocked first step" && child.Status == TraceStatus.Failure);
        Assert.Contains(result.Trace.Children, child => child.Label == "Step fallback step" && child.Status == TraceStatus.Success);
        Assert.DoesNotContain(result.Trace.Children, child => child.Label == "Step unreached step");
    }

    [Fact]
    public void PlanInterpreterCommitsCheckVariableWritesBeforeEffect()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        Direction? effectFacing = null;
        var plan = new ActionPlanDefinition(
            new ActionPlanId("variable-test"),
            [
                new ActionPlanStep(
                    "bind facing",
                    [new TestPlanCheck("bind east", passed: true, new Dictionary<string, PlanValue>
                    {
                        ["facing"] = new DirectionPlanValue(Direction.East)
                    })],
                    new ReadDirectionEffect("read facing", "facing", value => effectFacing = value),
                    onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.Equal(Direction.East, effectFacing);
        Assert.True(context.TryGet<DirectionPlanValue>("facing", out var storedFacing));
        Assert.Equal(Direction.East, storedFacing.Value);
        Assert.True(TraceContains(result.Trace, "Set variable facing"));
    }

    [Fact]
    public void PlanInterpreterReturnsFailureWhenNoStepConsumesOrStops()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        var plan = new ActionPlanDefinition(
            new ActionPlanId("all-fail"),
            [
                new ActionPlanStep("first", [new TestPlanCheck("first check", passed: false)], onSuccess: null, onFailure: null),
                new ActionPlanStep("second", [new TestPlanCheck("second check", passed: false)], onSuccess: null, onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.False(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.Equal(TraceStatus.Failure, result.Trace.Status);
        Assert.Contains("no step", result.Trace.Detail);
    }

    [Fact]
    public void BuiltInCanMoveCheckAndMoveEffectMoveActorUsingDirectionVariable()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.South));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("move-from-variable"),
            [
                new ActionPlanStep(
                    "move facing",
                    [new CanMoveCheck("facing")],
                    new MoveEffect("facing"),
                    onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "Can move facing"));
        Assert.True(TraceContains(result.Trace, "Move facing"));
    }

    [Fact]
    public void BuiltInCanMoveCheckAndMoveEffectMoveActorUsingCanonicalFacingSlot()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.South));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("move-from-facing-slot"),
            [
                new ActionPlanStep(
                    "move facing",
                    [new CanMoveCheck()],
                    new MoveEffect(),
                    onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.True(TraceContains(result.Trace, "Read slot Facing"));
        Assert.True(TraceContains(result.Trace, "Can move Facing"));
        Assert.True(TraceContains(result.Trace, "Move Facing"));
    }

    [Fact]
    public void BuiltInCanMoveCheckFailureFallsThroughToSetVariableEffect()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.South));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("turn-around"),
            [
                new ActionPlanStep(
                    "try blocked move",
                    [new CanMoveCheck("facing")],
                    new MoveEffect("facing"),
                    onFailure: null),
                new ActionPlanStep(
                    "turn north",
                    [],
                    new SetVariableEffect("facing", new DirectionPlanValue(Direction.North), consumesTurn: false, continuePlan: false),
                    onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(TestWorld.SlimeId));
        Assert.True(context.TryGet<DirectionPlanValue>("facing", out var facing));
        Assert.Equal(Direction.North, facing.Value);
        Assert.True(TraceContains(result.Trace, "Set variable facing"));
    }

    [Fact]
    public void CallPlanEffectRunsNestedPlanWithSharedContextAndTrace()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        var childId = new ActionPlanId("child");
        var parent = new ActionPlanDefinition(
            new ActionPlanId("parent"),
            [
                new ActionPlanStep("call child", [], new CallPlanEffect(childId), onFailure: null)
            ]);
        var child = new ActionPlanDefinition(
            childId,
            [
                new ActionPlanStep(
                    "set nested variable",
                    [],
                    new SetVariableEffect("facing", new DirectionPlanValue(Direction.East), consumesTurn: true, continuePlan: false),
                    onFailure: null)
            ]);
        var interpreter = new ActionPlanInterpreter(
            new MovementService(),
            new Dictionary<ActionPlanId, ActionPlanDefinition>
            {
                [parent.Id] = parent,
                [child.Id] = child
            });

        var result = interpreter.Execute(world, TestWorld.PlayerId, parent, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.True(context.TryGet<DirectionPlanValue>("facing", out var facing));
        Assert.Equal(Direction.East, facing.Value);
        Assert.True(TraceContains(result.Trace, "Call plan child"));
        Assert.True(TraceContains(result.Trace, "Plan child"));
    }

    [Fact]
    public void CallPlanEffectFailsWithTraceWhenDepthGuardIsExceeded()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        var recursiveId = new ActionPlanId("recursive");
        var recursive = new ActionPlanDefinition(
            recursiveId,
            [
                new ActionPlanStep("call self", [], new CallPlanEffect(recursiveId), onFailure: null)
            ]);
        var interpreter = new ActionPlanInterpreter(
            new MovementService(),
            new Dictionary<ActionPlanId, ActionPlanDefinition>
            {
                [recursive.Id] = recursive
            },
            maxCallDepth: 2);

        var result = interpreter.Execute(world, TestWorld.PlayerId, recursive, context);

        Assert.False(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.Equal(TraceStatus.Failure, result.Trace.Status);
        Assert.True(TraceContains(result.Trace, "Plan call depth exceeded"));
    }

    [Fact]
    public void BlockingEntityCheckWritesTargetWhenFacingBlockedEntity()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.South));
        var check = new BlockingEntityCheck("facing", "target");

        var result = check.Evaluate(world, TestWorld.SlimeId, context, new MovementService());

        Assert.True(result.Passed);
        Assert.Equal(TraceStatus.Success, result.Trace.Status);
        Assert.True(result.VariableWrites.TryGetValue("target", out var target));
        var targetValue = Assert.IsType<EntityPlanValue>(target);
        Assert.Equal(TestWorld.PlayerId, targetValue.Value);
        Assert.Contains("player", result.Trace.Detail);
    }

    [Fact]
    public void BlockingEntityCheckWritesCanonicalTargetWhenCanonicalFacingIsBlocked()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.South));
        var check = new BlockingEntityCheck();

        var result = check.Evaluate(world, TestWorld.SlimeId, context, new MovementService());

        Assert.True(result.Passed);
        Assert.Equal(TraceStatus.Success, result.Trace.Status);
        Assert.Empty(result.VariableWrites);
        Assert.NotNull(result.SlotWrites);
        Assert.True(result.SlotWrites.TryGetValue(ActionPlanSlot.Target, out var target));
        var targetValue = Assert.IsType<EntityPlanValue>(target);
        Assert.Equal(TestWorld.PlayerId, targetValue.Value);
        Assert.Contains("Target=player", result.Trace.Detail);
    }

    [Fact]
    public void BlockingEntityCheckFailsWithoutWritingTargetWhenNoBlockerExists()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.North));
        var check = new BlockingEntityCheck("facing", "target");

        var result = check.Evaluate(world, TestWorld.SlimeId, context, new MovementService());

        Assert.False(result.Passed);
        Assert.Equal(TraceStatus.Failure, result.Trace.Status);
        Assert.Empty(result.VariableWrites);
        Assert.False(context.TryGet<EntityPlanValue>("target", out _));
    }

    [Fact]
    public void CanPickupCheckPassesForBoundAdjacentCarryableTarget()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set("target", new EntityPlanValue(TestWorld.RockId));
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1)));
        var check = new CanPickupCheck("target", new GridCoord(0, 0));

        var result = check.Evaluate(world, TestWorld.SlimeId, context, movement);

        Assert.True(result.Passed);
        Assert.Equal(TraceStatus.Success, result.Trace.Status);
        Assert.True(TraceContains(result.Trace, "Pickup rock -> slime(0,0)"));
    }

    [Fact]
    public void PickupEffectPicksUpBoundTargetIntoActorInventory()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set("target", new EntityPlanValue(TestWorld.RockId));
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("pickup-bound-target"),
            [
                new ActionPlanStep(
                    "pickup target",
                    [new CanPickupCheck("target", new GridCoord(0, 0))],
                    new PickupEffect("target", new GridCoord(0, 0)),
                    onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(TestWorld.RockId));
        Assert.True(TraceContains(result.Trace, "Pickup target"));
    }

    [Fact]
    public void CanPickupCheckAndPickupEffectUseCanonicalTargetSlot()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Target, new EntityPlanValue(TestWorld.RockId));
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("pickup-canonical-target"),
            [
                new ActionPlanStep(
                    "pickup target",
                    [new CanPickupCheck(new GridCoord(0, 0))],
                    new PickupEffect(new GridCoord(0, 0)),
                    onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(TestWorld.RockId));
        Assert.True(TraceContains(result.Trace, "Read slot Target"));
        Assert.True(TraceContains(result.Trace, "Pickup Target"));
    }

    [Fact]
    public void ReverseDirectionEffectUpdatesDirectionVariableWithoutConsumingTurn()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.West));
        var effect = new ReverseDirectionEffect("facing", consumesTurn: false, continuePlan: false);

        var result = effect.Apply(world, TestWorld.SlimeId, context, new MovementService());

        Assert.True(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.True(context.TryGet<DirectionPlanValue>("facing", out var facing));
        Assert.Equal(Direction.East, facing.Value);
        Assert.True(TraceContains(result.Trace, "Set variable facing"));
    }

    [Fact]
    public void ReverseDirectionEffectUpdatesCanonicalFacingSlotWithoutConsumingTurn()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        var effect = new ReverseDirectionEffect(consumesTurn: false, continuePlan: false);

        var result = effect.Apply(world, TestWorld.SlimeId, context, new MovementService());

        Assert.True(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.True(context.TryGet<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing));
        Assert.Equal(Direction.East, facing.Value);
        Assert.True(TraceContains(result.Trace, "Read slot Facing"));
        Assert.True(TraceContains(result.Trace, "Set slot Facing"));
    }

    [Fact]
    public void CallPlanEffectSharesCanonicalSlotsWithNestedPlan()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        context.Set(ActionPlanSlot.Facing, new DirectionPlanValue(Direction.West));
        var childId = new ActionPlanId("child-canonical");
        var parent = new ActionPlanDefinition(
            new ActionPlanId("parent-canonical"),
            [new ActionPlanStep("call child", [], new CallPlanEffect(childId), onFailure: null)]);
        var child = new ActionPlanDefinition(
            childId,
            [new ActionPlanStep("reverse facing", [], new ReverseDirectionEffect(consumesTurn: true, continuePlan: false), onFailure: null)]);
        var interpreter = new ActionPlanInterpreter(
            new MovementService(),
            new Dictionary<ActionPlanId, ActionPlanDefinition> { [child.Id] = child });

        var result = interpreter.Execute(world, TestWorld.PlayerId, parent, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.True(context.TryGet<DirectionPlanValue>(ActionPlanSlot.Facing, out var facing));
        Assert.Equal(Direction.East, facing.Value);
        Assert.True(TraceContains(result.Trace, "Call plan child-canonical"));
        Assert.True(TraceContains(result.Trace, "Set slot Facing"));
    }

    [Fact]
    public void WaitEffectConsumesTurnWithoutChangingWorldPosition()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        var plan = new ActionPlanDefinition(
            new ActionPlanId("wait"),
            [
                new ActionPlanStep("wait", [], new WaitEffect(), onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Wait"));
    }

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
    public void BackstepMovesOppositeFacingConsumesTurnAndPreservesFacing()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("backstep"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Backstep)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.Equal(Direction.North, world.GetActionFacing(TestWorld.PlayerId));
        Assert.Null(world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. Backstep: Success; fallback=stopped");
        Assert.Contains(summary, line => line == "   reads: Facing=North");
        Assert.Contains(summary, line => line == "   results: moved South; preserved Facing=North");
        Assert.True(TraceContains(result.Trace, "Move South"));
        Assert.True(TraceContains(result.Trace, "Preserve Facing"));
    }

    [Fact]
    public void BackstepBlockedByEntityWritesTargetAndFallsThrough()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.South);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("backstep-then-wait"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Backstep),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget)
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,2)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.Equal(Direction.South, world.GetActionFacing(TestWorld.PlayerId));
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. Backstep: Failure; reason=InvalidPlacement; fallback=continued");
        Assert.Contains(summary, line => line == "   reads: Facing=South");
        Assert.Contains(summary, line => line == "   writes: Target=slime");
        Assert.True(TraceContains(result.Trace, "Action Step PickupTarget"));
    }

    [Fact]
    public void BackstepOutOfBoundsFailsWithoutMeaningfulTargetWrite()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0))));
        world.SetActionFacing(TestWorld.PlayerId, Direction.South);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("backstep-out-of-bounds"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.Backstep)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(0,0)", world.FormatEntityAddress(TestWorld.PlayerId));
        Assert.Equal(Direction.South, world.GetActionFacing(TestWorld.PlayerId));
        Assert.Null(world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. Backstep: Failure; reason=MoveOutOfBounds; fallback=stopped");
        Assert.Contains(summary, line => line == "   reads: Facing=South");
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void AcquireNearestTargetSelectsNearestSamePlaneTargetAndWritesTarget()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 3))));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("acquire-nearest"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.AcquireNearestTarget)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "   writes: Target=slime");
        Assert.Contains(summary, line => line.Contains("distance=1", StringComparison.Ordinal));
        Assert.Contains(summary, line => line.Contains("tieBreak=row-major", StringComparison.Ordinal));
    }

    [Fact]
    public void AcquireNearestTargetFallsThroughWithoutOverwritingWhenNoCandidateExists()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.RockId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("acquire-none"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.AcquireNearestTarget)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.RockId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line.Contains("no same-plane target found", StringComparison.Ordinal));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void AcquireNearestTargetContinuesToSeekTargetInSameTurn()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 1))));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("acquire-then-seek"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.AcquireNearestTarget),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.SeekTarget)
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.RockId, world.GetActionTarget(TestWorld.SlimeId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 1)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.Contains(summary, line => line == "1. AcquireNearestTarget: Success; fallback=continued");
        Assert.Contains(summary, line => line == "2. SeekTarget: Success; fallback=stopped");
        Assert.Contains(summary, line => line.Contains("moved East toward rock", StringComparison.Ordinal));
    }

    [Fact]
    public void SeekTargetAdjacentFallsThroughAndPreservesTargetForDestroyTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("seek-then-destroy"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.SeekTarget),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DestroyTarget)
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.False(world.Entities.ContainsKey(TestWorld.SlimeId));
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. SeekTarget: Failure; reason=TargetNotAdjacent; fallback=continued");
        Assert.Contains(summary, line => line == "2. DestroyTarget: Success; fallback=stopped");
    }

    [Fact]
    public void SeekTargetBlockedByIncidentalEntityPreservesGoalTarget()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 4))));
        world.SetActionTarget(TestWorld.SlimeId, TestWorld.RockId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("seek-blocked"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.SeekTarget)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.SlimeId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.RockId, world.GetActionTarget(TestWorld.SlimeId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 1)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.Contains(summary, line => line == "1. SeekTarget: Failure; reason=InvalidPlacement; fallback=stopped");
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void FleeTargetMovesAwayFromTargetAndPreservesTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("flee-target"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.FleeTarget)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 3)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. FleeTarget: Success; fallback=stopped");
        Assert.Contains(summary, line => line == "   reads: Target=slime");
        Assert.Contains(summary, line => line.Contains("moved South away from slime; distance 1->2", StringComparison.Ordinal));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void FleeTargetSkipsBlockedIncreasingCandidateAndReportsBlocker()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 3))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("flee-target-blocked-candidate"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.FleeTarget)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 2)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Contains(summary, line => line.Contains("moved West away from slime; distance 1->2", StringComparison.Ordinal));
        Assert.True(TraceDetailContains(result.Trace, "South blocked"));
    }

    [Fact]
    public void FleeTargetFallsThroughWhenNoValidIncreasingMoveExists()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("flee-target-trapped-by-corner"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.FleeTarget)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. FleeTarget: Failure; reason=InvalidPlacement; fallback=stopped");
        Assert.Contains(summary, line => line.Contains("no valid distance-increasing flee step", StringComparison.Ordinal));
        Assert.True(TraceDetailContains(result.Trace, "North blocked"));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void FleeTargetInvalidTargetFallsThroughAndPreservesTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.PlayerId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("flee-target-self"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.FleeTarget)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.PlayerId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. FleeTarget: Failure; reason=TargetIsActor; fallback=stopped");
        Assert.Contains(summary, line => line.Contains("FleeTarget cannot flee self", StringComparison.Ordinal));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void MaintainChebyshevDistanceTwoBacksAwayWhenTooCloseAndPreservesTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = CreateBehaviorPlan("maintain-chebyshev-distance-two", ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 3)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. MaintainChebyshevDistanceTwo: Success; fallback=stopped");
        Assert.Contains(summary, line => line == "   reads: Target=slime");
        Assert.Contains(summary, line => line.Contains("mode=flee/back-away; moved South relative to slime; Chebyshev distance 1->2", StringComparison.Ordinal));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void MaintainChebyshevDistanceTwoClosesWhenTooFarAndPreservesTarget()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 2))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = CreateBehaviorPlan("maintain-chebyshev-distance-two-close", ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 2)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Contains(summary, line => line.Contains("mode=seek/close; moved West relative to slime; Chebyshev distance 3->2", StringComparison.Ordinal));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void MaintainChebyshevDistanceTwoFallsThroughAtExactDistance()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 3))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("maintain-chebyshev-distance-two-exact"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.FleeTarget)
            ]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 4)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. MaintainChebyshevDistanceTwo: Failure; fallback=continued");
        Assert.Contains(summary, line => line.Contains("mode=ideal-distance fallthrough; target slime; distance=2", StringComparison.Ordinal));
        Assert.Contains(summary, line => line == "2. FleeTarget: Success; fallback=stopped");
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void MaintainChebyshevDistanceTwoFallsThroughWhenNoValidImprovingMoveExists()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0))));
        Assert.True(movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = CreateBehaviorPlan("maintain-chebyshev-distance-two-trapped", ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 0)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Contains(summary, line => line.StartsWith("1. MaintainChebyshevDistanceTwo: Failure; reason=", StringComparison.Ordinal));
        Assert.Contains(summary, line => line.Contains("mode=flee/back-away; no valid Chebyshev distance-2 step", StringComparison.Ordinal));
        Assert.True(TraceDetailContains(result.Trace, "North blocked"));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void StrafeClockwiseMovesPerpendicularToSeekPrimaryAndPreservesTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = CreateBehaviorPlan("strafe-clockwise", ActionPlanBehaviorStepKind.StrafeClockwise);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. StrafeClockwise: Success; fallback=stopped");
        Assert.Contains(summary, line => line == "   reads: Target=slime");
        Assert.Contains(summary, line => line.Contains("primary=North; moved East strafing clockwise around slime", StringComparison.Ordinal));
        Assert.True(TraceDetailContains(result.Trace, "primary=North; strafe=East"));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void StrafeAnticlockwiseMovesOppositePerpendicularAndPreservesTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = CreateBehaviorPlan("strafe-anticlockwise", ActionPlanBehaviorStepKind.StrafeAnticlockwise);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 2)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. StrafeAnticlockwise: Success; fallback=stopped");
        Assert.Contains(summary, line => line.Contains("primary=North; moved West strafing anticlockwise around slime", StringComparison.Ordinal));
        Assert.True(TraceDetailContains(result.Trace, "primary=North; strafe=West"));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void StrafeClockwiseUsesSeekTargetPrimaryTieBreakOnDiagonal()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = CreateBehaviorPlan("strafe-clockwise-diagonal", ActionPlanBehaviorStepKind.StrafeClockwise);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(3, 2)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.True(TraceDetailContains(result.Trace, "primary=North; strafe=East"));
    }

    [Fact]
    public void StrafeClockwiseAdjacentTargetCanMovePerpendicularWithoutContactFailure()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("strafe-adjacent-then-destroy"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.StrafeClockwise),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DestroyTarget)
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.True(world.Entities.ContainsKey(TestWorld.SlimeId));
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. StrafeClockwise: Success; fallback=stopped");
        Assert.DoesNotContain(summary, line => line.StartsWith("2. DestroyTarget", StringComparison.Ordinal));
    }

    [Fact]
    public void StrafeClockwiseBlockedSelectedDestinationFallsThroughAndPreservesTarget()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = CreateBehaviorPlan("strafe-clockwise-blocked", ActionPlanBehaviorStepKind.StrafeClockwise);

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.SlimeId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. StrafeClockwise: Failure; reason=InvalidPlacement; fallback=stopped");
        Assert.Contains(summary, line => line.Contains("primary=North; strafe=East blocked", StringComparison.Ordinal));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

    [Fact]
    public void StrafeAnticlockwiseInvalidTargetFallsThroughAndPreservesTarget()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.PlayerId);
        var plan = CreateBehaviorPlan("strafe-anticlockwise-self", ActionPlanBehaviorStepKind.StrafeAnticlockwise);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TestWorld.PlayerId, world.GetActionTarget(TestWorld.PlayerId));
        Assert.Contains(summary, line => line == "1. StrafeAnticlockwise: Failure; reason=TargetIsActor; fallback=stopped");
        Assert.Contains(summary, line => line.Contains("StrafeAnticlockwise cannot target self", StringComparison.Ordinal));
        Assert.DoesNotContain(summary, line => line.Contains("writes:", StringComparison.Ordinal));
    }

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

    [Fact]
    public void PushFacingMovesBlockerAndActorAndConsumesTurn()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("push-facing"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PushFacing)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 1)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 0)), world.GetEntityLocation(TestWorld.SlimeId));
        Assert.True(TraceContains(result.Trace, "Action Step PushFacing"));
    }

    [Fact]
    public void DestroyTargetRecursivelyRemovesTargetInventoryAndContainedEntities()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("destroy-target"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DestroyTarget)]));

        var result = new ActionPlanInterpreter(movement).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.False(world.Entities.ContainsKey(TestWorld.SlimeId));
        Assert.False(world.Entities.ContainsKey(TestWorld.RockId));
        Assert.False(world.Planes.ContainsKey(TestWorld.SlimeInventoryPlaneId));
        Assert.DoesNotContain(TestWorld.SlimeId, world.Occupancy.Values);
        Assert.DoesNotContain(TestWorld.RockId, world.Occupancy.Values);
        Assert.True(TraceContains(result.Trace, "Action Step DestroyTarget"));
    }

    [Fact]
    public void CreateFacingCreatesPlaceholderRockEntityInFacingDirection()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("create-facing"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.CreateFacing)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        var created = world.GetOccupant(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)));
        Assert.NotNull(created);
        Assert.Equal("Placeholder Rock", world.Entities[created!.Value].Name);
        Assert.True(TraceContains(result.Trace, "Action Step CreateFacing"));
    }

    [Fact]
    public void TurnFacingTraceFormatterSummarizesFacingReadAndWrite()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("turn-left"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TurnLeft)]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, TestWorld.PlayerId, plan, new ActionPlanContext());

        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.Collection(
            summary,
            line => Assert.Equal("Plan turn-left: Success; consumedTurn=True; continuePlan=False", line),
            line => Assert.Equal("1. TurnLeft: Success; fallback=stopped", line),
            line => Assert.Equal("   reads: Facing=North", line),
            line => Assert.Equal("   writes: Facing=West", line),
            line => Assert.Equal("Terminal: succeeded; consumed turn", line));
    }

    [Fact]
    public void EmptyBehaviorChainResolvesAsNoTurn()
    {
        var world = TestWorld.CreateWorld();
        var plan = new ActionPlanDefinition(
            new ActionPlanId("empty-behavior"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.SlimeId,
            plan,
            new ActionPlanContext());

        Assert.False(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.False(result.ContinuePlan);
        Assert.Equal(TraceStatus.Success, result.Trace.Status);
        Assert.Contains("no action steps", result.Trace.Detail);
    }

    [Fact]
    public void PickupTargetPrimitiveDefaultsMissingTargetToSelf()
    {
        var world = TestWorld.CreateWorld();
        var context = new ActionPlanContext();
        var plan = new ActionPlanDefinition(
            new ActionPlanId("pickup-self"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget)
            ]));

        _ = new ActionPlanInterpreter(new MovementService()).Execute(
            world,
            TestWorld.SlimeId,
            plan,
            context);

        Assert.True(context.TryGet<EntityPlanValue>(ActionPlanSlot.Target, out var target));
        Assert.Equal(TestWorld.SlimeId, target.Value);
    }

    [Fact]
    public void InterpretedEntityActionPlanCanBeScheduledByTurnService()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.West));
        var (wandering, _, registry) = CreateWanderingPlanDefinitions();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [TestWorld.SlimeId] = new InterpretedEntityActionPlan(wandering, context, registry)
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Slime@world(0,1)", world.FormatEntityAddress(TestWorld.SlimeId));
        Assert.True(TraceContains(world.LastTrace!, "Plan wandering"));
        Assert.True(TraceContains(world.LastTrace!, "Move facing"));
    }

    [Fact]
    public void InterpretedWanderingPlanCallsNestedPickupPlanForBlockingCarryableTarget()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.West));
        var (wandering, _, registry) = CreateWanderingPlanDefinitions();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [TestWorld.SlimeId] = new InterpretedEntityActionPlan(wandering, context, registry)
            });
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(0, 1)));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(TestWorld.SlimeId));
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(TestWorld.RockId));
        Assert.True(TraceContains(world.LastTrace!, "Call plan handleBlocker"));
        Assert.True(TraceContains(world.LastTrace!, "Plan handleBlocker"));
        Assert.True(context.TryGet<EntityPlanValue>("target", out var target));
        Assert.Equal(TestWorld.RockId, target.Value);
    }

    private static (ActionPlanDefinition Wandering, ActionPlanDefinition HandleBlocker, IReadOnlyDictionary<ActionPlanId, ActionPlanDefinition> Registry) CreateWanderingPlanDefinitions()
    {
        var wanderingId = new ActionPlanId("wandering");
        var handleBlockerId = new ActionPlanId("handleBlocker");
        var wandering = new ActionPlanDefinition(
            wanderingId,
            [
                new ActionPlanStep(
                    "move facing",
                    [new CanMoveCheck("facing")],
                    new MoveEffect("facing"),
                    onFailure: null),
                new ActionPlanStep(
                    "handle blocker",
                    [new BlockingEntityCheck("facing", "target")],
                    new CallPlanEffect(handleBlockerId),
                    new ReverseDirectionEffect("facing", consumesTurn: false, continuePlan: true)),
                new ActionPlanStep("wait", [], new WaitEffect(), onFailure: null)
            ]);
        var handleBlocker = new ActionPlanDefinition(
            handleBlockerId,
            [
                new ActionPlanStep(
                    "pickup blocker",
                    [new CanPickupCheck("target", new GridCoord(0, 0))],
                    new PickupEffect("target", new GridCoord(0, 0)),
                    onFailure: null),
                new ActionPlanStep(
                    "reverse after bump",
                    [],
                    new ReverseDirectionEffect("facing", consumesTurn: true, continuePlan: false),
                    onFailure: null)
            ]);

        return (wandering, handleBlocker, new Dictionary<ActionPlanId, ActionPlanDefinition>
        {
            [wandering.Id] = wandering,
            [handleBlocker.Id] = handleBlocker
        });
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

    private static ActionPlanDefinition CreateBehaviorPlan(string id, ActionPlanBehaviorStepKind stepKind) =>
        new(
            new ActionPlanId(id),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(stepKind)]));

    private static EntityId AddEntityWithInventory(
        WorldState world,
        string id,
        string name,
        PlaneCoord location,
        int inventoryWidth,
        int inventoryHeight,
        int carryingCapacity)
    {
        var entityId = AddEntity(world, id, name, location, inventoryWidth, inventoryHeight, bulk: 1, aperture: carryingCapacity);
        var inventoryPlaneId = new PlaneId($"{id}Inventory");
        AddPlane(world, inventoryPlaneId, inventoryWidth, inventoryHeight);
        world.RegisterInventoryPlane(entityId, inventoryPlaneId);
        return entityId;
    }

    private static EntityId AddEntity(
        WorldState world,
        string id,
        string name,
        PlaneCoord location,
        int inventoryWidth = 0,
        int inventoryHeight = 0,
        int bulk = 1,
        int aperture = 1)
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

    private sealed record TestPlanCheck(
        string Label,
        bool passed,
        IReadOnlyDictionary<string, PlanValue>? Writes = null) : IPlanCheck
    {
        public PlanCheckResult Evaluate(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement) =>
            new(passed, Writes ?? new Dictionary<string, PlanValue>(), new TraceNode(Label, passed ? TraceStatus.Success : TraceStatus.Failure));
    }

    private sealed class RecordingPlanEffect(string label, List<string> executed, bool consumesTurn) : IPlanEffect
    {
        public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
        {
            executed.Add(label);

            return new PlanEffectResult(
                Succeeded: true,
                ConsumesTurn: consumesTurn,
                ContinuePlan: !consumesTurn,
                TraceNode.Success(label));
        }
    }

    private sealed class ReadDirectionEffect(string label, string variableName, Action<Direction> read) : IPlanEffect
    {
        public PlanEffectResult Apply(WorldState world, EntityId actorId, ActionPlanContext context, MovementService movement)
        {
            if (!context.TryGet<DirectionPlanValue>(variableName, out var value))
            {
                return new PlanEffectResult(false, ConsumesTurn: false, ContinuePlan: false, TraceNode.Failure(label, FailureReason.None, $"missing {variableName}"));
            }

            read(value.Value);

            return new PlanEffectResult(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success(label, value.Value.ToString()));
        }
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
