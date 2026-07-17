using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Navigation;
using GameGameGame.SadConsoleApp.Ui.Screens;
using GameGameGame.SadConsoleApp.Ui.Styling;
using GameGameGame.SadConsoleApp.Ui.Tiles;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace GameGameGame.SadConsoleApp.Ui.Rendering;

internal sealed class ComponentGalleryConsole : Console
{
    public const int ScreenWidth = SadConsoleScreenMetrics.ScreenWidth;
    public const int ScreenHeight = SadConsoleScreenMetrics.ScreenHeight;
    internal const int ColorSampleGlyph = 219;

    private readonly ComponentGalleryScreen _gallery;
    private readonly SadConsoleTheme _theme;
    private readonly TilesetProfile _candiiProfile;
    private Console? _candiiPreviewLayer;
    private string _message = "Component gallery. Arrows select components. Enter focuses. Esc releases focus or exits.";

    public ComponentGalleryConsole(SadConsoleTheme? theme = null, TilesetProfile? candiiProfile = null) : base(ScreenWidth, ScreenHeight)
    {
        _theme = theme ?? SadConsoleTheme.Default;
        _candiiProfile = candiiProfile ?? TilesetProfileLoader.Load(ResolveAssetPath("Candii.tileset.json"));
        _gallery = ComponentGalleryScreen.CreateDefault(_theme);
        UseKeyboard = true;
        IsFocused = true;
        FocusedMode = FocusBehavior.Set;
        Redraw();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyReleased(Keys.Up)) Handle(UiComponentCommand.Up);
        else if (keyboard.IsKeyReleased(Keys.Down)) Handle(UiComponentCommand.Down);
        else if (keyboard.IsKeyReleased(Keys.Left)) Handle(UiComponentCommand.Left);
        else if (keyboard.IsKeyReleased(Keys.Right)) Handle(UiComponentCommand.Right);
        else if (keyboard.IsKeyReleased(Keys.Enter)) Handle(UiComponentCommand.Select);
        else if (keyboard.IsKeyReleased(Keys.Escape)) Handle(UiComponentCommand.Cancel);
        else return false;

        Redraw();
        return true;
    }

    private void Handle(UiComponentCommand command)
    {
        var result = _gallery.Handle(command);
        _message = result.Kind switch
        {
            FocusRouterResultKind.SelectedComponent => $"Selected component: {result.ComponentId}. Enter focuses it.",
            FocusRouterResultKind.FocusedComponent => $"Focused component: {result.ComponentId}. Controls now route there; Esc releases focus.",
            FocusRouterResultKind.RouteToFocusedComponent => $"Routed {result.RoutedCommand} to focused component: {result.ComponentId}.",
            FocusRouterResultKind.ReleasedFocus => $"Released focus from {result.ComponentId}. Arrows select components again.",
            FocusRouterResultKind.CancelScreen => "Leaving component gallery.",
            _ => _message
        };

        if (result.Kind == FocusRouterResultKind.CancelScreen)
        {
            SadConsole.Game.Instance.MonoGameInstance.Exit();
        }
    }

    private void Redraw()
    {
        ClearSurface();
        PrintClipped(1, 0, Width - 2, _gallery.Title, Color.Yellow);
        PrintClipped(1, 1, Width - 2, _gallery.Purpose, Color.White);
        PrintClipped(1, 2, Width - 2, _message, Color.Gray);

        foreach (var component in _gallery.Components())
        {
            if (component.Id == "candii-tileset")
            {
                continue;
            }

            DrawComponent(component);
        }

        RenderCandiiPreview();

        Surface.IsDirty = true;
    }

    private void RenderCandiiPreview()
    {
        var component = _gallery.Components().FirstOrDefault(component => component.Id == "candii-tileset");
        if (component is null)
        {
            RemoveCandiiPreviewLayer();
            return;
        }

        if (!SadConsole.Game.Instance.Fonts.TryGetValue(_candiiProfile.FontName, out var candiiFont))
        {
            RemoveCandiiPreviewLayer();
            PrintClipped(component.Bounds.Left + 1, component.Bounds.Top + 1, component.Bounds.Width - 2, $"{_candiiProfile.FontName} font was not loaded.", Color.Red);
            return;
        }

        var bounds = component.Bounds;
        var rootCellWidth = Math.Max(1, WidthPixels / Width);
        var rootCellHeight = Math.Max(1, HeightPixels / Height);
        var panelWidth = Math.Max(1, bounds.Width);
        var panelHeight = Math.Max(1, bounds.Height * rootCellHeight / Math.Max(1, candiiFont.GlyphHeight));

        if (_candiiPreviewLayer is null || _candiiPreviewLayer.Width != panelWidth || _candiiPreviewLayer.Height != panelHeight)
        {
            RemoveCandiiPreviewLayer();
            _candiiPreviewLayer = new Console(panelWidth, panelHeight)
            {
                Font = candiiFont,
                UsePixelPositioning = true,
                IsVisible = true
            };
            Children.Add(_candiiPreviewLayer);
        }

        _candiiPreviewLayer.Position = new Point(bounds.Left * rootCellWidth, bounds.Top * rootCellHeight);
        _candiiPreviewLayer.Font = candiiFont;
        ClearCandiiPreviewLayer(_candiiPreviewLayer);
        DrawCandiiPreviewPanel(_candiiPreviewLayer, component);
        _candiiPreviewLayer.Surface.IsDirty = true;
    }

    private void RemoveCandiiPreviewLayer()
    {
        if (_candiiPreviewLayer is null)
        {
            return;
        }

        Children.Remove(_candiiPreviewLayer);
        _candiiPreviewLayer = null;
    }

    private void DrawCandiiPreviewPanel(Console target, IUiComponent component)
    {
        var border = ColorFromToken(component.State.BorderColor(_theme));
        DrawCandiiBox(target, border, _candiiProfile.Roles.PanelBorder);
        PrintTextToTarget(target, 2, 0, component.Title, ColorFromToken(_theme.Panel.TitleText));
        DrawCandiiPreviewContent(target);
    }

    private void DrawCandiiPreviewContent(Console target)
    {
        PrintTextToTarget(target, 1, 1, "Candii glyph calibration", Color.White);
        PrintTextToTarget(target, 1, 2, "ASCII: ABC xyz 0123 @#[]{}", Color.LightGray);
        PrintTextToTarget(target, 1, 4, "Candii border indexes:", Color.Yellow);
        DrawBoxSample(target, 1, 5, _candiiProfile.Roles.PanelBorder, Color.Cyan);

        PrintTextToTarget(target, 1, 9, "Candidate ranges; labels are tile indexes", Color.Yellow);
        DrawGlyphRange(target, 1, 11, 144, "144");
        DrawGlyphRange(target, 1, 13, 160, "160");
        DrawGlyphRange(target, 1, 15, 176, "176");
        DrawGlyphRange(target, 1, 17, 192, "192");

        PrintTextToTarget(target, 1, 20, "Full glyph map 0-255:", Color.Yellow);
        var startY = 22;
        for (var row = 0; row < 16 && startY + row < target.Height - 1; row++)
        {
            for (var column = 0; column < 16 && column < target.Width - 1; column++)
            {
                var glyph = row * 16 + column;
                var foreground = glyph is >= 32 and <= 126 ? Color.LightGray : Color.Gray;
                SetCell(target, 1 + column, startY + row, glyph, foreground, Color.Black);
            }
        }
    }

    private void DrawBoxSample(Console target, int left, int top, TileBorderGlyphSet glyphs, Color color)
    {
        SetCell(target, left, top, glyphs.TopLeft, color, Color.Black);
        SetCell(target, left + 1, top, glyphs.Horizontal, color, Color.Black);
        SetCell(target, left + 2, top, glyphs.Horizontal, color, Color.Black);
        SetCell(target, left + 3, top, glyphs.TopRight, color, Color.Black);
        SetCell(target, left, top + 1, glyphs.Vertical, color, Color.Black);
        SetCell(target, left + 3, top + 1, glyphs.Vertical, color, Color.Black);
        SetCell(target, left, top + 2, glyphs.BottomLeft, color, Color.Black);
        SetCell(target, left + 1, top + 2, glyphs.Horizontal, color, Color.Black);
        SetCell(target, left + 2, top + 2, glyphs.Horizontal, color, Color.Black);
        SetCell(target, left + 3, top + 2, glyphs.BottomRight, color, Color.Black);
        PrintTextToTarget(target, left + 6, top, $"TL{glyphs.TopLeft} H{glyphs.Horizontal} TR{glyphs.TopRight}", Color.Gray);
        PrintTextToTarget(target, left + 6, top + 1, $"V{glyphs.Vertical}       V{glyphs.Vertical}", Color.Gray);
        PrintTextToTarget(target, left + 6, top + 2, $"BL{glyphs.BottomLeft} H{glyphs.Horizontal} BR{glyphs.BottomRight}", Color.Gray);
    }

    private void DrawGlyphRange(Console target, int left, int top, int startGlyph, string label)
    {
        PrintTextToTarget(target, left, top, label, Color.Gray);
        for (var offset = 0; offset < 16 && left + 5 + offset < target.Width - 1; offset++)
        {
            SetCell(target, left + 5 + offset, top, startGlyph + offset, Color.LightGray, Color.Black);
        }
    }

    private static void DrawCandiiBox(Console target, Color color, TileBorderGlyphSet glyphs)
    {
        var right = target.Width - 1;
        var bottom = target.Height - 1;
        for (var x = 0; x <= right; x++)
        {
            SetCell(target, x, 0, x == 0 ? glyphs.TopLeft : x == right ? glyphs.TopRight : glyphs.Horizontal, color, Color.Black);
            SetCell(target, x, bottom, x == 0 ? glyphs.BottomLeft : x == right ? glyphs.BottomRight : glyphs.Horizontal, color, Color.Black);
        }

        for (var y = 1; y < bottom; y++)
        {
            SetCell(target, 0, y, glyphs.Vertical, color, Color.Black);
            SetCell(target, right, y, glyphs.Vertical, color, Color.Black);
        }
    }

    private void PrintTextToTarget(Console target, int x, int y, string text, Color foreground)
    {
        for (var index = 0; index < text.Length && x + index < target.Width; index++)
        {
            SetCell(target, x + index, y, _candiiProfile.ResolveTextGlyph(text[index]), foreground, Color.Black);
        }
    }

    private void ClearCandiiPreviewLayer(Console target)
    {
        for (var y = 0; y < target.Height; y++)
        {
            for (var x = 0; x < target.Width; x++)
            {
                SetCell(target, x, y, _candiiProfile.Blank, Color.White, Color.Black);
            }
        }
    }

    private static string ResolveAssetPath(string fileName)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "assets", fileName);
        if (File.Exists(outputPath)) return outputPath;

        var workingPath = Path.Combine(Environment.CurrentDirectory, "assets", fileName);
        if (File.Exists(workingPath)) return workingPath;

        return outputPath;
    }

    private void DrawComponent(IUiComponent component)
    {
        var border = ColorFromToken(component.State.BorderColor(_theme));
        var bounds = component.Bounds;
        DrawBox(bounds, border, _theme.Panel.BorderGlyphs);
        PrintClipped(bounds.Left + 2, bounds.Top, Math.Max(0, bounds.Width - 4), component.Title, ColorFromToken(_theme.Panel.TitleText));

        var rows = component.RenderRows(_theme).Skip(1).ToList();
        var maxRows = Math.Max(0, bounds.Height - 2);
        for (var index = 0; index < rows.Count && index < maxRows; index++)
        {
            var row = rows[index];
            var visibleRow = StripStyleTokens(row);
            PrintClipped(bounds.Left + 1, bounds.Top + 1 + index, Math.Max(0, bounds.Width - 2), visibleRow, ColorForRow(row, component.State));
            DrawColorSampleGlyph(this, bounds.Left + 1, bounds.Top + 1 + index, Math.Max(0, bounds.Width - 2), visibleRow, row);
        }
    }

    private void DrawBox(SadConsoleRect rect, Color color, BorderGlyphTheme glyphs)
    {
        var right = rect.Left + rect.Width - 1;
        var bottom = rect.Bottom - 1;
        for (var x = rect.Left; x <= right; x++)
        {
            SetCell(x, rect.Top, x == rect.Left ? glyphs.TopLeft : x == right ? glyphs.TopRight : glyphs.Horizontal, color, Color.Black);
            SetCell(x, bottom, x == rect.Left ? glyphs.BottomLeft : x == right ? glyphs.BottomRight : glyphs.Horizontal, color, Color.Black);
        }

        for (var y = rect.Top + 1; y < bottom; y++)
        {
            SetCell(rect.Left, y, glyphs.Vertical, color, Color.Black);
            SetCell(right, y, glyphs.Vertical, color, Color.Black);
        }
    }

    private Color ColorForRow(string row, UiComponentState componentState)
    {
        if (row.Contains(_theme.Field.InvalidText, StringComparison.Ordinal)) return ColorFromToken(_theme.Field.InvalidText);
        if (row.Contains(_theme.Field.DirtyText, StringComparison.Ordinal)) return ColorFromToken(_theme.Field.DirtyText);
        if (row.Contains(_theme.Field.EditableText, StringComparison.Ordinal)) return ColorFromToken(_theme.Field.EditableText);
        foreach (var token in SadConsoleKnownColorTokens.Values)
        {
            if (row.Contains($"({token})", StringComparison.OrdinalIgnoreCase)) return ColorFromToken(token);
        }
        if (row.Contains(_theme.List.FocusedRowText, StringComparison.Ordinal)) return ColorFromToken(_theme.List.FocusedRowText);
        if (row.Contains(_theme.List.SelectedRowText, StringComparison.Ordinal)) return ColorFromToken(_theme.List.SelectedRowText);
        if (row.Contains(_theme.List.EmptyText, StringComparison.Ordinal)) return ColorFromToken(_theme.List.EmptyText);
        return componentState == UiComponentState.Focused ? Color.White : Color.LightGray;
    }

    internal static string StripStyleTokens(string text)
    {
        var result = text;
        while (true)
        {
            var start = result.IndexOf('(');
            if (start < 0) return result;
            var end = result.IndexOf(')', start + 1);
            if (end < 0) return result;
            var removeLength = end - start + 1;
            if (end + 1 < result.Length && result[end + 1] == ' ')
            {
                removeLength++;
            }

            result = result.Remove(start, removeLength);
        }
    }

    internal static string? SampleColorTokenForRow(string row)
    {
        foreach (var token in SadConsoleKnownColorTokens.Values)
        {
            if (row.Contains($"({token}) ■", StringComparison.OrdinalIgnoreCase)) return token;
        }

        return null;
    }

    internal static void DrawColorSampleGlyph(Console target, int x, int y, int width, string visibleRow, string styledRow)
    {
        if (width <= 0 || SampleColorTokenForRow(styledRow) is not { } token) return;

        var sampleIndex = visibleRow.IndexOf('■');
        if (sampleIndex < 0 || sampleIndex >= width) return;

        SetCell(target, x + sampleIndex, y, ColorSampleGlyph, ColorFromToken(token), Color.Black);
    }

    internal static Color ColorFromToken(string token) => token switch
    {
        "MutedBlue" => Color.SteelBlue,
        "Gold" => Color.Gold,
        "Brown" => Color.SaddleBrown,
        "HotPink" => Color.HotPink,
        "DarkGray" => Color.DarkGray,
        "Red" => Color.Red,
        "White" => Color.White,
        "LightGray" => Color.LightGray,
        "Gray" => Color.Gray,
        "Cyan" => Color.Cyan,
        "Orange" => Color.Orange,
        "DarkBlue" => Color.DarkBlue,
        "Black" => Color.Black,
        "Default" => Color.White,
        "Green" => Color.Green,
        "DarkGreen" => Color.DarkGreen,
        "Yellow" => Color.Yellow,
        "Earth" => Color.SaddleBrown,
        _ => Color.White
    };

    internal static string BorderGlyphPreview(BorderGlyphTheme glyphs) =>
        $"{glyphs.TopLeft}{glyphs.Horizontal}{glyphs.TopRight} {glyphs.Vertical} {glyphs.BottomLeft}{glyphs.Horizontal}{glyphs.BottomRight}";

    private void PrintClipped(int x, int y, int width, string text, Color color)
    {
        if (y < 0 || y >= Height || x >= Width || width <= 0) return;
        var clipped = text.Length <= width ? text : text[..Math.Max(0, width - 1)];
        PrintText(x, y, clipped.PadRight(Math.Max(0, width)), color);
    }

    private void PrintText(int x, int y, string text, Color foreground)
    {
        for (var index = 0; index < text.Length && x + index < ScreenWidth; index++)
        {
            SetCell(x + index, y, text[index], foreground, Color.Black);
        }
    }

    private void ClearSurface()
    {
        for (var y = 0; y < ScreenHeight; y++)
        {
            for (var x = 0; x < ScreenWidth; x++)
            {
                SetCell(x, y, ' ', Color.White, Color.Black);
            }
        }
    }

    private void SetCell(int x, int y, int glyph, Color foreground, Color background)
    {
        SetCell(this, x, y, glyph, foreground, background);
    }

    internal static void SetCell(Console target, int x, int y, int glyph, Color foreground, Color background)
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
