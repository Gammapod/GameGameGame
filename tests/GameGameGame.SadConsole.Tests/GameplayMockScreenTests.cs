using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.SadConsoleApp;
using GameGameGame.SadConsoleApp.Ui.Screens;

namespace GameGameGame.SadConsole.Tests;

public sealed class GameplayMockScreenTests
{
    [Fact]
    public void StartupRecognizesPlayMockModeWithContentAndScenario()
    {
        var startup = SadConsoleStartup.FromArgs(["--play-mock", "content.yaml", "demo-scenario"]);

        Assert.True(startup.LaunchPlayMock);
        Assert.Equal("content.yaml", startup.DirectContentPath);
        Assert.Equal("demo-scenario", startup.DirectScenarioId);
        Assert.False(startup.LaunchGallery);
        Assert.Null(startup.Catalog);
    }

    [Fact]
    public void FrameUsesPlayerPointOfViewCurrentPlaceAsCenteredViewport()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.Equal(session.PlayerEntityId, frame.PlayerProjection.EntityId);
        Assert.NotNull(frame.PlayerProjection.PointOfView);
        Assert.NotNull(frame.PlayerProjection.PointOfView.CurrentPlace);
        Assert.Equal(session.ActiveContainerEntityId, frame.PlayerProjection.PointOfView.CurrentPlace.EntityId);
        Assert.Equal(session.ActiveContainerEntityId, frame.CurrentPlaceProjection?.EntityId);
        Assert.NotNull(frame.CurrentPlaceProjection?.InventoryGrid);
        Assert.Equal("current-place", frame.Components[0].Id);
        Assert.Equal(0, frame.HudBounds.Left);
        Assert.InRange(frame.HudBounds.Width, 20, 28);
        Assert.True(frame.CurrentPlaceBounds.Left > frame.HudBounds.Left + frame.HudBounds.Width);
        Assert.Equal(0, frame.CurrentPlaceBounds.Top);
        Assert.True(frame.CurrentPlaceBounds.Bottom <= frame.InspectionBounds.Top);
    }

    [Fact]
    public void FrameDrawsPersistentHudRowsAboveMainViewport()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.Contains(frame.HudRows, row => row.Contains("Player:"));
        Assert.Contains(frame.HudRows, row => row.Contains("Current place: Mock Room"));
        Assert.Contains(frame.HudRows, row => row.Contains("no turns advance"));
        Assert.Equal(0, frame.HudBounds.Top);
        Assert.Equal(42, frame.HudBounds.Bottom);
        Assert.True(frame.InspectionBounds.Top >= 28);
    }

    [Fact]
    public void LargerScreenPreservesRelativeGameplayRegions()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(200, 60);

        Assert.InRange(frame.HudBounds.Width, 38, 42);
        Assert.Equal(0, frame.HudBounds.Top);
        Assert.Equal(60, frame.HudBounds.Bottom);
        Assert.Equal(40, frame.InspectionBounds.Top);
        Assert.Equal(60, frame.InspectionBounds.Bottom);
        Assert.Equal(0, frame.CurrentPlaceBounds.Top);
        Assert.Equal(frame.InspectionBounds.Top, frame.CurrentPlaceBounds.Bottom);
        Assert.True(frame.CurrentPlaceBounds.Width > 150);
    }

    [Fact]
    public void InspectionCyclesVisibleNonPlayerEntitiesWithoutAdvancingTurn()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);
        var turn = session.World.TurnNumber;

        var message = screen.InspectNextEntity();
        var frame = screen.BuildFrame(120, 42);

        Assert.Contains("Inspecting Mock Crate", message);
        Assert.Equal(turn, session.World.TurnNumber);
        Assert.Equal(new EntityId("mockCrate"), screen.InspectedEntityId);
        Assert.NotNull(frame.InspectedProjection?.InventoryGrid);
        Assert.Contains(frame.Components, component => component.Id == "inspected-entity");
    }

    private static PlayableScenarioSession CreateGameplayMockSession()
    {
        var document = new EditableContentDocument();
        var crateTemplateId = document.AddEntityTemplate(
            "Mock Crate",
            new EntityTemplate("Mock Crate", InventoryWidth: 2, InventoryHeight: 1, Bulk: 2, Aperture: 2),
            new EntityPresentation('c', PresentationColor.Earth));
        var roomTemplateId = document.AddEntityTemplate(
            "Mock Room",
            new EntityTemplate(
                "Mock Room",
                InventoryWidth: 6,
                InventoryHeight: 4,
                Bulk: 100,
                Aperture: 100,
                CarriedEntities: [new CarriedEntityTemplate(new EntityId("mockCrate"), crateTemplateId, new GridCoord(3, 1))]),
            new EntityPresentation('#', PresentationColor.Gray));
        var playerTemplateId = document.AddEntityTemplate(
            "Mock Player",
            new EntityTemplate("Mock Player", InventoryWidth: 1, InventoryHeight: 1, Bulk: 1, Aperture: 5),
            new EntityPresentation('@', PresentationColor.Yellow));
        document.UpsertScenario(new ScenarioDefinition(
            "play-mock-scenario",
            "Play Mock Scenario",
            roomTemplateId,
            playerTemplateId,
            new EntityId("mockPlayer"),
            new GridCoord(1, 1)));

        return PlayableScenarioLauncher.CreateFromDocument(document, "play-mock-scenario");
    }
}
