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
    private OverlayPanelConsole? _actionSelectorOverlay;
    private OverlayPanelConsole? _toastOverlay;
    private string _message = "Choose a scenario. Debug-room is the current target.";
    private ToastNotificationState? _toast;

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
        _model = new ScenarioBrowserScreenModel(catalog, selectedIndex, FrontendInputMode.Keyboard);
        _shell = shell;
        _layout = ScenarioBrowserLayout.Resolve(shell.DrawableBounds);
        _tilesetProfile = TilesetProfileLoader.LoadCandii();
        _chromeState = new ScenarioBrowserChromeState(windowMode, layoutDebugVisible);
        _displaySettings = displaySettings;
        UseKeyboard = true;
        UseMouse = true;
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
        else if (keyboard.IsKeyReleased(Keys.F2)) OpenComponentGallery();
        else if (keyboard.IsKeyReleased(Keys.F12)) ToggleLayoutDebug();
        else if (keyboard.IsKeyReleased(Keys.F11)) ToggleFullscreen();
        else return false;

        return true;
    }

    public override bool ProcessMouse(MouseScreenObjectState state)
    {
        if (!state.IsOnScreenObject)
        {
            return false;
        }

        var handled = false;
        if (state.Mouse.ScrollWheelValueChange != 0)
        {
            var delta = state.Mouse.ScrollWheelValueChange > 0 ? 1 : -1;
            _message = _model.Scroll(delta).Message;
            handled = true;
        }

        var position = state.SurfaceCellPosition;
        if (TryVisibleRowIndexAt(position.X, position.Y, out var hoverRowIndex))
        {
            var hoverResult = _model.HoverVisibleRow(_model.Viewport(_layout.ListHeight), hoverRowIndex);
            _message = hoverResult.Message;
            handled = true;
        }
        else if (_model.HoveredIndex is not null)
        {
            _model.ClearHover();
            handled = true;
        }

        if (state.Mouse.LeftClicked)
        {
            if (TryVisibleRowIndexAt(position.X, position.Y, out var rowIndex))
            {
                var result = _model.SelectVisibleRow(_model.Viewport(_layout.ListHeight), rowIndex, launch: true);
                _message = result.Message;
                handled = true;
            }
        }

        if (handled)
        {
            Redraw();
        }

        return handled;
    }

    public override void Render(TimeSpan delta)
    {
        if (_toast?.Advance(delta) == true)
        {
            HideToastOverlay();
        }

        base.Render(delta);
    }

    private void ToggleLayoutDebug()
    {
        var visible = _chromeState.ToggleLayoutDebug();
        _message = visible ? "Layout debug visible." : "Layout debug hidden.";
        Redraw();
    }

    private void OpenComponentGallery()
    {
        HideActionSelectorOverlay();
        HideToastOverlay();
        var gallery = new ComponentGalleryConsole(_shell, _displaySettings, _tilesetProfile, () =>
        {
            global::SadConsole.Game.Instance.Screen = this;
            IsFocused = true;
            FocusedMode = global::SadConsole.FocusBehavior.Set;
            Redraw();
        });
        global::SadConsole.Game.Instance.Screen = gallery;
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
            TryLaunch(entry);
        }

        Redraw();
    }

    private void TryLaunch(WorkspaceScenarioCatalogEntry entry)
    {
        try
        {
            var session = WorkspaceScenarioCatalogService.Launch(_catalog, entry.EntryId);
            if (!session.CanPlay)
            {
                var presentation = ScenarioLaunchFailurePresenter.FromSession(session);
                _message = $"Cannot play {session.Name}; warning shown.";
                ShowToast(ToastNotificationPresenter.LaunchWarning(presentation));
                return;
            }

            HideToastOverlay();
            var play = new PlayModeConsole(session, _shell, _displaySettings, _tilesetProfile, () =>
            {
                global::SadConsole.Game.Instance.Screen = this;
                IsFocused = true;
                FocusedMode = global::SadConsole.FocusBehavior.Set;
                Redraw();
            });
            global::SadConsole.Game.Instance.Screen = play;
        }
        catch (Exception ex)
        {
            var presentation = ScenarioLaunchFailurePresenter.FromException(entry, ex);
            _message = $"Launch failed for {entry.Name}; warning shown.";
            ShowToast(ToastNotificationPresenter.LaunchWarning(presentation));
        }
    }

    private bool TryVisibleRowIndexAt(int x, int y, out int rowIndex)
    {
        rowIndex = y - _layout.ListY;
        return x >= _layout.TextX
            && x < _layout.TextX + _layout.TextWidth
            && rowIndex >= 0
            && rowIndex < _model.Viewport(_layout.ListHeight).Entries.Count;
    }

    private void Redraw()
    {
        ClearSurface();
        DrawBorder();
        var bounds = _layout.Bounds;
        PrintClipped(_layout.TextX, _layout.TitleY, _layout.TextWidth, _model.Title, Color.White);
        PrintClipped(_layout.TextX, _layout.SummaryY, _layout.TextWidth, $"Drawable: {bounds.Width}x{bounds.Height} cells | Scenarios: {_model.Entries.Count}", Color.Gray);
        var viewport = _model.Viewport(_layout.ListHeight);
        var position = viewport.PositionSummary(_model.SelectedIndex, _model.Entries.Count);
        var scrollHint = viewport.HasItemsAbove || viewport.HasItemsBelow
            ? $" | showing {viewport.StartIndex + 1}-{viewport.EndIndexExclusive}"
            : string.Empty;
        PrintClipped(_layout.TextX, _layout.HeadingY, _layout.TextWidth, $"Available scenarios ({position}{scrollHint})", Color.Yellow);

        var y = _layout.ListY;
        for (var visibleIndex = 0; visibleIndex < viewport.Entries.Count; visibleIndex++, y++)
        {
            var entry = viewport.Entries[visibleIndex];
            var marker = visibleIndex == viewport.SelectedVisibleIndex ? ">" : " ";
            var kind = entry.IsWorkspaceBacked ? "workspace" : "file";
            var absoluteIndex = viewport.StartIndex + visibleIndex;
            var color = visibleIndex == viewport.SelectedVisibleIndex
                ? Color.Cyan
                : absoluteIndex == _model.HoveredIndex
                    ? Color.Gold
                    : Color.White;
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

        if (_model.ActionSelectorOpen)
        {
            ShowActionSelectorOverlay();
        }
        else
        {
            HideActionSelectorOverlay();
        }

        if (_toast is not null && !_toast.IsExpired)
        {
            ShowToastOverlay();
        }
        else
        {
            HideToastOverlay();
        }

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

    private void ShowActionSelectorOverlay()
    {
        var entry = _model.SelectedEntry;
        if (entry is null)
        {
            HideActionSelectorOverlay();
            return;
        }

        var background = new Color((byte)0, (byte)0, (byte)0, (byte)210);
        var width = Math.Min(Math.Max(0, _layout.TextWidth - 2), 74);
        var panelBounds = new FrontendRect(_layout.TextX + 1, _layout.ListY, width + 2, Math.Min(10, _layout.MessageY - _layout.ListY));
        var rows = new[]
        {
            $"Scenario: {entry.Name}",
            $"Id: {entry.ScenarioId} | Source: {(entry.IsWorkspaceBacked ? "workspace" : "file")}",
            $"Status: {entry.Status ?? "none"} | Tags: {(entry.Tags.Count == 0 ? "none" : string.Join(",", entry.Tags))}",
            $"Preview: turn-0 preview placeholder; materialized preview surface pending.",
            string.Empty,
            $"{(_model.SelectedActionOption == ScenarioBrowserActionOption.Play ? ">" : " ")} Play",
            $"{(_model.SelectedActionOption == ScenarioBrowserActionOption.Edit ? ">" : " ")} Edit (placeholder)"
        };

        HideActionSelectorOverlay();
        _actionSelectorOverlay = new OverlayPanelConsole(
            new OverlayPanelModel(
                OverlayPanelGeometry.HalfTileOffset(panelBounds, _displaySettings),
                rows,
                Color.Gold,
                Color.White,
                background),
            _tilesetProfile);
        Children.Add(_actionSelectorOverlay);
        _actionSelectorOverlay.IsVisible = true;
        _actionSelectorOverlay.Surface.IsDirty = true;
    }

    private void HideActionSelectorOverlay()
    {
        if (_actionSelectorOverlay is not null)
        {
            Children.Remove(_actionSelectorOverlay);
            _actionSelectorOverlay = null;
        }
    }

    private void ShowToast(ToastNotificationState toast)
    {
        RemoveToastOverlay();
        _toast = toast;
        ShowToastOverlay();
    }

    private void ShowToastOverlay()
    {
        if (_toast is null || _toast.IsExpired)
        {
            HideToastOverlay();
            return;
        }

        if (_toastOverlay is not null)
        {
            return;
        }

        _toastOverlay = new OverlayPanelConsole(
            ToastNotificationPresenter.ToOverlay(_toast, _layout, _displaySettings),
            _tilesetProfile);
        Children.Add(_toastOverlay);
        _toastOverlay.IsVisible = true;
        _toastOverlay.Surface.IsDirty = true;
    }

    private void HideToastOverlay()
    {
        RemoveToastOverlay();

        if (_toast?.IsExpired == true)
        {
            _toast = null;
        }
    }

    private void RemoveToastOverlay()
    {
        if (_toastOverlay is not null)
        {
            Children.Remove(_toastOverlay);
            _toastOverlay = null;
            Surface.IsDirty = true;
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
