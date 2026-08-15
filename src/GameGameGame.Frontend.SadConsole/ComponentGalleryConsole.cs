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
    private PixelGlyphSpriteConsole? _moveSpriteOverlay;
    private EntityInspectionPlayspaceOverlayPresenter? _inspectionOverlays;
    private ToastNotificationState? _toast;
    private TimeSpan _moveAnimationElapsed;
    private PlayAnimationQueuePlayback? _moveQueuePlayback;
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

        if (_model.SelectedExample?.Kind == ComponentGalleryExampleKind.MoveAnimation)
        {
            _moveAnimationElapsed += delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
            var duration = StaticPlayRendererExamples.MoveSlideDuration;
            if (_moveAnimationElapsed >= duration)
            {
                _moveAnimationElapsed -= duration;
            }

            Redraw();
        }

        if (_model.SelectedExample?.Kind == ComponentGalleryExampleKind.MoveAnimationQueue && _moveQueuePlayback is not null)
        {
            _moveQueuePlayback.Advance(delta, speed: 1d);
            if (_moveQueuePlayback.Completed)
            {
                _moveQueuePlayback = new PlayAnimationQueuePlayback(StaticPlayRendererExamples.MoveQueueSteps(), StaticPlayRendererExamples.LayeredRoom(_tilesetProfile).Camera);
            }

            Redraw();
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
                HideMoveSpriteOverlay();
                ClearInspectionOverlays();
                _returnToBrowser();
                break;
            case ComponentGalleryResultKind.SelectorPopupRequested:
                ShowSelectorOverlay();
                break;
            case ComponentGalleryResultKind.ToastRequested:
                ShowToast(_model.CreateToastExample());
                break;
            case ComponentGalleryResultKind.StaticPlayRendererSelected:
                HideSelectorOverlay();
                HideMoveSpriteOverlay();
                ClearInspectionOverlays();
                _moveQueuePlayback = null;
                break;
            case ComponentGalleryResultKind.MoveAnimationSelected:
                HideSelectorOverlay();
                ClearInspectionOverlays();
                _moveAnimationElapsed = TimeSpan.Zero;
                _moveQueuePlayback = null;
                break;
            case ComponentGalleryResultKind.MoveAnimationQueueSelected:
                HideSelectorOverlay();
                ClearInspectionOverlays();
                _moveQueuePlayback = new PlayAnimationQueuePlayback(StaticPlayRendererExamples.MoveQueueSteps(), StaticPlayRendererExamples.LayeredRoom(_tilesetProfile).Camera);
                break;
            case ComponentGalleryResultKind.EntityInspectionPanelSelected:
                HideSelectorOverlay();
                HideMoveSpriteOverlay();
                _moveQueuePlayback = null;
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

        DrawSelectedExamplePreview(y + 2);

        PrintClipped(_bounds.X + 2, _bounds.Bottom - 2, _bounds.Width - 4, _message, Color.LightGreen);
        PrintClipped(_bounds.X + 2, _bounds.Bottom - 1, _bounds.Width - 4, _model.Footer, Color.Gray);

        if (_model.SelectorPopupOpen) ShowSelectorOverlay(); else HideSelectorOverlay();
        if (_toast is not null && !_toast.IsExpired) ShowToastOverlay(); else HideToastOverlay();
        Surface.IsDirty = true;
    }

    private void DrawSelectedExamplePreview(int y)
    {
        if (_model.SelectedExample?.Kind == ComponentGalleryExampleKind.StaticPlayRenderer)
        {
            DrawStaticPlayRendererPreview(y);
            return;
        }

        if (_model.SelectedExample?.Kind == ComponentGalleryExampleKind.MoveAnimation)
        {
            DrawMoveAnimationPreview(y);
            return;
        }

        if (_model.SelectedExample?.Kind == ComponentGalleryExampleKind.MoveAnimationQueue)
        {
            DrawMoveAnimationQueuePreview(y);
            return;
        }

        if (_model.SelectedExample?.Kind == ComponentGalleryExampleKind.EntityInspectionPanel)
        {
            DrawEntityInspectionPanelPreview(y);
            return;
        }

        ClearInspectionOverlays();
        HideMoveSpriteOverlay();
        PrintClipped(_bounds.X + 2, y, _bounds.Width - 4, "Select an example to inspect its live pattern.", Color.DarkGray);
    }

    private void DrawStaticPlayRendererPreview(int y)
    {
        HideMoveSpriteOverlay();
        ClearInspectionOverlays();
        var previewWidth = Math.Min(18, _bounds.Width - 8);
        var previewHeight = Math.Min(12, Math.Max(0, _bounds.Bottom - y - 3));
        if (previewWidth <= 2 || previewHeight <= 2)
        {
            return;
        }

        var panel = new FrontendRect(_bounds.X + 4, y, previewWidth, previewHeight);
        PanelRenderer.DrawPanel(this, panel, _tilesetProfile.Roles.PanelBorder, Color.Gold, Color.Black);
        PrintClipped(panel.X + 1, panel.Y, panel.Width - 2, "camera 12x8", Color.LightYellow);
        var frame = StaticPlayRendererExamples.LayeredRoom(_tilesetProfile);
        LayeredPlaySurfaceRenderer.Draw(this, new FrontendRect(panel.X + 1, panel.Y + 1, frame.Camera.ViewportWidth, frame.Camera.ViewportHeight), frame, _tilesetProfile);
        PrintClipped(panel.X, panel.Bottom + 1, _bounds.Width - 4, "Layers: backdrop < sprite < accent/status < UX highlight", Color.Gray);
    }

    private void DrawMoveAnimationPreview(int y)
    {
        var previewWidth = Math.Min(18, _bounds.Width - 8);
        ClearInspectionOverlays();
        var previewHeight = Math.Min(12, Math.Max(0, _bounds.Bottom - y - 3));
        if (previewWidth <= 2 || previewHeight <= 2)
        {
            HideMoveSpriteOverlay();
            return;
        }

        var panel = new FrontendRect(_bounds.X + 4, y, previewWidth, previewHeight);
        PanelRenderer.DrawPanel(this, panel, _tilesetProfile.Roles.PanelBorder, Color.Gold, Color.Black);
        PrintClipped(panel.X + 1, panel.Y, panel.Width - 2, "move slide", Color.LightYellow);
        var frame = StaticPlayRendererExamples.LayeredRoom(_tilesetProfile);
        var backdropOnly = frame with { Entities = [], Overlays = [] };
        var viewport = new FrontendRect(panel.X + 1, panel.Y + 1, frame.Camera.ViewportWidth, frame.Camera.ViewportHeight);
        LayeredPlaySurfaceRenderer.Draw(this, viewport, backdropOnly, _tilesetProfile);
        PrintClipped(panel.X, panel.Bottom + 1, _bounds.Width - 4, "The yellow sprite is pixel-positioned between adjacent cells.", Color.Gray);
        ShowMoveSpriteOverlay(viewport, frame.Camera);
    }

    private void DrawMoveAnimationQueuePreview(int y)
    {
        var previewWidth = Math.Min(18, _bounds.Width - 8);
        ClearInspectionOverlays();
        var previewHeight = Math.Min(12, Math.Max(0, _bounds.Bottom - y - 3));
        if (previewWidth <= 2 || previewHeight <= 2)
        {
            HideMoveSpriteOverlay();
            return;
        }

        var frame = StaticPlayRendererExamples.LayeredRoom(_tilesetProfile);
        _moveQueuePlayback ??= new PlayAnimationQueuePlayback(StaticPlayRendererExamples.MoveQueueSteps(), frame.Camera);
        var panel = new FrontendRect(_bounds.X + 4, y, previewWidth, previewHeight);
        PanelRenderer.DrawPanel(this, panel, _tilesetProfile.Roles.PanelBorder, Color.Gold, Color.Black);
        var active = _moveQueuePlayback.ActiveStep?.Id ?? "final redraw";
        PrintClipped(panel.X + 1, panel.Y, panel.Width - 2, active, Color.LightYellow);
        var backdropOnly = frame with { Entities = [], Overlays = [] };
        var viewport = new FrontendRect(panel.X + 1, panel.Y + 1, frame.Camera.ViewportWidth, frame.Camera.ViewportHeight);
        LayeredPlaySurfaceRenderer.Draw(this, viewport, backdropOnly, _tilesetProfile);
        PrintClipped(panel.X, panel.Bottom + 1, _bounds.Width - 4, "Queue plays one initiative step at a time, then redraws final state.", Color.Gray);
        ShowMoveQueueSpriteOverlay(viewport);
    }

    private void DrawEntityInspectionPanelPreview(int y)
    {
        HideMoveSpriteOverlay();
        var panelWidth = Math.Min(58, _bounds.Width - 8);
        var panelHeight = Math.Min(24, Math.Max(0, _bounds.Bottom - y - 1));
        if (panelWidth < 24 || panelHeight < 16)
        {
            ClearInspectionOverlays();
            PrintClipped(_bounds.X + 2, y, _bounds.Width - 4, "Not enough space to show inspection panel example.", Color.Red);
            return;
        }

        var bounds = new FrontendRect(_bounds.X + 4, y, panelWidth, panelHeight);
        var layout = EntityInspectionPanelLayout.Resolve(bounds, showInventory: true);
        var model = EntityInspectionPanelModel.GalleryExample();
        EntityInspectionPanelRenderer.Draw(this, layout, model, _tilesetProfile);
        _inspectionOverlays ??= new EntityInspectionPlayspaceOverlayPresenter(this, _displaySettings, _tilesetProfile);
        _inspectionOverlays.Draw(layout, model);
    }

    private void ClearInspectionOverlays()
    {
        _inspectionOverlays?.Clear();
        _inspectionOverlays = null;
    }

    private void ShowMoveSpriteOverlay(FrontendRect viewport, PlayCamera camera)
    {
        var entity = StaticPlayRendererExamples.AnimatedPlayer();
        var animation = StaticPlayRendererExamples.AdjacentMoveSlide();
        var command = LayeredPlaySurfaceProjector.BuildAnimatedEntityCommands(camera, entity, animation, _moveAnimationElapsed)
            .FirstOrDefault(command => command.Layer == PlayRenderLayer.EntitySprite);
        if (command is null)
        {
            HideMoveSpriteOverlay();
            return;
        }

        var pixelX = (int)Math.Round((viewport.X + command.ScreenPosition.X) * _displaySettings.ScaledTileWidth);
        var pixelY = (int)Math.Round((viewport.Y + command.ScreenPosition.Y) * _displaySettings.ScaledTileHeight);
        if (_moveSpriteOverlay is null)
        {
            _moveSpriteOverlay = new PixelGlyphSpriteConsole(command.Glyph, command.Foreground, command.Background);
            Children.Add(_moveSpriteOverlay);
        }

        _moveSpriteOverlay.Position = new Point(pixelX, pixelY);
        _moveSpriteOverlay.Surface.IsDirty = true;
    }

    private void ShowMoveQueueSpriteOverlay(FrontendRect viewport)
    {
        var command = _moveQueuePlayback?.ActiveCommands().FirstOrDefault(command => command.Layer == PlayRenderLayer.EntitySprite);
        if (command is null)
        {
            HideMoveSpriteOverlay();
            return;
        }

        var pixelX = (int)Math.Round((viewport.X + command.ScreenPosition.X) * _displaySettings.ScaledTileWidth);
        var pixelY = (int)Math.Round((viewport.Y + command.ScreenPosition.Y) * _displaySettings.ScaledTileHeight);
        if (_moveSpriteOverlay is null)
        {
            _moveSpriteOverlay = new PixelGlyphSpriteConsole(command.Glyph, command.Foreground, command.Background);
            Children.Add(_moveSpriteOverlay);
        }

        _moveSpriteOverlay.SetGlyph(command.Glyph, command.Foreground, command.Background);
        _moveSpriteOverlay.Position = new Point(pixelX, pixelY);
        _moveSpriteOverlay.Surface.IsDirty = true;
    }

    private void HideMoveSpriteOverlay()
    {
        if (_moveSpriteOverlay is null) return;
        Children.Remove(_moveSpriteOverlay);
        _moveSpriteOverlay = null;
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

