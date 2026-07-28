using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Screens;

namespace GameGameGame.SadConsole.Tests;

public sealed class ActorPovPlayLayoutTests
{
    [Fact]
    public void ActorPovPlayLayoutReservesBottomThirdForInventoryChain()
    {
        var layout = ActorPovPlayLayout.Resolve(SadConsoleRect.FromSize(1, 1, 99, 36));

        Assert.Equal(SadConsoleRect.FromSize(1, 1, 99, 23), layout.WorldRegion);
        Assert.Equal(SadConsoleRect.FromSize(1, 25, 99, 12), layout.InventoryChainRegion);
        Assert.Equal(24, layout.HorizontalSeparatorY);
    }

    [Fact]
    public void ActorPovPlayLayoutCentersSquareCurrentPovInUpperWorldRegion()
    {
        var layout = ActorPovPlayLayout.Resolve(SadConsoleRect.FromSize(1, 1, 99, 36));

        Assert.Equal(23, layout.CurrentPovRegion.Width);
        Assert.Equal(23, layout.CurrentPovRegion.Height);
        Assert.Equal(39, layout.CurrentPovRegion.Left);
        Assert.Equal(1, layout.CurrentPovRegion.Top);
        Assert.Equal(38, layout.ParentCurrentSeparatorX);
        Assert.Equal(62, layout.CurrentInspectionSeparatorX);
        Assert.Equal(SadConsoleRect.FromSize(1, 1, 37, 23), layout.ParentChainRegion);
        Assert.Equal(SadConsoleRect.FromSize(63, 1, 37, 23), layout.InspectionChainRegion);
    }

    [Fact]
    public void ActorPovPlayLayoutHandlesNarrowDrawableBoundsWithoutNegativeSideRegions()
    {
        var layout = ActorPovPlayLayout.Resolve(SadConsoleRect.FromSize(1, 1, 12, 30));

        Assert.Equal(12, layout.CurrentPovRegion.Width);
        Assert.Equal(12, layout.CurrentPovRegion.Height);
        Assert.Equal(0, layout.ParentChainRegion.Width);
        Assert.Equal(0, layout.InspectionChainRegion.Width);
        Assert.Equal(10, layout.InventoryChainRegion.Height);
    }

    [Fact]
    public void ActorPovPlayLayoutExposesCandiiChromeGlyphsForRegionSplits()
    {
        var layout = ActorPovPlayLayout.Resolve(SadConsoleRect.FromSize(1, 1, 99, 36));
        var chrome = layout.ChromeCells();

        Assert.Equal(158, ActorPovPlayLayout.HorizontalLineGlyph);
        Assert.Equal(141, ActorPovPlayLayout.VerticalLineGlyph);
        Assert.Equal(155, ActorPovPlayLayout.HorizontalWithVerticalOffshootGlyph);
        Assert.Contains(chrome, cell => cell.X == 1 && cell.Y == layout.HorizontalSeparatorY && cell.Glyph == ActorPovPlayLayout.HorizontalLineGlyph);
        Assert.Contains(chrome, cell => cell.X == layout.ParentCurrentSeparatorX && cell.Y == layout.WorldRegion.Top && cell.Glyph == ActorPovPlayLayout.VerticalLineGlyph);
        Assert.Contains(chrome, cell => cell.X == layout.CurrentInspectionSeparatorX && cell.Y == layout.HorizontalSeparatorY && cell.Glyph == ActorPovPlayLayout.HorizontalWithVerticalOffshootGlyph);
    }
}
