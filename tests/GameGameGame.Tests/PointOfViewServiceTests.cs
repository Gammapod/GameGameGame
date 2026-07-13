using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Core)]
public sealed class PointOfViewServiceTests
{
    [Fact]
    public void PointOfViewUsesContainmentBreadcrumbsAndSelectsNearestContainerAsCurrentPlace()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0)));
        var service = new PointOfViewService();

        var result = service.Describe(world, TestWorld.RockId);

        Assert.Equal(TestWorld.RockId, result.ObserverEntityId);
        Assert.Equal(EntityContainmentPathStatus.Complete, result.Breadcrumb.Status);
        Assert.Equal([TestWorld.PlayerId, TestWorld.SlimeId, TestWorld.RockId], result.Breadcrumb.Segments.Select(segment => segment.EntityId).ToArray());
        Assert.NotNull(result.CurrentPlace);
        var place = result.CurrentPlace;
        Assert.Equal(TestWorld.SlimeId, place.EntityId);
        Assert.Equal(TestWorld.SlimeInventoryPlaneId, place.ContainingPlaneId);
        Assert.Equal(PointOfViewPlaceSelectionRule.NearestContainingInventoryOwner, place.SelectionRule);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void PointOfViewReportsObserverBulkPlaceApertureAndRatio()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        world.Entities[TestWorld.RockId] = world.Entities[TestWorld.RockId] with { Bulk = 5 };
        world.Entities[TestWorld.SlimeId] = world.Entities[TestWorld.SlimeId] with { Aperture = 20 };
        var service = new PointOfViewService();

        var result = service.Describe(world, TestWorld.RockId);

        Assert.NotNull(result.CurrentPlace);
        var place = result.CurrentPlace;
        Assert.Equal(5, place.ObserverBulk);
        Assert.Equal(20, place.PlaceAperture);
        Assert.Equal(0.25m, place.BulkToApertureRatio);
    }

    [Fact]
    public void PointOfViewReportsMissingObserverDiagnostic()
    {
        var world = TestWorld.CreateWorld();
        var missingEntityId = new EntityId("missingObserver");
        var service = new PointOfViewService();

        var result = service.Describe(world, missingEntityId);

        Assert.Equal(missingEntityId, result.ObserverEntityId);
        Assert.Equal(EntityContainmentPathStatus.RequestedEntityNotFound, result.Breadcrumb.Status);
        Assert.Null(result.CurrentPlace);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == PointOfViewDiagnosticCode.ObserverNotFound);
    }

    [Fact]
    public void PointOfViewReportsNoCurrentPlaceWhenObserverHasNoContainingInventoryOwner()
    {
        var world = TestWorld.CreateWorld();
        var service = new PointOfViewService();

        var result = service.Describe(world, TestWorld.PlayerId);

        Assert.Equal(EntityContainmentPathStatus.Complete, result.Breadcrumb.Status);
        Assert.Null(result.CurrentPlace);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == PointOfViewDiagnosticCode.CurrentPlaceNotFound);
    }

    [Fact]
    public void PointOfViewPreservesBreadcrumbTruncationFromQueryOptions()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0)));
        movement.TryPlace(world, TestWorld.SlimeId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(1, 0)));
        var service = new PointOfViewService();

        var result = service.Describe(world, TestWorld.RockId, new PointOfViewQueryOptions(MaxBreadcrumbDepth: 2));

        Assert.Equal(EntityContainmentPathStatus.Truncated, result.Breadcrumb.Status);
        Assert.Equal([TestWorld.SlimeId, TestWorld.RockId], result.Breadcrumb.Segments.Select(segment => segment.EntityId).ToArray());
        Assert.NotNull(result.CurrentPlace);
        Assert.Equal(TestWorld.SlimeId, result.CurrentPlace.EntityId);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == PointOfViewDiagnosticCode.BreadcrumbIncomplete);
    }

    [Fact]
    public void PointOfViewAdjectivesComeFromObserverActionStepSuccessCapabilitiesInCurrentPlace()
    {
        var world = CreatePointOfViewRoomWorld();
        var observer = new EntityId("observer");
        var portableGem = new EntityId("portableGem");
        var heavyRock = new EntityId("heavyRock");
        var enterableChest = new EntityId("enterableChest");
        var plan = new ActionPlanDescriptor(
            new ActionPlanId("observerPlan"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.EnterTarget)
            ]));
        var service = new PointOfViewService();

        var result = service.Describe(world, observer, plan);

        Assert.Contains(result.TargetAdjectives, adjective => adjective.EntityId == portableGem && adjective.Adjective == "portable" && adjective.Capability == ActionPlanBehaviorStepKind.PickupTarget);
        Assert.DoesNotContain(result.TargetAdjectives, adjective => adjective.EntityId == heavyRock && adjective.Adjective == "portable");
        Assert.Contains(result.TargetAdjectives, adjective => adjective.EntityId == enterableChest && adjective.Adjective == "enterable" && adjective.Capability == ActionPlanBehaviorStepKind.EnterTarget);
        Assert.DoesNotContain(result.TargetAdjectives, adjective => adjective.EntityId == portableGem && adjective.Adjective == "enterable");
    }

    [Fact]
    public void PointOfViewReciprocalAdjectivesComeFromOtherEntityActionStepSuccessCapabilitiesAgainstObserver()
    {
        var world = CreatePointOfViewRoomWorld();
        var observer = new EntityId("observer");
        var enterableChest = new EntityId("enterableChest");
        var heavyRock = new EntityId("heavyRock");
        var observerPlan = new ActionPlanDescriptor(
            new ActionPlanId("observerPlan"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget)
            ]));
        var chestPlan = new ActionPlanDescriptor(
            new ActionPlanId("chestPlan"),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget)
            ]));
        var service = new PointOfViewService();

        var result = service.Describe(
            world,
            observer,
            observerPlan,
            entityId => entityId == enterableChest ? chestPlan : null);

        Assert.Contains(result.ReciprocalAdjectives, adjective => adjective.EntityId == enterableChest && adjective.Adjective == "portable" && adjective.Capability == ActionPlanBehaviorStepKind.PickupTarget);
        Assert.DoesNotContain(result.ReciprocalAdjectives, adjective => adjective.EntityId == heavyRock);
    }

    private static WorldState CreatePointOfViewRoomWorld()
    {
        var world = new WorldState();
        var worldPlaneId = new PlaneId("world");
        var roomPlaneId = new PlaneId("room");
        var chestPlaneId = new PlaneId("chest");
        AddPlane(world, worldPlaneId, 1, 1);
        AddPlane(world, roomPlaneId, 4, 2);
        AddPlane(world, chestPlaneId, 1, 1);
        AddEntity(world, new EntityId("room"), "Room", new PlaneCoord(worldPlaneId, new GridCoord(0, 0)), 4, 2, bulk: 100, aperture: 100);
        AddEntity(world, new EntityId("observer"), "Observer", new PlaneCoord(roomPlaneId, new GridCoord(0, 0)), 1, 1, bulk: 1, aperture: 3);
        AddEntity(world, new EntityId("portableGem"), "Portable Gem", new PlaneCoord(roomPlaneId, new GridCoord(1, 0)), 0, 0, bulk: 1, aperture: 0);
        AddEntity(world, new EntityId("heavyRock"), "Heavy Rock", new PlaneCoord(roomPlaneId, new GridCoord(2, 0)), 0, 0, bulk: 9, aperture: 0);
        AddEntity(world, new EntityId("enterableChest"), "Enterable Chest", new PlaneCoord(roomPlaneId, new GridCoord(0, 1)), 1, 1, bulk: 10, aperture: 5);
        world.RegisterInventoryPlane(new EntityId("room"), roomPlaneId);
        world.RegisterInventoryPlane(new EntityId("observer"), new PlaneId("observerInventory"));
        AddPlane(world, new PlaneId("observerInventory"), 1, 1);
        world.RegisterInventoryPlane(new EntityId("enterableChest"), chestPlaneId);
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

    private static void AddEntity(WorldState world, EntityId entityId, string name, PlaneCoord location, int inventoryWidth, int inventoryHeight, int bulk, int aperture)
    {
        var nodeId = world.GetNodeId(location);
        world.Entities.Add(entityId, new Entity(entityId, name, nodeId, inventoryWidth, inventoryHeight, bulk, aperture));
        world.Occupancy.Add(nodeId, entityId);
    }
}
