using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Editor)]
public sealed class FrontendEditorServiceAgentApiTests
{
    [Fact]
    public void FrontendEditorServiceAndAgentApiShareContentEditorSessionAsParallelSurfaces()
    {
        var session = ContentEditorSession.CreateNew();
        var rockId = session.Editor.CreateEntityPreset("Rock");
        var frontend = new FrontendEditorService(session);
        var agent = new AgentContentEditorApi(session);

        session.Editor.UpdateEntityPreset(
            rockId,
            session.Editor.GetEntityPreset(rockId).Template with { Name = "Agent Visible Rock" },
            new EntityPresentation('R', PresentationColor.White));

        var frontendSnapshot = frontend.GetSnapshot();
        var agentSnapshot = agent.GetDocumentSnapshot();

        Assert.Contains(frontendSnapshot.EntityTemplates, template => template.TemplateId == rockId.Value && template.Name == "Agent Visible Rock");
        Assert.True(frontendSnapshot.IsDirty);
        Assert.True(agentSnapshot.IsDirty);
        Assert.Contains("Agent Visible Rock", agentSnapshot.YamlPreview);
    }
}
