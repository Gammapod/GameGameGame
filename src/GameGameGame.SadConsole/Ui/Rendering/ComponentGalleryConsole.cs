using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Navigation;
using GameGameGame.SadConsoleApp.Ui.Screens;
using GameGameGame.SadConsoleApp.Ui.Styling;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace GameGameGame.SadConsoleApp.Ui.Rendering;

internal sealed class ComponentGalleryConsole : Console
{
    public const int ScreenWidth = 120;
    public const int ScreenHeight = 42;
    internal const int ColorSampleGlyph = 219;

    private readonly ComponentGalleryScreen _gallery;
    private readonly SadConsoleTheme _theme;
    private string _message = "Component gallery. Arrows select components. Enter focuses. Esc releases focus or exits.";

    public ComponentGalleryConsole(SadConsoleTheme? theme = null) : base(ScreenWidth, ScreenHeight)
    {
        _theme = theme ?? SadConsoleTheme.Default;
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
            DrawComponent(component);
        }

        Surface.IsDirty = true;
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
            if (start < 0) return CollapseSpaces(result);
            var end = result.IndexOf(')', start + 1);
            if (end < 0) return CollapseSpaces(result);
            result = result.Remove(start, end - start + 1).TrimStart();
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

    private static string CollapseSpaces(string text)
    {
        while (text.Contains("  ", StringComparison.Ordinal))
        {
            text = text.Replace("  ", " ", StringComparison.Ordinal);
        }

        return text.Trim();
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
