using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Screens;
using GameGameGame.SadConsoleApp.Ui.Styling;
using GameGameGame.SadConsoleApp.Ui.Tiles;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using GggDirection = GameGameGame.Core.Direction;

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
        if (_screen.HasActivePrompt)
        {
            if (ReadDirection(keyboard) is { } promptDirection)
            {
                if (_screen.ActivePromptAcceptsDirection(promptDirection))
                {
                    _screen.HandlePromptDirection(promptDirection);
                }
                else
                {
                    _screen.HandlePromptNavigationDirection(promptDirection);
                }

                Redraw();
                return true;
            }

            if (keyboard.IsKeyReleased(Keys.Escape))
            {
                _screen.HandlePromptCommand(UiComponentCommand.Cancel);
                Redraw();
                return true;
            }

            if (keyboard.IsKeyReleased(Keys.Enter))
            {
                _screen.HandlePromptCommand(UiComponentCommand.Select);
                Redraw();
                return true;
            }

            if (keyboard.IsKeyReleased(Keys.Up))
            {
                _screen.HandlePromptCommand(UiComponentCommand.Up);
                Redraw();
                return true;
            }

            if (keyboard.IsKeyReleased(Keys.Down))
            {
                _screen.HandlePromptCommand(UiComponentCommand.Down);
                Redraw();
                return true;
            }
        }

        if (keyboard.IsKeyReleased(Keys.Enter))
        {
            _screen.SubmitDefaultAction();
            Redraw();
            return true;
        }

        if (ReadDirection(keyboard) is { } direction)
        {
            _screen.SubmitMove(direction);
            Redraw();
            return true;
        }

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
        var drawable = _layout.DrawableBounds;

        if (_screen.CurrentSpaceGridComponent(drawable, showDebugLabels: false) is { } currentSpaceGrid)
        {
            _renderer.DrawComponent(currentSpaceGrid);
        }

        if (_layout.DebugVisible)
        {
            DrawDebugOverlay(drawable);
        }

        if (_screen.PromptComponent(drawable) is { } prompt)
        {
            _renderer.RenderOverlay(prompt);
        }
        else
        {
            _renderer.ClearOverlay();
        }

        DrawBorderBuffer();
        Surface.IsDirty = true;
    }

    private void DrawDebugOverlay(SadConsoleRect drawable)
    {
        if (_screen.CurrentSpaceGridComponent(drawable, showDebugLabels: true) is { } currentSpaceGrid)
        {
            _renderer.DrawComponent(currentSpaceGrid);
        }

        var rows = new List<string>
        {
            _screen.FooterText,
            _screen.LastActionStatus,
            $"Theme: {_theme.Name} | {_displaySettings.Summary} | Drawable: {drawable.Width}x{drawable.Height}",
            $"Scenario: {_scenario.Name} ({_scenario.ScenarioId})"
        };
        rows.AddRange(_screen.DebugRows());

        var maxRows = Math.Min(rows.Count, Math.Max(0, drawable.Height));
        var startY = Math.Max(drawable.Top, drawable.Bottom - maxRows);
        for (var index = 0; index < maxRows; index++)
        {
            _renderer.PrintClipped(drawable.Left, startY + index, drawable.Width, rows[index], Color.DarkGray);
        }
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

    private static GggDirection? ReadDirection(Keyboard keyboard) =>
        keyboard.KeysReleased.Select(key => ReadDirectionKey(key.Key)).FirstOrDefault(direction => direction is not null);

    internal static GggDirection? ReadDirectionKey(Keys key) => key switch
    {
        Keys.Up or Keys.NumPad8 => GggDirection.North,
        Keys.Down or Keys.NumPad2 => GggDirection.South,
        Keys.Left or Keys.NumPad4 => GggDirection.West,
        Keys.Right or Keys.NumPad6 => GggDirection.East,
        Keys.NumPad7 => GggDirection.NorthWest,
        Keys.NumPad9 => GggDirection.NorthEast,
        Keys.NumPad1 => GggDirection.SouthWest,
        Keys.NumPad3 => GggDirection.SouthEast,
        _ => null
    };
}
