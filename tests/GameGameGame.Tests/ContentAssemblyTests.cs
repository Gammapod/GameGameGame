using GameGameGame.Content;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class ContentAssemblyTests
{
    [Fact]
    public void InspectionDtosAreOwnedByContentAssembly()
    {
        Assert.Equal("GameGameGame.Content", typeof(EntityInspectionPanel).Assembly.GetName().Name);
        Assert.Equal("GameGameGame.Content", typeof(EntityInspectionService).Assembly.GetName().Name);
        Assert.Equal("GameGameGame.Content", typeof(PresentationColor).Assembly.GetName().Name);
    }
}
