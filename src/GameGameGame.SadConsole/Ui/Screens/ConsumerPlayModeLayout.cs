using GameGameGame.SadConsoleApp.Ui.Tiles;
using SadRogue.Primitives;

namespace GameGameGame.SadConsoleApp.Ui.Screens;

internal sealed record ConsumerPlayModeLayout(
    int Width,
    int Height,
    SadConsoleRect RootBounds,
    SadConsoleRect DrawableBounds,
    int BorderGlyph,
    Color BorderForeground,
    Color BorderBackground,
    bool DebugVisible)
{
    public const int BorderBufferThickness = 1;
    public const int BorderBufferGlyph = 181;

    public static ConsumerPlayModeLayout FromDisplaySettings(SadConsoleDisplaySettings displaySettings, bool debugVisible = false) =>
        FromPixels(displaySettings.WindowWidthPixels, displaySettings.WindowHeightPixels, displaySettings, debugVisible);

    public static ConsumerPlayModeLayout FromPixels(int availableWidthPixels, int availableHeightPixels, SadConsoleDisplaySettings displaySettings, bool debugVisible = false)
    {
        var width = Math.Max(3, availableWidthPixels / Math.Max(1, displaySettings.ScaledTileWidth));
        var height = Math.Max(3, availableHeightPixels / Math.Max(1, displaySettings.ScaledTileHeight));
        return FromCellSize(width, height, debugVisible);
    }

    public static ConsumerPlayModeLayout FromCellSize(int width, int height, bool debugVisible = false)
    {
        width = Math.Max(3, width);
        height = Math.Max(3, height);
        var borderColor = debugVisible ? Color.Red : Color.Black;
        return new ConsumerPlayModeLayout(
            width,
            height,
            SadConsoleRect.FromSize(0, 0, width, height),
            SadConsoleRect.FromSize(BorderBufferThickness, BorderBufferThickness, width - 2, height - 2),
            BorderBufferGlyph,
            borderColor,
            Color.Black,
            debugVisible);
    }

    public ConsumerPlayModeLayout WithDebugVisible(bool debugVisible) =>
        FromCellSize(Width, Height, debugVisible);
}
