using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.ConsoleApp;

public static class ConsoleInspectionDisplayFormatter
{
    public static string FormatBreadcrumb(
        WorldState world,
        EntityContainmentPath path,
        Func<EntityId, char> getGlyph)
    {
        if (path.Segments.Count == 0)
        {
            return $"{path.RequestedEntityId} [{path.Status}]";
        }

        var breadcrumb = string.Join(" > ", path.Segments.Select(segment => FormatSegment(world, segment.EntityId, getGlyph)));
        return path.Status == EntityContainmentPathStatus.Complete
            ? breadcrumb
            : $"{breadcrumb} [{path.Status}]";
    }

    public static IReadOnlyList<EntityInspectionProperty> BuildPanelProperties(
        WorldState world,
        EntityInspectionPanel panel,
        EntityContainmentPath path,
        LocalTurnOrderReport? turnOrderReport,
        Func<EntityId, char> getGlyph)
    {
        var entityId = panel.EntityId;
        var location = world.GetEntityLocation(entityId);
        var inventory = panel.InventoryGrid is { } grid
            ? $"{grid.PlaneId} ({grid.Width}x{grid.Height})"
            : "none";
        var facing = world.GetActionFacing(entityId)?.ToString() ?? "none";
        var target = world.GetActionTarget(entityId) is { } targetId
            ? FormatSegment(world, targetId, getGlyph)
            : "none";
        var previousAction = turnOrderReport?.Rows.FirstOrDefault(row => row.EntityId == entityId)?.PreviousAction
            ?? world.LastTurnReport?.Actions.LastOrDefault(action => action.ActorId == entityId)?.Summary
            ?? "None";

        return
        [
            new("Path", FormatBreadcrumb(world, path, getGlyph)),
            new("Location", location.ToString()),
            new("Inventory", inventory),
            new("Load", FormatLoad(panel)),
            new("Facing", facing),
            new("Target", target),
            new("Previous", previousAction)
        ];
    }

    private static string FormatSegment(WorldState world, EntityId entityId, Func<EntityId, char> getGlyph) =>
        world.Entities.TryGetValue(entityId, out var entity)
            ? $"{getGlyph(entityId)} {entity.Name}"
            : entityId.ToString();

    private static string FormatLoad(EntityInspectionPanel panel)
    {
        var bulk = FindProperty(panel, "Bulk") ?? "?";
        var aperture = FindProperty(panel, "Aperture") ?? "?";
        return $"bulk {bulk}, aperture {aperture}";
    }

    private static string? FindProperty(EntityInspectionPanel panel, string name) =>
        panel.Properties.FirstOrDefault(property => property.Name == name)?.Value;
}
