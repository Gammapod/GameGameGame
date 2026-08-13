using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class OverlayPanelGeometryTests
{
    [Fact]
    public void OverlayPanelGeometryAppliesHalfTilePixelOffset()
    {
        var display = SadConsoleDisplaySettings.FromSettings(FrontendSadConsoleSettings.Default);
        var bounds = new FrontendRect(3, 5, 20, 8);

        var geometry = OverlayPanelGeometry.HalfTileOffset(bounds, display);

        Assert.Equal(display.ScaledTileWidth / 2, geometry.PixelOffsetX);
        Assert.Equal(display.ScaledTileHeight / 2, geometry.PixelOffsetY);
        Assert.Equal(bounds.X * display.ScaledTileWidth + display.ScaledTileWidth / 2, geometry.PixelX);
        Assert.Equal(bounds.Y * display.ScaledTileHeight + display.ScaledTileHeight / 2, geometry.PixelY);
    }
}
