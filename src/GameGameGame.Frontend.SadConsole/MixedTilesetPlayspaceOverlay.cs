using SadRogue.Primitives;

namespace GameGameGame.Frontend.SadConsole;

internal sealed record MixedTilesetPlayspaceOverlayGeometry(
    FrontendRect ParentCellRegion,
    int ChildWidth,
    int ChildHeight,
    int PixelX,
    int PixelY)
{
    public static MixedTilesetPlayspaceOverlayGeometry FromParentRegion(
        FrontendRect parentCellRegion,
        int childWidth,
        int childHeight,
        SadConsoleDisplaySettings parentDisplaySettings) => new(
        parentCellRegion,
        childWidth,
        childHeight,
        parentCellRegion.X * parentDisplaySettings.ScaledTileWidth,
        parentCellRegion.Y * parentDisplaySettings.ScaledTileHeight);
}

internal sealed class Candii16PlayspaceOverlayConsole : global::SadConsole.Console
{
    private readonly TilesetProfile _tilesetProfile;

    public Candii16PlayspaceOverlayConsole(
        MixedTilesetPlayspaceOverlayGeometry geometry,
        TilesetProfile tilesetProfile,
        global::SadConsole.IFont candii16,
        global::SadConsole.IFont.Sizes fontSizePreset)
        : base(geometry.ChildWidth, geometry.ChildHeight)
    {
        _tilesetProfile = tilesetProfile;
        UsePixelPositioning = true;
        IsVisible = true;
        UseKeyboard = false;
        UseMouse = false;
        Position = new Point(geometry.PixelX, geometry.PixelY);
        Font = candii16;
        FontSize = candii16.GetFontSize(fontSizePreset);

        RedrawBackdrop();
    }

    public void DrawEntityAt(int x, int y, int glyph, Color foreground)
    {
        SetGlyph(x, y, glyph, foreground, Color.Black);
    }

    public void RedrawBackdrop()
    {
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
            SetGlyph(x, y, _tilesetProfile.Roles.DefaultBackdrop, Color.DimGray, Color.Black);
        Surface.IsDirty = true;
    }

    private void SetGlyph(int x, int y, int glyph, Color foreground, Color background)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        Surface[x, y].Glyph = glyph;
        Surface[x, y].Foreground = foreground;
        Surface[x, y].Background = background;
    }

    public static bool TryResolveCandii16Font(out global::SadConsole.IFont font)
    {
        var fonts = global::SadConsole.Game.Instance?.Fonts;
        if (fonts is not null)
        {
            foreach (var key in new[] { "Candii16", "Candii16.font", "assets/Candii16.font" })
            {
                if (fonts.TryGetValue(key, out font!) && font.GlyphWidth == 16 && font.GlyphHeight == 16)
                {
                    return true;
                }
            }
        }

        font = null!;
        return false;
    }
}

internal sealed class EntityInspectionPlayspaceOverlayPresenter(global::SadConsole.Console host, SadConsoleDisplaySettings displaySettings, TilesetProfile tilesetProfile)
{
    private Candii16PlayspaceOverlayConsole? _portrait;
    private Candii16PlayspaceOverlayConsole? _inventory;

    public void Draw(EntityInspectionPanelLayout layout)
    {
        _portrait = EnsureOverlay(_portrait, layout.PortraitRegion, 3, 3);
        _portrait.RedrawBackdrop();
        _portrait.DrawEntityAt(1, 1, tilesetProfile.PresentationMappings.GlyphsByPresentationId["object.pushBlock"], Color.LightGray);

        if (layout.InventoryRegion is { } inventoryRegion)
        {
            _inventory = EnsureOverlay(_inventory, inventoryRegion, 5, 3);
            _inventory.RedrawBackdrop();
        }
        else
        {
            RemoveInventory();
        }
    }

    public void Clear()
    {
        if (_portrait is not null)
        {
            host.Children.Remove(_portrait);
            _portrait = null;
        }

        RemoveInventory();
    }

    private Candii16PlayspaceOverlayConsole EnsureOverlay(Candii16PlayspaceOverlayConsole? existing, FrontendRect parentRegion, int width, int height)
    {
        if (!Candii16PlayspaceOverlayConsole.TryResolveCandii16Font(out var candii16))
        {
            if (existing is not null)
            {
                host.Children.Remove(existing);
            }

            throw new InvalidOperationException("Candii16 font must be loaded as a 16x16 extra SadConsole font before drawing mixed-tileset inspection overlays.");
        }

        var geometry = MixedTilesetPlayspaceOverlayGeometry.FromParentRegion(parentRegion, width, height, displaySettings);
        if (existing is null || existing.Width != width || existing.Height != height)
        {
            if (existing is not null) host.Children.Remove(existing);
            existing = new Candii16PlayspaceOverlayConsole(geometry, tilesetProfile, candii16, displaySettings.FontSizePreset);
            host.Children.Add(existing);
        }

        existing.Font = candii16;
        existing.FontSize = candii16.GetFontSize(displaySettings.FontSizePreset);
        existing.Position = new Point(geometry.PixelX, geometry.PixelY);
        return existing;
    }

    private void RemoveInventory()
    {
        if (_inventory is not null)
        {
            host.Children.Remove(_inventory);
            _inventory = null;
        }
    }
}
