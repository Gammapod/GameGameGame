using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Styling;
using SadConsole;

namespace GameGameGame.SadConsoleApp.Ui.Components;

internal sealed record InventorySpaceViewModel(
    string Id,
    string Title,
    PlaneId PlaneId,
    int Width,
    int Height,
    InventorySpaceCellMetrics CellMetrics,
    InventorySpaceViewport Viewport,
    InventorySpaceBackdropLayer Backdrop,
    IReadOnlyList<InventorySpaceEntityVisual> Entities,
    IReadOnlyList<InventorySpaceDecorator> Decorators,
    InventorySpaceFrame Frame)
{
    public bool IsVisible(GridCoord coord) => Viewport.Contains(coord);

    public SadConsoleRect CellBounds(GridCoord coord)
    {
        if (!IsVisible(coord))
        {
            throw new ArgumentOutOfRangeException(nameof(coord), $"Coordinate {coord} is outside viewport {Viewport}.");
        }

        var localX = coord.X - Viewport.Origin.X;
        var localY = coord.Y - Viewport.Origin.Y;
        var stepX = CellMetrics.Width + CellMetrics.Gap;
        var stepY = CellMetrics.Height + CellMetrics.Gap;
        return SadConsoleRect.FromSize(
            localX * stepX,
            localY * stepY,
            CellMetrics.Width,
            CellMetrics.Height);
    }

    public IReadOnlyList<GridCoord> VisibleCoords()
    {
        var coords = new List<GridCoord>();
        for (var y = Viewport.Origin.Y; y < Viewport.Origin.Y + Viewport.Height; y++)
        {
            for (var x = Viewport.Origin.X; x < Viewport.Origin.X + Viewport.Width; x++)
            {
                if (x >= 0 && y >= 0 && x < Width && y < Height)
                {
                    coords.Add(new GridCoord(x, y));
                }
            }
        }

        return coords;
    }

    public static InventorySpaceViewModel FromProjection(
        string id,
        EntityPanelProjection projection,
        EntityId? controlledEntityId = null,
        GridCoord? selectedCoord = null,
        GridCoord? focusedCoord = null,
        InventorySpaceCellMetrics? cellMetrics = null,
        InventorySpaceViewport? viewport = null,
        bool showFrame = true,
        IReadOnlyDictionary<EntityId, Direction>? facingByEntityId = null)
    {
        if (projection.InventoryGrid is not { } grid)
        {
            throw new ArgumentException("Projection must include an inventory grid.", nameof(projection));
        }

        var metrics = cellMetrics ?? InventorySpaceCellMetrics.Default;
        var visibleViewport = viewport ?? InventorySpaceViewport.Full(grid.Width, grid.Height);
        var namesByEntityId = projection.Contents.ToDictionary(row => row.EntityId, row => row.EntityName);
        var facingFacts = facingByEntityId is null
            ? new Dictionary<EntityId, Direction>()
            : new Dictionary<EntityId, Direction>(facingByEntityId);
        if (projection.ActionState.Facing is { } projectionFacing)
        {
            facingFacts[projection.EntityId] = projectionFacing;
        }
        var entities = grid.Cells
            .Where(cell => cell.EntityId is not null)
            .Select(cell => new InventorySpaceEntityVisual(
                cell.Coord,
                cell.EntityId!.Value,
                new InventorySpaceVisualLayer(cell.Glyph, cell.Color),
                Accent: null,
                InventorySpaceVisualPlacement.Default,
                namesByEntityId.GetValueOrDefault(cell.EntityId.Value)))
            .ToList();
        var decorators = new List<InventorySpaceDecorator>();

        if (controlledEntityId is { } controlled)
        {
            decorators.AddRange(grid.Cells
                .Where(cell => cell.EntityId == controlled)
                .Select(cell => new InventorySpaceDecorator(
                    cell.Coord,
                    InventorySpaceDecoratorRole.Controlled,
                    EntityId: controlled,
                    Style: new InventorySpaceVisualLayer('*', PresentationColor.Yellow),
                    Priority: 100)));
        }

        if (selectedCoord is { } selected)
        {
            decorators.Add(new InventorySpaceDecorator(
                selected,
                InventorySpaceDecoratorRole.Selected,
                EntityId: null,
                Style: new InventorySpaceVisualLayer('+', PresentationColor.Yellow),
                Priority: 80));
        }

        if (focusedCoord is { } focused)
        {
            decorators.Add(new InventorySpaceDecorator(
                focused,
                InventorySpaceDecoratorRole.Focused,
                EntityId: null,
                Style: new InventorySpaceVisualLayer('>', PresentationColor.Cyan),
                Priority: 90));
        }

        decorators.AddRange(grid.Cells
            .Where(cell => cell.EntityId is { } entityId && facingFacts.ContainsKey(entityId))
            .Select(cell => FacingDecorator(cell.Coord, cell.EntityId!.Value, facingFacts[cell.EntityId.Value])));

        return new InventorySpaceViewModel(
            id,
            projection.Name,
            grid.PlaneId,
            grid.Width,
            grid.Height,
            metrics,
            visibleViewport,
            new InventorySpaceBackdropLayer(new InventorySpaceVisualLayer(223, PresentationColor.Gray, ForegroundRgb: 0x808080, BackgroundRgb: 0x404040)),
            entities,
            decorators,
            new InventorySpaceFrame(showFrame, projection.Name, PresentationColor.Yellow));
    }

    public static InventorySpaceDecorator FacingDecorator(GridCoord coord, EntityId entityId, Direction direction)
    {
        var (glyph, mirror) = FacingGlyph(direction);
        return new InventorySpaceDecorator(
            coord,
            InventorySpaceDecoratorRole.Facing,
            entityId,
            new InventorySpaceVisualLayer(glyph, PresentationColor.Yellow, Mirror: mirror),
            Priority: 110);
    }

    public static (int Glyph, Mirror Mirror) FacingGlyph(Direction direction) => direction switch
    {
        Direction.North => (252, Mirror.None),
        Direction.South => (252, Mirror.Vertical),
        Direction.East => (253, Mirror.None),
        Direction.West => (253, Mirror.Horizontal),
        Direction.NorthWest => (251, Mirror.None),
        Direction.NorthEast => (251, Mirror.Horizontal),
        Direction.SouthWest => (251, Mirror.Vertical),
        Direction.SouthEast => (251, Mirror.Horizontal | Mirror.Vertical),
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown facing direction.")
    };
}

internal sealed record InventorySpaceCellMetrics(int Width, int Height, int Gap)
{
    public static InventorySpaceCellMetrics Default { get; } = new(1, 1, 0);
}

internal enum InventorySpaceZoom
{
    Micro4,
    Small8,
    Normal16,
    Large24,
    Huge32
}

internal enum InventorySpaceRelationshipTier
{
    CurrentLocation,
    PlayerInventory,
    ImmediateParent,
    Grandparent,
    GreatGrandparentOrBeyond
}

internal sealed record InventorySpaceDisplayProfile(
    InventorySpaceRelationshipTier RelationshipTier,
    InventorySpaceZoom SpaceZoom,
    int CellPixelSize,
    int CellGapPixels,
    bool UsesCandiiFont,
    bool ShowFacingDecorators)
{
    public static InventorySpaceDisplayProfile ForRelationshipTier(InventorySpaceRelationshipTier tier) => tier switch
    {
        InventorySpaceRelationshipTier.CurrentLocation => new(tier, InventorySpaceZoom.Huge32, 32, 0, UsesCandiiFont: true, ShowFacingDecorators: true),
        InventorySpaceRelationshipTier.PlayerInventory => new(tier, InventorySpaceZoom.Large24, 24, 1, UsesCandiiFont: true, ShowFacingDecorators: true),
        InventorySpaceRelationshipTier.ImmediateParent => new(tier, InventorySpaceZoom.Normal16, 16, 0, UsesCandiiFont: true, ShowFacingDecorators: true),
        InventorySpaceRelationshipTier.Grandparent => new(tier, InventorySpaceZoom.Small8, 8, 0, UsesCandiiFont: true, ShowFacingDecorators: true),
        InventorySpaceRelationshipTier.GreatGrandparentOrBeyond => new(tier, InventorySpaceZoom.Micro4, 4, 0, UsesCandiiFont: false, ShowFacingDecorators: true),
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown inventory-space relationship tier.")
    };

    public int? CandiiScale => UsesCandiiFont ? CellPixelSize / 8 : null;

    public bool CanRenderGlyphFacingDecorators => ShowFacingDecorators && UsesCandiiFont;

    public int RequiredPixelWidth(int viewportWidth) =>
        Math.Max(0, viewportWidth) * CellPixelSize + Math.Max(0, viewportWidth - 1) * CellGapPixels;

    public int RequiredPixelHeight(int viewportHeight) =>
        Math.Max(0, viewportHeight) * CellPixelSize + Math.Max(0, viewportHeight - 1) * CellGapPixels;
}

internal sealed record InventorySpaceViewport(GridCoord Origin, int Width, int Height)
{
    public static InventorySpaceViewport Full(int width, int height) => new(new GridCoord(0, 0), width, height);

    public bool Contains(GridCoord coord) =>
        coord.X >= Origin.X
        && coord.Y >= Origin.Y
        && coord.X < Origin.X + Width
        && coord.Y < Origin.Y + Height;
}

internal sealed record InventorySpaceBackdropLayer(InventorySpaceVisualLayer Tile);

internal sealed record InventorySpaceEntityVisual(
    GridCoord Coord,
    EntityId EntityId,
    InventorySpaceVisualLayer Primary,
    InventorySpaceVisualLayer? Accent,
    InventorySpaceVisualPlacement Placement,
    string? DisplayName = null);

internal sealed record InventorySpaceVisualLayer(
    int Glyph,
    PresentationColor Foreground,
    PresentationColor? Background = null,
    int? ForegroundRgb = null,
    int? BackgroundRgb = null,
    Mirror Mirror = Mirror.None);

internal sealed record InventorySpaceVisualPlacement(
    InventorySpaceScaleMode ScaleMode,
    InventorySpaceAnchor Anchor,
    int OffsetX = 0,
    int OffsetY = 0)
{
    public static InventorySpaceVisualPlacement Default { get; } = new(InventorySpaceScaleMode.Native, InventorySpaceAnchor.Center);
}

internal enum InventorySpaceScaleMode
{
    Native,
    FitCell,
    FillCell,
    Centered
}

internal enum InventorySpaceAnchor
{
    Center,
    TopLeft,
    BottomCenter
}

internal sealed record InventorySpaceDecorator(
    GridCoord Coord,
    InventorySpaceDecoratorRole Role,
    EntityId? EntityId,
    InventorySpaceVisualLayer Style,
    int Priority = 0);

internal enum InventorySpaceDecoratorRole
{
    Selected,
    Focused,
    Controlled,
    Warning,
    Error,
    ValidTarget,
    BlockedTarget,
    Hover,
    Facing,
    Target,
    NextAction
}

internal sealed record InventorySpaceFrame(bool Visible, string? Title, PresentationColor Color);

internal sealed record InventorySpaceRenderOptions(
    bool ShowFrame,
    bool ShowTitle,
    bool ShowRowLabels,
    bool ShowColumnLabels,
    bool ShowDebugRows)
{
    public static InventorySpaceRenderOptions Bare { get; } = new(
        ShowFrame: false,
        ShowTitle: false,
        ShowRowLabels: false,
        ShowColumnLabels: false,
        ShowDebugRows: false);

    public static InventorySpaceRenderOptions Labeled { get; } = new(
        ShowFrame: false,
        ShowTitle: false,
        ShowRowLabels: true,
        ShowColumnLabels: true,
        ShowDebugRows: false);

    public static InventorySpaceRenderOptions FramedDebug { get; } = new(
        ShowFrame: true,
        ShowTitle: true,
        ShowRowLabels: true,
        ShowColumnLabels: true,
        ShowDebugRows: true);
}

internal sealed class InventorySpaceComponent : IUiComponent
{
    public InventorySpaceComponent(
        string id,
        string title,
        SadConsoleRect bounds,
        InventorySpaceViewModel view,
        IReadOnlyList<string>? bodyRows = null,
        UiComponentState state = UiComponentState.Unselected,
        InventorySpaceRenderOptions? options = null,
        InventorySpaceDisplayProfile? displayProfile = null)
    {
        Id = id;
        Title = title;
        Bounds = bounds;
        View = view;
        BodyRows = bodyRows ?? [];
        State = state;
        Options = options ?? InventorySpaceRenderOptions.FramedDebug;
        DisplayProfile = displayProfile;
    }

    public string Id { get; }
    public string Title { get; }
    public SadConsoleRect Bounds { get; }
    public UiComponentState State { get; }
    public InventorySpaceViewModel View { get; }
    public IReadOnlyList<string> BodyRows { get; }
    public InventorySpaceRenderOptions Options { get; }
    public InventorySpaceDisplayProfile? DisplayProfile { get; }
    public int RequiredHeight => FrameRows + TitleRows + DebugRows + ColumnLabelRows + View.Viewport.Height;
    public int RequiredWidth => FrameColumns + RowLabelColumns + GridWidth;

    private int FrameRows => Options.ShowFrame ? 2 : 0;
    private int FrameColumns => Options.ShowFrame ? 2 : 0;
    private int TitleRows => Options.ShowTitle && !Options.ShowFrame ? 1 : 0;
    private int DebugRows => Options.ShowDebugRows ? BodyRows.Count + 1 : 0;
    private int ColumnLabelRows => Options.ShowColumnLabels ? 1 : 0;
    private int RowLabelColumns => Options.ShowRowLabels ? 4 : 0;
    private int GridWidth => View.Viewport.Width * View.CellMetrics.Width + Math.Max(0, View.Viewport.Width - 1) * View.CellMetrics.Gap;

    public SadConsoleRect CellBounds(GridCoord coord)
    {
        var relative = View.CellBounds(coord);
        return SadConsoleRect.FromSize(
            Bounds.Left + FrameLeftColumns + RowLabelColumns + relative.Left,
            Bounds.Top + FrameTopRows + TitleRows + DebugRows + ColumnLabelRows + relative.Top,
            relative.Width,
            relative.Height);
    }

    private int FrameTopRows => Options.ShowFrame ? 1 : 0;
    private int FrameLeftColumns => Options.ShowFrame ? 1 : 0;

    public IReadOnlyList<string> RenderRows(SadConsoleTheme theme)
    {
        var rows = new List<string> { $"[{State.BorderColor(theme)}] {Title}" };
        if (Options.ShowDebugRows)
        {
            rows.AddRange(BodyRows);
            rows.Add($"inventory-space cells are rendered by the SadConsole renderer: {View.Width}x{View.Height} viewport {View.Viewport.Width}x{View.Viewport.Height}");
            rows.Add($"layers: backdrop + {View.Entities.Count} primary visual(s) + {View.Decorators.Count} decorator(s)");
        }

        return rows;
    }
}
