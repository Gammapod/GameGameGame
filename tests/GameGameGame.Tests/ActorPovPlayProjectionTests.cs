using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class ActorPovPlayProjectionTests
{
    [Fact]
    public void ActorPovPlayProjectionComposesCurrentPlaceAndParentChainFromPointOfViewBreadcrumb()
    {
        var fixture = ActorPovFixture.Create();
        var service = new ActorPovPlayProjectionService(fixture.Appearance, fixture.ActionPlanDescriptor);

        var projection = service.Project(fixture.World, fixture.ActorId, fixture.ActionPlans);

        Assert.Equal(fixture.ActorId, projection.ControlledActor.EntityId);
        Assert.NotNull(projection.CurrentPlace);
        Assert.Equal(fixture.RoomId, projection.CurrentPlace.EntityId);
        var parent = Assert.Single(projection.ParentChain);
        Assert.Equal(fixture.ScenarioHostId, parent.Entity.EntityId);
        Assert.Equal(fixture.RoomId, parent.ChildEntityId);
        Assert.Equal(new GridCoord(0, 0), parent.ChildCoordinateInEntityInventory);
        Assert.Empty(projection.Diagnostics);
    }

    [Fact]
    public void ActorPovPlayProjectionProjectsWorldInspectionCandidatesFromCurrentPlaceContents()
    {
        var fixture = ActorPovFixture.Create();
        var service = new ActorPovPlayProjectionService(fixture.Appearance, fixture.ActionPlanDescriptor);

        var projection = service.Project(fixture.World, fixture.ActorId, fixture.ActionPlans);

        var candidate = Assert.Single(projection.WorldInspectionCandidates);
        Assert.Equal(fixture.ChestId, candidate.Entity.EntityId);
        Assert.Equal(ActorPovInspectionCandidateKind.WorldPeer, candidate.Kind);
        Assert.Equal(new GridCoord(2, 0), candidate.CoordinateInSourceInventory);
        Assert.DoesNotContain(projection.WorldInspectionCandidates, item => item.Entity.EntityId == fixture.ActorId);
        Assert.Contains(candidate.TargetAdjectives, adjective => adjective.EntityId == fixture.ChestId && adjective.Adjective == "enterable");
    }

    [Fact]
    public void ActorPovPlayProjectionProjectsActorInventoryAndCarriedInspectionCandidatesSeparately()
    {
        var fixture = ActorPovFixture.Create();
        var service = new ActorPovPlayProjectionService(fixture.Appearance, fixture.ActionPlanDescriptor);

        var projection = service.Project(fixture.World, fixture.ActorId, fixture.ActionPlans);

        Assert.NotNull(projection.ActorInventory);
        Assert.Equal(fixture.ActorId, projection.ActorInventory.EntityId);
        var carried = Assert.Single(projection.CarriedInspectionCandidates);
        Assert.Equal(fixture.BackpackId, carried.Entity.EntityId);
        Assert.Equal(ActorPovInspectionCandidateKind.ActorCarriedItem, carried.Kind);
        Assert.Equal(new GridCoord(0, 0), carried.CoordinateInSourceInventory);
    }

    [Fact]
    public void ActorPovPlayProjectionCarriesPointOfViewDiagnosticsWithoutFrontendGuessing()
    {
        var world = TestWorld.CreateWorld();
        var service = new ActorPovPlayProjectionService();

        var projection = service.Project(world, TestWorld.PlayerId, new Dictionary<EntityId, IEntityActionPlan>());

        Assert.Null(projection.CurrentPlace);
        Assert.Contains(projection.Diagnostics, diagnostic => diagnostic.Code == PointOfViewDiagnosticCode.CurrentPlaceNotFound);
    }

    [Fact]
    public void TopologyVisibilityProjectionReportsDepthLimitedReachabilityWithoutClaimingLineOfSight()
    {
        var world = TestWorld.CreateWorld();
        var service = new TopologyVisibilityProjectionService();

        var projection = service.Project(world, TestWorld.PlayerId, maxDepth: 1);

        Assert.Equal(TestWorld.PlayerId, projection.ObserverEntityId);
        Assert.Equal(new TopologyCellRef(world.GetEntityLocation(TestWorld.PlayerId)), projection.Origin);
        Assert.Contains(projection.VisibleCells, cell => cell.Cell == projection.Origin && cell.Distance == 0);
        Assert.Contains(projection.VisibleCells, cell => cell.Direction == Direction.East && cell.Distance == 1 && cell.Kind == TopologyEdgeKind.DefaultGrid);
        Assert.Contains(projection.Diagnostics, diagnostic => diagnostic.Code == TopologyVisibilityDiagnosticCode.LineOfSightNotImplemented);
    }

    [Fact]
    public void TopologyVisibilityProjectionReportsVisibleEntitiesWithinDepthLimitedReachability()
    {
        var world = TestWorld.CreateWorld();
        var service = new TopologyVisibilityProjectionService();

        var originOnly = service.Project(world, TestWorld.PlayerId, maxDepth: 0);
        var nearby = service.Project(world, TestWorld.PlayerId, maxDepth: 1);

        var originEntity = Assert.Single(originOnly.VisibleEntities);
        Assert.Equal(TestWorld.PlayerId, originEntity.EntityId);
        Assert.Equal(0, originEntity.Distance);
        Assert.Contains(nearby.VisibleEntities, entity => entity.EntityId == TestWorld.SlimeId && entity.Distance == 1);
    }

    [Fact]
    public void TopologyVisibilityProjectionUsesOctagonalDistanceBandsForPovReachability()
    {
        var world = new WorldState();
        var observerId = new EntityId("observer");
        var planeId = new PlaneId("pov-octagonal-room");
        AddPlane(world, planeId, width: 5, height: 5);
        AddEntity(world, observerId, "Observer", new PlaneCoord(planeId, new GridCoord(2, 2)));
        var service = new TopologyVisibilityProjectionService();

        var projection = service.Project(world, observerId, maxDepth: 3);

        Assert.Contains(projection.VisibleCells, cell => cell.Cell.SourceCoord == new PlaneCoord(planeId, new GridCoord(3, 3)) && cell.Distance == 1);
        Assert.Contains(projection.VisibleCells, cell => cell.Cell.SourceCoord == new PlaneCoord(planeId, new GridCoord(4, 3)) && cell.Distance == 2);
        Assert.Contains(projection.VisibleCells, cell => cell.Cell.SourceCoord == new PlaneCoord(planeId, new GridCoord(4, 4)) && cell.Distance == 3);
    }

    [Fact]
    public void TopologyVisibilityProjectionSeparatesVisibleRangeFromDimmedContextRange()
    {
        var world = TestWorld.CreateWorld();
        var service = new TopologyVisibilityProjectionService();

        var projection = service.Project(world, TestWorld.PlayerId, maxDepth: 0, contextDepth: 1);

        Assert.Contains(projection.VisibleCells, cell => cell.Cell == projection.Origin && cell.Distance == 0);
        Assert.DoesNotContain(projection.VisibleCells, cell => cell.Direction == Direction.East);
        Assert.Contains(projection.ContextCells, cell => cell.Direction == Direction.East && cell.Distance == 1);
    }

    [Fact]
    public void TopologyVisibilityProjectionReportsMissingObserverWithoutFrontendGuessing()
    {
        var service = new TopologyVisibilityProjectionService();

        var projection = service.Project(TestWorld.CreateWorld(), new EntityId("missing"), maxDepth: 1);

        Assert.Empty(projection.VisibleCells);
        Assert.Contains(projection.Diagnostics, diagnostic => diagnostic.Code == TopologyVisibilityDiagnosticCode.ObserverNotFound);
    }

    [Fact]
    public void TopologyVisibilityProjectionReportsDistinctGraphNodesForOverlappingLayoutReachability()
    {
        var world = TestWorld.CreateWorld();
        var observerId = new EntityId("observer");
        var observerSource = new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(2, 0));
        var linkedSource = new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0));
        MoveEntity(world, TestWorld.PlayerId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(4, 4)));
        AddEntity(world, observerId, "Observer", observerSource);
        world.MergedInventoryLayers.Add(new MergedInventoryLayer(
            new MergedInventoryLayerId("visibility-overlap"),
            [
                new MergedInventorySpaceContribution(TestWorld.PlayerId, new GridCoord(0, 0)),
                new MergedInventorySpaceContribution(TestWorld.SlimeId, new GridCoord(2, 0))
            ]));
        world.SourceCellLinks.Add(new SourceCellLink(observerSource, Direction.East, linkedSource, Direction.West));
        var service = new TopologyVisibilityProjectionService();

        var projection = service.Project(world, observerId, maxDepth: 1);
        var overlappingCells = projection.VisibleCells
            .Where(cell => cell.LayoutCoord == new TopologyLayoutCoord(new GridCoord(2, 0)))
            .ToList();

        Assert.Contains(overlappingCells, cell => cell.Cell == new TopologyCellRef(observerSource));
        Assert.Contains(overlappingCells, cell => cell.Cell == new TopologyCellRef(linkedSource));
        Assert.Equal(2, overlappingCells.Select(cell => cell.NodeId).Distinct().Count());
    }

    private sealed record ActorPovFixture(
        WorldState World,
        EntityId ScenarioHostId,
        EntityId RoomId,
        EntityId ActorId,
        EntityId ChestId,
        EntityId BackpackId,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> ActionPlans,
        Func<EntityId, ActionPlanDescriptor?> ActionPlanDescriptor,
        Func<EntityId, EntityInspectionAppearance> Appearance)
    {
        public static ActorPovFixture Create()
        {
            var world = new WorldState();
            var hostId = new EntityId("scenarioHost");
            var roomId = new EntityId("room");
            var actorId = new EntityId("actor");
            var chestId = new EntityId("chest");
            var backpackId = new EntityId("backpack");
            var outerPlane = new PlaneId("outerPlane");
            var hostPlane = new PlaneId("hostPlane");
            var roomPlane = new PlaneId("roomPlane");
            var actorInventoryPlane = new PlaneId("actorInventory");
            var chestPlane = new PlaneId("chestInventory");
            var backpackPlane = new PlaneId("backpackInventory");

            AddPlane(world, outerPlane, 1, 1);
            AddPlane(world, hostPlane, 1, 1);
            AddPlane(world, roomPlane, 4, 2);
            AddPlane(world, actorInventoryPlane, 2, 1);
            AddPlane(world, chestPlane, 1, 1);
            AddPlane(world, backpackPlane, 1, 1);
            AddEntity(world, hostId, "Scenario Host", new PlaneCoord(outerPlane, new GridCoord(0, 0)), 1, 1, 100, 100);
            AddEntity(world, roomId, "Room", new PlaneCoord(hostPlane, new GridCoord(0, 0)), 4, 2, 50, 50);
            AddEntity(world, actorId, "Actor", new PlaneCoord(roomPlane, new GridCoord(1, 0)), 2, 1, 1, 10);
            AddEntity(world, chestId, "Chest", new PlaneCoord(roomPlane, new GridCoord(2, 0)), 1, 1, 5, 5);
            AddEntity(world, backpackId, "Backpack", new PlaneCoord(actorInventoryPlane, new GridCoord(0, 0)), 1, 1, 1, 5);
            world.RegisterInventoryPlane(hostId, hostPlane);
            world.RegisterInventoryPlane(roomId, roomPlane);
            world.RegisterInventoryPlane(actorId, actorInventoryPlane);
            world.RegisterInventoryPlane(chestId, chestPlane);
            world.RegisterInventoryPlane(backpackId, backpackPlane);

            var plan = new ActionPlanDescriptor(
                new ActionPlanId("actorPlan"),
                [],
                Behavior: new ActionPlanBehaviorDescriptor([
                    new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.EnterTarget)
                ]));
            IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans = new Dictionary<EntityId, IEntityActionPlan>();
            var appearances = new Dictionary<EntityId, EntityInspectionAppearance>
            {
                [hostId] = new('H', PresentationColor.Gray),
                [roomId] = new('#', PresentationColor.Gray),
                [actorId] = new('@', PresentationColor.Yellow),
                [chestId] = new('c', PresentationColor.Earth),
                [backpackId] = new('b', PresentationColor.Cyan)
            };

            return new ActorPovFixture(
                world,
                hostId,
                roomId,
                actorId,
                chestId,
                backpackId,
                actionPlans,
                entityId => entityId == actorId ? plan : null,
                entityId => appearances.TryGetValue(entityId, out var appearance) ? appearance : new EntityInspectionAppearance('?', PresentationColor.Gray));
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

        private static void AddEntity(WorldState world, EntityId entityId, string name, PlaneCoord location, int inventoryWidth, int inventoryHeight, int bulk, int aperture)
        {
            var nodeId = world.GetNodeId(location);
            world.Entities.Add(entityId, new Entity(entityId, name, nodeId, inventoryWidth, inventoryHeight, bulk, aperture));
            world.Occupancy.Add(nodeId, entityId);
        }
    }

    private static void AddEntity(WorldState world, EntityId entityId, string name, PlaneCoord location)
    {
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 1));
        world.Occupancy.Add(nodeId, entityId);
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

    private static void MoveEntity(WorldState world, EntityId entityId, PlaneCoord destination)
    {
        var entity = world.Entities[entityId];
        world.Occupancy.Remove(entity.OccupiedNodeId);
        var nodeId = world.GetNodeId(destination);
        world.Occupancy.Add(nodeId, entityId);
        world.Entities[entityId] = entity with { OccupiedNodeId = nodeId };
    }
}
