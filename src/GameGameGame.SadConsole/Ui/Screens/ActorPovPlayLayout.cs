namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed record ActorPovPlayLayout(
    SadConsoleRect DrawableBounds,
    SadConsoleRect WorldRegion,
    SadConsoleRect InventoryChainRegion,
    SadConsoleRect ParentChainRegion,
    SadConsoleRect CurrentPovRegion,
    SadConsoleRect InspectionChainRegion,
    int HorizontalSeparatorY,
    int? ParentCurrentSeparatorX,
    int? CurrentInspectionSeparatorX)
{
    public const int HorizontalLineGlyph = 158;
    public const int VerticalLineGlyph = 141;
    public const int HorizontalWithVerticalOffshootGlyph = 155;

    public static ActorPovPlayLayout Resolve(SadConsoleRect drawableBounds)
    {
        var inventoryHeight = Math.Max(1, drawableBounds.Height / 3);
        var separatorRows = drawableBounds.Height >= 3 ? 1 : 0;
        var worldHeight = Math.Max(1, drawableBounds.Height - inventoryHeight - separatorRows);
        inventoryHeight = Math.Max(0, drawableBounds.Height - worldHeight - separatorRows);
        var horizontalSeparatorY = drawableBounds.Top + worldHeight;

        var worldRegion = SadConsoleRect.FromSize(
            drawableBounds.Left,
            drawableBounds.Top,
            drawableBounds.Width,
            worldHeight);
        var inventoryRegion = SadConsoleRect.FromSize(
            drawableBounds.Left,
            drawableBounds.Top + worldHeight + separatorRows,
            drawableBounds.Width,
            inventoryHeight);

        var squareSize = Math.Max(1, Math.Min(worldRegion.Width, worldRegion.Height));
        var squareLeft = worldRegion.Left + Math.Max(0, (worldRegion.Width - squareSize) / 2);
        var worldRightExclusive = worldRegion.Left + worldRegion.Width;
        var currentRightExclusive = squareLeft + squareSize;
        int? parentSeparatorX = squareLeft > worldRegion.Left ? squareLeft - 1 : null;
        int? inspectionSeparatorX = currentRightExclusive < worldRightExclusive ? currentRightExclusive : null;
        var currentPovRegion = SadConsoleRect.FromSize(
            squareLeft,
            worldRegion.Top,
            squareSize,
            squareSize);

        var parentRegion = SadConsoleRect.FromSize(
            worldRegion.Left,
            worldRegion.Top,
            Math.Max(0, (parentSeparatorX ?? worldRegion.Left) - worldRegion.Left),
            worldRegion.Height);
        var inspectionLeft = inspectionSeparatorX is { } rightSeparator ? rightSeparator + 1 : currentRightExclusive;
        var inspectionRegion = SadConsoleRect.FromSize(
            inspectionLeft,
            worldRegion.Top,
            Math.Max(0, worldRightExclusive - inspectionLeft),
            worldRegion.Height);

        return new ActorPovPlayLayout(
            drawableBounds,
            worldRegion,
            inventoryRegion,
            parentRegion,
            currentPovRegion,
            inspectionRegion,
            horizontalSeparatorY,
            parentSeparatorX,
            inspectionSeparatorX);
    }

    public IReadOnlyList<ActorPovPlayChromeCell> ChromeCells()
    {
        var cells = new List<ActorPovPlayChromeCell>();
        for (var x = DrawableBounds.Left; x < DrawableBounds.Left + DrawableBounds.Width; x++)
        {
            cells.Add(new ActorPovPlayChromeCell(x, HorizontalSeparatorY, HorizontalLineGlyph));
        }

        AddVerticalSeparator(cells, ParentCurrentSeparatorX);
        AddVerticalSeparator(cells, CurrentInspectionSeparatorX);
        return cells;
    }

    private void AddVerticalSeparator(List<ActorPovPlayChromeCell> cells, int? separatorX)
    {
        if (separatorX is not { } x)
        {
            return;
        }

        for (var y = WorldRegion.Top; y < WorldRegion.Top + WorldRegion.Height; y++)
        {
            cells.Add(new ActorPovPlayChromeCell(x, y, VerticalLineGlyph));
        }

        cells.Add(new ActorPovPlayChromeCell(x, HorizontalSeparatorY, HorizontalWithVerticalOffshootGlyph));
    }
}

internal sealed record ActorPovPlayChromeCell(int X, int Y, int Glyph);
