using GameGameGame.Content;
using GameGameGame.Core;
using SadRogue.Primitives;

namespace GameGameGame.Frontend.SadConsole;

internal sealed record PlayCellVisual(
    int X,
    int Y,
    int BackdropGlyph,
    Color BackdropForeground,
    Color BackdropBackground,
    int? EntityGlyph = null,
    Color? EntityForeground = null,
    EntityId? EntityId = null);

internal sealed record PlayGridViewModel(
    string Title,
    int Width,
    int Height,
    IReadOnlyList<PlayCellVisual> Cells,
    EntityId ControlledEntityId,
    GridCoord? ControlledEntityCoord)
{
    public PlayCellVisual CellAt(int x, int y) => Cells.Single(cell => cell.X == x && cell.Y == y);

    public static PlayGridViewModel FromSession(PlayableScenarioSession session, TilesetProfile tilesetProfile)
    {
        var planeId = session.World.GetRegisteredInventoryPlaneId(session.ActiveContainerEntityId)
            ?? session.ActivePlaneId;
        var plane = session.World.Planes[planeId];
        var cells = new List<PlayCellVisual>();

        for (var y = 0; y < plane.Height; y++)
        {
            for (var x = 0; x < plane.Width; x++)
            {
                var coord = new PlaneCoord(planeId, new GridCoord(x, y));
                var occupant = session.World.GetOccupant(coord);
                var entityGlyph = occupant is { } entityId
                    ? ResolveEntityGlyph(session, tilesetProfile, entityId)
                    : (int?)null;

                cells.Add(new PlayCellVisual(
                    x,
                    y,
                    tilesetProfile.Roles.DefaultBackdrop,
                    Color.Gray,
                    Color.Black,
                    entityGlyph,
                    occupant == session.PlayerEntityId ? Color.Yellow : Color.White,
                    occupant));
            }
        }

        var controlledCoord = session.World.Entities.ContainsKey(session.PlayerEntityId)
            ? session.World.GetEntityLocation(session.PlayerEntityId).Coord
            : (GridCoord?)null;

        return new PlayGridViewModel(
            session.Name,
            plane.Width,
            plane.Height,
            cells,
            session.PlayerEntityId,
            controlledCoord);
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

internal static class PlayGridRenderer
{
    public static void Draw(global::SadConsole.Console target, FrontendRect bounds, PlayGridViewModel model)
    {
        var startX = bounds.X + Math.Max(0, (bounds.Width - model.Width) / 2);
        var startY = bounds.Y + Math.Max(0, (bounds.Height - model.Height) / 2);

        foreach (var cell in model.Cells)
        {
            var x = startX + cell.X;
            var y = startY + cell.Y;
            SetGlyph(target, x, y, cell.BackdropGlyph, cell.BackdropForeground, cell.BackdropBackground);
            if (cell.EntityGlyph is { } entityGlyph)
            {
                SetGlyph(target, x, y, entityGlyph, cell.EntityForeground ?? Color.White, cell.BackdropBackground);
            }
        }
    }

    private static void SetGlyph(global::SadConsole.Console target, int x, int y, int glyph, Color foreground, Color background)
    {
        if (x < 0 || y < 0 || x >= target.Width || y >= target.Height)
        {
            return;
        }

        target.Surface[x, y].Glyph = glyph;
        target.Surface[x, y].Foreground = foreground;
        target.Surface[x, y].Background = background;
    }
}
