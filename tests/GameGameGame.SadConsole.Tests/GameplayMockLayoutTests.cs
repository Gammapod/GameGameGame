using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Screens;

namespace GameGameGame.SadConsole.Tests;

public sealed class GameplayMockLayoutTests
{
    [Fact]
    public void ResolvePreservesDefaultPlayModeBounds()
    {
        var layout = GameplayMockLayout.Resolve(120, 42);

        Assert.Equal(new SadConsoleRect(0, 0, 120, 42), layout.RootBounds);
        Assert.Equal(new SadConsoleRect(0, 0, 24, 42), layout.HudBounds);
        Assert.Equal(new SadConsoleRect(25, 0, 94, 28), layout.CurrentPlaceBounds);
        Assert.Equal(new SadConsoleRect(25, 28, 94, 42), layout.InspectionBounds);
        Assert.Equal(new SadConsoleRect(27, 2, 38, 12), layout.ActionSelectorBounds);
        Assert.Equal(new SadConsoleRect(25, 21, 60, 27), layout.DiagnosticsBounds);
    }

    [Fact]
    public void ResolveClampsSmallViewportToExistingMinimumBehavior()
    {
        var layout = GameplayMockLayout.Resolve(20, 10);

        Assert.Equal(40, layout.Width);
        Assert.Equal(18, layout.Height);
        Assert.Equal(new SadConsoleRect(0, 0, 20, 18), layout.HudBounds);
        Assert.Equal(new SadConsoleRect(21, 0, 20, 10), layout.CurrentPlaceBounds);
        Assert.Equal(new SadConsoleRect(21, 10, 20, 18), layout.InspectionBounds);
    }

    [Fact]
    public void ResolveScalesLargerViewportWithStableLayers()
    {
        var layout = GameplayMockLayout.Resolve(160, 60);

        Assert.Equal(new SadConsoleRect(0, 0, 160, 60), layout.RootBounds);
        Assert.Equal(new SadConsoleRect(0, 0, 32, 60), layout.HudBounds);
        Assert.Equal(new SadConsoleRect(33, 0, 126, 40), layout.CurrentPlaceBounds);
        Assert.Equal(new SadConsoleRect(33, 40, 126, 60), layout.InspectionBounds);
        Assert.True(layout.ActionSelector.Layer > layout.CurrentPlace.Layer);
        Assert.True(layout.Diagnostics.Layer > layout.ActionSelector.Layer);
    }

    [Fact]
    public void ResolveUsesFloorRoundedOneToFourHorizontalSplit()
    {
        var layout = GameplayMockLayout.Resolve(121, 42);

        Assert.Equal(new SadConsoleRect(0, 0, 24, 42), layout.HudBounds);
        Assert.Equal(new SadConsoleRect(25, 0, 95, 28), layout.CurrentPlaceBounds);
    }

    [Fact]
    public void ResolveUsesTwoToOneVerticalSplitWithMinimumInspectionHeight()
    {
        var layout = GameplayMockLayout.Resolve(120, 24);

        Assert.Equal(new SadConsoleRect(25, 0, 94, 16), layout.CurrentPlaceBounds);
        Assert.Equal(new SadConsoleRect(25, 16, 94, 24), layout.InspectionBounds);
    }

    [Fact]
    public void ResolveExposesExpectedNamedRegions()
    {
        var regions = GameplayMockLayout.Resolve(120, 42).Regions;

        Assert.Equal(["0", "0.1", "0.2", "0.3", "0.2.1", "0.diagnostics"], regions.Select(region => region.Id));
    }

    [Fact]
    public void HitTestChoosesTopmostLayerAtCell()
    {
        var layout = GameplayMockLayout.Resolve(120, 42);

        var hit = GameplayMockLayout.HitTest(layout, 27, 22);

        Assert.NotNull(hit);
        Assert.Equal("0.diagnostics", hit.Region.Id);
        Assert.Equal(2, hit.LocalX);
        Assert.Equal(1, hit.LocalY);
    }

    [Fact]
    public void HitTestReturnsLocalCoordinatesForActionOverlay()
    {
        var layout = GameplayMockLayout.Resolve(120, 42);

        var hit = GameplayMockLayout.HitTest(layout, 28, 3);

        Assert.NotNull(hit);
        Assert.Equal("0.2.1", hit.Region.Id);
        Assert.Equal(1, hit.LocalX);
        Assert.Equal(1, hit.LocalY);
    }
}
