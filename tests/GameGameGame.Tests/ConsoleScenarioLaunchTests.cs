using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.ConsoleApp;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Console)]
public sealed class ConsoleScenarioLaunchTests
{
    [Fact]
    public void ConsoleScenarioLauncherBuildsPlayableSessionFromPersistedScenario()
    {
        var document = new EditableContentDocument();
        var roomId = document.AddEntityTemplate(
            "Console Alpha Room",
            new EntityTemplate("Console Alpha Room", InventoryWidth: 3, InventoryHeight: 2, Weight: 100, CarryingCapacity: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var playerId = document.AddEntityTemplate(
            "Console Player",
            new EntityTemplate(
                "Console Player",
                InventoryWidth: 1,
                InventoryHeight: 1,
                Weight: 1,
                CarryingCapacity: 5,
                ActionStateDefaults: new ActorActionStateDefaults(Direction.East)),
            new EntityPresentation('@', PresentationColor.Yellow));
        document.UpsertScenario(new ScenarioDefinition(
            "console-alpha",
            "Console Alpha",
            roomId,
            playerId,
            new EntityId("consolePlayer"),
            new GridCoord(1, 0)));

        var session = ConsoleScenarioLauncher.CreateFromDocument(document, "console-alpha");

        Assert.Equal("console-alpha", session.ScenarioId);
        Assert.Equal(new EntityId("consolePlayer"), session.PlayerEntityId);
        Assert.Equal(new PlaneId("scenarioRoot"), session.ActivePlaneId);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(1, 0)), session.World.GetEntityLocation(session.PlayerEntityId));
        Assert.NotNull(session.World.GetInventoryPlaneId(session.PlayerEntityId));
        Assert.Empty(session.ValidationDiagnostics);
        Assert.Empty(session.RuntimeFailures);
    }
}
