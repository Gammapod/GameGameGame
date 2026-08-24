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
    SadMirror FacingMirror = SadMirror.None,
    bool IsInPointOfView = true,
    bool IsDimmedByPointOfView = false,
    TopologyNodeId? TopologyNodeId = null,
    TopologyLayoutCoord? LayoutCoord = null,
    PlaneCoord? SourceCoord = null);

internal enum PlayGridDiagnosticCode
{
    DisplayCoordinateCollision
}

internal sealed record PlayGridDiagnostic(
    PlayGridDiagnosticCode Code,
    GridCoord DisplayCoord,
    IReadOnlyList<PlaneCoord> SourceCoords,
    string Message);

internal sealed record PlayGridViewModel(
    string Title,
    int Width,
    int Height,
    IReadOnlyList<PlayCellVisual> Cells,
    EntityId ControlledEntityId,
    GridCoord? ControlledEntityCoord,
    PlaneId PlaneId,
    EntityId? ContainerEntityId,
    IReadOnlyList<PlayGridDiagnostic> Diagnostics)
{
    private readonly IReadOnlyDictionary<(int X, int Y), PlayCellVisual> _cellsByCoord = Cells.ToDictionary(cell => (cell.X, cell.Y));
    private readonly IReadOnlyDictionary<PlaneCoord, PlayCellVisual> _cellsBySourceCoord = Cells
        .Where(cell => cell.SourceCoord is not null)
        .ToDictionary(cell => cell.SourceCoord!.Value);

    public PlayCellVisual? TryCellAt(int x, int y) => _cellsByCoord.TryGetValue((x, y), out var cell) ? cell : null;

    public PlayCellVisual? TryCellForSource(PlaneCoord sourceCoord) =>
        _cellsBySourceCoord.TryGetValue(sourceCoord, out var cell) ? cell : null;

    public GridCoord? TryDisplayCoordForSource(PlaneCoord sourceCoord)
    {
        var cell = TryCellForSource(sourceCoord);
        return cell is null ? null : new GridCoord(cell.X, cell.Y);
    }

    public PlayCellVisual CellAt(int x, int y) => TryCellAt(x, y)
        ?? throw new InvalidOperationException($"Cell ({x},{y}) is outside rendered plane {PlaneId}.");

    public static PlayGridViewModel FromSession(
        PlayableScenarioSession session,
        TilesetProfile tilesetProfile,
        PlaneId? preferredPlaneId = null,
        TopologyVisibilityProjection? topologyVisibility = null,
        bool showOutsidePointOfViewContext = false)
    {
        var planeId = preferredPlaneId is { } requestedPlaneId && session.World.Planes.ContainsKey(requestedPlaneId)
            ? requestedPlaneId
            : ResolveRenderedPlane(session);
        var plane = session.World.Planes[planeId];
        var cellsByDisplayCoord = new Dictionary<(int X, int Y), PlayCellVisual>();
        var diagnostics = new List<PlayGridDiagnostic>();
        var visibleCellsBySource = topologyVisibility?.VisibleCells
            .ToDictionary(cell => cell.Cell.SourceCoord, cell => cell);
        if (topologyVisibility is null)
        {
            for (var y = 0; y < plane.Height; y++)
            {
                for (var x = 0; x < plane.Width; x++)
                {
                    var coord = new PlaneCoord(planeId, new GridCoord(x, y));
                    var displayCoord = new GridCoord(x, y);
                    AddCellVisual(cellsByDisplayCoord, diagnostics, BuildCellVisual(
                        session,
                        tilesetProfile,
                        coord,
                        displayCoord,
                        isInPointOfView: true,
                        visibleCell: null));
                }
            }
        }
        else
        {
            var projectedCells = topologyVisibility.ContextCells
                .Select(contextCell => new
                {
                    Cell = contextCell,
                    RawDisplayCoord = contextCell.LayoutCoord?.Coord ?? contextCell.Cell.SourceCoord.Coord,
                    IsInPointOfView = visibleCellsBySource?.ContainsKey(contextCell.Cell.SourceCoord) == true
                })
                .ToList();
            var minX = projectedCells.Count == 0 ? 0 : projectedCells.Min(cell => cell.RawDisplayCoord.X);
            var minY = projectedCells.Count == 0 ? 0 : projectedCells.Min(cell => cell.RawDisplayCoord.Y);
            var maxX = projectedCells.Count == 0 ? plane.Width - 1 : projectedCells.Max(cell => cell.RawDisplayCoord.X);
            var maxY = projectedCells.Count == 0 ? plane.Height - 1 : projectedCells.Max(cell => cell.RawDisplayCoord.Y);

            foreach (var projectedCell in projectedCells)
            {
                if (!showOutsidePointOfViewContext && !projectedCell.IsInPointOfView)
                {
                    continue;
                }

                var contextCell = projectedCell.Cell;
                var displayCoord = new GridCoord(
                    projectedCell.RawDisplayCoord.X - minX,
                    projectedCell.RawDisplayCoord.Y - minY);
                var isInPointOfView = projectedCell.IsInPointOfView;
                AddCellVisual(cellsByDisplayCoord, diagnostics, BuildCellVisual(
                    session,
                    tilesetProfile,
                    contextCell.Cell.SourceCoord,
                    displayCoord,
                    isInPointOfView,
                    isInPointOfView ? visibleCellsBySource![contextCell.Cell.SourceCoord] : contextCell));
            }

            if (cellsByDisplayCoord.Count == 0 && topologyVisibility.Origin.SourceCoord.PlaneId == planeId)
            {
                var originDisplayCoord = new GridCoord(
                    topologyVisibility.Origin.SourceCoord.Coord.X - minX,
                    topologyVisibility.Origin.SourceCoord.Coord.Y - minY);
                AddCellVisual(cellsByDisplayCoord, diagnostics, BuildCellVisual(
                    session,
                    tilesetProfile,
                    topologyVisibility.Origin.SourceCoord,
                    originDisplayCoord,
                    isInPointOfView: true,
                    visibleCell: null));
            }

            var projectedWidth = Math.Max(0, maxX - minX + 1);
            var projectedHeight = Math.Max(0, maxY - minY + 1);
            return BuildModel(session, planeId, plane, topologyVisibility, cellsByDisplayCoord.Values, diagnostics, projectedWidth, projectedHeight);
        }

        return BuildModel(session, planeId, plane, topologyVisibility, cellsByDisplayCoord.Values, diagnostics, null, null);
    }

    private static PlayGridViewModel BuildModel(
        PlayableScenarioSession session,
        PlaneId planeId,
        Plane plane,
        TopologyVisibilityProjection? topologyVisibility,
        IEnumerable<PlayCellVisual> cellVisuals,
        IReadOnlyList<PlayGridDiagnostic> diagnostics,
        int? projectedWidth,
        int? projectedHeight)
    {
        var cells = cellVisuals
            .OrderBy(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .ToList();
        var width = Math.Max(projectedWidth ?? 0, cells.Count == 0 ? plane.Width : Math.Max(plane.Width, cells.Max(cell => cell.X) + 1));
        var height = Math.Max(projectedHeight ?? 0, cells.Count == 0 ? plane.Height : Math.Max(plane.Height, cells.Max(cell => cell.Y) + 1));

        var controlledCoord = session.World.Entities.ContainsKey(session.PlayerEntityId)
            && session.World.GetEntityLocation(session.PlayerEntityId).PlaneId == planeId
            ? session.World.GetEntityLocation(session.PlayerEntityId).Coord
            : (GridCoord?)null;
        if (topologyVisibility is not null
            && cells.FirstOrDefault(cell => cell.SourceCoord == topologyVisibility.Origin.SourceCoord) is { } originCell)
        {
            controlledCoord = new GridCoord(originCell.X, originCell.Y);
        }

        var containerEntityId = InventoryPlaneOwnership.TryFindOwner(session.World, planeId, out var ownerId)
            ? ownerId
            : (EntityId?)null;

        return new PlayGridViewModel(
            session.Name,
            width,
            height,
            cells,
            session.PlayerEntityId,
            controlledCoord,
            planeId,
            containerEntityId,
            diagnostics);
    }

    private static void AddCellVisual(
        Dictionary<(int X, int Y), PlayCellVisual> cellsByDisplayCoord,
        List<PlayGridDiagnostic> diagnostics,
        PlayCellVisual cell)
    {
        var key = (cell.X, cell.Y);
        if (cellsByDisplayCoord.TryGetValue(key, out var existing)
            && existing.SourceCoord is { } existingSource
            && cell.SourceCoord is { } newSource
            && existingSource != newSource)
        {
            diagnostics.Add(new PlayGridDiagnostic(
                PlayGridDiagnosticCode.DisplayCoordinateCollision,
                new GridCoord(cell.X, cell.Y),
                [existingSource, newSource],
                $"Topology display coordinate ({cell.X},{cell.Y}) contains multiple source cells: {existingSource} and {newSource}."));
        }

        if (!cellsByDisplayCoord.TryGetValue(key, out var current) || ShouldReplaceDisplayedCell(current, cell))
        {
            cellsByDisplayCoord[key] = cell;
        }
    }

    private static bool ShouldReplaceDisplayedCell(PlayCellVisual current, PlayCellVisual candidate)
    {
        if (candidate.IsInPointOfView != current.IsInPointOfView)
        {
            return candidate.IsInPointOfView;
        }

        return true;
    }

    private static PlayCellVisual BuildCellVisual(
        PlayableScenarioSession session,
        TilesetProfile tilesetProfile,
        PlaneCoord sourceCoord,
        GridCoord displayCoord,
        bool isInPointOfView,
        TopologyVisibleCellProjection? visibleCell)
    {
        var occupant = session.World.GetOccupant(sourceCoord);
        var entityGlyph = occupant is { } entityId
            ? ResolveEntityGlyph(session, tilesetProfile, entityId)
            : (int?)null;
        var facing = occupant is { } facingEntityId && session.World.GetActionFacing(facingEntityId) is { } direction
            ? tilesetProfile.Roles.FacingGlyph(direction)
            : ((int Glyph, SadMirror Mirror)?)null;
        var backdropForeground = isInPointOfView ? Color.Gray : Color.DimGray;
        var entityForeground = occupant == session.PlayerEntityId
            ? Color.Yellow
            : isInPointOfView ? Color.White : Color.DimGray;

        return new PlayCellVisual(
            displayCoord.X,
            displayCoord.Y,
            ResolveBackdropGlyph(session, tilesetProfile, sourceCoord.PlaneId),
            backdropForeground,
            Color.Black,
            entityGlyph,
            entityForeground,
            occupant,
            facing?.Glyph,
            facing?.Mirror ?? SadMirror.None,
            isInPointOfView,
            !isInPointOfView,
            visibleCell?.NodeId,
            visibleCell?.LayoutCoord,
            sourceCoord);
    }

    private static int ResolveBackdropGlyph(PlayableScenarioSession session, TilesetProfile tilesetProfile, PlaneId planeId)
    {
        return InventoryPlaneOwnership.TryFindOwner(session.World, planeId, out var ownerId)
            && session.World.Entities.TryGetValue(ownerId, out var owner)
            ? tilesetProfile.Roles.BackdropForMaterial(owner.Material)
            : tilesetProfile.Roles.DefaultBackdrop;
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
        GridCoord? highlightCoord = null,
        CellHighlightPresentation? cellHighlight = null)
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

            ApplyDecorators(target, x, y, cell, entityHidden, highlightCoord == new GridCoord(cell.X, cell.Y) ? cellHighlight : null);
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

    private static void ApplyDecorators(global::SadConsole.Console target, int x, int y, PlayCellVisual cell, bool entityHidden, CellHighlightPresentation? cellHighlight)
    {
        var decorators = new List<global::SadConsole.CellDecorator>();
        if (!entityHidden && cell.FacingGlyph is { } facingGlyph && cell.EntityGlyph is not null)
        {
            decorators.Add(new global::SadConsole.CellDecorator(Color.LightYellow, facingGlyph, cell.FacingMirror));
        }

        if (cellHighlight is not null)
        {
            decorators.Add(new global::SadConsole.CellDecorator(cellHighlight.Foreground, cellHighlight.Glyph, cellHighlight.Mirror));
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
    SadMirror HighlightMirror)
{
    public static PlayRenderedGridCell Clear { get; } = new(
        0,
        Color.Black,
        Color.Black,
        null,
        SadMirror.None,
        null,
        null,
        SadMirror.None);
}

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
        GridCoord? highlightCoord = null,
        CellHighlightPresentation? cellHighlight = null)
    {
        var gridBounds = PlayGridRenderer.ResolveGridBounds(bounds, model);
        if (_drawnGridBounds != gridBounds)
        {
            _drawnCells.Clear();
            _drawnGridBounds = gridBounds;
        }

        foreach (var stale in ResolveStaleDrawnCoordinatesForSparseModel(_drawnCells.Keys, gridBounds, model))
        {
            DrawCell(target, stale.X, stale.Y, PlayRenderedGridCell.Clear);
            _drawnCells.Remove(stale);
        }

        foreach (var cell in model.Cells)
        {
            var x = gridBounds.X + cell.X;
            var y = gridBounds.Y + cell.Y;
            var entityHidden = cell.EntityId is { } entityId && hiddenEntityIds?.Contains(entityId) == true;
            var highlight = highlightCoord == new GridCoord(cell.X, cell.Y) ? cellHighlight : null;
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

    internal static IReadOnlyList<(int X, int Y)> ResolveStaleDrawnCoordinatesForSparseModel(
        IEnumerable<(int X, int Y)> previouslyDrawnCoordinates,
        FrontendRect gridBounds,
        PlayGridViewModel model)
    {
        var currentCoordinates = model.Cells
            .Select(cell => (X: gridBounds.X + cell.X, Y: gridBounds.Y + cell.Y))
            .ToHashSet();

        return previouslyDrawnCoordinates
            .Where(coord => !currentCoordinates.Contains(coord))
            .OrderBy(coord => coord.Y)
            .ThenBy(coord => coord.X)
            .ToList();
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
