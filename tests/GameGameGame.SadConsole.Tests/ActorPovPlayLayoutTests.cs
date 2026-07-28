using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Screens;

namespace GameGameGame.SadConsole.Tests;

public sealed class ActorPovPlayLayoutTests
{
    [Fact]
    public void ResolveExposesExpectedNamedRegionsWithStableLayers()
    {
        var layout = ActorPovPlayLayoutResolver.Resolve(SadConsoleRect.FromSize(1, 1, 120, 40));

        Assert.Equal(
            [
                ActorPovPlayRegionIds.Root,
                ActorPovPlayRegionIds.ParentChain,
                ActorPovPlayRegionIds.CurrentPlace,
                ActorPovPlayRegionIds.WorldInspection,
                ActorPovPlayRegionIds.ActorInventory,
                ActorPovPlayRegionIds.ActorInventoryInspection,
                ActorPovPlayRegionIds.Chrome,
                ActorPovPlayRegionIds.Connectors,
                ActorPovPlayRegionIds.Diagnostics
            ],
            layout.Regions.Select(region => region.Id));
        Assert.True(layout.Chrome.Layer > layout.CurrentPlace.Layer);
        Assert.True(layout.Connectors.Layer > layout.Chrome.Layer);
        Assert.True(layout.DiagnosticsRegion.Layer > layout.Connectors.Layer);
    }

    [Fact]
    public void ResolveKeepsAllConcreteRegionsInsideDrawableBounds()
    {
        var drawable = SadConsoleRect.FromSize(1, 1, 118, 38);

        var layout = ActorPovPlayLayoutResolver.Resolve(drawable);

        Assert.Equal(drawable, layout.DrawableBounds);
        Assert.All(layout.Regions.Where(region => !region.IsOmitted), region => AssertInside(drawable, region.Bounds));
    }

    [Fact]
    public void ResolveCentersCurrentPovAsSquareWithinUpperWorldArea()
    {
        var drawable = SadConsoleRect.FromSize(1, 1, 118, 38);

        var layout = ActorPovPlayLayoutResolver.Resolve(drawable);

        var current = layout.CurrentPlace.Bounds;

        Assert.Equal(current.Width, current.Height);
        Assert.Equal(29, current.Width);
        Assert.Equal(drawable.Left + ((drawable.Width - current.Width) / 2), current.Left);
        Assert.True(layout.ParentChain.Bounds.Left < current.Left);
        Assert.True(layout.WorldInspection.Bounds.Left > current.Left);
    }

    [Fact]
    public void ResolveReservesBottomBandForActorInventoryAndCarriedInspection()
    {
        var drawable = SadConsoleRect.FromSize(1, 1, 118, 38);

        var layout = ActorPovPlayLayoutResolver.Resolve(drawable);

        Assert.Equal(layout.ActorInventory.Bounds.Top, layout.ActorInventoryInspection.Bounds.Top);
        Assert.Equal(layout.ActorInventory.Bounds.Bottom, layout.ActorInventoryInspection.Bounds.Bottom);
        Assert.True(layout.ActorInventory.Bounds.Top > layout.CurrentPlace.Bounds.Bottom);
        Assert.Equal(drawable.Bottom, layout.ActorInventory.Bounds.Bottom);
        Assert.True(layout.ActorInventory.Bounds.Width > 0);
        Assert.True(layout.ActorInventoryInspection.Bounds.Width > 0);
    }

    [Fact]
    public void ResolveOmitsContentRegionsForTooSmallDrawableBounds()
    {
        var drawable = SadConsoleRect.FromSize(1, 1, 20, 10);

        var layout = ActorPovPlayLayoutResolver.Resolve(drawable);

        Assert.Contains(layout.Diagnostics, diagnostic => diagnostic.Code == "actor-pov.layout.too-small");
        Assert.All(
            layout.Regions.Where(region => region.Role == ActorPovPlayRegionRole.Content),
            region =>
            {
                Assert.True(region.IsOmitted);
                AssertInside(drawable, region.Bounds);
            });
        AssertInside(drawable, layout.DiagnosticsRegion.Bounds);
    }

    [Fact]
    public void ResolveUsesDrawableBoundsWithoutConsumingPlayModeBorderBuffer()
    {
        var playLayout = ConsumerPlayModeLayout.FromCellSize(120, 42);

        var actorPov = ActorPovPlayLayoutResolver.Resolve(playLayout.DrawableBounds);

        Assert.Equal(playLayout.DrawableBounds, actorPov.Root.Bounds);
        Assert.All(actorPov.Regions.Where(region => !region.IsOmitted), region => AssertInside(playLayout.DrawableBounds, region.Bounds));
    }

    private static void AssertInside(SadConsoleRect outer, SadConsoleRect inner)
    {
        Assert.True(inner.Left >= outer.Left, $"{inner} starts left of {outer}");
        Assert.True(inner.Top >= outer.Top, $"{inner} starts above {outer}");
        Assert.True(inner.Left + inner.Width <= outer.Left + outer.Width, $"{inner} extends right of {outer}");
        Assert.True(inner.Bottom <= outer.Bottom, $"{inner} extends below {outer}");
    }
}
