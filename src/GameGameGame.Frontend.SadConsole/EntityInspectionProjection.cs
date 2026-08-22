using GameGameGame.Content;
using GameGameGame.Core;
using SadRogue.Primitives;

namespace GameGameGame.Frontend.SadConsole;

internal static class EntityInspectionPanelModelFactory
{
    public static EntityInspectionPanelModel FromEntity(
        PlayableScenarioSession session,
        PlayGridViewModel grid,
        PlayCellVisual visual,
        ActionChoiceRequest? actionChoiceRequest,
        TilesetProfile tilesetProfile,
        PlayHighlightState? highlight = null,
        PlayHighlightState? inventoryHighlight = null)
    {
        var entityId = visual.EntityId ?? throw new InvalidOperationException("Cannot build an inspection panel model for an empty play cell.");
        var entity = session.World.Entities[entityId];
        return new EntityInspectionPanelModel(
            string.IsNullOrWhiteSpace(entity.Name) ? entityId.ToString() : entity.Name,
            entity.Aperture,
            entity.Bulk,
            entity.HasUsableInventory,
            InspectionPortraitProjector.Project(grid, visual, highlight),
            InspectionInventoryProjector.Project(session, entityId, tilesetProfile, inventoryHighlight),
            InspectionActionChoiceProjector.Project(actionChoiceRequest, entityId));
    }
}

internal static class InspectionInventoryProjector
{
    public static IReadOnlyList<EntityInspectionPortraitCell> Project(PlayableScenarioSession session, EntityId entityId, TilesetProfile tilesetProfile, PlayHighlightState? highlight = null)
    {
        if (session.World.GetRegisteredInventoryPlaneId(entityId) is not { } planeId
            || !session.World.Planes.TryGetValue(planeId, out var plane))
        {
            return [];
        }

        var cells = new List<EntityInspectionPortraitCell>(plane.Width * plane.Height);
        for (var y = 0; y < plane.Height; y++)
        for (var x = 0; x < plane.Width; x++)
        {
            var coord = new PlaneCoord(planeId, new GridCoord(x, y));
            var occupant = session.World.GetOccupant(coord);
            var glyph = occupant is { } occupantId ? ResolveEntityGlyph(session, tilesetProfile, occupantId) : (int?)null;
            var facing = occupant is { } facingEntityId && session.World.GetActionFacing(facingEntityId) is { } direction
                ? tilesetProfile.Roles.FacingGlyph(direction)
                : ((int Glyph, global::SadConsole.Mirror Mirror)?)null;
            cells.Add(new EntityInspectionPortraitCell(
                x,
                y,
                tilesetProfile.Roles.DefaultBackdrop,
                Color.DimGray,
                Color.Black,
                glyph,
                glyph is null ? null : Color.White,
                facing?.Glyph,
                facing?.Mirror ?? global::SadConsole.Mirror.None,
                highlight?.Coord == new GridCoord(x, y) ? highlight.Kind : null));
        }

        return cells;
    }

    private static int ResolveEntityGlyph(PlayableScenarioSession session, TilesetProfile tilesetProfile, EntityId entityId)
    {
        if (session.Registry.TryGetTemplateIdForEntity(session.World, entityId, out var templateId)
            && session.Registry.Presentations.TryGetValue(templateId, out var presentation)
            && tilesetProfile.PresentationMappings.GlyphsByPresentationId.TryGetValue(presentation.PresentationId.Value, out var mappedGlyph))
        {
            return mappedGlyph;
        }

        return session.World.Entities.TryGetValue(entityId, out var entity) && !string.IsNullOrWhiteSpace(entity.Name)
            ? entity.Name[0]
            : '?';
    }
}

internal static class InspectionPortraitProjector
{
    public static IReadOnlyList<EntityInspectionPortraitCell> Project(PlayGridViewModel grid, PlayCellVisual center, PlayHighlightState? highlight)
    {
        var cells = new List<EntityInspectionPortraitCell>(9);
        for (var y = 0; y < 3; y++)
        for (var x = 0; x < 3; x++)
        {
            var sourceCoord = new GridCoord(center.X + x - 1, center.Y + y - 1);
            var source = grid.TryCellAt(sourceCoord.X, sourceCoord.Y);
            var highlightKind = highlight?.Coord == sourceCoord ? highlight.Kind : (CellHighlightKind?)null;
            cells.Add(source is null
                ? new EntityInspectionPortraitCell(x, y, 160, Color.Black, Color.Black, HighlightKind: highlightKind)
                : new EntityInspectionPortraitCell(
                    x,
                    y,
                    source.BackdropGlyph,
                    source.BackdropForeground,
                    source.BackdropBackground,
                    source.EntityGlyph,
                    source.EntityForeground,
                    source.FacingGlyph,
                    source.FacingMirror,
                    highlightKind));
        }

        return cells;
    }
}

internal static class InspectionActionChoiceProjector
{
    public static IReadOnlyList<EntityInspectionActionRow> Project(ActionChoiceRequest? request, EntityId targetId)
    {
        if (request is null)
        {
            return [NoValidActions()];
        }

        var rows = new List<EntityInspectionActionRow>();
        foreach (var candidate in PlayActionCandidateProjector.ForInspectedEntity(request, targetId))
        {
            rows.Add(Row(candidate));
        }

        return rows.Count == 0 ? [NoValidActions()] : rows;
    }

    public static IReadOnlyList<EntityInspectionActionRow> ProjectPlayerInventory(ActionChoiceRequest? request)
    {
        var rows = new List<EntityInspectionActionRow>();
        foreach (var candidate in PlayActionCandidateProjector.ForPlayerInventory(request))
        {
            rows.Add(Row(candidate));
        }

        return rows.Count == 0 ? [NoValidActions()] : rows;
    }

    private static EntityInspectionActionRow NoValidActions() => new(
        FrontendTextMessage.Create(FrontendTextIds.InspectionActionNoValidActions),
        Selectable: false);

    private static EntityInspectionActionRow Row(PlayActionCandidate candidate) => candidate.IsValid
        ? new EntityInspectionActionRow(candidate.Text, Selectable: true, Candidate: candidate)
        : new EntityInspectionActionRow(
            FrontendTextMessage.Create(
                FrontendTextIds.InspectionActionUnavailable,
                ("action", FrontendTextResolver.InspectionPrototype.Resolve(candidate.Text)),
                ("reason", Explain(candidate.Explanation))),
            Selectable: false,
            candidate.Explanation,
            candidate);

    private static string Explain(FrontendTextMessage? message) => message is null
        ? "unavailable"
        : FrontendTextResolver.InspectionPrototype.Resolve(message);
}
