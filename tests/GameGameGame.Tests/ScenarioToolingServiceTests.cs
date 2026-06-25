using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Headless;

namespace GameGameGame.Tests;

[Trait(TestSuites.TraitName, TestSuites.Headless)]
public sealed class ScenarioToolingServiceTests
{
    [Fact]
    public void ScenarioRunServiceRunsRootInventoryActorsInInitiativeOrder()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Scenario Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Scenario Room", InventoryWidth: 3, InventoryHeight: 2, Weight: 100, CarryingCapacity: 100),
            new EntityPresentation('#', PresentationColor.Gray));

        var eastWalkerId = editor.CreateEntityPreset("East Walker");
        editor.UpdateEntityPreset(
            eastWalkerId,
            new EntityTemplate("East Walker", InventoryWidth: 0, InventoryHeight: 0, Weight: 1, CarryingCapacity: 1),
            new EntityPresentation('e', PresentationColor.Green));
        editor.SetInitialFacing(eastWalkerId, Direction.East);
        var eastPlanId = editor.CreateActionPlan("East Walker Behavior");
        editor.SetActionPlanBehavior(eastPlanId, [ActionPlanBehaviorStepKind.MoveFacing]);
        editor.SetDefaultActionPlan(eastWalkerId, eastPlanId);

        var southWalkerId = editor.CreateEntityPreset("South Walker");
        editor.UpdateEntityPreset(
            southWalkerId,
            new EntityTemplate("South Walker", InventoryWidth: 0, InventoryHeight: 0, Weight: 1, CarryingCapacity: 1),
            new EntityPresentation('s', PresentationColor.Cyan));
        editor.SetInitialFacing(southWalkerId, Direction.South);
        var southPlanId = editor.CreateActionPlan("South Walker Behavior");
        editor.SetActionPlanBehavior(southPlanId, [ActionPlanBehaviorStepKind.MoveFacing]);
        editor.SetDefaultActionPlan(southWalkerId, southPlanId);

        editor.PlaceCarriedEntity(scenarioRootId, new EntityId("eastWalker"), eastWalkerId, new GridCoord(0, 0));
        editor.PlaceCarriedEntity(scenarioRootId, new EntityId("southWalker"), southWalkerId, new GridCoord(2, 0));

        var report = ScenarioRunService.Run(document, new ScenarioRunRequest(scenarioRootId, TurnCount: 1));

        Assert.Equal([new EntityId("eastWalker"), new EntityId("southWalker")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Equal(["East Walker", "South Walker"], report.Turns.Select(turn => turn.ActorName).ToArray());
        Assert.Contains("East Walker: scenarioRoot(1,0), facing East, target none", report.FinalStateLines);
        Assert.Contains("South Walker: scenarioRoot(2,1), facing South, target none", report.FinalStateLines);
        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
        Assert.Empty(report.CapabilityGaps);
    }

    [Fact]
    public void ScenarioRunServiceShowsBehaviorStepsAndTreatsNoActionAsObservation()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Scenario Duel Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Scenario Duel Room", InventoryWidth: 3, InventoryHeight: 1, Weight: 100, CarryingCapacity: 100),
            new EntityPresentation('#', PresentationColor.Gray));

        var passiveId = editor.CreateEntityPreset("Passive Walker");
        editor.UpdateEntityPreset(
            passiveId,
            new EntityTemplate("Passive Walker", InventoryWidth: 0, InventoryHeight: 0, Weight: 1, CarryingCapacity: 1),
            new EntityPresentation('p', PresentationColor.Green));
        editor.SetInitialFacing(passiveId, Direction.East);
        var passivePlanId = editor.CreateActionPlan("Passive Walker Behavior");
        editor.SetActionPlanBehavior(passivePlanId, [ActionPlanBehaviorStepKind.MoveFacing]);
        editor.SetDefaultActionPlan(passiveId, passivePlanId);

        var destroyerId = editor.CreateEntityPreset("Destroyer Walker");
        editor.UpdateEntityPreset(
            destroyerId,
            new EntityTemplate("Destroyer Walker", InventoryWidth: 0, InventoryHeight: 0, Weight: 1, CarryingCapacity: 1),
            new EntityPresentation('d', PresentationColor.Yellow));
        editor.SetInitialFacing(destroyerId, Direction.West);
        var destroyerPlanId = editor.CreateActionPlan("Destroyer Walker Behavior");
        editor.SetActionPlanBehavior(destroyerPlanId, [ActionPlanBehaviorStepKind.MoveFacing, ActionPlanBehaviorStepKind.DestroyTarget]);
        editor.SetDefaultActionPlan(destroyerId, destroyerPlanId);

        editor.PlaceCarriedEntity(scenarioRootId, new EntityId("passive"), passiveId, new GridCoord(0, 0));
        editor.PlaceCarriedEntity(scenarioRootId, new EntityId("destroyer"), destroyerId, new GridCoord(1, 0));

        var report = ScenarioRunService.Run(document, new ScenarioRunRequest(scenarioRootId, TurnCount: 1));

        Assert.Empty(report.RuntimeFailures);
        Assert.Contains(report.RuntimeObservations, observation => observation.Contains("Passive Walker", StringComparison.Ordinal));
        Assert.Contains(report.Turns[0].TraceLines, line => line.StartsWith("1. MoveFacing: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("1. MoveFacing: Failure", StringComparison.Ordinal));
        Assert.Contains(report.Turns[1].TraceLines, line => line.StartsWith("2. DestroyTarget: Success", StringComparison.Ordinal));
        Assert.Contains("   writes: Target=passive", report.Turns[1].TraceLines);
        Assert.DoesNotContain(report.FinalStateLines, line => line.StartsWith("Passive Walker:", StringComparison.Ordinal));
        Assert.Contains("Destroyer Walker: scenarioRoot(1,0), facing West, target passive", report.FinalStateLines);
    }

    [Fact]
    public void ScenarioMaterializerMaterializesAlphaScenarioWithPlayerInsertion()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Alpha Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Alpha Room", InventoryWidth: 4, InventoryHeight: 3, Weight: 100, CarryingCapacity: 100),
            new EntityPresentation('#', PresentationColor.Gray));

        var playerTemplateId = editor.CreateEntityPreset("Alpha Player");
        editor.UpdateEntityPreset(
            playerTemplateId,
            new EntityTemplate("Alpha Player", InventoryWidth: 0, InventoryHeight: 0, Weight: 1, CarryingCapacity: 5),
            new EntityPresentation('@', PresentationColor.Yellow));
        editor.SetInitialFacing(playerTemplateId, Direction.North);

        var materialization = ScenarioMaterializer.Materialize(document, new ScenarioDefinition(
            ScenarioId: "alpha-smoke",
            Name: "Alpha Smoke",
            ScenarioRootEntityTemplateId: scenarioRootId,
            PlayerEntityTemplateId: playerTemplateId,
            PlayerEntityId: new EntityId("playerOne"),
            PlayerStart: new GridCoord(2, 1)));

        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Equal("alpha-smoke", materialization.ScenarioId);
        Assert.Equal(new EntityId("scenarioRoot"), materialization.ScenarioRootEntityId);
        Assert.Equal(new EntityId("playerOne"), materialization.PlayerEntityId);
        Assert.Equal(new PlaneId("scenarioRoot"), materialization.ScenarioPlaneId);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(2, 1)), materialization.PlayerLocation);
        Assert.Contains("Scenario: alpha-smoke (Alpha Smoke)", materialization.SetupLines);
        Assert.Contains("Player: Alpha Player playerOne at scenarioRoot(2,1), facing North, target none", materialization.SetupLines);
    }

    [Fact]
    public void ScenarioMaterializerReportsAuthoringDiagnostics()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Blocked Alpha Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Blocked Alpha Room", InventoryWidth: 2, InventoryHeight: 1, Weight: 100, CarryingCapacity: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var blockerTemplateId = editor.CreateEntityPreset("Blocker");
        editor.PlaceCarriedEntity(scenarioRootId, new EntityId("blocker"), blockerTemplateId, new GridCoord(0, 0));
        var playerTemplateId = editor.CreateEntityPreset("Blocked Player");

        var missingRoot = ScenarioMaterializer.Materialize(document, new ScenarioDefinition(
            "missing-root",
            "Missing Root",
            new EntityTemplateId("missingRoot"),
            playerTemplateId,
            new EntityId("player"),
            new GridCoord(0, 0)));
        var missingPlayer = ScenarioMaterializer.Materialize(document, new ScenarioDefinition(
            "missing-player",
            "Missing Player",
            scenarioRootId,
            new EntityTemplateId("missingPlayer"),
            new EntityId("player"),
            new GridCoord(0, 0)));
        var invalidStart = ScenarioMaterializer.Materialize(document, new ScenarioDefinition(
            "invalid-start",
            "Invalid Start",
            scenarioRootId,
            playerTemplateId,
            new EntityId("player"),
            new GridCoord(3, 0)));
        var occupiedStart = ScenarioMaterializer.Materialize(document, new ScenarioDefinition(
            "occupied-start",
            "Occupied Start",
            scenarioRootId,
            playerTemplateId,
            new EntityId("player"),
            new GridCoord(0, 0)));

        Assert.Contains(missingRoot.ValidationDiagnostics, diagnostic => diagnostic.Contains("missing scenario root template missingRoot", StringComparison.Ordinal));
        Assert.Contains(missingPlayer.ValidationDiagnostics, diagnostic => diagnostic.Contains("missing player template missingPlayer", StringComparison.Ordinal));
        Assert.Contains(invalidStart.ValidationDiagnostics, diagnostic => diagnostic.Contains("player start scenarioRoot(3,0) is outside scenario plane", StringComparison.Ordinal));
        Assert.Contains(occupiedStart.ValidationDiagnostics, diagnostic => diagnostic.Contains("player start scenarioRoot(0,0) is occupied by blocker", StringComparison.Ordinal));
        Assert.Empty(missingRoot.RuntimeFailures);
        Assert.Empty(missingPlayer.RuntimeFailures);
        Assert.Empty(invalidStart.RuntimeFailures);
        Assert.Empty(occupiedStart.RuntimeFailures);
    }

    [Fact]
    public void ScenarioMaterializerPersistsAndMaterializesAlphaScenarioDefinitionById()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Persisted Alpha Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Persisted Alpha Room", InventoryWidth: 3, InventoryHeight: 2, Weight: 100, CarryingCapacity: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var playerTemplateId = editor.CreateEntityPreset("Persisted Player");
        editor.UpdateEntityPreset(
            playerTemplateId,
            new EntityTemplate("Persisted Player", InventoryWidth: 0, InventoryHeight: 0, Weight: 1, CarryingCapacity: 5),
            new EntityPresentation('@', PresentationColor.Yellow));

        editor.UpsertScenario(new ScenarioDefinition(
            "persisted-alpha",
            "Persisted Alpha",
            scenarioRootId,
            playerTemplateId,
            new EntityId("persistedPlayer"),
            new GridCoord(1, 1)));

        var materialization = ScenarioMaterializer.Materialize(document, "persisted-alpha");

        Assert.True(document.ValidateCanonicalAuthoring().IsValid);
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Equal("persisted-alpha", materialization.ScenarioId);
        Assert.Equal(new EntityId("persistedPlayer"), materialization.PlayerEntityId);
        Assert.Equal(new PlaneCoord(new PlaneId("scenarioRoot"), new GridCoord(1, 1)), materialization.PlayerLocation);
    }

    [Fact]
    public void ScenarioMaterializerValidatesPersistedAlphaScenarioDefinitions()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);

        editor.UpsertScenario(new ScenarioDefinition(
            "broken-alpha",
            "Broken Alpha",
            new EntityTemplateId("missingRoom"),
            new EntityTemplateId("missingPlayer"),
            new EntityId("player"),
            new GridCoord(0, 0)));

        var validation = document.ValidateCanonicalAuthoring();

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Contains("Scenario broken-alpha references missing scenario root template missingRoom", StringComparison.Ordinal));
        Assert.Contains(validation.Errors, error => error.Contains("Scenario broken-alpha references missing player template missingPlayer", StringComparison.Ordinal));
    }
}
