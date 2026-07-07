using GameGameGame.Core;

namespace GameGameGame.Tests;

public sealed class SimulationHistorySessionTests
{
    [Fact]
    public void WorldStateClonePreservesMutableSimulationStateWithoutSharingCollections()
    {
        var world = TestWorld.CreateWorld();
        world.AdvanceTurn();
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        world.SetActionTarget(TestWorld.PlayerId, 2, TestWorld.SlimeId);
        world.SetBehaviorProvider(TestWorld.SlimeId, TestWorld.RockId);
        var trace = TraceNode.Success("original trace", "before clone");
        trace.Add(TraceNode.Info("child trace"));
        world.RecordTrace(trace);
        world.RecordTurnReport(new SimulationTurnReport(
            world.TurnNumber,
            [new TurnActionReport(TestWorld.PlayerId, "Player", Succeeded: true, ConsumedTurn: true, "Player moved.", trace)]));

        var clone = world.Clone();

        Assert.Equal(world.TurnNumber, clone.TurnNumber);
        Assert.Equal(world.GetEntityLocation(TestWorld.PlayerId), clone.GetEntityLocation(TestWorld.PlayerId));
        Assert.Equal(TestWorld.PlayerInventoryPlaneId, clone.GetRegisteredInventoryPlaneId(TestWorld.PlayerId));
        Assert.Equal(Direction.East, clone.GetActionFacing(TestWorld.PlayerId));
        Assert.Equal(TestWorld.SlimeId, clone.GetActionTarget(TestWorld.PlayerId, 2));
        Assert.Equal(TestWorld.RockId, clone.GetBehaviorProvider(TestWorld.SlimeId));
        Assert.Equal("original trace", clone.LastTrace?.Label);
        Assert.Equal("child trace", clone.LastTrace?.Children.Single().Label);
        Assert.Equal("Player moved.", clone.LastTurnReport?.Actions.Single().Summary);
        Assert.NotSame(world.Entities, clone.Entities);
        Assert.NotSame(world.Occupancy, clone.Occupancy);
        Assert.NotSame(world.ActionStates[TestWorld.PlayerId], clone.ActionStates[TestWorld.PlayerId]);
        Assert.NotSame(world.BehaviorProviders, clone.BehaviorProviders);
        Assert.NotSame(world.LastTrace, clone.LastTrace);
        Assert.NotSame(world.LastTurnReport?.Actions.Single().Trace, clone.LastTurnReport?.Actions.Single().Trace);

        Assert.True(new MovementService().TryMove(clone, TestWorld.PlayerId, Direction.East));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), clone.GetEntityLocation(TestWorld.PlayerId));
        Assert.Equal(clone.Entities[TestWorld.PlayerId].OccupiedNodeId, clone.GetNodeId(clone.GetEntityLocation(TestWorld.PlayerId)));
    }

    [Fact]
    public void SimulationHistorySessionStartsWithFrameZeroSnapshot()
    {
        var world = TestWorld.CreateWorld();

        var history = SimulationHistorySession.Start(
            world,
            TestWorld.PlayerId,
            TestWorld.WorldPlaneId,
            activeContainerId: null);

        Assert.Equal(0, history.CurrentFrameIndex);
        Assert.Equal(0, history.CurrentFrame.FrameIndex);
        Assert.Equal(0, history.CurrentFrame.WorldTurnNumber);
        Assert.Equal(TestWorld.PlayerId, history.CurrentFrame.ControlledEntityId);
        Assert.Equal(TestWorld.WorldPlaneId, history.CurrentFrame.ActivePlaneId);
        Assert.Null(history.CurrentFrame.ActiveContainerId);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), history.CurrentFrame.Snapshot.GetEntityLocation(TestWorld.PlayerId));

        Assert.True(new MovementService().TryMove(world, TestWorld.PlayerId, Direction.East));
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), history.CurrentFrame.Snapshot.GetEntityLocation(TestWorld.PlayerId));
    }

    [Fact]
    public void RollbackRestoresFrameSnapshotAndVisibleTraceContext()
    {
        var world = TestWorld.CreateWorld();
        var previousTrace = TraceNode.Success("frame 0 trace", "visible before rollback");
        world.RecordTrace(previousTrace);
        world.RecordTurnReport(new SimulationTurnReport(
            world.TurnNumber,
            [new TurnActionReport(TestWorld.PlayerId, "Player", Succeeded: true, ConsumedTurn: true, "Frame 0 action.", previousTrace)]));
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);

        Assert.True(new MovementService().TryMove(world, TestWorld.PlayerId, Direction.East));
        world.AdvanceTurn();
        var futureTrace = TraceNode.Success("future trace", "must not leak after rollback");
        world.RecordTrace(futureTrace);
        world.RecordTurnReport(new SimulationTurnReport(
            world.TurnNumber,
            [new TurnActionReport(TestWorld.PlayerId, "Player", Succeeded: true, ConsumedTurn: true, "Future action.", futureTrace)]));

        history.RollbackToFrame(0);

        Assert.Equal(0, world.TurnNumber);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Equal("frame 0 trace", world.LastTrace?.Label);
        Assert.Equal("Frame 0 action.", world.LastTurnReport?.Actions.Single().Summary);
        Assert.Equal(0, history.CurrentFrameIndex);
    }

    [Fact]
    public void RollbackPreviousFrameReportsAvailabilityAndRestoresPriorFrame()
    {
        var world = TestWorld.CreateWorld();
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);
        var commands = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        Assert.False(history.CanRollback);
        Assert.False(history.RollbackPreviousFrame());

        history.SubmitControlledCommand(commands, ControlledActorCommand.Move(Direction.East));

        Assert.True(history.CanRollback);
        Assert.True(history.RollbackPreviousFrame());
        Assert.False(history.CanRollback);
        Assert.Equal(0, history.CurrentFrameIndex);
        Assert.Equal(0, world.TurnNumber);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), world.GetEntityLocation(TestWorld.PlayerId));
    }

    [Fact]
    public void SubmitSuccessfulControlledCommandCreatesIntervalAndNextFrame()
    {
        var world = TestWorld.CreateWorld();
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);
        var commands = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        var result = history.SubmitControlledCommand(commands, ControlledActorCommand.Move(Direction.East));

        Assert.True(result.Succeeded);
        Assert.True(result.AdvancedTurn);
        Assert.Equal(1, world.TurnNumber);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), world.GetEntityLocation(TestWorld.PlayerId));
        Assert.Equal(1, history.CurrentFrameIndex);
        Assert.Equal(2, history.Frames.Count);
        Assert.Single(history.Intervals);
        var interval = history.Intervals.Single();
        Assert.Equal(0, interval.FromFrameIndex);
        Assert.Equal(1, interval.ToFrameIndex);
        Assert.NotNull(interval.ControlledResult);
        var controlledResult = interval.ControlledResult;
        Assert.Equal(ControlledActorCommandKind.Move, controlledResult.Kind);
        Assert.Equal(TestWorld.PlayerId, controlledResult.ActorId);
        Assert.Same(result, controlledResult);
        Assert.Equal(1, history.CurrentFrame.WorldTurnNumber);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)), history.CurrentFrame.Snapshot.GetEntityLocation(TestWorld.PlayerId));
        Assert.Equal("Turn 1", history.CurrentFrame.Snapshot.LastTrace?.Label);

        history.RollbackToFrame(0);

        Assert.Equal(0, history.CurrentFrameIndex);
        Assert.Empty(history.Intervals);
        Assert.Single(history.Frames);
        Assert.Equal(0, world.TurnNumber);
        Assert.Equal(new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(1, 2)), world.GetEntityLocation(TestWorld.PlayerId));
    }

    [Fact]
    public void SubmitSuccessfulControlledCommandPreservesCommandServiceSemantics()
    {
        var directWorld = TestWorld.CreateWorld();
        var historyWorld = TestWorld.CreateWorld();
        var directCommands = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());
        var historyCommands = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());
        var history = SimulationHistorySession.Start(historyWorld, TestWorld.PlayerId, TestWorld.WorldPlaneId);

        var directResult = directCommands.Execute(directWorld, TestWorld.PlayerId, ControlledActorCommand.Move(Direction.East));
        var historyResult = history.SubmitControlledCommand(historyCommands, ControlledActorCommand.Move(Direction.East));

        Assert.Equal(directResult.Succeeded, historyResult.Succeeded);
        Assert.Equal(directResult.ConsumedTurn, historyResult.ConsumedTurn);
        Assert.Equal(directResult.AdvancedTurn, historyResult.AdvancedTurn);
        Assert.Equal(directWorld.TurnNumber, historyWorld.TurnNumber);
        Assert.Equal(directWorld.GetEntityLocation(TestWorld.PlayerId), historyWorld.GetEntityLocation(TestWorld.PlayerId));
        Assert.Equal(directWorld.LastTurnReport?.TurnNumber, historyWorld.LastTurnReport?.TurnNumber);
        Assert.Equal(directWorld.LastTurnReport?.Actions.Single().Summary, historyWorld.LastTurnReport?.Actions.Single().Summary);
    }

    [Fact]
    public void SubmitSuccessfulControlledCommandRecordsActorLogsFromTurnReport()
    {
        var world = TestWorld.CreateWorld();
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>
        {
            [TestWorld.SlimeId] = new FixedEntityActionPlan(PlannedActionPlan.Single(new WaitAction()))
        };
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);
        var commands = new ControlledActorCommandService(new MovementService(), actionPlans);

        history.SubmitControlledCommand(commands, ControlledActorCommand.Move(Direction.West));

        var interval = Assert.Single(history.Intervals);
        Assert.Collection(
            interval.ActorLogs,
            log =>
            {
                Assert.Equal(0, log.Order);
                Assert.Equal(TestWorld.PlayerId, log.ActorId);
                Assert.Equal("Player", log.ActorName);
                Assert.True(log.Succeeded);
                Assert.True(log.ConsumedTurn);
                Assert.Equal("Moved West", log.Summary);
                Assert.Equal("Resolve plan for Player", log.Trace.Label);
            },
            log =>
            {
                Assert.Equal(1, log.Order);
                Assert.Equal(TestWorld.SlimeId, log.ActorId);
                Assert.Equal("Slime", log.ActorName);
                Assert.True(log.Succeeded);
                Assert.True(log.ConsumedTurn);
                Assert.Equal("Waited", log.Summary);
                Assert.Equal("Resolve plan for Slime", log.Trace.Label);
            });
    }

    [Fact]
    public void RecordActorIntervalCreatesNextFrameWithAutonomousActorLogs()
    {
        var world = TestWorld.CreateWorld();
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);
        var trace = TraceNode.Success("Resolve plan for Slime");
        var actorLogs = new[]
        {
            new SimulationHistoryActorLog(
                0,
                TestWorld.SlimeId,
                "Slime",
                Succeeded: true,
                ConsumedTurn: true,
                ContinuePlan: false,
                "Waited",
                trace)
        };

        world.AdvanceTurn();
        history.RecordActorInterval(actorLogs, TestWorld.WorldPlaneId);

        Assert.Equal(1, history.CurrentFrameIndex);
        Assert.Equal(2, history.Frames.Count);
        var interval = Assert.Single(history.Intervals);
        Assert.Null(interval.ControlledResult);
        Assert.Same(trace, Assert.Single(interval.ActorLogs).Trace);
        Assert.Equal(1, history.CurrentFrame.WorldTurnNumber);
    }

    [Fact]
    public void SubmitFailedControlledCommandAddsCurrentFrameLogWithoutAdvancingFrameOrTurn()
    {
        var world = TestWorld.CreateWorld();
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);
        var commands = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        var result = history.SubmitControlledCommand(commands, ControlledActorCommand.Move(Direction.North));

        Assert.False(result.Succeeded);
        Assert.False(result.AdvancedTurn);
        Assert.Equal(0, world.TurnNumber);
        Assert.Equal(0, history.CurrentFrameIndex);
        Assert.Single(history.Frames);
        Assert.Empty(history.Intervals);
        var entry = Assert.Single(history.CurrentFrameLogEntries);
        Assert.Equal(0, entry.FrameIndex);
        Assert.Same(result, entry.ControlledResult);
        Assert.Equal(ControlledActorCommandKind.Move, entry.ControlledResult.Kind);
        Assert.Equal(FailureReason.InvalidPlacement, entry.ControlledResult.FailureReason);
        Assert.Equal("Move North", world.LastTrace?.Label);
    }

    [Fact]
    public void ActionLogProjectionFromHistoryIncludesSuccessfulIntervalsAndCurrentFrameFailuresInOrder()
    {
        var world = TestWorld.CreateWorld();
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);
        var commands = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        history.SubmitControlledCommand(commands, ControlledActorCommand.Move(Direction.East));
        history.SubmitControlledCommand(commands, ControlledActorCommand.Move(Direction.North));

        var log = ActionLogProjection.FromHistory(history);

        Assert.Collection(
            log.Chronological,
            outcome =>
            {
                Assert.Equal(1, outcome.TurnNumber);
                Assert.Equal(TestWorld.PlayerId, outcome.ActorId);
                Assert.True(outcome.Succeeded);
                Assert.Equal("move", outcome.ActionKind);
                Assert.Equal(Direction.East, outcome.Direction);
                Assert.Equal("Player moved East", outcome.Sentence);
                Assert.Contains(TestWorld.PlayerId, outcome.AnchorEntityIds);
                Assert.Contains(TestWorld.WorldPlaneId, outcome.AnchorPlaneIds);
            },
            outcome =>
            {
                Assert.Null(outcome.TurnNumber);
                Assert.Equal(TestWorld.PlayerId, outcome.ActorId);
                Assert.False(outcome.Succeeded);
                Assert.Equal("move", outcome.ActionKind);
                Assert.Equal(Direction.North, outcome.Direction);
                Assert.Contains("Player tried to move North", outcome.Sentence);
                Assert.Contains(TestWorld.PlayerId, outcome.AnchorEntityIds);
                Assert.Contains(TestWorld.WorldPlaneId, outcome.AnchorPlaneIds);
            });
    }

    [Fact]
    public void ActionLogProjectionFromHistoryFiltersProjectedRowsByEntityAndPlane()
    {
        var world = TestWorld.CreateWorld();
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);
        var commands = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        history.SubmitControlledCommand(commands, ControlledActorCommand.Move(Direction.East));
        history.SubmitControlledCommand(commands, ControlledActorCommand.Move(Direction.North));

        var log = ActionLogProjection.FromHistory(history);

        Assert.Equal(2, log.ForEntity(TestWorld.PlayerId).Count);
        Assert.Equal(2, log.ForPlane(TestWorld.WorldPlaneId).Count);
        Assert.Empty(log.ForEntity(TestWorld.SlimeId));
        Assert.Empty(log.ForPlane(TestWorld.PlayerInventoryPlaneId));
    }

    [Fact]
    public void ActionLogProjectionFromHistoryIncludesAutonomousActorOutcomes()
    {
        var world = TestWorld.CreateWorld();
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>
        {
            [TestWorld.SlimeId] = new FixedEntityActionPlan(PlannedActionPlan.Single(new WaitAction()))
        };
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);
        var commands = new ControlledActorCommandService(new MovementService(), actionPlans);

        history.SubmitControlledCommand(commands, ControlledActorCommand.Move(Direction.West));

        var log = ActionLogProjection.FromHistory(history);

        Assert.Collection(
            log.Chronological,
            outcome =>
            {
                Assert.Equal(TestWorld.PlayerId, outcome.ActorId);
                Assert.Equal("Player moved West", outcome.Sentence);
            },
            outcome =>
            {
                Assert.Equal(1, outcome.TurnNumber);
                Assert.Equal(TestWorld.SlimeId, outcome.ActorId);
                Assert.Equal("Slime", outcome.ActorName);
                Assert.True(outcome.Succeeded);
                Assert.True(outcome.ConsumedTurn);
                Assert.Equal("turn", outcome.ActionKind);
                Assert.Equal("Slime: Waited", outcome.Sentence);
                Assert.Contains(TestWorld.SlimeId, outcome.AnchorEntityIds);
                Assert.Contains(TestWorld.WorldPlaneId, outcome.AnchorPlaneIds);
                Assert.Empty(outcome.ActionStepAttempts);
                Assert.Equal("Resolve plan for Slime", outcome.Trace.Label);
            });
    }

    [Fact]
    public void ActionLogProjectionFromHistoryIncludesAutonomousActionStepAttemptsWhenAvailable()
    {
        var world = TestWorld.CreateWorld();
        var planTrace = new TraceNode("Plan slimePlan", TraceStatus.Success);
        var stepTrace = new TraceNode("Action Step SeekTarget", TraceStatus.Success);
        stepTrace.Add(TraceNode.Success("Primitive SeekTarget", "moved West"));
        planTrace.Add(stepTrace);
        var actionPlans = new Dictionary<EntityId, IEntityActionPlan>
        {
            [TestWorld.SlimeId] = new FixedEntityActionPlan(PlannedActionPlan.Single(new FixedResolutionIntent(
                new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, planTrace, Direction.West))))
        };
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);
        var commands = new ControlledActorCommandService(new MovementService(), actionPlans);

        history.SubmitControlledCommand(commands, ControlledActorCommand.Move(Direction.West));

        var outcome = ActionLogProjection.FromHistory(history).Chronological.Single(row => row.ActorId == TestWorld.SlimeId);
        var attempt = Assert.Single(outcome.ActionStepAttempts);
        Assert.Equal("SeekTarget", attempt.StepKind);
        Assert.Equal(TraceStatus.Success, attempt.Status);
        Assert.True(attempt.Stopped);
        Assert.Equal(["moved West"], attempt.Results);
        Assert.Same(stepTrace, attempt.Trace);
    }

    [Fact]
    public void RollbackDiscardsFailedCommandLogsFromDiscardedFutureFrames()
    {
        var world = TestWorld.CreateWorld();
        var history = SimulationHistorySession.Start(world, TestWorld.PlayerId, TestWorld.WorldPlaneId);
        var commands = new ControlledActorCommandService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        history.SubmitControlledCommand(commands, ControlledActorCommand.Move(Direction.East));
        var failedResult = history.SubmitControlledCommand(commands, ControlledActorCommand.Move(Direction.North));

        Assert.Equal(1, history.CurrentFrameIndex);
        Assert.Same(failedResult, Assert.Single(history.CurrentFrameLogEntries).ControlledResult);

        history.RollbackToFrame(0);

        Assert.Equal(0, history.CurrentFrameIndex);
        Assert.Empty(history.CurrentFrameLogEntries);
        Assert.Empty(history.GetFrameLogEntries(1));
        Assert.Single(history.Frames);
        Assert.Empty(history.Intervals);
    }

    private sealed class FixedEntityActionPlan(PlannedActionPlan plan) : IEntityActionPlan
    {
        public PlannedActionPlan PlanTurn(WorldState world, EntityId entityId, MovementService movement) => plan;
    }

    private sealed class FixedResolutionIntent(ActionResolution resolution) : IActionIntent
    {
        public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement) =>
            new(true, TraceNode.Success("unused"));

        public void Execute(WorldState world, EntityId actorId, MovementService movement)
        {
        }

        public ActionResolution Resolve(WorldState world, EntityId actorId, MovementService movement) => resolution;
    }
}
