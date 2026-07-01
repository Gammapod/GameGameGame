using GameGameGame.Content;
using GameGameGame.Core;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Content)]
public sealed class EntityPanelProjectionTests
{
    [Fact]
    public void EntityPanelProjectionCombinesIdentityPathStateGridAndContents()
    {
        var document = CreateProjectionDocument();
        var session = PlayableScenarioLauncher.CreateFromDocument(document, "panel-projection");
        session.World.SetActionFacing(session.PlayerEntityId, Direction.East);
        var service = new EntityPanelProjectionService(entityId => session.Registry.GetPresentationForEntity(entityId).ToInspectionAppearance());

        var projection = service.Project(
            session.World,
            session.ActiveContainerEntityId,
            session.ActionPlans,
            playerId: session.PlayerEntityId);

        Assert.Equal(session.ActiveContainerEntityId, projection.EntityId);
        Assert.Equal("Projection Room", projection.Name);
        Assert.Equal('#', projection.Glyph);
        Assert.Equal(PresentationColor.Gray, projection.Color);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioHost"), new GridCoord(0, 0)), projection.Location);
        Assert.Equal(EntityContainmentPathStatus.Complete, projection.Breadcrumb.Status);
        Assert.NotNull(projection.InventoryGrid);
        Assert.Contains(projection.Properties, property => property.Name == "Bulk" && property.Value == "100");
        Assert.Contains(projection.Contents, row => row.EntityId == session.PlayerEntityId && row.Participation == LocalTurnParticipation.Player);
        Assert.Contains(projection.Contents, row => row.EntityName == "Projection Crate" && row.Participation == LocalTurnParticipation.Inert);
    }

    [Fact]
    public void EntityPanelProjectionIncludesStructuredLocalLogSnippetsWhenAnchored()
    {
        var document = CreateProjectionDocument();
        var session = PlayableScenarioLauncher.CreateFromDocument(document, "panel-projection");
        var service = new EntityPanelProjectionService(entityId => session.Registry.GetPresentationForEntity(entityId).ToInspectionAppearance());
        var commandService = new ControlledActorCommandService(new MovementService(), session.ActionPlans);
        var crateId = new EntityId("projectionCrate");
        var destination = new PlaneCoord(session.World.GetInventoryPlaneId(session.PlayerEntityId)!.Value, new GridCoord(0, 0));
        var outcome = ActionOutcomeProjection.FromCommandResult(
            session.World,
            commandService.Execute(session.World, session.PlayerEntityId, ControlledActorCommand.Pickup(crateId, destination)));
        var log = ActionLogProjection.FromOutcomes([outcome]);

        var playerProjection = service.Project(
            session.World,
            session.PlayerEntityId,
            session.ActionPlans,
            playerId: session.PlayerEntityId,
            actionLog: log);

        Assert.Contains(playerProjection.LocalLog, entry => entry.Sentence == "Projection Player picked up Projection Crate");
        Assert.Contains(playerProjection.Contents, row => row.EntityId == crateId && row.PreviousAction == "Projection Player picked up Projection Crate");
    }

    private static EditableContentDocument CreateProjectionDocument()
    {
        var document = new EditableContentDocument();
        var crateTemplateId = document.AddEntityTemplate(
            "Projection Crate",
            new EntityTemplate("Projection Crate", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 0),
            new EntityPresentation('c', PresentationColor.Earth));
        var roomId = document.AddEntityTemplate(
            "Projection Room",
            new EntityTemplate(
                "Projection Room",
                InventoryWidth: 3,
                InventoryHeight: 2,
                Bulk: 100,
                Aperture: 100,
                CarriedEntities: [new CarriedEntityTemplate(new EntityId("projectionCrate"), crateTemplateId, new GridCoord(1, 0))]),
            new EntityPresentation('#', PresentationColor.Gray));
        var playerTemplateId = document.AddEntityTemplate(
            "Projection Player",
            new EntityTemplate("Projection Player", InventoryWidth: 1, InventoryHeight: 1, Bulk: 1, Aperture: 5),
            new EntityPresentation('@', PresentationColor.Yellow));
        document.UpsertScenario(new ScenarioDefinition(
            "panel-projection",
            "Panel Projection",
            roomId,
            playerTemplateId,
            new EntityId("projectionPlayer"),
            new GridCoord(0, 0)));
        return document;
    }
}
