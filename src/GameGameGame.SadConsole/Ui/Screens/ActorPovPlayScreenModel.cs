using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Presentation;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed record ActorPovPlayScreenModel(
    ActorPovPlayLayout Layout,
    ActorPovPlayProjection Projection,
    ActorPovPlayPresentationState PresentationState,
    IReadOnlyList<ActorPovPlayViewportState> Viewports,
    IReadOnlyList<ActorPovPlayScreenDiagnostic> Diagnostics,
    IReadOnlyDictionary<EntityId, Direction> FacingByEntityId,
    IReadOnlyDictionary<EntityId, IReadOnlyList<EntityId>> TargetsByEntityId,
    InventorySpaceRootCellMetrics RootCellMetrics,
    InventorySpaceViewModel? CurrentLayerView,
    IReadOnlySet<PlaneId> CurrentLayerPlaneIds)
{
    public EntityPanelProjection ControlledActor => Projection.ControlledActor;
    public EntityPanelProjection? CurrentPlace => Projection.CurrentPlace;
    public EntityPanelProjection? ActorInventory => Projection.ActorInventory;
    public ActorPovInspectionCandidateProjection? SelectedWorldInspectionCandidate =>
        Projection.WorldInspectionCandidates.FirstOrDefault(candidate => candidate.Entity.EntityId == PresentationState.SelectedWorldInspectionEntityId);
    public ActorPovInspectionCandidateProjection? SelectedCarriedInspectionCandidate =>
        Projection.CarriedInspectionCandidates.FirstOrDefault(candidate => candidate.Entity.EntityId == PresentationState.SelectedCarriedInspectionEntityId);
}

internal sealed record ActorPovPlayPresentationState(
    EntityId? SelectedWorldInspectionEntityId = null,
    EntityId? SelectedCarriedInspectionEntityId = null,
    string? FocusedRegionId = null);

internal sealed record ActorPovPlayViewportState(
    string RegionId,
    EntityId EntityId,
    InventorySpaceViewport Viewport);

internal sealed record ActorPovPlayScreenDiagnostic(string Source, string Code, string Message);

internal static class ActorPovPlayScreenModelBuilder
{
    public static ActorPovPlayScreenModel Build(
        PlayableScenarioSession session,
        SadConsoleRect drawableBounds,
        ActorPovPlayPresentationState? presentationState = null,
        EntityId? controlledActorId = null,
        ActionLogProjection? actionLog = null,
        InventorySpaceRootCellMetrics? rootCellMetrics = null,
        int? topologyPovDepth = null) =>
        Build(
            session.World,
            controlledActorId ?? session.PlayerEntityId,
            session.ActionPlans,
            drawableBounds,
            ResolveInspectionAppearance(session),
            ResolveActionPlanDescriptor(session),
            presentationState,
            actionLog,
            rootCellMetrics,
            topologyPovDepth);

    public static ActorPovPlayScreenModel Build(
        WorldState world,
        EntityId controlledActorId,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        SadConsoleRect drawableBounds,
        Func<EntityId, EntityInspectionAppearance>? getAppearance = null,
        Func<EntityId, ActionPlanDescriptor?>? getActionPlanDescriptor = null,
        ActorPovPlayPresentationState? presentationState = null,
        ActionLogProjection? actionLog = null,
        InventorySpaceRootCellMetrics? rootCellMetrics = null,
        int? topologyPovDepth = null)
    {
        var layout = ActorPovPlayLayoutResolver.Resolve(drawableBounds);
        var projection = new ActorPovPlayProjectionService(getAppearance, getActionPlanDescriptor)
            .Project(world, controlledActorId, actionPlans, actionLog);
        var normalizedState = NormalizePresentationState(projection, presentationState);
        var resolvedRootCellMetrics = rootCellMetrics ?? InventorySpaceRootCellMetrics.DefaultPlay;
        return new ActorPovPlayScreenModel(
            layout,
            projection,
            normalizedState,
            BuildViewports(projection),
            BuildDiagnostics(layout, projection),
            BuildFacingFacts(world),
            BuildTargetFacts(world),
            resolvedRootCellMetrics,
            BuildCurrentLayerView(world, controlledActorId, getAppearance, topologyPovDepth),
            BuildCurrentLayerPlaneIds(world, controlledActorId, projection));
    }

    private static InventorySpaceViewModel? BuildCurrentLayerView(
        WorldState world,
        EntityId controlledActorId,
        Func<EntityId, EntityInspectionAppearance>? getAppearance,
        int? topologyPovDepth)
    {
        if (topologyPovDepth is { } depth && world.Entities.ContainsKey(controlledActorId))
        {
            return InventorySpaceViewModel.FromActorTopologyFlood(
                "0.actor-pov.current-layer.topology-flood",
                world,
                controlledActorId,
                depth,
                cellMetrics: InventorySpaceCellMetrics.Default,
                facingByEntityId: BuildFacingFacts(world),
                getAppearance: getAppearance);
        }

        if (!world.Entities.ContainsKey(controlledActorId) ||
            !MergedInventoryLayerResolver.TryResolveCell(world, world.GetEntityLocation(controlledActorId), out var cell))
        {
            return null;
        }

        return InventorySpaceViewModel.FromMergedLayer(
            "0.actor-pov.current-layer.inventory-space",
            world,
            cell.Layer,
            controlledActorId,
            cellMetrics: InventorySpaceCellMetrics.Default,
            facingByEntityId: BuildFacingFacts(world),
            getAppearance: getAppearance);
    }

    private static IReadOnlySet<PlaneId> BuildCurrentLayerPlaneIds(
        WorldState world,
        EntityId controlledActorId,
        ActorPovPlayProjection projection)
    {
        if (world.Entities.ContainsKey(controlledActorId) &&
            MergedInventoryLayerResolver.TryResolveCell(world, world.GetEntityLocation(controlledActorId), out var cell))
        {
            return cell.Layer.Spaces
                .Select(space => world.GetRegisteredInventoryPlaneId(space.OwnerId))
                .OfType<PlaneId>()
                .ToHashSet();
        }

        return projection.CurrentPlace?.InventoryGrid is { } grid
            ? new HashSet<PlaneId> { grid.PlaneId }
            : new HashSet<PlaneId>();
    }

    private static IReadOnlyDictionary<EntityId, Direction> BuildFacingFacts(WorldState world) =>
        world.ActionStates
            .Where(pair => pair.Value.Facing is not null)
            .ToDictionary(pair => pair.Key, pair => pair.Value.Facing!.Value);

    private static IReadOnlyDictionary<EntityId, IReadOnlyList<EntityId>> BuildTargetFacts(WorldState world) =>
        world.ActionStates
            .Select(pair => (EntityId: pair.Key, Targets: BuildDistinctTargets(pair.Value)))
            .Where(pair => pair.Targets.Count > 0)
            .ToDictionary(pair => pair.EntityId, pair => pair.Targets);

    private static IReadOnlyList<EntityId> BuildDistinctTargets(EntityActionState state)
    {
        var targets = new List<EntityId>();
        if (state.Target is { } primaryTarget)
        {
            targets.Add(primaryTarget);
        }

        targets.AddRange(state.Targets.Values);
        targets.AddRange(state.LabeledTargets.Values);
        return targets.Distinct().ToList();
    }

    private static ActorPovPlayPresentationState NormalizePresentationState(
        ActorPovPlayProjection projection,
        ActorPovPlayPresentationState? requested)
    {
        var selectedWorld = SelectValidOrFirst(
            requested?.SelectedWorldInspectionEntityId,
            projection.WorldInspectionCandidates);
        var selectedCarried = SelectValidOrFirst(
            requested?.SelectedCarriedInspectionEntityId,
            projection.CarriedInspectionCandidates);
        return new ActorPovPlayPresentationState(
            selectedWorld,
            selectedCarried,
            requested?.FocusedRegionId ?? ActorPovPlayRegionIds.CurrentPlace);
    }

    private static EntityId? SelectValidOrFirst(
        EntityId? requested,
        IReadOnlyList<ActorPovInspectionCandidateProjection> candidates)
    {
        if (requested is not null && candidates.Any(candidate => candidate.Entity.EntityId == requested))
        {
            return requested;
        }

        return candidates.FirstOrDefault()?.Entity.EntityId;
    }

    private static IReadOnlyList<ActorPovPlayViewportState> BuildViewports(ActorPovPlayProjection projection)
    {
        var states = new List<ActorPovPlayViewportState>();
        AddViewport(states, ActorPovPlayRegionIds.CurrentPlace, projection.CurrentPlace);
        AddViewport(states, ActorPovPlayRegionIds.ActorInventory, projection.ActorInventory);
        AddViewport(states, ActorPovPlayRegionIds.WorldInspection, projection.WorldInspectionCandidates.FirstOrDefault()?.Entity);
        AddViewport(states, ActorPovPlayRegionIds.ActorInventoryInspection, projection.CarriedInspectionCandidates.FirstOrDefault()?.Entity);
        return states;
    }

    private static void AddViewport(List<ActorPovPlayViewportState> states, string regionId, EntityPanelProjection? projection)
    {
        if (projection?.InventoryGrid is not { } grid)
        {
            return;
        }

        states.Add(new ActorPovPlayViewportState(
            regionId,
            projection.EntityId,
            InventorySpaceViewport.Full(grid.Width, grid.Height)));
    }

    private static IReadOnlyList<ActorPovPlayScreenDiagnostic> BuildDiagnostics(
        ActorPovPlayLayout layout,
        ActorPovPlayProjection projection)
    {
        var diagnostics = new List<ActorPovPlayScreenDiagnostic>();
        diagnostics.AddRange(layout.Diagnostics.Select(diagnostic => new ActorPovPlayScreenDiagnostic("layout", diagnostic.Code, diagnostic.Message)));
        diagnostics.AddRange(projection.Diagnostics.Select(diagnostic => new ActorPovPlayScreenDiagnostic("projection", diagnostic.Code.ToString(), diagnostic.Message)));
        return diagnostics;
    }

    private static Func<EntityId, EntityInspectionAppearance> ResolveInspectionAppearance(PlayableScenarioSession session) => entityId =>
    {
        if (session.Registry.TryGetTemplateIdForEntity(session.World, entityId, out var templateId)
            && session.Registry.Presentations.TryGetValue(templateId, out var presentation))
        {
            return SadConsolePresentationResolver.Default.ResolveAppearance(presentation.ToInspectionAppearance());
        }

        return new EntityInspectionAppearance('?', PresentationColor.Gray);
    };

    private static Func<EntityId, ActionPlanDescriptor?> ResolveActionPlanDescriptor(PlayableScenarioSession session) => entityId =>
    {
        if (!session.Registry.TryGetTemplateIdForEntity(session.World, entityId, out var templateId))
        {
            return null;
        }

        var template = session.Registry.GetEntityTemplate(templateId);
        var defaultPlanId = session.World.GetDefaultActionPlanId(entityId) is { } runtimePlanId
            ? new ActionPlanTemplateId(runtimePlanId.Value)
            : template.DefaultActionPlanId;
        return defaultPlanId is { } planId
            && session.Registry.ActionPlanDescriptors.TryGetValue(planId, out var descriptor)
                ? descriptor
                : null;
    };
}
