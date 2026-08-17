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
        PlayHighlightState? highlight = null)
    {
        var entityId = visual.EntityId ?? throw new InvalidOperationException("Cannot build an inspection panel model for an empty play cell.");
        var entity = session.World.Entities[entityId];
        return new EntityInspectionPanelModel(
            string.IsNullOrWhiteSpace(entity.Name) ? entityId.ToString() : entity.Name,
            entity.Aperture,
            entity.Bulk,
            entity.HasUsableInventory,
            InspectionPortraitProjector.Project(grid, visual, highlight),
            InspectionActionChoiceProjector.Project(actionChoiceRequest, entityId));
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
