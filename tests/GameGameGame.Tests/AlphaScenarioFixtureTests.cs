using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.ConsoleApp;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class AlphaScenarioFixtureTests
{
    [Fact]
    public void AlphaScenarioFixtureLoadsValidatesAndMaterializesPlayer()
    {
        var document = AlphaScenarioContent.LoadDocument();

        var validation = document.ValidateCanonicalAuthoring();
        var materialization = ScenarioMaterializer.Materialize(document, AlphaScenarioContent.DefaultScenarioId);

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.True(materialization.CanPlay, string.Join(Environment.NewLine, materialization.ValidationDiagnostics));
        Assert.Equal(new EntityId("alphaPlayer"), materialization.PlayerEntityId);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(1, 1)), materialization.PlayerLocation);
    }

    [Fact]
    public void AlphaScenarioFixtureCanLaunchInConsoleAndAcceptPlayerMove()
    {
        var session = ConsoleScenarioLauncher.CreateFromDocument(
            AlphaScenarioContent.LoadDocument(),
            AlphaScenarioContent.DefaultScenarioId);
        var movement = new MovementService();
        var turns = new TurnService(movement, session.ActionPlans);

        var acted = turns.TakeActorTurnThenAdvance(
            session.World,
            session.PlayerEntityId,
            PlannedActionPlan.Single(new MoveAction(Direction.North)));

        Assert.True(acted);
        Assert.Equal(new PlaneCoord(session.ActivePlaneId, new GridCoord(1, 0)), session.World.GetEntityLocation(session.PlayerEntityId));
    }
}
