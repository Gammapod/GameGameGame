namespace GameGameGame.Frontend.SadConsole;

internal sealed record SadConsoleDisplaySettings(
    string TilesetId,
    int NativeTileWidth,
    int NativeTileHeight,
    int UiScale,
    int StartupWindowWidthPixels,
    int StartupWindowHeightPixels)
{
    public static SadConsoleDisplaySettings FromSettings(FrontendSadConsoleSettings settings) => new(
        "Candii",
        NativeTileWidth: 8,
        NativeTileHeight: 8,
        UiScale: Math.Clamp(settings.UiScale, 1, 4),
        StartupWindowWidthPixels: Math.Max(640, settings.WindowWidthPixels),
        StartupWindowHeightPixels: Math.Max(360, settings.WindowHeightPixels));

    public int ScaledTileWidth => NativeTileWidth * UiScale;
    public int ScaledTileHeight => NativeTileHeight * UiScale;

    public global::SadConsole.IFont.Sizes FontSizePreset => UiScale switch
    {
        <= 1 => global::SadConsole.IFont.Sizes.One,
        2 => global::SadConsole.IFont.Sizes.Two,
        3 => global::SadConsole.IFont.Sizes.Three,
        _ => global::SadConsole.IFont.Sizes.Four
    };
}

internal sealed record FrontendRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width - 1;
    public int Bottom => Y + Height - 1;
    public bool Contains(int x, int y) => x >= X && y >= Y && x <= Right && y <= Bottom;
}

internal sealed record FrontendDisplayShell(
    int PixelWidth,
    int PixelHeight,
    int LogicalWidth,
    int LogicalHeight,
    FrontendRect DrawableBounds)
{
    public static FrontendDisplayShell Resolve(int pixelWidth, int pixelHeight, SadConsoleDisplaySettings displaySettings, int borderCells = 1)
    {
        var logicalWidth = Math.Max(1, pixelWidth / displaySettings.ScaledTileWidth);
        var logicalHeight = Math.Max(1, pixelHeight / displaySettings.ScaledTileHeight);
        var border = Math.Max(0, Math.Min(borderCells, Math.Min(logicalWidth / 2, logicalHeight / 2)));
        var drawable = new FrontendRect(
            border,
            border,
            Math.Max(0, logicalWidth - border * 2),
            Math.Max(0, logicalHeight - border * 2));

        return new FrontendDisplayShell(pixelWidth, pixelHeight, logicalWidth, logicalHeight, drawable);
    }
}
