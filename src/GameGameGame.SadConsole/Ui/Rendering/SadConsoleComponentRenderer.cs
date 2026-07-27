using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Screens;
using GameGameGame.SadConsoleApp.Ui.Styling;
using GameGameGame.SadConsoleApp.Ui.Tiles;
using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace GameGameGame.SadConsoleApp.Ui.Rendering;

internal sealed class SadConsoleComponentRenderer
{
    private readonly Console host;
    private readonly SadConsoleTheme theme;
    private readonly SadConsoleDisplaySettings displaySettings;
    private readonly TilesetProfile tileset;
    private readonly TilesetTextRenderer textRenderer;
    private Console? _overlayLayer;
    private bool _overlayAttached;

    public SadConsoleComponentRenderer(Console host, SadConsoleTheme theme, SadConsoleDisplaySettings? displaySettings = null)
    {
        this.host = host;
        this.theme = theme;
        this.displaySettings = displaySettings ?? SadConsoleDisplaySettings.Default;
        tileset = TilesetProfileLoader.Load(ResolveAssetPath("Candii.tileset.json"));
        textRenderer = new TilesetTextRenderer(tileset);
    }

    public string DisplaySummary => displaySettings.Summary;

    public void ClearSurface() => ClearSurface(host);

    public void PrintClipped(int x, int y, int width, string text, Color color) =>
        PrintClipped(host, x, y, width, text, color);

    public void DrawComponent(IUiComponent component) => DrawComponent(host, component, localBounds: false);

    public void DrawConnectorFallback(ConnectorLineViewModel connector)
    {
        foreach (var segment in connector.FallbackTileSegments().OrderBy(segment => segment.Layer))
        {
            var color = ColorForPresentation(segment.Color);
            if (segment.StartCellX == segment.EndCellX)
            {
                var minY = Math.Min(segment.StartCellY, segment.EndCellY);
                var maxY = Math.Max(segment.StartCellY, segment.EndCellY);
                for (var y = minY; y <= maxY; y++)
                {
                    SetCell(host, segment.StartCellX, y, segment.Glyph, color, Color.Black);
                }
            }
            else if (segment.StartCellY == segment.EndCellY)
            {
                var minX = Math.Min(segment.StartCellX, segment.EndCellX);
                var maxX = Math.Max(segment.StartCellX, segment.EndCellX);
                for (var x = minX; x <= maxX; x++)
                {
                    SetCell(host, x, segment.StartCellY, segment.Glyph, color, Color.Black);
                }
            }
        }
    }

    public void ClearOverlay()
    {
        if (!_overlayAttached)
        {
            return;
        }

        host.Children.Remove(_overlayLayer!);
        _overlayAttached = false;
    }

    public void RenderOverlay(IUiComponent overlay)
    {
        var bounds = overlay.Bounds;
        if (_overlayLayer is null || _overlayLayer.Width != bounds.Width || _overlayLayer.Height != bounds.Height)
        {
            ClearOverlay();
            _overlayLayer = new Console(bounds.Width, bounds.Height);
        }

        _overlayLayer.Position = CenteredOverlayPosition(bounds.Width, bounds.Height);
        if (!_overlayAttached)
        {
            host.Children.Add(_overlayLayer);
            _overlayAttached = true;
        }

        ClearSurface(_overlayLayer);
        DrawComponent(_overlayLayer, overlay, localBounds: true);
        _overlayLayer.Surface.IsDirty = true;
    }

    private Point CenteredOverlayPosition(int width, int height) => new(
        Math.Max(0, (host.Width - width) / 2),
        Math.Max(0, (host.Height - height) / 2));

    private static string ResolveAssetPath(string fileName)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "assets", fileName);
        if (File.Exists(outputPath)) return outputPath;

        var workingPath = Path.Combine(Environment.CurrentDirectory, "assets", fileName);
        if (File.Exists(workingPath)) return workingPath;

        return outputPath;
    }

    public static Color ColorFromToken(string token) => ComponentGalleryConsole.ColorFromToken(token);

    private void DrawComponent(Console target, IUiComponent component, bool localBounds)
    {
        if (component is InventorySpaceComponent inventorySpace)
        {
            DrawInventorySpaceComponent(target, inventorySpace, localBounds);
            return;
        }

        if (component is InventoryGridComponent inventoryGrid)
        {
            DrawInventoryGridComponent(target, inventoryGrid, localBounds);
            return;
        }

        if (component is InventorySummaryComponent inventorySummary)
        {
            DrawInventorySummaryComponent(target, inventorySummary, localBounds);
            return;
        }

        if (component is TransferInventoryComparisonComponent transferInventory)
        {
            DrawTransferInventoryComparisonComponent(target, transferInventory, localBounds);
            return;
        }

        var border = ColorFromToken(component.State.BorderColor(theme));
        var bounds = localBounds ? new SadConsoleRect(0, 0, target.Width, target.Height) : component.Bounds;
        FillRect(target, bounds, Color.Black);
        DrawBox(target, bounds, border, tileset.Roles.PanelBorder);
        PrintClipped(target, bounds.Left + 2, bounds.Top, Math.Max(0, bounds.Width - 4), component.Title, ColorFromToken(theme.Panel.TitleText));

        var rows = component.RenderRows(theme).Skip(1).ToList();
        var maxRows = Math.Max(0, bounds.Height - 2);
        for (var index = 0; index < rows.Count && index < maxRows; index++)
        {
            var row = rows[index];
            var visibleRow = ComponentGalleryConsole.StripStyleTokens(row);
            PrintClipped(target, bounds.Left + 1, bounds.Top + 1 + index, Math.Max(0, bounds.Width - 2), visibleRow, ColorForRow(row, component.State));
            DrawColorSampleGlyph(target, bounds.Left + 1, bounds.Top + 1 + index, Math.Max(0, bounds.Width - 2), visibleRow, row);
        }
    }

    private void DrawTransferInventoryComparisonComponent(Console target, TransferInventoryComparisonComponent component, bool localBounds)
    {
        var border = ColorFromToken(component.State.BorderColor(theme));
        var bounds = localBounds ? new SadConsoleRect(0, 0, target.Width, target.Height) : component.Bounds;
        FillRect(target, bounds, Color.Black);
        DrawBox(target, bounds, border, tileset.Roles.PanelBorder);
        PrintClipped(target, bounds.Left + 2, bounds.Top, Math.Max(0, bounds.Width - 4), component.Title, ColorFromToken(theme.Panel.TitleText));

        var innerLeft = bounds.Left + 1;
        var innerTop = bounds.Top + 1;
        var innerWidth = Math.Max(0, bounds.Width - 2);
        if (innerWidth <= 0 || bounds.Height <= 3)
        {
            return;
        }

        PrintClipped(target, innerLeft, bounds.Bottom - 3, innerWidth, component.SelectedSummary, Color.Gold);
        PrintClipped(target, innerLeft, bounds.Bottom - 2, innerWidth, component.Controls, Color.Gray);

        var sideTop = innerTop;
        var sideHeight = Math.Max(1, bounds.Height - 5);
        var gap = 1;
        var sideWidth = Math.Max(8, (innerWidth - gap) / 2);
        DrawTransferInventorySide(target, component.ActorSide, SadConsoleRect.FromSize(innerLeft, sideTop, sideWidth, sideHeight));
        DrawTransferInventorySide(target, component.CounterpartySide, SadConsoleRect.FromSize(innerLeft + sideWidth + gap, sideTop, Math.Max(8, innerWidth - sideWidth - gap), sideHeight));
    }

    private void DrawTransferInventorySide(Console target, TransferInventorySideComponent side, SadConsoleRect bounds)
    {
        DrawBox(target, bounds, Color.DarkGray, tileset.Roles.PanelBorder);
        PrintClipped(target, bounds.Left + 1, bounds.Top, Math.Max(0, bounds.Width - 2), side.Title, Color.White);
        var y = bounds.Top + 1;
        foreach (var row in side.Rows)
        {
            if (y >= bounds.Bottom - 1) break;
            PrintClipped(target, bounds.Left + 1, y++, Math.Max(0, bounds.Width - 2), row, Color.LightGray);
        }

        if (side.GridWidth <= 0 || side.GridHeight <= 0)
        {
            PrintClipped(target, bounds.Left + 1, y, Math.Max(0, bounds.Width - 2), "no usable inventory", Color.DarkGray);
            return;
        }

        var cellWidth = 3;
        var gridPixelWidth = side.GridWidth * cellWidth;
        var gridLeft = Math.Max(bounds.Left + 4, bounds.Left + ((bounds.Width - gridPixelWidth) / 2));
        var gridTop = Math.Min(bounds.Bottom - 2, Math.Max(y + 1, bounds.Top + 4));
        var cells = side.Cells.ToDictionary(cell => cell.Coord);
        for (var row = 0; row < side.GridHeight && gridTop + row < bounds.Bottom - 1; row++)
        {
            PrintClipped(target, bounds.Left + 1, gridTop + row, 3, $"{row,2}:", Color.DarkGray);
            for (var column = 0; column < side.GridWidth; column++)
            {
                var x = gridLeft + column * cellWidth;
                if (x + 2 >= bounds.Left + bounds.Width - 1) break;

                var coord = new GameGameGame.Core.GridCoord(column, row);
                var cell = cells.GetValueOrDefault(coord);
                var foreground = cell?.Color is { } color ? ColorFromToken(color.ToString()) : Color.DarkGray;
                var background = side.SelectedCoord == coord
                    ? Color.DarkGoldenrod
                    : side.ValidSelectionCoords.Contains(coord)
                        ? Color.DarkGreen
                        : Color.Black;
                var glyph = cell?.Glyph ?? '.';
                SetCell(target, x, gridTop + row, tileset.Blank, foreground, background);
                SetCell(target, x + 1, gridTop + row, glyph, side.SelectedCoord == coord ? Color.Yellow : foreground, background);
                SetCell(target, x + 2, gridTop + row, tileset.Blank, foreground, background);
            }
        }
    }

    private void DrawInventorySpaceComponent(Console target, InventorySpaceComponent component, bool localBounds)
    {
        var view = component.View;
        var options = component.Options;
        var border = options.ShowFrame
            ? ColorForPresentation(view.Frame.Color)
            : ColorFromToken(component.State.BorderColor(theme));
        var bounds = localBounds ? new SadConsoleRect(0, 0, target.Width, target.Height) : component.Bounds;
        FillRect(target, bounds, Color.Black);
        if (options.ShowFrame)
        {
            DrawBox(target, bounds, border, tileset.Roles.PanelBorder);
        }

        var innerLeft = bounds.Left + (options.ShowFrame ? 1 : 0);
        var innerTop = bounds.Top + (options.ShowFrame ? 1 : 0);
        var innerRight = bounds.Left + bounds.Width - (options.ShowFrame ? 1 : 0);
        var innerBottom = bounds.Bottom - (options.ShowFrame ? 1 : 0);
        var yCursor = innerTop;

        if (options.ShowTitle)
        {
            PrintClipped(target, innerLeft + (options.ShowFrame ? 1 : 0), bounds.Top, Math.Max(0, bounds.Width - 4), component.Title, ColorFromToken(theme.Panel.TitleText));
            if (!options.ShowFrame)
            {
                yCursor++;
            }
        }

        if (options.ShowDebugRows)
        {
            var rows = component.BodyRows.ToList();
            var maxRows = Math.Min(rows.Count, Math.Max(0, innerBottom - yCursor - 1));
            for (var index = 0; index < maxRows; index++)
            {
                PrintClipped(target, innerLeft, yCursor++, Math.Max(0, innerRight - innerLeft), rows[index], ColorForRow(rows[index], component.State));
            }

            if (yCursor < innerBottom)
            {
                PrintClipped(target, innerLeft, yCursor++, Math.Max(0, innerRight - innerLeft), "drawn grid:", Color.DarkGray);
            }
        }

        var columnLabelY = yCursor;
        if (options.ShowColumnLabels)
        {
            yCursor++;
        }

        var gridTop = yCursor;
        var gridLeft = innerLeft + (options.ShowRowLabels ? 4 : 0);
        var gridAreaBottom = innerBottom;
        if (gridTop >= gridAreaBottom)
        {
            return;
        }

        var gridPixelWidth = view.Viewport.Width * view.CellMetrics.Width + Math.Max(0, view.Viewport.Width - 1) * view.CellMetrics.Gap;
        if (gridLeft + gridPixelWidth >= innerRight)
        {
            gridLeft = innerLeft + (options.ShowRowLabels ? 4 : 0);
        }

        if (options.ShowColumnLabels)
        {
            DrawInventorySpaceColumnLabels(target, view, gridLeft, columnLabelY, bounds);
        }

        var entitiesByCoord = view.Entities
            .Where(entity => view.IsVisible(entity.Coord))
            .GroupBy(entity => entity.Coord)
            .ToDictionary(group => group.Key, group => group.First());
        var decoratorsByCoord = view.Decorators
            .Where(decorator => view.IsVisible(decorator.Coord))
            .GroupBy(decorator => decorator.Coord)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(decorator => decorator.Priority).ToList());

        foreach (var coord in view.VisibleCoords())
        {
            var relative = view.CellBounds(coord);
            var cellRect = SadConsoleRect.FromSize(gridLeft + relative.Left, gridTop + relative.Top, relative.Width, relative.Height);
            if (cellRect.Left >= bounds.Left + bounds.Width - 1 || cellRect.Top >= gridAreaBottom)
            {
                continue;
            }

            var decorators = decoratorsByCoord.GetValueOrDefault(coord) ?? [];
            var backdropSecondary = ColorForLayerBackground(view.Backdrop.Tile) ?? Color.Black;
            var cellSecondary = InventorySpaceHighlightBackground(decorators) ?? backdropSecondary;
            if (options.ShowRowLabels && coord.X == view.Viewport.Origin.X)
            {
                PrintClipped(target, innerLeft, cellRect.Top, 3, $"{coord.Y,2}:", Color.DarkGray);
            }

            DrawInventorySpaceBackdrop(target, cellRect, view.Backdrop.Tile, cellSecondary, bounds, gridAreaBottom);

            if (entitiesByCoord.TryGetValue(coord, out var entity))
            {
                DrawInventorySpaceEntity(target, cellRect, entity, decorators, bounds, gridAreaBottom, cellSecondary);
            }
            else
            {
                DrawInventorySpaceDecorators(target, cellRect, decorators, bounds, gridAreaBottom, entityPresent: false);
            }
        }
    }

    private static Color? InventorySpaceHighlightBackground(IReadOnlyList<InventorySpaceDecorator> decorators)
    {
        if (decorators.Any(decorator => decorator.Role == InventorySpaceDecoratorRole.Focused))
        {
            return Color.DarkCyan;
        }

        if (decorators.Any(decorator => decorator.Role == InventorySpaceDecoratorRole.Selected))
        {
            return Color.DarkGoldenrod;
        }

        return null;
    }

    private void DrawInventorySpaceColumnLabels(Console target, InventorySpaceViewModel view, int gridLeft, int y, SadConsoleRect componentBounds)
    {
        if (y >= componentBounds.Bottom - 1)
        {
            return;
        }

        for (var x = view.Viewport.Origin.X; x < view.Viewport.Origin.X + view.Viewport.Width; x++)
        {
            if (x < 0 || x >= view.Width) continue;

            var coord = new GameGameGame.Core.GridCoord(x, view.Viewport.Origin.Y);
            var relative = view.CellBounds(coord);
            var labelX = gridLeft + relative.Left + Math.Max(0, view.CellMetrics.Width / 2);
            if (labelX >= componentBounds.Left + componentBounds.Width - 1) break;

            SetCell(target, labelX, y, ColumnLabelGlyph(x), Color.DarkGray, Color.Black);
        }
    }

    private void DrawInventorySpaceBackdrop(
        Console target,
        SadConsoleRect cellRect,
        InventorySpaceVisualLayer backdropLayer,
        Color backdropSecondary,
        SadConsoleRect componentBounds,
        int gridAreaBottom)
    {
        for (var y = cellRect.Top; y < cellRect.Bottom && y < gridAreaBottom; y++)
        {
            for (var x = cellRect.Left; x < cellRect.Left + cellRect.Width && x < componentBounds.Left + componentBounds.Width - 1; x++)
            {
                SetCell(target, x, y, backdropLayer.Glyph, ColorForLayerForeground(backdropLayer), backdropSecondary);
            }
        }
    }

    private void DrawInventorySpaceEntity(
        Console target,
        SadConsoleRect cellRect,
        InventorySpaceEntityVisual entity,
        IReadOnlyList<InventorySpaceDecorator> decorators,
        SadConsoleRect componentBounds,
        int gridAreaBottom,
        Color backdropSecondary)
    {
        if (cellRect.Top >= gridAreaBottom)
        {
            return;
        }

        var entityX = cellRect.Left + Math.Max(0, cellRect.Width / 2);
        var entityY = cellRect.Top + Math.Max(0, cellRect.Height / 2);
        if (entityX < componentBounds.Left + componentBounds.Width - 1 && entityY < gridAreaBottom)
        {
            SetCell(
                target,
                entityX,
                entityY,
                entity.Primary.Glyph,
                ColorForLayerForeground(entity.Primary),
                ColorForLayerBackground(entity.Primary) ?? backdropSecondary);
        }

        DrawInventorySpaceDecorators(target, cellRect, decorators, componentBounds, gridAreaBottom, entityPresent: true);
    }

    private void DrawInventorySpaceDecorators(
        Console target,
        SadConsoleRect cellRect,
        IReadOnlyList<InventorySpaceDecorator> decorators,
        SadConsoleRect componentBounds,
        int gridAreaBottom,
        bool entityPresent)
    {
        if (decorators.Count == 0 || cellRect.Width < 3 || cellRect.Top >= gridAreaBottom)
        {
            return;
        }

        var leftDecorator = decorators.FirstOrDefault(decorator => decorator.Role == InventorySpaceDecoratorRole.Controlled);
        if (leftDecorator is not null)
        {
            SetCell(target, cellRect.Left, cellRect.Top, leftDecorator.Style.Glyph, ColorForLayerForeground(leftDecorator.Style), ColorForLayerBackground(leftDecorator.Style) ?? Color.Black);
        }

        var rightDecorator = decorators.FirstOrDefault(decorator => decorator.Role != InventorySpaceDecoratorRole.Controlled);
        if (rightDecorator is not null)
        {
            var x = cellRect.Left + cellRect.Width - 1;
            if (x < componentBounds.Left + componentBounds.Width - 1)
            {
                SetCell(target, x, cellRect.Top, rightDecorator.Style.Glyph, ColorForLayerForeground(rightDecorator.Style), ColorForLayerBackground(rightDecorator.Style) ?? Color.Black);
            }
        }

        if (!entityPresent && leftDecorator is null && rightDecorator is null && decorators[0] is { } decorator)
        {
            SetCell(target, cellRect.Left + cellRect.Width / 2, cellRect.Top, decorator.Style.Glyph, ColorForLayerForeground(decorator.Style), ColorForLayerBackground(decorator.Style) ?? Color.Black);
        }
    }

    private static int ColumnLabelGlyph(int column)
    {
        var normalized = ((column % 26) + 26) % 26;
        return 'A' + normalized;
    }

    private void DrawInventorySummaryComponent(Console target, InventorySummaryComponent component, bool localBounds)
    {
        var border = ColorFromToken(component.State.BorderColor(theme));
        var bounds = localBounds ? new SadConsoleRect(0, 0, target.Width, target.Height) : component.Bounds;
        FillRect(target, bounds, Color.Black);
        DrawBox(target, bounds, border, tileset.Roles.PanelBorder);
        PrintClipped(target, bounds.Left + 2, bounds.Top, Math.Max(0, bounds.Width - 4), component.Title, ColorFromToken(theme.Panel.TitleText));

        var leftWidth = Math.Min(42, Math.Max(0, bounds.Width - 4));
        for (var index = 0; index < component.Rows.Count && index < Math.Max(0, bounds.Height - 2); index++)
        {
            var row = component.Rows[index];
            PrintClipped(target, bounds.Left + 1, bounds.Top + 1 + index, leftWidth, row, ColorForRow(row, component.State));
        }

        var previewLeft = bounds.Left + leftWidth + 3;
        var previewTop = bounds.Top + 1;
        var previewRight = bounds.Left + bounds.Width - 2;
        var previewBottom = bounds.Bottom - 2;
        if (previewLeft > previewRight || previewTop > previewBottom)
        {
            return;
        }

        PrintClipped(target, previewLeft, previewTop, previewRight - previewLeft + 1, "read-only inventory preview", Color.DarkGray);
        if (component.GridWidth <= 0 || component.GridHeight <= 0)
        {
            PrintClipped(target, previewLeft, previewTop + 1, previewRight - previewLeft + 1, "no usable grid", Color.DarkGray);
            return;
        }

        var gridLeft = previewLeft + 3;
        var gridTop = previewTop + 2;
        var cellWidth = 3;
        var cells = component.Cells.ToDictionary(cell => cell.Coord);
        for (var y = 0; y < component.GridHeight && gridTop + y <= previewBottom; y++)
        {
            PrintClipped(target, previewLeft, gridTop + y, 3, $"{y,2}:", Color.DarkGray);
            for (var x = 0; x < component.GridWidth; x++)
            {
                var xPos = gridLeft + x * cellWidth;
                if (xPos + 2 > previewRight) break;

                var coord = new GameGameGame.Core.GridCoord(x, y);
                var cell = cells.GetValueOrDefault(coord);
                var glyph = cell?.Glyph ?? '.';
                var foreground = cell?.Color is { } color ? ColorFromToken(color.ToString()) : Color.DarkGray;
                SetCell(target, xPos, gridTop + y, tileset.Blank, foreground, Color.Black);
                SetCell(target, xPos + 1, gridTop + y, glyph, foreground, Color.Black);
                SetCell(target, xPos + 2, gridTop + y, tileset.Blank, foreground, Color.Black);
            }
        }
    }

    private void DrawInventoryGridComponent(Console target, InventoryGridComponent component, bool localBounds)
    {
        var border = ColorFromToken(component.State.BorderColor(theme));
        var bounds = localBounds ? new SadConsoleRect(0, 0, target.Width, target.Height) : component.Bounds;
        FillRect(target, bounds, Color.Black);
        DrawBox(target, bounds, border, tileset.Roles.PanelBorder);
        PrintClipped(target, bounds.Left + 2, bounds.Top, Math.Max(0, bounds.Width - 4), component.Title, ColorFromToken(theme.Panel.TitleText));

        var rows = component.RenderRows(theme).Skip(1).Where(row => !row.Contains("grid cells are rendered", StringComparison.Ordinal)).ToList();
        for (var index = 0; index < rows.Count && index < Math.Max(0, bounds.Height - 2); index++)
        {
            PrintClipped(target, bounds.Left + 1, bounds.Top + 1 + index, Math.Max(0, bounds.Width - 2), rows[index], Color.White);
        }

        if (component.GridWidth <= 0 || component.GridHeight <= 0)
        {
            return;
        }

        var gridLeft = bounds.Left + 4;
        var gridTop = bounds.Top + 4;
        var cellWidth = 3;
        var cells = component.Cells.ToDictionary(cell => cell.Coord);
        for (var y = 0; y < component.GridHeight && gridTop + y < bounds.Bottom - 1; y++)
        {
            PrintClipped(target, bounds.Left + 1, gridTop + y, 3, $"{y,2}:", Color.DarkGray);
            for (var x = 0; x < component.GridWidth; x++)
            {
                var coord = new GameGameGame.Core.GridCoord(x, y);
                var xPos = gridLeft + x * cellWidth;
                if (xPos >= bounds.Left + bounds.Width - 1) break;

                var cell = cells.GetValueOrDefault(coord);
                var glyph = cell?.Glyph ?? '.';
                var foreground = cell?.Color is { } color ? ColorFromToken(color.ToString()) : Color.DarkGray;
                var isCursor = coord == component.Cursor;
                SetCell(target, xPos, gridTop + y, tileset.Blank, foreground, isCursor ? Color.DarkBlue : Color.Black);
                SetCell(target, xPos + 1, gridTop + y, glyph, isCursor ? Color.Yellow : foreground, isCursor ? Color.DarkBlue : Color.Black);
                SetCell(target, xPos + 2, gridTop + y, tileset.Blank, foreground, isCursor ? Color.DarkBlue : Color.Black);
            }
        }
    }

    private void DrawBox(Console target, SadConsoleRect rect, Color color, TileBorderGlyphSet glyphs)
    {
        var right = rect.Left + rect.Width - 1;
        var bottom = rect.Bottom - 1;
        for (var x = rect.Left; x <= right; x++)
        {
            SetCell(target, x, rect.Top, x == rect.Left ? glyphs.TopLeft : x == right ? glyphs.TopRight : glyphs.Horizontal, color, Color.Black);
            SetCell(target, x, bottom, x == rect.Left ? glyphs.BottomLeft : x == right ? glyphs.BottomRight : glyphs.Horizontal, color, Color.Black);
        }

        for (var y = rect.Top + 1; y < bottom; y++)
        {
            SetCell(target, rect.Left, y, glyphs.Vertical, color, Color.Black);
            SetCell(target, right, y, glyphs.Vertical, color, Color.Black);
        }
    }

    private void FillRect(Console target, SadConsoleRect rect, Color background)
    {
        var right = Math.Min(target.Width, rect.Left + rect.Width);
        var bottom = Math.Min(target.Height, rect.Bottom);
        for (var y = Math.Max(0, rect.Top); y < bottom; y++)
        {
            for (var x = Math.Max(0, rect.Left); x < right; x++)
            {
                SetCell(target, x, y, tileset.Blank, Color.White, background);
            }
        }
    }

    private Color ColorForRow(string row, UiComponentState componentState)
    {
        if (row.Contains(theme.List.FocusedRowText, StringComparison.Ordinal)) return ColorFromToken(theme.List.FocusedRowText);
        foreach (var token in SadConsoleKnownColorTokens.Values)
        {
            if (row.Contains($"({token})", StringComparison.OrdinalIgnoreCase)) return ColorFromToken(token);
        }
        if (row.Contains(theme.List.SelectedRowText, StringComparison.Ordinal)) return ColorFromToken(theme.List.SelectedRowText);
        if (row.Contains(theme.List.EmptyText, StringComparison.Ordinal)) return ColorFromToken(theme.List.EmptyText);
        return componentState == UiComponentState.Focused ? Color.White : Color.LightGray;
    }

    private void PrintClipped(Console target, int x, int y, int width, string text, Color color)
    {
        if (y < 0 || y >= target.Height || x >= target.Width || width <= 0) return;
        var clipped = text.Length <= width ? text : text[..Math.Max(0, width - 1)];
        textRenderer.Print(target, x, y, clipped.PadRight(Math.Max(0, width)), color, Color.Black);
    }

    private static Color ColorForPresentation(PresentationColor color) => color switch
    {
        PresentationColor.Gray => Color.Gray,
        PresentationColor.White => Color.White,
        PresentationColor.Yellow => Color.Yellow,
        PresentationColor.Cyan => Color.Cyan,
        PresentationColor.Green => Color.Green,
        PresentationColor.DarkGreen => Color.DarkGreen,
        PresentationColor.Earth => Color.SaddleBrown,
        PresentationColor.Default => Color.White,
        _ => Color.White
    };

    private static Color ColorForLayerForeground(InventorySpaceVisualLayer layer) =>
        layer.ForegroundRgb is { } rgb ? ColorFromRgb(rgb) : ColorForPresentation(layer.Foreground);

    private static Color? ColorForLayerBackground(InventorySpaceVisualLayer layer) =>
        layer.BackgroundRgb is { } rgb
            ? ColorFromRgb(rgb)
            : layer.Background is { } background
                ? ColorForPresentation(background)
                : null;

    private static Color ColorFromRgb(int rgb) => new((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF), byte.MaxValue);

    private void ClearSurface(Console target)
    {
        textRenderer.Clear(target, Color.White, Color.Black);
    }

    private void DrawColorSampleGlyph(Console target, int x, int y, int width, string visibleRow, string styledRow)
    {
        if (width <= 0 || ComponentGalleryConsole.SampleColorTokenForRow(styledRow) is not { } token) return;

        var sampleIndex = visibleRow.IndexOf('■');
        if (sampleIndex < 0 || sampleIndex >= width) return;

        SetCell(target, x + sampleIndex, y, tileset.Roles.ColorSample, ColorFromToken(token), Color.Black);
    }

    private static void SetCell(Console target, int x, int y, int glyph, Color foreground, Color background)
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
