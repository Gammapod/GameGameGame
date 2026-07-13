using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsoleSessionLayoutTests
{
    [Fact]
    public void BuildPanelChainSlotsReturnsNoSlotsForEmptyChain()
    {
        Assert.Empty(SadConsoleSessionLayout.BuildPanelChainSlots(0));
    }

    [Fact]
    public void BuildPanelChainSlotsUsesFullPanelForSingleEntity()
    {
        var slot = Assert.Single(SadConsoleSessionLayout.BuildPanelChainSlots(1));

        Assert.False(slot.IsCollapsed);
        Assert.Equal(new SadConsoleRect(1, 6, 118, 32), slot.Bounds);
    }

    [Fact]
    public void BuildPanelChainSlotsLaysOutShallowChainsWithoutOverlap()
    {
        var slots = SadConsoleSessionLayout.BuildPanelChainSlots(3);

        Assert.All(slots, slot => Assert.False(slot.IsCollapsed));
        AssertWithinPanelAreaAndNonOverlapping(slots);
    }

    [Fact]
    public void BuildPanelChainSlotsCollapsesRootForLongChains()
    {
        var slots = SadConsoleSessionLayout.BuildPanelChainSlots(4);

        Assert.True(slots[0].IsCollapsed);
        Assert.Equal(14, slots[0].Bounds.Width);
        Assert.All(slots.Skip(1), slot => Assert.False(slot.IsCollapsed));
        AssertWithinPanelAreaAndNonOverlapping(slots);
    }

    private static void AssertWithinPanelAreaAndNonOverlapping(IReadOnlyList<SadConsolePanelSlot> slots)
    {
        var previousRight = 0;
        foreach (var slot in slots)
        {
            Assert.InRange(slot.Bounds.Left, 1, SadConsoleScreenMetrics.ScreenWidth - 1);
            Assert.InRange(slot.Bounds.Left + slot.Bounds.Width, 1, SadConsoleScreenMetrics.ScreenWidth - 1);
            Assert.Equal(6, slot.Bounds.Top);
            Assert.Equal(32, slot.Bounds.Bottom);
            Assert.True(slot.Bounds.Left > previousRight);
            previousRight = slot.Bounds.Left + slot.Bounds.Width - 1;
        }
    }
}
