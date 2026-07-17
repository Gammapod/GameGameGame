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
            new EntityTemplate("Scenario Room", InventoryWidth: 3, InventoryHeight: 2, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));

        var eastWalkerId = editor.CreateEntityPreset("East Walker");
        editor.UpdateEntityPreset(
            eastWalkerId,
            new EntityTemplate("East Walker", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 1),
            new EntityPresentation('e', PresentationColor.Green));
        editor.SetInitialFacing(eastWalkerId, Direction.East);
        var eastPlanId = editor.CreateActionPlan("East Walker Behavior");
        editor.SetActionPlanBehavior(eastPlanId, [ActionPlanBehaviorStepKind.MoveFacing]);
        editor.SetDefaultActionPlan(eastWalkerId, eastPlanId);

        var southWalkerId = editor.CreateEntityPreset("South Walker");
        editor.UpdateEntityPreset(
            southWalkerId,
            new EntityTemplate("South Walker", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 1),
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
    public void ScenarioRunServiceRunsNestedScenarioRootInventoryActors()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Nested Actor Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Nested Actor Room", InventoryWidth: 2, InventoryHeight: 1, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));

        var containerId = editor.CreateEntityPreset("Nested Actor Container");
        editor.UpdateEntityPreset(
            containerId,
            new EntityTemplate("Nested Actor Container", InventoryWidth: 1, InventoryHeight: 1, Bulk: 1, Aperture: 10),
            new EntityPresentation('c', PresentationColor.Cyan));

        var nestedActorId = editor.CreateEntityPreset("Nested Walker");
        editor.UpdateEntityPreset(
            nestedActorId,
            new EntityTemplate("Nested Walker", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 1),
            new EntityPresentation('n', PresentationColor.Green));
        editor.SetInitialFacing(nestedActorId, Direction.North);
        var nestedPlanId = editor.CreateActionPlan("Nested Walker Behavior");
        editor.SetActionPlanBehavior(nestedPlanId, [ActionPlanBehaviorStepKind.MoveFacing]);
        editor.SetDefaultActionPlan(nestedActorId, nestedPlanId);

        editor.PlaceCarriedEntity(containerId, new EntityId("nestedActor"), nestedActorId, new GridCoord(0, 0));
        editor.PlaceCarriedEntity(scenarioRootId, new EntityId("nestedContainer"), containerId, new GridCoord(0, 0));

        var report = ScenarioRunService.Run(document, new ScenarioRunRequest(scenarioRootId, TurnCount: 1));

        Assert.Equal([new EntityId("nestedActor")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Equal(["Nested Walker"], report.Turns.Select(turn => turn.ActorName).ToArray());
        Assert.Contains("Nested Actor Container inventory:", report.InventorySummaryLines);
        Assert.Contains("  - Nested Walker nestedActor at (0,0)", report.InventorySummaryLines);
        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
    }

    [Fact]
    public void ScenarioRunServiceLabelsRootOnlyCompatibilityRuns()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Root Only Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Root Only Room", InventoryWidth: 1, InventoryHeight: 1, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));

        var report = ScenarioRunService.Run(document, new ScenarioRunRequest(scenarioRootId, TurnCount: 0));

        Assert.Contains("Run mode: Root-only compatibility simulation", report.SetupLines);
        Assert.Contains("Scenario: legacy-run (Legacy RunScenario)", report.SetupLines);
    }

    [Fact]
    public void ScenarioRunServiceRunsPersistedScenarioByIdWithInsertedPlayer()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Persisted Run Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Persisted Run Room", InventoryWidth: 4, InventoryHeight: 3, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));

        var playerTemplateId = editor.CreateEntityPreset("Persisted Runner");
        editor.UpdateEntityPreset(
            playerTemplateId,
            new EntityTemplate("Persisted Runner", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 5),
            new EntityPresentation('@', PresentationColor.Yellow));
        editor.SetInitialFacing(playerTemplateId, Direction.East);
        var planId = editor.CreateActionPlan("Player Move East");
        editor.SetActionPlanBehavior(planId, [ActionPlanBehaviorStepKind.MoveFacing]);
        editor.SetDefaultActionPlan(playerTemplateId, planId);
        editor.UpsertScenario(new ScenarioDefinition(
            "persisted-runner",
            "Persisted Runner Scenario",
            scenarioRootId,
            playerTemplateId,
            new EntityId("insertedPlayer"),
            new GridCoord(1, 1)));

        var report = ScenarioRunService.Run(document, new PersistedScenarioRunRequest("persisted-runner", TurnCount: 1));

        Assert.Contains("Run mode: Persisted scenario simulation", report.SetupLines);
        Assert.Contains("Scenario: persisted-runner (Persisted Runner Scenario)", report.SetupLines);
        Assert.Contains("Player: Persisted Runner insertedPlayer at scenarioRoot(1,1), facing East, target none", report.SetupLines);
        Assert.Equal([new EntityId("insertedPlayer")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        Assert.Contains("Persisted Runner: scenarioRoot(2,1), facing East, target none", report.FinalStateLines);
        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeFailures);
    }

    [Fact]
    public void ScenarioRunServiceSummarizesCarriedInventoryContents()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Inventory Summary Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Inventory Summary Room", InventoryWidth: 3, InventoryHeight: 2, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var carrierId = editor.CreateEntityPreset("Report Carrier");
        editor.UpdateEntityPreset(
            carrierId,
            new EntityTemplate("Report Carrier", InventoryWidth: 2, InventoryHeight: 1, Bulk: 1, Aperture: 10),
            new EntityPresentation('c', PresentationColor.Cyan));
        var gemId = editor.CreateEntityPreset("Report Gem");
        editor.UpdateEntityPreset(
            gemId,
            new EntityTemplate("Report Gem", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 1),
            new EntityPresentation('*', PresentationColor.Yellow));

        editor.PlaceCarriedEntity(carrierId, new EntityId("reportGem"), gemId, new GridCoord(1, 0));
        editor.PlaceCarriedEntity(scenarioRootId, new EntityId("reportCarrier"), carrierId, new GridCoord(1, 1));

        var report = ScenarioRunService.Run(document, new ScenarioRunRequest(scenarioRootId, TurnCount: 0));

        Assert.Contains("Report Carrier inventory:", report.InventorySummaryLines);
        Assert.Contains("  - Report Gem reportGem at (1,0)", report.InventorySummaryLines);
    }

    [Fact]
    public void ScenarioInventorySummaryFormatterIsCycleSafe()
    {
        var world = new WorldState();
        var aId = new EntityId("cycleA");
        var bId = new EntityId("cycleB");
        var aInventory = new PlaneId("cycleAInventory");
        var bInventory = new PlaneId("cycleBInventory");
        world.Planes.Add(aInventory, new Plane(aInventory, "Cycle A Inventory", 1, 1));
        world.Planes.Add(bInventory, new Plane(bInventory, "Cycle B Inventory", 1, 1));
        var aSlot = world.AddNode(aInventory, new GridCoord(0, 0));
        var bSlot = world.AddNode(bInventory, new GridCoord(0, 0));
        world.Entities.Add(aId, new Entity(aId, "Cycle A", bSlot, InventoryWidth: 1, InventoryHeight: 1, Bulk: 1, Aperture: 10));
        world.Entities.Add(bId, new Entity(bId, "Cycle B", aSlot, InventoryWidth: 1, InventoryHeight: 1, Bulk: 1, Aperture: 10));
        world.Occupancy.Add(aSlot, bId);
        world.Occupancy.Add(bSlot, aId);
        world.RegisterInventoryPlane(aId, aInventory);
        world.RegisterInventoryPlane(bId, bInventory);

        var lines = ScenarioInventorySummaryFormatter.SummarizeEntityInventory(world, aId);

        Assert.Contains("Cycle A inventory:", lines);
        Assert.Contains("  - Cycle B cycleB at (0,0)", lines);
        Assert.Contains("    Cycle B inventory:", lines);
        Assert.Contains("      - Cycle A cycleA at (0,0)", lines);
        Assert.Contains("        - cycle detected for cycleA; nested contents omitted", lines);
    }

    [Fact]
    public void ScenarioRunServiceShowsBehaviorStepsAndTreatsNoActionAsObservation()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Scenario Duel Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Scenario Duel Room", InventoryWidth: 3, InventoryHeight: 1, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));

        var passiveId = editor.CreateEntityPreset("Passive Walker");
        editor.UpdateEntityPreset(
            passiveId,
            new EntityTemplate("Passive Walker", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 1),
            new EntityPresentation('p', PresentationColor.Green));
        editor.SetInitialFacing(passiveId, Direction.East);
        var passivePlanId = editor.CreateActionPlan("Passive Walker Behavior");
        editor.SetActionPlanBehavior(passivePlanId, [ActionPlanBehaviorStepKind.MoveFacing]);
        editor.SetDefaultActionPlan(passiveId, passivePlanId);

        var destroyerId = editor.CreateEntityPreset("Destroyer Walker");
        editor.UpdateEntityPreset(
            destroyerId,
            new EntityTemplate("Destroyer Walker", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 1),
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
            new EntityTemplate("Alpha Room", InventoryWidth: 4, InventoryHeight: 3, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));

        var playerTemplateId = editor.CreateEntityPreset("Alpha Player");
        editor.UpdateEntityPreset(
            playerTemplateId,
            new EntityTemplate("Alpha Player", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 5),
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
            new EntityTemplate("Blocked Alpha Room", InventoryWidth: 2, InventoryHeight: 1, Bulk: 100, Aperture: 100),
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
            new EntityTemplate("Persisted Alpha Room", InventoryWidth: 3, InventoryHeight: 2, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var playerTemplateId = editor.CreateEntityPreset("Persisted Player");
        editor.UpdateEntityPreset(
            playerTemplateId,
            new EntityTemplate("Persisted Player", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 5),
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
    public void ScenarioDefinitionsRoundTripAuthoredPlayerControlBindings()
    {
        var session = ContentEditorSession.CreateNew();
        var document = session.Document;
        var editor = session.Editor;
        var scenarioRootId = editor.CreateEntityPreset("Controlled Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Controlled Room", InventoryWidth: 3, InventoryHeight: 2, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var playerTemplateId = editor.CreateEntityPreset("Controlled Actor");
        editor.UpdateEntityPreset(
            playerTemplateId,
            new EntityTemplate("Controlled Actor", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 5),
            new EntityPresentation('@', PresentationColor.Yellow));

        editor.UpsertScenario(new ScenarioDefinition(
            "controlled-scenario",
            "Controlled Scenario",
            scenarioRootId,
            playerTemplateId,
            new EntityId("insertedPlayer"),
            new GridCoord(1, 1),
            new Dictionary<string, IReadOnlyList<EntityId>>
            {
                ["player-1"] = [new EntityId("insertedPlayer")]
            }));

        var scenario = document.GetScenario("controlled-scenario");
        var summary = new FrontendEditorService(session).GetSnapshot().Scenarios.Single();

        Assert.Equal([new EntityId("insertedPlayer")], scenario.PlayerControls["player-1"]);
        var summaryControls = Assert.IsType<Dictionary<string, IReadOnlyList<string>>>(summary.PlayerControls);
        Assert.Equal(["insertedPlayer"], summaryControls["player-1"]);
        Assert.True(document.ValidateCanonicalAuthoring().IsValid);
    }

    [Fact]
    public void ScenarioMaterializerResolvesAuthoredPlayerControlBindings()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Resolved Control Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Resolved Control Room", InventoryWidth: 3, InventoryHeight: 2, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var playerTemplateId = editor.CreateEntityPreset("Resolved Control Actor");
        editor.UpdateEntityPreset(
            playerTemplateId,
            new EntityTemplate("Resolved Control Actor", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 5),
            new EntityPresentation('@', PresentationColor.Yellow));

        editor.UpsertScenario(new ScenarioDefinition(
            "resolved-control",
            "Resolved Control",
            scenarioRootId,
            playerTemplateId,
            new EntityId("insertedPlayer"),
            new GridCoord(1, 1),
            new Dictionary<string, IReadOnlyList<EntityId>>
            {
                ["player-1"] = [new EntityId("insertedPlayer")]
            }));

        var materialization = ScenarioMaterializer.Materialize(document, "resolved-control");

        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Equal([new EntityId("insertedPlayer")], materialization.PlayerControls["player-1"]);
        Assert.Equal(EntityControlSource.PlayerChoice, materialization.World.GetActionControlSource(new EntityId("insertedPlayer")));
        Assert.Contains("Control: player-1 -> insertedPlayer", materialization.SetupLines);
    }

    [Fact]
    public void ScenarioMaterializerDefaultsLegacyPlayerControlWhenNoBindingIsAuthored()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Legacy Control Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Legacy Control Room", InventoryWidth: 2, InventoryHeight: 2, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var playerTemplateId = editor.CreateEntityPreset("Legacy Control Player");
        editor.UpdateEntityPreset(
            playerTemplateId,
            new EntityTemplate("Legacy Control Player", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 5),
            new EntityPresentation('@', PresentationColor.Yellow));

        editor.UpsertScenario(new ScenarioDefinition(
            "legacy-control",
            "Legacy Control",
            scenarioRootId,
            playerTemplateId,
            new EntityId("legacyPlayer"),
            new GridCoord(0, 0)));

        var materialization = ScenarioMaterializer.Materialize(document, "legacy-control");

        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Equal([new EntityId("legacyPlayer")], materialization.PlayerControls["player-1"]);
    }

    [Fact]
    public void ScenarioValidationReportsMissingControlledEntityReferences()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Control Validation Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Control Validation Room", InventoryWidth: 2, InventoryHeight: 2, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var playerTemplateId = editor.CreateEntityPreset("Control Validation Player");

        editor.UpsertScenario(new ScenarioDefinition(
            "bad-control",
            "Bad Control",
            scenarioRootId,
            playerTemplateId,
            new EntityId("insertedPlayer"),
            new GridCoord(0, 0),
            new Dictionary<string, IReadOnlyList<EntityId>>
            {
                ["player-1"] = [new EntityId("missingActor")]
            }));

        var validation = document.ValidateCanonicalAuthoring();

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Contains("Scenario bad-control player control player-1 references missing entity missingActor", StringComparison.Ordinal));
    }

    [Fact]
    public void ScenarioValidationReportsInvalidPlayerControlBindingShapes()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Control Shape Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Control Shape Room", InventoryWidth: 3, InventoryHeight: 2, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var actorTemplateId = editor.CreateEntityPreset("Control Shape Actor");
        editor.PlaceCarriedEntity(scenarioRootId, new EntityId("authoredActor"), actorTemplateId, new GridCoord(1, 0));
        var playerTemplateId = editor.CreateEntityPreset("Control Shape Player");

        editor.UpsertScenario(new ScenarioDefinition(
            "invalid-control-shapes",
            "Invalid Control Shapes",
            scenarioRootId,
            playerTemplateId,
            new EntityId("insertedPlayer"),
            new GridCoord(0, 0),
            new Dictionary<string, IReadOnlyList<EntityId>>
            {
                ["player-1"] = [],
                ["player-2"] = [new EntityId("authoredActor"), new EntityId("authoredActor")],
                ["player-3"] = [new EntityId("authoredActor")]
            }));

        var validation = document.ValidateCanonicalAuthoring();

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Contains("Scenario invalid-control-shapes player control player-1 has no controlled entities", StringComparison.Ordinal));
        Assert.Contains(validation.Errors, error => error.Contains("Scenario invalid-control-shapes player control player-2 lists entity authoredActor more than once", StringComparison.Ordinal));
        Assert.Contains(validation.Errors, error => error.Contains("Scenario invalid-control-shapes controlled entity authoredActor is assigned to both player-2 and player-3", StringComparison.Ordinal));
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
