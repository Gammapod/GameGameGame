using SadRogue.Primitives;

namespace GameGameGame.Frontend.SadConsole;

internal sealed class EntityInspectionOverlayConsole : global::SadConsole.Console
{
    private static readonly Color OverlayBackground = new(0, 0, 0, 210);
    private readonly SadConsoleDisplaySettings _displaySettings;
    private readonly TilesetProfile _tilesetProfile;
    private readonly EntityInspectionPlayspaceOverlayPresenter _playspaceOverlays;

    public EntityInspectionOverlayConsole(
        OverlayPanelGeometry geometry,
        SadConsoleDisplaySettings displaySettings,
        TilesetProfile tilesetProfile)
        : base(geometry.CellBounds.Width, geometry.CellBounds.Height)
    {
        _displaySettings = displaySettings;
        _tilesetProfile = tilesetProfile;
        UsePixelPositioning = true;
        UseKeyboard = false;
        UseMouse = false;
        IsVisible = true;
        Position = new Point(geometry.PixelX, geometry.PixelY);
        _playspaceOverlays = new EntityInspectionPlayspaceOverlayPresenter(this, _displaySettings, _tilesetProfile);
    }

    public void Draw(EntityInspectionPanelModel? model, int selectedActionIndex = 0, bool actionMenuFocused = false, EntityInspectionPanelRenderOptions? options = null)
    {
        ClearSurface();
        if (model is null)
        {
            _playspaceOverlays.Clear();
            PanelRenderer.DrawPanel(this, LocalBounds, _tilesetProfile.Roles.PanelBorder, Color.DarkGray, OverlayBackground);
            Print(2, 1, "No adjacent entity", Color.DarkGray, OverlayBackground);
        }
        else
        {
            var layout = EntityInspectionPanelLayout.ResolveAdaptive(LocalBounds, model);
            EntityInspectionPanelRenderer.Draw(this, layout, model, _tilesetProfile, OverlayBackground, selectedActionIndex, actionMenuFocused, options);
            _playspaceOverlays.Draw(layout, model);
        }

        Surface.IsDirty = true;
    }

    public void MoveTo(OverlayPanelGeometry geometry)
    {
        Position = new Point(geometry.PixelX, geometry.PixelY);
    }

    private FrontendRect LocalBounds => new(0, 0, Width, Height);

    private void ClearSurface()
    {
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
            SetGlyph(x, y, _tilesetProfile.Blank, Color.White, Color.Transparent);
    }

    private void Print(int x, int y, string text, Color foreground, Color background)
    {
        for (var index = 0; index < text.Length && x + index < Width; index++)
        {
            SetGlyph(x + index, y, _tilesetProfile.ResolveTextGlyph(text[index]), foreground, background);
        }
    }

    private void SetGlyph(int x, int y, int glyph, Color foreground, Color background)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        Surface[x, y].Glyph = glyph;
        Surface[x, y].Foreground = foreground;
        Surface[x, y].Background = background;
    }
}
