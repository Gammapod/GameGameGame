using GameGameGame.Core;

namespace GameGameGame.Content;

public sealed record EntityPanelProjection(
    EntityId EntityId,
    string Name,
    char Glyph,
    PresentationColor Color,
    PlaneCoord Location,
    EntityContainmentPath Breadcrumb,
    IReadOnlyList<EntityInspectionProperty> Properties,
    EntityPanelActionStateProjection ActionState,
    string? ActionPlanSummary,
    InventoryInspectionGrid? InventoryGrid,
    IReadOnlyList<EntityPanelContentRow> Contents,
    IReadOnlyList<ActionOutcome> LocalLog,
    EntityPointOfViewProjection? PointOfView = null);

public sealed record EntityPointOfViewProjection(
    EntityId ObserverEntityId,
    EntityContainmentPath Breadcrumb,
    EntityPointOfViewCurrentPlaceProjection? CurrentPlace,
    IReadOnlyList<PointOfViewDiagnostic> Diagnostics);

public sealed record EntityPointOfViewCurrentPlaceProjection(
    EntityId EntityId,
    string Name,
    char Glyph,
    PresentationColor Color,
    PlaneId ContainingPlaneId,
    PointOfViewPlaceSelectionRule SelectionRule,
    int ObserverBulk,
    int PlaceAperture,
    decimal? BulkToApertureRatio);

public sealed record EntityPanelActionStateProjection(
    Direction? Facing,
    EntityId? Target,
    IReadOnlyDictionary<int, EntityId> Targets);

public sealed record EntityPanelContentRow(
    int Order,
    EntityId EntityId,
    string EntityName,
    char Glyph,
    PlaneCoord Location,
    LocalTurnParticipation Participation,
    string PreviousAction);

public sealed class EntityPanelProjectionService(Func<EntityId, EntityInspectionAppearance>? getAppearance = null)
{
    private readonly EntityInspectionService _inspection = new(getAppearance);
    private readonly EntityContainmentPathService _paths = new();
    private readonly PointOfViewService _pointOfView = new();

    public EntityPanelProjection Project(
        WorldState world,
        EntityId entityId,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        EntityId? playerId = null,
        ActionLogProjection? actionLog = null)
    {
        var panel = _inspection.Inspect(world, entityId);
        var location = world.GetEntityLocation(entityId);
        var breadcrumb = _paths.GetUpwardPath(world, entityId);
        var localLog = BuildLocalLog(entityId, panel.InventoryGrid?.PlaneId, actionLog);
        var contents = BuildContents(world, panel, actionPlans, playerId, localLog);

        return new EntityPanelProjection(
            panel.EntityId,
            panel.Name,
            panel.Glyph,
            panel.Color,
            location,
            breadcrumb,
            panel.Properties,
            BuildActionState(world, entityId),
            actionPlans.ContainsKey(entityId) ? actionPlans[entityId].GetType().Name : null,
            panel.InventoryGrid,
            contents,
            localLog,
            BuildPointOfView(world, entityId));
    }

    private EntityPointOfViewProjection BuildPointOfView(WorldState world, EntityId entityId)
    {
        var pointOfView = _pointOfView.Describe(world, entityId);
        return new EntityPointOfViewProjection(
            pointOfView.ObserverEntityId,
            pointOfView.Breadcrumb,
            BuildCurrentPlaceProjection(world, pointOfView.CurrentPlace),
            pointOfView.Diagnostics);
    }

    private EntityPointOfViewCurrentPlaceProjection? BuildCurrentPlaceProjection(WorldState world, PointOfViewCurrentPlace? currentPlace)
    {
        if (currentPlace is null)
        {
            return null;
        }

        var appearance = _inspection.Inspect(world, currentPlace.EntityId);
        return new EntityPointOfViewCurrentPlaceProjection(
            currentPlace.EntityId,
            appearance.Name,
            appearance.Glyph,
            appearance.Color,
            currentPlace.ContainingPlaneId,
            currentPlace.SelectionRule,
            currentPlace.ObserverBulk,
            currentPlace.PlaceAperture,
            currentPlace.BulkToApertureRatio);
    }

    private static EntityPanelActionStateProjection BuildActionState(WorldState world, EntityId entityId)
    {
        if (!world.ActionStates.TryGetValue(entityId, out var state))
        {
            return new EntityPanelActionStateProjection(null, null, new Dictionary<int, EntityId>());
        }

        return new EntityPanelActionStateProjection(state.Facing, state.Target, new Dictionary<int, EntityId>(state.Targets));
    }

    private static IReadOnlyList<ActionOutcome> BuildLocalLog(EntityId entityId, PlaneId? inventoryPlaneId, ActionLogProjection? actionLog)
    {
        if (actionLog is null)
        {
            return [];
        }

        var outcomes = new List<ActionOutcome>();
        outcomes.AddRange(actionLog.ForEntity(entityId));
        if (inventoryPlaneId is { } planeId)
        {
            outcomes.AddRange(actionLog.ForPlane(planeId));
        }

        return outcomes
            .DistinctBy(outcome => ReferenceEquals(outcome.Trace, null) ? outcome.GetHashCode() : outcome.Trace.GetHashCode())
            .ToList();
    }

    private static IReadOnlyList<EntityPanelContentRow> BuildContents(
        WorldState world,
        EntityInspectionPanel panel,
        IReadOnlyDictionary<EntityId, IEntityActionPlan> actionPlans,
        EntityId? playerId,
        IReadOnlyList<ActionOutcome> localLog)
    {
        if (panel.InventoryGrid is not { } grid)
        {
            return [];
        }

        var localTurnOrder = LocalTurnOrderReport.Create(world, grid.PlaneId, actionPlans, playerId, rowEntityId =>
            grid.Cells.FirstOrDefault(cell => cell.EntityId == rowEntityId)?.Glyph ?? '?');

        return localTurnOrder.Rows
            .Select(row => new EntityPanelContentRow(
                row.Order,
                row.EntityId,
                row.EntityName,
                row.Glyph,
                row.Location,
                row.Participation,
                localLog.LastOrDefault(outcome => outcome.AnchorEntityIds.Contains(row.EntityId))?.Sentence ?? row.PreviousAction))
            .ToList();
    }
}
