using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Screens;
using GameGameGame.SadConsoleApp.Ui.Styling;
using GameGameGame.SadConsoleApp.Ui.Tiles;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace GameGameGame.SadConsoleApp.Ui.Rendering;

internal sealed class ConsumerPlayModeConsole : Console
{
    private readonly ScenarioCatalogEntry _scenario;
    private readonly Action _returnToScenarioSelection;
    private readonly SadConsoleTheme _theme;
    private readonly SadConsoleDisplaySettings _displaySettings;
    private readonly SadConsoleComponentRenderer _renderer;
    private readonly ConsumerPlayModeScreen _screen;
    private ConsumerPlayModeLayout _layout;

    public ConsumerPlayModeConsole(
        ScenarioCatalogEntry scenario,
        Action returnToScenarioSelection,
        SadConsoleTheme theme,
        SadConsoleDisplaySettings displaySettings,
        ConsumerPlayModeLayout? layout = null)
        : base((layout ?? ConsumerPlayModeLayout.FromDisplaySettings(displaySettings)).Width, (layout ?? ConsumerPlayModeLayout.FromDisplaySettings(displaySettings)).Height)
    {
        _scenario = scenario;
        _returnToScenarioSelection = returnToScenarioSelection;
        _theme = theme;
        _displaySettings = displaySettings;
        _renderer = new SadConsoleComponentRenderer(this, _theme, _displaySettings);
        _screen = ConsumerPlayModeScreen.Open(scenario);
        _layout = layout ?? ConsumerPlayModeLayout.FromDisplaySettings(displaySettings);
        UseKeyboard = true;
        IsFocused = true;
        FocusedMode = FocusBehavior.Set;
        Redraw();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyReleased(Keys.Escape))
        {
            _returnToScenarioSelection();
            return true;
        }

        if (keyboard.IsKeyReleased(Keys.F12))
        {
            _layout = _layout.WithDebugVisible(!_layout.DebugVisible);
            Redraw();
            return true;
        }

        return false;
    }

    private void Redraw()
    {
        _renderer.ClearSurface();
        DrawBorderBuffer();
        var drawable = _layout.DrawableBounds;
        _renderer.PrintClipped(drawable.Left, drawable.Top, drawable.Width, _screen.Title, Color.Yellow);
        _renderer.PrintClipped(drawable.Left, drawable.Top + 1, drawable.Width, _screen.Purpose, Color.White);
        _renderer.PrintClipped(drawable.Left, drawable.Top + 2, drawable.Width, $"Scenario: {_scenario.Name} ({_scenario.ScenarioId})", Color.LightGray);
        foreach (var component in _screen.Components(drawable))
        {
            _renderer.DrawComponent(component);
        }

        _renderer.PrintClipped(drawable.Left, drawable.Bottom - 2, drawable.Width, _screen.FooterText, Color.DarkGray);
        _renderer.PrintClipped(drawable.Left, drawable.Bottom - 1, drawable.Width, $"Theme: {_theme.Name} | {_displaySettings.Summary} | Drawable: {drawable.Width}x{drawable.Height}", Color.DarkGray);
        Surface.IsDirty = true;
    }

    private void DrawBorderBuffer()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (x != 0 && y != 0 && x != Width - 1 && y != Height - 1)
                {
                    continue;
                }

                Surface[x, y].Glyph = _layout.BorderGlyph;
                Surface[x, y].Foreground = _layout.BorderForeground;
                Surface[x, y].Background = _layout.BorderBackground;
            }
        }
    }
}
