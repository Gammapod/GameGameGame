using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class TargetPathMovementActionStepTests
{
    private static readonly PlaneId PlaneId = new("target-path-test");
    private static readonly PlaneId OtherPlaneId = new("target-path-other");
    private static readonly EntityId ActorId = new("actor");
    private static readonly EntityId TargetId = new("target");

    [Fact]
    public void TargetPathMoveSeekAdjacencyRoutesAroundMazeWithDiagonals()
    {
        var world = CreateWorld(7, 5, actor: new GridCoord(1, 1), target: new GridCoord(5, 1));
        AddBlocker(world, "wall-a", new GridCoord(2, 0));
        AddBlocker(world, "wall-b", new GridCoord(2, 1));
        AddBlocker(world, "wall-c", new GridCoord(2, 2));
        world.SetActionTarget(ActorId, TargetId);
        var plan = TargetPathPlan(ActionPlanTargetPathMode.SeekAdjacency);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, ActorId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(TargetId, world.GetActionTarget(ActorId));
        Assert.Equal(new PlaneCoord(PlaneId, new GridCoord(1, 2)), world.GetEntityLocation(ActorId));
        Assert.Equal(Direction.South, world.GetActionFacing(ActorId));
        Assert.Contains(summary, line => line == "1. TargetPathMove: Success; fallback=stopped");
        Assert.Contains(summary, line => line.Contains("moved South toward target adjacency", StringComparison.Ordinal));
    }

    [Fact]
    public void TargetPathMoveSeekFallsThroughWhenAlreadyAdjacent()
    {
        var world = CreateWorld(5, 5, actor: new GridCoord(1, 2), target: new GridCoord(2, 2));
        world.SetActionTarget(ActorId, TargetId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("target-path-seek-then-destroy"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                TargetPathStep(ActionPlanTargetPathMode.SeekAdjacency),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DestroyTarget)
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, ActorId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.False(world.Entities.ContainsKey(TargetId));
        Assert.Equal(TargetId, world.GetActionTarget(ActorId));
        Assert.Contains(summary, line => line == "1. TargetPathMove: Failure; reason=TargetNotAdjacent; fallback=continued");
        Assert.Contains(summary, line => line == "2. DestroyTarget: Success; fallback=stopped");
    }

    [Fact]
    public void TargetPathMoveSeekTreatsSourceCellLinkAsTargetAdjacency()
    {
        var world = CreateWorld(7, 4, actor: new GridCoord(1, 1), target: new GridCoord(5, 1));
        foreach (var direction in DirectionMath.AllDirections)
        {
            AddBlocker(world, $"target-ring-{direction}", new GridCoord(5, 1).Offset(direction));
        }

        var actorLocation = world.GetEntityLocation(ActorId);
        var targetLocation = world.GetEntityLocation(TargetId);
        world.SourceCellLinks.Add(new SourceCellLink(targetLocation, Direction.West, actorLocation, Direction.East));
        world.SetActionTarget(ActorId, TargetId);
        var plan = new ActionPlanDefinition(
            new ActionPlanId("target-path-source-link-then-destroy"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(
            [
                TargetPathStep(ActionPlanTargetPathMode.SeekAdjacency),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.DestroyTarget)
            ]));

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, ActorId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.False(world.Entities.ContainsKey(TargetId));
        Assert.Equal(actorLocation, world.GetEntityLocation(ActorId));
        Assert.Contains(summary, line => line == "1. TargetPathMove: Failure; reason=TargetNotAdjacent; fallback=continued");
        Assert.Contains(summary, line => line == "2. DestroyTarget: Success; fallback=stopped");
    }

    [Fact]
    public void TargetPathMoveFleeChoosesIncreasingPathDistanceFromAdjacency()
    {
        var world = CreateWorld(5, 5, actor: new GridCoord(1, 2), target: new GridCoord(3, 2));
        world.SetActionTarget(ActorId, TargetId);
        var before = DistanceToTargetAdjacency(world, world.GetEntityLocation(ActorId));
        var plan = TargetPathPlan(ActionPlanTargetPathMode.FleeAdjacency);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, ActorId, plan, new ActionPlanContext());
        var after = DistanceToTargetAdjacency(world, world.GetEntityLocation(ActorId));
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.True(after > before, $"expected flee distance to increase from {before}, but was {after}");
        Assert.Equal(TargetId, world.GetActionTarget(ActorId));
        Assert.NotNull(world.GetActionFacing(ActorId));
        Assert.Contains(summary, line => line == "1. TargetPathMove: Success; fallback=stopped");
        Assert.Contains(summary, line => line.Contains("away from target adjacency", StringComparison.Ordinal));
    }

    [Fact]
    public void TargetPathMoveFleeFallsThroughWhenNoIncreasingMoveExists()
    {
        var world = CreateWorld(2, 2, actor: new GridCoord(0, 0), target: new GridCoord(1, 1));
        world.SetActionTarget(ActorId, TargetId);
        var plan = TargetPathPlan(ActionPlanTargetPathMode.FleeAdjacency);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, ActorId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(PlaneId, new GridCoord(0, 0)), world.GetEntityLocation(ActorId));
        Assert.Equal(TargetId, world.GetActionTarget(ActorId));
        Assert.Contains(summary, line => line == "1. TargetPathMove: Failure; reason=InvalidPlacement; fallback=stopped");
        Assert.Contains(summary, line => line.Contains("no valid distance-increasing flee step", StringComparison.Ordinal));
    }

    [Fact]
    public void TargetPathMoveReportsDifferentPlaneAndUnreachableAdjacencyFailures()
    {
        var differentPlaneWorld = CreateWorld(5, 5, actor: new GridCoord(1, 1), target: new GridCoord(3, 3));
        AddPlane(differentPlaneWorld, OtherPlaneId, 3, 3);
        MoveEntity(differentPlaneWorld, TargetId, new PlaneCoord(OtherPlaneId, new GridCoord(1, 1)));
        differentPlaneWorld.SetActionTarget(ActorId, TargetId);

        var differentPlaneResult = new ActionPlanInterpreter(new MovementService()).Execute(
            differentPlaneWorld,
            ActorId,
            TargetPathPlan(ActionPlanTargetPathMode.SeekAdjacency),
            new ActionPlanContext());

        var blockedWorld = CreateWorld(5, 5, actor: new GridCoord(0, 0), target: new GridCoord(2, 2));
        foreach (var direction in DirectionMath.AllDirections)
        {
            AddBlocker(blockedWorld, $"block-{direction}", new GridCoord(2, 2).Offset(direction));
        }

        blockedWorld.SetActionTarget(ActorId, TargetId);
        var blockedResult = new ActionPlanInterpreter(new MovementService()).Execute(
            blockedWorld,
            ActorId,
            TargetPathPlan(ActionPlanTargetPathMode.SeekAdjacency),
            new ActionPlanContext());

        Assert.False(differentPlaneResult.Succeeded);
        Assert.Contains(BehaviorChainTraceFormatter.Format(differentPlaneResult), line => line.Contains("off-plane", StringComparison.Ordinal));
        Assert.False(blockedResult.Succeeded);
        Assert.Contains(BehaviorChainTraceFormatter.Format(blockedResult), line => line.Contains("no reachable target-adjacent", StringComparison.Ordinal));
    }

    [Fact]
    public void TargetPathMoveMaintainDistanceSeeksWhenTooFar()
    {
        var world = CreateWorld(9, 5, actor: new GridCoord(0, 2), target: new GridCoord(5, 2));
        world.SetActionTarget(ActorId, TargetId);
        var before = DistanceToTargetAdjacency(world, world.GetEntityLocation(ActorId));
        var plan = TargetPathPlan(ActionPlanTargetPathMode.MaintainDistance, desiredDistance: 2);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, ActorId, plan, new ActionPlanContext());
        var after = DistanceToTargetAdjacency(world, world.GetEntityLocation(ActorId));

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.True(after < before, $"expected maintain-distance seek to reduce distance from {before}, but was {after}");
        Assert.Contains(BehaviorChainTraceFormatter.Format(result), line => line.Contains("toward desired distance 2", StringComparison.Ordinal));
    }

    [Fact]
    public void TargetPathMoveMaintainDistanceFleesWhenTooClose()
    {
        var world = CreateWorld(7, 5, actor: new GridCoord(3, 1), target: new GridCoord(3, 2));
        world.SetActionTarget(ActorId, TargetId);
        var before = DistanceToTargetAdjacency(world, world.GetEntityLocation(ActorId));
        var plan = TargetPathPlan(ActionPlanTargetPathMode.MaintainDistance, desiredDistance: 2);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, ActorId, plan, new ActionPlanContext());
        var after = DistanceToTargetAdjacency(world, world.GetEntityLocation(ActorId));

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.True(after > before, $"expected maintain-distance flee to increase distance from {before}, but was {after}");
        Assert.Contains(BehaviorChainTraceFormatter.Format(result), line => line.Contains("toward desired distance 2", StringComparison.Ordinal));
    }

    [Fact]
    public void TargetPathMoveMaintainDistanceFallsThroughAtDesiredDistance()
    {
        var world = CreateWorld(7, 5, actor: new GridCoord(1, 2), target: new GridCoord(4, 2));
        world.SetActionTarget(ActorId, TargetId);
        var plan = TargetPathPlan(ActionPlanTargetPathMode.MaintainDistance, desiredDistance: 2);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, ActorId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(PlaneId, new GridCoord(1, 2)), world.GetEntityLocation(ActorId));
        Assert.Contains(summary, line => line == "1. TargetPathMove: Failure; reason=InvalidPlacement; fallback=stopped");
        Assert.Contains(summary, line => line.Contains("already at desired distance 2", StringComparison.Ordinal));
    }

    [Fact]
    public void TargetPathMoveOrbitClockwiseFollowsDeterministicRing()
    {
        var world = CreateWorld(9, 9, actor: new GridCoord(3, 1), target: new GridCoord(4, 4));
        world.SetActionTarget(ActorId, TargetId);
        var plan = TargetPathPlan(ActionPlanTargetPathMode.Orbit, desiredDistance: 2, orbitDirection: ActionPlanOrbitDirection.Clockwise);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, ActorId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(PlaneId, new GridCoord(4, 1)), world.GetEntityLocation(ActorId));
        Assert.Equal(Direction.East, world.GetActionFacing(ActorId));
        Assert.Contains(BehaviorChainTraceFormatter.Format(result), line => line.Contains("orbit Clockwise", StringComparison.Ordinal));
    }

    [Fact]
    public void TargetPathMoveOrbitAnticlockwiseFollowsDeterministicRing()
    {
        var world = CreateWorld(9, 9, actor: new GridCoord(3, 1), target: new GridCoord(4, 4));
        world.SetActionTarget(ActorId, TargetId);
        var plan = TargetPathPlan(ActionPlanTargetPathMode.Orbit, desiredDistance: 2, orbitDirection: ActionPlanOrbitDirection.Anticlockwise);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, ActorId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(PlaneId, new GridCoord(2, 1)), world.GetEntityLocation(ActorId));
        Assert.Equal(Direction.West, world.GetActionFacing(ActorId));
        Assert.Contains(BehaviorChainTraceFormatter.Format(result), line => line.Contains("orbit Anticlockwise", StringComparison.Ordinal));
    }

    [Fact]
    public void TargetPathMoveOrbitFollowsOctagonalDistanceBandsAroundCorners()
    {
        var world = CreateWorld(15, 15, actor: new GridCoord(9, 2), target: new GridCoord(7, 7));
        world.SetActionTarget(ActorId, TargetId);
        var plan = TargetPathPlan(ActionPlanTargetPathMode.Orbit, desiredDistance: 4, orbitDirection: ActionPlanOrbitDirection.Clockwise);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, ActorId, plan, new ActionPlanContext());

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(PlaneId, new GridCoord(10, 3)), world.GetEntityLocation(ActorId));
        Assert.Equal(Direction.SouthEast, world.GetActionFacing(ActorId));
        Assert.Contains(BehaviorChainTraceFormatter.Format(result), line => line.Contains("orbit Clockwise", StringComparison.Ordinal));
    }

    [Fact]
    public void TargetPathMoveOrbitCorrectsToDesiredDistanceBeforeOrbiting()
    {
        var world = CreateWorld(9, 9, actor: new GridCoord(1, 4), target: new GridCoord(4, 4));
        world.SetActionTarget(ActorId, TargetId);
        var before = DistanceToTargetAdjacency(world, world.GetEntityLocation(ActorId));
        var plan = TargetPathPlan(ActionPlanTargetPathMode.Orbit, desiredDistance: 1, orbitDirection: ActionPlanOrbitDirection.Clockwise);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, ActorId, plan, new ActionPlanContext());
        var after = DistanceToTargetAdjacency(world, world.GetEntityLocation(ActorId));

        Assert.True(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.True(after < before, $"expected orbit correction to reduce distance from {before}, but was {after}");
        Assert.Contains(BehaviorChainTraceFormatter.Format(result), line => line.Contains("corrected toward desired distance 1", StringComparison.Ordinal));
    }

    [Fact]
    public void TargetPathMoveOrbitFallsThroughWhenNextRingStepIsBlocked()
    {
        var world = CreateWorld(9, 9, actor: new GridCoord(3, 1), target: new GridCoord(4, 4));
        AddBlocker(world, "orbit-blocker", new GridCoord(4, 1));
        world.SetActionTarget(ActorId, TargetId);
        var plan = TargetPathPlan(ActionPlanTargetPathMode.Orbit, desiredDistance: 2, orbitDirection: ActionPlanOrbitDirection.Clockwise);

        var result = new ActionPlanInterpreter(new MovementService()).Execute(world, ActorId, plan, new ActionPlanContext());
        var summary = BehaviorChainTraceFormatter.Format(result);

        Assert.False(result.Succeeded);
        Assert.True(result.ConsumesTurn);
        Assert.Equal(new PlaneCoord(PlaneId, new GridCoord(3, 1)), world.GetEntityLocation(ActorId));
        Assert.Contains(summary, line => line == "1. TargetPathMove: Failure; reason=InvalidPlacement; fallback=stopped");
        Assert.Contains(summary, line => line.Contains("orbit Clockwise step East blocked", StringComparison.Ordinal));
    }

    private static ActionPlanDefinition TargetPathPlan(
        ActionPlanTargetPathMode mode,
        int? desiredDistance = null,
        ActionPlanOrbitDirection? orbitDirection = null) =>
        new(
            new ActionPlanId($"target-path-{mode}"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([TargetPathStep(mode, desiredDistance, orbitDirection)]));

    private static ActionPlanBehaviorStepDescriptor TargetPathStep(
        ActionPlanTargetPathMode mode,
        int? desiredDistance = null,
        ActionPlanOrbitDirection? orbitDirection = null) =>
        new(ActionPlanBehaviorStepKind.TargetPathMove, PathMode: mode, DesiredDistance: desiredDistance, OrbitDirection: orbitDirection);

    private static int DistanceToTargetAdjacency(WorldState world, PlaneCoord start)
    {
        var target = world.GetEntityLocation(TargetId);
        return DirectionMath.AllDirections
            .Select(direction => new PlaneCoord(target.PlaneId, target.Coord.Offset(direction)))
            .Where(coord => coord.PlaneId == start.PlaneId && world.Planes[coord.PlaneId].Contains(coord.Coord))
            .Min(coord => Math.Max(Math.Abs(coord.Coord.X - start.Coord.X), Math.Abs(coord.Coord.Y - start.Coord.Y)));
    }

    private static WorldState CreateWorld(int width, int height, GridCoord actor, GridCoord target)
    {
        var world = new WorldState();
        AddPlane(world, PlaneId, width, height);
        AddEntity(world, ActorId, "Actor", new PlaneCoord(PlaneId, actor));
        AddEntity(world, TargetId, "Target", new PlaneCoord(PlaneId, target));
        return world;
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

    private static void AddBlocker(WorldState world, string id, GridCoord coord) =>
        AddEntity(world, new EntityId(id), id, new PlaneCoord(PlaneId, coord));

    private static void AddEntity(WorldState world, EntityId entityId, string name, PlaneCoord location)
    {
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 1));
        world.Occupancy.Add(nodeId, entityId);
    }

    private static void MoveEntity(WorldState world, EntityId entityId, PlaneCoord destination)
    {
        var entity = world.Entities[entityId];
        world.Occupancy.Remove(entity.OccupiedNodeId);
        var nodeId = world.GetNodeId(destination);
        world.Occupancy.Add(nodeId, entityId);
        world.Entities[entityId] = entity with { OccupiedNodeId = nodeId };
    }
}
