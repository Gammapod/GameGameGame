using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed record ActorPovPlayProjection(
    EntityPanelProjection ControlledActor,
    EntityPanelProjection? CurrentPlace,
    IReadOnlyList<ActorPovChainNodeProjection> ParentChain,
    IReadOnlyList<ActorPovInspectionCandidateProjection> WorldInspectionCandidates,
    EntityPanelProjection? ActorInventory,
    IReadOnlyList<ActorPovInspectionCandidateProjection> CarriedInspectionCandidates,
    IReadOnlyList<PointOfViewDiagnostic> Diagnostics);

public sealed record ActorPovChainNodeProjection(
    EntityPanelProjection Entity,
    EntityId? ChildEntityId,
    GridCoord? ChildCoordinateInEntityInventory);

public enum ActorPovInspectionCandidateKind
{
    WorldPeer,
    ActorCarriedItem
}

public sealed record ActorPovInspectionCandidateProjection(
    ActorPovInspectionCandidateKind Kind,
    EntityPanelProjection Entity,
    GridCoord CoordinateInSourceInventory,
    IReadOnlyList<EntityPointOfViewTargetAdjectiveProjection> TargetAdjectives,
    IReadOnlyList<EntityPointOfViewTargetAdjectiveProjection> ReciprocalAdjectives);

public sealed class ActorPovPlayProjectionService(
    Func<EntityId, EntityInspectionAppearance>? getAppearance = null,
    Func<EntityId, ActionPlanDescriptor?>? getActionPlanDescriptor = null)
{
    private readonly EntityPanelProjectionService _panels = new(getAppearance, getActionPlanDescriptor);

    public ActorPovPlayProjection Project(
        WorldState world,
        EntityId controlledActorId,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        ActionLogProjection? actionLog = null)
    {
        var controlledActor = _panels.Project(world, controlledActorId, actionPlans, controlledActorId, actionLog);
        var pointOfView = controlledActor.PointOfView;
        var currentPlace = pointOfView?.CurrentPlace is { } currentPlaceFact
            ? _panels.Project(world, currentPlaceFact.EntityId, actionPlans, controlledActorId, actionLog)
            : null;

        return new ActorPovPlayProjection(
            controlledActor,
            currentPlace,
            BuildParentChain(world, controlledActorId, actionPlans, pointOfView, actionLog),
            BuildWorldInspectionCandidates(world, controlledActorId, actionPlans, currentPlace, pointOfView, actionLog),
            controlledActor.InventoryGrid is null ? null : controlledActor,
            BuildCarriedInspectionCandidates(world, actionPlans, controlledActor, pointOfView, actionLog),
            pointOfView?.Diagnostics ?? []);
    }

    private IReadOnlyList<ActorPovChainNodeProjection> BuildParentChain(
        WorldState world,
        EntityId controlledActorId,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        EntityPointOfViewProjection? pointOfView,
        ActionLogProjection? actionLog)
    {
        if (pointOfView?.CurrentPlace is not { } currentPlace)
        {
            return [];
        }

        var segments = pointOfView.Breadcrumb.Segments;
        var currentPlaceIndex = segments.ToList().FindIndex(segment => segment.EntityId == currentPlace.EntityId);
        if (currentPlaceIndex <= 0)
        {
            return [];
        }

        var nodes = new List<ActorPovChainNodeProjection>();
        for (var index = 0; index < currentPlaceIndex; index++)
        {
            var segment = segments[index];
            var childSegment = segments[index + 1];
            nodes.Add(new ActorPovChainNodeProjection(
                _panels.Project(world, segment.EntityId, actionPlans, controlledActorId, actionLog),
                childSegment.EntityId,
                childSegment.ContainerEntityId == segment.EntityId ? childSegment.CoordinateInContainingPlane : null));
        }

        return nodes;
    }

    private IReadOnlyList<ActorPovInspectionCandidateProjection> BuildWorldInspectionCandidates(
        WorldState world,
        EntityId controlledActorId,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        EntityPanelProjection? currentPlace,
        EntityPointOfViewProjection? pointOfView,
        ActionLogProjection? actionLog)
    {
        if (currentPlace is null)
        {
            return [];
        }

        return currentPlace.Contents
            .Where(row => row.EntityId != controlledActorId)
            .Select(row => BuildInspectionCandidate(
                world,
                controlledActorId,
                actionPlans,
                row.EntityId,
                row.Location.Coord,
                ActorPovInspectionCandidateKind.WorldPeer,
                pointOfView,
                actionLog))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .ToList();
    }

    private IReadOnlyList<ActorPovInspectionCandidateProjection> BuildCarriedInspectionCandidates(
        WorldState world,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        EntityPanelProjection controlledActor,
        EntityPointOfViewProjection? pointOfView,
        ActionLogProjection? actionLog) =>
        controlledActor.Contents
            .Select(row => BuildInspectionCandidate(
                world,
                controlledActor.EntityId,
                actionPlans,
                row.EntityId,
                row.Location.Coord,
                ActorPovInspectionCandidateKind.ActorCarriedItem,
                pointOfView,
                actionLog))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .ToList();

    private ActorPovInspectionCandidateProjection? BuildInspectionCandidate(
        WorldState world,
        EntityId controlledActorId,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        EntityId candidateEntityId,
        GridCoord coordinateInSourceInventory,
        ActorPovInspectionCandidateKind kind,
        EntityPointOfViewProjection? pointOfView,
        ActionLogProjection? actionLog)
    {
        var entity = _panels.Project(world, candidateEntityId, actionPlans, controlledActorId, actionLog);
        if (entity.InventoryGrid is null)
        {
            return null;
        }

        return new ActorPovInspectionCandidateProjection(
            kind,
            entity,
            coordinateInSourceInventory,
            pointOfView?.TargetAdjectives.Where(adjective => adjective.EntityId == candidateEntityId).ToList() ?? [],
            pointOfView?.ReciprocalAdjectives.Where(adjective => adjective.EntityId == candidateEntityId).ToList() ?? []);
    }
}
