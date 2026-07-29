using System.Text;
using System.Runtime.CompilerServices;
using GameGameGame.Content;
using GameGameGame.Core;
using GameGameGame.Headless;

namespace GameGameGame.Tests;

public sealed class ScenarioRunReportTests
{
    [Fact]
    public void CanonicalMoveShowcaseScenariosLoadValidateAndRun()
    {
        var path = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Beta", "CanonicalActions", "CanonicalMoveShowcase.yaml"));
        var document = EditableContentDocument.LoadYaml(File.ReadAllText(path));
        var validation = new ContentEditorService(document).Validate();

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));

        var outcomeReport = ScenarioRunService.Run(document, new PersistedScenarioRunRequest("beta-canonical-move-outcomes", TurnCount: 1));
        var playerReport = ScenarioRunService.Run(document, new PersistedScenarioRunRequest("beta-canonical-move-player-interaction", TurnCount: 0));

        Assert.Empty(outcomeReport.ValidationDiagnostics);
        Assert.Contains("Forward from North Probe: scenarioRoot(1,0), facing North, target none", outcomeReport.FinalStateLines);
        Assert.Contains("Back from North Probe: scenarioRoot(3,2), facing South, target none", outcomeReport.FinalStateLines);
        Assert.Contains("Diagonal One-Corner Allowed Probe: scenarioRoot(2,4), facing NorthEast, target none", outcomeReport.FinalStateLines);
        Assert.Contains("Diagonal Two-Corner Blocked Probe: scenarioRoot(4,5), facing South, target none", outcomeReport.FinalStateLines);
        Assert.Contains("Entity Block Failure Probe: scenarioRoot(8,5), facing East, target none", outcomeReport.FinalStateLines);
        Assert.Empty(playerReport.ValidationDiagnostics);
    }

    [Fact]
    public void CanonicalPickupDropShowcaseScenariosLoadValidateRunAndExposePlayerChoices()
    {
        var path = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Beta", "CanonicalActions", "CanonicalPickupDropShowcase.yaml"));
        var document = EditableContentDocument.LoadYaml(File.ReadAllText(path));
        var validation = new ContentEditorService(document).Validate();
        var canonicalValidation = document.ValidateCanonicalAuthoring();

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.True(canonicalValidation.IsValid, string.Join(Environment.NewLine, canonicalValidation.Errors));

        var outcomeReport = ScenarioRunService.Run(document, new PersistedScenarioRunRequest("beta-canonical-pickup-drop-outcomes", TurnCount: 1));

        Assert.Empty(outcomeReport.ValidationDiagnostics);
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Pickup Success Actor" && turn.TraceLines.Contains("1. PickupTarget: Success; fallback=stopped"));
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Pickup Failure Actor" && turn.TraceLines.Any(line => line.Contains("PickupTarget: Failure", StringComparison.Ordinal)));
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Drop Success Actor" && turn.TraceLines.Contains("1. DropFacing: Success; fallback=stopped"));
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Drop Failure Actor" && turn.TraceLines.Any(line => line.Contains("DropFacing: Failure", StringComparison.Ordinal)));
        Assert.Contains("Pickup Success Actor inventory:", outcomeReport.InventorySummaryLines);
        Assert.Contains("  - Pickup Success Gem pickupSuccessGem at (0,0)", outcomeReport.InventorySummaryLines);
        Assert.Contains("Pickup Failure Boulder: scenarioRoot(2,3), facing none, target none", outcomeReport.FinalStateLines);
        Assert.Contains("Drop Success Pebble: scenarioRoot(6,1), facing none, target none", outcomeReport.FinalStateLines);
        Assert.Contains("Drop Failure Actor inventory:", outcomeReport.InventorySummaryLines);
        Assert.Contains("  - Drop Failure Pebble dropFailurePebble at (0,0)", outcomeReport.InventorySummaryLines);

        var materialization = ScenarioMaterializer.Materialize(document, "beta-canonical-pickup-drop-player-interaction");
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Contains("Control: player-1 -> canonicalPickupDropPlayer", materialization.SetupLines);

        var descriptor = materialization.Registry.ActionPlanDescriptors[new ActionPlanTemplateId("canonicalPickupDropPlayerChoices")];
        var request = new ActionChoiceService(new MovementService()).CreateRequest(
            materialization.World,
            new EntityId("canonicalPickupDropPlayer"),
            descriptor);

        Assert.NotNull(request);
        var pickup = Assert.Single(request!.Choices, choice => choice.Kind == ActionChoiceKind.Pickup);
        Assert.Contains(pickup.EntityOptions, option => option.TargetId == new EntityId("playerPickupGem") && option.CanExecute);
        Assert.Contains(
            pickup.Destinations(new EntityId("playerPickupGem")),
            destination => destination.CanExecute && destination.Destination.Coord == new GridCoord(1, 0));
        var drop = Assert.Single(request.Choices, choice => choice.Kind == ActionChoiceKind.Drop);
        Assert.Contains(drop.EntityOptions, option => option.TargetId == new EntityId("playerCarriedPebble") && option.CanExecute);
        Assert.Contains(
            drop.Destinations(new EntityId("playerCarriedPebble")),
            destination => destination.Destination.Coord == new GridCoord(5, 3) && !destination.CanExecute && destination.BlockingEntityId == new EntityId("playerDropBlocker"));
        Assert.Contains(
            drop.Destinations(new EntityId("playerCarriedPebble")),
            destination => destination.CanExecute && destination.Destination.Coord == new GridCoord(5, 4));
    }

    [Fact]
    public void CanonicalEnterExitShowcaseScenariosLoadValidateAndPreferPlacedPlayerController()
    {
        var path = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Beta", "CanonicalActions", "CanonicalEnterExitShowcase.yaml"));
        var document = EditableContentDocument.LoadYaml(File.ReadAllText(path));
        var validation = new ContentEditorService(document).Validate();
        var canonicalValidation = document.ValidateCanonicalAuthoring();

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.True(canonicalValidation.IsValid, string.Join(Environment.NewLine, canonicalValidation.Errors));

        var materialization = ScenarioMaterializer.Materialize(document, "beta-canonical-exit-player-interaction");

        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Contains(new EntityId("exitPlayerChoiceBoxCanonicalExitPlayer"), materialization.PlayerControls["player-1"]);
        Assert.False(materialization.World.Entities.ContainsKey(new EntityId("canonicalExitInteractionObserver")));
    }

    [Fact]
    public void CanonicalTransferShowcaseScenariosLoadValidateRunAndExposeManualPlayerPlan()
    {
        var path = FindRepositoryFile(Path.Combine("src", "GameGameGame.Content", "Beta", "CanonicalActions", "CanonicalTransferShowcase.yaml"));
        var document = EditableContentDocument.LoadYaml(File.ReadAllText(path));
        var validation = new ContentEditorService(document).Validate();
        var canonicalValidation = document.ValidateCanonicalAuthoring();

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.True(canonicalValidation.IsValid, string.Join(Environment.NewLine, canonicalValidation.Errors));

        var outcomeReport = ScenarioRunService.Run(document, new PersistedScenarioRunRequest("beta-canonical-transfer-outcomes", TurnCount: 1));

        Assert.Empty(outcomeReport.ValidationDiagnostics);
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Transfer Give Success Actor" && turn.TraceLines.Contains("1. Transfer: Success; fallback=stopped"));
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Transfer Take Success Actor" && turn.TraceLines.Contains("1. Transfer: Success; fallback=stopped"));
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Transfer Missing Target Actor" && turn.TraceLines.Any(line => line.Contains("Transfer: Failure", StringComparison.Ordinal)));
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Transfer No Counterparty Actor" && turn.TraceLines.Any(line => line.Contains("reason=TargetMissing", StringComparison.Ordinal)));
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Transfer No Inventory Actor" && turn.TraceLines.Any(line => line.Contains("reason=TargetHasNoInventory", StringComparison.Ordinal)));
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Transfer Not In Actor Actor" && turn.TraceLines.Any(line => line.Contains("reason=TargetNotInInventory", StringComparison.Ordinal)));
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Transfer Destination Full Actor" && turn.TraceLines.Any(line => line.Contains("reason=InvalidPlacement", StringComparison.Ordinal)));
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Transfer Destination Aperture Actor" && turn.TraceLines.Any(line => line.Contains("reason=ApertureBlocked", StringComparison.Ordinal)));
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Transfer Source Aperture Actor" && turn.TraceLines.Any(line => line.Contains("reason=ApertureBlocked", StringComparison.Ordinal)));
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Transfer Take Not In Source Actor" && turn.TraceLines.Any(line => line.Contains("reason=TargetNotInInventory", StringComparison.Ordinal)));
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Transfer Actor Full Actor" && turn.TraceLines.Any(line => line.Contains("reason=InvalidPlacement", StringComparison.Ordinal)));
        Assert.Contains(outcomeReport.Turns, turn => turn.ActorName == "Transfer Exit Policy Actor" && turn.TraceLines.Any(line => line.Contains("reason=InventoryPolicyBlocked", StringComparison.Ordinal)));
        Assert.Contains("  - Transfer Gem giveSuccessGem at (0,0)", outcomeReport.InventorySummaryLines);
        Assert.Contains("  - Transfer Gem takeSuccessGem at (0,0)", outcomeReport.InventorySummaryLines);

        var materialization = ScenarioMaterializer.Materialize(document, "beta-canonical-transfer-player-interaction");
        Assert.Empty(materialization.ValidationDiagnostics);
        Assert.Empty(materialization.RuntimeFailures);
        Assert.Contains("Control: player-1 -> canonicalTransferPlayer", materialization.SetupLines);

        var descriptor = materialization.Registry.ActionPlanDescriptors[new ActionPlanTemplateId("transferPlayerGiveTakePlan")];
        Assert.Collection(
            descriptor.Behavior!.Steps,
            giveStep =>
            {
                Assert.Equal(ActionPlanBehaviorStepKind.Transfer, giveStep.Kind);
                Assert.Equal(TransferDirection.ActorToTarget, giveStep.TransferDirection);
                Assert.Equal(ActionPlanMoveDirectionMode.Forward, giveStep.DirectionMode);
                Assert.Equal(1, giveStep.TargetSlot);
            },
            takeStep =>
            {
                Assert.Equal(ActionPlanBehaviorStepKind.Transfer, takeStep.Kind);
                Assert.Equal(TransferDirection.TargetToActor, takeStep.TransferDirection);
                Assert.Equal(ActionPlanMoveDirectionMode.Forward, takeStep.DirectionMode);
                Assert.Equal(1, takeStep.TargetSlot);
            });
    }

    [Fact]
    public void TargetPathMovementFailureScenarioEmitsDistinctStructuredLogs()
    {
        var document = CreateTargetPathFailureScenarioDocument();
        var validation = new ContentEditorService(document).Validate();
        var canonicalValidation = document.ValidateCanonicalAuthoring();

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.True(canonicalValidation.IsValid, string.Join(Environment.NewLine, canonicalValidation.Errors));

        var report = ScenarioRunService.Run(document, new PersistedScenarioRunRequest("beta-canonical-target-path-failure-logs", TurnCount: 1));

        Assert.Empty(report.ValidationDiagnostics);
        AssertTargetPathTurnContains(report, "Missing Target Actor", "missing target label target");
        AssertTargetPathTurnContains(report, "Off Plane Actor", "missing Target slot");
        AssertTargetPathTurnContains(report, "Unreachable Adjacency Actor", "no reachable target-adjacent");
        AssertTargetPathTurnContains(report, "No Flee Actor", "no valid distance-increasing flee step");
        AssertTargetPathTurnContains(report, "Blocked Orbit Actor", "orbit Clockwise step East blocked");
        AssertTargetPathTurnContains(report, "Already Adjacent Actor", "already at target adjacency");
        Assert.Contains("No Flee Actor: scenarioRoot(0,0), facing West, target noFleeTarget", report.FinalStateLines);
        Assert.Contains("Blocked Orbit Actor: scenarioRoot(13,3), facing West, target blockedOrbitTarget", report.FinalStateLines);
        Assert.Empty(report.RuntimeFailures);
    }

    [Fact]
    public void TargetPathMovementMazeScenarioDemonstratesSeekAndFleePathfinding()
    {
        var document = CreateTargetPathMazeScenarioDocument();
        var report = ScenarioRunService.Run(document, new PersistedScenarioRunRequest("beta-canonical-target-path-maze", TurnCount: 1));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Contains(report.Turns, turn => turn.ActorName == "Maze Seeking Actor" && turn.TraceLines.Any(line => line.Contains("moved South toward target adjacency", StringComparison.Ordinal)));
        Assert.Contains(report.Turns, turn => turn.ActorName == "Maze Fleeing Actor" && turn.TraceLines.Any(line => line.Contains("away from target adjacency", StringComparison.Ordinal)));
        Assert.Contains("Maze Seeking Actor: scenarioRoot(1,2), facing South, target mazeSeekBeacon", report.FinalStateLines);
        Assert.Contains("Maze Fleeing Actor: scenarioRoot(11,1), facing East, target mazeFleeBeacon", report.FinalStateLines);
        Assert.Empty(report.RuntimeFailures);
        Assert.Empty(report.CapabilityGaps);
    }

    [Fact]
    public void TargetPathMovementOrbitScenarioDemonstratesOppositeDirectionOrbiters()
    {
        var document = CreateTargetPathOrbitScenarioDocument();
        var report = ScenarioRunService.Run(document, new PersistedScenarioRunRequest("beta-canonical-target-path-dual-orbit", TurnCount: 1));

        Assert.Empty(report.ValidationDiagnostics);
        Assert.Contains(report.Turns, turn => turn.ActorName == "Clockwise Close Orbiter" && turn.TraceLines.Any(line => line.Contains("orbit Clockwise", StringComparison.Ordinal) && line.Contains("desiredDistance=2", StringComparison.Ordinal)));
        Assert.Contains(report.Turns, turn => turn.ActorName == "Anticlockwise Far Orbiter" && turn.TraceLines.Any(line => line.Contains("orbit Anticlockwise", StringComparison.Ordinal) && line.Contains("desiredDistance=4", StringComparison.Ordinal)));
        Assert.Contains("Clockwise Close Orbiter: scenarioRoot(6,3), facing East, target orbitPlayer", report.FinalStateLines);
        Assert.Contains("Anticlockwise Far Orbiter: scenarioRoot(4,1), facing West, target orbitPlayer", report.FinalStateLines);
        Assert.Contains("Orbit Player: scenarioRoot(6,6), facing none, target none", report.FinalStateLines);
        Assert.Empty(report.RuntimeFailures);
    }

    private static void AssertTargetPathTurnContains(GameGameGame.Content.ScenarioRunReport report, string actorName, string expectedDetail)
    {
        var turn = Assert.Single(report.Turns, turn => turn.ActorName == actorName);
        Assert.Contains(turn.TraceLines, line => line.Contains("1. TargetPathMove: Failure", StringComparison.Ordinal));
        Assert.Contains(turn.TraceLines, line => line.Contains(expectedDetail, StringComparison.Ordinal));
    }

    private static EditableContentDocument CreateTargetPathFailureScenarioDocument()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var root = CreateTemplate(editor, "Target Path Failure Log Room", '#', 22, 10, bulk: 100, aperture: 100);
        var blocker = CreateTemplate(editor, "Target Path Blocker", 'x');
        var missingCandidate = CreateTemplate(editor, "Missing Candidate Beacon", '?');

        var seekPlan = CreateTargetPathPlan(editor, "Failure Seek Target", ActionPlanTargetPathMode.SeekAdjacency);
        var offPlanePlan = CreateTargetPathPlan(editor, "Failure Off Plane Target", ActionPlanTargetPathMode.SeekAdjacency, targetLabel: null);
        var fleePlan = CreateTargetPathPlan(editor, "Failure Flee Target", ActionPlanTargetPathMode.FleeAdjacency);
        var orbitPlan = CreateTargetPathPlan(editor, "Failure Orbit Target", ActionPlanTargetPathMode.Orbit, desiredDistance: 2, orbitDirection: ActionPlanOrbitDirection.Clockwise);

        var missingActor = CreateTargetPathActor(editor, "Missing Target Actor", 'm', seekPlan, missingCandidate, range: 1);
        editor.PlaceCarriedEntity(root, new EntityId("missingTargetActor"), missingActor, new GridCoord(4, 0));

        var offPlaneBeacon = CreateTemplate(editor, "Off Plane Beacon", 'b');
        var offPlaneActor = CreateTargetPathActor(editor, "Off Plane Actor", 'o', offPlanePlan, offPlaneBeacon, range: 4, inventoryWidth: 1, inventoryHeight: 1, locality: new TargetingLocalityQuery([TargetingLocalityOrigin.OwnInventory]));
        editor.PlaceCarriedEntity(offPlaneActor, new EntityId("offPlaneBeacon"), offPlaneBeacon, new GridCoord(0, 0));
        var offPlaneActorModel = editor.GetEntityPreset(offPlaneActor);
        editor.UpdateEntityPreset(
            offPlaneActor,
            offPlaneActorModel.Template with { ActionStateDefaults = new ActorActionStateDefaults(Direction.West, new EntityId("offPlaneBeacon")) },
            offPlaneActorModel.Presentation);
        editor.PlaceCarriedEntity(root, new EntityId("offPlaneActor"), offPlaneActor, new GridCoord(7, 0));

        var unreachableBeacon = CreateTemplate(editor, "Unreachable Beacon", 'u');
        var unreachableActor = CreateTargetPathActor(editor, "Unreachable Adjacency Actor", 'u', seekPlan, unreachableBeacon, range: 10);
        editor.PlaceCarriedEntity(root, new EntityId("unreachableActor"), unreachableActor, new GridCoord(1, 6));
        editor.PlaceCarriedEntity(root, new EntityId("unreachableTarget"), unreachableBeacon, new GridCoord(4, 6));
        foreach (var coord in Around(new GridCoord(4, 6))) editor.PlaceCarriedEntity(root, new EntityId($"unreachableBlocker{coord.X}_{coord.Y}"), blocker, coord);

        var noFleeBeacon = CreateTemplate(editor, "No Flee Beacon", 'f');
        var noFleeActor = CreateTargetPathActor(editor, "No Flee Actor", 'f', fleePlan, noFleeBeacon, range: 3);
        editor.PlaceCarriedEntity(root, new EntityId("noFleeActor"), noFleeActor, new GridCoord(0, 0));
        editor.PlaceCarriedEntity(root, new EntityId("noFleeTarget"), noFleeBeacon, new GridCoord(1, 1));

        var blockedOrbitBeacon = CreateTemplate(editor, "Blocked Orbit Beacon", 'q');
        var blockedOrbitActor = CreateTargetPathActor(editor, "Blocked Orbit Actor", 'q', orbitPlan, blockedOrbitBeacon, range: 10);
        editor.PlaceCarriedEntity(root, new EntityId("blockedOrbitActor"), blockedOrbitActor, new GridCoord(13, 3));
        editor.PlaceCarriedEntity(root, new EntityId("blockedOrbitTarget"), blockedOrbitBeacon, new GridCoord(14, 6));
        editor.PlaceCarriedEntity(root, new EntityId("blockedOrbitBlocker"), blocker, new GridCoord(14, 3));

        var adjacentBeacon = CreateTemplate(editor, "Already Adjacent Beacon", 'a');
        var adjacentActor = CreateTargetPathActor(editor, "Already Adjacent Actor", 'a', seekPlan, adjacentBeacon, range: 3);
        editor.PlaceCarriedEntity(root, new EntityId("alreadyAdjacentActor"), adjacentActor, new GridCoord(18, 5));
        editor.PlaceCarriedEntity(root, new EntityId("alreadyAdjacentTarget"), adjacentBeacon, new GridCoord(19, 5));

        editor.UpsertScenario(new ScenarioDefinition("beta-canonical-target-path-failure-logs", "Canonical Target Path Failure Logs", root, null, null, null));
        return document;
    }

    private static EditableContentDocument CreateTargetPathMazeScenarioDocument()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var root = CreateTemplate(editor, "Target Path Maze Room", '#', 16, 8, bulk: 100, aperture: 100);
        var blocker = CreateTemplate(editor, "Maze Wall", 'x');
        var seekBeacon = CreateTemplate(editor, "Maze Seek Beacon", 's');
        var fleeBeacon = CreateTemplate(editor, "Maze Flee Beacon", 'f');
        var seekPlan = CreateTargetPathPlan(editor, "Maze Seek", ActionPlanTargetPathMode.SeekAdjacency);
        var fleePlan = CreateTargetPathPlan(editor, "Maze Flee", ActionPlanTargetPathMode.FleeAdjacency);
        var seeker = CreateTargetPathActor(editor, "Maze Seeking Actor", 'S', seekPlan, seekBeacon, range: 12);
        var fleer = CreateTargetPathActor(editor, "Maze Fleeing Actor", 'F', fleePlan, fleeBeacon, range: 12);

        editor.PlaceCarriedEntity(root, new EntityId("mazeSeekingActor"), seeker, new GridCoord(1, 1));
        editor.PlaceCarriedEntity(root, new EntityId("mazeSeekBeacon"), seekBeacon, new GridCoord(5, 1));
        foreach (var coord in new[] { new GridCoord(2, 0), new GridCoord(2, 1), new GridCoord(2, 2) }) editor.PlaceCarriedEntity(root, new EntityId($"seekWall{coord.X}_{coord.Y}"), blocker, coord);

        editor.PlaceCarriedEntity(root, new EntityId("mazeFleeingActor"), fleer, new GridCoord(10, 1));
        editor.PlaceCarriedEntity(root, new EntityId("mazeFleeBeacon"), fleeBeacon, new GridCoord(9, 1));
        foreach (var coord in new[] { new GridCoord(10, 0), new GridCoord(10, 2), new GridCoord(9, 0), new GridCoord(9, 2), new GridCoord(11, 0), new GridCoord(11, 2) }) editor.PlaceCarriedEntity(root, new EntityId($"fleeWall{coord.X}_{coord.Y}"), blocker, coord);

        editor.UpsertScenario(new ScenarioDefinition("beta-canonical-target-path-maze", "Canonical Target Path Maze", root, null, null, null));
        return document;
    }

    private static EditableContentDocument CreateTargetPathOrbitScenarioDocument()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var root = CreateTemplate(editor, "Target Path Dual Orbit Room", '#', 13, 13, bulk: 100, aperture: 100);
        var player = CreateTemplate(editor, "Orbit Player", '@');
        var clockwisePlan = CreateTargetPathPlan(editor, "Orbit Clockwise Distance Two", ActionPlanTargetPathMode.Orbit, desiredDistance: 2, orbitDirection: ActionPlanOrbitDirection.Clockwise, targetLabel: "player");
        var anticlockwisePlan = CreateTargetPathPlan(editor, "Orbit Anticlockwise Distance Four", ActionPlanTargetPathMode.Orbit, desiredDistance: 4, orbitDirection: ActionPlanOrbitDirection.Anticlockwise, targetLabel: "player");
        var clockwise = CreateTargetPathActor(editor, "Clockwise Close Orbiter", 'c', clockwisePlan, player, range: 12, targetLabel: "player");
        var anticlockwise = CreateTargetPathActor(editor, "Anticlockwise Far Orbiter", 'a', anticlockwisePlan, player, range: 12, targetLabel: "player");

        editor.PlaceCarriedEntity(root, new EntityId("clockwiseCloseOrbiter"), clockwise, new GridCoord(5, 3));
        editor.PlaceCarriedEntity(root, new EntityId("anticlockwiseFarOrbiter"), anticlockwise, new GridCoord(5, 1));
        editor.PlaceCarriedEntity(root, new EntityId("orbitPlayer"), player, new GridCoord(6, 6));
        editor.UpsertScenario(new ScenarioDefinition("beta-canonical-target-path-dual-orbit", "Canonical Target Path Dual Orbit", root, null, null, null));
        return document;
    }

    private static EntityTemplateId CreateTemplate(ContentEditorService editor, string name, char glyph, int width = 0, int height = 0, int bulk = 1, int aperture = 1)
    {
        var id = editor.CreateEntityPreset(name);
        editor.UpdateEntityPreset(id, new EntityTemplate(name, width, height, bulk, aperture), new EntityPresentation(glyph, PresentationColor.White));
        return id;
    }

    private static ActionPlanTemplateId CreateTargetPathPlan(ContentEditorService editor, string name, ActionPlanTargetPathMode mode, int? desiredDistance = null, ActionPlanOrbitDirection? orbitDirection = null, string? targetLabel = "target")
    {
        var plan = editor.CreateActionPlan(name);
        editor.SetActionPlanBehavior(plan, [new ActionPlanBehaviorStepDescriptor(ActionPlanBehaviorStepKind.TargetPathMove, TargetLabel: targetLabel, PathMode: mode, DesiredDistance: desiredDistance, OrbitDirection: orbitDirection)]);
        return plan;
    }

    private static EntityTemplateId CreateTargetPathActor(ContentEditorService editor, string name, char glyph, ActionPlanTemplateId plan, EntityTemplateId targetTemplate, int range, int inventoryWidth = 0, int inventoryHeight = 0, TargetingLocalityQuery? locality = null, string targetLabel = "target")
    {
        var actor = CreateTemplate(editor, name, glyph, inventoryWidth, inventoryHeight, bulk: 1, aperture: 10);
        editor.SetInitialFacing(actor, Direction.West);
        editor.SetDefaultActionPlan(actor, plan);
        editor.SetTargetingRule(actor, new EntityTargetingRule(1, targetTemplate, range, Hint: targetLabel, Label: targetLabel, Locality: locality));
        return actor;
    }

    private static IEnumerable<GridCoord> Around(GridCoord center) => DirectionMath.AllDirections.Select(direction => center.Offset(direction));

    private static string FindRepositoryFile(string relativePath, [CallerFilePath] string sourceFilePath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourceFilePath) ?? AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.GetFullPath(relativePath);
    }

    [Fact]
    public void ScenarioRunServiceCanUseEditorAuthoredTemporaryContentForReport()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Scenario Room");
        var actorTemplateId = editor.CreateEntityPreset("Scenario Actor");
        var rockTemplateId = editor.CreateEntityPreset("Scenario Rock");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Scenario Room", InventoryWidth: 4, InventoryHeight: 5, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        editor.UpdateEntityPreset(
            actorTemplateId,
            new EntityTemplate("Scenario Actor", InventoryWidth: 3, InventoryHeight: 2, Bulk: 10, Aperture: 5),
            new EntityPresentation('@', PresentationColor.White));
        editor.UpdateEntityPreset(
            rockTemplateId,
            new EntityTemplate("Scenario Rock", InventoryWidth: 0, InventoryHeight: 0, Bulk: 3, Aperture: 3),
            new EntityPresentation('*', PresentationColor.Gray));
        editor.SetInitialFacing(actorTemplateId, Direction.East);
        editor.PlaceCarriedEntity(actorTemplateId, new EntityId("carriedRock"), rockTemplateId, new GridCoord(0, 0));
        var planTemplateId = editor.CreateActionPlan("Drop Facing");
        editor.SetActionPlanBehavior(planTemplateId, [ActionPlanBehaviorStepKind.DropFacing]);
        editor.SetDefaultActionPlan(actorTemplateId, planTemplateId);
        editor.PlaceCarriedEntity(scenarioRootId, new EntityId("actor"), actorTemplateId, new GridCoord(1, 2));
        var validation = editor.Validate();
        var canonicalValidation = document.ValidateCanonicalAuthoring();
        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.True(canonicalValidation.IsValid, string.Join(Environment.NewLine, canonicalValidation.Errors));

        var report = ScenarioRunService.Run(document, new ScenarioRunRequest(scenarioRootId, TurnCount: 1));

        Assert.Equal([new EntityId("actor")], report.ActorOrder.Select(actor => actor.EntityId).ToArray());
        var turn = Assert.Single(report.Turns);
        Assert.Equal("Scenario Actor", turn.ActorName);
        Assert.Contains("1. DropFacing: Success; fallback=stopped", turn.TraceLines);
        Assert.Contains("   reads: Facing=East", turn.TraceLines);
        Assert.Contains("Scenario Actor: scenarioRoot(1,2), facing East, target none", report.FinalStateLines);
        Assert.Contains("Scenario Rock: scenarioRoot(2,2), facing none, target none", report.FinalStateLines);
        Assert.Empty(report.ValidationDiagnostics);
        Assert.Empty(report.RuntimeObservations);
        Assert.Empty(report.RuntimeFailures);
        Assert.Empty(report.CapabilityGaps);
    }

    [Fact]
    public void ScenarioRunServiceReportsMultiTurnMoveFacingScenario()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var scenarioRootId = editor.CreateEntityPreset("Scenario Room");
        editor.UpdateEntityPreset(
            scenarioRootId,
            new EntityTemplate("Scenario Room", InventoryWidth: 5, InventoryHeight: 5, Bulk: 100, Aperture: 100),
            new EntityPresentation('#', PresentationColor.Gray));
        var actorTemplateId = editor.CreateEntityPreset("Player");
        editor.UpdateEntityPreset(
            actorTemplateId,
            new EntityTemplate("Player", InventoryWidth: 0, InventoryHeight: 0, Bulk: 1, Aperture: 1),
            new EntityPresentation('@', PresentationColor.White));
        editor.SetInitialFacing(actorTemplateId, Direction.East);
        var planTemplateId = editor.CreateActionPlan("Move Facing");
        editor.SetActionPlanBehavior(planTemplateId, [ActionPlanBehaviorStepKind.MoveFacing]);
        editor.SetDefaultActionPlan(actorTemplateId, planTemplateId);
        editor.PlaceCarriedEntity(scenarioRootId, new EntityId("player"), actorTemplateId, new GridCoord(1, 2));

        var report = ScenarioRunService.Run(document, new ScenarioRunRequest(scenarioRootId, TurnCount: 2));

        Assert.Equal([1, 2], report.Turns.Select(turn => turn.TurnNumber).ToArray());
        Assert.All(report.Turns, turn => Assert.Equal("Player", turn.ActorName));
        Assert.All(report.Turns, turn => Assert.Contains("1. MoveFacing: Success; fallback=stopped", turn.TraceLines));
        Assert.Contains("Player: scenarioRoot(3,2), facing East, target none", report.FinalStateLines);
        Assert.Empty(report.RuntimeObservations);
    }

    [Fact]
    public void ScenarioRunnerReportsPickupTargetScenario()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.RockId);
        var scenario = new HeadlessScenario(
            "actor_picks_up_target_rock",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("pickup-target", ActionPlanBehaviorStepKind.PickupTarget),
            [TestWorld.PlayerId, TestWorld.RockId]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 1).FormatText();

        Assert.Contains("- 1. PickupTarget: Success; fallback=stopped", report, StringComparison.Ordinal);
        Assert.Contains("-    reads: Target=rock", report, StringComparison.Ordinal);
        Assert.Contains("- Rock: player(0,0), facing none, target none", report, StringComparison.Ordinal);
        Assert.Contains("Diagnostics:\n- none", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerReportsGiveTargetScenario()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.Entities[TestWorld.PlayerId] = world.Entities[TestWorld.PlayerId] with { Aperture = 20 };
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.PlayerInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var scenario = new HeadlessScenario(
            "actor_gives_carried_rock_to_target",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("give-target", ActionPlanBehaviorStepKind.GiveTarget),
            [TestWorld.PlayerId, TestWorld.SlimeId, TestWorld.RockId]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 1).FormatText();

        Assert.Contains("- 1. GiveTarget: Success; fallback=stopped", report, StringComparison.Ordinal);
        Assert.Contains("-    reads: Target=slime", report, StringComparison.Ordinal);
        Assert.Contains("gave rock (Rock) from (0,0) to (0,0)", report, StringComparison.Ordinal);
        Assert.Contains("- Rock: slime(0,0), facing none, target none", report, StringComparison.Ordinal);
        Assert.Contains("Diagnostics:\n- none", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerReportsPushFacingScenario()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.North);
        var scenario = new HeadlessScenario(
            "actor_pushes_blocker_north",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("push-facing", ActionPlanBehaviorStepKind.PushFacing),
            [TestWorld.PlayerId, TestWorld.SlimeId]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 1).FormatText();

        Assert.Contains("- 1. PushFacing: Success; fallback=stopped", report, StringComparison.Ordinal);
        Assert.Contains("-    reads: Facing=North", report, StringComparison.Ordinal);
        Assert.Contains("- Player: world(1,1), facing North, target none", report, StringComparison.Ordinal);
        Assert.Contains("- Slime: world(1,0), facing none, target none", report, StringComparison.Ordinal);
        Assert.Contains("Diagnostics:\n- none", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerReportsDestroyTargetScenario()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.SlimeInventoryPlaneId, new GridCoord(0, 0))));
        world.SetActionTarget(TestWorld.PlayerId, TestWorld.SlimeId);
        var scenario = new HeadlessScenario(
            "actor_destroys_target_and_inventory",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("destroy-target", ActionPlanBehaviorStepKind.DestroyTarget),
            [TestWorld.PlayerId, TestWorld.SlimeId, TestWorld.RockId]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 1).FormatText();

        Assert.Contains("- 1. DestroyTarget: Success; fallback=stopped", report, StringComparison.Ordinal);
        Assert.Contains("-    reads: Target=slime", report, StringComparison.Ordinal);
        Assert.Contains("- Player: world(1,2), facing none, target slime", report, StringComparison.Ordinal);
        Assert.Contains("- slime: destroyed", report, StringComparison.Ordinal);
        Assert.Contains("- rock: destroyed", report, StringComparison.Ordinal);
        Assert.Contains("Diagnostics:\n- none", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerReportsCreateFacingScenario()
    {
        var world = TestWorld.CreateWorld();
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        var scenario = new HeadlessScenario(
            "actor_creates_placeholder_rock_facing_east",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("create-facing", ActionPlanBehaviorStepKind.CreateFacing),
            [TestWorld.PlayerId, new EntityId("placeholderRock")]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 1).FormatText();

        Assert.Contains("- 1. CreateFacing: Success; fallback=stopped", report, StringComparison.Ordinal);
        Assert.Contains("-    reads: Facing=East", report, StringComparison.Ordinal);
        Assert.Contains("- Player: world(1,2), facing East, target none", report, StringComparison.Ordinal);
        Assert.Contains("- Placeholder Rock: world(2,2), facing none, target none", report, StringComparison.Ordinal);
        Assert.Contains("Diagnostics:\n- none", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerReportsContentAuthoringValidationFailure()
    {
        var document = new EditableContentDocument();
        var editor = new ContentEditorService(document);
        var invalidActorId = editor.CreateEntityPreset("Invalid Actor");
        document.EntityTemplates[invalidActorId.Value].DefaultActionPlanId = "missingPlan";
        var world = TestWorld.CreateWorld();
        var scenario = new HeadlessScenario(
            "invalid_content_missing_default_plan",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("unused-plan", ActionPlanBehaviorStepKind.MoveFacing),
            [TestWorld.PlayerId],
            Diagnostics:
            [
                $"content authoring: Entity template {invalidActorId} (Invalid Actor) references missing defaultActionPlanId missingPlan."
            ]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 0).FormatText();

        Assert.Contains("Diagnostics:\n- content authoring: Entity template invalidActor (Invalid Actor) references missing defaultActionPlanId missingPlan.", report, StringComparison.Ordinal);
        Assert.Contains("Capability Gaps:\n- none", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerReportsRuntimeExecutionFailure()
    {
        var world = TestWorld.CreateWorld();
        var movement = new MovementService();
        world.SetActionFacing(TestWorld.PlayerId, Direction.East);
        Assert.True(movement.TryPlace(world, TestWorld.RockId, new PlaneCoord(TestWorld.WorldPlaneId, new GridCoord(2, 2))));
        var scenario = new HeadlessScenario(
            "actor_cannot_create_into_occupied_facing_cell",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("create-facing", ActionPlanBehaviorStepKind.CreateFacing),
            [TestWorld.PlayerId, TestWorld.RockId]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 1).FormatText();

        Assert.Contains("- 1. CreateFacing: Failure; reason=InvalidPlacement; fallback=stopped", report, StringComparison.Ordinal);
        Assert.Contains("- Terminal: failed; consumed turn", report, StringComparison.Ordinal);
        Assert.Contains("Diagnostics:\n- runtime execution: Turn 1: plan create-facing failed (cannot create placeholder entity at world(2,2))", report, StringComparison.Ordinal);
        Assert.Contains("- Rock: world(2,2), facing none, target none", report, StringComparison.Ordinal);
        Assert.Contains("Capability Gaps:\n- none", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ScenarioRunnerReportsUnsupportedCapabilityGap()
    {
        var world = TestWorld.CreateWorld();
        var scenario = new HeadlessScenario(
            "request_create_facing_specific_template",
            world,
            TestWorld.PlayerId,
            CreateBehaviorPlan("not-run", ActionPlanBehaviorStepKind.CreateFacing),
            [TestWorld.PlayerId],
            CapabilityGaps:
            [
                "unsupported capability: CreateFacing(templateId) is not available; current CreateFacing creates placeholder rocks only"
            ]);

        var report = MinimalScenarioRunner.Run(scenario, turnCount: 0).FormatText();

        Assert.Contains("Diagnostics:\n- none", report, StringComparison.Ordinal);
        Assert.Contains("Capability Gaps:\n- unsupported capability: CreateFacing(templateId) is not available; current CreateFacing creates placeholder rocks only", report, StringComparison.Ordinal);
    }

    private static ActionPlanDefinition CreateBehaviorPlan(string id, ActionPlanBehaviorStepKind stepKind) =>
        new(
            new ActionPlanId(id),
            [],
            Behavior: new ActionPlanBehaviorDescriptor([new ActionPlanBehaviorStepDescriptor(stepKind)]));

}

internal sealed record HeadlessScenario(
    string Name,
    WorldState World,
    EntityId ActorId,
    ActionPlanDefinition Plan,
    IReadOnlyList<EntityId> WatchedEntityIds,
    IReadOnlyList<string>? Diagnostics = null,
    IReadOnlyList<string>? CapabilityGaps = null);

internal sealed record ScenarioRunReport(
    string ScenarioName,
    IReadOnlyList<string> SetupLines,
    IReadOnlyList<ScenarioTurnReport> Turns,
    IReadOnlyList<string> FinalStateLines,
    IReadOnlyList<string> Diagnostics,
    IReadOnlyList<string> CapabilityGaps)
{
    public string FormatText()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Scenario: {ScenarioName}");
        builder.AppendLine();
        AppendSection(builder, "Setup", SetupLines);
        builder.AppendLine();
        builder.AppendLine("Run:");

        foreach (var turn in Turns)
        {
            builder.AppendLine($"Turn {turn.TurnNumber}: {turn.ActorName} executes {turn.PlanId}");

            foreach (var line in turn.TraceLines)
            {
                builder.AppendLine($"- {line}");
            }
        }

        builder.AppendLine();
        AppendSection(builder, "Final State", FinalStateLines);
        builder.AppendLine();
        AppendSection(builder, "Diagnostics", Diagnostics.Count == 0 ? ["none"] : Diagnostics);
        builder.AppendLine();
        AppendSection(builder, "Capability Gaps", CapabilityGaps.Count == 0 ? ["none"] : CapabilityGaps);
        return builder.ToString().Replace(Environment.NewLine, "\n", StringComparison.Ordinal).TrimEnd();
    }

    private static void AppendSection(StringBuilder builder, string label, IReadOnlyList<string> lines)
    {
        builder.AppendLine($"{label}:");

        foreach (var line in lines)
        {
            builder.AppendLine(line.StartsWith("  - ", StringComparison.Ordinal) ? line : $"- {line}");
        }
    }
}

internal sealed record ScenarioTurnReport(
    int TurnNumber,
    string ActorName,
    ActionPlanId PlanId,
    IReadOnlyList<string> TraceLines);

internal static class MinimalScenarioRunner
{
    public static ScenarioRunReport Run(HeadlessScenario scenario, int turnCount)
    {
        var interpreter = new ActionPlanInterpreter(new MovementService());
        var setupLines = SummarizeSetup(scenario);
        var turns = new List<ScenarioTurnReport>();
        var diagnostics = scenario.Diagnostics?.ToList() ?? [];
        var capabilityGaps = scenario.CapabilityGaps?.ToList() ?? [];

        for (var turn = 1; turn <= turnCount; turn++)
        {
            var result = interpreter.Execute(scenario.World, scenario.ActorId, scenario.Plan, new ActionPlanContext());
            scenario.World.RecordTrace(result.Trace);

            if (result.ConsumesTurn)
            {
                scenario.World.AdvanceTurn();
            }

            turns.Add(new ScenarioTurnReport(
                turn,
                scenario.World.Entities[scenario.ActorId].Name,
                scenario.Plan.Id,
                BehaviorChainTraceFormatter.Format(result)));

            if (!result.Succeeded)
            {
                diagnostics.Add($"runtime execution: Turn {turn}: plan {scenario.Plan.Id} failed ({FindFailureDetail(result.Trace)})");
            }
        }

        return new ScenarioRunReport(
            scenario.Name,
            setupLines,
            turns,
            SummarizeEntities(scenario.World, scenario.WatchedEntityIds),
            diagnostics,
            capabilityGaps);
    }

    private static IReadOnlyList<string> SummarizeSetup(HeadlessScenario scenario)
    {
        var actor = scenario.World.Entities[scenario.ActorId];
        var planSteps = scenario.Plan.Behavior is null
            ? "non-behavior plan"
            : string.Join(", ", scenario.Plan.Behavior.Steps.Select(step => step.Kind));

        var lines = new List<string>
        {
            $"Actor: {actor.Name} at {scenario.World.GetEntityLocation(scenario.ActorId)}, {FormatActionState(scenario.World, scenario.ActorId)}",
            $"Plan: {scenario.Plan.Id} [{planSteps}]",
            "Watched entities:"
        };

        lines.AddRange(SummarizeEntities(scenario.World, scenario.WatchedEntityIds).Select(line => $"  - {line}"));
        return lines;
    }

    private static IReadOnlyList<string> SummarizeEntities(WorldState world, IReadOnlyList<EntityId> entityIds) =>
        entityIds
            .Select(entityId => world.Entities.TryGetValue(entityId, out var entity)
                ? $"{entity.Name}: {world.GetEntityLocation(entityId)}, {FormatActionState(world, entityId)}"
                : $"{entityId}: destroyed")
            .ToList();

    private static string FindFailureDetail(TraceNode trace) =>
        DescendantsAndSelf(trace)
            .Where(node => node.Status == TraceStatus.Failure && !string.IsNullOrWhiteSpace(node.Detail))
            .Select(node => node.Detail!)
            .LastOrDefault()
        ?? trace.Detail
        ?? "no detail";

    private static IEnumerable<TraceNode> DescendantsAndSelf(TraceNode node)
    {
        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var descendant in DescendantsAndSelf(child))
            {
                yield return descendant;
            }
        }
    }

    private static string FormatActionState(WorldState world, EntityId entityId)
    {
        var facing = world.GetActionFacing(entityId)?.ToString() ?? "none";
        var target = world.GetActionTarget(entityId)?.ToString() ?? "none";
        return $"facing {facing}, target {target}";
    }
}
