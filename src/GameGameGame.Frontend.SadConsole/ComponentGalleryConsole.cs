using SadConsole.Input;
using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace GameGameGame.Frontend.SadConsole;

internal sealed class ComponentGalleryConsole : Console
{
    private readonly FrontendDisplayShell _shell;
    private readonly SadConsoleDisplaySettings _displaySettings;
    private readonly TilesetProfile _tilesetProfile;
    private readonly ComponentGalleryScreenModel _model;
    private readonly Action _returnToBrowser;
    private readonly FrontendRect _bounds;
    private OverlayPanelConsole? _selectorOverlay;
    private OverlayPanelConsole? _toastOverlay;
    private ToastNotificationState? _toast;
    private string _message = "Component gallery: choose an example.";

    public ComponentGalleryConsole(FrontendDisplayShell shell, SadConsoleDisplaySettings displaySettings, TilesetProfile tilesetProfile, Action returnToBrowser)
        : base(shell.LogicalWidth, shell.LogicalHeight)
    {
        _shell = shell;
        _displaySettings = displaySettings;
        _tilesetProfile = tilesetProfile;
        _returnToBrowser = returnToBrowser;
        _model = new ComponentGalleryScreenModel();
        _bounds = shell.DrawableBounds;
        UseKeyboard = true;
        UseMouse = true;
        IsFocused = true;
        FocusedMode = global::SadConsole.FocusBehavior.Set;
        Redraw();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyReleased(Keys.Up)) Handle(ComponentGalleryCommand.Up);
        else if (keyboard.IsKeyReleased(Keys.Down)) Handle(ComponentGalleryCommand.Down);
        else if (keyboard.IsKeyReleased(Keys.Enter)) Handle(ComponentGalleryCommand.Select);
        else if (keyboard.IsKeyReleased(Keys.Escape)) Handle(ComponentGalleryCommand.Cancel);
        else return false;
        return true;
    }

    public override bool ProcessMouse(MouseScreenObjectState state)
    {
        if (!state.IsOnScreenObject) return false;
        var position = state.SurfaceCellPosition;
        if (!TryExampleIndexAt(position.X, position.Y, out var index)) return false;

        _message = _model.HoverExample(index).Message;
        if (state.Mouse.LeftClicked)
        {
            Apply(_model.SelectExample(index));
        }

        Redraw();
        return true;
    }

    public override void Render(TimeSpan delta)
    {
        if (_toast?.Advance(delta) == true)
        {
            HideToastOverlay();
        }

        base.Render(delta);
    }

    private void Handle(ComponentGalleryCommand command)
    {
        Apply(_model.Handle(command));
        Redraw();
    }

    private void Apply(ComponentGalleryResult result)
    {
        _message = result.Message;
        switch (result.Kind)
        {
            case ComponentGalleryResultKind.ExitRequested:
                HideSelectorOverlay();
                HideToastOverlay();
                _returnToBrowser();
                break;
            case ComponentGalleryResultKind.SelectorPopupRequested:
                ShowSelectorOverlay();
                break;
            case ComponentGalleryResultKind.ToastRequested:
                ShowToast(_model.CreateToastExample());
                break;
        }
    }

    private bool TryExampleIndexAt(int x, int y, out int index)
    {
        index = y - (_bounds.Y + 4);
        return x >= _bounds.X + 2
            && x < _bounds.X + Math.Min(_bounds.Width - 2, 54)
            && index >= 0
            && index < _model.Examples.Count;
    }

    private void Redraw()
    {
        ClearSurface();
        DrawBorder();
        PrintClipped(_bounds.X + 2, _bounds.Y + 1, _bounds.Width - 4, "GameGameGame - Interactive Component Gallery", Color.White);
        PrintClipped(_bounds.X + 2, _bounds.Y + 2, _bounds.Width - 4, "Executable examples for accepted frontend UI patterns.", Color.Gray);

        var y = _bounds.Y + 4;
        for (var i = 0; i < _model.Examples.Count; i++, y++)
        {
            var example = _model.Examples[i];
            var marker = i == _model.SelectedIndex ? ">" : " ";
            var color = i == _model.SelectedIndex ? Color.Cyan : i == _model.HoveredIndex ? Color.Gold : Color.White;
            PrintClipped(_bounds.X + 2, y, _bounds.Width - 4, $"{marker} {example.Title} - {example.Description}", color);
        }

        PrintClipped(_bounds.X + 2, _bounds.Bottom - 2, _bounds.Width - 4, _message, Color.LightGreen);
        PrintClipped(_bounds.X + 2, _bounds.Bottom - 1, _bounds.Width - 4, _model.Footer, Color.Gray);

        if (_model.SelectorPopupOpen) ShowSelectorOverlay(); else HideSelectorOverlay();
        if (_toast is not null && !_toast.IsExpired) ShowToastOverlay(); else HideToastOverlay();
        Surface.IsDirty = true;
    }

    private void ShowSelectorOverlay()
    {
        if (_selectorOverlay is not null) return;
        var panel = new FrontendRect(_bounds.X + 4, _bounds.Y + 5, Math.Min(_bounds.Width - 8, 72), 8);
        var background = new Color((byte)0, (byte)0, (byte)0, (byte)210);
        _selectorOverlay = new OverlayPanelConsole(
            new OverlayPanelModel(OverlayPanelGeometry.HalfTileOffset(panel, _displaySettings), _model.SelectorPopupRows(), Color.Gold, Color.White, background),
            _tilesetProfile);
        Children.Add(_selectorOverlay);
    }

    private void HideSelectorOverlay()
    {
        if (_selectorOverlay is null) return;
        Children.Remove(_selectorOverlay);
        _selectorOverlay = null;
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

        if (_toastOverlay is not null) return;
        var panel = new FrontendRect(_bounds.X + 4, _bounds.Y + 5, Math.Min(_bounds.Width - 8, 72), Math.Min(7, _bounds.Height - 8));
        _toastOverlay = new OverlayPanelConsole(ToastNotificationPresenter.ToOverlayAt(_toast, panel, _displaySettings), _tilesetProfile);
        Children.Add(_toastOverlay);
    }

    private void HideToastOverlay()
    {
        RemoveToastOverlay();
        if (_toast?.IsExpired == true) _toast = null;
    }

    private void RemoveToastOverlay()
    {
        if (_toastOverlay is null) return;
        Children.Remove(_toastOverlay);
        _toastOverlay = null;
    }

    private void ClearSurface()
    {
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
            SetGlyph(x, y, _tilesetProfile.Blank, Color.White, Color.Black);
    }

    private void DrawBorder()
    {
        for (var x = 0; x < Width; x++)
        {
            SetGlyph(x, 0, 181, Color.Black, Color.Black);
            SetGlyph(x, Height - 1, 181, Color.Black, Color.Black);
        }

        for (var y = 0; y < Height; y++)
        {
            SetGlyph(0, y, 181, Color.Black, Color.Black);
            SetGlyph(Width - 1, y, 181, Color.Black, Color.Black);
        }
    }

    private void PrintClipped(int x, int y, int width, string text, Color color)
    {
        if (width <= 0 || y < 0 || y >= Height) return;
        var clipped = text.Length <= width ? text : text[..width];
        for (var index = 0; index < clipped.Length && x + index < Width; index++)
            SetGlyph(x + index, y, _tilesetProfile.ResolveTextGlyph(clipped[index]), color, Color.Black);
    }

    private void SetGlyph(int x, int y, int glyph, Color foreground, Color background)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        Surface[x, y].Glyph = glyph;
        Surface[x, y].Foreground = foreground;
        Surface[x, y].Background = background;
    }
}
