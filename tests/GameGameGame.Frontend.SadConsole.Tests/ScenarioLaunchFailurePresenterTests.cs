using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class ScenarioLaunchFailurePresenterTests
{
    [Fact]
    public void LaunchFailurePresenterShowsValidationDiagnosticsWhenScenarioCannotPlay()
    {
        var session = new PlayableScenarioSession(
            "beta-acquire-target-showcase",
            "Acquire Target Showcase",
            new WorldState(),
            new PrototypeContentRegistry(
                new Dictionary<EntityTemplateId, EntityTemplate>(),
                new Dictionary<ActionPlanTemplateId, ActionPlanDescriptor>(),
                new Dictionary<EntityTemplateId, EntityPresentation>()),
            new Dictionary<EntityId, IEntityActionPlan>(),
            new EntityId("player"),
            new PlaneId("plane"),
            new EntityId("room"),
            CanPlay: false,
            ["Unsupported/deprecated content field targetPolicy."],
            [],
            []);

        var presentation = ScenarioLaunchFailurePresenter.FromSession(session);

        Assert.Contains("Cannot play Acquire Target Showcase", presentation.Summary);
        Assert.Contains("Validation: Unsupported/deprecated content field targetPolicy.", presentation.Summary);
        Assert.Equal(["Validation: Unsupported/deprecated content field targetPolicy."], presentation.Details);
    }

    [Fact]
    public void LaunchFailurePresenterShowsExceptionMessageWhenLaunchThrows()
    {
        var entry = new WorkspaceScenarioCatalogEntry(
            "file:broken:scenario",
            "broken-scenario",
            "Broken Scenario",
            null,
            null,
            [],
            null,
            null,
            WorkspaceScenarioLaunchKind.File);

        var presentation = ScenarioLaunchFailurePresenter.FromException(entry, new InvalidOperationException("Missing template rat."));

        Assert.Equal("Launch failed for Broken Scenario: Missing template rat.", presentation.Summary);
        Assert.Equal(["Exception: Missing template rat."], presentation.Details);
    }
}
