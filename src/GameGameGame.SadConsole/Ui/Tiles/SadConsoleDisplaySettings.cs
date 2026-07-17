using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsoleApp.Ui.Tiles;

internal sealed record SadConsoleDisplaySettings(
    string TilesetId,
    int NativeTileWidth,
    int NativeTileHeight,
    int UiScale,
    int LogicalViewportWidth,
    int LogicalViewportHeight)
{
    public static SadConsoleDisplaySettings Default { get; } = new(
        "Candii",
        NativeTileWidth: 8,
        NativeTileHeight: 8,
        UiScale: 2,
        LogicalViewportWidth: SadConsoleScreenMetrics.ScreenWidth,
        LogicalViewportHeight: SadConsoleScreenMetrics.ScreenHeight);

    public int ScaledTileWidth => NativeTileWidth * UiScale;
    public int ScaledTileHeight => NativeTileHeight * UiScale;
    public int WindowWidthPixels => LogicalViewportWidth * ScaledTileWidth;
    public int WindowHeightPixels => LogicalViewportHeight * ScaledTileHeight;
    public string Summary => $"Tileset: {TilesetId} {NativeTileWidth}x{NativeTileHeight} | Scale: {UiScale}x | Logical: {LogicalViewportWidth}x{LogicalViewportHeight}";

    public SadConsole.IFont.Sizes FontSizePreset => UiScale switch
    {
        <= 1 => SadConsole.IFont.Sizes.One,
        2 => SadConsole.IFont.Sizes.Two,
        3 => SadConsole.IFont.Sizes.Three,
        _ => SadConsole.IFont.Sizes.Four
    };

    public SadConsoleDisplaySettings WithUiScale(int uiScale) => this with { UiScale = Math.Clamp(uiScale, 1, 4) };
}
