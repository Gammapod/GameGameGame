using GameGameGame.Content;
using SadConsole.Input;
using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace GameGameGame.Frontend.SadConsole;

internal sealed class ScenarioBrowserConsole : Console
{
    private readonly WorkspaceScenarioCatalogResult _catalog;
    private readonly ScenarioBrowserScreenModel _model;
    private readonly FrontendDisplayShell _shell;
    private readonly ScenarioBrowserLayout _layout;
    private readonly TilesetProfile _tilesetProfile;
    private readonly ScenarioBrowserChromeState _chromeState;
    private readonly SadConsoleDisplaySettings _displaySettings;
    private string _message = "Choose a scenario. Debug-room is the current target.";

    public ScenarioBrowserConsole(
        WorkspaceScenarioCatalogResult catalog,
        FrontendDisplayShell shell,
        SadConsoleDisplaySettings displaySettings,
        FrontendWindowMode windowMode = FrontendWindowMode.Fullscreen,
        bool layoutDebugVisible = false,
        int selectedIndex = 0)
        : base(shell.LogicalWidth, shell.LogicalHeight)
    {
        _catalog = catalog;
        _model = new ScenarioBrowserScreenModel(catalog, selectedIndex);
        _shell = shell;
        _layout = ScenarioBrowserLayout.Resolve(shell.DrawableBounds);
        _tilesetProfile = TilesetProfileLoader.LoadCandii();
        _chromeState = new ScenarioBrowserChromeState(windowMode, layoutDebugVisible);
        _displaySettings = displaySettings;
        UseKeyboard = true;
        IsFocused = true;
        FocusedMode = global::SadConsole.FocusBehavior.Set;
        Redraw();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyReleased(Keys.Up)) Handle(ScenarioBrowserCommand.Up);
        else if (keyboard.IsKeyReleased(Keys.Down)) Handle(ScenarioBrowserCommand.Down);
        else if (keyboard.IsKeyReleased(Keys.Enter)) Handle(ScenarioBrowserCommand.Select);
        else if (keyboard.IsKeyReleased(Keys.Escape)) Handle(ScenarioBrowserCommand.Cancel);
        else if (keyboard.IsKeyReleased(Keys.F12)) ToggleLayoutDebug();
        else if (keyboard.IsKeyReleased(Keys.F11)) ToggleFullscreen();
        else return false;

        return true;
    }

    private void ToggleLayoutDebug()
    {
        var visible = _chromeState.ToggleLayoutDebug();
        _message = visible ? "Layout debug visible." : "Layout debug hidden.";
        Redraw();
    }

    private void ToggleFullscreen()
    {
        var mode = _chromeState.ToggleWindowMode();
        var result = SadConsoleDisplayHost.ApplyWindowMode(mode, _displaySettings);
        var shell = FrontendDisplayShell.Resolve(result.PixelWidth, result.PixelHeight, _displaySettings);
        var replacement = new ScenarioBrowserConsole(
            _catalog,
            shell,
            _displaySettings,
            result.WindowMode,
            _chromeState.LayoutDebugVisible,
            _model.SelectedIndex)
        {
            _message = result.Message
        };
        global::SadConsole.Game.Instance.Screen = replacement;
    }

    private void Handle(ScenarioBrowserCommand command)
    {
        var result = _model.Handle(command);
        _message = result.Message;

        if (result.Kind == ScenarioBrowserResultKind.ExitRequested)
        {
            global::SadConsole.Game.Instance.MonoGameInstance.Exit();
            return;
        }

        if (result.Kind == ScenarioBrowserResultKind.LaunchRequested && result.Entry is { } entry)
        {
            try
            {
                var session = WorkspaceScenarioCatalogService.Launch(_catalog, entry.EntryId);
                _message = session.CanPlay
                    ? $"Playable session loaded: {session.Name} ({session.ScenarioId}). Play surface next."
                    : $"Scenario loaded with diagnostics: {session.Name}.";
            }
            catch (Exception ex)
            {
                _message = $"Launch failed: {ex.Message}";
            }
        }

        Redraw();
    }

    private void Redraw()
    {
        ClearSurface();
        DrawBorder();
        var bounds = _layout.Bounds;
        PrintClipped(_layout.TextX, _layout.TitleY, _layout.TextWidth, _model.Title, Color.White);
        PrintClipped(_layout.TextX, _layout.SummaryY, _layout.TextWidth, $"Drawable: {bounds.Width}x{bounds.Height} cells | Scenarios: {_model.Entries.Count}", Color.Gray);
        PrintClipped(_layout.TextX, _layout.HeadingY, _layout.TextWidth, "Available scenarios", Color.Yellow);

        var y = _layout.ListY;
        for (var index = 0; index < _model.Entries.Count && index < _layout.ListHeight; index++, y++)
        {
            var entry = _model.Entries[index];
            var marker = index == _model.SelectedIndex ? ">" : " ";
            var kind = entry.IsWorkspaceBacked ? "workspace" : "file";
            var color = index == _model.SelectedIndex ? Color.Cyan : Color.White;
            PrintClipped(_layout.TextX, y, _layout.TextWidth, $"{marker} {entry.Name} [{entry.ScenarioId}] ({kind})", color);
        }

        if (_model.Entries.Count == 0)
        {
            PrintClipped(_layout.TextX, y, _layout.TextWidth, "No scenarios were discovered.", Color.Red);
        }

        if (_model.Diagnostics.Count > 0)
        {
            var diagnosticY = Math.Min(Math.Max(y + 1, _layout.ListY), _layout.MessageY - 4);
            PrintClipped(_layout.TextX, diagnosticY, _layout.TextWidth, "Diagnostics", Color.Orange);
            foreach (var diagnostic in _model.Diagnostics.Take(3))
            {
                diagnosticY++;
                if (diagnosticY >= _layout.MessageY)
                {
                    break;
                }

                PrintClipped(_layout.TextX, diagnosticY, _layout.TextWidth, diagnostic, Color.Orange);
            }
        }

        PrintClipped(_layout.TextX, _layout.MessageY, _layout.TextWidth, _message, Color.LightGreen);
        PrintClipped(_layout.TextX, _layout.FooterY, _layout.TextWidth, _model.Footer, Color.Gray);

        if (_chromeState.LayoutDebugVisible)
        {
            DrawDebugOverlay(ScenarioBrowserDebugOverlay.Build(_model, _shell, _layout, _chromeState.WindowMode));
        }

        Surface.IsDirty = true;
    }

    private void DrawBorder()
    {
        for (var x = 0; x < Width; x++)
        {
            SetGlyph(x, 0, 181, BorderColor());
            SetGlyph(x, Height - 1, 181, BorderColor());
        }

        for (var y = 0; y < Height; y++)
        {
            SetGlyph(0, y, 181, BorderColor());
            SetGlyph(Width - 1, y, 181, BorderColor());
        }
    }

    private Color BorderColor() => _chromeState.LayoutDebugVisible ? Color.Red : Color.Black;

    private void DrawDebugOverlay(ScenarioBrowserDebugOverlay overlay)
    {
        if (!overlay.IsVisible)
        {
            return;
        }

        var background = new Color((byte)0, (byte)0, (byte)0, (byte)128);
        var x = _layout.TextX;
        var y = _layout.HeadingY;
        var width = Math.Min(_layout.TextWidth, 78);
        foreach (var row in overlay.Rows)
        {
            if (y >= _layout.MessageY)
            {
                break;
            }

            PrintClipped(x, y, width, row, Color.LightSalmon, background);
            y++;
        }
    }

    private void ClearSurface()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                SetGlyph(x, y, _tilesetProfile.Blank, Color.White);
            }
        }
    }

    private void Print(int x, int y, string text, Color color)
    {
        if (y < 0 || y >= Height) return;
        for (var index = 0; index < text.Length && x + index < Width; index++)
        {
            SetGlyph(x + index, y, _tilesetProfile.ResolveTextGlyph(text[index]), color);
        }
    }

    private void PrintClipped(int x, int y, int width, string text, Color color)
    {
        if (width <= 0) return;
        Print(x, y, text.Length <= width ? text : text[..width], color);
    }

    private void PrintClipped(int x, int y, int width, string text, Color foreground, Color background)
    {
        if (width <= 0 || y < 0 || y >= Height) return;
        var clipped = text.Length <= width ? text : text[..width];
        for (var index = 0; index < clipped.Length && x + index < Width; index++)
        {
            SetGlyph(x + index, y, _tilesetProfile.ResolveTextGlyph(clipped[index]), foreground, background);
        }
    }

    private void SetGlyph(int x, int y, int glyph, Color color)
    {
        SetGlyph(x, y, glyph, color, Color.Black);
    }

    private void SetGlyph(int x, int y, int glyph, Color foreground, Color background)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        Surface[x, y].Glyph = glyph;
        Surface[x, y].Foreground = foreground;
        Surface[x, y].Background = background;
    }

}
