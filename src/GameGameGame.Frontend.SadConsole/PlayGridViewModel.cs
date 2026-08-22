using GameGameGame.Content;
using GameGameGame.Core;
using SadRogue.Primitives;
using SadMirror = SadConsole.Mirror;

namespace GameGameGame.Frontend.SadConsole;

internal sealed record PlayCellVisual(
    int X,
    int Y,
    int BackdropGlyph,
    Color BackdropForeground,
    Color BackdropBackground,
    int? EntityGlyph = null,
    Color? EntityForeground = null,
    EntityId? EntityId = null,
    int? FacingGlyph = null,
    SadMirror FacingMirror = SadMirror.None);

internal sealed record PlayGridViewModel(
    string Title,
    int Width,
    int Height,
    IReadOnlyList<PlayCellVisual> Cells,
    EntityId ControlledEntityId,
    GridCoord? ControlledEntityCoord,
    PlaneId PlaneId,
    EntityId? ContainerEntityId)
{
    private readonly IReadOnlyDictionary<(int X, int Y), PlayCellVisual> _cellsByCoord = Cells.ToDictionary(cell => (cell.X, cell.Y));

    public PlayCellVisual? TryCellAt(int x, int y) => _cellsByCoord.TryGetValue((x, y), out var cell) ? cell : null;

    public PlayCellVisual CellAt(int x, int y) => TryCellAt(x, y)
        ?? throw new InvalidOperationException($"Cell ({x},{y}) is outside rendered plane {PlaneId}.");

    public static PlayGridViewModel FromSession(PlayableScenarioSession session, TilesetProfile tilesetProfile, PlaneId? preferredPlaneId = null)
    {
        var planeId = preferredPlaneId is { } requestedPlaneId && session.World.Planes.ContainsKey(requestedPlaneId)
            ? requestedPlaneId
            : ResolveRenderedPlane(session);
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
                var facing = occupant is { } facingEntityId && session.World.GetActionFacing(facingEntityId) is { } direction
                    ? tilesetProfile.Roles.FacingGlyph(direction)
                    : ((int Glyph, SadMirror Mirror)?)null;

                cells.Add(new PlayCellVisual(
                    x,
                    y,
                    tilesetProfile.Roles.DefaultBackdrop,
                    Color.Gray,
                    Color.Black,
                    entityGlyph,
                    occupant == session.PlayerEntityId ? Color.Yellow : Color.White,
                    occupant,
                    facing?.Glyph,
                    facing?.Mirror ?? SadMirror.None));
            }
        }

        var controlledCoord = session.World.Entities.ContainsKey(session.PlayerEntityId)
            && session.World.GetEntityLocation(session.PlayerEntityId).PlaneId == planeId
            ? session.World.GetEntityLocation(session.PlayerEntityId).Coord
            : (GridCoord?)null;
        var containerEntityId = InventoryPlaneOwnership.TryFindOwner(session.World, planeId, out var ownerId)
            ? ownerId
            : (EntityId?)null;

        return new PlayGridViewModel(
            session.Name,
            plane.Width,
            plane.Height,
            cells,
            session.PlayerEntityId,
            controlledCoord,
            planeId,
            containerEntityId);
    }

    private static PlaneId ResolveRenderedPlane(PlayableScenarioSession session)
    {
        if (session.World.Entities.ContainsKey(session.PlayerEntityId))
        {
            var playerPlane = session.World.GetEntityLocation(session.PlayerEntityId).PlaneId;
            if (session.World.Planes.ContainsKey(playerPlane))
            {
                return playerPlane;
            }
        }

        var activeContainerPlane = session.World.GetRegisteredInventoryPlaneId(session.ActiveContainerEntityId);
        if (activeContainerPlane is { } planeId && session.World.Planes.ContainsKey(planeId))
        {
            return planeId;
        }

        return session.ActivePlaneId;
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
    public static FrontendRect ResolveGridBounds(FrontendRect bounds, PlayGridViewModel model) => new(
        bounds.X + Math.Max(0, (bounds.Width - model.Width) / 2),
        bounds.Y + Math.Max(0, (bounds.Height - model.Height) / 2),
        model.Width,
        model.Height);

    public static void Draw(
        global::SadConsole.Console target,
        FrontendRect bounds,
        PlayGridViewModel model,
        IReadOnlySet<EntityId>? hiddenEntityIds = null,
        GridCoord? movementPreviewCoord = null,
        CellHighlightPresentation? movementPreviewHighlight = null)
    {
        var gridBounds = ResolveGridBounds(bounds, model);

        foreach (var cell in model.Cells)
        {
            var x = gridBounds.X + cell.X;
            var y = gridBounds.Y + cell.Y;
            SetGlyph(target, x, y, cell.BackdropGlyph, cell.BackdropForeground, cell.BackdropBackground);
            var entityHidden = cell.EntityId is { } entityId && hiddenEntityIds?.Contains(entityId) == true;
            if (cell.EntityGlyph is { } entityGlyph && !entityHidden)
            {
                SetGlyph(target, x, y, entityGlyph, cell.EntityForeground ?? Color.White, cell.BackdropBackground);
            }

            ApplyDecorators(target, x, y, cell, entityHidden, movementPreviewCoord == new GridCoord(cell.X, cell.Y) ? movementPreviewHighlight : null);
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
        target.Surface[x, y].Mirror = SadMirror.None;
        target.Surface[x, y].Decorators = null;
    }

    private static void ApplyDecorators(global::SadConsole.Console target, int x, int y, PlayCellVisual cell, bool entityHidden, CellHighlightPresentation? movementPreviewHighlight)
    {
        var decorators = new List<global::SadConsole.CellDecorator>();
        if (!entityHidden && cell.FacingGlyph is { } facingGlyph && cell.EntityGlyph is not null)
        {
            decorators.Add(new global::SadConsole.CellDecorator(Color.LightYellow, facingGlyph, cell.FacingMirror));
        }

        if (movementPreviewHighlight is not null)
        {
            decorators.Add(new global::SadConsole.CellDecorator(movementPreviewHighlight.Foreground, movementPreviewHighlight.Glyph, movementPreviewHighlight.Mirror));
        }

        if (decorators.Count > 0)
        {
            target.Surface[x, y].Decorators = decorators;
        }
    }
}

internal sealed record PlayRenderedGridCell(
    int Glyph,
    Color Foreground,
    Color Background,
    int? FacingGlyph,
    SadMirror FacingMirror,
    int? HighlightGlyph,
    Color? HighlightForeground,
    SadMirror HighlightMirror);

internal sealed class PlayGridSurfacePresenter
{
    private readonly Dictionary<(int X, int Y), PlayRenderedGridCell> _drawnCells = [];
    private FrontendRect? _drawnGridBounds;

    public void Invalidate()
    {
        _drawnCells.Clear();
        _drawnGridBounds = null;
    }

    public void Draw(
        global::SadConsole.Console target,
        FrontendRect bounds,
        PlayGridViewModel model,
        IReadOnlySet<EntityId>? hiddenEntityIds = null,
        GridCoord? movementPreviewCoord = null,
        CellHighlightPresentation? movementPreviewHighlight = null)
    {
        var gridBounds = PlayGridRenderer.ResolveGridBounds(bounds, model);
        if (_drawnGridBounds != gridBounds)
        {
            _drawnCells.Clear();
            _drawnGridBounds = gridBounds;
        }

        foreach (var cell in model.Cells)
        {
            var x = gridBounds.X + cell.X;
            var y = gridBounds.Y + cell.Y;
            var entityHidden = cell.EntityId is { } entityId && hiddenEntityIds?.Contains(entityId) == true;
            var highlight = movementPreviewCoord == new GridCoord(cell.X, cell.Y) ? movementPreviewHighlight : null;
            var state = ToRenderedCell(cell, entityHidden, highlight);
            var key = (x, y);
            if (_drawnCells.TryGetValue(key, out var previous) && previous == state)
            {
                continue;
            }

            DrawCell(target, x, y, state);
            _drawnCells[key] = state;
        }
    }

    private static PlayRenderedGridCell ToRenderedCell(PlayCellVisual cell, bool entityHidden, CellHighlightPresentation? highlight)
    {
        var glyph = !entityHidden && cell.EntityGlyph is { } entityGlyph ? entityGlyph : cell.BackdropGlyph;
        var foreground = !entityHidden && cell.EntityGlyph is not null ? cell.EntityForeground ?? Color.White : cell.BackdropForeground;
        return new PlayRenderedGridCell(
            glyph,
            foreground,
            cell.BackdropBackground,
            !entityHidden && cell.FacingGlyph is { } facingGlyph && cell.EntityGlyph is not null ? facingGlyph : null,
            cell.FacingMirror,
            highlight?.Glyph,
            highlight?.Foreground,
            highlight?.Mirror ?? SadMirror.None);
    }

    private static void DrawCell(global::SadConsole.Console target, int x, int y, PlayRenderedGridCell state)
    {
        if (x < 0 || y < 0 || x >= target.Width || y >= target.Height)
        {
            return;
        }

        target.Surface[x, y].Glyph = state.Glyph;
        target.Surface[x, y].Foreground = state.Foreground;
        target.Surface[x, y].Background = state.Background;
        target.Surface[x, y].Mirror = SadMirror.None;
        var decorators = new List<global::SadConsole.CellDecorator>();
        if (state.FacingGlyph is { } facingGlyph)
        {
            decorators.Add(new global::SadConsole.CellDecorator(Color.LightYellow, facingGlyph, state.FacingMirror));
        }

        if (state.HighlightGlyph is { } highlightGlyph && state.HighlightForeground is { } highlightForeground)
        {
            decorators.Add(new global::SadConsole.CellDecorator(highlightForeground, highlightGlyph, state.HighlightMirror));
        }

        target.Surface[x, y].Decorators = decorators.Count == 0 ? null : decorators;
    }
}
