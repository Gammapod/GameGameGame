using GameGameGame.Content;
using GameGameGame.Core;

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
    public void AlphaScenarioFixtureCanLaunchPlayableSessionAndAcceptPlayerMove()
    {
        var session = PlayableScenarioLauncher.CreateFromDocument(
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

    [Fact]
    public void ScenarioMaterializerSupportsRootOnlyScenarioCompatibility()
    {
        var document = new EditableContentDocument();
        var roomId = document.AddEntityTemplate(
            "Root Only Room",
            new EntityTemplate("Root Only Room", InventoryWidth: 2, InventoryHeight: 2, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));

        var materialization = ScenarioMaterializer.MaterializeRootOnly(
            document,
            "root-only",
            "Root Only",
            roomId,
            ScenarioMaterializer.DefaultScenarioRootEntityId,
            ScenarioMaterializer.DefaultScenarioPlaneId);

        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.False(materialization.CanPlay);
        Assert.Null(materialization.PlayerEntityId);
        Assert.Null(materialization.PlayerLocation);
        Assert.Equal(ScenarioMaterializer.DefaultScenarioPlaneId, materialization.ScenarioPlaneId);
        Assert.Equal(ScenarioMaterializer.DefaultScenarioRootEntityId, materialization.ScenarioRootEntityId);
        Assert.Equal(ScenarioMaterializer.DefaultScenarioPlaneId, materialization.World.GetInventoryPlaneId(materialization.ScenarioRootEntityId));
        Assert.Contains("Scenario: root-only (Root Only)", materialization.SetupLines);
    }
}
