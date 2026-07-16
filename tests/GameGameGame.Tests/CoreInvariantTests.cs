using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class CoreInvariantTests
{
    [Fact]
    public void EntityLocationsAreRepresentedByNodeOccupancy()
    {
        var world = TestWorld.CreateWorld();
        var playerLocation = world.GetEntityLocation(TestWorld.PlayerId);
        var playerNodeId = world.GetNodeId(playerLocation);

        Assert.Equal(world.Entities[TestWorld.PlayerId].OccupiedNodeId, playerNodeId);
        Assert.Equal(TestWorld.PlayerId, world.Occupancy[playerNodeId]);
    }

    [Fact]
    public void MovementCannotPlaceEntityOnOccupiedNode()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        var occupiedLocation = world.GetEntityLocation(TestWorld.PlayerId);
        var originalRockLocation = world.GetEntityLocation(TestWorld.RockId);

        var placed = movement.TryPlace(world, TestWorld.RockId, occupiedLocation);

        Assert.False(placed);
        Assert.Equal(originalRockLocation, world.GetEntityLocation(TestWorld.RockId));
        Assert.Equal(TestWorld.PlayerId, world.GetOccupant(occupiedLocation));
    }

    [Fact]
    public void AdjacencyAllowsUnblockedIntercardinalNeighbor()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();

        var evaluation = movement.EvaluateAdjacency(world, TestWorld.PlayerId, TestWorld.RockId);

        Assert.True(evaluation.AreAdjacent);
        Assert.Equal(Direction.NorthEast, evaluation.Direction);
        Assert.True(evaluation.IsIntercardinal);
    }

    [Fact]
    public void AdjacencyRejectsIntercardinalNeighborWhenBothCornersAreBlocked()
    {
        var world = TestWorld.CreateWorld();
        var blockerId = new EntityId("east-corner-blocker");
        AddEntity(world, blockerId, "East Corner Blocker", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)));
        var movement = new MovementService();

        var evaluation = movement.EvaluateAdjacency(world, TestWorld.PlayerId, TestWorld.RockId);

        Assert.False(evaluation.AreAdjacent);
        Assert.Equal(Direction.NorthEast, evaluation.Direction);
        Assert.Equal(FailureReason.MoveBlocked, evaluation.FailureReason);
        Assert.Contains("blocked by both orthogonal corners", evaluation.FailureDetail);
    }

    [Fact]
    public void ZeroInventoryDimensionMakesInventoryUnusable()
    {
        var zeroWidth = new Entity(new EntityId("zeroWidth"), "Zero Width", new NodeId("node"), 0, 1, 0, 0);
        var zeroHeight = new Entity(new EntityId("zeroHeight"), "Zero Height", new NodeId("node"), 1, 0, 0, 0);

        Assert.False(zeroWidth.HasUsableInventory);
        Assert.False(zeroHeight.HasUsableInventory);
    }

    [Fact]
    public void PickupFailsWhenTargetBulkExceedsAperture()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 2 };
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Bulk = 3 };
        var action = new PickupAction(TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));

        var evaluation = action.Evaluate(world, TestWorld.PlayerId, movement);

        Assert.False(evaluation.CanExecute);
        Assert.Equal(FailureReason.ApertureBlocked, evaluation.Trace.Reason);
    }

    [Fact]
    public void PickupRejectsIntercardinalTargetWhenBothCornersAreBlocked()
    {
        var world = TestWorld.CreateWorld();
        var blockerId = new EntityId("east-corner-blocker");
        AddEntity(world, blockerId, "East Corner Blocker", new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2)));
        var movement = new MovementService();
        var action = new PickupAction(TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0)));

        var evaluation = action.Evaluate(world, TestWorld.PlayerId, movement);

        Assert.False(evaluation.CanExecute);
        Assert.Equal(FailureReason.TargetNotAdjacent, evaluation.Trace.Reason);
        Assert.Contains("blocked by both orthogonal corners", evaluation.Trace.Detail);
    }

    [Fact]
    public void TurnServiceOnlySchedulesEntitiesWithActionPlans()
    {
        var world = TestWorld.CreateWorld();
        var slimePlan = new RecordingEntityActionPlan(new WaitAction());
        var turns = new TurnService(
            new MovementService(),
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [TestWorld.SlimeId] = slimePlan
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal(1, slimePlan.TurnsPlanned);
        Assert.DoesNotContain(world.LastTrace!.Children, child => child.Label.Contains("Player"));
    }

    [Fact]
    public void TurnServiceUpdatesFacingAfterSuccessfulDirectionalMovement()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var turns = new TurnService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        var acted = turns.ResolvePlan(world, TestWorld.PlayerId, PlannedActionPlan.Single(new MoveAction(Direction.East)));

        Assert.True(acted);
        Assert.Equal(Direction.East, world.GetActionFacing(TestWorld.PlayerId));
    }

    [Fact]
    public void TurnServiceDoesNotUpdateFacingAfterFailedDirectionalMovement()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var turns = new TurnService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());

        var acted = turns.ResolvePlan(world, TestWorld.PlayerId, PlannedActionPlan.Single(new MoveAction(Direction.North)));

        Assert.False(acted);
        Assert.Equal(Direction.North, world.GetActionFacing(TestWorld.PlayerId));
    }

    [Fact]
    public void PreActionPlanOverrideRunsBeforeMainPlanAndClearsAfterTurn()
    {
        var world = TestWorld.CreateWorld();
        var turns = new TurnService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());
        var pre = new RecordingIntent(new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success("pre override")));
        var main = new RecordingIntent(new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success("main plan")));
        world.SetActionPlanOverride(TestWorld.PlayerId, ActionPlanOverrideSlot.Pre, PlannedActionPlan.Single(pre));

        var acted = turns.ResolvePlan(world, TestWorld.PlayerId, PlannedActionPlan.Single(main));

        Assert.True(acted);
        Assert.Equal(1, pre.Resolutions);
        Assert.Equal(0, main.Resolutions);
        Assert.Null(world.GetActionPlanOverride(TestWorld.PlayerId, ActionPlanOverrideSlot.Pre));
        Assert.True(TraceContains(world.LastTrace!, "pre override"));
        Assert.False(TraceContains(world.LastTrace!, "main plan"));
    }

    [Fact]
    public void PreActionPlanOverrideFallsThroughToMainPlan()
    {
        var world = TestWorld.CreateWorld();
        var turns = new TurnService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());
        var pre = new RecordingIntent(new ActionResolution(false, ConsumesTurn: false, ContinuePlan: true, TraceNode.Failure("pre override", FailureReason.None)));
        var main = new RecordingIntent(new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success("main plan")));
        world.SetActionPlanOverride(TestWorld.PlayerId, ActionPlanOverrideSlot.Pre, PlannedActionPlan.Single(pre));

        var acted = turns.ResolvePlan(world, TestWorld.PlayerId, PlannedActionPlan.Single(main));

        Assert.True(acted);
        Assert.Equal(1, pre.Resolutions);
        Assert.Equal(1, main.Resolutions);
        Assert.Equal(["pre override", "main plan"], world.LastTrace!.Children.Select(child => child.Label));
    }

    [Fact]
    public void SettingOccupiedPreActionPlanOverrideReplacesExistingOverride()
    {
        var world = TestWorld.CreateWorld();
        var turns = new TurnService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());
        var first = new RecordingIntent(new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success("first pre")));
        var second = new RecordingIntent(new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success("second pre")));
        var main = new RecordingIntent(new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success("main plan")));
        world.SetActionPlanOverride(TestWorld.PlayerId, ActionPlanOverrideSlot.Pre, PlannedActionPlan.Single(first));
        world.SetActionPlanOverride(TestWorld.PlayerId, ActionPlanOverrideSlot.Pre, PlannedActionPlan.Single(second));

        var acted = turns.ResolvePlan(world, TestWorld.PlayerId, PlannedActionPlan.Single(main));

        Assert.True(acted);
        Assert.Equal(0, first.Resolutions);
        Assert.Equal(1, second.Resolutions);
        Assert.Equal(0, main.Resolutions);
    }

    [Fact]
    public void MainActionPlanOverrideReplacesDefaultMainPlanForOneTurn()
    {
        var world = TestWorld.CreateWorld();
        var turns = new TurnService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());
        var mainOverride = new RecordingIntent(new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success("main override")));
        var defaultMain = new RecordingIntent(new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success("default main")));
        world.SetActionPlanOverride(TestWorld.PlayerId, ActionPlanOverrideSlot.Main, PlannedActionPlan.Single(mainOverride));

        var acted = turns.ResolvePlan(world, TestWorld.PlayerId, PlannedActionPlan.Single(defaultMain));

        Assert.True(acted);
        Assert.Equal(1, mainOverride.Resolutions);
        Assert.Equal(0, defaultMain.Resolutions);
        Assert.Null(world.GetActionPlanOverride(TestWorld.PlayerId, ActionPlanOverrideSlot.Main));
    }

    [Fact]
    public void ScheduledMainActionPlanOverrideDoesNotPlanDefaultEntityPlan()
    {
        var world = TestWorld.CreateWorld();
        var defaultPlan = new RecordingEntityActionPlan(new WaitAction());
        var mainOverride = new RecordingIntent(new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success("main override")));
        world.SetActionPlanOverride(TestWorld.SlimeId, ActionPlanOverrideSlot.Main, PlannedActionPlan.Single(mainOverride));
        var turns = new TurnService(
            new MovementService(),
            new Dictionary<EntityId, IEntityActionPlan>
            {
                [TestWorld.SlimeId] = defaultPlan
            });

        turns.AdvanceAfterPlayerTurn(world);

        Assert.Equal(0, defaultPlan.TurnsPlanned);
        Assert.Equal(1, mainOverride.Resolutions);
    }

    [Fact]
    public void PostActionPlanOverrideRunsAfterMainPlanFallsThrough()
    {
        var world = TestWorld.CreateWorld();
        var turns = new TurnService(new MovementService(), new Dictionary<EntityId, IEntityActionPlan>());
        var main = new RecordingIntent(new ActionResolution(false, ConsumesTurn: false, ContinuePlan: true, TraceNode.Failure("main plan", FailureReason.None)));
        var post = new RecordingIntent(new ActionResolution(true, ConsumesTurn: true, ContinuePlan: false, TraceNode.Success("post override")));
        world.SetActionPlanOverride(TestWorld.PlayerId, ActionPlanOverrideSlot.Post, PlannedActionPlan.Single(post));

        var acted = turns.ResolvePlan(world, TestWorld.PlayerId, PlannedActionPlan.Single(main));

        Assert.True(acted);
        Assert.Equal(1, main.Resolutions);
        Assert.Equal(1, post.Resolutions);
        Assert.Null(world.GetActionPlanOverride(TestWorld.PlayerId, ActionPlanOverrideSlot.Post));
        Assert.Equal(["main plan", "post override"], world.LastTrace!.Children.Select(child => child.Label));
    }

    private static bool TraceContains(TraceNode trace, string label)
    {
        return trace.Label == label || trace.Children.Any(child => TraceContains(child, label));
    }

    private sealed class RecordingEntityActionPlan(IActionIntent action) : IEntityActionPlan
    {
        public int TurnsPlanned { get; private set; }

        public PlannedActionPlan PlanTurn(WorldState world, EntityId entityId, MovementService movement)
        {
            TurnsPlanned++;

            return PlannedActionPlan.Single(action);
        }
    }

    private sealed class RecordingIntent(ActionResolution resolution) : IActionIntent
    {
        public int Resolutions { get; private set; }

        public ActionEvaluation Evaluate(WorldState world, EntityId actorId, MovementService movement) =>
            new(true, TraceNode.Success("unused"));

        public void Execute(WorldState world, EntityId actorId, MovementService movement)
        {
        }

        public ActionResolution Resolve(WorldState world, EntityId actorId, MovementService movement)
        {
            Resolutions++;
            return resolution;
        }
    }

    private static void AddEntity(WorldState world, EntityId entityId, string name, PlaneCoord location)
    {
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, 0, 0, 1, 1));
        world.Occupancy.Add(nodeId, entityId);
    }
}
