using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class ScenarioBrowserLayoutTests
{
    [Fact]
    public void ScenarioBrowserLayoutKeepsTextRegionsInsideDrawableBounds()
    {
        var bounds = new FrontendRect(1, 1, 78, 43);

        var layout = ScenarioBrowserLayout.Resolve(bounds);

        Assert.Equal(2, layout.TextX);
        Assert.True(layout.TextX > bounds.X);
        Assert.True(layout.TextX + layout.TextWidth - 1 < bounds.Right);
        Assert.InRange(layout.TitleY, bounds.Y, bounds.Bottom);
        Assert.InRange(layout.SummaryY, bounds.Y, bounds.Bottom);
        Assert.InRange(layout.HeadingY, bounds.Y, bounds.Bottom);
        Assert.InRange(layout.ListY, bounds.Y, bounds.Bottom);
        Assert.InRange(layout.MessageY, bounds.Y, bounds.Bottom);
        Assert.InRange(layout.FooterY, bounds.Y, bounds.Bottom);
        Assert.True(layout.ListY + layout.ListHeight < layout.MessageY);
    }
}
