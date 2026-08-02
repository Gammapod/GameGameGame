using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed partial class PrototypeActionStepReferenceTests
{
    // Quarantined prototype/legacy Action Step reference coverage.
    // These tests intentionally preserve compatibility observations for broad prototype-era
    // behavior while current vertical-slice work extracts canonical/release-facing actions
    // into focused suites. Move tests out of this suite only when a future canonical
    // promotion, migration, or retirement plan selects that behavior.

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
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Bulk = 4 };
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

}
