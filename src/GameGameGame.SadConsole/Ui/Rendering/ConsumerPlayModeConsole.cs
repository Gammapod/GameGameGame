using GameGameGame.Content;
using GameGameGame.SadConsoleApp.Ui.Components;
using GameGameGame.SadConsoleApp.Ui.Screens;
using GameGameGame.SadConsoleApp.Ui.Styling;
using GameGameGame.SadConsoleApp.Ui.Tiles;
using SadConsole;
using SadConsole.DrawCalls;
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
    private readonly ConnectorLineDrawCallRenderer _connectorRenderer = new();
    private readonly GameplayCaptureRecorder _captureRecorder = new();
    private readonly GameplayCapturePolicy _capturePolicy = GameplayCapturePolicy.Default;
    private readonly IGameplayCaptureSink _captureSink = new MonoGameGameplayCaptureSink();
    private readonly Queue<int> _pendingCaptureTurns = new();
    private readonly ConsumerPlayModeScreen _screen;
    private ConsumerPlayModeLayout _layout;
    private readonly List<ConnectorLineViewModel> _lastConnectors = [];

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
        FlushPendingCaptures();

        if (_screen.HasActivePrompt)
        {
            if (IsWaitKeyReleased(keyboard))
            {
                var submission = _screen.SubmitWait();
                Redraw();
                QueueCaptureAfterSuccessfulPlayerTurn(submission);
                return true;
            }

            if (keyboard.IsKeyReleased(Keys.U))
            {
                _screen.UndoPreviousFrame();
                Redraw();
                return true;
            }

            if (ReadDirection(keyboard) is { } promptDirection)
            {
                if (_screen.ActivePromptAcceptsDirection(promptDirection))
                {
                    var outcome = _screen.HandlePromptDirection(promptDirection);
                    QueueCaptureAfterSuccessfulPlayerTurn(outcome.Submission);
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
                var outcome = _screen.HandlePromptCommand(UiComponentCommand.Select);
                Redraw();
                QueueCaptureAfterSuccessfulPlayerTurn(outcome.Submission);
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
            var outcome = _screen.SubmitDefaultAction();
            Redraw();
            QueueCaptureAfterSuccessfulPlayerTurn(outcome.Submission);
            return true;
        }

        if (ReadDirection(keyboard) is { } direction)
        {
            var submission = _screen.SubmitMove(direction);
            Redraw();
            QueueCaptureAfterSuccessfulPlayerTurn(submission);
            return true;
        }

        if (IsWaitKeyReleased(keyboard))
        {
            var submission = _screen.SubmitWait();
            Redraw();
            QueueCaptureAfterSuccessfulPlayerTurn(submission);
            return true;
        }

        if (keyboard.IsKeyReleased(Keys.U))
        {
            _screen.UndoPreviousFrame();
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

        if (keyboard.IsKeyReleased(Keys.F10))
        {
            ToggleCaptureRecording();
            Redraw();
            return true;
        }

        return false;
    }

    public override void Render(TimeSpan delta)
    {
        base.Render(delta);
        if (_lastConnectors.Count > 0 && !_screen.HasActivePrompt)
        {
            GameHost.Instance.DrawCalls.Enqueue(new DrawCallCustom(DrawLinkedConnector));
        }
    }

    private void Redraw()
    {
        _renderer.ClearSurface();
        var drawable = _layout.DrawableBounds;
        var frame = _screen.BuildRenderFrame(drawable, _layout.DebugVisible);

        _lastConnectors.Clear();
        foreach (var component in frame.MainDrawableComponents)
        {
            _renderer.DrawComponent(component);
        }

        _lastConnectors.AddRange(frame.MainConnectors.Select(connector => connector.View));

        if (_layout.DebugVisible)
        {
            DrawDebugOverlay(frame);
        }

        if (frame.PromptOverlay is { } prompt)
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

    private void DrawDebugOverlay(ConsumerPlayModeRenderFrame frame)
    {
        _lastConnectors.Clear();
        foreach (var component in frame.DebugDrawableComponents)
        {
            _renderer.DrawComponent(component);
        }

        _lastConnectors.AddRange(frame.DebugConnectors.Select(connector => connector.View));

        if (frame.DiagnosticsChromeComponent is { } diagnosticsChrome)
        {
            _renderer.DrawComponent(diagnosticsChrome);
        }

        var rows = new List<string>
        {
            _screen.FooterText,
            _screen.LastActionStatus,
            _captureRecorder.StatusText,
            _capturePolicy.DebugSummary,
            $"Theme: {_theme.Name} | {_displaySettings.Summary} | Drawable: {frame.DrawableBounds.Width}x{frame.DrawableBounds.Height}",
            $"Scenario: {_scenario.Name} ({_scenario.ScenarioId})"
        };
        rows.AddRange(frame.DebugRows);

        var maxRows = Math.Min(rows.Count, Math.Max(0, frame.DrawableBounds.Height));
        var startY = Math.Max(frame.DrawableBounds.Top, frame.DrawableBounds.Bottom - maxRows);
        for (var index = 0; index < maxRows; index++)
        {
            _renderer.PrintClipped(frame.DrawableBounds.Left, startY + index, frame.DrawableBounds.Width, rows[index], Color.DarkGray);
        }
    }

    private void DrawLinkedConnector()
    {
        if (_lastConnectors.Count == 0)
        {
            return;
        }

        var cellWidth = Math.Max(1, WidthPixels / Math.Max(1, Width));
        var cellHeight = Math.Max(1, HeightPixels / Math.Max(1, Height));
        _connectorRenderer.Draw(_lastConnectors, AbsoluteArea.X, AbsoluteArea.Y, cellWidth, cellHeight, drawEndpoints: false);
    }

    private void ToggleCaptureRecording()
    {
        try
        {
            if (_captureRecorder.IsRecording)
            {
                while (_pendingCaptureTurns.Count > 0)
                {
                    _captureRecorder.CaptureFrame(_pendingCaptureTurns.Dequeue(), _captureSink);
                }

                var result = _captureRecorder.Stop(_captureSink);
                _screen.SetDebugStatus(result.Message);
            }
            else
            {
                var result = _captureRecorder.Start(_scenario.Name, _screen.WorldTurnNumber, _captureSink);
                _screen.SetDebugStatus(result.Message);
            }
        }
        catch (Exception ex)
        {
            _screen.SetDebugStatus($"Capture failed: {ex.Message}");
        }
    }

    private void QueueCaptureAfterSuccessfulPlayerTurn(GameplayRuntimeSubmission? submission)
    {
        if (!_captureRecorder.IsRecording)
        {
            return;
        }

        var frame = _screen.BuildRenderFrame(_layout.DrawableBounds, _layout.DebugVisible);
        if (!_capturePolicy.ShouldQueueAfterPlayerSubmission(submission, frame))
        {
            return;
        }

        _pendingCaptureTurns.Enqueue(_screen.WorldTurnNumber);
    }

    private void FlushPendingCaptures()
    {
        while (_pendingCaptureTurns.Count > 0)
        {
            try
            {
                var result = _captureRecorder.CaptureFrame(_pendingCaptureTurns.Dequeue(), _captureSink);
                _screen.SetDebugStatus(result.Message);
            }
            catch (Exception ex)
            {
                _pendingCaptureTurns.Clear();
                _screen.SetDebugStatus($"Capture failed: {ex.Message}");
                break;
            }
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

    internal static bool IsWaitKey(Keys key) => key is Keys.Space or Keys.D5 or Keys.NumPad5;

    private static bool IsWaitKeyReleased(Keyboard keyboard) =>
        keyboard.KeysReleased.Any(key => IsWaitKey(key.Key));
}
