using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp.Ui.Components;
using Microsoft.Xna.Framework.Graphics;
using SadConsole;
using SadConsole.DrawCalls;
using SadConsole.Host;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using XnaColor = Microsoft.Xna.Framework.Color;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace GameGameGame.SadConsoleApp.Ui.Rendering;

internal sealed class MixedScaleInventorySpaceRenderer
{
    private readonly Console _host;
    private readonly string _fontName;
    private readonly Dictionary<string, Console> _cellLayers = [];
    private readonly HashSet<string> _activeCellLayerKeys = [];
    private readonly List<MicroCellDraw> _microCells = [];
    private Texture2D? _pixel;

    public MixedScaleInventorySpaceRenderer(Console host, string fontName)
    {
        _host = host;
        _fontName = fontName;
    }

    public bool HasMicroCells => _microCells.Count > 0;

    public void BeginFrame()
    {
        _activeCellLayerKeys.Clear();
        _microCells.Clear();
    }

    public void EndFrame()
    {
        var inactiveKeys = _cellLayers.Keys
            .Where(key => !_activeCellLayerKeys.Contains(key))
            .ToList();
        foreach (var key in inactiveKeys)
        {
            _host.Children.Remove(_cellLayers[key]);
            _cellLayers.Remove(key);
        }
    }

    public void Clear()
    {
        foreach (var layer in _cellLayers.Values)
        {
            _host.Children.Remove(layer);
        }

        _cellLayers.Clear();
        _activeCellLayerKeys.Clear();
        _microCells.Clear();
    }

    public bool Draw(InventorySpaceViewModel view, InventorySpaceDisplayProfile profile, int leftPixels, int topPixels, IReadOnlyList<PixelRect>? occlusionRects = null)
    {
        var geometry = InventorySpacePresentationGeometry.FromComponent(
            new InventorySpaceComponent(
                $"mixed-scale-direct-draw.{view.Id}",
                view.Title,
                SadConsoleRect.FromSize(leftPixels, topPixels, 0, 0),
                view,
                options: InventorySpaceRenderOptions.Bare,
                displayProfile: profile),
            rootCellWidthPixels: 1,
            rootCellHeightPixels: 1);
        return Draw(view, geometry, occlusionRects);
    }

    public bool Draw(InventorySpaceViewModel view, InventorySpacePresentationGeometry geometry, IReadOnlyList<PixelRect>? occlusionRects = null)
    {
        var profile = geometry.Profile;
        if (profile.UsesCandiiFont)
        {
            return DrawCandii(view, geometry, occlusionRects ?? []);
        }

        DrawMicro(view, geometry, occlusionRects ?? []);
        return true;
    }

    public void QueueMicroDrawCall()
    {
        if (!HasMicroCells)
        {
            return;
        }

        GameHost.Instance.DrawCalls.Enqueue(new DrawCallCustom(DrawMicroCells));
    }

    private bool DrawCandii(InventorySpaceViewModel view, InventorySpacePresentationGeometry geometry, IReadOnlyList<PixelRect> occlusionRects)
    {
        if (!SadConsole.Game.Instance.Fonts.TryGetValue(_fontName, out var font))
        {
            return false;
        }

        var profile = geometry.Profile;
        var entitiesByCoord = view.Entities
            .Where(entity => view.IsVisible(entity.Coord))
            .GroupBy(entity => entity.Coord)
            .ToDictionary(group => group.Key, group => group.First());
        var facingDecoratorsByCoord = profile.ShowFacingDecorators
            ? view.Decorators
                .Where(decorator => decorator.Role == InventorySpaceDecoratorRole.Facing && view.IsVisible(decorator.Coord))
                .GroupBy(decorator => decorator.Coord)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(decorator => decorator.Priority).First())
            : [];
        foreach (var coord in view.VisibleCoords())
        {
            var cellBounds = geometry.CellPixelBounds(coord);
            if (occlusionRects.Any(rect => rect.Intersects(cellBounds)))
            {
                continue;
            }

            var key = $"{geometry.ComponentId}:{coord.X},{coord.Y}";
            _activeCellLayerKeys.Add(key);
            if (!_cellLayers.TryGetValue(key, out var cell))
            {
                cell = new Console(1, 1)
                {
                    UsePixelPositioning = true,
                    IsVisible = true,
                    UseMouse = false,
                    UseKeyboard = false
                };
                _host.Children.Add(cell);
                _cellLayers[key] = cell;
            }

            cell.Font = font;
            cell.FontSize = font.GetFontSize(FontSizeForProfile(profile));
            cell.Position = new Point(cellBounds.Left, cellBounds.Top);
            var glyph = view.Backdrop.Tile.Glyph;
            var foreground = ColorForPresentation(view.Backdrop.Tile.Foreground);
            var background = ColorForLayerBackground(view.Backdrop.Tile) ?? Color.Black;
            var mirror = view.Backdrop.Tile.Mirror;
            if (entitiesByCoord.TryGetValue(coord, out var entity))
            {
                glyph = entity.Primary.Glyph;
                foreground = ColorForPresentation(entity.Primary.Foreground);
                background = ColorForLayerBackground(entity.Primary) ?? background;
                mirror = entity.Primary.Mirror;
            }

            var cellDecorators = entitiesByCoord.ContainsKey(coord) && facingDecoratorsByCoord.TryGetValue(coord, out var facingDecorator)
                ? new List<CellDecorator>
                {
                    new(
                        ColorForPresentation(facingDecorator.Style.Foreground),
                        facingDecorator.Style.Glyph,
                        facingDecorator.Style.Mirror)
                }
                : null;
            SetCell(cell, 0, 0, glyph, foreground, background, mirror, cellDecorators);
            cell.Surface.IsDirty = true;
        }

        return true;
    }

    private void DrawMicro(InventorySpaceViewModel view, InventorySpacePresentationGeometry geometry, IReadOnlyList<PixelRect> occlusionRects)
    {
        var profile = geometry.Profile;
        var entitiesByCoord = view.Entities
            .Where(entity => view.IsVisible(entity.Coord))
            .GroupBy(entity => entity.Coord)
            .ToDictionary(group => group.Key, group => group.First());
        foreach (var coord in view.VisibleCoords())
        {
            var bounds = geometry.CellPixelBounds(coord);
            if (occlusionRects.Any(rect => rect.Intersects(bounds)))
            {
                continue;
            }

            var color = entitiesByCoord.TryGetValue(coord, out var entity)
                ? XnaColorForPresentation(entity.Primary.Foreground)
                : XnaColorForLayerBackground(view.Backdrop.Tile) ?? new XnaColor(64, 64, 64);
            _microCells.Add(new MicroCellDraw(bounds.Left, bounds.Top, profile.CellPixelSize, color));
        }
    }

    private void DrawMicroCells()
    {
        if (_microCells.Count == 0)
        {
            return;
        }

        var pixel = Pixel();
        foreach (var cell in _microCells)
        {
            Global.SharedSpriteBatch.Draw(pixel, new XnaRectangle(_host.AbsoluteArea.X + cell.X, _host.AbsoluteArea.Y + cell.Y, cell.Size, cell.Size), cell.Color);
        }
    }

    private Texture2D Pixel()
    {
        if (_pixel is not null)
        {
            return _pixel;
        }

        _pixel = new Texture2D(Global.GraphicsDevice, 1, 1);
        _pixel.SetData([XnaColor.White]);
        return _pixel;
    }

    private static IFont.Sizes FontSizeForProfile(InventorySpaceDisplayProfile profile) => profile.CandiiScale switch
    {
        <= 1 => IFont.Sizes.One,
        2 => IFont.Sizes.Two,
        3 => IFont.Sizes.Three,
        _ => IFont.Sizes.Four
    };

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

    private static XnaColor XnaColorForPresentation(PresentationColor color) => color switch
    {
        PresentationColor.Gray => XnaColor.Gray,
        PresentationColor.White => XnaColor.White,
        PresentationColor.Yellow => XnaColor.Gold,
        PresentationColor.Cyan => XnaColor.Cyan,
        PresentationColor.Green => XnaColor.Green,
        PresentationColor.DarkGreen => XnaColor.DarkGreen,
        PresentationColor.Earth => XnaColor.SaddleBrown,
        PresentationColor.Default => XnaColor.White,
        _ => XnaColor.White
    };

    private static Color? ColorForLayerBackground(InventorySpaceVisualLayer layer) =>
        layer.BackgroundRgb is { } rgb
            ? ColorFromRgb(rgb)
            : layer.Background is { } background
                ? ColorForPresentation(background)
                : null;

    private static XnaColor? XnaColorForLayerBackground(InventorySpaceVisualLayer layer) =>
        layer.BackgroundRgb is { } rgb
            ? XnaColorFromRgb(rgb)
            : layer.Background is { } background
                ? XnaColorForPresentation(background)
                : null;

    private static Color ColorFromRgb(int rgb) => new((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF), byte.MaxValue);

    private static XnaColor XnaColorFromRgb(int rgb) => new((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF), byte.MaxValue);

    private static void SetCell(Console target, int x, int y, int glyph, Color foreground, Color background, Mirror mirror, List<CellDecorator>? decorators)
    {
        if (x < 0 || y < 0 || x >= target.Width || y >= target.Height)
        {
            return;
        }

        target.Surface[x, y].Glyph = glyph;
        target.Surface[x, y].Foreground = foreground;
        target.Surface[x, y].Background = background;
        target.Surface[x, y].Mirror = mirror;
        target.Surface[x, y].Decorators = decorators;
    }

    private sealed record MicroCellDraw(int X, int Y, int Size, XnaColor Color);
}
