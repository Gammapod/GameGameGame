namespace GameGameGame.Core;

public enum PointOfViewPlaceSelectionRule
{
    NearestContainingInventoryOwner
}

public enum PointOfViewDiagnosticCode
{
    ObserverNotFound,
    BreadcrumbIncomplete,
    CurrentPlaceNotFound,
    ObserverBulkUnavailable,
    PlaceApertureUnavailable
}

public sealed record PointOfViewQueryOptions(int? MaxBreadcrumbDepth = null);

public sealed record PointOfViewDiagnostic(
    PointOfViewDiagnosticCode Code,
    string Message);

public sealed record PointOfViewCurrentPlace(
    EntityId EntityId,
    PlaneId ContainingPlaneId,
    PointOfViewPlaceSelectionRule SelectionRule,
    int ObserverBulk,
    int PlaceAperture,
    decimal? BulkToApertureRatio);

public sealed record PointOfViewResult(
    EntityId ObserverEntityId,
    EntityContainmentPath Breadcrumb,
    PointOfViewCurrentPlace? CurrentPlace,
    IReadOnlyList<PointOfViewDiagnostic> Diagnostics);

public sealed class PointOfViewService(EntityContainmentPathService? containmentPaths = null)
{
    private readonly EntityContainmentPathService _containmentPaths = containmentPaths ?? new EntityContainmentPathService();

    public PointOfViewResult Describe(WorldState world, EntityId observerEntityId, PointOfViewQueryOptions? options = null)
    {
        var breadcrumb = _containmentPaths.GetUpwardPath(world, observerEntityId, options?.MaxBreadcrumbDepth);
        var diagnostics = BuildBreadcrumbDiagnostics(observerEntityId, breadcrumb);

        var currentPlace = TryBuildCurrentPlace(world, observerEntityId, breadcrumb, diagnostics);

        return new PointOfViewResult(
            observerEntityId,
            breadcrumb,
            currentPlace,
            diagnostics);
    }

    private static List<PointOfViewDiagnostic> BuildBreadcrumbDiagnostics(EntityId observerEntityId, EntityContainmentPath breadcrumb)
    {
        var diagnostics = new List<PointOfViewDiagnostic>();
        if (breadcrumb.Status == EntityContainmentPathStatus.RequestedEntityNotFound)
        {
            diagnostics.Add(new PointOfViewDiagnostic(
                PointOfViewDiagnosticCode.ObserverNotFound,
                $"Observer entity {observerEntityId} was not found."));
            return diagnostics;
        }

        if (breadcrumb.Status != EntityContainmentPathStatus.Complete)
        {
            var detail = breadcrumb.Diagnostics.Count == 0
                ? breadcrumb.Status.ToString()
                : string.Join(" ", breadcrumb.Diagnostics);
            diagnostics.Add(new PointOfViewDiagnostic(
                PointOfViewDiagnosticCode.BreadcrumbIncomplete,
                $"Point-of-view breadcrumb for {observerEntityId} is {breadcrumb.Status}: {detail}"));
        }

        return diagnostics;
    }

    private static PointOfViewCurrentPlace? TryBuildCurrentPlace(
        WorldState world,
        EntityId observerEntityId,
        EntityContainmentPath breadcrumb,
        List<PointOfViewDiagnostic> diagnostics)
    {
        if (!world.Entities.TryGetValue(observerEntityId, out var observer))
        {
            return null;
        }

        var observerSegment = breadcrumb.Segments.LastOrDefault(segment => segment.EntityId == observerEntityId);
        if (observerSegment is null || observerSegment.ContainerEntityId is not { } currentPlaceEntityId || observerSegment.ContainingPlaneId is not { } containingPlaneId)
        {
            diagnostics.Add(new PointOfViewDiagnostic(
                PointOfViewDiagnosticCode.CurrentPlaceNotFound,
                $"Observer entity {observerEntityId} is not contained by an inventory owner, so no current place was selected."));
            return null;
        }

        if (!world.Entities.TryGetValue(currentPlaceEntityId, out var place))
        {
            diagnostics.Add(new PointOfViewDiagnostic(
                PointOfViewDiagnosticCode.CurrentPlaceNotFound,
                $"Current place entity {currentPlaceEntityId} was not found for observer {observerEntityId}."));
            return null;
        }

        decimal? ratio = null;
        if (observer.Bulk < 0)
        {
            diagnostics.Add(new PointOfViewDiagnostic(
                PointOfViewDiagnosticCode.ObserverBulkUnavailable,
                $"Observer entity {observerEntityId} has unsupported bulk {observer.Bulk}."));
        }
        else if (place.Aperture <= 0)
        {
            diagnostics.Add(new PointOfViewDiagnostic(
                PointOfViewDiagnosticCode.PlaceApertureUnavailable,
                $"Current place entity {currentPlaceEntityId} has unsupported aperture {place.Aperture}."));
        }
        else
        {
            ratio = (decimal)observer.Bulk / place.Aperture;
        }

        return new PointOfViewCurrentPlace(
            currentPlaceEntityId,
            containingPlaneId,
            PointOfViewPlaceSelectionRule.NearestContainingInventoryOwner,
            observer.Bulk,
            place.Aperture,
            ratio);
    }
}
