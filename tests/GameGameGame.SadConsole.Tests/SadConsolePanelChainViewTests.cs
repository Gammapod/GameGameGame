using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp;

namespace GameGameGame.SadConsole.Tests;

public sealed class SadConsolePanelChainViewTests
{
    [Fact]
    public void BuildPanelChainFallsBackToRequestedEntityWhenBreadcrumbHasNoSegments()
    {
        var requested = new EntityId("requested");
        var chain = SadConsolePanelChainViewBuilder.BuildPanelChain(new EntityContainmentPath(requested, EntityContainmentPathStatus.Complete, [], [], []));

        Assert.Equal([requested], chain);
    }

    [Fact]
    public void BuildVisiblePanelChainKeepsShallowChainsIntact()
    {
        var chain = EntityIds("root", "room", "bag", "gem");

        Assert.Equal(chain, SadConsolePanelChainViewBuilder.BuildVisiblePanelChain(chain));
    }

    [Fact]
    public void BuildVisiblePanelChainKeepsRootAndLastThreeForLongChains()
    {
        var chain = EntityIds("root", "floor", "room", "bag", "pouch", "gem");

        var visible = SadConsolePanelChainViewBuilder.BuildVisiblePanelChain(chain);

        Assert.Equal(EntityIds("root", "bag", "pouch", "gem"), visible);
    }

    [Fact]
    public void PanelTitleShowsCurrentContainerInspectionAndOmittedRootCount()
    {
        var root = new EntityId("root");
        var container = new EntityId("bag");
        var inspected = new EntityId("gem");

        Assert.Equal("Root (+2)", SadConsolePanelChainViewBuilder.PanelTitle(0, root, container, inspected, omittedCount: 2));
        Assert.Equal("Current Container", SadConsolePanelChainViewBuilder.PanelTitle(1, container, container, inspected, omittedCount: 2));
        Assert.Equal("Inspection", SadConsolePanelChainViewBuilder.PanelTitle(2, inspected, container, inspected, omittedCount: 2));
    }

    private static IReadOnlyList<EntityId> EntityIds(params string[] ids) => ids.Select(id => new EntityId(id)).ToList();
}
