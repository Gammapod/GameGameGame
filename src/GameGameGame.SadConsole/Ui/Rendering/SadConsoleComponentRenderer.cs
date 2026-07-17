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
