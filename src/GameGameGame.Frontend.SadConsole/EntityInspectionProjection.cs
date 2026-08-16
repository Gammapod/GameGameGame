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
            InspectionActionChoiceProjector.Project(session, entityId));
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
    public static IReadOnlyList<EntityInspectionActionRow> Project(PlayableScenarioSession session, EntityId targetId)
    {
        if (CreateActionChoiceRequest(session) is not { } request)
        {
            return [NoValidActions()];
        }

        var rows = new List<EntityInspectionActionRow>();
        foreach (var choice in request.Choices)
        {
            rows.AddRange(RowsForChoice(choice, targetId));
        }

        return rows.Count == 0 ? [NoValidActions()] : rows;
    }

    private static ActionChoiceRequest? CreateActionChoiceRequest(PlayableScenarioSession session)
    {
        if (!session.Registry.TryGetTemplateIdForEntity(session.World, session.PlayerEntityId, out var templateId))
        {
            return null;
        }

        var template = session.Registry.GetEntityTemplate(templateId);
        var defaultPlanId = session.World.GetDefaultActionPlanId(session.PlayerEntityId) is { } runtimePlanId
            ? new ActionPlanTemplateId(runtimePlanId.Value)
            : template.DefaultActionPlanId;
        if (defaultPlanId is not { } planId || !session.Registry.ActionPlanDescriptors.TryGetValue(planId, out var descriptor))
        {
            return null;
        }

        return new ActionChoiceService(new MovementService()).CreateRequest(session.World, session.PlayerEntityId, descriptor);
    }

    private static IEnumerable<EntityInspectionActionRow> RowsForChoice(ActionChoice choice, EntityId targetId)
    {
        foreach (var option in choice.EntityOptions.Where(option => option.TargetId == targetId))
        {
            yield return Row(ActionText(choice.Kind, targetId), option.CanExecute, option.FailureReason, option.FailureDetail);
        }

        if (choice.Kind != ActionChoiceKind.Transfer)
        {
            yield break;
        }

        foreach (var option in choice.TransferCounterparties.Where(option => option.CounterpartyId == targetId))
        {
            yield return Row(ActionText(ActionChoiceKind.Transfer, targetId), option.CanExecute, option.FailureReason, option.FailureDetail);
        }
    }

    private static EntityInspectionActionRow NoValidActions() => new(
        FrontendTextMessage.Create(FrontendTextIds.InspectionActionNoValidActions),
        Selectable: false);

    private static EntityInspectionActionRow Row(FrontendTextMessage action, bool canExecute, FailureReason? failureReason, string? failureDetail) => canExecute
        ? new EntityInspectionActionRow(action, Selectable: true)
        : new EntityInspectionActionRow(
            FrontendTextMessage.Create(
                FrontendTextIds.InspectionActionUnavailable,
                ("action", FrontendTextResolver.InspectionPrototype.Resolve(action)),
                ("reason", FailureText(failureReason, failureDetail))),
            Selectable: false,
            FrontendTextMessage.Create(FailureTextId(failureReason), ("detail", failureDetail ?? string.Empty)));

    private static FrontendTextMessage ActionText(ActionChoiceKind kind, EntityId targetId) => kind switch
    {
        ActionChoiceKind.Pickup => FrontendTextMessage.Create(FrontendTextIds.InspectionActionPickup, ("targetName", targetId.Value)),
        ActionChoiceKind.Drop => FrontendTextMessage.Create(FrontendTextIds.InspectionActionDrop, ("targetName", targetId.Value)),
        ActionChoiceKind.Enter => FrontendTextMessage.Create(FrontendTextIds.InspectionActionEnter, ("targetName", targetId.Value)),
        ActionChoiceKind.Push => FrontendTextMessage.Create(FrontendTextIds.InspectionActionPush, ("targetName", targetId.Value)),
        ActionChoiceKind.Transfer => FrontendTextMessage.Create(FrontendTextIds.InspectionActionTransfer, ("targetName", targetId.Value)),
        _ => FrontendTextMessage.Create(FrontendTextIds.InspectionActionGeneric, ("actionName", kind), ("targetName", targetId.Value))
    };

    private static string FailureTextId(FailureReason? failureReason) => failureReason is null
        ? "inspection.failure.unavailable"
        : $"inspection.failure.{failureReason}";

    private static string FailureText(FailureReason? failureReason, string? failureDetail) =>
        !string.IsNullOrWhiteSpace(failureDetail) ? failureDetail : failureReason?.ToString() ?? "unavailable";
}
