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

    [Fact]
    public void CurrentPlaceEntityRowsShowFacingAndLabeledTarget()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.Contains(frame.CurrentPlaceEntityRows, row => row.Contains("Mock Crate") && row.Contains("facing East") && row.Contains("loves -> Mock Player"));
    }

    [Fact]
    public void CurrentPlaceEntityRowsShowPlayerPointOfViewTargetAdjectivesWithoutAdvancingTurn()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);
        var turn = session.World.TurnNumber;

        var frame = screen.BuildFrame(120, 42);

        Assert.Equal(turn, session.World.TurnNumber);
        Assert.Contains(frame.CurrentPlaceEntityRows, row => row.Contains("Mock Crate") && row.Contains("adjectives portable, enterable"));
    }

    [Fact]
    public void CurrentPlaceEntityRowsShowPlayerPointOfViewReciprocalAdjectivesWithoutAdvancingTurn()
    {
        var session = CreateGameplayMockSession(includePlayerReciprocalAdjectives: true);
        var screen = new GameplayMockScreen(session);
        var turn = session.World.TurnNumber;

        var frame = screen.BuildFrame(120, 42);

        Assert.Equal(turn, session.World.TurnNumber);
        Assert.Contains(frame.CurrentPlaceEntityRows, row => row.Contains("Mock Crate") && row.Contains("reciprocal portable"));
    }

    [Fact]
    public void CurrentPlaceEntityRowsOmitReciprocalAdjectivesWhenProjectionDoesNotExposeThem()
    {
        var session = CreateGameplayMockSession(includePlayerReciprocalAdjectives: false);
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        var crateRow = Assert.Single(frame.CurrentPlaceEntityRows, row => row.Contains("Mock Crate"));
        Assert.DoesNotContain("reciprocal", crateRow);
    }

    [Fact]
    public void CurrentPlaceEntityRowsOmitTargetAdjectivesWhenProjectionDoesNotExposeThem()
    {
        var session = CreateGameplayMockSession(includePlayerTargetAdjectives: false);
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        var crateRow = Assert.Single(frame.CurrentPlaceEntityRows, row => row.Contains("Mock Crate"));
        Assert.DoesNotContain("portable", crateRow);
        Assert.DoesNotContain("enterable", crateRow);
    }

    [Fact]
    public void InspectedPanelRowsShowTargetingRulesAndActionPlanSteps()
    {
        var session = CreateGameplayMockSession();
        var screen = new GameplayMockScreen(session);

        screen.InspectNextEntity();
        var frame = screen.BuildFrame(120, 42);

        Assert.Contains(frame.InspectedTargetingRows, row => row.Contains("rule loves") && row.Contains("Mock Player"));
        Assert.Contains(frame.InspectedActionPlanRows, row => row.Contains("action plan: mockCratePlan"));
        Assert.Contains(frame.InspectedActionPlanRows, row => row.Contains("SeekTarget loves"));
    }

    [Fact]
    public void CurrentRoomSizeIsLargeWhenPlayerBulkIsLessThanTenPercentOfAperture()
    {
        var session = CreateGameplayMockSession(playerBulk: 5, roomAperture: 100);
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.Equal("Large", frame.CurrentRoomSizeLabel);
    }

    [Fact]
    public void CurrentRoomSizeIsSmallWhenPlayerBulkIsWithinTenPercentOfAperture()
    {
        var session = CreateGameplayMockSession(playerBulk: 95, roomAperture: 100);
        var screen = new GameplayMockScreen(session);

        var frame = screen.BuildFrame(120, 42);

        Assert.Equal("Small", frame.CurrentRoomSizeLabel);
    }

    private static PlayableScenarioSession CreateGameplayMockSession(
        int playerBulk = 1,
        int roomAperture = 100,
        bool includePlayerTargetAdjectives = true,
        bool includePlayerReciprocalAdjectives = false)
    {
        var document = new EditableContentDocument();
        var playerInteractionPlanId = new ActionPlanTemplateId("mockPlayerInteractionPlan");
        document.ActionPlans[playerInteractionPlanId.Value] = EditableContentDocument.ActionPlanDescriptorDto.From(new ActionPlanDescriptor(
            new ActionPlanId(playerInteractionPlanId.Value),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget),
                new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.EnterTarget)
            ])));
        var playerTemplateId = document.AddEntityTemplate(
            "Mock Player",
            new EntityTemplate(
                "Mock Player",
                InventoryWidth: 1,
                InventoryHeight: 1,
                Bulk: playerBulk,
                Aperture: 5,
                DefaultActionPlanId: includePlayerTargetAdjectives ? playerInteractionPlanId : null),
            new EntityPresentation('@', PresentationColor.Yellow));
        var cratePlanId = new ActionPlanTemplateId("mockCratePlan");
        var crateSteps = new List<ActionPlanBehaviorStepDescriptor>
        {
            new(ActionPlanBehaviorStepKind.SeekTarget, TargetLabel: "loves"),
            new(ActionPlanBehaviorStepKind.MaintainChebyshevDistanceTwo, TargetLabel: "loves")
        };
        if (includePlayerReciprocalAdjectives)
        {
            crateSteps.Add(new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.PickupTarget, TargetLabel: "loves"));
        }

        document.ActionPlans[cratePlanId.Value] = EditableContentDocument.ActionPlanDescriptorDto.From(new ActionPlanDescriptor(
            new ActionPlanId(cratePlanId.Value),
            [],
            Behavior: new ActionPlanBehaviorDescriptor(crateSteps)));
        var crateTemplateId = document.AddEntityTemplate(
            "Mock Crate",
            new EntityTemplate(
                "Mock Crate",
                InventoryWidth: 2,
                InventoryHeight: 1,
                Bulk: 2,
                Aperture: 2,
                DefaultActionPlanId: cratePlanId,
                ActionStateDefaults: new ActorActionStateDefaults(Direction.East),
                TargetingRules: [new EntityTargetingRule(1, playerTemplateId, Range: 10, Label: "loves")]),
            new EntityPresentation('c', PresentationColor.Earth));
        var roomTemplateId = document.AddEntityTemplate(
            "Mock Room",
            new EntityTemplate(
                "Mock Room",
                InventoryWidth: 6,
                InventoryHeight: 4,
                Bulk: 100,
                Aperture: roomAperture,
                CarriedEntities: [new CarriedEntityTemplate(new EntityId("mockCrate"), crateTemplateId, new GridCoord(3, 1))]),
            new EntityPresentation('#', PresentationColor.Gray));
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
