using GameGameGame.Core;
using WorldBuilder = GameGameGame.Content.PrototypeContent;

namespace GameGameGame.Tests;

public sealed class ActionPlanRefactorTests
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
    public void ActionPlanDescriptorMaterializesExecutableBuiltIns()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
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
            WorldBuilder.PlayerId,
            descriptor.Materialize(),
            context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(WorldBuilder.PlayerId));
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
        var world = WorldBuilder.CreateFirstSlice().World;
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

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, WorldBuilder.PlayerId, plan, context);

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
        var world = WorldBuilder.CreateFirstSlice().World;
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

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, WorldBuilder.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.Equal(Direction.East, effectFacing);
        Assert.True(context.TryGet<DirectionPlanValue>("facing", out var storedFacing));
        Assert.Equal(Direction.East, storedFacing.Value);
        Assert.True(TraceContains(result.Trace, "Set variable facing"));
    }

    [Fact]
    public void PlanInterpreterReturnsFailureWhenNoStepConsumesOrStops()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var context = new ActionPlanContext();
        var plan = new ActionPlanDefinition(
            new ActionPlanId("all-fail"),
            [
                new ActionPlanStep("first", [new TestPlanCheck("first check", passed: false)], onSuccess: null, onFailure: null),
                new ActionPlanStep("second", [new TestPlanCheck("second check", passed: false)], onSuccess: null, onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, WorldBuilder.PlayerId, plan, context);

        Assert.False(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.Equal(TraceStatus.Failure, result.Trace.Status);
        Assert.Contains("no step", result.Trace.Detail);
    }

    [Fact]
    public void BuiltInCanMoveCheckAndMoveEffectMoveActorUsingDirectionVariable()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
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

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, WorldBuilder.PlayerId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Player@world(1,3)", world.FormatEntityAddress(WorldBuilder.PlayerId));
        Assert.True(TraceContains(result.Trace, "Can move facing"));
        Assert.True(TraceContains(result.Trace, "Move facing"));
    }

    [Fact]
    public void BuiltInCanMoveCheckFailureFallsThroughToSetVariableEffect()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
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

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, WorldBuilder.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.True(context.TryGet<DirectionPlanValue>("facing", out var facing));
        Assert.Equal(Direction.North, facing.Value);
        Assert.True(TraceContains(result.Trace, "Set variable facing"));
    }

    [Fact]
    public void CallPlanEffectRunsNestedPlanWithSharedContextAndTrace()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
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

        var result = interpreter.Execute(world, WorldBuilder.PlayerId, parent, context);

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
        var world = WorldBuilder.CreateFirstSlice().World;
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

        var result = interpreter.Execute(world, WorldBuilder.PlayerId, recursive, context);

        Assert.False(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.Equal(TraceStatus.Failure, result.Trace.Status);
        Assert.True(TraceContains(result.Trace, "Plan call depth exceeded"));
    }

    [Fact]
    public void BlockingEntityCheckWritesTargetWhenFacingBlockedEntity()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.South));
        var check = new BlockingEntityCheck("facing", "target");

        var result = check.Evaluate(world, WorldBuilder.SlimeId, context, new MovementService());

        Assert.True(result.Passed);
        Assert.Equal(TraceStatus.Success, result.Trace.Status);
        Assert.True(result.VariableWrites.TryGetValue("target", out var target));
        var targetValue = Assert.IsType<EntityPlanValue>(target);
        Assert.Equal(WorldBuilder.PlayerId, targetValue.Value);
        Assert.Contains("player", result.Trace.Detail);
    }

    [Fact]
    public void BlockingEntityCheckFailsWithoutWritingTargetWhenNoBlockerExists()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.North));
        var check = new BlockingEntityCheck("facing", "target");

        var result = check.Evaluate(world, WorldBuilder.SlimeId, context, new MovementService());

        Assert.False(result.Passed);
        Assert.Equal(TraceStatus.Failure, result.Trace.Status);
        Assert.Empty(result.VariableWrites);
        Assert.False(context.TryGet<EntityPlanValue>("target", out _));
    }

    [Fact]
    public void CanPickupCheckPassesForBoundAdjacentCarryableTarget()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set("target", new EntityPlanValue(WorldBuilder.RockId));
        movement.TryPlace(world, WorldBuilder.RockId, new PlaneCoord(WorldBuilder.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, WorldBuilder.RockId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 1)));
        var check = new CanPickupCheck("target", new GridCoord(0, 0));

        var result = check.Evaluate(world, WorldBuilder.SlimeId, context, movement);

        Assert.True(result.Passed);
        Assert.Equal(TraceStatus.Success, result.Trace.Status);
        Assert.True(TraceContains(result.Trace, "Pickup rock -> slime(0,0)"));
    }

    [Fact]
    public void CanPickupCheckFailsForBoundTargetThatIsTooHeavy()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set("target", new EntityPlanValue(WorldBuilder.PlayerId));
        movement.TryPlace(world, WorldBuilder.PlayerId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(2, 1)));
        var check = new CanPickupCheck("target", new GridCoord(0, 0));

        var result = check.Evaluate(world, WorldBuilder.SlimeId, context, movement);

        Assert.False(result.Passed);
        Assert.Equal(TraceStatus.Failure, result.Trace.Status);
        Assert.Equal(FailureReason.CapacityExceeded, result.Trace.Reason);
    }

    [Fact]
    public void PickupEffectPicksUpBoundTargetIntoActorInventory()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set("target", new EntityPlanValue(WorldBuilder.RockId));
        movement.TryPlace(world, WorldBuilder.RockId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 1)));
        var plan = new ActionPlanDefinition(
            new ActionPlanId("pickup-bound-target"),
            [
                new ActionPlanStep(
                    "pickup target",
                    [new CanPickupCheck("target", new GridCoord(0, 0))],
                    new PickupEffect("target", new GridCoord(0, 0)),
                    onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(movement).Execute(world, WorldBuilder.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(WorldBuilder.RockId));
        Assert.True(TraceContains(result.Trace, "Pickup target"));
    }

    [Fact]
    public void ReverseDirectionEffectUpdatesDirectionVariableWithoutConsumingTurn()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.West));
        var effect = new ReverseDirectionEffect("facing", consumesTurn: false, continuePlan: false);

        var result = effect.Apply(world, WorldBuilder.SlimeId, context, new MovementService());

        Assert.True(result.Succeeded);
        Assert.False(result.ConsumesTurn);
        Assert.True(context.TryGet<DirectionPlanValue>("facing", out var facing));
        Assert.Equal(Direction.East, facing.Value);
        Assert.True(TraceContains(result.Trace, "Set variable facing"));
    }

    [Fact]
    public void WaitEffectConsumesTurnWithoutChangingWorldPosition()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var context = new ActionPlanContext();
        var plan = new ActionPlanDefinition(
            new ActionPlanId("wait"),
            [
                new ActionPlanStep("wait", [], new WaitEffect(), onFailure: null)
            ]);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, WorldBuilder.SlimeId, plan, context);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.True(TraceContains(result.Trace, "Wait"));
    }

    [Fact]
    public void InterpretedEntityActionPlanCanBeScheduledByTurnService()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.West));
        var (wandering, _, registry) = CreateWanderingPlanDefinitions();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = new InterpretedEntityActionPlan(wandering, context, registry)
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Slime@world(0,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.True(TraceContains(world.LastTrace!, "Plan wandering"));
        Assert.True(TraceContains(world.LastTrace!, "Move facing"));
    }

    [Fact]
    public void InterpretedWanderingPlanCallsNestedPickupPlanForBlockingCarryableTarget()
    {
        var world = WorldBuilder.CreateFirstSlice().World;
        var movement = new MovementService();
        var context = new ActionPlanContext();
        context.Set("facing", new DirectionPlanValue(Direction.West));
        var (wandering, _, registry) = CreateWanderingPlanDefinitions();
        var turns = new TurnService(
            movement,
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [WorldBuilder.SlimeId] = new InterpretedEntityActionPlan(wandering, context, registry)
            });
        movement.TryPlace(world, WorldBuilder.RockId, new PlaneCoord(WorldBuilder.GameInventoryPlaneId, new GridCoord(0, 1)));

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal("Slime@world(1,1)", world.FormatEntityAddress(WorldBuilder.SlimeId));
        Assert.Equal("Rock@slime(0,0)", world.FormatEntityAddress(WorldBuilder.RockId));
        Assert.True(TraceContains(world.LastTrace!, "Call plan handleBlocker"));
        Assert.True(TraceContains(world.LastTrace!, "Plan handleBlocker"));
        Assert.True(context.TryGet<EntityPlanValue>("target", out var target));
        Assert.Equal(WorldBuilder.RockId, target.Value);
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
}
