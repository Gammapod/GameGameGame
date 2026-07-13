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

public sealed record PointOfViewTargetAdjective(
    EntityId EntityId,
    ActionPlanBehaviorStepKind Capability,
    string Adjective);

public sealed record PointOfViewResult(
    EntityId ObserverEntityId,
    EntityContainmentPath Breadcrumb,
    PointOfViewCurrentPlace? CurrentPlace,
    IReadOnlyList<PointOfViewTargetAdjective> TargetAdjectives,
    IReadOnlyList<PointOfViewTargetAdjective> ReciprocalAdjectives,
    IReadOnlyList<PointOfViewDiagnostic> Diagnostics);

public sealed class PointOfViewService(EntityContainmentPathService? containmentPaths = null)
{
    private readonly EntityContainmentPathService _containmentPaths = containmentPaths ?? new EntityContainmentPathService();

    public PointOfViewResult Describe(
        WorldState world,
        EntityId observerEntityId,
        PointOfViewQueryOptions? options = null,
        ActionPlanDescriptor? observerActionPlan = null,
        Func<EntityId, ActionPlanDescriptor?>? actionPlanResolver = null)
    {
        var breadcrumb = _containmentPaths.GetUpwardPath(world, observerEntityId, options?.MaxBreadcrumbDepth);
        var diagnostics = BuildBreadcrumbDiagnostics(observerEntityId, breadcrumb);

        var currentPlace = TryBuildCurrentPlace(world, observerEntityId, breadcrumb, diagnostics);
        var targetAdjectives = BuildTargetAdjectives(world, observerEntityId, currentPlace, observerActionPlan);
        var reciprocalAdjectives = BuildReciprocalAdjectives(world, observerEntityId, currentPlace, actionPlanResolver);

        return new PointOfViewResult(
            observerEntityId,
            breadcrumb,
            currentPlace,
            targetAdjectives,
            reciprocalAdjectives,
            diagnostics);
    }

    public PointOfViewResult Describe(WorldState world, EntityId observerEntityId, ActionPlanDescriptor? observerActionPlan) =>
        Describe(world, observerEntityId, null, observerActionPlan);

    public PointOfViewResult Describe(
        WorldState world,
        EntityId observerEntityId,
        ActionPlanDescriptor? observerActionPlan,
        Func<EntityId, ActionPlanDescriptor?>? actionPlanResolver) =>
        Describe(world, observerEntityId, null, observerActionPlan, actionPlanResolver);

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

    private static IReadOnlyList<PointOfViewTargetAdjective> BuildTargetAdjectives(
        WorldState world,
        EntityId observerEntityId,
        PointOfViewCurrentPlace? currentPlace,
        ActionPlanDescriptor? observerActionPlan)
    {
        if (currentPlace is null || observerActionPlan?.Behavior is not { } behavior)
        {
            return [];
        }

        if (world.GetRegisteredInventoryPlaneId(currentPlace.EntityId) != currentPlace.ContainingPlaneId)
        {
            return [];
        }

        var capabilities = behavior.Steps
            .Select(step => ActionStepCatalog.Get(step.Kind).TargetCapability)
            .Where(capability => capability is not null)
            .Select(capability => capability!.Value)
            .Distinct()
            .ToList();
        if (capabilities.Count == 0)
        {
            return [];
        }

        var affordances = new EntityInteractionAffordanceService(new MovementService());
        return CurrentPlaceEntityIds(world, observerEntityId, currentPlace)
            .SelectMany(entityId => capabilities
                .Where(capability => affordances.QueryTargetCapability(world, observerEntityId, entityId, capability).CanTarget)
                .Select(capability => new PointOfViewTargetAdjective(entityId, capability, ToAdjective(capability))))
            .ToList();
    }

    private static IReadOnlyList<PointOfViewTargetAdjective> BuildReciprocalAdjectives(
        WorldState world,
        EntityId observerEntityId,
        PointOfViewCurrentPlace? currentPlace,
        Func<EntityId, ActionPlanDescriptor?>? actionPlanResolver)
    {
        if (currentPlace is null || actionPlanResolver is null)
        {
            return [];
        }

        var affordances = new EntityInteractionAffordanceService(new MovementService());
        return CurrentPlaceEntityIds(world, observerEntityId, currentPlace)
            .SelectMany(entityId => CapabilitiesFromPlan(actionPlanResolver(entityId))
                .Where(capability => affordances.QueryTargetCapability(world, entityId, observerEntityId, capability).CanTarget)
                .Select(capability => new PointOfViewTargetAdjective(entityId, capability, ToAdjective(capability))))
            .ToList();
    }

    private static IReadOnlyList<ActionPlanBehaviorStepKind> CapabilitiesFromPlan(ActionPlanDescriptor? actionPlan) =>
        actionPlan?.Behavior is { } behavior
            ? behavior.Steps
                .Select(step => ActionStepCatalog.Get(step.Kind).TargetCapability)
                .Where(capability => capability is not null)
                .Select(capability => capability!.Value)
                .Distinct()
                .ToList()
            : [];

    private static IEnumerable<EntityId> CurrentPlaceEntityIds(WorldState world, EntityId observerEntityId, PointOfViewCurrentPlace currentPlace) =>
        world.Occupancy
            .Where(entry => world.Nodes.TryGetValue(entry.Key, out var node) && node.PlaneId == currentPlace.ContainingPlaneId)
            .Select(entry => entry.Value)
            .Where(entityId => entityId != observerEntityId)
            .Distinct()
            .OrderBy(entityId => world.GetEntityLocation(entityId).Coord.Y)
            .ThenBy(entityId => world.GetEntityLocation(entityId).Coord.X)
            .ThenBy(entityId => entityId.Value, StringComparer.Ordinal);

    private static string ToAdjective(ActionPlanBehaviorStepKind capability) => capability switch
    {
        ActionPlanBehaviorStepKind.PickupTarget => "portable",
        ActionPlanBehaviorStepKind.EnterTarget => "enterable",
        ActionPlanBehaviorStepKind.PushFacing => "pushable",
        ActionPlanBehaviorStepKind.DestroyTarget => "breakable",
        ActionPlanBehaviorStepKind.GiveTarget => "receivable",
        ActionPlanBehaviorStepKind.TakeTarget => "takeable",
        _ => capability.ToString()
    };
}
