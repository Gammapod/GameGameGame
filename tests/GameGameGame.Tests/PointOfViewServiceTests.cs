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
}
